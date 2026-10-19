using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Build;

/// <summary>
/// Brings a Blazor service worker assets manifest - the <c>service-worker-assets.js</c> a PWA precaches from - back in
/// line with the files a publish serves once the trimming of the packages' scripts and stylesheets has changed them.
/// </summary>
/// <remarks>
/// <para>
/// The manifest is written by the project that owns the service worker, during that project's publish. In a standalone
/// WebAssembly app that is the project the trimming runs in, and the SDK writes the manifest after the trimming, from the
/// trimmed files. In a Blazor Web App it is the WebAssembly client: the client writes the manifest from the packages' full
/// bundles and every vendored file (it leaves the trimming to the server hosting it), and the server then trims against its
/// own assemblies and the client's together, and serves that. Left alone, the manifest lists each trimmed bundle with a hash
/// no served file has, and lists the vendored files and lazy-script modules the server no longer publishes. A service
/// worker that caches each asset under its URL and hash, and carries an entry over to the next version while the hash stays
/// the same, keeps an installed app on an old bundle; one that requests every asset with its integrity attribute fails to
/// install at all, and so does one that requests a file the server answers with a 404.
/// </para>
/// <para>
/// Read with regular expressions rather than <see cref="Json"/>, which would write the whole file out again in a shape of
/// its own: the SDK writes one flat object per asset, and a rewrite that touches only the entries that change leaves
/// everything else - the script around the object, the line endings, a statement appended after it - as it was. Anything
/// else is refused rather than guessed at. Public and free of MSBuild types, like <see cref="AssetComposer"/>, so the tests
/// run this exact code.
/// </para>
/// </remarks>
public static class ServiceWorkerManifest
{
    /// <summary>
    /// What the SDK writes into the service worker script ahead of the manifest's version, so that a new manifest also
    /// makes a byte-different service worker - the change a browser installs an update over.
    /// </summary>
    public const string VersionMarker = "Manifest version: ";

