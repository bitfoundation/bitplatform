﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitToggleButton
    {
        private ButtonStyle buttonStyle = ButtonStyle.Primary;

        private int? tabIndex;
        private bool isChecked;

        [Parameter] public bool AllowDisabledFocus { get; set; } = true;
        [Parameter] public string? AriaDescription { get; set; }
        [Parameter] public bool AriaHidden { get; set; }
        [Parameter] public string? AriaLabel { get; set; }
        /// <summary>
        /// determine if the button is checked state, default is true
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
        [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }
        /// <summary>
        /// the icon that shows in the button
        /// </summary>
        [Parameter] public string? IconName { get; set; }
        /// <summary>
        /// the text that shows in the label
        /// </summary>
        [Parameter] public string? Label { get; set; }
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        [Parameter] public EventCallback<bool> OnChange { get; set; }
        [Parameter]
        public ButtonStyle ButtonStyle
        {
            get => buttonStyle;
            set
            {
                buttonStyle = value;
                ClassBuilder.Reset();
            }
        }
        private bool IsCheckedHasBeenSet;
        protected virtual async Task HandleOnClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                await OnClick.InvokeAsync(e);
                if (IsCheckedHasBeenSet && IsCheckedChanged.HasDelegate is false) return;
                IsChecked = !IsChecked;
                await OnChange.InvokeAsync(IsChecked);
            }
        }

        protected override string RootElementClass => "bit-tgl-btn";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false
                                           ? string.Empty
                                           : ButtonStyle == ButtonStyle.Primary
                                               ? $"{RootElementClass}-primary-{VisualClassRegistrar()}"
                                               : $"{RootElementClass}-standard-{VisualClassRegistrar()}");
            ClassBuilder.Register(() => IsChecked is false
                                            ? string.Empty
                                            : $"{RootElementClass}-checked");
        }

        protected override async Task OnInitializedAsync()
        {
            if (!IsEnabled)
            {
                tabIndex = AllowDisabledFocus ? null : -1;
            }

            await base.OnInitializedAsync();
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            IsCheckedHasBeenSet = false;
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(AllowDisabledFocus):
                        AllowDisabledFocus = (bool)parameter.Value;
                        break;
                    case nameof(AriaDescription):
                        AriaDescription = (string?)parameter.Value;
                        break;
                    case nameof(AriaHidden):
                        AriaHidden = (bool)parameter.Value;
                        break;
                    case nameof(AriaLabel):
                        AriaLabel = (string?)parameter.Value;
                        break;
                    case nameof(IsChecked):
                        IsCheckedHasBeenSet = true;
                        IsChecked = (bool)parameter.Value;
                        break;
                    case nameof(IconName):
                        IconName = (string?)parameter.Value;
                        break;
                    case nameof(Label):
                        Label = (string?)parameter.Value;
                        break;
                    case nameof(OnClick):
                        OnClick = (Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>)parameter.Value;
                        break;
                    case nameof(OnChange):
                        OnChange = (EventCallback<bool>)parameter.Value;
                        break;
                    case nameof(ButtonStyle):
                        ButtonStyle = (Bit.Client.Web.BlazorUI.ButtonStyle)parameter.Value;
                        break;
                    case nameof(IsCheckedChanged):
                        IsCheckedChanged = (EventCallback<bool>)parameter.Value;
                        break;
                }
            }
            return base.SetParametersAsync(parameters);
        }
    }
}
