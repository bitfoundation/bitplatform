using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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

    private static IList<BitNavItem> TreeItems() =>
    [
        new() { Text = "Home", Url = "/home", Description = "Home page", Data = "dashboard" },
        new()
        {
            Text = "AdminPanel",
            ChildItems =
            [
                new() { Text = "Dashboard", Url = "/dashboard" },
                new() { Text = "Categories", Url = "/categories" },
            ]
        },
        new() { Text = "Settings", Url = "/settings" }
    ];

    private class CustomItem
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public List<CustomItem>? Children { get; set; }
    }

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
    public void BitNavPanelShouldRenderTheBadgesAndTheItemSuffixTemplateOfItsItems()
    {
        IList<BitNavItem> items =
        [
            new() { Text = "Inbox", Url = "/inbox", Badge = "3" },
            new() { Text = "Alerts", Url = "/alerts", Dot = true },
        ];

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ItemSuffixTemplate, (BitNavItem item) => builder => builder.AddMarkupContent(0, "<span class='suffix'>s</span>"));
        });

        Assert.AreEqual("3", component.Find(".bit-nav-bdg").TextContent);
        Assert.AreEqual(1, component.FindAll(".bit-nav-bdt").Count);
        Assert.AreEqual(2, component.FindAll(".bit-nav-sfx .suffix").Count);
    }

    [TestMethod]
    public void BitNavPanelShouldKeepTheBadgesAloneWhileToggled()
    {
        IList<BitNavItem> items = [new() { Text = "Inbox", Url = "/inbox", IconName = "Mail", Badge = "3" }];

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.IsToggled, true);
            parameters.Add(p => p.ItemSuffixTemplate, (BitNavItem item) => builder => builder.AddMarkupContent(0, "<span class='suffix'>s</span>"));
        });

        Assert.AreEqual("3", component.Find(".bit-nav-bdg").TextContent);
        Assert.AreEqual(0, component.FindAll(".suffix").Count);
        Assert.AreEqual("Inbox (3)", component.Find(".bit-nav-ict").GetAttribute("aria-label"));
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
    public void BitNavPanelToggleButtonShouldReportItsStateAndName()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
        });

        var toggleBtn = component.Find(".bit-npn-tbn");

        Assert.AreEqual("true", toggleBtn.GetAttribute("aria-expanded"));
        Assert.IsTrue(toggleBtn.GetAttribute("aria-label").HasValue());

        toggleBtn.Click();

        component.WaitForAssertion(() => Assert.AreEqual("false", component.Find(".bit-npn-tbn").GetAttribute("aria-expanded")));
    }

    [TestMethod]
    public void BitNavPanelToggleButtonShouldAcceptCustomAriaLabelAndIcon()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.ToggleAriaLabel, "Menu");
            parameters.Add(p => p.ToggleIcon, BitIconInfo.Fa("solid bars"));
        });

        var toggleBtn = component.Find(".bit-npn-tbn");

        Assert.AreEqual("Menu", toggleBtn.GetAttribute("aria-label"));
        Assert.AreEqual("Menu", toggleBtn.GetAttribute("title"));
        Assert.IsNotNull(component.Find(".bit-npn-tbn i.fa-bars"));
        Assert.AreEqual(0, component.FindAll(".bit-icon-ex--ColumnRightTwoThirds").Count);
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

        Assert.AreEqual("true", overlay.GetAttribute("aria-hidden"));

        overlay.Click();

        component.WaitForAssertion(() =>
        {
            Assert.IsFalse(isOpen);
            Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains("bit-npn-cls"));
        });
    }

    [TestMethod]
    public void BitNavPanelShouldNotRenderOverlayWhenNoOverlay()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoOverlay, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-npn-ovl").Count);
    }

    [TestMethod]
    public void BitNavPanelDisabledShouldDrawNoOverlay()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.IsOpen, true);
        });

        // Nothing of a disabled panel answers a click, so it covers the page with nothing that would then
        // have to be dismissed.
        Assert.AreEqual(0, component.FindAll(".bit-npn-ovl").Count);
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
    public void BitNavPanelItemClickShouldNotCloseWhenNoAutoClose()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.NoAutoClose, true);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-nav-ict").Click();

        component.WaitForAssertion(() => Assert.IsTrue(isOpen));
    }

    [TestMethod]
    public void BitNavPanelItemWithoutUrlClickShouldNotClose()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.FindAll(".bit-nav-ict")[1].Click();

        component.WaitForAssertion(() => Assert.IsTrue(isOpen));
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
    public async Task BitNavPanelNoToggleShouldIgnoreTheToggleMethod()
    {
        var isToggled = false;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.NoToggle, true);
            parameters.Bind(p => p.IsToggled, isToggled, v => isToggled = v);
        });

        await component.InvokeAsync(() => component.Instance.Toggle());

        Assert.IsFalse(isToggled);
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
    public void BitNavPanelShouldRenderWidthCssVariables()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Width, 260);
            parameters.Add(p => p.ToggledWidth, 72);
        });

        var style = component.Find(".bit-npn").GetAttribute("style");

        Assert.IsTrue(style?.Contains("--bit-npn-w:260px"));
        Assert.IsTrue(style?.Contains("--bit-npn-tw:72px"));
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
        // The link carries the name, so the logo inside it is left without one.
        Assert.AreEqual(string.Empty, img.GetAttribute("alt"));
        Assert.AreEqual(url, a.GetAttribute("href"));
        Assert.IsTrue(a.ClassList.Contains("bit-npn-hln"));
        Assert.AreEqual("Home", a.GetAttribute("aria-label"));
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
    public void BitNavPanelParentItemBodyClickShouldToggleExpansion()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
        });

        var parentButton = component.FindAll(".bit-nav-ict")[1];

        parentButton.Click();
        component.WaitForAssertion(() => Assert.AreEqual(5, component.FindAll(".bit-nav-ict").Count));

        component.FindAll(".bit-nav-ict")[1].Click();
        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count));
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

    [TestMethod]
    public void BitNavPanelSearchShouldFilterItemsByTextDescriptionAndData()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
        });

        // The text of an item.
        component.Find(".bit-srb-inp").Change("settings");
        component.WaitForAssertion(() =>
        {
            var items = component.FindAll(".bit-nav-ict");
            Assert.AreEqual(1, items.Count);
            Assert.IsTrue(items[0].TextContent.Contains("Settings"));
        });

        // The description of an item, and every word of the term having to be somewhere in it.
        component.Find(".bit-srb-inp").Change("home page");
        component.WaitForAssertion(() =>
        {
            var items = component.FindAll(".bit-nav-ict");
            Assert.AreEqual(1, items.Count);
            Assert.IsTrue(items[0].TextContent.Contains("Home"));
        });

        // The data of an item.
        component.Find(".bit-srb-inp").Change("dashboard");
        component.WaitForAssertion(() =>
        {
            var items = component.FindAll(".bit-nav-ict");
            // "Home" matches through its Data and "Dashboard" through its text, and neither is listed twice.
            Assert.AreEqual(2, items.Count);
        });
    }

    [TestMethod]
    public void BitNavPanelSearchShouldKeepTheBranchOfAMatchedItem()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
        });

        component.Find(".bit-srb-inp").Change("admin");

        component.WaitForAssertion(() =>
        {
            var items = component.FindAll(".bit-nav-ict");
            Assert.AreEqual(1, items.Count);
            Assert.IsTrue(items[0].TextContent.Contains("AdminPanel"));
        });

        // The matched group still opens onto the items it groups.
        component.FindAll(".bit-nav-ict")[0].Click();
        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count));
    }

    [TestMethod]
    public void BitNavPanelSearchWithNoResultShouldShowTheEmptyMessageAndRecover()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
            parameters.Add(p => p.EmptyListMessage, "Nothing here!");
        });

        component.Find(".bit-srb-inp").Change("zzz");

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, component.FindAll(".bit-nav-ict").Count);
            Assert.IsTrue(component.Markup.Contains("Nothing here!"));
        });

        // Typing on after an empty result has to keep filtering rather than showing the whole list again.
        component.Find(".bit-srb-inp").Change("settings");

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count);
            Assert.IsFalse(component.Markup.Contains("Nothing here!"));
        });
    }

    [TestMethod]
    public void BitNavPanelEmptyListTemplateShouldReplaceTheMessage()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
            parameters.Add<RenderFragment>(p => p.EmptyListTemplate, b => b.AddMarkupContent(0, "<div class='empty'>None</div>"));
        });

        component.Find(".bit-srb-inp").Change("zzz");

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".empty").Count));
    }

    [TestMethod]
    public void BitNavPanelSearchShouldBindTheSearchTextAndReportIt()
    {
        string? searchText = null;
        string? searched = null;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
            parameters.Add(p => p.OnSearch, (string? v) => searched = v);
            parameters.Bind(p => p.SearchText, searchText, v => searchText = v);
        });

        component.Find(".bit-srb-inp").Change("settings");

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual("settings", searchText);
            Assert.AreEqual("settings", searched);
        });
    }

    [TestMethod]
    public void BitNavPanelShouldFilterByTheSearchTextParameter()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchText, "settings");
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count));

        component.Render(parameters => parameters.Add(p => p.SearchText, null));

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count));
    }

    [TestMethod]
    public void BitNavPanelSearchFilterShouldReplaceTheDefaultMatching()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
            parameters.Add(p => p.SearchFilter, (BitNavItem item, string term) =>
                item.Text.StartsWith(term, StringComparison.OrdinalIgnoreCase));
        });

        // "page" is in the description of Home, which the custom filter does not look at.
        component.Find(".bit-srb-inp").Change("page");
        component.WaitForAssertion(() => Assert.AreEqual(0, component.FindAll(".bit-nav-ict").Count));

        component.Find(".bit-srb-inp").Change("ho");
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count));
    }

    [TestMethod]
    public void BitNavPanelClearSearchShouldRestoreTheWholeList()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
        });

        component.Find(".bit-srb-inp").Change("settings");
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count));

        component.InvokeAsync(() => component.Instance.ClearSearch());

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count);
            Assert.AreEqual(string.Empty, component.Find(".bit-srb-inp").GetAttribute("value") ?? string.Empty);
        });
    }

    [TestMethod]
    public void BitNavPanelTogglingShouldClearTheSearch()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
        });

        component.Find(".bit-srb-inp").Change("settings");
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count));

        component.Find(".bit-npn-tbn").Click();

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count));
    }

    [TestMethod]
    public void BitNavPanelEscapeShouldCloseTheDrawer()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        SetDrawerScreen(component, true);

        component.Find(".bit-npn").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() => Assert.IsFalse(isOpen));
    }

    [TestMethod]
    public void BitNavPanelEscapeShouldLeaveTheColumnOfAWideScreenAlone()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        SetDrawerScreen(component, false);

        // The column is not a surface over the page: closing it there would report a state change with
        // nothing on screen to show for it.
        component.Find(".bit-npn").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() => Assert.IsTrue(isOpen));
    }

    [TestMethod]
    public void BitNavPanelEscapeShouldNotCloseThePanelWhileASearchIsInPlace()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        SetDrawerScreen(component, true);

        component.Find(".bit-srb-inp").Change("settings");

        component.Find(".bit-npn").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() => Assert.IsTrue(isOpen));
    }

    [TestMethod]
    public void BitNavPanelEscapeShouldDoNothingWhenDisabled()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Disabled, true);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        SetDrawerScreen(component, true);

        component.Find(".bit-npn").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() => Assert.IsTrue(isOpen));
    }

    [TestMethod]
    public void BitNavPanelEscapeShouldDoNothingWhenTheDrawerIsClosed()
    {
        var reported = 0;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.IsOpenChanged, (bool _) => reported++);
        });

        SetDrawerScreen(component, true);

        // A closed drawer has nothing for the key to do, so it neither reports a state change nor claims the key.
        component.WaitForAssertion(() => Assert.IsNull(component.Find(".bit-npn").GetAttribute("data-bit-esc")));

        component.Find(".bit-npn").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, reported);
    }

    [TestMethod]
    public void BitNavPanelShouldClaimTheEscapeThatClosesTheDrawer()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        SetDrawerScreen(component, true);

        component.WaitForAssertion(() => Assert.AreEqual("claim", component.Find(".bit-npn").GetAttribute("data-bit-esc")));

        component.Find(".bit-npn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // A closed drawer has nothing left for the key to do, so it is left to whatever the panel sits in.
        component.WaitForAssertion(() =>
        {
            Assert.IsFalse(isOpen);
            Assert.IsNull(component.Find(".bit-npn").GetAttribute("data-bit-esc"));
        });
    }

    [TestMethod]
    public void BitNavPanelShouldNotClaimEscapeOnTheColumnOfAWideScreen()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
        });

        SetDrawerScreen(component, false);

        component.WaitForAssertion(() => Assert.IsNull(component.Find(".bit-npn").GetAttribute("data-bit-esc")));
    }

    [TestMethod]
    public void BitNavPanelShouldClaimTheEscapeThatEmptiesTheSearch()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
            parameters.Add(p => p.IsOpen, true);
        });

        // The column closes on nothing, so the search is the only thing the key can act on there.
        SetDrawerScreen(component, false);

        component.WaitForAssertion(() => Assert.IsNull(component.Find(".bit-npn").GetAttribute("data-bit-esc")));

        component.Find(".bit-srb-inp").Change("settings");

        component.WaitForAssertion(() => Assert.AreEqual("claim", component.Find(".bit-npn").GetAttribute("data-bit-esc")));

        component.Find(".bit-npn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count);
            Assert.IsNull(component.Find(".bit-npn").GetAttribute("data-bit-esc"));
        });
    }

    [TestMethod]
    public void BitNavPanelShouldLeaveTheEscapeTheSearchBoxClaimedAlone()
    {
        // The search box empties its own text on Escape and claims the press (data-bit-esc), which Blazor's dispatch
        // still bubbles up to the panel's root. The browser tells the panel the press was claimed inside it
        // (Utils.watchEscapeClaim), so the panel neither empties the search a second time nor closes the drawer.
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        SetDrawerScreen(component, true);

        component.Find(".bit-srb-inp").Change("settings");
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count));

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.watchEscapeClaim"].Count);

        // The search box claimed the press, which is what the browser answers ahead of the keydown.
        Assert.IsNotNull(component.Find(".bit-srb-inp").GetAttribute("data-bit-esc"));
        component.InvokeAsync(() => component.Instance._OnEscapeVerdict(true));

        // The search box empties its text on the press; bUnit does not bubble it, so the half Blazor's dispatch hands
        // the root is dispatched there by hand.
        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        component.Find(".bit-npn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count, "the search box emptied the search");
            Assert.IsTrue(isOpen, "the drawer stays open on the same press");
        });

        // The next press is the root's own, which the browser says so of, and it closes the drawer.
        component.InvokeAsync(() => component.Instance._OnEscapeVerdict(false));
        component.Find(".bit-npn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() => Assert.IsFalse(isOpen));
    }

    [TestMethod]
    public void BitNavPanelShouldIgnoreAModifiedEscape()
    {
        // Only the plain key is claimed, so only the plain key acts: one with a modifier is the surrounding surface's.
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        SetDrawerScreen(component, true);

        component.Find(".bit-srb-inp").Change("settings");
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count));

        component.Find(".bit-npn").KeyDown(new KeyboardEventArgs { Key = "Escape", ShiftKey = true });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count, "the search stays in place");
            Assert.IsTrue(isOpen);
        });

        // With no search to empty, the plain key would close the drawer; a modified one leaves it open.
        var drawerIsOpen = true;

        var drawer = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Bind(p => p.IsOpen, drawerIsOpen, v => drawerIsOpen = v);
        });

        SetDrawerScreen(drawer, true);

        drawer.WaitForAssertion(() => Assert.AreEqual("claim", drawer.Find(".bit-npn").GetAttribute("data-bit-esc")));

        drawer.Find(".bit-npn").KeyDown(new KeyboardEventArgs { Key = "Escape", AltKey = true });

        drawer.WaitForAssertion(() => Assert.IsTrue(drawerIsOpen));
    }

    [TestMethod]
    public void BitNavPanelShouldNotClaimEscapeWhenDisabled()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Disabled, true);
        });

        SetDrawerScreen(component, true);

        component.WaitForAssertion(() => Assert.IsNull(component.Find(".bit-npn").GetAttribute("data-bit-esc")));
    }

    [TestMethod]
    public void BitNavPanelOpenAndCloseShouldDriveTheOpenState()
    {
        var isOpen = false;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.InvokeAsync(() => component.Instance.Open());
        component.WaitForAssertion(() => Assert.IsTrue(isOpen));

        component.InvokeAsync(() => component.Instance.Close());
        component.WaitForAssertion(() => Assert.IsFalse(isOpen));
    }

    [TestMethod]
    public void BitNavPanelExpandAllAndCollapseAllShouldDriveTheTree()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
        });

        component.InvokeAsync(() => component.Instance.ExpandAll());
        component.WaitForAssertion(() => Assert.AreEqual(5, component.FindAll(".bit-nav-ict").Count));

        component.InvokeAsync(() => component.Instance.CollapseAll());
        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count));
    }

    [TestMethod]
    public void BitNavPanelAllExpandedShouldOpenEveryGroup()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.AllExpanded, true);
        });

        Assert.AreEqual(5, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavPanelShouldSelectTheDefaultSelectedItemInManualMode()
    {
        var items = TreeItems();

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NavMode, BitNavMode.Manual);
            parameters.Add(p => p.DefaultSelectedItem, items[2]);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-sel").Count));
        Assert.IsTrue(component.Find(".bit-nav-sel").TextContent.Contains("Settings"));
    }

    [TestMethod]
    public void BitNavPanelShouldNotSelectAnItemOfItsOwnInManualMode()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.NavMode, BitNavMode.Manual);
        });

        Assert.AreEqual(0, component.FindAll(".bit-nav-sel").Count);
    }

    [TestMethod]
    public void BitNavPanelShouldBindTheSelectedItem()
    {
        var items = TreeItems();
        BitNavItem? selected = null;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NavMode, BitNavMode.Manual);
            parameters.Bind(p => p.SelectedItem, selected, v => selected = v);
        });

        component.FindAll(".bit-nav-ict")[0].Click();

        component.WaitForAssertion(() => Assert.AreEqual("Home", selected?.Text));
    }

    [TestMethod]
    public void BitNavPanelShouldRenderCustomItemsThroughNameSelectors()
    {
        List<CustomItem> items =
        [
            new()
            {
                Name = "Home",
                Address = "/home",
                Children = [new() { Name = "Sub", Address = "/sub" }]
            },
            new() { Name = "Docs", Address = "/docs" }
        ];

        var component = RenderComponent<BitNavPanel<CustomItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SearchDebounceTime, 0);
            parameters.Add(p => p.NameSelectors, new BitNavNameSelectors<CustomItem>
            {
                Text = { Name = nameof(CustomItem.Name) },
                Url = { Name = nameof(CustomItem.Address) },
                ChildItems = { Name = nameof(CustomItem.Children) },
            });
        });

        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
        Assert.IsTrue(component.FindAll(".bit-nav-ict")[0].TextContent.Contains("Home"));

        // The search reads the custom item through the very same selectors.
        component.Find(".bit-srb-inp").Change("docs");
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count));
    }

    [TestMethod]
    [DataRow(BitSize.Small, "bit-nav-sm")]
    [DataRow(BitSize.Medium, "bit-nav-md")]
    [DataRow(BitSize.Large, "bit-nav-lg")]
    public void BitNavPanelShouldPassTheSizeToTheNav(BitSize size, string cssClass)
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find("nav.bit-nav").ClassList.Contains(cssClass));
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheChevronOfAnIconName()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.ChevronDownIconName, "ChevronDown");
        });

        var chevron = component.Find(".bit-nav-cbt i");

        Assert.IsTrue(chevron.ClassList.Contains("bit-icon"));
        Assert.IsTrue(chevron.ClassList.Contains("bit-icon--ChevronDown"));
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheChevronOfAnExternalIcon()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.ChevronDownIcon, BitIconInfo.Fa("solid chevron-down"));
        });

        var chevron = component.Find(".bit-nav-cbt i");

        Assert.IsTrue(chevron.ClassList.Contains("fa-chevron-down"));
    }

    [TestMethod]
    public void BitNavPanelShouldNameTheNavLandmark()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.AriaLabel, "Main menu");
        });

        Assert.AreEqual("Main menu", component.Find("nav.bit-nav").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavPanelDisabledShouldDisableItsControls()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Disabled, true);
        });

        // The button keeps its tab stop and reports the state through aria-disabled (AllowDisabledFocus).
        Assert.AreEqual("true", component.Find(".bit-npn-tbn").GetAttribute("aria-disabled"));
        Assert.IsTrue(component.Find(".bit-srb-inp").HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitNavPanelShouldPickUpItemsAssignedLater()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitNavItem>());
        });

        Assert.AreEqual(0, component.FindAll(".bit-nav-ict").Count);

        component.Render(parameters => parameters.Add(p => p.Items, Items));

        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavPanelSearchShouldSurviveAnItemsChange()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
        });

        component.Find(".bit-srb-inp").Change("settings");
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count));

        component.Render(parameters => parameters.Add(p => p.Items, TreeItems()));

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count));
    }

    [TestMethod]
    public void BitNavPanelToggledStateShouldRenderTheSearchButtonAndIconOnlyNav()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsToggled, true);
        });

        Assert.IsTrue(component.Find("nav.bit-nav").ClassList.Contains("bit-nav-ion"));

        // The rail shows a button in place of the box it has no room for.
        var searchButton = component.FindAll("button").Single(b => b.GetAttribute("aria-label") == "Search");

        Assert.IsNotNull(searchButton);
    }

    [TestMethod]
    public void BitNavPanelSearchButtonShouldLeaveTheToggledState()
    {
        var isToggled = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Bind(p => p.IsToggled, isToggled, v => isToggled = v);
        });

        component.FindAll("button").Single(b => b.GetAttribute("aria-label") == "Search").Click();

        component.WaitForAssertion(() => Assert.IsFalse(isToggled));
    }

    [TestMethod]
    public void BitNavPanelEscapeShouldClearAnActiveSearchFirst()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        SetDrawerScreen(component, true);

        component.Find(".bit-srb-inp").Change("settings");
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count));

        component.Find(".bit-npn").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count);
            Assert.IsTrue(isOpen);
        });

        component.Find(".bit-npn").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() => Assert.IsFalse(isOpen));
    }

    [TestMethod]
    public void BitNavPanelShouldAnnounceTheSearchResults()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
        });

        // The region is there from the start, empty: one that enters the DOM together with its first
        // message is never announced.
        Assert.AreEqual(string.Empty, component.Find(".bit-npn-lvr").TextContent);

        component.Find(".bit-srb-inp").Change("settings");
        component.WaitForAssertion(() => Assert.AreEqual("1 item found.", component.Find(".bit-npn-lvr").TextContent));

        component.Find(".bit-srb-inp").Change("zzz");
        component.WaitForAssertion(() => Assert.AreEqual("No item found.", component.Find(".bit-npn-lvr").TextContent));

        var live = component.Find(".bit-npn-lvr");
        Assert.AreEqual("status", live.GetAttribute("role"));
        Assert.AreEqual("polite", live.GetAttribute("aria-live"));
    }

    [TestMethod]
    public void BitNavPanelSearchAnnouncementProviderShouldReplaceTheDefaultText()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
            parameters.Add(p => p.SearchAnnouncementProvider, (int count) => $"{count} hits");
        });

        component.Find(".bit-srb-inp").Change("settings");

        component.WaitForAssertion(() => Assert.AreEqual("1 hits", component.Find(".bit-npn-lvr").TextContent));
    }

    [TestMethod]
    public void BitNavPanelFocusSearchBoxShouldLeaveAnExpandedRailToggled()
    {
        var isToggled = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.ExpandOnHover, true);
            parameters.Bind(p => p.IsToggled, isToggled, v => isToggled = v);
        });

        component.Find(".bit-npn").MouseEnter();

        component.WaitForAssertion(() => Assert.IsFalse(component.Find(".bit-npn").ClassList.Contains("bit-npn-tgl")));

        component.InvokeAsync(() => component.Instance.FocusSearchBox());

        // The search box is already on screen, so the focus goes to it: leaving the rail for good is not what
        // asking for the focus asked for.
        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(isToggled);
            Assert.AreEqual(1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
        });
    }

    [TestMethod]
    public void BitNavPanelShouldKeepTheKeyDownHandlerItWasGiven()
    {
        var isOpen = true;
        var handled = false;

        // Arbitrary HTML attributes are captured by BitComponentBase from unmatched parameters, so supply
        // them as raw component attributes (as real markup would) rather than through the builder, which
        // rejects unmatched params on components without [Parameter(CaptureUnmatchedValues)].
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitNavPanel<BitNavItem>>(0);
            builder.AddAttribute(1, nameof(BitNavPanel<BitNavItem>.Items), Items);
            builder.AddAttribute(2, nameof(BitNavPanel<BitNavItem>.IsOpen), isOpen);
            builder.AddAttribute(3, nameof(BitNavPanel<BitNavItem>.IsOpenChanged), EventCallback.Factory.Create<bool>(this, v => isOpen = v));
            builder.AddAttribute(4, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, () => handled = true));
            builder.CloseComponent();
        }).FindComponent<BitNavPanel<BitNavItem>>();

        SetDrawerScreen(component, true);

        component.Find(".bit-npn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // Both run: the panel still closes on the key, and the handler the caller splatted is still called.
        component.WaitForAssertion(() =>
        {
            Assert.IsFalse(isOpen);
            Assert.IsTrue(handled);
        });
    }

    [TestMethod]
    public void BitNavPanelExpandOnHoverShouldLeaveTheRailWhileHovered()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsToggled, true);
            parameters.Add(p => p.ExpandOnHover, true);
        });

        var root = component.Find(".bit-npn");

        Assert.IsTrue(root.ClassList.Contains("bit-npn-eoh"));
        Assert.IsTrue(root.ClassList.Contains("bit-npn-tgl"));

        root.MouseEnter();

        component.WaitForAssertion(() =>
        {
            Assert.IsFalse(component.Find(".bit-npn").ClassList.Contains("bit-npn-tgl"));
            Assert.IsFalse(component.Find("nav.bit-nav").ClassList.Contains("bit-nav-ion"));
        });

        component.Find(".bit-npn").MouseLeave();

        component.WaitForAssertion(() => Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains("bit-npn-tgl")));
    }

    [TestMethod]
    public void BitNavPanelExpandOnHoverShouldCollapseOnTheToggleItIsPointedAt()
    {
        var isToggled = false;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.ExpandOnHover, true);
            parameters.Bind(p => p.IsToggled, isToggled, v => isToggled = v);
        });

        // The pointer is over the panel and the focus lands on the button being clicked, both of which
        // expand a rail - so a panel that only read them would collapse into one and open again at once.
        component.Find(".bit-npn").MouseEnter();
        component.Find(".bit-npn").FocusIn();
        component.Find(".bit-npn-tbn").Click();

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(isToggled);
            Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains("bit-npn-tgl"));
        });

        // The pointer that leaves the collapsed rail and comes back to it expands it again.
        component.Find(".bit-npn").MouseEnter();

        component.WaitForAssertion(() => Assert.IsFalse(component.Find(".bit-npn").ClassList.Contains("bit-npn-tgl")));
    }

    [TestMethod]
    public void BitNavPanelWithoutExpandOnHoverShouldNotListenForHover()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsToggled, true);
        });

        // No handler is attached at all, so the pointer crossing the panel cannot re-render it.
        Assert.ThrowsExactly<MissingEventHandlerException>(() => component.Find(".bit-npn").MouseEnter());

        Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains("bit-npn-tgl"));
    }

    [TestMethod]
    public void BitNavPanelShouldForwardTheItemApisToTheNav()
    {
        var items = TreeItems();
        BitNavItem? selected = null;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NavMode, BitNavMode.Manual);
            parameters.Bind(p => p.SelectedItem, selected, v => selected = v);
        });

        component.InvokeAsync(() => component.Instance.ExpandItem(items[1]));
        component.WaitForAssertion(() => Assert.AreEqual(5, component.FindAll(".bit-nav-ict").Count));

        component.InvokeAsync(() => component.Instance.CollapseItem(items[1]));
        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count));

        component.InvokeAsync(() => component.Instance.ToggleItem(items[1]));
        component.WaitForAssertion(() => Assert.AreEqual(5, component.FindAll(".bit-nav-ict").Count));

        component.InvokeAsync(() => component.Instance.SelectItem(items[2]));
        component.WaitForAssertion(() => Assert.AreEqual("Settings", selected?.Text));
    }

    [TestMethod]
    public void BitNavPanelLogoLinkShouldCarryAName()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IconUrl, "/logo.png");
            parameters.Add(p => p.IconNavUrl, "/");
            parameters.Add(p => p.AriaLabel, "Main menu");
        });

        var link = component.Find("a:has(img.bit-npn-img)");

        Assert.AreEqual("Main menu", link.GetAttribute("aria-label"));
        Assert.AreEqual(string.Empty, component.Find("img.bit-npn-img").GetAttribute("alt"));
    }

    [TestMethod]
    public void BitNavPanelSearchShouldSeeItemsAppendedToTheSameList()
    {
        var items = new List<BitNavItem>(TreeItems());

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.SearchDebounceTime, 0);
        });

        component.Find(".bit-srb-inp").Change("settings");
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count));

        items.Add(new() { Text = "Settings backup", Url = "/backup" });
        component.Render();

        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count));
    }

    [TestMethod]
    public void BitNavPanelAutoFocusShouldMoveTheFocusIntoTheDrawerOnOpen()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.AutoFocus, true);
        });

        SetDrawerScreen(component, true);

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        component.WaitForAssertion(() =>
        {
            var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();
            Assert.AreEqual(1, focused.Count);
            // Before .NET 10 a re-render drops the reference id from the rendered markup, so the focused
            // reference is compared against the search box's own input reference instead of the element.
            var input = component.FindComponent<BitSearchBox>().Instance.InputElement;
            Assert.AreEqual(input.Id, ((ElementReference)focused[0].Arguments[0]!).Id);
        });
    }

    [TestMethod]
    public void BitNavPanelAutoFocusShouldFallBackToTheFirstItem()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.NoSearchBox, true);
        });

        SetDrawerScreen(component, true);

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        // A panel without a search box hands the focus to the first item of the nav instead - after the modal
        // drawer has taken it onto itself, so it is inside the drawer whether or not the item takes it.
        component.WaitForAssertion(() =>
        {
            var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();
            Assert.AreEqual(2, focused.Count);
            Assert.AreEqual(component.Instance.RootElement.Id, ((ElementReference)focused[0].Arguments[0]!).Id);
            Assert.AreNotEqual(component.Instance.RootElement.Id, ((ElementReference)focused[1].Arguments[0]!).Id);
        });
    }

    [TestMethod]
    public void BitNavPanelOpenColumnThatBecomesADrawerShouldTakeTheFocus()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
        });

        SetDrawerScreen(component, false);

        component.WaitForAssertion(() => Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains("bit-npn-col")));

        Assert.AreEqual(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);

        // The screen shrinks below the breakpoint while the panel is open: the column it was is now a modal drawer
        // over the page, so the keyboard is moved into it exactly as if it had just been opened.
        SetDrawerScreen(component, true);

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual("-1", component.Find(".bit-npn").GetAttribute("tabindex"));
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.captureFocusOrigin"].Count);

            var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();
            Assert.AreEqual(1, focused.Count);
            Assert.AreEqual(component.Instance.RootElement.Id, ((ElementReference)focused[0].Arguments[0]!).Id);
        });
    }

    [TestMethod]
    public void BitNavPanelAutoFocusShouldLeaveTheKeyboardAloneOnAWideScreen()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.AutoFocus, true);
        });

        SetDrawerScreen(component, false);

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        // Nothing opened over the page on a wide screen, so nothing takes the keyboard from it - and nothing
        // was recorded to hand it back to either.
        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
            Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.captureFocusOrigin"].Count);
        });
    }

    [TestMethod]
    public void BitNavPanelWithoutAutoFocusShouldLeaveTheFocusAlone()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
        });

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.AreEqual(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public void BitNavPanelShouldFollowTheCurrentUrlInAutomaticMode()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo("settings");

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, component.FindAll(".bit-nav-sel").Count);
            Assert.IsTrue(component.Find(".bit-nav-sel").TextContent.Contains("Settings"));
        });
    }

    [TestMethod]
    public void BitNavPanelShouldReportTheUrlSelectionThroughTheBoundSelectedItem()
    {
        BitNavItem? selected = null;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Bind(p => p.SelectedItem, selected, v => selected = v);
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo("dashboard");

        component.WaitForAssertion(() => Assert.AreEqual("Dashboard", selected?.Text));
    }

    [TestMethod]
    public void BitNavPanelShouldNotThrowOnNullItems()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, null!);
        });

        Assert.IsNotNull(component.Find(".bit-npn"));
        Assert.AreEqual(0, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavPanelShouldPointTheToggleButtonAtThePanelItControls()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
        });

        var root = component.Find(".bit-npn");
        var toggle = component.Find(".bit-npn-tbn");

        Assert.AreEqual(root.Id, toggle.GetAttribute("aria-controls"));
    }

    [TestMethod]
    public void BitNavPanelIconAriaLabelShouldNameTheLogoApartFromTheLandmark()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.AriaLabel, "Main menu");
            parameters.Add(p => p.IconUrl, "/images/icon.png");
            parameters.Add(p => p.IconNavUrl, "/");
            parameters.Add(p => p.IconAriaLabel, "bit platform");
        });

        Assert.AreEqual("bit platform", component.Find(".bit-npn-hdr a").GetAttribute("aria-label"));
        Assert.AreEqual("Main menu", component.Find("nav.bit-nav").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavPanelBareLogoShouldTakeTheIconAriaLabelAsItsAlternativeText()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IconUrl, "/images/icon.png");
            parameters.Add(p => p.IconAriaLabel, "bit platform");
        });

        Assert.AreEqual(0, component.FindAll(".bit-npn-hdr a").Count);
        Assert.AreEqual("bit platform", component.Find(".bit-npn-img").GetAttribute("alt"));
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheSearchIconOfTheRail()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsToggled, true);
            parameters.Add(p => p.SearchIconName, "Zoom");
        });

        var icon = component.Find(".bit-npn-tsb i");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Zoom"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsToggled, true);
            parameters.Add(p => p.SearchIcon, BitIconInfo.Fa("solid magnifying-glass"));
        });

        Assert.IsTrue(component.Find(".bit-npn-tsb i").ClassList.Contains("fa-magnifying-glass"));
    }

    [TestMethod]
    [DataRow(null, false)]
    [DataRow(BitPlacement.Start, false)]
    [DataRow(BitPlacement.End, true)]
    // The values that name no edge the drawer is docked to render the default Start.
    [DataRow(BitPlacement.Left, false)]
    [DataRow(BitPlacement.Right, false)]
    [DataRow(BitPlacement.Center, false)]
    public void BitNavPanelShouldDockTheDrawerToThePlacementItIsGiven(BitPlacement? placement, bool isEnd)
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Placement, placement);
        });

        Assert.AreEqual(isEnd, component.Find(".bit-npn").ClassList.Contains("bit-npn-end"));
    }

    // The drawer is docked to the edge its Placement names in the direction it is laid out in, and the swipe that closes
    // it goes towards that edge. A Dir that names no direction leaves it to the page (unset) or to the content (Auto),
    // which is read as the swipe starts: a Start drawer of a right-to-left page sits on the right.
    [TestMethod,
        DataRow(null, true),
        DataRow(BitDir.Auto, true),
        DataRow(BitDir.Ltr, false),
        DataRow(BitDir.Rtl, true)
    ]
    public async Task BitNavPanelSwipeShouldCloseTowardsTheEdgeOfTheDirectionItIsLaidOutIn(BitDir? dir, bool closes)
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isRtl", _ => true).SetResult(true);

        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Dir, dir);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        var trap = component.FindComponent<BitSwipeTrap>();

        await component.InvokeAsync(() => trap.Instance._OnStart(0, 0));
        await component.InvokeAsync(() => trap.Instance._OnTrigger(100, 0, 1, 0));

        Assert.AreEqual(closes, isOpen is false);
        // A named direction is answered without a round trip; only one Dir does not name is read off the browser.
        Assert.AreEqual(dir is null or BitDir.Auto, Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Utils.isRtl"));
    }

    [TestMethod,
        DataRow(BitDir.Ltr, null, -40, true),
        DataRow(BitDir.Ltr, null, 40, false),
        DataRow(BitDir.Ltr, BitPlacement.End, 40, true),
        DataRow(BitDir.Ltr, BitPlacement.End, -40, false),
        DataRow(BitDir.Rtl, null, 40, true),
        DataRow(BitDir.Rtl, BitPlacement.End, -40, true)
    ]
    public async Task BitNavPanelSwipeShouldDragTheDrawerTowardsTheEdgeItIsDockedToOnly(BitDir dir, BitPlacement? placement, int diffX, bool follows)
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Dir, dir);
            parameters.Add(p => p.Placement, placement);
            parameters.Add(p => p.IsOpen, true);
        });

        var trap = component.FindComponent<BitSwipeTrap>();

        // The drawer follows the finger towards the edge it came from, and stays put when dragged away from it.
        await component.InvokeAsync(() => trap.Instance._OnMove(0, 0, diffX, 0, 0, 0));

        var style = component.Find(".bit-npn").GetAttribute("style") ?? string.Empty;

        Assert.AreEqual(follows, style.Contains($"transform: translateX({diffX}px)"), style);

        // Letting go without a trigger puts it back.
        await component.InvokeAsync(() => trap.Instance._OnEnd(0, 0, diffX, 0, 0, 0));

        Assert.IsFalse((component.Find(".bit-npn").GetAttribute("style") ?? string.Empty).Contains("translateX"));
    }

    [TestMethod,
        DataRow(BitDir.Ltr, null, 100, false),
        DataRow(BitDir.Ltr, null, -100, true),
        DataRow(BitDir.Ltr, BitPlacement.End, 100, true),
        DataRow(BitDir.Rtl, BitPlacement.End, -100, true),
        DataRow(BitDir.Rtl, BitPlacement.End, 100, false)
    ]
    public async Task BitNavPanelSwipeShouldCloseTheDrawerTowardsTheEdgeItIsDockedTo(BitDir dir, BitPlacement? placement, int diffX, bool closes)
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Dir, dir);
            parameters.Add(p => p.Placement, placement);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        var trap = component.FindComponent<BitSwipeTrap>();

        await component.InvokeAsync(() => trap.Instance._OnTrigger(diffX, 0, 1, 0));

        Assert.AreEqual(closes, isOpen is false);
        // A named direction is never read off the browser.
        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Utils.isRtl"));
    }

    [TestMethod]
    public async Task BitNavPanelSwipeShouldLeaveAClosedPanelAlone()
    {
        var reported = 0;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Dir, BitDir.Ltr);
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.IsOpenChanged, (bool _) => reported++);
        });

        var trap = component.FindComponent<BitSwipeTrap>();

        await component.InvokeAsync(() => trap.Instance._OnMove(0, 0, -40, 0, 0, 0));
        await component.InvokeAsync(() => trap.Instance._OnTrigger(-100, 0, 1, 0));

        Assert.IsFalse((component.Find(".bit-npn").GetAttribute("style") ?? string.Empty).Contains("translateX"));
        Assert.AreEqual(0, reported);
    }

    [TestMethod]
    public void BitNavPanelNoSwipeShouldNotRenderTheSwipeTrap()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
        });

        // The gesture is trapped by default, and the trap is what the content sits in.
        Assert.IsTrue(component.Find(".bit-npn-swp").ClassList.Contains("bit-stp"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.NoSwipe, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-stp").Count);
        Assert.AreEqual(1, component.FindAll(".bit-npn-swp").Count);
        Assert.AreEqual(1, component.FindAll(".bit-npn-cnt").Count);
    }

    [TestMethod]
    public void BitNavPanelSearchShouldRequireEveryWordOfTheTerm()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
        });

        // Both words are somewhere in the same item, so it stays.
        component.Find(".bit-srb-inp").Change("home page");
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count));

        // Each word is in an item of its own, and neither item holds both.
        component.Find(".bit-srb-inp").Change("home settings");
        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, component.FindAll(".bit-nav-ict").Count);
            Assert.AreEqual("Nothing found!", component.Find(".bit-txt").TextContent.Trim());
        });
    }

    [TestMethod]
    public void BitNavPanelSearchFilterShouldAlsoSeeEveryWordOfTheTerm()
    {
        List<string> terms = [];

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.SearchDebounceTime, 0);
            parameters.Add(p => p.SearchFilter, (BitNavItem item, string term) =>
            {
                terms.Add(term);
                return item.Text.Contains(term, StringComparison.OrdinalIgnoreCase);
            });
        });

        component.Find(".bit-srb-inp").Change("set ting");

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(terms.Contains("set"));
            Assert.IsTrue(terms.Contains("ting"));
            // "Settings" holds both of them, and it is the only item that does.
            Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count);
        });
    }

    [TestMethod]
    public void BitNavPanelShouldNotSeedASelectionOfItsOwnInAutomaticMode()
    {
        var items = TreeItems();
        BitNavItem? selected = null;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.DefaultSelectedItem, items[0]);
            parameters.Bind(p => p.SelectedItem, selected, v => selected = v);
        });

        // The current URL matches none of the items, and the automatic mode has nothing else to go on.
        component.WaitForAssertion(() =>
        {
            Assert.IsNull(selected);
            Assert.AreEqual(0, component.FindAll(".bit-nav-sel").Count);
        });
    }

    [TestMethod]
    public void BitNavPanelShouldReportWhetherAnItemIsExpanded()
    {
        var items = TreeItems();

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        Assert.IsFalse(component.Instance.IsItemExpanded(items[1]));

        component.InvokeAsync(() => component.Instance.ExpandItem(items[1]));

        component.WaitForAssertion(() => Assert.IsTrue(component.Instance.IsItemExpanded(items[1])));
    }

    [TestMethod]
    public void BitNavPanelExpandOnHoverShouldLeaveTheRailWhileTheKeyboardIsInside()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsToggled, true);
            parameters.Add(p => p.ExpandOnHover, true);
        });

        component.Find(".bit-npn").FocusIn();

        component.WaitForAssertion(() => Assert.IsFalse(component.Find(".bit-npn").ClassList.Contains("bit-npn-tgl")));

        component.Find(".bit-npn").FocusOut();

        // The collapse waits out the gap between the focus leaving one item and landing on the next.
        component.WaitForAssertion(
            () => Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains("bit-npn-tgl")),
            TimeSpan.FromSeconds(3));
    }

    [TestMethod]
    public void BitNavPanelShouldReportItselfAsADialogWhileItCoversThePage()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AriaLabel, "Main menu");
        });

        // On a wide screen the panel is a column of the page, and a column is no dialog.
        Assert.IsNull(component.Find(".bit-npn").GetAttribute("role"));

        SetDrawerScreen(component, true);

        component.WaitForAssertion(() =>
        {
            var root = component.Find(".bit-npn");

            Assert.AreEqual("dialog", root.GetAttribute("role"));
            Assert.AreEqual("true", root.GetAttribute("aria-modal"));
            Assert.AreEqual("Main menu", root.GetAttribute("aria-label"));
        });

        SetDrawerScreen(component, false);

        component.WaitForAssertion(() => Assert.IsNull(component.Find(".bit-npn").GetAttribute("role")));
    }

    [TestMethod]
    public void BitNavPanelClosedDrawerShouldNotReportItselfAsADialog()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
        });

        SetDrawerScreen(component, true);

        component.WaitForAssertion(() => Assert.IsNull(component.Find(".bit-npn").GetAttribute("role")));
    }

    [TestMethod]
    public void BitNavPanelDrawerWithoutOverlayShouldNotReportItselfAsADialog()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoOverlay, true);
        });

        SetDrawerScreen(component, true);

        // Nothing is covered by a drawer that draws no overlay, so nothing behind it is inert either.
        component.WaitForAssertion(() => Assert.IsNull(component.Find(".bit-npn").GetAttribute("role")));
    }

    [TestMethod]
    public void BitNavPanelDrawerShouldFallBackToABuiltInNameWhenTheNavHasNone()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
        });

        SetDrawerScreen(component, true);

        component.WaitForAssertion(() => Assert.AreEqual("Navigation", component.Find(".bit-npn").GetAttribute("aria-label")));
    }

    [TestMethod]
    public void BitNavPanelStickyEndsShouldHandTheOverflowToTheNav()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
        });

        Assert.IsFalse(component.Find(".bit-npn").ClassList.Contains("bit-npn-ste"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.StickyEnds, true);
        });

        Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains("bit-npn-ste"));
    }

    [TestMethod]
    public void BitNavPanelDrawerWithoutAFocusTrapShouldNotClaimToBeModal()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoFocusTrap, true);
        });

        SetDrawerScreen(component, true);

        // It is still the dialog it looks like, but nothing behind it is inert while the keyboard can walk out.
        component.WaitForAssertion(() =>
        {
            var root = component.Find(".bit-npn");

            Assert.AreEqual("dialog", root.GetAttribute("role"));
            Assert.IsNull(root.GetAttribute("aria-modal"));
        });
    }

    [TestMethod]
    public void BitNavPanelCloseButtonShouldCloseThePanel()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        var close = component.Find(".bit-npn-cbn");

        Assert.AreEqual("Close the navigation panel", close.GetAttribute("aria-label"));

        close.Click();

        component.WaitForAssertion(() => Assert.IsFalse(isOpen));
    }

    [TestMethod]
    public void BitNavPanelShouldNotRenderTheCloseButtonByDefault()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
        });

        Assert.AreEqual(0, component.FindAll(".bit-npn-cbn").Count);
    }

    [TestMethod]
    public void BitNavPanelCloseButtonShouldAcceptItsOwnNameAndIcon()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseAriaLabel, "Dismiss the menu");
            parameters.Add(p => p.CloseIconName, "ChromeClose");
        });

        var close = component.Find(".bit-npn-cbn");

        Assert.AreEqual("Dismiss the menu", close.GetAttribute("aria-label"));
        Assert.AreEqual("Dismiss the menu", close.GetAttribute("title"));
        Assert.IsTrue(component.Find(".bit-npn-cbn i").ClassList.Contains("bit-icon--ChromeClose"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseIcon, BitIconInfo.Fa("solid xmark"));
        });

        Assert.IsTrue(component.Find(".bit-npn-cbn i").ClassList.Contains("fa-xmark"));
    }

    [TestMethod]
    public void BitNavPanelDrawerShouldHoldThePageAndTheFocusWhileItCoversThem()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        var rootId = component.Find(".bit-npn").Id;

        // A column of the page holds nothing.
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);

        SetDrawerScreen(component, true);

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
            Assert.AreEqual(rootId, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Single().Arguments[0]);
        });

        component.Find(".bit-npn-ovl").Click();

        component.WaitForAssertion(() =>
        {
            Assert.IsFalse(isOpen);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusTrap"].Count);
        });
    }

    [TestMethod]
    public void BitNavPanelDrawerShouldRespectTheOptOutsOfWhatItHolds()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoScrollLock, true);
            parameters.Add(p => p.NoFocusTrap, true);
        });

        SetDrawerScreen(component, true);

        component.WaitForAssertion(() => Assert.AreEqual("dialog", component.Find(".bit-npn").GetAttribute("role")));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);
    }

    [TestMethod]
    public void BitNavPanelAutoFocusDrawerShouldHandTheFocusBackOnTheWayOut()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.AutoFocus, true);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        SetDrawerScreen(component, true);

        component.WaitForAssertion(
            () => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.captureFocusOrigin"].Count));

        component.Find(".bit-npn-ovl").Click();

        component.WaitForAssertion(() =>
        {
            Assert.IsFalse(isOpen);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.restoreFocusOrigin"].Count);
        });
    }

    [TestMethod]
    public void BitNavPanelDrawerWithoutAFocusTrapOrAutoFocusShouldRecordNoFocusToHandBack()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoFocusTrap, true);
        });

        SetDrawerScreen(component, true);

        component.WaitForAssertion(() => Assert.AreEqual("dialog", component.Find(".bit-npn").GetAttribute("role")));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.captureFocusOrigin"].Count);
    }

    [TestMethod]
    public void BitNavPanelModalDrawerShouldTakeTheFocusOntoItselfAndHandItBack()
    {
        var isOpen = false;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        // Closed, the drawer is no stop of the keyboard at all.
        Assert.IsNull(component.Find(".bit-npn").GetAttribute("tabindex"));

        SetDrawerScreen(component, true);

        component.Render(parameters => parameters.Bind(p => p.IsOpen, true, v => isOpen = v));

        component.WaitForAssertion(() =>
        {
            var root = component.Find(".bit-npn");

            Assert.AreEqual("-1", root.GetAttribute("tabindex"));
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.captureFocusOrigin"].Count);

            // The drawer itself, not the search box: focusing an input would open the on-screen keyboard.
            var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();
            Assert.AreEqual(1, focused.Count);
            Assert.AreNotEqual(component.FindComponent<BitSearchBox>().Instance.InputElement.Id, ((ElementReference)focused[0].Arguments[0]!).Id);
        });

        component.Find(".bit-npn-ovl").Click();

        component.WaitForAssertion(() =>
        {
            Assert.IsFalse(isOpen);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.restoreFocusOrigin"].Count);
        });
    }

    [TestMethod]
    public void BitNavPanelDrawerWithoutAFocusTrapShouldLeaveTheFocusWhereItIs()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.NoFocusTrap, true);
        });

        SetDrawerScreen(component, true);

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        component.WaitForAssertion(() => Assert.AreEqual("dialog", component.Find(".bit-npn").GetAttribute("role")));

        Assert.IsNull(component.Find(".bit-npn").GetAttribute("tabindex"));
        Assert.AreEqual(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    [DataRow(null, "bit-npn-bmd", BitScreenQuery.LtMd)]
    [DataRow(BitNavPanelBreakpoint.Md, "bit-npn-bmd", BitScreenQuery.LtMd)]
    [DataRow(BitNavPanelBreakpoint.Sm, "bit-npn-bsm", BitScreenQuery.LtSm)]
    [DataRow(BitNavPanelBreakpoint.Lg, "bit-npn-blg", BitScreenQuery.LtLg)]
    [DataRow(BitNavPanelBreakpoint.Xl, "bit-npn-bxl", BitScreenQuery.LtXl)]
    public void BitNavPanelDrawerBreakpointShouldPickTheScreensTheDrawerIsOn(BitNavPanelBreakpoint? breakpoint, string cssClass, BitScreenQuery query)
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.DrawerBreakpoint, breakpoint);
        });

        // Until the browser has reported the screen, the stylesheet splits the screens at the breakpoint itself.
        Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains(cssClass));
        // The overlay is shown on the same screens as the drawer, so it carries the same class.
        Assert.IsTrue(component.Find(".bit-npn-ovl").ClassList.Contains(cssClass));
        Assert.AreEqual(query, component.FindComponent<BitMediaQuery>().Instance.ScreenQuery);

        // Once it has, the shape it reported is the class, so the stylesheet follows the live theme breakpoints
        // the media query is built from, exactly like the dialog role and the scroll lock do.
        SetDrawerScreen(component, true);

        component.WaitForAssertion(() =>
        {
            var root = component.Find(".bit-npn");

            Assert.IsTrue(root.ClassList.Contains("bit-npn-drw"));
            Assert.IsFalse(root.ClassList.Contains(cssClass));
            Assert.IsTrue(component.Find(".bit-npn-ovl").ClassList.Contains("bit-npn-drw"));
        });

        SetDrawerScreen(component, false);

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains("bit-npn-col"));
            // A column has no overlay at all.
            Assert.AreEqual(0, component.FindAll(".bit-npn-ovl").Count);
        });
    }

    [TestMethod]
    public void BitNavPanelAlwaysDrawerShouldBeADrawerWithoutAskingTheScreen()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.DrawerBreakpoint, BitNavPanelBreakpoint.Always);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains("bit-npn-drw"));
        Assert.AreEqual(0, component.FindComponents<BitMediaQuery>().Count);

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual("dialog", component.Find(".bit-npn").GetAttribute("role"));
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
        });

        component.Find(".bit-npn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() => Assert.IsFalse(isOpen));
    }

    [TestMethod]
    public void BitNavPanelNeverDrawerShouldStayAColumn()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.DrawerBreakpoint, BitNavPanelBreakpoint.Never);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        var root = component.Find(".bit-npn");

        Assert.IsTrue(root.ClassList.Contains("bit-npn-col"));
        Assert.AreEqual(0, component.FindAll(".bit-npn-ovl").Count);
        Assert.AreEqual(0, component.FindComponents<BitMediaQuery>().Count);
        Assert.IsNull(root.GetAttribute("role"));

        // A column is not dismissed by the key, and holds neither the page nor the focus.
        root.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
    }

    [TestMethod]
    public void BitNavPanelHeaderTextShouldNameTheLogoLinkAndKeepTheButtons()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.AriaLabel, "Main menu");
            parameters.Add(p => p.IconUrl, "/logo.png");
            parameters.Add(p => p.IconNavUrl, "/");
            parameters.Add(p => p.HeaderText, "bit platform");
        });

        var link = component.Find(".bit-npn-hdr a.bit-npn-hln");

        // The visible title is the name of the link, so nothing else is put in its place.
        Assert.IsNull(link.GetAttribute("aria-label"));
        Assert.AreEqual("bit platform", link.QuerySelector(".bit-npn-htx")!.TextContent);
        Assert.AreEqual(string.Empty, link.QuerySelector("img")!.GetAttribute("alt"));
        Assert.AreEqual(1, component.FindAll(".bit-npn-tbn").Count);
    }

    [TestMethod]
    public void BitNavPanelHeaderTextWithoutALinkShouldLeaveTheLogoUnnamed()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.AriaLabel, "Main menu");
            parameters.Add(p => p.IconUrl, "/logo.png");
            parameters.Add(p => p.HeaderText, "bit platform");
        });

        Assert.AreEqual(0, component.FindAll(".bit-npn-hdr a").Count);
        Assert.AreEqual(string.Empty, component.Find(".bit-npn-img").GetAttribute("alt"));
        Assert.AreEqual("bit platform", component.Find(".bit-npn-htx").TextContent);
    }

    [TestMethod]
    public void BitNavPanelRailShouldHideTheLogoLinkItself()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IconUrl, "/logo.png");
            parameters.Add(p => p.IconNavUrl, "/");
            parameters.Add(p => p.HeaderText, "bit platform");
            parameters.Add(p => p.IsToggled, true);
        });

        // A link left on screen with nothing visible in it would still be a tab stop of the rail.
        StringAssert.Contains(component.Find(".bit-npn-hln").GetAttribute("style"), "display:none");
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheBaseParametersOnItsRoot()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Id, "the-panel");
            parameters.Add(p => p.Class, "own-class");
            parameters.Add(p => p.Style, "margin:1px");
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-npn");

        Assert.AreEqual("the-panel", root.Id);
        Assert.IsTrue(root.ClassList.Contains("own-class"));
        // The class is what an engine without :dir() docks the drawer by.
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        // The toggle button points at the id the panel was given.
        Assert.AreEqual("the-panel", component.Find(".bit-npn-tbn").GetAttribute("aria-controls"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")
    ]
    public void BitNavPanelShouldRenderItsVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-npn").GetAttribute("style") ?? string.Empty;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility:hidden") || style.Contains("display:none"), style);
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod]
    public void BitNavPanelShouldSplatTheAttributesItWasGivenOnItsRoot()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitNavPanel<BitNavItem>>(0);
            builder.AddAttribute(1, nameof(BitNavPanel<BitNavItem>.Items), Items);
            builder.AddAttribute(2, "data-test", "panel");
            builder.AddAttribute(3, "title", "The menu");
            builder.CloseComponent();
        }).FindComponent<BitNavPanel<BitNavItem>>();

        var root = component.Find(".bit-npn");

        Assert.AreEqual("panel", root.GetAttribute("data-test"));
        Assert.AreEqual("The menu", root.GetAttribute("title"));
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-nav-apri"),
        DataRow(BitColor.Secondary, "bit-nav-asec"),
        DataRow(BitColor.Tertiary, "bit-nav-ater"),
        DataRow(BitColor.Info, "bit-nav-ainf"),
        DataRow(BitColor.Success, "bit-nav-asuc"),
        DataRow(BitColor.Warning, "bit-nav-awrn"),
        DataRow(BitColor.SevereWarning, "bit-nav-aswr"),
        DataRow(BitColor.Error, "bit-nav-aerr"),
        DataRow(BitColor.PrimaryBackground, "bit-nav-apbg"),
        DataRow(BitColor.SecondaryBackground, "bit-nav-asbg"),
        DataRow(BitColor.TertiaryBackground, "bit-nav-atbg"),
        DataRow(BitColor.PrimaryForeground, "bit-nav-apfg"),
        DataRow(BitColor.SecondaryForeground, "bit-nav-asfg"),
        DataRow(BitColor.TertiaryForeground, "bit-nav-atfg"),
        DataRow(BitColor.PrimaryBorder, "bit-nav-apbr"),
        DataRow(BitColor.SecondaryBorder, "bit-nav-asbr"),
        DataRow(BitColor.TertiaryBorder, "bit-nav-atbr")
    ]
    public void BitNavPanelShouldPaintTheAccentOnItsRootAndItsNav(BitColor accent, string cssClass)
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Accent, accent);
        });

        Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains(cssClass));
        Assert.AreEqual(accent, component.FindComponent<BitNav<BitNavItem>>().Instance.Accent);
    }

    [TestMethod]
    public void BitNavPanelShouldPublishNoAccentClassByDefault()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
        });

        Assert.IsFalse(component.Find(".bit-npn").ClassList.Any(c => c.StartsWith("bit-nav-a", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void BitNavPanelShouldHandItsNavParametersToTheNav()
    {
        var selectors = new BitNavNameSelectors<BitNavItem>();
        var navClasses = new BitNavClassStyles { Root = "nav-classes" };
        var navStyles = new BitNavClassStyles { Root = "color:red" };
        RenderFragment<BitNavItem> headerTemplate = _ => builder => builder.AddMarkupContent(0, "<span class='hdr-tpl'></span>");
        RenderFragment<BitNavItem> itemTemplate = _ => builder => builder.AddMarkupContent(0, "<span class='itm-tpl'></span>");

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, TreeItems());
            parameters.Add(p => p.Color, BitColor.Success);
            parameters.Add(p => p.CollapseAriaLabel, "Fold");
            parameters.Add(p => p.ExpandAriaLabel, "Unfold");
            parameters.Add(p => p.HeaderTemplate, headerTemplate);
            parameters.Add(p => p.HeaderTemplateRenderMode, BitNavItemTemplateRenderMode.Replace);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            parameters.Add(p => p.ItemTemplateRenderMode, BitNavItemTemplateRenderMode.Replace);
            parameters.Add(p => p.IndentPadding, 30);
            parameters.Add(p => p.IndentReversedPadding, 6);
            parameters.Add(p => p.IndentValue, 20);
            parameters.Add(p => p.NameSelectors, selectors);
            parameters.Add(p => p.NavClasses, navClasses);
            parameters.Add(p => p.NavStyles, navStyles);
            parameters.Add(p => p.NavMatch, BitNavMatch.Prefix);
            parameters.Add(p => p.NavMode, BitNavMode.Manual);
            parameters.Add(p => p.NoCollapse, true);
            parameters.Add(p => p.RenderType, BitNavRenderType.Grouped);
            parameters.Add(p => p.Reselectable, true);
            parameters.Add(p => p.ReversedChevron, true);
            parameters.Add(p => p.SingleExpand, true);
            parameters.Add(p => p.Size, BitSize.Large);
        });

        var nav = component.FindComponent<BitNav<BitNavItem>>().Instance;

        Assert.AreEqual(BitColor.Success, nav.Color);
        Assert.AreEqual("Fold", nav.CollapseAriaLabel);
        Assert.AreEqual("Unfold", nav.ExpandAriaLabel);
        Assert.AreSame(headerTemplate, nav.HeaderTemplate);
        Assert.AreEqual(BitNavItemTemplateRenderMode.Replace, nav.HeaderTemplateRenderMode);
        Assert.AreSame(itemTemplate, nav.ItemTemplate);
        Assert.AreEqual(BitNavItemTemplateRenderMode.Replace, nav.ItemTemplateRenderMode);
        Assert.AreEqual(30, nav.IndentPadding);
        Assert.AreEqual(6, nav.IndentReversedPadding);
        Assert.AreEqual(20, nav.IndentValue);
        Assert.AreSame(selectors, nav.NameSelectors);
        Assert.AreSame(navClasses, nav.Classes);
        Assert.AreSame(navStyles, nav.Styles);
        Assert.AreEqual(BitNavMatch.Prefix, nav.Match);
        Assert.AreEqual(BitNavMode.Manual, nav.Mode);
        Assert.IsTrue(nav.NoCollapse);
        Assert.AreEqual(BitNavRenderType.Grouped, nav.RenderType);
        Assert.IsTrue(nav.Reselectable);
        Assert.IsTrue(nav.ReversedChevron);
        Assert.IsTrue(nav.SingleExpand);
        Assert.AreEqual(BitSize.Large, nav.Size);
        // The nav always fills the panel.
        Assert.IsTrue(nav.FullWidth);

        Assert.IsTrue(component.Find("nav.bit-nav").ClassList.Contains("nav-classes"));
        StringAssert.Contains(component.Find("nav.bit-nav").GetAttribute("style"), "color:red");
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheItemTemplateInEveryItem()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.ItemTemplate, (BitNavItem item) => builder => builder.AddMarkupContent(0, $"<span class='itm-tpl'>{item.Text}!</span>"));
        });

        var templates = component.FindAll(".bit-nav-ict .itm-tpl");

        Assert.AreEqual(2, templates.Count);
        Assert.AreEqual("Home!", templates[0].TextContent);
    }

    [TestMethod]
    public void BitNavPanelShouldHandItsSearchBoxParametersToTheSearchBox()
    {
        var classes = new BitSearchBoxClassStyles { Root = "srb-classes" };
        var styles = new BitSearchBoxClassStyles { Root = "color:red" };

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Color, BitColor.Warning);
            parameters.Add(p => p.SearchBoxClasses, classes);
            parameters.Add(p => p.SearchBoxStyles, styles);
            parameters.Add(p => p.SearchBoxPlaceholder, "Find a page");
            parameters.Add(p => p.SearchDebounceTime, 42);
        });

        var searchBox = component.FindComponent<BitSearchBox>().Instance;

        Assert.AreSame(classes, searchBox.Classes);
        Assert.AreSame(styles, searchBox.Styles);
        Assert.AreEqual(BitColor.Warning, searchBox.Color);
        Assert.AreEqual(42, searchBox.DebounceTime);
        Assert.IsTrue(searchBox.Immediate);
        Assert.IsTrue(searchBox.Underlined);

        var input = component.Find(".bit-srb-inp");

        // The placeholder is the name of the box too, so it is never announced as a bare "edit text".
        Assert.AreEqual("Find a page", input.GetAttribute("placeholder"));
        Assert.AreEqual("Find a page", input.GetAttribute("aria-label"));
        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains("srb-classes"));
    }

    [TestMethod]
    public void BitNavPanelSearchBoxShouldFallBackToABuiltInName()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
        });

        Assert.AreEqual("Search", component.Find(".bit-srb-inp").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavPanelShouldPassItsColorToItsButtons()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.ShowCloseButton, true);
        });

        Assert.IsTrue(component.Find(".bit-npn-tbn").ClassList.Contains("bit-btn-err"));
        Assert.IsTrue(component.Find(".bit-npn-cbn").ClassList.Contains("bit-btn-err"));

        component.Render(parameters => parameters.Add(p => p.IsToggled, true));

        Assert.IsTrue(component.Find(".bit-npn-tsb").ClassList.Contains("bit-btn-err"));
    }

    [TestMethod]
    public void BitNavPanelShouldReportItemTogglesAndSelections()
    {
        var items = TreeItems();
        BitNavItem? toggled = null;
        BitNavItem? selected = null;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NavMode, BitNavMode.Manual);
            parameters.Add(p => p.OnItemToggle, (BitNavItem item) => toggled = item);
            parameters.Add(p => p.OnSelectItem, (BitNavItem item) => selected = item);
        });

        component.FindAll(".bit-nav-ict")[1].Click();

        component.WaitForAssertion(() => Assert.AreSame(items[1], toggled));

        component.FindAll(".bit-nav-ict")[0].Click();

        component.WaitForAssertion(() => Assert.AreSame(items[0], selected));
    }

    [TestMethod]
    public void BitNavPanelShouldApplyItsClassesAndStylesToEveryPart()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IconUrl, "/logo.png");
            parameters.Add(p => p.HeaderText, "bit");
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.Classes, new BitNavPanelClassStyles
            {
                Root = "c-root",
                Overlay = "c-overlay",
                Container = "c-container",
                Header = "c-header",
                HeaderIcon = "c-icon",
                HeaderText = "c-text",
                ToggleButton = "c-toggle",
                CloseButton = "c-close",
                SearchBox = "c-search",
                Nav = "c-nav",
            });
            parameters.Add(p => p.Styles, new BitNavPanelClassStyles
            {
                Root = "color:red",
                Overlay = "color:orange",
                Container = "color:yellow",
                Header = "color:green",
                HeaderIcon = "color:blue",
                HeaderText = "color:indigo",
                ToggleButton = "color:violet",
                CloseButton = "color:pink",
                SearchBox = "color:brown",
                Nav = "color:gray",
            });
        });

        void AssertPart(string selector, string cssClass, string style)
        {
            var element = component.Find(selector);

            Assert.IsTrue(element.ClassList.Contains(cssClass), selector);
            StringAssert.Contains(element.GetAttribute("style"), style, selector);
        }

        AssertPart(".bit-npn", "c-root", "color:red");
        AssertPart(".bit-npn-ovl", "c-overlay", "color:orange");
        AssertPart(".bit-npn-cnt", "c-container", "color:yellow");
        AssertPart(".bit-npn-hdr", "c-header", "color:green");
        AssertPart(".bit-npn-img", "c-icon", "color:blue");
        AssertPart(".bit-npn-htx", "c-text", "color:indigo");
        AssertPart(".bit-npn-tbn", "c-toggle", "color:violet");
        AssertPart(".bit-npn-cbn", "c-close", "color:pink");
        AssertPart(".bit-srb", "c-search", "color:brown");
        AssertPart("nav.bit-nav", "c-nav", "color:gray");
    }

    [TestMethod]
    public void BitNavPanelShouldApplyItsToggledAndSearchButtonClassesAndStyles()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsToggled, true);
            parameters.Add(p => p.Classes, new BitNavPanelClassStyles { Toggled = "c-toggled", ToggleSearchButton = "c-tsb" });
            parameters.Add(p => p.Styles, new BitNavPanelClassStyles { Toggled = "color:red", ToggleSearchButton = "color:blue" });
        });

        var root = component.Find(".bit-npn");

        Assert.IsTrue(root.ClassList.Contains("c-toggled"));
        StringAssert.Contains(root.GetAttribute("style"), "color:red");

        var searchButton = component.Find(".bit-npn-tsb");

        Assert.IsTrue(searchButton.ClassList.Contains("c-tsb"));
        StringAssert.Contains(searchButton.GetAttribute("style"), "color:blue");

        // The rail is left, and the toggled look with it.
        component.Render(parameters => parameters.Add(p => p.IsToggled, false));

        Assert.IsFalse(component.Find(".bit-npn").ClassList.Contains("c-toggled"));
        Assert.IsFalse((component.Find(".bit-npn").GetAttribute("style") ?? string.Empty).Contains("color:red"));
    }

    [TestMethod]
    public void BitNavPanelShouldApplyItsEmptyListMessageClassesAndStyles()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.SearchText, "zzz");
            parameters.Add(p => p.Classes, new BitNavPanelClassStyles { EmptyListMessage = "c-empty" });
            parameters.Add(p => p.Styles, new BitNavPanelClassStyles { EmptyListMessage = "color:red" });
        });

        component.WaitForAssertion(() =>
        {
            var message = component.Find(".bit-txt");

            Assert.AreEqual("Nothing found!", message.TextContent.Trim());
            Assert.IsTrue(message.ClassList.Contains("c-empty"));
            StringAssert.Contains(message.GetAttribute("style"), "color:red");
        });
    }

    [TestMethod]
    public void BitNavPanelRailShouldHideTheEmptyMessageAndTheSearchBox()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.SearchText, "zzz");
            parameters.Add(p => p.IsToggled, true);
        });

        // The rail has no room for either; the search box stays rendered but hidden, so it keeps its state.
        Assert.AreEqual(0, component.FindAll(".bit-txt").Count);
        StringAssert.Contains(component.Find(".bit-srb").GetAttribute("style"), "display:none");
    }

    [TestMethod]
    public void BitNavPanelFocusSearchBoxShouldLeaveTheRailAndFocusTheBox()
    {
        var isToggled = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Bind(p => p.IsToggled, isToggled, v => isToggled = v);
        });

        component.InvokeAsync(() => component.Instance.FocusSearchBox());

        component.WaitForAssertion(() => Assert.IsFalse(isToggled));

        // The search box only exists once the panel has left the rail, so the focus is moved in the render that
        // brings it back rather than after a guessed delay.
        component.Render(parameters => parameters.Bind(p => p.IsToggled, isToggled, v => isToggled = v));

        component.WaitForAssertion(() =>
        {
            Assert.IsFalse(component.Find(".bit-npn").ClassList.Contains("bit-npn-tgl"));
            Assert.AreEqual(1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
        });
    }

    [TestMethod]
    public async Task BitNavPanelFocusSearchBoxShouldDoNothingWithoutASearchBox()
    {
        var isToggled = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.NoSearchBox, true);
            parameters.Bind(p => p.IsToggled, isToggled, v => isToggled = v);
        });

        await component.InvokeAsync(() => component.Instance.FocusSearchBox());

        Assert.IsTrue(isToggled);
        Assert.AreEqual(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public async Task BitNavPanelShouldHandBackWhatItHoldsWhenDisposedOpen()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
        });

        SetDrawerScreen(component, true);

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.captureFocusOrigin"].Count);
        });

        // A drawer disposed while it is open would otherwise leave the page held by a key nothing releases again.
        await component.Instance.DisposeAsync();

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count);
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusTrap"].Count);
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusOrigin"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.restoreFocusOrigin"].Count);
    }

    [TestMethod]
    public async Task BitNavPanelShouldDisposeWithoutHavingHeldAnything()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
        });

        await component.Instance.DisposeAsync();

        // Only what was taken is handed back.
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusTrap"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusOrigin"].Count);
    }

    [TestMethod]
    public void BitNavPanelDrawerShouldHoldTheScrollerOfTheAppShellItSitsIn()
    {
        var scroller = new ElementReference("the-shell-scroller");

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<CascadingValue<ElementReference?>>(0);
            builder.AddAttribute(1, "Name", "BitAppShell.Container");
            builder.AddAttribute(2, "Value", (ElementReference?)scroller);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<BitNavPanel<BitNavItem>>(0);
                b.AddAttribute(1, nameof(BitNavPanel<BitNavItem>.Items), Items);
                b.AddAttribute(2, nameof(BitNavPanel<BitNavItem>.IsOpen), true);
                b.CloseComponent();
            }));
            builder.CloseComponent();
        }).FindComponent<BitNavPanel<BitNavItem>>();

        SetDrawerScreen(component, true);

        // What the drawer holds is the region the shell scrolls rather than the document.
        component.WaitForAssertion(() =>
        {
            var lockScroll = Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Single();

            Assert.AreEqual(component.Find(".bit-npn").Id, lockScroll.Arguments[0]);
            Assert.AreEqual(scroller.Id, ((ElementReference)lockScroll.Arguments[1]!).Id);
        });
    }

    [TestMethod]
    public void BitNavPanelShouldRenderTheToggleIconOfAnIconName()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.ToggleIconName, "GlobalNavButton");
        });

        Assert.IsTrue(component.Find(".bit-npn-tbn i").ClassList.Contains("bit-icon--GlobalNavButton"));
        Assert.AreEqual(0, component.FindAll(".bit-icon-ex--ColumnRightTwoThirds").Count);

        // Without one, the glyph ships with the Extras package rather than the icon font.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.ToggleIconName, null);
        });

        Assert.AreEqual(1, component.FindAll(".bit-npn-tbn .bit-icon-ex--ColumnRightTwoThirds").Count);
    }

    [TestMethod]
    public void BitNavPanelToggleButtonShouldFlipItsNameWithTheState()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
        });

        Assert.AreEqual("Collapse the navigation panel", component.Find(".bit-npn-tbn").GetAttribute("aria-label"));

        component.Render(parameters => parameters.Add(p => p.IsToggled, true));

        Assert.AreEqual("Expand the navigation panel", component.Find(".bit-npn-tbn").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavPanelHeaderShouldRenderNothingOfTheLogoWithoutOne()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IconNavUrl, "/");
        });

        // A link with nothing in it would be a tab stop leading nowhere visible.
        Assert.AreEqual(0, component.FindAll(".bit-npn-hdr a").Count);
        Assert.AreEqual(0, component.FindAll(".bit-npn-img").Count);
        Assert.AreEqual(1, component.FindAll(".bit-npn-tbn").Count);
    }

    // Drives the media query the panel reads its own shape from, which is the browser's answer in an app and
    // has none of its own in a test.
    private static void SetDrawerScreen(IRenderedComponent<BitNavPanel<BitNavItem>> component, bool isDrawer)
    {
        var mediaQuery = component.FindComponent<BitMediaQuery>();

        component.InvokeAsync(() => mediaQuery.Instance._OnMatchChange(isDrawer).AsTask());
    }
}
