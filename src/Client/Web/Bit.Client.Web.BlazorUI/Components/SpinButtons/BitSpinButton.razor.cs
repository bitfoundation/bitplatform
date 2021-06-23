using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSpinButton
    {
        private LabelPosition labelPosition = LabelPosition.Left;
        public ElementReference InputElement { get; set; }
        public double Value { get; set; }

        [Inject] public IJSRuntime? JSRuntime { get; set; }

        [Parameter] public double Min { get; set; } = 0;
        [Parameter] public double Max { get; set; } = double.MaxValue;
        [Parameter] public double DefaultValue { get; set; } = 0;
        [Parameter] public double Step { get; set; } = 1;
        [Parameter] public string Suffix { get; set; } = string.Empty;
        [Parameter] public string Label { get; set; } = string.Empty;
        [Parameter] public string IconName { get; set; } = string.Empty;

        [Parameter]
        public LabelPosition LabelPosition
        {
            get => labelPosition;
            set
            {
                labelPosition = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter] public EventCallback<string> OnChange { get; set; }

        private async Task HandleUpClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                var result = Value + Step;
                if (result <= Max)
                {
                    Value = Normalize(result);
                    await OnChange.InvokeAsync(ValueWithSuffix);
                }
            }
        }

        private async Task HandleDownClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                var result = Value - Step;
                if (result >= Min)
                {
                    Value = Normalize(result);
                    await OnChange.InvokeAsync(ValueWithSuffix);
                }
            }
        }

        protected override string RootElementClass => "bit-spb";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => $"{RootElementClass}-label-{LabelPosition}-{VisualClassRegistrar()}");
        }

        private async Task HandleValueOnInputChange(ChangeEventArgs e)
        {
            if (IsEnabled)
            {
                var userInput = e.Value?.ToString();
                var isNumber = double.TryParse(userInput, out var numericValue);
                if (isNumber && numericValue >= Min && numericValue <= Max)
                {
                    Value = numericValue;
                    await OnChange.InvokeAsync(ValueWithSuffix);
                }
                else
                {
                    _ = JSRuntime?.SetProperty(InputElement, "value", ValueWithSuffix);
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                Value = DefaultValue;
                _ = JSRuntime?.SetProperty(InputElement, "value", ValueWithSuffix);
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        private bool IsStepDecimal => Step.ToString().Contains(".");

        private double Normalize(double value)
        {
            var result = IsStepDecimal
                ? Math.Round(value, 2)
                : value;

            return result;
        }

        private string ValueWithSuffix => string.IsNullOrEmpty(Suffix) ? $"{Normalize(Value)}" : $"{Normalize(Value)} {Suffix}";
    }
}
