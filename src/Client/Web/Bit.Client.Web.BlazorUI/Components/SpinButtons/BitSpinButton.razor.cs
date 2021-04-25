﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSpinButton
    {
        public ElementReference InputElement { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }

        
        [Parameter] public double Min { get; set; } = 0;
        [Parameter] public double Max { get; set; } = double.MaxValue;
        [Parameter] public double Value { get; set; } = 0;
        [Parameter] public double Step { get; set; } = 1;
        [Parameter] public string Suffix { get; set; }
        [Parameter] public string Label { get; set; }
        [Parameter] public string IconName { get; set; }
        [Parameter] public LabelPosition LabelPosition { get; set; } = LabelPosition.left;

        private bool IsStepDecimal => Step.ToString().Contains(".");

        private double Normalize(double value)
        {
            var result = IsStepDecimal
                ? Math.Round(value, 2)
                : value;

            return result;
        }
        private string ValueWithSuffix => Suffix == string.Empty ? $"{Normalize(Value)}" : $"{Normalize(Value)} {Suffix}";

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
                await OnChange.InvokeAsync(ValueWithSuffix);
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
                var result = Value + Step;
                if (result <= Max)
                {
                    Value = Normalize(result);
                    await OnChange.InvokeAsync(ValueWithSuffix);
                }
                Console.WriteLine("Up Clicked!");
            }
        }

        protected virtual async Task HandleDownClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                var result = Value - Step;
                if (result >= Min)
                {
                    Value = Normalize(result);
                    await OnChange.InvokeAsync(ValueWithSuffix);
                }
                Console.WriteLine("Down Clicked!");
            }
        }

        protected override string GetElementClass()
        {
            ElementClassContainer.Clear();
            ElementClassContainer.Add($"bit-spin-button label-{LabelPosition}");
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
    }
}
