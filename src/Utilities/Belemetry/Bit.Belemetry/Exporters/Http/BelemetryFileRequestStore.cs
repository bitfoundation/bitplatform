using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// Persists undeliverable requests as one JSON file each in a folder - Hybrid apps going offline, servers whose
/// backend is briefly down. Bounded by size and age.
/// </summary>
internal sealed class BelemetryFileRequestStore : IBelemetryRequestStore
{
    private const string Extension = ".blmreq";

    private readonly string _directory;
    private readonly long _maxBytes;
    private readonly TimeSpan _maxAge;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public BelemetryFileRequestStore(string directory, long maxBytes, TimeSpan maxAge)
    {
        _directory = directory;
        _maxBytes = Math.Max(64 * 1024, maxBytes);
        _maxAge = maxAge;
    }

    public static string DefaultDirectory(string serviceName)
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        if (string.IsNullOrEmpty(root)) root = Path.GetTempPath();

        var safe = string.Concat(serviceName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return Path.Combine(root, "bit-belemetry", safe);
    }

    public async ValueTask SaveAsync(BelemetryHttpRequest request, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(_directory);

            var path = PathOf(request);
            var temp = path + ".tmp";
            await using (var stream = File.Create(temp))
            {
                // Never headers on disk: they may hold API keys. The exporter adds its current ones on replay.
                await JsonSerializer.SerializeAsync(stream, request.WithoutHeaders(), BelemetryJsonContext.Default.BelemetryHttpRequest, cancellationToken);
            }
            File.Move(temp, path, overwrite: true);

            EvictOverLimit();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Saves <paramref name="request"/> synchronously, for a process that is ending (an unhandled exception), where
    /// nothing async completes. Without the store's lock - a save or a resend pass may hold it across an await that never
    /// resumes - which a file of its own does without: it is written under a temporary name and moved into place, so a
    /// load never reads it half-written.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="keep">Requests saved before it that the store makes room without: older files are evicted for it, never these.</param>
    /// <returns>
    /// Whether the store is within its size limit with it. When it is not, the file stays all the same: whether it is
    /// worth more than the limit is the caller's to say (<see cref="DeleteNow"/>).
    /// </returns>
    public bool SaveNow(BelemetryHttpRequest request, IReadOnlyCollection<BelemetryHttpRequest> keep)
    {
        Directory.CreateDirectory(_directory);

        var path = PathOf(request);
        var temp = path + ".tmp";
        using (var stream = File.Create(temp))
        {
            // Never headers on disk, as in SaveAsync.
            JsonSerializer.Serialize(stream, request.WithoutHeaders(), BelemetryJsonContext.Default.BelemetryHttpRequest);
        }
        File.Move(temp, path, overwrite: true);

        try
        {
            var kept = new HashSet<string>(keep.Select(FileName), StringComparer.Ordinal) { FileName(request) };
            return EvictOverLimit(kept);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A save that holds the lock may be evicting too; whatever is still over the limit goes on the next save.
            return true;
        }
    }

    /// <summary>Deletes what <see cref="SaveNow"/> saved of <paramref name="request"/>, synchronously.</summary>
    public void DeleteNow(BelemetryHttpRequest request) => TryDelete(PathOf(request));

    /// <summary>The path of <paramref name="request"/>'s file in the store.</summary>
    private string PathOf(BelemetryHttpRequest request) => Path.Combine(_directory, FileName(request));

    /// <summary>The name of <paramref name="request"/>'s file: time-prefixed, so a plain name sorts the oldest first, for both loading and eviction.</summary>
    private static string FileName(BelemetryHttpRequest request) => $"{request.Time:D15}-{request.Id}{Extension}";

    public async ValueTask<IReadOnlyList<BelemetryHttpRequest>> LoadAsync(int max, CancellationToken cancellationToken)
    {
        var result = new List<BelemetryHttpRequest>();
        if (Directory.Exists(_directory) is false) return result;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var oldest = DateTimeOffset.UtcNow - _maxAge;
            foreach (var file in Directory.EnumerateFiles(_directory, "*" + Extension).Order(StringComparer.Ordinal))
            {
                if (result.Count >= max) break;

                try
                {
                    await using var stream = File.OpenRead(file);
                    var request = await JsonSerializer.DeserializeAsync(stream, BelemetryJsonContext.Default.BelemetryHttpRequest, cancellationToken);
                    stream.Close();

                    if (request is null || DateTimeOffset.FromUnixTimeMilliseconds(request.Time) < oldest)
                    {
                        File.Delete(file);
                        continue;
                    }

                    // A file an older version wrote may still hold headers; the exporter's current ones apply instead.
                    request.Headers = null;
                    result.Add(request);
                }
                catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
                {
                    TryDelete(file);
                }
            }
        }
        finally
        {
            _lock.Release();
        }

        return result;
    }

    public async ValueTask DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (Directory.Exists(_directory) is false) return;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            foreach (var file in Directory.EnumerateFiles(_directory, $"*-{id}{Extension}")) TryDelete(file);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask ClearAsync()
    {
        if (Directory.Exists(_directory) is false) return;

        await _lock.WaitAsync();
        try
        {
            foreach (var file in Directory.EnumerateFiles(_directory, "*" + Extension)) TryDelete(file);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Evicts the oldest files until the store is within its size limit, but none named in <paramref name="keep"/>;
    /// returns whether it is.
    /// </summary>
    private bool EvictOverLimit(IReadOnlySet<string>? keep = null)
    {
        var files = new DirectoryInfo(_directory).GetFiles("*" + Extension).OrderBy(f => f.Name, StringComparer.Ordinal).ToList();
        var total = files.Sum(f => f.Length);

        foreach (var file in files)
        {
            if (total <= _maxBytes) break;
            if (keep?.Contains(file.Name) is true) continue;
            total -= file.Length;
            TryDelete(file.FullName);
        }

        return total <= _maxBytes;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Another process may hold it; it is retried on the next pass.
        }
    }
}
