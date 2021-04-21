using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSpinnerButton
    {
        public ElementReference InputElement { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public double Min { get; set; } = 0;
        [Parameter] public double Max { get; set; } = double.MaxValue;
        [Parameter] public double Value { get; set; } = 0;
        [Parameter] public double Step { get; set; } = 1;
        [Parameter] public string Suffix { get; set; }

        private string ValueToString => Value.ToString("N2");
        private double NormalizeValue(double value) => double.Parse(value.ToString("N2"));
        private string ValueWithSuffix => Suffix == string.Empty ? $"{ValueToString}" : $"{ValueToString} {Suffix}";
        //[Parameter] public int DefaultValue { get; set; } = 0;

        [Parameter] public EventCallback<string> OnChange { get; set; }

        protected virtual async Task HandleValueOnInputChange(ChangeEventArgs e) 
        {
            if (IsEnabled) 
            {
                var userEnteredValue = double.Parse(e.Value.ToString());

                if (userEnteredValue >= Min && userEnteredValue <= Max)
                {
                    Value = userEnteredValue;
                }
                else {
                    await JSRuntime.SetProperty(InputElement, "value", ValueWithSuffix);
                }
                    OnChange.InvokeAsync(ValueWithSuffix);
            }
        } 
        
        protected virtual async Task HandleOnChange(string value) 
        {
            if (IsEnabled) 
            {
                await OnChange.InvokeAsync(value);
            }
        }

        protected virtual async Task HandleUpClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                if (Value + Step <= Max)
                {
                    Value = NormalizeValue(Value + Step);
                    OnChange.InvokeAsync(ValueWithSuffix);
                }
                Console.WriteLine("Up Clicked!");
            }
        }

        protected virtual async Task HandleDownClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                if (Value - Step >= Min)
                {
                    Value = NormalizeValue(Value - Step);
                    OnChange.InvokeAsync(ValueWithSuffix);
                }
                Console.WriteLine("Down Clicked!");
            }
        }

        protected override string GetElementClass()
        {
            return base.GetElementClass();
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
                    case nameof(Step):
                        Step = (double)parameter.Value;
                        break;
                    case nameof(Suffix):
                        Suffix = (string)parameter.Value;
                        break;
                    case nameof(OnChange):
                        OnChange = (EventCallback<string>)parameter.Value;
                        break;
                }
            }
            return base.SetParametersAsync(parameters);
        }
    }
}
