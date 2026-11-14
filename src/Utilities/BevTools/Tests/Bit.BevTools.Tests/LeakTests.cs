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
        services.AddSingleton<TourService>();
        services.AddSingleton<SubscriberRegistry>();
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
    public void A_callback_kept_by_an_object_inside_a_service_is_found_and_its_path_named()
    {
        // A tour service keeps the tour, the tour its steps, a step the component's callback: three objects below what
        // the component injected, which a search of the service's own fields alone does not reach.
        var group = Assert.ContainsSingle(RenderRemoveAndCheck<TourUser>());
        var cause = Assert.ContainsSingle(group.Causes);
        Assert.AreEqual("TourService (injected) → _tour.Steps[].OnClose", cause.Holder);
        Assert.AreEqual("handler TourUser.Close", cause.Detail);
    }

    [TestMethod]
    public void An_at_ref_of_a_component_still_on_the_page_is_named()
    {
        var root = _renderer.RenderRoot<RefHost>(new Dictionary<string, object?> { [nameof(RefHost.Show)] = true });
        Session.Components.Refresh(force: true);
        _renderer.SetRootParameters(root, new Dictionary<string, object?> { [nameof(RefHost.Show)] = false });
        Session.Components.Refresh(force: true);
        _renderer.SetRootParameters(root, new Dictionary<string, object?> { [nameof(RefHost.Show)] = false });

        var group = Assert.ContainsSingle(Session.Leaks.CollectAndCheck());
        Assert.AreEqual("RefChild", group.DisplayName);
        var cause = Assert.ContainsSingle(group.Causes);
        Assert.AreEqual("RefHost (still rendered) → _child", cause.Holder);
        StringAssert.Contains(cause.Suggestion, "@ref");
        Assert.IsTrue(cause.Bounded, "one field holds one instance, until the markup renders it again");

        Session.Diagnostics.Evaluate(force: true);
        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "memory.retained-components", Session.Diagnostics.Findings).Diagnostic;
        Assert.AreEqual(BevDiagnosticSeverity.Info, finding.Severity, "memory kept, not memory growing");
        StringAssert.Contains(finding.Evidence, "this does not grow");
    }

    [TestMethod]
    public void An_at_ref_is_not_evidence_of_a_leak_while_the_renderer_still_holds_the_component()
    {
        // A dialog host hides its dialog with @if and keeps the @ref: until the host renders again, the renderer holds
        // the dialog too, and the @ref alone does not make it a leak the way a singleton's event does.
        var root = _renderer.RenderRoot<RefHost>(new Dictionary<string, object?> { [nameof(RefHost.Show)] = true });
        Session.Components.Refresh(force: true);
        _renderer.SetRootParameters(root, new Dictionary<string, object?> { [nameof(RefHost.Show)] = false });
        Session.Components.Refresh(force: true);

        Assert.IsEmpty(Session.Leaks.CollectAndCheck());
    }

    [TestMethod]
    public void A_collection_of_at_refs_a_live_component_keeps_adding_to_is_a_leak()
    {
        var root = _renderer.RenderRoot<RefListHost>(new Dictionary<string, object?> { [nameof(RefListHost.Show)] = true });
        Session.Components.Refresh(force: true);
        _renderer.SetRootParameters(root, new Dictionary<string, object?> { [nameof(RefListHost.Show)] = false });
        Session.Components.Refresh(force: true);
        _renderer.SetRootParameters(root, new Dictionary<string, object?> { [nameof(RefListHost.Show)] = false });

        var group = Assert.ContainsSingle(Session.Leaks.CollectAndCheck());
        var cause = Assert.ContainsSingle(group.Causes);
        Assert.AreEqual("RefListHost (still rendered) → _children[]", cause.Holder);
        Assert.IsFalse(cause.Bounded);

        Session.Diagnostics.Evaluate(force: true);
        Assert.AreEqual(BevDiagnosticSeverity.Warning, Assert.ContainsSingle(f => f.Diagnostic.RuleId == "memory.retained-components", Session.Diagnostics.Findings).Diagnostic.Severity);
    }

    [TestMethod]
    public void The_one_rendered_instance_that_is_held_is_found_among_many_of_its_type()
    {
        // The selected row subscribed and keeps the page through its cascading value, and the page every row: the
        // cause is on the eighteenth row, not the first.
        RenderAndRemove(typeof(RowPage));
        RenderParentAgain();

        var group = Assert.ContainsSingle(Session.Leaks.CollectAndCheck());
        Assert.AreEqual("RowPage", group.DisplayName);
        Assert.Contains(new BevRetainedTypeCount("PageRow", RowPage.Rows), group.Descendants);
        var cause = Assert.ContainsSingle(group.Causes);
        Assert.AreEqual("LeakyEvents (injected) → Changed", cause.Holder);
        Assert.AreEqual("handler PageRow.OnChanged, on PageRow, one of the components it rendered", cause.Detail);
    }

    [TestMethod]
    public void A_callback_kept_in_a_tuple_of_a_services_list_is_found()
    {
        var group = Assert.ContainsSingle(RenderRemoveAndCheck<TupleSubscriber>());
        var cause = Assert.ContainsSingle(group.Causes);
        Assert.AreEqual("SubscriberRegistry (injected) → _subscribers[]", cause.Holder);
        Assert.AreEqual("handler TupleSubscriber.OnChanged", cause.Detail);
    }

    [TestMethod]
    public void A_component_a_second_renderer_still_holds_is_not_called_leaked()
    {
        // A page can have more than one renderer (a root component added from JavaScript, a second WebAssembly root):
        // the buffers of each are read, not only the first one's.
        using var second = new PlainRenderer(_scope.ServiceProvider);
        _renderer.RenderRoot<LeakHost>(new Dictionary<string, object?>());
        RenderInSecondAndRemove(second);

        CollectionAssert.AreEqual(new Renderer[] { _renderer, second }, Session.Components.Renderers.ToArray());
        Assert.AreSame(_renderer, Session.Components.Renderer, "the first renderer the session saw");
        Assert.IsEmpty(Session.Leaks.CollectAndCheck());
        Assert.IsTrue(WatchedTidySubscriber.Last!.IsAlive, "the second renderer still holds it, so this test proves its buffers are read");
    }

    // Not inlined, so nothing the render left on this frame's stack can keep the component alive.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private void RenderInSecondAndRemove(PlainRenderer renderer)
    {
        var root = renderer.RenderRoot<LeakHost>(new Dictionary<string, object?> { [nameof(LeakHost.Child)] = typeof(WatchedTidySubscriber) });
        Session.Components.Refresh(force: true);
        renderer.SetRootParameters(root, new Dictionary<string, object?> { [nameof(LeakHost.Child)] = null });
        Session.Components.Refresh(force: true);
    }

    [TestMethod]
    public void What_a_retained_component_rendered_is_reported_with_it_not_on_its_own()
    {
        RenderAndRemove(typeof(IconPage));
        RenderParentAgain();

        var group = Assert.ContainsSingle(Session.Leaks.CollectAndCheck(), "one finding for the page, not one per type it rendered");
        Assert.AreEqual("IconPage", group.DisplayName);
        Assert.AreEqual(1, group.Count);
        Assert.AreEqual(3, group.DescendantCount);
        Assert.AreEqual(new BevRetainedTypeCount("PageIcon", 3), Assert.ContainsSingle(group.Descendants));
        Assert.AreEqual("LeakyEvents (injected) → Changed", Assert.ContainsSingle(group.Causes).Holder);
        Assert.HasCount(4, group.InstanceIds);

        Session.Diagnostics.Evaluate(force: true);
        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "memory.retained-components", Session.Diagnostics.Findings).Diagnostic;
        Assert.AreEqual("IconPage: 1 disposed instance still alive, with 3 components it rendered", finding.Title);
        StringAssert.Contains(finding.Evidence, "With it, 3 disposed components it rendered: PageIcon 3.");
    }

    [TestMethod]
    public void A_disposed_component_is_never_named_as_what_holds_another()
    {
        // The slide is in the disposed carousel's list, and the carousel was cascaded to it: that is a cycle, which goes
        // when the carousel's own holder lets go. The slide goes under the carousel, through the CascadingValue between
        // them (collected), and the cause is the carousel's.
        RenderAndRemove(typeof(Carousel));
        RenderParentAgain();

        var group = Assert.ContainsSingle(Session.Leaks.CollectAndCheck());
        Assert.AreEqual("Carousel", group.DisplayName);
        Assert.Contains(new BevRetainedTypeCount("CarouselSlide", 1), group.Descendants);
        Assert.AreEqual("LeakyEvents (injected) → Changed", Assert.ContainsSingle(group.Causes).Holder);
    }

    [TestMethod]
    public void A_component_nothing_searched_holds_is_reported_with_what_was_searched()
    {
        try
        {
            RenderAndRemove(typeof(StaticallyHeld));
            RenderParentAgain();
            Session.Leaks.CollectAndCheck();
            Session.Diagnostics.Evaluate(force: true);

            var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "memory.retained-components", Session.Diagnostics.Findings).Diagnostic;
            StringAssert.Contains(finding.Evidence, "No reference to it was found within what BevTools searched: its injected singleton and scoped services, its cascading values");
            StringAssert.Contains(finding.Evidence, "at most 1,000 items of each collection or handler list, 20,000 objects per holder and 60,000 in all");
            StringAssert.Contains(finding.Suggestion, "heap snapshot");
        }
        finally
        {
            StaticallyHeld.Instances.Clear();
        }
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

