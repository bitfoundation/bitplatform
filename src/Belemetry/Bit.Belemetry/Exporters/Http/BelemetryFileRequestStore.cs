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

            // Time-prefixed names make a plain name sort the oldest first, for both loading and eviction.
            var path = Path.Combine(_directory, $"{request.Time:D15}-{request.Id}{Extension}");
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

    private void EvictOverLimit()
    {
        var files = new DirectoryInfo(_directory).GetFiles("*" + Extension).OrderBy(f => f.Name, StringComparer.Ordinal).ToList();
        var total = files.Sum(f => f.Length);

        foreach (var file in files)
        {
            if (total <= _maxBytes) break;
            total -= file.Length;
            TryDelete(file.FullName);
        }
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
