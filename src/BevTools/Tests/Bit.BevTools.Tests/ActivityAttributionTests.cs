using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// Covers the framework diagnostics BevTools depends on for its headline question ("why did this render?").
/// These assertions pin the exact activity and tag names of Microsoft.AspNetCore.Components: getting one of them
/// wrong silently degrades every render cause to "StateHasChanged" and every event title to "?.?", which is
/// exactly the failure mode a purely internal test suite cannot see.
/// </summary>
[TestClass]
public class ActivityAttributionTests : BunitContext
{
    public ActivityAttributionTests()
    {
        Services.AddBitBevToolsServices(o => o.Enabled = true);
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    // The framework's ActivitySource and meters arrived in .NET 10; on earlier runtimes there is nothing to observe,
    // and the report has to say so instead of claiming the data is merely pending.
#if NET10_0_OR_GREATER
    [TestMethod]
    public void AddBitBevToolsServices_registers_the_framework_activity_source_and_meters()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServices(o => o.Enabled = true);
        Assert.Contains(d => d.ServiceType.FullName == "Microsoft.AspNetCore.Components.ComponentsActivitySource", services);
        Assert.Contains(d => d.ServiceType.FullName == "Microsoft.AspNetCore.Components.ComponentsMetrics", services);
    }
#else
    [TestMethod]
    public void Before_net10_nothing_is_registered_and_the_capability_report_says_why()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServices(o => o.Enabled = true);
        Assert.DoesNotContain(d => d.ServiceType.FullName == "Microsoft.AspNetCore.Components.ComponentsActivitySource", services);

        Render<ClickableComponent>();
        var uiEvents = Session.Capabilities.Single(c => c.Name == "Render cause: UI event");
        Assert.AreEqual(BevCapabilityStatus.RequiresRuntimeSupport, uiEvents.Status);
        Assert.StartsWith("Requires .NET 10", uiEvents.Explanation, StringComparison.Ordinal);
    }
#endif

    [TestMethod]
    public void Framework_instrumentation_can_be_turned_off()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.UseFrameworkInstrumentation = false;
        });
        Assert.DoesNotContain(d => d.ServiceType.FullName == "Microsoft.AspNetCore.Components.ComponentsActivitySource", services);
    }

#if NET10_0_OR_GREATER
    [TestMethod]
    public void Ui_event_is_recorded_with_component_type_attribute_and_method()
    {
        var cut = Render<ClickableComponent>();
        cut.Find("button").Click();

        var uiEvent = Assert.ContainsSingle(e => e.Kind == BevToolsEventKind.UiEvent, Session.Timeline.Snapshot());

        // Regression guard: the tag keys are aspnetcore.components.type / code.function.name /
        // aspnetcore.components.attribute.name, and the framework's own DisplayName already reads well.
        Assert.Contains("onclick", uiEvent.Title, StringComparison.Ordinal);
        Assert.Contains("ClickableComponent", uiEvent.Title, StringComparison.Ordinal);
        Assert.Contains("OnClick", uiEvent.Title, StringComparison.Ordinal);
        Assert.AreEqual("ClickableComponent", uiEvent.ComponentName);
        Assert.DoesNotContain("?.?", uiEvent.Title, StringComparison.Ordinal);
        Assert.IsNotNull(uiEvent.DurationMs);
        Assert.AreEqual("completed", uiEvent.Detail);
        Assert.IsTrue(ActivityObserver.HandleEventActivitiesObserved);
    }

    [TestMethod]
    public void Render_caused_by_a_click_is_attributed_to_that_click()
    {
        var cut = Render<ClickableComponent>();
        cut.Find("button").Click();

        var record = Session.Components.All.Single(r => r.Type == typeof(ClickableComponent));
        var render = record.LastRender!;
        Assert.AreEqual(BevRenderCause.UiEvent, render.Cause);

        var uiEvent = Session.Timeline.Snapshot().Single(e => e.Kind == BevToolsEventKind.UiEvent);
        Assert.AreEqual(uiEvent.Id, render.TriggerEventId);
        Assert.AreEqual(uiEvent.Id, Session.Timeline.Find(render.EventId)!.ParentEventId);
    }

    [TestMethod]
    public void Event_handler_durations_are_aggregated_per_component_type()
    {
        var cut = Render<ClickableComponent>();
        cut.Find("button").Click();
        cut.Find("button").Click();

        Assert.IsTrue(Session.TypeMetrics.TryGetValue("ClickableComponent", out var metrics));
        Assert.AreEqual(2, metrics!.EventHandlers);
    }

    [TestMethod]
    public void Render_diff_metrics_close_the_batch_with_a_measured_diff()
    {
        Render<ClickableComponent>();

        var batch = Session.Timeline.Snapshot().First(e => e.Kind == BevToolsEventKind.RenderBatch);
        Assert.IsTrue(ActivityObserver.BatchMetricsObserved, "aspnetcore.components.render_diff.* must be observed");
        Assert.Contains("ms diffing", batch.Detail!);
        Assert.Contains("DOM edits", batch.Detail!);
    }

    [TestMethod]
    public void Parameter_update_metrics_are_recorded_per_component_type()
    {
        Render<ClickableComponent>();

        Assert.IsTrue(ActivityObserver.LifecycleMetricsObserved);
        Assert.IsTrue(Session.TypeMetrics.TryGetValue("ClickableComponent", out var metrics));
        Assert.IsTrue(metrics!.ParameterUpdates > 0);
    }
#endif
}

public class ClickableComponent : ComponentBase
{
    private int _count;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnClick));
        builder.AddContent(2, _count);
        builder.CloseElement();
    }

    private void OnClick() => _count++;
}
