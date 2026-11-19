namespace Bit.Bheadless;

/// <summary>
/// Render-fragment context exposed to dialog content. Allows consumers
/// to render content driven by open state and to close the dialog.
/// </summary>
public sealed record BitBhDialogRenderContext
{
    /// <summary>Whether the dialog is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the dialog.</summary>
    public required Action Close { get; init; }
}
