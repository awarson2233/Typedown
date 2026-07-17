[CmdletBinding()]
param(
    [string]$RepoRoot = "D:\source\repos\Typedown",
    [string]$Configuration = "Debug_Local",
    [string]$Platform = "ARM64",
    [string]$MSBuildPath = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\arm64\MSBuild.exe",
    [string]$ExpectedMainBranch = "winui3-migration",
    [switch]$AllowMainDirty
)

$ErrorActionPreference = "Stop"

$repoCheck = Join-Path $RepoRoot "scripts\verify-repos.ps1"
if (-not (Test-Path -LiteralPath $repoCheck)) {
    throw "Repository verification script not found: $repoCheck"
}

& $repoCheck -MainRepo $RepoRoot -ExpectedMainBranch $ExpectedMainBranch -AllowMainDirty:$AllowMainDirty.IsPresent
if ($LASTEXITCODE -ne 0) {
    throw "Repository verification failed."
}

$project = Join-Path $RepoRoot "Dev\Typedown.WinUI\Typedown.WinUI.csproj"
if (-not (Test-Path -LiteralPath $project)) {
    throw "Typedown.WinUI project not found: $project"
}

if (-not (Test-Path -LiteralPath $MSBuildPath)) {
    throw "ARM64 MSBuild not found: $MSBuildPath"
}

Write-Host "Building Typedown.WinUI $Configuration|$Platform (Typedown.Editor is built through ProjectReference)."
& $MSBuildPath $project /restore /t:Build /p:Configuration=$Configuration /p:Platform=$Platform /p:UseSharedCompilation=false /m:1 /nodeReuse:false /v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed for $Configuration|$Platform."
}

$runtimeId = switch ($Platform.ToLowerInvariant()) {
    "x64" { "win-x64" }
    "x86" { "win-x86" }
    "arm64" { "win-arm64" }
    default { throw "Unsupported platform: $Platform" }
}

$exe = Join-Path $RepoRoot "Dev\Typedown.WinUI\bin\$Platform\$Configuration\net10.0-windows10.0.26100.0\$runtimeId\Typedown.WinUI.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Expected executable not found: $exe"
}

Write-Host "Baseline build completed."
Write-Host "Executable: $exe"
