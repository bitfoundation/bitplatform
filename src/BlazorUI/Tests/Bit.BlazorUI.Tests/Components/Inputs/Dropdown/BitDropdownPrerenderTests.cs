using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Dropdown;

/// <summary>
/// What a dropdown sends before the page is interactive: the callout and the keyboard listeners are wired up through
/// JS in the after-render pass, which never runs on a prerender, and nothing the render itself does may reach for JS
/// - the selection a value names is resolved from the items alone.
/// </summary>
[TestClass]
public class BitDropdownPrerenderTests
{
    [TestMethod]
    public async Task BitDropdownShouldPrerenderItsSelectionAndItsItemsWithoutJavaScript()
    {
        var html = await Prerenderer.RenderAsync<BitDropdown<BitDropdownItem<string>, string>>(new Dictionary<string, object?>
        {
            [nameof(BitDropdown<BitDropdownItem<string>, string>.Label)] = "Fruit",
            [nameof(BitDropdown<BitDropdownItem<string>, string>.Name)] = "fruit",
            [nameof(BitDropdown<BitDropdownItem<string>, string>.DefaultValue)] = "f-ora",
            [nameof(BitDropdown<BitDropdownItem<string>, string>.Items)] = new List<BitDropdownItem<string>>
            {
                new() { Text = "Apple", Value = "f-app" },
                new() { Text = "Orange", Value = "f-ora" },
            },
        });

        StringAssert.Contains(html, "bit-drp");
        StringAssert.Contains(html, "role=\"combobox\"");
        StringAssert.Contains(html, "Fruit");
        StringAssert.Contains(html, "name=\"fruit\"");
        // The trigger names the selection and the mirrored select posts it.
        StringAssert.Contains(html, "Orange");
        StringAssert.Contains(html, "<option value=\"f-ora\"");
        StringAssert.Contains(html, ">Orange</option>");
        StringAssert.Contains(html, "aria-selected=\"true\"");
        StringAssert.Contains(html, "bit-input-hidden");
    }
}
