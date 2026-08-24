namespace Bit.BlazorUI;

/// <summary>
/// SnackBars provide brief notifications. The component is also known as a toast.
/// </summary>
/// <remarks>
/// A single BitSnackBar is the host of every notification a page shows: it is placed once (usually in the layout),
/// captured with <c>@ref</c>, and the code that has something to report calls <see cref="Show(BitSnackBarItem)"/>
/// or one of the per-color shortcuts on it. Each call returns the <see cref="BitSnackBarItem"/> that stands for
/// the notification, which is the handle to close, update, pause or resume it later.
/// <br />
/// Each item announces itself to assistive technology through a live region whose politeness follows its color -
/// the colors that report a problem interrupt the screen reader, the rest wait for a pause - so a snack bar is
/// heard as well as seen. The auto-dismiss countdown pauses while the pointer or the keyboard focus is inside the
/// item (and, with <see cref="PauseOnPageHidden"/>, while the page is in a hidden tab), so a notification is never
/// taken away from someone who is still reading or acting on it.
/// </remarks>
public partial class BitSnackBar : BitComponentBase
{
    private readonly List<BitSnackBarItem> _items = [];
    private readonly Dictionary<Guid, ElementReference> _dismissButtons = [];

    private BitPageVisibility? _pageVisibility;
    private bool _pageHidden;

    /// <summary>
    /// The service provider of the component, used to resolve the optional page visibility utility.
    /// </summary>
    /// <remarks>
    /// <see cref="PauseOnPageHidden"/> needs the shared <see cref="BitPageVisibility"/> utility, which only exists
    /// when the app registered the bit BlazorUI services. Resolving it through the provider rather than injecting it
    /// keeps a snack bar working in an app that never registered them, where a hard dependency would throw at every
    /// render instead of only turning one opt-in feature off.
    /// </remarks>
    [Inject] private IServiceProvider _serviceProvider { get; set; } = default!;



    /// <summary>
    /// The content of the action area of every snack bar item, rendered under its body.
    /// </summary>
    /// <remarks>
    /// An item that carries its own <see cref="BitSnackBarItem.Actions"/> renders that instead.
    /// </remarks>
    [Parameter] public RenderFragment<BitSnackBarItem>? ActionsTemplate { get; set; }

    /// <summary>
    /// Whether or not automatically dismiss the snack bar.
    /// </summary>
    /// <remarks>
    /// The countdown of an item is paused while the pointer or the keyboard focus is inside it, and a persistent
    /// item never takes part in it at all.
    /// </remarks>
    [Parameter] public bool AutoDismiss { get; set; }

    /// <summary>
    /// How long does it take to automatically dismiss the snack bar (default is 3 seconds).
    /// </summary>
    /// <remarks>
    /// A single item can ask for a lifetime of its own through <see cref="BitSnackBarItem.AutoDismissTime"/>.
    /// A value of zero or less turns the countdown off, which leaves the item there until it is dismissed.
    /// <br />
    /// Give a notification that carries anything the user has to read or act on at least five seconds, and give one
    /// that cannot be missed no countdown at all - the default is short enough to be spent before a longer message
    /// has been read.
    /// </remarks>
    [Parameter] public TimeSpan? AutoDismissTime { get; set; }

