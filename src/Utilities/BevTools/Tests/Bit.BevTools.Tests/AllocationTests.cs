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

    private static string Export(params (string Type, int Renders, long? Allocated)[] types) => ExportIn("Server", ".NET 10.0.0", types);

    private static string ExportIn(string renderMode, string runtime, params (string Type, int Renders, long? Allocated)[] types) =>
        JsonSerializer.Serialize(new
        {
            format = "bit-bevtools/1",
            exportedAt = DateTimeOffset.UtcNow,
            session = new { runtime, renderMode },
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
        Assert.IsGreaterThanOrEqualTo(8 * 1024, own, "the parent's own buffer: " + own);
        Assert.IsLessThan(64 * 1024, own, "the child's buffer is the child's: " + own);
        Assert.IsTrue(child.LastRender!.AllocatedBytes >= 64 * 1024);

        cut.Render(ps => ps.Add(p => p.ChildBytes, 64 * 1024).Add(p => p.OwnBytes, 8 * 1024));
        Assert.AreEqual(2, parent.RenderCount);
        Assert.AreEqual(parent.Renders.Sum(r => r.AllocatedBytes!.Value), parent.TotalAllocatedBytes);
        Assert.IsGreaterThanOrEqualTo(8 * 1024, parent.MaxAllocatedBytes);
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
        Assert.Contains("allocated per render -3 KB", card.Reason!);
        Assert.AreEqual(BevProfileChangeKind.Unchanged, result.Changes.Single(c => c.DisplayName == "Old").Kind, "not measured in the baseline: not compared");

        var markdown = result.ToMarkdown();
        Assert.Contains("| Allocated per render | Why |", markdown);
        Assert.Contains("| 2 KB → 20 KB | allocated per render +18 KB (+900 %) |", markdown);
    }

    [TestMethod]
    public void Allocations_are_not_compared_across_render_modes_nor_trusted_across_runtimes()
    {
        var baseline = ExportIn("Server", ".NET 10.0.0", ("Grid", 10, 10 * 2048));
        var otherMode = BevProfileComparison.Compare(baseline, ExportIn("WebAssembly", ".NET 10.0.0", ("Grid", 10, 10 * 20480)));
        Assert.AreEqual(BevProfileChangeKind.Unchanged, otherMode.Changes.Single().Kind, "a circuit's runtime and the browser's allocate differently");
        Assert.Contains("times and allocations are not", Assert.ContainsSingle(otherMode.Warnings));

        var otherRuntime = BevProfileComparison.Compare(baseline, ExportIn("Server", ".NET 11.0.0", ("Grid", 10, 10 * 2048)));
        Assert.Contains("Recorded on different runtimes (.NET 10.0.0 → .NET 11.0.0): render counts are comparable, times and allocations are not.", Assert.ContainsSingle(otherRuntime.Warnings));
    }

    [TestMethod]
    public void Byte_counts_are_read_as_the_whole_numbers_they_were_written_as_and_added_up_from_components()
    {
        Assert.AreEqual(long.MaxValue - 1, BevRenderProfile.Parse(Export(("Grid", 1, long.MaxValue - 1))).Types.Single().AllocatedBytes, "past what a double holds exactly");
        Assert.IsNull(BevRenderProfile.Parse(Export(("Grid", 1, -5))).Types.Single().AllocatedBytes);

        // An export without a profile section: the components' figures, since the baseline where one was set.
        string Components(object[] items, string? baselineAt = null) => JsonSerializer.Serialize(new
        {
            format = "bit-bevtools/1",
            components = baselineAt is null ? (object)new { items } : new { baselineAt, items },
        });

        var whole = BevRenderProfile.Parse(Components([
            new { type = "App.Grid", displayName = "Grid", renders = 2, totalRenderMs = 1.0, allocatedBytes = 3000L },
            new { type = "App.Grid", displayName = "Grid", renders = 3, totalRenderMs = 1.0, allocatedBytes = 5000L },
        ]));
        Assert.AreEqual(8000, whole.Types.Single().AllocatedBytes);
        Assert.IsTrue(whole.FromComponents);

        var since = BevRenderProfile.Parse(Components([
            new { type = "App.Grid", displayName = "Grid", renders = 4, totalRenderMs = 1.0, allocatedBytes = 9000L, rendersSinceBaseline = 1, renderMsSinceBaseline = 0.5, allocatedBytesSinceBaseline = 700L },
            new { type = "App.Grid", displayName = "Grid", renders = 2, totalRenderMs = 1.0, allocatedBytes = 300L },
        ], DateTimeOffset.UtcNow.ToString("O")));
        Assert.AreEqual(1000, since.Types.Single().AllocatedBytes, "since the baseline, and all of a component's created after it");

        var unmeasured = BevRenderProfile.Parse(Components([
            new { type = "App.Grid", displayName = "Grid", renders = 2, totalRenderMs = 1.0 },
        ]));
        Assert.IsNull(unmeasured.Types.Single().AllocatedBytes, "not measured is not zero");
    }

    [TestMethod]
    public void A_render_that_allocated_more_than_a_sample_holds_saturates_rather_than_wrapping()
    {
        var sample = new BevRenderSample { Sequence = 1, At = DateTimeOffset.UtcNow, StartTicks = 0, AllocatedBytes = long.MaxValue, Failed = true };
        Assert.AreEqual(int.MaxValue - 1, sample.AllocatedBytes);
        Assert.IsTrue(sample.Failed);
        sample.AllocatedBytes = -3;
        Assert.AreEqual(0, sample.AllocatedBytes);
        sample.AllocatedBytes = null;
        Assert.IsNull(sample.AllocatedBytes);
        Assert.IsTrue(sample.Failed);
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
        Assert.StartsWith("AllocatingParent allocates", finding.Title);
        Assert.Contains("4 renders in the last 30 s", finding.Evidence);
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
        Assert.Contains(nameof(AllocatingParent), first.TextContent, "the most allocating first");
        Assert.Contains("per render", first.InnerHtml);
    }

    internal sealed class AllocatingParent : ComponentBase
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

    internal sealed class AllocatingChild : ComponentBase
    {
        [Parameter] public int Bytes { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, new byte[Bytes].Length);
    }
}
