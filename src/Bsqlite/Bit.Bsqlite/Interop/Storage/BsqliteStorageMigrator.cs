using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// Copies a database from one backend to another without flipping the binding until the copy
/// looks like a SQLite file.
/// </summary>
/// <remarks>
/// <para>
/// Probe target → source has anything → quota headroom → copy → header check → flip binding →
/// delete source. A failure leaves the source and the binding untouched. <c>PRAGMA integrity_check</c>
/// needs an engine on the target; the header check is what we can always do, including on desktop.
/// </para>
/// <para>
/// The one step after the flip is deleting the source, and it can fail - another tab still holding
/// an OPFS file open, say. If the source is still all there, the flip is undone and the copy dropped,
/// so the binding keeps naming the backend whose file is current. If it is already gone, the move
/// stands. Either way the failure is rethrown, and the binding is the record of where the database
/// now lives: a caller that carries on should resolve again rather than assume.
/// </para>
/// </remarks>
public sealed class BsqliteStorageMigrator
{
    private static ReadOnlySpan<byte> SqliteMagic => "SQLite format 3\0"u8;

    public async Task MigrateAsync(
        string databaseName,
        IBsqliteStorageProvider source,
        IBsqliteStorageProvider target,
        IBsqliteStorageBindingStore bindingStore,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(bindingStore);

        if (string.Equals(source.Name, target.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var probe = await target.ProbeAsync(cancellationToken).ConfigureAwait(false);
        if (!probe.IsAvailable)
        {
            throw new BsqliteStorageUnavailableException(
                databaseName,
                [
                    new BsqliteStorageCandidateOutcome
                    {
                        ProviderName = target.Name,
                        Status = BsqliteStorageCandidateStatus.Unavailable,
                        Probe = probe,
                        Explanation = "The migration target is unavailable.",
                    },
                ]);
        }

        // Nothing to move. Exporting anyway would throw - most backends report a missing database
        // that way - and on the AutomaticOnOpen path that turns "a better backend appeared" into an
        // open that fails outright. The in-memory backend reaches this every time: its admin store
        // is not where a connection's data lives, so it never holds an image to copy.
        if (!await source.Admin.ExistsAsync(databaseName, cancellationToken).ConfigureAwait(false))
        {
            await bindingStore.SetProviderNameAsync(databaseName, target.Name, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var image = await source.Admin.ExportAsync(databaseName, cancellationToken).ConfigureAwait(false);

        if (probe.QuotaBytes is long quota
            && probe.UsageBytes is long usage
            && quota - usage < image.LongLength)
        {
            throw new BsqliteQuotaExceededException(
                $"Moving '{databaseName}' to '{target.Name}' needs {image.Length} bytes, but the "
                + $"origin only has {quota - usage} bytes free.");
        }

        VerifySqliteImage(image);

        await target.Admin.ImportAsync(databaseName, image, cancellationToken).ConfigureAwait(false);

        try
        {
            var copy = await target.Admin.ExportAsync(databaseName, cancellationToken).ConfigureAwait(false);
            if (!copy.AsSpan().SequenceEqual(image))
            {
                throw new BsqliteCorruptDatabaseException(
                    $"The copy of '{databaseName}' on '{target.Name}' does not match the source.");
            }
        }
        catch
        {
            await target.Admin.DeleteAsync(databaseName, cancellationToken).ConfigureAwait(false);
            throw;
        }

        // From the flip on nothing is cancellable. A cancel that landed after the binding was written,
        // or while the source was still being deleted, would send the cleanup below after a copy that
        // is the database by then.
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await bindingStore.SetProviderNameAsync(databaseName, target.Name, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
            // The copy is nobody's - unless the write landed and only its report failed.
            if (!await BindingMayNameAsync(bindingStore, databaseName, target.Name).ConfigureAwait(false))
            {
                await DeleteQuietlyAsync(target, databaseName).ConfigureAwait(false);
            }

            throw;
        }

        try
        {
            await source.Admin.DeleteAsync(databaseName, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            await UndoFlipIfSourceSurvivedAsync(databaseName, source, target, bindingStore).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Whether the binding names <paramref name="providerName"/> - or cannot be read to tell.</summary>
    private static async Task<bool> BindingMayNameAsync(
        IBsqliteStorageBindingStore bindingStore,
        string databaseName,
        string providerName)
    {
        try
        {
            return string.Equals(
                await bindingStore.GetProviderNameAsync(databaseName).ConfigureAwait(false),
                providerName,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return true;
        }
    }

    /// <summary>
    /// Points the binding back at <paramref name="source"/> when the failed delete left it whole.
    /// </summary>
    /// <remarks>
    /// Runs on the failure path, so nothing here is cancellable and nothing here throws over the
    /// original error. When it cannot tell whether the source survived, or cannot restore the
    /// binding, the binding stays on the verified copy - the one place known to hold all the data.
    /// </remarks>
    private static async Task UndoFlipIfSourceSurvivedAsync(
        string databaseName,
        IBsqliteStorageProvider source,
        IBsqliteStorageProvider target,
        IBsqliteStorageBindingStore bindingStore)
    {
        try
        {
            if (!await source.Admin.ExistsAsync(databaseName).ConfigureAwait(false))
            {
                return;
            }

            await bindingStore.SetProviderNameAsync(databaseName, source.Name).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return;
        }

        await DeleteQuietlyAsync(target, databaseName).ConfigureAwait(false);
    }

    private static async Task DeleteQuietlyAsync(IBsqliteStorageProvider provider, string databaseName)
    {
        try
        {
            await provider.Admin.DeleteAsync(databaseName).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Left behind, but unbound: nothing opens it, and the next migration's import replaces it.
        }
    }

    internal static void VerifySqliteImage(ReadOnlyMemory<byte> image)
    {
        if (image.Length == 0)
        {
            return;
        }

        if (image.Length < SqliteMagic.Length || !image.Span[..SqliteMagic.Length].SequenceEqual(SqliteMagic))
        {
            throw new BsqliteCorruptDatabaseException(
                "The exported image is not a SQLite database (missing the 'SQLite format 3' header).");
        }
    }
}
