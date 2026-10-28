using System.Diagnostics;
using Bit.Bcrdt.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// Where the components take their collaboration service from. An app that lazy loads Bit.Bcrdt on WebAssembly cannot
/// call AddBitBcrdtServices, so it configures the service through GetBcrdtCollaborationService instead; an app that did
/// register keeps exactly what it registered, and everywhere but WebAssembly the registration stays required.
/// </summary>
[TestClass]
public class LazyLoadingTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(15);
    private static readonly BcrdtSyncProviderOptions _noHeartbeat = new() { HeartbeatInterval = TimeSpan.Zero };

    private static async Task AssertEventuallyAsync(Func<bool> condition, string because)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            Assert.IsTrue(clock.Elapsed < Patience, because);
            await Task.Delay(10);
        }
    }

    [TestMethod]
    public async Task An_app_that_registered_bcrdt_keeps_its_own_service()
    {
        await using ServiceProvider app = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime()).AddBitBcrdtServices().BuildServiceProvider();
        await using AsyncServiceScope circuit = app.CreateAsyncScope();

        IBcrdtCollaborationService service = circuit.ServiceProvider.GetBcrdtCollaborationService(_ => Assert.Fail("the registration's configuration wins"));

        Assert.AreSame(circuit.ServiceProvider.GetRequiredService<IBcrdtCollaborationService>(), service);
    }

    [TestMethod]
    public void The_configured_service_is_one_per_WebAssembly_runtime()
    {
        var jsRuntime = new WebAssemblyJSRuntime();
        using ServiceProvider first = Connection(jsRuntime);
        using ServiceProvider second = Connection(jsRuntime);
        using ServiceProvider otherRuntime = Connection(new WebAssemblyJSRuntime());

        IBcrdtCollaborationService service = first.GetBcrdtCollaborationService(_ => { });

        Assert.AreSame(service, second.GetBcrdtCollaborationService());
        Assert.AreNotSame(service, otherRuntime.GetBcrdtCollaborationService(_ => { }));
    }

    [TestMethod]
    public async Task The_configuration_gives_the_service_its_transport_and_its_store()
    {
        var network = new BcrdtInMemoryNetwork();
        var store = new BcrdtInMemoryDocumentStore();
        using ServiceProvider tabA = Connection(new WebAssemblyJSRuntime());
        using ServiceProvider tabB = Connection(new WebAssemblyJSRuntime());

        await using IBcrdtCollaborationService a = tabA.GetBcrdtCollaborationService(collaboration => collaboration
            .UseTransport(id => network.CreateTransport(id))
            .UseStore(store)
            .Configure(options => options.Sync = _noHeartbeat));
        await using IBcrdtCollaborationService b = tabB.GetBcrdtCollaborationService(collaboration => collaboration
            .UseTransport(id => network.CreateTransport(id))
            .Configure(options => options.Sync = _noHeartbeat));

        BcrdtCollaborativeDocument documentA = await a.OpenAsync("shared");
        BcrdtCollaborativeDocument documentB = await b.OpenAsync("shared");
        await AssertEventuallyAsync(() => documentA.IsSynced && documentB.IsSynced, "both tabs should finish the handshake");

        documentA.Text("body").Insert(0, "lazy loaded");
        await AssertEventuallyAsync(() => documentB.Text("body").ToString() == "lazy loaded", "the edit should reach the other tab");

        await a.CloseAsync("shared");
        await b.CloseAsync("shared");
        CollectionAssert.AreEquivalent(new[] { "shared" }, (await store.ListAsync()).ToArray());
    }

    [TestMethod]
    public void Only_the_first_configuration_creates_the_service()
    {
        using ServiceProvider connection = Connection(new WebAssemblyJSRuntime());

        IBcrdtCollaborationService first = connection.GetBcrdtCollaborationService(_ => { });

        // A page that configures the service in OnInitialized, visited again - and a component that asks for it.
        Assert.AreSame(first, connection.GetBcrdtCollaborationService(_ => Assert.Fail("configured once already")));
        Assert.AreSame(first, connection.GetBcrdtCollaborationService());
    }

    [TestMethod]
    public void Callers_racing_to_configure_the_service_apply_exactly_one_configuration()
    {
        using ServiceProvider connection = Connection(new WebAssemblyJSRuntime());
        int configured = 0;
        using var start = new Barrier(8);

        IBcrdtCollaborationService[] services = [.. Enumerable.Range(0, 8).AsParallel().WithDegreeOfParallelism(8).Select(_ =>
        {
            start.SignalAndWait();
            return connection.GetBcrdtCollaborationService(_ => Interlocked.Increment(ref configured));
        })];

        Assert.AreEqual(1, configured);
        Assert.AreEqual(1, services.Distinct().Count());
    }

    [TestMethod]
    public void Asking_before_anything_configured_the_service_throws_and_creates_nothing()
    {
        using ServiceProvider connection = Connection(new WebAssemblyJSRuntime());

        // A component got there first: rather than a purely local service that silently never syncs, an error that says
        // how to provide one.
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => connection.GetBcrdtCollaborationService());
        StringAssert.Contains(error.Message, "AddBitBcrdtServices");
        StringAssert.Contains(error.Message, "OnInitialized");

        // Nothing was created by that, so a configuration that comes later - on the next page - still takes effect.
        bool configured = false;
        IBcrdtCollaborationService service = connection.GetBcrdtCollaborationService(_ => configured = true);
        Assert.IsTrue(configured);
        Assert.AreSame(service, connection.GetBcrdtCollaborationService());
    }

    [TestMethod]
    public void Outside_WebAssembly_a_configuration_does_not_stand_in_for_the_registration()
    {
        // Blazor Server and Hybrid load Bit.Bcrdt with the app, so a missing registration there is a mistake to report,
        // not a lazy loaded app to serve.
        using ServiceProvider circuit = Connection(new RemoteJSRuntime());

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => circuit.GetBcrdtCollaborationService(_ => Assert.Fail("not called outside WebAssembly")));

        StringAssert.Contains(error.Message, "AddBitBcrdtServices");
    }

    [TestMethod]
    public void Without_the_registration_or_a_Blazor_app_the_error_names_the_registration()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => services.GetBcrdtCollaborationService());

        StringAssert.Contains(error.Message, "AddBitBcrdtServices");
    }

    [TestMethod]
    public async Task The_scope_opens_its_document_through_the_configured_service()
    {
        using ServiceProvider connection = Connection(new WebAssemblyJSRuntime());
        await using var renderer = new ScopeRenderer(connection, isInteractive: true);

        // What the layout around the scope does in OnInitialized.
        IBcrdtCollaborationService service = connection.GetBcrdtCollaborationService(_ => { });
        var scope = await renderer.RenderAsync("notes");

        Assert.IsNotNull(scope.Document);
        Assert.AreSame(scope.Document, service.OpenDocuments.Single());

        await renderer.DisposeAsync();

        Assert.IsEmpty(service.OpenDocuments, "the scope releases its document when it goes away");
        Assert.IsEmpty(renderer.Errors);
    }

    [TestMethod]
    public async Task On_WebAssembly_a_scope_rendered_before_the_configuration_fails_loudly()
    {
        using ServiceProvider connection = Connection(new WebAssemblyJSRuntime());
        await using var renderer = new ScopeRenderer(connection, isInteractive: true);

        await renderer.RenderAsync("notes");

        var error = renderer.Errors.OfType<InvalidOperationException>().Single();
        StringAssert.Contains(error.Message, "GetBcrdtCollaborationService");
    }

    [TestMethod]
    public async Task Outside_WebAssembly_a_component_fails_when_it_is_created_without_the_registration()
    {
        // A server that prerenders without the registration fails right there, as it did when the service was injected -
        // not once the page has gone interactive.
        using ServiceProvider server = Connection(new RemoteJSRuntime());
        await using var renderer = new ScopeRenderer(server, isInteractive: false);

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => renderer.RenderAsync("notes"));

        StringAssert.Contains(error.Message, "AddBitBcrdtServices");
    }

    private static ServiceProvider Connection(IJSRuntime jsRuntime)
    {
        ServiceCollection services = new();
        services.AddSingleton(jsRuntime);
        return services.BuildServiceProvider();
    }

    // A renderer for one BcrdtCollaborationScope, built from the render tree types on purpose: there is no other way to
    // render a component outside Blazor without a test library.
