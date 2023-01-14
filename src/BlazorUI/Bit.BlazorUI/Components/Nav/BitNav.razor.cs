using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

public partial class BitNav : IDisposable
{
    internal IDictionary<BitNavItem, bool> _itemsExpanded = new Dictionary<BitNavItem, bool>();
    internal IDictionary<BitNavItemAriaCurrent, string> _ariaCurrentMap = new Dictionary<BitNavItemAriaCurrent, string>()
    {
        [BitNavItemAriaCurrent.Page] = "page",
        [BitNavItemAriaCurrent.Step] = "step",
        [BitNavItemAriaCurrent.Location] = "location",
        [BitNavItemAriaCurrent.Time] = "time",
        [BitNavItemAriaCurrent.Date] = "date",
        [BitNavItemAriaCurrent.True] = "true"
    };

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;

    /// <summary>
    /// Used to customize how content inside the group header is rendered.
    /// </summary>
    [Parameter] public RenderFragment<BitNavItem>? HeaderTemplate { get; set; }

    /// <summary>
    /// Used to customize how content inside the link tag is rendered.
    /// </summary>
    [Parameter] public RenderFragment<BitNavItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Determines how the navigation will be handled.
    /// The default value is Automatic
    /// </summary>
    [Parameter] public BitNavMode Mode { get; set; } = BitNavMode.Automatic;

    /// <summary>
    /// A collection of link items to display in the navigation bar.
    /// </summary>
    [Parameter] public IList<BitNavItem> Items { get; set; } = new List<BitNavItem>();

    /// <summary>
    /// Callback invoked when an item is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitNavItem> OnItemClick { get; set; }

    /// <summary>
    /// Callback invoked when a group header is clicked and Expanded.
    /// </summary>
    [Parameter] public EventCallback<BitNavItem> OnItemExpand { get; set; }

    /// <summary>
    /// The way to render nav links.
    /// </summary>
    [Parameter] public BitNavRenderType RenderType { get; set; } = BitNavRenderType.Normal;

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public BitNavItem? SelectedItem { get; set; }

    protected override string RootElementClass => "bit-nav";

    protected override async Task OnInitializedAsync()
    {
        if (Mode == BitNavMode.Automatic)
        {
            _navigationManager.LocationChanged += OnLocationChanged;
        }

        foreach (var item in Items)
        {
            SetItemsExpanded(item);
        };

        var flatNavLinkItems = Flatten(Items).ToList();
        var currrentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        SelectedItem = flatNavLinkItems.FirstOrDefault(item => item.Url == currrentUrl);

        await base.OnInitializedAsync();
    }

    private void SetItemsExpanded(BitNavItem item)
    {
        var isExpanded = item.Items.Any(ci => ci == SelectedItem) || item.IsExpanded;

        _itemsExpanded.Add(item, isExpanded);

        if (item.Items.Any())
        {
            foreach (var childItem in item.Items)
            {
                SetItemsExpanded(childItem);
            }
        }
    }

    private static IEnumerable<BitNavItem> Flatten(IEnumerable<BitNavItem> e) => e.SelectMany(c => Flatten(c.Items)).Concat(e);

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        if (Mode == BitNavMode.Manual) return;

        var currentPage = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);

        var currentItem = Flatten(Items).ToList().FirstOrDefault(CreateComparer(currentPage));

        if (currentItem is null) return;

        SelectedItem = currentItem;
        StateHasChanged();

        Func<BitNavItem, bool> CreateComparer(string currentPage)
        {
            return item => (item.Url ?? "").ToLower(Thread.CurrentThread.CurrentCulture) == currentPage.ToLower(Thread.CurrentThread.CurrentCulture);
        }
    }

    internal async void HandleOnItemClick(BitNavItem item)
    {
        if (item.IsEnabled == false) return;

        if (Mode == BitNavMode.Manual && item.Items.Any())
        {
            SelectedItem = item;
        }
        else if (item.Url.HasNoValue())
        {
            await HandleOnItemExpand(item);
        }

        await OnItemClick.InvokeAsync(item);
    }

    internal async Task HandleOnItemExpand(BitNavItem navLinkItem)
    {
        if (navLinkItem.IsEnabled is false || navLinkItem.Items.Any() is false) return;

        var oldIsExpanded = _itemsExpanded[navLinkItem];
        _itemsExpanded.Remove(navLinkItem);
        _itemsExpanded.Add(navLinkItem, !oldIsExpanded);

        await OnItemExpand.InvokeAsync(navLinkItem);
    }

    internal string GetItemClasses(BitNavItem item)
    {
        var enabledClass = item.IsEnabled ? "enabled" : "disabled";
        var hasUrlClass = item.Url.HasNoValue() ? "nourl" : "hasurl";

        return $"link-{enabledClass}-{hasUrlClass}";
    }

    internal bool IsRelativeUrl(string url)
    {
        var regex = new Regex(@"!/^[a-z0-9+-.]+:\/\//i");
        return regex.IsMatch(url);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && Mode == BitNavMode.Automatic)
        {
            _navigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
