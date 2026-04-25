# Produces a signed .msix file plus the public cert needed to install it.
# Auto-fetches MakeAppx.exe and SignTool.exe from the Microsoft.Windows.SDK.BuildTools
# NuGet so you don't need the full Windows SDK installed.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File package\build-msix.ps1
#
# Output:
#   package\output\LenovoRipple.msix
#   package\output\LenovoRippleDev.cer  (one-time trust on install machine)

$ErrorActionPreference = 'Stop'

$here    = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repo    = Split-Path -Parent $here
$proj    = Join-Path $repo 'LenovoRipple.csproj'
$staged  = Join-Path $here 'staged'
$out     = Join-Path $here 'output'
$toolDir = Join-Path $here '.sdk-tools'

if (Test-Path $staged) { Remove-Item $staged -Recurse -Force }
if (-not (Test-Path $out)) { New-Item -ItemType Directory $out | Out-Null }

if (-not (Test-Path (Join-Path $repo 'Assets\AppIcon.png'))) {
    & (Join-Path $here 'generate-icons.ps1')
}

Write-Host "Publishing $proj ..."
dotnet publish $proj -c Release -r win-x64 --no-self-contained -o $staged | Out-Host
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Copy-Item (Join-Path $here 'Package.appxmanifest') (Join-Path $staged 'AppxManifest.xml') -Force
Copy-Item (Join-Path $here 'Assets')                (Join-Path $staged 'Assets') -Recurse -Force
Copy-Item (Join-Path $here 'Public')                (Join-Path $staged 'Public') -Recurse -Force
# .pdb in a packaged app trips MakeAppx (block-mapped but useless for users).
Get-ChildItem $staged -Filter '*.pdb' -Recurse | Remove-Item -Force

# --- Locate MakeAppx + SignTool ---------------------------------------------------
function Find-Tool([string] $name) {
    $cmd = Get-Command $name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    foreach ($base in @('C:\Program Files (x86)\Windows Kits\10\bin', 'C:\Program Files\Windows Kits\10\bin')) {
        if (Test-Path $base) {
            $candidates = Get-ChildItem $base -ErrorAction SilentlyContinue |
                Where-Object PSIsContainer | Sort-Object Name -Descending
            foreach ($d in $candidates) {
                $p = Join-Path $d.FullName "x64\$name"
                if (Test-Path $p) { return $p }
            }
        }
    }
    return $null
}

function Ensure-SdkBuildTools {
    param([string] $cacheDir)
    $existing = Get-ChildItem $cacheDir -Recurse -Filter 'MakeAppx.exe' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\' } | Select-Object -First 1
    if ($existing) { return Split-Path -Parent $existing.FullName }

    Write-Host "Downloading Microsoft.Windows.SDK.BuildTools NuGet (~30 MB)..."
    if (-not (Test-Path $cacheDir)) { New-Item -ItemType Directory $cacheDir | Out-Null }
    $zip = Join-Path $cacheDir 'sdk-build-tools.zip'
    Invoke-WebRequest -UseBasicParsing `
        -Uri 'https://www.nuget.org/api/v2/package/Microsoft.Windows.SDK.BuildTools' `
        -OutFile $zip
    Expand-Archive -Path $zip -DestinationPath $cacheDir -Force
    Remove-Item $zip
    $existing = Get-ChildItem $cacheDir -Recurse -Filter 'MakeAppx.exe' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\' } | Select-Object -First 1
    if (-not $existing) { throw "MakeAppx.exe not found in NuGet payload" }
    return Split-Path -Parent $existing.FullName
}

$makeAppx = Find-Tool 'MakeAppx.exe'
$signTool = Find-Tool 'SignTool.exe'
if (-not $makeAppx -or -not $signTool) {
    $toolBin = Ensure-SdkBuildTools -cacheDir $toolDir
    if (-not $makeAppx) { $makeAppx = Join-Path $toolBin 'MakeAppx.exe' }
    if (-not $signTool) { $signTool = Join-Path $toolBin 'SignTool.exe' }
}
Write-Host "MakeAppx: $makeAppx"
Write-Host "SignTool: $signTool"

# --- Pack ------------------------------------------------------------------------
$msix = Join-Path $out 'LenovoRipple.msix'
if (Test-Path $msix) { Remove-Item $msix -Force }

Write-Host "Packing $msix ..."
& $makeAppx pack /d $staged /p $msix /o
if ($LASTEXITCODE -ne 0) { throw "MakeAppx failed" }

# --- Cert + sign ------------------------------------------------------------------
$cert = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object { $_.Subject -eq 'CN=LenovoRippleDev' } |
    Select-Object -First 1

if (-not $cert) {
    Write-Host "Creating self-signed code-signing cert (CN=LenovoRippleDev)..."
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject 'CN=LenovoRippleDev' `
        -KeyUsage DigitalSignature `
        -FriendlyName 'Lenovo Ripple Dev' `
        -CertStoreLocation Cert:\CurrentUser\My `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3','2.5.29.19={text}')
}

$cer = Join-Path $out 'LenovoRippleDev.cer'
Export-Certificate -Cert $cert -FilePath $cer | Out-Null

Write-Host "Signing..."
& $signTool sign /fd SHA256 /a /sha1 $cert.Thumbprint $msix
if ($LASTEXITCODE -ne 0) { throw "SignTool failed" }

Write-Host ""
Write-Host "Built and signed:" -ForegroundColor Green
Write-Host "  $msix"
Write-Host "  $cer"
Write-Host ""
Write-Host "Install on a fresh machine:" -ForegroundColor Cyan
Write-Host "  1. Trust the cert (one-time, as Administrator):"
Write-Host "       Import-Certificate -FilePath '$cer' -CertStoreLocation Cert:\LocalMachine\TrustedPeople"
Write-Host "  2. Install the package:"
Write-Host "       Add-AppxPackage -Path '$msix'"
