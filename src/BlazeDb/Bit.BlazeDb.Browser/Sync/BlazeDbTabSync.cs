using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Bit.BlazeDb.Browser;

/// <summary>
/// Keeps read-only replica tabs in step with the writer tab. The writer announces each checkpoint
/// on a BroadcastChannel; replicas respond by reloading from storage.
/// <para>
/// Only the signal crosses tabs - never the data. Replicas re-read the snapshot and WAL the writer
/// has already made durable, so they can never observe a state the writer has not committed. The
/// consequence is that a replica trails the writer by up to one checkpoint; call
/// <see cref="BlazeDbDatabase.ReloadAsync"/> directly if a tab needs to catch up sooner.
/// </para>
/// </summary>
[SupportedOSPlatform("browser")]
public sealed class BlazeDbTabSync : IAsyncDisposable
{
    // Each instance owns one BroadcastChannel object, addressed by a handle of its own, so a
    // publisher and a replica for the same database in one document do not close each other's.
    private readonly string _handle = Guid.NewGuid().ToString("N");
    private readonly BlazeDbDatabase? _replica;
    private readonly Func<ValueTask>? _onChanged;
    private bool _disposed;

    private BlazeDbTabSync(BlazeDbDatabase? replica, Func<ValueTask>? onChanged)
    {
        _replica = replica;
        _onChanged = onChanged;
    }

    /// <summary>Whether this browser implements BroadcastChannel.</summary>
    public static async ValueTask<bool> IsSupportedAsync(string? moduleUrl = null)
    {
        await BlazeDbTabsInterop.EnsureModuleAsync(moduleUrl).ConfigureAwait(false);
        return BlazeDbTabsInterop.IsSupported();
    }

    /// <summary>
    /// Announces this tab's checkpoints to replicas. Wire the returned publisher up before opening
    /// the database by assigning <see cref="PublishCheckpoint"/> to
    /// <see cref="BlazeDbDatabaseOptions.OnCheckpoint"/>.
    /// </summary>
    public static async ValueTask<BlazeDbTabSync> CreatePublisherAsync(string databaseName, string? moduleUrl = null)
    {
        await BlazeDbTabsInterop.EnsureModuleAsync(moduleUrl).ConfigureAwait(false);
        var sync = new BlazeDbTabSync(replica: null, onChanged: null);
        BlazeDbTabsInterop.Open(sync._handle, ChannelName(databaseName), null);
        return sync;
    }

    /// <summary>
    /// Subscribes <paramref name="replica"/> to the writer tab's checkpoints, reloading it each
    /// time one lands. <paramref name="onChanged"/> runs after a successful reload - use it to
    /// re-run queries and refresh the UI.
    /// </summary>
    public static async ValueTask<BlazeDbTabSync> CreateReplicaAsync(
        string databaseName,
        BlazeDbDatabase replica,
        Func<ValueTask>? onChanged = null,
        string? moduleUrl = null)
    {
        ArgumentNullException.ThrowIfNull(replica);
        await BlazeDbTabsInterop.EnsureModuleAsync(moduleUrl).ConfigureAwait(false);

        var sync = new BlazeDbTabSync(replica, onChanged);
        BlazeDbTabsInterop.Open(sync._handle, ChannelName(databaseName), generation =>
        {
            // Fire and forget: the JS callback cannot await, and a failed reload is retried on the
            // next checkpoint anyway.
            _ = sync.OnCheckpointAnnouncedAsync();
        });
        return sync;
    }

    /// <summary>
    /// Hand this to <see cref="BlazeDbDatabaseOptions.OnCheckpoint"/> on the writer tab.
    /// </summary>
    public Action<ulong> PublishCheckpoint => generation => BlazeDbTabsInterop.Post(_handle, generation);

    /// <summary>Reloads the replica now, without waiting for the next checkpoint.</summary>
    public async ValueTask RefreshAsync()
    {
        if (_replica is null || _disposed)
        {
            return;
        }
        await _replica.ReloadAsync().ConfigureAwait(false);
        if (_onChanged is not null)
        {
            await _onChanged().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The error the most recent announced reload failed with, or null once one has succeeded.
    /// A failed reload is retried on the next checkpoint; this exists so the tab can tell that
    /// it is falling behind rather than having the failure vanish into an unobserved task.
    /// </summary>
    public Exception? LastError { get; private set; }

    private bool _reloading;
    private bool _reloadRequested;

    /// <summary>
    /// Reloads once per burst of announcements. A writer that checkpoints twice while the first
    /// reload is still reading would otherwise start a second one on top of it; the reload itself
    /// reads a whole generation before it touches the tables, so overlapping is safe, but one reload
    /// that runs again afterwards is the same result for less work.
    /// </summary>
    private async Task OnCheckpointAnnouncedAsync()
    {
        _reloadRequested = true;
        if (_reloading)
        {
            return;
        }
        _reloading = true;
        try
        {
            while (_reloadRequested)
            {
                _reloadRequested = false;
                await RefreshAsync().ConfigureAwait(false);
                LastError = null;
            }
        }
        catch (ObjectDisposedException)
        {
            // The tab tore the database down between the announcement and the reload.
        }
        catch (Exception ex)
        {
            // Nothing awaits this task, so an exception here would be lost; keep it visible instead.
            LastError = ex;
        }
        finally
        {
            _reloading = false;
        }
    }

    private static string ChannelName(string databaseName) => "blazedb:" + databaseName;

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            BlazeDbTabsInterop.Close(_handle);
        }
        return default;
    }
}
