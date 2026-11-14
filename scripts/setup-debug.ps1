$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$apiProject = Join-Path $repoRoot 'src/SoilShop.Api/SoilShop.Api.csproj'
$envPath = Join-Path $repoRoot '.env'
if (-not (Test-Path -LiteralPath $envPath)) { throw 'Run scripts/setup.ps1 first to create .env.' }

$values = @{}
foreach ($line in Get-Content -LiteralPath $envPath) {
    if ($line -match '^([A-Za-z_][A-Za-z0-9_]*)=(.*)$') {
        $values[$Matches[1]] = $Matches[2].Trim().Trim('"').Trim("'")
    }
}
foreach ($key in @('POSTGRES_PASSWORD', 'JWT_KEY', 'BOOTSTRAP_PASSWORD')) {
    if ([string]::IsNullOrWhiteSpace($values[$key])) { throw "Missing $key in .env." }
}
if ([Text.Encoding]::UTF8.GetByteCount($values['JWT_KEY']) -lt 32) { throw 'JWT_KEY needs at least 32 bytes.' }

# The SDK loads User Secrets for the Development environment used by the VS profile.
[xml]$projectXml = Get-Content -LiteralPath $apiProject -Raw
if (-not $projectXml.SelectSingleNode('//UserSecretsId')) {
    dotnet user-secrets init --id SoilShop.Api.LocalDevelopment --project $apiProject
    if ($LASTEXITCODE -ne 0) { throw 'Could not initialize User Secrets.' }
}
$password = $values['POSTGRES_PASSWORD'].Replace('"', '""')
$username = if ($values['BOOTSTRAP_USERNAME']) { $values['BOOTSTRAP_USERNAME'] } else { 'admin' }
$settings = @{
    'ConnectionStrings:Shop' = 'Host=127.0.0.1;Port=15432;Database=soilshop;Username=soilshop;Password="' + $password + '"'
    'ConnectionStrings:Redis' = '127.0.0.1:16379,abortConnect=false'
    'Calculation:Address' = 'http://127.0.0.1:18092'
    'Jwt:Key' = $values['JWT_KEY']
    'Bootstrap:Username' = $username
    'Bootstrap:Password' = $values['BOOTSTRAP_PASSWORD']
}
$settings | ConvertTo-Json -Compress | dotnet user-secrets set --project $apiProject
if ($LASTEXITCODE -ne 0) { throw 'Could not configure local API secrets.' }

docker compose --project-directory $repoRoot -f (Join-Path $repoRoot 'compose.yaml') -f (Join-Path $repoRoot 'compose.debug.yaml') up -d --build --wait postgres redis calculation
if ($LASTEXITCODE -ne 0) { throw 'Debug dependencies failed to start. Check Docker Desktop and ports 15432, 16379, 18092.' }
Write-Output 'Debug dependencies ready. Start SoilShop.Api with the Development profile in Visual Studio. Secrets were not printed.'
