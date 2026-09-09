using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
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
    public void BitNavPanelDisabledOverlayClickShouldNotClose()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-npn-ovl").Click();

        component.WaitForAssertion(() => Assert.IsTrue(isOpen));
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
    public void BitNavPanelNoToggleShouldIgnoreTheToggleMethod()
    {
        var isToggled = false;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.NoToggle, true);
            parameters.Bind(p => p.IsToggled, isToggled, v => isToggled = v);
        });

        component.InvokeAsync(() => component.Instance.Toggle());

        component.WaitForAssertion(() => Assert.IsFalse(isToggled));
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

        // The description of an item.
        component.Find(".bit-srb-inp").Change("documentation page");
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count));

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
    public void BitNavPanelEscapeShouldCloseThePanel()
    {
        var isOpen = true;

        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-npn").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() => Assert.IsFalse(isOpen));
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

        // Nothing is announced while there is no search in place.
        Assert.AreEqual(0, component.FindAll(".bit-npn-lvr").Count);

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
    public void BitNavPanelAutoFocusShouldMoveTheFocusIntoThePanelOnOpen()
    {
        var component = RenderComponent<BitNavPanel<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.AutoFocus, true);
        });

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        component.WaitForAssertion(() =>
        {
            var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();
            Assert.AreEqual(1, focused.Count);
            focused[0].Arguments[0].ShouldBeElementReferenceTo(component.Find(".bit-srb-inp"));
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

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        // A panel without a search box hands the focus to the first item of the nav instead.
        component.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count));
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
}
