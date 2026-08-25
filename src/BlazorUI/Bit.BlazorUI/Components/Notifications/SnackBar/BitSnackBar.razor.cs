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
/// Every item is announced to assistive technology through a live region that was already on the page - the only
/// kind a screen reader reliably watches - whose politeness follows the color of the item: the colors that report a
/// problem interrupt the screen reader, the rest wait for a pause. The auto-dismiss countdown pauses while the
/// pointer or the keyboard focus is inside the item (and, with <see cref="PauseOnPageHidden"/> and
/// <see cref="PauseOnWindowBlur"/>, while the page is not being looked at), so a notification is never taken away
/// from someone who is still reading or acting on it, and <see cref="Hotkey"/> is what puts a keyboard user inside
/// one before it is gone.
/// </remarks>
public partial class BitSnackBar : BitComponentBase
{
    private readonly List<BitSnackBarItem> _items = [];
    private readonly List<BitSnackBarItem> _queue = [];
    private readonly Dictionary<Guid, ElementReference> _dismissButtons = [];
    private readonly List<(Guid Id, string Text)> _politeAnnouncements = [];
    private readonly List<(Guid Id, string Text)> _assertiveAnnouncements = [];

    private BitPageVisibility? _pageVisibility;
    private bool _pageHidden;
    private bool _windowBlurred;
    private string? _registeredHotkey;
    private string? _registeredHotkeyId;
    private string? _registeredSwipeId;
    private int _registeredSwipeThreshold;
    private DotNetObjectReference<BitSnackBar>? _dotnetObj;

    /// <summary>
    /// The service provider of the component, used to resolve the optional page visibility utility.
    /// </summary>
    /// <remarks>
    /// <see cref="PauseOnPageHidden"/> and <see cref="PauseOnWindowBlur"/> need the shared
    /// <see cref="BitPageVisibility"/> utility, which only exists when the app registered the bit BlazorUI services.
    /// Resolving it through the provider rather than injecting it keeps a snack bar working in an app that never
    /// registered them, where a hard dependency would throw at every render instead of only turning two opt-in
    /// features off.
    /// </remarks>
    [Inject] private IServiceProvider _serviceProvider { get; set; } = default!;

    [Inject] private IJSRuntime _js { get; set; } = default!;



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
    /// Prevents rendering the dismiss button of the snack bar items, without making them persistent.
    /// </summary>
    /// <remarks>
    /// The item still goes away on its own, still answers Escape and a swipe, and still takes a
    /// <see cref="Close(BitSnackBarItem)"/> - it just has no button of its own, which is the shape Material gives a
    /// snack bar that carries an action instead. Use <see cref="Persistent"/> where the item must not be dismissed
    /// at all. A single item can drop its button through <see cref="BitSnackBarItem.HideDismissButton"/>.
    /// <br />
    /// Leave the button in place on an item that does not dismiss itself either, or there is no way to be rid of it.
    /// </remarks>
    [Parameter] public bool HideDismissButton { get; set; }

    /// <summary>
    /// Prevents rendering the countdown progress bar of the auto-dismissing snack bars.
    /// </summary>
    [Parameter] public bool HideProgress { get; set; }

    /// <summary>
    /// The keyboard shortcut that moves the focus to the snack bar region, as a list of
    /// <see href="https://developer.mozilla.org/docs/Web/API/KeyboardEvent/code">KeyboardEvent.code</see> values.
    /// </summary>
    /// <remarks>
    /// A notification is at the end of the document and takes itself away again, so a keyboard user has no
    /// reliable way to reach one before it is gone. A shortcut that jumps to the region is what makes the actions
    /// inside a snack bar operable from the keyboard at all, and it is why Radix Toast ships the same feature with
    /// <c>F8</c> as its default. This is off unless a shortcut is given; pass <c>["F8"]</c> for that default.
    /// <br />
    /// The modifiers are written as their property names - <c>["KeyT", "altKey"]</c> is Alt+T - and the shortcut
    /// needs the bit BlazorUI script to be on the page. Say which shortcut it is in <c>AriaLabel</c>
    /// (for example <c>"Notifications (F8)"</c>) so it can be discovered by the people it is for.
    /// </remarks>
    [Parameter] public string[]? Hotkey { get; set; }

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
    /// Showing a new item while the cap is already reached dismisses the oldest one to make room for it - or holds
    /// the new one back until there is room, with <see cref="Overflow"/> - so a burst of notifications cannot grow
    /// into a wall that covers the page. Unset (or zero and below) means no cap.
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
    /// The distance the stack keeps from the edges of the screen (default is the theme's own inset).
    /// </summary>
    /// <remarks>
    /// Any CSS length, and it is what a page with a fixed app bar or a bottom navigation moves its notifications
    /// clear of. It reaches the stylesheet as the <c>--bit-snb-off</c> custom property.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Offset { get; set; }

