using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

public partial class BitNavGroup : IDisposable
{
    private bool SelectedKeyHasBeenSet;
    private string? selectedKey;

    private IList<BitNavOption> _options = new List<BitNavOption>();

    private BitNavOption? _selectedOption;
    private bool _disposed;

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;

    /// <summary>
    /// Option to render as children.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The initially selected key in manual mode.
    /// </summary>
    [Parameter] public string? DefaultSelectedKey { get; set; }

    /// <summary>
    /// Used to customize how content inside the group header is rendered.
    /// </summary>
    [Parameter] public RenderFragment<BitNavOption>? HeaderTemplate { get; set; }

    /// <summary>
    /// Determines how the navigation will be handled.
    /// The default value is Automatic.
    /// </summary>
    [Parameter] public BitNavMode Mode { get; set; } = BitNavMode.Automatic;

    /// <summary>
    /// Used to customize how content inside the option tag is rendered.
    /// </summary>
    [Parameter] public RenderFragment<BitNavOption>? OptionTemplate { get; set; }

    /// <summary>
    /// Callback invoked when an option is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitNavOption> OnOptionClick { get; set; }

    /// <summary>
    /// Callback invoked when an option is selected.
    /// </summary>
    [Parameter] public EventCallback<BitNavOption> OnSelectOption { get; set; }

    /// <summary>
    /// Callback invoked when a group header is clicked and Expanded or Collapse.
    /// </summary>
    [Parameter] public EventCallback<BitNavOption> OnOptionToggle { get; set; }

    /// <summary>
    /// The way to render nav options.
    /// </summary>
    [Parameter] public BitNavRenderType RenderType { get; set; } = BitNavRenderType.Normal;

    /// <summary>
    /// Selected option to show in Nav.
    /// </summary>
    [Parameter]
    public string? SelectedKey
    {
        get => selectedKey;
        set
        {
            if (value == selectedKey) return;

            selectedKey = value;
            _ = SelectedKeyChanged.InvokeAsync(value);

            _selectedOption?.SetSelected(false);
            _selectedOption = _options.FirstOrDefault(x => x._internalKey == value);
            _selectedOption?.SetSelected(true);

            _selectedOption?.Parent?.Expand();
        }
    }
    [Parameter] public EventCallback<string> SelectedKeyChanged { get; set; }

    protected override string RootElementClass => "bit-nvg";

    internal void Select(BitNavOption option)
    {
        SelectedKey = option._internalKey;

        _ = OnSelectOption.InvokeAsync(option);
    }

    internal void RegisterOption(BitNavOption option)
    {
        _options.Add(option);
    }

    internal void UnregisterOption(BitNavOption option)
    {
        _options.Remove(option);
    }

    protected override async Task OnInitializedAsync()
    {
        if (Mode == BitNavMode.Automatic)
        {
            _navigationManager.LocationChanged += OnLocationChanged;

            SelectOptionByCurrentUrl();
        }
        else if (DefaultSelectedKey is not null && SelectedKeyHasBeenSet is false)
        {
            SelectedKey = DefaultSelectedKey;
        }

        await base.OnInitializedAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SelectOptionByCurrentUrl();

        StateHasChanged();
    }

    private void SelectOptionByCurrentUrl()
    {
        var currentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        var currentOption = _options.FirstOrDefault(option => option.Url == currentUrl);

        SelectedKey = currentOption?._internalKey;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing is false || _disposed) return;

        _selectedOption?.Dispose();

        if (Mode == BitNavMode.Automatic)
        {
            _navigationManager.LocationChanged -= OnLocationChanged;
        }

        _disposed = true;
    }
}
