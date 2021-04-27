using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSearchBox
    {
        private string inputValue;
        private bool disableAnimation;
        private bool isUnderlined;
        private bool inputHasFocus = false;
        private string width;

        public ElementReference InputRef { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public string Placeholder { get; set; }

        [Parameter] public string IconName { get; set; } = "Search";

        [Parameter] public EventCallback OnClear { get; set; }

        [Parameter] public EventCallback<string> OnSearch { get; set; }

        [Parameter]
        public string Value
        {
            get => inputValue;
            set
            {
                inputValue = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public bool DisableAnimation
        {
            get => disableAnimation;
            set
            {
                disableAnimation = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public bool IsUnderlined
        {
            get => isUnderlined;
            set
            {
                isUnderlined = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public string Width
        {
            get => width;
            set
            {
                width = value;
                StyleBuilder.Reset();
            }
        }

        private bool InputHasFocus
        {
            get => inputHasFocus;
            set
            {
                inputHasFocus = value;
                ClassBuilder.Reset();
            }
        }

        protected virtual async Task HandleOnClear()
        {
            if (IsEnabled)
            {
                Value = string.Empty;
                await InputRef.FocusAsync();
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
                    await InputRef.FocusAsync();
                    await OnClear.InvokeAsync();
                }
                else
                {
                    Value = await JSRuntime.GetProperty(InputRef, "value");
                    await OnSearch.InvokeAsync(Value);
                }
            }
        }

        public void HandleInputFocusIn()
        {
            InputHasFocus = true;
        }

        public void HandleInputFocusOut()
        {
            InputHasFocus = false;
        }

        protected override string RootElementClass => "bit-sch-box-container";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => Value.HasValue() ? $"{RootElementClass}-has-value-{VisualClassRegistrar()}" : string.Empty);
            ClassBuilder.Register(() => DisableAnimation ? $"{RootElementClass}-no-animation-{VisualClassRegistrar()}" : string.Empty);
            ClassBuilder.Register(() => IsUnderlined ? $"{RootElementClass}-underlined-{VisualClassRegistrar()}" : string.Empty);
            ClassBuilder.Register(() => InputHasFocus ? $"{RootElementClass}-focused-{VisualClassRegistrar()}" : string.Empty);
        }

        protected override void RegisterComponentStyles()
        {
            StyleBuilder.Register(() => Width.HasValue() ? $"width: {Width}" : string.Empty);
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
