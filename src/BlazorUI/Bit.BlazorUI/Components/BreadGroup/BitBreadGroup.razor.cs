using System.Text;

namespace Bit.BlazorUI;

public partial class BitBreadGroup : IDisposable
{
    protected override bool UseVisual => false;

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
    /// The content of the BitBreadGroup, that are BitBreadOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The divider icon name. The default value is BitIconName.ChevronRight.
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
    /// Optional index where overflow options will be collapsed.
    /// </summary>
    [Parameter] public uint OverflowIndex { get; set; }

    /// <summary>
    /// The overflow icon name. The default value is BitIconName.More.
    /// </summary>
    [Parameter] public BitIconName OverflowIcon { get; set; } = BitIconName.More;

    /// <summary>
    /// The CSS class attribute for the selected option.
    /// </summary>
    [Parameter] public string? SelectedOptionClass { get; set; }

    /// <summary>
    /// The style attribute for the selected option.
    /// </summary>
    [Parameter] public string? SelectedOptionStyle { get; set; }


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
        shouldCallSetItemsToShow = shouldCallSetItemsToShow || _internalOverflowIndex != OverflowIndex;

        SetInternalFields();

        if (shouldCallSetItemsToShow)
        {
            SetOptionsToShow();
        }

        await base.OnParametersSetAsync();
    }

    private void SetInternalFields()
    {
        _internalMaxDisplayedOptions = MaxDisplayedOptions == 0 ? (uint)_allOptions.Count : MaxDisplayedOptions;
        _internalOverflowIndex = OverflowIndex >= _internalMaxDisplayedOptions ? 0 : OverflowIndex;
    }

    internal void RegisterOptions(BitBreadOption option)
    {
        _allOptions.Add(option);
        SetInternalFields();
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

        var cls = option.InternalClassBuilder.Value;
        if (cls.HasValue())
        {
            optionClasses.Append(' ');
            optionClasses.Append(cls);
        }

        return optionClasses.ToString().Trim();
    }

    private string? GetOptionStyles(BitBreadOption option)
    {
        var selectedStyle = (option.IsSelected ? SelectedOptionStyle ?? string.Empty : string.Empty).Trim();
        var optionStyle = (option.InternalStyleBuilder.Value ?? string.Empty).Trim();

        if (selectedStyle.HasNoValue() && optionStyle.HasNoValue()) return null;

        if (selectedStyle.HasValue() && optionStyle.HasValue()) return $"{selectedStyle};{optionStyle}";

        if (selectedStyle.HasValue()) return selectedStyle;

        return optionStyle;
    }

    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        _isCalloutOpen = false;
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
