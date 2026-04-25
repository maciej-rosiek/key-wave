# One-shot remote installer for Lenovo Ripple. Downloads the repo, publishes
# a Release build, and registers it as a packaged app so the
# com.microsoft.windows.lighting AppExtension is recognized — that's what
# makes background ambient lighting work while a game is foreground.
#
# Usage (paste in PowerShell):
#   irm https://raw.githubusercontent.com/maciej-rosiek/lenovo-ripple/main/package/install.ps1 | iex
#
# Files end up in: %LOCALAPPDATA%\LenovoRipple\source

$ErrorActionPreference = 'Stop'

function Require-Net10 {
    $runtimes = & dotnet --list-runtimes 2>$null
    if (-not ($runtimes -match 'Microsoft\.WindowsDesktop\.App 10\.')) {
        Write-Host ""
        Write-Host "Microsoft .NET 10 Desktop Runtime (or SDK) is required." -ForegroundColor Yellow
        Write-Host "Install it from: https://dotnet.microsoft.com/download/dotnet/10.0"
        Write-Host "Then re-run this installer."
        throw ".NET 10 Desktop runtime not found"
    }
}

function Require-Sdk {
    $sdks = & dotnet --list-sdks 2>$null
    if (-not ($sdks -match '^10\.')) {
        Write-Host ""
        Write-Host "The .NET 10 SDK is required to publish the app." -ForegroundColor Yellow
        Write-Host "Install it from: https://dotnet.microsoft.com/download/dotnet/10.0 (pick 'SDK')"
        Write-Host "Then re-run this installer."
        throw ".NET 10 SDK not found"
    }
}

Require-Net10
Require-Sdk

$root = Join-Path $env:LOCALAPPDATA 'LenovoRipple'
$src  = Join-Path $root 'source'

if (-not (Test-Path $root)) { New-Item -ItemType Directory $root | Out-Null }

# Prefer git if available; otherwise download the zipball.
$git = Get-Command git -ErrorAction SilentlyContinue
if ($git) {
    if (Test-Path (Join-Path $src '.git')) {
        Write-Host "Updating existing checkout in $src ..."
        Push-Location $src
        git fetch --all --quiet
        git reset --hard origin/main --quiet
        Pop-Location
    } else {
        if (Test-Path $src) { Remove-Item $src -Recurse -Force }
        Write-Host "Cloning into $src ..."
        git clone --quiet https://github.com/maciej-rosiek/lenovo-ripple $src
    }
} else {
    Write-Host "git not found, downloading source zipball..."
    $zip = Join-Path $env:TEMP 'lenovo-ripple-main.zip'
    Invoke-WebRequest -UseBasicParsing `
        -Uri 'https://github.com/maciej-rosiek/lenovo-ripple/archive/refs/heads/main.zip' `
        -OutFile $zip
    if (Test-Path $src) { Remove-Item $src -Recurse -Force }
    $tmpExtract = Join-Path $env:TEMP "lenovo-ripple-extract-$(Get-Random)"
    Expand-Archive -Path $zip -DestinationPath $tmpExtract -Force
    $extracted = Get-ChildItem $tmpExtract | Select-Object -First 1
    Move-Item $extracted.FullName $src
    Remove-Item $tmpExtract -Recurse -Force
    Remove-Item $zip
}

# Run dev-register from the freshly checked-out tree.
$register = Join-Path $src 'package\dev-register.ps1'
& powershell -NoProfile -ExecutionPolicy Bypass -File $register
$registerExit = $LASTEXITCODE

if ($registerExit -ne 0) {
    Write-Host ""
    Write-Host "Registration FAILED (exit code $registerExit)." -ForegroundColor Red
    Write-Host ""
    Write-Host "Most common cause: Developer Mode is OFF." -ForegroundColor Yellow
    Write-Host "  Add-AppxPackage -Register requires it for unsigned packages."
    Write-Host ""
    Write-Host "Fix:" -ForegroundColor Cyan
    Write-Host "  Settings -> Privacy & security -> For developers -> Developer Mode = On"
    Write-Host "  Then re-run this installer."
    return
}

Write-Host ""
Write-Host "Lenovo Ripple installed." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Open Settings -> Personalization -> Dynamic Lighting"
Write-Host "  2. Under 'Controlled by', pick 'Lenovo Ripple'"
Write-Host "  3. Launch 'Lenovo Ripple' from the Start menu"
Write-Host ""
Write-Host "To uninstall:  Get-AppxPackage LenovoRipple | Remove-AppxPackage"
