using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Breadcrumb;

[TestClass]
public class BitBreadcrumbAutoReorderOptionsTests : BunitTestContext
{
    [TestMethod]
    public void BitBreadcrumbShouldReorderOptionsOnPureMoveWhenAutoReorderOptionsEnabled()
    {
        // A pure reorder of keyed options moves the existing option components (and their markers) without
        // registering or unregistering anything. With AutoReorderOptions the breadcrumb still re-reads the
        // DOM marker order after the render, so the displayed order follows the new markup order.
        var handler = Context.JSInterop.Setup<string[]>("BitBlazorUI.Utils.getChildrenAttributes", _ => true);

        var component = RenderComponent<BitBreadcrumbAutoReorderTest>(
            parameters => parameters.Add(p => p.Items, ["A", "B", "C"]));

        component.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "A", "B", "C" }, GetItemTexts(component)));

        // Re-sort the source list; the @key'd options are moved, not recreated (no register/unregister).
        component.Render(parameters => parameters.Add(p => p.Items, ["C", "A", "B"]));

        handler.SetResult(GetMarkerIds(component));

        component.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "C", "A", "B" }, GetItemTexts(component)));
    }

    private static string[] GetItemTexts(IRenderedComponent<BitBreadcrumbAutoReorderTest> component)
    {
        return component.FindAll(".bit-brc-itm").Select(e => e.TextContent.Trim()).ToArray();
    }

    private static string[] GetMarkerIds(IRenderedComponent<BitBreadcrumbAutoReorderTest> component)
    {
        return component.FindAll("[data-bit-brc-opt]").Select(e => e.GetAttribute("data-bit-brc-opt")!).ToArray();
    }
}
