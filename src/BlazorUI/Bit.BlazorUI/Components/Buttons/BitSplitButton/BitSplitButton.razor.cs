using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitSplitButton<TItem> where TItem : class
{
    private bool SelectedItemHasBeenSet;

    private bool isCalloutOpen;
    private TItem? selectedItem;
    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;

    private string _calloutId = default!;


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
    private DotNetObjectReference<BitSplitButton<TItem>> _dotnetObj = default!;

    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] private EditContext? _editContext { get; set; }



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
    ///  List of Item, each of which can be a Button with different action in the SplitButton.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// Icon name of the chevron down part of the BitSplitButton.
    /// </summary>
    [Parameter] public string ChevronDownIcon { get; set; } = "ChevronDown";

    /// <summary>
    /// The content of the BitSplitButton, that are BitSplitButtonOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitSplitButton.
    /// </summary>
    [Parameter] public BitSplitButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// Default value of the SelectedItem.
    /// </summary>
    [Parameter] public TItem? DefaultSelectedItem { get; set; }

    /// <summary>
    /// If true, the current item is going to be change selected item.
    /// </summary>
    [Parameter] public bool IsSticky { get; set; }

    /// <summary>
    ///  List of Item, each of which can be a Button with different action in the SplitButton.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = new List<TItem>();

    /// <summary>
    /// The content inside the item can be customized.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitSplitButtonNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// The callback is called when the button or button item is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnClick { get; set; }

    /// <summary>
    /// The callback that is called when the selected item has changed.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnChange { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// Determines the current selected item that acts as the main button.
    /// </summary>
    [Parameter]
    public TItem? SelectedItem
    {
        get => selectedItem;
        set
        {
            if (selectedItem == value) return;

            selectedItem = value;
            ClassBuilder.Reset();
            _ = SelectedItemChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<TItem> SelectedItemChanged { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitSplitButton.
    /// </summary>
    [Parameter] public BitSplitButtonClassStyles? Styles { get; set; }


    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        _isCalloutOpen = false;
        StateHasChanged();
    }


    internal void RegisterOption(BitSplitButtonOption option)
    {
        var item = (option as TItem)!;

        _items.Add(item);

        if (SelectedItemHasBeenSet is false && option.IsSelected)
        {
            SelectedItem = item;
        }

        SelectedItem ??= _items.FirstOrDefault();

        StateHasChanged();
    }

    internal void UnregisterOption(BitSplitButtonOption option)
    {
        _items.Remove((option as TItem)!);
        StateHasChanged();
    }


    protected override string RootElementClass => "bit-spl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsEnabled is false
                                      ? string.Empty
                                      : ButtonStyle == BitButtonStyle.Primary
                                          ? $"{RootElementClass}-pri"
                                          : $"{RootElementClass}-std");

        ClassBuilder.Register(() => _isCalloutOpen ? $"{RootElementClass}-omn" : string.Empty);
        ClassBuilder.Register(() => GetIsEnabled(SelectedItem) ? string.Empty : $"{RootElementClass}-cds");
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _calloutId = $"BitSplitButton-{UniqueId}-callout";

        if (SelectedItemHasBeenSet is false && DefaultSelectedItem is not null)
        {
            SelectedItem = DefaultSelectedItem;
        }

        base.OnInitialized();
    }

    protected override Task OnParametersSetAsync()
    {
        _buttonType = ButtonType ?? (_editContext is null ? BitButtonType.Button : BitButtonType.Submit);

        if (ChildContent is null && Items.Any() && Items != _oldItems)
        {
            _oldItems = Items;
            _items = Items.ToList();

            SelectedItem ??= _items.LastOrDefault(GetIsSelected);
            SelectedItem ??= _items.FirstOrDefault();
        }

        return base.OnParametersSetAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }

        base.OnAfterRender(firstRender);
    }


    private string? GetClass(TItem? item)
    {
        if (item is null) return null;

        if (item is BitSplitButtonItem splitButtonItem)
        {
            return splitButtonItem.Class;
        }

        if (item is BitSplitButtonOption splitButtonOption)
        {
            return splitButtonOption.Class;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Class.Selector is not null)
        {
            return NameSelectors.Class.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Class.Name);
    }

    private string? GetIconName(TItem? item)
    {
        if (item is null) return null;

        if (item is BitSplitButtonItem splitButtonItem)
        {
            return splitButtonItem.IconName;
        }

        if (item is BitSplitButtonOption splitButtonOption)
        {
            return splitButtonOption.IconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.IconName.Selector is not null)
        {
            return NameSelectors.IconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
    }

    private bool GetIsEnabled(TItem? item)
    {
        if (item is null) return false;

        if (item is BitSplitButtonItem splitButtonItem)
        {
            return splitButtonItem.IsEnabled;
        }

        if (item is BitSplitButtonOption splitButtonOption)
        {
            return splitButtonOption.IsEnabled;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.IsEnabled.Selector is not null)
        {
            return NameSelectors.IsEnabled.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsEnabled.Name, true);
    }

    private bool GetIsSelected(TItem? item)
    {
        if (item is null) return false;

        if (item is BitSplitButtonItem splitButtonItem)
        {
            return splitButtonItem.IsSelected;
        }

        if (item is BitSplitButtonOption splitButtonOption)
        {
            return splitButtonOption.IsSelected;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsSelected.Selector is not null)
        {
            return NameSelectors.IsSelected.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsSelected.Name, false);
    }

    private string? GetKey(TItem? item)
    {
        if (item is null) return null;

        if (item is BitSplitButtonItem splitButtonItem)
        {
            return splitButtonItem.Key;
        }

        if (item is BitSplitButtonOption splitButtonOption)
        {
            return splitButtonOption.Key;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Key.Selector is not null)
        {
            return NameSelectors.Key.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Key.Name);
    }

    private string? GetStyle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitSplitButtonItem BitSplitButtonItem)
        {
            return BitSplitButtonItem.Style;
        }

        if (item is BitSplitButtonOption splitButtonOption)
        {
            return splitButtonOption.Style;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    private RenderFragment<TItem>? GetTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitSplitButtonItem BitSplitButtonItem)
        {
            return BitSplitButtonItem.Template as RenderFragment<TItem>;
        }

        if (item is BitSplitButtonOption splitButtonOption)
        {
            return splitButtonOption.Template as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Template.Selector is not null)
        {
            return NameSelectors.Template.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Template.Name);
    }

    private string? GetText(TItem? item)
    {
        if (item is null) return null;

        if (item is BitSplitButtonItem BitSplitButtonItem)
        {
            return BitSplitButtonItem.Text;
        }

        if (item is BitSplitButtonOption splitButtonOption)
        {
            return splitButtonOption.Text;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Text.Selector is not null)
        {
            return NameSelectors.Text.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Text.Name);
    }


    private async Task HandleOnClick(TItem? item)
    {
        if (IsEnabled is false || item is null || GetIsEnabled(item) is false) return;

        await OnClick.InvokeAsync(item);

        await InvokeItemClick(item);
    }

    private async Task HandleOnItemClick(TItem item)
    {
        if (IsSticky)
        {
            if (SelectedItemHasBeenSet is false || SelectedItemChanged.HasDelegate)
            {
                SelectedItem = item;
                await OnChange.InvokeAsync(item);
            }
        }
        else
        {
            if (GetIsEnabled(item) is false) return;

            await OnClick.InvokeAsync(item);

            await InvokeItemClick(item);
        }

        await CloseCallout();
    }

    private async Task InvokeItemClick(TItem item)
    {
        if (item is BitSplitButtonItem splitButtonItem)
        {
            splitButtonItem.OnClick?.Invoke(splitButtonItem);
        }
        else if (item is BitSplitButtonOption splitButtonOption)
        {
            await splitButtonOption.OnClick.InvokeAsync(splitButtonOption);
        }
        else
        {
            if (NameSelectors is null) return;

            if (NameSelectors.OnClick.Selector is not null)
            {
                NameSelectors.OnClick.Selector!(item)?.Invoke(item);
            }
            else
            {
                item.GetValueFromProperty<Action<TItem>?>(NameSelectors.OnClick.Name)?.Invoke(item);
            }
        }
    }

    private async Task OpenCallout()
    {
        _isCalloutOpen = true;
        await ToggleCallout();
    }

    private async Task CloseCallout()
    {
        _isCalloutOpen = false;
        await ToggleCallout();
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false) return;

        await _js.ToggleCallout(_dotnetObj,
                                _Id,
                                _calloutId,
                                _isCalloutOpen,
                                BitResponsiveMode.None,
                                BitDropDirection.TopAndBottom,
                                false,
                                "",
                                0,
                                "",
                                "",
                                false,
                                RootElementClass);
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected async void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        if (_dotnetObj is not null)
        {
            await _js.ClearCallout(_calloutId);
            _dotnetObj.Dispose();
        }

        _disposed = true;
    }
}
