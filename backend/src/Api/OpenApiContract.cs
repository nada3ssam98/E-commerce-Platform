using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using Platform.Api.TrustedPath;
using Platform.Persistence;

namespace Platform.Api;

/// <summary>
/// The OpenAPI document written to <c>contracts/openapi.json</c> at build time (ADR-03; M0-13). It is not served at
/// runtime. The output must be identical on every machine, so nothing environment-specific (servers) is included.
/// </summary>
public static class OpenApiContract
{
    /// <summary>
    /// Build-time generation starts the host in the <c>GetDocument.Insider</c> tool, without a database or secrets.
    /// Startup validation still runs (M0-9), so it gets placeholders without credentials; nothing connects.
    /// </summary>
    public static void AddBuildTimePlaceholders(this ConfigurationManager configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider")
        {
            return;
        }

        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [PlatformDatabaseOptions.SettingName] = "Host=localhost;Database=platform;Username=platform_app",
            [$"{TrustedPathOptions.SectionName}:{nameof(TrustedPathOptions.ApiEdgeKey)}"] = new string('e', TrustedPathOptions.MinimumKeyLength),
            [$"{TrustedPathOptions.SectionName}:{nameof(TrustedPathOptions.SsrApiKey)}"] = new string('s', TrustedPathOptions.MinimumKeyLength),
            [$"{TrustedPathOptions.SectionName}:{nameof(TrustedPathOptions.EdgeNetworks)}:0"] = "127.0.0.1/32",
            [$"{TrustedPathOptions.SectionName}:{nameof(TrustedPathOptions.SsrNetworks)}:0"] = "127.0.0.1/32",
        });
    }

    public static IServiceCollection AddPlatformOpenApi(this IServiceCollection services)
    {
        // Enums are part of the contract as names, not numbers (in responses and in the generated schema).
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<HealthStatus>()));

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo { Title = "Platform API", Version = "v1" };
                document.Servers = [];
                document.Components ??= new OpenApiComponents();
                document.Components.Headers ??= new Dictionary<string, IOpenApiHeader>();
                document.Components.Headers[ApiPipeline.RequestIdHeader] = new OpenApiHeader
                {
                    Description = "Identifier of the request, also used in logs and error responses.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                };
                return Task.CompletedTask;
            });

            // String enums (JsonStringEnumConverter) are typed as strings, so generated clients get string unions.
            options.AddSchemaTransformer((schema, context, _) =>
            {
                if (context.JsonTypeInfo.Type.IsEnum && schema.Enum is { Count: > 0 })
                {
                    schema.Type = JsonSchemaType.String;
                }

                return Task.CompletedTask;
            });

            // Every response carries the request id (ApiPipeline).
            options.AddOperationTransformer((operation, _, _) =>
            {
                if (operation.Responses is null)
                {
                    return Task.CompletedTask;
                }

                foreach (var response in operation.Responses.Values)
                {
                    if (response is OpenApiResponse concrete)
                    {
                        concrete.Headers ??= new Dictionary<string, IOpenApiHeader>();
                        concrete.Headers[ApiPipeline.RequestIdHeader] = new OpenApiHeaderReference(ApiPipeline.RequestIdHeader);
                    }
                }

                return Task.CompletedTask;
            });
        });
        return services;
    }
}
