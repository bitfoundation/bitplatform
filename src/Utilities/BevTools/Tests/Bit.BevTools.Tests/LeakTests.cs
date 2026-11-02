using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Bit.BevTools.Tests;

/// <summary>
/// Disposed components that garbage collection does not reclaim, and what holds them: the leaks a Blazor app actually
/// has. Each test removes a component, forces the collection a developer would ask for, and checks both the verdict and
/// the reference named as its cause.
/// <para>
/// They run on a plain renderer, not bUnit: a bUnit context keeps every component it rendered alive until it is
/// disposed, which would make every removed component look retained, and every retention look explained.
/// </para>
/// </summary>
[TestClass]
public sealed class LeakTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly PlainRenderer _renderer;
    private int _lastRoot;

    public LeakTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<LeakyEvents>();
        services.AddTransient<TransientEvents>();
        services.AddSingleton<IJSRuntime>(new TrackingTestJSRuntime());
        services.AddOptions();
        services.Configure<LeakOptions>(_ => { });
        services.AddBitBevToolsServices(o => o.Enabled = true);
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        Session.EnsureActivated();
        Session.Leaks.ForcedGracePeriod = TimeSpan.Zero;
        _renderer = new PlainRenderer(_scope.ServiceProvider);
    }

    private BevToolsSession Session => _scope.ServiceProvider.GetRequiredService<BevToolsSession>();

    public void Dispose()
    {
        _renderer.Dispose();
        _scope.Dispose();
        _provider.Dispose();
    }

    /// <summary>Renders a component of <typeparamref name="T"/>, removes it, and returns what the forced check reports.</summary>
    private BevRetainedComponents[] RenderRemoveAndCheck<T>() where T : IComponent
    {
        RenderAndRemove(typeof(T));
        RenderParentAgain();
        return Session.Leaks.CollectAndCheck();
    }

    // The parent's previous render tree holds a removed child until the parent renders again; until then nothing can
    // be judged. In an app that is the next render of the page or layout the child was in.
    private void RenderParentAgain() => _renderer.SetRootParameters(_lastRoot, new Dictionary<string, object?> { [nameof(LeakHost.Child)] = null });

    // Not inlined, so nothing the render left on this frame's stack can keep the component alive.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private void RenderAndRemove(Type child)
    {
        var root = _lastRoot = _renderer.RenderRoot<LeakHost>(new Dictionary<string, object?> { [nameof(LeakHost.Child)] = child });
        Session.Components.Refresh(force: true);
        _renderer.SetRootParameters(root, new Dictionary<string, object?> { [nameof(LeakHost.Child)] = null });
        Session.Components.Refresh(force: true);
    }

    [TestMethod]
    public void A_component_that_unsubscribes_in_Dispose_is_reclaimed()
    {
        var retained = RenderRemoveAndCheck<TidySubscriber>();
        Assert.IsEmpty(retained, string.Join(", ", retained.Select(r => r.DisplayName)));
        Assert.AreEqual(0, _scope.ServiceProvider.GetRequiredService<LeakyEvents>().Subscribers);
    }

    [TestMethod]
    public void A_component_only_its_parents_previous_render_tree_holds_is_not_called_leaked()
    {
        // Measured, not assumed: Blazor keeps a removed child in the parent's previous render tree (for diffing) until
        // the parent renders again, with or without BevTools. That is alive, and it is not a leak.
        RenderAndRemove(typeof(WatchedTidySubscriber));
        Assert.IsEmpty(Session.Leaks.CollectAndCheck());
        Assert.IsTrue(WatchedTidySubscriber.Last!.IsAlive, "the renderer still holds it, so this test proves the exclusion");

        _renderer.SetRootParameters(_lastRoot, new Dictionary<string, object?> { [nameof(LeakHost.Child)] = typeof(DomMapLeaf) });
        Assert.IsEmpty(Session.Leaks.CollectAndCheck());
        Assert.IsFalse(WatchedTidySubscriber.Last!.IsAlive, "the parent's next render released it");
    }

    [TestMethod]
    public void A_handler_left_on_a_singleton_keeps_the_component_and_is_named()
    {
        var group = Assert.ContainsSingle(RenderRemoveAndCheck<LeakySubscriber>());
        Assert.AreEqual("LeakySubscriber", group.DisplayName);
        Assert.AreEqual(1, group.Count);
        var cause = Assert.ContainsSingle(group.Causes);
        Assert.AreEqual("LeakyEvents (injected) → Changed", cause.Holder);
        Assert.AreEqual("handler LeakySubscriber.OnChanged", cause.Detail);
        StringAssert.Contains(cause.Suggestion, "Unsubscribe");
    }

    [TestMethod]
    public void A_lambda_that_captures_the_component_is_named_by_the_method_declaring_it()
    {
        var group = Assert.ContainsSingle(RenderRemoveAndCheck<LambdaSubscriber>());
        var cause = Assert.ContainsSingle(group.Causes);
        Assert.AreEqual("LeakyEvents (injected) → Changed", cause.Holder);
        Assert.AreEqual("lambda in LambdaSubscriber.OnInitialized, which captures the component", cause.Detail);
    }

    [TestMethod]
    public void An_undisposed_DotNetObjectReference_keeps_the_component_through_the_JS_runtime()
    {
        var group = Assert.ContainsSingle(RenderRemoveAndCheck<ReferenceHolder>());
        var cause = Assert.ContainsSingle(group.Causes);
        Assert.AreEqual("the JS runtime", cause.Holder);
        StringAssert.Contains(cause.Detail, "DotNetObjectReference<ReferenceHolder>");
        StringAssert.Contains(cause.Suggestion, "Dispose the DotNetObjectReference");
    }

    [TestMethod]
    public void Timers_that_were_never_stopped_are_named()
    {
        try
        {
            var group = Assert.ContainsSingle(RenderRemoveAndCheck<TimerHolder>());
            Assert.AreEqual(2, group.Causes.Count, string.Join("; ", group.Causes));
            Assert.Contains(c => c.Holder == "its own field _ticker" && c.Detail == "Timer still scheduled, with a callback that reaches the component", group.Causes);
            Assert.Contains(c => c.Holder == "its own field _poll" && c.Detail == "Timer still scheduled, with a callback that reaches the component", group.Causes);
        }
        finally
        {
            TimerHolder.StopAll();
        }
    }

    [TestMethod]
    public void A_component_is_not_called_retained_before_full_collections_ran_after_its_disposal()
    {
        Session.Leaks.GracePeriod = TimeSpan.Zero;
        RenderAndRemove(typeof(LeakySubscriber));
        RenderParentAgain();

        // Free to go from now, but no full collection has run since: not evidence yet.
        Assert.IsEmpty(Session.Leaks.Retained());
        Assert.AreEqual(1, Session.Leaks.Watched);

        GC.Collect();
        GC.Collect();
        Assert.ContainsSingle(Session.Leaks.Retained(), "two full collections after it was free to go");
    }

    [TestMethod]
    public void Collections_while_the_renderer_held_it_do_not_count()
    {
        // The parent's previous render tree holds the removed child through a forced check and natural collections;
        // the parent then renders, and the child is free to go but not collected yet: not a leak, whatever ran before.
        Session.Leaks.GracePeriod = TimeSpan.Zero;
        RenderAndRemove(typeof(WatchedTidySubscriber));
        Assert.IsEmpty(Session.Leaks.CollectAndCheck());
        GC.Collect();
        GC.Collect();
        Assert.IsEmpty(Session.Leaks.Retained());

        _renderer.SetRootParameters(_lastRoot, new Dictionary<string, object?> { [nameof(LeakHost.Child)] = typeof(DomMapLeaf) });
        Assert.IsEmpty(Session.Leaks.Retained(), "released a moment ago, and no collection since");
    }

    [TestMethod]
    public void A_check_forced_before_the_grace_period_ended_does_not_judge_the_component()
    {
        // Work still running when it was removed may hold it: a forced check this early leaves it alone, and later it
        // needs collections of its own.
        Session.Leaks.ForcedGracePeriod = TimeSpan.FromHours(1);
        Session.Leaks.GracePeriod = TimeSpan.Zero;
        RenderAndRemove(typeof(LeakySubscriber));
        RenderParentAgain();

        Assert.IsEmpty(Session.Leaks.CollectAndCheck());
        Assert.IsEmpty(Session.Leaks.Retained(), "free to go from now: the forced collections ran before");
    }

    [TestMethod]
    public void A_thousand_leaked_rows_on_one_event_are_analysed_in_one_reading_of_it()
    {
        var root = _renderer.RenderRoot<ManyRows>(new Dictionary<string, object?> { [nameof(ManyRows.Count)] = 1000 });
        Session.Components.Refresh(force: true);
        _renderer.SetRootParameters(root, new Dictionary<string, object?> { [nameof(ManyRows.Count)] = 0 });
        Session.Components.Refresh(force: true);
        _renderer.SetRootParameters(root, new Dictionary<string, object?> { [nameof(ManyRows.Count)] = 0 });

        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var group = Assert.ContainsSingle(Session.Leaks.CollectAndCheck());
        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started);

        Assert.AreEqual(1000, group.Count);
        StringAssert.Contains(Assert.ContainsSingle(group.Causes).Detail, "which captures the component");
        // Every row's closure on one shared event: read once per check, not once per row (1.2 s before, Release).
        Console.WriteLine($"1000 retained rows checked in {elapsed.TotalMilliseconds:0} ms");
        Assert.IsLessThan(1000, elapsed.TotalMilliseconds, $"took {elapsed.TotalMilliseconds:0} ms");
    }

    [TestMethod]
    public void A_forced_check_reports_a_component_its_parent_still_holds_when_a_service_holds_it_too()
    {
        // Hidden a moment ago: the page has not rendered since, so its previous render tree still holds the component,
        // but the singleton's event does too, after the disposal - the renderer lets go, the event never will.
        RenderAndRemove(typeof(LeakySubscriber));

        var group = Assert.ContainsSingle(Session.Leaks.CollectAndCheck());
        Assert.AreEqual("LeakyEvents (injected) → Changed", Assert.ContainsSingle(group.Causes).Holder);
        Assert.ContainsSingle(Session.Leaks.Retained(), "still retained on the next look, until it is collected");
    }

    [TestMethod]
    public void A_transient_service_the_component_subscribes_to_does_not_keep_it()
    {
        Assert.IsEmpty(RenderRemoveAndCheck<TransientViewModelUser>(), "the view model goes with the component");
    }

    [TestMethod]
    public void A_fired_one_shot_timer_is_not_named_as_the_cause()
    {
        try
        {
            var group = Assert.ContainsSingle(RenderRemoveAndCheck<StaticallyHeldWithFiredTimer>());
            Assert.IsEmpty(group.Causes, string.Join("; ", group.Causes));
        }
        finally
        {
            StaticallyHeldWithFiredTimer.Instances.Clear();
        }
    }

    [TestMethod]
    public void A_listener_left_on_an_options_monitor_is_named()
    {
        var group = Assert.ContainsSingle(RenderRemoveAndCheck<OptionsListener>());
        var cause = Assert.ContainsSingle(group.Causes);
        StringAssert.StartsWith(cause.Holder, "IOptionsMonitor<LeakOptions> (injected) → ");
        StringAssert.Contains(cause.Detail, "OptionsListener");
        StringAssert.Contains(cause.Suggestion, "Dispose the subscription");
    }

    [TestMethod]
    public void The_finding_names_the_cause_and_the_protocol_runs_the_check()
    {
        RenderAndRemove(typeof(LeakySubscriber));
        RenderParentAgain();

        using var answer = JsonDocument.Parse(BevToolsProtocol.Handle(Session, "command", "{\"name\":\"collect\"}"));
        Assert.AreEqual("1 disposed component(s) survived the collection", answer.RootElement.GetProperty("done").GetString());

        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "memory.retained-components", Session.Diagnostics.Findings).Diagnostic;
        Assert.AreEqual("LeakySubscriber: 1 disposed instance still alive", finding.Title);
        StringAssert.Contains(finding.Evidence, "Held by: LeakyEvents (injected) → Changed: handler LeakySubscriber.OnChanged");
    }

    [TestMethod]
    public void A_server_process_outside_Development_does_not_let_the_panel_force_a_collection()
    {
        var (session, scope) = TestHelpers.CreateSession(services: s => s.AddSingleton(Type.GetType(BevToolsAttachPolicy.ServerTypeName + ", Microsoft.AspNetCore.Hosting.Server.Abstractions")!, _ => new object()));
        using (scope)
        {
            Assert.IsTrue(session.IsInMultiUserHost);
            Assert.IsFalse(session.AllowsForcedCollection);
            using var answer = JsonDocument.Parse(BevToolsProtocol.Handle(session, "command", "{\"name\":\"collect\"}"));
            StringAssert.Contains(answer.RootElement.GetProperty("error").GetString(), "not allowed");
        }
    }
}

