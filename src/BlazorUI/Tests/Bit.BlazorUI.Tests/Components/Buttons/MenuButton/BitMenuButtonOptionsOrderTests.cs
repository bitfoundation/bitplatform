using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.MenuButton;

[TestClass]
public class BitMenuButtonOptionsOrderTests : BunitTestContext
{
    [TestMethod]
    public void BitMenuButtonShouldPreserveOptionsOrderWhenAnOptionIsAddedConditionally()
    {
        var component = RenderComponent<BitMenuButtonOptionsOrderTest>(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        CollectionAssert.AreEqual(new[] { "First", "Middle", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTexts(component));
    }

    private static string[] GetItemTexts(IRenderedComponent<BitMenuButtonOptionsOrderTest> component)
    {
        return component.FindAll(".bit-mnb-itm .bit-mnb-btx").Select(e => e.TextContent.Trim()).ToArray();
    }
}
