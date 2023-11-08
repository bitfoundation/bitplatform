using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitMenuButton<TItem> : IDisposable where TItem : class
{
    private bool isCalloutOpen;
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
    ///  List of Item, each of which can be a Button with different action in the BitMenuButton.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// Icon name of the chevron down part of the BitMenuButton.
    /// </summary>
    [Parameter] public string ChevronDownIcon { get; set; } = "ChevronDown";

    /// <summary>
    /// The content of the BitMenuButton, that are BitMenuButtonOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitMenuButton.
    /// </summary>
    [Parameter] public BitMenuButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The content inside the header of BitMenuButton can be customized.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// The icon to show inside the header of BitMenuButton.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    ///  List of BitMenuButtonItem to show as a item in BitMenuButton.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = new List<TItem>();

    /// <summary>
    /// The custom content to render each item.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitMenuButtonNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// The callback is called when the BitMenuButton header is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// OnClick of each item returns that item with its property.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitMenuButton.
    /// </summary>
    [Parameter] public BitMenuButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// The text to show inside the header of BitMenuButton.
    /// </summary>
    [Parameter] public string? Text { get; set; }


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

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsEnabled is false
                                       ? string.Empty
                                       : ButtonStyle == BitButtonStyle.Primary
                                           ? $"{RootElementClass}-pri"
                                           : $"{RootElementClass}-std");

        ClassBuilder.Register(() => _isCalloutOpen ? $"{RootElementClass}-omn" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _calloutId = $"BitMenuButton-{UniqueId}-callout";

        base.OnInitialized();
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

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }

        base.OnAfterRender(firstRender);
    }


    private string? GetClass(TItem item)
    {
        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Class;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Class;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Class.Selector is not null)
        {
            return NameSelectors.Class.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Class.Name);
    }

    private string? GetIconName(TItem item)
    {
        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.IconName;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.IconName.Selector is not null)
        {
            return NameSelectors.IconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
    }

    private bool GetIsEnabled(TItem item)
    {
        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.IsEnabled;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IsEnabled;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.IsEnabled.Selector is not null)
        {
            return NameSelectors.IsEnabled.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsEnabled.Name, true);
    }

    private string? GetKey(TItem item)
    {
        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Key;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Key;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Key.Selector is not null)
        {
            return NameSelectors.Key.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Key.Name);
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

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    private RenderFragment<TItem>? GetTemplate(TItem item)
    {
        if (item is BitMenuButtonItem bitMenuButtonItem)
        {
            return bitMenuButtonItem.Template as RenderFragment<TItem>;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Template as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Template.Selector is not null)
        {
            return NameSelectors.Template.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Template.Name);
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

        if (NameSelectors is null) return null;

        if (NameSelectors.Text.Selector is not null)
        {
            return NameSelectors.Text.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Text.Name);
    }


    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OpenCallout();

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleOnItemClick(TItem item)
    {
        if (IsEnabled is false || GetIsEnabled(item) is false) return;

        await CloseCallout();

        await OnItemClick.InvokeAsync(item);

        if (item is BitMenuButtonItem menuButtonItem)
        {
            menuButtonItem.OnClick?.Invoke(menuButtonItem);
        }
        else if (item is BitMenuButtonOption menuButtonOption)
        {
            await menuButtonOption.OnClick.InvokeAsync(menuButtonOption);
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
