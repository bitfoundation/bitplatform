using Microsoft.AspNetCore.Components;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Cascading context provided by <see cref="BitBhDialog"/> to its descendant parts
/// (<see cref="BitBhDialogPanel"/>, <see cref="BitBhDialogTitle"/>,
/// <see cref="BitBhDialogDescription"/>, <see cref="BitBhDialogBackdrop"/>). Carries
/// open state, ARIA wiring ids, and close / dismiss callbacks.
/// </summary>
public sealed class BitBhDialogContext : IBitBhCloseContext
{
    private readonly Func<Task> _closeAsync;
    private readonly Func<BitBhDismissReason, Task> _dismissAsync;
    private readonly Action<bool> _registerTitle;
    private readonly Action<bool> _registerDescription;
    private readonly Action<ElementReference?, bool> _registerPanel;

    internal BitBhDialogContext(
        bool isOpen,
        string baseId,
        string role,
        bool modal,
        bool hasTitle,
        bool hasDescription,
        Func<Task> closeAsync,
        Func<BitBhDismissReason, Task> dismissAsync,
        Action<bool> registerTitle,
        Action<bool> registerDescription,
        Action<ElementReference?, bool> registerPanel)
    {
        IsOpen = isOpen;
        BaseId = baseId;
        Role = role;
        Modal = modal;
        HasTitle = hasTitle;
        HasDescription = hasDescription;
        _closeAsync = closeAsync;
        _dismissAsync = dismissAsync;
        _registerTitle = registerTitle;
        _registerDescription = registerDescription;
        _registerPanel = registerPanel;
    }

    /// <summary>Whether the dialog is currently open.</summary>
    public bool IsOpen { get; }

    /// <summary>The base id used to derive the panel, title, and description ids.</summary>
    public string BaseId { get; }

    /// <summary>The ARIA role of the panel (<c>dialog</c> or <c>alertdialog</c>).</summary>
    public string Role { get; }

    /// <summary>Whether the dialog is modal.</summary>
    public bool Modal { get; }

    /// <summary>Whether a <see cref="BitBhDialogTitle"/> is rendered.</summary>
    public bool HasTitle { get; }

    /// <summary>Whether a <see cref="BitBhDialogDescription"/> is rendered.</summary>
    public bool HasDescription { get; }

    /// <summary>The deterministic id used as <c>aria-labelledby</c> on the dialog panel.</summary>
    public string TitleId => $"{BaseId}-title";

    /// <summary>The deterministic id used as <c>aria-describedby</c> on the dialog panel.</summary>
    public string DescriptionId => $"{BaseId}-description";

    /// <summary>The deterministic id of the dialog panel itself.</summary>
    public string PanelId => $"{BaseId}-panel";

    /// <summary>Closes the dialog.</summary>
    public Task CloseAsync() => _closeAsync();

    /// <summary>Requests dismissal (respects <c>DismissOnEscape</c> and <c>OnDismissRequest</c>).</summary>
    public Task DismissAsync(BitBhDismissReason reason) => _dismissAsync(reason);

    internal void RegisterTitle(bool present) => _registerTitle(present);
    internal void RegisterDescription(bool present) => _registerDescription(present);
    internal void RegisterPanel(ElementReference? initialFocus, bool dismissOnEscape) => _registerPanel(initialFocus, dismissOnEscape);
}

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
