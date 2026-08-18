using System.Text.RegularExpressions;
using Bit.Bswup.Demo.Server.Services;

namespace Bit.Bswup.Tests.Mcp.Services;

/// <summary>
/// The reference the tools answer from, read out of the library's own TypeScript rather than out
/// of prose. The point of the catalog is that it cannot drift from the shipped build, so these
/// tests fail the moment a parse stops finding what it used to - which is exactly when an agent
/// would start being told a default that no longer exists.
/// </summary>
[TestClass]
public class ScriptCatalogTests
{
    // -- Version ---------------------------------------------------------------

    [TestMethod]
    public void Version_IsReadOffTheShippedPageScript()
    {
        Assert.AreNotEqual("unknown", BswupScriptCatalog.Version,
            "the version stamp could not be parsed - every tool then reports 'unknown' as the build it answers for");
        StringAssert.Matches(BswupScriptCatalog.Version, new Regex(@"^\d+\.\d+\.\d+"));
    }

    [TestMethod]
    public void Version_MatchesTheVersionTheShippedWorkerStamps()
    {
        // Two files, one release. A mismatch means one of them was published without the other.
        var worker = BswupSourceCatalog.GetSourceFile("Library/Scripts/bit-bswup.sw.ts");

        Assert.IsNotNull(worker);
        StringAssert.Contains(worker, $"'{BswupScriptCatalog.Version}'");
    }

    // -- Script-tag attributes -------------------------------------------------

    [TestMethod]
    public void ScriptOptions_CoverEveryDocumentedAttribute()
    {
        var names = BswupScriptCatalog.ScriptOptions.Select(option => option.Name).ToArray();

        CollectionAssert.AreEquivalent(
            new[] { "scope", "log", "sw", "handler", "blazorScript", "updateInterval", "updateOnVisibility", "stallTimeout", "persistStorage", "options" },
            names);
    }

    [TestMethod]
    public void ScriptOptions_AreAllStillReadByTheShippedScript()
    {
        var unread = BswupScriptCatalog.ScriptOptions
            .Where(option => option.VerifiedFromSource is false)
            .Select(option => option.Name)
            .ToArray();

        Assert.AreEqual(0, unread.Length,
            $"documented but no longer read by bit-bswup.js: {string.Join(", ", unread)}");
    }

    [TestMethod]
    [DataRow("sw", "service-worker.js")]
    [DataRow("log", "warn")]
    [DataRow("scope", "/")]
    [DataRow("stallTimeout", "60")]
    [DataRow("persistStorage", "false")]
    [DataRow("updateOnVisibility", "false")]
    public void ScriptOptions_ReportTheDefaultTheShippedScriptApplies(string name, string expected)
    {
        var option = BswupScriptCatalog.ScriptOptions.Single(o => o.Name == name);

        Assert.AreEqual(expected, option.Default);
    }

    [TestMethod]
    public void ScriptOptions_EachCarryTheContextAnAgentNeedsToUseIt()
    {
        foreach (var option in BswupScriptCatalog.ScriptOptions)
        {
            Assert.AreEqual("Script attribute", option.Kind, option.Name);
            StringAssert.Contains(option.SetIn, "bit-bswup.js", option.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(option.Summary), $"{option.Name} has no summary");
            Assert.IsFalse(string.IsNullOrWhiteSpace(option.Type), $"{option.Name} has no type");
            StringAssert.Contains(option.Docs, "GetBswupDocsPage", option.Name);
        }
    }

    // -- Service-worker settings -----------------------------------------------

    [TestMethod]
    public void WorkerSettings_CoverTheSettingsTheShippedWorkerDeclares()
    {
        var names = BswupScriptCatalog.WorkerSettings.Select(setting => setting.Name).ToArray();

        foreach (var expected in new[]
        {
            "assetsInclude", "assetsExclude", "externalAssets", "defaultUrl", "assetsUrl", "prohibitedUrls",
            "caseInsensitiveUrl", "serverHandledUrls", "serverRenderedUrls", "noPrerenderQuery",
            "ignoreDefaultInclude", "ignoreDefaultExclude", "isPassive", "enableIntegrityCheck",
            "errorTolerance", "maxRetries", "retryDelay", "enableDiagnostics", "enableFetchDiagnostics",
            "disableHashlessAssetsUpdate", "forcePrerender", "enableCacheControl", "cacheVersion", "mode",
        })
        {
            CollectionAssert.Contains(names, expected);
        }
    }

    [TestMethod]
    public void WorkerSettings_DoNotIncludeTheWorkersOwnInternals()
    {
        var names = BswupScriptCatalog.WorkerSettings.Select(setting => setting.Name).ToArray();

        foreach (var internals in new[] { "clients", "skipWaiting", "registration", "assetsManifest" })
        {
            CollectionAssert.DoesNotContain(names, internals, $"'{internals}' is not something an app assigns");
        }
    }

