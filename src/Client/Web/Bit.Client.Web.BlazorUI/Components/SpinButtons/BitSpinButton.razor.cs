using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSpinButton
    {
        private LabelPosition labelPosition = LabelPosition.left;
        public ElementReference InputElement { get; set; }
        public double Value { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public double Min { get; set; } = 0;
        [Parameter] public double Max { get; set; } = double.MaxValue;
        [Parameter] public double DefaultValue { get; set; } = 0;
        [Parameter] public double Step { get; set; } = 1;
        [Parameter] public string Suffix { get; set; }
        [Parameter] public string Label { get; set; }
        [Parameter] public string IconName { get; set; }

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
                var userInput = e.Value.ToString();
                var isNumber = double.TryParse(userInput, out var numericValue);
                if (isNumber && numericValue >= Min && numericValue <= Max)
                {
                    Value = numericValue;
                    await OnChange.InvokeAsync(ValueWithSuffix);
                }
                else
                {
                    await JSRuntime.SetProperty(InputElement, "value", ValueWithSuffix);
                }
            }
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                Value = DefaultValue;
                await JSRuntime.SetProperty(InputElement, "value", ValueWithSuffix);
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(Min):
                        Min = (double)parameter.Value;
                        break;
                    case nameof(Max):
                        Max = (double)parameter.Value;
                        break;
                    case nameof(Value):
                        Value = (double)parameter.Value;
                        break;
                    case nameof(DefaultValue):
                        DefaultValue = (double)parameter.Value;
                        break;
                    case nameof(Step):
                        Step = (double)parameter.Value;
                        break;
                    case nameof(Suffix):
                        Suffix = (string)parameter.Value;
                        break;
                    case nameof(Label):
                        Label = (string)parameter.Value;
                        break;
                    case nameof(OnChange):
                        OnChange = (EventCallback<string>)parameter.Value;
                        break;
                    case nameof(IconName):
                        IconName = (string)parameter.Value;
                        break;
                    case nameof(LabelPosition):
                        LabelPosition = (LabelPosition)parameter.Value;
                        break;
                }
            }
            return base.SetParametersAsync(parameters);
        }

        private bool IsStepDecimal => Step.ToString().Contains(".");

        private double Normalize(double value)
        {
            var result = IsStepDecimal
                ? Math.Round(value, 2)
                : value;

            return result;
        }

        private string ValueWithSuffix => Suffix == string.Empty ? $"{Normalize(Value)}" : $"{Normalize(Value)} {Suffix}";
    }
}
