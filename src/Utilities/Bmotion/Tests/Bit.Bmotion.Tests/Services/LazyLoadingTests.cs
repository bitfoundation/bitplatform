using System.Runtime.CompilerServices;
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
/// AddBitBmotionServices, so it creates the services with ConfigureBmotionServices and GetBmotionService hands them out;
/// an app that did register keeps exactly what it registered, and a missing registration fails everywhere else.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests
{
    private const string ModulePath = "./_content/Bit.Bmotion/bit-bmotion.js";

    private Func<IServiceProvider, object?> _webAssemblyRuntimeOf = null!;

    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "box");
        b.CloseElement();
    };

    /// <summary>
    /// The tests run on .NET, not in a browser, so they stand in for WebAssembly: a container whose JS runtime is
    /// in-process, as WebAssembly's is, belongs to the runtime that JS runtime stands for.
    /// </summary>
    [TestInitialize]
    public void StandInForWebAssembly()
    {
        _webAssemblyRuntimeOf = BitBmotion.WebAssemblyRuntimeOf;
        BitBmotion.WebAssemblyRuntimeOf = services => services.GetService<IJSRuntime>() as IJSInProcessRuntime;
    }

    [TestCleanup]
    public void LeaveWebAssembly() => BitBmotion.WebAssemblyRuntimeOf = _webAssemblyRuntimeOf;

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
    public async Task ARootThatValidatesScopesIsAskedWhetherBmotionIsRegisteredWithoutResolvingIt()
    {
        // The default of WebAssemblyHostBuilder and of ASP.NET Core in Development: a scoped service resolved from the
        // root throws. Bmotion's engine is scoped, so telling whether it is registered must not resolve it.
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime())
            .AddBitBmotionServices(o => o.Prerender = BmPrerenderMode.Final)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        app.ConfigureBmotionServices(o => o.Prerender = BmPrerenderMode.Final);

        Assert.AreEqual(BmPrerenderMode.Final, app.GetBmotionService<BitBmotionOptions>().Prerender);
    }

    [TestMethod]
    public async Task ConfiguringCreatesTheServicesOncePerWebAssemblyRuntime()
    {
        var jsRuntime = new WebAssemblyJSRuntime();
        await using var first = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        await using var second = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        await using var otherRuntime = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime()).BuildServiceProvider();

        first.ConfigureBmotionServices(_ => { });
        otherRuntime.ConfigureBmotionServices(_ => { });
        var engine = first.GetBmotionService<BmotionAnimationEngine>();

        Assert.AreSame(engine, first.GetBmotionService<BmotionAnimationEngine>());
        // the same runtime, so the same services, configured or not
        Assert.AreSame(engine, second.GetBmotionService<BmotionAnimationEngine>());
        Assert.AreSame(first.GetBmotionService<BmotionAnimateService>(), second.GetBmotionService<BmotionAnimateService>());
        Assert.AreNotSame(engine, otherRuntime.GetBmotionService<BmotionAnimationEngine>());
        // a transient service is a new one on every call, as it is on every injection
        Assert.AreNotSame(first.GetBmotionService<BmotionScrollTracker>(), first.GetBmotionService<BmotionScrollTracker>());
        Assert.IsTrue(first.GetBmotionService<BmotionCapabilities>().SupportsFrameLoop);
    }

    [TestMethod]
    public async Task AScrollTrackerOfTheServicesBmotionCreatedIsNotHeldOnTo()
    {
        // Bmotion's own container lives as long as the tab and is never disposed: a tracker it held on to would stay
        // in memory after its component disposed it, one more on every visit to the page.
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime()).BuildServiceProvider();
        app.ConfigureBmotionServices(_ => { });

        var tracker = TakeTracker(app);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.IsFalse(tracker.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference TakeTracker(IServiceProvider services) => new(services.GetBmotionService<BmotionScrollTracker>());

    [TestMethod]
    public async Task OnWebAssemblyTheServicesAreNotThereUntilConfigured()
    {
        // What keeps the client project of a Blazor Web App that forgot the registration failing as it renders, instead
        // of running on options it never set.
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime()).BuildServiceProvider();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => app.GetBmotionService<BmotionAnimateService>());

        Assert.StartsWith("Bit.Bmotion's services are not registered", error.Message, StringComparison.Ordinal);
        Assert.Contains("ConfigureBmotionServices", error.Message, StringComparison.Ordinal);
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
    public void OffTheBrowserAnInProcessRuntimeIsNotWebAssembly()
    {
        // bUnit's JS runtime is in-process, as WebAssembly's is: the tests of a Blazor Server app that forgot the
        // registration must fail as the app does, not pass on services created for a WebAssembly runtime.
        BitBmotion.WebAssemblyRuntimeOf = _webAssemblyRuntimeOf;
        using var ctx = CreateUnregisteredContext();

        Assert.ThrowsExactly<InvalidOperationException>(() => ctx.Services.ConfigureBmotionServices(_ => { }));
        Assert.ThrowsExactly<InvalidOperationException>(() => RenderFadeUp(ctx));
    }

    [TestMethod]
    public void OutsideWebAssemblyAnElementWithoutTheRegistrationNamesIt()
    {
        // README.md, the MCP setup guide and the MCP prompts quote the start of this message
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
    public async Task TheOptionsAreSetOnceAndEveryCallAfterMustAgree()
    {
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime()).BuildServiceProvider();
        await using var registered = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime())
            .AddBitBmotionServices(o => o.Prerender = BmPrerenderMode.Animate).BuildServiceProvider();

        app.ConfigureBmotionServices(o => o.ReducedMotion = BmReducedMotionMode.User);
        // the same options again - every layout that holds animated elements calls it
        app.ConfigureBmotionServices(o => o.ReducedMotion = BmReducedMotionMode.User);
        registered.ConfigureBmotionServices(o => o.Prerender = BmPrerenderMode.Animate);

        // a settings page that changes them, or a browser that disagrees with its prerendering server, fails the same
        // way whether the services were created here or registered
        var changed = Assert.ThrowsExactly<InvalidOperationException>(() => app.ConfigureBmotionServices(o => o.ReducedMotion = BmReducedMotionMode.Always));
        var disagreed = Assert.ThrowsExactly<InvalidOperationException>(() => registered.ConfigureBmotionServices(o => o.Prerender = BmPrerenderMode.Final));

        Assert.Contains("sets ReducedMotion differently", changed.Message, StringComparison.Ordinal);
        Assert.Contains("sets Prerender differently", disagreed.Message, StringComparison.Ordinal);
        Assert.AreEqual(BmReducedMotionMode.User, app.GetBmotionService<BitBmotionOptions>().ReducedMotion);
        Assert.AreEqual(BmPrerenderMode.Animate, registered.GetBmotionService<BitBmotionOptions>().Prerender);
    }

    [TestMethod]
    public void EveryOptionIsComparedWhenTheyMustAgree()
    {
        // An option left out of the comparison could be set one way on the server and another in the browser unnoticed.
        foreach (var property in typeof(BitBmotionOptions).GetProperties())
        {
            var other = new BitBmotionOptions();
            var value = property.GetValue(other)!;
            property.SetValue(other, value is bool flag
                ? !flag
                : Enum.GetValues(property.PropertyType).Cast<object>().First(candidate => candidate.Equals(value) is false));

            CollectionAssert.AreEqual(new[] { property.Name }, new BitBmotionOptions().DifferencesFrom(other).ToArray(), property.Name);
        }
    }

    [TestMethod]
    public async Task TheServicesBmotionCreatesLogToTheApplicationsLog()
    {
        var log = new ListLogger<object>();
        await using var app = new ServiceCollection()
            .AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime())
            .AddLogging(logging => logging.AddProvider(new ListLoggerProvider(log)))
            .BuildServiceProvider();
        app.ConfigureBmotionServices(_ => { });

        // what the engine and the scroll tracker log through
        app.GetBmotionService<ILogger<BmotionAnimationEngine>>().LogWarning("from the engine");

        Assert.AreEqual("from the engine", log.Warnings.Single());
    }

    [TestMethod]
    public void OnWebAssemblyEveryComponentWithoutTheRegistrationOrTheConfigurationFailsAsItIsCreated()
    {
        // What tells the client project of a Blazor Web App that forgot the registration that it did - from whichever
        // component comes first.
        using var ctx = CreateUnregisteredContext();

        var errors = new[]
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => RenderFadeUp(ctx)),
            Assert.ThrowsExactly<InvalidOperationException>(() => ctx.Render<BmotionReorderGroup<string>>(ps => ps.Add(p => p.Items, ["one"]))),
            Assert.ThrowsExactly<InvalidOperationException>(() => ctx.Render<BmotionInspector>()),
        };

        foreach (var error in errors)
        {
            Assert.StartsWith("Bit.Bmotion's services are not registered", error.Message, StringComparison.Ordinal);
            Assert.Contains("AddBitBmotionServices", error.Message, StringComparison.Ordinal);
            Assert.Contains("ConfigureBmotionServices", error.Message, StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void AnElementAnimatesWithTheServicesBmotionCreated()
    {
        using var ctx = CreateUnregisteredContext();
        ctx.Services.ConfigureBmotionServices(_ => { });

        RenderFadeUp(ctx);

        // the engine the app's own code reaches is the one that took the element on, through the one module
        var engine = ctx.Services.GetBmotionService<BmotionAnimationEngine>();
        Assert.IsTrue(engine.IsRegistered("box"));
        Assert.HasCount(1, ctx.JSInterop.Invocations["import"]);
    }

    [TestMethod]
    public void TheReorderGroupAndTheInspectorWorkWithTheServicesBmotionCreated()
    {
        using var ctx = CreateUnregisteredContext();
        ctx.Services.ConfigureBmotionServices(_ => { });

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
