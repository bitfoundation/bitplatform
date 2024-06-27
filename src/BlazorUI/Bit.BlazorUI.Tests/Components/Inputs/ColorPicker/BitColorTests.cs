using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ColorPicker;

[TestClass]
public class BitColorTests
{
    [DataTestMethod, DataRow("#5D0914")]
    public void BitColorHexToRgbTest(string color)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual(93, bitColor.R);
        Assert.AreEqual(9, bitColor.G);
        Assert.AreEqual(20, bitColor.B);
        Assert.AreEqual(1, bitColor.A);
    }

    [DataTestMethod, DataRow("#5D0914", 0.6)]
    public void BitColorHexToRgbaTest(string color, double alpha)
    {
        var bitColor = new BitInternalColor(color, alpha);

        Assert.AreEqual(93, bitColor.R);
        Assert.AreEqual(9, bitColor.G);
        Assert.AreEqual(20, bitColor.B);
        Assert.AreEqual(bitColor.A, alpha);
    }

    [DataTestMethod, DataRow("rgb(93,9,20)")]
    public void BitColorRgbToHexTest(string color)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual("#5D0914", bitColor.Hex);
        Assert.AreEqual(1, bitColor.A);
    }

    [DataTestMethod, DataRow("rgb(93,9,20)", 0.6)]
    public void BitColorRgbaToHexTest(string color, double alpha)
    {
        var bitColor = new BitInternalColor(color, alpha);

        Assert.AreEqual("#5D0914", bitColor.Hex);
        Assert.AreEqual(alpha, bitColor.A);
    }

    [DataTestMethod, DataRow((byte)93, (byte)9, (byte)20, 0.6)]
    public void BitColorSetRgbaTest(byte red, byte green, byte blue, double alpha)
    {
        var bitColor = new BitInternalColor(red, green, blue, alpha);

        Assert.AreEqual("#5D0914", bitColor.Hex);
        Assert.AreEqual(alpha, bitColor.A);
    }

    [DataTestMethod, DataRow("#5d0914")]
    public void BitColorHexToHsvTest(string color)
    {
        var bitColor = new BitInternalColor(color);

        Assert.AreEqual(352.14, Math.Round(bitColor.Hsv.Hue, 2));
        Assert.AreEqual(0.90, Math.Round(bitColor.Hsv.Saturation, 2));
        Assert.AreEqual(0.36, Math.Round(bitColor.Hsv.Value, 2));
        Assert.AreEqual(1, bitColor.A);
    }

    [DataTestMethod, DataRow(352.143, 0.903, 0.365, 0.9)]
    public void BitColorHsvToHexTest(double hue, double saturation, double value, double alpha)
    {
        var bitColor = new BitInternalColor(hue, saturation, value, alpha);

        Assert.AreEqual("#5D0914", bitColor.Hex);
    }
}
