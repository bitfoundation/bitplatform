using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bit.Blex.Blazor;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// The Redux DevTools bridge: it must connect only when the extension answered, stream actions with
/// their payloads, survive an absent extension, and disconnect exactly its own connection.
/// </summary>
[TestClass]
public class DevToolsConnectorTests
{
    private static (FakeJsRuntime Js, BlexManager Manager, CounterStore Store) Setup(int handle)
    {
        var js = new FakeJsRuntime();
        js.Module.Returns["connect"] = handle;
        var manager = new BlexManager();
        var store = new CounterStore();
        manager.Register(store);
        return (js, manager, store);
    }

    [TestMethod]
    public async Task Connecting_SendsTheInitialSnapshot()
    {
        var (js, manager, _) = Setup(handle: 4);
        await using var connector = new BlexReduxDevToolsConnector(js);

        await connector.ConnectAsync(manager, "My App");

        Assert.AreEqual("./_content/Bit.Blex.Blazor/bit-blex-devtools.js", Assert.ContainsSingle(js.Imports));
        Assert.AreEqual("My App", js.Module.ArgsOf("connect")![1]);

        var init = js.Module.ArgsOf("init");
        Assert.IsNotNull(init);
        Assert.AreEqual(4, init![0]);
        Assert.Contains("\"counter\"", (string)init[1]!);
    }

    [TestMethod]
    public async Task EachActionIsStreamedWithItsPayload()
    {
        var (js, manager, store) = Setup(handle: 1);
        await using var connector = new BlexReduxDevToolsConnector(js);
        await connector.ConnectAsync(manager, "Blex");

        store.Add(5);

        var send = js.Module.ArgsOf("send");
        Assert.IsNotNull(send);
        Assert.AreEqual(1, send![0]);
        Assert.AreEqual("counter/Add", send[1]);
        Assert.Contains("\"Count\":5", (string)send[2]!);
        Assert.Contains("\"amount\":5", (string)send[3]!);
    }

    [TestMethod]
    public async Task AParameterlessAction_SendsNoPayload()
    {
        var (js, manager, store) = Setup(handle: 1);
        await using var connector = new BlexReduxDevToolsConnector(js);
        await connector.ConnectAsync(manager, "Blex");

        store.Increment();

        Assert.IsNull(js.Module.ArgsOf("send")![3]);
    }

    [TestMethod]
    public async Task NoExtension_MeansNoConnectionAndNoStreaming()
    {
        // connect() returns 0 when window.__REDUX_DEVTOOLS_EXTENSION__ is absent.
        var (js, manager, store) = Setup(handle: 0);
        await using var connector = new BlexReduxDevToolsConnector(js);

        await connector.ConnectAsync(manager, "Blex");
        store.Increment();

        Assert.AreEqual(0, js.Module.CountOf("init"));
        Assert.AreEqual(0, js.Module.CountOf("send"));
        Assert.AreEqual(1, store.Count);   // the app is entirely unaffected
    }

    [TestMethod]
    public async Task AFailedImport_IsReportedAndDoesNotBreakStartup()
    {
        var js = new FakeJsRuntime { ImportFailure = () => new JSException("module not found") };
        var manager = new BlexManager();
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;
        var store = new CounterStore();
        manager.Register(store);

        await using var connector = new BlexReduxDevToolsConnector(js);
        await connector.ConnectAsync(manager, "Blex");

        store.Increment();

        Assert.AreEqual(1, store.Count);
        Assert.Contains(e => e.Source == "devtools", errors);
    }

    [TestMethod]
    public async Task ADisconnectedCircuitDuringImport_IsSwallowed()
    {
        var js = new FakeJsRuntime { ImportFailure = () => new JSDisconnectedException("gone") };
        var manager = new BlexManager();
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        await using var connector = new BlexReduxDevToolsConnector(js);
        await connector.ConnectAsync(manager, "Blex");

        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public async Task DisposingDetachesOnlyThisConnection()
    {
        var (js, manager, _) = Setup(handle: 9);
        var connector = new BlexReduxDevToolsConnector(js);
        await connector.ConnectAsync(manager, "Blex");

        await connector.DisposeAsync();

        // By handle: the JS module is shared by every connector on the page, so a bare
        // ext.disconnect() would tear down whichever connection happened to be last.
        Assert.AreSequenceEqual([9], js.Module.ArgsOf("disconnect"));
        Assert.IsTrue(js.Module.Disposed);
    }

    [TestMethod]
    public async Task DisposingAfterThePageWentAway_DoesNotThrow()
    {
        // A reloaded BlazorWebView page disposes the old page's scope after the new page attached, and the
        // new page answers for a module it never had with a JSException. Escaping DisposeAsync, that
        // exception took the whole hybrid app down.
        var (js, manager, _) = Setup(handle: 4);
        var connector = new BlexReduxDevToolsConnector(js);
        await connector.ConnectAsync(manager, "Blex");

        js.Module.Failure = _ => new JSException("JS object instance with ID 1 does not exist (has it been disposed?).");

        await connector.DisposeAsync();
    }

    [TestMethod]
    public async Task MessagesFromTheExtension_DriveTimeTravel()
    {
        var (js, manager, store) = Setup(handle: 2);
        await using var connector = new BlexReduxDevToolsConnector(js);
        await connector.ConnectAsync(manager, "Blex");

        store.Increment();
        var snapshot = manager.CaptureGlobalState().ToJsonString();
        store.Increment();
        Assert.AreEqual(2, store.Count);

        await connector.HandleMessage(
            $$"""{"type":"DISPATCH","payload":{"type":"JUMP_TO_STATE"},"state":{{System.Text.Json.Nodes.JsonValue.Create(snapshot)!.ToJsonString()}}}""");

        Assert.AreEqual(1, store.Count);
    }

    [TestMethod]
    public async Task DisposingBeforeTheImportResolves_DoesNotConnect()
    {
        var (js, manager, store) = Setup(handle: 5);
        var connector = new BlexReduxDevToolsConnector(js);

        await connector.DisposeAsync();
        await connector.ConnectAsync(manager, "Blex");

        store.Increment();
        Assert.AreEqual(0, js.Module.CountOf("send"));
    }
}
