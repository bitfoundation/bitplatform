namespace Bit.Brouter;

/// <summary>
/// Opt-in alternative to Microsoft's <c>[SupplyParameterFromQuery]</c> for binding a component
/// property to a query string value. Query values bind via <c>[Parameter, SupplyParameterFromQuery]</c>
/// by default; reach for this attribute when the property's type falls outside the set the framework's
/// own query supplier can parse (e.g. enums): the framework supplier only reacts to its own attribute,
/// so a <c>[BrouterQuery]</c> property is invisible to it and Brouter performs the conversion instead
/// (any <see cref="Convert.ChangeType(object, Type)"/>-compatible scalar, <see cref="Guid"/>, enums,
/// and their nullables). Multi-value query parameters are exposed as <c>string[]</c> when the property
/// is array-typed.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class BrouterQueryAttribute : Attribute
{
    /// <summary>Optional override for the query key. Defaults to the property name (case-insensitive).</summary>
    public string? Name { get; set; }
}
