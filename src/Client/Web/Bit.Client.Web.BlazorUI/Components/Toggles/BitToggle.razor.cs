using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitToggle
    {
        private bool isChecked;
        private bool IsCheckedHasBeenSet;
        private Guid Id = Guid.NewGuid();
        private string? LabelledById;
        private string? StateText;
        private string LabelId => Id + "-label";
        private string StateTextId => Id + "-stateText";
        private string? AriaChecked => IsChecked ? "true" : "false";

        [Parameter] public string? DefaultText { get; set; }

        [Parameter] public string? OnText { get; set; }

        [Parameter] public string? OffText { get; set; }

        [Parameter] public bool IsInlineLabel { get; set; }

        [Parameter] public string? Label { get; set; }

        [Parameter] public RenderFragment? LabelFragment { get; set; }

        [Parameter] public string? Role { get; set; } = "Switch";

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

        [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }

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
