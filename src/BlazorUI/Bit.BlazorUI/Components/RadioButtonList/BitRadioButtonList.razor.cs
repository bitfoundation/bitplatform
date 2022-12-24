using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq.Expressions;
using System.Text;

namespace Bit.BlazorUI;

public partial class BitRadioButtonList<TItem, TValue>
{
    private string isEnabledField = "IsEnabled";
    private Expression<Func<TItem, bool>>? isEnabledSelector;
    private string iconNameField = "IconName";
    private Expression<Func<TItem, BitIconName>>? iconNameSelector;
    private string imageSrcField = "ImageSrc";
    private Expression<Func<TItem, object>>? imageSrcSelector;
    private string imageAltField = "ImageAlt";
    private Expression<Func<TItem, object>>? imageAltSelector;
    private string selectedImageSrcField = "SelectedImageSrc";
    private Expression<Func<TItem, object>>? selectedImageSrcSelector;
    private string textField = "Text";
    private Expression<Func<TItem, object>>? textSelector;
    private string valueField = "Value";
    private Expression<Func<TItem, object>>? valueSelector;
    private bool isRequired;

    private string _internalIsEnabledField = "IsEnabled";
    private string _internalIconNameField = "IconName";
    private string _internalImageSrcField = "ImageSrc";
    private string _internalImageAltField = "ImageAlt";
    private string _internalSelectedImageSrcField = "SelectedImageSrc";
    private string _internalTextField = "Text";
    private string _internalValueField = "Value";
    private string? _imageSizeStyle;
    public string _labelId => $"{UniqueId}RadioButtonListLabel";

    /// <summary>
    /// ID of an element to use as the aria label for this RadioButtonList.
    /// </summary>
    [Parameter] public string AriaLabelledBy { get; set; } = string.Empty;

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
    [Parameter] 
    public string IsEnabledField
    {
        get => isEnabledField;
        set
        {
            isEnabledField = value;
            _internalIsEnabledField = value;
        }
    }

    /// <summary>
    /// The field from the model that will be enable item.
    /// </summary>
    [Parameter] 
    public Expression<Func<TItem, bool>>? IsEnabledSelector
    {
        get => isEnabledSelector;
        set
        {
            isEnabledSelector = value;

            if (value is not null)
            {
                _internalIconNameField = value.GetName();
            }
        }
    }

    /// <summary>
    /// The name of the field from the model that will be the BitIconName.
    /// </summary>
    [Parameter] 
    public string IconNameField
    {
        get => iconNameField;
        set
        {
            iconNameField = value;
            _internalIconNameField = value;
        }
    }

    /// <summary>
    /// The name of the field from the model that will be the image src.
    /// </summary>
    [Parameter]
    public string ImageSrcField
    {
        get => imageSrcField;
        set
        {
            imageSrcField = value;
            _internalImageSrcField = value;
        }
    }

    /// <summary>
    /// The name of the field from the model that will be the image alternate text.
    /// </summary>
    [Parameter]
    public string ImageAltField
    {
        get => imageAltField;
        set
        {
            imageAltField = value;
            _internalImageAltField = value;
        }
    }

    /// <summary>
    /// The field from the model that will be the BitIconName.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, BitIconName>>? IconNameSelector
    {
        get => iconNameSelector;
        set
        {
            iconNameSelector = value;

            if (value is not null)
            {
                _internalIconNameField = value.GetName();
            }
        }
    }

    /// <summary>
    /// The field from the model that will be the image src.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, object>>? ImageSrcSelector
    {
        get => imageSrcSelector;
        set
        {
            imageSrcSelector = value;

            if (value is not null)
            {
                _internalImageSrcField = value.GetName();
            }
        }
    }

    /// <summary>
    /// The field from the model that will be the image alternate text.
    /// </summary>
    [Parameter] 
    public Expression<Func<TItem, object>>? ImageAltSelector
    {
        get => imageAltSelector;
        set
        {
            imageAltSelector = value;

            if (value is not null)
            {
                _internalImageAltField = value.GetName();
            }
        }
    }

    /// <summary>
    /// The width and height of the image in px for item field.
    /// </summary>
    [Parameter] public Size? ImageSize { get; set; }

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
    [Parameter] 
    public string SelectedImageSrcField
    {
        get => selectedImageSrcField;
        set
        {
            selectedImageSrcField = value;
            _internalSelectedImageSrcField = value;
        }
    }

