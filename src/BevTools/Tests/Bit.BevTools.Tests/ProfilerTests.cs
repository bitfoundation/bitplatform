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
        Assert.Contains("2 initial", cells[6], StringComparison.Ordinal);

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
        Assert.AreEqual("121 parent · 1 initial", cells[6]);
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
