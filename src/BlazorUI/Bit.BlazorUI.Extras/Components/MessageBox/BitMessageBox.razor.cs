namespace Bit.BlazorUI;

/// <summary>
/// BitMessageBox is a pre-implemented box for showing messages with title and body.
/// </summary>
/// <remarks>
/// The component is the content of a message box, not the layer it is shown on: it is rendered inline,
/// inside a <see cref="BitModal"/>, or - the usual way - through the <see cref="BitMessageBoxService"/>,
/// which shows it in a modal and hands back the <see cref="BitMessageBoxResult"/> it was answered with.
/// </remarks>
public partial class BitMessageBox : BitComponentBase
{
    private readonly Dictionary<BitMessageBoxResult, BitButton> _buttonRefs = [];



    /// <summary>
    /// Enables the loading state of the action button that was pressed for as long as its callback runs,
    /// which is what keeps a slow answer from being handed in twice.
    /// </summary>
    [Parameter] public bool AutoLoading { get; set; }

    /// <summary>
    /// Moves the focus onto the default action button once the message box is rendered.
    /// </summary>
    /// <remarks>
    /// Off by default, since a message box written inline in a page is part of that page and has no
    /// business taking the focus off it. It is on for every message box the
    /// <see cref="BitMessageBoxService"/> shows, which is the case the accessibility pattern asks for:
    /// a dialog that interrupts has to put the focus inside itself.
    /// <br/>
    /// <see cref="DefaultButton"/> picks which button it lands on.
    /// </remarks>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// The body of the message box.
    /// </summary>
    /// <remarks>
    /// The text is rendered as written: line breaks in it are kept, and long lines wrap rather than
    /// running off the side of the box. Use <see cref="BodyTemplate"/> for a body that is markup.
    /// </remarks>
    [Parameter] public string? Body { get; set; }

    /// <summary>
    /// The template of the body of the message box, which takes the place of <see cref="Body"/>.
    /// </summary>
    [Parameter] public RenderFragment? BodyTemplate { get; set; }

    /// <summary>
    /// The set of buttons the message box renders in its footer.
    /// </summary>
    [Parameter] public BitMessageBoxButtons Buttons { get; set; }

    /// <summary>
    /// The color of the action buttons of the message box.
    /// </summary>
    /// <remarks>
    /// Tertiary by default, which is the neutral of the theme. <see cref="PrimaryButtonColor"/> is what
    /// singles the affirmative button out of the set - an Error-colored Delete beside a neutral Cancel.
    /// </remarks>
    [Parameter] public BitColor? ButtonColor { get; set; }

    /// <summary>
    /// The text of the Cancel button.
    /// </summary>
    [Parameter] public string? CancelText { get; set; }

    /// <summary>
    /// The alias of <see cref="BodyTemplate"/>.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the message box.
    /// </summary>
    [Parameter] public BitMessageBoxClassStyles? Classes { get; set; }

    /// <summary>
    /// The title (and aria-label) of the close button, for accessibility and localization.
    /// </summary>
    [Parameter] public string? CloseButtonTitle { get; set; }

    /// <summary>
    /// The icon of the close button, provided as custom CSS classes of an external icon library.
    /// </summary>
    [Parameter] public BitIconInfo? CloseIcon { get; set; }

    /// <summary>
    /// The name of the icon of the close button, from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? CloseIconName { get; set; }

    /// <summary>
    /// The general color of the message box, which is what its leading icon is painted in and - unless
    /// <see cref="IconName"/> says otherwise - what picks the glyph.
    /// </summary>
    /// <remarks>
    /// This is the severity of the message: Info, Success, Warning, SevereWarning and Error each carry a
    /// glyph of their own, which is why setting a color is enough to give a message box its icon.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The action button that <see cref="AutoFocus"/> moves the focus onto.
    /// </summary>
    /// <remarks>
    /// The primary button (Ok, or Yes) by default. Point it at the Cancel or the No button for the
    /// message boxes that ask about something destructive, so that the answer a stray keystroke gives
    /// is the harmless one.
    /// <br/>
    /// A button that is not part of the current <see cref="Buttons"/> set is ignored.
    /// </remarks>
    [Parameter] public BitMessageBoxResult? DefaultButton { get; set; }