    /// <summary>
    /// The field from the model that will be the selected image src.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, object>>? SelectedImageSrcSelector
    {
        get => selectedImageSrcSelector;
        set
        {
            selectedImageSrcSelector = value;

            if (value is not null)
            {
                _internalSelectedImageSrcField = value.GetName();
            }
        }
    }

    /// <summary>
    /// The name of the field from the model that will be shown to the user.
    /// </summary>
    [Parameter]
    public string TextField
    {
        get => textField;
        set
        {
            textField = value;
            _internalTextField = value;
        }
    }

    /// <summary>
    /// The field from the model that will be shown to the user.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, object>>? TextSelector
    {
        get => textSelector;
        set
        {
            textSelector = value;

            if (value is not null)
            {
                _internalTextField = value.GetName();
            }
        }
    }

    /// <summary>
    /// The name of the field from the model that will be the underlying value.
    /// </summary>
    [Parameter] 
    public string ValueField
    {
        get => valueField;
        set
        {
            valueField = value;
            _internalValueField = value;
        }
    }

    /// <summary>
    /// The field from the model that will be the underlying value.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, object>>? ValueSelector
    {
        get => valueSelector;
        set
        {
            valueSelector = value;

            if (value is not null)
            {
                _internalValueField = value.GetName();
            }
        }
    }

    protected override string RootElementClass => "bit-rbl";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled && IsRequired
                                   ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => ValueInvalid is true
                                   ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => IsRtl
                                   ? $"{RootElementClass}-rtl-{VisualClassRegistrar()}" : string.Empty);
    }

    protected override Task OnParametersSetAsync()
    {
        if (ImageSize is not null)
        {
            _imageSizeStyle = $" width:{ImageSize.Value.Width}px; height:{ImageSize.Value.Height}px;";
        }

        return base.OnParametersSetAsync();
    }

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

    private string GetAriaLabelledBy() => Label.HasValue() || LabelTemplate is not null ? _labelId : AriaLabelledBy;

    private string? GetTextItem(TItem item) => item.GetValueAsObjectFromProperty(_internalTextField)?.ToString();

    private object? GetValueItem(TItem item) => item.GetValueAsObjectFromProperty(_internalValueField);

    private string? GetImageSrcItem(TItem item) => item.GetValueAsObjectFromProperty(_internalImageSrcField)?.ToString();

    private string? GetSelectedImageSrcItem(TItem item) => item.GetValueAsObjectFromProperty(_internalSelectedImageSrcField)?.ToString();

    private string? GetImageAltItem(TItem item) => item.GetValueAsObjectFromProperty(_internalImageAltField)?.ToString();

    private BitIconName? GetIconNameItem(TItem item) => item.GetBitIconNameFromProperty(_internalIconNameField);

    private bool GetIsEnabledItem(TItem item) => item.GetValueFromProperty(_internalIsEnabledField, true);

    private string? GetTextIdItem(TItem item)
    {
        var itemValue = GetValueItem(item);

        return $"RadioButtonListLabel{UniqueId}-{itemValue}";
    }

    private string? GetInputIdItem(TItem item)
    {
        var itemValue = GetValueItem(item);

        return $"RadioButtonList{UniqueId}-{itemValue}";
    }

    private bool GetIsCheckedItem(TItem item)
    {
        if (CurrentValue is null) return false;

        var itemValue = item.GetValueFromProperty<TValue>(_internalValueField);

        if (itemValue is null) return false;

        return EqualityComparer<TValue>.Default.Equals(itemValue, Value);
    }

    private string GetDivClassNameItem(TItem item)
    {
        const string itemRootElementClass = "bit-rbli";
        StringBuilder cssClass = new(itemRootElementClass);

        if (ItemTemplate is not null) return cssClass.ToString();

        if (GetIsCheckedItem(item))
        {
            cssClass
                .Append(' ')
                .Append(itemRootElementClass)
                .Append("-checked");
        }

        if (ItemLabelTemplate is not null) return cssClass.ToString();

        if (IsEnabled is false || GetIsEnabledItem(item) is false)
        {
            cssClass
               .Append(' ')
               .Append(itemRootElementClass)
               .Append("-disabled");
        }

        if (GetImageSrcItem(item).HasValue() || GetIconNameItem(item).HasValue)
        {
            cssClass
                .Append(' ')
                .Append(itemRootElementClass)
                .Append("-with-img");
        }

        return cssClass.ToString();
    }

    private string GetLabelClassNameItem(TItem item) => (GetImageSrcItem(item).HasValue() || GetIconNameItem(item).HasValue) && ItemLabelTemplate is null ? "bit-rbli-lbl-with-img" : "bit-rbli-lbl";

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");
}
