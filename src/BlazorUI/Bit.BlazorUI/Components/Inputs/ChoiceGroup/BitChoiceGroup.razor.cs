using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// ChoiceGroup let people select a single option from two or more choices.
/// </summary>
public partial class BitChoiceGroup<TItem, TValue> : BitInputBase<TValue> where TItem : class, new()
{
    private List<TItem> _items = [];
    private string _name = default!;
    private bool _autoFocusDone;
    private string _labelId = default!;
    private bool _optionsOrderDirty;
    private bool _defaultValueApplied;
    private TValue? _appliedDefaultValue;
    private string _optionsContainerId = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Id of an element to use as the aria label for the ChoiceGroup.
    /// </summary>
    [Parameter] public string? AriaLabelledBy { get; set; }

    /// <summary>
    /// Determines if the ChoiceGroup is auto focused on first render, focusing its checked item (or its first item).
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Keeps the assigned Index of each option in sync with the markup order of the options, even when
    /// an option is added, removed, or reordered conditionally after the first render (an option that
    /// appears later registers itself at the end of the list regardless of its markup position). This is
    /// achieved by reading the DOM order of the options after each render, so it adds a JS interop call
    /// per render and is opt-in. It only affects the options API (ChildContent/Options); the items API
    /// already follows the order of the Items collection.
    /// </summary>
    [Parameter] public bool AutoReorderOptions { get; set; }

    /// <summary>
    /// The content of the ChoiceGroup, a list of BitChoiceGroupOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitChoiceGroup.
    /// </summary>
    [Parameter] public BitChoiceGroupClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the BitChoiceGroup.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The gap between the items of the ChoiceGroup.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Gap { get; set; }

    /// <summary>
    /// Renders the items in the ChoiceGroup horizontally.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Horizontal { get; set; }

    /// <summary>
    /// Renders the icons and images in a single line with the items in the ChoiceGroup.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Inline { get; set; }

    /// <summary>
    /// Sets the data source that populates the items of the list.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// Used to customize the label for the Item Label content.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemLabelTemplate { get; set; }

    /// <summary>
    /// Used to add a prefix to each item.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemPrefixTemplate { get; set; }

    /// <summary>
    /// Used to customize the label for the Item content.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// The label for the ChoiceGroup.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom RenderFragment for the label of the ChoiceGroup.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitChoiceGroupNameSelectors<TItem, TValue>? NameSelectors { get; set; }

    /// <summary>
    /// Removes the circle from the start of each item.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoCircle { get; set; }

    /// <summary>
    /// Callback for when an item of the ChoiceGroup loses focus.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnBlur { get; set; }

    /// <summary>
    /// Callback for when the option clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnClick { get; set; }

    /// <summary>
    /// Callback for when an item of the ChoiceGroup receives focus.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnFocus { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// Reverses the label and radio button location.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// The size of the BitChoiceGroup.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitChoiceGroup.
    /// </summary>
    [Parameter] public BitChoiceGroupClassStyles? Styles { get; set; }



    internal void RegisterOption(BitChoiceGroupOption<TValue> option)
    {
        _items.Add((option as TItem)!);
        _optionsOrderDirty = true;

        SetIndexItems();

        InitDefaultValue();

        UpdateIsSelected();

        StateHasChanged();
    }

    internal void UnregisterOption(BitChoiceGroupOption<TValue> option)
    {
        if (IsDisposed) return;

        _items.Remove((option as TItem)!);
        _optionsOrderDirty = true;

        SetIndexItems();

        StateHasChanged();
    }

    // Reorders the registered options based on the DOM order of their rendered markers, since an option
    // that gets conditionally rendered (or reordered) after the first render registers itself at the end
    // of the items list, no matter where in the markup it is located. Opt-in via AutoReorderOptions.
    internal void ReorderOptions(string[] orderedOptionIds)
    {
        if (orderedOptionIds.Length == 0) return;

        List<TItem> ordered = new(_items.Count);

        foreach (var optionId in orderedOptionIds)
        {
            var item = _items.FirstOrDefault(i => (i as BitChoiceGroupOption<TValue>)?._OptionId == optionId);
            if (item is null || ordered.Contains(item)) continue;

            ordered.Add(item);
        }

        if (ordered.Count == 0) return;

        ordered.AddRange(_items.Except(ordered));

        if (ordered.SequenceEqual(_items)) return;

        _items = ordered;

        SetIndexItems();

        StateHasChanged();
    }

