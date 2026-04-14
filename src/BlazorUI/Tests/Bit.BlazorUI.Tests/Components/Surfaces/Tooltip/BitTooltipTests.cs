using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Tooltip;

[TestClass]
public class BitTooltipTests : BunitTestContext
{
    [TestMethod]
    public void BitTooltipShouldRenderTextAndRespectDefaultIsShown()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "This is a tooltip");
        });

        var wrapper = component.Find(".bit-ttp-wrp");

        // By default tooltip should not be visible
        Assert.IsFalse(wrapper.ClassList.Contains("bit-ttp-vis"));

        var component2 = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Shown tooltip");
            parameters.Add(p => p.DefaultIsShown, true);
        });

        var wrapper2 = component2.Find(".bit-ttp-wrp");

        Assert.IsTrue(wrapper2.ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShouldRenderTemplateContent()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add<RenderFragment>(
                p => p.Template, 
                builder => builder.AddMarkupContent(0, "<span class=\"tpl\">TemplateContent</span>"));
        });

        var tooltip = component.Find(".bit-ttp-ctn");

        Assert.IsTrue(tooltip.ToMarkup().Contains("TemplateContent"));
    }

    [TestMethod]
    public void BitTooltipShouldRenderAnchorWhenProvided()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add<RenderFragment>(
                p => p.Anchor, 
                builder => builder.AddMarkupContent(0, "<button class=\"anchor-btn\">Anchor</button>"));
        });

        var anchor = component.Find(".anchor-btn");

        Assert.IsNotNull(anchor);
        Assert.IsTrue(anchor.ToMarkup().Contains("Anchor"));
    }

    [TestMethod]
    public void BitTooltipShowOnClickShouldToggleVisibilityOnPointerUp()
    {
        var component = RenderComponent<BitTooltip>(parameters =>
        {
            parameters.Add(p => p.Text, "Click tooltip");
            parameters.Add(p => p.ShowOnClick, true);
        });

        var root = component.Find(".bit-ttp");
        var wrapper = component.Find(".bit-ttp-wrp");

        // initial hidden
        Assert.IsFalse(wrapper.ClassList.Contains("bit-ttp-vis"));

        // trigger pointer up (click)
        root.TriggerEvent("onpointerup", new PointerEventArgs());

        // after click it should be visible
        wrapper = component.Find(".bit-ttp-wrp");
        Assert.IsTrue(wrapper.ClassList.Contains("bit-ttp-vis"));

        // trigger again to hide
        root.TriggerEvent("onpointerup", new PointerEventArgs());

        wrapper = component.Find(".bit-ttp-wrp");
        Assert.IsFalse(wrapper.ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public async Task BitTooltipDisposeShouldNotThrow()
    {
        var component = RenderComponent<BitTooltip>(p =>
        {
            p.Add(x => x.Text, "Tip");
            p.Add(x => x.ShowDelay, 500);
            p.Add(x => x.HideDelay, 500);
        });

        await component.Instance.DisposeAsync();
    }

    [TestMethod]
    public async Task BitTooltipDisposeDuringShowDelayShouldNotThrow()
    {
        var component = RenderComponent<BitTooltip>(p =>
        {
            p.Add(x => x.Text, "Tip");
            p.Add(x => x.ShowDelay, 5000);
            p.Add(x => x.ShowOnHover, true);
            p.AddChildContent("<button>Hover me</button>");
        });

        var root = component.Find(".bit-ttp");
        await component.InvokeAsync(() => root.TriggerEvent("onpointerenter", new PointerEventArgs()));

        await component.Instance.DisposeAsync();
    }

    [TestMethod]
    public async Task BitTooltipDisposeDuringHideDelayShouldNotThrow()
    {
        var component = RenderComponent<BitTooltip>(p =>
        {
            p.Add(x => x.Text, "Tip");
            p.Add(x => x.DefaultIsShown, true);
            p.Add(x => x.HideDelay, 5000);
            p.Add(x => x.ShowOnHover, true);
            p.AddChildContent("<button>Hover me</button>");
        });

        var root = component.Find(".bit-ttp");
        await component.InvokeAsync(() => root.TriggerEvent("onpointerleave", new PointerEventArgs()));

        await component.Instance.DisposeAsync();
    }
}
