# Run the API directly from Visual Studio

Status: Complete

Problem: the Development launch profile did not receive Compose environment
configuration, so API startup failed first on Jwt:Key and then ConnectionStrings:Shop.

Use API User Secrets and an explicitly selected Compose debug override. Publish
dependency ports on loopback only, preserve existing database volumes, and keep
the default six-service deployment configuration unchanged. Do not commit secrets.

## Verification

On 2026-11-14, setup-debug.ps1 completed successfully. PostgreSQL and Redis were
healthy and Calculation started. The API ran directly on Windows in Development
on temporary port 18577 using the configured secrets. Database startup reported
no pending migrations. Health returned ready, Swagger returned HTTP 200 and
OpenAPI returned Soil Shop API. Redis returned PONG. The temporary API process
was stopped after verification. The actual Visual Studio F5 session and a gRPC
business call were not exercised by this startup check.
