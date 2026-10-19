using System.Text.Json;
using Bit.Butil.Build;

namespace ButilTests.Manual;

/// <summary>
/// The rewriting of a Blazor service worker assets manifest after the script trimming changed what a publish
/// serves, run directly on manifests written here in the shape the SDK writes them.
/// </summary>
/// <remarks>
/// <see cref="ScriptPublishing"/> checks the same rewriting through real publishes, which is where it is shown
/// to run at all. What a publish does not reach is the shapes it never happens to produce: the first, the last
/// or every entry dropped, a URL with escaped slashes or a leading one, LF line endings where the SDK wrote CRLF,
/// a file that is not a manifest. A mistake in any of those is a manifest that no longer parses - and a service
/// worker whose manifest does not parse installs nothing at all - so every result is read back with a real JSON
/// parser rather than compared as text.
/// </remarks>
internal static class ServiceWorkerRewriting
{
    private const string Bundle = "_content/Bit.Butil/bit-butil.js";
    private const string Version = "Ab1+/xyZ";

    // Real-looking digests, with the + and / a base64 hash carries, so nothing here passes only because a
    // hash happened to need no escaping.
    private const string ClientHash = "sha256-cj3M1aov0CmIHH/5sPV5gfiv1m56NVMrxGHuwF5Q+0Q=";
    private const string ServerHash = "sha256-aAGw1cHoOsnUbBukwS3/nD0YnCXiJi6u+jWGrMm61uY=";
    private const string OtherHash = "sha256-Zj3xCvg5u3efvb3PXu4x+4vIGRGb1rTgPLrCGLF5lYA=";
    private const string FrameworkHash = "sha256-PsbGE2Sn1zB5VCvHWc4XR9y2unVu7XuGWDOd6awq6pk=";

    public static (int Passed, int Failed) Run(List<string> failures)
    {
        var checks = new ScriptBundling.Checks(failures, "service worker manifest");

        CheckRehash(checks);
        CheckDrop(checks);
        CheckDropEdges(checks);
        CheckUnchanged(checks);
        CheckUrlSpellings(checks);
        CheckLineEndings(checks);
        CheckVersion(checks);
        CheckRefused(checks);
        CheckServiceWorkerScript(checks);

        return (checks.Passed, checks.Failed);
    }

    /// <summary>
    /// The bundle mode case: the client listed its own bundle, the server serves another, and the entry has to
    /// take the served file's hash while every other entry stays exactly as it was.
    /// </summary>
    private static void CheckRehash(ScriptBundling.Checks checks)
    {
        const string What = "a replaced bundle's entry takes the replacement's hash and nothing else changes";

        var original = Manifest("\r\n", ("_framework/dotnet.js", FrameworkHash), (Bundle, ClientHash), ("index.html", OtherHash));
        var update = ButilServiceWorkerManifest.Update(original, [], new Dictionary<string, string> { [Bundle] = ServerHash });

        if (Read(checks, What, update?.Text) is not { } result) return;

        checks.That(update!.Changed && update.Rehashed == 1 && update.Dropped == 0, $"{What} (and counts it as one replaced hash)",
            $"it reported {update.Rehashed} replaced and {update.Dropped} dropped");
        Expect(checks, What, result.Assets, [("_framework/dotnet.js", FrameworkHash), (Bundle, ServerHash), ("index.html", OtherHash)]);
        checks.That(update.PreviousVersion == Version && result.Version == update.Version && update.Version != Version, $"{What} (under a new version)",
            $"previous '{update.PreviousVersion}', reported '{update.Version}', written '{result.Version}'");
        checks.That(update.Text.StartsWith("self.assetsManifest = {", StringComparison.Ordinal) && update.Text.EndsWith("};\r\n", StringComparison.Ordinal),
            $"{What} (and the script around the object is kept)", "the text outside the object changed");
    }

    /// <summary>The lazy-scripts case: the module files the server no longer publishes leave the list.</summary>
    private static void CheckDrop(ScriptBundling.Checks checks)
    {
        const string What = "the files a publish dropped leave the list, and only those";

        var original = Manifest("\r\n", (Module("butil"), FrameworkHash), (Module("clipboard"), ClientHash), (Module("battery"), OtherHash), (Module("cookie"), ServerHash), ("index.html", OtherHash));
        var update = ButilServiceWorkerManifest.Update(original, [Module("battery"), Module("cookie"), Module("crypto")], new Dictionary<string, string>());

        if (Read(checks, What, update?.Text) is not { } result) return;

        checks.That(update!.Dropped == 2 && update.Rehashed == 0, $"{What} (counting the two it listed, not the one it did not)",
            $"it reported {update.Dropped} dropped and {update.Rehashed} replaced");
        Expect(checks, What, result.Assets, [(Module("butil"), FrameworkHash), (Module("clipboard"), ClientHash), ("index.html", OtherHash)]);
    }

