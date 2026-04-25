# Dev registration: publish the WPF app, drop the appxmanifest next to the exe,
# and register the package against that folder. No MSIX file or signing needed.
# After this, the running exe has package identity and the
# com.microsoft.windows.lighting AppExtension is recognized.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File package\dev-register.ps1

$ErrorActionPreference = 'Stop'

$here   = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repo   = Split-Path -Parent $here
$proj   = Join-Path $repo 'KeyWave.csproj'
$staged = Join-Path $here 'staged'

if (Test-Path $staged) { Remove-Item $staged -Recurse -Force }

if (-not (Test-Path (Join-Path $repo 'Assets\AppIcon.png'))) {
    & (Join-Path $here 'generate-icons.ps1')
}

Write-Host "Publishing $proj ..."
dotnet publish $proj -c Release -r win-x64 --self-contained false -o $staged | Out-Host
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Copy-Item (Join-Path $here 'Package.appxmanifest') (Join-Path $staged 'AppxManifest.xml') -Force
Copy-Item (Join-Path $here 'Assets')                (Join-Path $staged 'Assets') -Recurse -Force
Copy-Item (Join-Path $here 'Public')                (Join-Path $staged 'Public') -Recurse -Force

Write-Host ""
Write-Host "Registering package..."
try {
    Add-AppxPackage -Register (Join-Path $staged 'AppxManifest.xml') -ErrorAction Stop
} catch {
    Write-Host ""
    Write-Host "Add-AppxPackage failed: $_" -ForegroundColor Red
    if ($_.Exception.Message -match '0x80073CFF') {
        Write-Host ""
        Write-Host "This means Developer Mode is OFF." -ForegroundColor Yellow
        Write-Host "Loose-registering an unsigned manifest needs Developer Mode."
        Write-Host ""
        Write-Host "Fix:" -ForegroundColor Cyan
        Write-Host "  Settings -> Privacy & security -> For developers -> Developer Mode = On"
        Write-Host "  Then re-run this script."
    }
    exit 1
}

Write-Host ""
Write-Host "Done. The exe now has package identity."
Write-Host "  - Launch from Start menu: 'KeyWave'"
Write-Host "  - Then: Settings -> Personalization -> Dynamic Lighting,"
Write-Host "         under 'Controlled by' pick 'KeyWave'."
Write-Host ""
Write-Host "Re-running this script after a code change will re-publish and re-register."
Write-Host "To unregister: Get-AppxPackage KeyWave | Remove-AppxPackage"
