using System.Collections.Generic;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Sliders
{
    public partial class BitSliderDemo
    {
        private double? BitSliderHorizontalValue = 2;
        private double? BitSliderRangedLowerValue = 0;
        private double? BitSliderRangedUpperValue = 0;

        private Dictionary<string, object>? BitSliderRangedSliderBoxHtmlAttributes = new()
        {
            { "custom-attribute", "demo" }
        };

        private void ChangeBitSliderRangedValues()
        {
            BitSliderRangedLowerValue = 3;
            BitSliderRangedUpperValue = 7;
        }
    }
}
