using Bit.Bcrdt.Butil;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bcrdt.Tests.Integrations;

/// <summary>
/// What the browser lifecycle does with the open documents when the browser reports a change.
/// </summary>
/// <remarks>
/// The browser is a fake that raises the signals Bit.Butil would; everything behind it - the collaboration
/// service, the sync providers, the stores, presence - is real. That the signals reach .NET from a real page (an
/// <c>online</c> event after the network returns, <c>pagehide</c> on a closed tab, a page restored from the
/// back/forward cache) is a browser test's job: a Playwright suite would toggle
/// <c>BrowserContext.SetOfflineAsync</c>, hide and close pages, and navigate away and back.
/// </remarks>
[TestClass]
public class BrowserLifecycleTests
{
    // Long enough that only the lifecycle can have made a document reconnect within a test's lifetime.
    private static readonly BcrdtReconnectPolicy Patient = new() { BaseDelay = TimeSpan.FromDays(10), MaxDelay = TimeSpan.FromDays(10) };

    private static BcrdtCollaborationService CreateClient(IBcrdtTransportFactory? transports = null, IBcrdtDocumentStore? store = null, bool connect = true) =>
        new(
            new BcrdtCollaborationOptions
            {
                ConnectOnOpen = connect,
                CreateUndoManager = false,
                Sync = new BcrdtSyncProviderOptions { HeartbeatInterval = TimeSpan.Zero, Reconnect = Patient },
            },
            transports,
            store);

    [TestMethod]
    public async Task Coming_back_online_reconnects_a_document_waiting_out_its_backoff_at_once()
    {
        var network = new BcrdtInMemoryNetwork();
        var link = new SwitchableNetwork(network) { Up = false };
        await using BcrdtCollaborationService client = CreateClient(link);
        await using BcrdtCollaborationService peer = CreateClient(new SwitchableNetwork(network));
        var browser = new FakeBrowser();
        await using var lifecycle = new BcrdtBrowserLifecycle(client, browser, null);
        await lifecycle.StartAsync();

        BcrdtCollaborativeDocument waiting = await client.OpenAsync("shared");
        BcrdtCollaborativeDocument stopped = await client.OpenAsync("stopped");
        BcrdtCollaborativeDocument remote = await peer.OpenAsync("shared");
        await WaitUntilAsync(() => waiting.ConnectionStatus == BcrdtConnectionStatus.Reconnecting);
        await stopped.DisconnectAsync();
        await WaitUntilAsync(() => stopped.ConnectionStatus == BcrdtConnectionStatus.Disconnected);

        waiting.Text("body").Insert(0, "typed offline");
        link.Up = true;
        await browser.RaiseAsync(BcrdtBrowserSignal.Online);

        await WaitUntilAsync(() => waiting.IsSynced && remote.Text("body").ToString() == "typed offline");

        // A document the application took offline on purpose stays offline.
        Assert.AreEqual(BcrdtConnectionStatus.Disconnected, stopped.ConnectionStatus);
    }

    [TestMethod]
    public async Task A_failed_attempt_on_coming_back_online_leaves_one_reconnection_loop()
    {
        var link = new SwitchableNetwork(new BcrdtInMemoryNetwork()) { Up = false };
        await using BcrdtCollaborationService client = CreateClient(link);
        var browser = new FakeBrowser();
        await using var lifecycle = new BcrdtBrowserLifecycle(client, browser, null);
        await lifecycle.StartAsync();

        BcrdtCollaborativeDocument document = await client.OpenAsync("shared");
        await WaitUntilAsync(() => document.ConnectionStatus == BcrdtConnectionStatus.Reconnecting);
        int before = link.Attempts;

        // The browser was wrong: the link is still down.
        await browser.RaiseAsync(BcrdtBrowserSignal.Online);
        await WaitUntilAsync(() => document.ConnectionStatus == BcrdtConnectionStatus.Reconnecting);
        await Task.Delay(100);

        Assert.AreEqual(before + 1, link.Attempts);
        Assert.AreEqual(BcrdtConnectionStatus.Reconnecting, document.ConnectionStatus);
    }

