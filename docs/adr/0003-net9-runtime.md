# Target .NET 9 for the assignment

The user requested .NET 9 after the first .NET 10 implementation. All projects now target net9.0. Pin SDK 9.0.310 with roll-forward within .NET 9, Microsoft/EF packages and dotnet-ef 9.0.22, and Npgsql EF provider 9.0.4. Docker uses the corresponding SDK and ASP.NET 9 runtime. AutoMapper, gRPC and Redis remain in use.

Preserve existing migration IDs and applied database history. Historical migration designer ProductVersion annotations describe the tooling that originally generated them; they are not runtime targets. EF9 reports no pending model changes, so no schema/reset migration is necessary. Regenerate the idempotent SQL with EF9. Test both a fresh migrated database and the existing Compose database, preserving its stock/documents. Use the installed .NET 9 SDK for native tests rather than the workspace's optional .NET 10-only tool cache.
