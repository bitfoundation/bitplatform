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
/// Each item is announced to assistive technology through one of two live regions the host keeps in the page from
/// its first render, with the politeness that follows its color - the colors that report a problem interrupt the
/// screen reader, the rest wait for a pause - so a snack bar is heard as well as seen. The auto-dismiss countdown
/// pauses while the pointer or the keyboard focus is inside the item (and, with <see cref="PauseOnPageHidden"/>
/// and <see cref="PauseOnWindowBlur"/>, while the page is not being looked at), so a notification is never taken
/// away from someone who is still reading or acting on it, and <see cref="Hotkey"/> is what puts a keyboard user
/// inside one before it is gone.
/// </remarks>
public partial class BitSnackBar : BitComponentBase
{
    private readonly List<BitSnackBarItem> _items = [];
    private readonly List<BitSnackBarItem> _queue = [];
    private readonly Dictionary<Guid, ElementReference> _dismissButtons = [];

    private BitPageVisibility? _pageVisibility;
    private NavigationManager? _navigationManager;
    private bool _pageHidden;
    private bool _windowBlurred;
    private string? _registeredHotkey;
    private string? _registeredHotkeyId;
    private string? _registeredSwipeId;
    private int _registeredSwipeThreshold;
    private DotNetObjectReference<BitSnackBar>? _dotnetObj;

    // One counter per region rather than one shared between them: the counter keys the element that carries the
    // text, and re-keying the region an announcement did not touch would replace an element whose content had not
    // changed - which is exactly what a live region announces.
    private int _announceSequence;
    private int _politeGeneration;
    private int _assertiveGeneration;
    private string? _politeText;
    private string? _assertiveText;
    private Guid? _politeItemId;
    private Guid? _assertiveItemId;

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
    /// Closes every snack bar item as soon as the app navigates somewhere else.
    /// </summary>
    /// <remarks>
    /// A notification is about the page it was raised on, and one that outlives that page is read as being about
    /// the next one. Turning this on needs nothing but the router the app already has; in a host without a
    /// <c>NavigationManager</c> the items simply stay as they would otherwise.
    /// </remarks>
    [Parameter] public bool ClearOnNavigation { get; set; }

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
    /// Prevents rendering the dismiss button of every snack bar item.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Persistent"/> this only takes the button away: the items still count down, still answer
    /// the Escape key and still take part in <see cref="DismissOnClick"/>. Use it where the notification carries
    /// its own way out - an action button, or a countdown short enough that a second way out would only be noise -
    /// and never on a host whose items neither auto-dismiss nor offer one, which would leave them there for good.
    /// A single item can drop its button on its own through <see cref="BitSnackBarItem.HideDismiss"/>.
    /// </remarks>
    [Parameter] public bool HideDismiss { get; set; }

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
    /// A burst of notifications cannot grow into a wall that covers the page: what happens to the item that does
    /// not fit is up to <see cref="OverflowBehavior"/>, which by default dismisses the oldest one to make room
    /// for it. Unset (or zero and below) means no cap.
    /// </remarks>
    [Parameter] public int? MaxItems { get; set; }

    /// <summary>
    /// The maximum width of the snack bar items.
    /// </summary>
    /// <remarks>
    /// Any CSS length is accepted, and the stack never grows past the width of the screen whatever this says.
    /// Unset, an item is as wide as its longest line needs, which on a wide screen is a line too long to be read
    /// comfortably - a notification of any length is worth capping at a readable measure.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? MaxWidth { get; set; }

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
    /// The distance of the stack from the edges of the screen (default is 8px).
    /// </summary>
    /// <remarks>
    /// Any CSS length is accepted. This is what keeps a snack bar clear of the chrome the app already has at that
    /// edge - a bottom app bar, a cookie banner, the safe area of a phone.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Offset { get; set; }

    /// <summary>
    /// Callback for when any snack bar is dismissed, reporting the item that was dismissed.
    /// </summary>
    /// <remarks>
    /// The <see cref="BitSnackBarItem.DismissReason"/> of the item tells what took it away, so a callback can
    /// treat a notification the user threw away differently from one that simply ran out of time. An item that was
    /// still waiting in the queue when it was closed or cleared is reported here too, without ever having reported
    /// an <see cref="OnShow"/>.
    /// </remarks>
    [Parameter] public EventCallback<BitSnackBarItem> OnDismiss { get; set; }