    /// <summary>
    /// Used to customize how the content inside the body is rendered.
    /// </summary>
    [Parameter] public RenderFragment<string>? BodyTemplate { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the snack bar.
    /// </summary>
    [Parameter] public BitSnackBarClassStyles? Classes { get; set; }

    /// <summary>
    /// The accessible label of the dismiss button (default is "Close").
    /// </summary>
    /// <remarks>
    /// The dismiss button holds nothing but an icon, so without a label of its own a screen reader has nothing to
    /// announce it by. Set this to the word your app uses, translated.
    /// </remarks>
    [Parameter] public string? DismissAriaLabel { get; set; }

    /// <summary>
    /// The icon of the dismiss button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DismissIconName"/> when both are set.
    /// </summary>
    /// <example>
    /// Bootstrap: <c>DismissIcon="BitIconInfo.Bi(\"x-lg\")"</c>
    /// FontAwesome: <c>DismissIcon="BitIconInfo.Fa(\"solid xmark\")"</c>
    /// Custom CSS: <c>DismissIcon="BitIconInfo.Css(\"my-dismiss-icon\")"</c>
    /// </example>
    [Parameter] public BitIconInfo? DismissIcon { get; set; }

    /// <summary>
    /// The icon name of the dismiss button from the built-in Fluent UI icons. If unset, default will be the Fluent UI <c>Cancel</c> icon.
    /// </summary>
    /// <remarks>
    /// For external icon libraries, use <see cref="DismissIcon"/> instead.
    /// </remarks>
    [Parameter] public string? DismissIconName { get; set; }

    /// <summary>
    /// Dismisses a snack bar item when anywhere inside it is clicked.
    /// </summary>
    /// <remarks>
    /// A persistent item stays put: this only reaches the items that could be dismissed in the first place.
    /// <br />
    /// Leave this off while the item holds interactive content of its own - a click on that content dismisses the
    /// item along with it, which is right for an action button and wrong for anything the user is still filling in.
    /// </remarks>
    [Parameter] public bool DismissOnClick { get; set; }

    /// <summary>
    /// Prevents rendering the countdown progress bar of the auto-dismissing snack bars.
    /// </summary>
    [Parameter] public bool HideProgress { get; set; }

    /// <summary>
    /// The leading icon of every snack bar item using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Only rendered while <see cref="ShowIcon"/> is enabled. A single item can ask for an icon of its own through
    /// <see cref="BitSnackBarItem.Icon"/>.
    /// </remarks>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the leading icon of every snack bar item from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// Only rendered while <see cref="ShowIcon"/> is enabled. If unset, the icon of each item is selected
    /// automatically based on its color.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The maximum number of snack bar items to show at once.
    /// </summary>
    /// <remarks>
    /// Showing a new item while the cap is already reached dismisses the oldest one to make room for it, so a burst
    /// of notifications cannot grow into a wall that covers the page. Unset (or zero and below) means no cap.
    /// </remarks>
    [Parameter] public int? MaxItems { get; set; }

    /// <summary>
    /// Enables the multiline mode of both title and body.
    /// </summary>
    /// <remarks>
    /// A single-line title or body that does not fit is cut off with an ellipsis and keeps its full text in a
    /// tooltip; a multiline one wraps instead.
    /// </remarks>
    [Parameter] public bool Multiline { get; set; }

    /// <summary>
    /// Puts the newest snack bar item at the top of the stack instead of the bottom.
    /// </summary>
    [Parameter] public bool NewestOnTop { get; set; }

    /// <summary>
    /// Callback for when any snack bar is dismissed, reporting the item that was dismissed.
    /// </summary>
    [Parameter] public EventCallback<BitSnackBarItem> OnDismiss { get; set; }

    /// <summary>
    /// Callback for when any snack bar item is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitSnackBarItem> OnItemClick { get; set; }

    /// <summary>
    /// Callback for when a new snack bar item is shown.
    /// </summary>
    [Parameter] public EventCallback<BitSnackBarItem> OnShow { get; set; }

    /// <summary>
    /// Pauses the auto-dismiss countdown while the pointer or the keyboard focus is inside a snack bar item
    /// (default is true).
    /// </summary>
    /// <remarks>
    /// This is what keeps a notification from being taken away from someone who is still reading it or reaching for
    /// its action, and it is also how the countdown meets WCAG 2.2.1 (Timing Adjustable) without a longer timeout.
    /// Only turn it off where the snack bar must go away on its own no matter what.
    /// </remarks>
    [Parameter] public bool PauseOnHover { get; set; } = true;

    /// <summary>
    /// Pauses the auto-dismiss countdown of every snack bar item while the page is hidden.
    /// </summary>
    /// <remarks>
    /// A notification that counts down in a background tab is gone before the tab is ever looked at again. Turning
    /// this on holds the countdown until the page is visible, which needs the bit BlazorUI services to be registered
    /// (<c>AddBitBlazorUIServices</c>); without them the snack bar carries on counting down as it would otherwise.
    /// </remarks>
    [Parameter] public bool PauseOnPageHidden { get; set; }

    /// <summary>
    /// Makes the snack bar non-dismissible in UI and removes the dismiss button.
    /// </summary>
    /// <remarks>
    /// A persistent snack bar also opts out of the auto-dismiss countdown, so it stays until the code that opened it
    /// closes it through <see cref="Close(BitSnackBarItem)"/>. A single item can be made persistent on its own
    /// through <see cref="BitSnackBarItem.Persistent"/>.
    /// </remarks>
    [Parameter] public bool Persistent { get; set; }

    /// <summary>
    /// The position of the snack bars to show (default is bottom right).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSnackBarPosition? Position { get; set; }

    /// <summary>
    /// Skips showing a new snack bar while an identical one is already on screen.
    /// </summary>
    /// <remarks>
    /// Two items count as identical when their title, body and color all match. The <c>Show</c> call then returns the
    /// item that is already showing rather than a new one, so the caller still has a handle to it.
    /// </remarks>
    [Parameter] public bool PreventDuplicates { get; set; }

    /// <summary>
    /// A custom ARIA role for every snack bar item, overriding the one its color implies.
    /// </summary>
    /// <remarks>
    /// By default an item whose color reports a problem (Warning, SevereWarning, Error) is announced as an
    /// <c>alert</c> and every other item as a <c>status</c>. A single item can override this through
    /// <see cref="BitSnackBarItem.Role"/>.
    /// </remarks>
    [Parameter] public string? Role { get; set; }

    /// <summary>
    /// Renders a leading icon in each snack bar item, chosen from its color unless one is provided.
    /// </summary>
    [Parameter] public bool ShowIcon { get; set; }

    /// <summary>
    /// The size of the snack bar items.
    /// </summary>
    [Parameter] public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the snack bar.
    /// </summary>
    [Parameter] public BitSnackBarClassStyles? Styles { get; set; }

    /// <summary>
    /// Used to fully customize how a snack bar item is rendered, taking the place of its header, body and actions.
    /// </summary>
    /// <remarks>
    /// The countdown progress bar is still rendered under the template, and the template is responsible for whatever
    /// of the item it wants to show - including a way to dismiss it.
    /// </remarks>
    [Parameter] public RenderFragment<BitSnackBarItem>? Template { get; set; }

    /// <summary>
    /// Used to customize how content inside the title is rendered.
    /// </summary>
    [Parameter] public RenderFragment<string>? TitleTemplate { get; set; }

    /// <summary>
    /// The duration in milliseconds of the enter and exit animations of the snack bar items (default is 200).
    /// </summary>
    /// <remarks>
    /// A dismissed item is kept in the DOM for this long so its exit animation can play, which is also how long a
    /// <see cref="Close(BitSnackBarItem)"/> call takes to complete. Set it to zero to remove the item at once and
    /// skip both animations.
    /// <br />
    /// The duration reaches the stylesheet as a token, so it still collapses under
    /// <c>prefers-reduced-motion: reduce</c> unless the snack bar opts out with <c>ForceAnimation</c>.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int TransitionDuration { get; set; } = 200;

    /// <summary>
    /// The visual variant of the snack bar items.
    /// </summary>
    [Parameter] public BitVariant? Variant { get; set; }



    /// <summary>
    /// The snack bar items that are currently showing, oldest first (or newest first with <see cref="NewestOnTop"/>).
    /// </summary>
    /// <remarks>
    /// This is a snapshot rather than a view of the list the component keeps, so iterating it is safe across an
    /// await - a countdown that dismisses another item in the meantime cannot break the loop.
    /// </remarks>
    public IReadOnlyList<BitSnackBarItem> Items => [.. _items];



    /// <summary>
    /// Shows a new snackbar with Info color.
    /// </summary>
    public Task<BitSnackBarItem> Info(string title, string? body = "", bool persistent = false, TimeSpan? autoDismissTime = null) => Show(title, body, BitColor.Info, persistent: persistent, autoDismissTime: autoDismissTime);

    /// <summary>
    /// Shows a new snackbar with Success color.
    /// </summary>
    public Task<BitSnackBarItem> Success(string title, string? body = "", bool persistent = false, TimeSpan? autoDismissTime = null) => Show(title, body, BitColor.Success, persistent: persistent, autoDismissTime: autoDismissTime);

    /// <summary>
    /// Shows a new snackbar with Warning color.
    /// </summary>
    public Task<BitSnackBarItem> Warning(string title, string? body = "", bool persistent = false, TimeSpan? autoDismissTime = null) => Show(title, body, BitColor.Warning, persistent: persistent, autoDismissTime: autoDismissTime);

    /// <summary>
    /// Shows a new snackbar with SevereWarning color.
    /// </summary>
    public Task<BitSnackBarItem> SevereWarning(string title, string? body = "", bool persistent = false, TimeSpan? autoDismissTime = null) => Show(title, body, BitColor.SevereWarning, persistent: persistent, autoDismissTime: autoDismissTime);

    /// <summary>
    /// Shows a new snackbar with Error color.
    /// </summary>
    public Task<BitSnackBarItem> Error(string title, string? body = "", bool persistent = false, TimeSpan? autoDismissTime = null) => Show(title, body, BitColor.Error, persistent: persistent, autoDismissTime: autoDismissTime);

    /// <summary>
    /// Shows a new snackbar.
    /// </summary>
    public Task<BitSnackBarItem> Show(
        string title,
        string? body = "",
        BitColor color = BitColor.Info,
        string? cssClass = null,
        string? cssStyle = null,
        bool persistent = false,
        TimeSpan? autoDismissTime = null)
    {
        var item = new BitSnackBarItem
        {
            Title = title,
            Body = body,
            Color = color,
            CssClass = cssClass,
            CssStyle = cssStyle,
            Persistent = persistent,
            AutoDismissTime = autoDismissTime
        };

        return Show(item);
    }

    /// <summary>
    /// Shows a new snackbar.
    /// </summary>
    /// <remarks>
    /// Showing an item that is already showing is a no-op that returns it unchanged, and so is showing a duplicate of
    /// one while <see cref="PreventDuplicates"/> is enabled - in which case the item that is already showing comes
    /// back instead of the new one.
    /// </remarks>
    public async Task<BitSnackBarItem> Show(BitSnackBarItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // Every read of the item list happens on the renderer's synchronization context, so a Show called from a
        // background thread (a timer, a hub callback) cannot see it half-way through a change made by a countdown.
        var shown = item;

        await InvokeAsync(async () =>
        {
            if (_items.Contains(item)) return;

            if (PreventDuplicates)
            {
                var duplicate = _items.Find(i => i.Title == item.Title && i.Body == item.Body && i.Color == item.Color);
                if (duplicate is not null)
                {
                    shown = duplicate;
                    return;
                }
            }

            await TrimToMaxItems();

            if (NewestOnTop)
            {
                _items.Insert(0, item);
            }
            else
            {
                _items.Add(item);
            }

            StartCountdown(item);

            StateHasChanged();

            await OnShow.InvokeAsync(item);
        });

        return shown;
    }

    /// <summary>
    /// Closes a snackbar item.
    /// </summary>
    /// <remarks>
    /// The returned task completes once the item has left the DOM, which is after its exit animation
    /// (<see cref="TransitionDuration"/>) has played.
    /// </remarks>
    public Task Close(BitSnackBarItem item) => InvokeAsync(() => DismissAsync(item, animate: true));

    /// <summary>
    /// Closes every snackbar item that is currently showing.
    /// </summary>
    /// <remarks>
    /// The items are taken away at once rather than one exit animation after another, so clearing a full stack does
    /// not take as long as the stack is tall.
    /// </remarks>
    public Task Clear() => InvokeAsync(async () =>
    {
        foreach (var item in _items.ToArray())
        {
            await DismissAsync(item, animate: false);
        }
    });

    /// <summary>
    /// Re-renders a snackbar item after its properties were changed, and restarts its auto-dismiss countdown.
    /// </summary>
    /// <remarks>
    /// This is how a notification is turned into the report of what it was waiting for: keep the item a call to
    /// <c>Show</c> returned, set its title, body and color to the outcome, then hand it back here.
    /// </remarks>
    public Task Update(BitSnackBarItem item) => InvokeAsync(() =>
    {
        if (item is null || _items.Contains(item) is false) return;

        StartCountdown(item);

        StateHasChanged();
    });

    /// <summary>
    /// Pauses the auto-dismiss countdown of a snackbar item.
    /// </summary>
    public Task Pause(BitSnackBarItem item) => InvokeAsync(() =>
    {
        if (PauseItem(item)) StateHasChanged();
    });

    /// <summary>
    /// Resumes the auto-dismiss countdown of a snackbar item that was paused.
    /// </summary>
    /// <remarks>
    /// A countdown is held back for as long as any one reason to hold it back stands, so this does nothing while the
    /// pointer or the keyboard focus is still inside the item or the page is still hidden - the countdown is let go
    /// as soon as the last of those is over.
    /// </remarks>
    public Task Resume(BitSnackBarItem item) => InvokeAsync(() =>
    {
        if (ResumeItem(item)) StateHasChanged();
    });



    protected override string RootElementClass => "bit-snb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Position switch
        {
            BitSnackBarPosition.TopStart => "bit-snb-tst",
            BitSnackBarPosition.TopCenter => "bit-snb-tcn",
            BitSnackBarPosition.TopEnd => "bit-snb-ten",
            BitSnackBarPosition.BottomStart => "bit-snb-bst",
            BitSnackBarPosition.BottomCenter => "bit-snb-bcn",
            BitSnackBarPosition.BottomEnd => "bit-snb-ben",
            _ => "bit-snb-ben"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        // The duration is handed to the stylesheet as the -full token rather than written into the animations
        // directly, so the reduced-motion collapse in the stylesheet can still shorten it (an inline duration
        // would be out of reach of any media query).
        StyleBuilder.Register(() => FormattableString.Invariant($"--bit-snb-dur-full:{Math.Max(0, TransitionDuration)}ms"));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (PauseOnPageHidden is false || _pageVisibility is not null) return;

        // The utility is a scoped service of the library, so it is only there in an app that registered them.
        // Nothing else about the snack bar depends on it, which is why its absence turns this one feature off
        // instead of failing the render.
        _pageVisibility = _serviceProvider?.GetService(typeof(BitPageVisibility)) as BitPageVisibility;
        if (_pageVisibility is null) return;

        _pageVisibility.OnChange += HandlePageVisibilityChange;

        await _pageVisibility.Init();
    }



    private string _DismissAriaLabel => DismissAriaLabel ?? "Close";

    private string _RootAriaLabel => AriaLabel ?? "Notifications";

    private bool IsDismissible(BitSnackBarItem item) => Persistent is false && item.Persistent is false;

    private TimeSpan GetAutoDismissTime(BitSnackBarItem item)
    {
        return item.AutoDismissTime ?? AutoDismissTime ?? TimeSpan.FromSeconds(3);
    }

    private bool IsAutoDismissed(BitSnackBarItem item)
    {
        return AutoDismiss && IsDismissible(item) && GetAutoDismissTime(item) > TimeSpan.Zero;
    }

    private bool ShowProgressBar(BitSnackBarItem item)
    {
        return HideProgress is false && IsAutoDismissed(item);
    }

    private string GetDuration(BitSnackBarItem item)
    {
        return FormattableString.Invariant($"animation-duration:{GetAutoDismissTime(item).TotalSeconds}s");
    }

    private string GetItemRole(BitSnackBarItem item)
    {
        if (item.Role.HasValue()) return item.Role!;
        if (Role.HasValue()) return Role!;

        return item.Color is BitColor.Warning or BitColor.SevereWarning or BitColor.Error ? "alert" : "status";
    }

    // The politeness follows the role rather than being declared beside it, and a role that is not a live one at all
    // (a custom "presentation" or "none", say) gets no aria-live, so asking for such a role really does opt the item
    // out of being announced instead of leaving a live region behind under a non-live role.
    private string? GetItemAriaLive(BitSnackBarItem item) => GetItemRole(item) switch
    {
        "alert" => "assertive",
        "status" or "log" => "polite",
        _ => null
    };

    private string? GetItemAriaAtomic(BitSnackBarItem item) => GetItemAriaLive(item) is null ? null : "true";

    private BitIconInfo? GetIcon(BitSnackBarItem item)
    {
        return BitIconInfo.From(item.Icon ?? Icon, item.IconName ?? IconName ?? _IconMap[item.Color ?? BitColor.Info]);
    }

    private bool IsClickable(BitSnackBarItem item)
    {
        return item.OnClick is not null || OnItemClick.HasDelegate || (DismissOnClick && IsDismissible(item));
    }

    private string GetItemClasses(BitSnackBarItem item)
    {
        var classes = new List<string>(6)
        {
            item.Color switch
            {
                BitColor.Primary => "bit-snb-pri",
                BitColor.Secondary => "bit-snb-sec",
                BitColor.Tertiary => "bit-snb-ter",
                BitColor.Info => "bit-snb-inf",
                BitColor.Success => "bit-snb-suc",
                BitColor.Warning => "bit-snb-wrn",
                BitColor.SevereWarning => "bit-snb-swr",
                BitColor.Error => "bit-snb-err",
                BitColor.PrimaryBackground => "bit-snb-pbg",
                BitColor.SecondaryBackground => "bit-snb-sbg",
                BitColor.TertiaryBackground => "bit-snb-tbg",
                BitColor.PrimaryForeground => "bit-snb-pfg",
                BitColor.SecondaryForeground => "bit-snb-sfg",
                BitColor.TertiaryForeground => "bit-snb-tfg",
                BitColor.PrimaryBorder => "bit-snb-pbr",
                BitColor.SecondaryBorder => "bit-snb-sbr",
                BitColor.TertiaryBorder => "bit-snb-tbr",
                _ => "bit-snb-inf"
            },
            Variant switch
            {
                BitVariant.Fill => "bit-snb-fil",
                BitVariant.Outline => "bit-snb-otl",
                BitVariant.Text => "bit-snb-txt",
                _ => "bit-snb-fil"
            },
            Size switch
            {
                BitSize.Small => "bit-snb-sm",
                BitSize.Medium => "bit-snb-md",
                BitSize.Large => "bit-snb-lg",
                _ => "bit-snb-md"
            }
        };

        if (item._dismissing) classes.Add("bit-snb-dsm");
        if (item._paused) classes.Add("bit-snb-pau");
        if (IsClickable(item)) classes.Add("bit-snb-clk");

        return string.Join(" ", classes);
    }



    private async Task TrimToMaxItems()
    {
        if (MaxItems is not int max || max <= 0) return;

        // The cap counts the items that are still standing, not the ones whose exit animation is playing: those are
        // already on their way out and asking them to leave again does nothing, which would stall the loop. The one
        // that makes room goes without an animation of its own - it is being replaced rather than dismissed, and
        // animating it would leave the new item waiting for the room.
        while (_items.Count(i => i._dismissing is false) >= max)
        {
            var oldest = NewestOnTop
                ? _items.LastOrDefault(i => i._dismissing is false)
                : _items.Find(i => i._dismissing is false);

            if (oldest is null) break;

            await DismissAsync(oldest, animate: false);

            if (_items.Contains(oldest)) break;
        }
    }

    private async Task DismissAsync(BitSnackBarItem item, bool animate, bool focusNext = false)
    {
        if (item is null || _items.Contains(item) is false) return;
        if (item._dismissing) return;

        CancelCountdown(item);

        var duration = Math.Max(0, TransitionDuration);

        if (animate && duration > 0)
        {
            item._dismissing = true;
            StateHasChanged();

            await Task.Delay(duration);

            if (IsDisposed) return;
        }

        item._dismissing = false;

        // An element taken out from under the pointer does not always report the pointer leaving it, so the hover
        // state is cleared here rather than left to say the item is being read after it is gone - which would start
        // the next countdown of a re-shown item paused with nothing to let it go.
        item._hovered = false;

        var index = _items.IndexOf(item);

        if (_items.Remove(item) is false) return;

        _dismissButtons.Remove(item.Id);

        StateHasChanged();

        if (focusNext) await FocusNeighbourAsync(index);

        if (item.OnDismiss is not null) await item.OnDismiss(item);

        await OnDismiss.InvokeAsync(item);
    }

    // A dismiss button that removes itself leaves the keyboard focus on nothing, which sends the next Tab back to
    // the top of the page. Handing the focus to the item that took its place - or, for the last one in the stack,
    // to the one before it - keeps a run of dismissals reachable from the keyboard.
    private ValueTask FocusNeighbourAsync(int index)
    {
        if (_items.Count == 0) return ValueTask.CompletedTask;

        var neighbour = index < _items.Count ? _items[index] : _items[^1];

        // An item that has no dismiss button of its own has nothing here to focus, and the reference kept for it
        // would be pointing at an element that is no longer in the DOM.
        if (IsDismissible(neighbour) is false) return ValueTask.CompletedTask;

        if (_dismissButtons.TryGetValue(neighbour.Id, out var reference) is false) return ValueTask.CompletedTask;

        return reference.Context is null ? ValueTask.CompletedTask : reference.FocusAsync();
    }

    private async Task HandleItemClick(BitSnackBarItem item)
    {
        if (item.OnClick is not null) await item.OnClick(item);

        await OnItemClick.InvokeAsync(item);

        if (DismissOnClick && IsDismissible(item))
        {
            await DismissAsync(item, animate: true);
        }
    }

    // The dismiss button and the Escape key hand the focus on to the next item, which the public Close does not:
    // the code that closes a snack bar of its own accord has no reason to take the focus away from wherever the
    // user has it.
    private Task HandleDismissClick(BitSnackBarItem item) => DismissAsync(item, animate: true, focusNext: true);

    private Task HandleItemKeyDown(KeyboardEventArgs e, BitSnackBarItem item)
    {
        // Escape is what closes the thing that has the focus, and while the focus is inside a snack bar that
        // thing is the snack bar. Only the items that offer a dismiss button answer it, so the key never takes
        // away a persistent notification the app is keeping on screen on purpose.
        if (e.Key != "Escape" || IsDismissible(item) is false) return Task.CompletedTask;

        return DismissAsync(item, animate: true, focusNext: true);
    }

    private void HandleHoverStart(BitSnackBarItem item)
    {
        if (PauseOnHover is false) return;

        item._hovered = true;

        PauseItem(item);
    }

    private void HandleHoverEnd(BitSnackBarItem item)
    {
        // The flag is cleared whatever PauseOnHover says now, and the resume is attempted unconditionally: a snack
        // bar whose PauseOnHover was turned off while the pointer was inside it would otherwise be left paused with
        // nothing to let it go again. Resuming an item that is not paused is a no-op.
        item._hovered = false;

        ResumeItem(item);
    }

    private Task HandlePageVisibilityChange(bool hidden)
    {
        _pageHidden = hidden;

        return InvokeAsync(() =>
        {
            var changed = false;

            foreach (var item in _items.ToArray())
            {
                changed |= hidden ? PauseItem(item) : ResumeItem(item);
            }

            if (changed) StateHasChanged();
        });
    }



    private void StartCountdown(BitSnackBarItem item)
    {
        CancelCountdown(item);

        item._generation++;

        if (IsAutoDismissed(item) is false) return;

        item._remaining = GetAutoDismissTime(item);

        // A countdown handed to an item that is already being read - the pointer inside it, or the page in a hidden
        // tab - starts held back rather than running down behind the reader's back.
        if (_pageHidden || item._hovered)
        {
            item._paused = true;
            return;
        }

        item._paused = false;
        item._dueAt = DateTimeOffset.UtcNow + item._remaining;
        item._cts = new CancellationTokenSource();

        _ = RunCountdownAsync(item, item._cts);
    }

    private async Task RunCountdownAsync(BitSnackBarItem item, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(item._remaining, cts.Token);

            if (cts.IsCancellationRequested || IsDisposed) return;

            await InvokeAsync(() => DismissAsync(item, animate: true));
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        // The countdown runs outside any event handler, so an exception thrown by a consumer's OnDismiss would
        // otherwise be lost with the task nobody awaits. Handing it to the renderer puts it where the exceptions
        // of an event handler go: the error boundary of the app.
        catch (Exception ex) { await DispatchSafelyAsync(ex); }
    }

    private void DismissInBackground(BitSnackBarItem item) => _ = DismissInBackgroundAsync(item);

    private async Task DismissInBackgroundAsync(BitSnackBarItem item)
    {
        try
        {
            await InvokeAsync(() => DismissAsync(item, animate: true));
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex) { await DispatchSafelyAsync(ex); }
    }

    private async Task DispatchSafelyAsync(Exception exception)
    {
        if (IsDisposed) return;

        try
        {
            await DispatchExceptionAsync(exception);
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    private void CancelCountdown(BitSnackBarItem item)
    {
        var cts = item._cts;
        item._cts = null;
        item._paused = false;

        if (cts is null) return;

        cts.Cancel();
        cts.Dispose();
    }

    private bool PauseItem(BitSnackBarItem item)
    {
        if (item is null || item._paused || item._dismissing) return false;
        if (item._cts is null) return false;

        var remaining = item._dueAt - DateTimeOffset.UtcNow;
        item._remaining = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;

        var cts = item._cts;
        item._cts = null;
        cts.Cancel();
        cts.Dispose();

        item._paused = true;

        return true;
    }

    private bool ResumeItem(BitSnackBarItem item)
    {
        if (item is null || item._paused is false || item._dismissing) return false;

        // A countdown is held back for as long as any one reason to hold it back stands, so the page coming back
        // into view does not let go of an item the pointer is still inside, and the pointer leaving does not let go
        // of one whose page is still hidden.
        if (_pageHidden || item._hovered) return false;

        item._paused = false;

        if (IsAutoDismissed(item) is false) return true;

        if (item._remaining <= TimeSpan.Zero)
        {
            DismissInBackground(item);
            return true;
        }

        item._dueAt = DateTimeOffset.UtcNow + item._remaining;
        item._cts = new CancellationTokenSource();

        _ = RunCountdownAsync(item, item._cts);

        return true;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_pageVisibility is not null)
        {
            _pageVisibility.OnChange -= HandlePageVisibilityChange;
            _pageVisibility = null;
        }

        foreach (var item in _items)
        {
            CancelCountdown(item);
        }

        await base.DisposeAsync(disposing);
    }



    private static readonly Dictionary<BitColor, string> _IconMap = new()
    {
        [BitColor.Primary] = "Info",
        [BitColor.Secondary] = "Info",
        [BitColor.Tertiary] = "Info",
        [BitColor.Info] = "Info",
        [BitColor.Success] = "Completed",
        [BitColor.Warning] = "Info",
        [BitColor.SevereWarning] = "Warning",
        [BitColor.Error] = "ErrorBadge",
        [BitColor.PrimaryBackground] = "Info",
        [BitColor.SecondaryBackground] = "Info",
        [BitColor.TertiaryBackground] = "Info",
        [BitColor.PrimaryForeground] = "Info",
        [BitColor.SecondaryForeground] = "Info",
        [BitColor.TertiaryForeground] = "Info",
        [BitColor.PrimaryBorder] = "Info",
        [BitColor.SecondaryBorder] = "Info",
        [BitColor.TertiaryBorder] = "Info"
    };
}