public sealed class SubscriberRegistry
{
    private readonly List<(Guid Id, Action Callback)> _subscribers = [];

    public void Subscribe(Action callback) => _subscribers.Add((Guid.NewGuid(), callback));
}

public class TupleSubscriber : ComponentBase
{
    [Inject] public SubscriberRegistry Registry { get; set; } = default!;

    protected override void OnInitialized() => Registry.Subscribe(OnChanged);

    private void OnChanged()
    {
    }
}

public class RefListHost : ComponentBase
{
    private readonly List<RefChild> _children = [];

    [Parameter] public bool Show { get; set; }

    public int Children => _children.Count;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Show)
        {
            builder.OpenComponent<RefChild>(0);
            builder.AddComponentReferenceCapture(1, c => _children.Add((RefChild)c));
            builder.CloseComponent();
        }
    }
}

public class RowPage : ComponentBase
{
    public const int Rows = 30;

    private readonly List<PageRow> _rows = [];

    public int Count => _rows.Count;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<RowPage>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<RowPage>.Value), this);
        builder.AddComponentParameter(2, nameof(CascadingValue<RowPage>.IsFixed), true);
        builder.AddComponentParameter(3, nameof(CascadingValue<RowPage>.ChildContent), (RenderFragment)(b =>
        {
            for (var i = 0; i < Rows; i++)
            {
                b.OpenComponent<PageRow>(0);
                b.SetKey(i);
                b.AddComponentParameter(1, nameof(PageRow.Selected), i == 17);
                b.AddComponentReferenceCapture(2, c => _rows.Add((PageRow)c));
                b.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }
}

public class PageRow : ComponentBase
{
    [Inject] public LeakyEvents Events { get; set; } = default!;

    [CascadingParameter] public RowPage Page { get; set; } = default!;

    [Parameter] public bool Selected { get; set; }

    protected override void OnInitialized()
    {
        if (Selected)
        {
            Events.Changed += OnChanged;
        }
    }

    private void OnChanged()
    {
    }
}

public sealed class TourService
{
    private Tour? _tour;

    public void Start(Action onClose) => _tour = new Tour { Steps = [new TourStepModel(), new TourStepModel { OnClose = onClose }] };
}

public sealed class Tour
{
    public List<TourStepModel> Steps { get; init; } = [];
}

public sealed class TourStepModel
{
    public Action? OnClose { get; init; }
}

public class TourUser : ComponentBase
{
    [Inject] public TourService Tour { get; set; } = default!;

    protected override void OnInitialized() => Tour.Start(Close);

    private void Close()
    {
    }
}

public class RefHost : ComponentBase
{
    private RefChild? _child;

    [Parameter] public bool Show { get; set; }

    public RefChild? Child => _child;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Show)
        {
            builder.OpenComponent<RefChild>(0);
            builder.AddComponentReferenceCapture(1, c => _child = (RefChild)c);
            builder.CloseComponent();
        }
    }
}

