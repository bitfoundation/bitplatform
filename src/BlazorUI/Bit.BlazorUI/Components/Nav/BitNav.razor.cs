using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

public partial class BitNav : IDisposable
{
    private bool SelectedItemHasBeenSet;
    private BitNavItem? selectedItem;

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;

    /// <summary>
    /// The initially selected item in manual mode.
    /// </summary>
    [Parameter] public BitNavItem? DefaultSelectedItem { get; set; }

    /// <summary>
    /// Used to customize how content inside the group header is rendered.
    /// </summary>
    [Parameter] public RenderFragment<BitNavItem>? HeaderTemplate { get; set; }

    /// <summary>
    /// Used to customize how content inside the link tag is rendered.
    /// </summary>
    [Parameter] public RenderFragment<BitNavItem>? ItemTemplate { get; set; }

    /// <summary>
    /// A collection of link items to display in the navigation bar.
    /// </summary>
    [Parameter] public IList<BitNavItem> Items { get; set; } = new List<BitNavItem>();

    /// <summary>
    /// Determines how the navigation will be handled.
    /// The default value is Automatic.
    /// </summary>
    [Parameter] public BitNavMode Mode { get; set; } = BitNavMode.Automatic;

    /// <summary>
    /// Callback invoked when an item is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitNavItem> OnItemClick { get; set; }

    /// <summary>
    /// Callback invoked when an item is selected.
    /// </summary>
    [Parameter] public EventCallback<BitNavItem> OnSelectItem { get; set; }

    /// <summary>
    /// Callback invoked when a group header is clicked and Expanded or Collapse.
    /// </summary>
    [Parameter] public EventCallback<BitNavItem> OnItemToggle { get; set; }

    /// <summary>
    /// The way to render nav links.
    /// </summary>
    [Parameter] public BitNavRenderType RenderType { get; set; } = BitNavRenderType.Normal;

    /// <summary>
    /// Selected item to show in Nav.
    /// </summary>
    [Parameter]
    public BitNavItem? SelectedItem
    {
        get => selectedItem;
        set
        {
            if (value == selectedItem) return;
            selectedItem = value;
            ExpandParents(Items);
            SelectedItemChanged.InvokeAsync(selectedItem);
        }
    }
    [Parameter] public EventCallback<BitNavItem> SelectedItemChanged { get; set; }

    protected override string RootElementClass => "bit-nav";

    protected override async Task OnInitializedAsync()
    {
        if (Mode == BitNavMode.Automatic)
        {
            _navigationManager.LocationChanged += OnLocationChanged;

            SelectItemByCurrentUrl();
        }
        else if (DefaultSelectedItem is not null && SelectedItemHasBeenSet is false)
        {
            SelectedItem = DefaultSelectedItem;
        }

        await base.OnInitializedAsync();
    }

    private static List<BitNavItem> Flatten(IList<BitNavItem> e) => e.SelectMany(c => Flatten(c.Items)).Concat(e).ToList();

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SelectItemByCurrentUrl();

        StateHasChanged();
    }

    private void SelectItemByCurrentUrl()
    {
        var currentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        var currentItem = Flatten(Items).FirstOrDefault(item => item.Url == currentUrl);

        SelectedItem = currentItem;
    }

    private bool ExpandParents(IList<BitNavItem> items)
    {
        foreach (var item in items)
        {
            if (item == SelectedItem) return item.IsExpanded = true;

            if (item.Items.Any() && ExpandParents(item.Items)) return item.IsExpanded = true;
        }

        return false;
    }

    internal async Task HandleOnClick(BitNavItem item)
    {
        if (item.IsEnabled == false) return;

        await OnItemClick.InvokeAsync(item);

        if (item.Items.Any() && item.Url.HasNoValue())
        {
            await ToggleItem(item);
        }
        else if (Mode == BitNavMode.Manual)
        {
            SelectedItem = item;

            await OnSelectItem.InvokeAsync(item);
            
            StateHasChanged();
        }
    }

    internal async Task ToggleItem(BitNavItem item)
    {
        if (item.IsEnabled is false || item.Items.Any() is false) return;

        item.IsExpanded = !item.IsExpanded;

        await OnItemToggle.InvokeAsync(item);
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
