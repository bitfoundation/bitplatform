using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.NavPanel;

[TestClass]
public class BitNavPanelTests : BunitTestContext
{
    private static readonly IList<BitNavItem> Items =
    [
        new() { Text = "Home", Url = "/home", Description = "Home page" },
        new() { Text = "Docs", Url = "/docs", Description = "Documentation" }
    ];

    [TestMethod]
    public void BitNavPanelShouldRenderHeaderFooterAndItems()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add<RenderFragment>(p => p.Header, b => b.AddMarkupContent(0, "<div class='hdr'>Header</div>"));
            parameters.Add<RenderFragment>(p => p.Footer, b => b.AddMarkupContent(0, "<div class='ftr'>Footer</div>"));
        });

        var root = component.Find(".bit-npn");
        var header = component.Find(".hdr");
        var footer = component.Find(".ftr");

        Assert.IsNotNull(root);
        Assert.AreEqual("Header", header.TextContent);
        Assert.AreEqual("Footer", footer.TextContent);
        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavPanelToggleButtonShouldToggleState()
    {
        var isToggled = false;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Bind(p => p.IsToggled, isToggled, v => isToggled = v);
        });

        var toggleBtn = component.Find(".bit-npn-tbn");

        toggleBtn.Click();

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(isToggled);
            Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains("bit-npn-tgl"));
        });
    }

    [TestMethod]
    public void BitNavPanelOverlayClickShouldClose()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        var overlay = component.Find(".bit-npn-ovl");

        overlay.Click();

        component.WaitForAssertion(() =>
        {
            Assert.IsFalse(isOpen);
            Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains("bit-npn-cls"));
        });
    }

    [TestMethod]
    public void BitNavPanelItemClickShouldInvokeAndCloseWhenUrlExists()
    {
        var isOpen = true;
        string? clicked = null;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnItemClick, (BitNavItem item) => clicked = item.Text);
        });

        var firstLink = component.Find(".bit-nav-ict");

        firstLink.Click();

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual("Home", clicked);
            Assert.IsFalse(isOpen);
        });
    }

    [TestMethod]
    public void BitNavPanelShouldHideToggleWhenRequested()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.HideToggle, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-npn-tbn").Count);
    }

    [TestMethod]
    public void BitNavPanelNoToggleShouldIgnoreToggledState()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.NoToggle, true);
            parameters.Add(p => p.IsToggled, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-npn-tbn").Count);
        Assert.IsFalse(component.Find(".bit-npn").ClassList.Contains("bit-npn-tgl"));
    }

    [TestMethod]
    public void BitNavPanelShouldRespectLayoutClassesAndTopStyle()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.FitWidth, true);
            parameters.Add(p => p.FullWidth, true);
            parameters.Add(p => p.NoPad, true);
            parameters.Add(p => p.Top, 24);
        });

        var root = component.Find(".bit-npn");

        Assert.IsTrue(root.ClassList.Contains("bit-npn-fiw"));
        Assert.IsTrue(root.ClassList.Contains("bit-npn-fuw"));
        Assert.IsTrue(root.ClassList.Contains("bit-npn-npd"));
        Assert.IsTrue(root.GetAttribute("style")?.Contains("top:24px"));
    }

    [TestMethod]
    public void BitNavPanelShouldRenderIconWithNavigation()
    {
        var url = "https://example.com";

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IconUrl, "/logo.png");
            parameters.Add(p => p.IconNavUrl, url);
        });

        var img = component.Find("img.bit-npn-img");
        var a = component.Find("a:has(img.bit-npn-img)");

        Assert.AreEqual("/logo.png", img.GetAttribute("src"));
        Assert.AreEqual(a.GetAttribute("href"), url);
    }

    [TestMethod]
    public void BitNavPanelShouldHideSearchWhenRequested()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.NoSearchBox, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-srb-inp").Count);
        Assert.AreEqual(0, component.FindAll(".bit-npn-tsb").Count);
    }

    [TestMethod]
    public void BitNavPanelShouldRenderSearchBoxByDefault()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
        });

        Assert.AreEqual(1, component.FindAll(".bit-srb-inp").Count);
    }
}