    [TestMethod]
    public void WorkerSettings_ReadFromSourceCarryTheSummaryWrittenNextToTheDeclaration()
    {
        foreach (var setting in BswupScriptCatalog.WorkerSettings.Where(s => s.VerifiedFromSource))
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(setting.Summary),
                $"'{setting.Name}' was found in the worker's interface but its description was not");
            StringAssert.Contains(setting.SetIn, "BEFORE the importScripts line", setting.Name);
        }
    }

    [TestMethod]
    public void WorkerSettings_ReportTheDefaultsTheWorkerAppliesItself()
    {
        var settings = BswupScriptCatalog.WorkerSettings.ToDictionary(setting => setting.Name, StringComparer.Ordinal);

        Assert.AreEqual("lax", settings["errorTolerance"].Default, "read off `self.errorTolerance ||= 'lax'`");
        Assert.AreEqual("2", settings["maxRetries"].Default);
        Assert.AreEqual("300", settings["retryDelay"].Default);
    }

    [TestMethod]
    public void IsKnownSetting_AcceptsTheRealNamesAndRejectsATypo()
    {
        Assert.IsTrue(BswupScriptCatalog.IsKnownSetting("assetsInclude"));
        Assert.IsTrue(BswupScriptCatalog.IsKnownSetting("noPrerenderQuery"));
        Assert.IsFalse(BswupScriptCatalog.IsKnownSetting("assetsInclud"));
        Assert.IsFalse(BswupScriptCatalog.IsKnownSetting("AssetsInclude"), "JavaScript property names are case-sensitive");
        Assert.IsFalse(BswupScriptCatalog.IsKnownSetting("skipWaiting"), "a worker internal is not a setting an app assigns");
    }

    [TestMethod]
    public void IsKnownSetting_RejectsASettingTheWorkerNoLongerDeclares()
    {
        // A name kept for documentation after the worker dropped it is documented, not recognized -
        // the inspector must go on reporting it as a name the engine will silently ignore.
        var documentedOnly = BswupScriptCatalog.WorkerSettings.Where(setting => setting.VerifiedFromSource is false).ToArray();

        foreach (var setting in documentedOnly)
        {
            Assert.IsFalse(BswupScriptCatalog.IsKnownSetting(setting.Name), setting.Name);
            StringAssert.Contains(setting.Summary, "no longer declares", setting.Name);
        }
    }

    [TestMethod]
    public void GetSettingSummary_AnswersForARealSettingAndNotForAnythingElse()
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(BswupScriptCatalog.GetSettingSummary("isPassive")));
        Assert.IsNull(BswupScriptCatalog.GetSettingSummary("nonsense"));
    }

    // -- Mode presets ----------------------------------------------------------

    [TestMethod]
    public void Modes_AreTheOnesTheShippedWorkerSwitchesOn()
    {
        CollectionAssert.AreEquivalent(
            new[] { "NoPrerender", "InitialPrerender", "AlwaysPrerender", "FullOffline" },
            BswupScriptCatalog.Modes.Select(mode => mode.Name).ToArray());
    }

    [TestMethod]
    public void Modes_ThatFallThroughTogether_AreListedSeparatelyWithTheSameSettings()
    {
        // 'NoPrerender' and 'InitialPrerender' share one block in the worker, but each is a value
        // an app can write, so each has to be answerable on its own.
        var noPrerender = BswupScriptCatalog.Modes.Single(mode => mode.Name == "NoPrerender");
        var initialPrerender = BswupScriptCatalog.Modes.Single(mode => mode.Name == "InitialPrerender");

        CollectionAssert.AreEquivalent(noPrerender.Settings.ToArray(), initialPrerender.Settings.ToArray());
    }

    [TestMethod]
    public void Modes_ExpandToTheSettingsTheWorkerFillsIn()
    {
        var fullOffline = BswupScriptCatalog.Modes.Single(mode => mode.Name == "FullOffline");

        Assert.AreEqual("false", fullOffline.Settings["isPassive"]);
        Assert.AreEqual("/", fullOffline.Settings["defaultUrl"]);
        Assert.AreEqual("false", fullOffline.Settings["forcePrerender"]);
        Assert.AreEqual("lax", fullOffline.Settings["errorTolerance"]);
        Assert.AreEqual("true", fullOffline.Settings["caseInsensitiveUrl"]);

        var initialPrerender = BswupScriptCatalog.Modes.Single(mode => mode.Name == "InitialPrerender");

        Assert.AreEqual("true", initialPrerender.Settings["isPassive"]);
        Assert.AreEqual("no-prerender=true", initialPrerender.Settings["noPrerenderQuery"],
            "this is the reason the docs site picks this preset");
    }

    [TestMethod]
    public void Modes_EachSayThatAnExplicitAssignmentWins()
    {
        foreach (var mode in BswupScriptCatalog.Modes)
        {
            Assert.IsTrue(mode.Settings.Count > 0, mode.Name);
            StringAssert.Contains(mode.Note, "only fills settings", mode.Name);
        }
    }

    [TestMethod]
    public void Modes_OnlyFillSettingsTheWorkerActuallyKnows()
    {
        foreach (var mode in BswupScriptCatalog.Modes)
        {
            foreach (var setting in mode.Settings.Keys)
            {
                Assert.IsTrue(BswupScriptCatalog.IsKnownSetting(setting),
                    $"the '{mode.Name}' preset was parsed as filling in '{setting}', which is not a setting");
            }
        }
    }

    // -- Lifecycle messages ----------------------------------------------------

    [TestMethod]
    public void Events_CoverTheMessagesAHandlerHasToSwitchOn()
    {
        var names = BswupScriptCatalog.Events.Select(message => message.Name).ToArray();

        foreach (var expected in new[]
        {
            "downloadStarted", "downloadProgress", "downloadFinished", "updateReady", "updateFound",
            "updateNotFound", "updateCheckFailed", "stateChanged", "activate", "error",
        })
        {
            CollectionAssert.Contains(names, expected);
        }
    }

    [TestMethod]
    public void Events_ResolveToTheStringTheShippedScriptDeclares()
    {
        var events = BswupScriptCatalog.Events.ToDictionary(message => message.Name, StringComparer.Ordinal);

        Assert.AreEqual("DOWNLOAD_PROGRESS", events["downloadProgress"].Message);
        Assert.AreEqual("UPDATE_READY", events["updateReady"].Message);
        Assert.AreEqual("ERROR", events["error"].Message);
    }

    [TestMethod]
    public void Events_AreAllDeclaredByTheShippedScript()
    {
        var undeclared = BswupScriptCatalog.Events
            .Where(message => message.VerifiedFromSource is false)
            .Select(message => message.Name)
            .ToArray();

        Assert.AreEqual(0, undeclared.Length,
            $"documented but no longer declared - a handler matching on these gets undefined: {string.Join(", ", undeclared)}");
    }

    [TestMethod]
    public void Events_EachCarryASummaryAndAUniqueMessage()
    {
        foreach (var message in BswupScriptCatalog.Events)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(message.Summary), message.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(message.Message), message.Name);
        }

        var duplicates = BswupScriptCatalog.Events
            .Where(message => message.Deprecated is null)
            .GroupBy(message => message.Message, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.AreEqual(0, duplicates.Length, $"two live messages resolve to the same string: {string.Join(", ", duplicates)}");
    }

    // -- The JavaScript API ----------------------------------------------------

    [TestMethod]
    public void JsApi_CoversTheGlobalsThePageScriptInstalls()
    {
        var names = BswupScriptCatalog.JsApi.Select(member => member.Name).ToArray();

        CollectionAssert.IsSubsetOf(
            new[] { "checkForUpdate", "persistStorage", "skipWaiting", "forceRefresh", "version" },
            names);
    }

    [TestMethod]
    public void JsApi_MembersAreAllStillInstalledByTheShippedScript()
    {
        var missing = BswupScriptCatalog.JsApi
            .Where(member => member.VerifiedFromSource is false)
            .Select(member => member.Name)
            .ToArray();

        Assert.AreEqual(0, missing.Length,
            $"documented but no longer on the global BitBswup object: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void JsApi_MembersCarryASignatureAndASummary()
    {
        foreach (var member in BswupScriptCatalog.JsApi)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(member.Signature), member.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(member.Summary), member.Name);
        }
    }

    // -- The built-in asset lists ----------------------------------------------

    [TestMethod]
    public void DefaultAssetsInclude_IsReadOffTheShippedWorker()
    {
        var include = BswupScriptCatalog.DefaultAssetsInclude;

        Assert.IsTrue(include.Length > 10, "the built-in include list failed to parse");
        CollectionAssert.Contains(include, @"/\.dll$/");
        CollectionAssert.Contains(include, @"/\.wasm(\.br|\.gz)?$/",
            "the README documents this one as /\\.wasm/, which is why it is read off the source instead");
    }

    [TestMethod]
    public void DefaultAssetsExclude_KeepsTheWorkerScriptsOutOfTheCache()
    {
        var exclude = BswupScriptCatalog.DefaultAssetsExclude;

        CollectionAssert.Contains(exclude, @"/^service-worker\.js$/");
        Assert.IsTrue(exclude.Any(pattern => pattern.Contains("bit-bswup\\.sw\\.js", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void DefaultAssetPatterns_AreAllUsableRegularExpressions()
    {
        foreach (var pattern in BswupScriptCatalog.DefaultAssetsInclude.Concat(BswupScriptCatalog.DefaultAssetsExclude))
        {
            Assert.IsTrue(pattern.StartsWith('/'), $"'{pattern}' was not parsed as a regex literal");

            var end = pattern.LastIndexOf('/');

            // Checked rather than sliced blind: a literal read without its closing delimiter would
            // otherwise come back as an index-out-of-range instead of as what it is.
            Assert.IsTrue(end > 0, $"'{pattern}' has no closing delimiter");

            var body = pattern[1..end];

            // A pattern that will not compile here is one the analysis silently drops.
            _ = new Regex(body);
        }
    }
}
