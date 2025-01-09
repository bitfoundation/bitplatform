using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// ChoiceGroup let people select a single option from two or more choices.
/// </summary>
public partial class BitChoiceGroup<TItem, TValue> : BitInputBase<TValue> where TItem : class
{
    private List<TItem> _items = [];
    private string _labelId = default!;
    private IEnumerable<TItem>? _oldItems;



    /// <summary>
    /// Id of an element to use as the aria label for the ChoiceGroup.
    /// </summary>
    [Parameter] public string? AriaLabelledBy { get; set; }

    /// <summary>
    /// The content of the ChoiceGroup, a list of BitChoiceGroupOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitChoiceGroup.
    /// </summary>
    [Parameter] public BitChoiceGroupClassStyles? Classes { get; set; }

    /// <summary>
    /// Default selected item for ChoiceGroup.
    /// </summary>
    [Parameter] public TValue? DefaultValue { get; set; }

    /// <summary>
    /// Renders the items in the ChoiceGroup horizontally.
    /// </summary>
    [Parameter] public bool Horizontal { get; set; }

    /// <summary>
    /// Renders the icons and images in a single line with the items in the ChoiceGroup.
    /// </summary>
    [Parameter] public bool Inline { get; set; }

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
    /// Custom CSS styles for different parts of the BitChoiceGroup.
    /// </summary>
    [Parameter] public BitChoiceGroupClassStyles? Styles { get; set; }



    internal void RegisterOption(BitChoiceGroupOption<TValue> option)
    {
        _items.Add((option as TItem)!);

        InitDefaultValue();

        StateHasChanged();
    }

    internal void UnregisterOption(BitChoiceGroupOption<TValue> option)
    {
        _items.Remove((option as TItem)!);

        StateHasChanged();
    }



    protected override async Task OnInitializedAsync()
    {
        _labelId = $"BitChoiceGroup-{UniqueId}-label";

        InitDefaultValue();

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (ChildContent is not null || Items is null || Items.Any() is false) return;

        if (_oldItems is not null && Items.SequenceEqual(_oldItems)) return;

        _oldItems = Items;
        _items = Items.ToList();

        InitDefaultValue();
    }

    protected override string RootElementClass => "bit-chg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Inline ? "bit-chg-inl" : string.Empty);

        ClassBuilder.Register(() => NoCircle ? "bit-chg-ncr" : "bit-chg-wcr");

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-chg-req" : string.Empty);
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

    private string? GetInputId(TItem item) => GetId(item) ?? $"ChoiceGroup-{UniqueId}-Input-{GetValue(item)}";

    private bool GetIsCheckedItem(TItem item)
    {
        if (CurrentValue is null) return false;

        return EqualityComparer<TValue>.Default.Equals(GetValue(item), CurrentValue);
    }

    private async Task HandleClick(TItem item)
    {
        if (IsEnabled is false || GetIsEnabled(item) is false) return;

        await OnClick.InvokeAsync(item);
    }

    private async Task HandleChange(TItem item)
    {
        if (IsEnabled is false || GetIsEnabled(item) is false) return;

        SetIsSelectedForSelectedItem(item);

        CurrentValue = GetValue(item);
    }

    private string GetAriaLabelledBy() => AriaLabelledBy ?? _labelId;

    private string GetItemContainerCssStyles(TItem item)
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

    private string GetItemContainerCssClasses(TItem item)
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

        if (GetImageSrc(item).HasValue() || GetIconName(item).HasValue())
        {
            cssClass.Append(' ').Append("bit-chg-ihi");
        }

        return cssClass.ToString();
    }

    private string GetItemLabelWrapperCssClasses(TItem item)
    {
        var hasImageOrIcon = GetImageSrc(item).HasValue() || GetIconName(item).HasValue();
        return hasImageOrIcon && ItemLabelTemplate is null && Inline is false
                ? "bit-chg-ilwi"
                : "bit-chg-ilw";
    }

    private string? GetAriaLabel(TItem item)
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

    private bool GetIsEnabled(TItem item)
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

    private string? GetIconName(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.IconName;
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.IconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.IconName.Selector is not null)
        {
            return NameSelectors.IconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
    }

    private string? GetImageSrc(TItem item)
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

    private string? GetImageAlt(TItem item)
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

    private BitImageSize GetImageSize(TItem item)
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

    private string? GetPrefix(TItem item)
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

    private string? GetSelectedImageSrc(TItem item)
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

    private RenderFragment<TItem>? GetTemplate(TItem item)
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

    private string? GetText(TItem item)
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

    private TValue? GetValue(TItem item)
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
