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

        Assert.Contains("bit-drp", html);
        Assert.Contains("role=\"combobox\"", html);
        Assert.Contains("Fruit", html);
        Assert.Contains("name=\"fruit\"", html);
        // The trigger names the selection and the mirrored select posts it.
        Assert.Contains("Orange", html);
        Assert.Contains("<option value=\"f-ora\"", html);
        Assert.Contains(">Orange</option>", html);
        Assert.Contains("aria-selected=\"true\"", html);
        Assert.Contains("bit-input-hidden", html);
    }
}
