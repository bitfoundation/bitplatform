using System.Linq;
using System.Text;

namespace Bit.BlazorUI;

public partial class BitBreadGroup : IDisposable
{
    private bool _disposed;
    private bool _isCalloutOpen;
    private uint _internalOverflowIndex;
    private uint _internalMaxDisplayedOptions;
    private List<BitBreadOption> _allOptions = new();
    private List<BitBreadOption> _displayOptions = new();
    private List<BitBreadOption> _overflowOptions = new();
    private DotNetObjectReference<BitBreadGroup> _dotnetObj = default!;

    private string _wrapperId => $"{UniqueId}-wrapper";
    private string _calloutId => $"{UniqueId}-callout";
    private string _overlayId => $"{UniqueId}-overlay";
    private string _overflowDropDownId => $"{UniqueId}-overflow-dropdown";

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// The content of BitBreadGroup, common values are BitBreadGroup component.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The class HTML attribute for Selected Option.
    /// </summary>
    [Parameter] public string? SelectedOptionClass { get; set; }

    /// <summary>
    /// The style HTML attribute for Selected Option.
    /// </summary>
    [Parameter] public string? SelectedOptionStyle { get; set; }

    /// <summary>
    /// Render a custom divider in place of the default chevron.
    /// </summary>
    [Parameter] public BitIconName DividerIcon { get; set; } = BitIconName.ChevronRight;

    /// <summary>
    /// The maximum number of BitBreadGroup to display before coalescing.
    /// If not specified, all BitBreadGroup will be rendered.
    /// </summary>
    [Parameter] public uint MaxDisplayedOptions { get; set; }

    /// <summary>
    /// Aria label for the overflow button.
    /// </summary>
    [Parameter] public string? OverflowAriaLabel { get; set; }

    /// <summary>
    /// Optional index where overflow Options will be collapsed.
    /// </summary>
    [Parameter] public uint OverflowIndex { get; set; }

    /// <summary>
    /// Render a custom overflow icon in place of the default icon.
    /// </summary>
    [Parameter] public BitIconName OverflowIcon { get; set; } = BitIconName.More;

    protected override string RootElementClass => "bit-brg";

    protected override Task OnInitializedAsync()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        return base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        bool shouldCallSetItemsToShow = false;

        shouldCallSetItemsToShow = _internalMaxDisplayedOptions != MaxDisplayedOptions;
        _internalMaxDisplayedOptions = MaxDisplayedOptions == 0 ? (uint)_allOptions.Count : MaxDisplayedOptions;

        shouldCallSetItemsToShow = shouldCallSetItemsToShow || _internalOverflowIndex != OverflowIndex;
        _internalOverflowIndex = OverflowIndex >= _internalMaxDisplayedOptions ? 0 : OverflowIndex;

        if (shouldCallSetItemsToShow)
        {
            SetOptionsToShow();
        }

        await base.OnParametersSetAsync();
    }

    internal void RegisterOptions(BitBreadOption option)
    {
        _allOptions.Add(option);

        _internalMaxDisplayedOptions = MaxDisplayedOptions == 0 ? (uint)_allOptions.Count : MaxDisplayedOptions;

        _internalOverflowIndex = OverflowIndex >= _internalMaxDisplayedOptions ? 0 : OverflowIndex;

        SetOptionsToShow();
        StateHasChanged();
    }

    internal void UnregisterOptions(BitBreadOption option)
    {
        _allOptions.Remove(option);
        SetOptionsToShow();
        StateHasChanged();
    }

    internal void InternalStateHasChanged()
    {
        base.StateHasChanged();
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false) return;

        await _js.ToggleOverflowCallout(_dotnetObj, _wrapperId, _overflowDropDownId, _calloutId, _overlayId, _isCalloutOpen);

        _isCalloutOpen = !_isCalloutOpen;

        StateHasChanged();
    }

    private void SetOptionsToShow()
    {
        _displayOptions.Clear();
        _overflowOptions.Clear();

        if (_internalMaxDisplayedOptions >= _allOptions.Count)
        {
            _displayOptions = _allOptions.ToList();
            return;
        }

        var overflowOptionCount = _allOptions.Count - _internalMaxDisplayedOptions;

        for (int index = 0; index < _allOptions.Count; index++)
        {
            if (_internalOverflowIndex <= index && index < (overflowOptionCount + _internalOverflowIndex))
            {
                _overflowOptions.Add(_allOptions[index]);

                if (index == _internalOverflowIndex)
                {
                    _displayOptions.Add(_allOptions[index]);
                }
            }
            else
            {
                _displayOptions.Add(_allOptions[index]);
            }
        }
    }

    private string GetOptionClasses(BitBreadOption option)
    {
        StringBuilder optionClasses = new();

        optionClasses.Append("option");

        if (option.IsSelected)
        {
            optionClasses.Append(" selected-option");
        }

        if (option.IsSelected && SelectedOptionClass.HasValue())
        {
            optionClasses.Append(' ');
            optionClasses.Append(SelectedOptionClass);
        }

        return optionClasses.ToString();
    }

    private string GetOptionStyles(BitBreadOption option)
    {
        return option.IsSelected && SelectedOptionStyle.HasValue() ? SelectedOptionStyle ?? string.Empty : string.Empty;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (!disposing) return;

        _dotnetObj.Dispose();

        _disposed = true;
    }
}
