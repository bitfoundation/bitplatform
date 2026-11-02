using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ScrollToLatest;

[TestClass]
public class BitAiScrollToLatestTests : BitAiTestBase
{
    [TestMethod]
    public void BitAiScrollToLatestShouldRenderNothingWhileThereIsNowhereToGo()
    {
        var component = RenderComponent<BitAiScrollToLatest>();

        Assert.AreEqual(string.Empty, component.Markup.Trim());
    }

    [TestMethod]
    public void BitAiScrollToLatestShouldRenderAButtonNamedFromTheStringTable()
    {
        var component = RenderComponent<BitAiScrollToLatest>(p => p.Add(c => c.Visible, true));

        var button = component.Find(".bit-aistl > button.bit-btn");

        Assert.AreEqual("Scroll to latest", button.TextContent.Trim());
        Assert.IsTrue(button.ClassList.Contains("bit-btn-dft-pbg"));
        Assert.IsNotNull(button.QuerySelector(".bit-icon--Down"));
    }

    [TestMethod]
    public void BitAiScrollToLatestShouldRespectLabel()
    {
        var component = RenderComponent<BitAiScrollToLatest>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.Label, "Jump to the end"));

        Assert.AreEqual("Jump to the end", component.Find("button").TextContent.Trim());
    }

    [TestMethod]
    public void BitAiScrollToLatestShouldRaiseOnClick()
    {
        var clicks = 0;

        var component = RenderComponent<BitAiScrollToLatest>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.OnClick, () => clicks++));

        component.Find("button").Click();

        Assert.AreEqual(1, clicks);
    }

    [TestMethod]
    public void ADisabledButtonDoesNothing()
    {
        var clicks = 0;

        var component = RenderComponent<BitAiScrollToLatest>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.Disabled, true)
            .Add(c => c.OnClick, () => clicks++));

        Assert.IsTrue(component.Find(".bit-aistl").ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", component.Find("button").GetAttribute("aria-disabled"));

        component.Find("button").Click();

        Assert.AreEqual(0, clicks);
    }

    [TestMethod]
    public void BitAiScrollToLatestShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitAiScrollToLatest>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.Classes, new() { Root = "custom-root", Button = new() { Root = "custom-button" } })
            .Add(c => c.Styles, new() { Root = "color: red;", Button = new() { Root = "color: green;" } }));

        Assert.IsTrue(component.Find(".bit-aistl").ClassList.Contains("custom-root"));
        StringAssert.Contains(component.Find(".bit-aistl").GetAttribute("style"), "color: red;");
        Assert.IsTrue(component.Find("button").ClassList.Contains("custom-button"));
        StringAssert.Contains(component.Find("button").GetAttribute("style"), "color: green;");
    }

    [TestMethod]
    public void BitAiScrollToLatestShouldRespectClassAndStyle()
    {
        var component = RenderComponent<BitAiScrollToLatest>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "color: tomato;"));

        var root = component.Find(".bit-aistl");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "color: tomato;");
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiScrollToLatestShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitAiScrollToLatest>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aistl").GetAttribute("style");

        if (expectedStyle.Length == 0)
        {
            Assert.IsTrue(style is null || style.Contains("visibility:hidden") is false);
        }
        else
        {
            StringAssert.Contains(style, expectedStyle);
        }
    }

    [TestMethod]
    [DataRow(BitDir.Rtl, "bit-rtl")]
    [DataRow(BitDir.Ltr, null)]
    public void BitAiScrollToLatestShouldRespectDir(BitDir dir, string? expectedClass)
    {
        var component = RenderComponent<BitAiScrollToLatest>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.Dir, dir));

        var root = component.Find(".bit-aistl");

        Assert.AreEqual(dir.ToString().ToLower(), root.GetAttribute("dir"));
        Assert.AreEqual(expectedClass is not null, root.ClassList.Contains("bit-rtl"));
    }
}
