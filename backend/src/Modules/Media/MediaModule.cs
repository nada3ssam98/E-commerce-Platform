using System.Reflection;

namespace Platform.Modules.Media;

/// <summary>Identifies the Media module's assembly (e.g., for EF Core model composition in Persistence).</summary>
public static class MediaModule
{
    public static Assembly Assembly => typeof(MediaModule).Assembly;
}
