using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>Guards the cost of instrumentation. Thresholds are loose (Debug builds, shared CI) but catch order-of-magnitude regressions.</summary>
[TestClass]
public class OverheadTests : BunitContext
{
    public OverheadTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.MaxRendersPerComponent = 50;
        });
    }

    [TestMethod]
    public void Per_render_bookkeeping_stays_in_the_microsecond_range()
    {
        var cut = Render<ParentComponent>();
        var session = Services.GetRequiredService<BevToolsSession>();
        for (var i = 0; i < 500; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Refresh());
        }

        var overhead = session.Overhead;
        // 1 initial parent + child render, then 500 parent-only refreshes (child parameters do not change).
        Assert.IsTrue(overhead.AppRenderCount >= 502, $"expected at least 502 app renders, got {overhead.AppRenderCount}");
        Assert.IsTrue(overhead.InstrumentationPerRenderMicroseconds < 500, $"instrumentation cost {overhead.InstrumentationPerRenderMicroseconds:0} µs per render is too high");
    }

    [TestMethod]
    public void Overhead_of_a_measured_pass_is_reported_in_real_milliseconds()
    {
        // The meter takes Stopwatch ticks. Diagnostics, inspection and tree refreshes used to pass TimeSpan ticks,
        // which coincide only on Windows: on Linux, macOS and WebAssembly those figures read 100× too small.
        var (session, scope) = TestHelpers.CreateSession(o => o.Extensions.Add(new SlowRuleExtension()));
        using (scope)
        {
            session.Diagnostics.Evaluate(force: true);

            Assert.IsTrue(session.Overhead.DiagnosticsMs is >= 15 and < 5000, $"a 20 ms rule was reported as {session.Overhead.DiagnosticsMs:0.###} ms");
        }
    }

    private sealed class SlowRuleExtension : IBevToolsExtension
    {
        public string Name => "slow rule";

        public void Configure(IBevToolsExtensionBuilder builder) => builder.AddDiagnosticRule(new SlowRule());
    }

    private sealed class SlowRule : IBevDiagnosticRule
    {
        public string Id => "test.slow";

        public string Title => "Slow";

        public string Description => "";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < 20)
            {
                Thread.Sleep(1);
            }

            return [];
        }
    }

    [TestMethod]
    public void Buffers_stay_bounded_under_sustained_activity()
    {
        var cut = Render<ParentComponent>();
        var session = Services.GetRequiredService<BevToolsSession>();
        for (var i = 0; i < 2000; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Refresh());
        }

        Assert.IsTrue(session.Timeline.Count <= session.Options.MaxEvents);
        var parent = session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        Assert.AreEqual(50, parent.Renders.Length);
        Assert.AreEqual(2001, parent.RenderCount);
    }

    [TestMethod]
    public void Disposed_component_records_are_released()
    {
        var cut = Render<ParentComponent>(ps => ps.Add(p => p.ShowChild, true));
        var session = Services.GetRequiredService<BevToolsSession>();
        for (var i = 0; i < 20; i++)
        {
            cut.Render(ps => ps.Add(p => p.ShowChild, i % 2 == 0));
        }

        session.Components.Refresh(force: true);
        var childRecords = session.Components.All.Where(r => r.Type == typeof(ChildComponent)).ToList();
        Assert.IsTrue(childRecords.Count(r => !r.IsDisposed) <= 1);
        Assert.IsTrue(session.Components.LiveCount <= 2, "only live components stay in the identity map");
        foreach (var r in childRecords.Where(r => r.IsDisposed))
        {
            Assert.IsNull(r.Component);
        }
    }

    [TestMethod]
    public void Disabled_bevtools_adds_no_activator_or_handlers()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServices(o => o.Enabled = false);
        Assert.DoesNotContain(d => d.ServiceType == typeof(Microsoft.AspNetCore.Components.IComponentActivator), services);
        Assert.DoesNotContain(d => d.ServiceType == typeof(Microsoft.Extensions.Http.IHttpMessageHandlerBuilderFilter), services);
        Assert.DoesNotContain(d => d.ServiceType == typeof(Microsoft.Extensions.Logging.ILoggerProvider), services);
    }

    [TestMethod]
    public void Devtools_is_off_by_default_without_a_development_environment()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServices();
        using var provider = services.BuildServiceProvider();
        Assert.IsFalse(provider.GetRequiredService<BevToolsRegistry>().IsEnabled);
        Assert.IsFalse(provider.GetRequiredService<IBevToolsTimeline>().IsEnabled);
    }
}
