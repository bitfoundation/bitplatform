using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// Remembers sticky bindings in a file, for a Blazor Hybrid app: its databases live on the device, so
/// their bindings do too, rather than in the web view's <c>localStorage</c> - which may not be reachable
/// yet when the app opens a database, and is cleared with the web view's data.
/// </summary>
/// <remarks>
/// <para>
/// One line per database, its name and its provider's, each escaped. Written whole to a staging file and
/// moved over the old one, so a crash leaves either the old bindings or the new ones; with none left, the
/// file is deleted.
/// </para>
/// <para>
/// A Hybrid app that ran Bsqlite in its web view before kept its databases on a browser backend there, and
/// their bindings in the web view's <c>localStorage</c>. A database with no binding in the file is looked up
/// in <c>previous</c> - that <c>localStorage</c> - too, so the app keeps finding that data instead of starting
/// an empty database on the device, and a binding found there is copied into the file, so later opens find it
/// without the web view. When the web view cannot answer - it is not up yet - the lookup finds nothing.
/// </para>
/// </remarks>
internal sealed class BsqliteFileStorageBindingStore(
    string path,
    IBsqliteStorageBindingStore? previous = null,
    ILogger<BsqliteFileStorageBindingStore>? logger = null) : IBsqliteStorageBindingStore
{
    /// <summary>The file's name in the file system backend's root.</summary>
    public const string FileName = ".bit-bsqlite-bindings";

    // Every scope - every web view of the app - shares the file, so they share its lock too.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _path = Path.GetFullPath(path);
    private readonly ILogger _logger = logger ?? NullLogger<BsqliteFileStorageBindingStore>.Instance;

    public async ValueTask<string?> GetProviderNameAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var gate = Gate();
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if ((await ReadAsync(cancellationToken).ConfigureAwait(false)).GetValueOrDefault(databaseName) is { } bound)
            {
                return bound;
            }
        }
        finally
        {
            gate.Release();
        }

        var adopted = await GetPreviousProviderNameAsync(databaseName, cancellationToken).ConfigureAwait(false);
        if (adopted is not null)
        {
            await UpdateAsync(bindings => bindings.TryAdd(databaseName, adopted), cancellationToken).ConfigureAwait(false);
        }

        return adopted;
    }

    public async ValueTask SetProviderNameAsync(string databaseName, string providerName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        await UpdateAsync(bindings => bindings[databaseName] = providerName, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask RemoveAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        await UpdateAsync(bindings => bindings.Remove(databaseName), cancellationToken).ConfigureAwait(false);

        if (previous is not null)
        {
            try
            {
                await previous.RemoveAsync(databaseName, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "Could not remove the web view's binding of database {DatabaseName}.", databaseName);
            }
        }
    }

    private async ValueTask<string?> GetPreviousProviderNameAsync(string databaseName, CancellationToken cancellationToken)
    {
        if (previous is null)
        {
            return null;
        }

        try
        {
            return await previous.GetProviderNameAsync(databaseName, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(
                ex,
                "Could not read the web view's binding of database {DatabaseName}; it is treated as having none.",
                databaseName);
            return null;
        }
    }

    private SemaphoreSlim Gate() => Gates.GetOrAdd(_path, _ => new SemaphoreSlim(1, 1));

    private async Task UpdateAsync(Action<Dictionary<string, string>> change, CancellationToken cancellationToken)
    {
        var gate = Gate();
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var bindings = await ReadAsync(cancellationToken).ConfigureAwait(false);
            change(bindings);

            if (bindings.Count == 0)
            {
                // An empty file would read as a database that was never written to anything listing the folder.
                if (File.Exists(_path))
                {
                    File.Delete(_path);
                }

                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var staging = $"{_path}.{Guid.NewGuid():N}.tmp";
            try
            {
                await File.WriteAllLinesAsync(
                        staging,
                        bindings.Select(binding => $"{Uri.EscapeDataString(binding.Key)}={Uri.EscapeDataString(binding.Value)}"),
                        cancellationToken)
                    .ConfigureAwait(false);
                File.Move(staging, _path, overwrite: true);
            }
            finally
            {
                File.Delete(staging);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<Dictionary<string, string>> ReadAsync(CancellationToken cancellationToken)
    {
        var bindings = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(_path))
        {
            return bindings;
        }

        foreach (var line in await File.ReadAllLinesAsync(_path, cancellationToken).ConfigureAwait(false))
        {
            var separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0)
            {
                bindings[Uri.UnescapeDataString(line[..separator])] = Uri.UnescapeDataString(line[(separator + 1)..]);
            }
        }

        return bindings;
    }
}