public class ManyRows : ComponentBase
{
    [Parameter] public int Count { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        for (var i = 0; i < Count; i++)
        {
            builder.OpenComponent<LambdaSubscriber>(0);
            builder.SetKey(i);
            builder.CloseComponent();
        }
    }
}

public sealed class LeakOptions
{
    public int Value { get; set; }
}

public sealed class TransientEvents
{
    public event Action? Changed;

    public void Raise() => Changed?.Invoke();
}

public class TransientViewModelUser : ComponentBase
{
    [Inject] public TransientEvents Vm { get; set; } = default!;

    protected override void OnInitialized() => Vm.Changed += StateHasChanged;
}

public class StaticallyHeldWithFiredTimer : ComponentBase
{
    public static readonly List<object> Instances = [];

    private System.Threading.Timer? _once;

    protected override void OnInitialized()
    {
        Instances.Add(this);
        using var fired = new ManualResetEventSlim();
        _once = new System.Threading.Timer(static state => ((ManualResetEventSlim)state!).Set(), fired, 0, Timeout.Infinite);
        fired.Wait(TimeSpan.FromSeconds(5));
    }
}

public class OptionsListener : ComponentBase
{
    [Inject] public Microsoft.Extensions.Options.IOptionsMonitor<LeakOptions> Options { get; set; } = default!;

