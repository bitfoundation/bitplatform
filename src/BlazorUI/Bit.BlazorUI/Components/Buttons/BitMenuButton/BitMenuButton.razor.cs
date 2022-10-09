using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitMenuButton
{
    private bool _isCalloutOpen;
    private string? _menuButtonId;
    private string? _menuButtonCalloutId;
    private string? _menuButtonOverlayId;
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
    ///  List of Item, each of which can be a Button with different action in the SplitButton.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public RenderFragment<BitMenuButtonItem>? ItemTemplate { get; set; }


    /// <summary>
    ///  
    /// </summary>
    [Parameter] public IEnumerable<BitMenuButtonItem> Items { get; set; } = new List<BitMenuButtonItem>();

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public string? Text { get; set; }

    protected override string RootElementClass => "bit-mnb";

    protected override async Task OnInitializedAsync()
    {
        _menuButtonId = $"{RootElementClass}-{UniqueId}";
        _menuButtonCalloutId = $"{RootElementClass}-callout-{UniqueId}";
        _menuButtonOverlayId = $"{RootElementClass}-overlay-{UniqueId}";

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

    private async Task HandleOnClick()
    {
        if (IsEnabled is false) return;

        var obj = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("BitMenuButton.toggleSplitButtonCallout", obj, UniqueId, _menuButtonId, _menuButtonCalloutId, _menuButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = !_isCalloutOpen;

        await OnClick.InvokeAsync();
    }

    private async Task HandleOnItemClick(BitMenuButtonItem item)
    {
        if (item.IsEnabled is false) return;

        await OnClick.InvokeAsync();
    }

    private async Task CloseCallout()
    {
        var obj = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("BitMenuButton.toggleSplitButtonCallout", obj, UniqueId, _menuButtonId, _menuButtonCalloutId, _menuButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = false;
    }
}
