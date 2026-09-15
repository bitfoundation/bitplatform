using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bit.Bswup.Tests.Hosting.Infrastructure;

/// <summary>A service-worker-assets.js, and what a worker with Bswup's default include/exclude lists precaches from it.</summary>
public sealed class PrecacheManifest
{
    // bit-bswup.sw.ts DEFAULT_ASSETS_INCLUDE / DEFAULT_ASSETS_EXCLUDE.
    private static readonly Regex[] _defaultIncludes =
    [
        new(@"\.dll$"), new(@"\.wasm(\.br|\.gz)?$"), new(@"\.pdb(\.br|\.gz)?$"), new(@"\.html(\.br|\.gz)?$"), new(@"\.js$"), new(@"\.json$"),
        new(@"\.css$"), new(@"\.woff$"), new(@"\.png$"), new(@"\.jpe?g$"), new(@"\.gif$"), new(@"\.ico$"), new(@"\.blat$"), new(@"\.dat$"),
        new(@"\.svg$"), new(@"\.woff2$"), new(@"\.ttf$"), new(@"\.webp$"),
    ];

    private static readonly Regex[] _defaultExcludes =
    [
        new(@"^_content/Bit\.Bswup/bit-bswup\.sw(-cleanup)?(\.min)?\.js$"), new(@"^service-worker\.js$"),
    ];

    public string Version { get; private init; } = string.Empty;

    public IReadOnlyList<(string Url, string? Hash)> Assets { get; private init; } = [];

    public static async Task<PrecacheManifest> LoadAsync(HttpClient client, string appPath)
    {
        using var response = await client.GetAsync(appPath + "service-worker-assets.js");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{appPath}service-worker-assets.js was not served.");

        // self.assetsManifest = { ... };
        var script = await response.Content.ReadAsStringAsync();
        StringAssert.StartsWith(script.TrimStart('﻿').Trim(), "self.assetsManifest");

        using var json = JsonDocument.Parse(script[script.IndexOf('{')..(script.LastIndexOf('}') + 1)]);
        return new()
        {
            Version = json.RootElement.GetProperty("version").GetString()!,
            Assets = [.. json.RootElement.GetProperty("assets").EnumerateArray()
                .Select(asset => (asset.GetProperty("url").GetString()!, asset.TryGetProperty("hash", out var hash) ? hash.GetString() : null))],
        };
    }

    /// <summary>The assets a worker with the default include/exclude lists (minus <paramref name="excluded"/>) downloads on install.</summary>
    public IEnumerable<(string Url, string? Hash)> Precached(params string[] excluded) => Assets
        .Where(asset => _defaultIncludes.Any(pattern => pattern.IsMatch(asset.Url)))
        .Where(asset => _defaultExcludes.Any(pattern => pattern.IsMatch(asset.Url)) is false)
        .Where(asset => excluded.Any(pattern => Regex.IsMatch(asset.Url, pattern)) is false);

    /// <summary>
    /// Downloads every precached asset the way the worker does (its URL relative to <paramref name="appPath"/>) and
    /// returns what is wrong: an asset that is not served, or served with bytes its SRI hash does not describe - what
    /// fails a lax install silently, and a strict or integrity-checked one outright.
    /// </summary>
    public async Task<IReadOnlyList<string>> VerifyServedAsync(HttpClient client, string appPath, params string[] excluded)
    {
        var problems = new List<string>();

        foreach (var (url, hash) in Precached(excluded))
        {
            using var response = await client.GetAsync(appPath + url);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                problems.Add($"{url}: HTTP {(int)response.StatusCode}");
                continue;
            }

            if (hash is null || hash.StartsWith("sha256-", StringComparison.Ordinal) is false) continue;

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var actual = "sha256-" + Convert.ToBase64String(SHA256.HashData(bytes));
            if (actual != hash) problems.Add($"{url}: served {actual}, manifest says {hash}");
        }

        return problems;
    }
}
