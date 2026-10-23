$ErrorActionPreference = 'Stop'
$envPath = Join-Path (Split-Path $PSScriptRoot -Parent) '.env'
if (Test-Path -LiteralPath $envPath) { Write-Output 'Existing .env preserved.'; exit 0 }
function New-Secret { return [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)).ToLowerInvariant() }
$content = @(
    "POSTGRES_PASSWORD=$(New-Secret)",
    "JWT_KEY=$(New-Secret)",
    'BOOTSTRAP_USERNAME=admin',
    "BOOTSTRAP_PASSWORD=$(New-Secret)",
    'WEB_PORT=8080',
    'AUTOMAPPER_LICENSE_KEY='
)
[IO.File]::WriteAllLines($envPath, $content)
Write-Output 'Created ignored .env. Read BOOTSTRAP_USERNAME and BOOTSTRAP_PASSWORD locally to sign in. No credentials were printed.'
