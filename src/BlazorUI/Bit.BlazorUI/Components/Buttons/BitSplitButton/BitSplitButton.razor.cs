
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitSplitButton
{
    private BitSplitButtonItem? _currentItem;
    private bool _isCalloutOpen;
    private string? _splitButtonId;
    private string? _splitButtonCalloutId;
    private string? _splitButtonOverlayId;
    private string? _buttonStyle;

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// Detailed description of the button for the benefit of screen readers
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the element
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The style of button, Possible values: Primary | Standard
    /// </summary>
    [Parameter] public BitButtonStyle ButtonStyle { get; set; } = BitButtonStyle.Primary;

    /// <summary>
    /// The type of the button
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }

    /// <summary>
    /// If true, the current item is going to be change selected item.
    /// </summary>
    [Parameter] public bool IsSticky { get; set; }

    /// <summary>
    ///  List of Item, each of which can be a Button with different action in the SplitButton.
    /// </summary>
    [Parameter] public IEnumerable<BitSplitButtonItem> Items { get; set; } = new List<BitSplitButtonItem>();

    /// <summary>
    /// The callback is called when the button or button item is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitSplitButtonItem> OnClick { get; set; }

    protected override string RootElementClass => "bit-splb";

    protected override async Task OnInitializedAsync()
    {
        _currentItem = Items.FirstOrDefault();
        _splitButtonId = $"{RootElementClass}-{UniqueId}";
        _splitButtonCalloutId = $"{RootElementClass}-callout-{UniqueId}";
        _splitButtonOverlayId = $"{RootElementClass}-overlay-{UniqueId}";

        await base.OnInitializedAsync();
    }

    protected override Task OnParametersSetAsync()
    {
        _buttonStyle = IsEnabled
            ? ButtonStyle is BitButtonStyle.Primary ? "primary" : "standard"
            : null;

        ButtonType ??= EditContext is null
            ? BitButtonType.Button
            : BitButtonType.Submit;

        return base.OnParametersSetAsync();
    }

    private async Task HandleOnClick(BitSplitButtonItem? item)
    {
        if (item?.IsEnabled is false) return;

        await OnClick.InvokeAsync(item);
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false) return;

        var obj = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("BitSplitButton.toggleSplitButtonCallout", obj, UniqueId, _splitButtonId, _splitButtonCalloutId, _splitButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = !_isCalloutOpen;
    }

    private async Task HandleOnItemClick(BitSplitButtonItem item)
    {
        if (IsSticky) 
        {
            _currentItem = item;
        }
        else
        {
            if (item.IsEnabled is false) return;

            await OnClick.InvokeAsync(item);
        }

        var obj = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("BitSplitButton.toggleSplitButtonCallout", obj, UniqueId, _splitButtonId, _splitButtonCalloutId, _splitButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = false;
    }

    private async Task CloseCallout()
    {
        var obj = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("BitSplitButton.toggleSplitButtonCallout", obj, UniqueId, _splitButtonId, _splitButtonCalloutId, _splitButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = false;
    }
}