    /// <summary>
    /// What to do with a new snack bar item that arrives while <see cref="MaxItems"/> is already reached.
    /// </summary>
    /// <remarks>
    /// Only in effect while <see cref="MaxItems"/> caps the stack.
    /// </remarks>
    [Parameter] public BitSnackBarOverflow Overflow { get; set; }

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
    /// Pauses the auto-dismiss countdown of every snack bar item while the window does not have the focus.
    /// </summary>
    /// <remarks>
    /// A window that another one is covering, or whose focus went to the dev tools, is not hidden - the page
    /// visibility API says nothing about it, so <see cref="PauseOnPageHidden"/> alone lets a notification count
    /// down behind whatever is in front of it. This is the same guard react-toastify calls
    /// <c>pauseOnFocusLoss</c>. It needs the bit BlazorUI services to be registered
    /// (<c>AddBitBlazorUIServices</c>); without them the snack bar carries on counting down as it would otherwise.
    /// </remarks>
    [Parameter] public bool PauseOnWindowBlur { get; set; }

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
    /// <br />
    /// The role is what picks the live region the item is announced through - <c>alert</c> interrupts the screen
    /// reader, <c>status</c> and <c>log</c> wait for a pause - and a role that is not a live one at all
    /// (<c>presentation</c>, say) opts the item out of being announced.
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
    /// Lets a snack bar item be dragged out of the way with the pointer, in either inline direction.
    /// </summary>
    /// <remarks>
    /// This is how a notification is thrown away on a touch screen, where the dismiss button is a small target and
    /// the pointer is a thumb. A persistent item is not swipeable, for the same reason it has no dismiss button.
    /// It needs the bit BlazorUI script to be on the page.
    /// </remarks>
    [Parameter] public bool SwipeToDismiss { get; set; }

