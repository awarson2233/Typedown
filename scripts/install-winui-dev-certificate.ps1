[CmdletBinding()]
param()

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

$alreadyTrusted = Get-ChildItem Cert:\CurrentUser\TrustedPeople | Where-Object Thumbprint -eq $thumbprint
if ($alreadyTrusted) {
    Write-Host "Typedown WinUI dev certificate already trusted in CurrentUser\\TrustedPeople."
} else {
    Import-Certificate -FilePath $certPath -CertStoreLocation Cert:\CurrentUser\TrustedPeople | Out-Null
    Write-Host "Imported Typedown WinUI dev certificate into CurrentUser\\TrustedPeople."
}

if (Get-ChildItem Cert:\CurrentUser\Root | Where-Object Thumbprint -eq $thumbprint) {
    Write-Host "Typedown WinUI dev certificate already trusted in CurrentUser\\Root."
} else {
    Import-Certificate -FilePath $certPath -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
    Write-Host "Imported Typedown WinUI dev certificate into CurrentUser\\Root."
}

Write-Host "Subject: $subject"
Write-Host "Thumbprint: $thumbprint"
