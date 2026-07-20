@echo off
setlocal

cd /d "%~dp0"
if not exist build mkdir build

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "REF=%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.1"
set "MAGICK_VER=14.14.0"
set "MAGICK_Q=packages\magick.net-q16-anycpu.%MAGICK_VER%"
set "MAGICK_CORE=packages\magick.net.core.%MAGICK_VER%"
if not exist "%CSC%" (
  echo C# compiler was not found.
  exit /b 1
)
if not exist "%REF%\PresentationCore.dll" (
  echo .NET Framework reference assemblies were not found.
  exit /b 1
)
if not exist "%MAGICK_CORE%\lib\netstandard20\Magick.NET.Core.dll" goto restore_dependencies
if not exist "%MAGICK_Q%\lib\netstandard20\Magick.NET-Q16-AnyCPU.dll" goto restore_dependencies
if not exist "%MAGICK_Q%\runtimes\win-x64\native\Magick.Native-Q16-x64.dll" goto restore_dependencies
goto dependencies_ready

:restore_dependencies
echo Restoring Magick.NET dependencies...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File restore-dependencies.ps1 -Version "%MAGICK_VER%" || exit /b 1

:dependencies_ready

"%CSC%" /nologo /nowarn:1701 /target:winexe /optimize+ /platform:x64 /win32manifest:app.manifest /win32icon:appicon.ico /out:build\ImageConverter.exe /r:"%REF%\WindowsBase.dll" /r:"%REF%\PresentationCore.dll" /r:"%REF%\System.Xaml.dll" /r:"%REF%\Facades\netstandard.dll" /r:"%REF%\Facades\System.Runtime.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\System.Runtime.WindowsRuntime.dll" /r:"C:\Windows\System32\WinMetadata\Windows.Data.winmd" /r:"C:\Windows\System32\WinMetadata\Windows.Foundation.winmd" /r:"C:\Windows\System32\WinMetadata\Windows.Storage.winmd" /r:"%MAGICK_CORE%\lib\netstandard20\Magick.NET.Core.dll" /r:"%MAGICK_Q%\lib\netstandard20\Magick.NET-Q16-AnyCPU.dll" src\Program.cs || exit /b 1

copy /y "%MAGICK_CORE%\lib\netstandard20\Magick.NET.Core.dll" build\ >nul || exit /b 1
copy /y "%MAGICK_Q%\lib\netstandard20\Magick.NET-Q16-AnyCPU.dll" build\ >nul || exit /b 1
copy /y "%MAGICK_Q%\runtimes\win-x64\native\Magick.Native-Q16-x64.dll" build\ >nul || exit /b 1
copy /y LICENSE build\LICENSE.txt >nul || exit /b 1
copy /y "%MAGICK_Q%\Notice.txt" build\THIRD-PARTY-NOTICES.txt >nul || exit /b 1

if not exist build\Input mkdir build\Input
if not exist build\Output mkdir build\Output

echo Built build\ImageConverter.exe
exit /b 0
