using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Breadcrumb;

[TestClass]
public class BitBreadcrumbOptionsOrderTests : BunitTestContext
{
    [TestMethod]
    public void BitBreadcrumbShouldPreserveOptionsOrderWhenAnOptionIsAddedConditionally()
    {
        // The breadcrumb reads the DOM order of the option markers via JS interop to keep the order of
        // its registered items in sync with the markup order of the options, so the test provides the
        // marker ids (read from the rendered markup) as the result of that JS call. The result is set
        // only after the conditional option is rendered, so the earlier (pending) invocations complete
        // with the full, correctly ordered set of marker ids.
        var handler = Context.JSInterop.Setup<string[]>("BitBlazorUI.Utils.getChildrenAttributes", _ => true);

        var component = RenderComponent<BitBreadcrumbOptionsOrderTest>(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        handler.SetResult(GetMarkerIds(component));

        component.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "First", "Middle", "Last" }, GetItemTexts(component)));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        handler.SetResult(GetMarkerIds(component));

        component.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTexts(component)));
    }

    private static string[] GetItemTexts(IRenderedComponent<BitBreadcrumbOptionsOrderTest> component)
    {
        return component.FindAll(".bit-brc-itm").Select(e => e.TextContent.Trim()).ToArray();
    }

    private static string[] GetMarkerIds(IRenderedComponent<BitBreadcrumbOptionsOrderTest> component)
    {
        return component.FindAll("[data-bit-brc-opt]").Select(e => e.GetAttribute("data-bit-brc-opt")!).ToArray();
    }
}
