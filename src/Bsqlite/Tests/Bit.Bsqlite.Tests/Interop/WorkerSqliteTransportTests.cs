using Bit.Bsqlite.Data;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage;
using Microsoft.JSInterop;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// The transport is the only .NET type that talks to the worker, so these tests pin the calls it
/// makes and the way it turns a reply into either a result or a <see cref="BsqliteException"/>.
/// The worker itself is covered by the browser suite; this suite does not need one.
/// </summary>
[TestClass]
public sealed class WorkerSqliteTransportTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task Open_ImportsTheHostModule_ThenCreatesAHost_ThenCallsOpen_ThenSubscribes()
    {
        var js = new ScriptedJsRuntime();
        js.EnqueueEnvelope("""{ "ok": true, "result": { "build": "synchronous", "reused": false } }""");

        await using var transport = new BsqliteWorkerTransport(js, DefaultOptions());
        await transport.OpenAsync("app.db", Ct);

        AssertSequence.Equal(
            ["import", "module.createHost", "module.listen", "host.call"],
            js.Calls.Select(c => c.Identifier));
        // Stamped, not bare: the version in the query is what keeps a browser from answering the
        // import with a module it cached under an earlier one.
        Assert.AreEqual(BsqliteWorkerTransport.VersionedHostModuleUrl, js.Calls[0].Args[0]);
        Assert.StartsWith(
            BsqliteWorkerTransport.DefaultHostModuleUrl + "?v=",
            BsqliteWorkerTransport.VersionedHostModuleUrl,
            StringComparison.Ordinal);

        var request = OpenRequest(js);
        Assert.AreEqual("open", Read(request, "kind"));
        Assert.AreEqual("app.db", Read(request, "databaseName"));
        Assert.AreEqual("synchronous", Read(request, "requiredBuild"));
        Assert.IsNull(Read(request, "vfs"));
        Assert.AreEqual(nameof(BsqliteWorkerTransport.OnTablesChanged), js.Subscription!.Value.Method);
    }

    /// <summary>
    /// The subscription is what makes a live query re-run for a committed write - this tab's or
    /// another's - and it has to be in place before the open so a write during startup is not missed.
    /// </summary>
    [TestMethod]
    public async Task Open_SubscribesToCommittedWrites_ForThisDatabaseOnly()
    {
        var js = new ScriptedJsRuntime();
        js.EnqueueEnvelope("""{ "ok": true, "result": { "build": "synchronous", "reused": false } }""");

        await using var transport = new BsqliteWorkerTransport(js, DefaultOptions());
        await transport.OpenAsync("app.db", Ct);

        var listen = js.Calls.Single(c => c.Identifier == "module.listen");
        Assert.AreEqual("app.db", listen.Args[2]);
        Assert.IsInstanceOfType<DotNetObjectReference<BsqliteWorkerTransport>>(listen.Args[1]);

        // The worker reports this tab's own writes too, from the engine's hooks and only once
        // committed, so the command layer must not raise them again from the SQL text.
        Assert.IsTrue(transport.ReportsLocalWrites);
    }

    [TestMethod]
    public async Task OnTablesChanged_RaisesTheTransportEvent()
    {
        var js = new ScriptedJsRuntime();
        js.EnqueueEnvelope("""{ "ok": true, "result": { "build": "synchronous", "reused": false } }""");

        await using var transport = new BsqliteWorkerTransport(js, DefaultOptions());
        await transport.OpenAsync("app.db", Ct);

        BsqliteTablesChangedEventArgs? seen = null;
        transport.TablesChanged += (_, e) => seen = e;

        transport.OnTablesChanged(["product", "customer"]);

        Assert.IsNotNull(seen);
        Assert.Contains("product", seen.Tables);
        Assert.Contains("customer", seen.Tables);
    }

    /// <summary>
    /// Through <see cref="IBsqliteTransport"/>, not the concrete type. The interface declares
    /// <c>TablesChanged</c> with a no-op default implementation so existing transports still
    /// compile, and if the worker transport's own event failed to implement it the subscription
    /// would bind to that default and cross-tab live queries would go quiet with nothing to see.
    /// </summary>
    [TestMethod]
    public async Task ARemoteWrite_ReachesAConnectionThroughTheInterface()
    {
        var js = new ScriptedJsRuntime();
        js.EnqueueEnvelope("""{ "ok": true, "result": { "build": "synchronous", "reused": false } }""");

        var transport = new BsqliteWorkerTransport(js, DefaultOptions());
        IBsqliteTransport asInterface = transport;
        await asInterface.OpenAsync("app.db", Ct);

        await using var connection = new BsqliteConnection(asInterface, "app.db");
        BsqliteTablesChangedEventArgs? seen = null;
        connection.TablesChanged += (_, e) => seen = e;

        transport.OnTablesChanged(["product"]);

        Assert.IsNotNull(seen);
        Assert.Contains("product", seen.Tables);
    }

    [TestMethod]
    public async Task Open_SubscribesOnlyOnce_WhenCalledAgain()
    {
        var js = new ScriptedJsRuntime();
        js.EnqueueEnvelope("""{ "ok": true, "result": { "build": "synchronous", "reused": false } }""");
        js.EnqueueEnvelope("""{ "ok": true, "result": { "build": "synchronous", "reused": true } }""");

        await using var transport = new BsqliteWorkerTransport(js, DefaultOptions());
        await transport.OpenAsync("app.db", Ct);
        await transport.OpenAsync("app.db", Ct);

        Assert.ContainsSingle(c => c.Identifier == "module.listen", js.Calls);
    }

    [TestMethod]
    public async Task Open_EnablesForeignKeys_ByDefault_AndForwardsExtraPragmas()
    {
        var js = new ScriptedJsRuntime();
        js.EnqueueEnvelope("""{ "ok": true, "result": { "build": "synchronous" } }""");

        var options = new BsqliteWorkerTransportOptions
        {
            RequiredBuild = BsqliteEngineBuild.Synchronous,
            OnOpenSql = ["PRAGMA cache_size=-16000"],
        };

        await using var transport = new BsqliteWorkerTransport(js, options);
        await transport.OpenAsync("app.db", Ct);

        var open = OpenRequest(js);
        Assert.AreEqual(true, Read(open, "foreignKeys"));
        AssertSequence.Equal(["PRAGMA cache_size=-16000"], Assert.IsInstanceOfType<IEnumerable<string>>(Read(open, "onOpenSql")));
    }

    [TestMethod]
    public async Task Open_CanLeaveForeignKeysAlone()
    {
        var js = new ScriptedJsRuntime();
        js.EnqueueEnvelope("""{ "ok": true, "result": { "build": "synchronous" } }""");

        await using var transport = new BsqliteWorkerTransport(js, new BsqliteWorkerTransportOptions
        {
            RequiredBuild = BsqliteEngineBuild.Synchronous,
            EnableForeignKeys = false,
        });
        await transport.OpenAsync("app.db", Ct);

        var open = OpenRequest(js);
        Assert.AreEqual(false, Read(open, "foreignKeys"));
        Assert.IsEmpty(Assert.IsInstanceOfType<IEnumerable<string>>(Read(open, "onOpenSql")));
    }

    [TestMethod]
    public async Task Open_PassesTheVfsModuleAndExtensionsTheProviderDeclared()
    {
        var js = new ScriptedJsRuntime();
        js.EnqueueEnvelope("""{ "ok": true, "result": { "build": "jspi", "reused": false } }""");

        var options = new BsqliteWorkerTransportOptions
        {
            RequiredBuild = BsqliteEngineBuild.AsyncCapable,
            Vfs = new BsqliteJsModule("./_content/Bit.Bsqlite.Storage.IndexedDb/bit-bsqlite-idb-vfs.js"),
            ExtensionModules = [new BsqliteJsModule("./functions.js", "install")],
        };

        await using var transport = new BsqliteWorkerTransport(js, options);
        await transport.OpenAsync("app.db", Ct);

        var open = OpenRequest(js);
        var vfs = Read(open, "vfs")!;
        Assert.AreEqual("asyncCapable", Read(open, "requiredBuild"));
        Assert.AreEqual("./_content/Bit.Bsqlite.Storage.IndexedDb/bit-bsqlite-idb-vfs.js", Read(vfs, "moduleUrl"));
        Assert.AreEqual("register", Read(vfs, "registerExport"));

        var extension = Assert.ContainsSingle((object?[])Read(open, "extensions")!)!;
        Assert.AreEqual("./functions.js", Read(extension, "moduleUrl"));
        Assert.AreEqual("install", Read(extension, "registerExport"));
    }

    [TestMethod]
    public async Task Execute_SendsTheBinaryBatch_AndDecodesTheBinaryReply()
    {
        var js = new ScriptedJsRuntime();
        js.EnqueueEnvelope("""{ "ok": true, "result": { "build": "synchronous" } }""");
        js.EnqueueReply(new ReplyBuilder()
            .Result(["v"], [BsqliteWireFormat.TypeCode.Integer])
            .Row(9007199254740993L)
            .End()
            .Build());

        await using var transport = new BsqliteWorkerTransport(js, DefaultOptions());
        await transport.OpenAsync("app.db", Ct);

        var results = await transport.ExecuteAsync(
        [
            new BsqliteCommandRequest
            {
                CommandText = "SELECT v FROM t",
                ResultKind = BsqliteResultKind.Reader,
            },
        ], Ct);

        var call = js.Calls.Last(c => c.Identifier == "host.executeBinary");
        var sent = Assert.IsInstanceOfType<byte[]>(call.Args[0]);
        Assert.AreEqual(BsqliteWireFormat.Magic, sent[0]);

        var row = Assert.ContainsSingle(Assert.ContainsSingle(results).Rows);
        Assert.AreEqual(9007199254740993L, Assert.ContainsSingle(row));
    }

    [TestMethod]
    public async Task Execute_ThrowsTypedException_WhenTheReplyIsAFailure()
    {
        var js = new ScriptedJsRuntime();
        js.EnqueueEnvelope("""{ "ok": true, "result": { "build": "synchronous" } }""");
        js.EnqueueReply(ReplyBuilder.Error("UNIQUE constraint failed: t.id", 19, "SQLiteError"));

        await using var transport = new BsqliteWorkerTransport(js, DefaultOptions());
        await transport.OpenAsync("app.db", Ct);

        var error = await Assert.ThrowsExactlyAsync<BsqliteConstraintException>(
            () => transport.ExecuteAsync(
            [
                new BsqliteCommandRequest { CommandText = "INSERT INTO t (id) VALUES (1)" },
            ], Ct));

        Assert.AreEqual(19, error.SqliteErrorCode);
        Assert.Contains("UNIQUE", error.Message);
    }

    [TestMethod]
    public async Task Execute_BeforeOpen_Throws()
    {
        await using var transport = new BsqliteWorkerTransport(new ScriptedJsRuntime(), DefaultOptions());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => transport.ExecuteAsync([], Ct));
    }

    [TestMethod]
    public async Task Close_IsANoOp_WhenNothingWasOpened()
    {
        var js = new ScriptedJsRuntime();
        await using var transport = new BsqliteWorkerTransport(js, DefaultOptions());

        await transport.CloseAsync(Ct);

        Assert.IsEmpty(js.Calls);
    }

    [TestMethod]
    public async Task Dispose_TerminatesTheWorker()
    {
        var js = new ScriptedJsRuntime();
        js.EnqueueEnvelope("""{ "ok": true, "result": { "build": "synchronous" } }""");

        var transport = new BsqliteWorkerTransport(js, DefaultOptions());
        await transport.OpenAsync("app.db", Ct);
        await transport.DisposeAsync();

        Assert.IsTrue(js.HostDisposed);
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => transport.OpenAsync("app.db", Ct));
    }

    private static BsqliteWorkerTransportOptions DefaultOptions() => new()
    {
        RequiredBuild = BsqliteEngineBuild.Synchronous,
    };

    private static object OpenRequest(ScriptedJsRuntime js)
        => js.Calls.First(c => c.Identifier == "host.call").Args[0]!;

    private static object? Read(object target, string property)
        => target.GetType().GetProperty(property)?.GetValue(target);
}
