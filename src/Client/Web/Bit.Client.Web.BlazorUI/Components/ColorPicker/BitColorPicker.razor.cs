using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitColorPicker : IAsyncDisposable
    {
        private string? onWindowMouseUpAbortControllerId;

        private string? onWindowMouseMoveAbortControllerId;

        private string? saturationPickerBackgroundRgbCss;

        private string? saturationPickerBackgroundRgbaCss;

        private bool saturationPickerMouseDown;

        private BitColorPosition? saturationPickerThumbPosition;

        private BitColor color = new BitColor();

        private BitColorType colorType;

        private double hue;

        private double selectedSaturation = 1;

        private double selectedValue = 1;

        private bool ColorHasBeenSet;

        private bool AlphaHasBeenSet;

        public ElementReference SaturationPickerRef { get; set; }

        public string? Hex => color.Hex;

        public string Rgb => GetRgbCss(color);

        public string Rgba => GetRgbaCss(color);

        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// Whether to show a slider for editing alpha value.
        /// </summary>
        [Parameter]
        public bool ShowAlphaSlider { get; set; } = true;

        /// <summary>
        /// CSS-compatible string to describe the color.
        /// </summary>
        [Parameter]
        public string Color
        {
            get => colorType == BitColorType.Hex ? color.Hex! : GetRgbaCss(color);
            set
            {
                var valueAsBitColor = new BitColor(value, color.Alpha);
                if (valueAsBitColor == color) return;
                color = valueAsBitColor;
                hue = color.Hsv.Hue;
                SetSaturationPickerBackground();

                colorType = value.HasValue() && value.StartsWith("#", StringComparison.InvariantCultureIgnoreCase) ? BitColorType.Hex : BitColorType.Rgb;

                ColorChanged.InvokeAsync(value);
                AlphaChanged.InvokeAsync(color.Alpha);
                OnChange.InvokeAsync(value);
            }
        }

        [Parameter] public EventCallback<string> ColorChanged { get; set; }


        [Parameter]
        public double Alpha
        {
            get => color.Alpha;
            set
            {
                if (color.Alpha == value) return;
                color.Alpha = value;

                AlphaChanged.InvokeAsync(value);

                string colorValue = colorType == BitColorType.Hex ? color.Hex! : GetRgbaCss(color);
                OnChange.InvokeAsync(colorValue);
                ColorChanged.InvokeAsync(colorValue);
            }
        }

        [Parameter] public EventCallback<double> AlphaChanged { get; set; }

        [Parameter] public bool ShowPreview { get; set; }

        [Parameter] public EventCallback<string> OnChange { get; set; }

        protected override string RootElementClass => "bit-clr-pkr";

        protected override void OnInitialized()
        {
            SetSaturationPickerBackground();

            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                onWindowMouseUpAbortControllerId = await JSRuntime.RegisterOnWindowMouseUpEvent(this, "OnWindowMouseUp");
                onWindowMouseMoveAbortControllerId = await JSRuntime.RegisterOnWindowMouseMoveEvent(this, "OnWindowMouseMove");

                var saturationPickerRect = await JSRuntime.GetBoundingClientRect(SaturationPickerRef);

                saturationPickerThumbPosition = new BitColorPosition
                {
                    Left = Convert.ToInt32(saturationPickerRect?.Width ?? 0),
                    Top = 0
                };
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private void SetSaturationPickerBackground()
        {
            var bitColor = new BitColor(hue, 1, 1, 1);
            saturationPickerBackgroundRgbaCss = GetRgbaCss(bitColor);
            saturationPickerBackgroundRgbCss = GetRgbCss(bitColor);
        }

        private async Task PickColorTune(MouseEventArgs e)
        {
            var parent = await JSRuntime.GetBoundingClientRect(SaturationPickerRef);

            saturationPickerThumbPosition = new BitColorPosition
            {
                Left = e.ClientX < parent.Left ? 0 : e.ClientX > parent.Left + parent.Width ? Convert.ToInt32(parent.Width) : Convert.ToInt32(e.ClientX - parent.Left),
                Top = e.ClientY < parent.Top ? 0 : e.ClientY > parent.Top + parent.Height ? Convert.ToInt32(parent.Height) : Convert.ToInt32(e.ClientY - parent.Top)
            };

            selectedSaturation = Math.Clamp(ToValidSpanValue(0, parent.Width, 0, 1, Convert.ToInt32(e.ClientX - parent.Left)), 0, 1);
            selectedValue = Math.Clamp(ToValidSpanValue(0, parent.Height, 0, 1, parent.Height - Convert.ToInt32(e.ClientY - parent.Top)), 0, 1);

            color = new BitColor(hue, selectedSaturation, selectedValue, color.Alpha);

            SetSaturationPickerBackground();

            await ColorChanged.InvokeAsync(colorType == BitColorType.Hex ? color.Hex : GetRgbaCss(color));
            await AlphaChanged.InvokeAsync(color.Alpha);

            StateHasChanged();
        }

        private async Task PickMainColor(ChangeEventArgs args)
        {
            hue = Convert.ToInt32(args.Value, CultureInfo.InvariantCulture);

            color = new BitColor(hue, selectedSaturation, selectedValue, color.Alpha);

            SetSaturationPickerBackground();

            await ColorChanged.InvokeAsync(colorType == BitColorType.Hex ? color.Hex : GetRgbaCss(color));
            await AlphaChanged.InvokeAsync(color.Alpha);
        }

        private async Task PickAlphaColor(ChangeEventArgs args)
        {
            color.Alpha = Convert.ToDouble(args.Value, CultureInfo.InvariantCulture) / 100;

            await ColorChanged.InvokeAsync(colorType == BitColorType.Hex ? color.Hex : GetRgbaCss(color));
            await AlphaChanged.InvokeAsync(color.Alpha);
        }

        private async Task RGBColorChanged(string? red = null, string? green = null, string? blue = null)
        {
            color.SetColorByRgba(GetColorValue(red), GetColorValue(green), GetColorValue(blue));

            var saturationPickerRect = await JSRuntime.GetBoundingClientRect(SaturationPickerRef);

            var hsv = color.Hsv;
            hue = hsv.Hue;
            SetSaturationPickerBackground();

            saturationPickerThumbPosition = new BitColorPosition
            {
                Left = Convert.ToInt32(Math.Round(saturationPickerRect.Width * hsv.Saturation / 100)),
                Top = Convert.ToInt32(Math.Round(saturationPickerRect.Height - saturationPickerRect.Height * hsv.Value / 100))
            };

            await ColorChanged.InvokeAsync(colorType == BitColorType.Hex ? color.Hex : GetRgbaCss(color));
            await AlphaChanged.InvokeAsync(color.Alpha);
            StateHasChanged();
        }

        private static int? GetColorValue(string? color)
        {
            if (color.HasValue(false) && int.TryParse(color, out int colorCode))
            {
                return colorCode;
            }

            return null;
        }

        private static double ToValidSpanValue(double min, double max, double newMin, double newMax, double value)
        {
            return (value - min) * (newMax - newMin) / (max - min);
        }

        private async Task OnSaturationPickerMouseDown(MouseEventArgs e)
        {
            saturationPickerMouseDown = true;
            await PickColorTune(e);
        }

        private async Task OnSaturationPickerMouseMove(MouseEventArgs e)
        {
            if (saturationPickerMouseDown is false) return;

            await PickColorTune(e);
        }

        private static string GetRgbCss(BitColor bitColor)
        {
            return $"rgb({bitColor.Red},{bitColor.Green},{bitColor.Blue})";
        }

        private static string GetRgbaCss(BitColor bitColor)
        {
            return $"rgba({bitColor.Red},{bitColor.Green},{bitColor.Blue},{bitColor.Alpha})";
        }

        [JSInvokable]
        public void OnWindowMouseUp(MouseEventArgs e)
        {
            saturationPickerMouseDown = false;
        }

        [JSInvokable]
        public async Task OnWindowMouseMove(MouseEventArgs e)
        {
            await OnSaturationPickerMouseMove(e);
        }

        public async ValueTask DisposeAsync()
        {
            if (onWindowMouseUpAbortControllerId.HasValue())
            {
                await JSRuntime.AbortProcedure(onWindowMouseUpAbortControllerId!);
            }

            if (onWindowMouseMoveAbortControllerId.HasValue())
            {
                await JSRuntime.AbortProcedure(onWindowMouseMoveAbortControllerId!);
            }

            GC.SuppressFinalize(this);
        }
    }
}
