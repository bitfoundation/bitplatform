using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

public partial class BitNavOption : IDisposable
{
    private bool IsExpandedHasBeenSet;
    private bool isExpanded;

    private bool _parentIsExpanded;
    private event EventHandler<bool>? _internalIsExpandedChanged;
    internal string _internalKey = default!;
    private int _depth;

    private Dictionary<BitNavItemAriaCurrent, string> _ariaCurrentMap = new()
    {
        [BitNavItemAriaCurrent.Page] = "page",
        [BitNavItemAriaCurrent.Step] = "step",
        [BitNavItemAriaCurrent.Location] = "location",
        [BitNavItemAriaCurrent.Time] = "time",
        [BitNavItemAriaCurrent.Date] = "date",
        [BitNavItemAriaCurrent.True] = "true"
    };

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;

    [CascadingParameter] protected BitNavGroup? NavGroup { get; set; }
    [CascadingParameter] internal BitNavOption? Parent { get; set; }

    /// <summary>
    /// Aria-current token for active nav option.
    /// Must be a valid token value, and defaults to 'page'.
    /// </summary>
    [Parameter] public BitNavItemAriaCurrent AriaCurrent { get; set; } = BitNavItemAriaCurrent.Page;

    /// <summary>
    /// Aria label when options is collapsed and can be expanded.
    /// </summary>
    [Parameter] public string? CollapseAriaLabel { get; set; }

    /// <summary>
    /// Options to render as children of the current option.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Expand option initially.
    /// </summary>
    [Parameter] public bool? DefaultIsExpanded { get; set; }

    /// <summary>
    /// Aria label when group is collapsed and can be expanded.
    /// </summary>
    [Parameter] public string? ExpandAriaLabel { get; set; }

    /// <summary>
    /// (Optional) By default, any link with onClick defined will render as a button. 
    /// Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)
    /// </summary>
    [Parameter] public bool ForceAnchor { get; set; }

    /// <summary>
    /// Name of an icon to render next to this link button.
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    /// Whether or not the option is in an expanded state.
    /// </summary>
    [Parameter] 
    public bool IsExpanded 
    { 
        get => isExpanded;
        set
        {
            if (value == isExpanded) return;
            isExpanded = value;
            _ = IsExpandedChanged.InvokeAsync(value);
            _internalIsExpandedChanged?.Invoke(this, IsExpanded);
        }
    }
    [Parameter] public EventCallback<bool> IsExpandedChanged { get; set; }

    /// <summary>
    /// A unique value to use as a key or id of the option.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Text to render for this option.
    /// </summary>
    [Parameter] public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Text for title tooltip.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Link target, specifies how to open the option.
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// URL to navigate to for this option.
    /// </summary>
#pragma warning disable CA1056 // URI-like properties should not be strings
    [Parameter] public string? Url { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings

    protected override string RootElementClass => "bit-nvgo";

    protected override async Task OnInitializedAsync()
    {
        _internalKey = Key ?? UniqueId.ToString();

        if (DefaultIsExpanded is not null)
        {
            IsExpanded = DefaultIsExpanded.Value;
        }

        if (Parent is not null)
        {
            _depth = Parent._depth + 1;

            Parent._internalIsExpandedChanged += ParentIsExpandedChanged;
        }

        if (NavGroup is not null)
        {
            NavGroup.Options.Add(this);
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        if (IsExpanded)
        {
            _internalIsExpandedChanged?.Invoke(this, IsExpanded);
        }
    }

    private void ParentIsExpandedChanged(object? sender, bool isExpanded)
    {
        if (_parentIsExpanded && isExpanded is false)
        {
            _internalIsExpandedChanged?.Invoke(this, isExpanded);
        }

        _parentIsExpanded = isExpanded;

        StateHasChanged();
    }

    private async Task HandleOnClick()
    {
        if (IsEnabled == false) return;
        if (NavGroup is null) return;

        await NavGroup.OnOptionClick.InvokeAsync(this);

        if (ChildContent is not null && Url.HasNoValue())
        {
            await ToggleOption();
        }
        else if (NavGroup.Mode == BitNavMode.Manual)
        {
            NavGroup.SelectedKey = _internalKey;
            await NavGroup.OnSelectOption.InvokeAsync(this);
            StateHasChanged();
        }
    }

    private async Task ToggleOption()
    {
        if (IsEnabled is false) return;
        if (NavGroup is null) return;
        if (IsExpandedHasBeenSet && IsExpandedChanged.HasDelegate is false) return;

        IsExpanded = !IsExpanded;

        await NavGroup.OnOptionToggle.InvokeAsync(this);
    }

    private bool HasChevronButton()
    {
        return (NavGroup?.RenderType is BitNavRenderType.Normal && ChildContent is not null) || 
               (NavGroup?.RenderType is BitNavRenderType.Grouped && ChildContent is not null && Parent is not null);
    }

    private string GetOptionClasses()
    {
        if (NavGroup is null) return string.Empty;

        var enabledClass = NavGroup.IsEnabled is false || IsEnabled is false ? "disabled" : "";

        var isSelected = NavGroup.SelectedKey == _internalKey ? "selected" : "";

        var isHeader = NavGroup.RenderType == BitNavRenderType.Grouped && Parent is null ? "group-header" : "";

        return $"{enabledClass} {isSelected} {isHeader}";
    }

    private static bool IsRelativeUrl(string url) => new Regex(@"!/^[a-z0-9+-.]+:\/\//i").IsMatch(url);

    internal void InternalStateHasChanged() => base.StateHasChanged();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing is false) return;

        if (Parent is not null)
        {
            Parent._internalIsExpandedChanged -= ParentIsExpandedChanged;
        }

        if (NavGroup is not null)
        {
            NavGroup.Options.Remove(this);
        }
    }
}
