using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileInput;

/// <summary>
/// What a file input sends before the page is interactive: the drop zone is wired up through JS in the
/// after-render pass, which never runs on a prerender, and nothing the render itself does may reach for JS.
/// </summary>
[TestClass]
public class BitFileInputPrerenderTests
{
    [TestMethod]
    public async Task BitFileInputShouldPrerenderItsButtonAndItsInputsWithoutJavaScript()
    {
        var html = await Prerenderer.RenderAsync<BitFileInput>(new Dictionary<string, object?>
        {
            [nameof(BitFileInput.Label)] = "Attach",
            [nameof(BitFileInput.Name)] = "attachments",
            [nameof(BitFileInput.ShowDropZone)] = true,
            [nameof(BitFileInput.AllowedExtensions)] = new[] { ".pdf" },
        });

        StringAssert.Contains(html, "bit-fin");
        StringAssert.Contains(html, "bit-fin-dzn");
        StringAssert.Contains(html, "Attach");
        StringAssert.Contains(html, "type=\"file\"");
        StringAssert.Contains(html, "accept=\".pdf\"");
        StringAssert.Contains(html, "name=\"attachments\"");
        StringAssert.Contains(html, "bit-input-hidden");
    }
}
