namespace Bit.Bheadless;

/// <summary>How a <see cref="BitBhRatingItem"/> is filled.</summary>
public enum BitBhRatingItemState
{
    /// <summary>Not filled.</summary>
    Empty,

    /// <summary>Half filled (with <c>AllowHalf</c>).</summary>
    Half,

    /// <summary>Filled.</summary>
    Full,
}
