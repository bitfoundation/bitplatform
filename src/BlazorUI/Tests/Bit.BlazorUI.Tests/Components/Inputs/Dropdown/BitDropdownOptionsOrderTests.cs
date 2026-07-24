using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Dropdown;

[TestClass]
public class BitDropdownOptionsOrderTests : BunitTestContext
{
    [TestMethod]
    public void BitDropdownShouldPreserveOptionsOrderWhenAnOptionIsAddedConditionally()
    {
        var component = RenderComponent<BitDropdownOptionsOrderTest>(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        CollectionAssert.AreEqual(new[] { "First", "Middle", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTexts(component));
    }

    private static string[] GetItemTexts(IRenderedComponent<BitDropdownOptionsOrderTest> component)
    {
        return component.FindAll(".bit-drp-itm").Select(e => e.TextContent.Trim()).ToArray();
    }
}
