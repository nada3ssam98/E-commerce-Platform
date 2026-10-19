using Microsoft.EntityFrameworkCore;

namespace Platform.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context type for the whole platform (implementation-plan §6.1).
/// It knows no entity types at compile time: the model is contributed by the module assemblies
/// supplied through <see cref="PlatformModelAssemblies"/>. Every host registers the same set,
/// because EF Core builds and caches the model once per context type.
/// </summary>
public sealed class PlatformDbContext(
    DbContextOptions<PlatformDbContext> options,
    PlatformModelAssemblies modelAssemblies) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var assembly in modelAssemblies.Assemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }
}
