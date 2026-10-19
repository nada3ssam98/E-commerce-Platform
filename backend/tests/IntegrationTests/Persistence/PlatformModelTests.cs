using Microsoft.EntityFrameworkCore;
using Platform.Persistence;
using Platform.TestSupport;
using Shouldly;

namespace Platform.IntegrationTests.Persistence;

/// <summary>Checks of the composed EF Core model that need no database.</summary>
public sealed class PlatformModelTests
{
    [Fact]
    public void ModelMatchesTheLatestMigration()
    {
        using var context = PlatformModel.CreateDbContext("Host=localhost;Database=platform;Username=platform_migrator");

        context.Database.HasPendingModelChanges().ShouldBeFalse("the model changed without a migration");
    }

    [Fact]
    public void ModelIncludesEveryModuleAssembly()
    {
        var moduleProjects = Directory
            .GetFiles(Path.Combine(RepositoryPaths.BackendSource, "Modules"), "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Order(StringComparer.Ordinal);

        PlatformModel.ModuleAssemblies
            .Select(assembly => assembly.GetName().Name)
            .Order(StringComparer.Ordinal)
            .ShouldBe(moduleProjects);
    }
}
