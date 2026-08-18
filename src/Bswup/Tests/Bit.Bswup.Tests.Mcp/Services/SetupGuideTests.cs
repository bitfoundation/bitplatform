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
        CollectionAssert.AreEqual(new[] { "standalone-wasm", "blazor-web-app" }, BswupSetupGuide.HostingModels);

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
        StringAssert.Contains(guide, expectedTitle, input);
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
            StringAssert.Contains(guide, "## Checklist");
            StringAssert.Contains(guide, "autostart=\"false\"");
            StringAssert.Contains(guide, "service-worker.published.js");
            StringAssert.Contains(guide, "ServiceWorkerAssetsManifest");
            StringAssert.Contains(guide, "Cache-Control: no-cache");
        }
    }

    [TestMethod]
    public void StandaloneGuide_ShipsTheHostDocumentAndBothWorkerFiles()
    {
        StringAssert.Contains(Standalone, "### `Sample/BasicSample/wwwroot/index.html`");
        StringAssert.Contains(Standalone, "### `Sample/BasicSample/wwwroot/service-worker.js`");
        StringAssert.Contains(Standalone, "### `Sample/BasicSample/wwwroot/service-worker.published.js`");
        StringAssert.Contains(Standalone, "### `Sample/BasicSample/Bit.Bswup.BasicSample.csproj`");
    }

    [TestMethod]
    public void StandaloneGuide_SaysWhyTheSplashCannotBeTheComponent()
    {
        StringAssert.Contains(Standalone, "BswupProgress` component cannot help here");
    }

    [TestMethod]
    public void BlazorWebAppGuide_ShipsTheServerHostDocumentAndTheClientWorkerFiles()
    {
        StringAssert.Contains(BlazorWebApp, "App.razor");
        StringAssert.Contains(BlazorWebApp, "### `Sample/FullSample/Client/wwwroot/service-worker.js`");
        StringAssert.Contains(BlazorWebApp, "### `Sample/FullSample/Client/wwwroot/service-worker.published.js`");
    }

    [TestMethod]
    public void BlazorWebAppGuide_CoversWhatTheClientManifestCannotSee()
    {
        StringAssert.Contains(BlazorWebApp, "externalAssets");
        StringAssert.Contains(BlazorWebApp, "blazor.web.js");
        StringAssert.Contains(BlazorWebApp, "resource-collection");
        StringAssert.Contains(BlazorWebApp, "noPrerenderQuery");
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
        foreach (var guide in new[] { Standalone, BlazorWebApp })
        {
            var fences = guide.Split('\n').Count(line => line.TrimStart().StartsWith("```", StringComparison.Ordinal));

            Assert.AreEqual(0, fences % 2, "an unbalanced fence means a file closed its own code block early");
        }
    }

    [TestMethod]
    public void Guides_EndByPointingAtTheToolsThatCheckTheResult()
    {
        foreach (var guide in new[] { Standalone, BlazorWebApp })
        {
            StringAssert.Contains(guide, "InspectBswupServiceWorker");
            StringAssert.Contains(guide, "GetBswupServiceWorkerSettings");
        }
    }

    [TestMethod]
    public void Guides_EmbedTheSampleFilesVerbatim()
    {
        var worker = BswupSourceCatalog.GetSourceFile("Sample/BasicSample/wwwroot/service-worker.js")!.TrimEnd();

        StringAssert.Contains(Standalone, worker, "the guide is the real file, not a retelling of it");
    }
}
