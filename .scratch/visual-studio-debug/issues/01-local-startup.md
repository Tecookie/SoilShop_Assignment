# Configure Visual Studio API dependencies

Status: Complete

- Register API UserSecretsId and import local connection/bootstrap/JWT settings.
- Add opt-in loopback dependency ports in compose.debug.yaml.
- Add scripts/setup-debug.ps1 and document the F5 workflow.
- Verify native API startup, PostgreSQL initialization, health, Swagger, OpenAPI
  and Redis connectivity without printing secrets or changing database volumes.
