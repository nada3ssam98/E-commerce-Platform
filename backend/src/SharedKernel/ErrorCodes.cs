namespace Platform.SharedKernel;

/// <summary>
/// The registry of Problem Details <c>code</c> values (cross-cutting conventions: RFC 9457 with a <c>code</c>
/// extension; implementation-plan D-8). Business codes are added here by the feature that introduces them.
/// </summary>
public static class ErrorCodes
{
    public const string NotFound = "NOT_FOUND";
    public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";
    public const string InternalError = "INTERNAL_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";

    /// <summary>Used when a framework-generated error has no more specific code.</summary>
    public const string HttpError = "HTTP_ERROR";

    /// <summary>The code for an error response that no feature assigned a code to.</summary>
    public static string ForStatus(int statusCode) => statusCode switch
    {
        404 => NotFound,
        405 => MethodNotAllowed,
        500 => InternalError,
        503 => ServiceUnavailable,
        _ => HttpError,
    };
}
