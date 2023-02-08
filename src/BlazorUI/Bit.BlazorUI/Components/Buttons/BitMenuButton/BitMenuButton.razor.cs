using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitMenuButton
{
    protected override bool UseVisual => false;

    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;
    private bool isCalloutOpen;
    private string _menuButtonId => $"{RootElementClass}-{UniqueId}";
    private string _menuButtonCalloutId => $"{RootElementClass}-callout-{UniqueId}";
    private string _menuButtonOverlayId => $"{RootElementClass}-overlay-{UniqueId}";

    private bool _isCalloutOpen
    {
        get => isCalloutOpen;
        set
        {
            if (isCalloutOpen == value) return;

            isCalloutOpen = value;
            ClassBuilder.Reset();
        }
    }

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// Detailed description of the button for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the element.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The style of button, Possible values: Primary | Standard.
    /// </summary>
    [Parameter] public BitButtonStyle ButtonStyle
    {
        get => buttonStyle;
        set
        {
            buttonStyle = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    ///  List of Item, each of which can be a Button with different action in the MenuButton.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The content inside the header of MenuButton can be customized.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// The icon to show inside the header of MenuButton.
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    ///  List of BitMenuButtonItem to show as a item in MenuButton.
    /// </summary>
    [Parameter] public IEnumerable<BitMenuButtonItem> Items { get; set; } = new List<BitMenuButtonItem>();

    /// <summary>
    /// The content inside the MenuButton-item can be customized.
    /// </summary>
    [Parameter] public RenderFragment<BitMenuButtonItem>? ItemTemplate { get; set; }

    /// <summary>
    /// The callback is called when the MenuButton header is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// OnClick of each item returns that item with its property.
    /// </summary>
    [Parameter] public EventCallback<BitMenuButtonItem> OnItemClick { get; set; }

    /// <summary>
    /// The text to show inside the header of MenuButton.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] private EditContext? _editContext { get; set; }

    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        _isCalloutOpen = false;
        StateHasChanged();
    }

    protected override string RootElementClass => "bit-mnb";

    protected override Task OnParametersSetAsync()
    {
        ButtonType ??= _editContext is null
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

        ClassBuilder.Register(() => _isCalloutOpen
                                       ? "open-menu"
                                       : string.Empty);
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        var obj = DotNetObjectReference.Create(this);
        await _js.ToggleMenuButtonCallout(obj, UniqueId.ToString(), _menuButtonId, _menuButtonCalloutId, _menuButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = true;

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleOnItemClick(BitMenuButtonItem item)
    {
        if (IsEnabled is false || item.IsEnabled is false) return;

        var obj = DotNetObjectReference.Create(this);
        await _js.ToggleMenuButtonCallout(obj, UniqueId.ToString(), _menuButtonId, _menuButtonCalloutId, _menuButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = false;

        await OnItemClick.InvokeAsync(item);
    }

    private async Task CloseCallout()
    {
        var obj = DotNetObjectReference.Create(this);
        await _js.ToggleMenuButtonCallout(obj, UniqueId.ToString(), _menuButtonId, _menuButtonCalloutId, _menuButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = false;
    }
}
