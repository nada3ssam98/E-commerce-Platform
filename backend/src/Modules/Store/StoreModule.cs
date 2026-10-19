using System.Reflection;

namespace Platform.Modules.Store;

/// <summary>Identifies the Store module's assembly (e.g., for EF Core model composition in Persistence).</summary>
public static class StoreModule
{
    public static Assembly Assembly => typeof(StoreModule).Assembly;
}
