REM filepath: c:\Users\jonny\Documents\GitHub\WinPrintServer\build-dll.bat
@echo off
REM 编译C++ DLL并复制到UI项目

setlocal enabledelayedexpansion

REM 设置路径
set SOLUTION_DIR=%~dp0
set CPP_PROJECT=%SOLUTION_DIR%WinPrintServer\WinPrintServer.vcxproj
set UI_PROJECT=%SOLUTION_DIR%WinPrintServerUI\WinPrintServerUI

REM 自动判断平台
if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    set PLATFORM_SUFFIX=x64
) else (
    set PLATFORM_SUFFIX=
)
set UI_BIN_DEBUG=%UI_PROJECT%\bin\%PLATFORM_SUFFIX%\Debug

REM Visual Studio 版本（根据实际修改）
set MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe

REM 检查MSBuild路径
if not exist "%MSBUILD_PATH%" (
    echo 错误: 未找到MSBuild.exe，请检查Visual Studio安装路径
    pause
    exit /b 1
)

echo ===== 开始编译C++ DLL =====
echo 检测到平台: %PLATFORM_SUFFIX%
echo.

REM 编译 ReleaseDLL|x64
echo 正在编译 ReleaseDLL x64...
"%MSBUILD_PATH%" "%CPP_PROJECT%" /p:Configuration=ReleaseDLL /p:Platform=x64 /t:Build
if %errorlevel% neq 0 (
    echo 编译 ReleaseDLL x64 失败
    pause
    exit /b 1
)
echo 编译 ReleaseDLL x64 成功
echo.

REM 编译 ReleaseDLL|Win32
echo 正在编译 ReleaseDLL x86...
"%MSBUILD_PATH%" "%CPP_PROJECT%" /p:Configuration=ReleaseDLL /p:Platform=Win32 /t:Build
if %errorlevel% neq 0 (
    echo 编译 ReleaseDLL x86 失败
    pause
    exit /b 1
)
echo 编译 ReleaseDLL x86 成功
echo.

REM 创建输出目录
if not exist "%UI_BIN_DEBUG%" mkdir "%UI_BIN_DEBUG%"

REM 复制DLL到UI项目
echo ===== 复制DLL文件 =====
echo.

REM 复制x64 DLL
set DLL_X64=%SOLUTION_DIR%WinPrintServer\x64\ReleaseDLL\WinPrintServer.dll
if exist "%DLL_X64%" (
    copy "%DLL_X64%" "%UI_BIN_DEBUG%\WinPrintServer.dll"
    echo 复制 x64 DLL: %DLL_X64% -^> %UI_BIN_DEBUG%\WinPrintServer.dll
) else (
    echo 警告: 未找到 x64 DLL: %DLL_X64%
)
echo.

REM 复制x86 DLL
set DLL_X86=%SOLUTION_DIR%WinPrintServer\ReleaseDLL\WinPrintServer32.dll
if exist "%DLL_X86%" (
    copy "%DLL_X86%" "%UI_BIN_DEBUG%\WinPrintServer32.dll"
    echo 复制 x86 DLL: %DLL_X86% -^> %UI_BIN_DEBUG%\WinPrintServer32.dll
) else (
    echo 警告: 未找到 x86 DLL: %DLL_X86%
)
echo.

echo ===== 打包完成 =====
echo DLL 已复制到: %UI_BIN_DEBUG%
pause