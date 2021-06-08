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

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitColorPicker
    {
        protected override string RootElementClass => "bit-clr-pkr";

        [Inject] public IJSRuntime JSRuntime { get; set; }

        public ElementReference SaturationPickerRef { get; set; }
        public Position? SaturationPickerThumbPosition { get; set; }
        public bool SaturationPickerMouseDown { get; set; }

        public ElementReference HuePickerRef { get; set; }
        public Position? HuePickerThumbPosition { get; set; }
        public bool HuePickerMouseDown { get; set; }

        public ElementReference AlphaPickerRef { get; set; }
        public Position? AlphaPickerThumbPosition { get; set; }
        public bool AlphaPickerMouseDown { get; set; }

        public HSV SaturationPickerBackground { get; set; } = new HSV() { Hue = 0, Saturation = 1, Value = 1 };

        public double SelectedSaturation { get; set; } = 1;
        public double SelectedValue { get; set; } = 1;
        public double Hue { get; set; } = 0;
        public double Alpha { get; set; } = 1;

        public RGB Color { get; set; } = new RGB();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
        }

        private async Task PickColorTune(MouseEventArgs e)
        {
            SaturationPickerThumbPosition = new Position { Left = Convert.ToInt32(e.OffsetX), Top = Convert.ToInt32(e.OffsetY) };

            var parent = await JSRuntime.GetBoundingClientRect(SaturationPickerRef);

            SelectedSaturation = ToValidSpanValue(0, parent.Width, 0, 1, e.OffsetX);
            SelectedValue = ToValidSpanValue(0, parent.Height, 0, 1, parent.Height - e.OffsetY);

            Color = new HSV() { Hue = Hue, Saturation = SelectedSaturation, Value = SelectedValue }.ToRGB();
        }

        private async Task PickMainColor(MouseEventArgs e)
        {
            var parent = await JSRuntime.GetBoundingClientRect(HuePickerRef);
            Hue = ToValidSpanValue(0, parent.Width, 0, 360, e.OffsetX);
            Color = new HSV() { Hue = Hue, Saturation = SelectedSaturation, Value = SelectedValue }.ToRGB();

            SaturationPickerBackground = new HSV() { Hue = Hue, Saturation = 1, Value = 1 };
        }

        private async Task PickAlphaColor(MouseEventArgs e)
        {
            var parent = await JSRuntime.GetBoundingClientRect(AlphaPickerRef);
            Alpha = ToValidSpanValue(0, parent.Width, 0, 1, e.OffsetX);
        }

        private void UpdateColor(double? red = null, double? green = null, double? blue = null)
        {
            Color = new RGB()
            {
                Red = red ?? Color.Red,
                Green = green ?? Color.Green,
                Blue = blue ?? Color.Blue
            };

            Hue = Color.toHSV().Hue;
            SaturationPickerBackground = new HSV() { Hue = Hue, Saturation = 1, Value = 1 };
            StateHasChanged();
        }

        private async Task HexCHanged(ChangeEventArgs args)
        {
            var color = args.Value.ToString();

            if (color.StartsWith("#"))
            {
                color = color.Remove(0, 1);
            }

            if (color.Length is 6 && Regex.IsMatch(color, @"\A\b[0-9a-fA-F]+\b\Z"))
            {
                var hex = new Hex() { ColorCode = color };
                Color = hex.ToRGB();
                var hsv = Color.toHSV();
                Hue = hsv.Hue;
                SaturationPickerBackground = new HSV() { Hue = Hue, Saturation = 1, Value = 1 };

                var saturationPickerRect = await JSRuntime.GetBoundingClientRect(SaturationPickerRef);

                SaturationPickerThumbPosition = new Position
                {
                    Left = Convert.ToInt32(saturationPickerRect.Width * hsv.Saturation),
                    Top = Convert.ToInt32(saturationPickerRect.Height * hsv.Value)
                };
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


        private async Task OnHuePickerMouseMove(MouseEventArgs e)
        {

            if (HuePickerMouseDown is false) return;

            await PickMainColor(e);
        }

        private async Task OnHuePickerMouseDown(MouseEventArgs e)
        {

            HuePickerMouseDown = true;
            await PickMainColor(e);
        }


        private async Task OnAlphaPickerMouseMove(MouseEventArgs e)
        {
            if (AlphaPickerMouseDown is false) return;

            await PickAlphaColor(e);
        }
        private async Task OnAlphaPickerMouseDown(MouseEventArgs e)
        {
            AlphaPickerMouseDown = true;
            await PickAlphaColor(e);
        }
    }


    public class Position
    {
        public int Top { get; set; }
        public int Left { get; set; }
    }

}
