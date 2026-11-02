using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>
/// The profiler workflow is "reset, do one thing, read what rendered", and on list-heavy pages "what did all the rows
/// cost together". Neither is possible with per-instance totals since page load.
/// </summary>
[TestClass]
public class ProfilerTests : BunitContext
{
    public ProfilerTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
            // The tests refresh explicitly. The panel's own refresh loop could re-render between a FindAll and the
            // Click that follows it, and a click on a replaced element's handler does nothing.
            o.Ui.RefreshIntervalMs = 600_000;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void Baseline_restarts_the_since_figures_without_touching_raw_counters()
    {
        var cut = Render<ParentComponent>();
        cut.InvokeAsync(() => cut.Instance.Refresh());
        var parent = Session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        Assert.AreEqual(2, parent.RenderCount);
        Assert.AreEqual(2, parent.RendersSinceBaseline);

        Session.Components.MarkBaseline();
        Assert.IsNotNull(Session.Components.BaselineAt);
        Assert.AreEqual(0, parent.RendersSinceBaseline);
        Assert.AreEqual(0, parent.RenderMsSinceBaseline, delta: 1e-6);
        Assert.AreEqual(0, parent.MaxRenderMsSinceBaseline);
        Assert.AreEqual(2, parent.RenderCount);

        cut.InvokeAsync(() => cut.Instance.Refresh());
        Assert.AreEqual(1, parent.RendersSinceBaseline);
        Assert.AreEqual(3, parent.RenderCount);
        Assert.IsTrue(parent.MaxRenderMsSinceBaseline <= parent.MaxRenderMs);
        Assert.AreEqual(parent.LastRenderMs, parent.RenderMsSinceBaseline, delta: 1e-6);
    }

    [TestMethod]
    public void Components_created_after_the_baseline_count_in_full()
    {
        Session.Components.MarkBaseline();
        var cut = Render<ParentComponent>();
        var parent = Session.Components.All.Single(r => r.Type == typeof(ParentComponent));

        Assert.IsNull(parent.BaselineAt);
        Assert.AreEqual(1, parent.RendersSinceBaseline);
    }

