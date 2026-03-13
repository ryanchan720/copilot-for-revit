@echo off
setlocal enabledelayedexpansion

REM 输入参数
set "ZIP=%~f1"
set "DSTDIR=%~f2"

REM 等待宿主进程退出（约 8 秒）
ping -n 9 127.0.0.1 >nul

REM 校验参数
if not exist "%ZIP%" (
  echo Update package not found: %ZIP%
  exit /b 1
)
if not exist "%DSTDIR%" (
  mkdir "%DSTDIR%" >nul 2>&1
)

REM 调试：显示实际路径
echo ZIP Path: "%ZIP%"
echo Dest Path: "%DSTDIR%"

REM 使用环境变量从批处理传参到 PowerShell，避免 -Command 参数解析问题
set RETRY=0
:extract_retry
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $zip=$env:ZIP; $dst=$env:DSTDIR; Expand-Archive -LiteralPath $zip -DestinationPath $dst -Force"

if errorlevel 1 (
  set /a RETRY+=1
  echo Extract failed, retry !RETRY! of 10
  if !RETRY! geq 10 goto done
  ping -n 3 127.0.0.1 >nul
  goto extract_retry
)

echo Update finished.

:done
exit /b 0
