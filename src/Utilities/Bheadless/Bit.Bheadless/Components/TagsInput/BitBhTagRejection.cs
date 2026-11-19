namespace Bit.Bheadless;

/// <summary>A tag a <see cref="BitBhTagsInput"/> refused, and why.</summary>
public sealed record BitBhTagRejection(string Tag, BitBhTagRejectionReason Reason);
