namespace Bit.Brouter;

/// <summary>
/// Base type for parameter constraints. Custom constraints can be registered via
/// <see cref="BrouterConstraints.Register"/>.
/// </summary>
/// <remarks>
/// A single <see cref="RouteConstraint"/> instance is registered per constraint name and
/// reused across every route match (and across threads). Implementations must be stateless
/// and thread-safe; do not store per-match data on the instance.
/// </remarks>
public abstract class RouteConstraint
{
    /// <summary>The name of this constraint as it appears in templates (e.g. <c>"int"</c>).</summary>
    public string Constraint { get; internal set; } = string.Empty;

    /// <summary>Try to match a single URL segment against this constraint.</summary>
    public abstract bool TryMatch(string pathSegment, out object? convertedValue);


    internal static RouteConstraint Parse(string template, string segment, string constraint)
    {
        if (string.IsNullOrEmpty(constraint))
            throw new ArgumentException($"Malformed segment '{segment}' in route '{template}' contains an empty constraint.");

        var registered = BrouterConstraints.Create(constraint)
            ?? throw new ArgumentException($"Unsupported constraint '{constraint}' in route '{template}'.");

        // Idempotent: same constraint name resolves to the same singleton, so this assignment
        // always writes the same value.
        registered.Constraint = constraint;
        return registered;
    }
}
