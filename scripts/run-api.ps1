# Runs the API against the local PostgreSQL container (compose.yaml).
# Reads the database settings from the repository's .env file unless
# ConnectionStrings__MilkManager is already set in the environment.
#
#   pwsh scripts/run-api.ps1        (or: powershell -File scripts\run-api.ps1)
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $root ".env"

# Reads KEY=value from .env without executing it.
function Get-EnvValue([string] $Key) {
    $line = Get-Content $envFile | Where-Object { $_ -match "^$Key=" } | Select-Object -Last 1
    if ($null -eq $line) { return "" }
    return $line.Substring($Key.Length + 1).Trim()
}

if (-not $env:ConnectionStrings__MilkManager) {
    if (-not (Test-Path $envFile)) {
        throw "No .env file found. Copy .env.example to .env and set POSTGRES_PASSWORD."
    }

    $password = Get-EnvValue "POSTGRES_PASSWORD"
    if (-not $password) { throw "POSTGRES_PASSWORD is empty in .env." }

    $database = Get-EnvValue "POSTGRES_DB"; if (-not $database) { $database = "milkmanager" }
    $user = Get-EnvValue "POSTGRES_USER"; if (-not $user) { $user = "milkmanager" }
    $port = Get-EnvValue "POSTGRES_PORT"; if (-not $port) { $port = "5432" }

    $env:ConnectionStrings__MilkManager =
        "Host=localhost;Port=$port;Database=$database;Username=$user;Password=$password"
}

Set-Location (Join-Path $root "backend")
dotnet run --project src/MilkManager.Api --launch-profile http @args
exit $LASTEXITCODE
