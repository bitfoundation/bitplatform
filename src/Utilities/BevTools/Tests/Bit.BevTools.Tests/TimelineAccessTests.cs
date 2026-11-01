using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// Exports may show no more than the panels. Below Full they write a render's changed fields item by item and any other
/// list in an event's data as its summary (ValueSafetyTests), so the timeline panel must list them the same way.
/// </summary>
[TestClass]
public class TimelineAccessTests : BunitContext
{
    public TimelineAccessTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
            o.ValueAccess = BevValueAccess.Shallow;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public async Task Below_Full_the_timeline_lists_the_changes_a_render_captured_and_summarizes_other_lists()
    {
        var cut = Render<BevToolsPanel>();
        var sample = new BevRenderSample { Sequence = 1, At = DateTimeOffset.UtcNow, StartTicks = System.Diagnostics.Stopwatch.GetTimestamp(), ChangedState = ["_counter: 0 → 1"] };
        var render = Session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Render, Category = "render", Title = "Counter", DurationMs = 1, Data = new RenderEventData(sample, 2) });
        var order = Session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.StateChange, Category = "orders", Title = "Order placed", Data = new Dictionary<string, object?> { ["items"] = new List<string> { "opaque-item" } } });

        await cut.InvokeAsync(() => cut.Instance.SelectEvent(render));
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find(".bev-kv").TextContent, "changedState: _counter: 0 → 1"));

        await cut.InvokeAsync(() => cut.Instance.SelectEvent(order));
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find(".bev-kv").TextContent, "items:"));
        Assert.DoesNotContain("opaque-item", cut.Markup, StringComparison.Ordinal);
    }
}