    [TestMethod]
    public void Profiler_groups_instances_by_type_and_drills_back_down()
    {
        var panel = RenderPanel();
        Render<TwoChildren>();
        panel.Find("button[aria-label='Refresh captured data']").Click();
        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Profiler", StringComparison.Ordinal)).Click();

        panel.Find("input[aria-label='Group by type']").Change(true);
        var rows = panel.FindAll("tbody tr.bev-row");
        var grouped = Assert.ContainsSingle(r => r.TextContent.Contains("ChildComponent", StringComparison.Ordinal), rows);
        var cells = grouped.QuerySelectorAll("td").Select(c => c.TextContent.Trim()).ToList();
        Assert.AreEqual("2", cells[1]); // instances
        Assert.AreEqual("2", cells[2]); // renders
        Assert.Contains("2 initial", cells[7], StringComparison.Ordinal); // after Total, Avg, p95, Max

        grouped.Click();
        Assert.AreEqual("ChildComponent", Session.Ui.GetQuery("profiler"));
        Assert.AreEqual(2, panel.FindAll("tbody tr.bev-row").Count(r => r.TextContent.Contains("ChildComponent", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Reset_button_shows_only_renders_since_the_reset()
    {
        var panel = RenderPanel();
        var cut = Render<ParentComponent>();
        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Profiler", StringComparison.Ordinal)).Click();
        panel.Find("button[aria-label='Refresh captured data']").Click();
        Assert.Contains(r => r.TextContent.Contains("ParentComponent", StringComparison.Ordinal), panel.FindAll("tbody tr.bev-row"));

        panel.Find("button[aria-label='Reset profiler baseline']").Click();
        Assert.IsTrue(panel.Find("input[aria-label='Since baseline']").HasAttribute("checked"));
        Assert.Contains("Nothing rendered since the baseline", panel.Markup, StringComparison.Ordinal);

        cut.InvokeAsync(() => cut.Instance.Refresh());
        panel.Find("button[aria-label='Refresh captured data']").Click();
        var row = Assert.ContainsSingle(r => r.TextContent.Contains("ParentComponent", StringComparison.Ordinal), panel.FindAll("tbody tr.bev-row"));
        Assert.AreEqual("1", row.QuerySelectorAll("td")[1].TextContent.Trim());
    }

    [TestMethod]
    public void P95_is_the_nearest_rank_of_the_retained_durations()
    {
        Assert.AreEqual(0, BevStats.Percentile([], 95));
        Assert.AreEqual(5, BevStats.Percentile([5], 95));
        Assert.AreEqual(19, BevStats.Percentile(Enumerable.Range(1, 20).Select(i => (double)i).ToArray(), 95));
        Assert.AreEqual(100, BevStats.Percentile(Enumerable.Range(1, 100).Select(i => (double)i).ToArray(), 100));
    }

    [TestMethod]
    public void Profiler_rows_show_p95_and_headers_sort_by_keyboard_operable_buttons()
    {
        var panel = RenderPanel();
        var cut = Render<ParentComponent>();
        for (var i = 0; i < 4; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Refresh());
        }

        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Profiler", StringComparison.Ordinal)).Click();
        panel.Find("button[aria-label='Refresh captured data']").Click();

        var headers = panel.FindAll("thead th[aria-sort]");
        Assert.IsTrue(headers.All(h => h.QuerySelector("button[type=button]") is not null), "every sortable header is a button");
        Assert.AreEqual("descending", headers.Single(h => h.TextContent.Contains("Total", StringComparison.Ordinal)).GetAttribute("aria-sort"));

        var parent = Session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        var row = Assert.ContainsSingle(r => r.TextContent.Contains("ParentComponent", StringComparison.Ordinal), panel.FindAll("tbody tr.bev-row"));
        var p95Cell = row.QuerySelectorAll("td")[4];
        Assert.AreEqual("of 5 retained renders", p95Cell.GetAttribute("title"));
        var sorted = parent.Renders.Select(r => r.DurationMs).Order().ToArray();
        Assert.AreEqual(BevFmt.Ms(sorted[^1]), p95Cell.TextContent.Trim(), "nearest rank of 5 samples at 95% is the largest");

        panel.FindAll("thead th[aria-sort] button").Single(b => b.TextContent.StartsWith("p95", StringComparison.Ordinal)).Click();
        headers = panel.FindAll("thead th[aria-sort]");
        Assert.AreEqual("descending", headers.Single(h => h.TextContent.Contains("p95", StringComparison.Ordinal)).GetAttribute("aria-sort"));
        Assert.AreEqual("none", headers.Single(h => h.TextContent.Contains("Total", StringComparison.Ordinal)).GetAttribute("aria-sort"));
    }

    [TestMethod]
    public void A_selected_render_batch_shows_what_it_rendered_as_a_cost_tree()
    {
        var panel = RenderPanel();
        var cut = Render<ParentComponent>();
        Session.RenderTracker.CloseBatch();
        cut.InvokeAsync(() => cut.Instance.Increment()); // a new parameter value, so the child renders too
        var batchId = Session.RenderTracker.CurrentBatch!.Id;
        var parent = Session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        var child = Session.Components.All.Single(r => r.Type == typeof(ChildComponent));
        Assert.Contains(s => s.BatchId == batchId, parent.Renders);
        Assert.Contains(s => s.BatchId == batchId, child.Renders);

        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Profiler", StringComparison.Ordinal)).Click();
        panel.Find("button[aria-label='Refresh captured data']").Click();
        panel.FindAll("button").Single(b => b.TextContent == "Render batch #" + batchId).Click();

        var section = panel.FindAll(".bev-section").Single(s => s.TextContent.Contains("Render batch #" + batchId + ": what it rendered", StringComparison.Ordinal));
        var rows = section.QuerySelectorAll("tbody tr");
        Assert.AreEqual(2, rows.Length);
        Assert.StartsWith("ParentComponent", rows[0].QuerySelector("td")!.TextContent.Trim());
        Assert.StartsWith("ChildComponent", rows[1].QuerySelector("td")!.TextContent.Trim());
        Assert.Contains("padding-left:6px", rows[0].QuerySelector("td")!.GetAttribute("style")!, StringComparison.Ordinal);
        Assert.Contains("padding-left:20px", rows[1].QuerySelector("td")!.GetAttribute("style")!, StringComparison.Ordinal);

        var parentSelf = parent.Renders.Where(s => s.BatchId == batchId).Sum(s => s.DurationMs);
        var childSelf = child.Renders.Where(s => s.BatchId == batchId).Sum(s => s.DurationMs);
        var cells = rows[0].QuerySelectorAll("td");
        Assert.AreEqual(BevFmt.Ms(parentSelf), cells[1].TextContent.Trim());
        Assert.AreEqual(BevFmt.Ms(parentSelf + childSelf), cells[2].TextContent.Trim(), "with descendants adds the child's own time");

        rows[1].Click();
        Assert.AreEqual(child.InstanceId, Session.Ui.SelectedComponentId);
    }

    [TestMethod]
    public void P95_differs_from_max_sorts_rows_by_itself_and_follows_the_reset_window()
    {
        var panel = RenderPanel();
        Render<ParentComponent>();
        Render<TwoChildren>();
        var spiky = Session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        var steady = Session.Components.All.Single(r => r.Type == typeof(TwoChildren));
        // 21 samples each with the real first render: spiky is 19 × 1 ms and one 100 ms, steady is 20 × 10 ms.
        AddSamples(spiky, [.. Enumerable.Repeat(1d, 19), 100]);
        AddSamples(steady, [.. Enumerable.Repeat(10d, 20)]);
        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Profiler", StringComparison.Ordinal)).Click();
        panel.Find("button[aria-label='Refresh captured data']").Click();

        string[] Cells(string name) => panel.FindAll("tbody tr.bev-row").First(r => r.QuerySelector("td")!.TextContent.Trim().StartsWith(name, StringComparison.Ordinal))
            .QuerySelectorAll("td").Select(c => c.TextContent.Trim()).ToArray();
        string[] Order() => [.. panel.FindAll("tbody tr.bev-row").Select(r => r.QuerySelector("td")!.TextContent.Trim()).Where(n => n is "ParentComponent" or "TwoChildren")];

        Assert.AreEqual(BevFmt.Ms(1), Cells("ParentComponent")[4], "nearest rank 20 of 21 is the last 1 ms sample, not the 100 ms spike");
        Assert.AreEqual(BevFmt.Ms(100), Cells("ParentComponent")[5]);
        Assert.AreEqual(BevFmt.Ms(10), Cells("TwoChildren")[4]);
        Assert.AreEqual("of 21 retained renders", panel.FindAll("tbody tr.bev-row").First(r => r.TextContent.Contains("ParentComponent", StringComparison.Ordinal)).QuerySelectorAll("td")[4].GetAttribute("title"));

        panel.FindAll("thead th[aria-sort] button").Single(b => b.TextContent.StartsWith("p95", StringComparison.Ordinal)).Click();
        CollectionAssert.AreEqual(new[] { "TwoChildren", "ParentComponent" }, Order());
        panel.FindAll("thead th[aria-sort] button").Single(b => b.TextContent.StartsWith("Max", StringComparison.Ordinal)).Click();
        CollectionAssert.AreEqual(new[] { "ParentComponent", "TwoChildren" }, Order());

        // After a reset, only the renders since: three of 50 ms.
        panel.Find("button[aria-label='Reset profiler baseline']").Click();
        AddSamples(spiky, [50, 50, 50]);
        panel.Find("button[aria-label='Refresh captured data']").Click();
        Assert.AreEqual(BevFmt.Ms(50), Cells("ParentComponent")[4]);
        Assert.AreEqual("of 3 retained renders", panel.FindAll("tbody tr.bev-row").First(r => r.TextContent.Contains("ParentComponent", StringComparison.Ordinal)).QuerySelectorAll("td")[4].GetAttribute("title"));
    }

    [TestMethod]
    public void A_selected_batch_closes_by_its_button_by_selecting_it_again_and_when_the_timeline_is_cleared()
    {
        var panel = RenderPanel();
        var cut = Render<ParentComponent>();
        Session.RenderTracker.CloseBatch();
        cut.InvokeAsync(() => cut.Instance.Increment());
        var batchId = Session.RenderTracker.CurrentBatch!.Id;
        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Profiler", StringComparison.Ordinal)).Click();
        panel.Find("button[aria-label='Refresh captured data']").Click();
        var title = "Render batch #" + batchId + ": what it rendered";
        AngleSharp.Dom.IElement BatchButton() => panel.FindAll("button").Single(b => b.TextContent == "Render batch #" + batchId);

        BatchButton().Click();
        Assert.Contains(title, panel.Markup, StringComparison.Ordinal);
        Assert.AreEqual("true", BatchButton().GetAttribute("aria-pressed"));
        BatchButton().Click();
        Assert.DoesNotContain(title, panel.Markup, StringComparison.Ordinal);

        BatchButton().Click();
        panel.Find("button[aria-label='Close the batch']").Click();
        Assert.DoesNotContain(title, panel.Markup, StringComparison.Ordinal);

        BatchButton().Click();
        Session.ClearAll();
        panel.Find("button[aria-label='Refresh captured data']").Click();
        Assert.DoesNotContain(title, panel.Markup, StringComparison.Ordinal);
        Assert.IsNull(Session.Ui.Profiler.BatchId);
    }

    private static void AddSamples(BevComponentRecord record, double[] durations)
    {
        foreach (var ms in durations)
        {
            record.AddRender(new BevRenderSample { Sequence = record.RenderCount, At = DateTimeOffset.UtcNow, StartTicks = System.Diagnostics.Stopwatch.GetTimestamp(), DurationMs = ms });
        }
    }

    [TestMethod]
    public void A_sort_by_a_column_the_grouping_hides_falls_back_to_one_that_is_shown()
    {
        var panel = RenderPanel();
        Render<TwoChildren>();
        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Profiler", StringComparison.Ordinal)).Click();
        panel.Find("button[aria-label='Refresh captured data']").Click();

        panel.FindAll("thead th[aria-sort] button").Single(b => b.TextContent.StartsWith("Last", StringComparison.Ordinal)).Click();
        panel.Find("input[aria-label='Group by type']").Change(true);

        Assert.AreEqual("descending", panel.FindAll("thead th[aria-sort]").Single(h => h.TextContent.StartsWith("Total", StringComparison.Ordinal)).GetAttribute("aria-sort"), "Last is not a column by type");

        Session.Ui.SetQuery("profiler", "sort:MAX");
        panel.Find("button[aria-label='Refresh captured data']").Click();
        Assert.AreEqual("descending", panel.FindAll("thead th[aria-sort]").Single(h => h.TextContent.StartsWith("Max", StringComparison.Ordinal)).GetAttribute("aria-sort"), "the token's value is read whatever its case");
    }

    [TestMethod]
    public void Same_type_siblings_that_render_nothing_below_are_one_row_of_a_batch_tree()
    {
        var panel = RenderPanel();
        Session.RenderTracker.CloseBatch();
        Render<TwoChildren>();
        var batchId = Session.RenderTracker.CurrentBatch!.Id;
        var children = Session.Components.All.Where(r => r.Type == typeof(ChildComponent)).ToList();
        Assert.HasCount(2, children);

        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Profiler", StringComparison.Ordinal)).Click();
        panel.Find("button[aria-label='Refresh captured data']").Click();
        panel.FindAll("button").Single(b => b.TextContent == "Render batch #" + batchId).Click();

        var section = panel.FindAll(".bev-section").Single(s => s.TextContent.Contains("Render batch #" + batchId + ": what it rendered", StringComparison.Ordinal));
        var rows = section.QuerySelectorAll("tbody tr");
        Assert.AreEqual(2, rows.Length);
        Assert.StartsWith("TwoChildren", rows[0].QuerySelector("td")!.TextContent.Trim());
        Assert.AreEqual("ChildComponent ×2 instances", rows[1].QuerySelector("td")!.TextContent.Trim());
        Assert.AreEqual(BevFmt.Ms(children.Sum(c => c.Renders.Where(s => s.BatchId == batchId).Sum(s => s.DurationMs))), rows[1].QuerySelectorAll("td")[1].TextContent.Trim());

        rows[1].Click();
        Assert.AreEqual("ChildComponent", Session.Ui.GetQuery("profiler"));
        Assert.IsTrue(panel.Find("input[aria-label='Group by type']").HasAttribute("checked"));
    }

    [TestMethod]
    public void Header_button_pauses_and_resumes_capture()
    {
        var panel = RenderPanel();
        var button = panel.Find("button[aria-label='Pause capture']");
        button.Click();

        Assert.IsFalse(Session.IsCapturing);
        Assert.AreEqual("true", panel.Find("button[aria-label='Resume capture']").GetAttribute("aria-pressed"));

        panel.Find("button[aria-label='Resume capture']").Click();
        Assert.IsTrue(Session.IsCapturing);
        Assert.IsNotNull(panel.Find("button[aria-label='Pause capture']"));
    }

    [TestMethod]
    public void Component_tree_is_an_accessible_tree_with_subtree_totals_on_collapsed_nodes()
    {
        var panel = RenderPanel();
        Render<ParentComponent>();
        panel.Find("button[aria-label='Refresh captured data']").Click();

        var tree = panel.Find("[role='tree']");
        var items = tree.QuerySelectorAll("[role='treeitem']");
        Assert.AreEqual(2, items.Length);
        var parentRow = items[0];
        Assert.AreEqual("1", parentRow.GetAttribute("aria-level"));
        Assert.AreEqual("true", parentRow.GetAttribute("aria-expanded"));
        Assert.AreEqual("2", items[1].GetAttribute("aria-level"));
        Assert.IsNull(items[1].GetAttribute("aria-expanded"));

        parentRow.QuerySelector(".bev-caret")!.Click();
        parentRow = panel.Find("[role='treeitem']");
        Assert.AreEqual("false", parentRow.GetAttribute("aria-expanded"));
        Assert.Contains("⊕ 2 ×", parentRow.QuerySelector(".bev-subtree")!.TextContent, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task Export_and_report_commands_use_the_command_context_bridge()
    {
        RenderPanel();
        Render<ParentComponent>();
        var commands = new CommandRegistry();
        commands.AddRange(BuiltInCommands.Create());
        var context = new RecordingContext(Services);

        await commands.Find("session.export")!.ExecuteAsync(context, null);
        await commands.Find("session.report")!.ExecuteAsync(context, null);
        await commands.Find("capture.toggle")!.ExecuteAsync(context, null);
        await commands.Find("profiler.reset")!.ExecuteAsync(context, null);

        Assert.EndsWith(".json", context.DownloadedFileName, StringComparison.Ordinal);
        Assert.Contains("\"format\"", context.DownloadedContent!, StringComparison.Ordinal);
        // The BevTools panel is rendered in this test; none of its own components may leak into the export.
        Assert.DoesNotContain("\"Bit.BevTools.Bev", context.DownloadedContent!, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Bit.BevTools.BevToolsPanel\"", context.DownloadedContent!, StringComparison.Ordinal);
        Assert.Contains("\"ParentComponent\"", context.DownloadedContent!, StringComparison.Ordinal);
        Assert.StartsWith("# BevTools report", context.CopiedText, StringComparison.Ordinal);
        Assert.IsFalse(Session.IsCapturing);
        Assert.IsNotNull(Session.Components.BaselineAt);
        Assert.Contains(n => n.StartsWith("Export downloaded", StringComparison.Ordinal), context.Notifications);
    }

    /// <summary>
    /// The ring keeps 100 samples per instance. Fractions and cause breakdowns computed from it disagreed with the
    /// session-long render counters next to them: "250 of 100 compared renders", or a Causes column that stopped at 100.
    /// </summary>
    [TestMethod]
    public void Inspector_and_grouped_profiler_figures_add_up_beyond_the_sample_ring()
    {
        var panel = RenderPanel();
        var cut = Render<WastedRenderTests.ListParent>();
        var child = Session.Components.All.Single(r => r.Type == typeof(WastedRenderTests.ListChild));
        panel.InvokeAsync(() => panel.Instance.SelectComponent(child.InstanceId));

        // The first render after selecting starts the capture; the 120 after it are compared, and all are forced.
        for (var i = 0; i < 121; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Bump());
        }

        Assert.AreEqual(100, child.Renders.Length);
        panel.Find("button[aria-label='Refresh captured data']").Click();
        Assert.Contains("of 120 compared renders were forced", panel.Markup, StringComparison.Ordinal);
        var parentCause = Assert.ContainsSingle(r => r.QuerySelector("td")?.TextContent == "ParentRender", panel.FindAll("tr"));
        Assert.AreEqual("121", parentCause.QuerySelectorAll("td")[1].TextContent.Trim());

        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Profiler", StringComparison.Ordinal)).Click();
        panel.Find("input[aria-label='Group by type']").Change(true);
        var grouped = Assert.ContainsSingle(r => r.TextContent.Contains("ListChild", StringComparison.Ordinal), panel.FindAll("tbody tr.bev-row"));
        var cells = grouped.QuerySelectorAll("td").Select(c => c.TextContent.Trim()).ToList();
        Assert.AreEqual("122", cells[2]);
        Assert.AreEqual("121 parent · 1 initial", cells[7]);
    }

    /// <summary>A navigation while paused is not in the timeline, so it must not be remembered as one.</summary>
    [TestMethod]
    public void Navigation_while_paused_leaves_no_event_id_behind()
    {
        RenderPanel();
        var navigation = Services.GetRequiredService<NavigationManager>();

        Session.PauseCapture();
        navigation.NavigateTo("/paused");
        Assert.IsNull(Session.LastNavigationEventId);

        Session.ResumeCapture();
        navigation.NavigateTo("/resumed");
        var recorded = Session.Timeline.Find(Session.LastNavigationEventId!.Value);
        Assert.AreEqual("Navigate to /resumed", recorded!.Title);
    }

    private IRenderedComponent<BevToolsPanel> RenderPanel()
    {
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { Online = true });
        module.Setup<string?>("loadPrefs", _ => true).SetResult(null);
        module.SetupVoid("savePrefs", _ => true).SetVoidResult();
        return Render<BevToolsPanel>();
    }

    private sealed class TwoChildren : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<ChildComponent>(0);
            builder.AddComponentParameter(1, nameof(ChildComponent.Text), "one");
            builder.CloseComponent();
            builder.OpenComponent<ChildComponent>(2);
            builder.AddComponentParameter(3, nameof(ChildComponent.Text), "two");
            builder.CloseComponent();
        }
    }

    private sealed class RecordingContext(IServiceProvider services) : IBevToolsCommandContext
    {
        public IServiceProvider Services => services;

        public string? DownloadedFileName { get; private set; }

        public string? DownloadedContent { get; private set; }

        public string? CopiedText { get; private set; }

        public List<string> Notifications { get; } = [];

        public void ShowPanel(string panelId, string? query = null) { }

        public void SelectComponent(long instanceId) { }

        public void SelectEvent(long eventId) { }

        public void Notify(string message) => Notifications.Add(message);

        public Task<bool> CopyTextAsync(string text)
        {
            CopiedText = text;
            return Task.FromResult(true);
        }

        public Task<bool> DownloadTextAsync(string fileName, string content, string contentType = "text/plain")
        {
            DownloadedFileName = fileName;
            DownloadedContent = content;
            return Task.FromResult(true);
        }
    }
}
