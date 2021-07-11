﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSearchBox
    {
        private string? inputValue;
        private bool disableAnimation;
        private bool isUnderlined;
        private bool inputHasFocus;
        private string? width;

        public ElementReference InputRef { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Inject] public IJSRuntime JSRuntime { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Parameter] public string? Placeholder { get; set; }

        [Parameter] public string IconName { get; set; } = "Search";

        [Parameter] public EventCallback OnClear { get; set; }

        [Parameter] public EventCallback<string> OnSearch { get; set; }

        [Parameter]
        public string? Value
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
        public string? Width
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

        private async Task HandleOnKeyDown(KeyboardEventArgs eventArgs)
        {
            if (IsEnabled)
            {
                if (eventArgs.Code == "Escape")
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

        protected override string RootElementClass => "bit-sch-box";

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
    }
}
