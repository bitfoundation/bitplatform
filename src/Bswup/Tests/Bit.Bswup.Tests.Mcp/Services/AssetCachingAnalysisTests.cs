using System.Diagnostics;
using Bit.Bswup.Demo.Server.Dtos;
using Bit.Bswup.Demo.Server.Services;
using Bit.Bswup.Tests.Mcp.TestInfra;

namespace Bit.Bswup.Tests.Mcp.Services;

/// <summary>
/// What <c>AnalyzeBswupAssetCaching</c> answers about concrete assets. This is the tool that says
/// "yes, that file will be there offline", so a wrong answer is worse than no answer - which is
/// why the interesting cases here are the ones where it must refuse to decide rather than guess:
/// a pattern it cannot compile, and a pattern slow enough to run past the analysis budget.
/// </summary>
[TestClass]
public class AssetCachingAnalysisTests
{
    private static BswupAssetAnalysisDto Analyze(string script, params string[] urls)
        => BswupServiceWorkerInspector.AnalyzeAssets(script, urls);

    private static BswupAssetDecisionDto Decision(BswupAssetAnalysisDto analysis, string url)
    {
        var decision = analysis.Assets.FirstOrDefault(asset => asset.Url == url);

        Assert.IsNotNull(decision, $"'{url}' was not decided at all. Notes: {string.Join(" | ", analysis.Notes)}");

        return decision;
    }

    private static void AssertCached(BswupAssetAnalysisDto analysis, string url)
        => Assert.IsTrue(Decision(analysis, url).Cached, $"'{url}' should be cached but was not: {Decision(analysis, url).Reason}");

    private static void AssertNotCached(BswupAssetAnalysisDto analysis, string url)
        => Assert.IsFalse(Decision(analysis, url).Cached, $"'{url}' should not be cached but was: {Decision(analysis, url).Reason}");

    // -- The built-in lists ----------------------------------------------------

    [TestMethod]
    public void Analyze_AppliesTheShippedIncludePatterns()
    {
        var analysis = Analyze(ServiceWorkerFixtures.Clean,
            "_framework/dotnet.native.wasm", "_framework/blazor.boot.json", "css/app.css",
            "_framework/Bit.Bswup.dll", "images/logo.svg", "fonts/inter.woff2");

        foreach (var asset in analysis.Assets)
        {
            Assert.IsTrue(asset.Cached, $"'{asset.Url}' is a stock Blazor asset and must be precached: {asset.Reason}");
        }
    }

    [TestMethod]
    public void Analyze_CachesTheCompressedVariantsTheFrameworkShips()
    {
        var analysis = Analyze(ServiceWorkerFixtures.Clean, "_framework/dotnet.native.wasm.br", "_framework/dotnet.native.wasm.gz");

        AssertCached(analysis, "_framework/dotnet.native.wasm.br");
        AssertCached(analysis, "_framework/dotnet.native.wasm.gz");
    }

    [TestMethod]
    public void Analyze_NeverCachesTheServiceWorkerScripts()
    {
        var analysis = Analyze(ServiceWorkerFixtures.Clean, "service-worker.js", "_content/Bit.Bswup/bit-bswup.sw.js");

        AssertNotCached(analysis, "service-worker.js");
        AssertNotCached(analysis, "_content/Bit.Bswup/bit-bswup.sw.js");
        StringAssert.Contains(Decision(analysis, "service-worker.js").Reason, "excluded by");
    }

    [TestMethod]
    public void Analyze_ReportsAnAssetNoIncludePatternMatches_AsFetchedFromTheNetwork()
    {
        var analysis = Analyze(ServiceWorkerFixtures.Clean, "downloads/report.pdf");

        AssertNotCached(analysis, "downloads/report.pdf");
        StringAssert.Contains(Decision(analysis, "downloads/report.pdf").Reason, "no include pattern matches it");
    }

    [TestMethod]
    public void Analyze_AlwaysSaysWhichUrlsItIsComparing_AndThatTheWorkerExcludesItself()
    {
        var analysis = Analyze(ServiceWorkerFixtures.Clean, "css/app.css");

        Assert.IsTrue(analysis.Notes.Any(note => note.Contains("service-worker-assets.js", StringComparison.Ordinal)));
        Assert.IsTrue(analysis.Notes.Any(note => note.Contains("registered under a custom file name", StringComparison.Ordinal)));
    }