    [TestMethod]
    public async Task Being_shown_again_reconnects_unless_the_browser_is_offline()
    {
        var network = new BcrdtInMemoryNetwork();
        var link = new SwitchableNetwork(network) { Up = false };
        await using BcrdtCollaborationService client = CreateClient(link);
        var browser = new FakeBrowser { Online = false };
        await using var lifecycle = new BcrdtBrowserLifecycle(client, browser, null);
        var changes = new List<bool>();
        lifecycle.OnlineChanged += changes.Add;
        await lifecycle.StartAsync();

        Assert.IsFalse(lifecycle.IsOnline);
        BcrdtCollaborativeDocument document = await client.OpenAsync("shared");
        await WaitUntilAsync(() => document.ConnectionStatus == BcrdtConnectionStatus.Reconnecting);
        int before = link.Attempts;

        link.Up = true;
        await browser.RaiseAsync(BcrdtBrowserSignal.Visible);
        Assert.AreEqual(before, link.Attempts);

        await browser.RaiseAsync(BcrdtBrowserSignal.Online);
        await browser.RaiseAsync(BcrdtBrowserSignal.Offline);
        await browser.RaiseAsync(BcrdtBrowserSignal.Online);
        await WaitUntilAsync(() => document.IsSynced);

        CollectionAssert.AreEqual(new[] { false, true, false, true }, changes);
        Assert.IsTrue(lifecycle.IsOnline);

        // Back in front after the connection dropped while hidden: no waiting for the backoff either.
        link.Up = false;
        await link.DropAllAsync();
        await WaitUntilAsync(() => document.ConnectionStatus == BcrdtConnectionStatus.Reconnecting);
        link.Up = true;
        await browser.RaiseAsync(BcrdtBrowserSignal.Visible);
        await WaitUntilAsync(() => document.IsSynced);
    }

    [TestMethod]
    public async Task Turning_reconnection_off_leaves_the_backoff_alone()
    {
        var link = new SwitchableNetwork(new BcrdtInMemoryNetwork()) { Up = false };
        await using BcrdtCollaborationService client = CreateClient(link);
        var browser = new FakeBrowser();
        await using var lifecycle = new BcrdtBrowserLifecycle(client, browser, new BcrdtBrowserLifecycleOptions { ReconnectWhenOnline = false });
        await lifecycle.StartAsync();

        BcrdtCollaborativeDocument document = await client.OpenAsync("shared");
        await WaitUntilAsync(() => document.ConnectionStatus == BcrdtConnectionStatus.Reconnecting);
        int before = link.Attempts;

        link.Up = true;
        await browser.RaiseAsync(BcrdtBrowserSignal.Online);
        await Task.Delay(100);

        Assert.AreEqual(before, link.Attempts);
        Assert.AreEqual(BcrdtConnectionStatus.Reconnecting, document.ConnectionStatus);
    }

    [TestMethod]
    public async Task Hiding_the_page_waits_for_every_open_document_to_be_stored_then_runs_the_stores_flush()
    {
        var store = new SlowStore(TimeSpan.FromMilliseconds(150));
        await using BcrdtCollaborationService client = CreateClient(store: store, connect: false);
        string? seenByAfterFlush = null;
        int afterFlushes = 0;
        var browser = new FakeBrowser();
        await using var lifecycle = new BcrdtBrowserLifecycle(client, browser, new BcrdtBrowserLifecycleOptions
        {
            AfterFlush = async _ =>
            {
                Interlocked.Increment(ref afterFlushes);
                BcrdtStoredDocument stored = await store.LoadAsync("notes");
                seenByAfterFlush = stored.Update is null ? null : Text(stored.Update);
            },
        });
        await lifecycle.StartAsync();

        BcrdtCollaborativeDocument notes = await client.OpenAsync("notes");
        BcrdtCollaborativeDocument other = await client.OpenAsync("other");
        notes.Text("body").Insert(0, "saved before the page went away");
        other.Text("body").Insert(0, "and this");

        // Hidden twice while the first flush is still waiting: one flush, not two.
        Task first = browser.RaiseAsync(BcrdtBrowserSignal.Hidden);
        Task second = browser.RaiseAsync(BcrdtBrowserSignal.Freeze);
        await Task.WhenAll(first, second);

        Assert.AreEqual("saved before the page went away", seenByAfterFlush);
        Assert.AreEqual("and this", Text((await store.LoadAsync("other")).Update!));
        Assert.AreEqual(1, afterFlushes);

        await browser.RaiseAsync(BcrdtBrowserSignal.PageHide);
        Assert.AreEqual(2, afterFlushes);
    }

