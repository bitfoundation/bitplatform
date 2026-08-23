using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.NavBar;

[TestClass]
public class BitNavBarTests : BunitTestContext
{
    private static List<BitNavBarItem> BasicItems() =>
    [
        new() { Text = "Home", IconName = "Home" },
        new() { Text = "Products", IconName = "ProductVariant" },
        new() { Text = "Profile", IconName = "Contact" },
    ];

    private static List<BitNavBarItem> UrlItems() =>
    [
        new() { Text = "Home", Url = "/home" },
        new() { Text = "Products", Url = "/products" },
        new() { Text = "Profile", Url = "/profile" },
    ];

    private IRenderedComponent<BitNavBar<BitNavBarItem>> RenderNavBar(
        IList<BitNavBarItem> items,
        Action<ComponentParameterCollectionBuilder<BitNavBar<BitNavBarItem>>>? parameters = null)
    {
        return RenderComponent<BitNavBar<BitNavBarItem>>(p =>
        {
            p.Add(c => c.Items, items);
            parameters?.Invoke(p);
        });
    }

    private void Navigate(string relativeUrl)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(relativeUrl);
    }

    private static string[] Texts(IRenderedComponent<IComponent> component)
    {
        return [.. component.FindAll(".bit-nbr-txt").Select(e => e.TextContent)];
    }

    private static string? SelectedText(IRenderedComponent<IComponent> component)
    {
        return component.FindAll(".bit-nbr-sel .bit-nbr-txt").Select(e => e.TextContent).FirstOrDefault();
    }



    [TestMethod]
    public void BitNavBarShouldRenderRootElement()
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>();

        var root = component.Find(".bit-nbr");

        Assert.IsNotNull(root);
        Assert.AreEqual("NAV", root.TagName);
    }

    [TestMethod]
    public void BitNavBarShouldRenderTheItemsOfTheItemsCollection()
    {
        var component = RenderNavBar(BasicItems());

        CollectionAssert.AreEqual(new[] { "Home", "Products", "Profile" }, Texts(component));
        Assert.AreEqual(3, component.FindAll(".bit-nbr-itm").Count);
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

        AssertRootClass(component, expectedClass, "bit-nbr-ftw");
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

        AssertRootClass(component, expectedClass, "bit-nbr-flw");
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

        AssertRootClass(component, expectedClass, "bit-nbr-ion");
    }

    [TestMethod]
    [DataRow(true, "bit-nbr-inl")]
    [DataRow(false, "")]
    public void BitNavBarShouldRespectInlineText(bool inlineText, string expectedClass)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            parameters.Add(p => p.InlineText, inlineText);
        });

        AssertRootClass(component, expectedClass, "bit-nbr-inl");
    }

    [TestMethod]
    [DataRow(true, "bit-nbr-jst")]
    [DataRow(false, "")]
    public void BitNavBarShouldRespectJustified(bool justified, string expectedClass)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            parameters.Add(p => p.Justified, justified);
        });

        AssertRootClass(component, expectedClass, "bit-nbr-jst");
    }

    [TestMethod]
    [DataRow(true, "bit-nbr-vrt")]
    [DataRow(false, "")]
    public void BitNavBarShouldRespectVertical(bool vertical, string expectedClass)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            parameters.Add(p => p.Vertical, vertical);
        });

        AssertRootClass(component, expectedClass, "bit-nbr-vrt");
    }

    [TestMethod]
    [DataRow(true, "bit-nbr-sfa")]
    [DataRow(false, "")]
    public void BitNavBarShouldRespectSafeArea(bool safeArea, string expectedClass)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            parameters.Add(p => p.SafeArea, safeArea);
        });

        AssertRootClass(component, expectedClass, "bit-nbr-sfa");
    }

    [TestMethod]
    [DataRow(true, "bit-nbr-hut")]
    [DataRow(false, "")]
    public void BitNavBarShouldRespectHideUnselectedText(bool hideUnselectedText, string expectedClass)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            parameters.Add(p => p.HideUnselectedText, hideUnselectedText);
        });

        AssertRootClass(component, expectedClass, "bit-nbr-hut");
    }

    [TestMethod]
    public void BitNavBarIconOnlyShouldWinOverHideUnselectedText()
    {
        // Both hide the text, so the two classes would fight over the selected item: IconOnly hides it and
        // HideUnselectedText brings it back. Only one of them is ever rendered.
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            parameters.Add(p => p.IconOnly, true);
            parameters.Add(p => p.HideUnselectedText, true);
        });

        var root = component.Find(".bit-nbr");

        Assert.IsTrue(root.ClassList.Contains("bit-nbr-ion"));
        Assert.IsFalse(root.ClassList.Contains("bit-nbr-hut"));
    }

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-nbr-pri")]
    [DataRow(BitColor.Secondary, "bit-nbr-sec")]
    [DataRow(BitColor.Error, "bit-nbr-err")]
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

        Assert.IsTrue(component.Find(".bit-nbr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitNavBarShouldRespectFilled(bool filled)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            parameters.Add(p => p.Filled, filled);
        });

        Assert.AreEqual(filled, component.Find(".bit-nbr").ClassList.Contains("bit-nbr-fil"));
    }

    [TestMethod]
    public void BitNavBarShouldNotRenderTheFilledClassByDefault()
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>();

        Assert.IsFalse(component.Find(".bit-nbr").ClassList.Contains("bit-nbr-fil"));
    }

    [TestMethod]
    [DataRow(BitSize.Small, "bit-nbr-sm")]
    [DataRow(BitSize.Medium, "bit-nbr-md")]
    [DataRow(BitSize.Large, "bit-nbr-lg")]
    [DataRow(null, "bit-nbr-md")]
    public void BitNavBarShouldRespectSize(BitSize? size, string expectedClass)
    {
        var component = RenderComponent<BitNavBar<BitNavBarOption>>(parameters =>
        {
            if (size.HasValue)
            {
                parameters.Add(p => p.Size, size.Value);
            }
        });

        Assert.IsTrue(component.Find(".bit-nbr").ClassList.Contains(expectedClass));
    }



    [TestMethod]
    public void BitNavBarShouldRenderAnAnchorForAnItemWithAUrlAndAButtonWithout()
    {
        var component = RenderNavBar([new() { Text = "Home", Url = "/home" }, new() { Text = "Panel" }]);

        var items = component.FindAll(".bit-nbr-itm");

        Assert.AreEqual("A", items[0].TagName);
        Assert.AreEqual("/home", items[0].GetAttribute("href"));
        Assert.AreEqual("BUTTON", items[1].TagName);
    }

    [TestMethod]
    public void BitNavBarShouldKeepTheSelectedItemAnAnchor()
    {
        // An anchor that turns into a button on selection loses the focus that was on it, and takes the
        // context menu and the "open in a new tab" of the current destination away with it.
        Navigate("/products");

        var component = RenderNavBar(UrlItems());

        var selected = component.Find(".bit-nbr-sel");

        Assert.AreEqual("A", selected.TagName);
        Assert.AreEqual("/products", selected.GetAttribute("href"));
    }

    [TestMethod]
    public void BitNavBarShouldReportTheSelectedItemAsTheCurrentPage()
    {
        Navigate("/products");

        var component = RenderNavBar(UrlItems());

        var items = component.FindAll(".bit-nbr-itm");

        Assert.IsNull(items[0].GetAttribute("aria-current"));
        Assert.AreEqual("page", items[1].GetAttribute("aria-current"));
        Assert.IsNull(items[2].GetAttribute("aria-current"));
    }

    [TestMethod]
    [DataRow(BitNavAriaCurrent.Step, "step")]
    [DataRow(BitNavAriaCurrent.Location, "location")]
    [DataRow(BitNavAriaCurrent.True, "true")]
    public void BitNavBarShouldRespectTheAriaCurrentOfAnItem(BitNavAriaCurrent ariaCurrent, string expected)
    {
        Navigate("/products");

        var items = UrlItems();
        items[1].AriaCurrent = ariaCurrent;

        var component = RenderNavBar(items);

        Assert.AreEqual(expected, component.Find(".bit-nbr-sel").GetAttribute("aria-current"));
    }

    [TestMethod]
    public void BitNavBarShouldHideTheIconOfAnItemFromAssistiveTechnology()
    {
        var component = RenderNavBar(BasicItems());

        Assert.AreEqual("true", component.Find(".bit-nbr-ico").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitNavBarShouldNameAnIconOnlyItemAfterItsText()
    {
        // The text is the accessible name of the item, so an item that does not render it has to carry it
        // as a label instead, otherwise an icon-only bar announces nothing at all.
        var component = RenderNavBar(BasicItems(), p => p.Add(c => c.IconOnly, true));

        var item = component.Find(".bit-nbr-itm");

        Assert.AreEqual("Home", item.GetAttribute("aria-label"));
        Assert.AreEqual("Home", item.GetAttribute("title"));
        Assert.AreEqual(0, component.FindAll(".bit-nbr-txt").Count);
    }

    [TestMethod]
    public void BitNavBarShouldNotLabelAnItemThatRendersItsText()
    {
        var component = RenderNavBar(BasicItems());

        var item = component.Find(".bit-nbr-itm");

        Assert.IsNull(item.GetAttribute("aria-label"));
        Assert.IsNull(item.GetAttribute("title"));
    }

    [TestMethod]
    public void BitNavBarShouldLetTheAriaLabelOfAnItemWinOverItsText()
    {
        var items = BasicItems();
        items[0].AriaLabel = "Go home";

        var component = RenderNavBar(items, p => p.Add(c => c.IconOnly, true));

        Assert.AreEqual("Go home", component.Find(".bit-nbr-itm").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavBarShouldLabelTheUnselectedItemsWhileTheirTextIsHidden()
    {
        Navigate("/products");

        var component = RenderNavBar(UrlItems(), p => p.Add(c => c.HideUnselectedText, true));

        var items = component.FindAll(".bit-nbr-itm");

        Assert.AreEqual("Home", items[0].GetAttribute("aria-label"));
        // The selected item still renders its text, so it needs no label of its own.
        Assert.IsNull(items[1].GetAttribute("aria-label"));
        Assert.AreEqual("Profile", items[2].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavBarShouldRespectTheTitleOfAnItem()
    {
        var items = BasicItems();
        items[0].Title = "The home page";

        var component = RenderNavBar(items);

        Assert.AreEqual("The home page", component.Find(".bit-nbr-itm").GetAttribute("title"));
    }

    [TestMethod]
    public void BitNavBarShouldRespectTheTargetOfAnItem()
    {
        var items = UrlItems();
        items[0].Target = "_blank";

        var component = RenderNavBar(items);

        Assert.AreEqual("_blank", component.Find(".bit-nbr-itm").GetAttribute("target"));
    }

    [TestMethod]
    [DataRow("https://bitplatform.dev", "_blank", "noopener noreferrer")]
    [DataRow("//bitplatform.dev", "_blank", "noopener noreferrer")]
    [DataRow("/home", "_blank", null)]
    [DataRow("https://bitplatform.dev", null, null)]
    public void BitNavBarShouldProtectAnExternalTargetOnly(string url, string? target, string? expectedRel)
    {
        var component = RenderNavBar([new() { Text = "Home", Url = url, Target = target }]);

        Assert.AreEqual(expectedRel, component.Find(".bit-nbr-itm").GetAttribute("rel"));
    }



    [TestMethod]
    public void BitNavBarShouldDisableASingleItem()
    {
        var items = UrlItems();
        items[1].IsEnabled = false;

        var component = RenderNavBar(items);

        var disabled = component.FindAll(".bit-nbr-itm")[1];

        // A disabled item is not only greyed out: it loses its link, its tab stop and its keyboard stop.
        Assert.AreEqual("BUTTON", disabled.TagName);
        Assert.IsTrue(disabled.ClassList.Contains("bit-nbr-dis"));
        Assert.AreEqual("true", disabled.GetAttribute("aria-disabled"));
        Assert.AreEqual("-1", disabled.GetAttribute("tabindex"));
        Assert.IsTrue(disabled.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitNavBarShouldDisableEveryItemWhenTheNavBarIsDisabled()
    {
        var component = RenderNavBar(UrlItems(), p => p.Add(c => c.IsEnabled, false));

        Assert.IsTrue(component.Find(".bit-nbr").ClassList.Contains("bit-dis"));

        foreach (var item in component.FindAll(".bit-nbr-itm"))
        {
            Assert.AreEqual("BUTTON", item.TagName);
            Assert.AreEqual("-1", item.GetAttribute("tabindex"));
            Assert.AreEqual("true", item.GetAttribute("aria-disabled"));
        }
    }

    [TestMethod]
    public void BitNavBarShouldNotSelectADisabledItemOnClick()
    {
        var items = BasicItems();
        items[1].IsEnabled = false;
        var clicked = 0;

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.OnItemClick, (BitNavBarItem _) => clicked++);
        });

        component.FindAll(".bit-nbr-itm")[1].Click();

        Assert.AreEqual(0, clicked);
        Assert.IsNull(SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldNotHandleAClickWhileTheNavBarIsDisabled()
    {
        var clicked = 0;

        var component = RenderNavBar(BasicItems(), p =>
        {
            p.Add(c => c.IsEnabled, false);
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.OnItemClick, (BitNavBarItem _) => clicked++);
        });

        component.FindAll(".bit-nbr-itm")[0].Click();

        Assert.AreEqual(0, clicked);
        Assert.IsNull(SelectedText(component));
    }



    [TestMethod]
    public void BitNavBarShouldRenderTheBadgeOfAnItem()
    {
        var items = BasicItems();
        items[1].Badge = "12";

        var component = RenderNavBar(items);

        var badge = component.Find(".bit-nbr-bdg");

        Assert.AreEqual("12", badge.TextContent);
        // The badge decorates the name the item already carries, so it is not announced on top of it.
        Assert.AreEqual("true", badge.GetAttribute("aria-hidden"));
        Assert.IsFalse(badge.ClassList.Contains("bit-nbr-bdt"));
    }

    [TestMethod]
    public void BitNavBarShouldRenderTheDotOfAnItem()
    {
        var items = BasicItems();
        items[1].Dot = true;

        var component = RenderNavBar(items);

        var dot = component.Find(".bit-nbr-bdg");

        Assert.IsTrue(dot.ClassList.Contains("bit-nbr-bdt"));
        Assert.AreEqual(string.Empty, dot.TextContent);
    }

    [TestMethod]
    public void BitNavBarBadgeShouldWinOverTheDot()
    {
        var items = BasicItems();
        items[1].Badge = "3";
        items[1].Dot = true;

        var component = RenderNavBar(items);

        Assert.AreEqual(1, component.FindAll(".bit-nbr-bdg").Count);
        Assert.AreEqual("3", component.Find(".bit-nbr-bdg").TextContent);
        Assert.AreEqual(0, component.FindAll(".bit-nbr-bdt").Count);
    }

    [TestMethod]
    public void BitNavBarShouldRenderABadgeOfAnItemWithoutAnIcon()
    {
        var component = RenderNavBar([new() { Text = "Alerts", Badge = "9" }]);

        Assert.AreEqual(0, component.FindAll(".bit-nbr-ico").Count);
        Assert.AreEqual("9", component.Find(".bit-nbr-bdg").TextContent);
    }

    [TestMethod]
    public void BitNavBarShouldNotRenderAnIconContainerWithoutAnIconOrABadge()
    {
        var component = RenderNavBar([new() { Text = "Home" }]);

        Assert.AreEqual(0, component.FindAll(".bit-nbr-icc").Count);
    }

    [TestMethod]
    public void BitNavBarShouldFoldTheBadgeIntoTheNameOfItsItem()
    {
        // The badge itself is hidden from assistive technology, so a count that only exists as a colored
        // bubble has to reach the name of the item instead.
        var items = BasicItems();
        items[1].Badge = "12";

        var component = RenderNavBar(items);

        Assert.AreEqual("Products (12)", component.FindAll(".bit-nbr-itm")[1].GetAttribute("aria-label"));
        Assert.IsNull(component.FindAll(".bit-nbr-itm")[0].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavBarShouldLetTheBadgeAriaLabelOfAnItemWinOverItsBadge()
    {
        var items = BasicItems();
        items[1].Badge = "12";
        items[1].BadgeAriaLabel = "12 unread messages";

        var component = RenderNavBar(items);

        Assert.AreEqual("Products (12 unread messages)", component.FindAll(".bit-nbr-itm")[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavBarShouldAnnounceADotOnlyWithABadgeAriaLabel()
    {
        // A dot carries no text of its own, so there is nothing to announce until it is described.
        var items = BasicItems();
        items[1].Dot = true;

        var component = RenderNavBar(items);

        Assert.IsNull(component.FindAll(".bit-nbr-itm")[1].GetAttribute("aria-label"));

        items[1].BadgeAriaLabel = "needs attention";
        component.Render();

        Assert.AreEqual("Products (needs attention)", component.FindAll(".bit-nbr-itm")[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavBarShouldFoldTheBadgeIntoTheExplicitAriaLabelOfAnItem()
    {
        var items = BasicItems();
        items[1].Badge = "12";
        items[1].AriaLabel = "All the products";

        var component = RenderNavBar(items);

        Assert.AreEqual("All the products (12)", component.FindAll(".bit-nbr-itm")[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavBarShouldFoldTheBadgeIntoTheNameOfAnIconOnlyItem()
    {
        var items = BasicItems();
        items[1].Badge = "12";

        var component = RenderNavBar(items, p => p.Add(c => c.IconOnly, true));

        Assert.AreEqual("Products (12)", component.FindAll(".bit-nbr-itm")[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavBarShouldNotNameATemplatedItemAfterItsBadge()
    {
        // A template owns the content of its item, so the navbar does not know what names it and must not
        // overwrite that name with the text of the item.
        var items = BasicItems();
        items[1].Badge = "12";

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.ItemTemplate, (RenderFragment<BitNavBarItem>)(item => builder => builder.AddContent(0, item.Text)));
        });

        Assert.IsNull(component.FindAll(".bit-nbr-itm")[1].GetAttribute("aria-label"));
    }



    [TestMethod]
    public void BitNavBarShouldSwapToTheSelectedIconOfTheSelectedItem()
    {
        var items = BasicItems();
        items[0].SelectedIconName = "HomeSolid";
        items[1].SelectedIconName = "ProductVariantSolid";

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.DefaultSelectedItem, items[0]);
        });

        var icons = component.FindAll(".bit-nbr-ico");

        Assert.IsTrue(icons[0].ClassList.Contains("bit-icon--HomeSolid"));
        // Only the selected item swaps; the rest keep their own icon.
        Assert.IsTrue(icons[1].ClassList.Contains("bit-icon--ProductVariant"));
        Assert.IsFalse(icons[1].ClassList.Contains("bit-icon--ProductVariantSolid"));
    }

    [TestMethod]
    public void BitNavBarShouldKeepTheIconOfASelectedItemWithoutASelectedIcon()
    {
        var items = BasicItems();

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.DefaultSelectedItem, items[0]);
        });

        Assert.IsTrue(component.FindAll(".bit-nbr-ico")[0].ClassList.Contains("bit-icon--Home"));
    }

    [TestMethod]
    public void BitNavBarShouldLetTheSelectedIconWinOverTheSelectedIconName()
    {
        var items = BasicItems();
        items[0].SelectedIcon = BitIconInfo.Css("fa-solid fa-house");
        items[0].SelectedIconName = "HomeSolid";

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.DefaultSelectedItem, items[0]);
        });

        var icon = component.FindAll(".bit-nbr-ico")[0];

        Assert.IsTrue(icon.ClassList.Contains("fa-house"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--HomeSolid"));
    }

    [TestMethod]
    public void BitNavBarShouldFollowTheSelectionWithTheSelectedIcon()
    {
        var items = BasicItems();
        items[0].SelectedIconName = "HomeSolid";
        items[1].SelectedIconName = "ProductVariantSolid";

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.DefaultSelectedItem, items[0]);
        });

        component.FindAll(".bit-nbr-itm")[1].Click();

        var icons = component.FindAll(".bit-nbr-ico");

        Assert.IsTrue(icons[0].ClassList.Contains("bit-icon--Home"));
        Assert.IsFalse(icons[0].ClassList.Contains("bit-icon--HomeSolid"));
        Assert.IsTrue(icons[1].ClassList.Contains("bit-icon--ProductVariantSolid"));
    }



    [TestMethod]
    public void BitNavBarShouldSelectTheItemMatchingTheCurrentUrl()
    {
        Navigate("/products");

        var component = RenderNavBar(UrlItems());

        Assert.AreEqual("Products", SelectedText(component));
    }

    [TestMethod]
    [DataRow("/products", "/products", true)]
    [DataRow("/PRODUCTS", "/products", true)]
    [DataRow("/products/", "/products", true)]
    [DataRow("/products?page=2", "/products", true)]
    [DataRow("/products#top", "/products", true)]
    [DataRow("/products/1", "/products", false)]
    [DataRow("/products", "products", true)]
    [DataRow("/products", "./products", true)]
    public void BitNavBarShouldNormalizeBothSidesOfAnExactMatch(string currentUrl, string itemUrl, bool expected)
    {
        Navigate(currentUrl);

        var component = RenderNavBar([new() { Text = "Products", Url = itemUrl }]);

        Assert.AreEqual(expected, SelectedText(component) == "Products");
    }

    [TestMethod]
    [DataRow(BitNavMatch.Exact, "/products", true)]
    [DataRow(BitNavMatch.Exact, "/products/1", false)]
    [DataRow(BitNavMatch.Prefix, "/products/1", true)]
    [DataRow(BitNavMatch.Prefix, "/productsxyz", false)]
    [DataRow(BitNavMatch.Prefix, "/other", false)]
    public void BitNavBarShouldRespectTheMatchOfTheNavBar(BitNavMatch match, string currentUrl, bool expected)
    {
        Navigate(currentUrl);

        var component = RenderNavBar([new() { Text = "Products", Url = "/products" }], p => p.Add(c => c.Match, match));

        Assert.AreEqual(expected, SelectedText(component) == "Products");
    }

    [TestMethod]
    [DataRow("/components/navbar", "/components/*", true)]
    [DataRow("/components/nav/bar", "/components/*", false)]
    [DataRow("/components/nav/bar", "/components/**", true)]
    [DataRow("/components/nav", "/components/na?", true)]
    [DataRow("/components/navbar", "/components/na?", false)]
    public void BitNavBarShouldRespectTheWildcardMatch(string currentUrl, string pattern, bool expected)
    {
        Navigate(currentUrl);

        var component = RenderNavBar([new() { Text = "Pattern", Url = pattern }], p => p.Add(c => c.Match, BitNavMatch.Wildcard));

        Assert.AreEqual(expected, SelectedText(component) == "Pattern");
    }

    [TestMethod]
    [DataRow("/components/navbar", "^/components/nav(bar)?$", true)]
    [DataRow("/components/nav", "^/components/nav(bar)?$", true)]
    [DataRow("/components/pivot", "^/components/nav(bar)?$", false)]
    public void BitNavBarShouldRespectTheRegexMatch(string currentUrl, string pattern, bool expected)
    {
        Navigate(currentUrl);

        var component = RenderNavBar([new() { Text = "Pattern", Url = pattern }], p => p.Add(c => c.Match, BitNavMatch.Regex));

        Assert.AreEqual(expected, SelectedText(component) == "Pattern");
    }

    [TestMethod]
    public void BitNavBarShouldTreatAMalformedRegexAsANonMatch()
    {
        Navigate("/products");

        var component = RenderNavBar([new() { Text = "Pattern", Url = "([" }], p => p.Add(c => c.Match, BitNavMatch.Regex));

        Assert.IsNull(SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldLetTheMatchOfAnItemWinOverTheMatchOfTheNavBar()
    {
        Navigate("/products/1");

        var component = RenderNavBar(
        [
            new() { Text = "Home", Url = "/home" },
            new() { Text = "Products", Url = "/products", Match = BitNavMatch.Prefix },
        ], p => p.Add(c => c.Match, BitNavMatch.Exact));

        Assert.AreEqual("Products", SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldMatchTheAdditionalUrlsOfAnItem()
    {
        Navigate("/store");

        var component = RenderNavBar(
        [
            new() { Text = "Home", Url = "/home" },
            new() { Text = "Products", Url = "/products", AdditionalUrls = ["/store", "/shop"] },
        ]);

        Assert.AreEqual("Products", SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldClearTheSelectionWhenNothingMatchesTheUrl()
    {
        Navigate("/home");

        var component = RenderNavBar(UrlItems());

        Assert.AreEqual("Home", SelectedText(component));

        Navigate("/nowhere");

        Assert.IsNull(SelectedText(component));
        Assert.AreEqual(0, component.FindAll("[aria-current]").Count);
    }

    [TestMethod]
    public void BitNavBarShouldFollowTheUrlOnALocationChange()
    {
        Navigate("/home");

        var component = RenderNavBar(UrlItems());

        Assert.AreEqual("Home", SelectedText(component));

        Navigate("/profile");

        Assert.AreEqual("Profile", SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldNotFollowTheUrlInTheManualMode()
    {
        Navigate("/products");

        var component = RenderNavBar(UrlItems(), p => p.Add(c => c.Mode, BitNavMode.Manual));

        Assert.IsNull(SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldFollowTheUrlAfterTheModeSwitchesToAutomatic()
    {
        // The subscription is not tied to the mode, which is a parameter that can flip after the navbar is
        // initialized: a bar that switched to the automatic mode later on still has to follow the URL.
        Navigate("/products");

        var component = RenderNavBar(UrlItems(), p => p.Add(c => c.Mode, BitNavMode.Manual));

        Assert.IsNull(SelectedText(component));

        component.Render(p =>
        {
            p.Add(c => c.Items, UrlItems());
            p.Add(c => c.Mode, BitNavMode.Automatic);
        });

        Assert.AreEqual("Products", SelectedText(component));

        Navigate("/home");

        Assert.AreEqual("Home", SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldRematchAfterTheMatchChanges()
    {
        Navigate("/products/1");

        var items = UrlItems();
        var component = RenderNavBar(items);

        Assert.IsNull(SelectedText(component));

        component.Render(p =>
        {
            p.Add(c => c.Items, items);
            p.Add(c => c.Match, BitNavMatch.Prefix);
        });

        Assert.AreEqual("Products", SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldPickUpAnItemAppendedToTheSameCollection()
    {
        // The content of the collection is compared rather than only its identity, so a list that is
        // mutated in place is picked up too.
        Navigate("/settings");

        var items = UrlItems();
        var component = RenderNavBar(items);

        Assert.IsNull(SelectedText(component));

        items.Add(new() { Text = "Settings", Url = "/settings" });
        component.Render(p => p.Add(c => c.Items, items));

        CollectionAssert.AreEqual(new[] { "Home", "Products", "Profile", "Settings" }, Texts(component));
        Assert.AreEqual("Settings", SelectedText(component));
    }



    [TestMethod]
    public void BitNavBarShouldSelectTheClickedItemInTheManualMode()
    {
        var component = RenderNavBar(BasicItems(), p => p.Add(c => c.Mode, BitNavMode.Manual));

        component.FindAll(".bit-nbr-itm")[1].Click();

        Assert.AreEqual("Products", SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldRespectDefaultSelectedItem()
    {
        var items = BasicItems();

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.DefaultSelectedItem, items[2]);
        });

        Assert.AreEqual("Profile", SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldInvokeTheClickAndTheSelectCallbacks()
    {
        var items = BasicItems();
        BitNavBarItem? clicked = null;
        BitNavBarItem? selected = null;

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.OnItemClick, (BitNavBarItem i) => clicked = i);
            p.Add(c => c.OnSelectItem, (BitNavBarItem i) => selected = i);
        });

        component.FindAll(".bit-nbr-itm")[1].Click();

        Assert.AreSame(items[1], clicked);
        Assert.AreSame(items[1], selected);
    }

    [TestMethod]
    public void BitNavBarShouldInvokeTheSelectCallbackInTheAutomaticMode()
    {
        Navigate("/home");

        var items = UrlItems();
        var selected = new List<string?>();

        var component = RenderNavBar(items, p => p.Add(c => c.OnSelectItem, (BitNavBarItem i) => selected.Add(i?.Text)));

        Navigate("/profile");

        CollectionAssert.AreEqual(new[] { "Home", "Profile" }, selected);
    }

    [TestMethod]
    public void BitNavBarShouldSwallowTheEventsOfTheAlreadySelectedItem()
    {
        var items = BasicItems();
        var clicks = 0;
        var selects = 0;

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.DefaultSelectedItem, items[0]);
            p.Add(c => c.OnItemClick, (BitNavBarItem _) => clicks++);
            p.Add(c => c.OnSelectItem, (BitNavBarItem _) => selects++);
        });

        component.FindAll(".bit-nbr-itm")[0].Click();
        component.FindAll(".bit-nbr-itm")[0].Click();

        Assert.AreEqual(0, clicks);
        Assert.AreEqual(0, selects);
    }

    [TestMethod]
    public void BitNavBarShouldLetTheEventsThroughWhenReselectable()
    {
        var items = BasicItems();
        var clicks = 0;
        var selects = 0;

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.Reselectable, true);
            p.Add(c => c.DefaultSelectedItem, items[0]);
            p.Add(c => c.OnItemClick, (BitNavBarItem _) => clicks++);
            p.Add(c => c.OnSelectItem, (BitNavBarItem _) => selects++);
        });

        component.FindAll(".bit-nbr-itm")[0].Click();
        component.FindAll(".bit-nbr-itm")[0].Click();

        Assert.AreEqual(2, clicks);
        Assert.AreEqual(2, selects);
    }

    [TestMethod]
    public void BitNavBarShouldBindTheSelectedItemBothWays()
    {
        var items = BasicItems();
        BitNavBarItem? bound = null;

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Bind(c => c.SelectedItem, bound, v => bound = v);
        });

        component.FindAll(".bit-nbr-itm")[2].Click();

        Assert.AreSame(items[2], bound);
    }

    [TestMethod]
    public void BitNavBarShouldHighlightAValueEqualItem()
    {
        // The comparison goes through the default equality comparer of the item type, so a record or any
        // other value-equal item type highlights the selection correctly.
        var items = new List<ValueItem>
        {
            new("Home", "/home"),
            new("Products", "/products"),
        };

        var component = RenderComponent<BitNavBar<ValueItem>>(p =>
        {
            p.Add(c => c.Items, items);
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.SelectedItem, new ValueItem("Products", "/products"));
            p.Add(c => c.NameSelectors, new BitNavBarNameSelectors<ValueItem>
            {
                Text = { Selector = i => i.Text },
                Url = { Selector = i => i.Url },
            });
        });

        Assert.AreEqual("Products", component.Find(".bit-nbr-sel .bit-nbr-txt").TextContent);
    }

    public record ValueItem(string Text, string Url);



    [TestMethod]
    public async Task BitNavBarSelectItemShouldSelectAnItemProgrammatically()
    {
        var items = BasicItems();
        BitNavBarItem? selected = null;

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.OnSelectItem, (BitNavBarItem i) => selected = i);
        });

        await component.InvokeAsync(() => component.Instance.SelectItem(items[1]));

        Assert.AreEqual("Products", SelectedText(component));
        Assert.AreSame(items[1], selected);
    }

    [TestMethod]
    public async Task BitNavBarSelectItemShouldClearTheSelection()
    {
        var items = BasicItems();

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.DefaultSelectedItem, items[1]);
        });

        Assert.AreEqual("Products", SelectedText(component));

        await component.InvokeAsync(() => component.Instance.SelectItem(null));

        Assert.IsNull(SelectedText(component));
    }

    [TestMethod]
    public async Task BitNavBarFocusItemShouldFocusTheElementOfTheItem()
    {
        var items = BasicItems();
        var component = RenderNavBar(items);

        await component.InvokeAsync(async () => await component.Instance.FocusItem(items[2]));
        await component.InvokeAsync(async () => await component.Instance.FocusItem(items[0]));

        var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();
        var elements = component.FindAll(".bit-nbr-itm");

        Assert.AreEqual(2, focused.Count);
        focused[0].Arguments[0].ShouldBeElementReferenceTo(elements[2]);
        focused[1].Arguments[0].ShouldBeElementReferenceTo(elements[0]);
    }



    [TestMethod]
    [DataRow("ArrowRight", 0, 1)]
    [DataRow("ArrowDown", 0, 1)]
    [DataRow("ArrowLeft", 2, 1)]
    [DataRow("ArrowUp", 2, 1)]
    [DataRow("Home", 2, 0)]
    [DataRow("End", 0, 2)]
    public void BitNavBarShouldMoveTheFocusAlongTheBar(string key, int from, int expected)
    {
        var component = RenderNavBar(BasicItems());

        PressKeyOn(component, from, key);

        AssertFocused(component, expected);
    }

    [TestMethod]
    [DataRow("ArrowRight", 2, 1)]
    [DataRow("ArrowLeft", 0, 1)]
    public void BitNavBarShouldFlipTheHorizontalArrowsInRtl(string key, int from, int expected)
    {
        var component = RenderNavBar(BasicItems(), p => p.Add(c => c.Dir, BitDir.Rtl));

        PressKeyOn(component, from, key);

        AssertFocused(component, expected);
    }

    [TestMethod]
    public void BitNavBarShouldStopAtBothEndsOfTheBar()
    {
        var component = RenderNavBar(BasicItems());

        PressKeyOn(component, 0, "ArrowLeft");
        AssertFocused(component, 0);

        PressKeyOn(component, 2, "ArrowRight");
        AssertFocused(component, 2, 2);
    }

    [TestMethod]
    public void BitNavBarShouldWrapAtBothEndsWithWrapNavigation()
    {
        var component = RenderNavBar(BasicItems(), p => p.Add(c => c.WrapNavigation, true));

        PressKeyOn(component, 0, "ArrowLeft");
        AssertFocused(component, 2);

        PressKeyOn(component, 2, "ArrowRight");
        AssertFocused(component, 0, 2);
    }

    [TestMethod]
    public void BitNavBarShouldNotWrapHomeAndEndWithWrapNavigation()
    {
        // Home and End land inside the range, so the wrap must not carry them past an end.
        var component = RenderNavBar(BasicItems(), p => p.Add(c => c.WrapNavigation, true));

        PressKeyOn(component, 1, "Home");
        AssertFocused(component, 0);

        PressKeyOn(component, 1, "End");
        AssertFocused(component, 2, 2);
    }

    [TestMethod]
    public void BitNavBarShouldWrapOntoAnEnabledItemOnly()
    {
        var items = BasicItems();
        items[2].IsEnabled = false;

        var component = RenderNavBar(items, p => p.Add(c => c.WrapNavigation, true));

        PressKeyOn(component, 0, "ArrowLeft");

        AssertFocused(component, 1);
    }

    [TestMethod]
    public void BitNavBarShouldSkipADisabledItemWithTheArrowKeys()
    {
        // A disabled item renders as a native disabled button, which takes no focus at all, so walking onto
        // one would leave the focus where it was while the navbar believes it has moved.
        var items = BasicItems();
        items[1].IsEnabled = false;

        var component = RenderNavBar(items);

        PressKeyOn(component, 0, "ArrowRight");

        AssertFocused(component, 2);
    }

    [TestMethod]
    public void BitNavBarShouldNotHandleTheArrowKeysWhileDisabled()
    {
        var component = RenderNavBar(BasicItems(), p => p.Add(c => c.IsEnabled, false));

        component.FindAll(".bit-nbr-itm")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    [DataRow("ArrowRight")]
    [DataRow("Home")]
    public void BitNavBarShouldNotHandleAKeyWithAModifier(string key)
    {
        var component = RenderNavBar(BasicItems());

        component.FindAll(".bit-nbr-itm")[0].FocusIn();
        component.FindAll(".bit-nbr-itm")[0].KeyDown(new KeyboardEventArgs { Key = key, CtrlKey = true });

        Assert.AreEqual(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public void BitNavBarShouldLeaveEveryItemATabStopByDefault()
    {
        var component = RenderNavBar(UrlItems());

        foreach (var item in component.FindAll(".bit-nbr-itm"))
        {
            Assert.IsNull(item.GetAttribute("tabindex"));
        }
    }

    [TestMethod]
    public void BitNavBarShouldRoveTheTabStopWithSingleTabStop()
    {
        Navigate("/products");

        var component = RenderNavBar(UrlItems(), p => p.Add(c => c.SingleTabStop, true));

        var items = component.FindAll(".bit-nbr-itm");

        Assert.AreEqual("-1", items[0].GetAttribute("tabindex"));
        Assert.AreEqual("0", items[1].GetAttribute("tabindex"));
        Assert.AreEqual("-1", items[2].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitNavBarShouldPutTheSingleTabStopOnTheFirstItemWithoutASelection()
    {
        // A navbar without a reachable selection still has to be reachable itself.
        var component = RenderNavBar(BasicItems(), p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.SingleTabStop, true);
        });

        var items = component.FindAll(".bit-nbr-itm");

        Assert.AreEqual("0", items[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", items[1].GetAttribute("tabindex"));
        Assert.AreEqual("-1", items[2].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitNavBarShouldMoveTheSingleTabStopWithTheFocus()
    {
        // The stop follows the focus, so Tab comes back to the item the reader left the bar on rather than
        // jumping back to the selected one.
        var component = RenderNavBar(BasicItems(), p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.SingleTabStop, true);
        });

        PressKeyOn(component, 0, "End");

        var items = component.FindAll(".bit-nbr-itm");

        Assert.AreEqual("-1", items[0].GetAttribute("tabindex"));
        Assert.AreEqual("0", items[2].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitNavBarShouldKeepTheSingleTabStopOffADisabledFocusedItem()
    {
        var items = BasicItems();
        items[0].IsEnabled = false;

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.SingleTabStop, true);
        });

        // The stop follows the focus, but a disabled item is not one the keyboard walks, so focusing it
        // leaves the stop on the first item that is enabled instead of taking the bar out of the tab
        // sequence with it.
        component.FindAll(".bit-nbr-itm")[0].FocusIn();

        var rendered = component.FindAll(".bit-nbr-itm");

        Assert.AreEqual("-1", rendered[0].GetAttribute("tabindex"));
        Assert.AreEqual("0", rendered[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitNavBarShouldKeepASingleTabStopAfterTheFocusedOptionIsRemoved()
    {
        // An item that is gone would otherwise hold a stop no element carries, and take the whole bar out
        // of the tab sequence with it.
        var component = RenderComponent<BitNavBarOptionsTest>(parameters =>
        {
            parameters.Add(p => p.Mode, BitNavMode.Manual);
            parameters.Add(p => p.ShowMiddle, true);
            parameters.Add(p => p.SingleTabStop, true);
        });

        component.FindAll(".bit-nbr-itm")[1].FocusIn();

        Assert.AreEqual("0", component.FindAll(".bit-nbr-itm")[1].GetAttribute("tabindex"));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        var items = component.FindAll(".bit-nbr-itm");

        Assert.AreEqual(1, items.Count(i => i.GetAttribute("tabindex") == "0"));
        Assert.AreEqual("0", items[0].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitNavBarShouldMoveTheSingleTabStopWithTheSelection()
    {
        var component = RenderNavBar(BasicItems(), p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.SingleTabStop, true);
        });

        component.FindAll(".bit-nbr-itm")[2].Click();

        var items = component.FindAll(".bit-nbr-itm");

        Assert.AreEqual("-1", items[0].GetAttribute("tabindex"));
        Assert.AreEqual("0", items[2].GetAttribute("tabindex"));
    }



    [TestMethod]
    public void BitNavBarShouldRenderTheItemTemplateOfTheNavBar()
    {
        var component = RenderNavBar(BasicItems(), p =>
        {
            p.Add(c => c.ItemTemplate, (BitNavBarItem item) => (builder) =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-template");
                builder.AddContent(2, item.Text);
                builder.CloseElement();
            });
        });

        Assert.AreEqual(3, component.FindAll(".custom-template").Count);
        Assert.AreEqual(0, component.FindAll(".bit-nbr-txt").Count);
    }

    [TestMethod]
    public void BitNavBarShouldLetTheTemplateOfAnItemWinOverTheItemTemplate()
    {
        var items = BasicItems();
        items[1].Template = (item) => (builder) =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "item-template");
            builder.AddContent(2, item.Text);
            builder.CloseElement();
        };

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.ItemTemplate, (BitNavBarItem item) => (builder) =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-template");
                builder.AddContent(2, item.Text);
                builder.CloseElement();
            });
        });

        Assert.AreEqual(1, component.FindAll(".item-template").Count);
        Assert.AreEqual(2, component.FindAll(".custom-template").Count);
    }



    [TestMethod]
    public void BitNavBarShouldRespectTheClassAndTheStyleOfAnItem()
    {
        var items = BasicItems();
        items[1].Class = "custom-item";
        items[1].Style = "color: red";

        var component = RenderNavBar(items);

        var item = component.FindAll(".bit-nbr-itm")[1];

        Assert.IsTrue(item.ClassList.Contains("custom-item"));
        StringAssert.Contains(item.GetAttribute("style"), "color: red");
    }

    [TestMethod]
    public void BitNavBarShouldRespectTheClassesAndTheStyles()
    {
        var items = BasicItems();
        items[0].Badge = "1";

        Navigate("/nowhere");

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Classes, new BitNavBarClassStyles
            {
                Root = "root-class",
                Container = "container-class",
                Item = "item-class",
                ItemBadge = "badge-class",
                ItemIcon = "icon-class",
                ItemIconContainer = "icon-container-class",
                ItemText = "text-class",
            });
            p.Add(c => c.Styles, new BitNavBarClassStyles
            {
                Root = "background: red",
                Container = "background: green",
                Item = "background: blue",
                ItemBadge = "background: black",
                ItemIcon = "background: yellow",
                ItemIconContainer = "background: purple",
                ItemText = "background: orange",
            });
        });

        Assert.IsTrue(component.Find(".bit-nbr").ClassList.Contains("root-class"));
        StringAssert.Contains(component.Find(".bit-nbr").GetAttribute("style"), "background: red");

        Assert.IsTrue(component.Find(".bit-nbr-cnt").ClassList.Contains("container-class"));
        StringAssert.Contains(component.Find(".bit-nbr-cnt").GetAttribute("style"), "background: green");

        Assert.IsTrue(component.Find(".bit-nbr-itm").ClassList.Contains("item-class"));
        StringAssert.Contains(component.Find(".bit-nbr-itm").GetAttribute("style"), "background: blue");

        Assert.IsTrue(component.Find(".bit-nbr-bdg").ClassList.Contains("badge-class"));
        StringAssert.Contains(component.Find(".bit-nbr-bdg").GetAttribute("style"), "background: black");

        Assert.IsTrue(component.Find(".bit-nbr-ico").ClassList.Contains("icon-class"));
        StringAssert.Contains(component.Find(".bit-nbr-ico").GetAttribute("style"), "background: yellow");

        Assert.IsTrue(component.Find(".bit-nbr-icc").ClassList.Contains("icon-container-class"));
        StringAssert.Contains(component.Find(".bit-nbr-icc").GetAttribute("style"), "background: purple");

        Assert.IsTrue(component.Find(".bit-nbr-txt").ClassList.Contains("text-class"));
        StringAssert.Contains(component.Find(".bit-nbr-txt").GetAttribute("style"), "background: orange");
    }

    [TestMethod]
    public void BitNavBarShouldRespectTheSelectedItemClassAndStyle()
    {
        Navigate("/products");

        var component = RenderNavBar(UrlItems(), p =>
        {
            p.Add(c => c.Classes, new BitNavBarClassStyles { SelectedItem = "selected-class" });
            p.Add(c => c.Styles, new BitNavBarClassStyles { SelectedItem = "background: red" });
        });

        var items = component.FindAll(".bit-nbr-itm");

        Assert.IsFalse(items[0].ClassList.Contains("selected-class"));
        Assert.IsTrue(items[1].ClassList.Contains("selected-class"));
        StringAssert.Contains(items[1].GetAttribute("style"), "background: red");
    }

    [TestMethod]
    public void BitNavBarShouldNotRenderAnEmptyClassOrStyleFragment()
    {
        // The fragments are joined only when they carry something, so the item never ends up with a run of
        // blanks in its class list or with a stray semicolon in its style.
        var component = RenderNavBar(BasicItems());

        var item = component.Find(".bit-nbr-itm");

        Assert.AreEqual("bit-nbr-itm", item.GetAttribute("class"));
        Assert.AreEqual(string.Empty, item.GetAttribute("style"));
    }



    [TestMethod]
    public void BitNavBarShouldReadACustomItemTypeThroughTheNameSelectors()
    {
        Navigate("/products");

        var items = new List<BitNavBarCustomItem>
        {
            new() { Caption = "Home", Glyph = "Home", Link = "/home", Identifier = "home" },
            new()
            {
                Caption = "Products",
                Glyph = "ProductVariant",
                Link = "/products",
                Counter = "7",
                CounterLabel = "7 new products",
                SelectedGlyph = "ProductVariantSolid",
                Tooltip = "All the products",
                Window = "_blank",
                Current = BitNavAriaCurrent.Step,
                CssClass = "custom-item",
                CssStyle = "color: red",
            },
            new() { Caption = "Profile", Glyph = "Contact", Disabled = true, Marker = true, Label = "Your profile" },
        };

        var component = RenderComponent<BitNavBar<BitNavBarCustomItem>>(p =>
        {
            p.Add(c => c.Items, items);
            p.Add(c => c.NameSelectors, new BitNavBarNameSelectors<BitNavBarCustomItem>
            {
                Text = { Selector = i => i.Caption },
                IconName = { Selector = i => i.Glyph },
                Icon = { Selector = i => i.GlyphInfo },
                Url = { Selector = i => i.Link },
                AdditionalUrls = { Selector = i => i.ExtraLinks },
                Match = { Selector = i => i.Matching },
                Badge = { Selector = i => i.Counter },
                BadgeAriaLabel = { Selector = i => i.CounterLabel },
                SelectedIcon = { Selector = i => i.SelectedGlyphInfo },
                SelectedIconName = { Selector = i => i.SelectedGlyph },
                Dot = { Selector = i => i.Marker },
                IsEnabled = { Selector = i => i.Disabled is false },
                Class = { Selector = i => i.CssClass },
                Style = { Selector = i => i.CssStyle },
                Title = { Selector = i => i.Tooltip },
                AriaLabel = { Selector = i => i.Label },
                Target = { Selector = i => i.Window },
                Key = { Selector = i => i.Identifier },
                AriaCurrent = { Selector = i => i.Current },
            });
        });

        var rendered = component.FindAll(".bit-nbr-itm");

        CollectionAssert.AreEqual(new[] { "Home", "Products", "Profile" }, Texts(component));

        Assert.AreEqual("step", rendered[1].GetAttribute("aria-current"));
        // The Products item is the selected one here, so it reads the selected icon and its aria-label is
        // composed out of the explicit label the selector hands over and the description of its badge.
        Assert.IsTrue(component.FindAll(".bit-nbr-ico")[1].ClassList.Contains("bit-icon--ProductVariantSolid"));
        Assert.AreEqual("All the products", rendered[1].GetAttribute("title"));
        Assert.AreEqual("_blank", rendered[1].GetAttribute("target"));
        Assert.IsTrue(rendered[1].ClassList.Contains("custom-item"));
        StringAssert.Contains(rendered[1].GetAttribute("style"), "color: red");
        Assert.AreEqual("7", component.Find(".bit-nbr-bdg").TextContent);

        Assert.IsTrue(rendered[2].ClassList.Contains("bit-nbr-dis"));
        Assert.AreEqual("Products (7 new products)", rendered[1].GetAttribute("aria-label"));
        Assert.AreEqual("Your profile", rendered[2].GetAttribute("aria-label"));
        Assert.AreEqual(1, component.FindAll(".bit-nbr-bdt").Count);
    }

    [TestMethod]
    public void BitNavBarShouldReadACustomItemTypeByPropertyName()
    {
        var items = new List<BitNavBarItemLike>
        {
            new() { Text = "Home", IconName = "Home" },
            new() { Text = "Products", IconName = "ProductVariant", Badge = "3" },
        };

        var component = RenderComponent<BitNavBar<BitNavBarItemLike>>(p =>
        {
            p.Add(c => c.Items, items);
            p.Add(c => c.NameSelectors, new BitNavBarNameSelectors<BitNavBarItemLike>());
        });

        CollectionAssert.AreEqual(new[] { "Home", "Products" }, Texts(component));
        Assert.AreEqual("3", component.Find(".bit-nbr-bdg").TextContent);
    }

    public class BitNavBarItemLike
    {
        public string? Text { get; set; }
        public string? IconName { get; set; }
        public string? Badge { get; set; }
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

        CollectionAssert.AreEqual(new[] { "Home", "Settings" }, Texts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        CollectionAssert.AreEqual(new[] { "Home", "Profile", "Settings" }, Texts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "Home", "Settings" }, Texts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        CollectionAssert.AreEqual(new[] { "Home", "Profile", "Settings" }, Texts(component));
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

    [TestMethod]
    public void BitNavBarShouldRematchTheOptionsAfterTheUrlOfAnOptionChanges()
    {
        Navigate("/profile");

        var component = RenderComponent<BitNavBarUrlOptionsTest>(parameters =>
        {
            parameters.Add(p => p.ProfileUrl, "/profile");
        });

        Assert.AreEqual("Profile", SelectedText(component));

        component.Render(parameters => parameters.Add(p => p.ProfileUrl, "/somewhere-else"));

        Assert.IsNull(SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldNameTheNavigationLandmark()
    {
        // A page with more than one navigation landmark needs each of them named, otherwise they are all
        // announced as a bare "navigation".
        var component = RenderNavBar(BasicItems(), p => p.Add(c => c.AriaLabel, "Main navigation"));

        Assert.AreEqual("Main navigation", component.Find(".bit-nbr").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavBarShouldKeepTheKeyboardWorkingAfterAnOptionIsRemoved()
    {
        // An option that is removed takes its element registration with it, so the keyboard walks the
        // options that are left rather than addressing an element that is gone.
        var component = RenderComponent<BitNavBarOptionsTest>(parameters =>
        {
            parameters.Add(p => p.Mode, BitNavMode.Manual);
            parameters.Add(p => p.ShowMiddle, true);
        });

        PressKeyOn(component, 0, "End");
        AssertFocused(component, 2);

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "Home", "Settings" }, Texts(component));

        PressKeyOn(component, 0, "End");

        // The move lands on the last option that is left - the very element the first End landed on, which
        // is the one the Settings option still holds - and not on the element of the option that is gone.
        var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();

        Assert.AreEqual(2, focused.Count);
        Assert.AreEqual(((ElementReference)focused[0].Arguments[0]!).Id, ((ElementReference)focused[1].Arguments[0]!).Id);
        Assert.AreEqual("Settings", component.FindAll(".bit-nbr-txt")[^1].TextContent);
    }

    [TestMethod]
    public void BitNavBarShouldReadTheSelectedIconAndTheBadgeOfAnOption()
    {
        Navigate("/profile");

        var component = RenderComponent<BitNavBarBadgeOptionsTest>();

        var items = component.FindAll(".bit-nbr-itm");
        var icons = component.FindAll(".bit-nbr-ico");

        // The Profile option matches the current URL, so it is the one that swaps to its selected icon.
        Assert.IsTrue(icons[0].ClassList.Contains("bit-icon--Home"));
        Assert.IsTrue(icons[1].ClassList.Contains("bit-icon--ContactSolid"));

        Assert.AreEqual("Home (12)", items[0].GetAttribute("aria-label"));
        Assert.AreEqual("Profile (needs attention)", items[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavBarShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitNavBarHtmlAttributesTest>();

        var root = component.Find(".bit-nbr");

        var content = component.Find(".bit-nbr-cnt");

        Assert.AreEqual("bit", root.GetAttribute("data-val-test"));

        content.MarkupMatches(@"
<ul role=""list"" id:ignore class=""bit-nbr-cnt "">
    <li class=""bit-nbr-lit "">
        <button type=""button"" class=""bit-nbr-itm"" style="""">
            <span class=""bit-nbr-icc "">
                <i aria-hidden=""true"" class=""bit-nbr-ico bit-icon bit-icon--Home ""></i>
            </span>
            <span class=""bit-nbr-txt ""><span class=""bit-nbr-txc"">Home</span></span>
        </button>
    </li>
    <li class=""bit-nbr-lit "">
        <button type=""button"" class=""bit-nbr-itm"" style="""">
            <span class=""bit-nbr-icc "">
                <i aria-hidden=""true"" class=""bit-nbr-ico bit-icon bit-icon--ProductVariant ""></i>
            </span>
            <span class=""bit-nbr-txt ""><span class=""bit-nbr-txc"">Products</span></span>
        </button>
    </li>
</ul>");
    }



    [TestMethod]
    public void BitNavBarShouldRenderItsItemsAsAList()
    {
        // The destinations are marked up as a list inside the navigation landmark, which is what has a
        // screen reader announce how many of them the bar holds and which one it is on.
        var component = RenderNavBar(BasicItems());

        var list = component.Find("nav.bit-nbr > ul.bit-nbr-cnt");

        Assert.AreEqual("list", list.GetAttribute("role"));

        var wrappers = component.FindAll(".bit-nbr-cnt > li.bit-nbr-lit");

        Assert.AreEqual(3, wrappers.Count);
        Assert.IsTrue(wrappers.All(w => w.QuerySelector(".bit-nbr-itm") is not null));
    }

    [TestMethod]
    public void BitNavBarShouldRenderTheOptionsAsListItemsToo()
    {
        var component = RenderComponent<BitNavBarOptionsTest>(p => p.Add(c => c.ShowMiddle, true));

        Assert.AreEqual(3, component.FindAll(".bit-nbr-cnt > li.bit-nbr-lit > .bit-nbr-itm").Count);
    }

    [TestMethod]
    [DataRow(BitAlignment.Start, "bit-nbr-str")]
    [DataRow(BitAlignment.End, "bit-nbr-end")]
    [DataRow(BitAlignment.Center, "bit-nbr-ctr")]
    [DataRow(BitAlignment.SpaceBetween, "bit-nbr-sbt")]
    [DataRow(BitAlignment.SpaceAround, "bit-nbr-sar")]
    [DataRow(BitAlignment.SpaceEvenly, "bit-nbr-sev")]
    public void BitNavBarShouldRespectAlignment(BitAlignment alignment, string expectedClass)
    {
        var component = RenderNavBar(BasicItems(), p => p.Add(c => c.Alignment, alignment));

        Assert.IsTrue(component.Find(".bit-nbr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(BitAlignment.Baseline)]
    [DataRow(BitAlignment.Stretch)]
    public void BitNavBarShouldLeaveTheDistributionAloneWithoutAnAlignmentOfItsOwn(BitAlignment? alignment)
    {
        // Baseline and Stretch say how an item sits across the bar rather than how the items are spread
        // along it, so neither one (and no alignment at all) takes the bar off its default distribution.
        var component = RenderNavBar(BasicItems(), p =>
        {
            if (alignment.HasValue)
            {
                p.Add(c => c.Alignment, alignment.Value);
            }
        });

        var alignmentClasses = new[] { "bit-nbr-str", "bit-nbr-end", "bit-nbr-ctr", "bit-nbr-sbt", "bit-nbr-sar", "bit-nbr-sev" };

        Assert.IsFalse(component.Find(".bit-nbr").ClassList.Any(alignmentClasses.Contains));
    }

    [TestMethod]
    public void BitNavBarShouldRenderTheHeaderAndTheFooterOutsideOfTheListOfItems()
    {
        // Whatever the app puts beside its destinations is not one of them, so it stays out of the list and
        // is left out of the items a screen reader counts.
        var component = RenderNavBar(BasicItems(), p =>
        {
            p.Add(c => c.HeaderTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"the-header\">header</span>")));
            p.Add(c => c.FooterTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"the-footer\">footer</span>")));
        });

        Assert.IsNotNull(component.Find(".bit-nbr > .bit-nbr-hdr > .the-header"));
        Assert.IsNotNull(component.Find(".bit-nbr > .bit-nbr-ftr > .the-footer"));

        Assert.AreEqual(0, component.FindAll(".bit-nbr-cnt .the-header").Count);
        Assert.AreEqual(0, component.FindAll(".bit-nbr-cnt .the-footer").Count);
    }

    [TestMethod]
    public void BitNavBarShouldNotRenderAHeaderOrAFooterWithoutATemplate()
    {
        var component = RenderNavBar(BasicItems());

        Assert.AreEqual(0, component.FindAll(".bit-nbr-hdr").Count);
        Assert.AreEqual(0, component.FindAll(".bit-nbr-ftr").Count);
    }

    [TestMethod]
    public void BitNavBarShouldRespectTheHeaderFooterAndItemWrapperClassesAndStyles()
    {
        var component = RenderNavBar(BasicItems(), p =>
        {
            p.Add(c => c.HeaderTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span>header</span>")));
            p.Add(c => c.FooterTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span>footer</span>")));
            p.Add(c => c.Classes, new BitNavBarClassStyles
            {
                Header = "header-class",
                Footer = "footer-class",
                ItemWrapper = "wrapper-class"
            });
            p.Add(c => c.Styles, new BitNavBarClassStyles
            {
                Header = "color: red",
                Footer = "color: blue",
                ItemWrapper = "color: green"
            });
        });

        var header = component.Find(".bit-nbr-hdr");
        var footer = component.Find(".bit-nbr-ftr");
        var wrapper = component.Find(".bit-nbr-lit");

        Assert.IsTrue(header.ClassList.Contains("header-class"));
        Assert.IsTrue(footer.ClassList.Contains("footer-class"));
        Assert.IsTrue(wrapper.ClassList.Contains("wrapper-class"));

        StringAssert.Contains(header.GetAttribute("style"), "color: red");
        StringAssert.Contains(footer.GetAttribute("style"), "color: blue");
        StringAssert.Contains(wrapper.GetAttribute("style"), "color: green");
    }

    [TestMethod]
    public void BitNavBarShouldLayABadgeWithoutAnIconInTheFlowOfItsItem()
    {
        // A badge is positioned against the icon of its item, and an item without one has nothing to pin it
        // to, so its wrapper is marked and the badge is laid out in the flow instead.
        var items = BasicItems();
        items[0].IconName = null;
        items[0].Badge = "3";
        items[1].Badge = "4";

        var component = RenderNavBar(items);

        var containers = component.FindAll(".bit-nbr-icc");

        Assert.IsTrue(containers[0].ClassList.Contains("bit-nbr-nic"));
        Assert.IsFalse(containers[1].ClassList.Contains("bit-nbr-nic"));
    }

    [TestMethod]
    public void BitNavBarShouldMarkTheOptionsForTheOrderReadBackOnlyWithAutoReorderOptions()
    {
        // The marker is what the DOM read-back recovers the markup order from, so it is only rendered for
        // the navbars that asked for the reordering.
        var withReorder = RenderComponent<BitNavBarOptionsTest>(p =>
        {
            p.Add(c => c.ShowMiddle, true);
            p.Add(c => c.AutoReorderOptions, true);
        });

        Assert.AreEqual(3, withReorder.FindAll("[data-bit-nbr-opt]").Count);

        var withoutReorder = RenderComponent<BitNavBarOptionsTest>(p => p.Add(c => c.ShowMiddle, true));

        Assert.AreEqual(0, withoutReorder.FindAll("[data-bit-nbr-opt]").Count);
    }

    [TestMethod]
    public void BitNavBarShouldWalkTheOptionsInTheirRegistrationOrderWithoutAutoReorderOptions()
    {
        // An option that shows up after the first render registers itself at the end of the list, so the
        // keyboard reaches it last even though it is rendered in the middle of the bar. That is the very
        // thing AutoReorderOptions is there to correct.
        var component = RenderComponent<BitNavBarOptionsTest>(p => p.Add(c => c.SingleTabStop, true));

        component.Render(p => p.Add(c => c.ShowMiddle, true));

        CollectionAssert.AreEqual(new[] { "Home", "Profile", "Settings" }, Texts(component));

        PressKeyOn(component, 0, "ArrowRight");

        // The move lands on Settings, which is rendered last but registered second.
        AssertTabStop(component, 2);
    }

    [TestMethod]
    public void BitNavBarShouldWalkTheOptionsInTheirMarkupOrderWithAutoReorderOptions()
    {
        var component = RenderComponent<BitNavBarOptionsTest>(p =>
        {
            p.Add(c => c.SingleTabStop, true);
            p.Add(c => c.AutoReorderOptions, true);
        });

        component.Render(p => p.Add(c => c.ShowMiddle, true));

        // What the browser would have reported: the markers of the options in the order they are rendered.
        var orderedIds = component.FindAll("[data-bit-nbr-opt]")
                                  .Select(e => e.GetAttribute("data-bit-nbr-opt")!)
                                  .ToArray();

        Assert.AreEqual(3, orderedIds.Length);

        Context.JSInterop.Setup<string[]>("BitBlazorUI.Utils.getChildrenAttributes", _ => true).SetResult(orderedIds);

        // Any render re-runs the read-back, which is what the change that added the option gives the navbar
        // in the first place.
        component.Render(p => p.Add(c => c.ShowMiddle, true));

        PressKeyOn(component, 0, "ArrowRight");

        // The move now lands on the option that is rendered second, whenever it happened to register.
        AssertTabStop(component, 1);
    }

    [TestMethod]
    public void BitNavBarShouldKeepAnOptionThatHasNotRenderedItsMarkerYet()
    {
        // A read-back that only reports a part of the options (one of them has registered but has not
        // rendered its marker yet) leaves the rest at the end of the list rather than dropping them.
        var component = RenderComponent<BitNavBarOptionsTest>(p =>
        {
            p.Add(c => c.SingleTabStop, true);
            p.Add(c => c.AutoReorderOptions, true);
        });

        component.Render(p => p.Add(c => c.ShowMiddle, true));

        var reportedIds = component.FindAll("[data-bit-nbr-opt]")
                                   .Select(e => e.GetAttribute("data-bit-nbr-opt")!)
                                   .Take(2)
                                   .ToArray();

        Context.JSInterop.Setup<string[]>("BitBlazorUI.Utils.getChildrenAttributes", _ => true).SetResult(reportedIds);

        component.Render(p => p.Add(c => c.ShowMiddle, true));

        Assert.AreEqual(3, component.FindAll(".bit-nbr-itm").Count);

        PressKeyOn(component, 0, "End");

        AssertTabStop(component, 2);
    }


    [TestMethod]
    [DataRow(true, "bit-nbr-scr")]
    [DataRow(false, "")]
    public void BitNavBarShouldRespectScrollable(bool scrollable, string expectedClass)
    {
        var component = RenderNavBar(BasicItems(), p => p.Add(c => c.Scrollable, scrollable));

        AssertRootClass(component, expectedClass, "bit-nbr-scr");
    }

    [TestMethod]
    [DataRow(BitNavBarIndicator.Line, "bit-nbr-lin")]
    [DataRow(BitNavBarIndicator.Pill, "bit-nbr-pil")]
    public void BitNavBarShouldRespectTheIndicator(BitNavBarIndicator indicator, string expectedClass)
    {
        var component = RenderNavBar(BasicItems(), p => p.Add(c => c.Indicator, indicator));

        Assert.IsTrue(component.Find(".bit-nbr").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(BitNavBarIndicator.None)]
    public void BitNavBarShouldRenderNoIndicatorClassWithoutAnIndicator(BitNavBarIndicator? indicator)
    {
        var component = RenderNavBar(BasicItems(), p =>
        {
            if (indicator.HasValue)
            {
                p.Add(c => c.Indicator, indicator.Value);
            }
        });

        var root = component.Find(".bit-nbr");

        Assert.IsFalse(root.ClassList.Contains("bit-nbr-lin"));
        Assert.IsFalse(root.ClassList.Contains("bit-nbr-pil"));
    }

    [TestMethod]
    public void BitNavBarShouldScrollTheSelectedItemIntoViewWhileScrollable()
    {
        // The selection moves without a pointer or a keyboard of its own behind it, so the item it lands on
        // is brought into the scrolled area of the navbar rather than being left outside of it.
        var component = RenderNavBar(BasicItems(), p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.Scrollable, true);
        });

        Assert.AreEqual(0, ScrollCalls());

        component.FindAll(".bit-nbr-itm")[1].Click();

        Assert.AreEqual(1, ScrollCalls());
        Assert.AreEqual(component.Find(".bit-nbr-cnt").Id, LastScrollCall().Arguments[0]);
    }

    [TestMethod]
    public void BitNavBarShouldScrollTheItemTheUrlSelectsIntoView()
    {
        Navigate("/home");

        var component = RenderNavBar(UrlItems(), p => p.Add(c => c.Scrollable, true));

        var initialCalls = ScrollCalls();

        Navigate("/profile");

        Assert.IsTrue(ScrollCalls() > initialCalls);
    }

    [TestMethod]
    public void BitNavBarShouldNotScrollAnythingWithoutScrollable()
    {
        var component = RenderNavBar(BasicItems(), p => p.Add(c => c.Mode, BitNavMode.Manual));

        component.FindAll(".bit-nbr-itm")[1].Click();

        Assert.AreEqual(0, ScrollCalls());
    }

    [TestMethod]
    public void BitNavBarShouldSelectTheFocusedItemWithSelectOnFocus()
    {
        // The selection follows the focus in that mode, the way the tabs of a tab list do.
        var items = BasicItems();
        BitNavBarItem? selected = null;

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.SelectOnFocus, true);
            p.Add(c => c.OnSelectItem, (BitNavBarItem i) => selected = i);
        });

        component.FindAll(".bit-nbr-itm")[1].FocusIn();

        Assert.AreEqual("Products", SelectedText(component));
        Assert.AreSame(items[1], selected);
    }

    [TestMethod]
    public void BitNavBarShouldLeaveTheSelectionAloneOnFocusWithoutSelectOnFocus()
    {
        var component = RenderNavBar(BasicItems(), p => p.Add(c => c.Mode, BitNavMode.Manual));

        component.FindAll(".bit-nbr-itm")[1].FocusIn();

        Assert.IsNull(SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldNotSelectOnFocusInTheAutomaticMode()
    {
        // In the automatic mode the current URL is what selects an item, and a selection made on focus
        // would be undone by the very next match anyway.
        Navigate("/home");

        var component = RenderNavBar(UrlItems(), p => p.Add(c => c.SelectOnFocus, true));

        component.FindAll(".bit-nbr-itm")[1].FocusIn();

        Assert.AreEqual("Home", SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldNotSelectOnFocusWhileTheNavBarIsDisabled()
    {
        var component = RenderNavBar(BasicItems(), p =>
        {
            p.Add(c => c.IsEnabled, false);
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.SelectOnFocus, true);
        });

        component.FindAll(".bit-nbr-itm")[1].FocusIn();

        Assert.IsNull(SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldNotSelectADisabledItemOnFocus()
    {
        var items = BasicItems();
        items[1].IsEnabled = false;

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.SelectOnFocus, true);
        });

        component.FindAll(".bit-nbr-itm")[1].FocusIn();

        Assert.IsNull(SelectedText(component));
    }

    [TestMethod]
    public void BitNavBarShouldSelectTheOptionsOnFocusToo()
    {
        var component = RenderComponent<BitNavBarOptionsTest>(parameters =>
        {
            parameters.Add(p => p.Mode, BitNavMode.Manual);
            parameters.Add(p => p.ShowMiddle, true);
            parameters.Add(p => p.SelectOnFocus, true);
        });

        component.FindAll(".bit-nbr-itm")[1].FocusIn();

        Assert.AreEqual("Profile", SelectedText(component));
    }

    [TestMethod]
    public async Task BitNavBarScrollItemIntoViewShouldScrollAnyItemOfAScrollableNavBar()
    {
        var items = BasicItems();
        var component = RenderNavBar(items, p => p.Add(c => c.Scrollable, true));

        await component.InvokeAsync(async () => await component.Instance.ScrollItemIntoView(items[2]));

        Assert.AreEqual(1, ScrollCalls());
        Assert.AreEqual(component.Find(".bit-nbr-cnt").Id, LastScrollCall().Arguments[0]);
        LastScrollCall().Arguments[1].ShouldBeElementReferenceTo(component.FindAll(".bit-nbr-itm")[2]);
    }

    [TestMethod]
    public async Task BitNavBarScrollItemIntoViewShouldDoNothingWithoutScrollable()
    {
        var items = BasicItems();
        var component = RenderNavBar(items);

        await component.InvokeAsync(async () => await component.Instance.ScrollItemIntoView(items[2]));

        Assert.AreEqual(0, ScrollCalls());
    }


    [TestMethod]
    public void BitNavBarShouldReplaceTheItemWithItsTemplateInTheReplaceMode()
    {
        // An item that is a control of its own cannot be nested in the anchor (or the button) the navbar
        // would have rendered for it, so the template renders that element itself instead.
        var items = BasicItems();
        items[1].TemplateRenderMode = BitNavItemTemplateRenderMode.Replace;
        items[1].Template = (item) => (builder) =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "replaced-item");
            builder.AddContent(2, item.Text);
            builder.CloseElement();
        };

        var component = RenderNavBar(items);

        var wrappers = component.FindAll(".bit-nbr-lit");

        Assert.AreEqual(3, wrappers.Count);
        Assert.AreEqual(2, component.FindAll(".bit-nbr-itm").Count);
        Assert.AreEqual(1, component.FindAll(".bit-nbr-lit > .replaced-item").Count);
        Assert.IsNull(wrappers[1].QuerySelector(".bit-nbr-itm"));
    }

    [TestMethod]
    public void BitNavBarShouldKeepTheTemplateInsideTheItemInTheNormalMode()
    {
        var items = BasicItems();
        items[1].Template = (item) => (builder) =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "item-template");
            builder.AddContent(2, item.Text);
            builder.CloseElement();
        };

        var component = RenderNavBar(items);

        Assert.AreEqual(3, component.FindAll(".bit-nbr-itm").Count);
        Assert.AreEqual(1, component.FindAll(".bit-nbr-itm > .item-template").Count);
    }

    [TestMethod]
    public void BitNavBarShouldReplaceEveryItemWithTheItemTemplateInTheReplaceMode()
    {
        var component = RenderNavBar(BasicItems(), p =>
        {
            p.Add(c => c.ItemTemplateRenderMode, BitNavItemTemplateRenderMode.Replace);
            p.Add(c => c.ItemTemplate, (RenderFragment<BitNavBarItem>)(item => builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "replaced-item");
                builder.AddContent(2, item.Text);
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(0, component.FindAll(".bit-nbr-itm").Count);
        Assert.AreEqual(3, component.FindAll(".bit-nbr-lit > .replaced-item").Count);
    }

    [TestMethod]
    public void BitNavBarShouldLetTheModeOfAnItemWinOverTheModeOfTheNavBar()
    {
        // The template of an item wins over the navbar's own, so the mode that goes with it wins as well.
        var items = BasicItems();
        items[1].Template = (item) => (builder) =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "item-template");
            builder.AddContent(2, item.Text);
            builder.CloseElement();
        };

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.ItemTemplateRenderMode, BitNavItemTemplateRenderMode.Replace);
            p.Add(c => c.ItemTemplate, (RenderFragment<BitNavBarItem>)(item => builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "replaced-item");
                builder.AddContent(2, item.Text);
                builder.CloseElement();
            }));
        });

        // The item that carries a template of its own keeps the navbar's element around it, since its own
        // mode is the default one; the rest are replaced by the navbar's template.
        Assert.AreEqual(1, component.FindAll(".bit-nbr-itm > .item-template").Count);
        Assert.AreEqual(2, component.FindAll(".bit-nbr-lit > .replaced-item").Count);
    }

    [TestMethod]
    public void BitNavBarShouldSkipAReplacedItemWithTheArrowKeys()
    {
        // A replaced item renders no element of the navbar's, so walking onto one would leave the focus
        // where it was while the navbar believes it has moved.
        var items = BasicItems();
        items[1].TemplateRenderMode = BitNavItemTemplateRenderMode.Replace;
        items[1].Template = (item) => (builder) =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "replaced-item");
            builder.AddContent(2, item.Text);
            builder.CloseElement();
        };

        var component = RenderNavBar(items);

        PressKeyOn(component, 0, "ArrowRight");

        AssertFocused(component, 1);
    }

    [TestMethod]
    public void BitNavBarShouldKeepTheSingleTabStopOffAReplacedItem()
    {
        var items = BasicItems();
        items[0].TemplateRenderMode = BitNavItemTemplateRenderMode.Replace;
        items[0].Template = (item) => (builder) =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "replaced-item");
            builder.AddContent(2, item.Text);
            builder.CloseElement();
        };

        var component = RenderNavBar(items, p =>
        {
            p.Add(c => c.Mode, BitNavMode.Manual);
            p.Add(c => c.SingleTabStop, true);
        });

        // The first item the navbar renders itself holds the stop, since the replaced one carries no
        // element of the navbar's to put it on.
        AssertTabStop(component, 0);
    }

    [TestMethod]
    public void BitNavBarShouldReplaceAnOptionWithItsTemplateInTheReplaceMode()
    {
        var component = RenderComponent<BitNavBarReplacedOptionTest>();

        Assert.AreEqual(2, component.FindAll(".bit-nbr-lit").Count);
        Assert.AreEqual(1, component.FindAll(".bit-nbr-itm").Count);
        Assert.AreEqual(1, component.FindAll(".bit-nbr-lit > .replaced-option").Count);
    }



    // How many times the navbar has asked the browser to bring an item into its scrolled area.
    private int ScrollCalls()
    {
        return Context.JSInterop.Invocations["BitBlazorUI.NavBar.scrollItemIntoView"].Count;
    }

    private Bunit.JSRuntimeInvocation LastScrollCall()
    {
        return Context.JSInterop.Invocations["BitBlazorUI.NavBar.scrollItemIntoView"].Last();
    }



    private static void AssertRootClass(IRenderedComponent<IComponent> component, string expectedClass, string toggledClass)
    {
        var root = component.Find(".bit-nbr");

        if (string.IsNullOrEmpty(expectedClass))
        {
            Assert.IsFalse(root.ClassList.Contains(toggledClass));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains(expectedClass));
        }
    }

    // Where the single stop of the roving tab index sits, which follows the focus and is what the order of
    // the items is observable through without reaching for the element the focus call was handed.
    private static void AssertTabStop(IRenderedComponent<IComponent> component, int expectedIndex)
    {
        var items = component.FindAll(".bit-nbr-itm");

        for (var i = 0; i < items.Count; i++)
        {
            Assert.AreEqual(i == expectedIndex ? "0" : "-1", items[i].GetAttribute("tabindex"));
        }
    }

    private static void PressKeyOn(IRenderedComponent<IComponent> component, int index, string key)
    {
        var item = component.FindAll(".bit-nbr-itm")[index];
        item.FocusIn();
        item.KeyDown(new KeyboardEventArgs { Key = key });
    }

    private void AssertFocused(IRenderedComponent<IComponent> component, int expectedIndex, int expectedCallCount = 1)
    {
        var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();

        Assert.AreEqual(expectedCallCount, focused.Count);
        focused[^1].Arguments[0].ShouldBeElementReferenceTo(component.FindAll(".bit-nbr-itm")[expectedIndex]);
    }
}
