using Bit.Bcrdt.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// Where the components take their collaboration service from. An app that lazy loads Bit.Bcrdt on WebAssembly cannot
/// call AddBitBcrdtServices, so the components create one per browser connection, configured through
/// GetBcrdtCollaborationService; an app that did register keeps exactly what it registered.
/// </summary>
[TestClass]
public class LazyLoadingTests
{
    private static readonly BcrdtSyncProviderOptions _noHeartbeat = new() { HeartbeatInterval = TimeSpan.Zero };

    [TestMethod]
    public async Task An_app_that_registered_bcrdt_keeps_its_own_service()
    {
        await using ServiceProvider app = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).AddBitBcrdtServices().BuildServiceProvider();
        await using AsyncServiceScope circuit = app.CreateAsyncScope();

        IBcrdtCollaborationService service = circuit.ServiceProvider.GetBcrdtCollaborationService(_ => Assert.Fail("the registration's configuration wins"));

        Assert.AreSame(circuit.ServiceProvider.GetRequiredService<IBcrdtCollaborationService>(), service);
    }

    [TestMethod]
    public void Without_the_registration_bcrdt_creates_one_service_per_browser_connection()
    {
        var jsRuntime = new NoJSRuntime();
        using ServiceProvider first = Connection(jsRuntime);
        using ServiceProvider second = Connection(jsRuntime);
        using ServiceProvider otherConnection = Connection(new NoJSRuntime());

        IBcrdtCollaborationService service = first.GetBcrdtCollaborationService();

        Assert.AreSame(service, second.GetBcrdtCollaborationService());
        Assert.AreNotSame(service, otherConnection.GetBcrdtCollaborationService());
    }

    [TestMethod]
    public async Task The_configuration_gives_the_service_its_transport_and_its_store()
    {
        var network = new BcrdtInMemoryNetwork();
        var store = new BcrdtInMemoryDocumentStore();
        using ServiceProvider tabA = Connection(new NoJSRuntime());
        using ServiceProvider tabB = Connection(new NoJSRuntime());

        IBcrdtCollaborationService a = tabA.GetBcrdtCollaborationService(collaboration => collaboration
            .UseTransport(id => network.CreateTransport(id))
            .UseStore(store)
            .Configure(options => options.Sync = _noHeartbeat));
        IBcrdtCollaborationService b = tabB.GetBcrdtCollaborationService(collaboration => collaboration
            .UseTransport(id => network.CreateTransport(id))
            .Configure(options => options.Sync = _noHeartbeat));

        BcrdtCollaborativeDocument documentA = await a.OpenAsync("shared");
        BcrdtCollaborativeDocument documentB = await b.OpenAsync("shared");
        await Task.Delay(50);

        documentA.Text("body").Insert(0, "lazy loaded");
        await Task.Delay(50);

        Assert.AreEqual("lazy loaded", documentB.Text("body").ToString());

        await a.CloseAsync("shared");
        await b.CloseAsync("shared");
        CollectionAssert.AreEquivalent(new[] { "shared" }, (await store.ListAsync()).ToArray());
    }

    [TestMethod]
    public void Only_the_call_that_creates_the_service_configures_it()
    {
        using ServiceProvider connection = Connection(new NoJSRuntime());

        IBcrdtCollaborationService first = connection.GetBcrdtCollaborationService(_ => { });

        // A page that configures the service in OnInitialized, visited again - and a component that asks for it.
        Assert.AreSame(first, connection.GetBcrdtCollaborationService(_ => Assert.Fail("configured once already")));
        Assert.AreSame(first, connection.GetBcrdtCollaborationService());
    }

    [TestMethod]
    public void Configuring_a_service_already_created_without_a_configuration_throws()
    {
        using ServiceProvider connection = Connection(new NoJSRuntime());

        // A component got there first, with no transport and no store: a configuration now would be silently lost.
        connection.GetBcrdtCollaborationService();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => connection.GetBcrdtCollaborationService(_ => { }));
        StringAssert.Contains(error.Message, "OnInitialized");
    }

    [TestMethod]
    public void Without_the_registration_bcrdt_warns_once_per_app_and_not_when_configured()
    {
        var loggers = new SinkLoggerFactory();
        using ServiceProvider firstCircuit = Connection(new NoJSRuntime(), loggers);
        using ServiceProvider secondCircuit = Connection(new NoJSRuntime(), loggers);
        using ServiceProvider configured = Connection(new NoJSRuntime(), new SinkLoggerFactory());

        firstCircuit.GetBcrdtCollaborationService();
        firstCircuit.GetBcrdtCollaborationService();
        secondCircuit.GetBcrdtCollaborationService();
        configured.GetBcrdtCollaborationService(_ => { });

        string warning = loggers.Warnings.Single();
        StringAssert.Contains(warning, "AddBitBcrdtServices");
        StringAssert.Contains(warning, "GetBcrdtCollaborationService");
        Assert.IsEmpty(((SinkLoggerFactory)configured.GetRequiredService<ILoggerFactory>()).Warnings);
    }

    [TestMethod]
    public void Without_the_registration_or_a_Blazor_app_the_error_names_the_registration()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => services.GetBcrdtCollaborationService());

        StringAssert.Contains(error.Message, "AddBitBcrdtServices");
    }

    [TestMethod]
    public async Task The_scope_opens_its_document_without_the_registration()
    {
        using ServiceProvider connection = Connection(new NoJSRuntime());
        var renderer = new ScopeRenderer(connection, isInteractive: true);
        var scope = await renderer.RenderAsync("notes");

        // What app code reaches through GetBcrdtCollaborationService is the service the scope opened its document with.
        IBcrdtCollaborationService service = connection.GetBcrdtCollaborationService();
        Assert.IsNotNull(scope.Document);
        Assert.AreSame(scope.Document, service.OpenDocuments.Single());

        await renderer.Dispatcher.InvokeAsync(async () => await ((IAsyncDisposable)renderer).DisposeAsync());

        Assert.IsEmpty(service.OpenDocuments, "the scope releases its document when it goes away");
        Assert.IsEmpty(renderer.Errors);
    }

    [TestMethod]
    public async Task A_prerendered_scope_needs_no_collaboration_service()
    {
        // The server of a Blazor Web App that only prerenders: no registration, and no IJSRuntime to create one for.
        using ServiceProvider server = new ServiceCollection().BuildServiceProvider();
        var renderer = new ScopeRenderer(server, isInteractive: false);

        var scope = await renderer.RenderAsync("notes");
        await renderer.Dispatcher.InvokeAsync(async () => await ((IAsyncDisposable)renderer).DisposeAsync());

        Assert.IsNull(scope.Document);
        Assert.IsEmpty(renderer.Errors);
    }

    private static ServiceProvider Connection(IJSRuntime jsRuntime, ILoggerFactory? loggers = null)
    {
        ServiceCollection services = new();
        services.AddSingleton(jsRuntime);
        if (loggers is not null) services.AddSingleton(loggers);
        return services.BuildServiceProvider();
    }

    // A renderer for one BcrdtCollaborationScope, built from the render tree types on purpose: there is no other way to
    // render a component outside Blazor without a test library.
#pragma warning disable BL0006
    private sealed class ScopeRenderer(IServiceProvider services, bool isInteractive) : Renderer(services, NullLoggerFactory.Instance)
    {
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

        protected override void HandleException(Exception exception)
        {
            lock (Errors) Errors.Add(exception);
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }
#pragma warning restore BL0006

    private sealed class SinkLoggerFactory : ILoggerFactory
    {
        public List<string> Warnings { get; } = [];

        public ILogger CreateLogger(string categoryName) => new SinkLogger(this);

        public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();

        public void Dispose() { }

        private sealed class SinkLogger(SinkLoggerFactory sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (logLevel == LogLevel.Warning) lock (sink.Warnings) sink.Warnings.Add(formatter(state, exception));
            }
        }
    }

    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
    }
}