    /// <summary>
    /// The separators are where a rewritten array breaks: the comma after the last entry that is left, the
    /// brackets once nothing is.
    /// </summary>
    private static void CheckDropEdges(ScriptBundling.Checks checks)
    {
        (string Url, string Hash)[] assets = [(Module("butil"), FrameworkHash), (Module("clipboard"), ClientHash), (Module("cookie"), OtherHash)];
        var original = Manifest("\r\n", assets);

        foreach (var (what, removed) in new (string, string[])[]
        {
            ("dropping the first entry leaves a manifest that parses", [assets[0].Url]),
            ("dropping the last entry leaves a manifest that parses", [assets[2].Url]),
            ("dropping every entry leaves a manifest that parses, listing nothing", [.. assets.Select(a => a.Url)]),
        })
        {
            var update = ButilServiceWorkerManifest.Update(original, removed, new Dictionary<string, string>());
            if (Read(checks, what, update?.Text) is not { } result) continue;

            Expect(checks, what, result.Assets, [.. assets.Where(a => removed.Contains(a.Url) is false)]);
        }
    }

    /// <summary>
    /// Nothing to change is the common case - a server trimming to the same set as its client - and it has to
    /// leave the file byte for byte as the SDK wrote it, under the SDK's own version.
    /// </summary>
    private static void CheckUnchanged(ScriptBundling.Checks checks)
    {
        var original = Manifest("\r\n", (Bundle, ServerHash), ("index.html", OtherHash));

        var same = ButilServiceWorkerManifest.Update(original, [Module("cookie")], new Dictionary<string, string> { [Bundle] = ServerHash, ["_content/Other/other.js"] = OtherHash });
        checks.That(same is { Changed: false } && same.Text == original && same.Version == Version,
            "a manifest already listing what is served is left as it is, under its own version",
            same is null ? "it was refused" : $"changed: {same.Changed}, version '{same.Version}'");
    }

    /// <summary>
    /// A URL is matched as a URL, however the JSON spells it: an escaped slash is still a slash, a leading slash
    /// names the same file - and a URL the bundle's is a prefix of is a different file.
    /// </summary>
    private static void CheckUrlSpellings(ScriptBundling.Checks checks)
    {
        const string What = "URLs are matched however the manifest spells them, and only whole";

        var original = Manifest("\r\n",
            (Bundle.Replace("/", "\\/"), ClientHash),
            ("/" + Module("cookie"), OtherHash),
            (Bundle + ".map", ClientHash));
        var update = ButilServiceWorkerManifest.Update(original, [Module("cookie")], new Dictionary<string, string> { ["/" + Bundle] = ServerHash });

        if (Read(checks, What, update?.Text) is not { } result) return;

        Expect(checks, What, result.Assets, [(Bundle, ServerHash), (Bundle + ".map", ClientHash)]);
    }

    /// <summary>
    /// The SDK writes the platform's line endings; a rewrite that mixed them would not break a parser, but it
    /// would make a file that diffs as wholly changed, and the separators it adds are where that would happen.
    /// </summary>
    private static void CheckLineEndings(ScriptBundling.Checks checks)
    {
        foreach (var (name, newline) in new[] { ("CRLF", "\r\n"), ("LF", "\n") })
        {
            var original = Manifest(newline, (Module("butil"), FrameworkHash), (Module("cookie"), OtherHash), (Bundle, ClientHash));
            var update = ButilServiceWorkerManifest.Update(original, [Module("cookie")], new Dictionary<string, string> { [Bundle] = ServerHash });
            var text = update?.Text ?? string.Empty;

            var mixed = newline == "\n" ? text.Contains('\r') : text.Replace("\r\n", string.Empty).Contains('\n');
            checks.That(update is not null && mixed is false, $"a rewritten {name} manifest keeps {name} line endings", "it mixes line endings");
        }
    }

    /// <summary>
    /// The version is what a browser installs an update over, so it has to change with what is listed and with
    /// nothing else: the same list must not churn installed apps, and a different one must not be missed.
    /// </summary>
    private static void CheckVersion(ScriptBundling.Checks checks)
    {
        string? VersionFor(string newline, string hash) => ButilServiceWorkerManifest.Update(
            Manifest(newline, ("index.html", OtherHash), (Bundle, ClientHash)), [], new Dictionary<string, string> { [Bundle] = hash })?.Version;

        var version = VersionFor("\r\n", ServerHash);

        checks.That(version is { Length: 8 }, "a rewritten manifest's version has the SDK's eight-character shape", $"it is '{version}'");
        checks.That(version is not null && version == VersionFor("\r\n", ServerHash) && version == VersionFor("\n", ServerHash),
            "the version follows what is listed, not how the file is written", "the same list came out under two versions");
        checks.That(version is not null && version != VersionFor("\r\n", OtherHash),
            "a different hash makes a different version", "two different lists came out under one version");
    }

