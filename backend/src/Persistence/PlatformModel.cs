using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.Infrastructure.Persistence;
using Platform.Modules.Cart;
using Platform.Modules.Catalog;
using Platform.Modules.Customers;
using Platform.Modules.Inventory;
using Platform.Modules.Media;
using Platform.Modules.Ordering;
using Platform.Modules.Store;

namespace Platform.Persistence;

/// <summary>
/// The complete module set of the platform's EF Core model (implementation-plan §6.1).
/// Every host, the design-time factory, and the tests use this one list, so all processes
/// share the model that migrations are generated from.
/// </summary>
public static class PlatformModel
{
    public static IReadOnlyList<Assembly> ModuleAssemblies { get; } =
    [
        StoreModule.Assembly,
        MediaModule.Assembly,
        CatalogModule.Assembly,
        InventoryModule.Assembly,
        CustomersModule.Assembly,
        CartModule.Assembly,
        OrderingModule.Assembly,
    ];

    public static PlatformModelAssemblies Assemblies { get; } = new(ModuleAssemblies);

    /// <summary>Configures Npgsql with the migrations stored in this assembly.</summary>
    public static DbContextOptionsBuilder UsePlatformNpgsql(this DbContextOptionsBuilder options, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(PlatformModel).Assembly.GetName().Name));
    }

    /// <summary>Registers <see cref="PlatformDbContext"/> (scoped) with the complete module set.</summary>
    public static IServiceCollection AddPlatformPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(Assemblies);
        services.AddDbContext<PlatformDbContext>(options => options.UsePlatformNpgsql(connectionString));
        return services;
    }

    /// <summary>Creates a context outside dependency injection (design-time tooling, tests).</summary>
    public static PlatformDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>();
        options.UsePlatformNpgsql(connectionString);
        return new PlatformDbContext(options.Options, Assemblies);
    }
}
