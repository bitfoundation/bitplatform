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

    [DataTestMethod]
    [DataRow(true, "bit-nbr-ftw")]
    [DataRow(false, "")]
    public void BitNavBarShouldRespectFitWidth(bool fitWidth, string expectedClass)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            parameters.Add(p => p.FitWidth, fitWidth);
        });

        var root = component.Find(".bit-nbr");

        if (string.IsNullOrEmpty(expectedClass) is false)
        {
            Assert.IsTrue(root.ClassList.Contains(expectedClass));
        }
    }

    [DataTestMethod]
    [DataRow(true, "bit-nbr-flw")]
    [DataRow(false, "")]
    public void BitNavBarShouldRespectFullWidth(bool fullWidth, string expectedClass)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            parameters.Add(p => p.FullWidth, fullWidth);
        });

        var root = component.Find(".bit-nbr");

        if (string.IsNullOrEmpty(expectedClass) is false)
        {
            Assert.IsTrue(root.ClassList.Contains(expectedClass));
        }
    }

    [DataTestMethod]
    [DataRow(true, "bit-nbr-ion")]
    [DataRow(false, "")]
    public void BitNavBarShouldRespectIconOnly(bool iconOnly, string expectedClass)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            parameters.Add(p => p.IconOnly, iconOnly);
        });

        var root = component.Find(".bit-nbr");

        if (string.IsNullOrEmpty(expectedClass) is false)
        {
            Assert.IsTrue(root.ClassList.Contains(expectedClass));
        }
    }

    [DataTestMethod]
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
    public void BitNavBarShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitNavBarHtmlAttributesTest>();

        var content = component.Find(".bit-nbr-cnt");

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