    /// <summary>
    /// What is not a manifest in the SDK's shape is refused - the task then warns and publishes it untouched -
    /// rather than rewritten on a guess.
    /// </summary>
    private static void CheckRefused(ScriptBundling.Checks checks)
    {
        foreach (var (what, text) in new[]
        {
            ("an empty file", string.Empty),
            ("a script that is not a manifest", "self.addEventListener('fetch', () => { });"),
            ("a manifest with no assets", "self.assetsManifest = {\r\n  \"version\": \"Ab1+/xyZ\"\r\n};"),
            ("a manifest with no version", "self.assetsManifest = {\r\n  \"assets\": [\r\n  ]\r\n};"),
        })
        {
            checks.That(ButilServiceWorkerManifest.Update(text, [Bundle], new Dictionary<string, string> { [Bundle] = ServerHash }) is null,
                $"{what} is refused rather than rewritten", "it was rewritten");
        }
    }

    /// <summary>
    /// The script names the manifest's version, and a byte-different script is what makes a browser install an
    /// update - so the version has to move in the script as well, and only where the SDK put it.
    /// </summary>
    private static void CheckServiceWorkerScript(ScriptBundling.Checks checks)
    {
        const string Body = "self.importScripts('./service-worker-assets.js');\r\nself.addEventListener('fetch', () => { });\r\n";
        var script = $"/* {ButilServiceWorkerManifest.VersionMarker}{Version} */\r\n{Body}";

        var updated = ButilServiceWorkerManifest.UpdateVersion(script, Version, "Cd2-new1");
        checks.That(updated == $"/* {ButilServiceWorkerManifest.VersionMarker}Cd2-new1 */\r\n{Body}",
            "the service worker script takes the new version in place of the old, and changes nowhere else", $"it reads: {updated}");

        checks.That(ButilServiceWorkerManifest.UpdateVersion(Body, Version, "Cd2-new1") is null,
            "a service worker script carrying no version is reported rather than changed", "it was changed");
        checks.That(ButilServiceWorkerManifest.UpdateVersion(script, "Zz9-othr", "Cd2-new1") is null,
            "a service worker script carrying another manifest's version is reported rather than changed", "it was changed");
    }

    private static string Module(string name) => $"_content/Bit.Butil/modules/{name}.js";

    /// <summary>A manifest as the SDK writes one: two-space indentation, one flat object per asset.</summary>
    private static string Manifest(string newline, params (string Url, string Hash)[] assets) =>
        $"self.assetsManifest = {{{newline}" +
        $"  \"version\": \"{Version}\",{newline}" +
        $"  \"assets\": [{newline}" +
        string.Join($",{newline}", assets.Select(a => $"    {{{newline}      \"hash\": \"{a.Hash}\",{newline}      \"url\": \"{a.Url}\"{newline}    }}")) +
        $"{newline}  ]{newline}}};{newline}";

    /// <summary>
    /// The rewritten manifest read back with a real JSON parser: null, reported as the failure of
    /// <paramref name="what"/>, when there was no manifest to read or it no longer parses.
    /// </summary>
    private static (string Version, List<(string Url, string Hash)> Assets)? Read(ScriptBundling.Checks checks, string what, string? manifest)
    {
        if (manifest is null)
        {
            checks.That(false, what, "the manifest was refused");
            return null;
        }

        try
        {
            using var json = JsonDocument.Parse(manifest[manifest.IndexOf('{')..(manifest.LastIndexOf('}') + 1)]);
            var assets = json.RootElement.GetProperty("assets").EnumerateArray()
                .Select(asset => (asset.GetProperty("url").GetString()!, asset.GetProperty("hash").GetString()!))
                .ToList();

            return (json.RootElement.GetProperty("version").GetString()!, assets);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or ArgumentOutOfRangeException)
        {
            checks.That(false, what, $"the rewritten manifest no longer parses ({exception.Message})");
            return null;
        }
    }

    private static void Expect(ScriptBundling.Checks checks, string what, List<(string Url, string Hash)> actual, (string Url, string Hash)[] expected)
    {
        checks.That(actual.SequenceEqual(expected), what,
            $"it lists [{string.Join(", ", actual.Select(a => $"{a.Url} {a.Hash}"))}] where [{string.Join(", ", expected.Select(a => $"{a.Url} {a.Hash}"))}] was expected");
    }
}
