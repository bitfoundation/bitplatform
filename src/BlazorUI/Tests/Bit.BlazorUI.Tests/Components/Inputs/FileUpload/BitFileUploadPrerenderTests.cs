using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileUpload;

/// <summary>
/// What a file upload sends before the page is interactive: the drop zone and the uploader are wired up through
/// JS in the after-render pass, which never runs on a prerender, and nothing the render itself does may reach for
/// JS - the preloaded files included, which are listed from the parameters alone.
/// </summary>
[TestClass]
public class BitFileUploadPrerenderTests
{
    [TestMethod]
    public async Task BitFileUploadShouldPrerenderItsButtonAndItsPreloadedFilesWithoutJavaScript()
    {
        var html = await Prerenderer.RenderAsync<BitFileUpload>(new Dictionary<string, object?>
        {
            [nameof(BitFileUpload.Label)] = "Attach",
            [nameof(BitFileUpload.Name)] = "attachments",
            [nameof(BitFileUpload.ShowDropArea)] = true,
            [nameof(BitFileUpload.PreloadedFiles)] = (IReadOnlyCollection<BitFileInfo>)[new() { Name = "invoice.pdf", Size = 100, FileId = "server-1" }],
        },
        // The removal request goes through an injected HttpClient, which an app registers itself.
        services => services.AddScoped(_ => new HttpClient()));

        StringAssert.Contains(html, "bit-upl");
        StringAssert.Contains(html, "bit-upl-dra");
        StringAssert.Contains(html, "Attach");
        StringAssert.Contains(html, "type=\"file\"");
        StringAssert.Contains(html, "name=\"attachments\"");
        StringAssert.Contains(html, "invoice.pdf");
        StringAssert.Contains(html, "Already uploaded");
        StringAssert.Contains(html, "value=\"invoice.pdf\"");
    }
}
