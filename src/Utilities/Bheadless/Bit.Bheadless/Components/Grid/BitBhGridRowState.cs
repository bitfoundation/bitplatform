namespace Bit.Bheadless;

/// <summary>The state of a <see cref="BitBhGridRow"/>, cascaded to its cells' content.</summary>
/// <param name="Key">The row key.</param>
/// <param name="Level">The hierarchy level (treegrid).</param>
/// <param name="HasChildren">Whether the row has child rows.</param>
/// <param name="IsExpanded">Whether the row is expanded.</param>
/// <param name="IsSelected">Whether the row is selected.</param>
public sealed record BitBhGridRowState(string? Key, int? Level, bool HasChildren, bool IsExpanded, bool IsSelected);
