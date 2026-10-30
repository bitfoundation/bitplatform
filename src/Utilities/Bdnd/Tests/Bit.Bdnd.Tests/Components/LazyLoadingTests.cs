using Bit.Bdnd.Tests.Fakes;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests.Components;

/// <summary>
/// Where the components take their interop from. An app that lazy loads Bit.Bdnd on WebAssembly cannot call
/// AddBitBdndServices, so the components create their own and GetBdndInterop hands out the same one; an app that did
/// register keeps exactly what it registered, and everywhere but WebAssembly the registration stays required.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BdndComponentTestBase
{
    public LazyLoadingTests() : base(registerServices: false) { }

    /// <summary>A component of the app's own that injects the interop, as the sortable card of a registered app does.</summary>
    public sealed class InjectingCard : ComponentBase
    {
        [Inject] public BdndInterop Interop { get; set; } = default!;
    }

    [TestMethod]
    public async Task AnAppThatRegisteredBdndKeepsItsOwnInterop()
    {
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime()).AddBitBdndServices().BuildServiceProvider();
        await using var scope = app.CreateAsyncScope();

        Assert.AreSame(scope.ServiceProvider.GetRequiredService<BdndInterop>(), scope.ServiceProvider.GetBdndInterop());
    }

    [TestMethod]
    public async Task WithoutTheRegistrationBdndCreatesItsInteropOncePerWebAssemblyRuntime()
    {
        var jsRuntime = new WebAssemblyJSRuntime();
        await using var first = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        await using var second = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        await using var otherRuntime = new ServiceCollection().AddSingleton<IJSRuntime>(new WebAssemblyJSRuntime()).BuildServiceProvider();

        var interop = first.GetBdndInterop();

        Assert.AreSame(interop, first.GetBdndInterop());
        Assert.AreSame(interop, second.GetBdndInterop());
        Assert.AreNotSame(interop, otherRuntime.GetBdndInterop());
    }

    [TestMethod]
    public async Task OutsideWebAssemblyTheRegistrationStaysRequired()
    {
        // Blazor Server's and Blazor Hybrid's JS runtime, which reaches the browser over a connection, and a container
        // with no JS runtime at all: Bit.Bdnd is loaded with such an app, so a missing registration is a mistake
        await using var server = new ServiceCollection().AddSingleton<IJSRuntime>(new FakeJSRuntime()).BuildServiceProvider();
        await using var none = new ServiceCollection().BuildServiceProvider();

        foreach (var services in new IServiceProvider[] { server, none })
        {
            var error = Assert.ThrowsExactly<InvalidOperationException>(() => services.GetBdndInterop());

            Assert.Contains("call builder.Services.AddBitBdndServices() in Program.cs", error.Message, StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void OutsideWebAssemblyADraggableWithoutTheRegistrationNamesIt()
    {
        // the README's troubleshooting table quotes this message
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton<IJSRuntime>(new FakeJSRuntime());

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => context.Render<BdndProvider>(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box"))));

        Assert.StartsWith("Bit.Bdnd's services are not registered", error.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void WithoutTheRegistrationAComponentThatInjectsTheInteropNamesIt()
    {
        // the README's troubleshooting table quotes this message: the draggables work without the registration on
        // WebAssembly, but what the app's own components inject still has to be registered
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box"))
            .AddChildContent<InjectingCard>()));

        Assert.Contains("There is no registered service of type 'Bit.Bdnd.BdndInterop'", error.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GetBdndInteropRejectsNull()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BitBdnd.GetBdndInterop(null!));
    }

    [TestMethod]
    public async Task ADragAndDropWorksWithoutTheRegistration()
    {
        var log = new List<string>();
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d
                .Add(x => x.Type, "box")
                .Add(x => x.Item, "crate")
                .Add(x => x.OnEnd, e => log.Add($"end:{e.Item}:{e.DidDrop}:{e.DropResult}")))
            .AddChildContent<BdndDropTarget<string>>(t => t
                .Add(x => x.Accept, "box")
                .Add(x => x.OnDrop, e =>
                {
                    log.Add($"drop:{e.Item}");
                    e.Result = "archived";
                })));
        var source = cut.FindComponent<BdndDraggable<string>>();
        var bin = cut.FindComponent<BdndDropTarget<string>>();

        // both registered with the script
        Assert.ContainsSingle(SourceRegistrations);
        Assert.ContainsSingle(TargetRegistrations);

        await StartDragAsync(source);
        await HoverAsync(bin);
        await DropAsync(bin);
        await EndDragAsync(source);

        Assert.AreSequenceEqual(new[] { "drop:crate", "end:crate:True:archived" }, log);
    }

    [TestMethod]
    public async Task TheAppsOwnCodeSharesTheComponentsInterop()
    {
        Module.Setup<BdndDomRect?>("getBoundingClientRect", _ => true).SetResult(new BdndDomRect { Width = 30, Height = 40 });
        RenderProvider(p => p.AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate")));

        // a sortable card measuring the card it hovers, as the app's own code does
        var rect = await Services.GetBdndInterop().GetBoundingClientRectAsync(new ElementReference("card"));

        Assert.AreEqual(30, rect!.Width);
        // the module the draggable imported, not a second one
        Assert.ContainsSingle(JSInterop.Invocations["import"]);
    }

    /// <summary>The JavaScript runtime of WebAssembly - the only one in-process.</summary>
    private sealed class WebAssemblyJSRuntime : IJSInProcessRuntime
    {
        public TResult Invoke<TResult>(string identifier, params object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
    }
}