    [TestMethod]
    public async Task A_change_that_could_not_be_stored_is_reported_when_the_page_is_hidden()
    {
        await using BcrdtCollaborationService client = CreateClient(store: new RefusingStore(), connect: false);
        var browser = new FakeBrowser();
        await using var lifecycle = new BcrdtBrowserLifecycle(client, browser, new BcrdtBrowserLifecycleOptions
        {
            AfterFlush = _ => throw new InvalidOperationException("the store's own flush failed"),
        });
        var errors = new List<Exception>();
        lifecycle.Error += errors.Add;
        await lifecycle.StartAsync();

        BcrdtCollaborativeDocument document = await client.OpenAsync("doc");
        document.Text("body").Insert(0, "refused");
        await browser.RaiseAsync(BcrdtBrowserSignal.Hidden);

        Assert.HasCount(2, errors);
        Assert.IsInstanceOfType<IOException>(errors[0]);
        Assert.IsInstanceOfType<InvalidOperationException>(errors[1]);
    }

    [TestMethod]
    public async Task Leaving_the_page_withdraws_presence_and_coming_back_restores_it()
    {
        var network = new BcrdtInMemoryNetwork();
        await using BcrdtCollaborationService client = CreateClient(new SwitchableNetwork(network));
        await using BcrdtCollaborationService peer = CreateClient(new SwitchableNetwork(network));
        var browser = new FakeBrowser();
        await using var lifecycle = new BcrdtBrowserLifecycle(client, browser, null);
        await lifecycle.StartAsync();

        BcrdtCollaborativeDocument mine = await client.OpenAsync(
            "shared", new BcrdtOpenDocumentOptions { User = new BcrdtCollaboratorInfo("ada", "Ada", "#ff0000") });
        BcrdtCollaborativeDocument theirs = await peer.OpenAsync(
            "shared", new BcrdtOpenDocumentOptions { User = new BcrdtCollaboratorInfo("grace", "Grace", "#00ff00") });
        await WaitUntilAsync(() => theirs.Presence.Others.Any(other => other.User.Name == "Ada"));

        await browser.RaiseAsync(BcrdtBrowserSignal.PageHide);
        Assert.IsTrue(mine.Presence.IsSuspended);
        await WaitUntilAsync(() => !theirs.Presence.Others.Any(other => other.User.Name == "Ada"));

        await browser.RaiseAsync(BcrdtBrowserSignal.PageShow);
        Assert.IsFalse(mine.Presence.IsSuspended);
        await WaitUntilAsync(() => theirs.Presence.Others.Any(other => other.User.Name == "Ada"));
    }

    [TestMethod]
    public async Task Persistent_storage_is_asked_for_only_when_the_application_wants_it()
    {
        await using BcrdtCollaborationService client = CreateClient(connect: false);

        var unasked = new FakeBrowser { PersistGranted = true };
        await using (var lifecycle = new BcrdtBrowserLifecycle(client, unasked, null))
        {
            await lifecycle.StartAsync();
            Assert.IsNull(lifecycle.IsStoragePersistent);
            Assert.AreEqual(0, unasked.PersistRequests);
        }

        var asked = new FakeBrowser { PersistGranted = true };
        await using (var lifecycle = new BcrdtBrowserLifecycle(client, asked, new BcrdtBrowserLifecycleOptions { RequestPersistentStorage = true }))
        {
            await lifecycle.StartAsync();
            await lifecycle.StartAsync();
            Assert.IsTrue(lifecycle.IsStoragePersistent);
            Assert.AreEqual(1, asked.PersistRequests);
            Assert.AreEqual(1, asked.Subscriptions);
        }

        Assert.AreEqual(0, asked.Subscriptions);
    }

