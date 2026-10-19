using System.Reflection;

namespace Platform.Modules.Customers;

/// <summary>Identifies the Customers module's assembly (e.g., for EF Core model composition in Persistence).</summary>
public static class CustomersModule
{
    public static Assembly Assembly => typeof(CustomersModule).Assembly;
}
