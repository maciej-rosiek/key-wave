# Produces a signed .msix file that can be distributed.
# Requires:
#   - Windows SDK installed (MakeAppx.exe and SignTool.exe must be in PATH or in
#     a default Windows Kits location).
#   - A code-signing cert in Cert:\CurrentUser\My with subject CN=LenovoRippleDev.
#     If absent, this script creates a self-signed one (you may need to run
#     PowerShell as Administrator the first time).
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File package\build-msix.ps1

$ErrorActionPreference = 'Stop'

$here   = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repo   = Split-Path -Parent $here
$proj   = Join-Path $repo 'LenovoRipple.csproj'
$staged = Join-Path $here 'staged'
$out    = Join-Path $here 'output'

if (Test-Path $staged) { Remove-Item $staged -Recurse -Force }
if (-not (Test-Path $out)) { New-Item -ItemType Directory $out | Out-Null }

if (-not (Test-Path (Join-Path $repo 'Assets\AppIcon.png'))) {
    & (Join-Path $here 'generate-icons.ps1')
}

Write-Host "Publishing..."
dotnet publish $proj -c Release -r win-x64 --self-contained false -o $staged | Out-Host
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Copy-Item (Join-Path $here 'Package.appxmanifest') (Join-Path $staged 'AppxManifest.xml') -Force
Copy-Item (Join-Path $here 'Assets')                (Join-Path $staged 'Assets') -Recurse -Force
Copy-Item (Join-Path $here 'Public')                (Join-Path $staged 'Public') -Recurse -Force

# Find MakeAppx.exe / SignTool.exe — try PATH first, then a typical SDK location.
function Find-SdkTool([string] $name) {
    $cmd = Get-Command $name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $candidates = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -ErrorAction SilentlyContinue |
        Where-Object { $_.PSIsContainer } |
        Sort-Object -Property Name -Descending
    foreach ($dir in $candidates) {
        $p = Join-Path $dir.FullName "x64\$name"
        if (Test-Path $p) { return $p }
    }
    throw "$name not found. Install the Windows SDK or add it to PATH."
}

$makeAppx = Find-SdkTool 'MakeAppx.exe'
$signTool = Find-SdkTool 'SignTool.exe'

$msix = Join-Path $out 'LenovoRipple.msix'
Write-Host "Packing $msix ..."
& $makeAppx pack /d $staged /p $msix /o
if ($LASTEXITCODE -ne 0) { throw "MakeAppx failed" }

# Code-signing cert.
$cert = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object { $_.Subject -eq 'CN=LenovoRippleDev' } |
    Select-Object -First 1

if (-not $cert) {
    Write-Host "No CN=LenovoRippleDev cert found. Creating a self-signed cert..."
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject 'CN=LenovoRippleDev' `
        -KeyUsage DigitalSignature `
        -FriendlyName 'Lenovo Ripple Dev' `
        -CertStoreLocation Cert:\CurrentUser\My `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3','2.5.29.19={text}')
    # Export the public cert so it can be trusted on the install machine.
    $cer = Join-Path $out 'LenovoRippleDev.cer'
    Export-Certificate -Cert $cert -FilePath $cer | Out-Null
    Write-Host "Public cert exported: $cer"
}

Write-Host "Signing..."
& $signTool sign /fd SHA256 /a /sha1 $cert.Thumbprint $msix
if ($LASTEXITCODE -ne 0) { throw "SignTool failed" }

Write-Host ""
Write-Host "Built and signed:"
Write-Host "  $msix"
Write-Host ""
Write-Host "To install on a fresh machine:"
Write-Host "  1. (one time) Trust the cert as Administrator:"
Write-Host "       Import-Certificate -FilePath '$out\LenovoRippleDev.cer' -CertStoreLocation Cert:\LocalMachine\TrustedPeople"
Write-Host "  2. Add-AppxPackage -Path '$msix'"
