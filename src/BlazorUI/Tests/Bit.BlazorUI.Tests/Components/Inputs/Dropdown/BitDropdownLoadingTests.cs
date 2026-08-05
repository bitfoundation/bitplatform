using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Dropdown;

[TestClass]
public class BitDropdownLoadingTests : BunitTestContext
{
    [TestMethod]
    public void BitDropdownShouldKeepTheSelectionWhenLoadingEnds()
    {
        var component = RenderComponent<BitDropdownLoadingTest>(parameters => parameters.Add(p => p.IsLoading, true));

        component.Render(parameters => parameters.Add(p => p.IsLoading, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetSelectedTexts(component));
        Assert.AreEqual("First, Last", component.Find(".bit-drp-tcn").TextContent.Trim());
    }

    private static string[] GetSelectedTexts(IRenderedComponent<BitDropdownLoadingTest> component)
    {
        return component.FindAll("[role=option][aria-selected=true]").Select(e => e.TextContent.Trim()).ToArray();
    }
}
