using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Timeline;

[TestClass]
public class BitTimelineOptionsOrderTests : BunitTestContext
{
    [TestMethod]
    public void BitTimelineShouldPreserveOptionsOrderWhenAnOptionIsAddedConditionally()
    {
        var component = RenderComponent<BitTimelineOptionsOrderTest>(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        CollectionAssert.AreEqual(new[] { "First", "Middle", "Last" }, GetItemTexts(component));

        component.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, GetItemTexts(component));
    }

    private static string[] GetItemTexts(IRenderedComponent<BitTimelineOptionsOrderTest> component)
    {
        return component.FindAll(".bit-tln-pcn .bit-tln-ttx").Select(e => e.TextContent).ToArray();
    }
}
