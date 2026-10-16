using System.Threading.Tasks;
using Bit.Blex.Blazor;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// The browser-storage bridge: which module function each operation calls, and the watcher handle
/// that keeps two storage instances on one page from detaching each other's <c>storage</c> listener.
/// </summary>
[TestClass]
public class BrowserStorageTests
{
    [TestMethod]
    public async Task Operations_TargetTheRequestedArea()
    {
        var js = new FakeJsRuntime();
        js.Module.Returns["get"] = "stored";
        await using var storage = new BlexBrowserStorage(js, BlexBrowserStorageKind.Session);

        var read = await storage.GetAsync("blex:settings");
        await storage.SetAsync("blex:settings", "{}");
        await storage.RemoveAsync("blex:settings");

        Assert.AreEqual("stored", read);
        Assert.AreEqual("./_content/Bit.Blex.Blazor/bit-blex-storage.js", Assert.ContainsSingle(js.Imports));
        Assert.AreSequenceEqual(["session", "blex:settings"], js.Module.ArgsOf("get"));
        Assert.AreSequenceEqual(["session", "blex:settings", "{}"], js.Module.ArgsOf("set"));
        Assert.AreSequenceEqual(["session", "blex:settings"], js.Module.ArgsOf("remove"));
    }

    [TestMethod]
    public async Task TheModuleIsImportedOnce()
    {
        var js = new FakeJsRuntime();
        await using var storage = new BlexBrowserStorage(js);

        await storage.SetAsync("a", "1");
        await storage.SetAsync("b", "2");
        await storage.GetAsync("a");

        Assert.ContainsSingle(js.Imports);
    }

    [TestMethod]
    public async Task LocalStorage_WatchesAndUnwatchesByHandle()
    {
        var js = new FakeJsRuntime();
        js.Module.Returns["watch"] = 7;
        var storage = new BlexBrowserStorage(js, BlexBrowserStorageKind.Local);

        await storage.StartWatchingAsync();
        Assert.AreEqual("local", js.Module.ArgsOf("watch")![0]);

        await storage.DisposeAsync();

        // Unwatching by handle is what stops a second storage instance on the page from detaching
        // this one's listener; the module is shared, the handle is not.
        Assert.AreSequenceEqual([7], js.Module.ArgsOf("unwatch"));
        Assert.IsTrue(js.Module.Disposed);
    }

    [TestMethod]
    public async Task DisposingAfterThePageWentAway_DoesNotThrow()
    {
        // A reloaded BlazorWebView page disposes the old page's scope after the new page attached, and the
        // new page answers the unwatch with a JSException for a module it never had. Escaping DisposeAsync,
        // that exception took the whole hybrid app down.
        var js = new FakeJsRuntime();
        js.Module.Returns["watch"] = 3;
        var storage = new BlexBrowserStorage(js, BlexBrowserStorageKind.Local);
        await storage.StartWatchingAsync();

        js.Module.Failure = identifier => identifier == "unwatch"
            ? new JSException("JS object instance with ID 1 does not exist (has it been disposed?).")
            : null;

        await storage.DisposeAsync();
    }

    [TestMethod]
    public async Task WatchingIsIdempotent()
    {
        var js = new FakeJsRuntime();
        js.Module.Returns["watch"] = 1;
        await using var storage = new BlexBrowserStorage(js);

        await storage.StartWatchingAsync();
        await storage.StartWatchingAsync();

        Assert.AreEqual(1, js.Module.CountOf("watch"));
    }

    [TestMethod]
    public async Task SessionStorage_NeverWatches()
    {
        // sessionStorage is per-tab, so there is no other instance to hear from.
        var js = new FakeJsRuntime();
        var storage = new BlexBrowserStorage(js, BlexBrowserStorageKind.Session);

        await storage.StartWatchingAsync();
        await storage.DisposeAsync();

        Assert.AreEqual(0, js.Module.CountOf("watch"));
        Assert.AreEqual(0, js.Module.CountOf("unwatch"));
    }

    [TestMethod]
    public async Task AZeroHandle_MeansNothingToUnwatch()
    {
        var js = new FakeJsRuntime();   // "watch" returns default(int) == 0
        var storage = new BlexBrowserStorage(js);

        await storage.StartWatchingAsync();
        await storage.DisposeAsync();

        Assert.AreEqual(1, js.Module.CountOf("watch"));
        Assert.AreEqual(0, js.Module.CountOf("unwatch"));
    }

    [TestMethod]
    public async Task ExternalChange_IsRaisedFromTheJsCallback()
    {
        var js = new FakeJsRuntime();
        js.Module.Returns["watch"] = 3;
        await using var storage = new BlexBrowserStorage(js);
        await storage.StartWatchingAsync();

        string? seen = "unset";
        storage.ExternalChange += key => seen = key;

        await storage.OnExternalChangeAsync("blex:settings");
        Assert.AreEqual("blex:settings", seen);

        // A null key means the whole area changed; it is forwarded as-is.
        await storage.OnExternalChangeAsync(null);
        Assert.IsNull(seen);
    }

    [TestMethod]
    public async Task DisposingWithoutEverTouchingStorage_IsANoOp()
    {
        var js = new FakeJsRuntime();
        var storage = new BlexBrowserStorage(js);

        await storage.DisposeAsync();

        Assert.IsEmpty(js.Imports);
        Assert.IsFalse(js.Module.Disposed);
    }
}