    /// <summary>
    /// The template used to render the footer of the message box, which takes the place of its action buttons.
    /// </summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// The template used to render the header of the message box, which takes the place of its icon, title
    /// and close button.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Removes the leading icon of the message box, which a <see cref="Color"/> would otherwise bring with it.
    /// </summary>
    [Parameter] public bool HideIcon { get; set; }

    /// <summary>
    /// The leading icon of the message box, provided as custom CSS classes of an external icon library.
    /// </summary>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The accessible name of the leading icon, which turns it from decoration into an image that is
    /// announced - the severity of the message, for a message box whose color is the only thing saying it.
    /// </summary>
    [Parameter] public string? IconAriaLabel { get; set; }

    /// <summary>
    /// The name of the leading icon of the message box, from the built-in Fluent UI icons.
    /// If unset, the icon is selected automatically based on <see cref="Color"/>.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The template used to render the leading icon of the message box.
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// The text of the No button.
    /// </summary>
    [Parameter] public string? NoText { get; set; }

    /// <summary>
    /// The text of the Ok button.
    /// </summary>
    [Parameter] public string? OkText { get; set; }

    /// <summary>
    /// The event callback for the Cancel button of the message box.
    /// </summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>
    /// The event callback for closing the message box, raised by every button it renders of its own -
    /// after the callback of that button and after <see cref="OnResult"/>.
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// The event callback for the No button of the message box.
    /// </summary>
    [Parameter] public EventCallback OnNo { get; set; }

    /// <summary>
    /// The event callback for the Ok button of the message box.
    /// </summary>
    [Parameter] public EventCallback OnOk { get; set; }

    /// <summary>
    /// The event callback for the answer the message box was given, which is what tells the buttons apart:
    /// the close button answers with <see cref="BitMessageBoxResult.None"/>, the others with the result
    /// they stand for.
    /// </summary>
    [Parameter] public EventCallback<BitMessageBoxResult> OnResult { get; set; }

    /// <summary>
    /// The event callback for the Yes button of the message box.
    /// </summary>
    [Parameter] public EventCallback OnYes { get; set; }

    /// <summary>
    /// The color of the affirmative action button (Ok, or Yes), which falls back to
    /// <see cref="ButtonColor"/> where it is not set.
    /// </summary>
    [Parameter] public BitColor? PrimaryButtonColor { get; set; }

    /// <summary>
    /// Renders the action buttons in the reverse order, which also reverses the order the keyboard
    /// reaches them in.
    /// </summary>
    [Parameter] public bool Reversed { get; set; }

    /// <summary>
    /// Renders the close button in the header of the message box.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// The size of the message box, which scales its inset, its body text and its leading icon together.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the message box.
    /// </summary>
    [Parameter] public BitMessageBoxClassStyles? Styles { get; set; }

    /// <summary>
    /// The title of the message box.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The text of the Yes button.
    /// </summary>
    [Parameter] public string? YesText { get; set; }



    /// <summary>
    /// The answer the last showing of this message box was given, or
    /// <see cref="BitMessageBoxResult.None"/> while it has not been answered.
    /// </summary>
    public BitMessageBoxResult Result { get; private set; }

    /// <summary>
    /// Moves the focus onto the default action button of the message box.
    /// </summary>
    /// <remarks>
    /// A message box whose footer is a <see cref="FooterTemplate"/> renders no buttons of its own, so
    /// there is nothing for this to focus and the call does nothing.
    /// </remarks>
    public async ValueTask FocusAsync()
    {
        if (_buttonRefs.TryGetValue(_DefaultAction, out var button))
        {
            await button.FocusAsync();
        }
    }



