var builder = DistributedApplication.CreateBuilder(args);

// Role passwords: generated on first run and persisted in this project's user secrets (never committed).
var migratorPassword = builder.AddParameter(
    "platform-migrator-password", new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);
var appPassword = builder.AddParameter(
    "platform-app-password", new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);

// The three trusted-path credentials (ADR-04 §4b), generated and persisted the same way. Each process receives only
// its own: the edge holds the API edge key and the SSR ingress key, the SSR server the SSR ingress key and the
// SSR-to-API key, the API the API edge key and the SSR-to-API key.
var apiEdgeKey = builder.AddParameter(
    "platform-api-edge-key", new GenerateParameterDefault { MinLength = 48, Special = false }, secret: true, persist: true);
var ssrIngressKey = builder.AddParameter(
    "platform-ssr-ingress-key", new GenerateParameterDefault { MinLength = 48, Special = false }, secret: true, persist: true);
var ssrApiKey = builder.AddParameter(
    "platform-ssr-api-key", new GenerateParameterDefault { MinLength = 48, Special = false }, secret: true, persist: true);

// PostgreSQL 17 (ADR-02). The bootstrap script creates the platform database and its migration and
// application roles on first start (dev/postgres/init/01-bootstrap.sh).
var repository = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", ".."));
var postgres = builder.AddPostgres("postgres")
    .WithImageTag("17.11")
    .WithEnvironment("PLATFORM_MIGRATOR_PASSWORD", migratorPassword)
    .WithEnvironment("PLATFORM_APP_PASSWORD", appPassword)
    .WithInitFiles(Path.Combine(repository, "dev", "postgres", "init"));

// The API connects as the application role, which cannot run DDL (M0-11).
var endpoint = postgres.Resource.PrimaryEndpoint;
var platformDatabase = builder.AddConnectionString(
    "platform",
    ReferenceExpression.Create(
        $"Host={endpoint.Property(EndpointProperty.Host)};Port={endpoint.Property(EndpointProperty.Port)};Database=platform;Username=platform_app;Password={appPassword}"));

// Locally every process runs on this machine and the edge in a container, so the edge and SSR networks overlap and
// source binding cannot be shown (ADR-04 "Local development"); integration tests cover it (TH-12).
string[] loopback = ["127.0.0.0/8", "::1/128"];
string[] edgeNetworks = [.. loopback, "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"];

var api = builder.AddProject<Projects.Platform_Api>("api")
    .WithReference(platformDatabase)
    .WaitFor(postgres)
    .WithEnvironment("TrustedPath__ApiEdgeKey", apiEdgeKey)
    .WithEnvironment("TrustedPath__SsrApiKey", ssrApiKey);
for (var i = 0; i < edgeNetworks.Length; i++)
{
    api.WithEnvironment($"TrustedPath__EdgeNetworks__{i}", edgeNetworks[i]);
}

for (var i = 0; i < loopback.Length; i++)
{
    api.WithEnvironment($"TrustedPath__SsrNetworks__{i}", loopback[i]);
}

// Angular dev servers (run `npm ci` in frontend/ first). Fixed ports: the npm scripts pass them to `ng serve`.
var frontend = Path.Combine(repository, "frontend");
var storefront = builder.AddJavaScriptApp("storefront", frontend, "dev:storefront")
    .WithNpm(install: false)
    .WithHttpEndpoint(port: 4200, isProxied: false)
    .WithEnvironment("PLATFORM_SSR_INGRESS_KEY", ssrIngressKey)
    .WithEnvironment("PLATFORM_SSR_API_KEY", ssrApiKey)
    .WithEnvironment("INTERNAL_API_BASE_URL", api.GetEndpoint("http"))
    .WaitFor(api);

var admin = builder.AddJavaScriptApp("admin", frontend, "dev:admin")
    .WithNpm(install: false)
    .WithHttpEndpoint(port: 4300, isProxied: false);

// Local edge (ADR-04 §4g): https://brand-a.localhost, https://brand-b.localhost, https://admin.localhost.
// Its local CA persists in the volume; trusting it is a one-time step (README).
builder.AddContainer("edge", "caddy", "2.11.7")
    .WithBindMount(Path.Combine(repository, "dev", "caddy"), "/etc/caddy", isReadOnly: true)
    .WithVolume("platform-edge-data", "/data")
    .WithEndpoint(port: 443, targetPort: 443, scheme: "https", name: "https", isProxied: false)
    .WithEnvironment("PLATFORM_API_EDGE_KEY", apiEdgeKey)
    .WithEnvironment("PLATFORM_SSR_INGRESS_KEY", ssrIngressKey)
    .WithEnvironment("API_UPSTREAM", api.GetEndpoint("http"))
    .WithEnvironment("SSR_UPSTREAM", storefront.GetEndpoint("http"))
    .WithEnvironment("ADMIN_UPSTREAM", admin.GetEndpoint("http"))
    .WaitFor(storefront)
    .WaitFor(admin);

await builder.Build().RunAsync();
