using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools.Tests;

[TestClass]
public class RuntimeAttachTests : BunitContext
{
    public RuntimeAttachTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { Online = true, Visibility = "visible" });
        module.Setup<string?>("loadPrefs", _ => true).SetResult("""{"open":false}""");
        this.UseRenderer("WebAssembly");
    }

    [TestCleanup]
    public void DisposeAttachedSession() => (Services.GetService<BevToolsSession>() ?? Attached)?.Dispose();

    private BevToolsSession? Attached { get; set; }

    [TestMethod]
    public void Reflection_accessors_for_attaching_are_available_on_this_framework()
    {
        Assert.IsNotNull(BlazorReflection.ComponentFactoryField);
        Assert.IsNotNull(BlazorReflection.ComponentActivatorField);
        Assert.IsNotNull(BlazorReflection.ComponentStateByIdField);
        Assert.IsNotNull(BlazorReflection.RendererDisposedField);
        Assert.IsNotNull(BlazorReflection.HasNeverRenderedField);
        Assert.IsNotNull(BlazorReflection.HasPendingQueuedRenderField);
    }

    [TestMethod]
    public void The_panel_tracks_the_components_that_existed_before_it_was_attached_and_the_ones_created_after()
    {
        var cut = Render<LazyHost>();
        cut.InvokeAsync(cut.Instance.Open);
        Attached = cut.Instance.Session!;
        var session = Attached;

        Assert.IsTrue(session.IsAttachedAtRuntime);
        Assert.IsTrue(session.IsEnabled);
        Assert.IsTrue(session.Ui.IsOpen);
        Assert.IsNotEmpty(cut.FindAll(".bev-panel"));

        session.Components.Refresh(force: true);
        var parent = session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        var child = session.Components.All.Single(r => r.Type == typeof(ChildComponent));
        Assert.AreSame(parent, child.Parent);
        Assert.AreEqual(0, parent.RenderCount);
        Assert.IsTrue(parent.IsInstrumented);
        Assert.DoesNotContain(e => e.Title.EndsWith(" created", StringComparison.Ordinal) && e.ComponentInstanceId == parent.InstanceId, session.Timeline.Snapshot());

        var parentComponent = cut.FindComponent<ParentComponent>().Instance;
        cut.InvokeAsync(parentComponent.Increment);
        Assert.AreEqual(1, parent.RenderCount);
        Assert.AreEqual(BevRenderCause.StateHasChanged, parent.LastRender!.Cause);
        Assert.AreEqual(BevRenderCause.ParentRender, child.LastRender!.Cause);

        cut.InvokeAsync(cut.Instance.ShowLateChild);
        session.Components.Refresh(force: true);
        var late = session.Components.All.Single(r => r.Type == typeof(ChildComponent) && r != child);
        Assert.AreEqual(1, late.RenderCount);
        Assert.AreEqual(BevRenderCause.Initial, late.LastRender!.Cause);
        Assert.AreEqual(typeof(LazyHost), late.Parent?.Type);
    }

    [TestMethod]
    public void A_second_attach_returns_the_same_session_and_reopens_nothing_twice()
    {
        var cut = Render<LazyHost>();
        cut.InvokeAsync(cut.Instance.Open);
        Attached = cut.Instance.Session!;
        var trackedBefore = Attached.Components.TrackedCount;

        cut.InvokeAsync(cut.Instance.Open);

        Assert.AreSame(Attached, cut.Instance.Session);
        Assert.AreEqual(trackedBefore, Attached.Components.TrackedCount);
    }

    [TestMethod]
    public void The_capability_report_says_what_an_attached_session_can_not_see()
    {
        var cut = Render<LazyHost>();
        cut.InvokeAsync(cut.Instance.Open);
        Attached = cut.Instance.Session!;

        var capabilities = Attached.Capabilities;
        Assert.AreEqual(BevCapabilityStatus.Available, capabilities.Single(c => c.Name == "Component instances").Status);
        Assert.AreEqual(BevCapabilityStatus.NotAvailable, capabilities.Single(c => c.Area == "DI" && c.Name.StartsWith("Registrations", StringComparison.Ordinal)).Status);
        Assert.AreEqual(BevCapabilityStatus.NotAvailable, capabilities.Single(c => c.Area == "Circuit" && c.Name.StartsWith("Circuit lifecycle", StringComparison.Ordinal)).Status);
    }

    [TestMethod]
    public void Without_an_HttpClient_in_DI_the_capability_report_says_requests_need_the_handler()
    {
        using var scope = new ServiceCollection().BuildServiceProvider().CreateScope();

        Attached = BitBevTools.Attach(scope.ServiceProvider);

        Assert.AreEqual(BevCapabilityStatus.RequiresIntegration, Attached.Capabilities.Single(c => c.Area == "Network" && c.Name == "HTTP requests").Status);
    }

    [TestMethod]
    public void A_panel_with_BevTools_neither_registered_nor_attached_says_how_to_get_it()
    {
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => Render<BevToolsPanel>());

        Assert.Contains("BitBevTools.Attach", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void The_attached_session_ends_with_the_renderer()
    {
        BevToolsSession session;
        using (var context = new BunitContext())
        {
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.UseRenderer("WebAssembly");
            var cut = context.Render<LazyHost>();
            cut.InvokeAsync(cut.Instance.Open);
            session = cut.Instance.Session!;
            Assert.IsFalse(session.IsDisposed);
        }

        Assert.IsTrue(session.IsDisposed);
    }

    [TestMethod]
    public void An_attached_session_outlives_its_panel_and_ends_with_its_scope()
    {
        BevToolsSession session;
        using (var context = new BunitContext())
        {
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.UseRenderer("WebAssembly");
            var cut = context.Render<LazyHost>();
            cut.InvokeAsync(cut.Instance.Open);
            session = cut.Instance.Session!;

            // The user navigates away from the page that rendered the panel; the scope goes on.
            cut.InvokeAsync(cut.Instance.HidePanel);
            Assert.IsEmpty(cut.FindComponents<BevToolsPanel>());
            RuntimeAttachment.SweepEndedScopes();
            Assert.IsFalse(session.IsDisposed);
        }

        RuntimeAttachment.SweepEndedScopes();

        Assert.IsTrue(session.IsDisposed);
    }

    [TestMethod]
    public void A_session_attached_without_a_panel_ends_with_its_scope()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var scope = provider.CreateScope();
        var session = BitBevTools.Attach(scope.ServiceProvider);

        scope.Dispose();
        RuntimeAttachment.SweepEndedScopes();

        Assert.IsTrue(session.IsDisposed);
    }

    [TestMethod]
    public void Attach_runs_the_app_s_configure_callback_outside_its_lock()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        BevToolsSession? other = null;

        // A callback that waits on an attach of another scope, made on another thread, deadlocked inside the lock.
        Attached = BitBevTools.Attach(first.ServiceProvider, _ =>
            Assert.IsTrue(Task.Run(() => other = BitBevTools.Attach(second.ServiceProvider)).Wait(TimeSpan.FromSeconds(10))));

        Assert.IsNotNull(other);
        other.Dispose();
    }

    [TestMethod]
    public void Attach_returns_the_registered_session_when_BevTools_was_registered()
    {
        var provider = new ServiceCollection().AddBitBevToolsServices(o => o.Enabled = true).BuildServiceProvider();
        using var scope = provider.CreateScope();

        var session = BitBevTools.Attach(scope.ServiceProvider);

        Assert.AreSame(scope.ServiceProvider.GetRequiredService<BevToolsSession>(), session);
        Assert.IsFalse(session.IsAttachedAtRuntime);
    }

    [TestMethod]
    public void Attach_honors_an_explicit_opt_out()
    {
        using var scope = new ServiceCollection().BuildServiceProvider().CreateScope();

        Attached = BitBevTools.Attach(scope.ServiceProvider, o => o.Enabled = false);

        Assert.IsTrue(Attached.IsAttachedAtRuntime);
        Assert.IsFalse(Attached.IsEnabled);
    }

    [TestMethod]
    public async Task Attach_records_the_requests_of_the_HttpClient_registered_in_DI()
    {
        var provider = new ServiceCollection()
            .AddScoped(_ => new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))) { BaseAddress = new Uri("https://example.com/") })
            .BuildServiceProvider();
        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<HttpClient>();
        await client.GetAsync("before");

        Attached = BitBevTools.Attach(scope.ServiceProvider);
        using (Attached.UseAmbient())
        {
            await client.GetAsync("after");
        }

        Assert.AreEqual(1, Attached.Http.Count);
        Assert.AreEqual(1, Attached.Http.FailedCount);
        Assert.AreEqual(BevCapabilityStatus.Partial, Attached.Capabilities.Single(c => c.Area == "Network" && c.Name == "HTTP requests").Status);
    }

    [TestMethod]
    public void Attach_records_the_framework_errors_logged_after_it()
    {
        var provider = new ServiceCollection().AddLogging().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Microsoft.AspNetCore.Components.RenderTree.Renderer");

        Attached = BitBevTools.Attach(scope.ServiceProvider);
        using (Attached.UseAmbient())
        {
            logger.LogError(new InvalidOperationException("boom"), "Unhandled exception rendering component");
        }

        Assert.AreEqual(1, Attached.Errors.Count);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}

public class LazyHost : ComponentBase
{
    [Inject] public IServiceProvider Services { get; set; } = default!;

    public BevToolsSession? Session { get; private set; }

    private bool _panel;

    private bool _lateChild;

    public void Open()
    {
        Session = BitBevTools.Attach(Services);
        Session.Ui.Open();
        _panel = true;
        StateHasChanged();
    }

    public void ShowLateChild()
    {
        _lateChild = true;
        StateHasChanged();
    }

    public void HidePanel()
    {
        _panel = false;
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<ParentComponent>(0);
        builder.AddComponentParameter(1, nameof(ParentComponent.Value), 1);
        builder.CloseComponent();
        if (_lateChild)
        {
            builder.OpenComponent<ChildComponent>(2);
            builder.AddComponentParameter(3, nameof(ChildComponent.Text), "late");
            builder.CloseComponent();
        }

        if (_panel)
        {
            builder.OpenComponent<BevToolsPanel>(4);
            builder.CloseComponent();
        }
    }
}
