using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TagsInput;

/// <summary>
/// What a tags input sends before the page is interactive: the after-render pass that wires its scripts never runs
/// on a prerender, and the renderer it is handed throws on every JS call, so the chips, the input, the suggestion
/// list and the posted field have to come out of the render alone.
/// </summary>
[TestClass]
public class BitTagsInputPrerenderTests
{
    [TestMethod]
    public async Task BitTagsInputShouldRenderItsChipsAndFieldBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitTagsInput>(new Dictionary<string, object?>
        {
            [nameof(BitTagsInput.Label)] = "Skills",
            [nameof(BitTagsInput.Name)] = "skills",
            [nameof(BitTagsInput.Value)] = (ICollection<string>?)new List<string> { "blazor", "razor" },
            [nameof(BitTagsInput.Suggestions)] = new[] { "csharp" },
            [nameof(BitTagsInput.Separators)] = new[] { ";" },
            [nameof(BitTagsInput.ShowClearButton)] = true,
            [nameof(BitTagsInput.AutoFocus)] = true,
        });

        StringAssert.Contains(html, "Skills");
        StringAssert.Contains(html, "bit-tgi-tag");
        StringAssert.Contains(html, "blazor");
        StringAssert.Contains(html, "razor");
        StringAssert.Contains(html, "<datalist");
        StringAssert.Contains(html, "csharp");
        StringAssert.Contains(html, "bit-tgi-cbt");
        StringAssert.Contains(html, "name=\"skills\"");
        StringAssert.Contains(html, "value=\"blazor;razor\"");
    }
}
