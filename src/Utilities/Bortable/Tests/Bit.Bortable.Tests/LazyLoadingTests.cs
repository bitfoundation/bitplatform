using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bortable.Tests;

/// <summary>
/// Where the lists take their drag coordinator from. An app that lazy loads Bit.Bortable on WebAssembly cannot call
/// AddBitBortableServices, so it sets the Bit.Bortable.Unregistered switch and the lists create their own, once per
/// scope, and share it; an app that did register keeps exactly what it registered, and an app that did neither - one
/// that forgot the registration - fails where it happens, on every host.
/// </summary>
/// <remarks>The switch is process-wide, so these tests never run next to others.</remarks>
[TestClass, DoNotParallelize]
public class LazyLoadingTests : BortableTestContext
{
    public LazyLoadingTests() : base(registerServices: false) { }

    [TestInitialize]
    public void SetTheSwitch() => AppContext.SetSwitch(BortableService.UnregisteredSwitch, true);

    [TestCleanup]
    public void ClearTheSwitch() => AppContext.SetSwitch(BortableService.UnregisteredSwitch, false);

    [TestMethod]
    public async Task AnAppThatRegisteredBortableKeepsItsOwnCoordinator()
    {
        // the switch as well: the registration is what the lists use wherever there is one
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).AddBitBortableServices().BuildServiceProvider();
        await using var scope = app.CreateAsyncScope();

        Assert.AreSame(scope.ServiceProvider.GetRequiredService<BortableService>(), BortableService.Of(scope.ServiceProvider));
    }

    [TestMethod]
    public async Task WithTheSwitchBortableCreatesItsCoordinatorOncePerScope()
    {
        // one per circuit, or per renderer with a scope of its own, as a scoped registration gives: never shared
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).BuildServiceProvider();
        await using var first = app.CreateAsyncScope();
        await using var second = app.CreateAsyncScope();

        var coordinator = BortableService.Of(first.ServiceProvider);

        Assert.AreSame(coordinator, BortableService.Of(first.ServiceProvider));
        Assert.AreSame(coordinator, BortableService.Of(first.ServiceProvider.GetRequiredService<IServiceProvider>()));
        Assert.AreNotSame(coordinator, BortableService.Of(second.ServiceProvider));
    }

    [TestMethod]
    public async Task WithoutTheSwitchTheRegistrationStaysRequired()
    {
        // on WebAssembly too: an app that forgot the registration hears of it, rather than quietly losing its defaults
        AppContext.SetSwitch(BortableService.UnregisteredSwitch, false);
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).BuildServiceProvider();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => BortableService.Of(app));

        Assert.Contains("call builder.Services.AddBitBortableServices() in Program.cs", error.Message, StringComparison.Ordinal);
        Assert.Contains("sets the Bit.Bortable.Unregistered runtime switch", error.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void WithoutTheSwitchAListWithoutTheRegistrationNamesIt()
    {
        // the README's troubleshooting quotes this message
        AppContext.SetSwitch(BortableService.UnregisteredSwitch, false);
        using var bare = BareContext();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => bare.Render<Bortable<string>>(p => p.Add(c => c.Items, new List<string> { "A" })));

        Assert.StartsWith("Bit.Bortable's services are not registered", error.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task AListThatFailedToStartDoesNotFailAgainWhenDisposed()
    {
        AppContext.SetSwitch(BortableService.UnregisteredSwitch, false);
        using var bare = BareContext();
        Assert.ThrowsExactly<InvalidOperationException>(() => bare.Render<Bortable<string>>(p => p.Add(c => c.Items, new List<string> { "A" })));

        // disposing it must not resolve the coordinator again, and throw a second time
        await bare.DisposeComponentsAsync();

        Assert.IsNull(bare.Renderer.UnhandledException.IsCompleted ? await bare.Renderer.UnhandledException : null);
    }

    [TestMethod]
    public void WithTheSwitchTheListsTakeNoAppWideDefaults()
    {
        var list = RenderList(["A"]);

        // the parameters' own defaults: nothing set them app-wide
        Assert.AreEqual(150, list.Instance.Animation);
        Assert.IsNull(list.Instance.Messages);
    }

    [TestMethod]
    public async Task ADragBetweenListsWorksWithTheSwitch()
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

    private static BunitContext BareContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
    }
}