public class RefChild : ComponentBase
{
}

public class IconPage : ComponentBase
{
    private readonly List<PageIcon> _icons = [];

    [Inject] public LeakyEvents Events { get; set; } = default!;

    public int Icons => _icons.Count;

    protected override void OnInitialized() => Events.Changed += OnChanged;

    private void OnChanged()
    {
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        for (var i = 0; i < 3; i++)
        {
            builder.OpenComponent<PageIcon>(0);
            builder.SetKey(i);
            builder.AddComponentReferenceCapture(1, c => _icons.Add((PageIcon)c));
            builder.CloseComponent();
        }
    }
}

public class PageIcon : ComponentBase
{
}

public class Carousel : ComponentBase
{
    [Inject] public LeakyEvents Events { get; set; } = default!;

    public List<CarouselSlide> Slides { get; } = [];

    protected override void OnInitialized() => Events.Changed += OnChanged;

    private void OnChanged()
    {
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<Carousel>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<Carousel>.Value), this);
        builder.AddComponentParameter(2, nameof(CascadingValue<Carousel>.IsFixed), true);
        builder.AddComponentParameter(3, nameof(CascadingValue<Carousel>.ChildContent), (RenderFragment)(b =>
        {
            b.OpenComponent<CarouselSlide>(0);
            b.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public class CarouselSlide : ComponentBase
{
    [CascadingParameter] public Carousel Carousel { get; set; } = default!;

    protected override void OnInitialized() => Carousel.Slides.Add(this);
}

public class StaticallyHeld : ComponentBase
{
    public static readonly List<object> Instances = [];

    protected override void OnInitialized() => Instances.Add(this);
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
