# 编译C++ DLL并复制到UI项目

param(
    [string]$VisualStudioVersion = "2022"
)

# 设置路径
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionDir = $scriptDir
$cppProject = Join-Path $solutionDir "WinPrintServer\WinPrintServer.vcxproj"
$uiProject = Join-Path $solutionDir "WinPrintServerUI\WinPrintServerUI"
$uiBinDebug = Join-Path $uiProject "bin\Debug"

# 查找MSBuild
$msbuildPath = $null
$vsInstallPath = "C:\Program Files\Microsoft Visual Studio\$VisualStudioVersion"

if (Test-Path $vsInstallPath) {
    $msbuildPath = Get-ChildItem -Path $vsInstallPath -Recurse -Filter "MSBuild.exe" -ErrorAction SilentlyContinue | Select-Object -First 1 | ForEach-Object { $_.FullName }
}

if (-not $msbuildPath) {
    Write-Host "错误: 未找到MSBuild.exe，请检查Visual Studio安装路径" -ForegroundColor Red
    exit 1
}

Write-Host "使用MSBuild: $msbuildPath" -ForegroundColor Green
Write-Host ""

# 编译ReleaseDLL x64
Write-Host "===== 开始编译C++ DLL =====" -ForegroundColor Cyan
Write-Host ""
Write-Host "正在编译 ReleaseDLL x64..." -ForegroundColor Yellow

& $msbuildPath $cppProject /p:Configuration=ReleaseDLL /p:Platform=x64 /t:Build
if ($LASTEXITCODE -ne 0) {
    Write-Host "编译 ReleaseDLL x64 失败" -ForegroundColor Red
    exit 1
}
Write-Host "编译 ReleaseDLL x64 成功" -ForegroundColor Green
Write-Host ""

# 编译ReleaseDLL x86
Write-Host "正在编译 ReleaseDLL x86..." -ForegroundColor Yellow

& $msbuildPath $cppProject /p:Configuration=ReleaseDLL /p:Platform=Win32 /t:Build
if ($LASTEXITCODE -ne 0) {
    Write-Host "编译 ReleaseDLL x86 失败" -ForegroundColor Red
    exit 1
}
Write-Host "编译 ReleaseDLL x86 成功" -ForegroundColor Green
Write-Host ""

# 创建输出目录
if (-not (Test-Path $uiBinDebug)) {
    New-Item -ItemType Directory -Path $uiBinDebug -Force | Out-Null
}

# 复制DLL
Write-Host "===== 复制DLL文件 =====" -ForegroundColor Cyan
Write-Host ""

# 复制x64 DLL
$dllX64 = Join-Path $solutionDir "WinPrintServer\x64\ReleaseDLL\WinPrintServer.dll"
if (Test-Path $dllX64) {
    Copy-Item -Path $dllX64 -Destination (Join-Path $uiBinDebug "WinPrintServer.dll") -Force
    Write-Host "复制 x64 DLL: $dllX64 -> $(Join-Path $uiBinDebug 'WinPrintServer.dll')" -ForegroundColor Green
} else {
    Write-Host "警告: 未找到 x64 DLL: $dllX64" -ForegroundColor Yellow
}
Write-Host ""

# 复制x86 DLL
$dllX86 = Join-Path $solutionDir "WinPrintServer\ReleaseDLL\WinPrintServer32.dll"
if (Test-Path $dllX86) {
    Copy-Item -Path $dllX86 -Destination (Join-Path $uiBinDebug "WinPrintServer32.dll") -Force
    Write-Host "复制 x86 DLL: $dllX86 -> $(Join-Path $uiBinDebug 'WinPrintServer32.dll')" -ForegroundColor Green
} else {
    Write-Host "警告: 未找到 x86 DLL: $dllX86" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "===== 打包完成 =====" -ForegroundColor Cyan
Write-Host "DLL 已复制到: $uiBinDebug" -ForegroundColor Green