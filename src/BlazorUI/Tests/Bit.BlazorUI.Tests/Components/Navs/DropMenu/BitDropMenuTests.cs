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
    <button type=""button"" class=""bit-drm-btn"">
        <div class=""bit-drm-txt "">Menu</div>
        <i class=""bit-icon bit-icon--ChevronRight bit-ico-r90""></i>
    </button>
    <div style=""display:none;"" class=""bit-drm-ovl ""></div>
    <div class=""bit-drm-cal bit-drm-end"" id:ignore>
        <div>Body</div>
    </div>
</div>");

        var markup = component.Markup;

        Assert.IsTrue(markup.Contains("data-val-test=\"bit\""));
        Assert.IsTrue(markup.Contains("bit-drm"));
        Assert.IsTrue(markup.Contains("Body"));
    }
}
