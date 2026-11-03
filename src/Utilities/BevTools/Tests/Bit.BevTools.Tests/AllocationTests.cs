using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// What each render allocates: read from the runtime's per-thread counter right around BuildRenderTree, so it is the
/// component's own render, kept per component, per type, in exports, comparisons and a rule.
/// </summary>
[TestClass]
public class AllocationTests : BunitContext
{
    public AllocationTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
            o.Ui.RefreshIntervalMs = 600_000;
            o.Diagnostics.HeavyRenderAllocationBytes = 32 * 1024;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    private static string Export(params (string Type, int Renders, long? Allocated)[] types) =>
        JsonSerializer.Serialize(new
        {
            format = "bit-bevtools/1",
            exportedAt = DateTimeOffset.UtcNow,
            session = new { runtime = ".NET 10.0.0", renderMode = "Server" },
            profile = new
            {
                sinceBaseline = true,
                truncated = false,
                types = types.Select(t => new { type = "App." + t.Type, displayName = t.Type, instances = 1, renders = t.Renders, renderMs = 10.0, maxRenderMs = 1.0, allocatedBytes = t.Allocated }),
            },
        });

    [TestMethod]
    public void The_runtime_running_the_tests_counts_allocations_per_thread() => Assert.IsTrue(BevAllocations.IsMeasurable);

    [TestMethod]
    public void A_render_counts_what_its_own_fragment_allocated_and_not_its_childrens()
    {
        var cut = Render<AllocatingParent>(ps => ps.Add(p => p.ChildBytes, 64 * 1024).Add(p => p.OwnBytes, 8 * 1024));
        var parent = Session.Components.All.Single(r => r.Type == typeof(AllocatingParent));
        var child = Session.Components.All.Single(r => r.Type == typeof(AllocatingChild));

        var own = parent.LastRender!.AllocatedBytes!.Value;
        Assert.IsTrue(own >= 8 * 1024, "the parent's own buffer: " + own);
        Assert.IsTrue(own < 64 * 1024, "the child's buffer is the child's: " + own);
        Assert.IsTrue(child.LastRender!.AllocatedBytes >= 64 * 1024);

        cut.Render(ps => ps.Add(p => p.ChildBytes, 64 * 1024).Add(p => p.OwnBytes, 8 * 1024));
        Assert.AreEqual(2, parent.RenderCount);
        Assert.AreEqual(parent.Renders.Sum(r => r.AllocatedBytes!.Value), parent.TotalAllocatedBytes);
        Assert.IsTrue(parent.MaxAllocatedBytes >= 8 * 1024);
        Assert.IsTrue(Session.Timeline.Find(parent.LastRender.EventId)!.TryGetData("allocatedBytes", out var data) && data is long bytes && bytes == parent.LastRender.AllocatedBytes);
    }

    [TestMethod]
    public void The_baseline_window_restarts_the_allocation_figures()
    {
        var cut = Render<AllocatingParent>(ps => ps.Add(p => p.OwnBytes, 16 * 1024));
        var parent = Session.Components.All.Single(r => r.Type == typeof(AllocatingParent));
        Session.Components.MarkBaseline();
        Assert.AreEqual(0, parent.AllocatedBytesSinceBaseline);
        Assert.AreEqual(0, parent.MaxAllocatedBytesSinceBaseline);

        cut.Render(ps => ps.Add(p => p.OwnBytes, 16 * 1024));
        Assert.AreEqual(parent.LastRender!.AllocatedBytes, parent.AllocatedBytesSinceBaseline);

        var profile = BevRenderProfile.Of(Session);
        var type = profile.Types.Single(t => t.DisplayName == nameof(AllocatingParent));
        Assert.AreEqual(parent.AllocatedBytesSinceBaseline, type.AllocatedBytes);

        var json = BevSessionExporter.ToJson(Session, new BevSessionExportOptions { Sections = new HashSet<string> { "profile", "components" } });
        var parsed = BevRenderProfile.Parse(json).Types.Single(t => t.DisplayName == nameof(AllocatingParent));
        Assert.AreEqual(type.AllocatedBytes, parsed.AllocatedBytes, "written to the profile and read back");
        using var document = JsonDocument.Parse(json);
        var component = document.RootElement.GetProperty("components").GetProperty("items").EnumerateArray().Single(c => c.GetProperty("displayName").GetString() == nameof(AllocatingParent));
        Assert.AreEqual(parent.TotalAllocatedBytes, component.GetProperty("allocatedBytes").GetInt64());
        Assert.AreEqual(parent.AllocatedBytesSinceBaseline, component.GetProperty("allocatedBytesSinceBaseline").GetInt64());
    }

