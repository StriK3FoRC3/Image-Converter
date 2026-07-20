param(
    [string]$Version = "14.14.0"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageRoot = Join-Path $projectRoot "packages"
$packageIds = @("magick.net.core", "magick.net-q16-anycpu")

Add-Type -AssemblyName System.IO.Compression.FileSystem
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null

foreach ($packageId in $packageIds) {
    $packageName = "$packageId.$Version"
    $target = Join-Path $packageRoot $packageName
    $archive = Join-Path $packageRoot "$packageName.nupkg"
    $requiredFiles = if ($packageId -eq "magick.net.core") {
        @(Join-Path $target "lib\netstandard20\Magick.NET.Core.dll")
    } else {
        @(
            (Join-Path $target "lib\netstandard20\Magick.NET-Q16-AnyCPU.dll"),
            (Join-Path $target "runtimes\win-x64\native\Magick.Native-Q16-x64.dll"),
            (Join-Path $target "Notice.txt")
        )
    }

    $missingFiles = @($requiredFiles | Where-Object { -not (Test-Path -LiteralPath $_) })
    if ($missingFiles.Count -eq 0) {
        continue
    }

    if (-not (Test-Path -LiteralPath $archive)) {
        $url = "https://api.nuget.org/v3-flatcontainer/$packageId/$Version/$packageName.nupkg"
        $download = "$archive.download"
        Write-Host "Downloading $packageId $Version..."
        Invoke-WebRequest -Uri $url -OutFile $download -UseBasicParsing
        Move-Item -LiteralPath $download -Destination $archive
    }

    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Recurse -Force
    }
    [System.IO.Compression.ZipFile]::ExtractToDirectory($archive, $target)
}

Write-Host "Dependencies are ready."
