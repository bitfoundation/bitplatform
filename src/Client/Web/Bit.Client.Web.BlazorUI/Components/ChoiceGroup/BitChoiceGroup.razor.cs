using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI.Components.ChoiceGroup;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitChoiceGroup
    {
        private bool isRequired;

        /// <summary>
        /// If true, an option must be selected in the ChoiceGroup.
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
        /// List of options, each of which is a selection in the ChoiceGroup.
        /// </summary>
        [Parameter] public List<BitChoiceGroupOption> Options { get; set; } = new();

        /// <summary>
        /// Name of ChoiceGroup, this name is used to group each option into the same logical ChoiceGroup.
        /// </summary>
        [Parameter] public string Name { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Default selected key for ChoiceGroup.
        /// </summary>
        [Parameter] public string? DefaultSelectedKey { get; set; }

        /// <summary>
        /// ID of an element to use as the aria label for this ChoiceGroup.
        /// </summary>
        [Parameter] public string? AriaLabelledBy { get; set; }

        /// <summary>
        /// Descriptive label for the ChoiceGroup.
        /// </summary>
        [Parameter] public string? Label { get; set; }

        /// <summary>
        /// Used to customize the label for the ChoiceGroup.
        /// </summary>
        [Parameter] public RenderFragment? LabelFragment { get; set; }

        /// <summary>
        /// Callback for when the option has been changed
        /// </summary>
        [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }

        /// <summary>
        /// Callback for when the option clicked
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        protected override string RootElementClass => "bit-chg";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled && IsRequired
                                       ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => ValueInvalid is true
                                       ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" : string.Empty);
        }

        protected override async Task OnInitializedAsync()
        {
            if (DefaultSelectedKey.HasValue())
            {
                CurrentValue = Options.FirstOrDefault(o => o.Key == DefaultSelectedKey);
            }

            await base.OnInitializedAsync();
        }

        private string GetGroupLabelId() => $"ChoiceGroupLabel{UniqueId}";

        private string GetGroupAriaLabelledBy() => AriaLabelledBy ?? GetGroupLabelId();

        private string GetOptionInputId(BitChoiceGroupOption option) => $"ChoiceGroupOptionInput{UniqueId}-{option.Key}";

        private string GetOptionLabelId(BitChoiceGroupOption option) => $"ChoiceGroupOptionLabel{UniqueId}-{option.Key}";

        private bool GetOptionIsChecked(BitChoiceGroupOption option) =>
            CurrentValue is not null && CurrentValue.Key == option.Key;

        private string? GetOptionImageSrc(BitChoiceGroupOption option) =>
            GetOptionIsChecked(option) && option.SelectedImageSrc.HasValue()
            ? option.SelectedImageSrc
            : option.ImageSrc;

        private string GetOptionLabelClassName(BitChoiceGroupOption option) =>
            option.ImageSrc.HasValue() || option.iconName is not null ? "bit-chgo-lbl-with-img" : "bit-chgo-lbl";

        private string GetOptionImageSizeStyle(BitChoiceGroupOption option) => option.ImageSize is not null
                ? $"width:{option.ImageSize.Value.Width}px; height:{option.ImageSize.Value.Height}px;"
                : string.Empty;

        private string GetOptionDivClassName(BitChoiceGroupOption option)
        {
            const string itemRootElementClass = "bit-chgo";
            StringBuilder cssClass = new(itemRootElementClass);

            if (option.IsEnabled is false || IsEnabled is false)
            {
                cssClass
                   .Append(' ')
                   .Append(itemRootElementClass)
                   .Append("-disabled-")
                   .Append(VisualClassRegistrar());
            }

            if (option.ImageSrc.HasValue() || option.iconName is not null)
            {
                cssClass
                    .Append(' ')
                    .Append(itemRootElementClass)
                    .Append("-with-img-")
                    .Append(VisualClassRegistrar());
            }

            if (GetOptionIsChecked(option))
            {
                cssClass
                    .Append(' ')
                    .Append(itemRootElementClass)
                    .Append("-checked-")
                    .Append(VisualClassRegistrar());
            }

            return cssClass.ToString();
        }

        private async Task HandleClick(MouseEventArgs e, BitChoiceGroupOption option)
        {
            if (option.IsEnabled is false || IsEnabled is false) return;

            await OnClick.InvokeAsync(e);
        }

        private async Task HandleChange(ChangeEventArgs e, BitChoiceGroupOption option)
        {
            if (option.IsEnabled is false || IsEnabled is false) return;

            CurrentValue = option;

            await OnChange.InvokeAsync(e);
        }

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string? value,
            [MaybeNullWhen(false)] out BitChoiceGroupOption result,
            [NotNullWhen(false)] out string? validationErrorMessage)
        {
            throw new NotSupportedException($"This component does not parse string inputs." +
                $" Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");
        }

    }
}