    internal string _TitleId => $"{_Id}-ttl";

    internal string _BodyId => $"{_Id}-bdy";

    private string _TitleClass => Classes?.Title.HasValue() is true ? $"bit-msb-ttl {Classes!.Title}" : "bit-msb-ttl";

    private bool _HasBodyTemplate => (BodyTemplate ?? ChildContent) is not null;

    private BitIconInfo? _Icon => BitIconInfo.From(Icon, IconName ?? (Color.HasValue ? _IconMap[Color.Value] : null));

    private bool _HasIcon => HideIcon is false && (IconTemplate is not null || _Icon is not null);

    private bool _HasHeader => HeaderTemplate is not null || _HasIcon || Title.HasValue() || ShowCloseButton;

    private BitIconInfo? _CloseIcon => BitIconInfo.From(CloseIcon, CloseIconName ?? "ChromeClose");

    private string _CloseButtonTitle => CloseButtonTitle ?? "Close";

    // The button the primary look and the default focus go to, which is the affirmative answer of the set.
    private BitMessageBoxResult _PrimaryAction => Buttons is BitMessageBoxButtons.YesNo or BitMessageBoxButtons.YesNoCancel
                                                    ? BitMessageBoxResult.Yes
                                                    : BitMessageBoxResult.Ok;

    // A DefaultButton naming a button this set does not render would leave the focus nowhere, so the
    // primary one stands in for it.
    private BitMessageBoxResult _DefaultAction => DefaultButton.HasValue && Array.IndexOf(_Actions, DefaultButton.Value) >= 0
                                                    ? DefaultButton.Value
                                                    : _PrimaryAction;

    private BitMessageBoxResult[] _Actions
    {
        get
        {
            var actions = Buttons switch
            {
                BitMessageBoxButtons.OkCancel => _OkCancelActions,
                BitMessageBoxButtons.YesNo => _YesNoActions,
                BitMessageBoxButtons.YesNoCancel => _YesNoCancelActions,
                _ => _OkActions
            };

            // A reversed set is a new array rather than the shared one reversed in place, which would
            // reverse it again on every render.
            return Reversed ? [.. actions.Reverse()] : actions;
        }
    }

    private static readonly BitMessageBoxResult[] _OkActions = [BitMessageBoxResult.Ok];
    private static readonly BitMessageBoxResult[] _OkCancelActions = [BitMessageBoxResult.Ok, BitMessageBoxResult.Cancel];
    private static readonly BitMessageBoxResult[] _YesNoActions = [BitMessageBoxResult.Yes, BitMessageBoxResult.No];
    private static readonly BitMessageBoxResult[] _YesNoCancelActions = [BitMessageBoxResult.Yes, BitMessageBoxResult.No, BitMessageBoxResult.Cancel];

    private static readonly Dictionary<BitColor, string> _IconMap = new()
    {
        [BitColor.Primary] = "Info",
        [BitColor.Secondary] = "Info",
        [BitColor.Tertiary] = "Info",
        [BitColor.Info] = "Info",
        [BitColor.Success] = "Completed",
        [BitColor.Warning] = "Warning",
        [BitColor.SevereWarning] = "WarningSolid",
        [BitColor.Error] = "ErrorBadge",
        [BitColor.PrimaryBackground] = "Info",
        [BitColor.SecondaryBackground] = "Info",
        [BitColor.TertiaryBackground] = "Info",
        [BitColor.PrimaryForeground] = "Info",
        [BitColor.SecondaryForeground] = "Info",
        [BitColor.TertiaryForeground] = "Info",
        [BitColor.PrimaryBorder] = "Info",
        [BitColor.SecondaryBorder] = "Info",
        [BitColor.TertiaryBorder] = "Info",
    };



