using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSlider
    {
        private ElementReference ContainerRef { get; set; }
        private ElementReference TitleRef { get; set; }
        private ElementReference ValueLabelRef { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }
        //[Parameter] public string AriaLabel { get; set; }
        [Parameter] public int DefaultLowerValue { get; set; }
        [Parameter] public int DefaultValue { get; set; }
        [Parameter] public int Min { get; set; } = 1;
        [Parameter] public int Max { get; set; } = 10;
        [Parameter] public int LowerValue { get; set; }
        [Parameter] public bool OriginFromZero { get; set; }
        [Parameter] public string Label { get; set; }
        [Parameter] public bool Ranged { get; set; }
        [Parameter] public bool ShowValue { get; set; } = true;
        [Parameter] public int Step { get; set; } = 1;
        [Parameter] public int Value { get; set; }
        [Parameter] public int[] Range { get; set; }
        [Parameter] public bool Vertical { get; set; }
        [Parameter] public string ValueFormat { get; set; }
        [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }
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
        private string styleContainer;
        private int inputHeight;

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsReadonly
                                                ? $"{RootElementClass}-readonly-{VisualClassRegistrar()}"
                                                : string.Empty);

            ClassBuilder.Register(() => Vertical
                                                ? $"{RootElementClass}{(Ranged ? "-ranged" : null)}-column"
                                                : $"{RootElementClass}{(Ranged ? "-ranged" : null)}-row");
        }

        protected override async Task OnInitializedAsync()
        {
            if (DefaultValue != default)
            {
                Value = DefaultValue;
            }

            if (DefaultLowerValue != default)
            {
                LowerValue = DefaultLowerValue;
            }

            if (Ranged)
            {
                Range = new[]
                {
                    LowerValue,
                    Value
                };
            }

            if (!Vertical)
            {
                FillSlider();
            }

            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && Vertical)
            {
                if (Ranged)
                {
                    inputHeight = await JSRuntime.GetHeight(RootElement);

                    if (!string.IsNullOrEmpty(Label))
                    {
                        var titleHeight = await JSRuntime.GetHeight(TitleRef);
                        inputHeight -= titleHeight;
                    }

                    if (ShowValue)
                    {
                        var valueLabelHeight = await JSRuntime.GetHeight(ValueLabelRef);
                        inputHeight -= (valueLabelHeight * 2);
                    }
                }
                else
                {
                    inputHeight = await JSRuntime.GetHeight(ContainerRef);
                }
                FillSlider();
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        protected virtual async Task HandleChange(ChangeEventArgs e)
        {
            if (IsEnabled)
            {
                await OnChange.InvokeAsync(e);
            }
        }

        private void HandleInput(ChangeEventArgs e)
        {
            if (!Ranged)
            {
                Value = Convert.ToInt32(e.Value);
                FillSlider();
            }
            else
            {
                Range[0] = Convert.ToInt32(e.Value);
            }
        }

        private void HandleInput(ChangeEventArgs e, int index)
        {
            if (Ranged)
            {
                Range[index] = Convert.ToInt32(e.Value);
                FillSlider();
            }
        }

        private void FillSlider()
        {
            if (Ranged)
            {
                styleProgress = $"--a: {Range[0]}; --b: {Range[1]}; --min: {Min}; --max: {Max}";
                if (Vertical)
                {
                    styleContainer = $"width: {inputHeight}px; height: {inputHeight}px;";
                    StateHasChanged();
                }
            }
            else
            {
                if (Vertical)
                {
                    styleProgress = $"--value: {Value}; --min: {Min}; --max: {Max}; width: {inputHeight}px; transform: rotate(270deg) translateX(-{(inputHeight - 12)}px);";
                    StateHasChanged();
                }
                else
                {
                    styleProgress = $"--value: {Value}; --min: {Min}; --max: {Max};";
                }
            }
        }

        private string GetValueDisplay(int val)
        {
            if (string.IsNullOrEmpty(ValueFormat))
            {
                return val.ToString();
            }
            else if (ValueFormat.Contains("p", StringComparison.CurrentCultureIgnoreCase))
            {
                int digitCount = (Max - 1).ToString().Length;
                return (val / Math.Pow(10, digitCount)).ToString(ValueFormat);
            }
            else
            {
                return val.ToString(ValueFormat);
            }
        }
    }
}
