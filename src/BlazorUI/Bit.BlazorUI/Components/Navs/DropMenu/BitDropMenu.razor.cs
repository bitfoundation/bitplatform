namespace Bit.BlazorUI;

/// <summary>
/// DropMenu component is a versatile dropdown menu used in Blazor applications. It allows you to create a button that,
/// when clicked, opens a callout hosting any content: an action list, a form, a filter panel, or a navigation menu.
/// The callout is positioned against the button, flips to the side with the most room, turns into a swipeable panel on
/// small screens, and closes on an outside click or the Escape key.
/// </summary>
public partial class BitDropMenu : BitComponentBase
{
    private static readonly string[] _scrollingKeys = ["ArrowDown", "ArrowUp"];

    private string _buttonId = default!;
    private string _calloutId = default!;
    private string _overlayId = default!;
    private bool _openOnFirstRender;
    private bool _selfDrivenIsOpen;
    private bool _focusCalloutOnClick;
    private bool _focusTrapped;
    private bool _hoverInside;
    private bool? _isHoverDevice;
    private string? _swipesKey;
    private CancellationTokenSource? _hoverCts;
    private ElementReference _buttonRef;
    private DotNetObjectReference<BitDropMenu>? _dotnetObj;
    private DotNetObjectReference<BitDropMenu>? _swipesDotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The description of the drop menu for the benefit of screen readers, rendered as the aria-describedby of the button.
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, adds an aria-hidden attribute instructing screen readers to ignore the button of the drop menu.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// Closes the callout as soon as a click lands anywhere inside it, which is what an action list is
    /// expected to do: picking an item completes the interaction. It is off by default, since a callout
    /// hosting a form or a filter panel is meant to stay open while it is being used.
    /// </summary>
    [Parameter] public bool AutoClose { get; set; }

    /// <summary>
    /// Moves the focus into the callout as soon as it opens, to its first focusable element,
    /// or to the callout itself when it holds none.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// The color kind of the background of the callout of the drop menu.
    /// </summary>
    [Parameter] public BitColorKind? Background { get; set; }

    /// <summary>
    /// Alias of the ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// The color kind of the border of the callout of the drop menu.
    /// </summary>
    [Parameter] public BitColorKind? Border { get; set; }

    /// <summary>
    /// Gets or sets the icon for the chevron down part of the drop menu using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ChevronDownIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ChevronDownIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: ChevronDownIcon="BitIconInfo.Bi("chevron-down")"
    /// FontAwesome: ChevronDownIcon="BitIconInfo.Fa("solid chevron-down")"
    /// Custom CSS: ChevronDownIcon="BitIconInfo.Css("my-chevron-class")"
    /// </example>
    [Parameter] public BitIconInfo? ChevronDownIcon { get; set; }

    /// <summary>
    /// Gets or sets the icon name for the chevron down part of the drop menu from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set. When null, defaults to "ChevronRight bit-ico-r90".
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// For external icon libraries, use <see cref="ChevronDownIcon"/> instead.
    /// </remarks>
    [Parameter] public string? ChevronDownIconName { get; set; }

