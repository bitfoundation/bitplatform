using System;
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
        Assert.AreEqual("First, Last", GetTriggerText(component));
    }

    private static string[] GetSelectedTexts(IRenderedComponent<BitDropdownLoadingTest> component)
    {
        return component.FindAll("[role=option][aria-selected=true]").Select(e => e.TextContent.Trim()).ToArray();
    }

    // The trigger names itself via aria-labelledby rather than a fixed id, so the element carrying the
    // displayed selection text is found the same way a screen reader would resolve it.
    private static string GetTriggerText(IRenderedComponent<BitDropdownLoadingTest> component)
    {
        // aria-labelledby is a list of ids, and the trigger appends the one of the element holding the
        // displayed selection to the one of the label, so the last of them is the one to read.
        var labelledBy = component.Find("[role=combobox]").GetAttribute("aria-labelledby") ?? string.Empty;
        var textId = labelledBy.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

        Assert.IsNotNull(textId, "The trigger names itself with no id at all.");

        return component.Find($"#{textId}").TextContent.Trim();
    }
}
