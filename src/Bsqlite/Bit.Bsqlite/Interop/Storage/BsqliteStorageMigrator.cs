using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// Copies a database from one backend to another without flipping the binding until the copy
/// looks like a SQLite file.
/// </summary>
/// <remarks>
/// Probe target → source has anything → quota headroom → copy → header check → flip binding →
/// delete source. A failure leaves the source and the binding untouched. <c>PRAGMA integrity_check</c>
/// needs an engine on the target; the header check is what we can always do, including on desktop.
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

        await bindingStore.SetProviderNameAsync(databaseName, target.Name, cancellationToken)
            .ConfigureAwait(false);
        await source.Admin.DeleteAsync(databaseName, cancellationToken).ConfigureAwait(false);
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
