using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Callout;

[TestClass]
public class BitCalloutTests : BunitTestContext
{
    [TestMethod]
    public void BitCalloutShouldRenderAnchorWhenProvided()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Anchor, (RenderFragment)(builder => builder.AddMarkupContent(0, "<button class=\"anchor-btn\">Anchor</button>")));
        });

        var anchor = component.Find(".bit-clo-acn");
        Assert.IsNotNull(anchor);
        Assert.IsTrue(anchor.ToMarkup().Contains("Anchor"));
    }

    [TestMethod]
    public void BitCalloutShouldShowOverlayWhenIsOpenTrue()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        var overlay = component.Find(".bit-clo-ovl");
        Assert.IsNotNull(overlay);

        // overlay style should contain display:block when open
        Assert.IsTrue(overlay.GetAttribute("style").Contains("display:block"));

        var content = component.Find(".bit-clo-cal");
        Assert.IsNotNull(content);
        Assert.IsTrue(content.ToMarkup().Contains("Hello"));
    }

    [TestMethod]
    public void BitCalloutShouldHideOverlayWhenIsOpenFalse()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.AddChildContent("<div class=\"content\">Hello</div>");
        });

        var overlay = component.Find(".bit-clo-ovl");
        Assert.IsNotNull(overlay);

        // overlay style should contain display:none when closed
        Assert.IsTrue(overlay.GetAttribute("style").Contains("display:none"));
    }

    [TestMethod]
    public void BitCalloutShouldPreferContentOverChildContent()
    {
        var component = RenderComponent<BitCallout>(parameters =>
        {
            parameters.Add(p => p.Content, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div class=\"content\">ContentFragment</div>")));
            parameters.AddChildContent("<div class=\"child\">ChildContent</div>");
        });

        var content = component.Find(".bit-clo-cal");
        Assert.IsNotNull(content);
        Assert.IsTrue(content.ToMarkup().Contains("ContentFragment"));
        Assert.IsFalse(content.ToMarkup().Contains("ChildContent"));
    }
}