    /// <summary>
    /// Callback for when any snack bar item is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitSnackBarItem> OnItemClick { get; set; }

    /// <summary>
    /// Callback for when a new snack bar item is shown.
    /// </summary>
    /// <remarks>
    /// An item held back by <see cref="BitSnackBarOverflowBehavior.Queue"/> reports this when it reaches the
    /// screen rather than when it was handed to <c>Show</c>.
    /// </remarks>
    [Parameter] public EventCallback<BitSnackBarItem> OnShow { get; set; }

    /// <summary>
    /// What happens to a new snack bar item that arrives while <see cref="MaxItems"/> is already reached
    /// (default is dismissing the oldest one).
    /// </summary>
    /// <remarks>
    /// Dismissing the oldest keeps the newest news on screen and is right for a stream of status updates;
    /// <see cref="BitSnackBarOverflowBehavior.Queue"/> shows every item in turn and is right where none of them
    /// may be missed; <see cref="BitSnackBarOverflowBehavior.Skip"/> drops the new one and is right where the
    /// items are interchangeable. This has no effect while <see cref="MaxItems"/> is unset.
    /// </remarks>
    [Parameter] public BitSnackBarOverflowBehavior OverflowBehavior { get; set; }

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
    /// <br />
    /// This follows the Page Visibility API, so it covers a tab in the background and a minimized window, but not a
    /// window that is still on screen while another app has the keyboard focus.
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
    /// A persistent snack bar also opts out of the auto-dismiss countdown and of the Escape key, so it stays until
    /// the code that opened it closes it through <see cref="Close(BitSnackBarItem)"/>. A single item can be made
    /// persistent on its own through <see cref="BitSnackBarItem.Persistent"/>.
    /// <br />
    /// To take the button away without also taking the countdown away, use <see cref="HideDismiss"/> instead.
    /// </remarks>
    [Parameter] public bool Persistent { get; set; }

    /// <summary>
    /// The corner or edge of the screen the snack bars are stacked at (default is the bottom end).
    /// </summary>
    /// <remarks>
    /// The Start and End values follow the text direction rather than the screen, so a stack keeps to the same
    /// side of the reading order in both LTR and RTL; the Left and Right ones stay on the same side of the
    /// screen in either. The enter animation of an item always slides out of the edge its stack is pinned to.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitPosition? Position { get; set; }

