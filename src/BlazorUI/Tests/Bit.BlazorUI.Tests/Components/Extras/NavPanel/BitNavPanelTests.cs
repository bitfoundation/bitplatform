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
            parameters.Add(p => p.IsEnabled, false);
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
        Assert.IsNotNull(img.GetAttribute("alt"));
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
    public void BitNavPanelEscapeShouldDoNothingWhenClosedOrDisabled()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-npn").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() => Assert.IsTrue(isOpen));
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
            parameters.Add(p => p.IsEnabled, false);
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

        // A panel without a search box hands the focus to the first item of the nav instead.
        component.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count));
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
    [DataRow(BitNavPanelPosition.Start, false)]
    [DataRow(BitNavPanelPosition.End, true)]
    public void BitNavPanelShouldDockTheDrawerToThePositionItIsGiven(BitNavPanelPosition position, bool isEnd)
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Position, position);
        });

        Assert.AreEqual(isEnd, component.Find(".bit-npn").ClassList.Contains("bit-npn-end"));
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
    public void BitNavPanelWithoutAutoFocusShouldRecordNoFocusToHandBack()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsOpen, true);
        });

        SetDrawerScreen(component, true);

        component.WaitForAssertion(
            () => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.captureFocusOrigin"].Count);
    }

    // Drives the media query the panel reads its own shape from, which is the browser's answer in an app and
    // has none of its own in a test.
    private static void SetDrawerScreen(IRenderedComponent<BitNavPanel<BitNavItem>> component, bool isDrawer)
    {
        var mediaQuery = component.FindComponent<BitMediaQuery>();

        component.InvokeAsync(() => mediaQuery.Instance._OnMatchChange(isDrawer).AsTask());
    }
}
