using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccordionList;

[TestClass]
public class BitAccordionListOptionsOrderTests : BunitTestContext
{
    [TestMethod]
    public void BitAccordionListShouldPreserveOptionsOrderWhenAnOptionIsAddedConditionally()
    {
        var component = RenderComponent<BitAccordionListOptionsOrderTest>(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTitles(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        CollectionAssert.AreEqual(new[] { "First", "Middle", "Last" }, GetItemTitles(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTitles(component));
    }

    private static string[] GetItemTitles(IRenderedComponent<BitAccordionListOptionsOrderTest> component)
    {
        return component.FindAll(".bit-acd-ttl").Select(e => e.TextContent).ToArray();
    }
}
