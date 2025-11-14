using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Layout;

[TestClass]
public class BitLayoutTests : BunitTestContext
{
    [TestMethod]
    public void BitLayoutShouldRenderRootElement()
    {
        var component = RenderComponent<BitLayout>();

        var root = component.Find(".bit-lyt");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitLayoutShouldRespectStickyHeaderAndFooter()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.StickyHeader, true);
            parameters.Add(p => p.StickyFooter, true);
        });

        var root = component.Find(".bit-lyt");

        Assert.IsTrue(root.ClassList.Contains("bit-lyt-shd"));
        Assert.IsTrue(root.ClassList.Contains("bit-lyt-sft"));
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

        // NavPanel should not be rendered when HideNavPanel is true
        try
        {
            component.Find(".bit-lyt-nmn");
            Assert.Fail("Nav panel should not be rendered when HideNavPanel is true");
        }
        catch (ElementNotFoundException)
        {
            // expected
        }
    }

    [TestMethod]
    public void BitLayoutShouldRespectReverseNavPanel()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.ReverseNavPanel, true);
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(0, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(1, "<div>Main</div>"));
        });

        var main = component.Find("main");

        var style = main.GetAttribute("style");

        Assert.IsTrue(style.Contains("flex-direction:row-reverse"));
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

        var mainContent = component.Find(".bit-lyt-mcn");

        var style = mainContent.GetAttribute("style");

        Assert.IsTrue(style.Contains("width:calc(100% - 250px)"));
    }

    [TestMethod]
    public void BitLayoutShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitLayoutClassStyles
            {
                Header = "custom-header",
                Main = "custom-main",
                NavPanel = "custom-nav",
                MainContent = "custom-mcn",
                Footer = "custom-footer"
            });

            parameters.Add(p => p.Styles, new BitLayoutClassStyles
            {
                Header = "color: tomato;",
                Main = "height: 10rem;",
                NavPanel = "background: yellow;",
                MainContent = "padding: 4px;",
                Footer = "color: blue;"
            });

            parameters.Add<RenderFragment>(p => p.Header, builder => builder.AddMarkupContent(0, "<div>Header</div>"));
            parameters.Add<RenderFragment>(p => p.NavPanel, builder => builder.AddMarkupContent(1, "<div>Nav</div>"));
            parameters.Add<RenderFragment>(p => p.Main, builder => builder.AddMarkupContent(2, "<div>Main</div>"));
            parameters.Add<RenderFragment>(p => p.Footer, builder => builder.AddMarkupContent(3, "<div>Footer</div>"));
        });

        var header = component.Find(".bit-lyt-hdr");
        var nav = component.Find(".bit-lyt-nmn");
        var main = component.Find(".bit-lyt-man");
        var mainContent = component.Find(".bit-lyt-mcn");
        var footer = component.Find(".bit-lyt-ftr");

        Assert.IsTrue(header.ClassList.Contains("custom-header"));
        Assert.IsTrue(nav.ClassList.Contains("custom-nav"));
        Assert.IsTrue(main.ClassList.Contains("custom-main"));
        Assert.IsTrue(mainContent.ClassList.Contains("custom-mcn"));
        Assert.IsTrue(footer.ClassList.Contains("custom-footer"));

        Assert.IsTrue(header.GetAttribute("style").Contains("tomato"));
        Assert.IsTrue(main.GetAttribute("style").Contains("height: 10rem"));
        Assert.IsTrue(nav.GetAttribute("style").Contains("yellow"));
        Assert.IsTrue(mainContent.GetAttribute("style").Contains("padding: 4px"));
        Assert.IsTrue(footer.GetAttribute("style").Contains("blue"));
    }
}
