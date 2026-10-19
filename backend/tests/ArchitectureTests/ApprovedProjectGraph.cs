using System.Xml.Linq;

namespace Platform.ArchitectureTests;

/// <summary>
/// The approved project-reference graph: system-behavior §1 (domains) and implementation-plan §5.2
/// (technical layers). A project may reference any project <b>reachable</b> from it through these edges —
/// a domain may use every domain below it — and nothing else. Adding a project requires adding it here.
/// </summary>
public static class ApprovedProjectGraph
{
    private const string Store = "Platform.Modules.Store";
    private const string Media = "Platform.Modules.Media";
    private const string Catalog = "Platform.Modules.Catalog";
    private const string Inventory = "Platform.Modules.Inventory";
    private const string Customers = "Platform.Modules.Customers";
    private const string Cart = "Platform.Modules.Cart";
    private const string Ordering = "Platform.Modules.Ordering";

    public static IReadOnlyList<string> Modules { get; } = [Store, Media, Catalog, Inventory, Customers, Cart, Ordering];

    public static IReadOnlyDictionary<string, string[]> Edges { get; } = new Dictionary<string, string[]>
    {
        ["Platform.SharedKernel"] = [],
        ["Platform.Infrastructure"] = ["Platform.SharedKernel"],
        ["Platform.ServiceDefaults"] = ["Platform.SharedKernel"],
        [Store] = ["Platform.Infrastructure"],
        [Media] = [Store],
        [Catalog] = [Store, Media],
        [Inventory] = [Catalog],
        [Customers] = [Store],
        [Cart] = [Catalog, Inventory, Customers],
        [Ordering] = [Cart, Catalog, Inventory, Customers, Media],
        ["Platform.ReadModels"] = [.. Modules],
        ["Platform.Persistence"] = ["Platform.Infrastructure", .. Modules],
        ["Platform.Api"] = ["Platform.ServiceDefaults", "Platform.ReadModels", "Platform.Persistence"],
        ["Platform.Cli"] = ["Platform.ReadModels", "Platform.Persistence"],
        ["Platform.AppHost"] = ["Platform.Api"],
    };

    /// <summary>Every project reachable from <paramref name="project"/> (excluding itself).</summary>
    public static IReadOnlySet<string> Reachable(string project)
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>(Edges[project]);
        while (pending.TryPop(out var next))
        {
            if (reachable.Add(next))
            {
                foreach (var dependency in Edges[next])
                {
                    pending.Push(dependency);
                }
            }
        }

        return reachable;
    }

    /// <summary>Returns one message per project-reference violation found in the given project files.</summary>
    public static IReadOnlyList<string> FindViolations(IEnumerable<string> projectFiles)
    {
        var violations = new List<string>();
        foreach (var file in projectFiles)
        {
            var project = ProjectName(file);
            if (!Edges.ContainsKey(project))
            {
                violations.Add($"{project}: not in the approved project graph");
                continue;
            }

            var allowed = Reachable(project);
            foreach (var reference in ProjectReferences(file))
            {
                if (!allowed.Contains(reference))
                {
                    violations.Add($"{project} -> {reference}");
                }
            }
        }

        return violations;
    }

    private static IEnumerable<string> ProjectReferences(string file) =>
        XDocument.Load(file)
            .Descendants("ProjectReference")
            .Select(element => (string?)element.Attribute("Include"))
            .OfType<string>()
            .Select(ProjectName);

    /// <summary><c>…\Platform.Modules.Store.csproj</c> (or a <c>.csproj.fixture</c>) → <c>Platform.Modules.Store</c>.</summary>
    private static string ProjectName(string path)
    {
        var name = Path.GetFileName(path.Replace('\\', '/'));
        foreach (var suffix in new[] { ".fixture", ".csproj" })
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal))
            {
                name = name[..^suffix.Length];
            }
        }

        return name;
    }
}
