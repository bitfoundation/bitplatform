namespace Bit.Blow;

/// <summary>Wrapper so we can pass an ElementReference without leaking the type around.</summary>
internal readonly record struct BlowElementReferenceLike(object Reference);
