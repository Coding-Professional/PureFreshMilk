# Runs the same checks as CI (.github/workflows/ci.yml), stopping at the first failure.
# Expects dependencies to be installed (npm ci in apps/web).
#
#   pwsh scripts/check.ps1          # backend + web checks
#   pwsh scripts/check.ps1 -E2E     # also the Playwright browser tests
param([switch] $E2E)
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot

# Native commands do not throw on failure in Windows PowerShell, so check exit codes.
function Invoke-Checked([string] $Command) {
    Write-Host "> $Command"
    Invoke-Expression $Command
    if ($LASTEXITCODE -ne 0) { throw "Failed ($LASTEXITCODE): $Command" }
}

Write-Host "`n==> Backend: restore, format, build, test"
Set-Location (Join-Path $root "backend")
Invoke-Checked "dotnet restore PureFreshMilk.slnx"
Invoke-Checked "dotnet format PureFreshMilk.slnx --verify-no-changes --no-restore"
Invoke-Checked "dotnet build PureFreshMilk.slnx --configuration Release --no-restore"
Invoke-Checked "dotnet test --solution PureFreshMilk.slnx --configuration Release --no-build"

Write-Host "`n==> Web: format, lint, types, unit tests, build"
Set-Location (Join-Path $root "apps/web")
Invoke-Checked "npm run format:check"
Invoke-Checked "npm run lint"
Invoke-Checked "npm run typecheck"
Invoke-Checked "npm test"
Invoke-Checked "npm run build"

if ($E2E) {
    Write-Host "`n==> Web: browser tests"
    # CI=1 makes Playwright serve the build above instead of building again.
    $env:CI = "1"
    try { Invoke-Checked "npm run test:e2e" } finally { Remove-Item Env:CI }
}

Write-Host "`n==> All checks passed"