    protected override void OnInitialized() => Options.OnChange(OnOptionsChanged);

    private void OnOptionsChanged(LeakOptions options, string? name) => StateHasChanged();
}

public sealed class LeakyEvents
{
    public event Action? Changed;

    public int Subscribers => Changed?.GetInvocationList().Length ?? 0;
}

public class LeakHost : ComponentBase
{
    [Parameter] public Type? Child { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Child is not null)
        {
            builder.OpenComponent(0, Child);
            builder.CloseComponent();
        }
    }
}

public class LeakySubscriber : ComponentBase
{
    [Inject] public LeakyEvents Events { get; set; } = default!;

    protected override void OnInitialized() => Events.Changed += OnChanged;

    private void OnChanged() => StateHasChanged();
}

public class TidySubscriber : ComponentBase, IDisposable
{
    [Inject] public LeakyEvents Events { get; set; } = default!;

    protected override void OnInitialized() => Events.Changed += OnChanged;

    private void OnChanged() => StateHasChanged();

    public void Dispose() => Events.Changed -= OnChanged;
}

public class WatchedTidySubscriber : TidySubscriber
{
    public static WeakReference? Last;

    public WatchedTidySubscriber() => Last = new WeakReference(this);
}

public class LambdaSubscriber : ComponentBase
{
    private int _changes;

