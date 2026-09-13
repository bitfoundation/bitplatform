using Bit.Bswup.Demo.Server.Dtos;
using Bit.Bswup.Demo.Server.Services;
using Bit.Bswup.Tests.Mcp.TestInfra;

namespace Bit.Bswup.Tests.Mcp.Services;

/// <summary>
/// What <c>InspectBswupServiceWorker</c> reports about a file. Every finding below stands for a
/// configuration that ships green and fails on a user's machine, offline, later - so the tests
/// pin both directions: the broken file is reported, and the correct one is left alone (a review
/// that cries wolf is one an agent learns to ignore).
/// </summary>
[TestClass]
public class ServiceWorkerInspectionTests
{
    private static BswupServiceWorkerInspectionDto Inspect(string script) => BswupServiceWorkerInspector.Inspect(script);

    private static bool Mentions(IEnumerable<string> messages, params string[] fragments)
        => messages.Any(message => fragments.All(fragment => message.Contains(fragment, StringComparison.OrdinalIgnoreCase)));

    // -- The healthy file ------------------------------------------------------

    [TestMethod]
    public void Inspect_CleanFile_ReportsNoProblems()
    {
        var report = Inspect(ServiceWorkerFixtures.Clean);

        Assert.IsTrue(report.ImportsBswup);
        Assert.IsNotNull(report.Import);
        StringAssert.Contains(report.Import, "bit-bswup.sw.js");
        Assert.AreEqual(0, report.Problems.Length, string.Join(" | ", report.Problems));
        Assert.AreEqual(0, report.Warnings.Length, string.Join(" | ", report.Warnings));
    }

    [TestMethod]
    public void Inspect_EngineNameOnlyInAStringAfterAnImport_IsNotReportedAsImportingIt()
    {
        // A file that imports the cleanup worker and then merely names the engine in a string.
        // The nearest importScripts above that name has already closed, so it is not the call
        // importing it - and reporting it as one would quote an import line the name is not in.
        var report = Inspect("""
            self.importScripts('_content/Bit.Bswup/bit-bswup.sw-cleanup.js');
            const engine = '_content/Bit.Bswup/bit-bswup.sw.js';
            """);

        Assert.IsFalse(report.ImportsBswup, $"the engine is only named in a string; Import: {report.Import}");
    }

    [TestMethod]
    public void Inspect_ListsTheSettingsTheFileAssigns_WithTheirSummaries()
    {
        var report = Inspect(ServiceWorkerFixtures.Clean);

        CollectionAssert.AreEquivalent(
            new[] { "assetsExclude", "caseInsensitiveUrl", "isPassive" },
            report.Settings.Select(setting => setting.Name).ToArray());

        Assert.IsTrue(report.Settings.All(setting => setting.Recognized), "every setting here is one the shipped worker reads");
        Assert.IsTrue(report.Settings.All(setting => setting.AfterImport is false));
        Assert.IsTrue(report.Settings.All(setting => string.IsNullOrWhiteSpace(setting.Summary) is false),
            "a recognized setting must come back with the summary the worker's own source carries");
    }

    [TestMethod]
    public void Inspect_DoesNotReportImportScriptsAsASetting()
    {
        var report = Inspect(ServiceWorkerFixtures.Clean);

        Assert.IsFalse(report.Settings.Any(setting => setting.Name == "importScripts"));
    }

    // -- The failures that produce no visible error ----------------------------

    [TestMethod]
    public void Inspect_MissingEngineImport_IsAProblem()
    {
        var report = Inspect("self.isPassive = false;");

        Assert.IsFalse(report.ImportsBswup);
        Assert.IsTrue(Mentions(report.Problems, "never imports the Bswup engine"));
    }

    [TestMethod]
    public void Inspect_SettingAssignedAfterTheImport_IsAProblem()
    {
        var report = Inspect(ServiceWorkerFixtures.SettingAfterImport);

        var late = report.Settings.Single(setting => setting.Name == "caseInsensitiveUrl");
        Assert.IsTrue(late.AfterImport);

        var early = report.Settings.Single(setting => setting.Name == "isPassive");
        Assert.IsFalse(early.AfterImport);

        Assert.IsTrue(Mentions(report.Problems, "caseInsensitiveUrl", "AFTER the importScripts line"));
        Assert.IsFalse(Mentions(report.Problems, "isPassive", "AFTER"));
    }

