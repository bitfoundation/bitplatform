using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

public partial class BitNavGroup : IDisposable
{
    private bool SelectedKeyHasBeenSet;
    private string? selectedKey;

    internal IList<BitNavOption> Options = new List<BitNavOption>();

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
            SelectedKeyChanged.InvokeAsync(value);
            ExpandParents(Options);
        }
    }
    [Parameter] public EventCallback<string> SelectedKeyChanged { get; set; }

    protected override string RootElementClass => "bit-nvg";

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

    private static List<BitNavOption> Flatten(IList<BitNavOption> e) => e.SelectMany(c => Flatten(c.Options)).Concat(e).ToList();

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SelectOptionByCurrentUrl();

        StateHasChanged();
    }

    private void SelectOptionByCurrentUrl()
    {
        var currentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        var currentOption = Flatten(Options).FirstOrDefault(option => option.Url == currentUrl);

        SelectedKey = currentOption?.Key;
    }

    private bool ExpandParents(IList<BitNavOption> options)
    {
        foreach (var option in options)
        {
            if (option.Key == SelectedKey || (option.Options.Any() && ExpandParents(option.Options)))
            {
                return option.IsExpanded = true;
            }
        }

        return false;
    }

    internal void RegisterOptions(BitNavOption option)
    {
        if (option.Key.HasNoValue())
        {
            option.Key = $"{Options.Count}";
        }
        Options.Add(option);
        StateHasChanged();
    }

    internal void UnregisterOptions(BitNavOption option)
    {
        Options.Remove(option);
        StateHasChanged();
    }

    internal void InternalStateHasChanged()
    {
        base.StateHasChanged();
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
