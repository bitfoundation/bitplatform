using System.Security.Claims;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>
/// What BevTools reads about the application around its components, through a real renderer: the handlers and
/// callbacks a component bound, the route it is on and every route the router knows, the authentication state, the
/// prerendering summary and the performance trace.
/// </summary>
[TestClass]
public class AppModelTests : BunitContext
{
    public AppModelTests()
    {
        Services.AddSingleton<IJSRuntime>(new FakeJSRuntime());
        Services.AddBitBevToolsServices(o => o.Enabled = true);
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void The_event_handlers_and_callbacks_a_component_bound_are_read_from_its_render_tree()
    {
        Render<WiringComponent>();
        var record = Session.Components.All.Single(r => r.Type == typeof(WiringComponent));
        var child = Session.Components.All.Single(r => r.Type == typeof(CallbackChild));
        var byId = Session.Components.All.Where(r => r.ComponentId is not null).ToDictionary(r => r.ComponentId!.Value);

        var wiring = BevComponentWiringReader.Read(record, id => byId.GetValueOrDefault(id));

        Assert.IsNull(wiring.Unavailable);
        Assert.Contains(b => b is { Target: "<button#save>", Event: "onclick", Handler: "WiringComponent.Save", Kind: BevEventBindingKind.DomEvent }, wiring.Bindings);
        Assert.Contains(b => b is { Target: "<input>", Event: "onchange", Kind: BevEventBindingKind.Bind } && b.Handler.StartsWith("@bind-value", StringComparison.Ordinal), wiring.Bindings);
        Assert.Contains(b => b is { Target: "<a.nav>", Event: "onclick", Handler: "lambda in WiringComponent.BuildRenderTree" }, wiring.Bindings);
        var setter = wiring.Bindings.Single(b => b.Target == "<input.search>");
        Assert.AreEqual(("oninput", "@bind-value → WiringComponent.OnQuery", BevEventBindingKind.Bind, "OnQuery"), (setter.Event, setter.Handler, setter.Kind, setter.HandlerMethod?.Name), "the setter @bind:set names, and where it is written");
        var callback = wiring.Bindings.Single(b => b.Kind == BevEventBindingKind.ChildCallback);
        Assert.AreEqual(("CallbackChild", "OnSelected", "WiringComponent.OnChildSelected", child.InstanceId), (callback.Target, callback.Event, callback.Handler, callback.ChildInstanceId));
        Assert.AreEqual(5, wiring.TotalBindings, "child content and plain attributes are not bindings");
    }

    [TestMethod]
    public void An_EventCallback_parameter_says_what_it_runs()
    {
        Render<WiringComponent>();
        var child = Session.Components.All.Single(r => r.Type == typeof(CallbackChild));

        var parameter = Session.Inspector.Describe("OnSelected", ((CallbackChild)child.Component!).OnSelected);

        Assert.AreEqual("EventCallback<Int32> → WiringComponent.OnChildSelected", parameter.Display);
        Assert.AreEqual("EventCallback (empty)", BevEventCallbacks.Describe(EventCallback.Empty));
        Assert.AreEqual("EventCallback (empty)", BevEventCallbacks.Describe(default(EventCallback)));
    }

    [TestMethod]
    public void The_current_route_and_the_route_table_are_read_from_the_router()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/bev-tests/routed/42?tab=details&token=abc123");
        RenderFragment<RouteData> found = routeData => builder =>
        {
            builder.OpenComponent<RouteView>(0);
            builder.AddComponentParameter(1, nameof(RouteView.RouteData), routeData);
            builder.CloseComponent();
        };
        Render<Router>(ps => ps.Add(p => p.AppAssembly, typeof(RoutedPage).Assembly).Add(p => p.Found, found));

        var routing = BevRoutingReader.Read(Session);

        var current = routing.Current;
        Assert.AreEqual("RoutedPage", current.PageDisplayName);
        Assert.IsNotNull(current.PageInstanceId);
        Assert.AreEqual("/bev-tests/routed/{id:int}", current.Template);
        Assert.Contains(v => v.Key == "id" && v.Value == "42", current.RouteValues);
        Assert.Contains(q => q.Key == "tab" && q.Value == "\"details\"", current.Query);
        Assert.Contains(q => q.Key == "token" && q.Value == BevRedactor.RedactedValue, current.Query);
        Assert.IsFalse(current.Uri!.Contains("abc123", StringComparison.Ordinal));
        var route = routing.Routes.Single(r => r.PageDisplayName == "RoutedPage");
        Assert.AreEqual("Authorize(Roles = Admin)", route.Authorization.Single());
        StringAssert.Contains(routing.RoutesSource, "AppAssembly");
    }

