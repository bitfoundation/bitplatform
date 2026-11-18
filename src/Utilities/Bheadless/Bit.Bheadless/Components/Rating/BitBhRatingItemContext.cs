namespace Bit.Bheadless;

/// <summary>What a <see cref="BitBhRatingItem"/>'s <c>Template</c> receives.</summary>
/// <param name="Index">The item's position (0-based): it stands for the values <c>Index + 0.5</c> and <c>Index + 1</c>.</param>
/// <param name="State">How the item is filled for the value shown (the hovered one while previewing).</param>
/// <param name="IsPreview">Whether the value shown is a pointer preview rather than the value.</param>
public sealed record BitBhRatingItemContext(int Index, BitBhRatingItemState State, bool IsPreview);
