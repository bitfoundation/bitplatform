using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;


namespace Bit.BlazorUI.Tests.Components.Inputs.ColorPicker;

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
    public void BitColorPickerMustRespectUiChange(bool show)
    {
        var com = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.ShowPreview, show);
            parameters.Add(p => p.ShowAlphaSlider, show);
            parameters.Add(p => p.Color, "rgb(255,255,255)");
        });

        if (show)
        {
            var slider = com.Find(".bit-clp-asd");
            Assert.IsNotNull(slider);

            var previewBox = com.Find(".bit-clp-pre");
            Assert.IsNotNull(previewBox);
        }
        else
        {
            Assert.ThrowsException<ElementNotFoundException>(() => com.Find(".bit-clp-asd"));
            Assert.ThrowsException<ElementNotFoundException>(() => com.Find(".bit-clp-pre"));
        }
    }

    [DataTestMethod,
        DataRow("#FC5603", "#FC5603", "rgb(252,86,3)", 1),
        DataRow("rgba(3,98,252,0.3)", "#0362FC", "rgb(3,98,252)", 0.3),
        DataRow("rgb(252,3,240)", "#FC03F0", "rgb(252,3,240)", 1)
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
