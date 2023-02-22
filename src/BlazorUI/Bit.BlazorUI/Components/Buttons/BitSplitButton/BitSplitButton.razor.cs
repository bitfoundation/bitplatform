using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitSplitButton<TItem> where TItem : class
{
    private const string IS_ENABLED_FIELD = nameof(BitSplitButtonItem.IsEnabled);
    private const string ICON_NAME_FIELD = nameof(BitSplitButtonItem.IconName);
    private const string TEXT_FIELD = nameof(BitSplitButtonItem.Text);
    private const string KEY_FIELD = nameof(BitSplitButtonItem.Key);

    protected override bool UseVisual => false;
    private BitButtonSize buttonSize = BitButtonSize.Medium;
    private bool isCalloutOpen;

    private string _internalIsEnabledField = IS_ENABLED_FIELD;
    private string _internalIconNameField = ICON_NAME_FIELD;
    private string _internalTextField = TEXT_FIELD;
    private string _internalkeyField = KEY_FIELD;

    private List<TItem> _children = new();
    private IEnumerable<TItem> _oldItems;
    private TItem? _currentItem;

    private string _splitButtonId => $"{RootElementClass}-{UniqueId}";
    private string _splitButtonCalloutId => $"{RootElementClass}-callout-{UniqueId}";
    private string _splitButtonOverlayId => $"{RootElementClass}-overlay-{UniqueId}";

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
    /// Detailed description of the button for the benefit of screen readers
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the element
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
            buttonSize = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The style of button, Possible values: Primary | Standard
    /// </summary>
    [Parameter] public BitButtonStyle ButtonStyle { get; set; } = BitButtonStyle.Primary;

    /// <summary>
    ///  List of Item, each of which can be a Button with different action in the SplitButton.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The content of the BitSplitButton, that are BitSplitButtonOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The content inside the item can be customized.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// If true, the current item is going to be change selected item.
    /// </summary>
    [Parameter] public bool IsSticky { get; set; }

    /// <summary>
    ///  List of Item, each of which can be a Button with different action in the SplitButton.
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
    /// The callback is called when the button or button item is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnClick { get; set; }

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

    protected override string RootElementClass => "bit-spl";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled is false
                               ? string.Empty
                               : ButtonStyle == BitButtonStyle.Primary
                                   ? "primary"
                                   : "standard");

        ClassBuilder.Register(() => ButtonSize == BitButtonSize.Small
                               ? "small"
                               : ButtonSize == BitButtonSize.Medium
                                   ? "medium"
                                   : "large");

        ClassBuilder.Register(() => _isCalloutOpen
                                       ? "open-menu"
                                       : string.Empty);
    }

    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        _isCalloutOpen = false;
        StateHasChanged();
    }

    internal void RegisterOption(BitSplitButtonOption option)
    {
        _children.Add((option as TItem)!);

        if (_currentItem is null)
        {
            _currentItem = _children.FirstOrDefault();
        }
    }

    internal void UnregisterOption(BitSplitButtonOption option)
    {
        _children.Remove((option as TItem)!);
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

        if (ChildContent is null && Items.Any() && Items != _oldItems)
        {
            _oldItems = Items;
            _children = Items.ToList();
            _currentItem = _children.FirstOrDefault();
        }

        return base.OnParametersSetAsync();
    }

    private BitIconName? GetIconName(TItem item)
    {
        if (item is BitSplitButtonItem splitButtonItem)
        {
            return splitButtonItem.IconName;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IconName;
        }

        return item.GetValueFromProperty<BitIconName>(_internalIconNameField);
    }

    private string? GetText(TItem item)
    {
        if (item is BitSplitButtonItem splitButtonItem)
        {
            return splitButtonItem.Text;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Text;
        }

        return item.GetValueAsObjectFromProperty(_internalTextField)?.ToString();
    }

    private string? GetKey(TItem item)
    {
        if (item is BitSplitButtonItem splitButtonItem)
        {
            return splitButtonItem.Key;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Key;
        }

        return item.GetValueAsObjectFromProperty(_internalkeyField)?.ToString();
    }

    private bool GetIsEnabled(TItem item)
    {
        if (item is BitSplitButtonItem splitButtonItem)
        {
            return splitButtonItem.IsEnabled;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IsEnabled;
        }

        return item.GetValueFromProperty(_internalIsEnabledField, true);
    }

    private async Task HandleOnClick(TItem? item)
    {
        if (IsEnabled is false || item is null || GetIsEnabled(item) is false) return;

        await OnClick.InvokeAsync(item);
    }

    private async Task HandleOnItemClick(TItem item)
    {
        if (IsSticky)
        {
            _currentItem = item;
        }
        else
        {
            if (GetIsEnabled(item) is false) return;

            await OnClick.InvokeAsync(item);
        }

        var obj = DotNetObjectReference.Create(this);
        await _js.ToggleSplitButtonCallout(obj, UniqueId.ToString(), _splitButtonId, _splitButtonCalloutId, _splitButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = false;
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false) return;

        var obj = DotNetObjectReference.Create(this);
        await _js.ToggleSplitButtonCallout(obj, UniqueId.ToString(), _splitButtonId, _splitButtonCalloutId, _splitButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = !_isCalloutOpen;
    }

    private async Task CloseCallout()
    {
        var obj = DotNetObjectReference.Create(this);
        await _js.ToggleSplitButtonCallout(obj, UniqueId.ToString(), _splitButtonId, _splitButtonCalloutId, _splitButtonOverlayId, _isCalloutOpen);
        _isCalloutOpen = false;
    }
}
