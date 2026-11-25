namespace Bit.BlazorUI;

/// <summary>Context handed to <c>ItemTemplate</c>.</summary>
public sealed record BitCommandPaletteItemContext(BitCommandPaletteRow Row, bool IsActive, bool IsChecked, bool IsPinned, bool IsConfirming, bool IsMac);