    private static readonly Regex AssetEntry = new(@",?\s*\{[^{}]*\}", RegexOptions.CultureInvariant);
    private static readonly Regex UrlProperty = new(@"""url""\s*:\s*""(?<value>(?:[^""\\]|\\.)*)""", RegexOptions.CultureInvariant);
    private static readonly Regex HashProperty = new(@"""hash""\s*:\s*""(?<value>(?:[^""\\]|\\.)*)""", RegexOptions.CultureInvariant);
    private static readonly Regex VersionProperty = new(@"""version""\s*:\s*""(?<value>(?:[^""\\]|\\.)*)""", RegexOptions.CultureInvariant);
    // Walked one flat object at a time up to the bracket that closes them, not to the last ']' in the file: anything a
    // build or the app appends after the object would otherwise be read as entries and lost in the rewrite. An entry that
    // is not one flat object fails the match, so the manifest is refused, not mangled.
    private static readonly Regex AssetsArray = new(@"""assets""\s*:\s*\[(?<entries>\s*(?:\{[^{}]*\}(?:\s*,\s*\{[^{}]*\})*)?\s*)\]", RegexOptions.CultureInvariant);
    private static readonly Regex PathToken = new(@"#\[(?<prefix>[^\]{]*)\{(?<name>[^}=]+)(?:=(?<value>[^}]*))?\}\](?<qualifier>[?!])?", RegexOptions.CultureInvariant);

    /// <summary>
    /// Drops the entries of the files a publish no longer serves, gives the entries of the files it replaced the hashes of
    /// the replacements, and versions the result afresh. Null when <paramref name="manifest"/> is not a manifest this can
    /// read.
    /// </summary>
    /// <param name="manifest">The text of <c>service-worker-assets.js</c>.</param>
    /// <param name="removedUrls">The URLs of the files no longer served, as <see cref="AssetUrl"/> spells them.</param>
    /// <param name="hashOf">
    /// The <c>sha256-</c> hash of the file now served at a URL (as <see cref="AssetUrl"/> spells it), or null where it is
    /// still the file the manifest was written from. Asked only about the URLs the manifest lists and keeps, so a caller
    /// hashing files on demand hashes none the manifest does not need.
    /// </param>
    /// <param name="basePath">
    /// The base path of the project that wrote the manifest. The SDK writes every URL under it - a package's files too,
    /// <c>app/_content/Bit.BlazorUI/...</c> for a client whose <c>StaticWebAssetBasePath</c> is <c>app</c> - so an entry is
    /// matched with it taken off, and one outside it names no file of the publish's.
    /// </param>
    public static ServiceWorkerManifestUpdate? Update(string manifest, IEnumerable<string> removedUrls, Func<string, string?> hashOf, string basePath = "")
    {
        var assets = AssetsArray.Match(manifest);
        var versionMatch = VersionProperty.Match(manifest);
        if (assets.Success is false || versionMatch.Success is false) return null;

        var previousVersion = versionMatch.Groups["value"].Value;
        var removed = new HashSet<string>(removedUrls.Select(Normalize), StringComparer.OrdinalIgnoreCase);
        var root = Normalize(basePath).Trim('/');

        var dropped = 0;
        var rehashed = 0;
        var kept = new List<(string Url, string Hash, string Text)>();
        foreach (Match entry in AssetEntry.Matches(assets.Groups["entries"].Value))
        {
            var body = entry.Value.TrimStart(',', ' ', '\t', '\r', '\n');
            var url = Normalize(Regex.Unescape(UrlProperty.Match(body).Groups["value"].Value));
            var hash = Regex.Unescape(HashProperty.Match(body).Groups["value"].Value);
            var served = Served(url, root);

            if (served is not null && removed.Contains(served))
            {
                dropped++;
                continue;
            }

            if (served is not null && hashOf(served) is { } replacement && string.Equals(hash, replacement, StringComparison.Ordinal) is false)
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
    /// The service worker script carrying <paramref name="version"/> where it carried <paramref name="previousVersion"/>,
    /// or null when it does not carry that version at all.
    /// </summary>
    public static string? UpdateVersion(string serviceWorker, string previousVersion, string version)
    {
        var marker = VersionMarker + previousVersion;
        return serviceWorker.Contains(marker) ? serviceWorker.Replace(marker, VersionMarker + version) : null;
    }

    /// <summary>
    /// The URL a static web asset is served at, as a manifest spells it. An optional token (<c>#[.{fingerprint}]?</c>)
    /// names a route that carries it, not the file, so it is dropped. A required (<c>#[.{fingerprint}]</c>) or preferred
    /// (<c>#[.{fingerprint}]!</c>) one is part of the file's name - <c>_framework/dotnet#[.{fingerprint}]!.js</c> is
    /// published as <c>dotnet.&lt;fingerprint&gt;.js</c> - so it is filled in: from the value the token carries
    /// (<c>{fingerprint=...}</c>), or else from the asset's metadata of that name.
    /// </summary>
    /// <param name="basePath">The asset's <c>BasePath</c>.</param>
    /// <param name="relativePath">The asset's <c>RelativePath</c>, tokens and all.</param>
    /// <param name="metadata">The asset's metadata by name, such as <c>Fingerprint</c>.</param>
    public static string AssetUrl(string basePath, string relativePath, Func<string, string> metadata)
    {
        var path = PathToken.Replace(relativePath.Replace('\\', '/'), token => token.Groups["qualifier"].Value == "?"
            ? string.Empty
            : token.Groups["prefix"].Value + (token.Groups["value"].Success ? token.Groups["value"].Value : metadata(MetadataName(token.Groups["name"].Value))));
        var root = basePath.Replace('\\', '/').Trim('/');
        path = path.TrimStart('/');
        return root.Length == 0 ? path : root + "/" + path;
    }

    /// <summary>A path token's name as the metadata it reads: <c>fingerprint</c> is <c>Fingerprint</c>.</summary>
    private static string MetadataName(string token) => token.Substring(0, 1).ToUpperInvariant() + token.Substring(1);

    /// <summary>A URL as the manifest spells it: forward slashes, no leading slash.</summary>
    private static string Normalize(string url) => url.Replace('\\', '/').TrimStart('/');

    /// <summary>A manifest's URL without the base path it was written under, or null when it is not under it.</summary>
    private static string? Served(string url, string root) =>
        root.Length == 0 ? url
        : url.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase) ? url.Substring(root.Length + 1)
        : null;

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