    [Inject] public LeakyEvents Events { get; set; } = default!;

    protected override void OnInitialized()
    {
        // A local captured next to the component: the handler's target is a closure, not the component itself.
        var step = 1;
        Events.Changed += () => _changes += step;
    }
}

public class ReferenceHolder : ComponentBase
{
    [Inject] public IJSRuntime JS { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("observe", DotNetObjectReference.Create(this));
        }
    }
}

public class TimerHolder : ComponentBase
{
    private static readonly List<IDisposable> Started = [];
    private System.Timers.Timer? _ticker;
    private System.Threading.Timer? _poll;

    protected override void OnInitialized()
    {
        _ticker = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds);
        _ticker.Elapsed += (_, _) => InvokeAsync(StateHasChanged);
        _ticker.Start();
        _poll = new System.Threading.Timer(_ => InvokeAsync(StateHasChanged), null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        lock (Started)
        {
            Started.Add(_ticker);
            Started.Add(_poll);
        }
    }

    public static void StopAll()
    {
        lock (Started)
        {
            Started.ForEach(t => t.Dispose());
            Started.Clear();
        }
    }
}

/// <summary>The smallest real renderer: no test framework holding on to what it rendered.</summary>
internal sealed class PlainRenderer(IServiceProvider services) : Renderer(services, NullLoggerFactory.Instance)
{
    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    public int RenderRoot<T>(Dictionary<string, object?> parameters) where T : IComponent => Dispatcher.InvokeAsync(async () =>
    {
        var id = AssignRootComponentId(InstantiateComponent(typeof(T)));
        await RenderRootComponentAsync(id, ParameterView.FromDictionary(parameters));
        return id;
    }).GetAwaiter().GetResult();

    public void SetRootParameters(int id, Dictionary<string, object?> parameters) =>
        Dispatcher.InvokeAsync(() => RenderRootComponentAsync(id, ParameterView.FromDictionary(parameters))).GetAwaiter().GetResult();

    protected override void HandleException(Exception exception) => throw exception;

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
}

/// <summary>A JS runtime that tracks DotNetObjectReferences the way every real one does, and never answers.</summary>
public sealed class TrackingTestJSRuntime : JSRuntime
{
    protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
    {
    }

    protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
    {
    }
}
