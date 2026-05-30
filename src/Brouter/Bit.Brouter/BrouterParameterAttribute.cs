namespace Bit.Brouter;

/// <summary>
/// Marks a component property as bound to a route parameter. The matched value
/// is converted (if needed) to the property's type before assignment.
/// Inspired by Microsoft's <c>SupplyParameterFromQuery</c>/<c>SupplyParameterFromForm</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class BrouterParameterAttribute : Attribute
{
    /// <summary>Optional override for the parameter name. Defaults to the property name (case-insensitive).</summary>
    public string? Name { get; set; }
}

/// <summary>
/// Marks a component property as bound to a query string parameter. Multi-value
/// query parameters are exposed as <c>string[]</c> when the property is array-typed.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class BrouterQueryAttribute : Attribute
{
    /// <summary>Optional override for the query key. Defaults to the property name.</summary>
    public string? Name { get; set; }
}
