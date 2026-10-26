namespace Bit.BevTools.Tests;

/// <summary>
/// A session export as a CI gate: the findings in it (<see cref="BevSessionCheck"/>), and the <c>bevtools</c> tool that
/// runs that check and <see cref="BevProfileComparison"/> over files.
/// </summary>
[TestClass]
public class SessionCheckTests
{
    private static string Export(params string[] findings) =>
        $$"""{"format":"bit-bevtools/1","exportedAt":"2026-10-23T08:00:00Z","session":{"platform":"Server","runtime":".NET 10.0.0"},"diagnostics":[{{string.Join(",", findings)}}],"errors":[],"errorGroups":[],"http":[]}""";

    private static string Finding(string rule, string severity, bool dismissed = false) =>
        $$"""{"ruleId":"{{rule}}","severity":"{{severity}}","title":"{{rule}} | title","evidence":"what was seen","dismissed":{{(dismissed ? "true" : "false")}}}""";

    [TestMethod]
    public void An_export_carries_the_findings_of_what_it_holds_even_when_the_rules_have_not_run_since()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.Diagnostics.Evaluate(force: true);
            foreach (var i in Enumerable.Range(0, 6))
            {
                session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Navigation, Category = "navigation", Title = i % 2 == 0 ? "Navigate to /login" : "Navigate to /account", Detail = "programmatic" });
            }

            // Within the second the rules are throttled to, and nothing shows their results: only the export runs them.
            var result = BevSessionCheck.Check(BevSessionExporter.ToJson(session));

            Assert.IsTrue(result.Failed);
            var loop = Assert.ContainsSingle(f => f.RuleId == "navigation.loop", result.Findings);
            Assert.AreEqual(BevDiagnosticSeverity.Error, loop.Severity);
            Assert.IsTrue(loop.Fails);
            StringAssert.Contains(result.ToMarkdown(), "**Failed:** 1 failing finding");
        }
    }

    [TestMethod]
    public void Severity_ignored_rules_and_dismissed_findings_decide_what_fails()
    {
        var json = Export(Finding("render.storm", "Warning"), Finding("http.per-item", "Warning"), Finding("component.hint", "Info"), Finding("navigation.loop", "Error", dismissed: true));

        var byDefault = BevSessionCheck.Check(json);
        Assert.IsTrue(byDefault.Failed);
        Assert.AreEqual(3, byDefault.Findings.Count);
        Assert.AreEqual(1, byDefault.DismissedFindings);
        CollectionAssert.AreEquivalent(new[] { "render.storm", "http.per-item" }, byDefault.Findings.Where(f => f.Fails).Select(f => f.RuleId).ToArray());

        var errorsOnly = BevSessionCheck.Check(json, new BevSessionCheckOptions { FailOn = BevDiagnosticSeverity.Error });
        Assert.IsFalse(errorsOnly.Failed, "the only error was dismissed");

        var ignoring = new BevSessionCheckOptions();
        ignoring.IgnoredRules.Add("render.storm");
        ignoring.IgnoredRules.Add("http.per-item");
        Assert.IsFalse(BevSessionCheck.Check(json, ignoring).Failed);

        var listOnly = BevSessionCheck.Check(json, new BevSessionCheckOptions { FailOn = null });
        Assert.IsFalse(listOnly.Failed);
        Assert.AreEqual(3, listOnly.Findings.Count);
        StringAssert.Contains(listOnly.ToMarkdown(), "render.storm \\| title", "a pipe in a title stays inside its cell");
    }

    [TestMethod]
    public void Recorded_errors_fail_the_check_only_when_asked()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.Errors.Record(new InvalidOperationException("Order 7001 was not found"), "blazor");
            session.Errors.Record(new InvalidOperationException("Order 9002 was not found"), "blazor");
            var json = BevSessionExporter.ToJson(session);

            var result = BevSessionCheck.Check(json);
            Assert.AreEqual(2, result.Errors);
            Assert.AreEqual(1, result.ErrorGroups);
            Assert.IsFalse(result.FailedOnErrors);

            var strict = BevSessionCheck.Check(json, new BevSessionCheckOptions { FailOnErrors = true });
            Assert.IsTrue(strict.Failed);
            StringAssert.Contains(strict.ToMarkdown(), "recorded errors");
        }
    }

    [TestMethod]
    public void A_file_that_cannot_be_checked_is_refused_rather_than_passed()
    {
        Assert.ThrowsExactly<FormatException>(() => BevSessionCheck.Check("not json"));
        Assert.ThrowsExactly<FormatException>(() => BevSessionCheck.Check("""{"format":"something-else"}"""));
        var withoutDiagnostics = Assert.ThrowsExactly<FormatException>(() => BevSessionCheck.Check("""{"format":"bit-bevtools/1","components":{}}"""));
        StringAssert.Contains(withoutDiagnostics.Message, "no diagnostics section");
        Assert.IsTrue(BevSessionCheck.Check(Export(Finding("x.new", "Critical"))).Failed, "a severity this version does not know is looked at, not passed");
        Assert.IsTrue(BevSessionCheck.Check(Export(Finding("x.minus", "-1"))).Failed, "a number is not a severity");
        Assert.IsTrue(BevSessionCheck.Check(Export(Finding("x.flags", "Warning, Error"))).Failed, "nor is a combination");

        var withoutErrors = """{"format":"bit-bevtools/1","diagnostics":[]}""";
        Assert.ThrowsExactly<FormatException>(() => BevSessionCheck.Check(withoutErrors, new BevSessionCheckOptions { FailOnErrors = true }), "errors cannot fail a check that has none to read");
        var partial = BevSessionCheck.Check(withoutErrors);
        Assert.IsNull(partial.Errors);
        Assert.IsNull(partial.Requests);
        StringAssert.Contains(partial.ToMarkdown(), "errors not in the export · HTTP requests not in the export");
    }

    [TestMethod]
    public void Error_counts_add_up_past_what_an_int_holds_and_caveats_say_what_limits_the_export()
    {
        var json = """
            {"format":"bit-bevtools/1","session":{"capturing":false},"options":{"diagnostics":{"disabledRules":["render.storm"],"evaluateWhileClosed":false}},
             "diagnostics":[],"errors":[{"count":2000000000},{"count":2000000000}]}
            """;

        var result = BevSessionCheck.Check(json, new BevSessionCheckOptions { FailOnErrors = true });

        Assert.AreEqual(4_000_000_000L, result.Errors);
        Assert.IsTrue(result.Failed);
        Assert.HasCount(3, result.Caveats);
        var markdown = result.ToMarkdown();
        StringAssert.Contains(markdown, "Capture was paused");
        StringAssert.Contains(markdown, "Rules disabled in this session, so they reported nothing: render.storm.");
        StringAssert.Contains(markdown, "Diagnostics.EvaluateWhileClosed was not on");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => result.ToMarkdown(-1));

        var (session, scope) = TestHelpers.CreateSession(o => o.Diagnostics.EvaluateWhileClosed = true);
        using (scope)
        {
            Assert.IsEmpty(BevSessionCheck.Check(BevSessionExporter.ToJson(session)).Caveats, "a session that ran its rules throughout, capturing, with every rule on");
        }
    }

    [TestMethod]
    public void The_tool_exits_with_what_the_check_decided_and_writes_the_report()
    {
        var folder = Directory.CreateTempSubdirectory("bevtools-cli-");
        try
        {
            var export = Path.Combine(folder.FullName, "session.json");
            File.WriteAllText(export, Export(Finding("render.storm", "Warning")));
            var report = Path.Combine(folder.FullName, "summary.md");

            var (code, output, _) = Run("check", export, "--report", report);
            Assert.AreEqual(BevToolsCli.Failed, code);
            StringAssert.Contains(output, "render.storm");
            Assert.AreEqual(output, File.ReadAllText(report));

            Assert.AreEqual(BevToolsCli.Passed, Run("check", export, "--fail-on", "error").Code);
            Assert.AreEqual(BevToolsCli.Passed, Run("check", export, "--ignore", "render.storm").Code);
            Assert.AreEqual(BevToolsCli.Passed, Run("check", export, "--fail-on", "none").Code);

            var (badLevel, _, levelError) = Run("check", export, "--fail-on", "1");
            Assert.AreEqual(BevToolsCli.Unusable, badLevel);
            StringAssert.Contains(levelError, "--fail-on takes info, warning, error or none");
            Assert.AreEqual(BevToolsCli.Unusable, Run("check", export, "--fail-on", "warning,error").Code, "a combination would be a level above every finding");
            Assert.AreEqual(BevToolsCli.Passed, Run("check", export, "--fail-on", "ERROR").Code);

            var (emptyReport, emptyOutput, emptyError) = Run("check", export, "--report", "");
            Assert.AreEqual(BevToolsCli.Unusable, emptyReport, "an unset $GITHUB_STEP_SUMMARY");
            Assert.AreEqual("", emptyOutput, "said before anything is checked");
            StringAssert.Contains(emptyError, "--report needs a file path");
            var (swallowed, _, swallowedError) = Run("check", export, "--report", "--fail-on-errors");
            Assert.AreEqual(BevToolsCli.Unusable, swallowed, "an option is never another option's value");
            StringAssert.Contains(swallowedError, "--report needs a value");
            Assert.IsFalse(File.Exists("--fail-on-errors"));
            Assert.AreEqual(BevToolsCli.Passed, Run("check", export, "--help").Code);
            Assert.AreEqual(BevToolsCli.Unusable, Run("check", export, "--frobnicate", "x").Code);
            Assert.AreEqual(BevToolsCli.Unusable, Run("check", Path.Combine(folder.FullName, "missing.json")).Code);
            Assert.AreEqual(BevToolsCli.Unusable, Run("lint", export).Code);
            Assert.AreEqual(BevToolsCli.Unusable, Run().Code);
            Assert.AreEqual(BevToolsCli.Passed, Run("--help").Code);

            var notAnExport = Path.Combine(folder.FullName, "other.json");
            File.WriteAllText(notAnExport, "{}");
            var (unusable, _, formatError) = Run("check", notAnExport);
            Assert.AreEqual(BevToolsCli.Unusable, unusable);
            StringAssert.Contains(formatError, "Not a BevTools session export");
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void The_tool_fails_a_comparison_with_a_regression_and_honours_its_thresholds()
    {
        static string Profile(int renders, double ms) =>
            $$$"""{"format":"bit-bevtools/1","exportedAt":"2026-10-23T08:00:00Z","profile":{"sinceBaseline":true,"types":[{"type":"Shop.ProductRow","displayName":"ProductRow","instances":10,"renders":{{{renders}}},"renderMs":{{{ms.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},"maxRenderMs":1}]}}""";

        var folder = Directory.CreateTempSubdirectory("bevtools-cli-");
        try
        {
            var baseline = Path.Combine(folder.FullName, "baseline.json");
            var current = Path.Combine(folder.FullName, "current.json");
            File.WriteAllText(baseline, Profile(100, 10));
            File.WriteAllText(current, Profile(108, 10.5));

            var (code, output, _) = Run("compare", baseline, current);
            Assert.AreEqual(BevToolsCli.Failed, code);
            StringAssert.Contains(output, "ProductRow");
            StringAssert.Contains(output, "renders +8");

            Assert.AreEqual(BevToolsCli.Passed, Run("compare", baseline, current, "--render-change-ratio", "0.1").Code, "8 % is within a 10 % tolerance");
            Assert.AreEqual(BevToolsCli.Passed, Run("compare", baseline, baseline).Code);
            Assert.AreEqual(BevToolsCli.Unusable, Run("compare", baseline, current, "--min-render-change", "1.5").Code);
            Assert.AreEqual(BevToolsCli.Unusable, Run("compare", baseline, current, "--min-render-change", "1e10").Code, "more than an int holds");
            Assert.AreEqual(BevToolsCli.Unusable, Run("compare", baseline, current, "--time-change-ratio", "Infinity").Code);
            Assert.AreEqual(BevToolsCli.Unusable, Run("compare", baseline).Code);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    private static (int Code, string Output, string Error) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = BevToolsCli.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }
}
