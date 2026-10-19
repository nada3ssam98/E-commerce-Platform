var builder = DistributedApplication.CreateBuilder(args);

// Role passwords: generated on first run and persisted in this project's user secrets (never committed).
var migratorPassword = builder.AddParameter(
    "platform-migrator-password", new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);
var appPassword = builder.AddParameter(
    "platform-app-password", new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);

// PostgreSQL 17 (ADR-02). The bootstrap script creates the platform database and its migration and
// application roles on first start (dev/postgres/init/01-bootstrap.sh).
var postgres = builder.AddPostgres("postgres")
    .WithImageTag("17.11")
    .WithEnvironment("PLATFORM_MIGRATOR_PASSWORD", migratorPassword)
    .WithEnvironment("PLATFORM_APP_PASSWORD", appPassword)
    .WithInitFiles(Path.Combine(builder.AppHostDirectory, "..", "..", "..", "dev", "postgres", "init"));

// The API connects as the application role, which cannot run DDL (M0-11).
var endpoint = postgres.Resource.PrimaryEndpoint;
var platformDatabase = builder.AddConnectionString(
    "platform",
    ReferenceExpression.Create(
        $"Host={endpoint.Property(EndpointProperty.Host)};Port={endpoint.Property(EndpointProperty.Port)};Database=platform;Username=platform_app;Password={appPassword}"));

builder.AddProject<Projects.Platform_Api>("api")
    .WithReference(platformDatabase)
    .WaitFor(postgres);

await builder.Build().RunAsync();
