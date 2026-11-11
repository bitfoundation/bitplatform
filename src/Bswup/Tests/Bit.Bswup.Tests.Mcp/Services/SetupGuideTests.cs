using Bit.Bswup.Demo.Server.Services;

namespace Bit.Bswup.Tests.Mcp.Services;

/// <summary>
/// The answer to "add Bswup to this app". It is assembled out of the real sample projects, so the
/// checks here are that the assembly still finds them - a guide that silently lost its
/// service-worker files is prose an agent will follow into a broken app - and that the two hosting
/// models really do answer differently, which is the entire reason the tool takes an argument.
/// </summary>
[TestClass]
public class SetupGuideTests
{
    private static string Standalone => BswupSetupGuide.Get("standalone-wasm")!;

    private static string BlazorWebApp => BswupSetupGuide.Get("blazor-web-app")!;

    [TestMethod]
    public void HostingModels_AreTheTwoTheGuideAnswersFor()
    {
        Assert.AreSequenceEqual(new[] { "standalone-wasm", "blazor-web-app" }, BswupSetupGuide.HostingModels);

        foreach (var model in BswupSetupGuide.HostingModels)
        {
            Assert.IsNotNull(BswupSetupGuide.Get(model), model);
        }
    }

    [TestMethod]
    [DataRow("standalone", "standalone Blazor WebAssembly")]
    [DataRow("wasm", "standalone Blazor WebAssembly")]
    [DataRow("webassembly", "standalone Blazor WebAssembly")]
    [DataRow("blazor-wasm", "standalone Blazor WebAssembly")]
    [DataRow("Standalone-WASM", "standalone Blazor WebAssembly")]
    [DataRow("standalone wasm", "standalone Blazor WebAssembly")]
    [DataRow("web-app", "Blazor Web App")]
    [DataRow("blazorwebapp", "Blazor Web App")]
    [DataRow("interactivewebassembly", "Blazor Web App")]
    [DataRow("hosted", "Blazor Web App")]
    [DataRow("  Blazor Web App  ", "Blazor Web App")]
    public void Get_AcceptsTheNamesPeopleActuallyUseForAHostingModel(string input, string expectedTitle)
    {
        var guide = BswupSetupGuide.Get(input);

        Assert.IsNotNull(guide, input);
        Assert.Contains(expectedTitle, guide, input);
    }

    [TestMethod]
    [DataRow("maui")]
    [DataRow("")]
    [DataRow(null)]
    public void Get_ReturnsNullForAModelItDoesNotKnow(string? input)
    {
        Assert.IsNull(BswupSetupGuide.Get(input));
    }

    [TestMethod]
    public void Guides_CarryTheChecklistWithTheTwoRulesThatDecideMostBugs()
    {
        foreach (var guide in new[] { Standalone, BlazorWebApp })
        {
            Assert.Contains("## Checklist", guide);
            Assert.Contains("autostart=\"false\"", guide);
            Assert.Contains("service-worker.published.js", guide);
            Assert.Contains("ServiceWorkerAssetsManifest", guide);
            Assert.Contains("Cache-Control: no-cache", guide);
        }
    }

    [TestMethod]
    public void StandaloneGuide_ShipsTheHostDocumentAndBothWorkerFiles()
    {
        Assert.Contains("### `Sample/BasicSample/wwwroot/index.html`", Standalone);
        Assert.Contains("### `Sample/BasicSample/wwwroot/service-worker.js`", Standalone);
        Assert.Contains("### `Sample/BasicSample/wwwroot/service-worker.published.js`", Standalone);
        Assert.Contains("### `Sample/BasicSample/Bit.Bswup.BasicSample.csproj`", Standalone);
    }

    [TestMethod]
    public void StandaloneGuide_SaysWhyTheSplashCannotBeTheComponent()
    {
        Assert.Contains("BswupProgress` component cannot help here", Standalone);
    }

