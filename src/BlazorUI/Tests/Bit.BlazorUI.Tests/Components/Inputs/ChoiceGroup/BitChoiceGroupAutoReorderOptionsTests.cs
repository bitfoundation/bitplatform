using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

[TestClass]
public class BitChoiceGroupAutoReorderOptionsTests : BunitTestContext
{
    [TestMethod]
    public void BitChoiceGroupShouldAssignMarkupOrderIndexWhenAutoReorderOptionsEnabled()
    {
        // With AutoReorderOptions the choice group reads the DOM order of the option markers via JS
        // interop to keep each option's Index in sync with its markup position, so the test feeds the
        // marker ids (read from the rendered markup, i.e. markup order) back as the result of that call.
        var handler = Context.JSInterop.Setup<string[]>("BitBlazorUI.Utils.getChildrenAttributes", _ => true);

        var component = RenderComponent<BitChoiceGroupAutoReorderTest>(parameters => parameters.Add(p => p.ShowMiddle, false));

        // The conditionally-shown option registers last, so without the reorder its Index would be 2
        // while it renders in the middle. The marker ids are supplied only after it is rendered, so the
        // pending read-back completes with the full, correctly ordered (markup order) set of ids.
        component.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        handler.SetResult(GetMarkerIds(component));

        component.WaitForAssertion(() =>
        {
            var indexByText = GetIndexByText(component);
            Assert.AreEqual(0, indexByText["First"]);
            Assert.AreEqual(1, indexByText["Middle"]);
            Assert.AreEqual(2, indexByText["Last"]);
        });
    }

    private static string[] GetMarkerIds(IRenderedComponent<BitChoiceGroupAutoReorderTest> component)
    {
        return component.FindAll("[data-bit-chg-opt]").Select(e => e.GetAttribute("data-bit-chg-opt")!).ToArray();
    }

    private static Dictionary<string, int> GetIndexByText(IRenderedComponent<BitChoiceGroupAutoReorderTest> component)
    {
        return component.FindComponents<BitChoiceGroupOption<string>>()
                        .ToDictionary(c => c.Instance.Text!, c => c.Instance.Index);
    }
}
