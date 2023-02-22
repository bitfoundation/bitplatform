using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq.Expressions;

namespace Bit.BlazorUI;

public partial class BitChoiceGroup<TItem> where TItem : class
{
    private const string ARIA_LABEL_FIELD = nameof(BitChoiceGroupItem.AriaLabel);
    private const string IS_ENABLED_FIELD = nameof(BitChoiceGroupItem.IsEnabled);
    private const string IMAGE_SRC_FIELD = nameof(BitChoiceGroupItem.ImageSrc);
    private const string IMAGE_ALT_FIELD = nameof(BitChoiceGroupItem.ImageAlt);
    private const string IMAGE_SIZE_FIELD = nameof(BitChoiceGroupItem.ImageSize);
    private const string ICON_NAME_FIELD = nameof(BitChoiceGroupItem.IconName);
    private const string SELECTED_IMAGE_SRC_FIELD = nameof(BitChoiceGroupItem.SelectedImageSrc);
    private const string TEXT_FIELD = nameof(BitChoiceGroupItem.Text);
    private const string VALUE_FIELD = nameof(BitChoiceGroupItem.Value);

    private bool isRequired;

    private string _internalAriaLabelField = ARIA_LABEL_FIELD;
    private string _internalIsEnabledField = IS_ENABLED_FIELD;
    private string _internalImageSrcField = IMAGE_SRC_FIELD;
    private string _internalImageAltField = IMAGE_ALT_FIELD;
    private string _internalImageSizeField = IMAGE_SIZE_FIELD;
    private string _internalIconNameField = ICON_NAME_FIELD;
    private string _internalSelectedImageSrcField = SELECTED_IMAGE_SRC_FIELD;
    private string _internalTextField = TEXT_FIELD;
    private string _internalValueField = VALUE_FIELD;

    private string _labelId => $"{UniqueId}_label";

    private List<TItem> _items = new();
    private IEnumerable<TItem>? _oldItems;

    /// <summary>
    /// ID of an element to use as the aria label for this RadioButtonList.
    /// </summary>
    [Parameter] public string AriaLabelledBy { get; set; } = string.Empty;

    /// <summary>
    /// The name of the field from the model that will be enable item.
    /// </summary>
    [Parameter] public string AriaLabelField { get; set; } = ARIA_LABEL_FIELD;

    /// <summary>
    /// The field from the model that will be enable item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? AriaLabelFieldSelector { get; set; }

    /// <summary>
    /// The content of RadioButtonGroup, common values are RadioButtonGroup component.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Default selected item for ChoiceGroup.
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

    /// <summary>
    /// Sets the data source that populates the items of the list.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = new List<TItem>();

    /// <summary>
    /// Used to customize the label for the Item content.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Used to customize the label for the Item Label content.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemLabelTemplate { get; set; }