    [TestMethod]
    public void BlazorWebAppGuide_ShipsTheServerHostDocumentAndTheClientWorkerFiles()
    {
        Assert.Contains("App.razor", BlazorWebApp);
        Assert.Contains("### `Sample/FullSample/Client/wwwroot/service-worker.js`", BlazorWebApp);
        Assert.Contains("### `Sample/FullSample/Client/wwwroot/service-worker.published.js`", BlazorWebApp);
    }

    [TestMethod]
    public void BlazorWebAppGuide_CoversWhatTheClientManifestCannotSee()
    {
        Assert.Contains("externalAssets", BlazorWebApp);
        Assert.Contains("blazor.web.js", BlazorWebApp);
        Assert.Contains("resource-collection", BlazorWebApp);
        Assert.Contains("noPrerenderQuery", BlazorWebApp);
    }

    [TestMethod]
    public void Guides_DoNotPadTheAnswerWithTheSamplesPagesAndLayouts()
    {
        Assert.IsFalse(Standalone.Contains("### `Sample/BasicSample/Pages/", StringComparison.Ordinal),
            "only the files setup actually lives in belong here");
    }

    [TestMethod]
    public void Guides_HostDocumentComesBeforeTheWorkerFiles()
    {
        var host = Standalone.IndexOf("### `Sample/BasicSample/wwwroot/index.html`", StringComparison.Ordinal);
        var worker = Standalone.IndexOf("### `Sample/BasicSample/wwwroot/service-worker.js`", StringComparison.Ordinal);
        var project = Standalone.IndexOf("### `Sample/BasicSample/Bit.Bswup.BasicSample.csproj`", StringComparison.Ordinal);

        Assert.IsTrue(host < worker && worker < project, "the files are meant to read in the order they are written");
    }

    [TestMethod]
    public void Guides_FenceEveryFileWithAFenceItsContentCannotClose()
    {
        // Counting fences would only catch an odd one out. What actually breaks a guide is an
        // embedded three-backtick sample inside a file opened with three of its own: the count
        // stays even and the block still ends early. So every open fence is measured against the
        // fences inside it - the outer one has to be the longer.
        foreach (var guide in new[] { Standalone, BlazorWebApp })
        {
            var open = 0;
            var heading = "(none)";

            foreach (var line in guide.Split('\n'))
            {
                // Trimmed at both ends: the guide is built with AppendLine, so splitting on
                // '\n' leaves a trailing carriage return, which would make a bare
                // closing fence look like content rather than the fence it is.
                var text = line.Trim();

                if (open == 0 && text.StartsWith("### ", StringComparison.Ordinal)) heading = text;

                if (text.StartsWith("```", StringComparison.Ordinal) is false) continue;

                var length = text.TakeWhile(character => character == '`').Count();

                // A closing fence is as long as the one it opens and carries nothing else; a
                // shorter one, or one with a language after it, is content inside the block.
                var closes = length == open && text.Length == length;

                if (open == 0) { open = length; continue; }

                Assert.IsTrue(closes || length < open,
                    $"the block under {heading} is fenced with {open} backticks and holds a {length}-backtick fence, which closes it early");

                if (closes) open = 0;
            }

            Assert.AreEqual(0, open, $"the block under {heading} is never closed");
        }
    }

    [TestMethod]
    public void Guides_EndByPointingAtTheToolsThatCheckTheResult()
    {
        foreach (var guide in new[] { Standalone, BlazorWebApp })
        {
            Assert.Contains("InspectBswupServiceWorker", guide);
            Assert.Contains("GetBswupServiceWorkerSettings", guide);
        }
    }

    [TestMethod]
    public void Guides_EmbedTheSampleFilesVerbatim()
    {
        var worker = BswupSourceCatalog.GetSourceFile("Sample/BasicSample/wwwroot/service-worker.js")!.TrimEnd();

        Assert.Contains(worker, Standalone, "the guide is the real file, not a retelling of it");
    }
}
