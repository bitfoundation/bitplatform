using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.ButtonGroup;

/// <summary>
/// What a button group sends before the page is interactive: the element references its items register, and the focus
/// AutoFocus gives through them, belong to the after-render pass, which never runs on a prerender - so the toggle
/// state, the roving tab stop and the autofocus attribute have to come out of the render itself.
/// </summary>
[TestClass]
public class BitButtonGroupPrerenderTests
{
    [TestMethod]
    public async Task BitButtonGroupShouldPrerenderTheToggledItemAndTheTabStopWithoutJavaScript()
    {
        var html = await Prerenderer.RenderAsync<BitButtonGroup<BitButtonGroupItem>>(new Dictionary<string, object?>
        {
            [nameof(BitButtonGroup<BitButtonGroupItem>.Items)] = new List<BitButtonGroupItem>
            {
                new() { Text = "A", Key = "a" },
                new() { Text = "B", Key = "b" }
            },
            [nameof(BitButtonGroup<BitButtonGroupItem>.SelectionMode)] = BitSelectionMode.Single,
            [nameof(BitButtonGroup<BitButtonGroupItem>.DefaultToggleKey)] = "b",
            [nameof(BitButtonGroup<BitButtonGroupItem>.AutoFocus)] = true,
        });

        Assert.Contains("role=\"radiogroup\"", html);
        Assert.Contains("aria-checked=\"true\"", html);
        Assert.Contains("bit-btg-chk", html);
        Assert.Contains("tabindex=\"0\"", html);
        Assert.Contains("autofocus", html);
    }
}
