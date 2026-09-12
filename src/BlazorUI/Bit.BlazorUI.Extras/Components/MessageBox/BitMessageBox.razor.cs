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
    private bool _answering;
    private BitButton? _closeButtonRef;
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
    /// business taking the focus off it. The <see cref="BitMessageBoxService"/> defaults it to true
    /// for the message boxes it shows, which is the case the accessibility pattern asks for:
    /// a dialog that interrupts has to put the focus inside itself.
    /// <br/>
    /// <see cref="DefaultButton"/> picks which button it lands on. The button is also marked with the
    /// <c>autofocus</c> attribute the surrounding layer reads, so a message box kept mounted between
    /// showings - a <see cref="BitModal"/> given <see cref="BitModal.KeepMounted"/> - lands the focus on
    /// it on every opening and not only on the first render.
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
    /// is the harmless one, and at <see cref="BitMessageBoxResult.None"/> to land the focus on the
    /// close button instead - which is also where it goes on its own for a message box that renders no
    /// action buttons.
    /// <br/>
    /// A button that is not part of the current <see cref="Buttons"/> set is ignored.
    /// </remarks>
    [Parameter] public BitMessageBoxResult? DefaultButton { get; set; }

    /// <summary>
    /// The template used to render the footer of the message box, which takes the place of its action buttons.
    /// </summary>
    /// <remarks>
    /// The controls in it are the page's own, so nothing in them answers the message box on its own:
    /// <see cref="AnswerAsync"/> is what ends it with an answer of the caller's choosing.
    /// </remarks>
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
    /// The event callback asked before the message box hands over an answer, which is what can refuse to
    /// let it be answered - a form in the body that has not been filled in, or a save that failed.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="BitMessageBoxBeforeResultArgs.Cancel"/> keeps the message box where it is:
    /// none of the callbacks of the answer are raised, and a message box shown through the
    /// <see cref="BitMessageBoxService"/> stays open with its caller still waiting.
    /// <br/>
    /// It guards the buttons the message box draws, the close button included. The Escape key and the
    /// overlay belong to the layer around it, which
    /// <see cref="BitModalParameters.CanClose"/> is the guard for.
    /// <br/>
    /// Turn <see cref="AutoLoading"/> on beside it to keep the pressed button spinning while the guard
    /// works out its answer.
    /// </remarks>
    [Parameter] public EventCallback<BitMessageBoxBeforeResultArgs> OnBeforeResult { get; set; }

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
    /// Answers the message box as though the button standing for that result had been pressed, which is
    /// what lets a footer of your own - or anything else the page decides with - end it with a real answer.
    /// </summary>
    /// <remarks>
    /// The answer takes the same road a pressed button does: <see cref="OnBeforeResult"/> is asked first and
    /// can refuse it, the callback of that answer is raised, then <see cref="OnResult"/> and
    /// <see cref="OnClose"/> - so a message box shown through the <see cref="BitMessageBoxService"/> closes
    /// and hands the answer back to whoever was waiting for it. A disabled message box answers nothing, and
    /// neither does one that is still working out an answer it was already given.
    /// <br/>
    /// The result need not be one of the buttons the current <see cref="Buttons"/> set renders.
    /// </remarks>
    public Task AnswerAsync(BitMessageBoxResult result) => HandleAction(result);

    /// <summary>
    /// Moves the focus onto the default action button of the message box.
    /// </summary>
    /// <remarks>
    /// A message box that renders no action buttons of its own - one given a <see cref="FooterTemplate"/>,
    /// or the <see cref="BitMessageBoxButtons.None"/> set - focuses its close button instead, so that a
    /// dialog which has just taken over the screen still has the focus somewhere inside it. Where there is
    /// neither, the call does nothing.
    /// </remarks>
    public async ValueTask FocusAsync()
    {
        var action = _DefaultAction;

        // A stale ref left behind by a button an earlier Buttons set rendered is not one of the buttons on
        // the screen, so the current set has the last word on what there is to focus.
        if (action is not BitMessageBoxResult.None &&
            FooterTemplate is null &&
            Array.IndexOf(_Actions, action) >= 0 &&
            _buttonRefs.TryGetValue(action, out var button))
        {
            await button.FocusAsync();
            return;
        }

        // The same goes for the close button: a HeaderTemplate takes it off the message box, and so does
        // ShowCloseButton, while the ref that captured it stays behind.
        if (_HasCloseButton && _closeButtonRef is not null)
        {
            await _closeButtonRef.FocusAsync();
        }
    }



    internal string _TitleId => $"{_Id}-ttl";

    internal string _BodyId => $"{_Id}-bdy";

    private string _TitleClass => Classes?.Title.HasValue() is true ? $"bit-msb-ttl {Classes!.Title}" : "bit-msb-ttl";

    private bool _HasBodyTemplate => (BodyTemplate ?? ChildContent) is not null;

    private BitIconInfo? _Icon => BitIconInfo.From(Icon, IconName ?? (Color.HasValue ? _IconMap[Color.Value] : null));

    private bool _HasIcon => HideIcon is false && (IconTemplate is not null || _Icon is not null);

    private bool _HasHeader => HeaderTemplate is not null || _HasIcon || Title.HasValue() || ShowCloseButton;

    private bool _HasBody => _HasBodyTemplate || Body.HasValue();

    private bool _HasCloseButton => ShowCloseButton && HeaderTemplate is null;

    // A footer with nothing in it is an empty row of padding under the body, so the None set leaves it off.
    private bool _HasFooter => FooterTemplate is not null || _Actions.Length > 0;

    private BitIconInfo? _CloseIcon => BitIconInfo.From(CloseIcon, CloseIconName ?? "ChromeClose");

    private string _CloseButtonTitle => CloseButtonTitle ?? "Close";

    // The button the primary look and the default focus go to, which is the affirmative answer of the set.
    private BitMessageBoxResult _PrimaryAction => Buttons is BitMessageBoxButtons.YesNo or BitMessageBoxButtons.YesNoCancel
                                                    ? BitMessageBoxResult.Yes
                                                    : BitMessageBoxResult.Ok;

    // A DefaultButton naming a button this set does not render would leave the focus nowhere, so the
    // primary one stands in for it - and None, which is the close button, is always a button to name.
    // A set with no action buttons in it has nothing but the close button to fall back on.
    private BitMessageBoxResult _DefaultAction => DefaultButton.HasValue &&
                                                  (DefaultButton.Value is BitMessageBoxResult.None || Array.IndexOf(_Actions, DefaultButton.Value) >= 0)
                                                    ? DefaultButton.Value
                                                    : _Actions.Length > 0 ? _PrimaryAction : BitMessageBoxResult.None;

    // What FocusAsync does imperatively, written into the markup: the layer the message box is the content
    // of - a modal, a panel, a callout - focuses the element marked with autofocus every time it opens,
    // which is what a message box that is kept mounted between showings needs. OnAfterRenderAsync's call
    // is what covers the message box rendered on its own, where nothing else is looking for the mark.
    private bool _AutoFocusAction(BitMessageBoxResult action) => AutoFocus && action == _DefaultAction;

    // The fallback FocusAsync falls back to: a set with no action buttons in it, or a footer of the
    // consumer's own, leaves the close button as the only button the message box knows about.
    private bool _AutoFocusCloseButton => AutoFocus &&
                                          (FooterTemplate is not null || _DefaultAction is BitMessageBoxResult.None);

    private BitMessageBoxResult[] _Actions
    {
        get
        {
            var actions = Buttons switch
            {
                BitMessageBoxButtons.OkCancel => _OkCancelActions,
                BitMessageBoxButtons.YesNo => _YesNoActions,
                BitMessageBoxButtons.YesNoCancel => _YesNoCancelActions,
                BitMessageBoxButtons.None => _NoActions,
                _ => _OkActions
            };

            // A reversed set is a new array rather than the shared one reversed in place, which would
            // reverse it again on every render. AsEnumerable is what keeps it that way: on net8/net9
            // an array binds to MemoryExtensions.Reverse (in place, returning void) rather than
            // LINQ's, whose array overload only arrived in net10.
            return Reversed ? [.. actions.AsEnumerable().Reverse()] : actions;
        }
    }

    private static readonly BitMessageBoxResult[] _NoActions = [];
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

        // Only one answer is given per showing: a slow callback leaves every other button pressable, and
        // a message box answered Ok and then Cancel while the first answer is still being worked out is
        // one whose caller is told two different things.
        if (_answering) return;

        _answering = true;

        try
        {
            if (OnBeforeResult.HasDelegate)
            {
                var args = new BitMessageBoxBeforeResultArgs { Result = result };

                await OnBeforeResult.InvokeAsync(args);

                // A refused answer leaves the message box exactly as it was: nothing is reported, nothing
                // is closed, and Result still holds whatever the last answer that went through was.
                if (args.Cancel) return;
            }

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
        finally
        {
            _answering = false;
        }
    }
}
