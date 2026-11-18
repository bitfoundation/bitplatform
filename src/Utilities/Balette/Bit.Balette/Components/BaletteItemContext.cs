namespace Bit.Balette;

/// <summary>Context handed to <c>ItemTemplate</c>.</summary>
public sealed record BaletteItemContext(BaletteRow Row, bool IsActive, bool IsChecked, bool IsPinned, bool IsConfirming, bool IsMac);
