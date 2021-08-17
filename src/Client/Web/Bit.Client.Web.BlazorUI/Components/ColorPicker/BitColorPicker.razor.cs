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

        private BitColor mainColor = new BitColor("rgb(255,255,255)");

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Parameter]
        public AlphaType AlphaType { get; set; }

        [Parameter]
        public string Color
        {
            get => MainColor.Color.ToCss();
            set
            {
                var valueAsBitColor = new BitColor(value);
                if (valueAsBitColor.Color.ToCss() == MainColor.Color.ToCss()) return;
                MainColor = valueAsBitColor;
                Hue = MainColor.Color.toHSV().Hue;
                SaturationPickerBackground = new HSV { Hue = Hue, Value = 1, Saturation = 1 };

                ColorChanged.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<string> ColorChanged { get; set; }

        [Parameter]
        public bool ShowPreview { get; set; }
        public bool ColorHasBeenSet { get; set; }
        public bool SaturationPickerMouseDown { get; set; }

        public ElementReference SaturationPickerRef { get; set; }

        public Position? SaturationPickerThumbPosition { get; set; }


        public double SelectedSaturation { get; set; } = 1;
        public double SelectedValue { get; set; } = 1;
        public double Hue { get; set; }

        public HSV SaturationPickerBackground { get; set; } = new HSV() { Hue = 0, Saturation = 1, Value = 1 };

        public BitColor MainColor
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

            MainColor.SetColorByRGB(new HSV() { Hue = Hue, Saturation = SelectedSaturation, Value = SelectedValue }.ToRGB(MainColor.Color.Alpha));
            SaturationPickerBackground = new HSV { Hue = Hue, Value = 1, Saturation = 1 };

            await ColorChanged.InvokeAsync(MainColor.Color.ToCss());

            StateHasChanged();
        }

        private async Task PickMainColor(ChangeEventArgs args)
        {
            Hue = Convert.ToInt32(args.Value);
            MainColor.SetColorByRGB(new HSV() { Hue = Hue, Saturation = SelectedSaturation, Value = SelectedValue }.ToRGB(MainColor.Color.Alpha));
            SaturationPickerBackground = new HSV() { Hue = Hue, Saturation = 1, Value = 1 };

            await ColorChanged.InvokeAsync(MainColor.Color.ToCss());
        }

        private async Task PickAlphaColor(ChangeEventArgs args)
        {
            MainColor.Color.Alpha = Convert.ToDouble(args.Value) / 100;
            await ColorChanged.InvokeAsync(MainColor.Color.ToCss());
        }

        private async Task RGBColorChanged(string? red = null, string? green = null, string? blue = null)
        {
            MainColor.Color.Red = string.IsNullOrWhiteSpace(red) ? MainColor.Color.Red : Convert.ToDouble(red);
            MainColor.Color.Green = string.IsNullOrWhiteSpace(green) ? MainColor.Color.Green : Convert.ToDouble(green);
            MainColor.Color.Blue = string.IsNullOrWhiteSpace(blue) ? MainColor.Color.Blue : Convert.ToDouble(blue);

            var saturationPickerRect = await JSRuntime.GetBoundingClientRect(SaturationPickerRef);

            var hsv = MainColor.Color.toHSV();
            Hue = hsv.Hue;
            SaturationPickerBackground = new HSV() { Hue = Hue, Saturation = 1, Value = 1 };

            SaturationPickerThumbPosition = new Position
            {
                Left = Convert.ToInt32(Math.Round(saturationPickerRect.Width * hsv.Saturation / 100)),
                Top = Convert.ToInt32(Math.Round(saturationPickerRect.Height - saturationPickerRect.Height * hsv.Value / 100))
            };


            await ColorChanged.InvokeAsync(MainColor.Color.ToCss());

            StateHasChanged();
        }

        private async Task HexCHanged(ChangeEventArgs args)
        {
            var color = args.Value.ToString();

            if (color.Length is 6 && Regex.IsMatch(color, @"\A\b[0-9a-fA-F]+\b\Z"))
            {
                var hex = new Hex() { ColorCode = color };

                var saturationPickerRect = await JSRuntime.GetBoundingClientRect(SaturationPickerRef);

                MainColor = new BitColor(hex.ToRGB().ToCss());
                var hsv = MainColor.Color.toHSV();
                Hue = hsv.Hue;
                SaturationPickerBackground = new HSV() { Hue = Hue, Saturation = 1, Value = 1 };

                SaturationPickerThumbPosition = new Position
                {
                    Left = Convert.ToInt32(saturationPickerRect.Width * hsv.Saturation / 100),
                    Top = Convert.ToInt32(saturationPickerRect.Height - saturationPickerRect.Height * hsv.Value / 100)
                };

                StateHasChanged();
            }
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