    // Emits the marker attribute the DOM read-back uses to recover the markup order of the options.
    // Only rendered when AutoReorderOptions is enabled (to avoid the extra attribute otherwise) and only
    // for options (the items API keeps the Items collection order and needs no marker).
    internal Dictionary<string, object>? GetItemMarkerAttributes(TItem item)
    {
        if (AutoReorderOptions is false) return null;
        if (item is not BitChoiceGroupOption<TValue> option) return null;

        return new() { [BitChoiceGroupOption<TValue>._OPTION_ID_ATTRIBUTE] = option._OptionId };
    }



    protected override async Task OnInitializedAsync()
    {
        _name = $"BitChoiceGroup-{UniqueId}-input-name";
        _labelId = $"BitChoiceGroup-{UniqueId}-label";
        _optionsContainerId = $"BitChoiceGroup-{UniqueId}-options-container";

        InitDefaultValue();

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (AutoReorderOptions is false) return;
        if (ChildContent is null && Options is null) return;
        if (_optionsOrderDirty is false) return;

        _optionsOrderDirty = false;

        try
        {
            var orderedOptionIds = await _js.BitUtilsGetChildrenAttributes(_optionsContainerId, BitChoiceGroupOption<TValue>._OPTION_ID_ATTRIBUTE);
            if (IsDisposed) return;
            if (orderedOptionIds is not null)
            {
                ReorderOptions(orderedOptionIds);
            }
        }
        catch (JSDisconnectedException) { } // the circuit is gone (e.g. the user navigated away), nothing to reorder
        catch (JSException) { } // a JS-side failure while reading the marker order is not fatal, keep the current order
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Opt-in: a pure reorder of existing options registers/unregisters nothing, so flag the DOM
        // read-back to run after this render to detect it. The read-back only mutates when the order
        // actually changed, so a stable order just costs one DOM read.
        if (AutoReorderOptions && (ChildContent is not null || Options is not null))
        {
            _optionsOrderDirty = true;
        }

        // Options render their items themselves and Blazor skips re-rendering them when only the
        // choice group's own parameters (Value, Styles, NoCircle, ...) change, so push a re-render to each one.
        RefreshOptions();

        // A new DefaultValue has to be applied again, the previous one has already had its chance.
        if (EqualityComparer<TValue>.Default.Equals(DefaultValue, _appliedDefaultValue) is false)
        {
            _appliedDefaultValue = DefaultValue;
            _defaultValueApplied = false;
        }

        if (ChildContent is null && Options is null)
        {
            // The comparison is against the items the component currently holds (not against the previously
            // assigned collection), so an in-place mutation of the same collection instance is detected too,
            // and clearing the collection actually clears the rendered items.
            var items = Items ?? [];

            if (_items.SequenceEqual(items) is false)
            {
                _items = [.. items];

                SetIndexItems();
            }
        }

        InitDefaultValue();

        // Value can change without the items changing (one-way binding, a programmatic assignment, ...),
        // so the IsSelected of every item is refreshed on each parameters set, not only when the items change.
        UpdateIsSelected();
    }

    protected override string RootElementClass => "bit-chg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Inline ? "bit-chg-inl" : string.Empty);

        ClassBuilder.Register(() => NoCircle ? "bit-chg-ncr" : "bit-chg-wcr");

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-chg-req" : string.Empty);

        ClassBuilder.Register(() => Horizontal ? "bit-chg-hor" : string.Empty);

