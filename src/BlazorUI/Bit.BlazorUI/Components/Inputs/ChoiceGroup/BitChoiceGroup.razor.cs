using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitChoiceGroup<TItem, TValue> where TItem : class
{
    private bool isRequired;

    private string _labelId = default!;
    private List<TItem> _items = new();
    private IEnumerable<TItem>? _oldItems;

    /// <summary>
    /// Id of an element to use as the aria label for the ChoiceGroup.
    /// </summary>
    [Parameter] public string AriaLabelledBy { get; set; } = string.Empty;

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
    /// Sets the data source that populates the items of the list.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = new List<TItem>();

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
    /// If true, selecting an option is mandatory in the ChoiceGroup.
    /// </summary>
    [Parameter]
    public bool IsRequired
    {
        get => isRequired;
        set
        {
            if (isRequired == value) return;

            isRequired = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The label for the ChoiceGroup.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom RenderFragment for the label of the ChoiceGroup.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The render flow of the items in the ChoiceGroup, Horizontal or Vertical.
    /// </summary>
    [Parameter] public BitLayoutFlow? LayoutFlow { get; set; }

    /// <summary>
    /// Name of the ChoiceGroup, this unique name is used to group each item into the same logical component.
    /// </summary>
    [Parameter] public string Name { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitChoiceGroupNameSelectors<TItem, TValue>? NameSelectors { get; set; }

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

        if (CurrentValue is null && DefaultValue is not null && ValueHasBeenSet is false && _items.Any(item => EqualityComparer<TValue>.Default.Equals(GetValue(item), DefaultValue)))
        {
            CurrentValue = DefaultValue;
        }

        StateHasChanged();
    }

    internal void UnregisterOption(BitChoiceGroupOption<TValue> option)
    {
        _items.Remove((option as TItem)!);
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-chg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsEnabled && IsRequired ? $"{RootElementClass}-req" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnInitializedAsync()
    {
        _labelId = $"BitChoiceGroup-{UniqueId}-label";

        if (ValueHasBeenSet is false && DefaultValue is not null && Items.Any(item => EqualityComparer<TValue>.Default.Equals(GetValue(item), DefaultValue)))
        {
            CurrentValue = DefaultValue;
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (ChildContent is not null || Items.Any() is false || Items == _oldItems) return;

        _oldItems = Items;
        _items = Items.ToList();

        if (ValueHasBeenSet is false && DefaultValue is not null && _items.Any(item => EqualityComparer<TValue>.Default.Equals(GetValue(item), DefaultValue)))
        {
            CurrentValue = DefaultValue;
        }
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");



    private string? GetInputId(TItem item) => GetId(item) ?? $"ChoiceGroup-{UniqueId}-Input-{GetValue(item)}";

    private bool GetIsCheckedItem(TItem item)
    {
        if (CurrentValue is null) return false;

        return EqualityComparer<TValue>.Default.Equals(GetValue(item), CurrentValue);
    }

    private bool ItemValueEqualityComparer(TItem item, TValue? value)
    {
        var itemValue = GetValue(item);

        if (itemValue is null) return false;

        return EqualityComparer<TValue>.Default.Equals(itemValue, value);
    }

    private async Task HandleClick(TItem item)
    {
        if (IsEnabled is false || GetIsEnabled(item) is false) return;

        await OnClick.InvokeAsync(item);
    }

    private async Task HandleChange(TItem item)
    {
        if (IsEnabled is false || GetIsEnabled(item) is false) return;

        var oldValue = CurrentValue;
        CurrentValue = GetValue(item);
    }

    private string GetAriaLabelledBy() => AriaLabelledBy ?? _labelId;

    private string? GetLayoutFlowClass() => LayoutFlow switch
    {
        BitLayoutFlow.Horizontal => "horizontal",
        BitLayoutFlow.Vertical => "vertical",
        _ => null
    };

    private string GetItemContainerCssClasses(TItem item)
    {
        StringBuilder cssClass = new(RootElementClass);
        cssClass.Append("-icn");

        if (ItemTemplate is not null) return cssClass.ToString();

        if (GetIsCheckedItem(item))
        {
            cssClass.Append(' ')
                    .Append(RootElementClass)
                    .Append("-ich");
        }

        if (ItemLabelTemplate is not null) return cssClass.ToString();

        if (IsEnabled is false || GetIsEnabled(item) is false)
        {
            cssClass.Append(' ')
                    .Append(RootElementClass)
                    .Append("-ids");
        }

        if (GetImageSrc(item).HasValue() || GetIconName(item).HasValue())
        {
            cssClass.Append(' ')
                    .Append(RootElementClass)
                    .Append("-ihi");
        }

        return cssClass.ToString();
    }

    private string GetItemLabelWrapperCssClasses(TItem item)
    {
        var hasImageOrIcon = GetImageSrc(item).HasValue() || GetIconName(item).HasValue();
        return hasImageOrIcon && ItemLabelTemplate is null
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

    private BitSize GetImageSize(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.ImageSize ?? new BitSize(0, 0);
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.ImageSize ?? new BitSize(0, 0);
        }

        if (NameSelectors is null) return new BitSize(0, 0);

        if (NameSelectors.ImageSize.Selector is not null)
        {
            return NameSelectors.ImageSize.Selector!(item) ?? new BitSize(0, 0);
        }

        return item.GetValueFromProperty<BitSize?>(NameSelectors.ImageSize.Name) ?? new BitSize(0, 0);
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
}
