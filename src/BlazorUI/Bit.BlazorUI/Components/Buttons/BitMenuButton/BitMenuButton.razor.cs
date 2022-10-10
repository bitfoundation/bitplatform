using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitMenuButton
{
    private bool _isCalloutOpen;
    private string? _menuButtonId;
    private string? _menuButtonCalloutId;
    private string? _menuButtonOverlayId;
    private BitButtonStyle _buttonStyle = BitButtonStyle.Primary;

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
    [Parameter] public BitButtonStyle ButtonStyle
    {
        get => _buttonStyle;
        set
        {
            _buttonStyle = value;
            ClassBuilder.Reset();
        }
    }

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
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public EventCallback<string> OnItemClick { get; set; }

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
        ButtonType ??= EditContext is null
            ? BitButtonType.Button
            : BitButtonType.Submit;

        return base.OnParametersSetAsync();
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled is false
                                       ? string.Empty
                                       : ButtonStyle == BitButtonStyle.Primary
                                           ? "primary"
                                           : "standard");
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        var obj = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("BitMenuButton.toggleMenuButtonCallout", obj, UniqueId, _menuButtonId, _menuButtonCalloutId, _menuButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = true;

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleOnItemClick(BitMenuButtonItem item)
    {
        if (IsEnabled is false || item.IsEnabled is false) return;

        var obj = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("BitMenuButton.toggleMenuButtonCallout", obj, UniqueId, _menuButtonId, _menuButtonCalloutId, _menuButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = false;

        await OnItemClick.InvokeAsync(item.key);
    }

    private async Task CloseCallout()
    {
        var obj = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("BitMenuButton.toggleMenuButtonCallout", obj, UniqueId, _menuButtonId, _menuButtonCalloutId, _menuButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = false;
    }
}
