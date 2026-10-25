using System.Collections.Concurrent;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// Remembers sticky bindings in a file, for a Blazor Hybrid app: its databases live on the device, so
/// their bindings do too, rather than in the web view's <c>localStorage</c> - which may not be reachable
/// yet when the app opens a database, and is cleared with the web view's data.
/// </summary>
/// <remarks>
/// One line per database, its name and its provider's, each escaped. Written whole to a staging file and
/// moved over the old one, so a crash leaves either the old bindings or the new ones.
/// </remarks>
internal sealed class BsqliteFileStorageBindingStore(string path) : IBsqliteStorageBindingStore
{
    // Every scope - every web view of the app - shares the file, so they share its lock too.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _path = Path.GetFullPath(path);

    public async ValueTask<string?> GetProviderNameAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var gate = Gate();
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return (await ReadAsync(cancellationToken).ConfigureAwait(false)).GetValueOrDefault(databaseName);
        }
        finally
        {
            gate.Release();
        }
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
