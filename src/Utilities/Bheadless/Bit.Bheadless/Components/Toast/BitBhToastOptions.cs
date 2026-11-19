namespace Bit.Bheadless;

/// <summary>Options for a toast shown with <see cref="BitBhToaster.Show(BitBhToastOptions)"/>.</summary>
public sealed class BitBhToastOptions
{
    /// <summary>
    /// The toast's id. Showing a toast with the id of one still shown updates that one in place (its timer starts
    /// over) instead of adding another - e.g. "Uploading 3 files…" becoming "Uploaded". Default: a new id.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// A variant for your template and styles (<c>data-variant</c>): "success", "error", "warning", "info" …
    /// <see cref="BitBhToaster.PromiseAsync{T}"/> uses "loading", "success" and "error".
    /// </summary>
    public string? Variant { get; set; }

    /// <summary>
    /// Invoked once the toast is gone, however it was dismissed (timer, close button, swipe, Escape, code). It runs on the
    /// thread that dismissed the toast - from a component, change state through <c>InvokeAsync</c> - and an update that
    /// sets none keeps the toast's earlier one.
    /// </summary>
    public Func<Task>? OnDismiss { get; set; }

    /// <summary>The toast title.</summary>
    public string? Title { get; set; }

    /// <summary>The toast description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Foreground toasts are the result of a user action and are announced
    /// assertively (<c>role="alert"</c>); background toasts politely (<c>role="status"</c>).
    /// Default: background.
    /// </summary>
    public bool Foreground { get; set; }

    /// <summary>Auto-dismiss delay (ms). Null uses the viewport default; 0 or less keeps it until dismissed.</summary>
    public int? Duration { get; set; }

    /// <summary>Optional action button label (e.g. "Undo").</summary>
    public string? ActionLabel { get; set; }

    /// <summary>Invoked when the action is chosen.</summary>
    public Func<Task>? OnAction { get; set; }

    /// <summary>Arbitrary data for your template (icon, variant, …).</summary>
    public object? Data { get; set; }
}
