using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSlider
    {
        //[Parameter] public string AriaLabel { get; set; }
        //[Parameter] public int DefaultLowerValue { get; set; }
        [Parameter] public int DefaultValue { get; set; }
        [Parameter] public int Min { get; set; } = 1;
        [Parameter] public int Max { get; set; } = 10;
        //[Parameter] public int LowerValue { get; set; }
        [Parameter] public bool OriginFromZero { get; set; }
        [Parameter] public string Label { get; set; }
        [Parameter] public bool Ranged { get; set; }
        [Parameter] public bool ShowValue { get; set; } = true;
        [Parameter] public int Step { get; set; } = 1;
        [Parameter] public int Value { get; set; }
        [Parameter] public bool Vertical { get; set; }
        [Parameter] public string ValueFormat { get; set; }
        [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }
        [Parameter] public EventCallback<ChangeEventArgs> OnInput { get; set; }
        protected override string RootElementClass => "bit-slider";

        [Parameter]
        public bool IsReadonly
        {
            get => isReadOnly;
            set
            {
                isReadOnly = value;
                ClassBuilder.Reset();
            }
        }
        private bool isReadOnly;
        private string styleProgress;

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsReadonly
                                                ? $"{RootElementClass}-readonly-{VisualClassRegistrar()}"
                                                : string.Empty);
            ClassBuilder.Register(() => $"{RootElementClass}-large-{VisualClassRegistrar()}");
        }

        protected override async Task OnInitializedAsync()
        {
            if (DefaultValue != default)
            {
                Value = DefaultValue;
            }
            FillSlider();

            await base.OnInitializedAsync();
        }

        protected virtual async Task HandleChange(ChangeEventArgs e)
        {
            if (IsEnabled)
            {
                await OnChange.InvokeAsync(e);
            }
        }

        protected virtual async Task HandleInput(ChangeEventArgs e)
        {
            if (!Ranged)
            {
                Value = Convert.ToInt32(e.Value);
                FillSlider();
            }

            if (IsEnabled)
            {
                await OnInput.InvokeAsync(e);
            }
        }

        private void FillSlider()
        {
            styleProgress = $"--value: {Value}; --min: {Min}; --max: {Max};";
        }

        private string GetValueDisplay()
        {
            if (string.IsNullOrEmpty(ValueFormat))
            {
                return Value.ToString();
            }
            else if (ValueFormat.Contains("p", StringComparison.CurrentCultureIgnoreCase))
            {
                int digitCount = (Max - 1).ToString().Length;
                return (Value / Math.Pow(10, digitCount)).ToString(ValueFormat);
            }
            else
            {
                return Value.ToString(ValueFormat);
            }
        }
    }
}
