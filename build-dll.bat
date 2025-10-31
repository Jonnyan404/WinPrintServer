REM filepath: c:\Users\jonny\Documents\GitHub\WinPrintServer\build-dll.bat
@echo off
REM ����C++ DLL�����Ƶ�UI��Ŀ

setlocal enabledelayedexpansion

REM ����·��
set SOLUTION_DIR=%~dp0
set CPP_PROJECT=%SOLUTION_DIR%WinPrintServer\WinPrintServer.vcxproj
set UI_PROJECT=%SOLUTION_DIR%WinPrintServerUI\WinPrintServerUI

REM �Զ��ж�ƽ̨
if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    set PLATFORM_SUFFIX=x64
) else (
    set PLATFORM_SUFFIX=
)
set UI_BIN_DEBUG=%UI_PROJECT%\bin\%PLATFORM_SUFFIX%\Debug

REM Visual Studio �汾������ʵ���޸ģ�
set MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe

REM ���MSBuild·��
if not exist "%MSBUILD_PATH%" (
    echo ����: δ�ҵ�MSBuild.exe������Visual Studio��װ·��
    pause
    exit /b 1
)

echo ===== ��ʼ����C++ DLL =====
echo ��⵽ƽ̨: %PLATFORM_SUFFIX%
echo.

REM ���� ReleaseDLL|x64
echo ���ڱ��� ReleaseDLL x64...
"%MSBUILD_PATH%" "%CPP_PROJECT%" /p:Configuration=ReleaseDLL /p:Platform=x64 /t:Build
if %errorlevel% neq 0 (
    echo ���� ReleaseDLL x64 ʧ��
    pause
    exit /b 1
)
echo ���� ReleaseDLL x64 �ɹ�
echo.

REM ���� ReleaseDLL|Win32
echo ���ڱ��� ReleaseDLL x86...
"%MSBUILD_PATH%" "%CPP_PROJECT%" /p:Configuration=ReleaseDLL /p:Platform=Win32 /t:Build
if %errorlevel% neq 0 (
    echo ���� ReleaseDLL x86 ʧ��
    pause
    exit /b 1
)
echo ���� ReleaseDLL x86 �ɹ�
echo.

REM �������Ŀ¼
if not exist "%UI_BIN_DEBUG%" mkdir "%UI_BIN_DEBUG%"

REM ����DLL��UI��Ŀ
echo ===== ����DLL�ļ� =====
echo.

REM ����x64 DLL
set DLL_X64=%SOLUTION_DIR%WinPrintServer\x64\ReleaseDLL\WinPrintServer.dll
if exist "%DLL_X64%" (
    copy "%DLL_X64%" "%UI_BIN_DEBUG%\WinPrintServer.dll"
    echo ���� x64 DLL: %DLL_X64% -^> %UI_BIN_DEBUG%\WinPrintServer.dll
) else (
    echo ����: δ�ҵ� x64 DLL: %DLL_X64%
)
echo.

REM ����x86 DLL
set DLL_X86=%SOLUTION_DIR%WinPrintServer\ReleaseDLL\WinPrintServer32.dll
if exist "%DLL_X86%" (
    copy "%DLL_X86%" "%UI_BIN_DEBUG%\WinPrintServer32.dll"
    echo ���� x86 DLL: %DLL_X86% -^> %UI_BIN_DEBUG%\WinPrintServer32.dll
) else (
    echo ����: δ�ҵ� x86 DLL: %DLL_X86%
)
echo.

echo ===== ������ =====
echo DLL �Ѹ��Ƶ�: %UI_BIN_DEBUG%
pause