    [TestMethod]
    public void Without_a_router_in_the_session_the_routing_snapshot_says_so()
    {
        Render<WiringComponent>();

        var routing = BevRoutingReader.Read(Session);

        Assert.IsNull(routing.Current.PageType);
        StringAssert.Contains(routing.Current.Source, "No RouteView");
        StringAssert.Contains(routing.RoutesSource, "No Router");
    }

    [TestMethod]
    public async Task The_authentication_state_is_a_state_provider_with_a_change_history()
    {
        var authorization = AddAuthorization();
        authorization.SetAuthorized("ada");
        authorization.SetRoles("Admin");
        authorization.SetClaims(new Claim("access_token", "opaque-token-value"), new Claim("email", "ada@example.com"));
        var session = Session;
        session.EnsureActivated();

        var entry = session.State.Entries.Single(e => e.Name == "Authentication");
        var snapshot = (BevAuthenticationSnapshot)entry.Provider.GetSnapshot()!;
        Assert.IsTrue(snapshot.IsAuthenticated);
        Assert.AreEqual("ada", snapshot.Name);
        Assert.Contains("Admin", snapshot.Roles);
        var flat = session.Inspector.Flatten(snapshot);
        Assert.IsFalse(flat.Values.Any(v => v.Contains("opaque-token-value", StringComparison.Ordinal)), "a claim named like a token is redacted");
        Assert.IsTrue(flat.Values.Any(v => v.Contains("ada@example.com", StringComparison.Ordinal)));

        authorization.SetNotAuthorized();
        await WaitUntil(() => entry.ChangeCount > 0 && !((BevAuthenticationSnapshot)entry.Provider.GetSnapshot()!).IsAuthenticated);

        var change = entry.Changes.Last();
        Assert.AreEqual("AuthenticationStateChanged", change.Action);
        Assert.AreEqual("anonymous", change.Detail, "the timeline detail names no one");
        Assert.Contains(c => c.Path == "IsAuthenticated", change.Changes);
    }

    [TestMethod]
    public void A_request_made_while_prerendering_and_again_when_interactive_is_flagged()
    {
        var session = Session;
        session.Prerender = new BevPrerenderSummary
        {
            Path = "/products",
            Requests = [new BevPrerenderRequest("GET", "https://localhost:5001/api/products", 200, 40, null), new BevPrerenderRequest("GET", "https://localhost:5001/api/once", 200, 5, null)],
        };
        AddRequest(session, "GET", "https://localhost:5001/api/products");
        AddRequest(session, "GET", "https://localhost:5001/api/other");

        session.Diagnostics.Evaluate(force: true);

        var finding = session.Diagnostics.Findings.Single(f => f.Diagnostic.RuleId == "webapp.prerender-refetch");
        StringAssert.Contains(finding.Diagnostic.Title, "GET /api/products");
        StringAssert.Contains(finding.Diagnostic.Suggestion, "PersistentState");
    }

    [TestMethod]
    public void The_prerendering_panel_hands_its_summary_to_the_interactive_one()
    {
        var state = this.AddBunitPersistentComponentState();
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        Render<WiringComponent>();
        AddRequest(Session, "GET", "https://localhost/api/products");

        Render<BevToolsPanel>();
        state.TriggerOnPersisting();

        Assert.IsTrue(state.TryTake<BevPrerenderSummary>(BevPrerenderSummary.StateKey, out var summary));
        Assert.IsNotNull(summary);
        Assert.IsTrue(summary.Components >= 2);
        Assert.Contains(r => r.Url == "https://localhost/api/products", summary.Requests);
    }