    protected override string RootElementClass => "bit-msb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root)
                    .Register(() => Color switch
                    {
                        BitColor.Primary => "bit-msb-pri",
                        BitColor.Secondary => "bit-msb-sec",
                        BitColor.Tertiary => "bit-msb-ter",
                        BitColor.Info => "bit-msb-inf",
                        BitColor.Success => "bit-msb-suc",
                        BitColor.Warning => "bit-msb-wrn",
                        BitColor.SevereWarning => "bit-msb-swr",
                        BitColor.Error => "bit-msb-err",
                        BitColor.PrimaryBackground => "bit-msb-pbg",
                        BitColor.SecondaryBackground => "bit-msb-sbg",
                        BitColor.TertiaryBackground => "bit-msb-tbg",
                        BitColor.PrimaryForeground => "bit-msb-pfg",
                        BitColor.SecondaryForeground => "bit-msb-sfg",
                        BitColor.TertiaryForeground => "bit-msb-tfg",
                        BitColor.PrimaryBorder => "bit-msb-pbr",
                        BitColor.SecondaryBorder => "bit-msb-sbr",
                        BitColor.TertiaryBorder => "bit-msb-tbr",
                        _ => string.Empty
                    })
                    .Register(() => Size switch
                    {
                        BitSize.Small => "bit-msb-sm",
                        BitSize.Medium => "bit-msb-md",
                        BitSize.Large => "bit-msb-lg",
                        _ => string.Empty
                    });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && AutoFocus)
        {
            await FocusAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    // Tertiary is the neutral the footer has always been drawn in, so a message box that says nothing
    // about its buttons looks exactly as it did before the two color parameters existed.
    private BitColor GetActionColor(BitMessageBoxResult action)
        => (action == _PrimaryAction ? PrimaryButtonColor : null) ?? ButtonColor ?? BitColor.Tertiary;

    private string? GetActionText(BitMessageBoxResult action) => action switch
    {
        BitMessageBoxResult.Cancel => CancelText ?? "Cancel",
        BitMessageBoxResult.Yes => YesText ?? "Yes",
        BitMessageBoxResult.No => NoText ?? "No",
        _ => OkText ?? "Ok"
    };

    // The per-button set stands on its own where it was given, and the shared one is what the buttons
    // that were given none fall back to - a coalesce rather than a merge, so which set is in force is
    // always the one a reader can point at.
    private BitButtonClassStyles? GetActionStyles(BitMessageBoxResult action) => (action switch
    {
        BitMessageBoxResult.Cancel => Styles?.CancelButton,
        BitMessageBoxResult.Yes => Styles?.YesButton,
        BitMessageBoxResult.No => Styles?.NoButton,
        _ => Styles?.OkButton
    }) ?? Styles?.ActionButton;

    private BitButtonClassStyles? GetActionClasses(BitMessageBoxResult action) => (action switch
    {
        BitMessageBoxResult.Cancel => Classes?.CancelButton,
        BitMessageBoxResult.Yes => Classes?.YesButton,
        BitMessageBoxResult.No => Classes?.NoButton,
        _ => Classes?.OkButton
    }) ?? Classes?.ActionButton;

    private Task HandleOnCloseClick() => HandleAction(BitMessageBoxResult.None);

    private async Task HandleAction(BitMessageBoxResult result)
    {
        if (IsEnabled is false) return;

        Result = result;

        switch (result)
        {
            case BitMessageBoxResult.Ok:
                await OnOk.InvokeAsync();
                break;
            case BitMessageBoxResult.Cancel:
                await OnCancel.InvokeAsync();
                break;
            case BitMessageBoxResult.Yes:
                await OnYes.InvokeAsync();
                break;
            case BitMessageBoxResult.No:
                await OnNo.InvokeAsync();
                break;
        }

        await OnResult.InvokeAsync(result);

        // Raised last, so a handler that tears the message box down does not do it before the answer
        // has been handed to whoever was waiting for it.
        await OnClose.InvokeAsync();
    }
}
