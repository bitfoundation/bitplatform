using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// A bell button whose badge counts the notifications not read yet, opening a panel that lists the history of
/// <see cref="BitSnackBarService"/>: the notifications the app's <see cref="BitSnackBar.ServiceHost"/> showed, newest
/// first, whether they are still on the screen or long gone.
/// </summary>
/// <remarks>
/// Place one beside the layout's <c>&lt;BitSnackBar ServiceHost /&gt;</c>; it needs the bit BlazorUI services
/// (<c>AddBitBlazorUIServices</c>), and shows nothing without them. The service keeps no history until its
/// <see cref="BitSnackBarService.HistoryLimit"/> is set, so the center raises it to <see cref="HistoryLimit"/>.
/// <br />
/// What has been read is the center's own: a notification counts as read once the panel was opened with it listed, or
/// through <see cref="MarkAllAsRead"/>. The notifications that were unread when the panel opened stay marked for as
/// long as it is open, so the user sees what is new.
/// </remarks>
public partial class BitNotificationCenter : BitComponentBase
{
    private const string DefaultAriaLabel = "Notifications";
    private const string DefaultUnreadAriaLabelFormat = "{0} unread";
    private const string DefaultHeaderText = "Notifications";
    private const string DefaultClearAllText = "Clear all";
    private const string DefaultUnreadItemText = "Unread";
    private const string DefaultIconName = "Ringer";



    [Inject] private IServiceProvider _serviceProvider { get; set; } = default!;

    private BitSnackBarService? _service;

    // Read is kept by item, the handle the history keeps too; it holds only items still in the history (see Prune).
    private readonly HashSet<BitSnackBarItem> _read = [];

    // The notifications marked new for as long as the panel is open: the ones unread when it opened, and the ones
    // arriving while it is.
    private readonly HashSet<BitSnackBarItem> _new = [];

    private BitDropMenuClassStyles? _dropMenuClasses;
    private BitDropMenuClassStyles? _dropMenuStyles;



    /// <summary>
    /// The default values of the parameters of the notification center, cascaded through a <see cref="BitParams"/>.
    /// A value set on the notification center itself wins over it.
    /// </summary>
    [CascadingParameter(Name = BitNotificationCenterParams.ParamName)]
    public BitNotificationCenterParams? CascadingParameters { get; set; }



