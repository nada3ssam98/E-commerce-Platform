using Platform.Persistence;

var builder = WebApplication.CreateBuilder(args);

// The API connects as the application role (no DDL); migrations are applied separately with the
// migration role — never at application startup (ADR-02).
var connectionString = builder.Configuration.GetConnectionString("platform")
    ?? throw new InvalidOperationException("Missing connection string 'ConnectionStrings:platform'.");

builder.Services.AddPlatformPersistence(connectionString);

var app = builder.Build();

await app.RunAsync();
