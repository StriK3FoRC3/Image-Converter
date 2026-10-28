param([switch]$Test)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$gac = Join-Path $env:WINDIR 'Microsoft.NET\assembly\GAC_MSIL'
$refs = @("$framework\WPF\WindowsBase.dll", "$framework\WPF\PresentationCore.dll", "$framework\System.Xaml.dll")
foreach ($name in @('netstandard','System.Runtime','System.Runtime.WindowsRuntime')) {
    $refs += (Get-ChildItem -LiteralPath (Join-Path $gac $name) -Recurse -Filter "$name.dll" | Select-Object -First 1).FullName
}
foreach ($name in @('Windows.Data','Windows.Foundation','Windows.Storage')) { $refs += "$env:WINDIR\System32\WinMetadata\$name.winmd" }
$refs += 'packages\magick.net.core.14.14.0\lib\netstandard20\Magick.NET.Core.dll'
$refs += 'packages\magick.net-q16-anycpu.14.14.0\lib\netstandard20\Magick.NET-Q16-AnyCPU.dll'
New-Item -ItemType Directory -Force build | Out-Null
if (!(Test-Path 'packages\magick.net.core.14.14.0\lib\netstandard20\Magick.NET.Core.dll') -or !(Test-Path 'packages\magick.net-q16-anycpu.14.14.0\lib\netstandard20\Magick.NET-Q16-AnyCPU.dll') -or !(Test-Path 'packages\magick.net-q16-anycpu.14.14.0\runtimes\win-x64\native\Magick.Native-Q16-x64.dll')) {
    & .\restore-dependencies.ps1
}
Copy-Item 'packages\magick.net.core.14.14.0\lib\netstandard20\Magick.NET.Core.dll' build
Copy-Item 'packages\magick.net-q16-anycpu.14.14.0\lib\netstandard20\Magick.NET-Q16-AnyCPU.dll' build
Copy-Item 'packages\magick.net-q16-anycpu.14.14.0\runtimes\win-x64\native\Magick.Native-Q16-x64.dll' build
Copy-Item LICENSE build\LICENSE.txt
Copy-Item 'packages\magick.net-q16-anycpu.14.14.0\Notice.txt' build\THIRD-PARTY-NOTICES.txt
$argsList = @('/nologo','/nowarn:1701','/optimize+','/platform:x64','/win32manifest:app.manifest','/win32icon:appicon.ico')
$argsList += $refs | ForEach-Object { '/r:' + $_ }
if ($Test) {
    New-Item -ItemType Directory -Force build\tests | Out-Null
    $argsList += @('/target:exe','/main:ImageConverterUtility.UiSmokeTests','/out:build\tests\UiSmokeTests.exe','src\Program.cs','tests\UiSmokeTests.cs')
} else { $argsList += @('/target:winexe','/out:build\ImageConverter.exe','src\Program.cs') }
& "$framework\csc.exe" @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if ($Test) { Copy-Item build\Magick*.dll build\tests; if (Test-Path tests\fixtures) { Copy-Item tests\fixtures\* build\tests }; & .\build\tests\UiSmokeTests.exe; exit $LASTEXITCODE }


