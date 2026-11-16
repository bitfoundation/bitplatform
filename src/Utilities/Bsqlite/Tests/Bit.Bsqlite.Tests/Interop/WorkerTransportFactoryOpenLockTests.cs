namespace Bit.Bsqlite.Interop.Tests;

[TestClass]
public sealed class WorkerTransportFactoryOpenLockTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    private static ValueTask<Storage.IBsqliteStorageProvider?> Unbound(CancellationToken cancellationToken) => default;

    /// <summary>
    /// The page keeps every held open lock in one map, by id, and a factory is made per DI scope. Ids each factory
    /// counted on its own collided between scopes, so releasing one scope's lock released the other's.
    /// </summary>
    [TestMethod]
    public async Task OpenLockIds_AreUniqueAcrossFactories()
    {
        var js = new ScriptedJsRuntime();
        IBsqliteOpenLock first = new BsqliteWorkerTransportFactory(js);
        IBsqliteOpenLock second = new BsqliteWorkerTransportFactory(js);

        await using var a = await first.AcquireOpenLockAsync("app.db", Unbound, Ct);
        await using var b = await second.AcquireOpenLockAsync("app.db", Unbound, Ct);

        var ids = js.Calls.Where(call => call.Identifier == "module.acquireOpenLock").Select(call => call.Args[1]).ToArray();
        Assert.HasCount(2, ids);
        Assert.AreNotEqual(ids[0], ids[1]);
    }

    /// <summary>The host module is imported once for a factory's locks, and released with the factory.</summary>
    [TestMethod]
    public async Task OpenLocks_ShareOneHostModule_ReleasedWithTheFactory()
    {
        var js = new ScriptedJsRuntime();
        var factory = new BsqliteWorkerTransportFactory(js);
        IBsqliteOpenLock locks = factory;

        await (await locks.AcquireOpenLockAsync("app.db", Unbound, Ct))!.DisposeAsync();
        await (await locks.AcquireOpenLockAsync("app.db", Unbound, Ct))!.DisposeAsync();

        Assert.AreEqual(1, js.Calls.Count(call => call.Identifier == "import"));
        Assert.AreEqual(2, js.Calls.Count(call => call.Identifier == "module.releaseOpenLock"));
        Assert.IsFalse(js.Module.Disposed);

        await factory.DisposeAsync();

        Assert.IsTrue(js.Module.Disposed);
    }

    /// <summary>
    /// A lock is released through the module it was taken through. Disposing the factory while one was held used to
    /// dispose that module first, so the release failed quietly and the lock stayed held - keeping every other tab
    /// from opening the database. The module now goes with the last lock.
    /// </summary>
    [TestMethod]
    public async Task DisposingTheFactory_WhileALockIsHeld_KeepsTheModuleUntilTheLockIsReleased()
    {
        var js = new ScriptedJsRuntime();
        var factory = new BsqliteWorkerTransportFactory(js);
        IBsqliteOpenLock locks = factory;

        var held = await locks.AcquireOpenLockAsync("app.db", Unbound, Ct);
        await factory.DisposeAsync();

        Assert.IsFalse(js.Module.Disposed);

        await held!.DisposeAsync();

        Assert.AreEqual(1, js.Calls.Count(call => call.Identifier == "module.releaseOpenLock"));
        Assert.IsTrue(js.Module.Disposed);
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => locks.AcquireOpenLockAsync("app.db", Unbound, Ct));
    }
}
