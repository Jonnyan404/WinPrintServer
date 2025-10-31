using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinPrintServerUI
{
    public partial class Form1 : Form
    {
        private static IntPtr dllHandle;
        private static IntPtr startServerPtr;
        private static IntPtr stopServerPtr;
        private static IntPtr setLogCallbackPtr;
        private static IntPtr getServerStatusPtr;
        private static IntPtr getServerInfoPtr;

        // 修改委托定义：使用 CallingConvention 和正确的参数类型
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int StartServerDelegate(int port, [MarshalAs(UnmanagedType.LPWStr)] string printerName);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void StopServerDelegate(int instanceId);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SetLogCallbackDelegate(int instanceId, LogCallback callback);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetServerStatusDelegate(int instanceId);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void GetServerInfoDelegate(int instanceId, ref int port, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder printerName, int bufferSize);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LogCallback(int instanceId, [MarshalAs(UnmanagedType.LPWStr)] string msg);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);

        // 服务实例管理
        private Dictionary<int, ServerInstanceInfo> serverInstances = new Dictionary<int, ServerInstanceInfo>();
        private Dictionary<int, LogCallback> logCallbacks = new Dictionary<int, LogCallback>();
        private List<LogEntry> allLogs = new List<LogEntry>(); // 存储所有日志
        private bool isClosing = false; // 标记是否正在关闭

        private class ServerInstanceInfo
        {
            public int InstanceId { get; set; }
            public int Port { get; set; }
            public string PrinterName { get; set; }
            public bool Running { get; set; }
            public ListViewItem ListViewItem { get; set; }
        }

        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public int InstanceId { get; set; }
            public int Port { get; set; }
            public string PrinterName { get; set; }
            public string Message { get; set; }
        }

        public Form1()
        {
            InitializeComponent();
            LoadDLL();
        }

        private void LoadDLL()
        {
            string dllName = Environment.Is64BitProcess ? "WinPrintServer.dll" : "WinPrintServer32.dll";
            rtbLog.AppendText("尝试加载DLL: " + dllName + Environment.NewLine);
            
            dllHandle = LoadLibrary(dllName);
            if (dllHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                MessageBox.Show("Failed to load DLL: " + dllName + " (错误码: " + errorCode + ")");
                rtbLog.AppendText("DLL加载失败，错误码: " + errorCode + Environment.NewLine);
                return;
            }

            rtbLog.AppendText("DLL加载成功" + Environment.NewLine);

            startServerPtr = GetProcAddress(dllHandle, "StartServer");
            stopServerPtr = GetProcAddress(dllHandle, "StopServer");
            setLogCallbackPtr = GetProcAddress(dllHandle, "SetLogCallback");
            getServerStatusPtr = GetProcAddress(dllHandle, "GetServerStatus");
            getServerInfoPtr = GetProcAddress(dllHandle, "GetServerInfo");

            if (startServerPtr == IntPtr.Zero || stopServerPtr == IntPtr.Zero || setLogCallbackPtr == IntPtr.Zero)
            {
                MessageBox.Show("Failed to find functions in DLL");
                rtbLog.AppendText("函数指针获取失败" + Environment.NewLine);
                return;
            }

            rtbLog.AppendText("函数指针获取成功" + Environment.NewLine);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 加载打印机列表
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                cmbPrinters.Items.Add(printer);
            }
            if (cmbPrinters.Items.Count > 0)
            {
                cmbPrinters.SelectedIndex = 0;
            }
            nudPort.Value = 9100;

            // 初始化服务列表视图
            lvServices.Columns.Add("ID", 50);
            lvServices.Columns.Add("打印机", 200);
            lvServices.Columns.Add("端口", 80);
            lvServices.Columns.Add("状态", 80);
            lvServices.View = View.Details;
            lvServices.FullRowSelect = true;
            lvServices.MultiSelect = false;
            
            // 添加右键菜单
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem stopMenuItem = new ToolStripMenuItem("停止服务");
            stopMenuItem.Click += (s, args) =>
            {
                if (lvServices.SelectedItems.Count > 0)
                {
                    StopService(lvServices.SelectedItems[0]);
                }
            };
            contextMenu.Items.Add(stopMenuItem);
            lvServices.ContextMenuStrip = contextMenu;
            
            // 添加提示标签 - 修正位置，放在服务列表下方
            Label tipLabel = new Label();
            tipLabel.Text = "💡 提示: 右键点击服务可停止";
            tipLabel.AutoSize = true;
            tipLabel.Location = new Point(lvServices.Left, lvServices.Bottom + 5);
            tipLabel.ForeColor = Color.Blue;
            tipLabel.Font = new Font(this.Font.FontFamily, 10, FontStyle.Regular);
            this.Controls.Add(tipLabel);
            tipLabel.BringToFront(); // 确保在最前面

            // 初始化日志筛选器
            cmbLogFilter.Items.Add("全部");
            cmbLogFilter.Items.Add("系统");
            cmbLogFilter.SelectedIndex = 0;
            cmbLogFilter.SelectedIndexChanged += CmbLogFilter_SelectedIndexChanged;
        }

        private void LvServices_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            // 更新日志筛选器
            if (lvServices.SelectedItems.Count > 0)
            {
                int.TryParse(lvServices.SelectedItems[0].SubItems[0].Text, out int instanceId);
                int.TryParse(lvServices.SelectedItems[0].SubItems[2].Text, out int port);
                string printer = lvServices.SelectedItems[0].SubItems[1].Text;
                
                string filterName = $"实例 {instanceId} ({printer}:{port})";
                
                // 检查是否已存在
                bool exists = false;
                foreach (var item in cmbLogFilter.Items)
                {
                    if (item.ToString().Contains($"实例 {instanceId}"))
                    {
                        exists = true;
                        break;
                    }
                }
                
                if (!exists && instanceId > 0)
                {
                    cmbLogFilter.Items.Add(filterName);
                }
            }
        }

        private void CmbLogFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshLog();
        }

        private void RefreshLog()
        {
            rtbLog.Clear();
            string filter = cmbLogFilter.SelectedItem?.ToString() ?? "全部";

            foreach (var log in allLogs)
            {
                bool show = false;
                if (filter == "全部")
                {
                    show = true;
                }
                else if (filter == "系统")
                {
                    show = log.InstanceId == 0;
                }
                else if (filter.StartsWith("实例"))
                {
                    // 提取实例ID
                    int instanceId = 0;
                    if (filter.Contains("("))
                    {
                        // 格式: "实例 1 (打印机:9100)"
                        string idPart = filter.Substring(filter.IndexOf("实例 ") + 3);
                        idPart = idPart.Substring(0, idPart.IndexOf(" ("));
                        int.TryParse(idPart, out instanceId);
                    }
                    else
                    {
                        // 旧格式: "实例 1"
                        string[] parts = filter.Split(' ');
                        if (parts.Length > 1)
                        {
                            int.TryParse(parts[1], out instanceId);
                        }
                    }
                    show = log.InstanceId == instanceId;
                }
                else if (filter.StartsWith("端口"))
                {
                    // 格式: "端口 9100"
                    int port = 0;
                    string[] parts = filter.Split(' ');
                    if (parts.Length > 1)
                    {
                        int.TryParse(parts[1], out port);
                    }
                    show = log.Port == port;
                }

                if (show)
                {
                    string timestamp = log.Timestamp.ToString("HH:mm:ss");
                    if (log.InstanceId == 0)
                    {
                        rtbLog.AppendText($"[{timestamp}] {log.Message}" + Environment.NewLine);
                    }
                    else
                    {
                        rtbLog.AppendText($"[{timestamp}] [实例 {log.InstanceId}] {log.Message}" + Environment.NewLine);
                    }
                }
            }
            
            // 自动滚动到底部
            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.ScrollToCaret();
        }

        private void LogMessage(int instanceId, string msg)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, string>(LogMessage), instanceId, msg);
            }
            else
            {
                // 获取该实例的端口和打印机信息
                int port = 0;
                string printerName = "";
                lock (serverInstances)
                {
                    if (serverInstances.ContainsKey(instanceId))
                    {
                        port = serverInstances[instanceId].Port;
                        printerName = serverInstances[instanceId].PrinterName;
                    }
                }

                // 添加到日志列表
                allLogs.Add(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    InstanceId = instanceId,
                    Port = port,
                    PrinterName = printerName,
                    Message = msg
                });

                // 限制日志数量，防止内存溢出
                if (allLogs.Count > 10000)
                {
                    allLogs.RemoveRange(0, 1000);
                }

                // 更新显示
                RefreshLog();

                // 更新服务状态
                lock (serverInstances)
                {
                    if (serverInstances.ContainsKey(instanceId))
                    {
                        var instance = serverInstances[instanceId];
                        if (msg.StartsWith("fatal"))
                        {
                            instance.ListViewItem.SubItems[3].Text = "异常";
                            instance.ListViewItem.ForeColor = Color.Red;
                            instance.Running = false;
                        }
                        else
                        {
                            instance.ListViewItem.SubItems[3].Text = "运行中";
                            instance.ListViewItem.ForeColor = Color.Black;
                        }
                    }
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbPrinters.SelectedItem == null)
                {
                    MessageBox.Show("请先选择打印机");
                    return;
                }

                int port = (int)nudPort.Value;
                string printer = cmbPrinters.SelectedItem.ToString();

                LogToUI(0, "开始启动服务...");
                LogToUI(0, $"打印机: {printer}, 端口: {port}");

                if (startServerPtr == IntPtr.Zero)
                {
                    MessageBox.Show("函数指针无效");
                    return;
                }

                LogToUI(0, "调用 StartServer...");
                
                try
                {
                    var startServer = (StartServerDelegate)Marshal.GetDelegateForFunctionPointer(startServerPtr, typeof(StartServerDelegate));
                    int instanceId = startServer(port, printer);

                    LogToUI(0, $"StartServer 返回: {instanceId}");

                    if (instanceId == -2)
                    {
                        MessageBox.Show($"端口 {port} 已被占用,请选择其他端口");
                        return;
                    }
                    else if (instanceId <= 0)
                    {
                        MessageBox.Show($"启动服务失败:服务实例ID无效 ({instanceId})");
                        return;
                    }
                    
                    // 创建日志回调委托
                    LogCallback logCallback = LogMessage;
                    logCallbacks[instanceId] = logCallback;

                    LogToUI(0, "设置日志回调...");

                    // 设置日志回调
                    var setLogCallback = (SetLogCallbackDelegate)Marshal.GetDelegateForFunctionPointer(setLogCallbackPtr, typeof(SetLogCallbackDelegate));
                    setLogCallback(instanceId, logCallback);

                    LogToUI(0, "日志回调设置成功");

                    // 创建服务实例信息
                    var instance = new ServerInstanceInfo
                    {
                        InstanceId = instanceId,
                        Port = port,
                        PrinterName = printer,
                        Running = true
                    };

                    // 添加到列表
                    ListViewItem item = new ListViewItem(new string[] { 
                        instanceId.ToString(), 
                        printer, 
                        port.ToString(), 
                        "运行中" 
                    });
                    item.Tag = instanceId;
                    lvServices.Items.Add(item);
                    instance.ListViewItem = item;

                    serverInstances[instanceId] = instance;

                    // 添加到日志筛选器
                    string filterName = $"实例 {instanceId} ({printer}:{port})";
                    cmbLogFilter.Items.Add(filterName);

                    LogToUI(0, $"服务 {instanceId} 启动成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("启动服务失败: " + ex.Message);
                    LogToUI(0, "启动服务失败: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("未捕获的异常: " + ex.Message);
                LogToUI(0, "未捕获的异常: " + ex.Message);
            }
        }

        private void LogToUI(int instanceId, string msg)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, string>(LogToUI), instanceId, msg);
            }
            else
            {
                // 获取该实例的端口和打印机信息
                int port = 0;
                string printerName = "";
                if (serverInstances.ContainsKey(instanceId))
                {
                    port = serverInstances[instanceId].Port;
                    printerName = serverInstances[instanceId].PrinterName;
                }

                allLogs.Add(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    InstanceId = instanceId,
                    Port = port,
                    PrinterName = printerName,
                    Message = msg
                });

                if (allLogs.Count > 10000)
                {
                    allLogs.RemoveRange(0, 1000);
                }

                RefreshLog();
            }
        }

        private void StopService(ListViewItem item)
        {
            if (item == null || item.Tag == null) return;
            
            int instanceId = (int)item.Tag;
            
            // 防止重复停止
            lock (serverInstances)
            {
                if (!serverInstances.ContainsKey(instanceId)) return;
            }

            // 立即更新 UI 状态
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    item.SubItems[3].Text = "停止中...";
                    item.ForeColor = Color.Gray;
                }));
            }
            else
            {
                item.SubItems[3].Text = "停止中...";
                item.ForeColor = Color.Gray;
            }

            // 在后台线程中执行停止操作
            Thread stopThread = new Thread(() =>
            {
                try
                {
                    LogToUI(0, $"正在停止服务 {instanceId}...");
                    
                    if (stopServerPtr != IntPtr.Zero)
                    {
                        try
                        {
                            var stopServer = (StopServerDelegate)Marshal.GetDelegateForFunctionPointer(
                                stopServerPtr, typeof(StopServerDelegate));
                            stopServer(instanceId);
                        }
                        catch (SEHException ex)
                        {
                            LogToUI(0, $"调用 StopServer 时发生 SEH 异常: {ex.Message}");
                        }
                        catch (AccessViolationException ex)
                        {
                            LogToUI(0, $"调用 StopServer 时发生访问冲突: {ex.Message}");
                        }
                    }

                    // 等待一小段时间
                    Thread.Sleep(300);

                    // UI 更新在主线程中进行
                    if (!isClosing && this.IsHandleCreated)
                    {
                        this.Invoke((Action)(() =>
                        {
                            try
                            {
                                lvServices.Items.Remove(item);
                                
                                // 从筛选器中移除
                                string filterPrefix = $"实例 {instanceId}";
                                for (int i = cmbLogFilter.Items.Count - 1; i >= 0; i--)
                                {
                                    if (cmbLogFilter.Items[i].ToString().StartsWith(filterPrefix))
                                    {
                                        cmbLogFilter.Items.RemoveAt(i);
                                        break;
                                    }
                                }
                                
                                if (cmbLogFilter.SelectedIndex == -1 && cmbLogFilter.Items.Count > 0)
                                {
                                    cmbLogFilter.SelectedIndex = 0;
                                }
                            }
                            catch (Exception ex)
                            {
                                LogToUI(0, $"更新 UI 失败: {ex.Message}");
                            }
                        }));
                    }

                    // 清理字典
                    lock (serverInstances)
                    {
                        serverInstances.Remove(instanceId);
                    }
                    lock (logCallbacks)
                    {
                        logCallbacks.Remove(instanceId);
                    }

                    LogToUI(0, $"服务 {instanceId} 已停止");
                }
                catch (Exception ex)
                {
                    LogToUI(0, $"停止服务 {instanceId} 时出错: {ex.GetType().Name} - {ex.Message}");
                }
            })
            {
                IsBackground = true
            };
            stopThread.Start();
        }

        private void btnStopAll_Click(object sender, EventArgs e)
        {
            if (isClosing) return;

            btnStopAll.Enabled = false;

            Thread stopAllThread = new Thread(() =>
            {
                var items = new List<ListViewItem>();
                
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        foreach (ListViewItem item in lvServices.Items)
                        {
                            items.Add(item);
                        }
                    }));
                }

                foreach (var item in items)
                {
                    if (isClosing) break;
                    StopService(item);
                    Thread.Sleep(300);
                }

                LogToUI(0, "所有服务已停止");

                if (!isClosing && this.IsHandleCreated)
                {
                    Invoke(new Action(() => btnStopAll.Enabled = true));
                }
            })
            {
                IsBackground = true
            };
            stopAllThread.Start();
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            allLogs.Clear();
            rtbLog.Clear();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isClosing = true;
            
            // 直接调用 StopServer，不等待
            if (stopServerPtr != IntPtr.Zero)
            {
                try
                {
                    var stopServer = (StopServerDelegate)Marshal.GetDelegateForFunctionPointer(
                        stopServerPtr, typeof(StopServerDelegate));
                    
                    var instanceIds = serverInstances.Keys.ToList();
                    foreach (var instanceId in instanceIds)
                    {
                        try
                        {
                            stopServer(instanceId);
                        }
                        catch { }
                    }
                }
                catch { }
            }

            // 短暂等待
            Thread.Sleep(200);

            try
            {
                if (dllHandle != IntPtr.Zero)
                {
                    FreeLibrary(dllHandle);
                }
            }
            catch { }
            
            base.OnFormClosing(e);
        }
    }
}
