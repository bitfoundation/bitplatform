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

    [TestMethod,
        DataRow(null, "bit-btg-ifl"),
        DataRow(BitVariant.Fill, "bit-btg-ifl"),
        DataRow(BitVariant.Outline, "bit-btg-iol"),
        DataRow(BitVariant.Text, "bit-btg-itx")]
    public void BitButtonGroupShouldMarkEveryButtonWithItsVariantWhateverWrapsIt(BitVariant? variant, string expected)
    {
        var component = RenderComponent<BitButtonGroupWrappedOptionTest>(parameters => parameters.Add(p => p.Variant, variant));

        // The stylesheet paints a button's colors through the variant class the button carries itself, so an option an
        // app wraps in an element of its own (a tooltip's anchor) is painted like the ones placed straight in the group.
        var buttons = component.FindAll("button.bit-btg-itm");

        Assert.AreEqual(2, buttons.Count);
        Assert.IsTrue(buttons.All(b => b.ClassList.Contains(expected)), $"Not every button carries {expected}.");
        Assert.AreEqual(1, buttons.Count(b => b.ParentElement?.ClassList.Contains("wrapper") is true));
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
