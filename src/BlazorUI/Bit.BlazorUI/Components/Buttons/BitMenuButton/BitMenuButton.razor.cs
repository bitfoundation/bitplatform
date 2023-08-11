using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitMenuButton<TItem> : IDisposable where TItem : class
{
    private const string CLASS_FIELD = nameof(BitMenuButtonItem.Class);
    private const string ICON_NAME_FIELD = nameof(BitMenuButtonItem.IconName);
    private const string IS_ENABLED_FIELD = nameof(BitMenuButtonItem.IsEnabled);
    private const string KEY_FIELD = nameof(BitMenuButtonItem.Key);
    private const string STYLE_FIELD = nameof(BitMenuButtonItem.Style);
    private const string TEXT_FIELD = nameof(BitMenuButtonItem.Text);


    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;
    private bool isCalloutOpen;

    private string _internalClassField = CLASS_FIELD;
    private string _internalIconNameField = ICON_NAME_FIELD;
    private string _internalIsEnabledField = IS_ENABLED_FIELD;
    private string _internalKeyField = KEY_FIELD;
    private string _internalStyleField = STYLE_FIELD;
    private string _internalTextField = TEXT_FIELD;


    private string? _calloutId;
    private string? _overlayId;


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

    private bool _disposed;
    private BitButtonType _buttonType;
    private List<TItem> _items = new();
    private IEnumerable<TItem> _oldItems = default!;
    private DotNetObjectReference<BitMenuButton<TItem>> _dotnetObj = default!;

    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] private EditContext? _editContext { get; set; }



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
    /// The CSS Class field name of the custom input class.
    /// </summary>
    [Parameter] public string ClassField { get; set; } = CLASS_FIELD;

    /// <summary>
    /// The CSS Class field selector of the custom input class.
    /// </summary>
    [Parameter] public Expression<Func<TItem, bool>>? ClassFieldSelector { get; set; }

    /// <summary>
    /// The content inside the header of MenuButton can be customized.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// The icon to show inside the header of MenuButton.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// IconName field name of the custom input class.
    /// </summary>
    [Parameter] public string IconNameField { get; set; } = ICON_NAME_FIELD;

    /// <summary>
    /// IconName field selector of the custom input class.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? IconNameFieldSelector { get; set; }

    /// <summary>
    /// IsEnabled field name of the custom input class.
    /// </summary>
    [Parameter] public string IsEnabledField { get; set; } = IS_ENABLED_FIELD;

    /// <summary>
    /// IsEnabled field selector of the custom input class.
    /// </summary>
    [Parameter] public Expression<Func<TItem, bool>>? IsEnabledFieldSelector { get; set; }

    /// <summary>
    ///  List of BitMenuButtonItem to show as a item in MenuButton.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = new List<TItem>();

    /// <summary>
    /// The custom content to render each item.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Key field name of the custom input class.
    /// </summary>
    [Parameter] public string KeyField { get; set; } = KEY_FIELD;

    /// <summary>
    /// Key field selector of the custom input class.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? KeyFieldSelector { get; set; }

    /// <summary>
    /// The callback is called when the MenuButton header is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// OnClick of each item returns that item with its property.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// The CSS Style field name of the custom input class.
    /// </summary>
    [Parameter] public string StyleField { get; set; } = STYLE_FIELD;

    /// <summary>
    /// The CSS Style field selector of the custom input class.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? StyleFieldSelector { get; set; }

    /// <summary>
    /// The text to show inside the header of MenuButton.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// Text field name of the custom input class.
    /// </summary>
    [Parameter] public string TextField { get; set; } = TEXT_FIELD;

    /// <summary>
    /// Text field selector of the custom input class.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? TextFieldSelector { get; set; }


    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        _isCalloutOpen = false;
        StateHasChanged();
    }


    internal void RegisterOption(BitMenuButtonOption option)
    {
        _items.Add((option as TItem)!);
        StateHasChanged();
    }

    internal void UnregisterOption(BitMenuButtonOption option)
    {
        _items.Remove((option as TItem)!);
        StateHasChanged();
    }


    protected override string RootElementClass => "bit-mnb";

    protected override async Task OnInitializedAsync()
    {
        _calloutId = $"{RootElementClass}-callout-{UniqueId}";
        _overlayId = $"{RootElementClass}-overlay-{UniqueId}";

        _internalClassField = ClassFieldSelector?.GetName() ?? ClassField;
        _internalIsEnabledField = IsEnabledFieldSelector?.GetName() ?? IsEnabledField;
        _internalIconNameField = IconNameFieldSelector?.GetName() ?? IconNameField;
        _internalKeyField = KeyFieldSelector?.GetName() ?? KeyField;
        _internalStyleField = StyleFieldSelector?.GetName() ?? StyleField;
        _internalTextField = TextFieldSelector?.GetName() ?? TextField;

        _dotnetObj = DotNetObjectReference.Create(this);

        await base.OnInitializedAsync();
    }

    protected override Task OnParametersSetAsync()
    {
        _buttonType = ButtonType ?? (_editContext is null ? BitButtonType.Button : BitButtonType.Submit);

        if (ChildContent is null && Items.Any() && Items != _oldItems)
        {
            _oldItems = Items;
            _items = Items.ToList();
        }

        return base.OnParametersSetAsync();
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled is false
                                       ? string.Empty
                                       : ButtonStyle == BitButtonStyle.Primary
                                           ? $"{RootElementClass}-pri"
                                           : $"{RootElementClass}-std");

        ClassBuilder.Register(() => _isCalloutOpen ? $"{RootElementClass}-omn" : string.Empty);
    }

    private string? GetClass(TItem item)
    {
        if (item is BitMenuButtonItem bitMenuButtonItem)
        {
            return bitMenuButtonItem.Class;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Class;
        }

        return item.GetValueFromProperty<string?>(_internalClassField);
    }

    private string? GetIconName(TItem item)
    {
        if (item is BitMenuButtonItem bitMenuButtonItem)
        {
            return bitMenuButtonItem.IconName;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IconName;
        }

        return item.GetValueFromProperty<string?>(_internalIconNameField);
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

        return item.GetValueFromProperty<string?>(_internalKeyField);
    }

    private string? GetStyle(TItem item)
    {
        if (item is BitMenuButtonItem bitMenuButtonItem)
        {
            return bitMenuButtonItem.Style;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Style;
        }

        return item.GetValueFromProperty<string?>(_internalStyleField);
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

        return item.GetValueFromProperty<string?>(_internalTextField);
    }

    private async Task OpenCallout(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        _isCalloutOpen = true;
        await _js.ToggleMenuButtonCallout(_dotnetObj, UniqueId.ToString(), _calloutId, _overlayId, _isCalloutOpen);

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleOnItemClick(TItem item)
    {
        if (IsEnabled is false || GetIsEnabled(item) is false) return;

        await CloseCallout();

        await OnItemClick.InvokeAsync(item);
    }

    private async Task CloseCallout()
    {
        _isCalloutOpen = false;
        await _js.ToggleMenuButtonCallout(_dotnetObj, UniqueId.ToString(), _calloutId, _overlayId, _isCalloutOpen);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        _dotnetObj.Dispose();

        _disposed = true;
    }
}
