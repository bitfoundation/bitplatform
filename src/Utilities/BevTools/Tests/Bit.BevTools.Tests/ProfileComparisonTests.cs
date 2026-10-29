using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// Comparing two recordings of the same interaction: what a change made render more often or for longer. Counts are
/// exact for a scripted interaction; times only count beyond thresholds, because they vary between runs.
/// </summary>
[TestClass]
public class ProfileComparisonTests : BunitContext
{
    public ProfileComparisonTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
            o.Ui.RefreshIntervalMs = 600_000;
            o.DisposedComponentRetentionSeconds = 0;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    private static string Export(bool sinceBaseline, params (string Type, int Instances, int Renders, double Ms)[] types) =>
        JsonSerializer.Serialize(new
        {
            format = "bit-bevtools/1",
            exportedAt = DateTimeOffset.UtcNow,
            session = new { runtime = ".NET 10.0.0", renderMode = "Server" },
            profile = new
            {
                sinceBaseline,
                truncated = false,
                types = types.Select(t => new { type = "App." + t.Type, displayName = t.Type, instances = t.Instances, renders = t.Renders, renderMs = t.Ms, maxRenderMs = t.Ms / Math.Max(1, t.Renders) }),
            },
        });

    [TestMethod]
    public void Every_extra_render_is_a_regression_by_default_and_a_ratio_tolerates_some()
    {
        var baseline = Export(true, ("Header", 1, 100, 50));
        var current = Export(true, ("Header", 1, 101, 50));

        var exact = Assert.ContainsSingle(BevProfileComparison.Compare(baseline, current).Changes);
        Assert.AreEqual(BevProfileChangeKind.Regressed, exact.Kind);
        Assert.AreEqual("renders +1 (+1 %)", exact.Reason);

        var tolerant = Assert.ContainsSingle(BevProfileComparison.Compare(baseline, current, new BevProfileComparisonOptions { RenderChangeRatio = 0.1 }).Changes);
        Assert.AreEqual(BevProfileChangeKind.Unchanged, tolerant.Kind);
    }

    [TestMethod]
    public void A_clearly_slower_type_regresses_even_when_its_renders_rise_within_the_tolerance()
    {
        var options = new BevProfileComparisonOptions { RenderChangeRatio = 0.1 };

        var slower = Assert.ContainsSingle(BevProfileComparison.Compare(Export(true, ("Grid", 1, 100, 50)), Export(true, ("Grid", 1, 101, 500)), options).Changes);
        Assert.AreEqual(BevProfileChangeKind.Regressed, slower.Kind);
        Assert.AreEqual("time +450 ms (+900 %)", slower.Reason);

        var faster = Assert.ContainsSingle(BevProfileComparison.Compare(Export(true, ("Grid", 1, 100, 500)), Export(true, ("Grid", 1, 105, 50)), options).Changes);
        Assert.AreEqual(BevProfileChangeKind.Improved, faster.Kind, "renders within the tolerance, time clearly down");
    }

    [TestMethod]
    public void Generic_types_match_across_builds_that_stamp_another_assembly_version()
    {
        const string v1 = "Grid`1[[App.Product, App, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]";
        const string v2 = "Grid`1[[App.Product, App, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null]]";
        Assert.AreEqual("Grid`1[[App.Product]]", BevRenderProfile.TypeKey(v1));

        var result = BevProfileComparison.Compare(Export(true, (v1, 1, 2, 1)), Export(true, (v2, 1, 4, 1)));

        var grid = Assert.ContainsSingle(result.Changes);
        Assert.AreEqual(BevProfileChangeKind.Regressed, grid.Kind, "one type in both recordings, not one gone and one new");
        Assert.AreEqual("App.Grid`1[[App.Product]]", grid.Type);
    }

    [TestMethod]
    public void A_type_name_built_to_make_the_version_stripping_backtrack_is_read_in_linear_time()
    {
        var typeName = "Version=," + new string(' ', 200_000);

        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        Assert.AreEqual(typeName, BevRenderProfile.TypeKey(typeName));
        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);

