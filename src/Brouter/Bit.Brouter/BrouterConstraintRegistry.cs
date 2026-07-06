using System.Collections.Concurrent;
using System.Globalization;

namespace Bit.Brouter;

/// <summary>
/// A per-container set of route parameter constraints. One instance lives on
/// <see cref="BrouterOptions.Constraints"/>, so its scope is the DI container that owns the options -
/// custom constraints registered here are visible only to the app/service provider that registered
/// them, which isolates separate apps in one process (and parallel test classes) from one another.
/// </summary>
/// <remarks>
/// <para>
/// Built-in constraints (<c>int</c>, <c>bool</c>, <c>guid</c>, <c>long</c>, <c>float</c>,
/// <c>double</c>, <c>decimal</c>, <c>datetime</c>) are always available: they are stateless singletons
/// shared across every registry, so they need no registration and cannot be overridden.
/// </para>
/// <para>
/// Register custom constraints once during application startup, before any route is parsed:
/// <c>builder.Services.AddBitBrouterServices(o =&gt; o.Constraints.Register("slug", new SlugConstraint()))</c>.
/// Each registered instance is cached and reused across all route matches (and across threads);
/// implementations must be stateless and thread-safe.
/// </para>
/// <para>
/// <b>Multi-tenancy note.</b> The scope is the DI container, not the tenant. This isolates separate
/// apps/service providers (and test classes) from one another; it gives per-tenant isolation only if
/// each tenant already owns a distinct container.
/// </para>
/// </remarks>
public sealed class BrouterConstraintRegistry
{
    // Built-in constraints are stateless singletons, safe to share across every registry and thread,
    // so they live in one shared immutable table rather than being copied into each instance.
    private static readonly IReadOnlyDictionary<string, BrouterRouteConstraint> _builtIns = new Dictionary<string, BrouterRouteConstraint>(StringComparer.OrdinalIgnoreCase)
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

    private readonly ConcurrentDictionary<string, BrouterRouteConstraint> _custom = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a custom constraint for this container. Templates can then use
    /// <c>{name:yourConstraintName}</c>. Throws if <paramref name="name"/> is a built-in constraint
    /// or is already registered on this registry. Thread-safe.
    /// </summary>
    /// <remarks>
    /// The provided <paramref name="constraint"/> is cached and shared across every route match.
    /// Implementations must be stateless and safe for concurrent use.
    /// </remarks>
    public void Register(string name, BrouterRouteConstraint constraint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(constraint);

        if (_builtIns.ContainsKey(name))
            throw new InvalidOperationException($"'{name}' is a built-in constraint and cannot be overridden.");

        if (_custom.TryAdd(name, constraint) is false)
            throw new InvalidOperationException($"A constraint named '{name}' is already registered.");
    }

    /// <summary>
    /// Removes a previously registered custom constraint from this container. Built-ins cannot be
    /// removed. Returns <c>true</c> if a custom constraint was removed. Thread-safe.
    /// </summary>
    public bool Unregister(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _custom.TryRemove(name, out _);
    }

    /// <summary>
    /// Resolves a constraint by name: this container's custom constraints first, then the shared
    /// built-ins. Returns null when the name is unknown.
    /// </summary>
    internal BrouterRouteConstraint? Create(string name) =>
        _custom.TryGetValue(name, out var constraint) ? constraint
        : _builtIns.TryGetValue(name, out var builtIn) ? builtIn
        : null;

    /// <summary>
    /// Resolves a built-in constraint by name, used when no per-container registry is threaded through
    /// parsing (e.g. a direct <see cref="BrouterTemplateParser.ParseTemplate(string, BrouterConstraintRegistry?)"/>
    /// call). Returns null when the name is not a built-in.
    /// </summary>
    internal static BrouterRouteConstraint? CreateBuiltIn(string name) =>
        _builtIns.TryGetValue(name, out var builtIn) ? builtIn : null;
}
