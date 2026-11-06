# Local configuration and JWT troubleshooting

Docker Compose reads the root .env and maps JWT_KEY to Jwt__Key. Native dotnet run / Visual Studio does not load .env automatically. ASP.NET Core reads appsettings.json plus appsettings.<Environment>.json; environment variables override JSON. Jwt__Key maps to Jwt:Key.

For native development, place local settings in src/SoilShop.Api/appsettings.Development.json and run:

```powershell
dotnet run --project src/SoilShop.Api --launch-profile SoilShop.Api
```

That profile must set ASPNETCORE_ENVIRONMENT=Development. When running without a profile, set the environment explicitly:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/SoilShop.Api --no-launch-profile
```

The API requires at least 32 UTF-8 bytes in Jwt:Key. A missing key is a different problem from a short value. Startup errors report the environment, content root and missing/short status, without printing the key. A Development-only JSON file is not read in Production. Check environment overrides and launch arguments before changing an already sufficient key. Local files containing real credentials must remain outside commits.

On 6 November 2026, the local Development JSON contained a 44-byte key. Native launch with profile passed JWT validation and reached PostgreSQL connection initialization. Native launch without profile reproduced the missing-key error in Production. Database connectivity is a separate setup requirement: Compose currently exposes only Web; native API requires an accessible PostgreSQL/Redis endpoint. Bootstrap credentials only create the first Admin in an empty database.
