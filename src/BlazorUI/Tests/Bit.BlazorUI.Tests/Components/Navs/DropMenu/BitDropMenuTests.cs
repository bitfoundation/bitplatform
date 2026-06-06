using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.DropMenu;

[TestClass]
public class BitDropMenuTests : BunitTestContext
{
    [TestMethod]
    public void BitDropMenuShouldRenderRootElement()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var root = component.Find(".bit-drm");
        var button = component.Find(".bit-drm-btn");

        Assert.IsNotNull(root);
        Assert.IsNotNull(button);
    }

    [TestMethod]
    public void BitDropMenuShouldRenderIconAndText()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.IconName, "Home");
            parameters.Add(p => p.Text, "MenuText");
        });

        var icon = component.Find(".bit-drm-icn");
        var text = component.Find(".bit-drm-txt");

        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Home"));
        Assert.AreEqual("MenuText", text.TextContent);
    }

    [TestMethod]
    public void BitDropMenuShouldToggleCalloutOnClick()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Body, (RenderFragment)(b => b.AddMarkupContent(0, @"<div class=""body"">BodyContent</div>")));
        });

        var root = component.Find(".bit-drm");

        var overlay = component.Find(".bit-drm-ovl");
        Assert.IsTrue(overlay.GetAttribute("style").Contains("display:none"));

        var button = component.Find(".bit-drm-btn");
        button.Click();

        overlay = component.Find(".bit-drm-ovl");
        Assert.IsTrue(overlay.GetAttribute("style").Contains("display:block"));

        overlay.Click();

        overlay = component.Find(".bit-drm-ovl");
        Assert.IsTrue(overlay.GetAttribute("style").Contains("display:none"));
    }

    [TestMethod]
    public void BitDropMenuShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitDropMenuHtmlAttributesTest>();

        component.MarkupMatches(@"
<div data-val-test=""bit"" type=""button"" class=""bit-drm"" tabindex=""0"" id:ignore>
    <button type=""button"" class=""bit-drm-btn "">
        <div class=""bit-drm-txt "">Menu</div>
        <i class=""bit-icon bit-icon--ChevronRight bit-ico-r90 ""></i>
    </button>
</div>
<div style=""display:none;"" class=""bit-drm-ovl "" id:ignore></div>
<div class=""bit-drm-cal bit-drm-end"" id:ignore>
    <div>Body</div>
</div>");

        var markup = component.Markup;

        Assert.IsTrue(markup.Contains("data-val-test=\"bit\""));
        Assert.IsTrue(markup.Contains("bit-drm"));
        Assert.IsTrue(markup.Contains("Body"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotAddNoShadowClassByDefault()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsFalse(callout.ClassList.Contains("bit-drm-nsh"));
    }

    [TestMethod]
    public void BitDropMenuShouldAddNoShadowClassWhenNoShadowIsSet()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.NoShadow, true);
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-drm-nsh"));
    }

    [TestMethod]
    [DataRow(BitColorKind.Primary, "bit-drm-pbg")]
    [DataRow(BitColorKind.Secondary, "bit-drm-sbg")]
    [DataRow(BitColorKind.Tertiary, "bit-drm-tbg")]
    [DataRow(BitColorKind.Transparent, "bit-drm-rbg")]
    public void BitDropMenuShouldAddBackgroundClass(BitColorKind background, string expectedClass)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Background, background);
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsTrue(callout.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitDropMenuShouldNotAddBackgroundClassByDefault()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsFalse(callout.ClassList.Contains("bit-drm-pbg"));
        Assert.IsFalse(callout.ClassList.Contains("bit-drm-sbg"));
        Assert.IsFalse(callout.ClassList.Contains("bit-drm-tbg"));
        Assert.IsFalse(callout.ClassList.Contains("bit-drm-rbg"));
    }

    [TestMethod]
    [DataRow(BitColorKind.Primary, "bit-drm-pbr")]
    [DataRow(BitColorKind.Secondary, "bit-drm-sbr")]
    [DataRow(BitColorKind.Tertiary, "bit-drm-tbr")]
    [DataRow(BitColorKind.Transparent, "bit-drm-rbr")]
    public void BitDropMenuShouldAddBorderClass(BitColorKind border, string expectedColorClass)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Border, border);
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-drm-brd"));
        Assert.IsTrue(callout.ClassList.Contains(expectedColorClass));
    }

    [TestMethod]
    public void BitDropMenuShouldNotAddBorderClassByDefault()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsFalse(callout.ClassList.Contains("bit-drm-brd"));
    }

    [TestMethod]
    public void BitDropMenuShouldCombineNoShadowBackgroundAndBorderClasses()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.NoShadow, true);
            parameters.Add(p => p.Background, BitColorKind.Secondary);
            parameters.Add(p => p.Border, BitColorKind.Tertiary);
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-drm-nsh"));
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-sbg"));
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-brd"));
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-tbr"));
    }
}

