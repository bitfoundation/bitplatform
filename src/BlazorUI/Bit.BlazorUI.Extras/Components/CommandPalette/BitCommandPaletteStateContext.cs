namespace Bit.BlazorUI;

/// <summary>Snapshot of palette state handed to header/footer templates.</summary>
public sealed record BitCommandPaletteStateContext(
    string Query,
    string? ModePrefix,
    string? ModeName,
    IReadOnlyList<string> Breadcrumbs,
    int ResultCount,
    int ActiveIndex,
    BitCommandPaletteCommand? ActiveCommand,
    bool IsLoading,
    bool IsMultiSelect,
    int SelectedCount,
    string? Toast,
    bool IsMac);
