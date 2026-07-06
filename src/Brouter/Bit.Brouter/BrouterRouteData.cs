using System.Diagnostics.CodeAnalysis;

namespace Bit.Brouter;

/// <summary>
/// Base of the typed wrappers Brouter cascades to matched route content
/// (<see cref="BrouterRouteData"/> and <see cref="BrouterRouteMeta"/>). Wraps the raw
/// <see cref="object"/> payload with type-safe accessors so consumers don't cast by hand,
/// mirroring the <c>Get</c>/<c>TryGet</c>/<c>GetOrDefault</c> surface of
/// <see cref="BrouterRouteParameters"/>.
/// </summary>
public abstract class BrouterRouteValue
{
    private protected BrouterRouteValue(object? value) => Value = value;

    /// <summary>The raw underlying value, or null when nothing was supplied.</summary>
    public object? Value { get; }

    /// <summary>True when an underlying value is present (non-null).</summary>
    public bool HasValue => Value is not null;

    /// <summary>Returns the value as <typeparamref name="T"/>.</summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no value is present or the value is not a <typeparamref name="T"/>. Use
    /// <see cref="GetOrDefault{T}"/> or <see cref="TryGet{T}(out T)"/> when absence or a
    /// different type is expected.
    /// </exception>
    public T Get<T>()
    {
        if (Value is T t) return t;

        // Distinguish "nothing was supplied" from "supplied, but of another type": the first
        // usually means the consumer rendered outside a matched route (or the route has no
        // loader/meta), the second is a plain type mismatch at the call site.
        throw new InvalidOperationException(Value is null
            ? $"No {Kind} value is present. Use TryGet/GetOrDefault when absence is expected."
            : $"The {Kind} value is of type {Value.GetType().Name} and cannot be read as {typeof(T).Name}.");
    }

    /// <summary>Returns the value as <typeparamref name="T"/>, or <paramref name="defaultValue"/> when absent or of another type.</summary>
    public T? GetOrDefault<T>(T? defaultValue = default) => Value is T t ? t : defaultValue;

    /// <summary>Tries to read the value as <typeparamref name="T"/>.</summary>
    public bool TryGet<T>([MaybeNullWhen(false)] out T value)
    {
        if (Value is T t)
        {
            value = t;
            return true;
        }
        value = default;
        return false;
    }

    // Human-readable kind ("route data" / "route meta") used in Get<T> error messages.
    private protected abstract string Kind { get; }
}

/// <summary>
/// The typed cascading wrapper around a matched route's <see cref="Broute.Loader"/> result.
/// Consume it with <c>[CascadingParameter] BrouterRouteData? Data</c> - the cascade is unnamed
/// and matched by this unique type, so no <c>Name</c> is used. The wrapper instance is always
/// non-null under a matched route; <see cref="BrouterRouteValue.Value"/> is null when the route
/// has no loader or the loader returned null.
/// </summary>
public sealed class BrouterRouteData : BrouterRouteValue
{
    /// <summary>A data instance carrying no value.</summary>
    public static readonly BrouterRouteData Empty = new(null);

    internal BrouterRouteData(object? value) : base(value) { }

    private protected override string Kind => "route data";
}

/// <summary>
/// The typed cascading wrapper around a matched route's <see cref="Broute.Meta"/> value.
/// Consume it with <c>[CascadingParameter] BrouterRouteMeta? Meta</c> - the cascade is unnamed
/// and matched by this unique type, so no <c>Name</c> is used. The wrapper instance is always
/// non-null under a matched route; <see cref="BrouterRouteValue.Value"/> is null when the route
/// declares no meta.
/// </summary>
public sealed class BrouterRouteMeta : BrouterRouteValue
{
    /// <summary>A meta instance carrying no value.</summary>
    public static readonly BrouterRouteMeta Empty = new(null);

    internal BrouterRouteMeta(object? value) : base(value) { }

    private protected override string Kind => "route meta";
}
