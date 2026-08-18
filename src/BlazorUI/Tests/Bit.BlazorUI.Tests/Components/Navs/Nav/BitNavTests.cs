using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Nav;

[TestClass]
public class BitNavTests : BunitTestContext
{
    // A custom item type that renames every member the nav reads, so a NameSelectors mapping is the only
    // way the nav can find them. Nothing here matches BitNavItem by convention on purpose.
    private class NavRecord
    {
        public string? Label { get; set; }
        public string? Address { get; set; }
        public string? Note { get; set; }
        public string? Glyph { get; set; }
        public bool Open { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Divider { get; set; }
        public List<NavRecord> Nodes { get; set; } = [];
    }

    // A custom item type whose members carry the names BitNavItem uses, so the default (name based)
    // selectors find them without any mapping beyond handing the nav a NameSelectors instance.
    private class NavConventionItem
    {
        public string? Text { get; set; }
        public string? Url { get; set; }
        public string? IconName { get; set; }
        public bool IsExpanded { get; set; }
        public List<NavConventionItem> ChildItems { get; set; } = [];
    }

    // A custom item type with no NameSelectors at all: the nav can read nothing from it, but it still has
    // to render and to keep the expansion state on its own instead of throwing.
    private class NavOpaqueItem
    {
        public string? Whatever { get; set; }
    }



    private static List<BitNavItem> BasicItems() =>
    [
        new() { Text = "Home", Url = "/home" },
        new() { Text = "Products", Url = "/products" },
        new() { Text = "Contact", Url = "/contact" },
    ];

    private static List<BitNavItem> TreeItems() =>
    [
        new()
        {
            Text = "Fruits",
            ChildItems =
            [
                new() { Text = "Apple" },
                new() { Text = "Banana" },
            ]
        },
        new()
        {
            Text = "Drinks",
            ChildItems =
            [
                new() { Text = "Coffee" },
                new() { Text = "Tea" },
            ]
        },
    ];

    // A selected item carrying every part the nav renders (an icon, a description, a chevron and a child),
    // next to a separator, so a single render covers all the class and style parts at once.
    private static List<BitNavItem> PartsItems() =>
    [
        new()
        {
            Text = "Fruits",
            IconName = "Home",
            Description = "some fruits",
            ChildItems = [new() { Text = "Apple" }]
        },
        new() { IsSeparator = true },
    ];

    private static void AddPartsParameters(ComponentParameterCollectionBuilder<BitNav<BitNavItem>> parameters, IList<BitNavItem> items)
    {
        parameters.Add(c => c.AllExpanded, true);
        parameters.Add(c => c.Mode, BitNavMode.Manual);
        parameters.Add(c => c.DefaultSelectedItem, items[0]);
    }

