using Bit.Bswup.Demo.Server.Services;

namespace Bit.Bswup.Tests.Mcp.Production;

/// <summary>
/// The server's own advice, turned back on the files this repository ships.
/// <para>
/// Every one of these files is handed to an agent as working setup - by the setup guide, by
/// <c>GetBswupSourceFile</c>, by the prompts that say "follow its shape". A file the server would
/// itself report a problem in is therefore worse than no answer, and the reverse is just as
/// interesting: if the inspector ever starts flagging a file that has always been correct, the
/// review has learned to cry wolf and an agent will learn to ignore it.
/// </para>
/// </summary>
[TestClass]
public class ShippedConfigurationTests
{
    /// <summary>Every service-worker file embedded in this assembly - the demo's and the samples'.</summary>
    public static IEnumerable<object[]> ServiceWorkerFiles =>
        BswupSourceCatalog.SourceFiles
            .Where(file => Path.GetFileName(file.Path).StartsWith("service-worker", StringComparison.OrdinalIgnoreCase))
            .Select(file => new object[] { file.Path });

    [TestMethod]
    [DynamicData(nameof(ServiceWorkerFiles))]
    public void EveryShippedServiceWorker_PassesTheServersOwnReview(string path)
    {
        var report = BswupServiceWorkerInspector.Inspect(BswupSourceCatalog.GetSourceFile(path));

        Assert.IsTrue(report.ImportsBswup, $"{path} does not import the engine");
        Assert.AreEqual(0, report.Problems.Length, $"{path}:\n{string.Join("\n", report.Problems)}");
        Assert.AreEqual(0, report.Warnings.Length, $"{path}:\n{string.Join("\n", report.Warnings)}");
    }

    [TestMethod]
    [DynamicData(nameof(ServiceWorkerFiles))]
    public void EveryShippedServiceWorker_AssignsEverySettingBeforeTheImport(string path)
    {
        var report = BswupServiceWorkerInspector.Inspect(BswupSourceCatalog.GetSourceFile(path));

        var late = report.Settings.Where(setting => setting.AfterImport).Select(setting => setting.Name).ToArray();

        Assert.AreEqual(0, late.Length, $"{path}: assigned after importScripts and silently ignored: {string.Join(", ", late)}");
    }

    [TestMethod]
    [DynamicData(nameof(ServiceWorkerFiles))]
    public void EveryShippedServiceWorker_UsesOnlyNamesTheShippedEngineReads(string path)
    {
        var report = BswupServiceWorkerInspector.Inspect(BswupSourceCatalog.GetSourceFile(path));

        var unknown = report.Settings.Where(setting => setting.Recognized is false).Select(setting => setting.Name).ToArray();

        Assert.AreEqual(0, unknown.Length, $"{path}: not read by the engine: {string.Join(", ", unknown)}");
    }

    [TestMethod]
    [DataRow("Demo/Client/wwwroot")]
    [DataRow("Sample/FullSample/Client/wwwroot")]
    public void TheDevelopmentAndPublishedWorkersAgree(string directory)
    {
        // The single most common Bswup bug, and the one the server warns about the loudest: a
        // setting added to only one of the two files works in development and fails in production.
        var development = Settings($"{directory}/service-worker.js");
        var published = Settings($"{directory}/service-worker.published.js");

        CollectionAssert.AreEquivalent(development.Keys.ToArray(), published.Keys.ToArray(),
            $"{directory}: the two files assign different settings");

        foreach (var (name, value) in development)
        {
            Assert.AreEqual(value, published[name], $"{directory}: self.{name} differs between the two files");
        }
    }

    [TestMethod]
    public void TheDocsSitesOwnWorkerCachesWhatTheSiteNeedsOfflineAndNothingItMustNot()
    {
        var script = BswupSourceCatalog.GetSourceFile("Demo/Client/wwwroot/service-worker.published.js");

        var analysis = BswupServiceWorkerInspector.AnalyzeAssets(script,
        [
            "_framework/dotnet.native.wasm", "_framework/blazor.boot.json", "app.css",
            "Bit.Bswup.Demo.Client.styles.scp.css", "service-worker.js", "_content/Bit.Bswup/bit-bswup.sw.js",
        ]);

        var cached = analysis.Assets.ToDictionary(asset => asset.Url, asset => asset.Cached);

        Assert.IsTrue(cached["_framework/dotnet.native.wasm"], "the app cannot boot offline without it");
        Assert.IsTrue(cached["_framework/blazor.boot.json"]);
        Assert.IsTrue(cached["app.css"]);
        Assert.IsFalse(cached["Bit.Bswup.Demo.Client.styles.scp.css"], "scoped-css bundles are in the manifest but never served");
        Assert.IsFalse(cached["service-worker.js"], "a cached worker script corrupts the update cycle");
        Assert.IsFalse(cached["_content/Bit.Bswup/bit-bswup.sw.js"]);
    }

    [TestMethod]
    public void TheDocsSitesOwnWorkerKeepsTheServerOwnedRoutesOutOfTheWorker()
    {
        var script = BswupSourceCatalog.GetSourceFile("Demo/Client/wwwroot/service-worker.published.js")!;
        var code = JavaScriptSource.StripComments(script);

        var handled = JavaScriptSource.ReadAssignments(code, "self").Single(a => a.Name == "serverHandledUrls").Value;

        // The MCP server and its HTTP mirror belong to the server; a controlled tab opening them
        // must not be answered with the cached app shell.
        StringAssert.Contains(handled, @"\/api\/");
        StringAssert.Contains(handled, "mcp");
        StringAssert.Contains(handled, "sitemap");
    }

    [TestMethod]
    public void TheSetupGuidesShipTheSameFilesTheReviewApproves()
    {
        // The guide is assembled from these files, so a guide that compiles and a file that
        // reviews clean have to be the same file.
        foreach (var model in BswupSetupGuide.HostingModels)
        {
            var guide = BswupSetupGuide.Get(model)!;

            foreach (var file in ServiceWorkerFiles.Select(row => (string)row[0]))
            {
                if (guide.Contains($"### `{file}`", StringComparison.Ordinal) is false) continue;

                var report = BswupServiceWorkerInspector.Inspect(BswupSourceCatalog.GetSourceFile(file));

                Assert.AreEqual(0, report.Problems.Length, $"{model} hands out {file}:\n{string.Join("\n", report.Problems)}");
            }
        }
    }

    private static Dictionary<string, string> Settings(string path)
    {
        var script = BswupSourceCatalog.GetSourceFile(path);

        Assert.IsNotNull(script, path);

        return BswupServiceWorkerInspector.Inspect(script).Settings
            .GroupBy(setting => setting.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => Unquote(group.Last().Value), StringComparer.Ordinal);
    }

    /// <summary>
    /// A value without its quotes. The two files are compared on what they MEAN: one spelling a
    /// string with single quotes and the other with double quotes is a style difference, and
    /// failing on it would train everyone to ignore this test.
    /// </summary>
    private static string Unquote(string value)
    {
        var text = value.Trim().TrimEnd(',');

        return text.Length >= 2 && (text[0] is '\'' or '"') && text[^1] == text[0] ? text[1..^1] : text;
    }
}
