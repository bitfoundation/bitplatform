using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Butil.Build;

/// <summary>
/// Brings a Blazor service worker assets manifest - the <c>service-worker-assets.js</c> a PWA precaches from -
/// back in line with the files a publish serves once Bit.Butil's script trimming has changed them.
/// </summary>
/// <remarks>
/// The manifest is written by the project that owns the service worker, during that project's publish. In a
/// standalone WebAssembly app that is the project the trimming runs in, and the SDK writes the manifest after the
/// trimming, from the trimmed files. In a Blazor Web App it is the WebAssembly client: the client writes the
/// manifest from its own copy of bit-butil.js - trimmed against the client's assemblies, or not trimmed at all -
/// and the server project then trims a second copy against the server's assemblies and the client's together,
/// and serves that one. Left alone, the manifest lists the bundle with a hash no served file has, and in
/// lazy-scripts mode lists module files the server no longer publishes. A service worker that caches each asset
/// under its URL and hash, and carries an entry over to the next version while the hash stays the same, keeps
/// an installed app on an old bundle; one that requests every asset with its integrity attribute fails to
/// install at all.
/// <br/>
/// Read with regular expressions rather than a JSON parser because the task ships with no dependency of its own
/// (see Bit.Butil.Build.csproj), and the SDK writes one flat object per asset. Anything else is refused rather
/// than guessed at. Public and free of MSBuild types, like <see cref="ButilScriptBundler"/>, so the test
/// harness runs this exact code.
/// </remarks>
public static class ButilServiceWorkerManifest
{
    /// <summary>
    /// What the SDK writes into the service worker script ahead of the manifest's version, so that a new manifest
    /// also makes a byte-different service worker - the change a browser installs an update over.
    /// </summary>
    public const string VersionMarker = "Manifest version: ";

    private static readonly Regex AssetEntry = new(@",?\s*\{[^{}]*\}", RegexOptions.CultureInvariant);
    private static readonly Regex UrlProperty = new(@"""url""\s*:\s*""(?<value>(?:[^""\\]|\\.)*)""", RegexOptions.CultureInvariant);
    private static readonly Regex HashProperty = new(@"""hash""\s*:\s*""(?<value>(?:[^""\\]|\\.)*)""", RegexOptions.CultureInvariant);
    private static readonly Regex VersionProperty = new(@"""version""\s*:\s*""(?<value>(?:[^""\\]|\\.)*)""", RegexOptions.CultureInvariant);
    private static readonly Regex AssetsArray = new(@"""assets""\s*:\s*\[(?<entries>.*)\]", RegexOptions.CultureInvariant | RegexOptions.Singleline);

    /// <summary>
    /// Drops the entries of the files a publish no longer serves, gives the entries of the files it replaced the
    /// hashes of the replacements, and versions the result afresh. Null when <paramref name="manifest"/> is not a
    /// manifest this can read.
    /// </summary>
    /// <param name="manifest">The text of <c>service-worker-assets.js</c>.</param>
    /// <param name="removedUrls">The URLs of the files no longer served, relative to the app's base path.</param>
    /// <param name="hashes">The <c>sha256-</c> hash of the file now served at each URL that changed.</param>
    public static ButilServiceWorkerManifestUpdate? Update(string manifest, IEnumerable<string> removedUrls, IReadOnlyDictionary<string, string> hashes)
    {
        var assets = AssetsArray.Match(manifest);
        var versionMatch = VersionProperty.Match(manifest);
        if (assets.Success is false || versionMatch.Success is false) return null;

        var previousVersion = versionMatch.Groups["value"].Value;
        var removed = new HashSet<string>(removedUrls.Select(Normalize), StringComparer.OrdinalIgnoreCase);
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in hashes) replacements[Normalize(pair.Key)] = pair.Value;

        var dropped = 0;
        var rehashed = 0;
        var kept = new List<(string Url, string Hash, string Text)>();
        foreach (Match entry in AssetEntry.Matches(assets.Groups["entries"].Value))
        {
            var body = entry.Value.TrimStart(',', ' ', '\t', '\r', '\n');
            var url = Normalize(Regex.Unescape(UrlProperty.Match(body).Groups["value"].Value));
            var hash = Regex.Unescape(HashProperty.Match(body).Groups["value"].Value);

            if (removed.Contains(url))
            {
                dropped++;
                continue;
            }

            if (replacements.TryGetValue(url, out var replacement) && string.Equals(hash, replacement, StringComparison.Ordinal) is false)
            {
                body = HashProperty.Replace(body, $"\"hash\": \"{replacement}\"", 1);
                hash = replacement;
                rehashed++;
            }

            kept.Add((url, hash, body));
        }

        if (dropped == 0 && rehashed == 0) return new(manifest, previousVersion, previousVersion, 0, 0);

        var version = VersionOf(kept.Select(e => e.Url + ":" + e.Hash));
        var newline = manifest.Contains("\r\n") ? "\r\n" : "\n";
        var entries = kept.Count == 0
            ? newline + "  "
            : newline + string.Join("," + newline, kept.Select(e => "    " + e.Text.Trim())) + newline + "  ";
        var group = assets.Groups["entries"];
        var rewritten = manifest.Substring(0, group.Index) + entries + manifest.Substring(group.Index + group.Length);
        rewritten = VersionProperty.Replace(rewritten, $"\"version\": \"{version}\"", 1);

        return new(rewritten, previousVersion, version, dropped, rehashed);
    }

    /// <summary>
    /// The service worker script carrying <paramref name="version"/> where it carried
    /// <paramref name="previousVersion"/>, or null when it does not carry that version at all.
    /// </summary>
    public static string? UpdateVersion(string serviceWorker, string previousVersion, string version)
    {
        var marker = VersionMarker + previousVersion;
        return serviceWorker.Contains(marker) ? serviceWorker.Replace(marker, VersionMarker + version) : null;
    }

    /// <summary>A URL as the manifest spells it: forward slashes, no leading slash.</summary>
    private static string Normalize(string url) => url.Replace('\\', '/').TrimStart('/');

    /// <summary>
    /// The same shape as the SDK's own version - eight characters of a hash over what is listed - so that it changes
    /// whenever what the service worker would precache changes, and only then.
    /// </summary>
    private static string VersionOf(IEnumerable<string> entries)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", entries)))).Substring(0, 8);
    }
}
