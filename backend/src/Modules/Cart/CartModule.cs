using System.Reflection;

namespace Platform.Modules.Cart;

/// <summary>Identifies the Cart module's assembly (e.g., for EF Core model composition in Persistence).</summary>
public static class CartModule
{
    public static Assembly Assembly => typeof(CartModule).Assembly;
}
