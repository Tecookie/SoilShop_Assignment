param([switch]$SkipBuild, [switch]$WithAdvice)
$ErrorActionPreference = 'Stop'
$rootPath = Split-Path $PSScriptRoot -Parent
Push-Location $rootPath
try {
    if (!(Test-Path -LiteralPath '.env')) { & ./scripts/setup.ps1 }
    $lines = [IO.File]::ReadAllLines((Join-Path $rootPath '.env'))
    $added = [Collections.Generic.List[string]]::new()
    if (!($lines | Where-Object { $_ -match '^DEMO_EMPLOYEE_USERNAME=' })) { $added.Add('DEMO_EMPLOYEE_USERNAME=employee') }
    if (!($lines | Where-Object { $_ -match '^DEMO_EMPLOYEE_PASSWORD=' })) {
        $password = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)).ToLowerInvariant()
        $added.Add("DEMO_EMPLOYEE_PASSWORD=$password")
    }
    if ($added.Count -gt 0) {
        $envFile = Join-Path $rootPath '.env'
        $current = [IO.File]::ReadAllText($envFile)
        if ($current.Length -gt 0 -and !$current.EndsWith("`n")) { [IO.File]::AppendAllText($envFile, [Environment]::NewLine) }
        [IO.File]::AppendAllLines($envFile, $added)
    }
    if ($SkipBuild) { docker compose up -d } else { docker compose up -d --build }
    if ($LASTEXITCODE -ne 0) { throw 'Could not start the shop services.' }
    # Wait for API migration/bootstrap completion before starting a second API process.
    $portLine = $lines | Where-Object { $_ -match '^WEB_PORT=\d+$' } | Select-Object -First 1
    $webPort = if ($portLine) { [int]($portLine -split '=',2)[1] } else { 8080 }
    $ready = $false
    for ($attempt = 0; $attempt -lt 45; $attempt++) {
        try { $response = Invoke-WebRequest "http://127.0.0.1:$webPort/api/openapi/v1.json" -TimeoutSec 2; if ($response.StatusCode -eq 200) { $ready = $true; break } } catch { }
        Start-Sleep -Seconds 1
    }
    if (!$ready) { throw 'API did not become ready. Review docker compose logs api before retrying.' }
    # One-shot process: no HTTP port or public seeding endpoint. Existing shop data is retained.
    if ($WithAdvice) { docker compose run --rm --no-deps -e DemoSeed__Run=true -e DemoSeed__WithAdvice=true api }
    else { docker compose run --rm --no-deps -e DemoSeed__Run=true api }
    if ($LASTEXITCODE -ne 0) { throw 'Demo seed did not finish. Correct the reported error and rerun; completed steps are preserved.' }
    Write-Output 'Demo import completed. Local Employee credentials are in ignored .env; no credentials were printed.'
} finally { Pop-Location }
