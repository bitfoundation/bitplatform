using Bit.Bdnd.Tests.Fakes;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests.Components;

/// <summary>
/// Where the components take their interop from. An app that lazy loads Bit.Bdnd on WebAssembly cannot call
/// AddBitBdndServices, so the components create their own and GetBdndInterop hands out the same one; an app that did
/// register keeps exactly what it registered.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BdndComponentTestBase
{
    public LazyLoadingTests() : base(registerServices: false) { }

    [TestMethod]
    public async Task AnAppThatRegisteredBdndKeepsItsOwnInterop()
    {
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new FakeJSRuntime()).AddBitBdndServices().BuildServiceProvider();
        await using var scope = app.CreateAsyncScope();

        Assert.AreSame(scope.ServiceProvider.GetRequiredService<BdndInterop>(), scope.ServiceProvider.GetBdndInterop());
    }

    [TestMethod]
    public async Task WithoutTheRegistrationBdndCreatesItsInteropOncePerBrowserConnection()
    {
        var jsRuntime = new FakeJSRuntime();
        await using var first = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        await using var second = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        await using var otherConnection = new ServiceCollection().AddSingleton<IJSRuntime>(new FakeJSRuntime()).BuildServiceProvider();

        var interop = first.GetBdndInterop();

        Assert.AreSame(interop, first.GetBdndInterop());
        Assert.AreSame(interop, second.GetBdndInterop());
        Assert.AreNotSame(interop, otherConnection.GetBdndInterop());
    }

    [TestMethod]
    public async Task WithoutTheRegistrationOrABlazorAppTheErrorNamesTheRegistration()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => services.GetBdndInterop());

        Assert.Contains("AddBitBdndServices", error.Message);
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
}
