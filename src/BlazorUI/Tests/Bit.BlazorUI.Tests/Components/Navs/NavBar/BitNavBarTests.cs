using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.NavBar;

[TestClass]
public class BitNavBarTests : BunitTestContext
{
    [TestMethod]
    public void BitNavBarShouldRenderRootElement()
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>();

        var root = component.Find(".bit-nbr");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    [DataRow(true, "bit-nbr-ftw")]
    [DataRow(false, "")]
    public void BitNavBarShouldRespectFitWidth(bool fitWidth, string expectedClass)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            parameters.Add(p => p.FitWidth, fitWidth);
        });

        var root = component.Find(".bit-nbr");

        if (string.IsNullOrEmpty(expectedClass))
        {
            Assert.IsFalse(root.ClassList.Contains(expectedClass));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains(expectedClass));
        }
    }

    [TestMethod]
    [DataRow(true, "bit-nbr-flw")]
    [DataRow(false, "")]
    public void BitNavBarShouldRespectFullWidth(bool fullWidth, string expectedClass)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            parameters.Add(p => p.FullWidth, fullWidth);
        });

        var root = component.Find(".bit-nbr");

        if (string.IsNullOrEmpty(expectedClass))
        {
            Assert.IsFalse(root.ClassList.Contains(expectedClass));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains(expectedClass));
        }
    }

    [TestMethod]
    [DataRow(true, "bit-nbr-ion")]
    [DataRow(false, "")]
    public void BitNavBarShouldRespectIconOnly(bool iconOnly, string expectedClass)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            parameters.Add(p => p.IconOnly, iconOnly);
        });

        var root = component.Find(".bit-nbr");

        if (string.IsNullOrEmpty(expectedClass))
        {
            Assert.IsFalse(root.ClassList.Contains(expectedClass));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains(expectedClass));
        }
    }

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-nbr-pri")]
    [DataRow(BitColor.Secondary, "bit-nbr-sec")]
    [DataRow(null, "bit-nbr-pri")]
    public void BitNavBarShouldRespectColor(BitColor? color, string expectedClass)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            if (color.HasValue)
            {
                parameters.Add(p => p.Color, color.Value);
            }
        });

        var root = component.Find(".bit-nbr");

        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitNavMode.Automatic)]
    [DataRow(BitNavMode.Manual)]
    public void BitNavBarShouldPreserveOptionsOrderWhenAnOptionIsAddedConditionally(BitNavMode mode)
    {
        var component = RenderComponent<BitNavBarOptionsTest>(parameters =>
        {
            parameters.Add(p => p.Mode, mode);
            parameters.Add(p => p.ShowMiddle, false);
        });

        CollectionAssert.AreEqual(new[] { "Home", "Settings" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        CollectionAssert.AreEqual(new[] { "Home", "Profile", "Settings" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "Home", "Settings" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        CollectionAssert.AreEqual(new[] { "Home", "Profile", "Settings" }, GetItemTexts(component));
    }

    [TestMethod]
    public void BitNavBarShouldUpdateOptionsSelectedStateOnClickInManualMode()
    {
        var component = RenderComponent<BitNavBarOptionsTest>(parameters =>
        {
            parameters.Add(p => p.Mode, BitNavMode.Manual);
            parameters.Add(p => p.ShowMiddle, true);
        });

        var items = component.FindAll(".bit-nbr-itm");
        Assert.AreEqual(3, items.Count);

        items[1].Click();

        items = component.FindAll(".bit-nbr-itm");
        Assert.IsFalse(items[0].ClassList.Contains("bit-nbr-sel"));
        Assert.IsTrue(items[1].ClassList.Contains("bit-nbr-sel"));
        Assert.IsFalse(items[2].ClassList.Contains("bit-nbr-sel"));

        items[2].Click();

        items = component.FindAll(".bit-nbr-itm");
        Assert.IsFalse(items[1].ClassList.Contains("bit-nbr-sel"));
        Assert.IsTrue(items[2].ClassList.Contains("bit-nbr-sel"));
    }

    private static string[] GetItemTexts(IRenderedComponent<BitNavBarOptionsTest> component)
    {
        return component.FindAll(".bit-nbr-txt").Select(e => e.TextContent).ToArray();
    }

    [TestMethod]
    public void BitNavBarShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitNavBarHtmlAttributesTest>();

        var root = component.Find(".bit-nbr");

        var content = component.Find(".bit-nbr-cnt");

        Assert.AreEqual("bit", root.GetAttribute("data-val-test"));

        content.MarkupMatches(@"
<div class=""bit-nbr-cnt "">
    <button type=""button"" tabindex=""-1"" class=""bit-nbr-itm"" style="""">
        <i class=""bit-nbr-ico bit-icon bit-icon--Home ""></i>
        <span class=""bit-nbr-txt "">Home</span>
    </button>
    <button type=""button"" tabindex=""-1"" class=""bit-nbr-itm"" style="""">
        <i class=""bit-nbr-ico bit-icon bit-icon--ProductVariant ""></i>
        <span class=""bit-nbr-txt "">Products</span>
    </button>
</div>");
    }
}
