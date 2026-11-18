namespace Bit.Bheadless;

/// <summary>
/// Render-fragment context exposed by <see cref="BitBhAlertDialog"/> parts for
/// state-driven rendering.
/// </summary>
public sealed record BitBhAlertDialogRenderContext
{
    /// <summary>Whether the alert dialog is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the alert dialog.</summary>
    public required Action Close { get; init; }
}
