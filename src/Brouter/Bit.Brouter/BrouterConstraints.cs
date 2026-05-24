using System.Collections.Concurrent;
using System.Globalization;

namespace Bit.Brouter;

/// <summary>
/// Registry of route parameter constraints. Built-in constraints are always registered;
/// custom constraints can be added via <see cref="Register"/>.
/// </summary>
/// <remarks>
/// Each registered <see cref="RouteConstraint"/> instance is cached and reused across all
/// route matches (and across threads). Implementations must therefore be stateless and
/// thread-safe.
/// </remarks>
public static class BrouterConstraints
{
    private static readonly ConcurrentDictionary<string, RouteConstraint> _constraints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["int"] = new TypeRouteConstraint<int>((string s, out int r) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out r)),
        ["bool"] = new TypeRouteConstraint<bool>(bool.TryParse),
        ["guid"] = new TypeRouteConstraint<Guid>(Guid.TryParse),
        ["long"] = new TypeRouteConstraint<long>((string s, out long r) => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out r)),
        ["float"] = new TypeRouteConstraint<float>((string s, out float r) => float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out r)),
        ["double"] = new TypeRouteConstraint<double>((string s, out double r) => double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out r)),
        ["decimal"] = new TypeRouteConstraint<decimal>((string s, out decimal r) => decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out r)),
        ["datetime"] = new TypeRouteConstraint<DateTime>((string s, out DateTime r) => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out r)),
    };

    /// <summary>
    /// Registers a custom constraint. Templates can then use <c>{name:yourConstraintName}</c>.
    /// Throws if <paramref name="name"/> is already registered. Thread-safe.
    /// </summary>
    /// <remarks>
    /// The provided <paramref name="constraint"/> is cached and shared across every route match.
    /// Implementations must be stateless and safe for concurrent use.
    /// </remarks>
    public static void Register(string name, RouteConstraint constraint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(constraint);

        if (_constraints.TryAdd(name, constraint) is false)
            throw new InvalidOperationException($"A constraint named '{name}' is already registered.");
    }

    private static readonly HashSet<string> _builtIns = new(StringComparer.OrdinalIgnoreCase)
    {
        "int", "bool", "guid", "long", "float", "double", "decimal", "datetime"
    };

    /// <summary>Removes a previously registered constraint. Built-ins cannot be removed. Thread-safe.</summary>
    public static bool Unregister(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_builtIns.Contains(name)) return false;

        var removed = _constraints.TryRemove(name, out _);
        return removed;
    }

    internal static RouteConstraint? Create(string name) =>
        _constraints.TryGetValue(name, out var constraint) ? constraint : null;
}
