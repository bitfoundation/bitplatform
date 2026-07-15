using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

[TestClass]
public class BitChoiceGroupOptionsOrderTests : BunitTestContext
{
    [TestMethod]
    public void BitChoiceGroupShouldPreserveOptionsOrderWhenAnOptionIsAddedConditionally()
    {
        var component = RenderComponent<BitChoiceGroupOptionsOrderTest>(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        CollectionAssert.AreEqual(new[] { "First", "Middle", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTexts(component));
    }

    private static string[] GetItemTexts(IRenderedComponent<BitChoiceGroupOptionsOrderTest> component)
    {
        return component.FindAll(".bit-chg-itx").Select(e => e.TextContent).ToArray();
    }
}
