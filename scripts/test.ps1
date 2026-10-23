$ErrorActionPreference = 'Stop'
$rootPath = Split-Path $PSScriptRoot -Parent
Push-Location $rootPath
$containerName = 'soilshop-test-' + [guid]::NewGuid().ToString('N').Substring(0,12)
$previousConnection = $env:SHOP_TEST_CONNECTION
$previousPassword = $env:POSTGRES_PASSWORD
$previousDotnetRoot = $env:DOTNET_ROOT
try {
    $dotnetPath = if (Test-Path '.tools/dotnet/dotnet.exe') { (Resolve-Path '.tools/dotnet/dotnet.exe').Path } else { (Get-Command dotnet).Source }
    $env:DOTNET_ROOT = Split-Path $dotnetPath -Parent
    $env:POSTGRES_PASSWORD = [guid]::NewGuid().ToString('N')
    docker run -d --name $containerName --memory 512m -e POSTGRES_PASSWORD -e POSTGRES_DB=soilshop_test -p '127.0.0.1::5432' --tmpfs /var/lib/postgresql/data postgres:16-alpine | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not start isolated PostgreSQL.' }
    $ready = $false
    for ($attempt = 0; $attempt -lt 45; $attempt++) {
        docker exec $containerName pg_isready -U postgres -d soilshop_test 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (!$ready) { throw 'Test database did not become ready.' }
    $port = ((docker port $containerName 5432/tcp) -split ':')[-1]
    $env:SHOP_TEST_CONNECTION = "Host=127.0.0.1;Port=$port;Database=soilshop_test;Username=postgres;Password=$env:POSTGRES_PASSWORD;Include Error Detail=false"
    & $dotnetPath test tests/SoilShop.Tests --logger 'trx;LogFileName=application.trx' --results-directory test-results --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'Application tests failed.' }
} finally {
    # Only the uniquely named container created by this script is removed.
    docker rm --force --volumes $containerName 2>$null | Out-Null
    $env:SHOP_TEST_CONNECTION = $previousConnection
    $env:POSTGRES_PASSWORD = $previousPassword
    $env:DOTNET_ROOT = $previousDotnetRoot
    Pop-Location
}
