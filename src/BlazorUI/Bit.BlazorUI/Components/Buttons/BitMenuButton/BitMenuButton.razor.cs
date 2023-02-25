using System.Linq.Expressions;
using System.Xml;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitMenuButton<TItem> where TItem : class
{
    private const string IS_ENABLED_FIELD = nameof(BitMenuButtonItem.IsEnabled);
    private const string ICON_NAME_FIELD = nameof(BitMenuButtonItem.IconName);
    private const string TEXT_FIELD = nameof(BitMenuButtonItem.Text);
    private const string KEY_FIELD = nameof(BitMenuButtonItem.Key);

    protected override bool UseVisual => false;
    private BitButtonSize buttonSize = BitButtonSize.Medium;
    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;
    private bool isCalloutOpen;

    private string _internalIsEnabledField = IS_ENABLED_FIELD;
    private string _internalIconNameField = ICON_NAME_FIELD;
    private string _internalTextField = TEXT_FIELD;
    private string _internalkeyField = KEY_FIELD;

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
    /// The size of button, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter]
    public BitButtonSize ButtonSize
    {
        get => buttonSize;
        set
        {
            if (buttonSize == value) return;

            buttonSize = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The style of button, Possible values: Primary | Standard.
    /// </summary>
    [Parameter]
    public BitButtonStyle ButtonStyle
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
    /// The content of the BitMenuButton, that are BitMenuButtonOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

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
    [Parameter] public IEnumerable<TItem> Items { get; set; } = new List<TItem>();

    /// <summary>
    /// Whether or not the item is enabled.
    /// </summary>
    [Parameter] public string IsEnabledField { get; set; } = IS_ENABLED_FIELD;

    /// <summary>
    /// Whether or not the item is enabled.
    /// </summary>
    [Parameter] public Expression<Func<TItem, bool>>? IsEnabledFieldSelector { get; set; }

    /// <summary>
    /// Name of an icon to render next to the item text.
    /// </summary>
    [Parameter] public string IconNameField { get; set; } = ICON_NAME_FIELD;

    /// <summary>
    /// Name of an icon to render next to the item text.
    /// </summary>
    [Parameter] public Expression<Func<TItem, BitIconName>>? IconNameFieldSelector { get; set; }

    /// <summary>
    /// The content inside the MenuButton-item can be customized.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// The callback is called when the MenuButton header is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// OnClick of each item returns that item with its property.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// The text to show inside the header of MenuButton.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// Name of an icon to render next to the item text.
    /// </summary>
    [Parameter] public string TextField { get; set; } = TEXT_FIELD;

    /// <summary>
    /// Name of an icon to render next to the item text.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? TextFieldSelector { get; set; }

    /// <summary>
    /// A unique value to use as a key of the item.
    /// </summary>
    [Parameter] public string KeyField { get; set; } = KEY_FIELD;

    /// <summary>
    /// A unique value to use as a key of the item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? KeyFieldSelector { get; set; }

    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] private EditContext? _editContext { get; set; }

    protected override string RootElementClass => "bit-mnb";

    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        _isCalloutOpen = false;
        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        _internalIsEnabledField = IsEnabledFieldSelector?.GetName() ?? IsEnabledField;
        _internalIconNameField = IconNameFieldSelector?.GetName() ?? IconNameField;
        _internalTextField = TextFieldSelector?.GetName() ?? TextField;
        _internalkeyField = KeyFieldSelector?.GetName() ?? KeyField;

        await base.OnInitializedAsync();
    }

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

        ClassBuilder.Register(() => ButtonSize switch
        {
            BitButtonSize.Small => "small",
            BitButtonSize.Large => "large",
            _ => "medium"
        });

        ClassBuilder.Register(() => _isCalloutOpen
                                       ? "open-menu"
                                       : string.Empty);
    }

    private BitIconName? GetIconName(TItem item) 
    {
        if (item is BitMenuButtonItem bitMenuButtonItem)
        {
            return bitMenuButtonItem.IconName;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IconName;
        }

        return item.GetValueFromProperty<BitIconName>(_internalIconNameField);
    } 

    private string? GetText(TItem item) 
    {
        if (item is BitMenuButtonItem bitMenuButtonItem)
        {
            return bitMenuButtonItem.Text;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Text;
        }

        return item.GetValueAsObjectFromProperty(_internalTextField)?.ToString();
    } 

    private string? GetKey(TItem item)
    {
        if (item is BitMenuButtonItem bitMenuButtonItem)
        {
            return bitMenuButtonItem.Key;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Key;
        }

        return item.GetValueAsObjectFromProperty(_internalkeyField)?.ToString();
    }

    private bool GetIsEnabled(TItem item) 
    {
        if (item is BitMenuButtonItem bitMenuButtonItem)
        {
            return bitMenuButtonItem.IsEnabled;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IsEnabled;
        }

        return item.GetValueFromProperty(_internalIsEnabledField, true);
    } 

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        var obj = DotNetObjectReference.Create(this);
        await _js.ToggleMenuButtonCallout(obj, UniqueId.ToString(), _menuButtonId, _menuButtonCalloutId, _menuButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = true;

        await OnClick.InvokeAsync(e);
    }

    internal async Task HandleOnItemClick(TItem item)
    {
        if (IsEnabled is false || GetIsEnabled(item) is false) return;

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
