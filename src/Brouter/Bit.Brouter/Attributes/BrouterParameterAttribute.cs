namespace Bit.Brouter;

/// <summary>
/// Optional override for how a component property binds to a route parameter. Route parameters
/// bind to <c>[Parameter]</c> properties by name automatically (Blazor-style); apply this
/// attribute when the property name and the route parameter name differ (<see cref="Name"/>),
/// or to bind a property whose name doesn't appear in the route's template. The matched value
/// is converted (if needed) to the property's type before assignment.
/// Query-string values bind via Microsoft's <c>[SupplyParameterFromQuery]</c> (or Brouter's
/// opt-in <see cref="BrouterQueryAttribute"/>) instead.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class BrouterParameterAttribute : Attribute
{
    /// <summary>Optional override for the parameter name. Defaults to the property name (case-insensitive).</summary>
    public string? Name { get; set; }
}
