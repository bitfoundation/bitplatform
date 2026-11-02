using System.Diagnostics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// A batch on a time axis (each render where it started, on the row of its depth) and renders per page (each render on
/// the page shown when it happened) - both read from the samples BevTools holds, nothing estimated.
/// </summary>
[TestClass]
public class ProfilerTimeAndRouteTests : BunitContext
{
    [TestMethod]
    public void A_batch_on_a_time_axis_places_each_render_at_its_start_on_the_row_of_its_depth()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var alive = new List<ChildComponent>();
            BevComponentRecord Add(BevComponentRecord? parent, long startTicks, double ms, long batch = 3)
            {
                var component = new ChildComponent();
                alive.Add(component);
                var record = session.Components.Register(component, typeof(ChildComponent), isBevTools: false, isHidden: false)!;
                record.Parent = parent;
                record.ParentResolved = true;
                record.AddRender(new BevRenderSample { Sequence = 1, At = DateTimeOffset.UtcNow, StartTicks = startTicks, BatchId = batch, DurationMs = ms });
                return record;
            }

            var t0 = Stopwatch.GetTimestamp();
            var ms = Stopwatch.Frequency / 1000;
            var list = Add(null, t0, 0.5);
            var row1 = Add(list, t0 + 2 * ms, 0.25);
            var row2 = Add(list, t0 + 3 * ms, 0.25);
            var cell = Add(row2, t0 + 4 * ms, 0.5);
            Add(null, t0, 9, batch: 4);

            var timeline = BevBatchTimeline.Build(session.Components.All, 3, maxBars: 300);

