using System.Reflection;

namespace Platform.Modules.Ordering;

/// <summary>Identifies the Ordering module's assembly (e.g., for EF Core model composition in Persistence).</summary>
public static class OrderingModule
{
    public static Assembly Assembly => typeof(OrderingModule).Assembly;
}