    // -- The file's own lists --------------------------------------------------

    [TestMethod]
    public void Analyze_ExcludeBeatsInclude()
    {
        // The docs site's own rule: scoped-css bundles are in the manifest but never served.
        var analysis = Analyze(ServiceWorkerFixtures.Clean, "Bit.Bswup.Demo.Client.styles.scp.css");

        AssertNotCached(analysis, "Bit.Bswup.Demo.Client.styles.scp.css");
        StringAssert.Contains(Decision(analysis, "Bit.Bswup.Demo.Client.styles.scp.css").Reason, "self.assetsExclude");
    }

    [TestMethod]
    public void Analyze_AppliesTheFilesOwnIncludePattern()
    {
        var script = ServiceWorkerFixtures.WithImport(@"self.assetsInclude = [/\.pdf$/];");
        var analysis = Analyze(script, "downloads/report.pdf");

        AssertCached(analysis, "downloads/report.pdf");
        StringAssert.Contains(Decision(analysis, "downloads/report.pdf").Reason, "self.assetsInclude");
    }

    [TestMethod]
    public void Analyze_MatchesAStringEntryAsALiteralSubstring()
    {
        var script = ServiceWorkerFixtures.WithImport("self.assetsExclude = ['weather.json'];");
        var analysis = Analyze(script, "sample-data/weather.json", "sample-data/forecast.json");

        AssertNotCached(analysis, "sample-data/weather.json");
        AssertCached(analysis, "sample-data/forecast.json");
    }

