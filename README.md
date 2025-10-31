# WinPrintServer
Windows TCP 打印服务器，将原始数据直接转发到打印机

## 使用方法
运行 `WinPrintServer.exe` 无命令行参数，将默认打印机作为 RAW 打印机在端口 9100 上呈现。

要呈现除默认打印机以外的打印机，可以在命令行中指定打印机名称。

```
WinPrintServer [选项] [打印机名称]
  -h           显示此帮助信息
  -p <端口>    指定监听端口（默认：9100）
  打印机名称  要打印的打印机名称。如果未指定，则使用默认打印机
```

## 示例

将名为 `EPSON098CEF (WF-3520 Series)` 的打印机作为 RAW 打印机呈现，使用以下命令。

```
WinPrintServer "EPSON098CEF (WF-3520 Series)"
```

指定自定义端口（例如 9200）和打印机：

```
WinPrintServer -p 9200 "EPSON098CEF (WF-3520 Series)"
```

*注意* - 由于打印机名称包含空格，名称在命令行中需要用引号括起来

# FAQ

- 优先使用原版驱动
- 备选驱动: https://www.support.xerox.com/en-us/content/135364 这个是x64,页面有x86的,请注意根据自己的平台来下载