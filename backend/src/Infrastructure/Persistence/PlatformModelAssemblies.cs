using System.Reflection;

namespace Platform.Infrastructure.Persistence;

/// <summary>The module assemblies whose entity configurations form the <see cref="PlatformDbContext"/> model.</summary>
public sealed class PlatformModelAssemblies(IReadOnlyList<Assembly> assemblies)
{
    public IReadOnlyList<Assembly> Assemblies { get; } = assemblies;
}
