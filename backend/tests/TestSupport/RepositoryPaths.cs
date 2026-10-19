namespace Platform.TestSupport;

/// <summary>Locates files in the repository from a test's output directory.</summary>
public static class RepositoryPaths
{
    private static readonly Lazy<string> RootPath = new(FindRoot);

    /// <summary>The repository root (the directory containing <c>global.json</c>).</summary>
    public static string Root => RootPath.Value;

    public static string BackendSource => Path.Combine(Root, "backend", "src");

    public static string PostgresBootstrapScript => Path.Combine(Root, "dev", "postgres", "init", "01-bootstrap.sh");

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "global.json")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"No global.json found above {AppContext.BaseDirectory}.");
    }
}
