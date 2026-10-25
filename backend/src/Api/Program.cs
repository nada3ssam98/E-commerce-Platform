using Platform.Api;
using Platform.Api.TrustedPath;
using Platform.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddBuildTimePlaceholders();

// The API connects as the application role (no DDL); migrations are applied separately with the
// migration role — never at application startup (ADR-02).
builder.Services.AddPlatformPersistence(builder.Configuration.GetConnectionString(PlatformDatabaseOptions.ConnectionStringName));
builder.Services.AddPlatformApi();
builder.Services.AddPlatformOpenApi();
builder.Services.AddPlatformTrustedPath(builder.Configuration);

var app = builder.Build();

app.UsePlatformApi();
app.MapHealthEndpoints();
app.MapTestDiagnostics();

await app.RunAsync();

/// <summary>Entry point; public for integration tests (<c>WebApplicationFactory</c>).</summary>
public partial class Program;
