using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Breadcrumb;

[TestClass]
public class BitBreadcrumbOptionsTests : BunitTestContext
{
    [TestMethod]
    public void BitBreadcrumbShouldRenderTheRegisteredOptions()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbOption>>(parameters =>
        {
            parameters.AddChildContent<BitBreadcrumbOption>(p => p.Add(o => o.Text, "Option 1").Add(o => o.Href, "/option-1"));
            parameters.AddChildContent<BitBreadcrumbOption>(p => p.Add(o => o.Text, "Option 2").Add(o => o.Href, "/option-2"));
            parameters.AddChildContent<BitBreadcrumbOption>(p => p.Add(o => o.Text, "Option 3").Add(o => o.IsSelected, true));
        });

        var items = component.FindAll(".bit-brc-icn .bit-brc-itm, .bit-brc-icn .bit-brc-nii");

        CollectionAssert.AreEqual(new[] { "Option 1", "Option 2", "Option 3" }, items.Select(i => i.TextContent.Trim()).ToArray());
        Assert.AreEqual("page", component.Find(".bit-brc-sel").GetAttribute("aria-current"));
        // The options only render their hidden markers, the breadcrumb renders the visible items.
        Assert.AreEqual(3, component.FindAll("[data-bit-brc-opt]").Count);
    }

    [TestMethod]
    public void BitBreadcrumbOptionShouldTakeItsTitleAndTargetAndAriaLabel()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbOption>>(parameters =>
        {
            parameters.AddChildContent<BitBreadcrumbOption>(p => p
                .Add(o => o.Text, "Option 1")
                .Add(o => o.Href, "/option-1")
                .Add(o => o.Title, "The first option")
                .Add(o => o.Target, "_blank")
                .Add(o => o.AriaLabel, "Go to the first option"));
        });

        var link = component.Find(".bit-brc-itm");

        Assert.AreEqual("The first option", link.GetAttribute("title"));
        Assert.AreEqual("_blank", link.GetAttribute("target"));
        Assert.AreEqual("Go to the first option", link.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitBreadcrumbOptionWithOnlyItsOwnClickHandlerShouldBeAButton()
    {
        var clicked = 0;

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbOption>>(parameters =>
        {
            parameters.AddChildContent<BitBreadcrumbOption>(p => p
                .Add(o => o.Text, "Option 1")
                .Add(o => o.OnClick, () => clicked++));
            parameters.AddChildContent<BitBreadcrumbOption>(p => p.Add(o => o.Text, "Option 2"));
        });

        Assert.AreEqual(1, component.FindAll("button.bit-brc-itm").Count);

        component.Find("button.bit-brc-itm").Click();

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public void BitBreadcrumbOptionShouldRefreshTheBreadcrumbWhenItsParametersChange()
    {
        var component = RenderComponent<BitBreadcrumbOptionParametersTest>(
            parameters => parameters.Add(p => p.Text, "First"));

        Assert.AreEqual("First", component.Find(".bit-brc-nii").TextContent.Trim());

        component.Render(parameters => parameters.Add(p => p.Text, "Second"));

        Assert.AreEqual("Second", component.Find(".bit-brc-nii").TextContent.Trim());
    }

    [TestMethod]
    public void BitBreadcrumbShouldSplitTheOptionsBetweenTheTrailAndTheOverflow()
    {
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbOption>>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedItems, (uint)2);
            parameters.Add(p => p.OverflowIndex, (uint)1);
            parameters.AddChildContent<BitBreadcrumbOption>(p => p.Add(o => o.Text, "Option 1").Add(o => o.Href, "/option-1"));
            parameters.AddChildContent<BitBreadcrumbOption>(p => p.Add(o => o.Text, "Option 2").Add(o => o.Href, "/option-2"));
            parameters.AddChildContent<BitBreadcrumbOption>(p => p.Add(o => o.Text, "Option 3").Add(o => o.Href, "/option-3"));
            parameters.AddChildContent<BitBreadcrumbOption>(p => p.Add(o => o.Text, "Option 4").Add(o => o.IsSelected, true));
        });

        var trail = component.FindAll(".bit-brc-icn .bit-brc-itm, .bit-brc-icn .bit-brc-nii");

        CollectionAssert.AreEqual(new[] { "Option 1", "Option 4" }, trail.Select(i => i.TextContent.Trim()).ToArray());

        var overflow = component.FindAll(".bit-brc-cal .bit-brc-ofi");

        CollectionAssert.AreEqual(new[] { "Option 2", "Option 3" }, overflow.Select(i => i.TextContent.Trim()).ToArray());
    }
}
