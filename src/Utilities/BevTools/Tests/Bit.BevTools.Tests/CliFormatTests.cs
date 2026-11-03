using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// <c>bevtools check --format json|sarif|github</c>, <c>--output</c> and <c>bevtools compare --format json</c>, over real
/// exports whose findings come through a real rule: every format says what the check decided, and exits as the
/// Markdown does.
/// </summary>
[TestClass]
public class CliFormatTests : BunitContext
{
    public CliFormatTests() => Services.AddBitBevToolsServices(o =>
    {
        o.Enabled = true;
        o.Extensions.Add(new CliPlantedFindings());
    });

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void The_json_format_is_the_check_finding_by_finding_and_exits_as_the_markdown_does()
    {
        var folder = Directory.CreateTempSubdirectory("bevtools-cli-");
        try
        {
            var export = WriteExport(folder.FullName);
            var parent = Session.Components.All.Single(c => c.DisplayName == nameof(ParentComponent)).InstanceId;
            var expected = BevSessionCheck.Check(File.ReadAllText(export));

            var (code, output, _) = Run("check", export, "--format", "json");

            Assert.AreEqual(BevToolsCli.Failed, code);
            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;
            Assert.IsFalse(root.GetProperty("passed").GetBoolean());
            Assert.AreEqual("Warning", root.GetProperty("failOn").GetString());
            Assert.AreEqual(export, root.GetProperty("file").GetString());
            Assert.AreEqual(expected.Findings.Count(f => f.Fails), root.GetProperty("failingFindings").GetInt32());
            Assert.AreEqual(1, root.GetProperty("dismissedFindings").GetInt32());
            Assert.AreEqual(expected.Errors, root.GetProperty("errors").GetInt64());
            CollectionAssert.AreEqual(expected.Caveats.ToArray(), root.GetProperty("caveats").EnumerateArray().Select(c => c.GetString()).ToArray());

            var findings = root.GetProperty("findings").EnumerateArray().ToList();
            var checkedFindings = expected.Findings.Concat(expected.Dismissed).ToList();
            Assert.HasCount(checkedFindings.Count, findings);
            foreach (var (f, json) in checkedFindings.Zip(findings))
            {
                Assert.AreEqual(f.RuleId, json.GetProperty("ruleId").GetString());
                Assert.AreEqual(f.Severity.ToString(), json.GetProperty("severity").GetString());
                Assert.AreEqual(f.Title, json.GetProperty("title").GetString());
                Assert.AreEqual(f.Evidence, json.GetProperty("evidence").GetString());
                Assert.AreEqual(f.Fingerprint, json.GetProperty("fingerprint").GetString());
                Assert.AreEqual(f.Fails, json.GetProperty("fails").GetBoolean());
                Assert.AreEqual(f.Dismissed, json.GetProperty("dismissed").GetBoolean());
            }

            var error = Assert.ContainsSingle(f => f.GetProperty("ruleId").GetString() == "cli.error", findings);
            Assert.AreEqual(parent, error.GetProperty("componentInstanceId").GetInt64(), "the component the rule named, as the export recorded it");
            Assert.AreEqual("Check the discount.", error.GetProperty("suggestion").GetString());
            Assert.AreEqual("cli.error", error.GetProperty("fingerprint").GetString());
            var dismissed = Assert.ContainsSingle(f => f.GetProperty("ruleId").GetString() == "cli.dismissed", findings);
            Assert.IsTrue(dismissed.GetProperty("dismissed").GetBoolean());
            Assert.IsFalse(dismissed.GetProperty("fails").GetBoolean(), "a dismissed error never fails");
            Assert.IsFalse(Assert.ContainsSingle(f => f.GetProperty("ruleId").GetString() == "cli.hint", findings).TryGetProperty("componentInstanceId", out _));

            var (markdownCode, markdown, _) = Run("check", export);
            Assert.AreEqual(BevToolsCli.Failed, markdownCode);
            Assert.AreEqual(expected.ToMarkdown(), markdown, "the default output is unchanged");

            var (listCode, listOutput, _) = Run("check", export, "--format", "JSON", "--fail-on", "none");
            Assert.AreEqual(BevToolsCli.Passed, listCode);
            using var list = JsonDocument.Parse(listOutput);
            Assert.IsTrue(list.RootElement.GetProperty("passed").GetBoolean());
            Assert.AreEqual(JsonValueKind.Null, list.RootElement.GetProperty("failOn").ValueKind);
            Assert.AreEqual(BevToolsCli.Passed, Run("check", export, "--format", "json", "--ignore", "cli.error", "--ignore", "cli.hostile").Code);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void The_sarif_format_is_a_sarif_2_1_0_log_without_invented_locations()
    {
        var folder = Directory.CreateTempSubdirectory("bevtools-cli-");
        try
        {
            var export = WriteExport(folder.FullName);
            var expected = BevSessionCheck.Check(File.ReadAllText(export));

            var (code, output, _) = Run("check", export, "--format", "sarif");

            Assert.AreEqual(BevToolsCli.Failed, code);
            Assert.DoesNotContain("\"locations\"", output, StringComparison.Ordinal);
            Assert.DoesNotContain("physicalLocation", output, StringComparison.Ordinal);
            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;
            Assert.AreEqual("https://json.schemastore.org/sarif-2.1.0.json", root.GetProperty("$schema").GetString());
            Assert.AreEqual("2.1.0", root.GetProperty("version").GetString());
            var run = Assert.ContainsSingle(root.GetProperty("runs").EnumerateArray());
            var driver = run.GetProperty("tool").GetProperty("driver");
            Assert.AreEqual("bevtools", driver.GetProperty("name").GetString());
            Assert.AreEqual("https://github.com/bitfoundation/bitplatform", driver.GetProperty("informationUri").GetString());
            var rules = driver.GetProperty("rules").EnumerateArray().Select(r => r.GetProperty("id").GetString()!).ToList();
            CollectionAssert.AreEquivalent(expected.Findings.Concat(expected.Dismissed).Select(f => f.RuleId).Distinct().ToArray(), rules.ToArray());
            Assert.AreEqual(rules.Count, rules.Distinct().Count(), "one rule per id");

            var invocation = Assert.ContainsSingle(run.GetProperty("invocations").EnumerateArray());
            Assert.IsTrue(invocation.GetProperty("executionSuccessful").GetBoolean());
            Assert.HasCount(expected.Caveats.Count, invocation.GetProperty("toolExecutionNotifications").EnumerateArray());
            Assert.IsFalse(run.GetProperty("properties").GetProperty("passed").GetBoolean());

            var results = run.GetProperty("results").EnumerateArray().ToList();
            Assert.HasCount(expected.Findings.Count + expected.Dismissed.Count, results);
            foreach (var result in results)
            {
                var ruleId = result.GetProperty("ruleId").GetString();
                Assert.AreEqual(ruleId, rules[result.GetProperty("ruleIndex").GetInt32()]);
                Assert.IsFalse(result.TryGetProperty("locations", out _));
                var finding = expected.Findings.Concat(expected.Dismissed).Single(f => f.RuleId == ruleId);
                Assert.AreEqual(finding.Fingerprint, result.GetProperty("partialFingerprints").GetProperty(BevCliFormats.FingerprintKey).GetString());
                StringAssert.StartsWith(result.GetProperty("message").GetProperty("text").GetString(), finding.Title);
                StringAssert.Contains(result.GetProperty("message").GetProperty("text").GetString(), finding.Evidence);
                Assert.AreEqual(finding.Dismissed, result.TryGetProperty("suppressions", out var suppressions));
                if (finding.Dismissed)
                {
                    Assert.AreEqual("external", Assert.ContainsSingle(suppressions.EnumerateArray()).GetProperty("kind").GetString());
                }
            }

            string? Level(string rule) => results.Single(r => r.GetProperty("ruleId").GetString() == rule).GetProperty("level").GetString();
            Assert.AreEqual("error", Level("cli.error"));
            Assert.AreEqual("warning", Level("cli.hostile"));
            Assert.AreEqual("note", Level("cli.hint"));
            Assert.AreEqual(BevToolsCli.Passed, Run("check", export, "--format", "sarif", "--fail-on", "none").Code, "the format never changes the exit code");
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void The_github_format_escapes_what_an_export_says_so_no_text_can_start_a_command()
    {
        Assert.AreEqual("100%25 done%0D%0Anext", BevCliFormats.EscapeData("100% done\r\nnext"));
        Assert.AreEqual("%250A", BevCliFormats.EscapeData("%0A"), "% first, so an escape already in the text is not decoded by the runner");
        Assert.AreEqual("a%3Ab%2Cc%25%0A", BevCliFormats.EscapeProperty("a:b,c%\n"));
        Assert.AreEqual("x::y", BevCliFormats.EscapeData("x::y"), "the message is everything after the first ::, which an escaped property cannot hold");

        var folder = Directory.CreateTempSubdirectory("bevtools-cli-");
        try
        {
            var export = WriteExport(folder.FullName);
            var report = Path.Combine(folder.FullName, "summary.md");

            var (code, output, _) = Run("check", export, "--format", "github", "--report", report);

            Assert.AreEqual(BevToolsCli.Failed, code);
            Assert.AreEqual(BevSessionCheck.Check(File.ReadAllText(export)).ToMarkdown(), File.ReadAllText(report), "--report still gets the Markdown");
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines[..^1])
            {
                Assert.IsTrue(line.StartsWith("::error title=", StringComparison.Ordinal) || line.StartsWith("::warning title=", StringComparison.Ordinal) || line.StartsWith("::notice title=", StringComparison.Ordinal), line);
            }

            StringAssert.StartsWith(lines[^1], "BevTools check failed: ");
            var hostile = Assert.ContainsSingle(l => l.Contains("cli.hostile", StringComparison.Ordinal), lines);
            Assert.AreEqual("::warning title=BevTools cli.hostile%3A Hostile%3A a%3Ab%2C c::line one 50%25 done%0D%0Aline two%0A::error title=pwned::injected %250A", hostile);
            Assert.DoesNotContain(l => l.StartsWith("::error title=pwned", StringComparison.Ordinal), lines);
            StringAssert.Contains(Assert.ContainsSingle(l => l.StartsWith("::error title=BevTools cli.error%3A", StringComparison.Ordinal), lines), "::Total -5 for order 7%0ASuggestion: Check the discount.");
            StringAssert.EndsWith(Assert.ContainsSingle(l => l.StartsWith("::notice title=BevTools cli.hint%3A", StringComparison.Ordinal), lines), "%0AListed only: it does not fail this check.");
            Assert.DoesNotContain(l => l.Contains("cli.dismissed", StringComparison.Ordinal), lines, "a dismissed finding is no annotation");
            Assert.Contains(l => l.StartsWith("::notice title=BevTools check caveat::", StringComparison.Ordinal), lines);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void Output_writes_the_format_to_a_file_and_bad_formats_are_refused()
    {
        var folder = Directory.CreateTempSubdirectory("bevtools-cli-");
        try
        {
            var export = WriteExport(folder.FullName);
            var sarif = Path.Combine(folder.FullName, "bevtools.sarif");

            var (code, output, _) = Run("check", export, "--format", "sarif", "--output", sarif);
            Assert.AreEqual(BevToolsCli.Failed, code);
            Assert.AreEqual("", output, "the result went to the file");
            Assert.AreEqual(BevToolsCli.Failed, Run("check", export, "--format", "sarif", "--output", sarif).Code);
            using (var document = JsonDocument.Parse(File.ReadAllText(sarif)))
            {
                Assert.AreEqual("2.1.0", document.RootElement.GetProperty("version").GetString(), "replaced, not appended: still one document");
            }

            var markdownFile = Path.Combine(folder.FullName, "check.md");
            var (markdownCode, markdownOutput, _) = Run("check", export, "--output", markdownFile);
            Assert.AreEqual(BevToolsCli.Failed, markdownCode);
            Assert.AreEqual("", markdownOutput);
            Assert.AreEqual(BevSessionCheck.Check(File.ReadAllText(export)).ToMarkdown(), File.ReadAllText(markdownFile));

            var (emptyCode, emptyOutput, emptyError) = Run("check", export, "--output", "");
            Assert.AreEqual(BevToolsCli.Unusable, emptyCode);
            Assert.AreEqual("", emptyOutput);
            StringAssert.Contains(emptyError, "--output needs a file path");
            var (badCode, _, badError) = Run("check", export, "--format", "xml");
            Assert.AreEqual(BevToolsCli.Unusable, badCode);
            StringAssert.Contains(badError, "--format takes markdown, json, sarif or github, not 'xml'");
            var (compareCode, _, compareError) = Run("compare", export, export, "--format", "sarif");
            Assert.AreEqual(BevToolsCli.Unusable, compareCode, "a comparison has no SARIF form");
            StringAssert.Contains(compareError, "--format takes markdown or json");
            Assert.AreEqual(BevToolsCli.Unusable, Run("check", export, "--format").Code);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task The_json_comparison_carries_every_change_and_exits_as_the_markdown_does()
    {
        var folder = Directory.CreateTempSubdirectory("bevtools-cli-");
        try
        {
            var cut = Render<ParentComponent>(ps => ps.Add(p => p.Value, 1));
            var baseline = Path.Combine(folder.FullName, "baseline.json");
            File.WriteAllText(baseline, BevSessionExporter.ToJson(Session));
            await cut.InvokeAsync(() => cut.Instance.Increment());
            await cut.InvokeAsync(() => cut.Instance.Increment());
            var current = Path.Combine(folder.FullName, "current.json");
            File.WriteAllText(current, BevSessionExporter.ToJson(Session));
            var expected = BevProfileComparison.Compare(File.ReadAllText(baseline), File.ReadAllText(current));

            var (code, output, _) = Run("compare", baseline, current, "--format", "json");

            Assert.AreEqual(BevToolsCli.Failed, code);
            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;
            Assert.IsFalse(root.GetProperty("passed").GetBoolean());
            Assert.AreEqual(expected.Regressions.Count(), root.GetProperty("regressions").GetInt32());
            Assert.AreEqual(expected.CurrentRenders, root.GetProperty("currentRenders").GetInt32());
            Assert.AreEqual(1, root.GetProperty("options").GetProperty("minRenderChange").GetInt32());
            var changes = root.GetProperty("changes").EnumerateArray().ToList();
            Assert.HasCount(expected.Changes.Count, changes);
            var parent = Assert.ContainsSingle(c => c.GetProperty("displayName").GetString() == nameof(ParentComponent), changes);
            Assert.AreEqual("Regressed", parent.GetProperty("kind").GetString());
            Assert.AreEqual(parent.GetProperty("baselineRenders").GetInt32() + 2, parent.GetProperty("currentRenders").GetInt32());
            StringAssert.StartsWith(parent.GetProperty("reason").GetString(), "renders +2");
            Assert.IsTrue(parent.TryGetProperty("currentP95RenderMs", out _));

            var (sameCode, sameOutput, _) = Run("compare", baseline, baseline, "--format", "json");
            Assert.AreEqual(BevToolsCli.Passed, sameCode);
            using var same = JsonDocument.Parse(sameOutput);
            Assert.IsTrue(same.RootElement.GetProperty("passed").GetBoolean());
            Assert.AreEqual(expected.ToMarkdown(), Run("compare", baseline, current).Output, "the default output is unchanged");
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void Check_and_compare_never_load_the_MCP_SDK()
    {
        var folder = Directory.CreateTempSubdirectory("bevtools-cli-");
        var bin = Path.GetDirectoryName(typeof(BevToolsCli).Assembly.Location)!;
        // A context of its own: whatever this test process loaded for other tests, what the tool loads shows here.
        var context = new IsolatedTool(bin);
        try
        {
            var export = WriteExport(folder.FullName);
            var tool = context.LoadFromAssemblyPath(Path.Combine(bin, "bevtools.dll"));
            var run = tool.GetType("Bit.BevTools.BevToolsCli")!.GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Static, [typeof(string[]), typeof(TextWriter), typeof(TextWriter)])!;
            int RunIsolated(params string[] args) => (int)run.Invoke(null, [args, TextWriter.Null, TextWriter.Null])!;

            Assert.AreEqual(BevToolsCli.Failed, RunIsolated("check", export, "--format", "sarif"));
            Assert.AreEqual(BevToolsCli.Passed, RunIsolated("compare", export, export, "--format", "json"));
            Assert.AreEqual(BevToolsCli.Unusable, RunIsolated("mcp", Path.Combine(folder.FullName, "missing.json")));
            Assert.DoesNotContain(a => a.GetName().Name!.StartsWith("ModelContextProtocol", StringComparison.Ordinal), context.Assemblies);

            // The control: reading the tools' attributes loads it, so the assertion above can fail.
            _ = tool.GetType("Bit.BevTools.BevMcpTools")!.GetMethod("ListSessions")!.GetCustomAttributes(inherit: false);
            Assert.Contains(a => a.GetName().Name!.StartsWith("ModelContextProtocol", StringComparison.Ordinal), context.Assemblies);
        }
        finally
        {
            context.Unload();
            folder.Delete(recursive: true);
        }
    }

    /// <summary>Renders a component the planted error names, records an error, runs the rules, dismisses one finding, exports.</summary>
    private string WriteExport(string folder)
    {
        Render<ParentComponent>(ps => ps.Add(p => p.Value, 1));
        Session.Errors.Record(new InvalidOperationException("Order 7001 was not found"), "blazor");
        Session.Diagnostics.Evaluate(force: true);
        Session.Diagnostics.Dismiss("cli.dismissed");
        var path = Path.Combine(folder, "session.json");
        File.WriteAllText(path, BevSessionExporter.ToJson(Session));
        return path;
    }

    private static (int Code, string Output, string Error) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = BevToolsCli.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }

    private sealed class IsolatedTool(string bin) : AssemblyLoadContext("bevtools-isolated", isCollectible: true)
    {
        // The tool itself and the MCP SDK load here; everything else is shared with the test process.
        protected override Assembly? Load(AssemblyName assemblyName) =>
            assemblyName.Name is { } name && name.StartsWith("ModelContextProtocol", StringComparison.Ordinal) && File.Exists(Path.Combine(bin, name + ".dll"))
                ? LoadFromAssemblyPath(Path.Combine(bin, name + ".dll"))
                : null;
    }
}

/// <summary>
/// A rule that reports findings with known text - one naming a component, one with text hostile to every format, a
/// hint, one to dismiss - so the tool's formats are tested on what a real rule puts in a real export.
/// </summary>
internal sealed class CliPlantedFindings : IBevToolsExtension
{
    internal const string Hostile = "line one 50% done\r\nline two\n::error title=pwned::injected %0A";

    public string Name => "CLI planted findings";

    public void Configure(IBevToolsExtensionBuilder builder) => builder.AddDiagnosticRule(new Rule());

    private sealed class Rule : IBevDiagnosticRule
    {
        public string Id => "cli.planted";

        public string Title => "Planted findings";

        public string Description => "Findings with known text, for the tool's output formats.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var parent = context.Components.FirstOrDefault(c => c.DisplayName == nameof(ParentComponent))?.InstanceId;
            yield return new BevDiagnostic("cli.error", BevDiagnosticSeverity.Error, "Order total is negative", "Total -5 for order 7", "Check the discount.", Fingerprint: "cli.error", ComponentInstanceId: parent);
            yield return new BevDiagnostic("cli.hostile", BevDiagnosticSeverity.Warning, "Hostile: a:b, c", Hostile, Fingerprint: "cli.hostile");
            yield return new BevDiagnostic("cli.hint", BevDiagnosticSeverity.Info, "A hint", "Seen once.", Fingerprint: "cli.hint");
            yield return new BevDiagnostic("cli.dismissed", BevDiagnosticSeverity.Error, "Dismissed problem", "Someone looked at it.", Fingerprint: "cli.dismissed");
        }
    }
}
