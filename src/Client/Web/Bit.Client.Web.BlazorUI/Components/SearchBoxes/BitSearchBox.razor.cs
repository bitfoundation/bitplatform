using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSearchBox
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }

        public string InputFocusClass = string.Empty;

        [Parameter] public string Value { get; set; }
        [Parameter] public string Placeholder { get; set; }
        [Parameter] public bool DisableAnimation { get; set; }
        [Parameter] public bool IsUnderlined { get; set; }
        [Parameter] public string IconName { get; set; } = "Search";
        [Parameter] public string Width { get; set; }
        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback OnClear { get; set; }

        public ElementReference InputRef { get; set; }

        protected virtual async Task HandleOnClear()
        {
            if (IsEnabled)
            {
                Value = string.Empty;
                _ = InputRef.FocusAsync();
                await OnClear.InvokeAsync();
            }
        }

        protected virtual async Task HandleOnKeyDown(KeyboardEventArgs k)
        {
            if (IsEnabled)
            {
                if (k.Code == "Escape")
                {
                    Value = string.Empty;
                    _ = InputRef.FocusAsync();
                    await OnClear.InvokeAsync();
                }
                else
                {
                    Value = await JSRuntime.GetProperty(InputRef, "value");
                    await OnSearch.InvokeAsync(Value);
                }
            }
        }

        public void HandleInputFocusIn() => InputFocusClass = "focused";
        public void HandleInputFocusOut() => InputFocusClass = string.Empty;

        protected override string GetElementClass()
        {    
            ElementClassContainer.Clear();
            ElementClassContainer.Add("bit-search-box");
            ElementClassContainer.Add("bit-search-box-container");
            ElementClassContainer.Add(Value.HasValue() ? "has-value" : string.Empty);
            ElementClassContainer.Add(DisableAnimation ? "no-animation" : string.Empty);
            ElementClassContainer.Add(IsUnderlined ? "underlined" : string.Empty);
            ElementClassContainer.Add(InputFocusClass);
            
            return base.GetElementClass();
        }

        protected override string GetElementStyle()
        {
            ElementStyleContainer.Add(Width.HasValue() ? $"width: {Width}" : string.Empty);
            return base.GetElementStyle();
        }

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
                    case nameof(IsUnderlined):
                        IsUnderlined = (bool)parameter.Value;
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
