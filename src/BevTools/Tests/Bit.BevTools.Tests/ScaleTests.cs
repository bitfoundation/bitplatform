using System.Diagnostics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// Behaviour at the sizes real applications reach: thousands of components, deep and recursive trees, render storms
/// and long-running churn. A developer tool that falls over on a large app is worse than no tool, because it fails
/// exactly when it is needed.
/// </summary>
[TestClass]
public class ScaleTests : BunitContext
{
    public ScaleTests() => Services.AddBitBevToolsServices(o => o.Enabled = true);

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void A_thousand_components_are_tracked_and_the_tree_builds_quickly()
    {
        Render<WideTree>(ps => ps.Add(p => p.Count, 1000));
        Session.Components.Refresh(force: true);

        Assert.IsTrue(Session.Components.LiveCount >= 1000, $"tracked only {Session.Components.LiveCount}");

        var start = Stopwatch.GetTimestamp();
        var tree = Session.Components.BuildTree(includeHidden: false);
        var elapsed = Stopwatch.GetElapsedTime(start);

        var root = Assert.ContainsSingle(tree);
        Assert.AreEqual(1000, root.Children.Count);
        Assert.AreEqual(1001, root.SubtreeRenders);
        Assert.IsTrue(elapsed.TotalMilliseconds < 500, $"building a 1000-node tree took {elapsed.TotalMilliseconds:0} ms");
    }

    [TestMethod]
    public void A_deeply_recursive_component_tree_is_tracked_correctly()
    {
        // Recursive components are common (menus, org charts, file trees) and are exactly what a developer opens a
        // component tree to understand. 1000 levels is the practical ceiling of the test renderer, not of BevTools:
        // bUnit's Htmlizer walks the frame tree recursively and overflows well before the tracker does.
        Render<DeepTree>(ps => ps.Add(p => p.Depth, 1000));
        Session.Components.Refresh(force: true);

        var tree = Session.Components.BuildTree(includeHidden: false);
        var root = Assert.ContainsSingle(tree);
        Assert.AreEqual(1001, root.SubtreeRenders);

        var depth = 0;
        var node = root;
        while (node.Children.Count > 0)
        {
            node = node.Children[0];
            depth++;
        }

        Assert.AreEqual(1000, depth);
        Assert.AreEqual(1000, node.Depth);
    }

    [TestMethod]
    public void Tree_aggregation_handles_a_chain_far_deeper_than_any_call_stack()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.MaxTrackedComponents = 60_000);
        using (scope)
        {
            // BevTools holds components weakly, so the test has to keep them alive itself.
            var alive = new List<ChildComponent>(50_000);
            BevComponentRecord? parent = null;
            for (var i = 0; i < 50_000; i++)
            {
                var component = new ChildComponent();
                alive.Add(component);
                var record = session.Components.Register(component, typeof(ChildComponent), isBevTools: false, isHidden: false)!;
                record.ComponentId = i;
                record.Parent = parent;
                record.ParentResolved = true;
                record.AddRender(new BevRenderSample { Sequence = i, At = DateTimeOffset.UtcNow, StartTicks = Stopwatch.GetTimestamp() });
                parent = record;
            }

            var root = Assert.ContainsSingle(session.Components.BuildTree(includeHidden: false));
            Assert.AreEqual(50_000, root.SubtreeRenders);
            GC.KeepAlive(alive);
        }
    }

    [TestMethod]
    public void A_render_storm_stays_bounded_and_is_reported_as_a_diagnostic()
    {
        var cut = Render<ParentComponent>();
        for (var i = 0; i < 3000; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Refresh());
        }

        Session.RenderTracker.CloseBatch();
        Session.Diagnostics.Evaluate(force: true);

        var parent = Session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        Assert.AreEqual(3001, parent.RenderCount);
        Assert.IsTrue(parent.Renders.Length <= Session.Options.MaxRendersPerComponent);
        Assert.IsTrue(Session.Timeline.Count <= Session.Options.MaxEvents);
        Assert.Contains(f => f.Diagnostic.RuleId == "render.storm", Session.Diagnostics.Findings);
    }

    [TestMethod]
    public void Timeline_lookups_stay_fast_when_the_buffer_is_full()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.MaxEvents = 10_000);
        using (scope)
        {
            long lastId = 0;
            for (var i = 0; i < 20_000; i++)
            {
                lastId = session.Timeline.Record(new BevToolsEvent { Title = "e" + i });
            }

            Assert.AreEqual(10_000, session.Timeline.Count);
            Assert.IsNull(session.Timeline.Find(1)); // evicted

            var start = Stopwatch.GetTimestamp();
            for (var i = 0; i < 20_000; i++)
            {
                Assert.IsNotNull(session.Timeline.Find(lastId - (i % 10_000)));
            }

            var elapsed = Stopwatch.GetElapsedTime(start);
            Assert.IsTrue(elapsed.TotalMilliseconds < 1000, $"20k lookups over a full buffer took {elapsed.TotalMilliseconds:0} ms");
        }
    }

    [TestMethod]
    public void Repeated_creation_and_destruction_does_not_accumulate_records()
    {
        var cut = Render<Churn>(ps => ps.Add(p => p.Count, 50));
        for (var i = 0; i < 40; i++)
        {
            var count = i % 2 == 0 ? 0 : 50;
            cut.Render(ps => ps.Add(p => p.Count, count));
        }

        Session.Components.Refresh(force: true);

        // 40 rounds x 50 children would be 2000 records if nothing were ever released.
        Assert.IsTrue(Session.Components.LiveCount <= 51, $"{Session.Components.LiveCount} components still counted as live");
        foreach (var r in Session.Components.All.Where(r => r.IsDisposed))
        {
            Assert.IsNull(r.Component);
        }
    }

    [TestMethod]
    public void Disposed_records_are_dropped_once_their_retention_window_passes()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.DisposedComponentRetentionSeconds = 0);
        using (scope)
        {
            var record = session.Components.Register(new ChildComponent(), typeof(ChildComponent), isBevTools: false, isHidden: false)!;
            record.IsDisposed = true;
            record.DisposedAt = DateTimeOffset.UtcNow.AddSeconds(-1);

            session.Components.Refresh(force: true);

            Assert.AreEqual(0, session.Components.TrackedCount);
        }
    }
}

public class WideTree : ComponentBase
{
    [Parameter] public int Count { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        for (var i = 0; i < Count; i++)
        {
            builder.OpenComponent<ChildComponent>(i);
            builder.AddComponentParameter(Count + i, nameof(ChildComponent.Text), "n" + i);
            builder.CloseComponent();
        }
    }
}

public class DeepTree : ComponentBase
{
    [Parameter] public int Depth { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Depth <= 0)
        {
            builder.AddContent(0, "leaf");
            return;
        }

        builder.OpenComponent<DeepTree>(1);
        builder.AddComponentParameter(2, nameof(Depth), Depth - 1);
        builder.CloseComponent();
    }
}

public class Churn : ComponentBase
{
    [Parameter] public int Count { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        for (var i = 0; i < Count; i++)
        {
            builder.OpenComponent<ChildComponent>(i);
            builder.SetKey(Guid.NewGuid());
            builder.AddComponentParameter(1000 + i, nameof(ChildComponent.Text), "c" + i);
            builder.CloseComponent();
        }
    }
}
