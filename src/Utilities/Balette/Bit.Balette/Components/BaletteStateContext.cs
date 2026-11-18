namespace Bit.Balette;

/// <summary>Snapshot of palette state handed to header/footer templates.</summary>
public sealed record BaletteStateContext(
    string Query,
    string? ModePrefix,
    string? ModeName,
    IReadOnlyList<string> Breadcrumbs,
    int ResultCount,
    int ActiveIndex,
    BaletteCommand? ActiveCommand,
    bool IsLoading,
    bool IsMultiSelect,
    int SelectedCount,
    string? Toast,
    bool IsMac);
