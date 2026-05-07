[CmdletBinding()]
param(
    [string]$RepoRoot = "D:\source\repos\Typedown",
    [string]$Configuration = "Debug_Local",
    [string]$Platform = "x64",
    [string]$ExpectedMainBranch = "winui3-migration",
    [switch]$AllowMainDirty,
    [switch]$SkipEditorBuild
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

$editorDir = Join-Path $RepoRoot "Dev\Typedown.Editor"
if (-not $SkipEditorBuild) {
    if (-not (Test-Path -LiteralPath (Join-Path $editorDir "package.json"))) {
        throw "Editor package.json not found: $editorDir"
    }

    Push-Location $editorDir
    try {
        if (-not (Test-Path -LiteralPath "node_modules")) {
            Write-Host "node_modules not found; running yarn."
            & yarn
            if ($LASTEXITCODE -ne 0) {
                throw "yarn failed."
            }
        }

        Write-Host "Building editor static assets."
        & yarn build
        if ($LASTEXITCODE -ne 0) {
            throw "yarn build failed."
        }
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "Skipping editor build by request."
}

$project = Join-Path $RepoRoot "Dev\Typedown.WinUI\Typedown.WinUI.csproj"
if (-not (Test-Path -LiteralPath $project)) {
    throw "Typedown.WinUI project not found: $project"
}

Write-Host "Building Typedown.WinUI $Configuration|$Platform."
& dotnet build $project -c $Configuration -p:Platform=$Platform -p:UseSharedCompilation=false /nodeReuse:false /v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed for $Configuration|$Platform."
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
