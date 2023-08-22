using System.Text;
using System.Drawing;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitChoiceGroup<TItem, TValue> where TItem : class
{
    private const string ARIA_LABEL_FIELD = nameof(BitChoiceGroupItem<TValue>.AriaLabel);
    private const string ID_FIELD = nameof(BitChoiceGroupItem<TValue>.Id);
    private const string IS_ENABLED_FIELD = nameof(BitChoiceGroupItem<TValue>.IsEnabled);
    private const string IMAGE_SRC_FIELD = nameof(BitChoiceGroupItem<TValue>.ImageSrc);
    private const string IMAGE_ALT_FIELD = nameof(BitChoiceGroupItem<TValue>.ImageAlt);
    private const string IMAGE_SIZE_FIELD = nameof(BitChoiceGroupItem<TValue>.ImageSize);
    private const string ICON_NAME_FIELD = nameof(BitChoiceGroupItem<TValue>.IconName);
    private const string SELECTED_IMAGE_SRC_FIELD = nameof(BitChoiceGroupItem<TValue>.SelectedImageSrc);
    private const string TEXT_FIELD = nameof(BitChoiceGroupItem<TValue>.Text);
    private const string VALUE_FIELD = nameof(BitChoiceGroupItem<TValue>.Value);

    private bool isRequired;

    private string _internalAriaLabelField = ARIA_LABEL_FIELD;
    private string _internalIdField = ID_FIELD;
    private string _internalIsEnabledField = IS_ENABLED_FIELD;
    private string _internalImageSrcField = IMAGE_SRC_FIELD;
    private string _internalImageAltField = IMAGE_ALT_FIELD;
    private string _internalImageSizeField = IMAGE_SIZE_FIELD;
    private string _internalIconNameField = ICON_NAME_FIELD;
    private string _internalSelectedImageSrcField = SELECTED_IMAGE_SRC_FIELD;
    private string _internalTextField = TEXT_FIELD;
    private string _internalValueField = VALUE_FIELD;

    private string _labelId = default!;
    private List<TItem> _items = new();
    private IEnumerable<TItem>? _oldItems;

    /// <summary>
    /// Id of an element to use as the aria label for the ChoiceGroup.
    /// </summary>
    [Parameter] public string AriaLabelledBy { get; set; } = string.Empty;

    /// <summary>
    /// The name of the field from the model that will be enable item.
    /// </summary>
    [Parameter] public string AriaLabelField { get; set; } = ARIA_LABEL_FIELD;

    /// <summary>
    /// The name of the field from the model that will be enable item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? AriaLabelFieldSelector { get; set; }

    /// <summary>
    /// The content of the ChoiceGroup, a list of BitChoiceGroupOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Default selected item for ChoiceGroup.
    /// </summary>
    [Parameter] public TValue? DefaultValue { get; set; }

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
    /// The name of the field from the model that will be enable item.
    /// </summary>
    [Parameter] public string IsEnabledField { get; set; } = IS_ENABLED_FIELD;

    /// <summary>
    /// The field from the model that will be enable item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, bool>>? IsEnabledFieldSelector { get; set; }

    /// <summary>
    /// The name of the field from the model that will be the id.
    /// </summary>
    [Parameter] public string IdField { get; set; } = ID_FIELD;

    /// <summary>
    /// The name of the field from the model that will be the id.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? IdFieldSelector { get; set; }

    /// <summary>
    /// The name of the field from the model.
    /// </summary>
    [Parameter] public string IconNameField { get; set; } = ICON_NAME_FIELD;

    /// <summary>
    /// The field selector from the model.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? IconNameFieldSelector { get; set; }

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
    /// Name of the ChoiceGroup, this name is used to group each item into the same logical component.
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
    [Parameter] public Expression<Func<TItem, TValue>>? ValueFieldSelector { get; set; }

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
        ClassBuilder.Register(() => IsEnabled && IsRequired ? $"{RootElementClass}-req" : string.Empty);

        ClassBuilder.Register(() => IsRtl ? $"{RootElementClass}-rtl" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _internalAriaLabelField = AriaLabelFieldSelector?.GetName() ?? AriaLabelField;
        _internalIdField = IdFieldSelector?.GetName() ?? IdField;
        _internalIsEnabledField = IsEnabledFieldSelector?.GetName() ?? IsEnabledField;
        _internalIconNameField = IconNameFieldSelector?.GetName() ?? IconNameField;
        _internalImageSrcField = ImageSrcFieldSelector?.GetName() ?? ImageSrcField;
        _internalImageAltField = ImageAltFieldSelector?.GetName() ?? ImageAltField;
        _internalImageSizeField = ImageSizeFieldSelector?.GetName() ?? ImageSizeField;
        _internalSelectedImageSrcField = SelectedImageSrcFieldSelector?.GetName() ?? SelectedImageSrcField;
        _internalTextField = TextFieldSelector?.GetName() ?? TextField;
        _internalValueField = ValueFieldSelector?.GetName() ?? ValueField;

        _labelId = $"ChoiceGroup-{UniqueId}-Label";

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

        return item.GetValueFromProperty<string?>(_internalAriaLabelField);
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

        return item.GetValueFromProperty<string?>(_internalIdField);
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

        return item.GetValueFromProperty(_internalIsEnabledField, true);
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

        return item.GetValueFromProperty<string?>(_internalImageSrcField);
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

        return item.GetValueFromProperty<string?>(_internalImageAltField);
    }

    private Size GetImageSize(TItem item)
    {
        if (item is BitChoiceGroupItem<TValue> choiceGroupItem)
        {
            return choiceGroupItem.ImageSize ?? new Size(0, 0);
        }

        if (item is BitChoiceGroupOption<TValue> choiceGroupOption)
        {
            return choiceGroupOption.ImageSize ?? new Size(0, 0);
        }

        return item.GetValueFromProperty(_internalImageSizeField, new Size(0, 0));
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

        return item.GetBitIconNameFromProperty(_internalIconNameField);
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

        return item.GetValueFromProperty<string?>(_internalSelectedImageSrcField);
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

        return item.GetValueFromProperty<string?>(_internalTextField);
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

        return item.GetValueFromProperty<TValue?>(_internalValueField);
    }

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

    private string GetDivClassNameItem(TItem item)
    {
        const string itemRootElementClass = "bit-chgi";

        StringBuilder cssClass = new(itemRootElementClass);

        if (ItemTemplate is not null) return cssClass.ToString();

        if (GetIsCheckedItem(item))
        {
            cssClass.Append(' ')
                    .Append(itemRootElementClass)
                    .Append("-chk");
        }

        if (ItemLabelTemplate is not null) return cssClass.ToString();

        if (IsEnabled is false || GetIsEnabled(item) is false)
        {
            cssClass.Append(' ')
                    .Append(itemRootElementClass)
                    .Append("-dis");
        }

        if (GetImageSrc(item).HasValue() || GetIconName(item).HasValue())
        {
            cssClass.Append(' ')
                    .Append(itemRootElementClass)
                    .Append("-img");
        }

        return cssClass.ToString();
    }

    private string GetLabelClassNameItem(TItem item) =>
        (GetImageSrc(item).HasValue() || GetIconName(item).HasValue()) && ItemLabelTemplate is null 
        ? "bit-chgi-lbl-img" 
        : "bit-chgi-lbl";

    private async Task HandleClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;

        CurrentValue = e.Value.ConvertTo<TValue>();

        await OnChange.InvokeAsync(e);
    }

    private string GetAriaLabelledBy() => AriaLabelledBy ?? _labelId;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");
}
