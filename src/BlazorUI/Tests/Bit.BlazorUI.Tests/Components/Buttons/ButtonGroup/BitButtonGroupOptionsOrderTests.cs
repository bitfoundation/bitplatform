using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.ButtonGroup;

[TestClass]
public class BitButtonGroupOptionsOrderTests : BunitTestContext
{
    [TestMethod]
    public void BitButtonGroupShouldPreserveOptionsOrderWhenAnOptionIsAddedConditionally()
    {
        var component = RenderComponent<BitButtonGroupOptionsOrderTest>(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        CollectionAssert.AreEqual(new[] { "First", "Middle", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTexts(component));
    }

    [TestMethod]
    public void BitButtonGroupShouldReleaseTheTabStopWhenTheOptionHoldingItIsRemoved()
    {
        var component = RenderComponent<BitButtonGroupOptionRemovalTest>();

        component.FindAll("button")[1].Focus();

        Assert.AreEqual("0", component.FindAll("button")[1].GetAttribute("tabindex"));

        // The removed option takes the focus out of the group with it, so the tab stop goes back to the first
        // button rather than staying booked in the option's name: an option later given the same key is not the
        // one the focus was on, and would otherwise take the tab stop over without the focus ever being there.
        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        Assert.AreEqual("0", component.FindAll("button")[0].GetAttribute("tabindex"));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        Assert.AreEqual("0", component.FindAll("button")[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", component.FindAll("button")[1].GetAttribute("tabindex"));
    }

    private static string[] GetItemTexts(IRenderedComponent<BitButtonGroupOptionsOrderTest> component)
    {
        return component.FindAll(".bit-btg-btx").Select(e => e.TextContent).ToArray();
    }
}