    [TestMethod]
    public void A_performance_trace_nests_renders_in_their_batch_and_links_causes()
    {
        var cut = Render<ParentComponent>(ps => ps.Add(p => p.Value, 1));
        cut.InvokeAsync(() => cut.Instance.Increment());
        var ui = Session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.UiEvent, Category = "ui-event", Title = "onclick → Parent.Increment", DurationMs = 2 });
        Session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Http, Category = "http", Title = "GET /api/items", DurationMs = 12, ParentEventId = ui });
        Session.RenderTracker.CloseBatch();

        // A batch that ends with a render too short to measure: drawn a microsecond long, it must not run past the span.
        const long instantBatch = 9_999;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var measured = new BevRenderSample { Sequence = 1, At = DateTimeOffset.UtcNow, StartTicks = start, BatchId = instantBatch };
        var instant = new BevRenderSample { Sequence = 2, At = measured.At, StartTicks = start + System.Diagnostics.Stopwatch.Frequency / 50_000, BatchId = instantBatch };
        Session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.RenderBatch, Category = "render-batch", Title = "Render batch #" + instantBatch, StartTicks = start, Timestamp = measured.At, Data = new Dictionary<string, object?> { ["batch"] = instantBatch } });
        Session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Render, Category = "render", Title = "Measured", DurationMs = 0.01, StartTicks = measured.StartTicks, Timestamp = measured.At, Data = new RenderEventData(measured, 1) });
        Session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Render, Category = "render", Title = "Instant", DurationMs = 0, StartTicks = instant.StartTicks, Timestamp = instant.At, Data = new RenderEventData(instant, 1) });

        using var trace = JsonDocument.Parse(BevTraceExporter.ToChromeTrace(Session));

        var events = trace.RootElement.GetProperty("traceEvents").EnumerateArray().ToList();
        Assert.AreEqual(BevTraceExporter.Format, trace.RootElement.GetProperty("otherData").GetProperty("format").GetString());
        var renderer = events.Where(e => e.GetProperty("ph").GetString() == "X" && e.GetProperty("tid").GetInt32() == 1).ToList();
        var batches = renderer.Where(e => e.GetProperty("cat").GetString() == "render-batch").ToList();
        var renders = renderer.Where(e => e.GetProperty("cat").GetString() == "render").ToList();
        Assert.IsNotEmpty(batches);
        Assert.HasCount(4 + 2, renders, "two components, rendered twice each, and the two recorded here");
        foreach (var render in renders)
        {
            var ts = render.GetProperty("ts").GetDouble();
            Assert.IsTrue(batches.Any(b => b.GetProperty("ts").GetDouble() <= ts && ts + render.GetProperty("dur").GetDouble() <= b.GetProperty("ts").GetDouble() + b.GetProperty("dur").GetDouble() + 0.001),
                "every render lies inside a batch span on the renderer track");
        }

        var instantSpan = batches.Single(b => b.GetProperty("name").GetString() == "Render batch #" + instantBatch);
        var instantRender = renders.Single(r => r.GetProperty("name").GetString() == "Instant");
        Assert.AreEqual(1, instantRender.GetProperty("dur").GetDouble());
        Assert.IsLessThanOrEqualTo(End(instantSpan), End(instantRender), "the zero-length render ends at or before its batch");

        Assert.Contains(e => e.GetProperty("ph").GetString() == "X" && e.GetProperty("name").GetString() == "GET /api/items", events);
        Assert.Contains(e => e.GetProperty("ph").GetString() == "s", events);
        Assert.Contains(e => e.GetProperty("ph").GetString() == "M" && e.GetProperty("args").GetProperty("name").GetString()!.StartsWith("BevTools session #", StringComparison.Ordinal), events);

        static double End(JsonElement slice) => slice.GetProperty("ts").GetDouble() + slice.GetProperty("dur").GetDouble();
    }

    [TestMethod]
    public void Render_modes_read_as_they_are_written_in_markup()
    {
        Assert.AreEqual("InteractiveServer", BevRenderModes.Describe(RenderMode.InteractiveServer));
        Assert.AreEqual("InteractiveWebAssembly, no prerendering", BevRenderModes.Describe(new InteractiveWebAssemblyRenderMode(prerender: false)));
        Assert.AreEqual("InteractiveAuto", BevRenderModes.Describe(RenderMode.InteractiveAuto));
        Assert.IsNull(BevRenderModes.Describe(null));
    }

    [TestMethod]
    public void The_runtime_report_names_the_value_access_and_the_prerender_summary()
    {
        var session = Session;
        session.Prerender = new BevPrerenderSummary { Path = "/", Components = 12, DurationMs = 30 };

        var sections = BevRuntimeInfo.Build(session);

        Assert.Contains(s => s.Title == "Rendering" && s.Facts.Any(f => f.Name == "Value access" && f.Value == "Full"), sections);
        Assert.Contains(s => s.Title == "Prerendering", sections);
        Assert.Contains(s => s.Title == ".NET runtime", sections);
    }

    private static void AddRequest(BevToolsSession session, string method, string url) => session.Http.Add(new BevHttpRecord
    {
        Id = session.Http.NextId(),
        StartedAt = DateTimeOffset.UtcNow,
        Method = method,
        Url = url,
        StatusCode = 200,
        DurationMs = 10,
    });

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(10);
        }

        Assert.IsTrue(condition());
    }

    public sealed class WiringComponent : ComponentBase
    {
        public string Name { get; set; } = "";

        public string? Query { get; private set; }

        public int Saved { get; private set; }

        private void OnQuery(string? value) => Query = value;

        private void Save() => Saved++;

        private void OnChildSelected(int id) => Saved += id;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "id", "save");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, Save));
            builder.AddContent(3, "Save");
            builder.CloseElement();
            builder.OpenElement(4, "input");
            builder.AddAttribute(5, "value", BindConverter.FormatValue(Name));
            builder.AddAttribute(6, "onchange", EventCallback.Factory.CreateBinder(this, value => Name = value ?? "", Name));
            builder.SetUpdatesAttributeName("value");
            builder.CloseElement();
            builder.OpenComponent<CallbackChild>(7);
            builder.AddComponentParameter(8, nameof(CallbackChild.OnSelected), EventCallback.Factory.Create<int>(this, OnChildSelected));
            builder.AddComponentParameter(9, nameof(CallbackChild.ChildContent), (RenderFragment)(b => b.AddContent(0, "child")));
            builder.CloseComponent();
            builder.OpenElement(10, "a");
            builder.AddAttribute(11, "class", "nav link");
            builder.AddAttribute(12, "onclick", () => Saved++);
            builder.CloseElement();
            // @bind:get="Query" @bind:set="OnQuery" @bind:event="oninput", as the Razor compiler emits it.
            builder.OpenElement(13, "input");
            builder.AddAttribute(14, "class", "search");
            builder.AddAttribute(15, "value", BindConverter.FormatValue(Query));
            builder.AddAttribute(16, "oninput", EventCallback.Factory.CreateBinder(this, Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.CreateInferredBindSetter(callback: OnQuery, value: Query), Query ?? ""));
            builder.SetUpdatesAttributeName("value");
            builder.CloseElement();
        }
    }

    public sealed class CallbackChild : ComponentBase
    {
        [Parameter] public EventCallback<int> OnSelected { get; set; }

        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
    }

    [Route("/bev-tests/routed/{id:int}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public sealed class RoutedPage : ComponentBase
    {
        [Parameter] public int Id { get; set; }

        [SupplyParameterFromQuery] public string? Tab { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "routed " + Id + " " + Tab);
    }
}
