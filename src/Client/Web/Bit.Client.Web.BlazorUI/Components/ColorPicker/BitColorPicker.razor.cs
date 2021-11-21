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

        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// Whether to show a slider for editing alpha value.
        /// </summary>
        [Parameter] public bool ShowAlphaSlider { get; set; } = true;

        /// <summary>
        /// CSS-compatible string to describe the color.
        /// </summary>
        [Parameter]
        public string Color
        {
            get => colorType == BitColorType.Hex ? color.Hex! : color.Rgb!;
            set
            {
                colorType = value.HasValue() && value.StartsWith("#", StringComparison.InvariantCultureIgnoreCase) ? BitColorType.Hex : BitColorType.Rgb;

                var valueAsBitColor = new BitColor(value, Alpha);
                if (valueAsBitColor == color) return;
                color = valueAsBitColor;
                hue = color.Hsv.Hue;
                SetSaturationPickerBackground();

                ColorChanged.InvokeAsync(value);
            }
        }

        [Parameter] public EventCallback<string> ColorChanged { get; set; }

        /// <summary>
        /// Indicates the Alpha value.
        /// </summary>
        [Parameter]
        public double Alpha
        {
            get => color.Alpha;
            set
            {
                if (color.Alpha == value) return;
                color.Alpha = value;

                AlphaChanged.InvokeAsync(value);
            }
        }

        [Parameter] public EventCallback<double> AlphaChanged { get; set; }

        /// <summary>
        /// Whether to show color preview box.
        /// </summary>
        [Parameter] public bool ShowPreview { get; set; }

        [Parameter] public EventCallback<BitColorEventArgs> OnChange { get; set; }

        public ElementReference SaturationPickerRef { get; set; }
        public string? Hex => color.Hex;
        public string? Rgb => color.Rgb;
        public string ColorRectangleDescriptionId { get; set; } = string.Empty;

        protected override string RootElementClass => "bit-clr-pkr";

        protected override void OnInitialized()
        {
            ColorRectangleDescriptionId = $"ColorRectangle-description{UniqueId}";
            SetSaturationPickerBackground();

            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                onWindowMouseUpAbortControllerId = await JSRuntime.RegisterOnWindowMouseUpEvent(this, "OnWindowMouseUp");
                onWindowMouseMoveAbortControllerId = await JSRuntime.RegisterOnWindowMouseMoveEvent(this, "OnWindowMouseMove");

                await SetPositionAsync();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task SetPositionAsync()
        {
            var hsv = color.Hsv;
            var saturationPickerRect = await JSRuntime.GetBoundingClientRect(SaturationPickerRef);

            var width = saturationPickerRect?.Width ?? 0;
            var height = saturationPickerRect?.Height ?? 0;

            saturationPickerThumbPosition = new BitColorPosition
            {
                Left = Convert.ToInt32(width * hsv.Saturation / 100),
                Top = Convert.ToInt32(height - (height * hsv.Value / 100))
            };

            StateHasChanged();
        }

        private void SetSaturationPickerBackground()
        {
            var bitColor = new BitColor(hue, 1, 1, 1);
            saturationPickerBackgroundRgbaCss = bitColor.Rgba;
            saturationPickerBackgroundRgbCss = bitColor.Rgb;
        }

        private async Task PickColorTune(MouseEventArgs e)
        {
            if (ColorHasBeenSet && ColorChanged.HasDelegate is false) return;

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
            string? colorValue = colorType == BitColorType.Hex ? color.Hex : color.Rgb;
            await ColorChanged.InvokeAsync(colorValue);
            await AlphaChanged.InvokeAsync(color.Alpha);
            await OnChange.InvokeAsync(new() { Color = colorValue, Alpha = color.Alpha });
            StateHasChanged();
        }

        private async Task PickMainColor(ChangeEventArgs args)
        {
            if (ColorHasBeenSet && ColorChanged.HasDelegate is false) return;

            hue = Convert.ToInt32(args.Value, CultureInfo.InvariantCulture);
            color = new BitColor(hue, selectedSaturation, selectedValue, color.Alpha);
            SetSaturationPickerBackground();
            string? colorValue = colorType == BitColorType.Hex ? color.Hex : color.Rgb;
            await ColorChanged.InvokeAsync(colorValue);
            await AlphaChanged.InvokeAsync(color.Alpha);
            await OnChange.InvokeAsync(new() { Color = colorValue, Alpha = color.Alpha });
        }

        private async Task PickAlphaColor(ChangeEventArgs args)
        {
            if (AlphaHasBeenSet && AlphaChanged.HasDelegate is false) return;

            var alpha = Convert.ToDouble(args.Value, CultureInfo.InvariantCulture) / 100;
            color = new BitColor(color.Hex ?? "", alpha);
            string? colorValue = colorType == BitColorType.Hex ? color.Hex : color.Rgb;
            await ColorChanged.InvokeAsync(colorValue);
            await AlphaChanged.InvokeAsync(color.Alpha);
            await OnChange.InvokeAsync(new() { Color = colorValue, Alpha = color.Alpha });
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

        private string GetRootElAriaLabel()
        {
            var ariaLabel = $"Color picker, Red {color.Red} Green {color.Green} Blue {color.Blue} ";
            if (ShowAlphaSlider)
            {
                ariaLabel += $"Alpha {color.Alpha * 100}% selected.";
            }
            else
            {
                ariaLabel += "selected.";
            }

            return ariaLabel;
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
