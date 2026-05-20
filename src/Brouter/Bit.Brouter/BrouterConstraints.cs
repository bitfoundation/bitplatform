using System.Collections.Concurrent;
using System.Globalization;

namespace Bit.Brouter;

/// <summary>
/// Registry of route parameter constraints. Built-in constraints are always registered;
/// custom constraints can be added via <see cref="Register"/>.
/// </summary>
public static class BrouterConstraints
{
    private static readonly ConcurrentDictionary<string, Func<RouteConstraint>> _factories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["int"] = () => new TypeRouteConstraint<int>((string s, out int r) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out r)),
        ["bool"] = () => new TypeRouteConstraint<bool>(bool.TryParse),
        ["guid"] = () => new TypeRouteConstraint<Guid>(Guid.TryParse),
        ["long"] = () => new TypeRouteConstraint<long>((string s, out long r) => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out r)),
        ["float"] = () => new TypeRouteConstraint<float>((string s, out float r) => float.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out r)),
        ["double"] = () => new TypeRouteConstraint<double>((string s, out double r) => double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out r)),
        ["decimal"] = () => new TypeRouteConstraint<decimal>((string s, out decimal r) => decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out r)),
        ["datetime"] = () => new TypeRouteConstraint<DateTime>((string s, out DateTime r) => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out r)),
    };

    /// <summary>
    /// Registers a custom constraint. Templates can then use <c>{name:yourConstraintName}</c>.
    /// Throws if <paramref name="name"/> is already registered. Thread-safe.
    /// </summary>
    public static void Register(string name, Func<RouteConstraint> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(factory);

        if (_factories.TryAdd(name, factory) is false)
            throw new InvalidOperationException($"A constraint named '{name}' is already registered.");

        RouteConstraint.InvalidateCache(name);
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

        var removed = _factories.TryRemove(name, out _);
        if (removed)
        {
            RouteConstraint.InvalidateCache(name);
        }
        return removed;
    }

    internal static RouteConstraint? Create(string name) =>
        _factories.TryGetValue(name, out var factory) ? factory() : null;
}
