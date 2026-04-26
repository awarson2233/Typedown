[CmdletBinding()]
param(
    [string]$RepoRoot = "D:\source\repos\Typedown",
    [string]$Configuration = "Debug_Local",
    [string]$Platform = "x64",
    [string]$ExpectedMainBranch = "winui3-migration",
    [string]$ExpectedXamlUIBranch = "winui3-migration",
    [switch]$AllowMainDirty,
    [switch]$AllowXamlUIDirty,
    [switch]$SkipEditorBuild
)

$ErrorActionPreference = "Stop"

$repoCheck = Join-Path $RepoRoot "scripts\verify-repos.ps1"
if (-not (Test-Path -LiteralPath $repoCheck)) {
    throw "Repository verification script not found: $repoCheck"
}

& $repoCheck -MainRepo $RepoRoot -ExpectedMainBranch $ExpectedMainBranch -ExpectedXamlUIBranch $ExpectedXamlUIBranch -AllowMainDirty:$AllowMainDirty.IsPresent -AllowXamlUIDirty:$AllowXamlUIDirty.IsPresent
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

$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path -LiteralPath $vswhere)) {
    throw "vswhere not found: $vswhere"
}

$msbuild = (& $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\Current\Bin\MSBuild.exe" | Select-Object -First 1)
if (-not $msbuild) {
    throw "MSBuild not found through vswhere."
}

$project = Join-Path $RepoRoot "Dev\Typedown\Typedown.csproj"
if (-not (Test-Path -LiteralPath $project)) {
    throw "Typedown project not found: $project"
}

Write-Host "Building Typedown $Configuration|$Platform."
& $msbuild $project /restore /t:Build /p:Configuration=$Configuration /p:Platform=$Platform /p:UseSharedCompilation=false /nologo /m:1 /nodeReuse:false /v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed for $Configuration|$Platform."
}

$runtimeId = switch ($Platform.ToLowerInvariant()) {
    "x64" { "win-x64" }
    "x86" { "win-x86" }
    "arm64" { "win-arm64" }
    default { throw "Unsupported platform: $Platform" }
}

$exe = Join-Path $RepoRoot "Dev\Typedown\bin\$Platform\$Configuration\net9.0-windows10.0.26100.0\$runtimeId\Typedown.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Expected executable not found: $exe"
}

Write-Host "Baseline build completed."
Write-Host "Executable: $exe"
