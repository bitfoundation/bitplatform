using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

public partial class BitNavGroup : IDisposable
{
    private bool SelectedKeyHasBeenSet;
    private string? selectedKey;

    internal IList<BitNavOption> _options = new List<BitNavOption>();

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;

    /// <summary>
    /// A list of items to render as children of the current item
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The initially selected item in manual mode.
    /// </summary>
    [Parameter] public string? DefaultSelectedKey { get; set; }

    /// <summary>
    /// Used to customize how content inside the group header is rendered.
    /// </summary>
    [Parameter] public RenderFragment<BitNavOption>? HeaderTemplate { get; set; }

    /// <summary>
    /// Used to customize how content inside the link tag is rendered.
    /// </summary>
    [Parameter] public RenderFragment<BitNavOption>? ItemTemplate { get; set; }

    /// <summary>
    /// Determines how the navigation will be handled.
    /// The default value is Automatic.
    /// </summary>
    [Parameter] public BitNavGroupMode Mode { get; set; } = BitNavGroupMode.Automatic;

    /// <summary>
    /// Callback invoked when an item is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitNavOption> OnItemClick { get; set; }

    /// <summary>
    /// Callback invoked when an item is selected.
    /// </summary>
    [Parameter] public EventCallback<BitNavOption> OnSelectItem { get; set; }

    /// <summary>
    /// Callback invoked when a group header is clicked and Expanded or Collapse.
    /// </summary>
    [Parameter] public EventCallback<BitNavOption> OnItemToggle { get; set; }

    /// <summary>
    /// The way to render nav links.
    /// </summary>
    [Parameter] public BitNavGroupRenderType RenderType { get; set; } = BitNavGroupRenderType.Normal;

    /// <summary>
    /// Selected item to show in Nav.
    /// </summary>
    [Parameter]
    public string? SelectedKey
    {
        get => selectedKey;
        set
        {
            if (value == selectedKey) return;
            selectedKey = value;
            SelectedKeyChanged.InvokeAsync(value);
            ExpandParents(_options);
        }
    }
    [Parameter] public EventCallback<string> SelectedKeyChanged { get; set; }

    protected override string RootElementClass => "bit-nvg";

    protected override async Task OnInitializedAsync()
    {
        if (Mode == BitNavGroupMode.Automatic)
        {
            _navigationManager.LocationChanged += OnLocationChanged;

            SelectItemByCurrentUrl();
        }
        else if (DefaultSelectedKey is not null && SelectedKeyHasBeenSet is false)
        {
            SelectedKey = DefaultSelectedKey;
        }

        await base.OnInitializedAsync();
    }

    private static List<BitNavOption> Flatten(IList<BitNavOption> e) => e.SelectMany(c => Flatten(c._options)).Concat(e).ToList();

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SelectItemByCurrentUrl();

        StateHasChanged();
    }

    private void SelectItemByCurrentUrl()
    {
        var currentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        var currentItem = Flatten(_options).FirstOrDefault(item => item.Url == currentUrl);

        SelectedKey = currentItem?.Key;
    }

    private bool ExpandParents(IList<BitNavOption> items)
    {
        foreach (var item in items)
        {
            if (item.Key == SelectedKey || (item._options.Any() && ExpandParents(item._options))) return item.IsExpanded = true;
        }

        return false;
    }

    internal async Task HandleOnClick(BitNavOption item)
    {
        if (item.IsEnabled == false) return;

        await OnItemClick.InvokeAsync(item);

        if (item._options.Any() && item.Url.HasNoValue())
        {
            await ToggleItem(item);
        }
        else if (Mode == BitNavGroupMode.Manual)
        {
            SelectedKey = item.Key;

            await OnSelectItem.InvokeAsync(item);

            StateHasChanged();
        }
    }

    internal async Task ToggleItem(BitNavOption item)
    {
        if (item.IsEnabled is false || item._options.Any() is false) return;

        item.IsExpanded = !item.IsExpanded;

        await OnItemToggle.InvokeAsync(item);
    }

    internal void RegisterOptions(BitNavOption option)
    {
        if (option.Key.HasNoValue())
        {
            option.Key = $"{_options.Count}";
        }
        _options.Add(option);
        StateHasChanged();
    }

    internal void UnregisterOptions(BitNavOption option)
    {
        _options.Remove(option);
        StateHasChanged();
    }

    internal void RegisterChildOptions(BitNavOption parent, BitNavOption option)
    {
        if (option.Key.HasNoValue())
        {
            option.Key = $"{parent.Key}-{parent._options.Count}";
        }
        Flatten(_options).FirstOrDefault(i => i == parent)?._options.Add(option);
        StateHasChanged();
    }

    internal void UnregisterChildOptions(BitNavOption parent, BitNavOption option)
    {
        Flatten(_options).FirstOrDefault(i => i == parent)?._options.Remove(option);
        StateHasChanged();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && Mode == BitNavGroupMode.Automatic)
        {
            _navigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