    [TestMethod]
    public void Inspect_UnknownSettingName_IsAProblemAndSuggestsTheRealOne()
    {
        var report = Inspect(ServiceWorkerFixtures.UnknownSetting);

        Assert.IsFalse(report.Settings.Single().Recognized);
        Assert.IsTrue(Mentions(report.Problems, "assetsExclud", "not a setting"));
        Assert.IsTrue(Mentions(report.Problems, "Did you mean", "assetsExclude"),
            "a near-miss name is the whole point of the check");
    }

    [TestMethod]
    public void Inspect_UnknownSettingWithNoNearMatch_PointsAtTheFullList()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.qqqzzz = 1;"));

        Assert.IsTrue(Mentions(report.Problems, "qqqzzz", "GetBswupServiceWorkerSettings"));
    }

    [TestMethod]
    public void Inspect_SettingAssignedTwice_IsAWarning()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.isPassive = true;\nself.isPassive = false;"));

        Assert.IsTrue(Mentions(report.Warnings, "isPassive", "assigned 2 times"));
    }

    // -- Mode presets ----------------------------------------------------------

    [TestMethod]
    public void Inspect_UnknownMode_IsAProblemThatNamesTheValidValues()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.mode = 'Offline';"));

        Assert.IsTrue(Mentions(report.Problems, "'Offline'", "not a mode"));
        Assert.IsTrue(Mentions(report.Problems, "FullOffline"), "the valid modes must be listed");
    }

    [TestMethod]
    public void Inspect_KnownMode_ReportsWhatThePresetFillsIn()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.mode = 'FullOffline';"));

        Assert.AreEqual(0, report.Problems.Length, string.Join(" | ", report.Problems));
        Assert.IsTrue(Mentions(report.Notes, "'FullOffline' preset fills in", "defaultUrl"));
    }

    [TestMethod]
    public void Inspect_ExplicitAssignmentWinsOverThePreset_AndSaysSo()
    {
        // What the docs site's own worker does: take the preset, then turn passive caching back off.
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.mode = 'InitialPrerender';\nself.isPassive = false;"));

        Assert.IsTrue(Mentions(report.Notes, "own assignments win over the preset", "isPassive"));
    }

    // -- The offline app shell -------------------------------------------------

    [TestMethod]
    public void Inspect_RootDefaultUrlWithNoRootExternalAsset_IsAWarning()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.defaultUrl = '/';"));

        Assert.IsTrue(Mentions(report.Warnings, "defaultUrl is the root document"));
    }

    [TestMethod]
    public void Inspect_RootDefaultUrlWithARootExternalAsset_IsNotAWarning()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("""
            self.defaultUrl = '/';
            self.externalAssets = [{ "url": "/" }];
            self.noPrerenderQuery = 'no-prerender=true';
            """));

        Assert.IsFalse(Mentions(report.Warnings, "defaultUrl is the root document"));
    }

    [TestMethod]
    public void Inspect_RootDefaultUrlSuppliedByAPreset_IsCheckedAgainstExternalAssetsToo()
    {
        // The preset supplies defaultUrl = '/', so the shell check has to run even though the
        // file never assigns it - checking only the file's own assignments would miss this.
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.mode = 'FullOffline';"));

        Assert.IsTrue(Mentions(report.Warnings, "defaultUrl is the root document"));
    }

    [TestMethod]
    public void Inspect_RootExternalAssetWithoutDefaultUrl_IsANote()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("""self.externalAssets = [{ "url": "/" }];"""));

        Assert.IsTrue(Mentions(report.Notes, "defaultUrl is left at its default"));
    }

    [TestMethod]
    public void Inspect_RootShellWithoutNoPrerenderQuery_IsANote()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("""
            self.defaultUrl = '/';
            self.externalAssets = [{ "url": "/" }];
            """));

        Assert.IsTrue(Mentions(report.Notes, "noPrerenderQuery"));
    }

    // -- The URL-matching lists ------------------------------------------------

    [TestMethod]
    public void Inspect_StringEntriesInAUrlList_AreAWarningAboutSubstringMatching()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.serverHandledUrls = ['/api/', /\\/health$/];"));

        Assert.IsTrue(Mentions(report.Warnings, "serverHandledUrls", "LITERAL SUBSTRING"));
        Assert.IsTrue(Mentions(report.Warnings, "'/api/'"), "the offending entry must be named");
    }

    [TestMethod]
    public void Inspect_RegexOnlyUrlList_IsNotAWarning()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.serverHandledUrls = [/\\/api\\//];"));

        Assert.IsFalse(Mentions(report.Warnings, "serverHandledUrls"));
    }

    [TestMethod]
    public void Inspect_UrlListBuiltFromVariables_IsReportedAsUnanalyzed()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.assetsExclude = [...sharedPatterns];"));

        Assert.IsTrue(Mentions(report.Notes, "assetsExclude", "not analyzed"));
    }

    [TestMethod]
    public void Inspect_ProhibitedUrls_SaysItIsNotASecurityBoundary()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.prohibitedUrls = [/\\/admin\\//];"));

        Assert.IsTrue(Mentions(report.Notes, "not a security boundary"));
    }

    // -- The flags -------------------------------------------------------------

    [TestMethod]
    public void Inspect_IgnoreDefaultExclude_WarnsAboutCachingTheWorkerScripts()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.ignoreDefaultExclude = true;"));

        Assert.IsTrue(Mentions(report.Warnings, "ignoreDefaultExclude", "corrupts the update cycle"));
    }

    [TestMethod]
    public void Inspect_IgnoreDefaultInclude_IsANoteAboutWhatStopsBeingCached()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.ignoreDefaultInclude = true;"));

        Assert.IsTrue(Mentions(report.Notes, "ignoreDefaultInclude", "ONLY the patterns in assetsInclude"));
    }

    [TestMethod]
    public void Inspect_FalsyFlag_IsNotReported()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.ignoreDefaultExclude = false;"));

        Assert.IsFalse(Mentions(report.Warnings, "ignoreDefaultExclude"), "an explicitly-off flag changes nothing");
    }

    [TestMethod]
    public void Inspect_UnknownErrorTolerance_IsAWarning()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.errorTolerance = 'careful';"));

        Assert.IsTrue(Mentions(report.Warnings, "errorTolerance is 'careful'", "falls back to 'lax'"));
    }

    [TestMethod]
    public void Inspect_StrictErrorTolerance_IsANoteAboutWhatItCosts()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.errorTolerance = 'strict';"));

        Assert.AreEqual(0, report.Warnings.Length, string.Join(" | ", report.Warnings));
        Assert.IsTrue(Mentions(report.Notes, "strict", "rejects the install"));
    }

    [TestMethod]
    public void Inspect_FetchDiagnostics_WarnsAboutLeavingItOnInProduction()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.enableFetchDiagnostics = true;"));

        Assert.IsTrue(Mentions(report.Warnings, "enableFetchDiagnostics", "every user"));
    }

    [TestMethod]
    public void Inspect_IntegrityCheck_IsANoteAboutItsRequirements()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("self.enableIntegrityCheck = true;"));

        Assert.IsTrue(Mentions(report.Notes, "enableIntegrityCheck", "byte-identically"));
    }

    // -- The cleanup worker ----------------------------------------------------

    [TestMethod]
    public void Inspect_CleanupWorker_IsRecognizedAndNotReportedAsAMissingImport()
    {
        var report = Inspect(ServiceWorkerFixtures.Cleanup);

        Assert.IsTrue(Mentions(report.Notes, "CLEANUP worker"));
        Assert.IsFalse(Mentions(report.Problems, "never imports the Bswup engine"),
            "backing Bswup out is a supported state, not a broken file");
    }

    // -- Robustness ------------------------------------------------------------

    [TestMethod]
    public void Inspect_FindsTheImportEvenWhenTheEngineNameAppearsEarlierInAString()
    {
        var script = """
            const engine = 'bit-bswup.sw.js';
            self.isPassive = false;
            self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');
            """;

        var report = Inspect(script);

        Assert.IsTrue(report.ImportsBswup, "stopping at the first mention would report a working file as broken");
        Assert.IsFalse(report.Settings.Single(setting => setting.Name == "isPassive").AfterImport);
    }

    [TestMethod]
    public void Inspect_CommentedOutSettingsAreNotReported()
    {
        var report = Inspect(ServiceWorkerFixtures.WithImport("//self.enableFetchDiagnostics = true;"));

        Assert.AreEqual(0, report.Settings.Length);
        Assert.AreEqual(0, report.Warnings.Length);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public void Inspect_EmptyInput_AnswersInsteadOfThrowing(string? script)
    {
        var report = BswupServiceWorkerInspector.Inspect(script);

        Assert.IsFalse(report.ImportsBswup);
        Assert.AreEqual(0, report.Settings.Length);
        Assert.IsTrue(report.Problems.Length > 0, "an empty file is a file that does nothing");
    }
}
