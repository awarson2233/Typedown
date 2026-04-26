[CmdletBinding()]
param(
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $scriptRoot = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        Split-Path -Parent $MyInvocation.MyCommand.Path
    }
    else {
        $PSScriptRoot
    }

    $RepoRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
}

function Read-RequiredFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required file not found: $Path"
    }

    Get-Content -LiteralPath $Path
}

$solution = Join-Path $RepoRoot "Typedown.sln"
$appProject = Join-Path $RepoRoot "Dev\Typedown\Typedown.csproj"
$coreProject = Join-Path $RepoRoot "Dev\Typedown.Core\Typedown.Core.csproj"
$packageProject = Join-Path $RepoRoot "Tools\Typedown.Package\Typedown.Package.wapproj"

Write-Host "Build matrix inspection"
Write-Host "RepoRoot: $RepoRoot"
Write-Host ""

Write-Host "Solution ARM64 mappings that point to x64:"
$solutionLines = Read-RequiredFile $solution
$x64Arm64Mappings = $solutionLines | Where-Object { $_ -match '\|ARM64\.(ActiveCfg|Build\.0|Deploy\.0)\s*=\s*[^|]+\|x64' }
if ($x64Arm64Mappings) {
    $x64Arm64Mappings | ForEach-Object { Write-Host "  $_" }
}
else {
    Write-Host "  None"
}

Write-Host ""
Write-Host "Project RuntimeIdentifier / platform declarations:"
foreach ($project in @($appProject, $coreProject, $packageProject)) {
    Write-Host "  $($project.Substring($RepoRoot.Length + 1))"
    $lines = Read-RequiredFile $project
    $matches = $lines | Where-Object {
        $_ -match '<Platforms>' -or
        $_ -match '<RuntimeIdentifier' -or
        $_ -match '<RuntimeIdentifiers>' -or
        $_ -match '<AppxBundlePlatforms'
    }

    if ($matches) {
        $matches | ForEach-Object { Write-Host "    $($_.Trim())" }
    }
    else {
        Write-Host "    No matching declarations"
    }
}

Write-Host ""
Write-Host "Phase 7 policy: ARM64 is documented only. Do not use this output as a passing ARM64 build signal."
