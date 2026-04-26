[CmdletBinding()]
param(
    [ValidateSet("CurrentUser", "LocalMachine", "All")]
    [string]$Scope = "CurrentUser"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$certPath = Join-Path $repoRoot "Dev\Typedown.WinUI\Typedown.WinUI.DevTest.cer"
$pfxPath = Join-Path $repoRoot "Dev\Typedown.WinUI\Typedown.WinUI.DevTest.pfx"
$password = ConvertTo-SecureString "TypedownWinUIDevTest" -AsPlainText -Force

if (-not (Test-Path $certPath)) {
    throw "WinUI dev certificate not found: $certPath"
}

$pfxImport = Import-PfxCertificate -FilePath $pfxPath -CertStoreLocation Cert:\CurrentUser\My -Password $password
$certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certPath)
$subject = $certificate.Subject
$thumbprint = $pfxImport.Thumbprint

function Import-CertificateIfMissing {
    param(
        [Parameter(Mandatory = $true)]
        [string]$StorePath,
        [Parameter(Mandatory = $true)]
        [string]$DisplayName
    )

    if (Get-ChildItem $StorePath | Where-Object Thumbprint -eq $thumbprint) {
        Write-Host "Typedown WinUI dev certificate already trusted in $DisplayName."
        return
    }

    Import-Certificate -FilePath $certPath -CertStoreLocation $StorePath | Out-Null
    Write-Host "Imported Typedown WinUI dev certificate into $DisplayName."
}

if ($Scope -in @("CurrentUser", "All")) {
    Import-CertificateIfMissing -StorePath Cert:\CurrentUser\TrustedPeople -DisplayName "CurrentUser\\TrustedPeople"
    Import-CertificateIfMissing -StorePath Cert:\CurrentUser\Root -DisplayName "CurrentUser\\Root"
}

if ($Scope -in @("LocalMachine", "All")) {
    Import-CertificateIfMissing -StorePath Cert:\LocalMachine\TrustedPeople -DisplayName "LocalMachine\\TrustedPeople"
    Import-CertificateIfMissing -StorePath Cert:\LocalMachine\Root -DisplayName "LocalMachine\\Root"
}

Write-Host "Subject: $subject"
Write-Host "Thumbprint: $thumbprint"
