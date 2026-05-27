using System.Collections.Concurrent;
using System.Globalization;

namespace Bit.Brouter;

/// <summary>
/// Registry of route parameter constraints. Built-in constraints are always registered;
/// custom constraints can be added via <see cref="Register"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each registered <see cref="BrouterRouteConstraint"/> instance is cached and reused across all
/// route matches (and across threads). Implementations must therefore be stateless and
/// thread-safe.
/// </para>
/// <para>
/// <b>Process scope.</b> The registry is a process-wide static. On Blazor Server every circuit
/// observes the same set, and on parallel test runs (e.g. <c>dotnet test</c> with multi-target
/// frameworks or per-class parallelism) registrations can race. Register custom constraints
/// once during application startup, before any route is parsed; in tests, prefer
/// <see cref="Unregister"/> in <c>[TestCleanup]</c> to keep test classes independent.
/// </para>
/// </remarks>
public static class BrouterConstraints
{
    private static readonly ConcurrentDictionary<string, BrouterRouteConstraint> _constraints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["int"] = new BrouterTypeRouteConstraint<int>((string s, out int r) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out r)),
        ["bool"] = new BrouterTypeRouteConstraint<bool>(bool.TryParse),
        ["guid"] = new BrouterTypeRouteConstraint<Guid>(Guid.TryParse),
        ["long"] = new BrouterTypeRouteConstraint<long>((string s, out long r) => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out r)),
        ["float"] = new BrouterTypeRouteConstraint<float>((string s, out float r) => float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out r)),
        ["double"] = new BrouterTypeRouteConstraint<double>((string s, out double r) => double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out r)),
        ["decimal"] = new BrouterTypeRouteConstraint<decimal>((string s, out decimal r) => decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out r)),
        ["datetime"] = new BrouterTypeRouteConstraint<DateTime>((string s, out DateTime r) => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out r)),
    };

    /// <summary>
    /// Registers a custom constraint. Templates can then use <c>{name:yourConstraintName}</c>.
    /// Throws if <paramref name="name"/> is already registered. Thread-safe.
    /// </summary>
    /// <remarks>
    /// The provided <paramref name="constraint"/> is cached and shared across every route match.
    /// Implementations must be stateless and safe for concurrent use.
    /// </remarks>
    public static void Register(string name, BrouterRouteConstraint constraint)
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

    internal static BrouterRouteConstraint? Create(string name) =>
        _constraints.TryGetValue(name, out var constraint) ? constraint : null;
}
