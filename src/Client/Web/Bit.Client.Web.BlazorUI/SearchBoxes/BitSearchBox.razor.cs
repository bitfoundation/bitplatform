using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.SearchBoxes
{
    public partial class BitSearchBox
    {
        public string InputFocusClass = string.Empty;

        [Parameter] public string Value { get; set; }
        [Parameter] public string Placeholder { get; set; }
        [Parameter] public bool DisableAnimation { get; set; }
        [Parameter] public bool IsUndelined { get; set; }
        [Parameter] public string IconName { get; set; } = "Search";
        [Parameter] public string Width { get; set; }
        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback OnClear{ get; set; }

        public ElementReference InputRef { get; set; }

        public string InputValueClass => Value.HasValue() ? "has-value" : string.Empty;
        public string AnimationClass => DisableAnimation ? "no-animation" : string.Empty;
        public string UndelinedClass => IsUndelined ? "underlined" : string.Empty;
        public string WidthStyle => $"width: {Width}";

        protected virtual async Task HandleOnClear() {
            if (IsEnabled)
            {
                Value = string.Empty;
                _ = InputRef.FocusAsync();
                await OnClear.InvokeAsync();
            }
        }

        protected virtual async Task HandleOnKeyDown(string value, string keyCode)
        {
            if (IsEnabled)
            {
                if (keyCode == "Escape")
                {
                    Value = string.Empty;
                    _ = InputRef.FocusAsync();
                    await OnClear.InvokeAsync();
                }
                else {
                    await OnSearch.InvokeAsync(value);
                }
            }
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
                    case nameof(Width):
                        Width = (string)parameter.Value;
                        break;
                    case nameof(OnSearch):
                        OnSearch = (EventCallback<string>)parameter.Value;
                        break;
                    case nameof(OnClear):
                        OnClear = (EventCallback)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(parameters);
        }
    }
}
