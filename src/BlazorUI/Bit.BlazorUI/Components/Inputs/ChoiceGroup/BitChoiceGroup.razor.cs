using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// ChoiceGroup let people select a single option from two or more choices.
/// </summary>
public partial class BitChoiceGroup<TItem, TValue> : BitInputBase<TValue> where TItem : class, new()
{
    private List<TItem> _items = [];
    private string _name = default!;
    private string _labelId = default!;
    private bool _optionsOrderDirty;
    private string _optionsContainerId = default!;
    private IEnumerable<TItem>? _oldItems;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Id of an element to use as the aria label for the ChoiceGroup.
    /// </summary>
    [Parameter] public string? AriaLabelledBy { get; set; }

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
    /// Callback for when the option clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnClick { get; set; }

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

        if (ChildContent is not null || Options is not null || Items is null || Items.Any() is false) return;

        if (_oldItems is not null && Items.SequenceEqual(_oldItems)) return;

        _oldItems = Items;
        _items = [.. Items];

        SetIndexItems();

        InitDefaultValue();
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
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");



    private void InitDefaultValue()
    {
        if (ValueHasBeenSet)
        {
            var item = _items.FirstOrDefault(item => EqualityComparer<TValue>.Default.Equals(GetValue(item), Value));
            if (item is not null)
            {
                SetIsSelectedForSelectedItem(item);
            }
        }
        else if (DefaultValue is not null)
        {
            var item = _items.FirstOrDefault(item => EqualityComparer<TValue>.Default.Equals(GetValue(item), DefaultValue));
            if (item is not null)
            {
                SetIsSelectedForSelectedItem(item);
                Value = DefaultValue;
            }
        }
    }

    private void SetIndexItems()
    {
        if (_items.Any() is false) return;

        for (var i = 0; i < _items.Count; i++)
        {
            var index = i;
            var item = _items[i];
            SetIndex(item, index);
        }
    }

    private string GetAriaLabelledBy() => AriaLabelledBy ?? _labelId;

    internal string? GetInputId(TItem item) => GetId(item) ?? $"ChoiceGroup-{UniqueId}-Input-{GetValue(item)}";

    internal async Task HandleClick(TItem item)
    {
        if (IsEnabled is false || ReadOnly || GetIsEnabled(item) is false) return;

        await OnClick.InvokeAsync(item);
    }

    internal void HandleChange(TItem item)
    {
        if (IsEnabled is false || ReadOnly || GetIsEnabled(item) is false) return;

        SetIsSelectedForSelectedItem(item);

        CurrentValue = GetValue(item);

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

    internal string GetItemContainerCssStyles(TItem item)
    {
        StringBuilder cssStyle = new();

        if (GetStyle(item).HasValue())
        {
            cssStyle.Append(GetStyle(item));
        }

        if (string.IsNullOrEmpty(Styles?.ItemContainer) is false)
        {
            cssStyle.Append(' ').Append(Styles?.ItemContainer);
        }

        if (GetIsCheckedItem(item))
        {
            cssStyle.Append(' ').Append(Styles?.ItemChecked);
        }

        return cssStyle.ToString();
    }

    internal string GetItemContainerCssClasses(TItem item)
    {
        StringBuilder cssClass = new("bit-chg-icn");

        if (GetClass(item).HasValue())
        {
            cssClass.Append(' ').Append(GetClass(item));
        }

        if (string.IsNullOrEmpty(Classes?.ItemContainer) is false)
        {
            cssClass.Append(' ').Append(Classes?.ItemContainer);
        }

        if (ItemTemplate is not null) return cssClass.ToString();

        if (GetIsCheckedItem(item))
        {
            cssClass.Append(' ').Append("bit-chg-ich");
            cssClass.Append(' ').Append(Classes?.ItemChecked);
        }

        if (ItemLabelTemplate is not null) return cssClass.ToString();

        if (IsEnabled is false || GetIsEnabled(item) is false)
        {
            cssClass.Append(' ').Append("bit-chg-ids");
        }

        if (GetImageSrc(item).HasValue() || GetIcon(item) is not null)
        {
            cssClass.Append(' ').Append("bit-chg-ihi");
        }

        return cssClass.ToString();
    }

    internal string GetItemLabelCssClasses(TItem item)
    {
        var hasImageOrIcon = GetImageSrc(item).HasValue() || GetIcon(item) is not null;
        return hasImageOrIcon && ItemLabelTemplate is null && Inline is false
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

    internal BitImageSize GetImageSize(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.ImageSize ?? new BitImageSize(0, 0);
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.ImageSize ?? new BitImageSize(0, 0);
        }

        if (NameSelectors is null) return new BitImageSize(0, 0);

        if (NameSelectors.ImageSize.Selector is not null)
        {
            return NameSelectors.ImageSize.Selector!(item) ?? new BitImageSize(0, 0);
        }

        return item.GetValueFromProperty<BitImageSize?>(NameSelectors.ImageSize.Name) ?? new BitImageSize(0, 0);
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
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            choiceGroupOption.Index = value;
        }

        if (NameSelectors is null) return;

        item.SetValueToProperty(NameSelectors.Index, value);
    }

    private void SetIsSelected(TItem item, bool value)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            choiceGroupItem.IsSelected = value;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            choiceGroupOption.IsSelected = value;
        }

        if (NameSelectors is null) return;

        item.SetValueToProperty(NameSelectors.IsSelected, value);
    }

    private void SetIsSelectedForSelectedItem(TItem item)
    {
        foreach (var itm in _items)
        {
            SetIsSelected(itm, false);
        }

        SetIsSelected(item, true);
    }
}
