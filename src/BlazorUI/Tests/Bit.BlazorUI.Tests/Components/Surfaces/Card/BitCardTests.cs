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
}
