using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Toggle;

/// <summary>
/// What a toggle sends before the page is interactive: the after-render pass never runs on a prerender, and the
/// renderer it is handed throws on every JS call, so the switch, its state and its form value have to come out of the
/// render alone.
/// </summary>
[TestClass]
public class BitTogglePrerenderTests
{
    [TestMethod]
    public async Task BitToggleShouldRenderTheSwitchAndItsStateBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitToggle>(new Dictionary<string, object?>
        {
            [nameof(BitToggle.Label)] = "Dark mode",
            [nameof(BitToggle.Value)] = true,
            [nameof(BitToggle.Name)] = "dark-mode",
            [nameof(BitToggle.ErrorMessage)] = "Rejected.",
        });

        StringAssert.Contains(html, "bit-tgl-chk");
        StringAssert.Contains(html, "role=\"switch\"");
        StringAssert.Contains(html, "aria-checked=\"true\"");
        StringAssert.Contains(html, "Dark mode");
        StringAssert.Contains(html, "name=\"dark-mode\"");
        StringAssert.Contains(html, "Rejected.");
    }
}
