using System.Diagnostics.CodeAnalysis;

namespace Bit.Butil;

/// <summary>
/// A continuous interval over IndexedDB keys - the .NET mirror of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/IDBKeyRange">IDBKeyRange</see>.
/// </summary>
/// <remarks>
/// Anywhere an <see cref="IndexedDbHandle"/> method takes an <c>object query</c> you may pass either a
/// plain key or one of these ranges; the ranged overloads that take this type exist where the plain
/// signature already meant something else.
/// <br/>
/// Keys travel as JSON, so only the JSON-representable half of the IndexedDB key type set survives:
/// numbers, strings, booleans and arrays of those (arrays being how compound keys are expressed).
/// A <see cref="System.DateTime"/> arrives in JS as an ISO-8601 <em>string</em>, not a <c>Date</c> -
/// that still orders correctly (ISO-8601 sorts lexicographically) as long as every key in the store
/// was written the same way.
/// </remarks>
public sealed class IndexedDbKeyRange
{
    /// <summary>Marker read by the JS side to tell a range apart from a plain key. Always true.</summary>
    public bool IsKeyRange => true;

    /// <summary>True when this range matches a single exact key (<see cref="Only"/>).</summary>
    public bool IsOnly { get; private set; }

    /// <summary>Lower bound, or null when the range is open-ended below.</summary>
    public object? Lower { get; private set; }

    /// <summary>Upper bound, or null when the range is open-ended above.</summary>
    public object? Upper { get; private set; }

    /// <summary>True to exclude <see cref="Lower"/> itself from the range.</summary>
    public bool LowerOpen { get; private set; }

    /// <summary>True to exclude <see cref="Upper"/> itself from the range.</summary>
    public bool UpperOpen { get; private set; }

    // Ranges are serialized TO JavaScript: every property here is written by reflection and read by
    // nobody in C#, which is exactly the shape a trimmer removes. If the getters go, a range
    // serializes to {}, the isKeyRange marker disappears, and the JS side silently treats it as a
    // plain key instead of a range - wrong results, no exception. Anchoring the dependency on the
    // factories means any code path that can produce a range has already preserved it, which is
    // sturdier than annotating every method that accepts one (most take it as `object query`).
    private const DynamicallyAccessedMemberTypes Serialized = DynamicallyAccessedMemberTypes.All;

    /// <summary>Matches the single key <paramref name="value"/>. See <c>IDBKeyRange.only()</c>.</summary>
    [DynamicDependency(Serialized, typeof(IndexedDbKeyRange))]
    public static IndexedDbKeyRange Only(object value)
        => new() { IsOnly = true, Lower = value, Upper = value };

    /// <summary>
    /// Matches every key at or above <paramref name="lower"/>. Pass <paramref name="open"/> to exclude
    /// <paramref name="lower"/> itself. See <c>IDBKeyRange.lowerBound()</c>.
    /// </summary>
    [DynamicDependency(Serialized, typeof(IndexedDbKeyRange))]
    public static IndexedDbKeyRange LowerBound(object lower, bool open = false)
        => new() { Lower = lower, LowerOpen = open };

    /// <summary>
    /// Matches every key at or below <paramref name="upper"/>. Pass <paramref name="open"/> to exclude
    /// <paramref name="upper"/> itself. See <c>IDBKeyRange.upperBound()</c>.
    /// </summary>
    [DynamicDependency(Serialized, typeof(IndexedDbKeyRange))]
    public static IndexedDbKeyRange UpperBound(object upper, bool open = false)
        => new() { Upper = upper, UpperOpen = open };

    /// <summary>
    /// Matches every key between <paramref name="lower"/> and <paramref name="upper"/>, inclusive on
    /// both ends unless the matching <c>*Open</c> flag is set. See <c>IDBKeyRange.bound()</c>.
    /// </summary>
    [DynamicDependency(Serialized, typeof(IndexedDbKeyRange))]
    public static IndexedDbKeyRange Bound(object lower, object upper, bool lowerOpen = false, bool upperOpen = false)
        => new() { Lower = lower, Upper = upper, LowerOpen = lowerOpen, UpperOpen = upperOpen };
}
