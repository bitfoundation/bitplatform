using System.Diagnostics;
using System.Globalization;
using Bit.Bcrdt.Blazor;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// What <see cref="BcrdtSyncStatus"/> reads off a document and says about it: the snapshot it takes and the words its
/// default rendering shows.
/// </summary>
/// <remarks>
/// The component itself - the live region, the toggle, following the connection in a real page - runs in the
/// end-to-end suite (<c>LiveTests</c>); these pin down the state and the wording it is built from.
/// </remarks>
[TestClass]
public class SyncStatusTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    private sealed class RoomTransportFactory(BcrdtInMemoryNetwork network) : IBcrdtTransportFactory
    {
        public IBcrdtTransport Create(string documentId) => network.CreateTransport(documentId);
    }

    private static async Task AssertEventuallyAsync(Func<bool> condition, string because)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            Assert.IsLessThan(Patience, clock.Elapsed, because);
            await Task.Delay(10);
        }
    }

    [TestMethod]
    public async Task A_document_without_a_transport_is_local_only()
    {
        await using var service = new BcrdtCollaborationService(new BcrdtCollaborationOptions { CreateUndoManager = false });
        BcrdtCollaborativeDocument document = await service.OpenAsync("alone", cancellationToken: TestContext.CancellationToken);

        var status = BcrdtSyncStatusSnapshot.Of(document);

        Assert.IsTrue(status.IsLocalOnly);
        Assert.IsFalse(status.HasTransport);
        Assert.IsFalse(status.IsSynced);
        Assert.IsFalse(status.IsReadOnly);
        // Nothing to bring back online, so nothing offers to.
        Assert.IsFalse(status.IsOffline);
        Assert.IsFalse(status.IsBusy);
        Assert.IsNull(status.LastSyncedAt);
        Assert.AreEqual("Local only", BcrdtSyncStatus.Describe(status, CultureInfo.InvariantCulture));
        Assert.AreEqual("local", BcrdtSyncStatus.StatusName(status));
    }

    [TestMethod]
    public async Task The_snapshot_follows_the_connection_offline_and_back()
    {
        var network = new BcrdtInMemoryNetwork();
        await using var service = new BcrdtCollaborationService(
            new BcrdtCollaborationOptions
            {
                Sync = new BcrdtSyncProviderOptions { HeartbeatInterval = TimeSpan.Zero },
                CreateUndoManager = false,
            },
            new RoomTransportFactory(network));
        BcrdtCollaborativeDocument document = await service.OpenAsync("shared", cancellationToken: TestContext.CancellationToken);

        await AssertEventuallyAsync(() => BcrdtSyncStatusSnapshot.Of(document).IsSynced, "the document should sync");
        var synced = BcrdtSyncStatusSnapshot.Of(document);
        Assert.AreEqual(BcrdtConnectionStatus.Synced, synced.Status);
        Assert.IsTrue(synced.HasTransport);
        Assert.IsFalse(synced.IsOffline);
        Assert.IsNotNull(synced.LastSyncedAt);
        Assert.AreEqual(document.Sync!.LastSyncedAt, synced.LastSyncedAt);
        Assert.AreEqual("Synced", BcrdtSyncStatus.Describe(synced, CultureInfo.InvariantCulture));
        Assert.AreEqual("synced", BcrdtSyncStatus.StatusName(synced));

        await document.DisconnectAsync(TestContext.CancellationToken);
        await AssertEventuallyAsync(() => BcrdtSyncStatusSnapshot.Of(document).IsOffline, "the document should go offline");
        var offline = BcrdtSyncStatusSnapshot.Of(document);

        // How fresh the offline copy is: the moment it last caught up, which going offline does not move.
        Assert.AreEqual(synced.LastSyncedAt, offline.LastSyncedAt);
        Assert.AreEqual(
            "Offline - last synced " + synced.LastSyncedAt!.Value.ToLocalTime().ToString("T", CultureInfo.InvariantCulture),
            BcrdtSyncStatus.Describe(offline, CultureInfo.InvariantCulture));
        Assert.AreEqual("disconnected", BcrdtSyncStatus.StatusName(offline));

        await document.ConnectAsync(TestContext.CancellationToken);
        await AssertEventuallyAsync(() => BcrdtSyncStatusSnapshot.Of(document).IsSynced, "the document should sync again");

        // A snapshot is a value: the one taken offline still says so.
        Assert.IsTrue(offline.IsOffline);
    }

    [TestMethod]
    [DataRow(BcrdtConnectionStatus.Connecting, "Connecting - never synced", false, true)]
    [DataRow(BcrdtConnectionStatus.Handshaking, "Syncing - never synced", false, true)]
    [DataRow(BcrdtConnectionStatus.Reconnecting, "Reconnecting - never synced", false, true)]
    [DataRow(BcrdtConnectionStatus.Faulted, "Connection failed - never synced", true, false)]
    [DataRow(BcrdtConnectionStatus.Disconnected, "Offline - never synced", true, false)]
    public void Every_state_is_said_in_words(BcrdtConnectionStatus state, string words, bool offline, bool busy)
    {
        var status = new BcrdtSyncStatusSnapshot(state, HasTransport: true, BcrdtDocumentAccess.ReadWrite, LastSyncedAt: null);

        Assert.AreEqual(words, BcrdtSyncStatus.Describe(status, CultureInfo.InvariantCulture));
        Assert.AreEqual(offline, status.IsOffline);
        Assert.AreEqual(busy, status.IsBusy);
        Assert.IsFalse(status.IsSynced);
        Assert.AreEqual(state.ToString().ToLowerInvariant(), BcrdtSyncStatus.StatusName(status));
    }

    [TestMethod]
    public void Read_only_access_is_said_after_the_state()
    {
        var synced = new BcrdtSyncStatusSnapshot(BcrdtConnectionStatus.Synced, HasTransport: true, BcrdtDocumentAccess.ReadOnly, DateTimeOffset.UtcNow);
        Assert.IsTrue(synced.IsReadOnly);
        Assert.AreEqual("Synced - read-only", BcrdtSyncStatus.Describe(synced, CultureInfo.InvariantCulture));

        var at = new DateTimeOffset(2026, 11, 11, 9, 30, 15, TimeSpan.Zero);
        var reconnecting = synced with { Status = BcrdtConnectionStatus.Reconnecting, LastSyncedAt = at };
        Assert.AreEqual(
            "Reconnecting - last synced " + at.ToLocalTime().ToString("T", CultureInfo.InvariantCulture) + " - read-only",
            BcrdtSyncStatus.Describe(reconnecting, CultureInfo.InvariantCulture));
    }

    public TestContext TestContext { get; set; }
}
