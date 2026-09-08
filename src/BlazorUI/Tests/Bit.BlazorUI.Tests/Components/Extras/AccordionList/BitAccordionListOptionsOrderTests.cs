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

    [TestMethod]
    public void BitAccordionListShouldOrderTheItemsOfConditionalOptionsByTheirMarkupOrder()
    {
        // An option added conditionally registers itself behind every option that was already there, so the
        // order the list keeps its items in has to be read back from the render - which is the markup order,
        // and the order the expanded keys are reported in and the keyboard navigation walks.
        var component = RenderComponent<BitAccordionListOptionsOrderTest>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.ShowMiddle, false);
        });

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        var headers = component.FindAll(".bit-acd-hdr");
        headers[2].Click();
        component.FindAll(".bit-acd-hdr")[1].Click();

        Assert.AreEqual("middle,last", string.Join(",", component.Instance.List!.GetExpandedKeys()));
    }

    private static string[] GetItemTitles(IRenderedComponent<BitAccordionListOptionsOrderTest> component)
    {
        return component.FindAll(".bit-acd-ttl").Select(e => e.TextContent).ToArray();
    }
}
