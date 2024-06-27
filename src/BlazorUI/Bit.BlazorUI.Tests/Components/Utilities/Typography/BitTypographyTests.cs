using System.Globalization;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Typography;

[TestClass]
public class BitTypographyTests : BunitTestContext
{
    private static readonly Dictionary<BitTypographyVariant, string> VariantMapping = new()
    {
        { BitTypographyVariant.Body1, "p" },
        { BitTypographyVariant.Body2, "p" },
        { BitTypographyVariant.Button, "span" },
        { BitTypographyVariant.Caption, "span" },
        { BitTypographyVariant.H1, "h1" },
        { BitTypographyVariant.H2, "h2" },
        { BitTypographyVariant.H3, "h3" },
        { BitTypographyVariant.H4, "h4" },
        { BitTypographyVariant.H5, "h5" },
        { BitTypographyVariant.H6, "h6" },
        { BitTypographyVariant.Inherit, "p" },
        { BitTypographyVariant.Overline, "span" },
        { BitTypographyVariant.Subtitle1, "h6" },
        { BitTypographyVariant.Subtitle2, "h6" }
    };

    [DataTestMethod,
     DataRow(BitTypographyVariant.Body1),
     DataRow(BitTypographyVariant.Body2),
     DataRow(BitTypographyVariant.Button),
     DataRow(BitTypographyVariant.Caption),
     DataRow(BitTypographyVariant.H1),
     DataRow(BitTypographyVariant.H2),
     DataRow(BitTypographyVariant.H3),
     DataRow(BitTypographyVariant.H4),
     DataRow(BitTypographyVariant.H5),
     DataRow(BitTypographyVariant.H6),
     DataRow(BitTypographyVariant.Inherit),
     DataRow(BitTypographyVariant.Overline),
     DataRow(BitTypographyVariant.Subtitle1),
     DataRow(BitTypographyVariant.Subtitle2),
     DataRow(null)
    ]
    [TestMethod]
    public void BitTypographyVariantTest(BitTypographyVariant? variant)
    {
        var com = RenderComponent<BitTypography>(parameters =>
        {
            if (variant.HasValue)
            {
                parameters.Add(p => p.Variant, variant.Value);
            }
        });

        var expectedVariant = variant ?? BitTypographyVariant.Subtitle1;
        var expectedElement = VariantMapping[expectedVariant];

        var expectedHtml = $"<{expectedElement} diff:ignore></{expectedElement}>";

        com.MarkupMatches(expectedHtml);

        var element = com.Find(expectedElement);

        Assert.IsTrue(
            element.ClassList.Contains($"bit-tpg-{expectedVariant.ToString().ToLower(CultureInfo.InvariantCulture)}"));
    }

    [
        DataRow(true),
        DataRow(false)
    ]
    [TestMethod]
    public void BitTypographyNoWrapTest(bool hasNoWrap)
    {
        var com = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.NoWrap, hasNoWrap);
        });

        var defaultVariant = BitTypographyVariant.Subtitle1;
        var defaultElement = VariantMapping[defaultVariant];

        var element = com.Find(defaultElement);

        Assert.AreEqual(hasNoWrap, element.ClassList.Contains("bit-tpg-nowrap"));
    }

    [
        DataRow(true),
        DataRow(false)
    ]
    [TestMethod]
    public void BitTypographyGutterTest(bool hasGutter)
    {
        var com = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.Gutter, hasGutter);
        });

        var defaultVariant = BitTypographyVariant.Subtitle1;
        var defaultElement = VariantMapping[defaultVariant];

        var element = com.Find(defaultElement);

        Assert.AreEqual(hasGutter, element.ClassList.Contains("bit-tpg-gutter"));
    }

    [
        DataRow("div"),
        DataRow(null)
    ]
    [TestMethod]
    public void BitTypographyComponentTest(string element)
    {
        var com = RenderComponent<BitTypography>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        var defaultVariant = BitTypographyVariant.Subtitle1;
        var defaultElement = VariantMapping[defaultVariant];

        var el = com.Find(element ?? defaultElement);

        Assert.IsTrue(el.ClassList.Contains("bit-tpg"));
    }
}