    /// <summary>
    /// Skips showing a new snack bar while an identical one is already on screen.
    /// </summary>
    /// <remarks>
    /// Two items count as identical when their title, body and color all match, and an item whose exit animation is
    /// already playing matches nothing. The <c>Show</c> call then returns the item that is already showing rather
    /// than a new one, so the caller still has a handle to it; that item's auto-dismiss countdown starts over - the
    /// second event is news too, and a notification that vanished a moment after it was repeated would be read as
    /// the first one being over rather than as both having happened - and its
    /// <see cref="BitSnackBarItem.DuplicateCount"/> goes up, which is what a template can show the repeat with.
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
    /// Draws the countdown progress bar depleting from full to empty instead of filling from empty to full.
    /// </summary>
    /// <remarks>
    /// A depleting bar reads as the time the notification has left, a filling one as how far it has got through
    /// its lifetime. Both are in wide use; pick the one that matches the other timers of the app.
    /// </remarks>
    [Parameter] public bool ReverseProgress { get; set; }

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
    /// The snack bar items that are waiting for a slot under <see cref="BitSnackBarOverflowBehavior.Queue"/>,
    /// in the order they will be shown.
    /// </summary>
    /// <remarks>
    /// Like <see cref="Items"/> this is a snapshot. It is empty unless <see cref="MaxItems"/> is set and
    /// <see cref="OverflowBehavior"/> is <see cref="BitSnackBarOverflowBehavior.Queue"/>.
    /// </remarks>
    public IReadOnlyList<BitSnackBarItem> PendingItems => [.. _queue];



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
    /// Showing an item that is already showing (or already waiting in the queue) is a no-op that returns it
    /// unchanged, and so is showing a duplicate of one while <see cref="PreventDuplicates"/> is enabled - in which
    /// case the item that is already showing comes back instead of the new one, with its countdown started over.
    /// <br />
    /// An item that does not fit under <see cref="MaxItems"/> is dealt with by <see cref="OverflowBehavior"/>:
    /// it may be held back until a slot frees up, or dropped, in which case the returned item never reaches the
    /// screen and no <see cref="OnShow"/> is reported for it.
    /// </remarks>
    public async Task<BitSnackBarItem> Show(BitSnackBarItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

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

                    // Suppressing the repeat keeps the page from filling with the same notification, but the thing
                    // did happen again; the item that stands for all of them counts how many.
                    duplicate.DuplicateCount++;

                    // The repeat is news of its own, so the notification that stands for it is given its full
                    // lifetime back rather than being left to run out on the clock of the first one.
                    if (_items.Contains(duplicate))
                    {
                        StartCountdown(duplicate);
                        Announce(duplicate);
                        StateHasChanged();
                    }

                    return;
                }
            }

            if (IsFull())
            {
                switch (OverflowBehavior)
                {
                    case BitSnackBarOverflowBehavior.Queue:
                        // An item that was dismissed before and is being shown again is waiting to arrive now,
                        // not gone: what took it away last time is no longer what it is.
                        item.DismissReason = null;
                        item.DuplicateCount = 0;
                        _queue.Add(item);
                        return;

                    case BitSnackBarOverflowBehavior.Skip:
                        return;

                    default:
                        await TrimToMaxItems();
                        break;
                }
            }

            await AddItem(item);
        });

        return shown;
    }

    /// <summary>
    /// Closes a snackbar item.
    /// </summary>
    /// <remarks>
    /// The returned task completes once the item has left the DOM, which is after its exit animation
    /// (<see cref="TransitionDuration"/>) has played. An item that is still waiting in the queue is taken out of
    /// it instead, and never reaches the screen.
    /// </remarks>
    public Task Close(BitSnackBarItem item) => InvokeAsync(async () =>
    {
        if (item is not null && _queue.Remove(item))
        {
            item.DismissReason = BitSnackBarDismissReason.Programmatic;

            StateHasChanged();

            await ReportDismissed(item);

            return;
        }

        await DismissAsync(item!, animate: true, reason: BitSnackBarDismissReason.Programmatic);
    });

    /// <summary>
    /// Closes every snackbar item that is currently showing, and drops everything that was waiting in the queue.
    /// </summary>
    /// <remarks>
    /// The items are taken away at once rather than one exit animation after another, so clearing a full stack does
    /// not take as long as the stack is tall.
    /// </remarks>
    public Task Clear() => InvokeAsync(async () =>
    {
        var queued = _queue.ToArray();
        _queue.Clear();

        foreach (var item in _items.ToArray())
        {
            await DismissAsync(item, animate: false, reason: BitSnackBarDismissReason.Clear);
        }

        foreach (var item in queued)
        {
            item.DismissReason = BitSnackBarDismissReason.Clear;

            await ReportDismissed(item);
        }
    });

    /// <summary>
    /// Re-renders a snackbar item after its properties were changed, and restarts its auto-dismiss countdown.
    /// </summary>
    /// <remarks>
    /// This is how a notification is turned into the report of what it was waiting for: keep the item a call to
    /// <c>Show</c> returned, set its title, body and color to the outcome, then hand it back here. The new text is
    /// announced again, so the outcome is heard as well as seen.
    /// <br />
    /// The restarted countdown starts held back if anything is still holding it - the pointer or the keyboard focus
    /// inside the item, a hidden page, or a <see cref="Pause(BitSnackBarItem)"/> that has not been released.
    /// </remarks>
    public Task Update(BitSnackBarItem item) => InvokeAsync(() =>
    {
        // An item whose exit animation is playing is past being updated: giving it a countdown it will never spend
        // and announcing text nobody will see would only be noise.
        if (item is null || item._dismissing || _items.Contains(item) is false) return;

        StartCountdown(item);

        // The text of the item has changed, which is a new thing to say - a notification that turned from
        // "Uploading..." into "Upload failed" has to be heard again, not only seen again.
        Announce(item);

        StateHasChanged();
    });

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
    /// This is a hold of its own rather than the same one the pointer takes, so a countdown held back from code
    /// is not let go again by the pointer happening to leave the item. Only <see cref="Resume(BitSnackBarItem)"/>
    /// releases it.
    /// </remarks>
    public Task Pause(BitSnackBarItem item) => InvokeAsync(() =>
    {
        if (item is null) return;

        item._held = true;

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
        if (item is null) return;

        item._held = false;

        if (ResumeItem(item)) StateHasChanged();
    });



    protected override string RootElementClass => "bit-snb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Position switch
        {
            BitPosition.TopStart => "bit-snb-tst",
            BitPosition.TopCenter => "bit-snb-tcn",
            BitPosition.TopEnd => "bit-snb-ten",
            BitPosition.TopLeft => "bit-snb-tlf",
            BitPosition.TopRight => "bit-snb-trg",
            BitPosition.CenterStart => "bit-snb-cst",
            BitPosition.Center => "bit-snb-ctr",
            BitPosition.CenterEnd => "bit-snb-cen",
            BitPosition.CenterLeft => "bit-snb-clf",
            BitPosition.CenterRight => "bit-snb-crg",
            BitPosition.BottomStart => "bit-snb-bst",
            BitPosition.BottomCenter => "bit-snb-bcn",
            BitPosition.BottomEnd => "bit-snb-ben",
            BitPosition.BottomLeft => "bit-snb-blf",
            BitPosition.BottomRight => "bit-snb-brg",
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

        StyleBuilder.Register(() => MaxWidth.HasValue() ? $"--bit-snb-max-w:{MaxWidth}" : string.Empty);
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        await SyncQueueAsync();

        // A PauseOnPageHidden or PauseOnWindowBlur that is turned off again while it is holding everything back
        // has no event of its own coming - the next visibility change might never happen - so the hold is
        // re-evaluated here instead of leaving every countdown stopped for good.
        if (_items.Exists(i => i._paused) || _PageHeld) await SyncPageHoldAsync();
    }

    private async Task SyncQueueAsync()
    {
        if (_queue.Count == 0) return;

        // Raising MaxItems (or dropping the cap) frees slots that nothing is going to dismiss its way into, so the
        // items that were held back for one take them here instead of waiting for a dismissal that never comes.
        if (OverflowBehavior == BitSnackBarOverflowBehavior.Queue)
        {
            await PromoteFromQueue();
            return;
        }

        // Queueing is no longer how overflow is dealt with, so what was waiting is dealt with the new way rather
        // than held for a slot nobody is going to free.
        var waiting = _queue.ToArray();

        _queue.Clear();

        foreach (var item in waiting)
        {
            await Show(item);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // Not gated by firstRender: the parameter can be turned on later, and a subscription only taken on the
        // first render would leave it doing nothing. The already-resolved manager is what keeps this from
        // subscribing twice.
        if (ClearOnNavigation && _navigationManager is null)
        {
            // Resolved through the provider rather than injected for the same reason as the page visibility
            // utility: a snack bar has to keep working in a host that has no router at all.
            _navigationManager = _serviceProvider?.GetService(typeof(NavigationManager)) as NavigationManager;

            if (_navigationManager is not null)
            {
                _navigationManager.LocationChanged += HandleLocationChanged;
            }
        }

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

            await DismissAsync(item, animate: true, reason: BitSnackBarDismissReason.Swipe);
        });
    }



    private string _DismissAriaLabel => DismissAriaLabel ?? "Close";

    private string _RootAriaLabel => AriaLabel ?? "Notifications";

    private bool IsDismissible(BitSnackBarItem item) => Persistent is false && item.Persistent is false;

    private bool ShowDismissButton(BitSnackBarItem item)
    {
        return IsDismissible(item) && HideDismiss is false && item.HideDismiss is false;
    }

    // An item whose exit animation is playing is on its way out, so a new notification identical to it is news
    // rather than a repeat - matching it would drop the new one and leave nothing on screen.
    private static bool IsDuplicate(BitSnackBarItem left, BitSnackBarItem right)
    {
        return left._dismissing is false
            && left.Title == right.Title
            && left.Body == right.Body
            && left.Color == right.Color;
    }

    private bool IsFull() => MaxItems is int max && max > 0 && _items.Count(i => i._dismissing is false) >= max;

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

    // The politeness of the announcement follows the role rather than being declared beside it, and a role that is
    // not a live one at all (a custom "presentation" or "none", say) is announced by neither region, so asking for
    // such a role really does opt the item out of being announced.
    private string? GetAnnouncePoliteness(BitSnackBarItem item) => GetItemRole(item) switch
    {
        "alert" => "assertive",
        "status" or "log" => "polite",
        _ => null
    };

    private string? GetAnnounceText(BitSnackBarItem item)
    {
        if (item.AnnounceText is not null) return item.AnnounceText;

        if (item.Title.HasValue() is false) return item.Body;
        if (item.Body.HasValue() is false) return item.Title;

        // The stop is what makes a screen reader read the two as two sentences rather than running the body
        // straight on from the title.
        return $"{item.Title}. {item.Body}";
    }

    // Placing the text into a region that has been in the page since the first render is what makes it heard;
    // the generation is what makes the same text heard twice, since a region whose content did not change has
    // nothing for the screen reader to notice.
    private void Announce(BitSnackBarItem item)
    {
        var politeness = GetAnnouncePoliteness(item);
        if (politeness is null) return;

        var text = GetAnnounceText(item);
        if (text.HasValue() is false) return;

        _announceSequence++;

        if (politeness == "assertive")
        {
            _assertiveText = text;
            _assertiveItemId = item.Id;
            _assertiveGeneration++;
        }
        else
        {
            _politeText = text;
            _politeItemId = item.Id;
            _politeGeneration++;
        }

        _ = ClearAnnouncementLaterAsync(_announceSequence);
    }

    // Once the announcement has been made there is nothing left for it to do, and text left behind in the region
    // would be read a second time by anyone going through the page with a virtual cursor - the item it belongs to
    // is right there saying the same thing. A second is long enough for every screen reader to have picked it up.
    private async Task ClearAnnouncementLaterAsync(int sequence)
    {
        try
        {
            await Task.Delay(_AnnouncementLifetime);

            if (IsDisposed) return;

            await InvokeAsync(() =>
            {
                // Only the announcement that is still the latest one takes its own text back out; an older one
                // whose region has since been written to again has nothing left of its own to clear.
                if (_announceSequence != sequence) return;
                if (_politeText is null && _assertiveText is null) return;

                // Taking the text away removes the element that carried it, and a live region says nothing about
                // what left it - only about what arrives.
                _politeText = null;
                _politeItemId = null;
                _assertiveText = null;
                _assertiveItemId = null;

                StateHasChanged();
            });
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex) { await DispatchSafelyAsync(ex); }
    }

    // The announcement of an item that has left is taken back out, so what a screen reader user finds while
    // reading through the region is what is on screen rather than the last thing that was said there.
    private void ClearAnnouncement(BitSnackBarItem item)
    {
        if (_politeItemId == item.Id)
        {
            _politeText = null;
            _politeItemId = null;
        }

        if (_assertiveItemId == item.Id)
        {
            _assertiveText = null;
            _assertiveItemId = null;
        }
    }

    private BitIconInfo? GetIcon(BitSnackBarItem item)
    {
        var iconName = _IconMap.TryGetValue(item.Color ?? BitColor.Info, out var mapped) ? mapped : _IconMap[BitColor.Info];

        return BitIconInfo.From(item.Icon ?? Icon, item.IconName ?? IconName ?? iconName);
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



    private async Task AddItem(BitSnackBarItem item)
    {
        item.DismissReason = null;
        item.DuplicateCount = 0;

        if (NewestOnTop)
        {
            _items.Insert(0, item);
        }
        else
        {
            _items.Add(item);
        }

        StartCountdown(item);

        Announce(item);

        StateHasChanged();

        await OnShow.InvokeAsync(item);
    }

    // A slot has freed up, so whatever was held back for one takes it. The items go in the order they were handed
    // over - the point of queueing rather than dropping is that none of them is missed and none of them arrives
    // out of turn.
    private async Task PromoteFromQueue()
    {
        while (_queue.Count > 0 && IsFull() is false)
        {
            var next = _queue[0];
            _queue.RemoveAt(0);

            await AddItem(next);
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

            await DismissAsync(oldest, animate: false, reason: BitSnackBarDismissReason.Overflow);

            if (_items.Contains(oldest)) break;
        }
    }

    private async Task DismissAsync(
        BitSnackBarItem item,
        bool animate,
        BitSnackBarDismissReason reason,
        bool focusNext = false)
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
        // and focus states are cleared here rather than left to say the item is being read after it is gone - which
        // would start the next countdown of a re-shown item paused with nothing to let it go.
        item._hovered = false;
        item._focused = false;
        item._held = false;
        item._activationPending = false;

        var index = _items.IndexOf(item);

        if (_items.Remove(item) is false) return;

        _dismissButtons.Remove(item.Id);

        ClearAnnouncement(item);

        item.DismissReason = reason;

        StateHasChanged();

        if (focusNext) await FocusNeighbourAsync(index);

        await ReportDismissed(item);

        // The slot this item held is free now, so whatever was queued for one moves in - after the callbacks, so
        // the code watching the host sees the item leave before it sees the next one arrive.
        await PromoteFromQueue();
    }

    private async Task ReportDismissed(BitSnackBarItem item)
    {
        if (item.OnDismiss is not null) await item.OnDismiss(item);

        await OnDismiss.InvokeAsync(item);
    }

    // A dismiss button that removes itself leaves the keyboard focus on nothing, which sends the next Tab back to
    // the top of the page. Handing the focus to the nearest item that still offers a dismiss button - the one that
    // took its place, or, failing that, the closest one before it - keeps a run of dismissals reachable from the
    // keyboard.
    private async Task FocusNeighbourAsync(int index)
    {
        for (var offset = 0; offset < _items.Count; offset++)
        {
            if (TryPick(index + offset, out var forward))
            {
                await FocusAsync(forward);
                return;
            }

            if (TryPick(index - offset - 1, out var backward))
            {
                await FocusAsync(backward);
                return;
            }
        }

        bool TryPick(int at, out ElementReference reference)
        {
            reference = default;

            if (at < 0 || at >= _items.Count) return false;

            // An item that has no dismiss button of its own has nothing here to focus, and the reference kept for
            // it would be pointing at an element that is no longer in the DOM.
            if (ShowDismissButton(_items[at]) is false) return false;

            if (_dismissButtons.TryGetValue(_items[at].Id, out reference) is false) return false;

            return reference.Context is not null;
        }

        // The reference can still be stale - an item whose dismiss button was taken away by a parameter change
        // keeps the one it was given - and reaching for an element the browser no longer has is not worth failing
        // a dismissal over.
        static async Task FocusAsync(ElementReference reference)
        {
            try
            {
                await reference.FocusAsync();
            }
            // JSException and ObjectDisposedException both derive from this one.
            catch (InvalidOperationException) { }
        }
    }

    private async Task HandleItemClick(BitSnackBarItem item)
    {
        // A control inside the item turned the Enter or Space into a click of its own, which is this one: the key
        // has been answered and the key-up must not answer it a second time.
        item._activationPending = false;

        if (item.OnClick is not null) await item.OnClick(item);

        await OnItemClick.InvokeAsync(item);

        if (DismissOnClick && IsDismissible(item))
        {
            await DismissAsync(item, animate: true, reason: BitSnackBarDismissReason.Click);
        }
    }

    // The dismiss button and the Escape key hand the focus on to the next item, which the public Close does not:
    // the code that closes a snack bar of its own accord has no reason to take the focus away from wherever the
    // user has it.
    private Task HandleDismissClick(BitSnackBarItem item)
    {
        // The dismiss button keeps its click to itself, so the Enter or Space that pressed it is answered here and
        // nowhere else - the item must not also be reported as clicked.
        item._activationPending = false;

        return DismissAsync(item, animate: true, reason: BitSnackBarDismissReason.DismissButton, focusNext: true);
    }

    private Task HandleItemKeyDown(KeyboardEventArgs e, BitSnackBarItem item)
    {
        // A clickable item is a control, and a control that only answers the pointer is out of reach of anyone
        // working from the keyboard (WCAG 2.1.1). It carries a tab stop of its own for the same reason.
        //
        // The key is only noted here, not answered: this handler also sees the keys pressed on whatever the item
        // holds, and a control of its own turns Enter or Space into a click that arrives before the key is
        // released. What is left unanswered by the time of the key-up was pressed on the item itself.
        if ((e.Key is "Enter" or " " or "Spacebar") && IsClickable(item))
        {
            item._activationPending = true;
        }

        // Escape is what closes the thing that has the focus, and while the focus is inside a snack bar that
        // thing is the snack bar. Only the items that can be dismissed at all answer it, so the key never takes
        // away a persistent notification the app is keeping on screen on purpose.
        if (e.Key != "Escape" || IsDismissible(item) is false) return Task.CompletedTask;

        return DismissAsync(item, animate: true, reason: BitSnackBarDismissReason.Escape, focusNext: true);
    }

    private Task HandleItemKeyUp(KeyboardEventArgs e, BitSnackBarItem item)
    {
        if (e.Key is not ("Enter" or " " or "Spacebar")) return Task.CompletedTask;

        if (item._activationPending is false) return Task.CompletedTask;

        item._activationPending = false;

        return HandleItemClick(item);
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

    // The pointer and the keyboard are tracked apart rather than through one flag: focus moving between the
    // controls inside an item reports leaving it before it reports entering it again, and a single flag would let
    // that hand-off run the countdown of an item the pointer had never left.
    private void HandleFocusStart(BitSnackBarItem item)
    {
        if (PauseOnHover is false) return;

        item._focused = true;

        PauseItem(item);
    }

    private void HandleFocusEnd(BitSnackBarItem item)
    {
        item._focused = false;

        // A key held down while the focus moves away never reports its release here, so the note it left is
        // dropped rather than answered by the next key-up the item happens to see.
        item._activationPending = false;

        ResumeItem(item);
    }

    private void HandleLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs args)
    {
        // The subscription is taken once and kept, so the parameter is read here as well: turning it off again
        // leaves the items where they are.
        if (ClearOnNavigation is false) return;

        _ = ClearInBackgroundAsync();
    }

    private async Task ClearInBackgroundAsync()
    {
        try
        {
            await Clear();
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex) { await DispatchSafelyAsync(ex); }
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

        // A countdown handed to an item that is already being read - the pointer or the keyboard focus inside it,
        // or the page in a hidden tab - starts held back rather than running down behind the reader's back.
        if (IsHeldBack(item))
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

            await InvokeAsync(() => DismissAsync(item, animate: true, reason: BitSnackBarDismissReason.Timeout));
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
            await InvokeAsync(() => DismissAsync(item, animate: true, reason: BitSnackBarDismissReason.Timeout));
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

    // A countdown is held back for as long as any one reason to hold it back stands, so the page coming back into
    // view does not let go of an item the pointer is still inside, the pointer leaving does not let go of one the
    // keyboard focus is still inside or one the code asked to hold, and none of them lets go of an item whose page
    // is still hidden.
    private bool IsHeldBack(BitSnackBarItem item) => _PageHeld || item._hovered || item._focused || item._held;

    // The page-level reasons to hold a countdown back are read through the parameters that asked for them rather
    // than from the flags alone: the subscription is never taken down once it is made, so a snack bar whose
    // PauseOnPageHidden was turned off again would otherwise still be stopped by a hidden tab.
    private bool _PageHeld => (PauseOnPageHidden && _pageHidden) || (PauseOnWindowBlur && _windowBlurred);

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

        if (IsHeldBack(item)) return false;

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

        if (_navigationManager is not null)
        {
            _navigationManager.LocationChanged -= HandleLocationChanged;
            _navigationManager = null;
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
        }

        // The host is going away with its items, so nothing it was holding on their behalf is worth keeping: the
        // element references in particular would otherwise point at a DOM that is no longer there.
        _items.Clear();
        _queue.Clear();
        _dismissButtons.Clear();

        await base.DisposeAsync(disposing);
    }



    private static readonly TimeSpan _AnnouncementLifetime = TimeSpan.FromSeconds(1);

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
