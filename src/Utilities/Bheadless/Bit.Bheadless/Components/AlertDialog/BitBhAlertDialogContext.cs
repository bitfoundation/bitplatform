namespace Bit.Bheadless;

/// <summary>
/// Cascading context provided by <see cref="BitBhAlertDialog"/> to its descendant
/// parts (<see cref="BitBhAlertDialogTrigger"/>, <see cref="BitBhAlertDialogOverlay"/>,
/// <see cref="BitBhAlertDialogContent"/>, <see cref="BitBhAlertDialogTitle"/>,
/// <see cref="BitBhAlertDialogDescription"/>, <see cref="BitBhAlertDialogCancel"/>,
/// <see cref="BitBhAlertDialogAction"/>). Carries open state, ARIA wiring ids, and
/// the open/close callbacks.
/// </summary>
public sealed class BitBhAlertDialogContext : IBitBhCloseContext
{
    private readonly Func<Task> _openAsync;
    private readonly Func<Task> _closeAsync;
    private readonly Func<BitBhDismissReason, Task> _dismissAsync;
    private readonly Action<BitBhAlertDialogPart, bool> _register;

    internal BitBhAlertDialogContext(
        bool isOpen,
        bool isPresent,
        bool isHidden,
        string baseId,
        bool hasTitle,
        bool hasDescription,
        Func<Task> openAsync,
        Func<Task> closeAsync,
        Func<BitBhDismissReason, Task> dismissAsync,
        Action<BitBhAlertDialogPart, bool> register)
    {
        IsOpen = isOpen;
        IsPresent = isPresent;
        IsHidden = isHidden;
        BaseId = baseId;
        HasTitle = hasTitle;
        HasDescription = hasDescription;
        _openAsync = openAsync;
        _closeAsync = closeAsync;
        _dismissAsync = dismissAsync;
        _register = register;
    }

    /// <summary>Whether the alert dialog is currently open.</summary>
    public bool IsOpen { get; }

    /// <summary>Whether the overlay and content should be rendered (open, or animating out).</summary>
    public bool IsPresent { get; }

    /// <summary>Whether the overlay and content should be hidden (waiting to be portalled / positioned).</summary>
    public bool IsHidden { get; }

    /// <summary>Base id used to derive the trigger, content, title, and description ids.</summary>
    public string BaseId { get; }

    /// <summary>Whether a title is rendered.</summary>
    public bool HasTitle { get; }

    /// <summary>Whether a description is rendered.</summary>
    public bool HasDescription { get; }

    /// <summary>Deterministic id of the trigger button.</summary>
    public string TriggerId => $"{BaseId}-trigger";

    /// <summary>Deterministic id of the content surface.</summary>
    public string ContentId => $"{BaseId}-content";

    /// <summary>Deterministic id of the overlay.</summary>
    public string OverlayId => $"{BaseId}-overlay";

    /// <summary>Deterministic id of the cancel button (receives initial focus).</summary>
    public string CancelId => $"{BaseId}-cancel";

    /// <summary>The deterministic id used as <c>aria-labelledby</c> on the content.</summary>
    public string TitleId => $"{BaseId}-title";

    /// <summary>The deterministic id used as <c>aria-describedby</c> on the content.</summary>
    public string DescriptionId => $"{BaseId}-description";

    /// <summary>Opens the alert dialog.</summary>
    public Task OpenAsync() => _openAsync();

    /// <summary>Closes the alert dialog.</summary>
    public Task CloseAsync() => _closeAsync();

    /// <summary>Requests dismissal (Escape).</summary>
    public Task DismissAsync(BitBhDismissReason reason) => _dismissAsync(reason);

    internal void Register(BitBhAlertDialogPart part, bool present) => _register(part, present);
}

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
