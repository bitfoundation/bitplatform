using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitColorPicker : IAsyncDisposable
    {
        private string? onWindowMouseUpAbortControllerId;
        private string? onWindowMouseMoveAbortControllerId;

        private BitColor mainColor = new BitColor();

        private ColorType ColorType { get; set; }

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Parameter]
        public AlphaType AlphaType { get; set; }

        [Parameter]
        public string Color
        {
            get => ColorType == ColorType.Hex ? MainColor.Hex.ColorCode : MainColor.ToRgbaCss();
            set
            {
                var valueAsBitColor = new BitColor(value, MainColor.Alpha);
                if (valueAsBitColor.ToRgbaCss() == MainColor.ToRgbaCss()) return;
                MainColor = valueAsBitColor;
                Hue = MainColor.Rgb.ToHsv().Hue;
                SaturationPickerBackground = new Hsv { Hue = Hue, Value = 1, Saturation = 1 };

                ColorType = value.StartsWith("#") ? ColorType.Hex : ColorType.Rgb;

                ColorChanged.InvokeAsync(value);
                HexChanged.InvokeAsync(MainColor.Hex.ColorCode);
                AlphaChanged.InvokeAsync(MainColor.Alpha);

            }
        }

        [Parameter]
        public EventCallback<string> ColorChanged { get; set; }

        [Parameter]
        public string Hex
        {
            get => MainColor.Hex.ColorCode;
            set
            {
                var valueAsBitColor = new BitColor(value, MainColor.Alpha);
                if (valueAsBitColor.Hex.ColorCode == MainColor.Hex.ColorCode) return;
                MainColor = valueAsBitColor;
                Hue = MainColor.Rgb.ToHsv().Hue;
                SaturationPickerBackground = new Hsv { Hue = Hue, Value = 1, Saturation = 1 };

                HexChanged.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<string> HexChanged { get; set; }

        [Parameter]
        public string Rgb
        {
            get => MainColor.Hex.ColorCode;
            set
            {
                var valueAsBitColor = new BitColor(value, MainColor.Alpha);
                if (valueAsBitColor.Hex.ColorCode == MainColor.Hex.ColorCode) return;
                MainColor = valueAsBitColor;
                Hue = MainColor.Rgb.ToHsv().Hue;
                SaturationPickerBackground = new Hsv { Hue = Hue, Value = 1, Saturation = 1 };

                RgbChanged.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<string> RgbChanged { get; set; }

        [Parameter]
        public double Alpha
        {
            get => MainColor.Alpha;
            set
            {
                if (MainColor.Alpha == value) return;
                MainColor.Alpha = value;

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

        private Hsv SaturationPickerBackground { get; set; } = new Hsv() { Hue = 0, Saturation = 1, Value = 1 };

        private BitColor MainColor
        {
            get => mainColor; set
            {
                mainColor = value;
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
                    Left = Convert.ToInt32(saturationPickerRect.Width),
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

            var newColor = new Hsv() { Hue = Hue, Saturation = SelectedSaturation, Value = SelectedValue }.ToRgb();

            MainColor.SetColorByRgb(newColor);
            SaturationPickerBackground = new Hsv { Hue = Hue, Value = 1, Saturation = 1 };

            await ColorChanged.InvokeAsync(ColorType == ColorType.Hex ? MainColor.Hex.ColorCode : MainColor.ToRgbaCss());
            await RgbChanged.InvokeAsync(MainColor.Rgb.ToCss());
            await HexChanged.InvokeAsync(MainColor.Hex.ColorCode);
            await AlphaChanged.InvokeAsync(MainColor.Alpha);

            StateHasChanged();
        }

        private async Task PickMainColor(ChangeEventArgs args)
        {
            Hue = Convert.ToInt32(args.Value);

            var newColor = new Hsv() { Hue = Hue, Saturation = SelectedSaturation, Value = SelectedValue }.ToRgb();

            MainColor.SetColorByRgb(newColor);
            SaturationPickerBackground = new Hsv() { Hue = Hue, Saturation = 1, Value = 1 };

            await ColorChanged.InvokeAsync(ColorType == ColorType.Hex ? MainColor.Hex.ColorCode : MainColor.ToRgbaCss());
            await RgbChanged.InvokeAsync(MainColor.Rgb.ToCss());
            await HexChanged.InvokeAsync(MainColor.Hex.ColorCode);
            await AlphaChanged.InvokeAsync(MainColor.Alpha);
        }

        private async Task PickAlphaColor(ChangeEventArgs args)
        {
            MainColor.Alpha = Convert.ToDouble(args.Value) / 100;

            await ColorChanged.InvokeAsync(ColorType == ColorType.Hex ? MainColor.Hex.ColorCode : MainColor.ToRgbaCss());
            await RgbChanged.InvokeAsync(MainColor.Rgb.ToCss());
            await HexChanged.InvokeAsync(MainColor.Hex.ColorCode);
            await AlphaChanged.InvokeAsync(MainColor.Alpha);
        }

        private async Task RGBColorChanged(string? red = null, string? green = null, string? blue = null)
        {
            MainColor.Rgb.Red = string.IsNullOrWhiteSpace(red) ? MainColor.Rgb.Red : Convert.ToDouble(red);
            MainColor.Rgb.Green = string.IsNullOrWhiteSpace(green) ? MainColor.Rgb.Green : Convert.ToDouble(green);
            MainColor.Rgb.Blue = string.IsNullOrWhiteSpace(blue) ? MainColor.Rgb.Blue : Convert.ToDouble(blue);

            var saturationPickerRect = await JSRuntime.GetBoundingClientRect(SaturationPickerRef);

            var hsv = MainColor.Rgb.ToHsv();
            Hue = hsv.Hue;
            SaturationPickerBackground = new Hsv() { Hue = Hue, Saturation = 1, Value = 1 };

            SaturationPickerThumbPosition = new Position
            {
                Left = Convert.ToInt32(Math.Round(saturationPickerRect.Width * hsv.Saturation / 100)),
                Top = Convert.ToInt32(Math.Round(saturationPickerRect.Height - saturationPickerRect.Height * hsv.Value / 100))
            };


            await ColorChanged.InvokeAsync(ColorType == ColorType.Hex ? MainColor.Hex.ColorCode : MainColor.ToRgbaCss());
            await RgbChanged.InvokeAsync(MainColor.Rgb.ToCss());
            await HexChanged.InvokeAsync(MainColor.Hex.ColorCode);
            await AlphaChanged.InvokeAsync(MainColor.Alpha);
            StateHasChanged();
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
