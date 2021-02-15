using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.SearchBoxes
{
    public partial class BitSearchBox
    {
        [Parameter] public string Value { get; set; }
        [Parameter] public string Placeholder { get; set; }
        [Parameter] public bool DisableAnimation { get; set; }
        [Parameter] public bool IsUndelined { get; set; }
        [Parameter] public string IconName { get; set; } = "Search";

        public ElementReference InputRef { get; set; }

        
        public string InputValueClass => Value.HasValue() ? "has-value" : string.Empty;
        public string AnimationClass => DisableAnimation ? "no-animation" : string.Empty;
        public string UndelinedClass => IsUndelined ? "underlined" : string.Empty;

        public string InputFocusClass = string.Empty;

        public void ClearInput() {
            Console.WriteLine("clear clicked");
            Value = string.Empty;
            InputRef.FocusAsync();
        }

        public void HandleInputFocusIn() => InputFocusClass = "focused";
        public void HandleInputFocusOut() => InputFocusClass = string.Empty;

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(Value):
                        Value = (string)parameter.Value;
                        break;
                    case nameof(Placeholder):
                        Placeholder = (string)parameter.Value;
                        break;
                    case nameof(DisableAnimation):
                        DisableAnimation = (bool)parameter.Value;
                        break;
                    case nameof(IsUndelined):
                        IsUndelined = (bool)parameter.Value;
                        break;
                    case nameof(IconName):
                        IconName = (string)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(parameters);
        }
    }
}
