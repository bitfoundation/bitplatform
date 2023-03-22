using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Bit.BlazorUI.Tests.ColorPicker;

[TestClass]
public class BitColorPickerTests : BunitTestContext
{
    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitColorPickerMustRespectUiChange(bool visibility)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowAlphaSlider, visibility);
            parameters.Add(p => p.ShowPreview, visibility);
            parameters.Add(p => p.Color, "rgb(255,255,255)");
        });

        if (visibility)
        {
            var slider = com.Find(".alpha-slider");
            Assert.IsNotNull(slider);

            var previewBox = com.Find(".preview-box");
            Assert.IsNotNull(previewBox);
        }
        else
        {
            Assert.ThrowsException<ElementNotFoundException>(() => com.Find(".alpha-slider"));
            Assert.ThrowsException<ElementNotFoundException>(() => com.Find(".preview-box"));
        }
    }

    [DataTestMethod,
        DataRow("#fc5603", "#fc5603", "rgb(252,86,3)", 1),
        DataRow("rgba(3,98,252,0.3)", "#0362fc", "rgb(3,98,252)", 0.3),
        DataRow("rgb(252,3,240)", "#fc03f0", "rgb(252,3,240)", 1)
    ]
    public void BitColorPickerMustRespectValueChange(string color, string hex, string rgb, double alpha)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Color, color);
            parameters.Add(p => p.Alpha, alpha);
        });

        var expectedColor = color.StartsWith("#") ? hex : rgb;

        Assert.AreEqual(expectedColor, com.Instance.Color);
        Assert.AreEqual(hex, com.Instance.Hex);
        Assert.AreEqual(rgb, com.Instance.Rgb);
        Assert.AreEqual(alpha, com.Instance.Alpha);
    }
}
