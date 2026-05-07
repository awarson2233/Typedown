[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$editorRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$sourceCandidates = @(
    (Join-Path $editorRoot "..\Typedown\Resources\Statics"),
    (Join-Path $editorRoot "build")
)
$targetDir = Join-Path $editorRoot "..\Typedown.WinUI\Resources\Statics"
$targetParent = Split-Path -Parent $targetDir

foreach ($candidate in $sourceCandidates) {
    if (Test-Path -LiteralPath $candidate) {
        $sourceDir = (Resolve-Path $candidate).Path
        break
    }
}

if (-not $sourceDir) {
    throw "Editor build output not found. Checked: $($sourceCandidates -join ', ')"
}

if (-not (Test-Path -LiteralPath $targetParent)) {
    throw "WinUI resources directory not found: $targetParent"
}

New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

Get-ChildItem -LiteralPath $targetDir -Force | Remove-Item -Recurse -Force
Copy-Item -Path (Join-Path $sourceDir "*") -Destination $targetDir -Recurse -Force

Write-Host "Synced editor static bundle to $targetDir"
