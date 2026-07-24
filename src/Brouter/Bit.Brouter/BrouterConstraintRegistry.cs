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
/// Built-in constraints are always available: they are stateless singletons shared across every
/// registry, so they need no registration and cannot be overridden. Type constraints (<c>int</c>,
/// <c>bool</c>, <c>guid</c>, <c>long</c>, <c>float</c>, <c>double</c>, <c>decimal</c>,
/// <c>datetime</c>) convert the bound value; validation constraints (<c>alpha</c>, <c>file</c>,
/// <c>nonfile</c>, and the parameterized <c>min(1)</c>, <c>max(10)</c>, <c>range(1,10)</c>,
/// <c>minlength(2)</c>, <c>maxlength(8)</c>, <c>length(4)</c> / <c>length(2,8)</c>,
/// <c>regex(...)</c>) only accept/reject and leave the value as a string - mirroring the
/// built-in Blazor router.
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
        ["alpha"] = new BrouterPredicateRouteConstraint(static s =>
        {
            foreach (var c in s)
            {
                if (char.IsAsciiLetter(c) is false) return false;
            }
            return true;
        }),
        ["file"] = new BrouterPredicateRouteConstraint(static s => BrouterPredicateRouteConstraint.IsFileName(s)),
        // nonfile accepts a missing value (empty catch-all remainder): its core use-case is
        // excluding static-asset requests, and "no segment at all" is certainly not a file.
        ["nonfile"] = new BrouterPredicateRouteConstraint(static s => BrouterPredicateRouteConstraint.IsFileName(s) is false, matchesMissingValue: true),
    };

    // Names of the parameterized built-ins, resolved as "name(args)" tokens. Kept separate from the
    // singleton table above because each distinct argument list needs its own instance.
    private static readonly HashSet<string> _parameterizedBuiltInNames = new(["min", "max", "range", "minlength", "maxlength", "length", "regex"], StringComparer.OrdinalIgnoreCase);

    // Cache one instance per exact token ("min(1)", "regex(^\d+$)") so a constraint - notably a
    // regex - is built once per process no matter how many routes or re-registrations use it.
    private static readonly ConcurrentDictionary<string, BrouterRouteConstraint> _parameterizedCache = new(StringComparer.Ordinal);

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

        if (_builtIns.ContainsKey(name) || _parameterizedBuiltInNames.Contains(name))
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
        _custom.TryGetValue(name, out var constraint) ? constraint : CreateBuiltIn(name);

    /// <summary>
    /// Resolves a built-in constraint by name or parameterized token ("min(1)"), used when no
    /// per-container registry is threaded through parsing (e.g. a direct
    /// <see cref="BrouterTemplateParser.ParseTemplate(string, BrouterConstraintRegistry?)"/>
    /// call). Returns null when the token is not a built-in.
    /// </summary>
    internal static BrouterRouteConstraint? CreateBuiltIn(string name) =>
        _builtIns.TryGetValue(name, out var builtIn) ? builtIn : CreateParameterizedBuiltIn(name);

    private static BrouterRouteConstraint? CreateParameterizedBuiltIn(string token)
    {
        var open = token.IndexOf('(');
        if (open <= 0 || token[^1] != ')') return null;

        var name = token[..open];
        if (_parameterizedBuiltInNames.Contains(name) is false) return null;

        // The cache key folds the name (constraint tokens are case-insensitive) but keeps the
        // argument text case-sensitive, since regex patterns are semantically case-sensitive input.
        var key = string.Concat(name.ToLowerInvariant(), token.AsSpan(open));
        return _parameterizedCache.GetOrAdd(key, static k => BuildParameterizedBuiltIn(k));
    }

    // k is the normalized token "name(args)" with the name already lowercased.
    private static BrouterRouteConstraint BuildParameterizedBuiltIn(string k)
    {
        var open = k.IndexOf('(');
        var name = k[..open];
        var arg = k[(open + 1)..^1];

        switch (name)
        {
            case "min":
            {
                var min = ParseLongArgument(name, arg);
                return new BrouterPredicateRouteConstraint(s => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v >= min);
            }
            case "max":
            {
                var max = ParseLongArgument(name, arg);
                return new BrouterPredicateRouteConstraint(s => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v <= max);
            }
            case "range":
            {
                var (min, max) = ParseTwoLongArguments(name, arg);
                return new BrouterPredicateRouteConstraint(s => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v >= min && v <= max);
            }
            case "minlength":
            {
                var min = ParseIntArgument(name, arg);
                return new BrouterPredicateRouteConstraint(s => s.Length >= min);
            }
            case "maxlength":
            {
                var max = ParseIntArgument(name, arg);
                return new BrouterPredicateRouteConstraint(s => s.Length <= max);
            }
            case "length":
            {
                var comma = arg.IndexOf(',');
                if (comma < 0)
                {
                    var exact = ParseIntArgument(name, arg);
                    return new BrouterPredicateRouteConstraint(s => s.Length == exact);
                }
                var min = ParseIntArgument(name, arg[..comma]);
                var max = ParseIntArgument(name, arg[(comma + 1)..]);
                return new BrouterPredicateRouteConstraint(s => s.Length >= min && s.Length <= max);
            }
            default: // "regex" - the only remaining parameterized name.
                return new BrouterRegexRouteConstraint(arg);
        }
    }

    private static long ParseLongArgument(string constraintName, string arg) =>
        long.TryParse(arg.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new ArgumentException($"Invalid argument '{arg}' for the '{constraintName}' constraint: an integer is required.");

    private static int ParseIntArgument(string constraintName, string arg) =>
        int.TryParse(arg.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 0
            ? value
            : throw new ArgumentException($"Invalid argument '{arg}' for the '{constraintName}' constraint: a non-negative integer is required.");

    private static (long Min, long Max) ParseTwoLongArguments(string constraintName, string arg)
    {
        var comma = arg.IndexOf(',');
        if (comma < 0)
            throw new ArgumentException($"Invalid argument '{arg}' for the '{constraintName}' constraint: two comma-separated integers are required.");

        return (ParseLongArgument(constraintName, arg[..comma]), ParseLongArgument(constraintName, arg[(comma + 1)..]));
    }
}
