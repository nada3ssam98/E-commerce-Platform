namespace Platform.SharedKernel;

/// <summary>
/// The registry of Problem Details <c>code</c> values (cross-cutting conventions: RFC 9457 with a <c>code</c>
/// extension; implementation-plan D-8). Business codes are added here by the feature that introduces them.
/// </summary>
public static class ErrorCodes
{
    /// <summary>The request did not arrive over the trusted path, or its caller may not use the route (ADR-04).</summary>
    public const string Forbidden = "FORBIDDEN";

    public const string NotFound = "NOT_FOUND";
    public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";
    public const string InternalError = "INTERNAL_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";

    /// <summary>Used when a framework-generated error has no more specific code.</summary>
    public const string HttpError = "HTTP_ERROR";

    /// <summary>Every registered code, in declaration order; listed in the OpenAPI document (D-8).</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        .. typeof(ErrorCodes).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!),
    ];

    /// <summary>The code for an error response that no feature assigned a code to.</summary>
    public static string ForStatus(int statusCode) => statusCode switch
    {
        403 => Forbidden,
        404 => NotFound,
        405 => MethodNotAllowed,
        500 => InternalError,
        503 => ServiceUnavailable,
        _ => HttpError,
    };
}
