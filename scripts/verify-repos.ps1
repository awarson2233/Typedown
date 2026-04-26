[CmdletBinding()]
param(
    [string]$MainRepo = "D:\source\repos\Typedown",
    [string]$XamlUIRepo = "D:\source\repos\Typedown.XamlUI",
    [string]$ExpectedBranch = "winui3-migration",
    [switch]$AllowMainDirty,
    [switch]$AllowXamlUIDirty
)

$ErrorActionPreference = "Stop"

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)][string]$Repo,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    $output = & git -C $Repo @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed in $Repo"
    }
    return $output
}

function Test-Repo {
    param(
        [Parameter(Mandatory = $true)][string]$Repo,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][bool]$AllowDirty
    )

    if (-not (Test-Path -LiteralPath $Repo)) {
        throw "$Name repo not found: $Repo"
    }

    Invoke-Git $Repo @("rev-parse", "--is-inside-work-tree") | Out-Null

    $branch = (Invoke-Git $Repo @("branch", "--show-current")).Trim()
    if ($branch -ne $ExpectedBranch) {
        throw "$Name repo is on branch '$branch', expected '$ExpectedBranch'"
    }

    $remotes = Invoke-Git $Repo @("remote")
    if ($remotes -notcontains "awarson2233") {
        throw "$Name repo is missing remote 'awarson2233'"
    }

    $dirty = Invoke-Git $Repo @("status", "--porcelain=v1")
    if (-not $AllowDirty -and $dirty) {
        throw "$Name repo has uncommitted changes. Re-run with the matching Allow*Dirty switch only for local verification."
    }

    $head = (Invoke-Git $Repo @("rev-parse", "HEAD")).Trim()
    $summary = (Invoke-Git $Repo @("log", "-1", "--oneline")).Trim()
    Write-Host "$Name OK"
    Write-Host "  Path: $Repo"
    Write-Host "  Branch: $branch"
    Write-Host "  HEAD: $head"
    Write-Host "  Commit: $summary"
    if ($dirty) {
        Write-Host "  Dirty: allowed for this run"
    }
    else {
        Write-Host "  Dirty: no"
    }
}

Test-Repo -Repo $MainRepo -Name "Typedown" -AllowDirty:$AllowMainDirty.IsPresent
Test-Repo -Repo $XamlUIRepo -Name "Typedown.XamlUI" -AllowDirty:$AllowXamlUIDirty.IsPresent

Write-Host "Repository verification completed."
