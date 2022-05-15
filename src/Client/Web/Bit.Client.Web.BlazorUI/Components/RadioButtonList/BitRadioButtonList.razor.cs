using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitRadioButtonList<TItem, TValue>
    {
        private bool isRequired;
        private string? imageSizeStyle;

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
        /// Descriptive label for the radio button list.
        /// </summary>
        [Parameter] public string? Label { get; set; }

        /// <summary>
        /// ID of an element to use as the aria label for this RadioButtonList.
        /// </summary>
        [Parameter] public string AriaLabelledBy { get; set; } = string.Empty;

        /// <summary>
        /// Used to customize the label for the radio button list.
        /// </summary>
        [Parameter] public RenderFragment? LabelFragment { get; set; }

        /// <summary>
        /// Name of RadioButtonList, this name is used to group each item into the same logical RadioButtonList
        /// </summary>
        [Parameter] public string Name { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Sets the data source that populates the items of the list.
        /// </summary>
        [Parameter] public IEnumerable<TItem>? Items { get; set; }

        /// <summary>
        /// The name of the field from the model that will be shown to the user.
        /// </summary>
        [Parameter] public string TextField { get; set; } = "Text";

        /// <summary>
        /// The name of the field from the model that will be the underlying value.
        /// </summary>
        [Parameter] public string ValueField { get; set; } = "Value";

        /// <summary>
        /// The name of the field from the model that will be the BitIconName.
        /// </summary>
        [Parameter] public string IconNameField { get; set; } = "IconName";

        /// <summary>
        /// The name of the field from the model that will be the image src.
        /// </summary>
        [Parameter] public string ImageSrcField { get; set; } = "ImageSrc";

        /// <summary>
        /// The name of the field from the model that will be the selected image src.
        /// </summary>
        [Parameter] public string SelectedImageSrcField { get; set; } = "SelectedImageSrc";

        /// <summary>
        /// The name of the field from the model that will be the image alternate text.
        /// </summary>
        [Parameter] public string ImageAltField { get; set; } = "ImageAlt";

        /// <summary>
        /// The name of the field from the model that will be enable item.
        /// </summary>
        [Parameter] public string IsEnabledField { get; set; } = "IsEnabled";

        /// <summary>
        /// The field from the model that will be shown to the user.
        /// </summary>
        [Parameter] public Expression<Func<TItem, object>>? TextSelector { get; set; }

        /// <summary>
        /// The field from the model that will be the underlying value.
        /// </summary>
        [Parameter] public Expression<Func<TItem, object>>? ValueSelector { get; set; }

        /// <summary>
        /// The field from the model that will be the BitIconName.
        /// </summary>
        [Parameter] public Expression<Func<TItem, BitIconName>>? IconNameSelector { get; set; }

        /// <summary>
        /// The field from the model that will be the image src.
        /// </summary>
        [Parameter] public Expression<Func<TItem, object>>? ImageSrcSelector { get; set; }

        /// <summary>
        /// The field from the model that will be the selected image src.
        /// </summary>
        [Parameter] public Expression<Func<TItem, object>>? SelectedImageSrcSelector { get; set; }

        /// <summary>
        /// The field from the model that will be the image alternate text.
        /// </summary>
        [Parameter] public Expression<Func<TItem, object>>? ImageAltSelector { get; set; }

        /// <summary>
        /// The field from the model that will be enable item.
        /// </summary>
        [Parameter] public Expression<Func<TItem, bool>>? IsEnabledSelector { get; set; }

        /// <summary>
        /// The width and height of the image in px for item field.
        /// </summary>
        [Parameter] public Size? ImageSize { get; set; }

        /// <summary>
        /// Callback for when the option clicked
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        /// <summary>
        /// Callback for when the option has been changed
        /// </summary>
        [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }

        public string LabelId { get; set; } = string.Empty;

        protected override string RootElementClass => "bit-rbl";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled && IsRequired
                                       ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => ValueInvalid is true 
                                       ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" : string.Empty);
        }

        protected override Task OnInitializedAsync()
        {
            LabelId = $"RadioButtonListLabel{UniqueId}";

            if (TextSelector is not null)
            {
                TextField = TextSelector.GetName();
            }

            if (ValueSelector is not null)
            {
                ValueField = ValueSelector.GetName();
            }

            if (IconNameSelector is not null)
            {
                IconNameField = IconNameSelector.GetName();
            }

            if (ImageSrcSelector is not null)
            {
                ImageSrcField = ImageSrcSelector.GetName();
            }

            if (SelectedImageSrcSelector is not null)
            {
                SelectedImageSrcField = SelectedImageSrcSelector.GetName();
            }

            if (ImageAltSelector is not null)
            {
                ImageAltField = ImageAltSelector.GetName();
            }

            if (IsEnabledSelector is not null)
            {
                IsEnabledField = IsEnabledSelector.GetName();
            }

            return base.OnInitializedAsync();
        }

        protected override Task OnParametersSetAsync()
        {
            if (ImageSize is not null)
            {
                imageSizeStyle = $" width:{ImageSize.Value.Width}px; height:{ImageSize.Value.Height}px;";
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

        private string GetAriaLabelledBy() => Label.HasValue() || LabelFragment is not null ? LabelId : AriaLabelledBy;

        private string? GetTextItem(TItem item) => item.GetValueAsStringFromProperty(TextField);

        private object? GetValueItem(TItem item) => item.GetValueFromProperty(ValueField);

        private string? GetImageSrcItem(TItem item) => item.GetValueAsStringFromProperty(ImageSrcField);

        private string? GetSelectedImageSrcItem(TItem item) => item.GetValueAsStringFromProperty(SelectedImageSrcField);

        private string? GetImageAltItem(TItem item) => item.GetValueAsStringFromProperty(ImageAltField);

        private BitIconName? GetIconNameItem(TItem item) => item.GetBitIconNameFromProperty(IconNameField);

        private bool GetIsEnabledItem(TItem item) => item.GetValueFromProperty(IsEnabledField, true);

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

            var itemValue = item.GetValueFromProperty<TValue>(ValueField);

            if (itemValue is null) return false;

            return EqualityComparer<TValue>.Default.Equals(itemValue, Value);
        }

        private string GetDivClassNameItem(TItem item)
        {
            const string itemRootElementClass = "bit-rbli";
            StringBuilder cssClass = new(itemRootElementClass);

            if (IsEnabled is false || GetIsEnabledItem(item) is false)
            {
                cssClass
                   .Append(' ')
                   .Append(itemRootElementClass)
                   .Append("-disabled-")
                   .Append(VisualClassRegistrar());
            }

            if (GetImageSrcItem(item).HasValue() || GetIconNameItem(item).HasValue)
            {
                cssClass
                    .Append(' ')
                    .Append(itemRootElementClass)
                    .Append("-with-img-")
                    .Append(VisualClassRegistrar());
            }

            if (GetIsCheckedItem(item))
            {
                cssClass
                    .Append(' ')
                    .Append(itemRootElementClass)
                    .Append("-checked-")
                    .Append(VisualClassRegistrar());
            }

            return cssClass.ToString();
        }

        private string GetLabelClassNameItem(TItem item) => GetImageSrcItem(item).HasValue() || GetIconNameItem(item).HasValue ? "bit-rbli-lbl-with-img" : "bit-rbli-lbl";

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
            => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");
    }
}
