using System.Diagnostics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// Batch bookkeeping and the framework diagnostics it depends on. These cover the cases that only show up in a real
/// host: measurements that arrive after the batch they describe, and a runtime where meters are compiled out.
/// Framework instrumentation is off here so the assertions are about BevTools' own logic, not about whichever
/// measurements the test renderer happened to emit.
/// </summary>
[TestClass]
public class BatchAttributionTests : BunitContext
{
    public BatchAttributionTests() => Services.AddBitBevToolsServices(o =>
    {
        o.Enabled = true;
        o.UseFrameworkInstrumentation = false;
    });

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void A_late_diff_measurement_is_matched_to_the_batch_it_belongs_to()
    {
        // The queue only holds batches while render_diff measurements are expected; on .NET 10 the first real batch
        // sets that, so state it here rather than depend on which test rendered through the framework meters first.
        ActivityObserver.MarkBatchMetricsObserved();
        var cut = Render<ParentComponent>();
        var tracker = Session.RenderTracker;

        // Blazor Server records the diff duration when the browser acknowledges the render, which can be after the
        // next batch has already started. Attributing it to whatever batch is current would report nonsense.
        var first = tracker.CurrentBatch!;
        tracker.CloseBatch();
        cut.InvokeAsync(() => cut.Instance.Refresh());
        var second = tracker.CurrentBatch!;
        Assert.AreNotSame(first, second);

        tracker.RecordBatchDiff(3.5);
        tracker.RecordBatchDiffSize(7);

        Assert.AreEqual(3.5, first.DiffMs);
        Assert.AreEqual(7, first.DiffSize);
        Assert.IsNull(second.DiffMs);
        var detail = Session.Timeline.Find(first.EventId)!.Detail!;
        Assert.Contains("3.50 ms diffing", detail, StringComparison.Ordinal);
        Assert.Contains("7 DOM edits", detail, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_prompt_diff_measurement_closes_the_current_batch()
    {
        Render<ParentComponent>();
        var tracker = Session.RenderTracker;
        var batch = tracker.CurrentBatch!;

        tracker.RecordBatchDiff(1.25);

        Assert.IsTrue(batch.Closed);
        Assert.AreEqual(1.25, batch.DiffMs);
    }

    [TestMethod]
    public void Renders_never_join_a_batch_that_was_already_closed()
    {
        var cut = Render<ParentComponent>();
        var tracker = Session.RenderTracker;
        var first = tracker.CurrentBatch!;
        tracker.CloseBatch();
        var counted = first.Count;

        cut.InvokeAsync(() => cut.Instance.Refresh());

        Assert.AreNotSame(first, tracker.CurrentBatch);
        Assert.AreEqual(counted, first.Count);
    }

    [TestMethod]
    public void Queue_overflow_disables_attribution_instead_of_shifting_measurements()
    {
        ActivityObserver.MarkBatchMetricsObserved();
        using var context = new BunitContext();
        context.Services.AddBitBevToolsServices(options =>
        {
            options.Enabled = true;
            options.UseFrameworkInstrumentation = false;
            options.MaxEvents = 4;
        });
        var session = context.Services.GetRequiredService<BevToolsSession>();
        var cut = context.Render<ParentComponent>();
        var batches = new List<BevRenderBatchInfo>();

        for (var i = 0; i < 6; i++)
        {
            batches.Add(session.RenderTracker.CurrentBatch!);
            session.RenderTracker.CloseBatch();
            if (i < 5)
            {
                cut.InvokeAsync(() => cut.Instance.Refresh());
            }
        }

        session.RenderTracker.RecordBatchDiff(1);
        session.RenderTracker.RecordBatchDiffSize(10);
        session.RenderTracker.RecordBatchDiff(2);
        session.RenderTracker.RecordBatchDiffSize(20);
        session.RenderTracker.RecordBatchDiff(3);
        session.RenderTracker.RecordBatchDiffSize(30);

        Assert.IsTrue(session.RenderTracker.DiffAttributionLost);
        foreach (var batch in batches)
        {
            Assert.IsNull(batch.DiffMs);
            Assert.IsNull(batch.DiffSize);
        }
        Assert.Contains(e => e.Title == "Render-diff attribution disabled", session.Timeline.Snapshot());
    }

    [TestMethod]
    public void Devtools_own_interactions_are_not_recorded_as_application_events()
    {
        Render<PanelLikeComponent>();
        var before = Session.Timeline.Snapshot().Count(e => e.Kind == BevToolsEventKind.UiEvent);

        Emit("Event onclick -> Bit.BevTools.BevTimelinePanel.Select");
        Emit("Event onclick -> Bit.BevTools.BevToolsPanel.Toggle");
        Assert.AreEqual(before, Session.Timeline.Snapshot().Count(e => e.Kind == BevToolsEventKind.UiEvent));

        // An application namespace that merely starts with the same word is still application activity.
        Emit("Event onclick -> Bit.BevTools.Demo.Client.Pages.Home.Go");
        Assert.AreEqual(before + 1, Session.Timeline.Snapshot().Count(e => e.Kind == BevToolsEventKind.UiEvent));
    }

    [TestMethod]
    public void An_exception_logged_after_its_event_handler_failed_points_at_that_event()
    {
        Render<ParentComponent>();
        var eventId = EmitFailure("Event onclick -> MyApp.Pages.Errors.ThrowInHandler", typeof(InvalidOperationException).FullName!);

        // The framework logs the handler's exception only after it has stopped the event's activity.
        var logged = Session.Errors.Record(new InvalidOperationException("Handler failure"), "blazor");
        var later = Session.Errors.Record(new InvalidOperationException("Unrelated failure"), "blazor");

        Assert.AreEqual(eventId, logged.PrecedingEventId);
        Assert.IsNull(later.PrecedingEventId);
    }

    [TestMethod]
    public void A_failed_event_is_not_claimed_by_an_exception_of_another_type()
    {
        Render<ParentComponent>();
        EmitFailure("Event onclick -> MyApp.Pages.Errors.ThrowAsync", typeof(TimeoutException).FullName!);

        var logged = Session.Errors.Record(new InvalidOperationException("Something else"), "blazor");

        Assert.IsNull(logged.PrecedingEventId);
    }

    /// <summary>Mimics ComponentsActivitySource failing an event: error.type tag and error status, set when stopping.</summary>
    private long EmitFailure(string displayName, string errorType)
    {
        using var source = new ActivitySource(ActivityObserver.SourceName);
        var activity = source.CreateActivity(ActivityObserver.HandleEventName, ActivityKind.Client);
        if (activity is null)
        {
            Assert.Fail("no listener is attached to the components ActivitySource");
        }

        activity.DisplayName = displayName;
        activity.Start();
        activity.SetTag("error.type", errorType);
        activity.SetStatus(ActivityStatusCode.Error);
        activity.Stop();
        return Session.Timeline.Snapshot().Last(e => e.Kind == BevToolsEventKind.UiEvent).Id;
    }

    /// <summary>Mimics ComponentsActivitySource: display name set before the activity starts, tags only on stop.</summary>
    private static void Emit(string displayName, string operationName = ActivityObserver.HandleEventName)
    {
        using var source = new ActivitySource(ActivityObserver.SourceName);
        var activity = source.CreateActivity(operationName, ActivityKind.Client);
        if (activity is null)
        {
            Assert.Fail("no listener is attached to the components ActivitySource");
        }

        activity.DisplayName = displayName;
        activity.Start();
        activity.Stop();
    }

    [TestMethod]
    [DataRow("Event onclick -> MyApp.Pages.Counter.IncrementCount", true, "MyApp.Pages.Counter", "IncrementCount", "onclick")]
    [DataRow("Event oninput -> Lib.Filter.OnQuery", true, "Lib.Filter", "OnQuery", "oninput")]
    [DataRow("Route /counter -> MyApp.Pages.Counter", false, "MyApp.Pages.Counter", null, "/counter")]
    [DataRow("something else", true, null, null, null)]
    public void Display_names_are_parsed_back_into_their_parts(string displayName, bool hasMethod, string? type, string? method, string? attribute)
    {
        var parsed = ActivityObserver.ParseDisplayName(displayName, hasMethod);

        Assert.AreEqual(type, parsed.ComponentType);
        Assert.AreEqual(method, parsed.Method);
        Assert.AreEqual(attribute, parsed.Attribute);
    }

    [TestMethod]
    public void Navigating_to_an_application_page_under_the_product_namespace_is_recorded()
    {
        // A navigation names the page type with no method after it. Cutting one off anyway turned
        // Bit.BevTools.Shop.Counter into "Bit.BevTools.Shop", which looks like BevTools' own namespace.
        Render<PanelLikeComponent>();

        Emit("Route /counter -> Bit.BevTools.Shop.Counter", ActivityObserver.NavigateName);

        var navigation = Assert.ContainsSingle(e => e.Kind == BevToolsEventKind.Navigation, Session.Timeline.Snapshot());
        Assert.AreEqual("Counter", navigation.ComponentName);
    }

    [TestMethod]
    public void A_batch_of_only_bevtools_renders_does_not_take_the_diff_of_a_closed_application_batch()
    {
        // WebAssembly: each measurement arrives while its batch is still open. The panel's own refresh is a batch
        // too; with no entry of its own, its measurement used to overwrite the application batch before it.
        ActivityObserver.MarkBatchMetricsObserved();
        Render<ParentComponent>();
        var tracker = Session.RenderTracker;
        var application = tracker.CurrentBatch!;
        tracker.RecordBatchDiff(3);
        tracker.RecordBatchDiffSize(30);

        Render<BevInspectorSection>(ps => ps.Add(p => p.Title, "own"));
        var own = tracker.CurrentBatch!;
        Assert.AreNotSame(application, own);
        Assert.IsFalse(own.IsTracked);
        tracker.RecordBatchDiff(9);
        tracker.RecordBatchDiffSize(90);

        Assert.AreEqual(3, application.DiffMs);
        Assert.AreEqual(30, application.DiffSize);
        Assert.Contains("3.00 ms diffing", Session.Timeline.Find(application.EventId)!.Detail!, StringComparison.Ordinal);
        Assert.DoesNotContain(e => e.Kind == BevToolsEventKind.RenderBatch && e.Id > application.EventId, Session.Timeline.Snapshot());
    }

    [TestMethod]
    public void Late_diff_measurements_stay_aligned_across_batches_bevtools_does_not_record()
    {
        // Blazor Server: measurements arrive with the browser's acknowledgement, after later batches started. An
        // unrecorded batch in between still produces one, which used to be handed to the next application batch,
        // shifting every later attribution by one.
        ActivityObserver.MarkBatchMetricsObserved();
        var cut = Render<ParentComponent>();
        var tracker = Session.RenderTracker;
        var first = tracker.CurrentBatch!;
        tracker.CloseBatch();

        Render<BevInspectorSection>(ps => ps.Add(p => p.Title, "own"));
        var own = tracker.CurrentBatch!;
        tracker.CloseBatch();

        Session.PauseCapture();
        cut.InvokeAsync(() => cut.Instance.Refresh());
        var paused = tracker.CurrentBatch!;
        tracker.CloseBatch();
        Session.ResumeCapture();

        cut.InvokeAsync(() => cut.Instance.Refresh());
        var second = tracker.CurrentBatch!;
        Assert.IsFalse(own.IsTracked);
        Assert.IsFalse(paused.IsTracked);
        Assert.IsTrue(second.IsTracked);

        tracker.RecordBatchDiff(1);
        tracker.RecordBatchDiff(2);
        tracker.RecordBatchDiff(3);
        tracker.RecordBatchDiff(4);

        Assert.AreEqual(1, first.DiffMs);
        Assert.AreEqual(4, second.DiffMs);
        Assert.Contains("4.00 ms diffing", Session.Timeline.Find(second.EventId)!.Detail!, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Missing_metrics_support_is_explained_instead_of_being_reported_as_pending()
    {
        Assert.IsTrue(CapabilityReport.MetricsAreSupported(), "the test host supports metrics");
        Assert.DoesNotContain("MetricsSupport", CapabilityReport.MetricsHint(metricsSupported: true), StringComparison.Ordinal);

        AppContext.SetSwitch("System.Diagnostics.Metrics.Meter.IsSupported", false);
        try
        {
            Assert.IsFalse(CapabilityReport.MetricsAreSupported());
            Assert.Contains("MetricsSupport", CapabilityReport.MetricsHint(metricsSupported: false), StringComparison.Ordinal);
        }
        finally
        {
            AppContext.SetSwitch("System.Diagnostics.Metrics.Meter.IsSupported", true);
        }
    }
}

public class PanelLikeComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        builder.CloseElement();
    }
}
