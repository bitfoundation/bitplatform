using System.Linq;
using System.Text;

namespace Bit.BlazorUI;

public partial class BitBreadGroup : IDisposable
{
    private int maxDisplayedOptions;
    private int overfelowIndex;
    internal int _internalOverfelowIndex;

    private string _wrapperId => $"{UniqueId}-wrapper";
    private string _calloutId => $"{UniqueId}-callout";
    private string _overlayId => $"{UniqueId}-overlay";
    internal string _overflowDropDownId => $"{UniqueId}-overflow-dropdown";

    private bool _disposed;
    private bool _isCalloutOpen;
    internal List<BitBreadOption> _allOptions = new();
    internal List<BitBreadOption> _displayOptions = new();
    internal List<BitBreadOption> _overflowOptions = new();

    private DotNetObjectReference<BitBreadGroup> _dotnetObj = default!;

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The class HTML attribute for Current Option.
    /// </summary>
    [Parameter] public string? CurrentOptionClass { get; set; }

    /// <summary>
    /// The style HTML attribute for Current Option.
    /// </summary>
    [Parameter] public string? CurrentOptionStyle { get; set; }

    /// <summary>
    /// Render a custom divider in place of the default chevron >
    /// </summary>
    [Parameter] public BitIconName DividerIcon { get; set; } = BitIconName.ChevronRight;

    /// <summary>
    /// The maximum number of breadcrumbs to display before coalescing.
    /// If not specified, all breadcrumbs will be rendered.
    /// </summary>
    [Parameter]
    public int MaxDisplayedOptions
    {
        get => maxDisplayedOptions;
        set
        {
            maxDisplayedOptions = value;
            SetOptionsToShow();
        }
    }

    /// <summary>
    /// Aria label for the overflow button.
    /// </summary>
    [Parameter] public string? OverflowAriaLabel { get; set; }

    /// <summary>
    /// Optional index where overflow Options will be collapsed.
    /// </summary>
    [Parameter]
    public int OverflowIndex 
    {
        get => overfelowIndex;
        set
        {
            overfelowIndex = value;
            _internalOverfelowIndex = value;
            SetOptionsToShow();
        }
    }

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

    private async Task CloseCallout()
    {
        if (IsEnabled is false) return;

        await _js.ToggleOverflowCallout(_dotnetObj, _wrapperId, _overflowDropDownId, _calloutId, _overlayId, _isCalloutOpen);

        _isCalloutOpen = false;

        StateHasChanged();
    }

    internal async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await _js.ToggleOverflowCallout(_dotnetObj, _wrapperId, _overflowDropDownId, _calloutId, _overlayId, _isCalloutOpen);

        _isCalloutOpen = !_isCalloutOpen;
    }

    internal void RegisterOptions(BitBreadOption option)
    {
        _allOptions.Add(option);
        SetOptionsToShow();
    }

    internal void UnRegisterOptions(BitBreadOption option)
    {
        _allOptions.Remove(option);
        SetOptionsToShow();
    }

    private void SetOptionsToShow()
    {
        if (_allOptions.Count == 0) return;

        _displayOptions.Clear();
        _overflowOptions.Clear();

        if (MaxDisplayedOptions == 0 || MaxDisplayedOptions >= _allOptions.Count)
        {
            _displayOptions.AddRange(_allOptions);
        }
        else 
        {
            if (OverflowIndex >= MaxDisplayedOptions || OverflowIndex < 0)
            {
                _internalOverfelowIndex = 0;
            }

            int overflowOptionCount = _allOptions.Count - MaxDisplayedOptions;

            for (int index = 0; index < _allOptions.Count; index++)
            {
                if (_internalOverfelowIndex <= index && overflowOptionCount > 0)
                {
                    _overflowOptions.Add(_allOptions[index]);
                    overflowOptionCount--;
                }
                else
                {
                    _displayOptions.Add(_allOptions[index]);
                }
            }
        }
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