    /// <summary>
    /// How far a snack bar item has to be dragged before it is dismissed, in pixels (default is 50).
    /// </summary>
    /// <remarks>
    /// Only in effect while <see cref="SwipeToDismiss"/> is enabled. A drag that stops short of this springs the
    /// item back where it was.
    /// </remarks>
    [Parameter] public int SwipeThreshold { get; set; } = 50;

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
    /// The snack bar items that are waiting for room on screen, oldest first.
    /// </summary>
    /// <remarks>
    /// Only ever holds anything while <see cref="MaxItems"/> caps the stack and <see cref="Overflow"/> is
    /// <see cref="BitSnackBarOverflow.Queue"/>. Like <see cref="Items"/> this is a snapshot.
    /// </remarks>
    public IReadOnlyList<BitSnackBarItem> Queued => [.. _queue];



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
    /// <br />
    /// With <see cref="Overflow"/> set to <see cref="BitSnackBarOverflow.Queue"/>, an item that arrives while
    /// <see cref="MaxItems"/> is reached is returned having been put in the queue rather than shown: it is in
    /// <see cref="Queued"/>, and its <see cref="OnShow"/> fires when it reaches the screen.
    /// </remarks>
    public async Task<BitSnackBarItem> Show(BitSnackBarItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (IsDisposed) return item;

        // Every read of the item list happens on the renderer's synchronization context, so a Show called from a
        // background thread (a timer, a hub callback) cannot see it half-way through a change made by a countdown.
        var shown = item;

        await InvokeAsync(async () =>
        {
            if (_items.Contains(item) || _queue.Contains(item)) return;

            if (PreventDuplicates)
            {
                var duplicate = _items.Find(i => IsDuplicate(i, item)) ?? _queue.Find(i => IsDuplicate(i, item));
                if (duplicate is not null)
                {
                    shown = duplicate;
                    return;
                }
            }

            // A queued item is not on screen and has not been announced, so nothing of what showing one does
            // happens here - not the countdown, not the render, not OnShow. It all happens in PumpQueueAsync
            // once there is room for it.
            if (Overflow is BitSnackBarOverflow.Queue && IsFull())
            {
                _queue.Add(item);
                return;
            }

            await TrimToMaxItems();

            await ShowNowAsync(item);
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
    public Task Close(BitSnackBarItem item)
    {
        if (IsDisposed) return Task.CompletedTask;

        return InvokeAsync(async () =>
        {
            // An item that is still waiting for room was never shown, so it is taken out of the queue rather than
            // dismissed: its OnShow never fired, and firing its OnDismiss alone would leave a consumer counting
            // one more notification gone than arrived.
            if (_queue.Remove(item))
            {
                item.DismissReason = BitSnackBarDismissReason.Close;
                return;
            }

            await DismissAsync(item, BitSnackBarDismissReason.Close, animate: true);
        });
    }

    /// <summary>
    /// Closes every snackbar item that is currently showing, and drops whatever is waiting for room.
    /// </summary>
    /// <remarks>
    /// The items are taken away at once rather than one exit animation after another, so clearing a full stack does
    /// not take as long as the stack is tall. The returned task completes once nothing is left, including the items
    /// whose exit animation was already playing when the call arrived.
    /// <br />
    /// The items that were still waiting for room are dropped without their dismiss callbacks, for the same reason
    /// <see cref="Close(BitSnackBarItem)"/> drops one: they were never shown, so their <see cref="OnShow"/> never
    /// fired either.
    /// </remarks>
    public Task Clear()
    {
        if (IsDisposed) return Task.CompletedTask;

        return InvokeAsync(async () =>
        {
            foreach (var item in _queue.ToArray())
            {
                item.DismissReason = BitSnackBarDismissReason.Clear;
            }

            _queue.Clear();

            foreach (var item in _items.ToArray())
            {
                await DismissAsync(item, BitSnackBarDismissReason.Clear, animate: false);
            }
        });
    }

    /// <summary>
    /// Re-renders a snackbar item after its properties were changed, and restarts its auto-dismiss countdown.
    /// </summary>
    /// <remarks>
    /// This is how a notification is turned into the report of what it was waiting for: keep the item a call to
    /// <c>Show</c> returned, set its title, body and color to the outcome, then hand it back here.
    /// <br />
    /// An item whose exit animation is already playing is past the point of being updated, so this leaves it alone
    /// rather than handing a countdown to something that is on its way out.
    /// </remarks>
    public Task Update(BitSnackBarItem item)
    {
        if (IsDisposed) return Task.CompletedTask;

        return InvokeAsync(() =>
        {
            if (item is null || _items.Contains(item) is false) return;
            if (item._dismissing) return;

            // An updated item is a new report - "Uploading" became "Upload complete" - so it is announced again.
            // Without this the change is only ever seen, never heard.
            Announce(item);

            StartCountdown(item);

            StateHasChanged();
        });
    }

    /// <summary>
    /// Moves the keyboard focus to the snack bar region.
    /// </summary>
    /// <remarks>
    /// This is what <see cref="Hotkey"/> does, offered on its own for an app that already has a shortcut
    /// registry of its own to hang it off. Focusing the region puts the next Tab inside the notifications, which
    /// is how their dismiss buttons and actions are reached from the keyboard.
    /// </remarks>
    public ValueTask FocusAsync()
    {
        return RootElement.Context is null ? ValueTask.CompletedTask : RootElement.FocusAsync();
    }

    /// <summary>
    /// Pauses the auto-dismiss countdown of a snackbar item.
    /// </summary>
    /// <remarks>
    /// A hold the code asked for is only let go by <see cref="Resume(BitSnackBarItem)"/>: the pointer leaving the
    /// item, or the page coming back into view, does not let go of it.
    /// </remarks>
    public Task Pause(BitSnackBarItem item)
    {
        if (IsDisposed) return Task.CompletedTask;

        return InvokeAsync(() =>
        {
            if (item is null) return;

            item._userPaused = true;

            if (PauseItem(item)) StateHasChanged();
        });
    }

    /// <summary>
    /// Resumes the auto-dismiss countdown of a snackbar item that was paused.
    /// </summary>
    /// <remarks>
    /// A countdown is held back for as long as any one reason to hold it back stands, so this does nothing while the
    /// pointer or the keyboard focus is still inside the item or the page is still hidden - the countdown is let go
    /// as soon as the last of those is over.
    /// </remarks>
    public Task Resume(BitSnackBarItem item)
    {
        if (IsDisposed) return Task.CompletedTask;

        return InvokeAsync(() =>
        {
            if (item is null) return;

            item._userPaused = false;

            if (ResumeItem(item)) StateHasChanged();
        });
    }



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

        StyleBuilder.Register(() => Offset.HasValue() ? $"--bit-snb-off:{Offset}" : string.Empty);
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // A cap that is raised - or an Overflow that is turned on - has to let whatever is waiting through, and
        // neither of them goes through Show.
        if (_queue.Count > 0) await PumpQueueAsync();

        // The same for a PauseOnPageHidden or PauseOnWindowBlur that is turned off again while it is holding
        // everything back: the next visibility event might never come, so the hold is re-evaluated here.
        if (_items.Exists(i => i._paused) || _PageHeld) await SyncPageHoldAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        await SyncHotkeyAsync();

        await SyncSwipeAsync();

        if ((PauseOnPageHidden || PauseOnWindowBlur) is false || _pageVisibility is not null) return;

        // The utility is a scoped service of the library, so it is only there in an app that registered them.
        // Nothing else about the snack bar depends on it, which is why its absence turns this one feature off
        // instead of failing the render.
        _pageVisibility = _serviceProvider?.GetService(typeof(BitPageVisibility)) as BitPageVisibility;
        if (_pageVisibility is null) return;

        _pageVisibility.OnChange += HandlePageVisibilityChange;
        _pageVisibility.OnWindowFocusChange += HandleWindowFocusChange;

        await _pageVisibility.Init();
    }

    // The shortcut is registered from the rendered id rather than from a parameter setter, so it follows a Hotkey
    // that is changed later and is torn down with the component. The script is a no-op in a prerender pass, which
    // is why this runs after the render rather than in OnInitialized.
    private async Task SyncHotkeyAsync()
    {
        var hotkey = Hotkey is { Length: > 0 } ? string.Join(' ', Hotkey) : null;
        var id = hotkey is null ? null : _Id;

        if (hotkey == _registeredHotkey && id == _registeredHotkeyId) return;

        // An Id that changed leaves a registration behind under the old one, which would go on answering the
        // shortcut for an element that is no longer this snack bar.
        var previousId = _registeredHotkeyId;

        _registeredHotkey = hotkey;
        _registeredHotkeyId = id;

        try
        {
            if (previousId is not null && previousId != id)
            {
                await _js.InvokeVoid("BitBlazorUI.SnackBars.unregisterHotkey", previousId);
            }

            if (id is not null)
            {
                await _js.InvokeVoid("BitBlazorUI.SnackBars.registerHotkey", id, Hotkey);
            }
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }

    private async Task SyncSwipeAsync()
    {
        var id = SwipeToDismiss ? _Id : null;
        var threshold = Math.Max(1, SwipeThreshold);

        // A threshold that changed only matters while the swipe is registered, so a snack bar that never asked
        // for one reaches the script not once.
        if (id == _registeredSwipeId && (id is null || threshold == _registeredSwipeThreshold)) return;

        var previousId = _registeredSwipeId;

        _registeredSwipeId = id;
        _registeredSwipeThreshold = threshold;

        try
        {
            if (previousId is not null && previousId != id)
            {
                await _js.InvokeVoid("BitBlazorUI.SnackBars.unregisterSwipe", previousId);
            }

            if (id is null) return;

            _dotnetObj ??= DotNetObjectReference.Create(this);

            await _js.InvokeVoid("BitBlazorUI.SnackBars.registerSwipe", id, threshold, _dotnetObj);
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }

    /// <summary>
    /// Dismisses the snack bar item that was swiped away. Called by the bit BlazorUI script.
    /// </summary>
    [JSInvokable("SwipeDismissed")]
    public Task _SwipeDismissed(string id)
    {
        if (IsDisposed || Guid.TryParse(id, out var itemId) is false) return Task.CompletedTask;

        return InvokeAsync(async () =>
        {
            var item = _items.Find(i => i.Id == itemId);

            // A swipe on a persistent item is the same request as a click on the dismiss button it does not have.
            if (item is null || IsDismissible(item) is false) return;

            await DismissAsync(item, BitSnackBarDismissReason.Swipe, animate: true);
        });
    }



    private string _DismissAriaLabel => DismissAriaLabel ?? "Close";

    private string _RootAriaLabel => AriaLabel ?? "Notifications";

    // The page-level reasons to hold a countdown back are read through the parameters that asked for them rather
    // than from the flags alone: the subscription is never taken down once it is made, so a snack bar whose
    // PauseOnPageHidden was turned off again would otherwise still be stopped by a hidden tab.
    private bool _PageHeld => (PauseOnPageHidden && _pageHidden) || (PauseOnWindowBlur && _windowBlurred);

    private bool IsHeld(BitSnackBarItem item) => _PageHeld || item._hovered;

    private bool IsDismissible(BitSnackBarItem item) => Persistent is false && item.Persistent is false;

    // Hiding the button is not the same as making the item persistent: the item still counts down, still answers
    // Escape and a swipe, and still takes a Close - it just carries no button of its own.
    private bool ShowsDismissButton(BitSnackBarItem item)
    {
        return IsDismissible(item) && HideDismissButton is false && item.HideDismissButton is false;
    }

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
    // (a custom "presentation" or "none", say) has no politeness at all, so asking for such a role really does opt
    // the item out of being announced instead of leaving a live region behind under a non-live role.
    private string? GetItemPoliteness(BitSnackBarItem item) => GetItemRole(item) switch
    {
        "alert" => "assertive",
        "status" or "log" => "polite",
        _ => null
    };

    // An item the announcer speaks for is explicitly not a live region of its own, or it would be read twice.
    // One it has nothing to say for - a Template item with no text - stays its own live region, which is less
    // reliable than the announcer but is the only way it is heard at all.
    private string? GetItemAriaLive(BitSnackBarItem item)
    {
        var politeness = GetItemPoliteness(item);

        if (politeness is null) return null;

        return item._announced ? "off" : politeness;
    }

    private string? GetItemAriaAtomic(BitSnackBarItem item)
    {
        return GetItemPoliteness(item) is not null && item._announced is false ? "true" : null;
    }

    private BitIconInfo? GetIcon(BitSnackBarItem item)
    {
        // The color only picks the icon when neither the item nor the component named one, and the lookup falls back
        // the way GetItemClasses does, so a color from outside the enum still draws an icon instead of throwing in
        // the middle of a render.
        var name = _IconMap.GetValueOrDefault(item.Color ?? BitColor.Info, _IconMap[BitColor.Info]);

        return BitIconInfo.From(item.Icon ?? Icon, item.IconName ?? IconName ?? name);
    }

    private bool IsClickable(BitSnackBarItem item)
    {
        return item.OnClick is not null || OnItemClick.HasDelegate || (DismissOnClick && IsDismissible(item));
    }

    // Only an item that answers a click becomes a tab stop of its own. Every other item keeps whatever focusable
    // content it holds - its dismiss button, its actions - as the only way into it, so an ordinary run of
    // notifications does not grow the Tab order by one stop per notification.
    private string? GetItemTabIndex(BitSnackBarItem item) => IsClickable(item) ? "0" : null;

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



    private static bool IsDuplicate(BitSnackBarItem one, BitSnackBarItem other)
    {
        return one.Title == other.Title && one.Body == other.Body && one.Color == other.Color;
    }

    // The cap counts the items that are still standing rather than everything in the list, for the same reason
    // TrimToMaxItems does: an item whose exit animation is playing has already given its room up.
    private bool IsFull()
    {
        return MaxItems is int max && max > 0 && _items.Count(i => i._dismissing is false) >= max;
    }

    private async Task ShowNowAsync(BitSnackBarItem item)
    {
        // An item that is showing has not been dismissed, whatever became of it the last time it was shown.
        item.DismissReason = null;

        if (NewestOnTop)
        {
            _items.Insert(0, item);
        }
        else
        {
            _items.Add(item);
        }

        Announce(item);

        StartCountdown(item);

        StateHasChanged();

        await OnShow.InvokeAsync(item);
    }

    // What the announcer says for an item: whatever the item spells out for it, or its text. An item that has
    // neither - one drawn entirely by a Template, say - has nothing here to announce, and is left to be its own
    // live region instead so that it is still heard.
    private static string? GetAnnouncement(BitSnackBarItem item)
    {
        if (item.AnnouncementText.HasValue()) return item.AnnouncementText;

        if (item.Title.HasValue() is false) return item.Body.HasValue() ? item.Body : null;

        return item.Body.HasValue() ? $"{item.Title}. {item.Body}" : item.Title;
    }

    private void Announce(BitSnackBarItem item)
    {
        item._announced = false;

        var text = GetAnnouncement(item);
        if (text is null) return;

        var live = GetItemPoliteness(item);
        if (live is null) return;

        var announcements = live == "assertive" ? _assertiveAnnouncements : _politeAnnouncements;

        var id = Guid.NewGuid();
        announcements.Add((id, text));

        item._announced = true;

        _ = RetireAnnouncementAsync(announcements, id);
    }

    // An announcement is taken back out once it has been read, so the region does not grow into a transcript of
    // everything the page has ever said - which a screen reader user can walk into and read back.
    private async Task RetireAnnouncementAsync(List<(Guid Id, string Text)> announcements, Guid id)
    {
        try
        {
            await Task.Delay(_AnnouncementLifetime);

            if (IsDisposed) return;

            await InvokeAsync(() =>
            {
                if (announcements.RemoveAll(a => a.Id == id) == 0) return;

                StateHasChanged();
            });
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex) { await DispatchSafelyAsync(ex); }
    }

    // Called whenever an item leaves and whenever the cap is raised, so a queue drains as room appears rather than
    // waiting for the next Show to notice it.
    private async Task PumpQueueAsync()
    {
        // Deliberately not gated on Overflow: only the Queue mode ever fills the queue, but turning that mode off
        // again while something is waiting must let what is already queued through rather than strand it.
        while (_queue.Count > 0 && IsFull() is false)
        {
            var next = _queue[0];
            _queue.RemoveAt(0);

            await ShowNowAsync(next);
        }
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

            await DismissAsync(oldest, BitSnackBarDismissReason.MaxItems, animate: false);

            if (_items.Contains(oldest)) break;
        }
    }

    private async Task DismissAsync(BitSnackBarItem item, BitSnackBarDismissReason reason, bool animate, bool focusNext = false)
    {
        if (item is null || _items.Contains(item) is false) return;

        // An item whose exit animation is already playing is on its way out under whatever reason started it, and
        // asking it to leave again does nothing - but the caller is still owed the wait, so Clear and Close come
        // back only once the item that was already leaving has actually gone.
        if (item._dismissing)
        {
            var pending = item._dismissSignal;

            if (pending is not null) await pending.Task;

            return;
        }

        CancelCountdown(item);

        var duration = Math.Max(0, TransitionDuration);

        if (animate && duration > 0)
        {
            item._dismissing = true;
            item._dismissSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            StateHasChanged();

            await Task.Delay(duration);

            if (IsDisposed)
            {
                SignalDismissed(item);
                return;
            }
        }

        item._dismissing = false;

        // An element taken out from under the pointer does not always report the pointer leaving it, so the hover
        // state is cleared here rather than left to say the item is being read after it is gone - which would start
        // the next countdown of a re-shown item paused with nothing to let it go.
        item._hovered = false;

        var index = _items.IndexOf(item);

        if (_items.Remove(item) is false)
        {
            SignalDismissed(item);
            return;
        }

        _dismissButtons.Remove(item.Id);

        // The reason is on the item before either callback runs, so both of them see the same answer to "why is
        // this gone", and a handler that only cares about one kind of dismissal can tell them apart.
        item.DismissReason = reason;

        StateHasChanged();

        if (focusNext) await FocusNeighbourAsync(index);

        if (item.OnDismiss is not null) await item.OnDismiss(item);

        await OnDismiss.InvokeAsync(item);

        // The room this item gave up goes to whatever was waiting for it, after the callbacks rather than before:
        // an OnDismiss that shows a notification of its own is then ahead of the queue, as it would be without one.
        await PumpQueueAsync();

        SignalDismissed(item);
    }

    private static void SignalDismissed(BitSnackBarItem item)
    {
        var signal = item._dismissSignal;
        item._dismissSignal = null;
        signal?.TrySetResult();
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
        if (ShowsDismissButton(neighbour) is false) return ValueTask.CompletedTask;

        if (_dismissButtons.TryGetValue(neighbour.Id, out var reference) is false) return ValueTask.CompletedTask;

        return reference.Context is null ? ValueTask.CompletedTask : reference.FocusAsync();
    }

    private async Task HandleItemClick(BitSnackBarItem item)
    {
        if (item.OnClick is not null) await item.OnClick(item);

        await OnItemClick.InvokeAsync(item);

        if (DismissOnClick && IsDismissible(item))
        {
            await DismissAsync(item, BitSnackBarDismissReason.Click, animate: true);
        }
    }

    // The dismiss button and the Escape key hand the focus on to the next item, which the public Close does not:
    // the code that closes a snack bar of its own accord has no reason to take the focus away from wherever the
    // user has it.
    private Task HandleDismissClick(BitSnackBarItem item)
    {
        return DismissAsync(item, BitSnackBarDismissReason.DismissButton, animate: true, focusNext: true);
    }

    private Task HandleItemKeyDown(KeyboardEventArgs e, BitSnackBarItem item)
    {
        // Escape is what closes the thing that has the focus, and while the focus is inside a snack bar that
        // thing is the snack bar. Only the items that offer a dismiss button answer it, so the key never takes
        // away a persistent notification the app is keeping on screen on purpose.
        if (e.Key == "Escape")
        {
            return IsDismissible(item)
                ? DismissAsync(item, BitSnackBarDismissReason.Escape, animate: true, focusNext: true)
                : Task.CompletedTask;
        }

        // An item that answers a click is an interactive element, and one that only a pointer can reach is out of
        // bounds for a keyboard user (WCAG 2.1.1). Enter is what activates it; Space is left alone because a div
        // cannot stop it from scrolling the page without also swallowing Tab.
        if (e.Key == "Enter" && IsClickable(item))
        {
            return HandleItemClick(item);
        }

        return Task.CompletedTask;
    }

    // The keydown of the dismiss button does not bubble, so Escape is passed on from here: it closes the item the
    // button belongs to whether the focus is on the button or anywhere else inside the item.
    private Task HandleDismissKeyDown(KeyboardEventArgs e, BitSnackBarItem item)
    {
        if (e.Key != "Escape" || IsDismissible(item) is false) return Task.CompletedTask;

        return DismissAsync(item, BitSnackBarDismissReason.Escape, animate: true, focusNext: true);
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

        return SyncPageHoldAsync();
    }

    private Task HandleWindowFocusChange(bool blurred)
    {
        _windowBlurred = blurred;

        return SyncPageHoldAsync();
    }

    // Both page-level reasons run through here rather than each pausing and resuming on its own, so the window
    // getting the focus back while the tab is still hidden - or the other way round - does not let go of a
    // countdown the other reason is still holding.
    private Task SyncPageHoldAsync()
    {
        var held = _PageHeld;

        return InvokeAsync(() =>
        {
            var changed = false;

            foreach (var item in _items.ToArray())
            {
                changed |= held ? PauseItem(item) : ResumeItem(item);
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
        // tab or behind another window - starts held back rather than running down behind the reader's back.
        if (IsHeld(item))
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

            await InvokeAsync(() => DismissAsync(item, BitSnackBarDismissReason.Timeout, animate: true));
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
            await InvokeAsync(() => DismissAsync(item, BitSnackBarDismissReason.Timeout, animate: true));
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

        // A countdown that is being thrown away takes the reasons it was held with it, so the next one this item
        // is given - from an Update, or from being shown again - does not start out stopped.
        item._userPaused = false;

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
        // of one whose page is still hidden - or of one the code asked to be held.
        if (item._userPaused || IsHeld(item)) return false;

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
            _pageVisibility.OnWindowFocusChange -= HandleWindowFocusChange;
            _pageVisibility = null;
        }

        if (_registeredHotkeyId is not null || _registeredSwipeId is not null)
        {
            var hotkeyId = _registeredHotkeyId;
            var swipeId = _registeredSwipeId;

            _registeredHotkey = null;
            _registeredHotkeyId = null;
            _registeredSwipeId = null;

            try
            {
                if (hotkeyId is not null) await _js.InvokeVoid("BitBlazorUI.SnackBars.unregisterHotkey", hotkeyId);
                if (swipeId is not null) await _js.InvokeVoid("BitBlazorUI.SnackBars.unregisterSwipe", swipeId);
            }
            catch (JSDisconnectedException) { }
            catch (ObjectDisposedException) { }
        }

        _dotnetObj?.Dispose();
        _dotnetObj = null;

        foreach (var item in _items)
        {
            CancelCountdown(item);

            // Anything waiting on an exit animation that will now never finish is let go rather than left hanging.
            SignalDismissed(item);
        }

        _queue.Clear();

        await base.DisposeAsync(disposing);
    }



    // Long enough for a screen reader to have got to the announcement, short enough that the region does not fill
    // up with what the page said a minute ago.
    private static readonly TimeSpan _AnnouncementLifetime = TimeSpan.FromSeconds(2);

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
