using System.Collections.Generic;
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

    [TestMethod]
    public void BitButtonGroupOptionsShouldHonourTheToggleCapAndCorrectTheBoundKeys()
    {
        IEnumerable<string>? toggleKeys = null;

        var component = RenderComponent<BitButtonGroupOptionMaxTogglesTest>(parameters =>
        {
            parameters.Add(p => p.ToggleKeys, (IEnumerable<string>?)["a", "b", "c"]);
            parameters.Add(p => p.ToggleKeysChanged, (IEnumerable<string>? keys) => toggleKeys = keys);
        });

        var buttons = component.FindAll("button");
        Assert.AreEqual("true", buttons[0].GetAttribute("aria-pressed"));
        Assert.AreEqual("true", buttons[1].GetAttribute("aria-pressed"));

        // The third key is past the cap, so it is not opened with - and the bound value says so, which the options
        // API can only tell once its options have registered themselves, that is once the group has rendered.
        Assert.AreEqual("true", buttons[2].GetAttribute("aria-disabled"));
        Assert.AreEqual("false", buttons[2].GetAttribute("aria-pressed"));
        CollectionAssert.AreEqual(new[] { "a", "b" }, toggleKeys?.ToArray());
    }

    private static string[] GetItemTexts(IRenderedComponent<BitButtonGroupOptionsOrderTest> component)
    {
        return component.FindAll(".bit-btg-btx").Select(e => e.TextContent).ToArray();
    }
}
