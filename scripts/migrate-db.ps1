# Applies the EF Core migrations to the local Compose database. It never deletes data.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $root ".env"

function Get-EnvValue([string] $Key) {
    $line = Get-Content $envFile | Where-Object { $_ -match "^$Key=" } | Select-Object -Last 1
    if ($null -eq $line) { return "" }
    return $line.Substring($Key.Length + 1).Trim()
}

if (-not $env:ConnectionStrings__PureFreshMilk) {
    if (-not (Test-Path $envFile)) {
        throw "No .env file found. Copy .env.example to .env and set POSTGRES_PASSWORD."
    }
    $password = Get-EnvValue "POSTGRES_PASSWORD"
    if (-not $password) { throw "POSTGRES_PASSWORD is empty in .env." }
    $database = Get-EnvValue "POSTGRES_DB"; if (-not $database) { $database = "purefreshmilk" }
    $user = Get-EnvValue "POSTGRES_USER"; if (-not $user) { $user = "purefreshmilk" }
    $port = Get-EnvValue "POSTGRES_PORT"; if (-not $port) { $port = "5432" }
    $env:ConnectionStrings__PureFreshMilk =
        "Host=localhost;Port=$port;Database=$database;Username=$user;Password=$password"
}

Set-Location (Join-Path $root "backend")
dotnet tool restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet ef database update --project src/PureFreshMilk.Infrastructure --startup-project src/PureFreshMilk.Api
exit $LASTEXITCODE