    private IRenderedComponent<BitNav<BitNavItem>> RenderNav(IList<BitNavItem> items, Action<ComponentParameterCollectionBuilder<BitNav<BitNavItem>>>? extra = null)
    {
        return RenderComponent<BitNav<BitNavItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            extra?.Invoke(parameters);
        });
    }

    private void Navigate(string relativeUrl)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(relativeUrl);
    }



    #region rendering

    [TestMethod]
    public void BitNavShouldRenderTheExpectedRootStructure()
    {
        var component = RenderNav(BasicItems());

        var root = component.Find(".bit-nav");

        // The nav is a navigation landmark holding a list, and the list semantics are spelled out with the
        // roles because a list with its markers removed loses them in some browsers.
        Assert.AreEqual("NAV", root.TagName);
        Assert.AreEqual("navigation", root.GetAttribute("role"));
        Assert.AreEqual("list", root.QuerySelector("ul")!.GetAttribute("role"));
        Assert.AreEqual(3, component.FindAll("li[role=listitem]").Count);
    }

    [TestMethod]
    public void BitNavShouldApplyTheDefaultRootClassesWhenNothingIsProvided()
    {
        var component = RenderNav(BasicItems());

        var root = component.Find(".bit-nav");

        Assert.IsTrue(root.ClassList.Contains("bit-nav"));
        Assert.IsTrue(root.ClassList.Contains("bit-nav-md"), "the default size");
        Assert.IsTrue(root.ClassList.Contains("bit-nav-pri"), "the default color");
        Assert.IsTrue(root.ClassList.Contains("bit-nav-apbg"), "the default accent");
        Assert.IsFalse(root.ClassList.Contains("bit-nav-ftw"));
        Assert.IsFalse(root.ClassList.Contains("bit-nav-flw"));
        Assert.IsFalse(root.ClassList.Contains("bit-nav-ion"));
    }

    [TestMethod]
    public void BitNavShouldRespectAriaLabel()
    {
        var component = RenderNav(BasicItems(), p => p.Add(c => c.AriaLabel, "Main navigation"));

        Assert.AreEqual("Main navigation", component.Find(".bit-nav").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavShouldRespectId()
    {
        var component = RenderNav(BasicItems(), p => p.Add(c => c.Id, "the-nav"));

        Assert.AreEqual("the-nav", component.Find(".bit-nav").GetAttribute("id"));
    }

    [TestMethod]
    public void BitNavShouldSplatTheUnmatchedAttributesOntoItsRoot()
    {
        var component = RenderComponent<BitNavHtmlAttributesTest>(p => p.Add(c => c.Items, BasicItems()));

        var root = component.Find(".bit-nav");

        Assert.AreEqual("the-value", root.GetAttribute("data-test"));
        Assert.AreEqual("the-title", root.GetAttribute("title"));
    }

    [TestMethod]
    public void BitNavShouldRespectStyleAndClass()
    {
        var component = RenderNav(BasicItems(), p =>
        {
            p.Add(c => c.Style, "color: red");
            p.Add(c => c.Class, "the-class");
        });

        var root = component.Find(".bit-nav");

        StringAssert.Contains(root.GetAttribute("style"), "color: red");
        Assert.IsTrue(root.ClassList.Contains("the-class"));
    }

    [TestMethod,
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Auto, "auto")]
    public void BitNavShouldRespectDir(BitDir dir, string expectedDir)
    {
        var component = RenderNav(BasicItems(), p => p.Add(c => c.Dir, dir));

        Assert.AreEqual(expectedDir, component.Find(".bit-nav").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitNavShouldRespectIsEnabled()
    {
        var component = RenderNav(BasicItems(), p => p.Add(c => c.IsEnabled, false));

        Assert.IsTrue(component.Find(".bit-nav").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitNavShouldDisableEveryItemOfADisabledNav()
    {
        var component = RenderNav(BasicItems(), p => p.Add(c => c.IsEnabled, false));

        foreach (var item in component.FindAll(".bit-nav-ict"))
        {
            // Greying the items out is not enough: a link that keeps its href and its tab stop could still
            // be followed with the keyboard.
            Assert.IsTrue(item.ClassList.Contains("bit-nav-dis"));
            Assert.AreEqual("true", item.GetAttribute("aria-disabled"));
            Assert.AreEqual("-1", item.GetAttribute("tabindex"));
            Assert.IsNull(item.GetAttribute("href"));
        }
    }

    [TestMethod]
    public void BitNavShouldNotToggleAnItemOfADisabledNav()
    {
        var component = RenderNav(TreeItems(), p => p.Add(c => c.IsEnabled, false));

        component.FindAll(".bit-nav-cbt")[0].Click();

        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod,
        DataRow(true, false, "bit-nav-ftw"),
        DataRow(false, true, "bit-nav-flw"),
        DataRow(false, false, null)]
    public void BitNavShouldApplyTheWidthClasses(bool fitWidth, bool fullWidth, string? expectedClass)
    {
        var component = RenderNav(BasicItems(), p =>
        {
            p.Add(c => c.FitWidth, fitWidth);
            p.Add(c => c.FullWidth, fullWidth);
        });

        var root = component.Find(".bit-nav");

        // The whole family is asserted, so a class landing on the root by accident is caught as well.
        foreach (var cls in new[] { "bit-nav-ftw", "bit-nav-flw" })
        {
            Assert.AreEqual(cls == expectedClass, root.ClassList.Contains(cls), cls);
        }
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-nav-sm"),
        DataRow(BitSize.Medium, "bit-nav-md"),
        DataRow(BitSize.Large, "bit-nav-lg"),
        DataRow(null, "bit-nav-md")]
    public void BitNavShouldApplyTheSizeClass(BitSize? size, string expectedClass)
    {
        var component = RenderNav(BasicItems(), p => p.Add(c => c.Size, size));

        var root = component.Find(".bit-nav");

        foreach (var cls in new[] { "bit-nav-sm", "bit-nav-md", "bit-nav-lg" })
        {
            Assert.AreEqual(cls == expectedClass, root.ClassList.Contains(cls), cls);
        }
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-nav-pri"),
        DataRow(BitColor.Secondary, "bit-nav-sec"),
        DataRow(BitColor.Tertiary, "bit-nav-ter"),
        DataRow(BitColor.Info, "bit-nav-inf"),
        DataRow(BitColor.Success, "bit-nav-suc"),
        DataRow(BitColor.Warning, "bit-nav-wrn"),
        DataRow(BitColor.SevereWarning, "bit-nav-swr"),
        DataRow(BitColor.Error, "bit-nav-err"),
        DataRow(BitColor.PrimaryBackground, "bit-nav-pbg"),
        DataRow(BitColor.SecondaryBackground, "bit-nav-sbg"),
        DataRow(BitColor.TertiaryBackground, "bit-nav-tbg"),
        DataRow(BitColor.PrimaryForeground, "bit-nav-pfg"),
        DataRow(BitColor.SecondaryForeground, "bit-nav-sfg"),
        DataRow(BitColor.TertiaryForeground, "bit-nav-tfg"),
        DataRow(BitColor.PrimaryBorder, "bit-nav-pbr"),
        DataRow(BitColor.SecondaryBorder, "bit-nav-sbr"),
        DataRow(BitColor.TertiaryBorder, "bit-nav-tbr"),
        DataRow(null, "bit-nav-pri")]
    public void BitNavShouldApplyTheColorClass(BitColor? color, string expectedClass)
    {
        var component = RenderNav(BasicItems(), p => p.Add(c => c.Color, color));

        Assert.IsTrue(component.Find(".bit-nav").ClassList.Contains(expectedClass));
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
        DataRow(BitColor.TertiaryBorder, "bit-nav-atbr"),
        DataRow(null, "bit-nav-apbg")]
    public void BitNavShouldApplyTheAccentClass(BitColor? accent, string expectedClass)
    {
        var component = RenderNav(BasicItems(), p => p.Add(c => c.Accent, accent));

        Assert.IsTrue(component.Find(".bit-nav").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitNavShouldRespectIconOnly()
    {
        var items = new List<BitNavItem>
        {
            new() { Text = "Fruits", Description = "some fruits", ChildItems = [new() { Text = "Apple" }] }
        };

        var component = RenderNav(items, p => p.Add(c => c.IconOnly, true));

        Assert.IsTrue(component.Find(".bit-nav").ClassList.Contains("bit-nav-ion"));
        // IconOnly hides the text through the stylesheet, but the chevron and the description are dropped
        // from the markup entirely, since neither has an icon-sized form.
        Assert.AreEqual(0, component.FindAll(".bit-nav-cbt").Count);
        Assert.AreEqual(0, component.FindAll(".bit-nav-des").Count);
    }

    [TestMethod]
    public void BitNavShouldRenderNothingForAnEmptyItemsCollection()
    {
        var component = RenderNav([]);

        Assert.AreEqual(0, component.FindAll(".bit-nav-ict").Count);
        Assert.IsNotNull(component.Find(".bit-nav ul"));
    }

    #endregion

    #region items

    [TestMethod]
    public void BitNavShouldRenderTheTextAndTheIconOfAnItem()
    {
        var items = new List<BitNavItem> { new() { Text = "Home", IconName = "Home" } };

        var component = RenderNav(items);

        Assert.AreEqual("Home", component.Find(".bit-nav-itx").TextContent);

        var icon = component.Find(".bit-nav-iic");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Home"));
        // The icon repeats the text, so it is hidden from assistive technologies.
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitNavItemIconShouldTakePrecedenceOverIconName()
    {
        var items = new List<BitNavItem>
        {
            new() { Text = "Home", IconName = "Home", Icon = BitIconInfo.Css("fa-solid fa-house") }
        };

        var component = RenderNav(items);

        var icon = component.Find(".bit-nav-iic");
        Assert.IsTrue(icon.ClassList.Contains("fa-house"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--Home"));
    }

    [TestMethod]
    public void BitNavShouldRenderNoIconElementForAnItemWithoutAnIcon()
    {
        var component = RenderNav([new() { Text = "Home" }]);

        Assert.AreEqual(0, component.FindAll(".bit-nav-iic").Count);
    }

    [TestMethod]
    public void BitNavShouldRenderTheDescriptionOfAnItem()
    {
        var component = RenderNav([new() { Text = "Home", Description = "the home page" }]);

        var description = component.Find(".bit-nav-des");
        Assert.AreEqual("the home page", description.TextContent.Trim());
        Assert.AreEqual("the home page", description.GetAttribute("title"));
    }

    [TestMethod,
        DataRow(null, "Home"),
        DataRow("", ""),
        DataRow("a title", "a title")]
    public void BitNavShouldFallBackToTheTextForTheTitleOfAnItem(string? title, string expectedTitle)
    {
        var component = RenderNav([new() { Text = "Home", Title = title }]);

        // An empty Title is a value like any other, so only a null one falls back to the text.
        Assert.AreEqual(expectedTitle, component.Find(".bit-nav-ict").GetAttribute("title"));
    }

    [TestMethod,
        DataRow("/home", false, "A"),
        DataRow(null, true, "A"),
        DataRow(null, false, "BUTTON")]
    public void BitNavShouldRenderAnAnchorOnlyForALinkOrAForcedAnchor(string? url, bool forceAnchor, string expectedTag)
    {
        var component = RenderNav([new() { Text = "Home", Url = url, ForceAnchor = forceAnchor }]);

        Assert.AreEqual(expectedTag, component.Find(".bit-nav-ict").TagName);
    }

    [TestMethod]
    public void BitNavShouldRenderTheHrefOfALinkItem()
    {
        var component = RenderNav([new() { Text = "Home", Url = "/home" }]);

        Assert.AreEqual("/home", component.Find(".bit-nav-ict").GetAttribute("href"));
    }

    [TestMethod]
    public void BitNavShouldKeepTheHrefOfTheSelectedItem()
    {
        var items = BasicItems();

        var component = RenderNav(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.DefaultSelectedItem, items[0]);
        });

        // The selected item stays a link: dropping its href would take it out of the tab order and strip
        // the semantics that aria-current="page" is announced against.
        var selected = component.Find(".bit-nav-sel");
        Assert.AreEqual("A", selected.TagName);
        Assert.AreEqual("/home", selected.GetAttribute("href"));
    }

    [TestMethod]
    public void BitNavShouldMarkADisabledItem()
    {
        var component = RenderNav([new() { Text = "Home", Url = "/home", IsEnabled = false }]);

        var item = component.Find(".bit-nav-ict");

        Assert.IsTrue(item.ClassList.Contains("bit-nav-dis"));
        // An anchor has no disabled attribute, so the disabled state is reported with aria-disabled and the
        // item is taken out of the tab order and stripped of its href instead.
        Assert.AreEqual("true", item.GetAttribute("aria-disabled"));
        Assert.AreEqual("-1", item.GetAttribute("tabindex"));
        Assert.IsNull(item.GetAttribute("href"));
    }

    [TestMethod]
    public void BitNavShouldDisableTheButtonOfADisabledNonLinkItem()
    {
        var component = RenderNav([new() { Text = "Home", IsEnabled = false }]);

        var item = component.Find(".bit-nav-ict");

        Assert.AreEqual("BUTTON", item.TagName);
        Assert.IsTrue(item.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitNavShouldNotSetATabIndexOnAnEnabledItem()
    {
        var component = RenderNav([new() { Text = "Home", Url = "/home" }]);

        // An anchor with an href is focusable on its own, so it must not carry a tabindex of its own; the
        // item without one would not be, hence the explicit 0 there.
        Assert.IsNull(component.Find(".bit-nav-ict").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitNavShouldMakeAForcedAnchorWithoutAnHrefFocusable()
    {
        var component = RenderNav([new() { Text = "Home", ForceAnchor = true }]);

        Assert.AreEqual("0", component.Find(".bit-nav-ict").GetAttribute("tabindex"));
    }

    [TestMethod,
        DataRow("https://bitplatform.dev/", "_blank", "noopener noreferrer"),
        DataRow("//bitplatform.dev/", "_blank", "noopener noreferrer"),
        DataRow("mailto:info@bitplatform.dev", "_blank", "noopener noreferrer"),
        DataRow("tel:+123456789", "_blank", "noopener noreferrer"),
        DataRow("/home", "_blank", null),
        DataRow("https://bitplatform.dev/", null, null)]
    public void BitNavShouldOnlyAddRelToATargetedAbsoluteLink(string url, string? target, string? expectedRel)
    {
        var component = RenderNav([new() { Text = "Home", Url = url, Target = target }]);

        // rel="noopener noreferrer" only protects against a cross-origin target, so an in-app link keeps it off.
        Assert.AreEqual(expectedRel, component.Find(".bit-nav-ict").GetAttribute("rel"));
    }

    [TestMethod]
    public void BitNavShouldRenderTheTargetOfAnItem()
    {
        var component = RenderNav([new() { Text = "Home", Url = "/home", Target = "_blank" }]);

        Assert.AreEqual("_blank", component.Find(".bit-nav-ict").GetAttribute("target"));
    }

    [TestMethod]
    public void BitNavShouldRenderTheAriaLabelOfAnItem()
    {
        var component = RenderNav([new() { Text = "Home", AriaLabel = "Go home" }]);

        Assert.AreEqual("Go home", component.Find(".bit-nav-ict").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavShouldRenderASeparatorItem()
    {
        var items = new List<BitNavItem>
        {
            new() { Text = "Home" },
            new() { IsSeparator = true },
            new() { Text = "Contact" },
        };

        var component = RenderNav(items);

        var separator = component.Find(".bit-nav-sep");
        Assert.AreEqual("separator", separator.GetAttribute("role"));
        // A separator is a rule, not a nav item, so it renders no link and no button of its own.
        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldRespectTheClassAndTheStyleOfAnItem()
    {
        var component = RenderNav([new() { Text = "Home", Class = "item-class", Style = "color: red" }]);

        var listItem = component.Find("li");

        Assert.IsTrue(listItem.ClassList.Contains("item-class"));
        StringAssert.Contains(listItem.GetAttribute("style"), "color: red");
    }

    [TestMethod]
    public void BitNavShouldRenderTheItemsInTheOrderOfTheCollection()
    {
        var component = RenderNav(BasicItems());

        CollectionAssert.AreEqual(
            new[] { "Home", "Products", "Contact" },
            component.FindAll(".bit-nav-itx").Select(e => e.TextContent).ToArray());
    }

    [TestMethod]
    public void BitNavShouldRespectItemsChangingAfterRender()
    {
        var component = RenderNav(BasicItems());

        Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count);

        component.Render(p => p.Add(c => c.Items, new List<BitNavItem> { new() { Text = "Only" } }));

        Assert.AreEqual(1, component.FindAll(".bit-nav-ict").Count);
        Assert.AreEqual("Only", component.Find(".bit-nav-itx").TextContent);
    }

    [TestMethod]
    public void BitNavShouldRespectTheItemsCollectionBeingMutatedInPlace()
    {
        var items = BasicItems();
        var component = RenderNav(items);

        Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count);

        // The same collection instance is handed back, so only its content tells the nav that something
        // has changed; a caller appending to its own list must not have to hand over a new one.
        items.Add(new() { Text = "Blog" });
        component.Render();

        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
        Assert.AreEqual("Blog", component.FindAll(".bit-nav-itx")[3].TextContent);
    }

    [TestMethod]
    public void BitNavShouldExpandTheItemsThatArriveAfterTheFirstRenderWithAllExpanded()
    {
        var component = RenderNav([], p => p.Add(c => c.AllExpanded, true));

        Assert.AreEqual(0, component.FindAll(".bit-nav-ict").Count);

        // Items loaded from a service after the first render still honor AllExpanded.
        component.Render(p => p.Add(c => c.Items, TreeItems()));

        Assert.AreEqual(6, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldKeepTheExpansionOfTheItemsItAlreadyRenderedWithAllExpanded()
    {
        var items = TreeItems();
        var component = RenderNav(items, p => p.Add(c => c.AllExpanded, true));

        component.FindAll(".bit-nav-cbt")[0].Click();
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);

        // A re-render (or a new collection holding the same items) must not undo what the user collapsed.
        component.Render(p => p.Add(c => c.Items, new List<BitNavItem>(items)));

        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldInitializeAnItemAppendedToANestedChildItems()
    {
        var items = TreeItems();
        var component = RenderNav(items, p => p.Add(c => c.AllExpanded, true));

        items[0].ChildItems[0].ChildItems.Add(new() { Text = "Granny Smith", ChildItems = [new() { Text = "Pie" }] });

        component.Render();

        // The root list is untouched by a nested append, so only a comparison that reads the whole tree
        // sees the new item at all, and only then does AllExpanded reach it and its own children.
        CollectionAssert.AreEqual(
            new[] { "Fruits", "Apple", "Granny Smith", "Pie", "Banana", "Drinks", "Coffee", "Tea" },
            component.FindAll(".bit-nav-itx").Select(e => e.TextContent).ToArray());
    }

    #endregion

    #region indentation

    [TestMethod]
    public void BitNavShouldIndentTheLevelsByTheIndentValue()
    {
        const int indentValue = 20;
        const int indentPadding = 30;

        var component = RenderNav(TreeItems(), p =>
        {
            p.Add(c => c.AllExpanded, true);
            p.Add(c => c.IndentValue, indentValue);
            p.Add(c => c.IndentPadding, indentPadding);
        });

        var items = component.FindAll(".bit-nav-ict");

        // A parent reserves no chevron space (it has one), so its padding is purely its depth; a child at
        // depth 1 adds one IndentValue on top of the chevron compensation.
        StringAssert.Contains(items[0].GetAttribute("style"), "padding-inline-start:0px");
        StringAssert.Contains(items[1].GetAttribute("style"), $"padding-inline-start:{indentValue + indentPadding}px");
    }

    [TestMethod]
    public void BitNavShouldRespectTheIndentPaddingOfAChildlessItem()
    {
        var component = RenderNav([new() { Text = "Home" }], p => p.Add(c => c.IndentPadding, 33));

        StringAssert.Contains(component.Find(".bit-nav-ict").GetAttribute("style"), "padding-inline-start:33px");
    }

    [TestMethod]
    public void BitNavShouldRespectTheIndentReversedPaddingInTheReversedChevronMode()
    {
        var component = RenderNav([new() { Text = "Home" }], p =>
        {
            p.Add(c => c.ReversedChevron, true);
            p.Add(c => c.IndentReversedPadding, 7);
        });

        StringAssert.Contains(component.Find(".bit-nav-ict").GetAttribute("style"), "padding-inline-start:7px");
    }

    [TestMethod]
    public void BitNavShouldReverseTheItemRowInTheReversedChevronMode()
    {
        var component = RenderNav(TreeItems(), p => p.Add(c => c.ReversedChevron, true));

        StringAssert.Contains(component.Find(".bit-nav-mct").GetAttribute("style"), "flex-flow:row-reverse");
    }

    [TestMethod]
    public void BitNavShouldDropTheIndentationInTheIconOnlyMode()
    {
        var component = RenderNav(TreeItems(), p =>
        {
            p.Add(c => c.AllExpanded, true);
            p.Add(c => c.IconOnly, true);
        });

        foreach (var item in component.FindAll(".bit-nav-ict"))
        {
            StringAssert.Contains(item.GetAttribute("style"), "padding-inline-start:0px");
        }
    }

    #endregion

    #region expansion

    [TestMethod]
    public void BitNavShouldRenderTheChildrenOfAnExpandedItemOnly()
    {
        var items = TreeItems();
        items[0].IsExpanded = true;

        var component = RenderNav(items);

        // 2 roots + the 2 children of the expanded one.
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldRespectAllExpanded()
    {
        var component = RenderNav(TreeItems(), p => p.Add(c => c.AllExpanded, true));

        Assert.AreEqual(6, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldRenderTheToggleButtonOnlyForAnItemWithChildren()
    {
        var items = new List<BitNavItem>
        {
            new() { Text = "Fruits", ChildItems = [new() { Text = "Apple" }] },
            new() { Text = "Contact" },
        };

        var component = RenderNav(items);

        Assert.AreEqual(1, component.FindAll(".bit-nav-cbt").Count);
    }

    [TestMethod,
        DataRow(true, "true"),
        DataRow(false, "false")]
    public void BitNavShouldReportTheExpansionStateOfAnExpandableItem(bool isExpanded, string expectedAriaExpanded)
    {
        var items = TreeItems();
        items[0].IsExpanded = isExpanded;

        var component = RenderNav(items);

        // The state is reported on the item itself as well as on its chevron, so a screen reader landing on
        // either one learns that the item is expandable.
        Assert.AreEqual(expectedAriaExpanded, component.FindAll(".bit-nav-ict")[0].GetAttribute("aria-expanded"));
        Assert.AreEqual(expectedAriaExpanded, component.FindAll(".bit-nav-cbt")[0].GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitNavShouldNotReportAnExpansionStateForALeafItem()
    {
        var component = RenderNav([new() { Text = "Home" }]);

        Assert.IsNull(component.Find(".bit-nav-ict").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitNavShouldToggleAnItemFromTheChevron()
    {
        var component = RenderNav(TreeItems());

        component.FindAll(".bit-nav-cbt")[0].Click();
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);

        component.FindAll(".bit-nav-cbt")[0].Click();
        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod,
        DataRow("Enter"),
        DataRow(" "),
        DataRow("Spacebar")]
    public void BitNavShouldToggleAnItemFromTheActivationKeysOfTheChevron(string key)
    {
        var component = RenderNav(TreeItems());

        component.FindAll(".bit-nav-cbt")[0].KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldIgnoreOtherKeysOnTheChevron()
    {
        var component = RenderNav(TreeItems());

        component.FindAll(".bit-nav-cbt")[0].KeyDown(new KeyboardEventArgs { Key = "a" });

        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldGiveTheChevronAButtonRole()
    {
        var component = RenderNav(TreeItems());

        var chevron = component.Find(".bit-nav-cbt");

        // The chevron lives inside the item's own anchor or button, so it cannot be a button element; it
        // carries the role and the tab stop explicitly instead.
        Assert.AreEqual("button", chevron.GetAttribute("role"));
        Assert.AreEqual("0", chevron.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitNavShouldToggleAParentWithoutAUrlFromItsBody()
    {
        var component = RenderNav(TreeItems());

        var parent = component.FindAll(".bit-nav-ict")[0];

        parent.Click();
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);

        component.FindAll(".bit-nav-ict")[0].Click();
        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldNotToggleAParentWithAUrlFromItsBody()
    {
        var items = TreeItems();
        items[0].Url = "/fruits";

        var component = RenderNav(items);

        component.FindAll(".bit-nav-ict")[0].Click();

        // A parent that is also a link navigates on a click; only its chevron toggles it.
        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldRespectNoCollapse()
    {
        var component = RenderNav(TreeItems(), p =>
        {
            p.Add(c => c.AllExpanded, true);
            p.Add(c => c.NoCollapse, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-nav-cbt").Count);
        Assert.AreEqual(6, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavSingleExpandShouldCloseABranchThatWasNotOpenedByAToggle()
    {
        var items = TreeItems();
        items[1].IsExpanded = true;

        var component = RenderNav(items, p => p.Add(c => c.SingleExpand, true));
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);

        component.FindAll(".bit-nav-cbt")[0].Click();

        // The branch that was open from the start closes as well: the mode reads the state of the tree
        // rather than remembering which item was toggled last.
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
        Assert.AreEqual("Apple", component.FindAll(".bit-nav-itx")[1].TextContent);
    }

    [TestMethod]
    public void BitNavSingleExpandShouldKeepThePathToTheExpandedItemOpen()
    {
        var items = new List<BitNavItem>
        {
            new()
            {
                Text = "Fruits",
                IsExpanded = true,
                ChildItems = [new() { Text = "Apple", ChildItems = [new() { Text = "Granny Smith" }] }]
            },
            new() { Text = "Drinks", ChildItems = [new() { Text = "Coffee" }] },
        };

        var component = RenderNav(items, p => p.Add(c => c.SingleExpand, true));

        component.FindAll(".bit-nav-cbt")[1].Click();

        // Opening a child keeps its parents open, otherwise the item that was just expanded would not
        // even be on screen.
        CollectionAssert.AreEqual(
            new[] { "Fruits", "Apple", "Granny Smith", "Drinks" },
            component.FindAll(".bit-nav-itx").Select(e => e.TextContent).ToArray());
    }

    [TestMethod]
    public void BitNavShouldRespectSingleExpand()
    {
        var component = RenderNav(TreeItems(), p => p.Add(c => c.SingleExpand, true));

        component.FindAll(".bit-nav-cbt")[0].Click();
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);

        // Expanding the second branch collapses the first one, so the total stays at two roots plus one
        // expanded branch rather than growing to six.
        component.FindAll(".bit-nav-cbt")[1].Click();
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
        Assert.AreEqual("Coffee", component.FindAll(".bit-nav-itx")[2].TextContent);
    }

    [TestMethod]
    public async Task BitNavShouldExpandAndCollapseEverythingFromThePublicApi()
    {
        var component = RenderNav(TreeItems());

        await component.InvokeAsync(() => component.Instance.ExpandAll());
        Assert.AreEqual(6, component.FindAll(".bit-nav-ict").Count);

        await component.InvokeAsync(() => component.Instance.CollapseAll());
        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public async Task BitNavShouldExpandASingleSubtreeFromThePublicApi()
    {
        var items = TreeItems();
        var component = RenderNav(items);

        await component.InvokeAsync(() => component.Instance.ExpandAll(items[1]));

        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
        Assert.AreEqual("Coffee", component.FindAll(".bit-nav-itx")[2].TextContent);
    }

    [TestMethod]
    public async Task BitNavExpandAllShouldDoNothingInTheSingleExpandMode()
    {
        var component = RenderNav(TreeItems(), p => p.Add(c => c.SingleExpand, true));

        await component.InvokeAsync(() => component.Instance.ExpandAll());

        // Expanding everything would break the single-open rule, so the call is a no-op there.
        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public async Task BitNavShouldToggleAnItemFromThePublicApi()
    {
        var items = TreeItems();
        var component = RenderNav(items);

        await component.InvokeAsync(() => component.Instance.ToggleItem(items[0]));

        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public async Task BitNavShouldExpandAndCollapseASingleItemFromThePublicApi()
    {
        var items = TreeItems();
        var component = RenderNav(items);

        await component.InvokeAsync(() => component.Instance.ExpandItem(items[0]));
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);

        await component.InvokeAsync(() => component.Instance.CollapseItem(items[0]));
        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public async Task BitNavExpandAndCollapseItemShouldDoNothingForAnItemAlreadyInThatState()
    {
        var items = TreeItems();
        var toggleCount = 0;

        var component = RenderNav(items, p => p.Add(c => c.OnItemToggle, (BitNavItem _) => { toggleCount++; }));

        // The item is already collapsed and stays collapsed, so nothing is toggled and nothing is reported.
        await component.InvokeAsync(() => component.Instance.CollapseItem(items[0]));
        Assert.AreEqual(0, toggleCount);

        await component.InvokeAsync(() => component.Instance.ExpandItem(items[0]));
        await component.InvokeAsync(() => component.Instance.ExpandItem(items[0]));
        Assert.AreEqual(1, toggleCount);
    }

    [TestMethod]
    public async Task BitNavShouldReportTheExpansionOfAnItemFromThePublicApi()
    {
        var items = TreeItems();
        var component = RenderNav(items);

        Assert.IsFalse(component.Instance.IsItemExpanded(items[0]));

        await component.InvokeAsync(() => component.Instance.ExpandItem(items[0]));

        Assert.IsTrue(component.Instance.IsItemExpanded(items[0]));
    }

    [TestMethod]
    public void BitNavShouldInvokeOnItemToggle()
    {
        BitNavItem? toggled = null;
        var toggleCount = 0;

        var component = RenderNav(TreeItems(), p => p.Add(c => c.OnItemToggle, (BitNavItem item) => { toggled = item; toggleCount++; }));

        component.FindAll(".bit-nav-cbt")[0].Click();

        Assert.AreEqual(1, toggleCount);
        Assert.AreEqual("Fruits", toggled!.Text);
        Assert.IsTrue(toggled.IsExpanded);
    }

    [TestMethod]
    public void BitNavShouldNotToggleADisabledItem()
    {
        var items = TreeItems();
        items[0].IsEnabled = false;

        var component = RenderNav(items);

        component.FindAll(".bit-nav-cbt")[0].Click();

        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldExpandTheAncestorsOfTheSelectedItem()
    {
        var items = TreeItems();

        var component = RenderNav(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.SelectedItem, items[1].ChildItems[1]);
        });

        // A selection deeper in the tree opens the path to it, otherwise the selected item would not be
        // visible at all.
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
        Assert.AreEqual("Tea", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);
    }

    #endregion

    #region chevron

    [TestMethod,
        DataRow(true, "bit-nav-exp"),
        DataRow(false, "bit-ico-r90")]
    public void BitNavShouldRotateTheChevronWithTheExpansionState(bool isExpanded, string expectedClass)
    {
        var items = TreeItems();
        items[0].IsExpanded = isExpanded;

        var component = RenderNav(items);

        var chevronIcon = component.Find(".bit-nav-cbt i");

        foreach (var cls in new[] { "bit-nav-exp", "bit-ico-r90" })
        {
            Assert.AreEqual(cls == expectedClass, chevronIcon.ClassList.Contains(cls), cls);
        }
    }

    [TestMethod]
    public void BitNavShouldRespectTheChevronDownIconName()
    {
        var component = RenderNav(TreeItems(), p => p.Add(c => c.ChevronDownIconName, "ChevronDown"));

        var chevronIcon = component.Find(".bit-nav-cbt i");

        Assert.IsTrue(chevronIcon.ClassList.Contains("bit-icon--ChevronDown"));
        // A custom icon keeps the rotation of the default one, since the rotation is a class of its own
        // rather than a part of the icon name.
        Assert.IsTrue(chevronIcon.ClassList.Contains("bit-ico-r90"));
    }

    [TestMethod]
    public void BitNavChevronDownIconShouldTakePrecedenceOverChevronDownIconName()
    {
        var component = RenderNav(TreeItems(), p =>
        {
            p.Add(c => c.ChevronDownIconName, "ChevronDown");
            p.Add(c => c.ChevronDownIcon, BitIconInfo.Css("fa-solid fa-angle-right"));
        });

        var chevronIcon = component.Find(".bit-nav-cbt i");

        Assert.IsTrue(chevronIcon.ClassList.Contains("fa-angle-right"));
        Assert.IsFalse(chevronIcon.ClassList.Contains("bit-icon--ChevronDown"));
    }

    #endregion

    #region toggle aria labels

    [TestMethod,
        DataRow(true, "collapse me"),
        DataRow(false, "expand me")]
    public void BitNavShouldUseTheToggleAriaLabelsOfTheItem(bool isExpanded, string expectedLabel)
    {
        var items = TreeItems();
        items[0].IsExpanded = isExpanded;
        items[0].CollapseAriaLabel = "collapse me";
        items[0].ExpandAriaLabel = "expand me";

        var component = RenderNav(items);

        Assert.AreEqual(expectedLabel, component.FindAll(".bit-nav-cbt")[0].GetAttribute("aria-label"));
    }

    [TestMethod,
        DataRow(true, "collapse"),
        DataRow(false, "expand")]
    public void BitNavShouldFallBackToItsOwnToggleAriaLabels(bool isExpanded, string expectedLabel)
    {
        var items = TreeItems();
        items[0].IsExpanded = isExpanded;

        var component = RenderNav(items, p =>
        {
            p.Add(c => c.CollapseAriaLabel, "collapse");
            p.Add(c => c.ExpandAriaLabel, "expand");
        });

        Assert.AreEqual(expectedLabel, component.FindAll(".bit-nav-cbt")[0].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavShouldNameTheToggleButtonAfterTheItemWhenNoLabelIsProvided()
    {
        var component = RenderNav(TreeItems());

        // A button with no accessible name is announced as "button" and nothing else, so the item's text is
        // the last resort rather than leaving it unnamed.
        Assert.AreEqual("Fruits", component.FindAll(".bit-nav-cbt")[0].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavShouldTakeTheToggleButtonOfADisabledItemOutOfTheTabOrder()
    {
        var items = TreeItems();
        items[0].IsEnabled = false;

        var component = RenderNav(items);

        var chevron = component.FindAll(".bit-nav-cbt")[0];

        // The chevron of a disabled item does nothing, so it must not be a tab stop that says otherwise.
        Assert.AreEqual("-1", chevron.GetAttribute("tabindex"));
        Assert.AreEqual("true", chevron.GetAttribute("aria-disabled"));
        Assert.AreEqual("0", component.FindAll(".bit-nav-cbt")[1].GetAttribute("tabindex"));
        Assert.IsNull(component.FindAll(".bit-nav-cbt")[1].GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitNavShouldNameAnItemThatRendersAChevronAfterItsText()
    {
        var component = RenderNav(TreeItems());

        // The chevron lives inside the item and carries a name of its own, which the item would otherwise
        // fold into its own name and announce twice.
        Assert.AreEqual("Fruits", component.FindAll(".bit-nav-ict")[0].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavShouldLetTheAriaLabelOfAnItemWinOverItsText()
    {
        var items = TreeItems();
        items[0].AriaLabel = "All the fruits";

        var component = RenderNav(items);

        Assert.AreEqual("All the fruits", component.FindAll(".bit-nav-ict")[0].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavShouldNotNameAnItemWithoutAChevronAfterItsText()
    {
        var component = RenderNav([new() { Text = "Home" }]);

        // A leaf is named by its own content, which is exactly its text, so nothing has to be spelled out.
        Assert.IsNull(component.Find(".bit-nav-ict").GetAttribute("aria-label"));
    }

    #endregion

    #region selection

    [TestMethod]
    public void BitNavShouldMarkTheSelectedItem()
    {
        var items = BasicItems();

        var component = RenderNav(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.SelectedItem, items[1]);
        });

        var selected = component.FindAll(".bit-nav-ict")[1];

        Assert.IsTrue(selected.ClassList.Contains("bit-nav-sel"));
        Assert.AreEqual(1, component.FindAll(".bit-nav-sel").Count);
    }

    [TestMethod]
    public void BitNavShouldRespectDefaultSelectedItemInTheManualMode()
    {
        var items = BasicItems();

        var component = RenderNav(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.DefaultSelectedItem, items[2]);
        });

        Assert.AreEqual("Contact", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);
    }

    [TestMethod]
    public void BitNavShouldSelectTheClickedItemInTheManualMode()
    {
        var items = BasicItems();
        var component = RenderNav(items, p => p.Add(c => c.Mode, BitNavMode.Manual));

        component.FindAll(".bit-nav-ict")[1].Click();

        Assert.AreEqual("Products", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);
    }

    [TestMethod]
    public void BitNavShouldNotSelectTheClickedItemInTheAutomaticMode()
    {
        var component = RenderNav(BasicItems());

        component.FindAll(".bit-nav-ict")[1].Click();

        // The automatic mode follows the URL, and a click on a link that does not navigate must not move
        // the selection behind the URL's back.
        Assert.AreEqual(0, component.FindAll(".bit-nav-sel").Count);
    }

    [TestMethod]
    public void BitNavShouldNotSelectADisabledItem()
    {
        var items = BasicItems();
        items[1].IsEnabled = false;

        var component = RenderNav(items, p => p.Add(c => c.Mode, BitNavMode.Manual));

        component.FindAll(".bit-nav-ict")[1].Click();

        Assert.AreEqual(0, component.FindAll(".bit-nav-sel").Count);
    }

    [TestMethod,
        DataRow(BitNavAriaCurrent.Page, "page"),
        DataRow(BitNavAriaCurrent.Step, "step"),
        DataRow(BitNavAriaCurrent.Location, "location"),
        DataRow(BitNavAriaCurrent.Date, "date"),
        DataRow(BitNavAriaCurrent.Time, "time"),
        DataRow(BitNavAriaCurrent.True, "true")]
    public void BitNavShouldRenderTheAriaCurrentOfTheSelectedItem(BitNavAriaCurrent ariaCurrent, string expectedToken)
    {
        var items = BasicItems();
        items[0].AriaCurrent = ariaCurrent;

        var component = RenderNav(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.SelectedItem, items[0]);
        });

        var itemElements = component.FindAll(".bit-nav-ict");

        Assert.AreEqual(expectedToken, itemElements[0].GetAttribute("aria-current"));
        // Only the selected item carries aria-current, otherwise every item would claim to be the current one.
        Assert.IsNull(itemElements[1].GetAttribute("aria-current"));
    }

    [TestMethod]
    public void BitNavShouldInvokeOnSelectItemOnce()
    {
        var items = BasicItems();
        var selectCount = 0;
        BitNavItem? selected = null;

        var component = RenderNav(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.OnSelectItem, (BitNavItem item) => { selected = item; selectCount++; });
        });

        component.FindAll(".bit-nav-ict")[1].Click();
        component.FindAll(".bit-nav-ict")[1].Click();

        // The second click selects the same item, so the selection does not change and the callback does
        // not fire again.
        Assert.AreEqual(1, selectCount);
        Assert.AreEqual("Products", selected!.Text);
    }

    [TestMethod]
    public void BitNavShouldRespectReselectable()
    {
        var items = BasicItems();
        var selectCount = 0;

        var component = RenderNav(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.Reselectable, true);
            p.Add(c => c.OnSelectItem, (BitNavItem _) => { selectCount++; });
        });

        component.FindAll(".bit-nav-ict")[1].Click();
        component.FindAll(".bit-nav-ict")[1].Click();

        Assert.AreEqual(2, selectCount);
    }

    [TestMethod]
    public void BitNavShouldInvokeOnItemClick()
    {
        BitNavItem? clicked = null;

        var component = RenderNav(BasicItems(), p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.OnItemClick, (BitNavItem item) => { clicked = item; });
        });

        component.FindAll(".bit-nav-ict")[2].Click();

        Assert.AreEqual("Contact", clicked!.Text);
    }

    [TestMethod]
    public void BitNavShouldNotInvokeOnItemClickForADisabledItem()
    {
        var items = BasicItems();
        items[0].IsEnabled = false;
        var clickCount = 0;

        var component = RenderNav(items, p => p.Add(c => c.OnItemClick, (BitNavItem _) => { clickCount++; }));

        component.FindAll(".bit-nav-ict")[0].Click();

        Assert.AreEqual(0, clickCount);
    }

    [TestMethod]
    public async Task BitNavShouldSelectAnItemFromThePublicApi()
    {
        var items = BasicItems();
        var component = RenderNav(items, p => p.Add(c => c.Mode, BitNavMode.Manual));

        await component.InvokeAsync(() => component.Instance.SelectItem(items[2]));

        Assert.AreEqual("Contact", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);
    }

    [TestMethod]
    public async Task BitNavFocusItemShouldFocusTheElementOfTheItem()
    {
        var items = BasicItems();
        var component = RenderNav(items);

        await component.InvokeAsync(async () => await component.Instance.FocusItem(items[1]));
        await component.InvokeAsync(async () => await component.Instance.FocusItem(items[2]));
        await component.InvokeAsync(async () => await component.Instance.FocusItem(items[1]));

        var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();
        var elements = component.FindAll(".bit-nav-ict");

        // Each call moves the focus to the element the item it was given rendered as, so a request never
        // lands on the element of another item, and the same item is addressed by the same one every time.
        Assert.AreEqual(3, focused.Count);
        focused[0].Arguments[0].ShouldBeElementReferenceTo(elements[1]);
        focused[1].Arguments[0].ShouldBeElementReferenceTo(elements[2]);
        focused[2].Arguments[0].ShouldBeElementReferenceTo(elements[1]);
    }

    [TestMethod]
    public async Task BitNavFocusItemShouldFocusAnItemOfACollapsedBranch()
    {
        var items = TreeItems();
        var component = RenderNav(items);

        await component.InvokeAsync(async () => await component.Instance.FocusItem(items[0].ChildItems[1]));

        // The element of an item that was not rendered yet only exists once the child that renders it has
        // rendered, which happens after the nav's own OnAfterRender: the request has to survive that render
        // instead of being consumed by it and dropped.
        var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();

        Assert.AreEqual(1, focused.Count);
        focused[0].Arguments[0].ShouldBeElementReferenceTo(component.FindAll(".bit-nav-ict")[2]);
    }

    [TestMethod]
    public async Task BitNavFocusItemShouldOpenTheBranchOfACollapsedOption()
    {
        var component = RenderComponent<BitNav<BitNavOption>>(parameters =>
        {
            parameters.AddChildContent<BitNavOption>(o =>
            {
                o.Add(p => p.Text, "Fruits");
                o.AddChildContent<BitNavOption>(c => c.Add(p => p.Text, "Apple"));
            });
        });

        var apple = component.FindComponents<BitNavOption>()[1].Instance;

        await component.InvokeAsync(async () => await component.Instance.FocusItem(apple));

        // A collapsed option keeps its children rendered (hidden), so there is an element for the item long
        // before it is on screen: the branch still has to be opened, otherwise the focus would land on an
        // element inside a display:none list.
        Assert.IsFalse(IsHidden(component.Find(".bit-nav-ict").ParentElement!.QuerySelector("ul")!.GetAttribute("style")));

        var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();

        Assert.AreEqual(1, focused.Count);
        focused[0].Arguments[0].ShouldBeElementReferenceTo(component.FindAll(".bit-nav-ict")[1]);
    }

    #endregion

    #region url matching

    [TestMethod]
    public void BitNavShouldSelectTheItemMatchingTheCurrentUrl()
    {
        Navigate("/products");

        var component = RenderNav(BasicItems());

        Assert.AreEqual("Products", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);
    }

    [TestMethod]
    public void BitNavShouldFollowTheLocationChanges()
    {
        Navigate("/home");

        var component = RenderNav(BasicItems());
        Assert.AreEqual("Home", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);

        Navigate("/contact");

        Assert.AreEqual("Contact", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);
    }

    [TestMethod]
    public void BitNavShouldNotFollowTheUrlInTheManualMode()
    {
        Navigate("/products");

        var component = RenderNav(BasicItems(), p => p.Add(c => c.Mode, BitNavMode.Manual));

        Assert.AreEqual(0, component.FindAll(".bit-nav-sel").Count);
    }

    [TestMethod,
        DataRow(BitNavMatch.Exact, "/products", true),
        DataRow(BitNavMatch.Exact, "/products/1", false),
        DataRow(BitNavMatch.Prefix, "/products/1", true),
        DataRow(BitNavMatch.Prefix, "/other", false)]
    public void BitNavShouldRespectTheMatchOfTheNav(BitNavMatch match, string url, bool expectedSelected)
    {
        Navigate(url);

        var items = new List<BitNavItem> { new() { Text = "Products", Url = "/products" } };
        var component = RenderNav(items, p => p.Add(c => c.Match, match));

        Assert.AreEqual(expectedSelected, component.FindAll(".bit-nav-sel").Count == 1);
    }

    [TestMethod,
        DataRow("/products/12", @"^/products/\d+$", true),
        DataRow("/products/ab", @"^/products/\d+$", false)]
    public void BitNavShouldRespectTheRegexMatch(string url, string pattern, bool expectedSelected)
    {
        Navigate(url);

        var items = new List<BitNavItem> { new() { Text = "Products", Url = pattern } };
        var component = RenderNav(items, p => p.Add(c => c.Match, BitNavMatch.Regex));

        Assert.AreEqual(expectedSelected, component.FindAll(".bit-nav-sel").Count == 1);
    }

    [TestMethod]
    public void BitNavShouldTreatAMalformedRegexAsANonMatch()
    {
        Navigate("/products");

        var items = new List<BitNavItem> { new() { Text = "Products", Url = "([" } };

        // A pattern comes from the item, so a broken one must not tear the whole nav down.
        var component = RenderNav(items, p => p.Add(c => c.Match, BitNavMatch.Regex));

        Assert.AreEqual(0, component.FindAll(".bit-nav-sel").Count);
    }

    [TestMethod,
        DataRow("/products/1", "/products/*", true),
        DataRow("/products/1/2", "/products/*", false),
        DataRow("/products/1/2", "/products/**", true),
        DataRow("/products/1", "/products/?", true),
        DataRow("/products/12", "/products/?", false)]
    public void BitNavShouldRespectTheWildcardMatch(string url, string pattern, bool expectedSelected)
    {
        Navigate(url);

        var items = new List<BitNavItem> { new() { Text = "Products", Url = pattern } };
        var component = RenderNav(items, p => p.Add(c => c.Match, BitNavMatch.Wildcard));

        Assert.AreEqual(expectedSelected, component.FindAll(".bit-nav-sel").Count == 1);
    }

    [TestMethod]
    public void BitNavShouldLetTheMatchOfTheItemWinOverTheMatchOfTheNav()
    {
        Navigate("/products/1");

        var items = new List<BitNavItem>
        {
            new() { Text = "Home", Url = "/home" },
            new() { Text = "Products", Url = "/products", Match = BitNavMatch.Prefix },
        };

        var component = RenderNav(items, p => p.Add(c => c.Match, BitNavMatch.Exact));

        Assert.AreEqual("Products", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);
    }

    [TestMethod]
    public void BitNavShouldMatchTheAdditionalUrlsOfAnItem()
    {
        Navigate("/store");

        var items = new List<BitNavItem>
        {
            new() { Text = "Products", Url = "/products", AdditionalUrls = ["/store", "/shop"] }
        };

        var component = RenderNav(items);

        Assert.AreEqual("Products", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);
    }

    [TestMethod,
        DataRow("/products", "/product", false),
        DataRow("/products", "/products", true),
        DataRow("/products/1", "/products", true),
        DataRow("/products-archive", "/products", false)]
    public void BitNavPrefixShouldOnlyMatchOnAPathBoundary(string url, string itemUrl, bool expectedSelected)
    {
        Navigate(url);

        var items = new List<BitNavItem> { new() { Text = "Products", Url = itemUrl } };
        var component = RenderNav(items, p => p.Add(c => c.Match, BitNavMatch.Prefix));

        // A plain StartsWith would light "/products" up on "/products-archive", which is a different page.
        Assert.AreEqual(expectedSelected, component.FindAll(".bit-nav-sel").Count == 1);
    }

    [TestMethod,
        DataRow(BitNavMatch.Exact, "/Products", "/products"),
        DataRow(BitNavMatch.Exact, "/products/", "/products"),
        DataRow(BitNavMatch.Exact, "/products", "/products/"),
        DataRow(BitNavMatch.Exact, "/products?page=2", "/products"),
        DataRow(BitNavMatch.Exact, "/products#top", "/products"),
        DataRow(BitNavMatch.Exact, "/products", "products"),
        DataRow(BitNavMatch.Prefix, "/Products/1", "/products"),
        DataRow(BitNavMatch.Prefix, "/products/1?page=2", "/products")]
    public void BitNavShouldCompareTheUrlsTheWayTheBrowserTreatsThem(BitNavMatch match, string url, string itemUrl)
    {
        Navigate(url);

        var items = new List<BitNavItem> { new() { Text = "Products", Url = itemUrl } };
        var component = RenderNav(items, p => p.Add(c => c.Match, match));

        // The letter case, a trailing slash, a query string, a fragment and a missing leading slash all
        // describe the same page, so none of them may cost the item its selection.
        Assert.AreEqual(1, component.FindAll(".bit-nav-sel").Count);
    }

    [TestMethod]
    public void BitNavShouldMatchAnItemThatSpellsItsUrlOutInFull()
    {
        Navigate("/products");

        // The base is read from the navigation manager rather than hardcoded, so the test states what it
        // means - an absolute URL under the base of the app - instead of pinning a particular host.
        var itemUrl = $"{Services.GetRequiredService<NavigationManager>().BaseUri}products";

        var items = new List<BitNavItem> { new() { Text = "Products", Url = itemUrl } };
        var component = RenderNav(items);

        Assert.AreEqual(1, component.FindAll(".bit-nav-sel").Count);
    }

    [TestMethod]
    public void BitNavShouldMatchTheUrlOfAnItemThatCarriesAQueryString()
    {
        Navigate("/products?page=2");

        var items = new List<BitNavItem>
        {
            new() { Text = "Page 1", Url = "/products?page=1" },
            new() { Text = "Page 2", Url = "/products?page=2" },
        };

        var component = RenderNav(items);

        // An item that spells out a query string is matched against it, so the two pages stay apart.
        Assert.AreEqual("Page 2", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);
    }

    [TestMethod]
    public void BitNavShouldMatchAPatternAgainstThePathOfAUrlCarryingAQueryString()
    {
        Navigate("/products/12?page=2");

        var items = new List<BitNavItem> { new() { Text = "Products", Url = @"^/products/\d+$" } };
        var component = RenderNav(items, p => p.Add(c => c.Match, BitNavMatch.Regex));

        Assert.AreEqual(1, component.FindAll(".bit-nav-sel").Count);
    }

    [TestMethod]
    public void BitNavShouldRematchWhenTheMatchOfTheNavChanges()
    {
        Navigate("/products/1");

        var items = new List<BitNavItem> { new() { Text = "Products", Url = "/products" } };
        var component = RenderNav(items, p => p.Add(c => c.Match, BitNavMatch.Exact));

        Assert.AreEqual(0, component.FindAll(".bit-nav-sel").Count);

        component.Render(p => p.Add(c => c.Match, BitNavMatch.Prefix));

        Assert.AreEqual(1, component.FindAll(".bit-nav-sel").Count);
    }

    [TestMethod]
    public void BitNavShouldFollowTheUrlAfterTheModeIsSwitchedToAutomatic()
    {
        Navigate("/products");

        var component = RenderNav(BasicItems(), p => p.Add(c => c.Mode, BitNavMode.Manual));
        Assert.AreEqual(0, component.FindAll(".bit-nav-sel").Count);

        component.Render(p => p.Add(c => c.Mode, BitNavMode.Automatic));
        Assert.AreEqual("Products", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);

        // The nav that switched modes later on also has to keep following the URL from then on.
        Navigate("/contact");
        Assert.AreEqual("Contact", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);
    }

    [TestMethod]
    public void BitNavShouldSelectTheDeepestMatchingItem()
    {
        Navigate("/products/phones");

        var items = new List<BitNavItem>
        {
            new()
            {
                Text = "Products",
                Url = "/products",
                Match = BitNavMatch.Prefix,
                IsExpanded = true,
                ChildItems = [new() { Text = "Phones", Url = "/products/phones" }]
            }
        };

        var component = RenderNav(items);

        // Both the parent (by prefix) and the child (exactly) match, and the more specific one wins.
        Assert.AreEqual("Phones", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);
    }

    #endregion

    #region keyboard navigation

    // The keyboard navigation listens on the items themselves, so the key is dispatched on the first one;
    // which item the nav acts on comes from the focus it is tracking, not from the element of the event.
    private static void PressKey(IRenderedComponent<BitNav<BitNavItem>> component, string key)
    {
        component.FindAll(".bit-nav-ict")[0].KeyDown(new KeyboardEventArgs { Key = key });
    }

    [TestMethod]
    public void BitNavShouldExpandTheFocusedItemWithTheForwardArrow()
    {
        var component = RenderNav(TreeItems());

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "ArrowRight");

        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldCollapseTheFocusedItemWithTheBackwardArrow()
    {
        var items = TreeItems();
        items[0].IsExpanded = true;

        var component = RenderNav(items);

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "ArrowLeft");

        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldSwapTheArrowDirectionsInRtl()
    {
        var component = RenderNav(TreeItems(), p => p.Add(c => c.Dir, BitDir.Rtl));

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "ArrowLeft");

        // In a right-to-left nav the Left arrow points into the tree, so it is the one that expands.
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldMoveTheFocusToTheNextItemWithTheDownArrow()
    {
        var component = RenderNav(TreeItems());

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "ArrowDown");
        // The focus is now on the second root, so the forward arrow expands that one instead of the first.
        PressKey(component, "ArrowRight");

        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
        Assert.AreEqual("Coffee", component.FindAll(".bit-nav-itx")[2].TextContent);
    }

    [TestMethod]
    public void BitNavShouldMoveTheFocusToThePreviousItemWithTheUpArrow()
    {
        var component = RenderNav(TreeItems());

        component.FindAll(".bit-nav-ict")[1].FocusIn();
        PressKey(component, "ArrowUp");
        PressKey(component, "ArrowRight");

        Assert.AreEqual("Apple", component.FindAll(".bit-nav-itx")[1].TextContent);
    }

    [TestMethod]
    public void BitNavShouldMoveTheFocusToTheLastItemWithEnd()
    {
        var component = RenderNav(TreeItems());

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "End");
        PressKey(component, "ArrowRight");

        Assert.AreEqual("Coffee", component.FindAll(".bit-nav-itx")[2].TextContent);
    }

    [TestMethod]
    public void BitNavShouldMoveTheFocusToTheFirstItemWithHome()
    {
        var component = RenderNav(TreeItems());

        component.FindAll(".bit-nav-ict")[1].FocusIn();
        PressKey(component, "Home");
        PressKey(component, "ArrowRight");

        Assert.AreEqual("Apple", component.FindAll(".bit-nav-itx")[1].TextContent);
    }

    [TestMethod]
    public void BitNavShouldStepIntoAnExpandedItemWithTheForwardArrow()
    {
        var items = TreeItems();
        items[0].IsExpanded = true;
        items[0].ChildItems[0].ChildItems = [new() { Text = "Granny Smith" }];

        var component = RenderNav(items);

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        // The first press only moves into the branch, since it is already open; the second one expands the
        // child the focus landed on.
        PressKey(component, "ArrowRight");
        PressKey(component, "ArrowRight");

        Assert.AreEqual("Granny Smith", component.FindAll(".bit-nav-itx")[2].TextContent);
    }

    [TestMethod]
    public void BitNavShouldStepIntoTheFirstReachableChildWithTheForwardArrow()
    {
        var items = TreeItems();
        items[0].IsExpanded = true;
        items[0].ChildItems[0].IsEnabled = false;

        var component = RenderNav(items);

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "ArrowRight");
        PressKey(component, "ArrowDown");

        // The step skips the disabled child, which takes no focus at all, exactly like the arrow keys do:
        // landing on it would leave the focus where it was while the nav believes it has moved, and the
        // next arrow key would then jump to the first item of the whole nav.
        var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();
        var elements = component.FindAll(".bit-nav-ict");

        Assert.AreEqual(2, focused.Count);
        focused[0].Arguments[0].ShouldBeElementReferenceTo(elements[2]);
        focused[1].Arguments[0].ShouldBeElementReferenceTo(elements[3]);
    }

    [TestMethod]
    public void BitNavShouldStepOutToTheParentWithTheBackwardArrow()
    {
        var items = TreeItems();
        items[0].IsExpanded = true;

        var component = RenderNav(items);

        component.FindAll(".bit-nav-ict")[1].FocusIn();
        // The focused child cannot collapse, so the press moves the focus to its parent; the second press
        // collapses that parent.
        PressKey(component, "ArrowLeft");
        PressKey(component, "ArrowLeft");

        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldExpandEverySiblingWithTheAsterisk()
    {
        var component = RenderNav(TreeItems());

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "*");

        Assert.AreEqual(6, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldNotExpandTheSiblingsWithTheAsteriskWhileTheExpandersAreHidden()
    {
        var component = RenderNav(TreeItems(), p => p.Add(c => c.NoCollapse, true));

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "*");

        // A nav without expanders offers no way to close what the asterisk would open, so the key is inert.
        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldNotExpandTheSiblingsWithTheAsteriskInTheSingleExpandMode()
    {
        var component = RenderNav(TreeItems(), p => p.Add(c => c.SingleExpand, true));

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "*");

        // Opening a whole level would break the single-open rule, exactly like ExpandAll does.
        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldSkipADisabledSiblingWithTheAsterisk()
    {
        var items = TreeItems();
        items[1].IsEnabled = false;

        var component = RenderNav(items);

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "*");

        // Only the enabled branch opens: 2 roots plus the 2 children of the first one.
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public async Task BitNavFocusItemShouldOpenTheBranchesTheItemIsNestedIn()
    {
        var items = TreeItems();
        var component = RenderNav(items);

        await component.InvokeAsync(async () => await component.Instance.FocusItem(items[0].ChildItems[1]));

        // The focus can only land on an element that is rendered, so the path down to the item is opened.
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldMoveTheFocusWithTypeAhead()
    {
        var component = RenderNav(TreeItems());

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "d");
        PressKey(component, "ArrowRight");

        Assert.AreEqual("Coffee", component.FindAll(".bit-nav-itx")[2].TextContent);
    }

    [TestMethod]
    public void BitNavShouldNotStartATypeAheadWithSpace()
    {
        var component = RenderNav(TreeItems());

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, " ");
        PressKey(component, "ArrowRight");

        // Space is the activation key of the chevron, so it must not be swallowed as a search term.
        Assert.AreEqual("Apple", component.FindAll(".bit-nav-itx")[1].TextContent);
    }

    [TestMethod]
    public void BitNavShouldNotListenForKeysOnItsRootElement()
    {
        var component = RenderNav(TreeItems());

        // The keys are handled on the items rather than on the root, so anything a template renders keeps
        // its own key handling instead of being swallowed by the navigation.
        Assert.ThrowsExactly<MissingEventHandlerException>(
            () => component.Find(".bit-nav").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" }));
    }

    [TestMethod]
    public void BitNavShouldIgnoreModifiedKeys()
    {
        var component = RenderNav(TreeItems());

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        component.FindAll(".bit-nav-ict")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight", CtrlKey = true });

        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldIgnoreTheKeyboardWhileDisabled()
    {
        var component = RenderNav(TreeItems(), p => p.Add(c => c.IsEnabled, false));

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "ArrowRight");

        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldSkipASeparatorWhileNavigating()
    {
        var items = new List<BitNavItem>
        {
            new() { Text = "Home" },
            new() { IsSeparator = true },
            new() { Text = "Fruits", ChildItems = [new() { Text = "Apple" }] },
        };

        var component = RenderNav(items);

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "ArrowDown");
        PressKey(component, "ArrowRight");

        // One press lands on "Fruits" rather than on the separator between them.
        Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldSkipADisabledItemWhileNavigating()
    {
        var items = new List<BitNavItem>
        {
            new() { Text = "Home" },
            new() { Text = "Bit academy", IsEnabled = false },
            new() { Text = "Fruits", ChildItems = [new() { Text = "Apple" }] },
        };

        var component = RenderNav(items);

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        PressKey(component, "ArrowDown");
        PressKey(component, "ArrowRight");

        // A disabled item without a URL renders as a native disabled button, which takes no focus at all, so
        // one press lands on "Fruits" instead of stranding the focus on the item between them.
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldStillReachTheChildrenOfADisabledItemWhileNavigating()
    {
        var items = new List<BitNavItem>
        {
            new() { Text = "Home" },
            new()
            {
                Text = "Fruits",
                IsEnabled = false,
                IsExpanded = true,
                ChildItems = [new() { Text = "Apple", ChildItems = [new() { Text = "Granny Smith" }] }]
            },
        };

        var component = RenderNav(items);

        component.FindAll(".bit-nav-ict")[0].FocusIn();
        // Skipping a disabled item does not skip what it holds: one press lands on its enabled child, which
        // the second press expands.
        PressKey(component, "ArrowDown");
        PressKey(component, "ArrowRight");

        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
    }

    #endregion

    #region grouped

    [TestMethod,
        DataRow(BitNavRenderType.Grouped, true),
        DataRow(BitNavRenderType.Normal, false)]
    public void BitNavShouldRenderTheGroupHeadersOnlyInTheGroupedRenderType(BitNavRenderType renderType, bool expectedHeader)
    {
        var component = RenderNav(TreeItems(), p => p.Add(c => c.RenderType, renderType));

        Assert.AreEqual(expectedHeader, component.FindAll(".bit-nav-gcb").Count > 0);
    }

    [TestMethod]
    public void BitNavShouldRenderTheGroupHeaderOnlyForTheRootLevel()
    {
        var component = RenderNav(TreeItems(), p =>
        {
            p.Add(c => c.RenderType, BitNavRenderType.Grouped);
            p.Add(c => c.AllExpanded, true);
        });

        Assert.AreEqual(2, component.FindAll(".bit-nav-gcb").Count);
        Assert.AreEqual(4, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldToggleAGroupFromItsHeader()
    {
        var component = RenderNav(TreeItems(), p => p.Add(c => c.RenderType, BitNavRenderType.Grouped));

        component.FindAll(".bit-nav-gcb")[0].Click();

        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod,
        DataRow(true, "collapse me"),
        DataRow(false, "expand me")]
    public void BitNavShouldNameTheGroupHeaderAfterItsExpansionState(bool isExpanded, string expectedLabel)
    {
        var items = TreeItems();
        items[0].IsExpanded = isExpanded;
        items[0].CollapseAriaLabel = "collapse me";
        items[0].ExpandAriaLabel = "expand me";

        var component = RenderNav(items, p => p.Add(c => c.RenderType, BitNavRenderType.Grouped));

        Assert.AreEqual(expectedLabel, component.FindAll(".bit-nav-gcb")[0].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavShouldRenderNoChevronForAGroupHeaderWithoutChildren()
    {
        var items = new List<BitNavItem> { new() { Text = "Empty group" } };

        var component = RenderNav(items, p => p.Add(c => c.RenderType, BitNavRenderType.Grouped));

        // A group that cannot be expanded must not advertise a chevron or an expansion state.
        Assert.AreEqual(0, component.FindAll(".bit-nav-gcb i").Count);
        Assert.IsNull(component.Find(".bit-nav-gcb").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitNavShouldRenderTheIconOfAGroupHeader()
    {
        var items = TreeItems();
        items[0].IconName = "Home";

        var component = RenderNav(items, p => p.Add(c => c.RenderType, BitNavRenderType.Grouped));

        Assert.IsTrue(component.Find(".bit-nav-gcb .bit-nav-iic").ClassList.Contains("bit-icon--Home"));
    }

    [TestMethod]
    public void BitNavShouldDisableTheGroupHeaderOfADisabledItem()
    {
        var items = TreeItems();
        items[0].IsEnabled = false;

        var component = RenderNav(items, p => p.Add(c => c.RenderType, BitNavRenderType.Grouped));

        Assert.IsTrue(component.FindAll(".bit-nav-gcb")[0].HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitNavShouldRenderTheDescriptionOfAGroupHeader()
    {
        var items = TreeItems();
        items[0].Description = "some fruits";

        var component = RenderNav(items, p => p.Add(c => c.RenderType, BitNavRenderType.Grouped));

        Assert.AreEqual("some fruits", component.Find(".bit-nav-gcb .bit-nav-des").TextContent.Trim());
    }

    #endregion

    #region templates

    [TestMethod]
    public void BitNavShouldRenderTheItemTemplateInsideTheItem()
    {
        var component = RenderNav(BasicItems(), p =>
            p.Add(c => c.ItemTemplate, (BitNavItem item) => builder => builder.AddMarkupContent(0, $"<span class='custom'>{item.Text}</span>")));

        var item = component.Find(".bit-nav-ict");

        Assert.AreEqual(3, component.FindAll(".custom").Count);
        // In the Normal render mode the template replaces the content of the item, not the item itself.
        Assert.IsNotNull(item.QuerySelector(".custom"));
        Assert.AreEqual(0, component.FindAll(".bit-nav-itx").Count);
    }

    [TestMethod]
    public void BitNavShouldReplaceTheItemWithTheItemTemplateInTheReplaceMode()
    {
        var component = RenderNav(BasicItems(), p =>
        {
            p.Add(c => c.ItemTemplate, (BitNavItem item) => builder => builder.AddMarkupContent(0, $"<span class='custom'>{item.Text}</span>"));
            p.Add(c => c.ItemTemplateRenderMode, BitNavItemTemplateRenderMode.Replace);
        });

        Assert.AreEqual(3, component.FindAll(".custom").Count);
        Assert.AreEqual(0, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldLetTheTemplateOfAnItemWinOverTheItemTemplate()
    {
        var items = BasicItems();
        items[0].Template = item => builder => builder.AddMarkupContent(0, $"<span class='own'>{item.Text}</span>");

        var component = RenderNav(items, p =>
            p.Add(c => c.ItemTemplate, (BitNavItem item) => builder => builder.AddMarkupContent(0, $"<span class='shared'>{item.Text}</span>")));

        Assert.AreEqual(1, component.FindAll(".own").Count);
        Assert.AreEqual(2, component.FindAll(".shared").Count);
    }

    [TestMethod]
    public void BitNavShouldRespectTheTemplateRenderModeOfAnItem()
    {
        var items = BasicItems();
        items[0].Template = item => builder => builder.AddMarkupContent(0, $"<span class='own'>{item.Text}</span>");
        items[0].TemplateRenderMode = BitNavItemTemplateRenderMode.Replace;

        var component = RenderNav(items);

        Assert.AreEqual(1, component.FindAll(".own").Count);
        // Only the first item lost its container; the other two keep theirs.
        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavShouldRenderTheHeaderTemplateInsideTheGroupHeader()
    {
        var component = RenderNav(TreeItems(), p =>
        {
            p.Add(c => c.RenderType, BitNavRenderType.Grouped);
            p.Add(c => c.HeaderTemplate, (BitNavItem item) => builder => builder.AddMarkupContent(0, $"<span class='custom-header'>{item.Text}</span>"));
        });

        Assert.AreEqual(2, component.FindAll(".bit-nav-gcb .custom-header").Count);
    }

    [TestMethod]
    public void BitNavShouldReplaceTheGroupHeaderWithTheHeaderTemplateInTheReplaceMode()
    {
        var component = RenderNav(TreeItems(), p =>
        {
            p.Add(c => c.RenderType, BitNavRenderType.Grouped);
            p.Add(c => c.HeaderTemplate, (BitNavItem item) => builder => builder.AddMarkupContent(0, $"<span class='custom-header'>{item.Text}</span>"));
            p.Add(c => c.HeaderTemplateRenderMode, BitNavItemTemplateRenderMode.Replace);
        });

        Assert.AreEqual(2, component.FindAll(".custom-header").Count);
        Assert.AreEqual(0, component.FindAll(".bit-nav-gcb").Count);
    }

    #endregion

    #region classes and styles

    [TestMethod]
    public void BitNavShouldApplyTheClassesOfTheParts()
    {
        var items = PartsItems();

        var component = RenderNav(items, p =>
        {
            AddPartsParameters(p, items);
            p.Add(c => c.Classes, new BitNavClassStyles
            {
                Root = "test-root",
                Description = "test-description",
                Item = "test-item",
                SelectedItem = "test-selected-item",
                ItemContainer = "test-item-container",
                SelectedItemContainer = "test-selected-item-container",
                ItemIcon = "test-item-icon",
                ItemText = "test-item-text",
                ToggleButton = "test-toggle-button",
                ToggleIcon = "test-toggle-icon",
                Separator = "test-separator",
            });
        });

        Assert.IsTrue(component.Find(".bit-nav").ClassList.Contains("test-root"));
        Assert.IsTrue(component.Find(".bit-nav-des").ClassList.Contains("test-description"));
        Assert.IsTrue(component.Find(".bit-nav-itm").ClassList.Contains("test-item"));
        Assert.IsTrue(component.Find(".bit-nav-itm").ClassList.Contains("test-selected-item"));
        Assert.IsTrue(component.Find(".bit-nav-ict").ClassList.Contains("test-item-container"));
        Assert.IsTrue(component.Find(".bit-nav-ict").ClassList.Contains("test-selected-item-container"));
        Assert.IsTrue(component.Find(".bit-nav-iic").ClassList.Contains("test-item-icon"));
        Assert.IsTrue(component.Find(".bit-nav-itx").ClassList.Contains("test-item-text"));
        Assert.IsTrue(component.Find(".bit-nav-cbt").ClassList.Contains("test-toggle-button"));
        Assert.IsTrue(component.Find(".bit-nav-cbt i").ClassList.Contains("test-toggle-icon"));
        Assert.IsTrue(component.Find(".bit-nav-sep").ClassList.Contains("test-separator"));
    }

    [TestMethod]
    public void BitNavShouldApplyTheStylesOfTheParts()
    {
        var items = PartsItems();

        var component = RenderNav(items, p =>
        {
            AddPartsParameters(p, items);
            p.Add(c => c.Styles, new BitNavClassStyles
            {
                Root = "color: red",
                Description = "color: blue",
                Item = "color: green",
                SelectedItem = "color: gold",
                ItemContainer = "color: purple",
                SelectedItemContainer = "color: teal",
                ItemIcon = "color: orange",
                ItemText = "color: brown",
                ToggleButton = "color: cyan",
                ToggleIcon = "color: pink",
                Separator = "color: gray",
            });
        });

        StringAssert.Contains(component.Find(".bit-nav").GetAttribute("style"), "color: red");
        StringAssert.Contains(component.Find(".bit-nav-des").GetAttribute("style"), "color: blue");
        // The style of an item and the one of the selected item are two declarations, so they are joined
        // with a semicolon instead of being run together into a single malformed one.
        StringAssert.Contains(component.Find(".bit-nav-itm").GetAttribute("style"), "color: green;color: gold");
        StringAssert.Contains(component.Find(".bit-nav-ict").GetAttribute("style"), "color: purple;color: teal");
        StringAssert.Contains(component.Find(".bit-nav-iic").GetAttribute("style"), "color: orange");
        StringAssert.Contains(component.Find(".bit-nav-itx").GetAttribute("style"), "color: brown");
        StringAssert.Contains(component.Find(".bit-nav-cbt").GetAttribute("style"), "color: cyan");
        StringAssert.Contains(component.Find(".bit-nav-cbt i").GetAttribute("style"), "color: pink");
        StringAssert.Contains(component.Find(".bit-nav-sep").GetAttribute("style"), "color: gray");
    }

    [TestMethod]
    public void BitNavShouldApplyTheHeaderClassesAndStylesInTheGroupedRenderType()
    {
        var component = RenderNav(TreeItems(), p =>
        {
            p.Add(c => c.RenderType, BitNavRenderType.Grouped);
            p.Add(c => c.Classes, new BitNavClassStyles { Header = "test-header", HeaderText = "test-header-text" });
            p.Add(c => c.Styles, new BitNavClassStyles { Header = "color: red", HeaderText = "color: blue" });
        });

        var header = component.Find(".bit-nav-gcb");
        var headerText = component.Find(".bit-nav-ghd");

        Assert.IsTrue(header.ClassList.Contains("test-header"));
        Assert.IsTrue(headerText.ClassList.Contains("test-header-text"));
        StringAssert.Contains(header.GetAttribute("style"), "color: red");
        StringAssert.Contains(headerText.GetAttribute("style"), "color: blue");
    }

    [TestMethod]
    public void BitNavShouldNotApplyTheSelectedClassesToAnUnselectedItem()
    {
        var items = BasicItems();

        var component = RenderNav(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.DefaultSelectedItem, items[0]);
            p.Add(c => c.Classes, new BitNavClassStyles { SelectedItem = "test-selected-item", SelectedItemContainer = "test-selected-item-container" });
        });

        var unselected = component.FindAll(".bit-nav-ict")[1];

        Assert.IsFalse(unselected.ClassList.Contains("test-selected-item-container"));
        Assert.IsFalse(unselected.QuerySelector(".bit-nav-itm")!.ClassList.Contains("test-selected-item"));
    }

    #endregion

    #region custom items

    [TestMethod]
    public void BitNavShouldReadACustomItemThroughTheNameSelectors()
    {
        var items = new List<NavRecord>
        {
            new()
            {
                Label = "Fruits",
                Note = "some fruits",
                Glyph = "Home",
                Open = true,
                Nodes = [new() { Label = "Apple", Address = "/apple" }]
            },
            new() { Divider = true },
            new() { Label = "Disabled", Enabled = false },
        };

        var component = RenderComponent<BitNav<NavRecord>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NameSelectors, new BitNavNameSelectors<NavRecord>
            {
                Text = { Name = nameof(NavRecord.Label) },
                Url = { Name = nameof(NavRecord.Address) },
                Description = { Name = nameof(NavRecord.Note) },
                IconName = { Name = nameof(NavRecord.Glyph) },
                IsExpanded = { Name = nameof(NavRecord.Open) },
                IsEnabled = { Name = nameof(NavRecord.Enabled) },
                IsSeparator = { Name = nameof(NavRecord.Divider) },
                ChildItems = { Name = nameof(NavRecord.Nodes) },
            });
        });

        CollectionAssert.AreEqual(
            new[] { "Fruits", "Apple", "Disabled" },
            component.FindAll(".bit-nav-itx").Select(e => e.TextContent).ToArray());

        Assert.AreEqual("some fruits", component.Find(".bit-nav-des").TextContent.Trim());
        Assert.IsTrue(component.Find(".bit-nav-iic").ClassList.Contains("bit-icon--Home"));
        Assert.AreEqual(1, component.FindAll(".bit-nav-sep").Count);
        Assert.IsTrue(component.FindAll(".bit-nav-ict")[2].ClassList.Contains("bit-nav-dis"));
        Assert.AreEqual("/apple", component.FindAll(".bit-nav-ict")[1].GetAttribute("href"));
    }

    [TestMethod]
    public void BitNavShouldReadACustomItemThroughTheSelectorLambdas()
    {
        var items = new List<NavRecord>
        {
            new() { Label = "Fruits", Open = true, Nodes = [new() { Label = "Apple" }] }
        };

        var component = RenderComponent<BitNav<NavRecord>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NameSelectors, new BitNavNameSelectors<NavRecord>
            {
                Text = { Selector = item => item.Label },
                IsExpanded = { Selector = item => item.Open },
                ChildItems = { Selector = item => item.Nodes },
            });
        });

        CollectionAssert.AreEqual(
            new[] { "Fruits", "Apple" },
            component.FindAll(".bit-nav-itx").Select(e => e.TextContent).ToArray());
    }

    [TestMethod]
    public void BitNavShouldReadACustomItemByConvention()
    {
        var items = new List<NavConventionItem>
        {
            new()
            {
                Text = "Fruits",
                IconName = "Home",
                IsExpanded = true,
                ChildItems = [new() { Text = "Apple", Url = "/apple" }]
            }
        };

        var component = RenderComponent<BitNav<NavConventionItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            // The members already carry the names of BitNavItem, so the default selectors find them.
            parameters.Add(p => p.NameSelectors, new BitNavNameSelectors<NavConventionItem>());
        });

        CollectionAssert.AreEqual(
            new[] { "Fruits", "Apple" },
            component.FindAll(".bit-nav-itx").Select(e => e.TextContent).ToArray());
    }

    [TestMethod]
    public void BitNavShouldToggleACustomItemThroughTheNameSelectors()
    {
        var items = new List<NavRecord>
        {
            new() { Label = "Fruits", Nodes = [new() { Label = "Apple" }] }
        };

        var component = RenderComponent<BitNav<NavRecord>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NameSelectors, new BitNavNameSelectors<NavRecord>
            {
                Text = { Name = nameof(NavRecord.Label) },
                IsExpanded = { Name = nameof(NavRecord.Open) },
                ChildItems = { Name = nameof(NavRecord.Nodes) },
            });
        });

        component.Find(".bit-nav-cbt").Click();

        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
        // The expansion state is written back to the mapped member of the item.
        Assert.IsTrue(items[0].Open);
    }

    [TestMethod]
    public void BitNavShouldRenderACustomItemWithoutAnyNameSelectors()
    {
        var items = new List<NavOpaqueItem> { new() { Whatever = "Home" }, new() { Whatever = "Contact" } };

        var component = RenderComponent<BitNav<NavOpaqueItem>>(parameters => parameters.Add(p => p.Items, items));

        // Nothing can be read from the item, so the nav renders two empty rows instead of throwing.
        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
        Assert.AreEqual(0, component.FindAll(".bit-nav-cbt").Count);
    }

    #endregion

    #region options

    [TestMethod]
    public void BitNavShouldRenderTheOptions()
    {
        var component = RenderComponent<BitNav<BitNavOption>>(parameters =>
        {
            parameters.AddChildContent<BitNavOption>(o => o.Add(p => p.Text, "Home"));
            parameters.AddChildContent<BitNavOption>(o => o.Add(p => p.Text, "Contact"));
        });

        CollectionAssert.AreEqual(
            new[] { "Home", "Contact" },
            component.FindAll(".bit-nav-itx").Select(e => e.TextContent).ToArray());
    }

    [TestMethod]
    public void BitNavShouldKeepTheChildrenOfACollapsedOptionInTheDom()
    {
        var component = RenderComponent<BitNav<BitNavOption>>(parameters =>
        {
            parameters.AddChildContent<BitNavOption>(o =>
            {
                o.Add(p => p.Text, "Fruits");
                o.AddChildContent<BitNavOption>(c => c.Add(p => p.Text, "Apple"));
            });
        });

        // The children of an option render themselves, so they stay in the DOM (hidden) while collapsed to
        // keep their registrations alive.
        Assert.AreEqual(2, component.FindAll(".bit-nav-ict").Count);
        StringAssert.Contains(component.Find(".bit-nav-ict").ParentElement!.QuerySelector("ul")!.GetAttribute("style"), "display:none");
    }

    [TestMethod]
    public void BitNavShouldToggleAnOption()
    {
        var component = RenderComponent<BitNav<BitNavOption>>(parameters =>
        {
            parameters.AddChildContent<BitNavOption>(o =>
            {
                o.Add(p => p.Text, "Fruits");
                o.AddChildContent<BitNavOption>(c => c.Add(p => p.Text, "Apple"));
            });
        });

        var childrenList = component.Find(".bit-nav-ict").ParentElement!.QuerySelector("ul")!;
        Assert.IsTrue(IsHidden(childrenList.GetAttribute("style")));

        component.Find(".bit-nav-cbt").Click();

        // An expanded branch is one that is not hidden, which is what is asserted: the style attribute may
        // well carry other declarations one day without the branch being collapsed by them.
        Assert.IsFalse(IsHidden(component.Find(".bit-nav-ict").ParentElement!.QuerySelector("ul")!.GetAttribute("style")));
    }

    [TestMethod]
    public void BitNavShouldRespectAllExpandedForTheOptions()
    {
        var component = RenderComponent<BitNav<BitNavOption>>(parameters =>
        {
            parameters.Add(p => p.AllExpanded, true);
            parameters.AddChildContent<BitNavOption>(o =>
            {
                o.Add(p => p.Text, "Fruits");
                o.AddChildContent<BitNavOption>(c => c.Add(p => p.Text, "Apple"));
            });
        });

        Assert.IsFalse(IsHidden(component.Find(".bit-nav-ict").ParentElement!.QuerySelector("ul")!.GetAttribute("style")));
    }

    // The children of an option stay in the DOM while collapsed and are hidden through the style attribute,
    // so "shown" is the absence of that declaration rather than the absence of the whole attribute.
    private static bool IsHidden(string? style) => style?.Contains("display:none") is true;

    #endregion
}
