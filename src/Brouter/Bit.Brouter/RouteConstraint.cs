namespace Bit.Brouter;

/// <summary>
/// Base type for parameter constraints. Custom constraints can be registered via
/// <see cref="BrouterConstraints.Register"/>.
/// </summary>
/// <remarks>
/// A single <see cref="RouteConstraint"/> instance is created per constraint name and reused
/// across every route match (and across threads). Implementations must be stateless and
/// thread-safe; do not store per-match data on the instance.
/// </remarks>
public abstract class RouteConstraint
{
    private static readonly Dictionary<string, RouteConstraint> _cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object _lock = new();

    /// <summary>The name of this constraint as it appears in templates (e.g. <c>"int"</c>).</summary>
    public string Constraint { get; private set; } = string.Empty;

    /// <summary>Try to match a single URL segment against this constraint.</summary>
    public abstract bool TryMatch(string pathSegment, out object? convertedValue);


    internal static RouteConstraint Parse(string template, string segment, string constraint)
    {
        if (string.IsNullOrEmpty(constraint))
            throw new ArgumentException($"Malformed segment '{segment}' in route '{template}' contains an empty constraint.");

        lock (_lock)
        {
            if (_cache.TryGetValue(constraint, out var cached))
                return cached;

            var fresh = BrouterConstraints.Create(constraint)
                ?? throw new ArgumentException($"Unsupported constraint '{constraint}' in route '{template}'.");

            fresh.Constraint = constraint;
            _cache[constraint] = fresh;
            return fresh;
        }
    }

    internal static void InvalidateCache(string constraint)
    {
        lock (_lock)
        {
            _cache.Remove(constraint);
        }
    }
}
