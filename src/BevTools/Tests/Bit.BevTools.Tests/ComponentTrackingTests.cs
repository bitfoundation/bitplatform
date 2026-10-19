using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>End-to-end instrumentation tests through a real renderer (bUnit).</summary>
[TestClass]
public class ComponentTrackingTests : BunitContext
{
    private readonly FakeJSRuntime _js = new();

    public ComponentTrackingTests()
    {
        Services.AddSingleton<IJSRuntime>(_js);
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.CaptureComponentStateOnRender = true;
        });
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void Reflection_accessors_are_available_on_this_framework()
    {
        Assert.IsNull(BlazorReflection.InitializationError);
        Assert.IsTrue(BlazorReflection.CanWrapRenderFragment);
        Assert.IsTrue(BlazorReflection.CanResolveRenderer);
        Assert.IsTrue(BlazorReflection.CanResolveHierarchy);
        Assert.IsTrue(BlazorReflection.CanDetectBatches);
    }

    [TestMethod]
    public void Components_are_registered_with_hierarchy_and_render_counts()
    {
        var cut = Render<ParentComponent>(ps => ps.Add(p => p.Value, 1));
        Session.Components.Refresh(force: true);

        var parent = Session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        var child = Session.Components.All.Single(r => r.Type == typeof(ChildComponent));
        Assert.IsTrue(parent.IsInstrumented);
        Assert.AreEqual(1, parent.RenderCount);
        Assert.AreEqual(1, child.RenderCount);
        Assert.AreSame(parent, child.Parent);
        Assert.IsNotNull(parent.ComponentId);
        Assert.AreEqual(BevRenderCause.Initial, child.LastRender!.Cause);
        Assert.IsNotNull(Session.Dispatcher);
        Assert.IsTrue(parent.LastRender!.DurationMs >= 0);

        var tree = Session.Components.BuildTree(includeHidden: false);
        var root = Assert.ContainsSingle(tree);
        Assert.AreSame(parent, root.Record);
        Assert.ContainsSingle(root.Children);
        Assert.AreEqual(2, root.SubtreeRenders);
    }

    [TestMethod]
    public void Child_render_caused_by_parent_is_attributed_and_state_diff_captured()
    {
        var cut = Render<ParentComponent>(ps => ps.Add(p => p.Value, 1));
        cut.InvokeAsync(() => cut.Instance.Increment());

        Session.Components.Refresh(force: true);
        var parent = Session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        var child = Session.Components.All.Single(r => r.Type == typeof(ChildComponent));

        Assert.AreEqual(2, parent.RenderCount);
        Assert.AreEqual(2, child.RenderCount);
        Assert.AreEqual(BevRenderCause.StateHasChanged, parent.LastRender!.Cause);
        Assert.AreEqual(BevRenderCause.ParentRender, child.LastRender!.Cause);
        Assert.Contains("ParentComponent", child.LastRender.CauseDetail!);
        Assert.AreEqual(parent.LastRender.BatchId, child.LastRender.BatchId);
        Assert.Contains(s => s.StartsWith("_counter: 0 → 1"), parent.LastRender.ChangedState!);

        var childEvent = Session.Timeline.Find(child.LastRender.EventId)!;
        Assert.AreEqual(parent.LastRender.EventId, childEvent.ParentEventId);
    }

    [TestMethod]
    public void Batches_are_detected_and_closed()
    {
        var cut = Render<ParentComponent>();
        cut.InvokeAsync(() => cut.Instance.Refresh());
        cut.InvokeAsync(() => cut.Instance.Refresh());
        Session.RenderTracker.CloseBatch();

        var batches = Session.Timeline.Snapshot().Where(e => e.Kind == BevToolsEventKind.RenderBatch).ToList();
        Assert.AreEqual(3, batches.Count);
        Assert.Contains("2 components rendered", batches[0].Detail!); // initial: parent + child
        Assert.Contains("1 component rendered", batches[1].Detail!); // refresh: child parameters unchanged, only the parent renders
        foreach (var b in batches)
        {
            Assert.Contains("ms building", b.Detail!);
        }
        Assert.AreEqual(3, Session.RenderTracker.BatchCount);
    }

    [TestMethod]
    public void Disposed_components_are_detected_and_pruned_from_the_live_tree()
    {
        var cut = Render<ParentComponent>(ps => ps.Add(p => p.ShowChild, true));
        Session.Components.Refresh(force: true);
        Assert.ContainsSingle(r => r.Type == typeof(ChildComponent) && !r.IsDisposed, Session.Components.All);

        cut.Render(ps => ps.Add(p => p.ShowChild, false));
        Session.Components.Refresh(force: true);

        var child = Session.Components.All.Single(r => r.Type == typeof(ChildComponent));
        Assert.IsTrue(child.IsDisposed);
        Assert.DoesNotContain(n => n.Record.Type == typeof(ChildComponent), Session.Components.BuildTree(false)[0].Children);
        Assert.Contains(e => e.Kind == BevToolsEventKind.Lifecycle && e.Title == "ChildComponent disposed", Session.Timeline.Snapshot());
    }

    [TestMethod]
    public void Injected_js_runtime_is_wrapped_and_calls_are_attributed_to_the_component()
    {
        var cut = Render<ParentComponent>();
        var child = Session.Components.All.Single(r => r.Type == typeof(ChildComponent));
        var instance = Assert.IsExactInstanceOfType<ChildComponent>(child.Component);

        Assert.IsExactInstanceOfType<BevTrackingJSRuntime>(instance.JS);
        cut.InvokeAsync(async () => await instance.JS!.InvokeVoidAsync("hello"));

        var call = Assert.ContainsSingle(Session.Interop.Snapshot());
        Assert.AreEqual(child.InstanceId, call.ComponentInstanceId);
        Assert.AreEqual("ChildComponent", call.ComponentName);
        Assert.ContainsSingle(_js.Calls, "hello");
    }

    [TestMethod]
    public void Render_exceptions_are_captured_with_the_component()
    {
        Assert.Throws<Exception>(() => Render<ThrowingComponent>(ps => ps.Add(p => p.Throw, true)));

        var record = Session.Components.All.Single(r => r.Type == typeof(ThrowingComponent));
        Assert.AreEqual(1, record.ErrorCount);
        var error = Assert.ContainsSingle(e => e.Source == "render", Session.Errors.Snapshot());
        Assert.AreEqual(record.InstanceId, error.ComponentInstanceId);
        Assert.AreEqual("boom", error.Message);
        Assert.IsTrue(record.LastRender!.Failed);
    }

    [TestMethod]
    public void BevTools_own_components_are_excluded_from_app_statistics()
    {
        Render<ParentComponent>();
        Assert.DoesNotContain(n => n.Record.IsBevTools, Session.Components.BuildTree(false));
        Assert.IsTrue(Session.Overhead.AppRenderCount >= 2);
        Assert.AreEqual(0, Session.Overhead.BevToolsRenderCount);
    }

    [TestMethod]
    public void Ignored_components_are_not_tracked()
    {
        Services.GetRequiredService<BitBevToolsOptions>().IgnoreComponentPredicates.Add(t => t == typeof(ChildComponent));
        Render<ParentComponent>();
        Assert.DoesNotContain(r => r.Type == typeof(ChildComponent), Session.Components.All);
    }

    [TestMethod]
    public void Session_is_resolved_from_the_renderer_dispatcher()
    {
        var cut = Render<ParentComponent>();
        BevToolsSession? resolved = null;
        cut.InvokeAsync(() => resolved = SessionResolver.Resolve());
        Assert.AreSame(Session, resolved);
    }

    [TestMethod]
    public void Disabled_tracking_options_remove_optional_work_without_disabling_render_counts()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IJSRuntime>(new FakeJSRuntime());
        context.Services.AddBitBevToolsServices(options =>
        {
            options.Enabled = true;
            options.UseFrameworkInstrumentation = false;
            options.RecordRenderEvents = false;
            options.TrackRenderCauses = false;
            options.TrackJsInterop = false;
        });

        var cut = context.Render<ParentComponent>();
        cut.InvokeAsync(() => cut.Instance.Refresh());
        var session = context.Services.GetRequiredService<BevToolsSession>();
        var parent = session.Components.All.Single(record => record.Type == typeof(ParentComponent));
        var child = session.Components.All.Single(record => record.Type == typeof(ChildComponent));

        Assert.AreEqual(2, parent.RenderCount);
        Assert.AreEqual(BevRenderCause.Unknown, parent.LastRender!.Cause);
        Assert.DoesNotContain(e => e.Kind == BevToolsEventKind.Render, session.Timeline.Snapshot());
        Assert.IsNotExactInstanceOfType<BevTrackingJSRuntime>(((ChildComponent)child.Component!).JS);
    }
}