            Assert.HasCount(4, timeline.Bars);
            Assert.AreEqual(3, timeline.Depth);
            CollectionAssert.AreEqual(new[] { list.InstanceId, row1.InstanceId, row2.InstanceId, cell.InstanceId }, timeline.Bars.Select(b => b.InstanceId).ToArray(), "in the order they started");
            CollectionAssert.AreEqual(new[] { 0, 1, 1, 2 }, timeline.Bars.Select(b => b.Depth).ToArray());
            Assert.AreEqual(4, timeline.Bars[3].StartMs, 0.01);
            Assert.AreEqual(4.5, timeline.SpanMs, 0.01);
            GC.KeepAlive(alive);
        }
    }

    [TestMethod]
    public void A_twenty_thousand_deep_chain_is_placed_without_recursion_or_rewalking()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.MaxTrackedComponents = 30_000);
        using (scope)
        {
            var alive = new List<ChildComponent>(20_000);
            BevComponentRecord? parent = null;
            var t0 = Stopwatch.GetTimestamp();
            for (var i = 0; i < 20_000; i++)
            {
                var component = new ChildComponent();
                alive.Add(component);
                var record = session.Components.Register(component, typeof(ChildComponent), isBevTools: false, isHidden: false)!;
                record.Parent = parent;
                record.ParentResolved = true;
                record.AddRender(new BevRenderSample { Sequence = i, At = DateTimeOffset.UtcNow, StartTicks = t0 + i, BatchId = 9, DurationMs = 0.01 });
                parent = record;
            }

            var started = Stopwatch.GetTimestamp();
            var timeline = BevBatchTimeline.Build(session.Components.All, 9, maxBars: 300);
            var elapsed = Stopwatch.GetElapsedTime(started);

            Assert.HasCount(300, timeline.Bars);
            Assert.AreEqual(19_700, timeline.Omitted);
            Assert.AreEqual(299, timeline.Bars[^1].Depth);
            Assert.IsLessThan(2000, elapsed.TotalMilliseconds, $"took {elapsed.TotalMilliseconds:0} ms");
            GC.KeepAlive(alive);
        }
    }

    [TestMethod]
    public void Each_render_is_put_on_the_page_shown_when_it_happened()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var (title, secondsAgo) in new[] { ("Navigate to /products", 30), ("Navigate to /products/{id}", 20), ("Navigate to /products", 10) })
            {
                session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Navigation, Category = "navigation", Title = title, Timestamp = now.AddSeconds(-secondsAgo) });
            }

            var component = new ChildComponent();
            var record = session.Components.Register(component, typeof(ChildComponent), isBevTools: false, isHidden: false)!;
            foreach (var (secondsAgo, ms) in new[] { (40, 1.0), (25, 2.0), (15, 4.0), (5, 8.0), (4, 16.0) })
            {
                record.AddRender(new BevRenderSample { Sequence = secondsAgo, At = now.AddSeconds(-secondsAgo), StartTicks = 0, DurationMs = ms });
            }

            var routes = BevRouteProfile.Build(session).ToDictionary(r => r.Route);

            Assert.AreEqual((2, 3, 26.0), (routes["/products"].Visits, routes["/products"].Renders, routes["/products"].RenderMs));
            Assert.AreEqual((1, 1, 4.0), (routes["/products/{id}"].Visits, routes["/products/{id}"].Renders, routes["/products/{id}"].RenderMs));
            Assert.AreEqual(1, routes[BevRouteProfile.BeforeFirst].Renders);
            Assert.AreEqual(("ChildComponent", 3, 26.0), Assert.ContainsSingle(routes["/products"].Top));
            GC.KeepAlive(component);
        }
    }

    [TestMethod]
    [DataRow(false, DisplayName = "the panel records the navigation (framework instrumentation off)")]
    [DataRow(true, DisplayName = "the router's Navigate activity records it, where there is one (.NET 10)")]
    public void A_pages_first_render_is_on_that_page_although_the_router_renders_it_before_anything_hears_of_the_navigation(bool frameworkInstrumentation)
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
            o.Ui.RefreshIntervalMs = 600_000;
            o.UseFrameworkInstrumentation = frameworkInstrumentation;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
        var session = Services.GetRequiredService<BevToolsSession>();
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/bev-tests/route-a");
        var shell = Render<RoutedShell>();
        shell.InvokeAsync(() => navigation.NavigateTo("/bev-tests/route-b"));
        shell.InvokeAsync(() => navigation.NavigateTo("/bev-tests/route-a"));

        var routes = BevRouteProfile.Build(session).ToDictionary(r => r.Route);

        var pageB = routes["/bev-tests/route-b"];
        CollectionAssert.IsSubsetOf(new[] { "RoutedPageB", "RoutedWidget" }, pageB.Top.Select(t => t.Component).ToArray(), string.Join(" | ", routes.Values.Select(r => r.Route + ": " + string.Join(", ", r.Top.Select(t => t.Component)))));
        Assert.IsTrue(routes["/bev-tests/route-a"].Top.Any(t => t.Component == "RoutedPageA"), "the second visit to A");
        Assert.IsFalse(routes.Values.Where(r => r.Route != "/bev-tests/route-b").Any(r => r.Top.Any(t => t.Component is "RoutedPageB" or "RoutedWidget")), "nothing of B on the page before it");
    }

    [TestMethod]
    public void Of_navigations_at_the_same_instant_a_render_is_on_the_last_and_enhanced_navigations_are_the_same_page()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var at = DateTimeOffset.UtcNow.AddSeconds(-10);
            foreach (var (title, offset) in new[] { ("Navigate to /login", 0), ("Navigate to /account", 0), ("Enhanced navigation to https://shop.test/account", 5) })
            {
                session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Navigation, Category = "navigation", Title = title, Timestamp = at.AddSeconds(offset) });
            }

            var component = new ChildComponent();
            var record = session.Components.Register(component, typeof(ChildComponent), isBevTools: false, isHidden: false)!;
            record.AddRender(new BevRenderSample { Sequence = 1, At = at, StartTicks = 0, DurationMs = 1 });

            var routes = BevRouteProfile.Build(session).ToDictionary(r => r.Route);

            Assert.AreEqual(1, routes["/account"].Renders, "a redirect on a millisecond clock: the render is the redirect target's");
            Assert.AreEqual(2, routes["/account"].Visits, "the enhanced navigation is the same page");
            Assert.IsFalse(routes.ContainsKey("/login"));
            GC.KeepAlive(component);
        }
    }

    [TestMethod]
    public void The_profiler_shows_a_batch_on_a_time_axis_and_renders_per_page_on_request()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
            o.Ui.RefreshIntervalMs = 600_000;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { Online = true });
        module.Setup<string?>("loadPrefs", _ => true).SetResult(null);
        var panel = Render<BevToolsPanel>();
        var session = Services.GetRequiredService<BevToolsSession>();
        var cut = Render<ParentComponent>();
        session.RenderTracker.CloseBatch();
        cut.InvokeAsync(() => cut.Instance.Increment());
        var batchId = session.RenderTracker.CurrentBatch!.Id;
        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Profiler", StringComparison.Ordinal)).Click();
        panel.Find("button[aria-label='Refresh captured data']").Click();
        panel.FindAll("button").Single(b => b.TextContent == "Render batch #" + batchId).Click();

        panel.FindAll(".bev-segmented button").Single(b => b.TextContent == "Time").Click();
        var axis = panel.Find(".bev-time-axis");
        Assert.AreEqual("img", axis.GetAttribute("role"));
        StringAssert.Contains(axis.GetAttribute("aria-label"), "2 renders");
        CollectionAssert.AreEqual(new[] { "ParentComponent", "ChildComponent" }, panel.FindAll(".bev-time-bar").Select(b => b.TextContent.Trim()).ToArray());
        Assert.Contains("top:18px", panel.FindAll(".bev-time-bar")[1].GetAttribute("style")!, StringComparison.Ordinal);

        Assert.IsEmpty(panel.FindAll(".bev-section").Where(s => s.TextContent.Contains("Costliest there", StringComparison.Ordinal)), "not computed until asked");
        panel.FindAll("button").Single(b => b.TextContent == "Show" && b.GetAttribute("aria-expanded") == "false").Click();
        var routes = panel.FindAll(".bev-section").Single(s => s.TextContent.Contains("Costliest there", StringComparison.Ordinal));
        Assert.IsGreaterThan(0, routes.QuerySelectorAll("tbody tr").Length);
    }

    /// <summary>
    /// Renders per page rebuilds at most once a second, and what was captured within that second shows when it is over,
    /// with nothing else happening. That trailing rebuild once asked the interval again; when its timer fired a
    /// fraction of a millisecond early, it scheduled itself for that fraction - a Task.Delay(0) that completes at once -
    /// on the same stack, until a stack overflow ended the process (seen on the demo's Blazor Server host).
    /// </summary>
    [TestMethod]
    public void Renders_per_page_shows_what_changed_within_the_second_once_it_is_over()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
            o.Ui.RefreshIntervalMs = 600_000;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { Online = true });
        module.Setup<string?>("loadPrefs", _ => true).SetResult(null);
        var panel = Render<BevToolsPanel>();
        var cut = Render<ParentComponent>();
        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Profiler", StringComparison.Ordinal)).Click();
        panel.FindAll("button").Single(b => b.TextContent == "Show" && b.GetAttribute("aria-expanded") == "false").Click();
        var before = RendersPerPage(panel);

        cut.InvokeAsync(() => cut.Instance.Increment());
        panel.Find("button[aria-label='Refresh captured data']").Click();

        panel.WaitForAssertion(() => Assert.AreEqual(before + 2, RendersPerPage(panel), "the parent and its child, a second later"), TimeSpan.FromSeconds(10));
    }

    private static int RendersPerPage(IRenderedComponent<BevToolsPanel> panel) => panel.FindAll(".bev-section")
        .Single(s => s.TextContent.Contains("Costliest there", StringComparison.Ordinal))
        .QuerySelectorAll("tbody tr")
        .Sum(row => int.Parse(row.QuerySelectorAll("td")[2].TextContent.Trim(), System.Globalization.CultureInfo.InvariantCulture));
}

public class RoutedShell : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Router>(0);
        builder.AddAttribute(1, nameof(Router.AppAssembly), typeof(RoutedShell).Assembly);
        builder.AddAttribute(2, nameof(Router.Found), (RenderFragment<RouteData>)(routeData => b =>
        {
            b.OpenComponent<RouteView>(0);
            b.AddAttribute(1, nameof(RouteView.RouteData), routeData);
            b.CloseComponent();
        }));
        builder.CloseComponent();
        builder.OpenComponent<BevToolsPanel>(4);
        builder.CloseComponent();
    }
}

[Route("/bev-tests/route-a")]
public class RoutedPageA : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "page A");
}

[Route("/bev-tests/route-b")]
public class RoutedPageB : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, "page B");
        builder.OpenComponent<RoutedWidget>(1);
        builder.CloseComponent();
    }
}

public class RoutedWidget : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "widget");
}
