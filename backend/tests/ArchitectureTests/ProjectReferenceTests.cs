using Platform.TestSupport;
using Shouldly;

namespace Platform.ArchitectureTests;

/// <summary>M0-12: a project reference that violates the dependency graph fails this test.</summary>
public sealed class ProjectReferenceTests
{
    private static readonly string FixtureDirectory =
        Path.Combine(RepositoryPaths.Root, "backend", "tests", "ArchitectureTests", "Fixtures");

    [Fact]
    public void SourceProjectsReferenceOnlyWhatTheApprovedGraphAllows()
    {
        var projectFiles = Directory.GetFiles(RepositoryPaths.BackendSource, "*.csproj", SearchOption.AllDirectories);

        ApprovedProjectGraph.FindViolations(projectFiles).ShouldBeEmpty();
    }

    [Fact]
    public void EveryProjectInTheApprovedGraphExists()
    {
        var projects = Directory
            .GetFiles(RepositoryPaths.BackendSource, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Order(StringComparer.Ordinal);

        ApprovedProjectGraph.Edges.Keys.Order(StringComparer.Ordinal).ShouldBe(projects);
    }

    /// <summary>Deliberate violation fixtures: one creates a cycle, one does not (so the compiler would not catch it).</summary>
    [Theory]
    [InlineData("CatalogReferencesOrdering/Platform.Modules.Catalog.csproj.fixture", "Platform.Modules.Catalog -> Platform.Modules.Ordering")]
    [InlineData("CustomersReferencesMedia/Platform.Modules.Customers.csproj.fixture", "Platform.Modules.Customers -> Platform.Modules.Media")]
    public void ForbiddenReferenceIsReported(string fixture, string expectedViolation)
    {
        var violations = ApprovedProjectGraph.FindViolations([Path.Combine(FixtureDirectory, fixture)]);

        violations.ShouldBe([expectedViolation]);
    }

    [Fact]
    public void UnknownProjectIsReported()
    {
        var violations = ApprovedProjectGraph.FindViolations(
            [Path.Combine(FixtureDirectory, "UnknownProject", "Platform.Unknown.csproj.fixture")]);

        violations.ShouldBe(["Platform.Unknown: not in the approved project graph"]);
    }
}
