using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Layout;

[TestClass]
public class BitLayoutTests : BunitTestContext
{
    [TestMethod]
    public void BitLayoutShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitLayout>();

        // The header, the nav panel, the aside and the footer are only rendered once they are given
        // content, so the bare layout is the middle row around a main landmark and nothing else.
        component.MarkupMatches(@"
<div class=""bit-lyt"" id:ignore>
    <div class=""bit-lyt-man"">
        <main class=""bit-lyt-mcn"" id:ignore></main>
    </div>
</div>");
    }

    [TestMethod]
    public void BitLayoutShouldRenderEverySectionAsItsLandmarkElement()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add<RenderFragment>(p => p.Header, builder => builder.AddMarkupContent(0, "<div>Header</div>"));
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(1, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(2, "<div>Main</div>"));
            parameters.Add<RenderFragment>(p => p.Aside, builder => builder.AddMarkupContent(3, "<div>Aside</div>"));
            parameters.Add<RenderFragment>(p => p.Footer, builder => builder.AddMarkupContent(4, "<div>Footer</div>"));
        });

        Assert.AreEqual("HEADER", component.Find(".bit-lyt-hdr").TagName);
        Assert.AreEqual("NAV", component.Find(".bit-lyt-nmn").TagName);
        Assert.AreEqual("MAIN", component.Find(".bit-lyt-mcn").TagName);
        Assert.AreEqual("ASIDE", component.Find(".bit-lyt-asd").TagName);
        Assert.AreEqual("FOOTER", component.Find(".bit-lyt-ftr").TagName);
    }

    [TestMethod]
    public void BitLayoutShouldKeepTheNavPanelAndTheAsideOutsideOfTheMainLandmark()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(0, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(1, "<div>Main</div>"));
            parameters.Add<RenderFragment>(p => p.Aside, builder => builder.AddMarkupContent(2, "<div>Aside</div>"));
        });

        var main = component.Find("main");

        // A "skip to main content" that lands in front of the navigation skips nothing, so the two
        // panels have to be siblings of the main landmark rather than children of it.
        Assert.AreEqual(0, main.QuerySelectorAll("nav").Length);
        Assert.AreEqual(0, main.QuerySelectorAll("aside").Length);
    }

    [TestMethod]
    public void BitLayoutShouldRenderTheSectionsInTheirReadingOrder()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.SkipLink, true);
            parameters.Add<RenderFragment>(p => p.Header, builder => builder.AddMarkupContent(0, "<div>Header</div>"));
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(1, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(2, "<div>Main</div>"));
            parameters.Add<RenderFragment>(p => p.Aside, builder => builder.AddMarkupContent(3, "<div>Aside</div>"));
            parameters.Add<RenderFragment>(p => p.Footer, builder => builder.AddMarkupContent(4, "<div>Footer</div>"));
        });

        var rootChildren = component.Find(".bit-lyt").Children;

        // The skip link is the first focusable element of the layout, or it skips nothing.
        Assert.AreEqual(4, rootChildren.Length);
        Assert.IsTrue(rootChildren[0].ClassList.Contains("bit-lyt-skp"));
        Assert.IsTrue(rootChildren[1].ClassList.Contains("bit-lyt-hdr"));
        Assert.IsTrue(rootChildren[2].ClassList.Contains("bit-lyt-man"));
        Assert.IsTrue(rootChildren[3].ClassList.Contains("bit-lyt-ftr"));

        var rowChildren = component.Find(".bit-lyt-man").Children;

        Assert.AreEqual(3, rowChildren.Length);
        Assert.IsTrue(rowChildren[0].ClassList.Contains("bit-lyt-nmn"));
        Assert.IsTrue(rowChildren[1].ClassList.Contains("bit-lyt-mcn"));
        Assert.IsTrue(rowChildren[2].ClassList.Contains("bit-lyt-asd"));
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    [DataRow(false, false)]
    public void BitLayoutShouldRespectStickyHeaderAndFooter(bool stickyHeader, bool stickyFooter)
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.StickyHeader, stickyHeader);
            parameters.Add(p => p.StickyFooter, stickyFooter);
        });

        var root = component.Find(".bit-lyt");

        Assert.AreEqual(stickyHeader, root.ClassList.Contains("bit-lyt-shd"));
        Assert.AreEqual(stickyFooter, root.ClassList.Contains("bit-lyt-sft"));
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    [DataRow(false, false)]
    public void BitLayoutShouldRespectStickyNavPanelAndAside(bool stickyNavPanel, bool stickyAside)
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.StickyNavPanel, stickyNavPanel);
            parameters.Add(p => p.StickyAside, stickyAside);
        });

        var root = component.Find(".bit-lyt");

        Assert.AreEqual(stickyNavPanel, root.ClassList.Contains("bit-lyt-snv"));
        Assert.AreEqual(stickyAside, root.ClassList.Contains("bit-lyt-sas"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitLayoutShouldRespectFullHeight(bool fullHeight)
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.FullHeight, fullHeight);
        });

        Assert.AreEqual(fullHeight, component.Find(".bit-lyt").ClassList.Contains("bit-lyt-flh"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitLayoutShouldRespectBordered(bool bordered)
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.Bordered, bordered);
        });

        Assert.AreEqual(bordered, component.Find(".bit-lyt").ClassList.Contains("bit-lyt-brd"));
    }

    [TestMethod]
    public void BitLayoutShouldResetTheClassesWhenAParameterChangesAtRuntime()
    {
        var component = RenderComponent<BitLayout>();

        Assert.IsFalse(component.Find(".bit-lyt").ClassList.Contains("bit-lyt-shd"));

        component.Render(parameters => parameters.Add(p => p.StickyHeader, true));

        Assert.IsTrue(component.Find(".bit-lyt").ClassList.Contains("bit-lyt-shd"));

        component.Render(parameters => parameters.Add(p => p.StickyHeader, false));

        Assert.IsFalse(component.Find(".bit-lyt").ClassList.Contains("bit-lyt-shd"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.FullHeightPanels, true);
            parameters.Add(p => p.ScrollableMain, true);
        });

        Assert.IsTrue(component.Find(".bit-lyt").ClassList.Contains("bit-lyt-fhp"));
        Assert.IsTrue(component.Find(".bit-lyt").ClassList.Contains("bit-lyt-scm"));
    }

    [TestMethod]
    public void BitLayoutShouldSwapTheMainElementWhenNestedChangesAtRuntime()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(0, "<div>Main</div>"));
        });

        Assert.AreEqual("MAIN", component.Find(".bit-lyt-mcn").TagName);

        component.Render(parameters => parameters.Add(p => p.Nested, true));

        Assert.AreEqual("DIV", component.Find(".bit-lyt-mcn").TagName);

        component.Render(parameters => parameters.Add(p => p.Nested, false));

        Assert.AreEqual("MAIN", component.Find(".bit-lyt-mcn").TagName);
    }

    [TestMethod]
    public void BitLayoutShouldRespectTheWidthsOfBothPanelsWhenFullHeightPanelsChangesAtRuntime()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.NavPanelWidth, 250);
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(0, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(1, "<div>Main</div>"));
        });

        StringAssert.Contains(component.Find(".bit-lyt-mcn").GetAttribute("style")!, "width:calc(100% - 250px)");
        Assert.IsFalse(component.Find(".bit-lyt-nmn").HasAttribute("style"));

        // The width moves from the room the main content reserves to the panel itself, and back.
        component.Render(parameters => parameters.Add(p => p.FullHeightPanels, true));

        Assert.IsFalse(component.Find(".bit-lyt-mcn").HasAttribute("style"));
        StringAssert.Contains(component.Find(".bit-lyt-nmn").GetAttribute("style")!, "width:250px");

        component.Render(parameters => parameters.Add(p => p.FullHeightPanels, false));

        StringAssert.Contains(component.Find(".bit-lyt-mcn").GetAttribute("style")!, "width:calc(100% - 250px)");
        Assert.IsFalse(component.Find(".bit-lyt-nmn").HasAttribute("style"));
    }

    [TestMethod]
    public void BitLayoutShouldHideNavPanelWhenHideNavPanelIsTrue()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.HideNavPanel, true);
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(0, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(1, "<div>Main</div>"));
        });

        Assert.AreEqual(0, component.FindAll(".bit-lyt-nmn").Count);
    }

    [TestMethod]
    public void BitLayoutShouldHideEverySectionOnItsOwn()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.HideHeader, true);
            parameters.Add(p => p.HideNavPanel, true);
            parameters.Add(p => p.HideAside, true);
            parameters.Add(p => p.HideFooter, true);
            parameters.Add<RenderFragment>(p => p.Header, builder => builder.AddMarkupContent(0, "<div>Header</div>"));
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(1, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(2, "<div>Main</div>"));
            parameters.Add<RenderFragment>(p => p.Aside, builder => builder.AddMarkupContent(3, "<div>Aside</div>"));
            parameters.Add<RenderFragment>(p => p.Footer, builder => builder.AddMarkupContent(4, "<div>Footer</div>"));
        });

        Assert.AreEqual(0, component.FindAll(".bit-lyt-hdr").Count);
        Assert.AreEqual(0, component.FindAll(".bit-lyt-nmn").Count);
        Assert.AreEqual(0, component.FindAll(".bit-lyt-asd").Count);
        Assert.AreEqual(0, component.FindAll(".bit-lyt-ftr").Count);

        // The main section is the one landmark a page is expected to have exactly one of, so it stays.
        Assert.AreEqual(1, component.FindAll(".bit-lyt-mcn").Count);
    }

    [TestMethod]
    public void BitLayoutShouldNotRenderTheSectionsThatHaveNoContent()
    {
        var component = RenderComponent<BitLayout>();

        Assert.AreEqual(0, component.FindAll(".bit-lyt-hdr").Count);
        Assert.AreEqual(0, component.FindAll(".bit-lyt-nmn").Count);
        Assert.AreEqual(0, component.FindAll(".bit-lyt-asd").Count);
        Assert.AreEqual(0, component.FindAll(".bit-lyt-ftr").Count);
        Assert.AreEqual(0, component.FindAll(".bit-lyt-skp").Count);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitLayoutShouldRespectReverseNavPanel(bool reverse)
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.ReverseNavPanel, reverse);
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(0, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(1, "<div>Main</div>"));
        });

        // The reversal is a class of the root rather than an inline style of the row, so it stays out of
        // the way of Styles.Main and can be themed like everything else.
        Assert.AreEqual(reverse, component.Find(".bit-lyt").ClassList.Contains("bit-lyt-rnv"));
    }

    [TestMethod]
    public void BitLayoutShouldRespectNavPanelWidth()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.NavPanelWidth, 250);
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(0, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(1, "<div>Main</div>"));
        });

        Assert.IsTrue(component.Find(".bit-lyt-mcn").GetAttribute("style")!.Contains("width:calc(100% - 250px)"));
    }

    [TestMethod]
    public void BitLayoutShouldReserveTheRoomOfBothPanelsInTheMainContent()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.NavPanelWidth, 250);
            parameters.Add(p => p.AsideWidth, 150);
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(0, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(1, "<div>Main</div>"));
            parameters.Add<RenderFragment>(p => p.Aside, builder => builder.AddMarkupContent(2, "<div>Aside</div>"));
        });

        Assert.IsTrue(component.Find(".bit-lyt-mcn").GetAttribute("style")!.Contains("width:calc(100% - 400px)"));
    }

    [TestMethod]
    public void BitLayoutShouldGiveTheRoomOfAHiddenPanelBackToTheMainContent()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.NavPanelWidth, 250);
            parameters.Add(p => p.AsideWidth, 150);
            parameters.Add(p => p.HideNavPanel, true);
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(0, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(1, "<div>Main</div>"));
            parameters.Add<RenderFragment>(p => p.Aside, builder => builder.AddMarkupContent(2, "<div>Aside</div>"));
        });

        // Only the aside is left to reserve room for.
        Assert.IsTrue(component.Find(".bit-lyt-mcn").GetAttribute("style")!.Contains("width:calc(100% - 150px)"));
    }

    [TestMethod]
    public void BitLayoutShouldNotReserveAnyRoomWhenThereIsNoPanelToReserveItFor()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            // The widths are set, but neither panel has any content, so nothing is rendered for them.
            parameters.Add(p => p.NavPanelWidth, 250);
            parameters.Add(p => p.AsideWidth, 150);
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(0, "<div>Main</div>"));
        });

        var style = component.Find(".bit-lyt-mcn").GetAttribute("style");

        Assert.IsTrue(string.IsNullOrWhiteSpace(style), $"Expected no inline width, found '{style}'.");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitLayoutShouldRespectFullHeightPanels(bool fullHeightPanels)
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.FullHeightPanels, fullHeightPanels);
        });

        Assert.AreEqual(fullHeightPanels, component.Find(".bit-lyt").ClassList.Contains("bit-lyt-fhp"));
    }

    [TestMethod]
    public void BitLayoutShouldGiveThePanelsTheirOwnWidthUnderFullHeightPanels()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.FullHeightPanels, true);
            parameters.Add(p => p.NavPanelWidth, 250);
            parameters.Add(p => p.AsideWidth, 150);
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(0, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(1, "<div>Main</div>"));
            parameters.Add<RenderFragment>(p => p.Aside, builder => builder.AddMarkupContent(2, "<div>Aside</div>"));
        });

        // The panels are columns of their own here, so the width goes to them and the main content has
        // nothing left to reserve for them.
        StringAssert.Contains(component.Find(".bit-lyt-nmn").GetAttribute("style")!, "width:250px");
        StringAssert.Contains(component.Find(".bit-lyt-asd").GetAttribute("style")!, "width:150px");

        var mainStyle = component.Find(".bit-lyt-mcn").GetAttribute("style");

        Assert.IsTrue(string.IsNullOrWhiteSpace(mainStyle), $"Expected no inline width, found '{mainStyle}'.");
    }

    [TestMethod]
    public void BitLayoutShouldNotGiveThePanelsAWidthOfTheirOwnWithoutFullHeightPanels()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.NavPanelWidth, 250);
            parameters.Add(p => p.AsideWidth, 150);
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(0, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(1, "<div>Main</div>"));
            parameters.Add<RenderFragment>(p => p.Aside, builder => builder.AddMarkupContent(2, "<div>Aside</div>"));
        });

        // The width is the room the main content leaves beside itself, which is what lets a panel collapse
        // to a rail or slide out of the flow on its own terms.
        Assert.IsFalse(component.Find(".bit-lyt-nmn").HasAttribute("style"));
        Assert.IsFalse(component.Find(".bit-lyt-asd").HasAttribute("style"));
        StringAssert.Contains(component.Find(".bit-lyt-mcn").GetAttribute("style")!, "width:calc(100% - 400px)");
    }

    [TestMethod]
    public void BitLayoutShouldKeepTheOwnWidthAndTheCustomStyleOfThePanels()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.FullHeightPanels, true);
            parameters.Add(p => p.NavPanelWidth, 250);
            parameters.Add(p => p.AsideWidth, 150);
            parameters.Add(p => p.Styles, new BitLayoutClassStyles { NavPanel = "background: yellow;", Aside = "background: teal;" });
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(0, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Aside, builder => builder.AddMarkupContent(1, "<div>Aside</div>"));
        });

        var navStyle = component.Find(".bit-lyt-nmn").GetAttribute("style")!;
        var asideStyle = component.Find(".bit-lyt-asd").GetAttribute("style")!;

        StringAssert.Contains(navStyle, "width:250px");
        StringAssert.Contains(navStyle, "background: yellow");
        StringAssert.Contains(asideStyle, "width:150px");
        StringAssert.Contains(asideStyle, "background: teal");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitLayoutShouldRespectScrollableMain(bool scrollableMain)
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.ScrollableMain, scrollableMain);
        });

        Assert.AreEqual(scrollableMain, component.Find(".bit-lyt").ClassList.Contains("bit-lyt-scm"));
    }

    [TestMethod]
    public void BitLayoutShouldRenderTheMainSectionAsAPlainElementWhenNested()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.Nested, true);
            parameters.Add(p => p.SkipLink, true);
            parameters.Add(p => p.Id, "inner");
            parameters.Add(p => p.Classes, new BitLayoutClassStyles { MainContent = "custom-mcn" });
            parameters.Add(p => p.Styles, new BitLayoutClassStyles { MainContent = "padding: 4px;" });
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(0, "<div>Main</div>"));
        });

        var mainContent = component.Find(".bit-lyt-mcn");

        // A page may hold only one main landmark, so the nested one gives that up - and nothing else.
        Assert.AreEqual("DIV", mainContent.TagName);
        Assert.AreEqual(0, component.FindAll("main").Count);
        Assert.AreEqual("inner-main", mainContent.Id);
        Assert.AreEqual("-1", mainContent.GetAttribute("tabindex"));
        Assert.IsTrue(mainContent.ClassList.Contains("custom-mcn"));
        StringAssert.Contains(mainContent.GetAttribute("style")!, "padding: 4px");
        Assert.AreEqual("#inner-main", component.Find(".bit-lyt-skp").GetAttribute("href"));
    }

    [TestMethod]
    public void BitLayoutShouldKeepTheLandmarkSectionsOfANestedLayout()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.Nested, true);
            parameters.Add<RenderFragment>(p => p.Header, builder => builder.AddMarkupContent(0, "<div>Header</div>"));
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(1, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(2, "<div>Main</div>"));
            parameters.Add<RenderFragment>(p => p.Aside, builder => builder.AddMarkupContent(3, "<div>Aside</div>"));
            parameters.Add<RenderFragment>(p => p.Footer, builder => builder.AddMarkupContent(4, "<div>Footer</div>"));
        });

        // Only the main landmark is the one a page may hold exactly once; the rest keep their elements.
        Assert.AreEqual("HEADER", component.Find(".bit-lyt-hdr").TagName);
        Assert.AreEqual("NAV", component.Find(".bit-lyt-nmn").TagName);
        Assert.AreEqual("ASIDE", component.Find(".bit-lyt-asd").TagName);
        Assert.AreEqual("FOOTER", component.Find(".bit-lyt-ftr").TagName);
    }

    [TestMethod]
    public void BitLayoutShouldRespectHeaderAndFooterHeight()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.HeaderHeight, 64);
            parameters.Add(p => p.FooterHeight, 40);
        });

        var style = component.Find(".bit-lyt").GetAttribute("style")!;

        // The heights are handed down as custom properties, so the header height is also readable by the
        // sticky panels that pin themselves at that offset.
        Assert.IsTrue(style.Contains("--bit-lyt-hdr-h:64px"));
        Assert.IsTrue(style.Contains("--bit-lyt-ftr-h:40px"));
    }

    [TestMethod]
    public void BitLayoutShouldRespectGapAndPadding()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.Gap, "1rem");
            parameters.Add(p => p.Padding, "16px 24px");
        });

        var style = component.Find(".bit-lyt").GetAttribute("style")!;

        Assert.IsTrue(style.Contains("--bit-lyt-gap:1rem"));
        Assert.IsTrue(style.Contains("--bit-lyt-pad:16px 24px"));
    }

    [TestMethod]
    public void BitLayoutShouldRespectZIndex()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.ZIndex, 5);
        });

        StringAssert.Contains(component.Find(".bit-lyt").GetAttribute("style")!, "--bit-lyt-zin:5");
    }

    [TestMethod]
    public void BitLayoutShouldNotRenderTheSizingVariablesWhenTheyAreNotSet()
    {
        var component = RenderComponent<BitLayout>();

        var style = component.Find(".bit-lyt").GetAttribute("style");

        Assert.IsTrue(string.IsNullOrWhiteSpace(style), $"Expected no inline style, found '{style}'.");
    }

    [TestMethod]
    public void BitLayoutShouldRenderTheSkipLinkPointingAtTheMainSection()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.SkipLink, true);
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(0, "<div>Main</div>"));
        });

        var link = component.Find(".bit-lyt-skp");
        var main = component.Find(".bit-lyt-mcn");

        Assert.AreEqual("A", link.TagName);
        Assert.AreEqual("Skip to main content", link.TextContent.Trim());
        Assert.AreEqual($"#{main.Id}", link.GetAttribute("href"));

        // The main section only takes the focus the link sends it once it is focusable.
        Assert.AreEqual("-1", main.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitLayoutShouldRespectSkipLinkText()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.SkipLink, true);
            parameters.Add(p => p.SkipLinkText, "Jump to the content");
        });

        Assert.AreEqual("Jump to the content", component.Find(".bit-lyt-skp").TextContent.Trim());
    }

    [TestMethod]
    public void BitLayoutShouldNotMakeTheMainSectionFocusableWithoutASkipLink()
    {
        var component = RenderComponent<BitLayout>();

        Assert.IsFalse(component.Find(".bit-lyt-mcn").HasAttribute("tabindex"));
    }

    [TestMethod]
    public void BitLayoutShouldPointTheSkipLinkAtTheGivenId()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.SkipLink, true);
            parameters.Add(p => p.Id, "my-layout");
        });

        Assert.AreEqual("#my-layout-main", component.Find(".bit-lyt-skp").GetAttribute("href"));
        Assert.AreEqual("my-layout-main", component.Find(".bit-lyt-mcn").Id);
    }

    [TestMethod]
    public void BitLayoutShouldRespectTheAriaLabelsOfThePanels()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.NavPanelAriaLabel, "Main");
            parameters.Add(p => p.AsideAriaLabel, "On this page");
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(0, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Aside, builder => builder.AddMarkupContent(1, "<div>Aside</div>"));
        });

        Assert.AreEqual("Main", component.Find(".bit-lyt-nmn").GetAttribute("aria-label"));
        Assert.AreEqual("On this page", component.Find(".bit-lyt-asd").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitLayoutShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.SkipLink, true);

            parameters.Add(p => p.Classes, new BitLayoutClassStyles
            {
                Root = "custom-root",
                SkipLink = "custom-skip",
                Header = "custom-header",
                Main = "custom-main",
                NavPanel = "custom-nav",
                MainContent = "custom-mcn",
                Aside = "custom-aside",
                Footer = "custom-footer"
            });

            parameters.Add(p => p.Styles, new BitLayoutClassStyles
            {
                Root = "color: red;",
                SkipLink = "font-weight: bold;",
                Header = "color: tomato;",
                Main = "height: 10rem;",
                NavPanel = "background: yellow;",
                MainContent = "padding: 4px;",
                Aside = "background: teal;",
                Footer = "color: blue;"
            });

            parameters.Add<RenderFragment>(p => p.Header, builder => builder.AddMarkupContent(0, "<div>Header</div>"));
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(1, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(2, "<div>Main</div>"));
            parameters.Add<RenderFragment>(p => p.Aside, builder => builder.AddMarkupContent(3, "<div>Aside</div>"));
            parameters.Add<RenderFragment>(p => p.Footer, builder => builder.AddMarkupContent(4, "<div>Footer</div>"));
        });

        var root = component.Find(".bit-lyt");
        var skip = component.Find(".bit-lyt-skp");
        var header = component.Find(".bit-lyt-hdr");
        var nav = component.Find(".bit-lyt-nmn");
        var main = component.Find(".bit-lyt-man");
        var mainContent = component.Find(".bit-lyt-mcn");
        var aside = component.Find(".bit-lyt-asd");
        var footer = component.Find(".bit-lyt-ftr");

        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        Assert.IsTrue(skip.ClassList.Contains("custom-skip"));
        Assert.IsTrue(header.ClassList.Contains("custom-header"));
        Assert.IsTrue(nav.ClassList.Contains("custom-nav"));
        Assert.IsTrue(main.ClassList.Contains("custom-main"));
        Assert.IsTrue(mainContent.ClassList.Contains("custom-mcn"));
        Assert.IsTrue(aside.ClassList.Contains("custom-aside"));
        Assert.IsTrue(footer.ClassList.Contains("custom-footer"));

        Assert.IsTrue(root.GetAttribute("style")!.Contains("color: red"));
        Assert.IsTrue(skip.GetAttribute("style")!.Contains("font-weight: bold"));
        Assert.IsTrue(header.GetAttribute("style")!.Contains("tomato"));
        Assert.IsTrue(main.GetAttribute("style")!.Contains("height: 10rem"));
        Assert.IsTrue(nav.GetAttribute("style")!.Contains("yellow"));
        Assert.IsTrue(mainContent.GetAttribute("style")!.Contains("padding: 4px"));
        Assert.IsTrue(aside.GetAttribute("style")!.Contains("teal"));
        Assert.IsTrue(footer.GetAttribute("style")!.Contains("blue"));
    }

    [TestMethod]
    public void BitLayoutShouldKeepTheReservedWidthAndTheCustomStyleOfTheMainSection()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.NavPanelWidth, 250);
            parameters.Add(p => p.Styles, new BitLayoutClassStyles { MainContent = "padding: 4px;" });
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(0, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(1, "<div>Main</div>"));
        });

        var style = component.Find(".bit-lyt-mcn").GetAttribute("style")!;

        Assert.IsTrue(style.Contains("width:calc(100% - 250px)"));
        Assert.IsTrue(style.Contains("padding: 4px"));
    }

    [TestMethod]
    [DataRow(BitDir.Rtl, "rtl")]
    [DataRow(BitDir.Ltr, "ltr")]
    [DataRow(BitDir.Auto, "auto")]
    public void BitLayoutShouldRespectDir(BitDir dir, string expectedDir)
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        var root = component.Find(".bit-lyt");

        Assert.AreEqual(expectedDir, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitLayoutShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        Assert.AreEqual(expectedStyle, component.Find(".bit-lyt").GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitLayoutShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        Assert.AreEqual(isEnabled is false, component.Find(".bit-lyt").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitLayoutShouldRespectClassAndStyle()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "color: red;");
        });

        var root = component.Find(".bit-lyt");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("color: red"));
    }

    [TestMethod]
    public void BitLayoutShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitLayoutHtmlAttributesTest>();

        component.MarkupMatches(@"
<div data-val-test=""bit"" class=""bit-lyt"" id:ignore>
    <div class=""bit-lyt-man"">
        <main class=""bit-lyt-mcn"" id:ignore>
            I'm a layout
        </main>
    </div>
</div>");
    }

    [TestMethod]
    public void BitLayoutShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitLayoutCascadingParamsTest>();

        var layouts = component.FindAll(".bit-lyt");

        // The first layout takes everything from the cascading parameters.
        Assert.IsTrue(layouts[0].ClassList.Contains("bit-lyt-brd"));
        Assert.IsTrue(layouts[0].ClassList.Contains("bit-lyt-flh"));
        Assert.IsTrue(layouts[0].ClassList.Contains("bit-lyt-fhp"));
        Assert.IsTrue(layouts[0].ClassList.Contains("bit-lyt-scm"));
        Assert.IsTrue(layouts[0].ClassList.Contains("bit-lyt-shd"));
        Assert.IsTrue(layouts[0].ClassList.Contains("bit-lyt-sft"));
        Assert.IsTrue(layouts[0].ClassList.Contains("bit-lyt-snv"));
        Assert.IsTrue(layouts[0].ClassList.Contains("bit-lyt-sas"));
        Assert.IsTrue(layouts[0].ClassList.Contains("bit-lyt-rnv"));

        var firstStyle = layouts[0].GetAttribute("style")!;

        StringAssert.Contains(firstStyle, "--bit-lyt-gap:1rem");
        StringAssert.Contains(firstStyle, "--bit-lyt-pad:2rem");
        StringAssert.Contains(firstStyle, "--bit-lyt-hdr-h:64px");
        StringAssert.Contains(firstStyle, "--bit-lyt-ftr-h:40px");
        StringAssert.Contains(firstStyle, "--bit-lyt-zin:7");

        Assert.AreEqual("Cascaded skip", layouts[0].QuerySelector(".bit-lyt-skp")!.TextContent.Trim());
        Assert.AreEqual("Cascaded nav", layouts[0].QuerySelector(".bit-lyt-nmn")!.GetAttribute("aria-label"));
        Assert.AreEqual("Cascaded aside", layouts[0].QuerySelector(".bit-lyt-asd")!.GetAttribute("aria-label"));

        // The cascaded widths go to the panels themselves, since the cascaded FullHeightPanels makes them
        // columns of their own, and the cascaded Nested keeps the main section from being a landmark.
        StringAssert.Contains(layouts[0].QuerySelector(".bit-lyt-nmn")!.GetAttribute("style")!, "width:200px");
        StringAssert.Contains(layouts[0].QuerySelector(".bit-lyt-asd")!.GetAttribute("style")!, "width:100px");
        Assert.AreEqual("DIV", layouts[0].QuerySelector(".bit-lyt-mcn")!.TagName);

        // The second one sets its own values, which the cascading parameters must not overwrite.
        Assert.IsFalse(layouts[1].ClassList.Contains("bit-lyt-brd"));
        Assert.IsTrue(layouts[1].ClassList.Contains("bit-lyt-flh"));
        StringAssert.Contains(layouts[1].GetAttribute("style")!, "--bit-lyt-gap:2rem");
        Assert.AreEqual("Own skip", layouts[1].QuerySelector(".bit-lyt-skp")!.TextContent.Trim());
        Assert.AreEqual("Own nav", layouts[1].QuerySelector(".bit-lyt-nmn")!.GetAttribute("aria-label"));
        Assert.AreEqual("Cascaded aside", layouts[1].QuerySelector(".bit-lyt-asd")!.GetAttribute("aria-label"));
        Assert.AreEqual("MAIN", layouts[1].QuerySelector(".bit-lyt-mcn")!.TagName);
    }
}
