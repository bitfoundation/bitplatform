namespace Bit.Brouter;

/// <summary>
/// Base type for parameter constraints. Custom constraints can be registered via
/// <see cref="BrouterConstraints.Register"/>.
/// </summary>
/// <remarks>
/// A single <see cref="BrouterRouteConstraint"/> instance is registered per constraint name and
/// reused across every route match (and across threads). Implementations must be stateless
/// and thread-safe; do not store per-match data on the instance.
/// </remarks>
public abstract class BrouterRouteConstraint
{
    /// <summary>Try to match a single URL segment against this constraint.</summary>
    public abstract bool TryMatch(string pathSegment, out object? convertedValue);


    internal static BrouterRouteConstraint Resolve(string template, string segment, string constraint)
    {
        if (string.IsNullOrEmpty(constraint))
            throw new ArgumentException($"Malformed segment '{segment}' in route '{template}' contains an empty constraint.");

        return BrouterConstraints.Create(constraint)
            ?? throw new ArgumentException($"Unsupported constraint '{constraint}' in route '{template}'.");
    }
}

/// <summary>
/// Pairs a constraint token (the name as it appears in the template, e.g. <c>"int"</c>)
/// with the resolved <see cref="BrouterRouteConstraint"/> instance.
/// </summary>
/// <remarks>
/// The token is stored alongside the constraint rather than on the constraint itself so the
/// shared singleton instances stay stateless and safe for concurrent use.
/// </remarks>
internal readonly record struct BrouterRouteConstraintBinding(string Name, BrouterRouteConstraint Constraint);
