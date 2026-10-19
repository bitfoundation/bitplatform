using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// <see cref="BmotionReorderGroup{TItem}"/> when its drag-start measurement is slow - always the
/// case on Blazor Server, where every measurement is a round trip: pointer moves that arrive
/// before the rows have been measured have nothing to work with and are skipped (rather than
/// throwing), and the release waits for the measurement, then catches the order up with where
/// the row was let go.
/// </summary>
[TestClass]
public class ReorderMeasurementTests
{
    private const double RowHeight = 50;

    private static readonly RenderFragment<string> Row = item => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "row");
        b.AddContent(2, item);
        b.CloseElement();
    };

    [TestMethod]
    public async Task MovesBeforeTheMeasurementLands_AreSkipped_AndTheReleaseCatchesUp()
    {
        using var ctx = new BmotionTestContext();
        List<string>? committed = null;
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b", "c" })
            .Add(p => p.ItemsChanged, EventCallback.Factory.Create<List<string>>(this, l => committed = l))
            .Add(p => p.ChildContent, Row));
        var ids = cut.FindAll("div.row").Select(d => d.Id!).ToArray();

        // Rows stacked 50px apart, measured the way getBoundingClientRect does - transform included,
        // so a row that has already been dragged reports where it is now.
        ctx.Interop.BoundingRectProvider = id =>
        {
            int index = Array.IndexOf(ids, id);
            if (index < 0) return null;
            double dragged = ctx.Engine.GetState(id)?.Transforms.GetValueOrDefault("y") ?? 0;
            return new BmotionBoundingRect { Left = 0, Top = (index * RowHeight) + dragged, Width = 200, Height = RowHeight };
        };
        var measurement = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ctx.Interop.BoundingRectGate = measurement.Task;

        // Press on "a": the drag starts measuring, and the measurement is still out.
        var row = (Bmotion)ctx.Interop.EventListenerRefs[ids[0]];
        var press = cut.InvokeAsync(row.OnPointerDown_Drag);
        Assert.IsFalse(press.IsCompleted, "the drag start awaits its measurement");

        // A move past b's midpoint arrives first: skipped, not a KeyNotFoundException.
        await cut.InvokeAsync(() => row.SetDragPosition(0, 60));
        await cut.InvokeAsync(row.OnDragMove);
        Assert.IsNull(committed);

        // The release arrives before the measurement too: it has to wait for it.
        var release = cut.InvokeAsync(() => row.OnPointerUp_Drag(0, 0));
        Assert.IsFalse(release.IsCompleted, "the release waits for the measurement");

        await cut.InvokeAsync(measurement.SetResult);
        await ctx.RunFramesAsync(0, 3000);
        await press.WaitAsync(TimeSpan.FromSeconds(10));
        await release.WaitAsync(TimeSpan.FromSeconds(10));

        // "a" (centre 25) was let go 60px down (centre 85, past b's 75): the order catches up.
        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "b", "a", "c" }, committed));
        Assert.IsFalse(ctx.Renderer.UnhandledException.IsCompleted, "nothing may throw along the way");
    }
}