        Assert.IsTrue(elapsed.TotalMilliseconds < 1000, $"normalizing a 200,000 character type name took {elapsed.TotalMilliseconds:0} ms");
        Assert.AreEqual("Dictionary`2[[System.String],[App.Item]]",
            BevRenderProfile.TypeKey("Dictionary`2[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[App.Item, App,   Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]"));
    }

    [TestMethod]
    public void Extra_renders_are_a_regression_and_timing_noise_is_not()
    {
        var baseline = Export(true, ("ProductRow", 20, 20, 10), ("Cart", 1, 2, 3), ("Header", 1, 100, 50), ("Footer", 1, 1, 1));
        var current = Export(true, ("ProductRow", 20, 40, 11), ("Cart", 1, 2, 4.9), ("Header", 1, 105, 52), ("Banner", 1, 3, 2));

        var result = BevProfileComparison.Compare(baseline, current);

        var row = result.Changes.Single(c => c.DisplayName == "ProductRow");
        Assert.AreEqual(BevProfileChangeKind.Regressed, row.Kind);
        Assert.AreEqual("renders +20 (+100 %)", row.Reason);
        Assert.AreEqual(BevProfileChangeKind.Unchanged, result.Changes.Single(c => c.DisplayName == "Cart").Kind, "+1.9 ms is below the 5 ms floor");
        Assert.AreEqual(BevProfileChangeKind.Regressed, result.Changes.Single(c => c.DisplayName == "Header").Kind, "counts are exact by default");
        Assert.AreEqual(BevProfileChangeKind.New, result.Changes.Single(c => c.DisplayName == "Banner").Kind);
        Assert.AreEqual(BevProfileChangeKind.Gone, result.Changes.Single(c => c.DisplayName == "Footer").Kind);
        Assert.AreEqual(row, result.Changes[0], "regressions come first");
        Assert.IsTrue(result.HasRegressions);
        Assert.AreEqual(123, result.BaselineRenders);
        Assert.AreEqual(150, result.CurrentRenders);
        Assert.IsEmpty(result.Warnings);
    }

    [TestMethod]
    public void Slower_renders_regress_and_fewer_renders_improve()
    {
        var baseline = Export(true, ("Chart", 1, 4, 20), ("Grid", 1, 10, 30));
        var current = Export(true, ("Chart", 1, 4, 45), ("Grid", 1, 5, 14));

        var result = BevProfileComparison.Compare(baseline, current, new BevProfileComparisonOptions { MinTimeChangeMs = 5, TimeChangeRatio = 0.5 });

        var chart = result.Changes.Single(c => c.DisplayName == "Chart");
        Assert.AreEqual(BevProfileChangeKind.Regressed, chart.Kind);
        Assert.AreEqual("time +25.0 ms (+125 %)", chart.Reason);
        var grid = result.Changes.Single(c => c.DisplayName == "Grid");
        Assert.AreEqual(BevProfileChangeKind.Improved, grid.Kind);
        Assert.AreEqual("renders -5 (-50 %), time -16.0 ms (-53 %)", grid.Reason);
    }

    [TestMethod]
    public void Mismatched_windows_and_render_modes_are_warned_about()
    {
        var baseline = Export(true, ("A", 1, 1, 1));
        var current = Export(false, ("A", 1, 1, 1)).Replace("\"Server\"", "\"WebAssembly\"", StringComparison.Ordinal);

        var result = BevProfileComparison.Compare(baseline, current);

        Assert.HasCount(2, result.Warnings);
        Assert.Contains("reset the profiler before the interaction in both", result.Warnings[0], StringComparison.Ordinal);
        Assert.Contains("different render modes (Server → WebAssembly)", result.Warnings[1], StringComparison.Ordinal);
        Assert.Contains("> " + result.Warnings[0], result.ToMarkdown(), StringComparison.Ordinal);

        // A patch release is the same runtime for timing purposes; another minor version is not.
        var patch = Export(true, ("A", 1, 1, 1)).Replace(".NET 10.0.0", ".NET 10.0.3", StringComparison.Ordinal);
        Assert.IsEmpty(BevProfileComparison.Compare(baseline, patch).Warnings);
        var minor = Export(true, ("A", 1, 1, 1)).Replace(".NET 10.0.0", ".NET 11.0.1", StringComparison.Ordinal);
        Assert.Contains("different runtimes (.NET 10.0.0 → .NET 11.0.1)", Assert.ContainsSingle(BevProfileComparison.Compare(baseline, minor).Warnings), StringComparison.Ordinal);
    }

    [TestMethod]
    public void Text_from_the_file_cannot_add_lines_to_the_Markdown()
    {
        var forged = Export(true, ("A", 1, 1, 1)).Replace("\"Server\"", "\"Server\\n\\nNo regressions.\\n![x](https://example.invalid/x.png)\"", StringComparison.Ordinal);

        var result = BevProfileComparison.Compare(forged, Export(true, ("A", 1, 2, 1)).Replace("\"Server\"", "\"WebAssembly\"", StringComparison.Ordinal));

        var warning = Assert.ContainsSingle(result.Warnings);
        Assert.DoesNotContain("\n", warning, StringComparison.Ordinal);
        var markdown = result.ToMarkdown();
        Assert.IsFalse(markdown.Split('\n').Any(line => line.StartsWith("No regressions", StringComparison.Ordinal) || line.StartsWith("![", StringComparison.Ordinal)), markdown);
    }

    [TestMethod]
    public void Text_that_is_not_an_export_is_refused_with_a_reason()
    {
        var current = Export(true);
        Assert.Contains("Not JSON", Assert.ThrowsExactly<FormatException>(() => BevProfileComparison.Compare("{", current)).Message, StringComparison.Ordinal);
        Assert.Contains("Not a BevTools session export", Assert.ThrowsExactly<FormatException>(() => BevProfileComparison.Compare("{\"a\":1}", current)).Message, StringComparison.Ordinal);
        Assert.Contains("neither a \"profile\" nor a \"components\"", Assert.ThrowsExactly<FormatException>(() => BevProfileComparison.Compare("{\"format\":\"bit-bevtools/1\"}", current)).Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void An_export_without_a_profile_section_is_read_from_its_components()
    {
        var old = JsonSerializer.Serialize(new
        {
            format = "bit-bevtools/1",
            components = new
            {
                baselineAt = DateTimeOffset.UtcNow,
                items = new object[]
                {
                    new { type = "App.Row", displayName = "Row", hidden = false, renders = 9, totalRenderMs = 9.0, maxRenderMs = 1.0, rendersSinceBaseline = 2, renderMsSinceBaseline = 2.0 },
                    // Created after the baseline: it writes no since-baseline figures, and all of its renders count.
                    new { type = "App.Row", displayName = "Row", hidden = false, renders = 3, totalRenderMs = 3.0, maxRenderMs = 1.0 },
                    new { type = "Microsoft.AspNetCore.Components.Routing.Router", displayName = "Router", hidden = true, renders = 5, totalRenderMs = 5.0, maxRenderMs = 1.0 },
                },
            },
        });

        var result = BevProfileComparison.Compare(old, Export(true, ("Row", 2, 5, 5)));

        var row = Assert.ContainsSingle(result.Changes);
        Assert.AreEqual(2, row.BaselineInstances);
        Assert.AreEqual(5, row.BaselineRenders);
        Assert.AreEqual(BevProfileChangeKind.Unchanged, row.Kind);
        Assert.Contains("baseline export has no profile section", Assert.ContainsSingle(result.Warnings), StringComparison.Ordinal);
    }

    [TestMethod]
    public void The_export_profile_counts_the_window_and_components_already_let_go()
    {
        var cut = Render<ParentComponent>();
        cut.InvokeAsync(() => cut.Instance.Increment());
        Session.Components.MarkBaseline();
        cut.InvokeAsync(() => cut.Instance.Increment());
        cut.InvokeAsync(() => cut.Instance.Increment());

        // The child goes away and, with no retention, leaves the registry at the next refresh: its renders since the
        // baseline must still be in the profile.
        cut.Render(p => p.Add(c => c.ShowChild, false));
        Session.Components.Refresh(force: true);
        Session.Components.Refresh(force: true);
        Assert.IsFalse(Session.Components.All.Any(r => r.Type == typeof(ChildComponent)), "the disposed child was let go");

        using var json = JsonDocument.Parse(BevSessionExporter.ToJson(Session, new BevSessionExportOptions { Sections = new HashSet<string> { "profile" } }));
        var profile = json.RootElement.GetProperty("profile");
        Assert.IsTrue(profile.GetProperty("sinceBaseline").GetBoolean());
        var types = profile.GetProperty("types").EnumerateArray().ToDictionary(t => t.GetProperty("displayName").GetString()!);
        Assert.AreEqual(3, types["ParentComponent"].GetProperty("renders").GetInt32(), "two increments and the re-render without the child");
        Assert.AreEqual(2, types["ChildComponent"].GetProperty("renders").GetInt32());
        Assert.AreEqual(1, types["ChildComponent"].GetProperty("instances").GetInt32());
        Assert.IsTrue(types["ParentComponent"].TryGetProperty("p95RenderMs", out _));
        Assert.AreEqual(3, types["ParentComponent"].GetProperty("retainedSamples").GetInt32());
        Assert.IsFalse(json.RootElement.TryGetProperty("components", out _), "only the requested section");

        // Compared with itself, a recording has no changes.
        var export = BevSessionExporter.ToJson(Session);
        Assert.IsTrue(BevProfileComparison.Compare(export, export).Changes.All(c => c.Kind == BevProfileChangeKind.Unchanged));

        // A new window starts the components already let go at zero, and Clear forgets them.
        Session.Components.MarkBaseline();
        var gone = Assert.ContainsSingle(Session.Components.GoneTotals());
        Assert.AreEqual(0, gone.RendersSinceBaseline);
        Assert.AreEqual(4, gone.Renders, "its renders over the whole session are kept: the first, one before the first reset, two after");
        Assert.IsFalse(BevRenderProfile.Of(Session).Types.Any(t => t.DisplayName == "ChildComponent"));
        Session.Components.ClearDisposed();
        Assert.IsEmpty(Session.Components.GoneTotals());
    }

    [TestMethod]
    public void Markdown_lists_regressions_with_safe_cells()
    {
        var result = BevProfileComparison.Compare(Export(true, ("A|B", 1, 1, 1)), Export(true, ("A|B", 1, 3, 1)));

        var markdown = result.ToMarkdown();

        Assert.Contains("**1 regression**", markdown, StringComparison.Ordinal);
        Assert.Contains("#### Regressed (1)", markdown, StringComparison.Ordinal);
        Assert.Contains("| `A\\|B` | 1 → 1 | 1 → 3 |", markdown, StringComparison.Ordinal);

        var multiline = BevProfileComparison.Compare(Export(true, ("Evil\nRow", 1, 1, 1)), Export(true, ("Evil\nRow", 1, 3, 1))).ToMarkdown();
        Assert.Contains("| `Evil Row` |", multiline, StringComparison.Ordinal);
    }

    [TestMethod]
    public void The_profiler_compares_the_session_with_an_uploaded_export()
    {
        var panel = RenderPanel();
        var cut = Render<ParentComponent>();
        Session.Components.MarkBaseline();
        cut.InvokeAsync(() => cut.Instance.Increment());
        var baseline = BevSessionExporter.ToJson(Session);
        cut.InvokeAsync(() => cut.Instance.Increment());
        cut.InvokeAsync(() => cut.Instance.Increment());

        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Profiler", StringComparison.Ordinal)).Click();
        panel.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(baseline, "before.json"));

        var section = panel.FindAll(".bev-section").Single(s => s.TextContent.Contains("Compared with before.json", StringComparison.Ordinal));
        Assert.Contains("2 regressed", section.TextContent, StringComparison.Ordinal);
        var rows = section.QuerySelectorAll("tbody tr");
        Assert.AreEqual(2, rows.Length);
        Assert.Contains("1 → 3", rows[0].TextContent, StringComparison.Ordinal);

        rows[0].Click();
        Assert.IsNotNull(Session.Ui.GetQuery("profiler"));

        // Looking at another tab and coming back keeps the comparison and the sort; Compare again counts what rendered since.
        panel.FindAll("thead th[aria-sort] button").Single(b => b.TextContent.StartsWith("Max", StringComparison.Ordinal)).Click();
        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Components", StringComparison.Ordinal)).Click();
        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Profiler", StringComparison.Ordinal)).Click();
        Assert.Contains("Compared with before.json", panel.Markup, StringComparison.Ordinal);
        Assert.AreEqual("descending", panel.FindAll("thead th[aria-sort]").Single(h => h.TextContent.StartsWith("Max", StringComparison.Ordinal)).GetAttribute("aria-sort"));
        cut.InvokeAsync(() => cut.Instance.Increment());
        panel.FindAll("button").Single(b => b.TextContent.Contains("Compare again", StringComparison.Ordinal)).Click();
        Assert.Contains("1 → 4", panel.FindAll(".bev-section").Single(s => s.TextContent.Contains("Compared with", StringComparison.Ordinal)).QuerySelectorAll("tbody tr")[0].TextContent, StringComparison.Ordinal);

        panel.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("not json", "broken.json"));
        Assert.Contains("broken.json: Not JSON", panel.Markup, StringComparison.Ordinal);
        panel.Find("button[aria-label='Close the comparison']").Click();
        Assert.DoesNotContain("broken.json", panel.Markup, StringComparison.Ordinal);
    }

    private IRenderedComponent<BevToolsPanel> RenderPanel()
    {
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { Online = true });
        module.Setup<string?>("loadPrefs", _ => true).SetResult(null);
        module.SetupVoid("savePrefs", _ => true).SetVoidResult();
        return Render<BevToolsPanel>();
    }
}
