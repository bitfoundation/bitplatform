using System.Diagnostics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// Which work a UI event or navigation is the cause of. A timer, background loop or fire-and-forget task started
/// inside one inherits its execution context, Activity.Current included, and keeps it after the trigger ended; what
/// that work renders later is not the trigger's doing. The renders the trigger itself produces still are, including
/// the ones the framework only processes after stopping its activity.
/// </summary>
[TestClass]
public class TriggerOwnershipTests : BunitContext
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    public TriggerOwnershipTests()
    {
        Services.AddBitBevToolsServices(o => o.Enabled = true);
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    private BevComponentRecord RecordOf<TComponent>() => Session.Components.All.Single(r => r.Type == typeof(TComponent));

    private BevToolsEvent BatchEventOf(BevRenderSample sample) =>
        Session.Timeline.Snapshot().Single(e => e.Kind == BevToolsEventKind.RenderBatch && e.Title == "Render batch #" + sample.BatchId);

    [TestMethod]
    public async Task Work_that_inherited_an_activity_after_it_stopped_is_not_attributed_to_it()
    {
        // The framework's own activities only exist on .NET 10; emitting one the same way keeps the rule covered on
        // every runtime: a render while the navigation runs is its own, a continuation that outlives it is not.
        var cut = Render<FireAndForget>();
        var record = RecordOf<FireAndForget>();
        using var source = new ActivitySource(ActivityObserver.SourceName);
        await cut.InvokeAsync(() =>
        {
            var activity = source.CreateActivity(ActivityObserver.NavigateName, ActivityKind.Internal)!;
            activity.DisplayName = "Route /later -> MyApp.Pages.Later";
            activity.Start();
            cut.Instance.Start();
            activity.Stop();
        });

        var navigation = Assert.ContainsSingle(e => e.Kind == BevToolsEventKind.Navigation, Session.Timeline.Snapshot());
        Assert.AreEqual(BevRenderCause.Navigation, record.LastRender!.Cause);
        Assert.AreEqual(navigation.Id, record.LastRender.TriggerEventId);

        cut.Instance.Gate.SetResult();
        await cut.Instance.Done.Task.WaitAsync(Patience);

        Assert.AreEqual(BevRenderCause.StateHasChanged, record.LastRender!.Cause);
        Assert.IsNull(record.LastRender.TriggerEventId);
    }

    // UI events and navigations are the framework's HandleEvent and Navigate activities, which .NET 10 introduced.
#if NET10_0_OR_GREATER
    [TestMethod]
    public async Task A_timer_started_by_a_click_is_not_attributed_to_that_click()
    {
        var cut = Render<TimerOnClick>();
        var record = RecordOf<TimerOnClick>();

        cut.Find("button").Click();

        var click = Assert.ContainsSingle(e => e.Kind == BevToolsEventKind.UiEvent, Session.Timeline.Snapshot());
        Assert.AreEqual(BevRenderCause.UiEvent, record.LastRender!.Cause);
        Assert.AreEqual(click.Id, record.LastRender.TriggerEventId);

        await cut.Instance.Ticked.Task.WaitAsync(Patience);

        var tick = record.LastRender!;
        Assert.AreEqual(BevRenderCause.StateHasChanged, tick.Cause);
        Assert.IsNull(tick.TriggerEventId);
        Assert.IsNull(BatchEventOf(tick).ParentEventId);
    }

    [TestMethod]
    public async Task A_fire_and_forget_continuation_of_a_click_is_not_attributed_to_that_click()
    {
        var cut = Render<FireAndForget>();
        var record = RecordOf<FireAndForget>();
        BevErrorRecord? error = null;
        cut.Instance.Continued = () => error = Session.Errors.Record(new InvalidOperationException("later"), "test");

        cut.Find("button").Click();

        var click = Assert.ContainsSingle(e => e.Kind == BevToolsEventKind.UiEvent, Session.Timeline.Snapshot());
        Assert.AreEqual(click.Id, record.LastRender!.TriggerEventId);

        cut.Instance.Gate.SetResult();
        await cut.Instance.Done.Task.WaitAsync(Patience);

        // Nothing the continuation does points at the click: its render, the component it shows, the error it raises.
        Assert.AreEqual(BevRenderCause.StateHasChanged, record.LastRender!.Cause);
        Assert.IsNull(record.LastRender.TriggerEventId);
        var child = RecordOf<ChildComponent>();
        Assert.IsNull(child.LastRender!.TriggerEventId);
        Assert.IsNull(Session.Timeline.Snapshot().Single(e => e.Kind == BevToolsEventKind.Lifecycle && e.ComponentInstanceId == child.InstanceId).ParentEventId);
        Assert.IsNull(error!.PrecedingEventId);
    }

    [TestMethod]
    public void A_click_owns_the_batch_that_renders_what_it_queued()
    {
        // The framework stops a synchronous handler's activity before it renders what the handler queued, so this
        // batch runs after the stop and still belongs to the click - the component it shows included.
        var cut = Render<FireAndForget>();
        var record = RecordOf<FireAndForget>();

        cut.Find("#show").Click();

        var click = Assert.ContainsSingle(e => e.Kind == BevToolsEventKind.UiEvent, Session.Timeline.Snapshot());
        Assert.AreEqual(BevRenderCause.UiEvent, record.LastRender!.Cause);
        Assert.AreEqual(click.Id, BatchEventOf(record.LastRender).ParentEventId);
        var child = RecordOf<ChildComponent>();
        Assert.AreEqual(click.Id, child.LastRender!.TriggerEventId);
        Assert.AreEqual(click.Id, Session.Timeline.Snapshot().Single(e => e.Kind == BevToolsEventKind.Lifecycle && e.ComponentInstanceId == child.InstanceId).ParentEventId);
    }

    [TestMethod]
    public async Task An_async_handler_owns_the_render_after_its_await()
    {
        var cut = Render<AsyncClick>();
        var record = RecordOf<AsyncClick>();

        cut.Find("button").Click();
        cut.Instance.Gate.SetResult();
        await cut.Instance.Completed.Task.WaitAsync(Patience);

        var click = Assert.ContainsSingle(e => e.Kind == BevToolsEventKind.UiEvent, Session.Timeline.Snapshot());
        Assert.AreEqual(BevRenderCause.UiEvent, record.LastRender!.Cause);
        Assert.AreEqual(click.Id, record.LastRender.TriggerEventId);
    }

    [TestMethod]
    public async Task A_timer_started_by_the_page_a_navigation_rendered_is_not_attributed_to_that_navigation()
    {
        // The demo's live clock: the timer starts in OnAfterRender of the page's first render, inside the Navigate
        // activity, and ticks long after it.
        RenderFragment<RouteData> found = routeData => builder =>
        {
            builder.OpenComponent<RouteView>(0);
            builder.AddComponentParameter(1, nameof(RouteView.RouteData), routeData);
            builder.AddComponentParameter(2, nameof(RouteView.DefaultLayout), typeof(NavigationLayout));
            builder.CloseComponent();
        };
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/bev-tests/start");
        var router = Render<Router>(ps => ps.Add(p => p.AppAssembly, typeof(ClockPage).Assembly).Add(p => p.Found, found));
        var layout = RecordOf<NavigationLayout>();

        navigationManager.NavigateTo("/bev-tests/clock");

        // The navigation's own renders are still the navigation's: the layout it re-rendered, the page it created.
        var navigations = Session.Timeline.Snapshot().Where(e => e.Kind == BevToolsEventKind.Navigation && e.Title.Contains("/bev-tests/clock", StringComparison.Ordinal)).Select(e => e.Id).ToArray();
        Assert.AreEqual(BevRenderCause.Navigation, layout.LastRender!.Cause);
        Assert.Contains(layout.LastRender.TriggerEventId!.Value, navigations);
        var page = RecordOf<ClockPage>();
        Assert.Contains(page.Renders[0].TriggerEventId!.Value, navigations);

        await router.FindComponent<ClockPage>().Instance.Ticked.Task.WaitAsync(Patience);

        var tick = page.LastRender!;
        Assert.AreEqual(BevRenderCause.StateHasChanged, tick.Cause);
        Assert.IsNull(tick.TriggerEventId);
        Assert.IsNull(BatchEventOf(tick).ParentEventId);
    }
#endif

    /// <summary>A live clock started by a button: the timer captures the execution context, and with it the click.</summary>
    public sealed class TimerOnClick : ComponentBase, IDisposable
    {
        private Timer? _timer;
        private int _ticks;

        public TaskCompletionSource Ticked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, Start));
            builder.AddContent(2, _ticks);
            builder.CloseElement();
        }

        private void Start() => _timer = new Timer(_ => InvokeAsync(() =>
        {
            _ticks++;
            StateHasChanged();
            Ticked.TrySetResult();
        }), null, 10, Timeout.Infinite);

        public void Dispose() => _timer?.Dispose();
    }

    /// <summary>Starts work it does not await, which refreshes the component once <see cref="Gate"/> opens.</summary>
    public sealed class FireAndForget : ComponentBase
    {
        private bool _showChild;
        private int _refreshes;

        public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Runs inside the continuation, after its render.</summary>
        public Action? Continued { get; set; }

        public void Start()
        {
            _ = RefreshLaterAsync();
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, Start));
            builder.AddContent(2, _refreshes);
            builder.CloseElement();
            builder.OpenElement(3, "button");
            builder.AddAttribute(4, "id", "show");
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => _showChild = true));
            builder.CloseElement();
            if (_showChild)
            {
                builder.OpenComponent<ChildComponent>(6);
                builder.AddComponentParameter(7, nameof(ChildComponent.Text), "shown");
                builder.CloseComponent();
            }
        }

        private async Task RefreshLaterAsync()
        {
            await Gate.Task;
            await InvokeAsync(() =>
            {
                _refreshes++;
                _showChild = true;
                StateHasChanged();
                Continued?.Invoke();
            });
            Done.TrySetResult();
        }
    }

    /// <summary>An awaited handler: the framework renders again when it completes, still inside the event.</summary>
    public sealed class AsyncClick : ComponentBase
    {
        private int _count;

        public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnClick));
            builder.AddContent(2, _count);
            builder.CloseElement();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (_count > 0)
            {
                Completed.TrySetResult();
            }
        }

        private async Task OnClick()
        {
            await Gate.Task;
            _count++;
        }
    }

    public sealed class NavigationLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "main");
            builder.AddContent(1, Body);
            builder.CloseElement();
        }
    }

    [Route("/bev-tests/start")]
    public sealed class StartPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "start");
    }

    /// <summary>The demo's LiveClock, reduced to one tick.</summary>
    [Route("/bev-tests/clock")]
    public sealed class ClockPage : ComponentBase, IDisposable
    {
        private Timer? _timer;
        private int _ticks;

        public TaskCompletionSource Ticked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, _ticks);

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                _timer = new Timer(_ => InvokeAsync(() =>
                {
                    _ticks++;
                    StateHasChanged();
                    Ticked.TrySetResult();
                }), null, 10, Timeout.Infinite);
            }
        }

        public void Dispose() => _timer?.Dispose();
    }
}