    /// <summary>
    /// The content of the callout of the drop menu.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the drop menu.
    /// </summary>
    [Parameter] public BitDropMenuClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the button of the drop menu.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The initial opening state of the callout in the uncontrolled mode, which is when the IsOpen parameter is not set.
    /// </summary>
    [Parameter] public bool? DefaultIsOpen { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the callout of the drop menu.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

    /// <summary>
    /// Expands the drop menu width to 100% of the available width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The delay in milliseconds before the callout closes once the pointer leaves the drop menu in the
    /// <see cref="OpenOnHover"/> mode. It bridges the gap between the button and the callout, so moving the
    /// pointer from one to the other does not close what the pointer is on its way to. Defaults to 150.
    /// </summary>
    [Parameter] public int HoverCloseDelay { get; set; } = 150;

    /// <summary>
    /// The delay in milliseconds before the callout opens once the pointer enters the drop menu in the
    /// <see cref="OpenOnHover"/> mode, so that passing over the button on the way somewhere else does not
    /// open it. Defaults to 0, which opens it as soon as the pointer arrives.
    /// </summary>
    [Parameter] public int HoverOpenDelay { get; set; }

    /// <summary>
    /// Gets or sets the icon to display inside the header of the drop menu using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: Icon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: Icon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: Icon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display inside the header of the drop menu from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.AddFriend</c>).
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Determines whether the drop menu is in the loading state. It replaces the icon of the button with a
    /// spinner and disables the button, so the callout can no longer be opened by the user or by the
    /// <see cref="Open"/> and <see cref="Toggle"/> methods, and a callout that is already open is closed.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IsLoading { get; set; }

    /// <summary>
    /// Determines the opening state of the callout of the drop menu.
    /// </summary>
    [Parameter, CallOnSet(nameof(OnSetIsOpen))]
    [ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Expands the callout of the drop menu to at least the width of the button of the drop menu.
    /// It is applied after the callout is measured, so it takes precedence over <see cref="Width"/>.
    /// </summary>
    [Parameter] public bool MatchWidth { get; set; }

    /// <summary>
    /// The maximum height of the callout of the drop menu as a CSS value (e.g. "20rem"), beyond which its content scrolls.
    /// It takes over from the automatic cap that otherwise keeps the callout within the room the viewport leaves, so it
    /// should stay within what the shortest screen the drop menu is used on can show.
    /// </summary>
    [Parameter] public string? MaxHeight { get; set; }

    /// <summary>
    /// The maximum width of the callout of the drop menu as a CSS value (e.g. "20rem"), beyond which its content wraps.
    /// </summary>
    [Parameter] public string? MaxWidth { get; set; }

    /// <summary>
    /// The minimum width of the callout of the drop menu as a CSS value (e.g. "20rem"), so that a narrow
    /// content does not end up in a cramped callout.
    /// </summary>
    [Parameter] public string? MinWidth { get; set; }

    /// <summary>
    /// Removes the chevron-down icon from the button of the drop menu.
    /// </summary>
    [Parameter] public bool NoChevron { get; set; }

    /// <summary>
    /// Removes the box-shadow from the callout of the drop menu.
    /// </summary>
    [Parameter] public bool NoShadow { get; set; }

    /// <summary>
    /// The callback is called when the drop menu is clicked.
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// The callback is called when the drop menu is dismissed.
    /// </summary>
    [Parameter] public EventCallback OnDismiss { get; set; }

    /// <summary>
    /// The callback is called when the callout of the drop menu is opened.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// Opens the callout when the pointer enters the drop menu and closes it when the pointer leaves it,
    /// which is what a navigation menu is usually expected to do. The button keeps toggling the callout on
    /// a click, so the keyboard and the touch screens - where hovering does not exist and this mode turns
    /// itself off - are left with a way to reach it.
    /// </summary>
    [Parameter] public bool OpenOnHover { get; set; }

    /// <summary>
    /// The position of the responsive panel to show on the screen.
    /// </summary>
    [Parameter] public BitPanelPosition? PanelPosition { get; set; }

    /// <summary>
    /// Renders the drop menu in responsive mode on small screens.
    /// </summary>
    [Parameter] public bool Responsive { get; set; }

    /// <summary>
    /// The id of the element which needs to be scrollable in the content of the callout of the drop menu.
    /// </summary>
    [Parameter] public string? ScrollContainerId { get; set; }

    /// <summary>
    /// The size of the button of the drop menu.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the drop menu.
    /// </summary>
    [Parameter] public BitDropMenuClassStyles? Styles { get; set; }

    /// <summary>
    /// The custom content to render inside the header of the drop menu.
    /// </summary>
    [Parameter] public RenderFragment? Template { get; set; }

    /// <summary>
    /// The text to show inside the header of the drop menu.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the button of the drop menu.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Makes the background of the header of the drop menu transparent.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Transparent { get; set; }

    /// <summary>
    /// Keeps the keyboard inside the callout while it is open: the focus moves into it as it opens, Tab and
    /// Shift+Tab cycle within it instead of running on into the page behind it, and the callout reports
    /// itself as a modal dialog to the screen readers. It is what the callouts that host a form or a filter
    /// panel need, and it implies <see cref="AutoFocus"/>.
    /// </summary>
    [Parameter] public bool TrapFocus { get; set; }

    /// <summary>
    /// The visual variant of the button of the drop menu: filled (the default look), outlined, or text only.
    /// It decides how the <see cref="Color"/> is painted onto the button, so the two are set together.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// The width of the callout of the drop menu as a CSS value (e.g. "20rem"). By default the callout is
    /// only as wide as its content needs. <see cref="MatchWidth"/> takes precedence over it.
    /// </summary>
    [Parameter] public string? Width { get; set; }



    /// <summary>
    /// Opens the callout of the drop menu programmatically, unless the drop menu is disabled or loading.
    /// </summary>
    public async Task Open()
    {
        await OpenCallout();

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Closes the callout of the drop menu programmatically.
    /// </summary>
    public async Task Close()
    {
        // A drop menu that is already closed has nothing to close, and going through with it would reach
        // the JS side to reposition a callout that is not shown.
        if (IsOpen)
        {
            await CloseCallout();
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Toggles the callout of the drop menu programmatically.
    /// </summary>
    public async Task Toggle()
    {
        if (IsOpen)
        {
            await CloseCallout();
        }
        else
        {
            await OpenCallout();
        }

        await InvokeAsync(StateHasChanged);
    }



    [JSInvokable("CloseCallout")]
    public async Task _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        // The callout has already been hidden by the JS side, which is why nothing is toggled here: the
        // state is all that is left to correct, and going back through the positioning code would only
        // hide a callout that is already hidden - and restore one that is already back where it came
        // from. Assigning the state is what would otherwise take that path, so it is suppressed for it.
        // The focus is deliberately left where it is: whatever took over from this callout is about to
        // take it.
        await DisposeFocusTrap();

        _selfDrivenIsOpen = true;
        try
        {
            await DismissCallout();
        }
        finally
        {
            _selfDrivenIsOpen = false;
        }

        StateHasChanged();
    }

    [JSInvokable("OnStart")]
    public Task _OnStart(decimal startX, decimal startY) => Task.CompletedTask;

    [JSInvokable("OnMove")]
    public Task _OnMove(decimal diffX, decimal diffY) => Task.CompletedTask;

    [JSInvokable("OnEnd")]
    public Task _OnEnd(decimal diffX, decimal diffY) => Task.CompletedTask;

    [JSInvokable("OnClose")]
    public async Task _OnClose()
    {
        await CloseCallout();
        await InvokeAsync(StateHasChanged);
    }



    protected override string RootElementClass => "bit-drm";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-drm-pri",
            BitColor.Secondary => "bit-drm-sec",
            BitColor.Tertiary => "bit-drm-ter",
            BitColor.Info => "bit-drm-inf",
            BitColor.Success => "bit-drm-suc",
            BitColor.Warning => "bit-drm-wrn",
            BitColor.SevereWarning => "bit-drm-swr",
            BitColor.Error => "bit-drm-err",
            BitColor.PrimaryBackground => "bit-drm-pbg",
            BitColor.SecondaryBackground => "bit-drm-sbg",
            BitColor.TertiaryBackground => "bit-drm-tbg",
            BitColor.PrimaryForeground => "bit-drm-pfg",
            BitColor.SecondaryForeground => "bit-drm-sfg",
            BitColor.TertiaryForeground => "bit-drm-tfg",
            BitColor.PrimaryBorder => "bit-drm-pbr",
            BitColor.SecondaryBorder => "bit-drm-sbr",
            BitColor.TertiaryBorder => "bit-drm-tbr",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-drm-fil",
            BitVariant.Outline => "bit-drm-otl",
            BitVariant.Text => "bit-drm-tex",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-drm-sm",
            BitSize.Medium => "bit-drm-md",
            BitSize.Large => "bit-drm-lg",
            _ => "bit-drm-md"
        });

        ClassBuilder.Register(() => IsOpen ? "bit-drm-omn" : string.Empty);

        ClassBuilder.Register(() => IsOpen ? Classes?.Opened : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-drm-flw" : string.Empty);

        ClassBuilder.Register(() => IsLoading ? "bit-drm-ldg" : string.Empty);

        ClassBuilder.Register(() => Transparent ? "bit-drm-trn" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => IsOpen ? Styles?.Opened : string.Empty);
    }

    protected override void OnInitialized()
    {
        _buttonId = $"BitDropMenu-{UniqueId}-button";
        _calloutId = $"BitDropMenu-{UniqueId}-callout";
        _overlayId = $"BitDropMenu-{UniqueId}-overlay";

        // The uncontrolled starting state. The callout itself can only be shown once the DOM exists,
        // so the actual opening is deferred to the first render like an initially set IsOpen is.
        if (IsOpenHasBeenSet is false && DefaultIsOpen.HasValue)
        {
            IsOpen = DefaultIsOpen.Value;
        }

        _openOnFirstRender = IsOpen;

        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        await CloseWhenUnavailable();

        // The swipe gestures are registered against the callout with the geometry they were set up
        // with, and all of the inputs of that geometry are parameters that can change at runtime
        // (Responsive itself can be bound to a media query), so re-register whenever any of them does.
        if (IsRendered && GetSwipesKey() != _swipesKey)
        {
            await DisposeSwipes();
            await SetupSwipes();
        }

        // The focus trap is registered against the open callout, so turning it on or off while the callout
        // is open has to reach the already registered one rather than wait for the next time it opens.
        if (IsRendered && IsOpen)
        {
            if (TrapFocus)
            {
                await SetupFocusTrap();
            }
            else
            {
                await DisposeFocusTrap();
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // Whether the pointer of the device can hover at all decides both whether the hover mode applies
        // and whether the overlay may stop taking the clicks, so it is resolved before the drop menu is
        // interacted with rather than on the first hover, and only for the drop menus that ask for it.
        if (OpenOnHover && _isHoverDevice is null)
        {
            _isHoverDevice = await GetIsHoverDevice();

            StateHasChanged();
        }

        if (firstRender is false) return;

        _dotnetObj = DotNetObjectReference.Create(this);

        await SetupSwipes();

        // The keydown handler of the button opens the callout on the arrow keys, whose default
        // behavior (scrolling the page) Blazor cannot suppress per key from the handler itself.
        try
        {
            await _js.BitUtilsPreventDefaultKeys(_buttonId, _scrollingKeys);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        // An IsOpen (or DefaultIsOpen) that starts out true reaches OnSetIsOpen before the first render,
        // when neither the callout element nor the .NET object reference the JS side needs exist yet.
        if (_openOnFirstRender)
        {
            _openOnFirstRender = false;

            await ToggleCallout();

            await SetupFocusTrap();

            await FocusCalloutIfNeeded();
        }
    }



    private async Task HandleOnClick()
    {
        // A key that activates the button records its intent here and the click the browser dispatches
        // for it is the one that acts on it, so that the two do not each toggle the callout in turn.
        var focusCallout = _focusCalloutOnClick;
        _focusCalloutOnClick = false;

        if (IsEnabled is false || IsLoading) return;

        // A click on the trigger while the callout is open usually lands on the overlay above it, but a
        // keyboard activation always arrives here, as does a click when an ancestor stacking context
        // lifts the trigger over the overlay, so activating an open drop menu closes it. The exception is
        // the pointer that opened the callout by hovering and is still on the button: closing here would
        // take away what the user has only just been shown, and moving the pointer off closes it anyway.
        if (IsOpen is false)
        {
            await OpenCallout(focusCallout);
        }
        else if (HoverDriven is false || _hoverInside is false)
        {
            await CloseCallout();
        }

        // Not every engine focuses a button it has just dispatched a click for - WebKit leaves the focus
        // where it was - and a drop menu whose trigger never took the focus is a drop menu without its
        // Escape key, since the handler that closes the callout on it sits on the trigger. Doing it here
        // is what the engines that focus it have already done, so nothing moves for them. It is left
        // alone where the callout is the one taking the focus, which is the case an activation from the
        // keyboard and the AutoFocus and TrapFocus modes each ask for.
        if (focusCallout is false && AutoFocus is false && TrapFocus is false)
        {
            await FocusButton();
        }

        await OnClick.InvokeAsync();
    }

    private async Task HandleOnButtonKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || IsLoading) return;

        // Escape dismisses the callout, and so does tabbing off the trigger: the callout is relocated to the
        // end of the body while it is open, so the tab sequence runs from the trigger on into the page
        // behind it rather than into the content, and a callout left open there would float over a page the
        // keyboard has already moved on from, with an overlay under it swallowing every click. Closing on
        // Tab is also what a menu button is expected to do; the focus itself is left to the browser to move.
        if (e.Key is "Escape" or "Tab")
        {
            if (IsOpen is false) return;

            await CloseCallout();
            StateHasChanged();
        }
        else if (e.Key is "Enter" or " " or "Spacebar")
        {
            // Activating a menu button from the keyboard hands the focus over to what it opens, unlike a
            // click, which leaves the focus where the pointer put it. The opening itself is left to the
            // click the browser dispatches for these keys, so the two do not each toggle the callout.
            if (IsOpen) return;

            _focusCalloutOnClick = true;
        }
        else if (e.Key is "ArrowDown" or "ArrowUp")
        {
            // The arrow keys are how the keyboard reaches the content of a menu button, so unlike a click
            // they always hand the focus over to it, whether or not the drop menu was asked to do so.
            if (IsOpen)
            {
                // The callout is already open, which is the state an arrow key from the trigger reaches
                // when the pointer opened it: the content is showing but the keyboard is still outside it.
                await FocusCalloutIfNeeded(force: true);
                return;
            }

            await OpenCallout(focusCallout: true);
            StateHasChanged();
        }
    }

    private async Task HandleOnCalloutClick()
    {
        if (AutoClose is false || IsEnabled is false || IsOpen is false) return;

        await CloseCallout();

        // The close runs on the callout's own event, which does not re-render the button, so refresh
        // the open-state classes and aria-expanded here.
        StateHasChanged();
    }

    private async Task HandleOnCalloutKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || IsOpen is false) return;

        if (e.Key is not "Escape") return;

        // The focus is inside the callout, so closing it hands the focus back to the trigger on its own.
        await CloseCallout();

        // The close runs on the callout's own event, which does not re-render the button, so refresh
        // the open-state classes and aria-expanded here.
        StateHasChanged();
    }

    private async Task HandleOnMouseEnter()
    {
        if (HoverDriven is false) return;

        _hoverInside = true;

        // Whichever of the two is pending: entering the callout cancels the close the pointer leaving the
        // button scheduled, and coming back to the button cancels the close leaving the callout scheduled.
        CancelHover();

        if (IsEnabled is false || IsLoading || IsOpen) return;

        if (await DelayHover(HoverOpenDelay) is false) return;

        await OpenCallout();

        StateHasChanged();
    }

    private async Task HandleOnMouseLeave()
    {
        if (HoverDriven is false) return;

        _hoverInside = false;

        CancelHover();

        if (IsEnabled is false || IsOpen is false) return;

        if (await DelayHover(HoverCloseDelay) is false) return;

        // The pointer came back before the delay was up, onto the button or into the callout.
        if (_hoverInside) return;

        await CloseCallout();

        StateHasChanged();
    }

    private async Task OpenCallout(bool focusCallout = false)
    {
        // A drop menu the user cannot reach must not be opened by the Open and Toggle methods either,
        // since the callout would then hang over the page with a disabled trigger under it. An IsOpen
        // the parent sets itself is left alone: the state is the parent's to own there.
        if (IsOpen || IsEnabled is false || IsLoading) return;

        // Assigning IsOpen runs OnSetIsOpen, which is the entry point for the open state changing from
        // the outside and toggles the callout on its own. Here the toggling is done below instead, once
        // the assignment is known to have gone through, so it is suppressed for the assignment itself.
        _selfDrivenIsOpen = true;
        try
        {
            if (await AssignIsOpen(true) is false) return;
        }
        finally
        {
            _selfDrivenIsOpen = false;
        }

        await ToggleCallout();

        await SetupFocusTrap();

        await FocusCalloutIfNeeded(focusCallout);

        await OnOpen.InvokeAsync();
    }

    // A drop menu that is turned off or put into the loading state while its callout is open would leave
    // it hanging over the page with a disabled trigger under it, and in the hover mode it would be stuck
    // there: a disabled root takes no pointer events, so the pointer leaving it never closes it again.
    private async Task CloseWhenUnavailable()
    {
        if (IsOpen is false) return;

        if (IsEnabled && IsLoading is false) return;

        if (IsRendered)
        {
            await CloseCallout();
            return;
        }

        // Before the first render there is no callout to hide, only the state to correct.
        _openOnFirstRender = false;

        _selfDrivenIsOpen = true;
        try
        {
            await AssignIsOpen(false);
        }
        finally
        {
            _selfDrivenIsOpen = false;
        }
    }

    private async Task CloseCallout()
    {
        var wasOpen = IsOpen;

        // Whether the focus is the callout's to hand back has to be known before the callout is hidden,
        // since hiding the element the focus sits in is what drops the focus to the body.
        var restoreFocus = wasOpen && await CalloutContainsFocus();

        _selfDrivenIsOpen = true;
        try
        {
            await DismissCallout();
        }
        finally
        {
            _selfDrivenIsOpen = false;
        }

        // An IsOpen the parent holds at true without a change callback stays open: toggling the callout
        // here would only replay the entry animation of a callout that is not going anywhere.
        if (wasOpen && IsOpen) return;

        await DisposeFocusTrap();

        await ToggleCallout();

        // The element the focus was on is gone with the callout, which would leave the focus on the body
        // and the keyboard back at the top of the page, so it goes back to the trigger it came from.
        if (restoreFocus)
        {
            await FocusButton();
        }
    }

    private async Task ToggleCallout()
    {
        if (IsDisposed) return;

        // The reference is created on the first render, so before it there is nothing to position either.
        if (_dotnetObj is null) return;

        try
        {
            await _js.BitCalloutToggleCallout(
                dotnetObj: _dotnetObj,
                componentId: _Id,
                component: null,
                calloutId: _calloutId,
                callout: null,
                overlayId: _overlayId,
                isCalloutOpen: IsOpen,
                responsiveMode: Responsive ? BitResponsiveMode.Panel : BitResponsiveMode.None,
                dropDirection: DropDirection,
                isRtl: Dir is BitDir.Rtl,
                // Whatever is named as the scrollable part of the content is what the positioning code caps to
                // the room the viewport leaves. With nothing named, the callout itself takes that role, so that
                // content taller than the screen scrolls inside the callout instead of running off the bottom
                // of it, where a fixed-positioned element leaves it out of reach of the page's own scrolling.
                scrollContainerId: ScrollContainerId.HasValue() ? ScrollContainerId! : (FitsToViewport ? _calloutId : ""),
                scrollOffset: 0,
                headerId: "",
                footerId: "",
                setCalloutWidth: MatchWidth,
                fixedCalloutWidth: false,
                maxWindowWidth: 0);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private void OnSetIsOpen()
    {
        // The open/close path of the component toggles the callout itself, right after the assignment.
        if (_selfDrivenIsOpen) return;

        // Before the first render the callout element does not exist yet; OnAfterRenderAsync opens it.
        if (IsRendered is false)
        {
            _openOnFirstRender = IsOpen;
            return;
        }

        _ = ToggleCalloutFromOutside();
    }

    // The open state changing from the outside goes through the same steps the component's own open and
    // close path does, so that a drop menu driven by its IsOpen parameter alone still hands the keyboard
    // over to its content and still keeps it there.
    private async Task ToggleCalloutFromOutside()
    {
        if (IsOpen)
        {
            await ToggleCallout();

            await SetupFocusTrap();

            await FocusCalloutIfNeeded();
        }
        else
        {
            await DisposeFocusTrap();

            await ToggleCallout();
        }
    }

    private async Task DismissCallout()
    {
        // AssignIsOpen reports success for a value it did not have to change, so the already-closed
        // case is filtered out here to keep OnDismiss from firing for a dismissal that never happened.
        if (IsOpen is false) return;

        if (await AssignIsOpen(false) is false) return;

        await OnDismiss.InvokeAsync();
    }

    private async Task FocusCalloutIfNeeded(bool force = false)
    {
        // A trapped callout has to hold the focus to trap it: leaving it on the trigger would let the very
        // first Tab out of the callout, since the trap only ever sees the keys pressed inside of it.
        if ((force || AutoFocus || TrapFocus) is false || IsOpen is false || IsDisposed) return;

        if (_dotnetObj is null) return;

        try
        {
            await _js.BitUtilsFocusFirstElement(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task FocusButton()
    {
        if (IsDisposed) return;

        try
        {
            await _buttonRef.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task<bool> CalloutContainsFocus()
    {
        // Before the first render there is neither a callout nor a JS side to ask about it.
        if (IsDisposed || _dotnetObj is null) return false;

        try
        {
            return await _js.BitUtilsContainsActiveElement(_calloutId);
        }
        catch (JSDisconnectedException) { return false; } // we can ignore this exception here
    }

    private async Task<bool> GetIsHoverDevice()
    {
        try
        {
            return await _js.BitUtilsIsHoverDevice();
        }
        catch (JSDisconnectedException) { return false; } // we can ignore this exception here
    }

    // The hover mode only applies to the devices that have a pointer to hover with: a tap on a touch
    // screen reports a mouseover of its own, which would fight the click that is meant to toggle the menu.
    private bool HoverDriven => OpenOnHover && _isHoverDevice is true;

    // Whether the callout is the one that has to be kept within the viewport. A named scroll container is
    // the consumer taking that over, a max height is the consumer capping it by hand, and a responsive
    // drop menu is a panel sized against the screen on exactly the screens where the callout would not fit.
    private bool FitsToViewport => Responsive is false && MaxHeight.HasValue() is false && ScrollContainerId.HasValue() is false;

    private void CancelHover()
    {
        var cts = _hoverCts;
        if (cts is null) return;

        _hoverCts = null;
        cts.Cancel();
        cts.Dispose();
    }

    // Waits out the hover delay and reports whether the wait is still the one that matters: the pointer
    // moving again cancels it, and the drop menu may be gone by the time it is over.
    private async Task<bool> DelayHover(int delay)
    {
        if (delay <= 0) return IsDisposed is false;

        var cts = new CancellationTokenSource();
        _hoverCts = cts;

        try
        {
            await Task.Delay(delay, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        if (ReferenceEquals(_hoverCts, cts) is false) return false;

        _hoverCts = null;
        cts.Dispose();

        return IsDisposed is false;
    }

    private async Task SetupFocusTrap()
    {
        if (TrapFocus is false || _focusTrapped || IsDisposed || _dotnetObj is null) return;

        _focusTrapped = true;

        try
        {
            await _js.BitUtilsSetupFocusTrap(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task DisposeFocusTrap()
    {
        if (_focusTrapped is false) return;

        _focusTrapped = false;

        try
        {
            await _js.BitUtilsDisposeFocusTrap(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // The geometry the swipe gestures were registered with, or null when there are none to register.
    private string? GetSwipesKey()
    {
        return Responsive is false ? null : $"{PanelPosition}|{Dir}|{ScrollContainerId}";
    }

    private async Task SetupSwipes()
    {
        if (Responsive is false || IsDisposed) return;

        _swipesKey = GetSwipesKey();

        // Swipes.dispose releases the .NET reference it was handed, so the gestures get one of their
        // own instead of the one the callout positioning keeps using for the life of the component.
        _swipesDotnetObj = DotNetObjectReference.Create(this);

        try
        {
            await _js.BitSwipesSetup(
                id: _calloutId,
                trigger: 0.25m,
                position: PanelPosition ?? BitPanelPosition.End,
                isRtl: Dir is BitDir.Rtl,
                // The axis the panel is swiped away along is the one it slid in on, and the lock is what
                // takes that axis from the page: a top or bottom panel dragged with the wrong lock follows
                // the finger while the page scrolls out from under it at the same time.
                orientationLock: PanelPosition is BitPanelPosition.Top or BitPanelPosition.Bottom
                                    ? BitSwipeOrientation.Vertical
                                    : BitSwipeOrientation.Horizontal,
                dotnetObj: _swipesDotnetObj,
                isResponsive: true,
                scrollContainerId: ScrollContainerId ?? "");
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task DisposeSwipes()
    {
        if (_swipesKey is null) return;

        _swipesKey = null;

        try
        {
            await _js.BitSwipesDispose(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        // Swipes.setup bails out on the screens the responsive mode does not apply to, leaving nothing
        // for Swipes.dispose to release, so the reference is also released here (disposing is idempotent).
        _swipesDotnetObj?.Dispose();
        _swipesDotnetObj = null;
    }

    private string? GetCalloutStyles()
    {
        // The positioning code clears the callout's inline sizing on every layout pass, so the caps travel
        // as custom properties the stylesheet reads instead of as declarations of their own.
        var maxHeight = MaxHeight.HasValue() ? $"--bit-drm-cal-mxh:{MaxHeight};" : null;
        var width = Width.HasValue() ? $"--bit-drm-cal-wid:{Width};" : null;
        var minWidth = MinWidth.HasValue() ? $"--bit-drm-cal-mnw:{MinWidth};" : null;
        var maxWidth = MaxWidth.HasValue() ? $"--bit-drm-cal-mxw:{MaxWidth};" : null;

        var result = $"{maxHeight}{width}{minWidth}{maxWidth}{Styles?.Callout}";

        return result.HasValue() ? result : null;
    }

    private string GetCalloutCssClasses()
    {
        List<string> classes = ["bit-drm-cal"];

        if (IsOpen)
        {
            classes.Add("bit-drm-ocl");
        }

        // While open the callout is relocated to the body, which takes it out of the subtree that carries
        // the root's bit-fam class, so ForceAnimation has to be rendered on the callout itself for its
        // opening animation to opt out of reduced motion.
        if (ForceAnimation)
        {
            classes.Add("bit-fam");
        }

        if (Responsive)
        {
            classes.Add("bit-drm-res");

            classes.Add(PanelPosition switch
            {
                BitPanelPosition.Start => "bit-drm-sta",
                BitPanelPosition.Top => "bit-drm-top",
                BitPanelPosition.Bottom => "bit-drm-btm",
                _ => "bit-drm-end"
            });
        }

        if (NoShadow)
        {
            classes.Add("bit-drm-nsh");
        }

        if (MaxHeight.HasValue())
        {
            classes.Add("bit-drm-mxh");
        }

        if (FitsToViewport)
        {
            classes.Add("bit-drm-fit");
        }

        var backgroundClass = Background switch
        {
            BitColorKind.Primary => "bit-drm-bpg",
            BitColorKind.Secondary => "bit-drm-bsg",
            BitColorKind.Tertiary => "bit-drm-btg",
            BitColorKind.Transparent => "bit-drm-brg",
            _ => ""
        };

        if (backgroundClass.HasValue())
        {
            classes.Add(backgroundClass);
        }

        var borderClass = Border switch
        {
            BitColorKind.Primary => "bit-drm-brd bit-drm-bpr",
            BitColorKind.Secondary => "bit-drm-brd bit-drm-bsr",
            BitColorKind.Tertiary => "bit-drm-brd bit-drm-btr",
            BitColorKind.Transparent => "bit-drm-brd bit-drm-brr",
            _ => ""
        };

        if (borderClass.HasValue())
        {
            classes.Add(borderClass);
        }

        if (Dir is BitDir.Rtl)
        {
            classes.Add("bit-drm-rtl");
        }

        if (Classes?.Callout is not null)
        {
            classes.Add(Classes.Callout);
        }

        return string.Join(' ', classes).Trim();
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        CancelHover();

        try
        {
            await _js.BitCalloutClearCallout(_calloutId);
            await _js.BitUtilsDisposePreventDefaultKeys(_buttonId);
            await _js.BitUtilsDisposeFocusTrap(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await DisposeSwipes();

        _dotnetObj?.Dispose();
    }
}