        ClassBuilder.Register(() => Reversed ? "bit-chg-rvs" : string.Empty);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-chg-pri",
            BitColor.Secondary => "bit-chg-sec",
            BitColor.Tertiary => "bit-chg-ter",
            BitColor.Info => "bit-chg-inf",
            BitColor.Success => "bit-chg-suc",
            BitColor.Warning => "bit-chg-wrn",
            BitColor.SevereWarning => "bit-chg-swr",
            BitColor.Error => "bit-chg-err",
            BitColor.PrimaryBackground => "bit-chg-pbg",
            BitColor.SecondaryBackground => "bit-chg-sbg",
            BitColor.TertiaryBackground => "bit-chg-tbg",
            BitColor.PrimaryForeground => "bit-chg-pfg",
            BitColor.SecondaryForeground => "bit-chg-sfg",
            BitColor.TertiaryForeground => "bit-chg-tfg",
            BitColor.PrimaryBorder => "bit-chg-pbr",
            BitColor.SecondaryBorder => "bit-chg-sbr",
            BitColor.TertiaryBorder => "bit-chg-tbr",
            _ => "bit-chg-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-chg-sm",
            BitSize.Medium => "bit-chg-md",
            BitSize.Large => "bit-chg-lg",
            _ => "bit-chg-md"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Gap.HasValue() ? $"--bit-chg-item-gap:{Gap}" : string.Empty);
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");



    // Applies the DefaultValue of the uncontrolled mode at most once per assigned DefaultValue, so a later
    // change of the items (or an option registering itself afterwards) cannot revert the user's own choice.
    private void InitDefaultValue()
    {
        if (ValueHasBeenSet || _defaultValueApplied || DefaultValue is null) return;

        var item = _items.FirstOrDefault(item => EqualityComparer<TValue>.Default.Equals(GetValue(item), DefaultValue));
        if (item is null) return;

        _defaultValueApplied = true;

        Value = DefaultValue;
    }

    private void SetIndexItems()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            SetIndex(_items[i], i);
        }
    }

    // The internal label element is only rendered (and therefore only worth referencing) when there is a
    // label to show. Referencing it unconditionally would point the radiogroup at an empty element, and
    // since aria-labelledby wins over aria-label that would leave the group without an accessible name.
    internal bool HasLabel => LabelTemplate is not null || Label.HasValue();

    private string? GetAriaLabelledBy() => AriaLabelledBy ?? (HasLabel ? _labelId : null);

    // The index is used instead of the value, because a value is free-form (it can contain spaces or any
    // other character that is not valid in an id) and is not guaranteed to be unique among the items,
    // while the id has to be a valid, unique IDREF for the label's "for" and for aria-describedby.
    internal string? GetInputId(TItem item) => GetId(item) ?? $"ChoiceGroup-{UniqueId}-Input-{GetItemIndex(item)}";

    private int GetItemIndex(TItem item) => _items.FindIndex(i => ReferenceEquals(i, item));

    // Keeps the InputElement of the base class pointing at the input the Tab key would land on: the input
    // of the checked item, or the first enabled one when nothing is checked (the browser skips a disabled
    // radio when tabbing into the group). Called by each item after render; also performs the one-time AutoFocus.
    internal async Task SetInputElement(TItem item, ElementReference inputElement)
    {
        if (GetIsCheckedItem(item) is false)
        {
            if (_items.Any(GetIsCheckedItem)) return;
            if (ReferenceEquals(GetTabTargetItem(), item) is false) return;
        }

        InputElement = inputElement;

        if (AutoFocus && _autoFocusDone is false && IsEnabled && ReadOnly is false && GetIsEnabled(item))
        {
            _autoFocusDone = true;
            await InputElement.FocusAsync();
        }
    }

    private TItem? GetTabTargetItem() => _items.FirstOrDefault(GetIsItemEnabled) ?? _items.FirstOrDefault();

    internal async Task HandleFocus(TItem item)
    {
        await OnFocus.InvokeAsync(item);
    }

    internal async Task HandleBlur(TItem item)
    {
        await OnBlur.InvokeAsync(item);
    }

    internal async Task HandleClick(TItem item)
    {
        if (IsEnabled is false || ReadOnly || GetIsEnabled(item) is false) return;

        await OnClick.InvokeAsync(item);
    }

    internal void HandleChange(TItem item)
    {
        if (IsEnabled is false || ReadOnly || GetIsEnabled(item) is false) return;

        CurrentValue = GetValue(item);

        // After the value has been written, so an assignment the base class rejects
        // (a bound Value without a way to write it back) does not leave a wrong IsSelected behind.
        UpdateIsSelected();

        RefreshOptions();

        StateHasChanged();
    }

    private void RefreshOptions()
    {
        // In the Items API there are no registered options, so there is nothing to refresh.
        if ((Options ?? ChildContent) is null) return;

        foreach (var item in _items)
        {
            (item as BitChoiceGroupOption<TValue>)?.InternalStateHasChanged();
        }
    }

    internal bool GetIsCheckedItem(TItem item)
    {
        if (CurrentValue is null) return false;

        return EqualityComparer<TValue>.Default.Equals(GetValue(item), CurrentValue);
    }

    // The parts are joined with a semicolon: they are separate declaration lists and a value that does not
    // already end with one (a plain "color:red" of an item's Style) would otherwise merge into the next part.
    internal string GetItemContainerCssStyles(TItem item)
    {
        List<string> styles = [];

        var itemStyle = GetStyle(item);
        if (itemStyle.HasValue())
        {
            styles.Add(itemStyle!);
        }

        if (Styles?.ItemContainer.HasValue() ?? false)
        {
            styles.Add(Styles.ItemContainer!);
        }

        if (GetIsCheckedItem(item) && (Styles?.ItemChecked.HasValue() ?? false))
        {
            styles.Add(Styles.ItemChecked!);
        }

        return string.Join(';', styles);
    }

    internal string GetItemContainerCssClasses(TItem item)
    {
        List<string> classes = ["bit-chg-icn"];

        var itemClass = GetClass(item);
        if (itemClass.HasValue())
        {
            classes.Add(itemClass!);
        }

        if (Classes?.ItemContainer.HasValue() ?? false)
        {
            classes.Add(Classes.ItemContainer!);
        }

        // The checked and disabled markers are emitted for every rendering mode, including the templated
        // ones, so Classes.ItemChecked stays in sync with Styles.ItemChecked and a custom template can
        // style its checked and disabled states through the same hooks the built-in rendering uses.
        if (GetIsCheckedItem(item))
        {
            classes.Add("bit-chg-ich");

            if (Classes?.ItemChecked.HasValue() ?? false)
            {
                classes.Add(Classes.ItemChecked!);
            }
        }

        if (GetIsItemEnabled(item) is false)
        {
            classes.Add("bit-chg-ids");
        }

        // Only meaningful for the built-in item content, a template renders its own image or icon.
        if (GetTemplate(item) is null && ItemTemplate is null && ItemLabelTemplate is null &&
            (GetImageSrc(item).HasValue() || GetIcon(item) is not null))
        {
            classes.Add("bit-chg-ihi");
        }

        return string.Join(' ', classes);
    }

    // The tile layout only applies to the built-in image/icon markup of an item, so it follows the exact
    // same conditions that markup is rendered under; a template renders its own content and lays it out itself.
    internal string GetItemLabelCssClasses(TItem item)
    {
        if (Inline) return string.Empty;
        if (GetTemplate(item) is not null || ItemTemplate is not null || ItemLabelTemplate is not null) return string.Empty;

        return GetImageSrc(item).HasValue() || GetIcon(item) is not null
                ? "bit-chg-ili"
                : string.Empty;
    }

    internal string GetName()
    {
        return Name.HasValue() ? Name! : _name;
    }

    internal string? GetAriaLabel(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.AriaLabel;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.AriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.AriaLabel.Selector is not null)
        {
            return NameSelectors.AriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.AriaLabel.Name);
    }

    /// <summary>
    /// An item is only interactive when both the ChoiceGroup and the item itself are enabled.
    /// </summary>
    internal bool GetIsItemEnabled(TItem item) => IsEnabled && GetIsEnabled(item);

    internal bool GetIsEnabled(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.IsEnabled;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.IsEnabled;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.IsEnabled.Selector is not null)
        {
            return NameSelectors.IsEnabled.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsEnabled.Name, true);
    }

    internal BitIconInfo? GetIcon(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return BitIconInfo.From(choiceGroupItem.Icon, choiceGroupItem.IconName);
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return BitIconInfo.From(choiceGroupOption.Icon, choiceGroupOption.IconName);
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

    internal string? GetImageSrc(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.ImageSrc;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.ImageSrc;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.ImageSrc.Selector is not null)
        {
            return NameSelectors.ImageSrc.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.ImageSrc.Name);
    }

    internal string? GetImageAlt(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.ImageAlt;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.ImageAlt;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.ImageAlt.Selector is not null)
        {
            return NameSelectors.ImageAlt.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.ImageAlt.Name);
    }

    // Null when no size was provided, so the image keeps its intrinsic size instead of collapsing
    // to the 0x0 an unset size would otherwise render as.
    internal BitImageSize? GetImageSize(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.ImageSize;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.ImageSize;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.ImageSize.Selector is not null)
        {
            return NameSelectors.ImageSize.Selector!(item);
        }

        return item.GetValueFromProperty<BitImageSize?>(NameSelectors.ImageSize.Name);
    }

    internal string GetImageWrapperCssStyles(TItem item)
    {
        var imageSize = GetImageSize(item);

        List<string> styles = [];

        if (imageSize is not null)
        {
            styles.Add(FormattableString.Invariant($"width:{imageSize.Value.Width}px;height:{imageSize.Value.Height}px"));
        }

        if (Styles?.ItemImageWrapper.HasValue() ?? false)
        {
            styles.Add(Styles.ItemImageWrapper!);
        }

        return string.Join(';', styles);
    }

    // The checked state only swaps the image when a dedicated one was provided, otherwise the item keeps
    // showing its normal image instead of losing it as soon as it gets selected.
    internal string? GetImageSrcForState(TItem item, bool isChecked)
    {
        var imageSrc = GetImageSrc(item);

        if (isChecked is false) return imageSrc;

        var selectedImageSrc = GetSelectedImageSrc(item);

        return selectedImageSrc.HasValue() ? selectedImageSrc : imageSrc;
    }

    internal string? GetPrefix(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.Prefix;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.Prefix;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Prefix.Selector is not null)
        {
            return NameSelectors.Prefix.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Prefix.Name);
    }

    internal string? GetSelectedImageSrc(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.SelectedImageSrc;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.SelectedImageSrc;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.SelectedImageSrc.Selector is not null)
        {
            return NameSelectors.SelectedImageSrc.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.SelectedImageSrc.Name);
    }

    internal RenderFragment<TItem>? GetTemplate(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.Template as RenderFragment<TItem>;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.Template as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Template.Selector is not null)
        {
            return NameSelectors.Template.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Template.Name);
    }

    internal string? GetDescription(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.Description;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.Description;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Description.Selector is not null)
        {
            return NameSelectors.Description.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Description.Name);
    }

    internal string? GetText(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.Text;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.Text;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Text.Selector is not null)
        {
            return NameSelectors.Text.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Text.Name);
    }

    internal TValue? GetValue(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.Value;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.Value;
        }

        if (NameSelectors is null) return default;

        if (NameSelectors.Value.Selector is not null)
        {
            return NameSelectors.Value.Selector!(item);
        }

        return item.GetValueFromProperty<TValue?>(NameSelectors.Value.Name);
    }

    private string? GetStyle(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.Style;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.Style;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    private string? GetClass(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.Class;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.Class;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Class.Selector is not null)
        {
            return NameSelectors.Class.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Class.Name);
    }

    private string? GetId(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.Id;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.Id;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Id.Selector is not null)
        {
            return NameSelectors.Id.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Id.Name);
    }

    private void SetIndex(TItem item, int value)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            choiceGroupItem.Index = value;
            return;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            choiceGroupOption.Index = value;
            return;
        }

        if (NameSelectors is null) return;

        item.SetValueToProperty(NameSelectors.Index, value);
    }

    private void SetIsSelected(TItem item, bool value)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            choiceGroupItem.IsSelected = value;
            return;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            choiceGroupOption.IsSelected = value;
            return;
        }

        if (NameSelectors is null) return;

        item.SetValueToProperty(NameSelectors.IsSelected, value);
    }

    // Mirrors the current value onto the IsSelected of every item. The value can change without the items
    // changing at all (one-way binding, a programmatic assignment, a reset to null), so this runs on every
    // parameters set and after every change, not only when an item is picked.
    private void UpdateIsSelected()
    {
        foreach (var item in _items)
        {
            SetIsSelected(item, GetIsCheckedItem(item));
        }
    }
}
