using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

/// <summary>
/// A menu button combines a button with a callout menu of related actions or links.
/// It supports split and sticky modes, toggle behavior, checkable items, group headers, separators,
/// links, keyboard shortcuts, a loading state, and full keyboard navigation with proper ARIA menu semantics.
/// </summary>
public partial class BitMenuButton<TItem> : BitComponentBase where TItem : class
{
    private List<TItem> _items = [];
    internal BitButtonType _buttonType;
    private string _calloutId = default!;
    private string _overlayId = default!;
    private bool _focusFirstItemOnOpen;
    private ElementReference _operatorButtonRef;
    private ElementReference _chevronButtonRef;
    private IEnumerable<TItem> _oldItems = default!;
    private DotNetObjectReference<BitMenuButton<TItem>> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The EditContext, which is set if the menu button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] protected EditContext? EditContext { get; set; }



    /// <summary>
    /// Detailed description of the menu button for the benefit of screen readers (rendered into
    /// <c>aria-describedby</c>).
    /// </summary>
    /// <remarks>
    /// It is rendered as visually hidden text beside the button and read after its name, not as part of it.
    /// An <c>aria-describedby</c> written on the component by hand is kept and this description is added to it,
    /// since the attribute is a list of ids.
    /// </remarks>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the menu button.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The background color kind of the callout.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    ///  The value of the type attribute of the menu button.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The icon of the check mark shown on a checked item, using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CheckIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? CheckIcon { get; set; }

    /// <summary>
    /// The name of the icon of the check mark shown on a checked item.
    /// </summary>
    [Parameter] public string? CheckIconName { get; set; }

    /// <summary>
    /// The aria-label of the chevron down button of the split menu button for the benefit of screen readers.
    /// Defaults to <c>More options</c>: the chevron carries no text of its own, so without a name it reaches
    /// a screen reader as an unlabelled button.
    /// </summary>
    [Parameter] public string? ChevronDownAriaLabel { get; set; }

    /// <summary>
    /// The icon for the chevron down part of the menu button.
    /// </summary>
    [Parameter] public BitIconInfo? ChevronDownIcon { get; set; }

    /// <summary>
    /// The icon name of the chevron down part of the menu button.
    /// </summary>
    [Parameter] public string? ChevronDownIconName { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the chevron down button of the split menu button.
    /// </summary>
    [Parameter] public string? ChevronDownTitle { get; set; }

    /// <summary>
    /// The content of the menu button, that are BitMenuButtonOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the menu button.
    /// </summary>
    [Parameter] public BitMenuButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// Closes the callout when an item is clicked, which is what a menu of one-off commands wants.
    /// </summary>
    /// <remarks>
    /// Turn it off for a menu the user works inside of - a set of checkable items such as a column picker -
    /// so that several items can be toggled without reopening the menu between them.
    /// </remarks>
    [Parameter] public bool CloseOnItemClick { get; set; } = true;

    /// <summary>
    /// The general color of the menu button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Default value of the IsToggled parameter in toggle mode.
    /// </summary>
    [Parameter] public bool? DefaultIsToggled { get; set; }

    /// <summary>
    /// Default value of the SelectedItem.
    /// </summary>
    [Parameter] public TItem? DefaultSelectedItem { get; set; }

    /// <summary>
    /// Keeps a disabled menu button, and the disabled items of any menu button, focusable: the disabled state is
    /// conveyed with the <c>aria-disabled</c> attribute instead of the native <c>disabled</c> one, so the button
    /// stays in the tab order, the arrow keys still reach the items, and a screen reader announces both that they
    /// exist and that they are unavailable. Their actions stay suppressed either way.
    /// </summary>
    [Parameter] public bool DisabledInteractive { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

    /// <summary>
    /// Expands the menu button width to 100% of the available width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The content inside the header of menu button can be customized.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The icon to show inside the header of menu button.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// If true, removes the icon from the header button.
    /// </summary>
    [Parameter] public bool NoIcon { get; set; }

    /// <summary>
    /// Determines whether the menu button is in the loading state.
    /// It replaces the default icon of the header button with a spinner and ignores its click.
    /// In split mode the chevron still opens the menu, so the rest of the commands stay reachable.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IsLoading { get; set; }

    /// <summary>
    /// Determines the opening state of the callout.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetIsOpen))]
    [ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Determines whether the header button is in the checked/toggled state when Toggle is enabled.
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsToggled { get; set; }

    /// <summary>
    ///  List of items to show in the menu button.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// The custom template content to render each item.
    /// </summary>
    [Parameter] public RenderFragment<TItem?>? ItemTemplate { get; set; }

    /// <summary>
    /// The text to show beside the spinner while the menu button is in the loading state, replacing the text of
    /// the header button. It is also announced by screen readers through a status live region when the loading
    /// state starts, which is what tells a user who cannot see the spinner that the operation is running.
    /// </summary>
    [Parameter] public string? LoadingLabel { get; set; }

    /// <summary>
    /// The tallest the callout grows before its items start to scroll, as a CSS length (e.g. <c>12rem</c>).
    /// Without one the callout is capped to the room the viewport leaves below or above the button.
    /// </summary>
    [Parameter] public string? MaxHeight { get; set; }

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
    /// The callback that is called when the IsToggled value changes in toggle mode.
    /// </summary>
    [Parameter] public EventCallback<bool> OnToggleChange { get; set; }

    /// <summary>
    /// Alias of the ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// Determines the current selected item that acts as the header item.
    /// </summary>
    [Parameter, ResetClassBuilder, TwoWayBound]
    [CallOnSet(nameof(OnSetSelectedItem))]
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
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool Sticky { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the menu button.
    /// </summary>
    [Parameter] public BitMenuButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// The text to show inside the header of menu button.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the header button.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// If true, enables the toggling behavior on the header button in split mode.
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder] 
    public bool Toggle { get; set; }

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

        if (Sticky)
        {
            if (SelectedItemHasBeenSet is false && option.IsSelected)
            {
                _ = AssignSelectedItem(item);
            }

            if (SelectedItem is null)
            {
                _ = AssignSelectedItem(_items.FirstOrDefault(IsSelectable));
            }
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

        ClassBuilder.Register(() => FullWidth ? "bit-mnb-flw" : string.Empty);

        ClassBuilder.Register(() => IsLoading ? "bit-mnb-lod" : string.Empty);

        ClassBuilder.Register(() => Split ? "bit-mnb-spl" : "bit-mnb-nsp");

        ClassBuilder.Register(() => Toggle && Split && IsToggled ? $"bit-mnb-tgl {Classes?.Toggled}" : string.Empty);

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

        StyleBuilder.Register(() => Toggle && Split && IsToggled ? Styles?.Toggled : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _calloutId = $"BitMenuButton-{UniqueId}-callout";
        _overlayId = $"BitMenuButton-{UniqueId}-overlay";

        if (Split && Toggle && IsToggledHasBeenSet is false && DefaultIsToggled.HasValue)
        {
            await AssignIsToggled(DefaultIsToggled.Value);
        }

        if (Sticky && SelectedItemHasBeenSet is false && DefaultSelectedItem is not null)
        {
            await AssignSelectedItem(DefaultSelectedItem);
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        _buttonType = ButtonType ?? (EditContext is null ? BitButtonType.Button : BitButtonType.Submit);

        // Options render their items themselves and Blazor skips re-rendering them when only the
        // menu button's own parameters (Styles, Sticky, ItemTemplate, ...) change, so push a re-render to each one.
        RefreshOptions();

        if (ChildContent is not null || Options is not null || Items.Any() is false || Items == _oldItems) return;

        _oldItems = Items;
        _items = [.. Items];

        if (Sticky is false) return;

        if (SelectedItem is not null) return;

        var item = _items.LastOrDefault(GetIsSelected);

        if (item is not null)
        {
            if (await AssignSelectedItem(item) is false) return;
        }
        else
        {
            item = _items.FirstOrDefault(IsSelectable);
            await AssignSelectedItem(item);
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }

        base.OnAfterRender(firstRender);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            try
            {
                // Prevents the default behavior (scrolling) of the navigation keys handled by the
                // keydown handlers, since Blazor cannot conditionally preventDefault per key.
                await _js.BitMenuButtonsSetup(_Id, _calloutId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }
    }



    // Whether any item of the menu is checkable, which is what decides that every item reserves the check
    // column: the labels of the checked and the unchecked rows have to line up with each other, and a column
    // that appears only on the checked ones would shift the label of a row as its state changes. It is read
    // once per item render rather than cached, since an option is free to turn Checkable on at any time.
    internal bool ShowCheckColumn => _items.Exists(GetCheckable);

    // The items a sticky menu button can promote to its header: the ones that are commands in their own right,
    // which leaves out the separators and the group labels that only structure the list around them.
    private bool IsSelectable(TItem item)
    {
        return GetIsEnabled(item) && GetIsSeparator(item) is false && GetIsHeader(item) is false;
    }

    internal string? GetCheckIconCss()
    {
        return BitIconInfo.From(CheckIcon, CheckIconName ?? "Accept")?.GetCssClasses();
    }

    internal string? GetAriaLabel(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.AriaLabel;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.AriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.AriaLabel.Selector is not null)
        {
            return NameSelectors.AriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.AriaLabel.Name);
    }

    internal bool GetCheckable(TItem? item)
    {
        if (item is null) return false;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Checkable;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Checkable;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.Checkable.Selector is not null)
        {
            return NameSelectors.Checkable.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.Checkable.Name, false);
    }

    internal bool GetIsChecked(TItem? item)
    {
        if (item is null) return false;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.IsChecked;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IsChecked;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsChecked.Selector is not null)
        {
            return NameSelectors.IsChecked.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsChecked.Name, false);
    }

    internal bool GetIsHeader(TItem? item)
    {
        if (item is null) return false;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.IsHeader;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IsHeader;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsHeader.Selector is not null)
        {
            return NameSelectors.IsHeader.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsHeader.Name, false);
    }

    internal string? GetSecondaryText(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.SecondaryText;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.SecondaryText;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.SecondaryText.Selector is not null)
        {
            return NameSelectors.SecondaryText.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.SecondaryText.Name);
    }

    internal string? GetClass(TItem? item)
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

    internal BitIconInfo? GetIcon(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return BitIconInfo.From(menuButtonItem.Icon, menuButtonItem.IconName);
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return BitIconInfo.From(menuButtonOption.Icon, menuButtonOption.IconName);
        }

        if (NameSelectors is null) return null;

        BitIconInfo? icon = null;
        if (NameSelectors.Icon.Selector is not null)
        {
            icon = NameSelectors.Icon.Selector!(item);
        }
        else
        {
            icon = item.GetValueFromProperty<BitIconInfo?>(NameSelectors.Icon.Name);
        }

        string? iconName = null;
        if (NameSelectors.IconName.Selector is not null)
        {
            iconName = NameSelectors.IconName.Selector!(item);
        }
        else
        {
            iconName = item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
        }

        return BitIconInfo.From(icon, iconName);
    }

    internal string? GetHref(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Href;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Href;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Href.Selector is not null)
        {
            return NameSelectors.Href.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Href.Name);
    }

    internal bool GetIsEnabled(TItem? item)
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

    internal bool GetIsSeparator(TItem? item)
    {
        if (item is null) return false;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.IsSeparator;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IsSeparator;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsSeparator.Selector is not null)
        {
            return NameSelectors.IsSeparator.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsSeparator.Name, false);
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

    internal string? GetStyle(TItem? item)
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

    internal string? GetTarget(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Target;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Target;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Target.Selector is not null)
        {
            return NameSelectors.Target.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Target.Name);
    }

    internal RenderFragment<TItem>? GetTemplate(TItem? item)
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

    internal string? GetText(TItem? item)
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

    internal string? GetTitle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Title;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Title;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Title.Selector is not null)
        {
            return NameSelectors.Title.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Title.Name);
    }

    private async Task HandleOnHeaderClick(TItem? item)
    {
        if (IsEnabled is false || IsLoading) return;

        if (Split is false)
        {
            // The trigger of a plain menu button toggles its menu. With the pointer the overlay takes the
            // click that would close it, but from the keyboard the trigger keeps the focus after it was
            // opened, so Enter and Space arrive here with the menu already open - and have to close it.
            if (IsOpen)
            {
                _focusFirstItemOnOpen = false;
                await CloseCallout();
                StateHasChanged();
                return;
            }

            await OpenCallout();
        }

        if (Toggle && Split)
        {
            if (await AssignIsToggled(!IsToggled) is false) return;

            await OnToggleChange.InvokeAsync(IsToggled);
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

    internal async Task HandleOnItemClick(TItem item)
    {
        if (IsEnabled is false || GetIsEnabled(item) is false) return;

        // A checkable item's state changes on activation, so it is flipped before anything is told about the
        // click: a handler that reads the item back sees the state its own click produced.
        if (GetCheckable(item))
        {
            await SetIsChecked(item, GetIsChecked(item) is false);
        }

        if (CloseOnItemClick)
        {
            await CloseCallout();
        }

        // CloseCallout changes IsOpen but does not re-render the root itself, so refresh now to update
        // the open-state classes even when the Sticky branch below returns early.
        StateHasChanged();

        if (Sticky)
        {
            if (await AssignSelectedItem(item) is false) return;

            await OnChange.InvokeAsync(item);
        }
        else
        {
            await OnClick.InvokeAsync(item);

            await InvokeItemClick(item);
        }

        // The click handler runs on the clicked item's renderer, so the root element (open state
        // classes) needs an explicit re-render here.
        StateHasChanged();

        // The focused item is hidden along with the callout, so return the focus to the trigger button. A menu
        // that stays open keeps the focus where the user left it, which is what lets the next arrow key
        // continue from the item just toggled instead of starting over.
        if (CloseOnItemClick)
        {
            await FocusTrigger();
        }
    }

    private async Task SetIsChecked(TItem item, bool value)
    {
        if (item is BitMenuButtonItem menuButtonItem)
        {
            menuButtonItem.IsChecked = value;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            await menuButtonOption.SetIsChecked(value);
        }

        if (NameSelectors is null) return;

        item.SetValueToProperty(NameSelectors.IsChecked.Name, value);
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

    private async Task HandleOnTriggerKeyDown(KeyboardEventArgs e, bool opener)
    {
        if (IsEnabled is false) return;

        if (e.Key is "Escape")
        {
            if (IsOpen is false) return;

            await CloseCallout();
            StateHasChanged();
            return;
        }

        if (opener is false) return;

        // The header of a plain menu button is the only way into the menu, and the loading state is what takes
        // its click away - so it takes the keys that stand in for that click with it, rather than leaving the
        // keyboard a way past a guard the pointer answers to.
        if (Split is false && IsLoading) return;

        if (e.Key is "ArrowDown" or "ArrowUp")
        {
            if (await OpenCallout() is false) return;

            await FocusItem(e.Key is "ArrowDown" ? "first" : "last");
        }
        else if (e.Key is "Enter" or " ")
        {
            // The native click that follows this keydown opens the callout, so
            // mark it to move the focus to the first item as the APG pattern requires.
            _focusFirstItemOnOpen = true;
        }
    }

    private async Task HandleOnCalloutKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || IsOpen is false) return;

        switch (e.Key)
        {
            case "ArrowDown":
                await FocusItem("next");
                break;
            case "ArrowUp":
                await FocusItem("prev");
                break;
            case "Home":
                await FocusItem("first");
                break;
            case "End":
                await FocusItem("last");
                break;
            case "Escape":
            case "Tab":
                await CloseCallout();
                StateHasChanged();
                await FocusTrigger();
                break;
            default:
                if (e.Key?.Length is 1 && e.Key != " " && e.CtrlKey is false && e.AltKey is false && e.MetaKey is false)
                {
                    await FocusItem("char", e.Key);
                }
                break;
        }
    }

    private ValueTask FocusItem(string mode, string? character = null)
    {
        // A disabled item is normally stepped over, but DisabledInteractive is the page asking for it to be
        // reachable - which is the whole point of keeping it focusable rather than natively disabled.
        return _js.BitMenuButtonsFocusItem(_calloutId, mode, character, DisabledInteractive);
    }

    private async Task FocusTrigger()
    {
        if (Split)
        {
            await _chevronButtonRef.FocusAsync();
        }
        else
        {
            await _operatorButtonRef.FocusAsync();
        }
    }

    /// <summary>
    /// Opens the callout and reports whether it actually opened, so callers can skip
    /// follow-up work (like focusing an item) on a menu that stayed closed.
    /// </summary>
    private async Task<bool> OpenCallout()
    {
        // The loading state belongs to the header button alone: in split mode the chevron still opens the menu,
        // so the operation running under the main half never takes the rest of the commands away with it.
        if (IsEnabled is false) return false;

        var focusFirstItem = _focusFirstItemOnOpen;
        _focusFirstItemOnOpen = false;

        if (await AssignIsOpen(true) is false) return false;

        await ToggleCallout();

        if (focusFirstItem)
        {
            await FocusItem("first");
        }

        return true;
    }

    private async Task CloseCallout()
    {
        if (await AssignIsOpen(false) is false) return;

        await ToggleCallout();
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false || IsDisposed) return;

        await _js.BitCalloutToggleCallout(
            dotnetObj: _dotnetObj,
            componentId: _Id,
            component: null,
            calloutId: _calloutId,
            callout: null,
            overlayId: _overlayId,
            isCalloutOpen: IsOpen,
            responsiveMode: BitResponsiveMode.None,
            dropDirection: DropDirection,
            isRtl: Dir is BitDir.Rtl,
            // With nothing else named as the scrollable part, the callout itself takes that role, so that a menu
            // taller than the screen scrolls inside the callout instead of running off the bottom of it - where a
            // fixed-positioned element leaves it out of reach of the page's own scrolling. An author-set MaxHeight
            // is a cap of its own (a CSS custom property the stylesheet reads), so the fitting pass steps aside.
            scrollContainerId: MaxHeight.HasValue() ? "" : _calloutId,
            scrollOffset: 0,
            headerId: "",
            footerId: "",
            setCalloutWidth: true,
            fixedCalloutWidth: false,
            maxWindowWidth: 0);
    }

    private void OnSetIsOpen()
    {
        _ = ToggleCallout();
    }

    private void OnSetSelectedItem()
    {
        // The selected item affects both the sticky header (rendered by this component) and the
        // items rendered by the options themselves, so re-render all of them.
        RefreshOptions();
        StateHasChanged();
    }

    private void RefreshOptions()
    {
        // In the Items API there are no registered options, so there is nothing to refresh.
        if ((Options ?? ChildContent) is null) return;

        foreach (var item in _items)
        {
            (item as BitMenuButtonOption)?.InternalStateHasChanged();
        }
    }

    private string GetItemKey(TItem item, string defaultKey)
    {
        return GetKey(item) ?? $"{UniqueId}-{defaultKey}";
    }

    private string? GetCalloutCss()
    {
        List<string> classes = [];

        if (IsOpen)
        {
            classes.Add("bit-mnb-ocl");
        }

        // While open the callout is reparented to the body, which takes it out of the subtree that
        // carries the root's bit-fam class, so ForceAnimation has to be rendered on the callout
        // itself for its opening animation to opt out of reduced motion.
        if (ForceAnimation)
        {
            classes.Add("bit-fam");
        }

        if (MaxHeight.HasValue())
        {
            classes.Add("bit-mnb-mxh");
        }

        // The callout is rendered outside the root element - and moved to the body while it is open - so it is
        // a sibling of the root rather than a descendant of it, and nothing the root declares reaches it. The
        // two classes that carry what the items need are repeated here: the size class sizes their text, their
        // height and their padding, and the color class paints the focus ring of the focused one and the glyph
        // of a checked one in the color the menu button was given.
        classes.Add(BitCssClasses.Color(Color, "bit-mnb"));
        classes.Add(BitCssClasses.Size(Size ?? BitSize.Medium, "bit-mnb"));

        var bgClass = Background switch
        {
            BitColorKind.Primary => "bit-mnb-bpg",
            BitColorKind.Secondary => "bit-mnb-bsg",
            BitColorKind.Tertiary => "bit-mnb-btg",
            BitColorKind.Transparent => "bit-mnb-brg",
            _ => null
        };

        if (bgClass is not null)
        {
            classes.Add(bgClass);
        }

        var result = string.Join(' ', classes).Trim();
        return result.HasValue() ? result : null;
    }

    private string? GetCalloutStyle()
    {
        // The positioning code clears the callout's inline sizing on every layout pass, so the cap travels as a
        // custom property the stylesheet reads instead of as a max-height of its own.
        var maxHeight = MaxHeight.HasValue() ? $"--bit-MenuButton-callout-max-height:{MaxHeight};" : null;

        var result = $"{maxHeight}{Styles?.Callout}";
        return result.HasValue() ? result : null;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        if (_dotnetObj is not null)
        {
            _dotnetObj.Dispose();

            try
            {
                await _js.BitCalloutClearCallout(_calloutId);
                await _js.BitMenuButtonsDispose(_Id);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }
    }
}
