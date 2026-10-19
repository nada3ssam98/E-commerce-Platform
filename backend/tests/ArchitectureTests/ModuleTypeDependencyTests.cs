using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Platform.Infrastructure.Persistence;
using Platform.Persistence;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Assembly = System.Reflection.Assembly;

namespace Platform.ArchitectureTests;

/// <summary>
/// Type-level counterpart of <see cref="ProjectReferenceTests"/> (ArchUnitNET, ADR-11): code in a module
/// must not use types of modules it may not depend on. Becomes meaningful as modules gain code.
/// </summary>
public sealed class ModuleTypeDependencyTests
{
    private static readonly Dictionary<string, Assembly> ModuleAssemblies =
        PlatformModel.ModuleAssemblies.ToDictionary(assembly => assembly.GetName().Name!, StringComparer.Ordinal);

    private static readonly Assembly InfrastructureAssembly = typeof(PlatformDbContext).Assembly;

    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies([InfrastructureAssembly, .. ModuleAssemblies.Values])
        .Build();

    public static TheoryData<string> Modules => [.. ApprovedProjectGraph.Modules];

    [Theory]
    [MemberData(nameof(Modules))]
    public void ModuleTypesDoNotDependOnForbiddenModules(string module)
    {
        var allowed = ApprovedProjectGraph.Reachable(module);
        var forbidden = ApprovedProjectGraph.Modules
            .Where(other => other != module && !allowed.Contains(other))
            .Select(other => ModuleAssemblies[other])
            .ToArray();
        if (forbidden.Length == 0)
        {
            return; // the top module (Ordering) may use every other module
        }

        Types().That().ResideInAssembly(ModuleAssemblies[module])
            .Should().NotDependOnAny(Types().That().ResideInAssembly(forbidden[0], forbidden[1..]))
            .Check(Architecture);
    }

    [Fact]
    public void InfrastructureTypesDoNotDependOnModules()
    {
        var modules = ModuleAssemblies.Values.ToArray();

        Types().That().ResideInAssembly(InfrastructureAssembly)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(modules[0], modules[1..]))
            .Check(Architecture);
    }
}
