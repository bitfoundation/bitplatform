using System.Runtime.CompilerServices;

namespace Bit.Blow;

/// <summary>
/// An object compared by reference only, for the values a renderer checks on every frame to decide whether to draw
/// again. The object's own <c>Equals</c> is never run: for a boxed <c>JsonElement</c> - the data of every element read
/// from a document - that is a reflection walk over its fields, on every node and edge, on every frame of a drag.
/// </summary>
internal readonly struct BlowSameReference(object? value) : IEquatable<BlowSameReference>
{
    public object? Value { get; } = value;

    public bool Equals(BlowSameReference other) => ReferenceEquals(Value, other.Value);

    public override bool Equals(object? obj) => obj is BlowSameReference other && Equals(other);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(Value);
}
