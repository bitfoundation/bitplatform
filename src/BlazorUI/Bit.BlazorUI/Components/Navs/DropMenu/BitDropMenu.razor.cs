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
    private string? _swipesKey;
    private ElementReference _buttonRef;
    private DotNetObjectReference<BitDropMenu> _dotnetObj = default!;
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
    /// Determines whether the drop menu is in the loading state.
    /// It replaces the icon of the button with a spinner and prevents the callout from being opened.
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
    /// </summary>
    [Parameter] public bool MatchWidth { get; set; }

    /// <summary>
    /// The maximum height of the callout of the drop menu as a CSS value (e.g. "20rem"), beyond which its content scrolls.
    /// </summary>
    [Parameter] public string? MaxHeight { get; set; }

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
    /// Opens the callout of the drop menu programmatically.
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
        await CloseCallout();

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
        await DismissCallout();

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

        // The swipe gestures are registered against the callout with the geometry they were set up
        // with, and all of the inputs of that geometry are parameters that can change at runtime
        // (Responsive itself can be bound to a media query), so re-register whenever any of them does.
        if (IsRendered && GetSwipesKey() != _swipesKey)
        {
            await DisposeSwipes();
            await SetupSwipes();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

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

            await FocusCalloutIfNeeded();
        }
    }



    private async Task HandleOnClick()
    {
        if (IsEnabled is false || IsLoading) return;

        // A click on the trigger while the callout is open usually lands on the overlay above it, but a
        // keyboard activation always arrives here, as does a click when an ancestor stacking context
        // lifts the trigger over the overlay, so activating an open drop menu closes it.
        if (IsOpen)
        {
            await CloseCallout();
        }
        else
        {
            await OpenCallout();
        }

        await OnClick.InvokeAsync();
    }

    private async Task HandleOnButtonKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || IsLoading) return;

        if (e.Key is "Escape")
        {
            if (IsOpen is false) return;

            await CloseCallout();
            StateHasChanged();
        }
        else if (e.Key is "ArrowDown" or "ArrowUp")
        {
            if (IsOpen) return;

            await OpenCallout();
            StateHasChanged();
        }
    }

    private async Task HandleOnCalloutKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || IsOpen is false) return;

        if (e.Key is not "Escape") return;

        await CloseCallout();

        // The close runs on the callout's own event, which does not re-render the button, so refresh
        // the open-state classes and aria-expanded here before handing the focus back to the trigger.
        StateHasChanged();

        await FocusButton();
    }

    private async Task OpenCallout()
    {
        if (IsOpen || IsLoading) return;

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

        await FocusCalloutIfNeeded();

        await OnOpen.InvokeAsync();
    }

    private async Task CloseCallout()
    {
        _selfDrivenIsOpen = true;
        try
        {
            await DismissCallout();
        }
        finally
        {
            _selfDrivenIsOpen = false;
        }

        await ToggleCallout();
    }

    private async Task ToggleCallout()
    {
        if (IsDisposed) return;

        // The reference is created on the first render, so before it there is nothing to position either.
        if (_dotnetObj is null) return;

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
            scrollContainerId: ScrollContainerId ?? "",
            scrollOffset: 0,
            headerId: "",
            footerId: "",
            setCalloutWidth: MatchWidth,
            fixedCalloutWidth: false,
            maxWindowWidth: 0);
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

        _ = ToggleCallout();
    }

    private async Task DismissCallout()
    {
        // AssignIsOpen reports success for a value it did not have to change, so the already-closed
        // case is filtered out here to keep OnDismiss from firing for a dismissal that never happened.
        if (IsOpen is false) return;

        if (await AssignIsOpen(false) is false) return;

        await OnDismiss.InvokeAsync();
    }

    private async Task FocusCalloutIfNeeded()
    {
        if (AutoFocus is false || IsOpen is false || IsDisposed) return;

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
                orientationLock: BitSwipeOrientation.Horizontal,
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
        // The positioning code clears the callout's inline max-height on every layout pass, so the cap
        // travels as a custom property the stylesheet reads instead of as a max-height of its own.
        var maxHeight = MaxHeight.HasValue() ? $"--bit-drm-cal-mxh:{MaxHeight};" : null;

        var result = $"{maxHeight}{Styles?.Callout}";

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

        try
        {
            await _js.BitCalloutClearCallout(_calloutId);
            await _js.BitUtilsDisposePreventDefaultKeys(_buttonId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await DisposeSwipes();

        _dotnetObj?.Dispose();
    }
}
