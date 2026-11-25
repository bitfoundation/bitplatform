namespace Bit.BlazorUI;

/// <summary>
/// A button that opens the <see cref="BitCommandPalette"/> - optionally with a query (a mode prefix) or on a command's
/// page - and shows the palette's toggle shortcut in the platform's own symbols once the palette has detected it.
/// </summary>
public partial class BitCommandPaletteTrigger : BitComponentBase
{
    // Resolved rather than injected, so the trigger also works when the app never called AddBitBlazorUIExtrasServices.
    // Null until first used: the disposal leaves alone what was never resolved.
    private BitCommandPaletteRuntime? _runtime;



    [Inject] private IServiceProvider _services { get; set; } = default!;



    /// <summary>
    /// Replaces the button's content (icon, label and shortcut hint). Without a <see cref="Label"/> the button is named by
    /// what the content shows, followed by ", Open command palette"; content that shows only an icon sets
    /// <c>Label=""</c>, so the name does not start with the separator.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the trigger.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitCommandPaletteTriggerClassStyles? Classes { get; set; }

    /// <summary>
    /// The label. Default: "Search"; an empty one shows the icon alone. The button's accessible name starts with it. With
    /// <see cref="ChildContent"/> it is not shown, only named: set it to the text the content shows, or to an empty string
    /// for content that shows only an icon, and the button is named as the default one is.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Forces the macOS key symbols on or off. Default: the platform the palette detected.
    /// </summary>
    [Parameter] public bool? MacKeys { get; set; }

    /// <summary>
    /// Opens the palette directly inside the page of the command with this id.
    /// </summary>
    [Parameter] public string? PageCommandId { get; set; }

    /// <summary>
    /// Opens the palette with this query (e.g. a mode prefix).
    /// </summary>
    [Parameter] public string? Query { get; set; }

    /// <summary>
    /// Shows the toggle shortcut of the palette. Default: true.
    /// </summary>
    [Parameter] public bool ShowShortcut { get; set; } = true;

    /// <summary>
    /// Custom CSS styles for the different parts of the trigger.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitCommandPaletteTriggerClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-cpt";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        Palette.PlatformDetected += OnPaletteChanged;
        Palette.ModalPaletteChanged += OnPaletteChanged;

        base.OnInitialized();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_runtime is not null)
        {
            _runtime.Service.PlatformDetected -= OnPaletteChanged;
            _runtime.Service.ModalPaletteChanged -= OnPaletteChanged;
        }

        await base.DisposeAsync(disposing);
    }



    private BitCommandPaletteRuntime Runtime => _runtime ??= BitCommandPaletteRuntime.Of(_services);

    private BitCommandPaletteService Palette => Runtime.Service;

    private bool EffectiveMacKeys => MacKeys ?? Palette.IsMac;

    // What the modal palette answers to - its ToggleShortcut and Strings parameters override the options.
    private BitCommandPaletteKeyboardShortcut? Shortcut => Palette.ModalPalette is { } palette ? palette.ToggleShortcut : Runtime.Options.ToggleShortcut;

    private BitCommandPaletteStrings S => Palette.ModalPalette?.Strings ?? Runtime.Options.Strings;

    /// <summary>
    /// The button's name starts with the label it shows (WCAG 2.5.3, label in name), so someone who says "click Search"
    /// reaches it, and goes on to say what it does. The component cannot read the text of content an app puts in the
    /// label's place, so unless the app names that text through <see cref="Label"/> the button has no <c>aria-label</c>:
    /// the browser names it from the content and the visually hidden string after it. An AriaLabel wins over both.
    /// </summary>
    private string? AccessibleName
    {
        get
        {
            if (AriaLabel is not null)
            {
                return AriaLabel;
            }

            if (ChildContent is not null && Label is null)
            {
                return null;
            }

            var label = (Label ?? S.Search).Trim();
            var action = S.OpenPalette;
            if (label.Length == 0 || action.Contains(label, StringComparison.CurrentCultureIgnoreCase))
            {
                // Nothing shown to start with, or a whole name - "Search commands" for "Search" - that already holds it.
                return action;
            }

            return label.Contains(action, StringComparison.CurrentCultureIgnoreCase) ? label : label + S.TriggerNameSeparator + action;
        }
    }

    /// <summary>Value for <c>aria-keyshortcuts</c>, which uses DOM key names (Control, Meta, Shift, Alt).</summary>
    private string? ShortcutText => Shortcut is null ? null : string.Join(" ", Shortcut.Chords.Select(AriaChord));

    private string AriaChord(BitCommandPaletteKeyChord c)
    {
        var parts = new List<string>(5);
        if (c.Mod) parts.Add(EffectiveMacKeys ? "Meta" : "Control");
        if (c.Ctrl) parts.Add("Control");
        if (c.Alt) parts.Add("Alt");
        if (c.Shift) parts.Add("Shift");
        if (c.Meta) parts.Add("Meta");
        parts.Add(c.Key.Length == 1 ? c.Key.ToUpperInvariant() : char.ToUpperInvariant(c.Key[0]) + c.Key[1..]);
        return string.Join("+", parts);
    }

    private void OnPaletteChanged() => _ = InvokeAsync(StateHasChanged);

    private Task OnClick()
    {
        if (Disabled) return Task.CompletedTask;

        return Palette.OpenAsync(Query, PageCommandId);
    }
}