    [TestMethod]
    public async Task Outside_a_browser_starting_does_nothing()
    {
        await using BcrdtCollaborationService client = CreateClient(connect: false);
        var browser = new FakeBrowser { IsAvailable = false };
        await using var lifecycle = new BcrdtBrowserLifecycle(client, browser, new BcrdtBrowserLifecycleOptions { RequestPersistentStorage = true });

        await lifecycle.StartAsync();

        Assert.IsFalse(lifecycle.IsStarted);
        Assert.AreEqual(0, browser.Subscriptions);
        Assert.AreEqual(0, browser.PersistRequests);
    }

    [TestMethod]
    public async Task The_registration_resolves_through_Butil_and_never_touches_JavaScript_off_the_browser()
    {
        var services = new ServiceCollection();
        services.AddScoped<IJSRuntime, UnreachableJsRuntime>();
        services.AddBitBcrdtServices(collaboration => collaboration.UseBrowserLifecycle(options => options.RequestPersistentStorage = true));

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        BcrdtBrowserLifecycle lifecycle = scope.ServiceProvider.GetRequiredService<BcrdtBrowserLifecycle>();
        Assert.AreSame(lifecycle, scope.ServiceProvider.GetRequiredService<BcrdtBrowserLifecycle>());

        // The test host is no browser, so the lifecycle stays out of it - a call into JavaScript would throw here.
        await lifecycle.StartAsync();
        Assert.IsFalse(lifecycle.IsStarted);
        Assert.IsNull(lifecycle.IsStoragePersistent);
    }

