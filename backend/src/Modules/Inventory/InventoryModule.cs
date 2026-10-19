using System.Reflection;

namespace Platform.Modules.Inventory;

/// <summary>Identifies the Inventory module's assembly (e.g., for EF Core model composition in Persistence).</summary>
public static class InventoryModule
{
    public static Assembly Assembly => typeof(InventoryModule).Assembly;
}
