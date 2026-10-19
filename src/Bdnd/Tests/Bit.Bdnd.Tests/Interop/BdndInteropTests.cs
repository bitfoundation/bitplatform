using Bit.Bdnd.Tests.Fakes;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests.Interop;

/// <summary>The wrapper around the <c>bit-bdnd.js</c> module: what it imports, what it calls, and how it fails.</summary>
[TestClass]
public class BdndInteropTests
{
    private const string ModulePath = "./_content/Bit.Bdnd/bit-bdnd.js";

    [TestMethod]
    public void DragsAreTaggedWithTheHandlerIdAfterTheSourceTypePrefix()
    {
        // must match SOURCE_TYPE_PREFIX in bit-bdnd.ts
        Assert.AreEqual("application/x-bit-bdnd-abc", BdndInterop.SourceType("abc"));
    }

    [TestMethod]
    public async Task TheModuleIsImportedOnceFromTheStaticWebAssetPath()
    {
        var module = new FakeJSObjectReference()
            .Returns("getBoundingClientRect", _ => new BdndDomRect { Left = 1, Top = 2, Width = 30, Height = 40 });
        var js = new FakeJSRuntime().NextImport(module);
        var interop = new BdndInterop(js);
        var element = new ElementReference("el");

        var first = await interop.GetBoundingClientRectAsync(element);
        var second = await interop.GetBoundingClientRectAsync(element);

        var import = Assert.ContainsSingle(js.Calls);
        Assert.AreEqual("import", import.Identifier);
        Assert.AreSequenceEqual(new object?[] { ModulePath }, import.Args!);
        Assert.IsNotNull(first);
        Assert.AreEqual(1, first.Left);
        Assert.AreEqual(2, first.Top);
        Assert.AreEqual(30, first.Width);
        Assert.AreEqual(40, first.Height);
        Assert.IsNotNull(second);
        var measure = module.CallsTo("getBoundingClientRect").First();
        Assert.AreEqual(element, measure![0]);
    }

    [TestMethod]
    public async Task MeasuringAnElementThatIsNotRenderedAnswersNull()
    {
        var js = new FakeJSRuntime().NextImport(new FakeJSObjectReference());
        var interop = new BdndInterop(js);

        Assert.IsNull(await interop.GetBoundingClientRectAsync(default));
    }

    [TestMethod]
    public async Task AFailedImportIsRetriedByTheNextCall()
    {
        var module = new FakeJSObjectReference()
            .Returns("getBoundingClientRect", _ => new BdndDomRect { Width = 5 });
        var js = new FakeJSRuntime()
            .NextImport(new JSException("Failed to fetch dynamically imported module"))
            .NextImport(module);
        var interop = new BdndInterop(js);

        await Assert.ThrowsExactlyAsync<JSException>(async () => await interop.GetBoundingClientRectAsync(default));
        var rect = await interop.GetBoundingClientRectAsync(default);

        Assert.AreEqual(2, js.ImportCount);
        Assert.AreEqual(5, rect!.Width);
    }

    [TestMethod]
    public async Task RegisterDragSourcePassesTheElementTheCallbackObjectAndTheOptions()
    {
        var registration = new FakeJSObjectReference();
        var module = new FakeJSObjectReference().Returns("registerDragSource", _ => registration);
        var interop = new BdndInterop(new FakeJSRuntime().NextImport(module));
        var element = new ElementReference("source");
        var callbacks = new object();
        var options = new BdndDragSourceOptions("id", true, false, true, "copy");

        var result = await interop.RegisterDragSourceAsync(element, callbacks, options);

        Assert.AreSame(registration, result);
        var args = Assert.ContainsSingle(module.CallsTo("registerDragSource"));
        Assert.AreEqual(element, args![0]);
        Assert.AreSame(callbacks, args[1]);
        Assert.AreSame(options, args[2]);
    }

    [TestMethod]
    public async Task RegisterDropTargetPassesTheElementTheCallbackObjectAndTheOptions()
    {
        var registration = new FakeJSObjectReference();
        var module = new FakeJSObjectReference().Returns("registerDropTarget", _ => registration);
        var interop = new BdndInterop(new FakeJSRuntime().NextImport(module));
        var element = new ElementReference("target");
        var callbacks = new object();
        var options = new BdndDropTargetOptions(true, ["Files"]);

        var result = await interop.RegisterDropTargetAsync(element, callbacks, options);

        Assert.AreSame(registration, result);
        var args = Assert.ContainsSingle(module.CallsTo("registerDropTarget"));
        Assert.AreEqual(element, args![0]);
        Assert.AreSame(callbacks, args[1]);
        Assert.AreSame(options, args[2]);
    }

    [TestMethod]
    public async Task SourcesAndTargetsShareOneModule()
    {
        var module = new FakeJSObjectReference();
        var js = new FakeJSRuntime().NextImport(module);
        var interop = new BdndInterop(js);

        await interop.RegisterDragSourceAsync(default, new object(), new BdndDragSourceOptions("a", true, false, false, "move"));
        await interop.RegisterDropTargetAsync(default, new object(), new BdndDropTargetOptions(false, []));
        await interop.GetBoundingClientRectAsync(default);

        Assert.AreEqual(1, js.ImportCount);
        Assert.HasCount(3, module.Calls);
    }

    [TestMethod]
    public async Task DisposingReleasesTheModule()
    {
        var module = new FakeJSObjectReference();
        var interop = new BdndInterop(new FakeJSRuntime().NextImport(module));
        await interop.GetBoundingClientRectAsync(default);

        await interop.DisposeAsync();
        await interop.DisposeAsync();

        Assert.AreEqual(1, module.DisposeCount);
    }

    [TestMethod]
    public async Task DisposingBeforeAnyImportDoesNotImport()
    {
        var js = new FakeJSRuntime();
        var interop = new BdndInterop(js);

        await interop.DisposeAsync();

        Assert.IsEmpty(js.Calls);
    }

    [TestMethod]
    public async Task DisposingAfterTheCircuitIsGoneDoesNotThrow()
    {
        var module = new FakeJSObjectReference { DisposeException = new JSDisconnectedException("gone") };
        var interop = new BdndInterop(new FakeJSRuntime().NextImport(module));
        await interop.GetBoundingClientRectAsync(default);

        await interop.DisposeAsync();

        Assert.AreEqual(1, module.DisposeCount);
    }

    [TestMethod]
    public async Task DisposingAfterAFailedImportDoesNotThrow()
    {
        var interop = new BdndInterop(new FakeJSRuntime().NextImport(new JSException("blocked")));
        await Assert.ThrowsExactlyAsync<JSException>(async () => await interop.GetBoundingClientRectAsync(default));

        await interop.DisposeAsync();
    }
}