    [TestMethod]
    public async Task Signals_after_disposal_are_ignored()
    {
        var link = new SwitchableNetwork(new BcrdtInMemoryNetwork()) { Up = false };
        await using BcrdtCollaborationService client = CreateClient(link);
        var browser = new FakeBrowser();
        var lifecycle = new BcrdtBrowserLifecycle(client, browser, null);
        await lifecycle.StartAsync();

        BcrdtCollaborativeDocument document = await client.OpenAsync("shared");
        await WaitUntilAsync(() => document.ConnectionStatus == BcrdtConnectionStatus.Reconnecting);
        await lifecycle.DisposeAsync();
        int before = link.Attempts;

        link.Up = true;
        await browser.Raise(BcrdtBrowserSignal.Online);

        Assert.AreEqual(0, browser.Subscriptions);
        Assert.AreEqual(before, link.Attempts);
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await lifecycle.StartAsync());
    }

    private static string Text(byte[] update)
    {
        using var doc = new BcrdtDocument();
        doc.ApplyUpdate(update);
        return doc.GetText("body").ToString();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int i = 0; i < 500 && !condition(); i++) await Task.Delay(10);
        Assert.IsTrue(condition(), "The condition was not met in time.");
    }

    /// <summary>The browser as the lifecycle sees it, raising whatever a test tells it to.</summary>
    private sealed class FakeBrowser : IBcrdtBrowser
    {
        private Func<BcrdtBrowserSignal, Task>? _handler;
        private Func<BcrdtBrowserSignal, Task>? _last;

        public bool IsAvailable { get; set; } = true;

        public bool Online { get; set; } = true;

        public bool PersistGranted { get; set; }

        public int PersistRequests { get; private set; }

        public int Subscriptions { get; private set; }

        /// <summary>Raises a signal the way the page would, and returns what the lifecycle made of it.</summary>
        public Task RaiseAsync(BcrdtBrowserSignal signal) => (_handler ?? throw new InvalidOperationException("Nothing is listening.")).Invoke(signal);

        /// <summary>Raises a signal at whatever last subscribed, even after it stopped listening.</summary>
        public Task Raise(BcrdtBrowserSignal signal) => _last?.Invoke(signal) ?? Task.CompletedTask;

        public ValueTask<IAsyncDisposable> SubscribeAsync(Func<BcrdtBrowserSignal, Task> handler, CancellationToken cancellationToken)
        {
            _handler = _last = handler;
            Subscriptions++;
            return ValueTask.FromResult<IAsyncDisposable>(new Unsubscribe(this));
        }

        public ValueTask<bool> IsOnlineAsync(CancellationToken cancellationToken) => ValueTask.FromResult(Online);

        public ValueTask<bool> RequestPersistentStorageAsync(CancellationToken cancellationToken)
        {
            PersistRequests++;
            return ValueTask.FromResult(PersistGranted);
        }

        private sealed class Unsubscribe(FakeBrowser browser) : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                browser._handler = null;
                browser.Subscriptions--;
                return ValueTask.CompletedTask;
            }
        }
    }

    /// <summary>An in-memory network whose connections can be refused, and dropped, as a network outage does.</summary>
    private sealed class SwitchableNetwork(BcrdtInMemoryNetwork network) : IBcrdtTransportFactory
    {
        private readonly List<IBcrdtTransport> _transports = [];
        private int _attempts;

        public volatile bool Up = true;

        public int Attempts => Volatile.Read(ref _attempts);

        public IBcrdtTransport Create(string documentId)
        {
            var transport = new Transport(this, network.CreateTransport(documentId));
            lock (_transports) _transports.Add(transport);
            return transport;
        }

        public async Task DropAllAsync()
        {
            IBcrdtTransport[] transports;
            lock (_transports) transports = [.. _transports];
            foreach (Transport transport in transports.Cast<Transport>()) await transport.DropAsync();
        }

        private sealed class Transport(SwitchableNetwork owner, IBcrdtTransport inner) : IBcrdtTransport
        {
            public BcrdtTransportState State => inner.State;

            public string Description => inner.Description;

            public event Action<ReadOnlyMemory<byte>>? MessageReceived
            {
                add => inner.MessageReceived += value;
                remove => inner.MessageReceived -= value;
            }

            public event Action<BcrdtTransportStateChange>? StateChanged
            {
                add => inner.StateChanged += value;
                remove => inner.StateChanged -= value;
            }

            public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref owner._attempts);
                return owner.Up ? inner.ConnectAsync(cancellationToken) : ValueTask.FromException(new IOException("The network is down."));
            }

            public ValueTask SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default) =>
                inner.SendAsync(message, cancellationToken);

            public ValueTask DisconnectAsync(CancellationToken cancellationToken = default) => inner.DisconnectAsync(cancellationToken);

            /// <summary>Loses the connection, as an outage does - not a disconnection the provider asked for.</summary>
            public ValueTask DropAsync() => inner.DisconnectAsync();

            public ValueTask DisposeAsync() => inner.DisposeAsync();
        }
    }

    /// <summary>An in-memory store whose appends take a while.</summary>
    private sealed class SlowStore(TimeSpan latency) : IBcrdtDocumentStore
    {
        private readonly BcrdtInMemoryDocumentStore _inner = new();

        public ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default) =>
            _inner.LoadAsync(documentId, cancellationToken);

        public async ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default)
        {
            await Task.Delay(latency, cancellationToken);
            await _inner.AppendUpdateAsync(documentId, update, cancellationToken);
        }

        public ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default) => _inner.CompactAsync(documentId, cancellationToken);

        public ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default) => _inner.DeleteAsync(documentId, cancellationToken);

        public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) => _inner.ListAsync(cancellationToken);
    }

    /// <summary>A store that refuses every update outright, as one over its record limit does.</summary>
    private sealed class RefusingStore : IBcrdtDocumentStore
    {
        public ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default) => default;

        public ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default) =>
            ValueTask.FromException(new ArgumentException("Refused."));

        public ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default) => default;

        public ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default) => default;

        public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) => new([]);
    }

    /// <summary>A JavaScript runtime that is not there.</summary>
    private sealed class UnreachableJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new InvalidOperationException($"JavaScript was called ({identifier}) outside a browser.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new InvalidOperationException($"JavaScript was called ({identifier}) outside a browser.");
    }
}
