using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitToggle
    {
#pragma warning disable CA1823 // Avoid unused private fields
        private bool IsCheckedHasBeenSet;
#pragma warning restore CA1823 // Avoid unused private fields

        private bool isChecked;
        private Guid Id = Guid.NewGuid();
        private string? LabelledById;
        private string? StateText;
        private string LabelId => Id + "-label";
        private string StateTextId => Id + "-stateText";
        private string? AriaChecked => IsChecked ? "true" : "false";

        /// <summary>
        /// Default text of the toggle when it is neither ON or OFF.
        /// </summary>
        [Parameter] public string? DefaultText { get; set; }

        /// <summary>
        /// Label of the toggle.
        /// </summary>
        [Parameter] public string? Label { get; set; }

        /// <summary>
        /// Custom label of the toggle.
        /// </summary>
        [Parameter] public RenderFragment? LabelFragment { get; set; }

        /// <summary>
        /// Denotes role of the toggle, default is switch.        
        /// </summary>
        [Parameter] public string? Role { get; set; } = "switch";

        /// <summary>
        /// Text to display when toggle is ON.
        /// </summary>
        [Parameter] public string? OnText { get; set; }

        /// <summary>
        /// Text to display when toggle is OFF.
        /// </summary>
        [Parameter] public string? OffText { get; set; }

        /// <summary>
        /// Whether the label (not the onText/offText) should be positioned inline with the toggle control. Left (right in RTL) side when on/off text provided VS right (left in RTL) side when there is no on/off text.
        /// </summary>
        [Parameter] public bool IsInlineLabel { get; set; }

        /// <summary>
        /// Checked state of the toggle.
        /// </summary>
        [Parameter]
        public bool IsChecked
        {
            get => isChecked;
            set
            {
                if (value == isChecked) return;
                isChecked = value;
                ClassBuilder.Reset();
                _ = IsCheckedChanged.InvokeAsync(value);
            }
        }

        /// <summary>
        /// Callback that is called when the IsChecked parameter changed.
        /// </summary>
        [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }

        /// <summary>
        /// Callback that is called when the checked value has changed.
        /// </summary>
        [Parameter] public EventCallback<bool> OnChange { get; set; }

        protected override string RootElementClass => "bit-tgl";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() =>
            {
                var isCheckedClass = IsChecked ? "checked" : "unchecked";
                var isEnabledClass = IsEnabled ? "enabled" : "disabled";
                return $"{RootElementClass}-{isEnabledClass}-{isCheckedClass}-{VisualClassRegistrar()}";
            });

            ClassBuilder.Register(() => IsInlineLabel ? $"{RootElementClass}-inline-{VisualClassRegistrar()}" : string.Empty);
            ClassBuilder.Register(() => OnText.HasNoValue() || OffText.HasNoValue()
                                            ? $"{RootElementClass}-noonoff-{VisualClassRegistrar()}" : string.Empty);
        }

        protected virtual async Task HandleOnClick(MouseEventArgs e)
        {
            if (IsEnabled is false || IsCheckedChanged.HasDelegate is false) return;
            IsChecked = !IsChecked;
            await OnChange.InvokeAsync(IsChecked);
        }

        protected override async Task OnInitializedAsync()
        {
            StateText = (IsChecked ? OnText : OffText) ?? DefaultText ?? "";

            if (AriaLabel.HasNoValue())
            {
                if (Label.HasValue())
                {
                    LabelledById = LabelId;
                }
                if (StateText.HasValue())
                {
                    LabelledById = LabelledById.HasValue() ? LabelId + " " + StateTextId : StateTextId;
                } 
            }

            await base.OnInitializedAsync();
        }
    }
}
