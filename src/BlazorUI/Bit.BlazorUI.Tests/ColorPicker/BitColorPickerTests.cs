﻿using Bunit;
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
    public void BitColorPickerMustRespecUiChange(bool visibility)
    {
        var cut = RenderComponent<BitColorPickerTest>(parameters =>
        {
            parameters.Add(p => p.ShowAlphaSlider, visibility);
            parameters.Add(p => p.ShowPreview, visibility);
            parameters.Add(p => p.Color, "rgb(255,255,255)");
        });

        if (visibility is false)
        {
            Assert.ThrowsException<ElementNotFoundException>(() => cut.Find(".alpha-slider"));
            Assert.ThrowsException<ElementNotFoundException>(() => cut.Find(".preview-box"));
        }
    }

    [DataTestMethod,
        DataRow("#fc5603", "#fc5603", "rgb(252,86,3)", 1),
        DataRow("rgba(3,98,252,0.3)", "#0362fc", "rgb(3,98,252)", 0.3),
        DataRow("rgb(252,3,240)", "#fc03f0", "rgb(252,3,240)", 1)
    ]
    public void BitColorPickerMustRespecValueChange(string color, string hex, string rgb, double alpha)
    {
        var cut = RenderComponent<BitColorPickerTest>(parameters =>
        {
            parameters.Add(p => p.Color, color);
            parameters.Add(p => p.Alpha, alpha);
        });

        Assert.AreEqual(color, cut.Instance.Color);
        Assert.AreEqual(hex, cut.Instance.ElementReference.Hex);
        Assert.AreEqual(rgb, cut.Instance.ElementReference.Rgb);
        Assert.AreEqual(alpha, cut.Instance.Alpha);
    }
}
