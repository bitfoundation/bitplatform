using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>
/// Bit.BevTools.Loader, as an app that lazy loads BevTools uses it. Bit.BevTools is always loadable in a test process,
/// so "lazy" is RegisterAtStartup = false here: the path a lazy loaded Bit.BevTools takes, minus the download.
/// </summary>
[TestClass]
public class LoaderTests : BunitContext
{
    public LoaderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { Online = true, Visibility = "visible" });
        module.Setup<string?>("loadPrefs", _ => true).SetResult("""{"open":false}""");
    }

    [TestMethod]
    public void Waiting_for_an_attach_installs_the_hooks_and_nothing_of_BevTools()
    {
        Services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);

        Assert.IsNull(Services.GetService<BevToolsSession>());
        Assert.IsInstanceOfType<BevToolsActivatorHook>(Services.GetService<IComponentActivator>());
        Assert.IsTrue(Services.GetServices<IHttpMessageHandlerBuilderFilter>().Any(f => f is BevToolsHttpHookFilter));

        var cut = RenderHost();

        Assert.IsFalse(cut.Instance.Loader.IsActive);
        Assert.IsEmpty(cut.FindAll(".bev-root"));
    }

    [TestMethod]
    public async Task Opening_attaches_BevTools_through_the_startup_hooks_and_shows_its_panel()
    {
        Services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        var cut = RenderHost();

        Assert.IsTrue(await Renderer.Dispatcher.InvokeAsync(() => cut.Instance.Loader.OpenAsync()));

        var session = cut.Instance.Session;
        Assert.IsTrue(session.IsAttachedAtRuntime);
        Assert.IsTrue(session.Ui.IsOpen, "a saved preference to keep the panel closed must not win over the app opening it");
        cut.WaitForAssertion(() => Assert.IsNotEmpty(cut.FindAll(".bev-panel")));
        Assert.IsTrue(session.Attachment!.ActivatorHooked, "the loader's activator hook makes reflection over the renderer unnecessary");

        session.Components.Refresh(force: true);
        var parent = session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        Assert.AreEqual(0, parent.RenderCount);
        Assert.IsTrue(parent.IsInstrumented);

        await cut.InvokeAsync(cut.Instance.ShowLateChild);
        session.Components.Refresh(force: true);
        var late = session.Components.All.Single(r => r.Type == typeof(ChildComponent) && r.Parent?.Type == typeof(LoaderHost));
        Assert.AreEqual(1, late.RenderCount);
        Assert.AreEqual(BevRenderCause.Initial, late.LastRender!.Cause);
        Assert.AreEqual(BevCapabilityStatus.Available, session.Capabilities.Single(c => c.Name == "Component instances").Status);

        // The loader's panel host lives in another assembly, but in BevTools' namespace: it is BevTools, not the app.
        Assert.IsTrue(session.Components.All.Where(r => r.Type == typeof(BevToolsLoaderPanel)).All(r => r.IsBevTools));
        Assert.DoesNotContain(r => r.Type == typeof(BevToolsLoaderPanel), session.Components.BuildTree(includeHidden: true).Select(n => n.Record));
    }

    [TestMethod]
    public async Task An_attached_session_shows_the_service_collection_the_loader_kept_at_startup()
    {
        Services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        Services.AddScoped<LoaderTestService>();
        var cut = RenderHost();

        await Renderer.Dispatcher.InvokeAsync(() => cut.Instance.Loader.OpenAsync());

        var session = cut.Instance.Session;
        Assert.IsNotNull(session.Registry.ServiceGraph);
        Assert.IsTrue(session.Registry.ServiceGraph.Services.Any(s => s.ServiceType == typeof(LoaderTestService)));
        Assert.AreEqual(BevCapabilityStatus.Available, session.Capabilities.Single(c => c.Area == "DI" && c.Name.StartsWith("Registrations", StringComparison.Ordinal)).Status);
    }

    [TestMethod]
    public async Task The_hook_handler_passes_requests_through_until_the_attach_and_records_them_once_after()
    {
        var transport = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        Services.AddScoped(_ => new HttpClient(new BevToolsHttpHookHandler(transport)) { BaseAddress = new Uri("https://example.com/") });
        var cut = RenderHost();
        var client = cut.Instance.Services.GetRequiredService<HttpClient>();

        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("before")).StatusCode);
        await Renderer.Dispatcher.InvokeAsync(() => cut.Instance.Loader.OpenAsync());
        var session = cut.Instance.Session;
        using (session.UseAmbient())
        {
            await client.GetAsync("after");
        }

        Assert.AreEqual(2, transport.Calls);
        Assert.AreEqual(BevHttpClientAttachment.InPipeline, session.Attachment!.HttpClient, "a pipeline that already records must not be wrapped a second time");
        Assert.AreEqual(1, session.Http.Count);
        Assert.AreEqual(1, session.Http.FailedCount);
    }

    [TestMethod]
    public async Task IHttpClientFactory_clients_are_recorded_through_the_hooks_the_loader_added_at_startup()
    {
        var transport = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        Services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        Services.AddHttpClient("api").ConfigurePrimaryHttpMessageHandler(() => transport);
        var cut = RenderHost();

        await Renderer.Dispatcher.InvokeAsync(() => cut.Instance.Loader.OpenAsync());
        var session = cut.Instance.Session;
        using (session.UseAmbient())
        {
            await Services.GetRequiredService<IHttpClientFactory>().CreateClient("api").GetAsync("https://example.com/items");
        }

        Assert.AreEqual(1, session.Http.Count);
        Assert.AreEqual(BevCapabilityStatus.Partial, session.Capabilities.Single(c => c.Area == "Network" && c.Name == "HTTP requests").Status);
    }

    [TestMethod]
    public async Task An_HttpClient_from_IHttpClientFactory_registered_in_DI_is_recorded_once()
    {
        var transport = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        Services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        Services.AddHttpClient("api", c => c.BaseAddress = new Uri("https://example.com/")).ConfigurePrimaryHttpMessageHandler(() => transport);
        Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));
        var cut = RenderHost();
        var client = cut.Instance.Services.GetRequiredService<HttpClient>();

        await Renderer.Dispatcher.InvokeAsync(() => cut.Instance.Loader.OpenAsync());
        var session = cut.Instance.Session;
        using (session.UseAmbient())
        {
            await client.GetAsync("items");
        }

        Assert.AreEqual(BevHttpClientAttachment.InPipeline, session.Attachment!.HttpClient, "the factory's pipeline records through the loader's hook already");
        Assert.AreEqual(1, session.Http.Count);
    }

    [TestMethod]
    public async Task A_BevTools_registered_at_startup_and_left_off_does_not_stop_an_attach()
    {
        var services = ScopeServices(environment: "Production");
        services.AddBitBevToolsServices();
        services.AddBitBevToolsLoaderServices(o =>
        {
            o.AllowInMultiUserHost = true;
            o.Authorize = _ => ValueTask.FromResult(true);
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<BevToolsLoader>();
        Assert.IsFalse(scope.ServiceProvider.GetRequiredService<BevToolsSession>().IsEnabled);

        Assert.IsTrue(loader.CanOpen);
        Assert.IsTrue(await loader.OpenAsync(), loader.Problem);

        var session = RuntimeAttachment.Resolve(scope.ServiceProvider)!;
        Assert.IsTrue(session.IsEnabled);
        Assert.IsTrue(session.IsAttachedAtRuntime);
    }

    [TestMethod]
    public async Task Every_scope_the_loader_opens_shares_one_registry_and_service_graph()
    {
        var services = ScopeServices();
        services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        services.AddScoped<LoaderTestService>();
        using var provider = services.BuildServiceProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        await first.ServiceProvider.GetRequiredService<BevToolsLoader>().OpenAsync();
        await second.ServiceProvider.GetRequiredService<BevToolsLoader>().OpenAsync();
        var one = RuntimeAttachment.Resolve(first.ServiceProvider)!;
        var other = RuntimeAttachment.Resolve(second.ServiceProvider)!;

        Assert.AreNotSame(one, other);
        Assert.AreSame(one.Registry, other.Registry);
        Assert.AreSame(one.Registry.ServiceGraph, other.Registry.ServiceGraph);
    }

    [TestMethod]
    public void A_bare_attach_in_an_app_with_the_loader_uses_its_startup_hooks()
    {
        var services = ScopeServices();
        services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        services.AddScoped<LoaderTestService>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var session = BitBevTools.Attach(scope.ServiceProvider);

        Assert.IsTrue(session.Attachment!.FactoryClientsHooked);
        Assert.IsTrue(session.Registry.ServiceGraph!.Services.Any(s => s.ServiceType == typeof(LoaderTestService)));
        Assert.AreEqual(BevCapabilityStatus.Available, session.Capabilities.Single(c => c.Area == "DI" && c.Name.StartsWith("Registrations", StringComparison.Ordinal)).Status);
        StringAssert.Contains(session.Capabilities.Single(c => c.Area == "Network" && c.Name == "HTTP requests").Explanation, "IHttpClientFactory clients go through the handlers Bit.BevTools.Loader added");
        session.Dispose();
    }

    [TestMethod]
    public async Task A_Bit_BevTools_of_another_version_is_reported_instead_of_thrown()
    {
        var services = ScopeServices();
        services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        var context = Context(services);
        var mismatched = new ThrowingEntryPoint(new MissingMethodException("Method not found: 'Bit.BevTools.BevToolsLoaderContext.Something'."));
        context.Deferred(mismatched, "test");
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<BevToolsLoader>();

        Assert.IsFalse(context.TryAddServices(mismatched, new ServiceCollection(), out var reason));
        StringAssert.Contains(reason, "reference the same version of both packages");
        Assert.IsFalse(await loader.OpenAsync());
        StringAssert.Contains(loader.Problem, "reference the same version of both packages");
    }

    [TestMethod]
    public async Task Whatever_attaching_fails_with_becomes_the_problem_OpenAsync_reports()
    {
        var services = ScopeServices();
        services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        Context(services).Deferred(new ThrowingEntryPoint(new InvalidOperationException("boom")), "test");
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<BevToolsLoader>();

        Assert.IsFalse(await loader.OpenAsync());
        StringAssert.Contains(loader.Problem, "boom");
        Assert.IsFalse(loader.IsActive);
    }

    [TestMethod]
    public void A_transient_HttpClient_is_not_wrapped_and_the_report_says_how_to_record_it()
    {
        var services = ScopeServices();
        services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        services.AddTransient(_ => new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var session = BitBevTools.Attach(scope.ServiceProvider);

        Assert.AreEqual(BevHttpClientAttachment.Transient, session.Attachment!.HttpClient);
        StringAssert.Contains(session.Capabilities.Single(c => c.Area == "Network" && c.Name == "HTTP requests").Explanation, "BevToolsHttpHookHandler");
        session.Dispose();
    }

    [TestMethod]
    public void Registering_at_startup_registers_BevTools_itself_and_renders_its_panel_from_the_start()
    {
        Services.AddBitBevToolsLoaderServices(o =>
        {
            o.RegisterAtStartup = true;
            o.ShowBadge = false;
            o.ToggleShortcut = "Ctrl+Alt+B";
        });

        var session = Services.GetRequiredService<BevToolsSession>();
        Assert.IsTrue(session.IsEnabled);
        Assert.IsFalse(session.IsAttachedAtRuntime);
        Assert.IsFalse(session.Options.Ui.ShowBadge);
        Assert.AreEqual("Ctrl+Alt+B", session.Options.Ui.ToggleShortcut);
        Assert.IsNotInstanceOfType<BevToolsActivatorHook>(Services.GetService<IComponentActivator>());

        var cut = RenderHost();

        Assert.IsTrue(cut.Instance.Loader.IsActive);
        Assert.IsNotEmpty(cut.FindAll(".bev-root"));
    }

    [TestMethod]
    public void By_default_BevTools_registers_at_startup_in_Development_only()
    {
        var development = new ServiceCollection().AddSingleton<IHostEnvironment>(new StubEnvironment("Development")).AddBitBevToolsLoaderServices();
        var production = new ServiceCollection().AddSingleton<IHostEnvironment>(new StubEnvironment("Production")).AddBitBevToolsLoaderServices();

        Assert.IsTrue(development.Any(d => d.ServiceType == typeof(BevToolsSession)));
        Assert.IsFalse(production.Any(d => d.ServiceType == typeof(BevToolsSession)));
        Assert.IsTrue(production.Any(d => d.ServiceType == typeof(BevToolsActivatorHook)));
        StringAssert.Contains(Context(production).StartupDecision, "Production");
    }

    [TestMethod]
    public void A_BevTools_the_app_registered_itself_is_the_one_the_loader_uses()
    {
        var services = new ServiceCollection().AddBitBevToolsServices(o => o.Enabled = true).AddBitBevToolsLoaderServices();

        Assert.IsTrue(Context(services).RegisteredAtStartup);
        StringAssert.Contains(Context(services).StartupDecision, "AddBitBevToolsServices");
        Assert.IsFalse(services.Any(d => d.ServiceType == typeof(BevToolsActivatorHook)));
    }

    [TestMethod]
    public void Calling_the_registration_again_configures_the_options_it_already_has()
    {
        var services = new ServiceCollection()
            .AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false)
            .AddBitBevToolsLoaderServices(o => o.AllowInMultiUserHost = true);

        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(BevToolsLoaderContext)));
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(BevToolsActivatorHook)));
        Assert.IsTrue(Context(services).Options.AllowInMultiUserHost);
    }

    [TestMethod]
    public async Task The_attached_session_ends_with_the_scope_that_opened_it()
    {
        var services = ScopeServices();
        services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        using var provider = services.BuildServiceProvider();
        BevToolsSession session;
        using (var scope = provider.CreateScope())
        {
            Assert.IsTrue(await scope.ServiceProvider.GetRequiredService<BevToolsLoader>().OpenAsync());
            session = RuntimeAttachment.Resolve(scope.ServiceProvider)!;
            Assert.IsFalse(session.IsDisposed);
        }

        Assert.IsTrue(session.IsDisposed);
    }

    [TestMethod]
    public async Task In_a_server_process_outside_Development_BevTools_is_not_opened()
    {
        // Whatever JS runtime the scope has, a decorated one included: the server is told by its IServer, as the attach tells it.
        var services = ScopeServices(environment: "Production");
        services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<BevToolsLoader>();

        Assert.IsFalse(loader.CanOpen);
        Assert.IsFalse(await loader.OpenAsync());
        StringAssert.Contains(loader.Problem, "AllowInMultiUserHost");
        Assert.IsNull(RuntimeAttachment.Resolve(scope.ServiceProvider));
    }

    [TestMethod]
    [DataRow("Development", false)]
    [DataRow("Production", true)]
    public async Task In_a_server_process_BevTools_opens_in_Development_or_when_allowed(string environment, bool allow)
    {
        var services = ScopeServices(environment: environment);
        services.AddBitBevToolsLoaderServices(o =>
        {
            o.RegisterAtStartup = false;
            o.AllowInMultiUserHost = allow;
            if (allow)
            {
                o.Authorize = _ => ValueTask.FromResult(true);
            }
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<BevToolsLoader>();

        Assert.IsTrue(loader.CanOpen);
        Assert.IsTrue(await loader.OpenAsync());
        Assert.IsTrue(RuntimeAttachment.Resolve(scope.ServiceProvider)!.IsEnabled);
    }

    [TestMethod]
    public async Task Allowing_it_in_a_server_process_outside_Development_takes_an_Authorize_callback_too()
    {
        var services = ScopeServices(environment: "Production");
        services.AddBitBevToolsLoaderServices(o =>
        {
            o.RegisterAtStartup = false;
            o.AllowInMultiUserHost = true;
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<BevToolsLoader>();

        Assert.IsFalse(loader.CanOpen);
        Assert.IsFalse(await loader.OpenAsync());
        StringAssert.Contains(loader.Problem, "Authorize");
        Assert.IsNull(RuntimeAttachment.Resolve(scope.ServiceProvider));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Authorize_refusing_or_throwing_leaves_BevTools_unloaded(bool throws)
    {
        var asked = 0;
        var services = ScopeServices(environment: "Production");
        services.AddBitBevToolsLoaderServices(o =>
        {
            o.RegisterAtStartup = false;
            o.AllowInMultiUserHost = true;
            o.Authorize = _ =>
            {
                asked++;
                return throws ? throw new InvalidOperationException("no user") : ValueTask.FromResult(false);
            };
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<BevToolsLoader>();

        Assert.IsTrue(loader.CanOpen, "the policy allows it; the callback is what refuses");
        Assert.IsFalse(await loader.IsAuthorizedAsync());
        Assert.IsFalse(await loader.OpenAsync());
        StringAssert.Contains(loader.Problem, throws ? "threw InvalidOperationException" : "did not accept");
        Assert.AreEqual(2, asked, "asked again on open, never answered from a cache");
        Assert.IsNull(RuntimeAttachment.Resolve(scope.ServiceProvider));
    }

    [TestMethod]
    public async Task A_session_opened_outside_Development_in_a_server_process_reads_no_values_unless_told_to()
    {
        var services = ScopeServices(environment: "Production");
        services.AddBitBevToolsLoaderServices(o =>
        {
            o.RegisterAtStartup = false;
            o.AllowInMultiUserHost = true;
            o.Authorize = _ => ValueTask.FromResult(true);
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsTrue(await scope.ServiceProvider.GetRequiredService<BevToolsLoader>().OpenAsync());
        var session = RuntimeAttachment.Resolve(scope.ServiceProvider)!;

        Assert.AreEqual(BevValueAccess.None, session.ValueAccess);
        var node = session.Inspector.Describe("Customer", new { Name = "Ada" });
        Assert.AreEqual(BevRedactor.HiddenValue, node.Display);
        Assert.IsFalse(node.IsExpandable);
    }

    [TestMethod]
    public void A_bare_attach_in_a_server_process_outside_Development_stays_off_unless_enabled_explicitly()
    {
        using var provider = ScopeServices(environment: "Production").BuildServiceProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var off = BitBevTools.Attach(first.ServiceProvider);
        var on = BitBevTools.Attach(second.ServiceProvider, o => o.Enabled = true);

        Assert.IsFalse(off.IsEnabled);
        StringAssert.Contains(off.Registry.EnabledReason, "serves many users");
        Assert.IsTrue(on.IsEnabled);
        on.Dispose();
    }

    [TestMethod]
    public void The_activator_hook_creates_components_like_the_framework_until_BevTools_attaches()
    {
        var services = ScopeServices();
        services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var hook = (BevToolsActivatorHook)scope.ServiceProvider.GetRequiredService<IComponentActivator>();

        Assert.IsInstanceOfType<ParentComponent>(hook.CreateInstance(typeof(ParentComponent)));
        Assert.ThrowsExactly<ArgumentException>(() => hook.CreateInstance(typeof(string)));
        Assert.AreSame(hook, scope.ServiceProvider.GetRequiredService<IComponentActivator>(), "a renderer and the BevTools attaching to it must meet the same hook");
    }

    [TestMethod]
    public void BevTools_creates_components_as_the_framework_does_on_each_runtime()
    {
        using var provider = new ServiceCollection().AddBitBevToolsServices(o => o.Enabled = true).AddScoped<LoaderTestService>().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var activator = scope.ServiceProvider.GetRequiredService<IComponentActivator>();
        Assert.IsInstanceOfType<BevToolsComponentActivator>(activator);

        Assert.IsNotNull(((ConstructorInjected)activator.CreateInstance(typeof(ConstructorInjected))).Service);
    }

    [TestMethod]
    public void An_activator_registered_before_the_loader_keeps_creating_the_components()
    {
        var custom = new CountingActivator();
        var services = ScopeServices();
        services.AddSingleton<IComponentActivator>(custom);
        services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = false);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IComponentActivator>().CreateInstance(typeof(ParentComponent));

        Assert.AreEqual(1, custom.Created);
    }

    /// <summary>After the services: bUnit takes no registrations once its renderer exists.</summary>
    private IRenderedComponent<LoaderHost> RenderHost()
    {
        this.UseRenderer("WebAssembly");
        return Render<LoaderHost>();
    }

    private static BevToolsLoaderContext Context(IServiceCollection services) =>
        (BevToolsLoaderContext)services.Single(d => d.ServiceType == typeof(BevToolsLoaderContext)).ImplementationInstance!;

    private static ServiceCollection ScopeServices(IJSRuntime? js = null, string? environment = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(js ?? new FakeJSRuntime());
        if (environment is not null)
        {
            services.AddSingleton<IHostEnvironment>(new StubEnvironment(environment));
            // Only its registration is consulted: it is what tells BevTools this process serves many users.
            services.AddSingleton<Microsoft.AspNetCore.Hosting.Server.IServer>(_ => throw new InvalidOperationException("not resolved"));
        }

        return services;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult(respond(request));
        }
    }

    /// <summary>A Bit.BevTools that fails once the loader calls into it, as one built against another loader does.</summary>
    private sealed class ThrowingEntryPoint(Exception exception) : IBevToolsEntryPoint
    {
        public Type PanelType => typeof(BevToolsPanel);

        public bool TryAddServices(IServiceCollection services, BevToolsLoaderContext context, out string reason) => throw exception;

        public IBevToolsAttachment Attach(IServiceProvider services, BevToolsLoaderContext context) => throw exception;
    }

    private sealed class CountingActivator : IComponentActivator
    {
        public int Created;

        public IComponent CreateInstance(Type componentType)
        {
            Created++;
            return (IComponent)Activator.CreateInstance(componentType)!;
        }
    }

    private sealed class StubEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Bit.BevTools.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class LoaderTestService;

    private sealed class ConstructorInjected(LoaderTestService service) : ComponentBase
    {
        public LoaderTestService Service { get; } = service;
    }
}

/// <summary>An app's layout as it uses the loader: its own components, a way to open BevTools, and the loader's panel.</summary>
public class LoaderHost : ComponentBase
{
    [Inject] public BevToolsLoader Loader { get; set; } = default!;

    [Inject] public IServiceProvider Services { get; set; } = default!;

    public BevToolsSession Session => RuntimeAttachment.Resolve(Services)!;

    private bool _lateChild;

    public void ShowLateChild()
    {
        _lateChild = true;
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

        builder.OpenComponent<BevToolsLoaderPanel>(4);
        builder.CloseComponent();
    }
}
