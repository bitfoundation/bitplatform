using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.Bmotion.Tests.Services;

/// <summary>
/// Where the components take their services from. An app that lazy loads Bit.Bmotion on WebAssembly cannot call
/// AddBitBmotionServices, so the components create their own and GetBmotionService hands out the same ones; an app that
/// did register keeps exactly what it registered, and everywhere but WebAssembly the registration stays required.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests
{
    private const string ModulePath = "./_content/Bit.Bmotion/bit-bmotion.js";

    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "box");
        b.CloseElement();
    };

    /// <summary>
    /// A bUnit context with no Bmotion registration, as an app that lazy loads Bit.Bmotion has: its JS runtime is
    /// in-process, as WebAssembly's is, and the module finds every element it is asked to register.
    /// </summary>
    private static BunitContext CreateUnregisteredContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<bool>("registerElement", _ => true).SetResult(true);
        return ctx;
    }

    private static IRenderedComponent<Bmotion> RenderFadeUp(BunitContext ctx)
        => ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(opacity: 0, y: 20))
            .Add(p => p.Animate, Bm.To(opacity: 1, y: 0))
            .Add(p => p.Transition, Bm.Tween(0.5, ease: BmEase.Linear))
            .Add(p => p.ChildContent, Div));

    [TestMethod]
    public async Task AnAppThatRegisteredBmotionKeepsItsOwnServices()
    {
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime()).AddBitBmotionServices().BuildServiceProvider();
        await using var scope = app.CreateAsyncScope();
        var services = scope.ServiceProvider;

        Assert.AreSame(services, BitBmotion.GetBmotionServices(services));
        Assert.AreSame(services.GetRequiredService<BmotionAnimateService>(), services.GetBmotionService<BmotionAnimateService>());
        Assert.AreSame(services.GetRequiredService<BmotionAnimationEngine>(), services.GetBmotionService<BmotionAnimationEngine>());
    }

    [TestMethod]
    public async Task WithoutTheRegistrationBmotionCreatesItsServicesOncePerWebAssemblyRuntime()
    {
        var jsRuntime = new WebAssemblyJSRuntime();
        await using var first = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        await using var second = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        await using var otherRuntime = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime()).BuildServiceProvider();

        var engine = first.GetBmotionService<BmotionAnimationEngine>();

        Assert.AreSame(engine, first.GetBmotionService<BmotionAnimationEngine>());
        Assert.AreSame(engine, second.GetBmotionService<BmotionAnimationEngine>());
        Assert.AreSame(first.GetBmotionService<BmotionAnimateService>(), second.GetBmotionService<BmotionAnimateService>());
        Assert.AreNotSame(engine, otherRuntime.GetBmotionService<BmotionAnimationEngine>());
        // a transient service is a new one on every call, as it is on every injection
        Assert.AreNotSame(first.GetBmotionService<BmotionScrollTracker>(), first.GetBmotionService<BmotionScrollTracker>());
        Assert.IsTrue(first.GetBmotionService<BmotionCapabilities>().SupportsFrameLoop);
    }

    [TestMethod]
    public async Task OutsideWebAssemblyTheRegistrationStaysRequired()
    {
        // Blazor Server's and Blazor Hybrid's JS runtime, which reaches the browser over a connection, and a container
        // with no JS runtime at all: Bit.Bmotion is loaded with such an app, so a missing registration is a mistake.
        await using var server = new ServiceCollection().AddSingleton<IJSRuntime>(new ServerJSRuntime()).BuildServiceProvider();
        await using var none = new ServiceCollection().BuildServiceProvider();

        foreach (var services in new IServiceProvider[] { server, none })
        {
            var error = Assert.ThrowsExactly<InvalidOperationException>(() => services.GetBmotionService<BmotionAnimateService>());
            Assert.Contains("call builder.Services.AddBitBmotionServices() in Program.cs", error.Message, StringComparison.Ordinal);

            Assert.ThrowsExactly<InvalidOperationException>(() => services.ConfigureBmotionServices(o => o.Prerender = BmPrerenderMode.Final));
        }
    }

    [TestMethod]
    public void OutsideWebAssemblyAnElementWithoutTheRegistrationNamesIt()
    {
        // the README's troubleshooting table quotes this message
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IJSRuntime>(new ServerJSRuntime());

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => RenderFadeUp(ctx));

        Assert.StartsWith("Bit.Bmotion's services are not registered", error.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void TheEntryPointsRejectNull()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BitBmotion.GetBmotionService<BmotionAnimateService>(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => BitBmotion.ConfigureBmotionServices(null!, _ => { }));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ServiceCollection().BuildServiceProvider().ConfigureBmotionServices(null!));
    }

    [TestMethod]
    public async Task ConfiguringSetsTheOptionsOfTheServicesBmotionCreatesAndLeavesRegisteredOnesAlone()
    {
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime()).BuildServiceProvider();
        await using var registered = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime())
            .AddBitBmotionServices(o => o.Prerender = BmPrerenderMode.Animate).BuildServiceProvider();

        app.ConfigureBmotionServices(o => o.ReducedMotion = BmReducedMotionMode.User);
        registered.ConfigureBmotionServices(o => o.Prerender = BmPrerenderMode.Final);

        Assert.AreEqual(BmReducedMotionMode.User, app.GetBmotionService<BitBmotionOptions>().ReducedMotion);
        // the options AddBitBmotionServices was given win, wherever it was called
        Assert.AreEqual(BmPrerenderMode.Animate, registered.GetBmotionService<BitBmotionOptions>().Prerender);
    }

    [TestMethod]
    public async Task WithoutTheRegistrationTheServicesLogToTheApplicationsLog()
    {
        var log = new ListLogger<object>();
        await using var app = new ServiceCollection()
            .AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime())
            .AddLogging(logging => logging.AddProvider(new ListLoggerProvider(log)))
            .BuildServiceProvider();

        // what the engine and the scroll tracker log through
        app.GetBmotionService<ILogger<BmotionAnimationEngine>>().LogWarning("from the engine");

        Assert.AreEqual("from the engine", log.Warnings.Single());
    }

    [TestMethod]
    public void AnElementThatCreatesTheServicesWarnsOnceThatTheyAreNotRegistered()
    {
        // What tells the client project of a Blazor Web App that forgot the registration why its options are ignored.
        using var ctx = CreateUnregisteredContext();
        var log = new ListLogger<object>();
        ctx.Services.AddLogging(logging => logging.AddProvider(new ListLoggerProvider(log)));

        RenderFadeUp(ctx);
        ctx.Render<Bmotion>(ps => ps.Add(p => p.Id, "other").Add(p => p.Animate, Bm.To(opacity: 1)).Add(p => p.ChildContent, Div));

        var warning = log.Warnings.Single(message => message.Contains("not registered", StringComparison.Ordinal));
        Assert.Contains("AddBitBmotionServices", warning, StringComparison.Ordinal);
        Assert.Contains("ConfigureBmotionServices", warning, StringComparison.Ordinal);
    }

    [TestMethod]
    public void AnAppThatConfiguresTheServicesItLazyLoadsIsNotWarned()
    {
        using var ctx = CreateUnregisteredContext();
        var log = new ListLogger<object>();
        ctx.Services.AddLogging(logging => logging.AddProvider(new ListLoggerProvider(log)));

        ctx.Services.ConfigureBmotionServices(o => o.ReducedMotion = BmReducedMotionMode.User);
        RenderFadeUp(ctx);

        Assert.IsFalse(log.Warnings.Any(message => message.Contains("not registered", StringComparison.Ordinal)), string.Join(Environment.NewLine, log.Warnings));
    }

    [TestMethod]
    public void AnElementAnimatesWithoutTheRegistration()
    {
        using var ctx = CreateUnregisteredContext();

        RenderFadeUp(ctx);

        // the engine the app's own code reaches is the one that took the element on, through the one module
        var engine = ctx.Services.GetBmotionService<BmotionAnimationEngine>();
        Assert.IsTrue(engine.IsRegistered("box"));
        Assert.HasCount(1, ctx.JSInterop.Invocations["import"]);
    }

    [TestMethod]
    public void TheReorderGroupAndTheInspectorWorkWithoutTheRegistration()
    {
        using var ctx = CreateUnregisteredContext();

        var list = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, ["one", "two"])
            .Add(p => p.ChildContent, item => b =>
            {
                b.OpenElement(0, "div");
                b.AddContent(1, item);
                b.CloseElement();
            }));
        var inspector = ctx.Render<BmotionInspector>();

        Assert.HasCount(2, list.FindAll("div"));
        Assert.IsNotNull(inspector.Find(".bm-inspector"));
    }

    [TestMethod]
    public void ConfiguredOptionsLetTheRuntimeTakeOverAnElementPrerenderedAtRest()
    {
        // A Blazor Web App whose server prerenders with BmPrerenderMode.Final, and whose browser lazy loads Bit.Bmotion:
        // the browser has to agree on the mode to take the element over at rest instead of replaying its entrance.
        using var ctx = CreateUnregisteredContext();
        var state = ctx.AddBunitPersistentComponentState();
        var signature = BmotionPrerenderState.Signature(BmPrerenderMode.Final, "opacity:1;");
        state.Persist(BmotionPrerenderState.EntryKey(signature, 0), "box");
        state.Persist(BmotionPrerenderState.CountKey(signature), 1);
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        ctx.Services.ConfigureBmotionServices(o => o.Prerender = BmPrerenderMode.Final);
        var cut = RenderFadeUp(ctx);

        Assert.AreEqual("opacity:1;", cut.Find("div").GetAttribute("style"));
    }

    /// <summary>The JavaScript runtime of WebAssembly - the only one in-process.</summary>
    private sealed class WebAssemblyJSRuntime : IJSInProcessRuntime
    {
        public TResult Invoke<TResult>(string identifier, params object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
    }

    /// <summary>The JavaScript runtime of Blazor Server and Blazor Hybrid, which reaches the browser over a connection.</summary>
    private sealed class ServerJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
    }

    private sealed class ListLoggerProvider(ListLogger<object> log) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => log;

        public void Dispose() { }
    }
}
