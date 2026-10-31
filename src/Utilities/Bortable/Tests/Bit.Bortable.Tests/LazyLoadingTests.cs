using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.Bortable.Tests;

/// <summary>
/// Where the lists take their drag coordinator from. An app that lazy loads Bit.Bortable on WebAssembly cannot call
/// AddBitBortableServices, so the lists create their own, once per WebAssembly runtime, and share it; an app that did
/// register keeps exactly what it registered, and everywhere but WebAssembly the registration stays required.
/// </summary>
[TestClass]
public class LazyLoadingTests : BortableTestContext
{
    public LazyLoadingTests() : base(registerServices: false) { }

    [TestMethod]
    public async Task AnAppThatRegisteredBortableKeepsItsOwnCoordinator()
    {
        // on Blazor Server as well: the registration is what the lists use wherever there is one
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new RemoteJSRuntime()).AddBitBortableServices().BuildServiceProvider();
        await using var scope = app.CreateAsyncScope();

        Assert.AreSame(scope.ServiceProvider.GetRequiredService<BortableService>(), BortableService.Of(scope.ServiceProvider));
    }

    [TestMethod]
    public async Task WithoutTheRegistrationBortableCreatesItsCoordinatorOncePerWebAssemblyRuntime()
    {
        var jsRuntime = new WebAssemblyJSRuntime();
        await using var first = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        await using var second = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        await using var otherRuntime = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime()).BuildServiceProvider();

        var coordinator = BortableService.Of(first);

        Assert.AreSame(coordinator, BortableService.Of(first));
        Assert.AreSame(coordinator, BortableService.Of(second));
        Assert.AreNotSame(coordinator, BortableService.Of(otherRuntime));
    }

    [TestMethod]
    public async Task OutsideWebAssemblyTheRegistrationStaysRequired()
    {
        // Blazor Server's and Blazor Hybrid's JS runtime, which reaches the browser over a connection, and a container
        // with no JS runtime at all: Bit.Bortable is loaded with such an app, so a missing registration is a mistake
        await using var server = new ServiceCollection().AddSingleton<IJSRuntime>(new RemoteJSRuntime()).BuildServiceProvider();
        await using var none = new ServiceCollection().BuildServiceProvider();

        foreach (var services in new IServiceProvider[] { server, none })
        {
            var error = Assert.ThrowsExactly<InvalidOperationException>(() => BortableService.Of(services));

            Assert.Contains("call builder.Services.AddBitBortableServices() in Program.cs", error.Message, StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void OutsideWebAssemblyAListWithoutTheRegistrationNamesIt()
    {
        // the README's troubleshooting quotes this message
        using var server = ServerContext();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => server.Render<Bortable<string>>(p => p.Add(c => c.Items, new List<string> { "A" })));

        Assert.StartsWith("Bit.Bortable's services are not registered", error.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task AListThatFailedToStartDoesNotFailAgainWhenDisposed()
    {
        using var server = ServerContext();
        Assert.ThrowsExactly<InvalidOperationException>(() => server.Render<Bortable<string>>(p => p.Add(c => c.Items, new List<string> { "A" })));

        // disposing it must not resolve the coordinator again, and throw a second time
        await server.DisposeComponentsAsync();

        Assert.IsNull(server.Renderer.UnhandledException.IsCompleted ? await server.Renderer.UnhandledException : null);
    }

    [TestMethod]
    public void WithoutTheRegistrationTheListsTakeNoAppWideDefaultsAndSayWhyOnce()
    {
        var log = new WarningLog();
        Services.AddSingleton<ILoggerFactory>(log);

        var first = RenderList(["A"]);
        RenderList(["B"]);

        // the parameters' own defaults: nothing set them app-wide
        Assert.AreEqual(150, first.Instance.Animation);
        Assert.IsNull(first.Instance.Messages);
        // one warning however many lists, under the category the guide says to filter
        var warning = Assert.ContainsSingle(log.Warnings);
        Assert.AreEqual("Bit.Bortable.BitBortable", warning.Category);
        Assert.Contains("AddBitBortableServices", warning.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task ADragBetweenListsWorksWithoutTheRegistration()
    {
        List<string> left = ["A", "B"], right = ["X", "Y"];
        var log = new List<Raised<string>>();
        var l1 = RenderList(left, "L1", p => { p.Add(c => c.Group, "g"); Record(p, "L1", log); });
        var l2 = RenderList(right, "L2", p => { p.Add(c => c.Group, "g"); Record(p, "L2", log); });

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);
        await DropAsync(l2);

        Assert.AreSequenceEqual(["B"], left);
        Assert.AreSequenceEqual(["X", "A", "Y"], right);
        Assert.AreEqual("X,A,Y", Labels(l2));
        Assert.AreEqual("Start:L1 Change:L2 ItemsChanged:L1 ItemsChanged:L2 Remove:L1 Add:L2 Sort:L1 Sort:L2 End:L1", Sequence(log));
        // both lists joined the one coordinator, and imported the module once between them
        Assert.HasCount(2, Coordinator.Zones);
        Assert.ContainsSingle(JSInterop.Invocations["import"]);
    }

    private static BunitContext ServerContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton<IJSRuntime>(new RemoteJSRuntime());
        return context;
    }

    /// <summary>The JavaScript runtime of WebAssembly - the only one in-process.</summary>
    private sealed class WebAssemblyJSRuntime : IJSInProcessRuntime
    {
        public TResult Invoke<TResult>(string identifier, params object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
    }

    /// <summary>The JavaScript runtime of Blazor Server and Blazor Hybrid, which reaches the browser over a connection.</summary>
    private sealed class RemoteJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
    }

    /// <summary>Keeps the warnings of Bit.Bortable's categories, with the category each was written under.</summary>
    private sealed class WarningLog : ILoggerFactory
    {
        public List<(string Category, string Message)> Warnings { get; } = [];

        public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

        public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();

        public void Dispose() { }

        private sealed class Logger(WarningLog log, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (logLevel == LogLevel.Warning && category.StartsWith("Bit.Bortable", StringComparison.Ordinal))
                    log.Warnings.Add((category, formatter(state, exception)));
            }
        }
    }
}