#pragma warning disable BL0006
    private sealed class ScopeRenderer(IServiceProvider services, bool isInteractive) : Renderer(services, NullLoggerFactory.Instance), IAsyncDisposable
    {
        private bool _disposed;

        public List<Exception> Errors { get; } = [];

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        protected override RendererInfo RendererInfo { get; } = new("Test", isInteractive);

        public Task<BcrdtCollaborationScope> RenderAsync(string documentId) => Dispatcher.InvokeAsync(async () =>
        {
            var scope = (BcrdtCollaborationScope)InstantiateComponent(typeof(BcrdtCollaborationScope));
            int id = AssignRootComponentId(scope);
            RenderFragment<BcrdtCollaborativeDocument> content = document => builder => builder.AddContent(0, document.Id);
            await RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(BcrdtCollaborationScope.DocumentId)] = documentId,
                [nameof(BcrdtCollaborationScope.ChildContent)] = content,
            }));
            return scope;
        });

        // On the dispatcher, as Blazor disposes a renderer, and once - so a test can dispose it early, to assert on what
        // disposal does, and still have it disposed when an assertion fails before that.
        public new async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await Dispatcher.InvokeAsync(async () => await base.DisposeAsync());
        }

        protected override void HandleException(Exception exception)
        {
            lock (Errors) Errors.Add(exception);
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }
#pragma warning restore BL0006

    /// <summary>The JavaScript runtime of WebAssembly - the only one in-process.</summary>
    private sealed class WebAssemblyJSRuntime : IJSInProcessRuntime
    {
        public TResult Invoke<TResult>(string identifier, params object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
    }

    /// <summary>The JavaScript runtime of Blazor Server or Hybrid, which reaches the browser over a connection.</summary>
    private sealed class RemoteJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
    }
}
