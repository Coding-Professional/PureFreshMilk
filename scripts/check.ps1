# Runs the same checks as CI (.github/workflows/ci.yml), stopping at the first failure.
# Expects dependencies to be installed (npm ci in apps/web).
#
#   pwsh scripts/check.ps1              # backend + web checks (the database tests are left out)
#   pwsh scripts/check.ps1 -Database    # also the database tests; start PostgreSQL first: docker compose up -d --wait
#   pwsh scripts/check.ps1 -E2E         # also the Playwright browser tests
param([switch] $Database, [switch] $E2E)
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
# Tests that need PostgreSQL are tagged Category=Database. The test project that holds only those is left with
# no tests, which ends with exit code 8 ("zero tests ran").
Invoke-Checked 'dotnet test --solution PureFreshMilk.slnx --configuration Release --no-build --filter-not-trait "Category=Database" --ignore-exit-code 8'

if ($Database) {
    Write-Host "`n==> Backend: database tests (PostgreSQL from compose.yaml)"
    if (-not $env:ConnectionStrings__PureFreshMilkTests) {
        $envFile = Join-Path $root ".env"
        if (-not (Test-Path $envFile)) {
            throw "No .env file found. Copy .env.example to .env and set POSTGRES_PASSWORD, or set ConnectionStrings__PureFreshMilkTests."
        }
        # Reads KEY=value from .env without executing it as code.
        function Get-EnvValue([string] $Name, [string] $Default) {
            $line = Get-Content $envFile | Where-Object { $_ -match "^$Name=" } | Select-Object -Last 1
            if ($line) { return ($line -replace "^$Name=", "").Trim() } else { return $Default }
        }
        $password = Get-EnvValue "POSTGRES_PASSWORD" ""
        if (-not $password) { throw "POSTGRES_PASSWORD is empty in .env." }
        $port = Get-EnvValue "POSTGRES_PORT" "5432"
        $db = Get-EnvValue "POSTGRES_DB" "purefreshmilk"
        $user = Get-EnvValue "POSTGRES_USER" "purefreshmilk"
        $env:ConnectionStrings__PureFreshMilkTests = "Host=localhost;Port=$port;Database=$db;Username=$user;Password=$password"
    }
    try { Invoke-Checked "dotnet test --project tests/PureFreshMilk.Infrastructure.Tests --configuration Release --no-build" }
    finally { Remove-Item Env:ConnectionStrings__PureFreshMilkTests -ErrorAction SilentlyContinue }
}

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
