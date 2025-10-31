// WinPrintServer.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <WinSock2.h>
#include <stdarg.h>
#include <thread>
#include <map>
#include <mutex>

const wchar_t* VERSION = L"v1.3";

// 服务实例结构体
struct ServerInstance {
    int id;
    int port;
    std::wstring printerName;
    std::thread serverThread;
    bool running;
    SOCKET serverSocket;  // 添加这个字段，用于关闭socket
    LPCWSTR dataType;
    void (*logCallback)(int instanceId, const wchar_t* msg);
};

// 全局服务实例管理
std::map<int, ServerInstance> serverInstances;
std::mutex instancesMutex;
int nextInstanceId = 1;

void log(int instanceId, LPCWSTR format, ...);
void logFatal(int instanceId, LPCWSTR msg, int errCode);
void logError(int instanceId, LPCWSTR msg, int errCode);
void ServerLoop(int instanceId, int port, std::wstring printerName);
void showUsage();

#ifdef _WINDLL
// ==================== DLL导出函数 ====================
extern "C" __declspec(dllexport) int StartServer(int port, const wchar_t* printerNameStr) {
    OutputDebugString(L"StartServer called\n");
    
    // 先验证打印机存在
    HANDLE hPrinter = INVALID_HANDLE_VALUE;
    if (!OpenPrinter(const_cast<LPWSTR>(printerNameStr), &hPrinter, NULL))
    {
        OutputDebugString(L"Printer validation failed\n");
        return -1;
    }
    ClosePrinter(hPrinter);
    
    std::lock_guard<std::mutex> lock(instancesMutex);
    
    // 检查端口是否已被使用
    for (const auto& pair : serverInstances) {
        if (pair.second.port == port && pair.second.running) {
            OutputDebugString(L"Port already in use\n");
            return -2;  // 返回特殊错误码表示端口冲突
        }
    }
    
    int instanceId = nextInstanceId++;
    
    // 使用 emplace 直接构造，避免复制
    auto result = serverInstances.emplace(instanceId, ServerInstance());
    ServerInstance& instance = result.first->second;
    
    instance.id = instanceId;
    instance.port = port;
    instance.printerName = printerNameStr;
    instance.running = true;
    instance.logCallback = nullptr;  // 先设为nullptr
    instance.dataType = nullptr;
    
    // 在启动线程前，先不调用任何log
    // 使用 std::move 创建线程
    instance.serverThread = std::thread(
        ServerLoop, instanceId, port, printerNameStr
    );
    
    OutputDebugString(L"Server instance created\n");
    return instanceId;
}

extern "C" __declspec(dllexport) void StopServer(int instanceId) {
    SOCKET sockToClose = INVALID_SOCKET;
    std::thread threadToDetach;
    
    {
        std::lock_guard<std::mutex> lock(instancesMutex);
        
        auto it = serverInstances.find(instanceId);
        if (it == serverInstances.end()) {
            // 实例不存在，直接返回
            return;
        }
        
        // 标记为停止
        it->second.running = false;
        
        // 保存 socket 句柄
        sockToClose = it->second.serverSocket;
        it->second.serverSocket = INVALID_SOCKET;
        
        // 移动线程对象
        if (it->second.serverThread.joinable()) {
            threadToDetach = std::move(it->second.serverThread);
        }
        
        // 从 map 中移除（但还没关闭 socket）
        serverInstances.erase(it);
    }
    
    // 在锁外关闭 socket 和分离线程
    if (sockToClose != INVALID_SOCKET) {
        closesocket(sockToClose);
    }
    
    if (threadToDetach.joinable()) {
        threadToDetach.detach();
    }
    
    // 发送日志（此时实例已被移除，但我们仍可记录）
    wchar_t buffer[128];
    swprintf_s(buffer, 128, L"Server instance %d stopped", instanceId);
    OutputDebugString(buffer);
}

extern "C" __declspec(dllexport) void SetLogCallback(int instanceId, void (*callback)(int, const wchar_t*)) {
    std::lock_guard<std::mutex> lock(instancesMutex);
    
    auto it = serverInstances.find(instanceId);
    if (it != serverInstances.end()) {
        it->second.logCallback = callback;
        // 第一条日志在设置回调之后
        if (callback != nullptr) {
            callback(instanceId, L"Server instance initialized");
        }
    }
}

extern "C" __declspec(dllexport) int GetServerStatus(int instanceId) {
    std::lock_guard<std::mutex> lock(instancesMutex);
    
    auto it = serverInstances.find(instanceId);
    if (it != serverInstances.end()) {
        return it->second.running ? 1 : 0;
    }
    return -1; // 不存在
}

