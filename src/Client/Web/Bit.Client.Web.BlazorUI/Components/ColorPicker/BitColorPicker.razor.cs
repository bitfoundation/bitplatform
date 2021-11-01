using System;
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

        private BitColor bitColor = new BitColor();

        private BitColorType ColorType { get; set; }

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

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
            get => ColorType == BitColorType.Hex ? BitColor.Hex : BitColor.ToRgbaCss();
            set
            {
                var valueAsBitColor = new BitColor(value, BitColor.Alpha);
                if (valueAsBitColor.ToRgbaCss() == BitColor.ToRgbaCss()) return;
                BitColor = valueAsBitColor;
                Hue = BitColor.Hsv.Hue;
                SaturationPickerBackground = new BitColor(Hue, 1, 1,1);

                ColorType = value.StartsWith("#", StringComparison.CurrentCultureIgnoreCase) ? BitColorType.Hex : BitColorType.Rgb;

                ColorChanged.InvokeAsync(value);
                HexChanged.InvokeAsync(BitColor.Hex);
                RgbChanged.InvokeAsync(BitColor.ToRgbCss());
                AlphaChanged.InvokeAsync(BitColor.Alpha);

            }
        }
        
        [Parameter]
        public EventCallback<string> ColorChanged { get; set; }

        /// <summary>
        /// Indicates the Hex value.
        /// </summary>
        [Parameter]
        public string Hex
        {
            get => BitColor.Hex;
            set
            {
                var valueAsBitColor = new BitColor(value, BitColor.Alpha);
                if (valueAsBitColor.Hex == BitColor.Hex) return;
                BitColor = valueAsBitColor;
                Hue = BitColor.Hsv.Hue;
                SaturationPickerBackground = new BitColor(Hue, 1, 1,1);

                ColorChanged.InvokeAsync(ColorType == BitColorType.Hex ? BitColor.Hex : BitColor.ToRgbaCss());
                RgbChanged.InvokeAsync(BitColor.ToRgbCss());
                HexChanged.InvokeAsync(BitColor.Hex);
                AlphaChanged.InvokeAsync(BitColor.Alpha);
            }
        }

        [Parameter]
        public EventCallback<string> HexChanged { get; set; }

        [Parameter]
        public string Rgb
        {
            get => BitColor.Hex;
            set
            {
                var valueAsBitColor = new BitColor(value, BitColor.Alpha);
                if (valueAsBitColor.Hex == BitColor.Hex) return;
                BitColor = valueAsBitColor;
                Hue = BitColor.Hsv.Hue;
                SaturationPickerBackground = new BitColor(Hue, 1, 1,1);

                ColorChanged.InvokeAsync(ColorType == BitColorType.Hex ? BitColor.Hex : BitColor.ToRgbaCss());
                RgbChanged.InvokeAsync(BitColor.ToRgbCss());
                HexChanged.InvokeAsync(BitColor.Hex);
                AlphaChanged.InvokeAsync(BitColor.Alpha);
            }
        }

        [Parameter]
        public EventCallback<string> RgbChanged { get; set; }

        [Parameter]
        public double Alpha
        {
            get => BitColor.Alpha;
            set
            {
                if (BitColor.Alpha == value) return;
                BitColor.Alpha = value;

                AlphaChanged.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<double> AlphaChanged { get; set; }

        [Parameter]
        public bool ShowPreview { get; set; }

        private bool ColorHasBeenSet { get; set; }
        private bool HexHasBeenSet { get; set; }
        private bool AlphaHasBeenSet { get; set; }
        private bool RgbHasBeenSet { get; set; }

        private bool SaturationPickerMouseDown { get; set; }

        public ElementReference SaturationPickerRef { get; set; }

        private Position? SaturationPickerThumbPosition { get; set; }

        private double SelectedSaturation { get; set; } = 1;
        private double SelectedValue { get; set; } = 1;
        private double Hue { get; set; }

        private BitColor SaturationPickerBackground { get; set; } = new BitColor(0,1,1,1);

        private BitColor BitColor
        {
            get => bitColor; set
            {
                bitColor = value;
            }
        }

        protected override string RootElementClass => "bit-clr-pkr";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                onWindowMouseUpAbortControllerId = await JSRuntime.RegisterOnWindowMouseUpEvent(this, "OnWindowMouseUp");
                onWindowMouseMoveAbortControllerId = await JSRuntime.RegisterOnWindowMouseMoveEvent(this, "OnWindowMouseMove");

                var saturationPickerRect = await JSRuntime.GetBoundingClientRect(SaturationPickerRef);

                SaturationPickerThumbPosition = new Position
                {
                    Left = Convert.ToInt32(saturationPickerRect?.Width),
                    Top = 0
                };
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task PickColorTune(MouseEventArgs e)
        {
            var parent = await JSRuntime.GetBoundingClientRect(SaturationPickerRef);

            SaturationPickerThumbPosition = new Position
            {
                Left = e.ClientX < parent.Left ? 0 : e.ClientX > parent.Left + parent.Width ? Convert.ToInt32(parent.Width) : Convert.ToInt32(e.ClientX - parent.Left),
                Top = e.ClientY < parent.Top ? 0 : e.ClientY > parent.Top + parent.Height ? Convert.ToInt32(parent.Height) : Convert.ToInt32(e.ClientY - parent.Top)
            };

            SelectedSaturation = Math.Clamp(ToValidSpanValue(0, parent.Width, 0, 1, Convert.ToInt32(e.ClientX - parent.Left)), 0, 1);
            SelectedValue = Math.Clamp(ToValidSpanValue(0, parent.Height, 0, 1, parent.Height - Convert.ToInt32(e.ClientY - parent.Top)), 0, 1);

            BitColor = new BitColor(Hue, SelectedSaturation, SelectedValue, BitColor.Alpha);

            SaturationPickerBackground = new BitColor(Hue, 1, 1, 1);

            await ColorChanged.InvokeAsync(ColorType == BitColorType.Hex ? BitColor.Hex : BitColor.ToRgbaCss());
            await RgbChanged.InvokeAsync(BitColor.ToRgbCss());
            await HexChanged.InvokeAsync(BitColor.Hex);
            await AlphaChanged.InvokeAsync(BitColor.Alpha);

            SaturationPickerBackground = new BitColor(Hue, 1, 1, 1);

            await ColorChanged.InvokeAsync(ColorType == BitColorType.Hex ? BitColor.Hex : BitColor.ToRgbaCss());
            await RgbChanged.InvokeAsync(BitColor.ToRgbCss());
            await HexChanged.InvokeAsync(BitColor.Hex);
            await AlphaChanged.InvokeAsync(BitColor.Alpha);

            StateHasChanged();
        }

        private async Task PickMainColor(ChangeEventArgs args)
        {
            Hue = Convert.ToInt32(args.Value);

            BitColor = new BitColor(Hue, SelectedSaturation, SelectedValue, BitColor.Alpha);

            SaturationPickerBackground = new BitColor(Hue, 1, 1,1);

            await ColorChanged.InvokeAsync(ColorType == BitColorType.Hex ? BitColor.Hex : BitColor.ToRgbaCss());
            await RgbChanged.InvokeAsync(BitColor.ToRgbCss());
            await HexChanged.InvokeAsync(BitColor.Hex);
            await AlphaChanged.InvokeAsync(BitColor.Alpha);
        }

        private async Task PickAlphaColor(ChangeEventArgs args)
        {
            BitColor.Alpha = Convert.ToDouble(args.Value) / 100;

            await ColorChanged.InvokeAsync(ColorType == BitColorType.Hex ? BitColor.Hex : BitColor.ToRgbaCss());
            await RgbChanged.InvokeAsync(BitColor.ToRgbCss());
            await HexChanged.InvokeAsync(BitColor.Hex);
            await AlphaChanged.InvokeAsync(BitColor.Alpha);
        }

        private async Task RGBColorChanged(string? red = null, string? green = null, string? blue = null)
        {
            BitColor.SetColorByRgba(GetColorValue(red), GetColorValue(green), GetColorValue(blue));

            var saturationPickerRect = await JSRuntime.GetBoundingClientRect(SaturationPickerRef);

            var hsv = BitColor.Hsv;
            Hue = hsv.Hue;
            SaturationPickerBackground = new BitColor(Hue, 1, 1,1 );

            SaturationPickerThumbPosition = new Position
            {
                Left = Convert.ToInt32(Math.Round(saturationPickerRect.Width * hsv.Saturation / 100)),
                Top = Convert.ToInt32(Math.Round(saturationPickerRect.Height - saturationPickerRect.Height * hsv.Value / 100))
            };


            await ColorChanged.InvokeAsync(ColorType == BitColorType.Hex ? BitColor.Hex : BitColor.ToRgbaCss());
            await RgbChanged.InvokeAsync(BitColor.ToRgbCss());
            await HexChanged.InvokeAsync(BitColor.Hex);
            await AlphaChanged.InvokeAsync(BitColor.Alpha);
            StateHasChanged();
        }

        private int? GetColorValue(string? color)
        {
            if (!string.IsNullOrEmpty(color) && int.TryParse(color, out int colorCode))
            {
                return colorCode;

            }

            return null;
        }

        private double ToValidSpanValue(double min, double max, double newMin, double newMax, double value)
        {
            return (value - min) * (newMax - newMin) / (max - min);
        }

        private async Task OnSaturationPickerMouseDown(MouseEventArgs e)
        {
            SaturationPickerMouseDown = true;
            await PickColorTune(e);
        }

        private async Task OnSaturationPickerMouseMove(MouseEventArgs e)
        {
            if (SaturationPickerMouseDown is false) return;

            await PickColorTune(e);
        }

        [JSInvokable]
        public async Task OnWindowMouseUp(MouseEventArgs e)
        {
            SaturationPickerMouseDown = false;
        }

        [JSInvokable]
        public async Task OnWindowMouseMove(MouseEventArgs e)
        {
            await OnSaturationPickerMouseMove(e);
        }

        public async ValueTask DisposeAsync()
        {
            await JSRuntime.AbortProcedure(onWindowMouseUpAbortControllerId);
            await JSRuntime.AbortProcedure(onWindowMouseMoveAbortControllerId);
        }
    }

    public class Position
    {
        public int Top { get; set; }
        public int Left { get; set; }
    }
}