    /// <summary>
    /// If true, an option must be selected in the RadioButtonList.
    /// </summary>
    [Parameter]
    public bool IsRequired
    {
        get => isRequired;
        set
        {
            isRequired = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The name of the field from the model that will be enable item.
    /// </summary>
    [Parameter] public string IsEnabledField { get; set; } = IS_ENABLED_FIELD;

    /// <summary>
    /// The field from the model that will be enable item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, bool>>? IsEnabledFieldSelector { get; set; }

    /// <summary>
    /// The name of the field from the model that will be the BitIconName.
    /// </summary>
    [Parameter] public string IconNameField { get; set; } = ICON_NAME_FIELD;

    /// <summary>
    /// The field from the model that will be the BitIconName.
    /// </summary>
    [Parameter] public Expression<Func<TItem, BitIconName>>? IconNameFieldSelector { get; set; }

    /// <summary>
    /// The name of the field from the model that will be the image src.
    /// </summary>
    [Parameter] public string ImageSrcField { get; set; } = IMAGE_SRC_FIELD;

    /// <summary>
    /// The field from the model that will be the image src.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? ImageSrcFieldSelector { get; set; }

    /// <summary>
    /// The name of the field from the model that will be the image alternate text.
    /// </summary>
    [Parameter] public string ImageAltField { get; set; } = IMAGE_ALT_FIELD;

    /// <summary>
    /// The field from the model that will be the image alternate text.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? ImageAltFieldSelector { get; set; }

    /// <summary>
    /// The name of the field from the model that will be the image alternate text.
    /// </summary>
    [Parameter] public string ImageSizeField { get; set; } = IMAGE_SIZE_FIELD;

    /// <summary>
    /// The field from the model that will be the image alternate text.
    /// </summary>
    [Parameter] public Expression<Func<TItem, Size>>? ImageSizeFieldSelector { get; set; }

    /// <summary>
    /// Change direction to RTL.
    /// </summary>
    [Parameter] public bool IsRtl { get; set; }

    /// <summary>
    /// Descriptive label for the radio button list.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Used to customize the label for the radio button list.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// You can define the RadioButtonList in Horizontal or Vertical mode.
    /// </summary>
    [Parameter] public BitLayoutFlow? LayoutFlow { get; set; }

    /// <summary>
    /// Name of RadioButtonList, this name is used to group each item into the same logical RadioButtonList.
    /// </summary>
    [Parameter] public string Name { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Callback for when the option clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback for when the option has been changed.
    /// </summary>
    [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }

    /// <summary>
    /// The name of the field from the model that will be the selected image src.
    /// </summary>
    [Parameter] public string SelectedImageSrcField { get; set; } = SELECTED_IMAGE_SRC_FIELD;

    /// <summary>
    /// The field from the model that will be the selected image src.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? SelectedImageSrcFieldSelector { get; set; }

    /// <summary>
    /// The name of the field from the model that will be shown to the user.
    /// </summary>
    [Parameter] public string TextField { get; set; } = TEXT_FIELD;

    /// <summary>
    /// The field from the model that will be shown to the user.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? TextFieldSelector { get; set; }

    /// <summary>
    /// The name of the field from the model that will be the underlying value.
    /// </summary>
    [Parameter] public string ValueField { get; set; } = VALUE_FIELD;

    /// <summary>
    /// The field from the model that will be the underlying value.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? ValueFieldSelector { get; set; }

    protected override string RootElementClass => "bit-chg";

    internal void RegisterOption(BitChoiceGroupOption option)
    {
        _items.Add((option as TItem)!);

        if (CurrentValue.HasNoValue() && DefaultValue.HasValue() && ValueHasBeenSet is false && _items.Any(item => GetValue(item) == DefaultValue))
        {
            CurrentValue = DefaultValue;
        }

        StateHasChanged();
    }

    internal void UnregisterOption(BitChoiceGroupOption option)
    {
        _items.Remove((option as TItem)!);
        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        _internalAriaLabelField = AriaLabelFieldSelector?.GetName() ?? AriaLabelField;
        _internalIsEnabledField = IsEnabledFieldSelector?.GetName() ?? IsEnabledField;
        _internalIconNameField = IconNameFieldSelector?.GetName() ?? IconNameField;
        _internalImageSrcField = ImageSrcFieldSelector?.GetName() ?? ImageSrcField;
        _internalImageAltField = ImageAltFieldSelector?.GetName() ?? ImageAltField;
        _internalImageSizeField = ImageSizeFieldSelector?.GetName() ?? ImageSizeField;
        _internalSelectedImageSrcField = SelectedImageSrcFieldSelector?.GetName() ?? SelectedImageSrcField;
        _internalTextField = TextFieldSelector?.GetName() ?? TextField;
        _internalValueField = ValueFieldSelector?.GetName() ?? ValueField;

        if (ValueHasBeenSet is false && DefaultValue.HasValue() && Items.Any(item => GetValue(item) == DefaultValue))
        {
            CurrentValue = DefaultValue;
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ChildContent is null && Items.Any() && Items != _oldItems)
        {
            _oldItems = Items;
            _items = Items.ToList();

            if (ValueHasBeenSet is false && DefaultValue.HasValue() && _items.Any(item => GetValue(item) == DefaultValue))
            {
                CurrentValue = DefaultValue;
            }
        }

        await base.OnParametersSetAsync();
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled && IsRequired
                                   ? $"{RootElementClass}-required-{VisualClassRegistrar()}"
                                   : string.Empty);

        ClassBuilder.Register(() => ValueInvalid is true
                                   ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}"
                                   : string.Empty);

        ClassBuilder.Register(() => IsRtl
                                   ? $"{RootElementClass}-rtl-{VisualClassRegistrar()}"
                                   : string.Empty);
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string result, [NotNullWhen(false)] out string? validationErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");

    private string? GetAriaLabel(TItem item)
    {
        if (item is BitChoiceGroupItem choiceGroupItem)
        {
            return choiceGroupItem.AriaLabel;
        }

        if (item is BitChoiceGroupOption choiceGroupOption)
        {
            return choiceGroupOption.AriaLabel;
        }

        return item.GetValueFromProperty<string>(_internalAriaLabelField);
    }

    private bool GetIsEnabled(TItem item)
    {
        if (item is BitChoiceGroupItem choiceGroupItem)
        {
            return choiceGroupItem.IsEnabled;
        }

        if (item is BitChoiceGroupOption choiceGroupOption)
        {
            return choiceGroupOption.IsEnabled;
        }

        return item.GetValueFromProperty(_internalIsEnabledField, true);
    }

    private string? GetImageSrc(TItem item)
    {
        if (item is BitChoiceGroupItem choiceGroupItem)
        {
            return choiceGroupItem.ImageSrc;
        }

        if (item is BitChoiceGroupOption choiceGroupOption)
        {
            return choiceGroupOption.ImageSrc;
        }

        return item.GetValueFromProperty<string>(_internalImageSrcField);
    }

    private string? GetImageAlt(TItem item)
    {
        if (item is BitChoiceGroupItem choiceGroupItem)
        {
            return choiceGroupItem.ImageAlt;
        }

        if (item is BitChoiceGroupOption choiceGroupOption)
        {
            return choiceGroupOption.ImageAlt;
        }

        return item.GetValueFromProperty<string>(_internalImageAltField);
    }

    private Size GetImageSize(TItem item)
    {
        if (item is BitChoiceGroupItem choiceGroupItem)
        {
            return choiceGroupItem.ImageSize ?? new Size(0, 0);
        }

        if (item is BitChoiceGroupOption choiceGroupOption)
        {
            return choiceGroupOption.ImageSize ?? new Size(0, 0);
        }

        return item.GetValueFromProperty(_internalImageSizeField, new Size(0,0));
    }

    private BitIconName? GetIconName(TItem item)
    {
        if (item is BitChoiceGroupItem choiceGroupItem)
        {
            return choiceGroupItem.IconName;
        }

        if (item is BitChoiceGroupOption choiceGroupOption)
        {
            return choiceGroupOption.IconName;
        }

        return item.GetBitIconNameFromProperty(_internalIconNameField);
    }

    private string? GetSelectedImageSrc(TItem item)
    {
        if (item is BitChoiceGroupItem choiceGroupItem)
        {
            return choiceGroupItem.SelectedImageSrc;
        }

        if (item is BitChoiceGroupOption choiceGroupOption)
        {
            return choiceGroupOption.SelectedImageSrc;
        }

        return item.GetValueFromProperty<string>(_internalSelectedImageSrcField);
    }

    private string? GetText(TItem item)
    {
        if (item is BitChoiceGroupItem choiceGroupItem)
        {
            return choiceGroupItem.Text;
        }

        if (item is BitChoiceGroupOption choiceGroupOption)
        {
            return choiceGroupOption.Text;
        }

        return item.GetValueFromProperty<string>(_internalTextField);
    }

    private string? GetValue(TItem item)
    {
        if (item is BitChoiceGroupItem choiceGroupItem)
        {
            return choiceGroupItem.Value;
        }

        if (item is BitChoiceGroupOption choiceGroupOption)
        {
            return choiceGroupOption.Value;
        }

        return item.GetValueFromProperty<string>(_internalValueField);
    }

    private string? GetInputId(TItem item) => $"chg_{UniqueId}_{GetValue(item)}";

    private bool GetIsCheckedItem(TItem item)
    {
        if (CurrentValue is null) return false;

        var itemValue = GetValue(item);

        if (itemValue is null) return false;

        return itemValue == Value;
    }

    private string GetDivClassNameItem(TItem item)
    {
        const string itemRootElementClass = "bit-chgi";

        StringBuilder cssClass = new(itemRootElementClass);

        if (ItemTemplate is not null) return cssClass.ToString();

        if (GetIsCheckedItem(item))
        {
            cssClass.Append(' ')
                    .Append(itemRootElementClass)
                    .Append("-checked");
        }

        if (ItemLabelTemplate is not null) return cssClass.ToString();

        if (IsEnabled is false || GetIsEnabled(item) is false)
        {
            cssClass.Append(' ')
                    .Append(itemRootElementClass)
                    .Append("-disabled");
        }

        if (GetImageSrc(item).HasValue() || GetIconName(item).HasValue)
        {
            cssClass.Append(' ')
                    .Append(itemRootElementClass)
                    .Append("-with-img");
        }

        return cssClass.ToString();
    }

    private string GetLabelClassNameItem(TItem item) =>
        (GetImageSrc(item).HasValue() || GetIconName(item).HasValue) && ItemLabelTemplate is null ? "bit-chgi-lbl-with-img" : "bit-chgi-lbl";

    private async Task HandleClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;

        CurrentValue = e.Value?.ToString();

        await OnChange.InvokeAsync(e);
    }

    private string GetAriaLabelledBy() => Label.HasValue() || LabelTemplate is not null ? _labelId : AriaLabelledBy;
}