extern "C" __declspec(dllexport) void GetServerInfo(int instanceId, int* port, wchar_t* printerName, int bufferSize) {
    std::lock_guard<std::mutex> lock(instancesMutex);
    
    auto it = serverInstances.find(instanceId);
    if (it != serverInstances.end()) {
        *port = it->second.port;
        wcsncpy_s(printerName, bufferSize, it->second.printerName.c_str(), bufferSize - 1);
    }
}
#endif

void ServerLoop(int instanceId, int port, std::wstring printerName) {
    SOCKET serverSocket = INVALID_SOCKET;
    SOCKET clientSocket = INVALID_SOCKET;
    HANDLE hPrinter = INVALID_HANDLE_VALUE;
    
    try
    {
        WORD wVersionRequested = MAKEWORD(2, 2);
        WSADATA wsaData;
        int status = WSAStartup(wVersionRequested, &wsaData);
        if (status != 0) {
            logFatal(instanceId, L"failed to initialize the socket library", status);
            return;
        }

        serverSocket = socket(AF_INET, SOCK_STREAM, 0);
        if (serverSocket == INVALID_SOCKET)
        {
            logFatal(instanceId, L"failed to create server socket", WSAGetLastError());
            WSACleanup();
            return;
        }

        // 保存 socket 句柄到实例中
        {
            std::lock_guard<std::mutex> lock(instancesMutex);
            auto it = serverInstances.find(instanceId);
            if (it != serverInstances.end()) {
                it->second.serverSocket = serverSocket;
            } else {
                // 实例已被移除，退出
                closesocket(serverSocket);
                WSACleanup();
                return;
            }
        }

        sockaddr_in addr{};
        addr.sin_family = AF_INET;
        addr.sin_port = htons(port);
        addr.sin_addr.S_un.S_addr = htonl(INADDR_ANY);

        if (bind(serverSocket, (const sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR)
        {
            closesocket(serverSocket);
            logFatal(instanceId, L"failed to bind server socket", WSAGetLastError());
            
            // 从实例列表中移除失败的实例
            {
                std::lock_guard<std::mutex> lock(instancesMutex);
                serverInstances.erase(instanceId);
            }
            
            WSACleanup();
            return;
        }

        if (listen(serverSocket, SOMAXCONN) == SOCKET_ERROR)
        {
            closesocket(serverSocket);
            logFatal(instanceId, L"failed to listen on server socket", WSAGetLastError());
            WSACleanup();
            return;
        }

        sockaddr_in client{};
        int size = sizeof(client);

        if (!OpenPrinter(const_cast<LPWSTR>(printerName.c_str()), &hPrinter, NULL))
        {
            logFatal(instanceId, L"printer not available", GetLastError());
            closesocket(serverSocket);
            WSACleanup();
            return;
        }

        DWORD cb;
        DWORD lerr;
        BOOL succeeded = GetPrinterDriver(hPrinter, NULL, 8, NULL, 0, &cb);
        if (!succeeded) 
        {
            lerr = GetLastError();
            if (lerr != ERROR_INSUFFICIENT_BUFFER)
            {
                logFatal(instanceId, L"failed to get printer driver", lerr);
                ClosePrinter(hPrinter);
                closesocket(serverSocket);
                WSACleanup();
                return;
            }
        }
        
        BYTE *pdiBuffer = new BYTE[cb];
        succeeded = GetPrinterDriver(hPrinter, NULL, 8, pdiBuffer, cb, &cb);
        if (!succeeded)
        {
            logFatal(instanceId, L"failed to get printer driver", GetLastError());
            delete[]pdiBuffer;
            ClosePrinter(hPrinter);
            closesocket(serverSocket);
            WSACleanup();
            return;
        }
        DRIVER_INFO_8* pdi8 = (DRIVER_INFO_8*)pdiBuffer;
        LPCWSTR dataType = (pdi8->dwPrinterDriverAttributes & PRINTER_DRIVER_XPS) != 0 ? L"XPS_PASS" : L"RAW";
        delete[]pdiBuffer;
        ClosePrinter(hPrinter);
        hPrinter = INVALID_HANDLE_VALUE;

        // 等待回调设置完成（最多等待500ms）
        for (int i = 0; i < 50; i++) {
            bool hasCallback = false;
            {
                std::lock_guard<std::mutex> lock(instancesMutex);
                auto it = serverInstances.find(instanceId);
                if (it != serverInstances.end() && it->second.logCallback != nullptr) {
                    hasCallback = true;
                }
            }
            if (hasCallback) break;
            std::this_thread::sleep_for(std::chrono::milliseconds(10));
        }

        log(instanceId, L"WinPrintServer %s started", VERSION);
        log(instanceId, L"Listening on port %d for printer '%s' (data type: %s)", port, printerName.c_str(), dataType);

        bool running = true;
        while (running)
        {
            // 检查是否应该继续运行
            {
                std::lock_guard<std::mutex> lock(instancesMutex);
                auto it = serverInstances.find(instanceId);
                if (it == serverInstances.end() || !it->second.running) {
                    running = false;
                    break;
                }
            }

            log(instanceId, L"Waiting for connection...");
            clientSocket = accept(serverSocket, (sockaddr*)&client, &size);
            
            if (clientSocket == INVALID_SOCKET) 
            {
                int wsaError = WSAGetLastError();
                
                // 检查是否因为关闭而失败
                if (wsaError == WSAENOTSOCK || wsaError == WSAEINTR || 
                    wsaError == WSAEINVAL || wsaError == WSAECONNRESET) {
                    log(instanceId, L"Server socket closed, shutting down...");
                    break;
                }
                
                // 再次检查运行状态
                {
                    std::lock_guard<std::mutex> lock(instancesMutex);
                    auto it = serverInstances.find(instanceId);
                    if (it == serverInstances.end() || !it->second.running) {
                        running = false;
                        break;
                    }
                }
                
                if (running)
                {
                    logError(instanceId, L"failed to accept client connection", wsaError);
                }
                continue;
            }
            
            log(instanceId, L"Connection from %d.%d.%d.%d", 
                client.sin_addr.S_un.S_un_b.s_b1, 
                client.sin_addr.S_un.S_un_b.s_b2, 
                client.sin_addr.S_un.S_un_b.s_b3, 
                client.sin_addr.S_un.S_un_b.s_b4);
            
            DOC_INFO_1 di{};
            di.pDocName = const_cast<LPWSTR>(L"RAW Print Job");
            di.pOutputFile = NULL;
            di.pDatatype = const_cast<LPWSTR>(dataType);

            if (!OpenPrinter(const_cast<LPWSTR>(printerName.c_str()), &hPrinter, NULL))
            {
                logError(instanceId, L"printer not available", GetLastError());
                goto cleanup;
            }

            if (StartDocPrinter(hPrinter, 1, (LPBYTE)&di) <= 0)
            {
                logError(instanceId, L"failed to start the print job", GetLastError());
                goto cleanup;
            }

            log(instanceId, L"Print job started");
            char buffer[4096];
            DWORD bytesWritten;
            while (running)
            {
                int bytesRead = recv(clientSocket, buffer, sizeof(buffer), 0);
                if (bytesRead <= 0)
                {
                    break;
                }
                BOOL result = WritePrinter(hPrinter, buffer, bytesRead, &bytesWritten);
                if (!result || bytesWritten != bytesRead)
                {
                    logError(instanceId, L"print failed", GetLastError());
                    break;
                }
            }
            EndDocPrinter(hPrinter);
            log(instanceId, L"Print job completed");

        cleanup:
            if (clientSocket != INVALID_SOCKET)
            {
                closesocket(clientSocket);
                clientSocket = INVALID_SOCKET;
            }

            if (hPrinter != INVALID_HANDLE_VALUE)
            {
                ClosePrinter(hPrinter);
                hPrinter = INVALID_HANDLE_VALUE;
            }
        }
    }
    catch (...)
    {
        log(instanceId, L"Fatal error in ServerLoop");
    }
    
    // 清理资源
    if (clientSocket != INVALID_SOCKET) {
        closesocket(clientSocket);
    }
    if (serverSocket != INVALID_SOCKET) {
        closesocket(serverSocket);
    }
    if (hPrinter != INVALID_HANDLE_VALUE) {
        ClosePrinter(hPrinter);
    }
    WSACleanup();
    
    log(instanceId, L"Server loop exited");
}

void logFatal(int instanceId, LPCWSTR msg, int errCode)
{
    wchar_t buffer[512];
    swprintf_s(buffer, 512, L"fatal: %s(%d)", msg, errCode);
    log(instanceId, buffer);
}

void logError(int instanceId, LPCWSTR msg, int errCode)
{
    wchar_t buffer[512];
    swprintf_s(buffer, 512, L"error: %s(%d)", msg, errCode);
    log(instanceId, buffer);
}

void log(int instanceId, LPCWSTR format, ...)
{
    va_list args;
    va_start(args, format);
    
    wchar_t buffer[512];
    vswprintf_s(buffer, 512, format, args);
    va_end(args);
    
    std::lock_guard<std::mutex> lock(instancesMutex);
    auto it = serverInstances.find(instanceId);
    // 修改：检查logCallback是否为null，只在非null时调用
    if (it != serverInstances.end() && it->second.logCallback != nullptr) {
        it->second.logCallback(instanceId, buffer);
    }
}

void showUsage()
{
#ifdef _WIN64
    wprintf(L"WinPrintServer %s (64bit)\n", VERSION);
#else
    wprintf(L"WinPrintServer %s (32bit)\n", VERSION);
#endif
    wprintf(L"Usage: WinPrintServer [options] [printername]\n");
    wprintf(L"Options:\n");
    wprintf(L"  -h              Show this help information\n");
    wprintf(L"  -p <port>       Specify the port to listen on (default: 9100)\n");
    wprintf(L"printername      The name of the printer. If not specified, uses default printer\n");
    wprintf(L"\nExamples:\n");
    wprintf(L"  WinPrintServer -p 9100 \"HP LaserJet\"\n");
    wprintf(L"  WinPrintServer \"Printer Name\"\n");
}

#ifndef _WINDLL
int wmain(int argc, wchar_t* argv[]) {
    int port = 9100;
    std::wstring printerName;
    
    // 解析命令行参数
    int optind = 1;
    while (optind < argc && argv[optind][0] == L'-')
    {
        if (_wcsicmp(argv[optind], L"-h") == 0 || _wcsicmp(argv[optind], L"--help") == 0)
        {
            showUsage();
            return 0;
        }
        else if (_wcsicmp(argv[optind], L"-p") == 0 || _wcsicmp(argv[optind], L"--port") == 0)
        {
            optind++;
            if (optind >= argc)
            {
                wprintf(L"Error: -p requires a port number\n");
                showUsage();
                return -1;
            }
            port = _wtoi(argv[optind]);
            if (port <= 0 || port > 65535)
            {
                wprintf(L"Error: invalid port number: %d\n", port);
                return -1;
            }
        }
        else
        {
            wprintf(L"Error: unknown option: %s\n", argv[optind]);
            showUsage();
            return -1;
        }
        optind++;
    }

    // 获取打印机名称
    if (optind == argc)
    {
        // 使用默认打印机
        DWORD cb;
        if (!GetDefaultPrinter(NULL, &cb) && GetLastError() != ERROR_INSUFFICIENT_BUFFER)
        {
            wprintf(L"Error: no default printer defined\n");
            showUsage();
            return -1;
        }
        LPWSTR szBuffer = new WCHAR[cb];
        if (!GetDefaultPrinter(szBuffer, &cb))
        {
            wprintf(L"Error: failed to get default printer\n");
            delete[] szBuffer;
            return -1;
        }
        printerName = szBuffer;
        delete[] szBuffer;
        wprintf(L"Using default printer: %s\n", printerName.c_str());
    }
    else if (optind == argc - 1)
    {
        printerName = argv[optind];
    }
    else
    {
        wprintf(L"Error: too many arguments\n");
        showUsage();
        return -1;
    }

    // 验证打印机是否存在
    HANDLE hPrinter = INVALID_HANDLE_VALUE;
    if (!OpenPrinter(const_cast<LPWSTR>(printerName.c_str()), &hPrinter, NULL))
    {
        wprintf(L"Error: printer '%s' not found or not accessible\n", printerName.c_str());
        return -1;
    }
    ClosePrinter(hPrinter);

    wprintf(L"Starting WinPrintServer on port %d for printer '%s'\n", port, printerName.c_str());
    wprintf(L"Press Ctrl+C to stop\n\n");

    // 启动服务
    int instanceId = 1;
    {
        std::lock_guard<std::mutex> lock(instancesMutex);
        auto result = serverInstances.emplace(instanceId, ServerInstance());
        ServerInstance& instance = result.first->second;
        
        instance.id = instanceId;
        instance.port = port;
        instance.printerName = printerName;
        instance.running = true;
        instance.logCallback = nullptr;
        instance.serverThread = std::thread(ServerLoop, instanceId, port, printerName);
    }

    // 等待线程完成
    {
        std::lock_guard<std::mutex> lock(instancesMutex);
        auto it = serverInstances.find(instanceId);
        if (it != serverInstances.end() && it->second.serverThread.joinable()) {
            it->second.serverThread.join();
        }
    }

    return 0;
}
#endif