    [TestMethod]
    public void Analyze_IgnoreDefaultInclude_LeavesOnlyTheFilesOwnPatterns()
    {
        var script = ServiceWorkerFixtures.WithImport("self.ignoreDefaultInclude = true;\nself.assetsInclude = [/\\.css$/];");
        var analysis = Analyze(script, "css/app.css", "_framework/blazor.boot.json");

        AssertCached(analysis, "css/app.css");
        AssertNotCached(analysis, "_framework/blazor.boot.json");
        Assert.IsTrue(analysis.Notes.Any(note => note.Contains("built-in include list is not applied", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Analyze_IgnoreDefaultExclude_LetsTheWorkerScriptsThrough_AndSaysSo()
    {
        var script = ServiceWorkerFixtures.WithImport("self.ignoreDefaultExclude = true;");
        var analysis = Analyze(script, "service-worker.js");

        AssertCached(analysis, "service-worker.js");
        Assert.IsTrue(analysis.Notes.Any(note => note.Contains("built-in exclude list is not applied", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Analyze_HonorsTheRegexLiteralsOwnCaseFlag()
    {
        var script = ServiceWorkerFixtures.WithImport(@"self.assetsExclude = [/APP\.CSS$/i];");
        var analysis = Analyze(script, "css/app.css");

        AssertNotCached(analysis, "css/app.css");
    }

    [TestMethod]
    public void Analyze_CaseInsensitiveUrl_AppliesTheFlagToEveryPattern()
    {
        var script = ServiceWorkerFixtures.WithImport("self.caseInsensitiveUrl = true;\nself.assetsExclude = [/APP\\.CSS$/];");
        var analysis = Analyze(script, "css/app.css");

        AssertNotCached(analysis, "css/app.css");
        Assert.IsTrue(analysis.Notes.Any(note => note.Contains("caseInsensitiveUrl is on", StringComparison.Ordinal)));
    }

    // -- Mode presets reach the analysis too -----------------------------------

    [TestMethod]
    public void Analyze_PassiveModeFromAPreset_IsExplainedInTheNotes()
    {
        var analysis = Analyze(ServiceWorkerFixtures.WithImport("self.mode = 'InitialPrerender';"), "css/app.css");

        Assert.IsTrue(analysis.Notes.Any(note => note.Contains("isPassive is on", StringComparison.Ordinal)),
            "the preset turns passive caching on, and the analysis has to run against the settings the worker ends up with");
    }

    [TestMethod]
    public void Analyze_ExplicitIsPassiveFalse_OverridesThePreset()
    {
        // Exactly what the docs site's own worker does.
        var analysis = Analyze(ServiceWorkerFixtures.WithImport("self.mode = 'InitialPrerender';\nself.isPassive = false;"), "css/app.css");

        Assert.IsFalse(analysis.Notes.Any(note => note.Contains("isPassive is on", StringComparison.Ordinal)));
    }

    // -- Patterns it must refuse to decide on ----------------------------------

    [TestMethod]
    public void Analyze_UnCompilablePattern_IsReportedAndLeftOut()
    {
        var script = ServiceWorkerFixtures.WithImport(@"self.assetsExclude = [/[unclosed/];");
        var analysis = Analyze(script, "css/app.css");

        Assert.IsTrue(analysis.Notes.Any(note => note.Contains("could not be evaluated here", StringComparison.Ordinal)),
            "a pattern that was silently dropped would make every URL it governs look decided");
        AssertCached(analysis, "css/app.css");
    }

    [TestMethod]
    public void Analyze_CatastrophicPattern_ReportsTheUrlsAsUndecidedRatherThanUncached()
    {
        // The patterns come out of a file the caller pasted in, so one of them can be pathological.
        // The whole analysis runs against one deadline, and whatever did not fit must come back as
        // "not analyzed" - reporting a partial scan as a completed one is the wrong answer to give
        // about a cache.
        var script = ServiceWorkerFixtures.WithImport(@"self.assetsExclude = [/(a+)+$/];");
        var urls = Enumerable.Range(0, 200).Select(index => new string('a', 40) + $"b{index}.css").ToArray();

        var stopwatch = Stopwatch.StartNew();
        var analysis = BswupServiceWorkerInspector.AnalyzeAssets(script, urls);
        stopwatch.Stop();

        Assert.IsTrue(analysis.Assets.Length < urls.Length, "the pathological pattern cannot have decided every URL in time");
        Assert.IsTrue(analysis.Notes.Any(note => note.Contains("were NOT analyzed", StringComparison.Ordinal)),
            $"the truncation has to be said out loud. Notes: {string.Join(" | ", analysis.Notes)}");
        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(20),
            $"the analysis must stay bounded; it took {stopwatch.Elapsed}");
    }

    [TestMethod]
    public void Analyze_ManyPatterns_AppliesACapAndSaysSo()
    {
        var patterns = string.Join(", ", Enumerable.Range(0, 150).Select(index => $"/x{index}\\.css$/"));
        var script = ServiceWorkerFixtures.WithImport($"self.assetsInclude = [{patterns}];");

        var analysis = Analyze(script, "css/app.css");

        Assert.IsTrue(analysis.Notes.Any(note => note.Contains("only the first", StringComparison.Ordinal)),
            $"a silently capped list reads as a complete one. Notes: {string.Join(" | ", analysis.Notes)}");
    }

    // -- Shape -----------------------------------------------------------------

    [TestMethod]
    public void Analyze_PublishesThePatternListsItDecidedWith()
    {
        var analysis = Analyze(ServiceWorkerFixtures.Clean, "css/app.css");

        Assert.IsTrue(analysis.Include.Any(pattern => pattern.Contains("(built-in)", StringComparison.Ordinal)));
        Assert.IsTrue(analysis.Exclude.Any(pattern => pattern.Contains("(built-in)", StringComparison.Ordinal)));
        Assert.IsTrue(analysis.Exclude.Any(pattern => pattern.Contains("(self.assetsExclude)", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Analyze_NoUrls_AnswersWithAnEmptyDecisionList()
    {
        var analysis = Analyze(ServiceWorkerFixtures.Clean);

        Assert.AreEqual(0, analysis.Assets.Length);
        Assert.IsTrue(analysis.Include.Length > 0);
    }

    [TestMethod]
    public void Analyze_EmptyScript_StillAppliesTheBuiltInLists()
    {
        var analysis = BswupServiceWorkerInspector.AnalyzeAssets(null, ["css/app.css", "service-worker.js"]);

        AssertCached(analysis, "css/app.css");
        AssertNotCached(analysis, "service-worker.js");
    }
}
