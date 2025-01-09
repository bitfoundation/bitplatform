using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

/// <summary>
/// A menu button is a menu item that displays a word or phrase that the user can click to initiate an operation.
/// </summary>
public partial class BitMenuButton<TItem> : BitComponentBase, IAsyncDisposable where TItem : class
{
    private bool _disposed;
    private List<TItem> _items = [];
    private BitButtonType _buttonType;
    private string _calloutId = default!;
    private IEnumerable<TItem> _oldItems = default!;
    private DotNetObjectReference<BitMenuButton<TItem>> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The EditContext, which is set if the menu button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] protected EditContext? EditContext { get; set; }



    /// <summary>
    /// Detailed description of the menu button for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the menu button.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    ///  The value of the type attribute of the menu button.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The icon name of the chevron down part of the menu button.
    /// </summary>
    [Parameter] public string? ChevronDownIcon { get; set; }

    /// <summary>
    /// The content of the menu button, that are BitMenuButtonOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the menu button.
    /// </summary>
    [Parameter] public BitMenuButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the menu button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Default value of the SelectedItem.
    /// </summary>
    [Parameter] public TItem? DefaultSelectedItem { get; set; }

    /// <summary>
    /// The content inside the header of menu button can be customized.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// The icon to show inside the header of menu button.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Determines the opening state of the callout.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(ToggleCallout))]
    [ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    ///  List of items to show in the menu button.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// The custom template content to render each item.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitMenuButtonNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// The callback is called when the menu button header is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem?> OnClick { get; set; }

    /// <summary>
    /// The callback that is called when the selected item has changed.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnChange { get; set; }

    /// <summary>
    /// Alias of the ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// Determines the current selected item that acts as the header item.
    /// </summary>
    [Parameter, ResetClassBuilder, TwoWayBound]
    public TItem? SelectedItem { get; set; }

    /// <summary>
    /// The size of the menu button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, the menu button renders as a split button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Split { get; set; }

    /// <summary>
    /// If true, the selected item is going to change the header item.
    /// </summary>
    [Parameter] public bool Sticky { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the menu button.
    /// </summary>
    [Parameter] public BitMenuButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// The text to show inside the header of menu button.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The visual variant of the menu button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    [JSInvokable("CloseCallout")]
    public async Task CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }



    internal void RegisterOption(BitMenuButtonOption option)
    {
        var item = (option as TItem)!;

        _items.Add(item);

        if (SelectedItemHasBeenSet is false && option.IsSelected)
        {
            _ = AssignSelectedItem(item);
        }

        if (SelectedItem is null)
        {
            _ = AssignSelectedItem(_items.FirstOrDefault(GetIsEnabled));
        }

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

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-mnb-pri",
            BitColor.Secondary => "bit-mnb-sec",
            BitColor.Tertiary => "bit-mnb-ter",
            BitColor.Info => "bit-mnb-inf",
            BitColor.Success => "bit-mnb-suc",
            BitColor.Warning => "bit-mnb-wrn",
            BitColor.SevereWarning => "bit-mnb-swr",
            BitColor.Error => "bit-mnb-err",
            BitColor.PrimaryBackground => "bit-mnb-pbg",
            BitColor.SecondaryBackground => "bit-mnb-sbg",
            BitColor.TertiaryBackground => "bit-mnb-tbg",
            BitColor.PrimaryForeground => "bit-mnb-pfg",
            BitColor.SecondaryForeground => "bit-mnb-sfg",
            BitColor.TertiaryForeground => "bit-mnb-tfg",
            BitColor.PrimaryBorder => "bit-mnb-pbr",
            BitColor.SecondaryBorder => "bit-mnb-sbr",
            BitColor.TertiaryBorder => "bit-mnb-tbr",
            _ => "bit-mnb-pri"
        });

        ClassBuilder.Register(() => IsOpen ? "bit-mnb-omn" : string.Empty);
        ClassBuilder.Register(() => IsOpen ? Classes?.Opened : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-mnb-sm",
            BitSize.Medium => "bit-mnb-md",
            BitSize.Large => "bit-mnb-lg",
            _ => "bit-mnb-md"
        });

        ClassBuilder.Register(() => Split ? "bit-mnb-spl" : "bit-mnb-nsp");

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-mnb-fil",
            BitVariant.Outline => "bit-mnb-otl",
            BitVariant.Text => "bit-mnb-txt",
            _ => "bit-mnb-fil"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => IsOpen ? Styles?.Opened : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        _calloutId = $"BitMenuButton-{UniqueId}-callout";

        if (SelectedItemHasBeenSet is false && DefaultSelectedItem is not null)
        {
            await AssignSelectedItem(DefaultSelectedItem);
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        _buttonType = ButtonType ?? (EditContext is null ? BitButtonType.Button : BitButtonType.Submit);

        if (ChildContent is not null || Items.Any() is false || Items == _oldItems) return;

        _oldItems = Items;
        _items = Items.ToList();

        if (SelectedItem is not null) return;

        var item = _items.LastOrDefault(GetIsSelected);

        if (item is not null)
        {
            if (await AssignSelectedItem(item) is false) return;
        }
        else
        {
            item = _items.FirstOrDefault(GetIsEnabled);
            await AssignSelectedItem(item);
        }
    }



    private string? GetClass(TItem? item)
    {
        if (item is null) return null;

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

    private string? GetIconName(TItem? item)
    {
        if (item is null) return null;

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

    private bool GetIsEnabled(TItem? item)
    {
        if (item is null) return false;

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

    private bool GetIsSelected(TItem? item)
    {
        if (item is null) return false;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.IsSelected;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IsSelected;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsSelected.Selector is not null)
        {
            return NameSelectors.IsSelected.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsSelected.Name, false);
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

    private string? GetStyle(TItem? item)
    {
        if (item is null) return null;

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

    private RenderFragment<TItem>? GetTemplate(TItem? item)
    {
        if (item is null) return null;

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

    private string? GetText(TItem? item)
    {
        if (item is null) return null;

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

    private async Task HandleOnHeaderClick(TItem? item)
    {
        if (IsEnabled is false) return;

        if (Split is false)
        {
            await OpenCallout();
        }

        if (item is not null)
        {
            if (GetIsEnabled(item) is false) return;

            await OnClick.InvokeAsync(item);

            await InvokeItemClick(item);
        }
        else
        {
            await OnClick.InvokeAsync();
        }
    }

    private async Task HandleOnItemClick(TItem item)
    {
        if (IsEnabled is false || GetIsEnabled(item) is false) return;

        await CloseCallout();

        if (Sticky is false)
        {
            if (GetIsEnabled(item) is false) return;

            await OnClick.InvokeAsync(item);

            await InvokeItemClick(item);
        }
        else
        {
            if (await AssignSelectedItem(item) is false) return;

            await OnChange.InvokeAsync(item);
        }
    }

    private async Task InvokeItemClick(TItem item)
    {
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
        if (await AssignIsOpen(true) is false) return;

        await ToggleCallout();
    }

    private async Task CloseCallout()
    {
        if (await AssignIsOpen(false) is false) return;

        await ToggleCallout();
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false) return;

        await _js.BitCalloutToggleCallout(_dotnetObj,
                                _Id,
                                null,
                                _calloutId,
                                null,
                                IsOpen,
                                BitResponsiveMode.None,
                                BitDropDirection.TopAndBottom,
                                Dir is BitDir.Rtl,
                                "",
                                0,
                                "",
                                "",
                                true);
    }

    private string GetItemKey(TItem item, string defaultKey)
    {
        return GetKey(item) ?? $"{UniqueId}-{defaultKey}";
    }



    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        if (_dotnetObj is not null)
        {
            _dotnetObj.Dispose();

            try
            {
                await _js.BitCalloutClearCallout(_calloutId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        _disposed = true;
    }
}
