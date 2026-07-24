using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Nav;

[TestClass]
public class BitNavOptionsOrderTests : BunitTestContext
{
    [TestMethod]
    public void BitNavShouldPreserveOptionsOrderWhenAnOptionIsAddedConditionally()
    {
        var component = RenderComponent<BitNavOptionsOrderTest>(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Child1", "Child2", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        CollectionAssert.AreEqual(new[] { "First", "Child1", "ChildMiddle", "Child2", "Middle", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Child1", "Child2", "Last" }, GetItemTexts(component));
    }

    private static string[] GetItemTexts(IRenderedComponent<BitNavOptionsOrderTest> component)
    {
        return component.FindAll(".bit-nav-itx").Select(e => e.TextContent).ToArray();
    }
}