    /// <summary>
    /// The text of the button that forgets every notification of the history (<see cref="BitSnackBarService.ClearHistory"/>).
    /// Defaults to "Clear all".
    /// </summary>
    [Parameter] public string? ClearAllText { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the notification center.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitNotificationCenterClassStyles? Classes { get; set; }

    /// <summary>
    /// The custom content the panel shows when there are no notifications, which takes the place of
    /// <see cref="EmptyText"/>.
    /// </summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// The text the panel shows when there are no notifications. Default: <see cref="BitStrings.NoItemsFound"/>.
    /// </summary>
    [Parameter] public string? EmptyText { get; set; }

    /// <summary>
    /// The text at the top of the panel. Defaults to "Notifications".
    /// </summary>
    [Parameter] public string? HeaderText { get; set; }

    /// <summary>
    /// How many notifications the center makes <see cref="BitSnackBarService"/> keep: the service's
    /// <see cref="BitSnackBarService.HistoryLimit"/> is raised to it when it keeps fewer, and never lowered. Default: 50.
    /// </summary>
    [Parameter] public int HistoryLimit { get; set; } = 50;

    /// <summary>
    /// The icon of the bell button. Takes precedence over <see cref="IconName"/>.
    /// </summary>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon of the bell button, from the bit BlazorUI icons. Defaults to "Ringer".
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Whether the panel is open. Opening it marks every notification it lists as read. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetIsOpen))]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The custom template of each notification of the list.
    /// </summary>
    [Parameter] public RenderFragment<BitSnackBarItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the notification center.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitNotificationCenterClassStyles? Styles { get; set; }

    /// <summary>
    /// The time zone the time of each notification is shown in. Defaults to the local time zone - which, in a
    /// server-side rendered app, is the server's rather than the user's.
    /// </summary>
    [Parameter] public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>
    /// The composite format the unread count joins the name of the bell button with ({0} = the count), so a screen
    /// reader hears it with the name. Defaults to "{0} unread".
    /// </summary>
    [Parameter] public string? UnreadAriaLabelFormat { get; set; }

    /// <summary>
    /// What a screen reader hears of a notification that had not been read when the panel opened, which is shown by
    /// its look alone. Defaults to "Unread".
    /// </summary>
    [Parameter] public string? UnreadItemText { get; set; }



    /// <summary>
    /// How many notifications of the history have not been read yet: the count on the badge of the bell.
    /// </summary>
    public int UnreadCount => History.Count(item => _read.Contains(item) is false);

    /// <summary>
    /// Marks every notification of the history as read, which clears the badge of the bell.
    /// </summary>
    public void MarkAllAsRead()
    {
        _read.UnionWith(History);

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-ntc";

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
        // Resolved rather than injected, so a page whose app registers no bit BlazorUI services still renders the bell.
        _service = _serviceProvider.GetService(typeof(BitSnackBarService)) as BitSnackBarService;

        if (_service is not null) _service.OnHistoryChange += HandleHistoryChange;

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        if (_service is not null && _service.HistoryLimit < HistoryLimit)
        {
            _service.HistoryLimit = HistoryLimit;
        }

        // Handed to the drop menu as its own, so they are built when the parameters change rather than on every render.
        _dropMenuClasses = new() { Button = $"bit-ntc-btn {Classes?.Button}".TrimEnd(), Callout = $"bit-ntc-cal {Classes?.Callout}".TrimEnd() };
        _dropMenuStyles = new() { Button = Styles?.Button, Callout = Styles?.Callout };

        base.OnParametersSet();
    }



    private IReadOnlyList<BitSnackBarItem> History => _service?.History ?? [];

    private void OnSetIsOpen()
    {
        if (IsOpen)
        {
            // What was unread stays marked while the panel is open, and is read from now on.
            foreach (var item in History)
            {
                if (_read.Add(item)) _new.Add(item);
            }
        }
        else
        {
            _new.Clear();
        }
    }

    private async Task HandleIsOpenChanged(bool isOpen)
    {
        await AssignIsOpen(isOpen);
    }

    private void HandleHistoryChange() => _ = InvokeAsync(() =>
    {
        if (IsDisposed) return;

        var history = History;

        // A notification that arrives while the panel is open is read as it is listed, and marked new like the rest.
        if (IsOpen)
        {
            foreach (var item in history)
            {
                if (_read.Add(item)) _new.Add(item);
            }
        }

        Prune(history);

        StateHasChanged();
    });

    // A notification gone from the history is forgotten here too, so the center holds on to none of them.
    private void Prune(IReadOnlyList<BitSnackBarItem> history)
    {
        if (_read.Count == 0 && _new.Count == 0) return;

        var kept = history.ToHashSet();

        _read.IntersectWith(kept);
        _new.IntersectWith(kept);
    }

    private void ClearAll()
    {
        _service?.ClearHistory();
    }

    private string GetButtonAriaLabel(int unread)
    {
        var name = AriaLabel ?? DefaultAriaLabel;

        return unread > 0
            ? $"{name}, {string.Format(CultureInfo.CurrentCulture, UnreadAriaLabelFormat ?? DefaultUnreadAriaLabelFormat, unread)}"
            : name;
    }

    private static string GetBadgeText(int unread) => unread > 99 ? "99+" : unread.ToString(CultureInfo.CurrentCulture);

    private BitIconInfo? GetButtonIcon() => BitIconInfo.From(Icon, IconName ?? DefaultIconName);

    // The icon the item had on the screen, short of one its host set for every item: the history does not say which host.
    private static BitIconInfo? GetItemIcon(BitSnackBarItem item)
        => BitIconInfo.From(item.Icon, item.IconName ?? BitSnackBar.GetColorIconName(item.Color));

    // The time of today's notifications, and the date too of an older one, both in the reader's own culture.
    private (string Iso, string Text)? GetItemTime(BitSnackBarItem item)
    {
        if (item.ShownAt is not { } shownAt) return null;

        var zone = TimeZone ?? TimeZoneInfo.Local;
        var local = TimeZoneInfo.ConvertTime(shownAt, zone);
        var today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).Date;

        var text = local.Date == today
            ? local.ToString("t", CultureInfo.CurrentCulture)
            : local.ToString("g", CultureInfo.CurrentCulture);

        return (shownAt.ToString("O", CultureInfo.InvariantCulture), text);
    }

    private string GetItemCssClass(BitSnackBarItem item, bool isNew)
    {
        var classes = new List<string> { "bit-ntc-itm" };

        classes.AddIfHasValue(BitCssClasses.Color(item.Color, "bit-ntc"));

        if (isNew)
        {
            classes.Add("bit-ntc-new");
            classes.AddIfHasValue(Classes?.UnreadItem);
        }

        classes.AddIfHasValue(Classes?.Item);

        return string.Join(" ", classes);
    }

    private string? GetItemStyle(bool isNew)
    {
        var style = isNew && Styles?.UnreadItem.HasValue() is true
            ? $"{Styles?.Item};{Styles?.UnreadItem}"
            : Styles?.Item;

        return style.HasValue() ? style : null;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_service is not null) _service.OnHistoryChange -= HandleHistoryChange;

        await base.DisposeAsync(disposing);
    }
}
