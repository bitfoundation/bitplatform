using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Breadcrumb;

[TestClass]
public class BitBreadcrumbAutoCollapseTests : BunitTestContext
{
    // Four items and the three dividers between them, as the measuring script reports them.
    private static readonly double[] Widths = [100, 20, 100, 20, 100, 20, 100];

    [TestMethod]
    public void BitBreadcrumbShouldCollapseTheItemsThatDoNotFitWhenAutoCollapseIsEnabled()
    {
        var handler = Context.JSInterop.Setup<BitOverflowMetrics?>("BitBlazorUI.Utils.getOverflowMetrics", _ => true);
        handler.SetResult(new BitOverflowMetrics { Available = 150, Content = 460, Widths = Widths });

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.AutoCollapse, true);
        });

        // The trail keeps shrinking while it overflows, down to the single item it may not drop.
        component.WaitForAssertion(() => Assert.AreEqual(1, GetRenderedItemCount(component)));
        Assert.AreEqual(1, component.FindAll(".bit-brc-obt").Count);
        Assert.AreEqual(3, component.FindAll(".bit-brc-cal .bit-brc-ofi").Count);
    }

    [TestMethod]
    public void BitBreadcrumbShouldKeepTheWholeTrailWhenItFits()
    {
        var handler = Context.JSInterop.Setup<BitOverflowMetrics?>("BitBlazorUI.Utils.getOverflowMetrics", _ => true);
        handler.SetResult(new BitOverflowMetrics { Available = 800, Content = 460, Widths = Widths });

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.AutoCollapse, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(4, GetRenderedItemCount(component)));
        Assert.AreEqual(0, component.FindAll(".bit-brc-obt").Count);
    }

    [TestMethod]
    public void BitBreadcrumbShouldBringTheCollapsedItemsBackWhenTheRoomReturns()
    {
        var handler = Context.JSInterop.Setup<BitOverflowMetrics?>("BitBlazorUI.Utils.getOverflowMetrics", _ => true);
        handler.SetResult(new BitOverflowMetrics { Available = 150, Content = 460, Widths = Widths });

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.AutoCollapse, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, GetRenderedItemCount(component)));

        // The component got wider: the items come back one by one, each one only when the room it
        // took while it was in the trail is free again.
        handler.SetResult(new BitOverflowMetrics { Available = 800, Content = 200, Widths = Widths });

        component.InvokeAsync(() => component.Instance._OnResize(new ContentRect()));

        component.WaitForAssertion(() => Assert.AreEqual(4, GetRenderedItemCount(component)));
        Assert.AreEqual(0, component.FindAll(".bit-brc-obt").Count);
    }

    [TestMethod]
    public void BitBreadcrumbShouldNotBringTheCollapsedItemsBackWithoutEnoughRoom()
    {
        var handler = Context.JSInterop.Setup<BitOverflowMetrics?>("BitBlazorUI.Utils.getOverflowMetrics", _ => true);
        handler.SetResult(new BitOverflowMetrics { Available = 150, Content = 460, Widths = Widths });

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.AutoCollapse, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, GetRenderedItemCount(component)));

        // A slack of 50px is less than the 120px the last collapsed item took, so nothing comes back.
        handler.SetResult(new BitOverflowMetrics { Available = 250, Content = 200, Widths = Widths });

        component.InvokeAsync(() => component.Instance._OnResize(new ContentRect()));

        component.WaitForAssertion(() => Assert.AreEqual(1, GetRenderedItemCount(component)));
    }

    [TestMethod]
    public void BitBreadcrumbShouldKeepMaxDisplayedItemsAsTheCapOfTheAutomaticCollapsing()
    {
        var handler = Context.JSInterop.Setup<BitOverflowMetrics?>("BitBlazorUI.Utils.getOverflowMetrics", _ => true);
        handler.SetResult(new BitOverflowMetrics { Available = 800, Content = 200, Widths = Widths });

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
            parameters.Add(p => p.MaxDisplayedItems, (uint)2);
            parameters.Add(p => p.AutoCollapse, true);
        });

        // Everything fits, but MaxDisplayedItems still caps the trail at two items.
        component.WaitForAssertion(() => Assert.AreEqual(2, GetRenderedItemCount(component)));
    }

    [TestMethod]
    public void BitBreadcrumbShouldNotMeasureAnythingWithoutAutoCollapse()
    {
        var handler = Context.JSInterop.Setup<BitOverflowMetrics?>("BitBlazorUI.Utils.getOverflowMetrics", _ => true);
        handler.SetResult(new BitOverflowMetrics { Available = 150, Content = 460, Widths = Widths });

        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.Items, GetBreadcrumbItems());
        });

        Assert.AreEqual(4, GetRenderedItemCount(component));
        Assert.AreEqual(0, handler.Invocations.Count);
    }

    private static int GetRenderedItemCount(IRenderedComponent<BitBreadcrumb<BitBreadcrumbItem>> component)
    {
        return component.FindAll(".bit-brc-icn .bit-brc-itm, .bit-brc-icn .bit-brc-nii").Count;
    }

    private static List<BitBreadcrumbItem> GetBreadcrumbItems()
    {
        return
        [
            new() { Text = "Folder 1", Href = "/folder-1" },
            new() { Text = "Folder 2", Href = "/folder-2" },
            new() { Text = "Folder 3", Href = "/folder-3" },
            new() { Text = "Folder 4", Href = "/folder-4", IsSelected = true }
        ];
    }
}
