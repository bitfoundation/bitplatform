using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Card;

[TestClass]
public class BitCardTests : BunitTestContext
{
    [TestMethod]
    public void BitCardShouldRenderChildContent()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.AddChildContent("<div class=\"content\">Hello Card</div>");
        });

        var card = component.Find(".bit-crd");
        Assert.IsNotNull(card);
        Assert.IsTrue(card.ToMarkup().Contains("Hello Card"));
    }

    [TestMethod]
    [DataRow(BitColorKind.Primary)]
    [DataRow(BitColorKind.Secondary)]
    [DataRow(BitColorKind.Tertiary)]
    [DataRow(BitColorKind.Transparent)]
    [DataRow(null)]
    public void BitCardBackgroundClassTest(BitColorKind? background)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            if (background.HasValue)
            {
                parameters.Add(p => p.Background, background.Value);
            }
        });

        var card = component.Find(".bit-crd");

        var expected = background switch
        {
            BitColorKind.Primary => "bit-crd-pbg",
            BitColorKind.Secondary => "bit-crd-sbg",
            BitColorKind.Tertiary => "bit-crd-tbg",
            BitColorKind.Transparent => "bit-crd-rbg",
            _ => "bit-crd-sbg"
        };

        Assert.IsTrue(card.ClassList.Contains(expected));
    }

    [TestMethod]
    [DataRow(BitColorKind.Primary)]
    [DataRow(BitColorKind.Secondary)]
    [DataRow(BitColorKind.Tertiary)]
    [DataRow(BitColorKind.Transparent)]
    [DataRow(null)]
    public void BitCardBorderClassTest(BitColorKind? border)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            if (border.HasValue)
            {
                parameters.Add(p => p.Border, border.Value);
            }
        });

        var card = component.Find(".bit-crd");

        if (!border.HasValue)
        {
            Assert.IsFalse(card.ClassList.Contains("bit-crd-brd"));
            return;
        }

        var expected = border switch
        {
            BitColorKind.Primary => "bit-crd-brd",
            BitColorKind.Secondary => "bit-crd-brd",
            BitColorKind.Tertiary => "bit-crd-brd",
            BitColorKind.Transparent => "bit-crd-brd",
            _ => string.Empty
        };

        Assert.IsTrue(card.ClassList.Contains(expected));

        var specific = border switch
        {
            BitColorKind.Primary => "bit-crd-pbr",
            BitColorKind.Secondary => "bit-crd-sbr",
            BitColorKind.Tertiary => "bit-crd-tbr",
            BitColorKind.Transparent => "bit-crd-rbr",
            _ => string.Empty
        };

        Assert.IsTrue(card.ClassList.Contains(specific));
    }

    [TestMethod]
    public void BitCardFullSizeShouldApplyCorrectCssClasses()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.FullSize, true);
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-fhe"));
        Assert.IsTrue(card.ClassList.Contains("bit-crd-fwi"));
    }

    [TestMethod]
    public void BitCardFullHeightShouldApplyCorrectCssClass()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.FullHeight, true);
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-fhe"));
    }

    [TestMethod]
    public void BitCardFullWidthShouldApplyCorrectCssClass()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.FullWidth, true);
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains("bit-crd-fwi"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardNoShadowTest(bool noShadow)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.NoShadow, noShadow);
        });

        var card = component.Find(".bit-crd");

        if (noShadow)
        {
            Assert.IsTrue(card.ClassList.Contains("bit-crd-nsd"));
        }
        else
        {
            Assert.IsFalse(card.ClassList.Contains("bit-crd-nsd"));
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(12)]
    [DataRow(24)]
    public void BitCardElevationShouldApplyCorrectClass(int elevation)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Elevation, elevation);
        });

        var card = component.Find(".bit-crd");

        Assert.IsTrue(card.ClassList.Contains($"bit-crd-e{elevation}"));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(25)]
    [DataRow(-1)]
    public void BitCardElevationOutOfRangeShouldBeIgnored(int elevation)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Elevation, elevation);
        });

        var card = component.Find(".bit-crd");

        for (var i = 1; i <= 24; i++)
        {
            Assert.IsFalse(card.ClassList.Contains($"bit-crd-e{i}"));
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardNoPaddingTest(bool noPadding)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.NoPadding, noPadding);
        });

        var card = component.Find(".bit-crd");

        if (noPadding)
        {
            Assert.IsTrue(card.ClassList.Contains("bit-crd-npd"));
        }
        else
        {
            Assert.IsFalse(card.ClassList.Contains("bit-crd-npd"));
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardOutlinedTest(bool outlined)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Outlined, outlined);
        });

        var card = component.Find(".bit-crd");

        if (outlined)
        {
            Assert.IsTrue(card.ClassList.Contains("bit-crd-otl"));
        }
        else
        {
            Assert.IsFalse(card.ClassList.Contains("bit-crd-otl"));
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitCardSquareTest(bool square)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Square, square);
        });

        var card = component.Find(".bit-crd");

        if (square)
        {
            Assert.IsTrue(card.ClassList.Contains("bit-crd-sqr"));
        }
        else
        {
            Assert.IsFalse(card.ClassList.Contains("bit-crd-sqr"));
        }
    }

    [TestMethod]
    [DataRow("300px")]
    [DataRow("50%")]
    [DataRow("20rem")]
    public void BitCardWidthShouldApplyCorrectStyle(string width)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Width, width);
        });

        var card = component.Find(".bit-crd");
        Assert.IsTrue(card.GetAttribute("style")!.Contains($"width:{width}"));
    }

    [TestMethod]
    [DataRow("200px")]
    [DataRow("50%")]
    [DataRow("10rem")]
    public void BitCardHeightShouldApplyCorrectStyle(string height)
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Height, height);
        });

        var card = component.Find(".bit-crd");
        Assert.IsTrue(card.GetAttribute("style")!.Contains($"height:{height}"));
    }

    [TestMethod]
    public void BitCardWidthAndHeightShouldApplyBothStyles()
    {
        var component = RenderComponent<BitCard>(parameters =>
        {
            parameters.Add(p => p.Width, "300px");
            parameters.Add(p => p.Height, "200px");
        });

        var card = component.Find(".bit-crd");
        var style = card.GetAttribute("style")!;
        Assert.IsTrue(style.Contains("width:300px"));
        Assert.IsTrue(style.Contains("height:200px"));
    }

    [TestMethod]
    public void BitCardWidthAndHeightShouldNotRenderWhenNull()
    {
        var component = RenderComponent<BitCard>();

        var card = component.Find(".bit-crd");
        var style = card.GetAttribute("style");
        Assert.IsTrue(style is null || (style.Contains("width") is false && style.Contains("height") is false));
    }
}
