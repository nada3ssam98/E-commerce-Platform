using System.Reflection;

namespace Platform.Modules.Catalog;

/// <summary>Identifies the Catalog module's assembly (e.g., for EF Core model composition in Persistence).</summary>
public static class CatalogModule
{
    public static Assembly Assembly => typeof(CatalogModule).Assembly;
}