    [TestMethod]
    public void More_bytes_per_render_is_a_regression_and_fewer_an_improvement_beyond_the_thresholds()
    {
        var baseline = Export(("Grid", 10, 10 * 2048), ("Row", 100, 100 * 512), ("Card", 10, 10 * 4096), ("Old", 10, null));
        var current = Export(("Grid", 10, 10 * 20480), ("Row", 100, 100 * 700), ("Card", 10, 10 * 1024), ("Old", 10, 10 * 99999));

        var result = BevProfileComparison.Compare(baseline, current);
        var grid = result.Changes.Single(c => c.DisplayName == "Grid");
        Assert.AreEqual(BevProfileChangeKind.Regressed, grid.Kind);
        Assert.AreEqual("allocated per render +18 KB (+900 %)", grid.Reason);
        Assert.AreEqual(2048, grid.BaselineBytesPerRender);
        Assert.AreEqual(20480, grid.CurrentBytesPerRender);
        Assert.AreEqual(BevProfileChangeKind.Unchanged, result.Changes.Single(c => c.DisplayName == "Row").Kind, "188 B more per render is below 1 KB");
        var card = result.Changes.Single(c => c.DisplayName == "Card");
        Assert.AreEqual(BevProfileChangeKind.Improved, card.Kind);
        StringAssert.Contains(card.Reason, "allocated per render -3 KB");
        Assert.AreEqual(BevProfileChangeKind.Unchanged, result.Changes.Single(c => c.DisplayName == "Old").Kind, "not measured in the baseline: not compared");

        var markdown = result.ToMarkdown();
        StringAssert.Contains(markdown, "| Allocated per render | Why |");
        StringAssert.Contains(markdown, "| 2 KB → 20 KB | allocated per render +18 KB (+900 %) |");
    }

    [TestMethod]
    public void A_component_that_allocates_heavily_on_every_render_is_reported()
    {
        var cut = Render<AllocatingParent>(ps => ps.Add(p => p.OwnBytes, 64 * 1024));
        for (var i = 0; i < 3; i++)
        {
            cut.Render(ps => ps.Add(p => p.OwnBytes, 64 * 1024));
        }

        Session.Diagnostics.Evaluate(force: true);
        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "render.allocations", Session.Diagnostics.Findings).Diagnostic;
        StringAssert.StartsWith(finding.Title, "AllocatingParent allocates");
        StringAssert.Contains(finding.Evidence, "4 renders in the last 30 s");
        Assert.DoesNotContain(f => f.Diagnostic.Title.StartsWith("AllocatingChild", StringComparison.Ordinal), Session.Diagnostics.Findings, "the child allocated nothing here");
    }

    [TestMethod]
    public void The_profiler_shows_and_sorts_by_what_renders_allocated()
    {
        Render<AllocatingParent>(ps => ps.Add(p => p.OwnBytes, 32 * 1024).Add(p => p.ChildBytes, 1024));
        var panel = Render<BevToolsPanel>();
        panel.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith("Profiler", StringComparison.Ordinal)).Click();
        panel.FindAll("th button").First(b => b.TextContent.Contains("Allocated", StringComparison.Ordinal)).Click();

        var first = panel.FindAll(".bev-body tbody tr.bev-row").First();
        StringAssert.Contains(first.TextContent, nameof(AllocatingParent), "the most allocating first");
        StringAssert.Contains(first.InnerHtml, "per render");
    }

    public sealed class AllocatingParent : ComponentBase
    {
        [Parameter] public int OwnBytes { get; set; }

        [Parameter] public int ChildBytes { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var buffer = new byte[OwnBytes];
            builder.OpenElement(0, "div");
            builder.AddContent(1, buffer.Length);
            builder.OpenComponent<AllocatingChild>(2);
            builder.AddComponentParameter(3, nameof(AllocatingChild.Bytes), ChildBytes);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }

    public sealed class AllocatingChild : ComponentBase
    {
        [Parameter] public int Bytes { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, new byte[Bytes].Length);
    }
}
