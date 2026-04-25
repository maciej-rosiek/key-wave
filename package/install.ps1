# One-shot installer for Lenovo Ripple. Downloads the latest signed MSIX
# from GitHub Releases, trusts the signing cert, and installs it.
# After install, the app has package identity + the
# com.microsoft.windows.lighting AppExtension, so Windows lets it drive
# the keyboard from the background.
#
# Usage (paste in PowerShell):
#   irm https://raw.githubusercontent.com/maciej-rosiek/lenovo-ripple/main/package/install.ps1 | iex
#
# Cert trust + MSIX install both require admin. The script self-elevates.

$ErrorActionPreference = 'Stop'

$repo  = 'maciej-rosiek/lenovo-ripple'
$msixName = 'LenovoRipple.msix'
$cerName  = 'LenovoRippleDev.cer'

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    return ([Security.Principal.WindowsPrincipal]$id).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    Write-Host "Lenovo Ripple installer needs admin (to trust the signing cert)." -ForegroundColor Yellow
    Write-Host "Re-launching elevated..."
    $cmd = "iex (irm 'https://raw.githubusercontent.com/$repo/main/package/install.ps1')"
    Start-Process powershell -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -NoExit -Command $cmd"
    return
}

# Verify .NET 10 Desktop Runtime is installed (the MSIX is framework-dependent).
$runtimes = & dotnet --list-runtimes 2>$null
if (-not ($runtimes -match 'Microsoft\.WindowsDesktop\.App 10\.')) {
    Write-Host ""
    Write-Host "Microsoft .NET 10 Desktop Runtime is required." -ForegroundColor Yellow
    Write-Host "Install it from: https://dotnet.microsoft.com/download/dotnet/10.0"
    Write-Host "  (pick 'Desktop Runtime' for x64)"
    Write-Host "Then re-run this installer."
    return
}

# Resolve latest release assets via GitHub API.
Write-Host "Fetching latest release..."
$release = Invoke-RestMethod -UseBasicParsing `
    -Uri "https://api.github.com/repos/$repo/releases/latest" `
    -Headers @{ 'User-Agent' = 'LenovoRipple-Installer' }

$msixAsset = $release.assets | Where-Object name -eq $msixName | Select-Object -First 1
$cerAsset  = $release.assets | Where-Object name -eq $cerName  | Select-Object -First 1
if (-not $msixAsset -or -not $cerAsset) {
    throw "Release $($release.tag_name) is missing $msixName or $cerName"
}
Write-Host "  $($release.tag_name)  $msixName ($([math]::Round($msixAsset.size/1MB,1)) MB)"

# Download to temp.
$tmp = Join-Path $env:TEMP "lenovo-ripple-install-$(Get-Random)"
New-Item -ItemType Directory $tmp -Force | Out-Null
$msix = Join-Path $tmp $msixName
$cer  = Join-Path $tmp $cerName
Invoke-WebRequest -UseBasicParsing -Uri $msixAsset.browser_download_url -OutFile $msix
Invoke-WebRequest -UseBasicParsing -Uri $cerAsset.browser_download_url  -OutFile $cer

# Trust the signing cert (one-time, until uninstalled).
Write-Host "Trusting signing cert in LocalMachine\TrustedPeople..."
Import-Certificate -FilePath $cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null

# Install (or upgrade) the package.
Write-Host "Installing $msixName ..."
Add-AppxPackage -Path $msix -ForceUpdateFromAnyVersion

Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Lenovo Ripple installed." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Settings -> Personalization -> Dynamic Lighting"
Write-Host "  2. Under 'Controlled by', pick 'Lenovo Ripple'"
Write-Host "  3. Launch 'Lenovo Ripple' from the Start menu"
Write-Host ""
Write-Host "To uninstall:  Get-AppxPackage LenovoRipple | Remove-AppxPackage"
