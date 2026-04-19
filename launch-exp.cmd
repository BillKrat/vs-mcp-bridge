@echo off
setlocal

set "DEVENV=C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\devenv.exe"
set "SOLUTION=%~dp0VsMcpBridge.slnx"

if not exist "%DEVENV%" (
    echo Visual Studio Insiders was not found at:
    echo   %DEVENV%
    exit /b 1
)

if exist "%SOLUTION%" (
     start "" "%DEVENV%" "%SOLUTION%" /RootSuffix Exp /Log
) else (
    echo Solution was not found at:
    echo   %SOLUTION%
    start "" "%DEVENV%" /RootSuffix Exp /Log
)

endlocal
