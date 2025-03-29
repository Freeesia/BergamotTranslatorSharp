@echo off

set VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
if not exist "%VSWHERE%" (
    echo Not found: "%VSWHERE%"
    exit /b 1
)

for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -property installationPath`) do (
    set VSDIR=%%i
)
if not defined VSDIR (
    echo Not found: Visual Studio
    exit /b 1
)
call "%VSDIR%\Common7\Tools\VsDevCmd.bat" -startdir=none -arch=x64 -host_arch=x64
