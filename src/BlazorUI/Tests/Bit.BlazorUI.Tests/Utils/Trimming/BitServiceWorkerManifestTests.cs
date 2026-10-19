using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bit.BlazorUI.Build;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Trimming;

/// <summary>
/// The rewriting of a Blazor service worker assets manifest after the trimming changed what a publish serves
/// (<see cref="ServiceWorkerManifest"/>), run directly on manifests written here in the shape the SDK writes them.
/// </summary>
/// <remarks>
/// HostedAppTrimmingTests in Bit.BlazorUI.Tests.Trimming checks the same rewriting through real publishes, which is where it
/// is shown to run at all. What a publish does not reach is the shapes it never happens to produce: the first, the last or
/// every entry dropped, a URL with escaped slashes or a leading one, LF line endings where the SDK wrote CRLF, a file that is
/// not a manifest. A mistake in any of those is a manifest that no longer parses - and a service worker whose manifest does
/// not parse installs nothing at all - so every result is read back with a real JSON parser rather than compared as text.
/// </remarks>
[TestClass]
public sealed class BitServiceWorkerManifestTests
{
    private const string Bundle = "_content/Bit.BlazorUI/scripts/bit.blazorui.js";
    private const string Styles = "_content/Bit.BlazorUI/styles/bit.blazorui.css";
    private const string Version = "Ab1+/xyZ";

    // Real-looking digests, with the + and / a base64 hash carries, so nothing here passes only because a hash happened to
    // need no escaping.
    private const string FullHash = "sha256-cj3M1aov0CmIHH/5sPV5gfiv1m56NVMrxGHuwF5Q+0Q=";
    private const string TrimmedHash = "sha256-aAGw1cHoOsnUbBukwS3/nD0YnCXiJi6u+jWGrMm61uY=";
    private const string OtherHash = "sha256-Zj3xCvg5u3efvb3PXu4x+4vIGRGb1rTgPLrCGLF5lYA=";
    private const string FrameworkHash = "sha256-PsbGE2Sn1zB5VCvHWc4XR9y2unVu7XuGWDOd6awq6pk=";

    /// <summary>
    /// The bundle mode case: the client listed the full bundle, the server serves a trimmed one, and the entry has to take
    /// the served file's hash while every other entry stays exactly as it was.
    /// </summary>
    [TestMethod]
    public void AReplacedBundlesEntryShouldTakeTheReplacementsHashAndNothingElseChange()
    {
        var original = Manifest("\r\n", ("_framework/dotnet.js", FrameworkHash), (Bundle, FullHash), ("index.html", OtherHash));
        var update = ServiceWorkerManifest.Update(original, [], new Dictionary<string, string> { [Bundle] = TrimmedHash });

        var result = Read(update);
        Assert.IsTrue(update!.Changed && update.Rehashed == 1 && update.Dropped == 0, $"It reported {update.Rehashed} replaced and {update.Dropped} dropped.");
        Expect(result.Assets, ("_framework/dotnet.js", FrameworkHash), (Bundle, TrimmedHash), ("index.html", OtherHash));

        Assert.AreEqual(Version, update.PreviousVersion);
        Assert.AreNotEqual(Version, update.Version);
        Assert.AreEqual(update.Version, result.Version, "The version written is not the version reported.");
        Assert.IsTrue(update.Text.StartsWith("self.assetsManifest = {", StringComparison.Ordinal) && update.Text.EndsWith("};\r\n", StringComparison.Ordinal),
                      "The script around the object changed.");
    }

    /// <summary>The vendored files and lazy-script modules the server no longer publishes leave the list, and only those.</summary>
    [TestMethod]
    public void TheFilesAPublishDroppedShouldLeaveTheListAndOnlyThose()
    {
        var original = Manifest("\r\n", (Module("Callouts"), FrameworkHash), (Leaflet("leaflet.js"), FullHash), (Module("BitSplitter"), OtherHash),
                                (Leaflet("leaflet.css"), TrimmedHash), ("index.html", OtherHash));
        var update = ServiceWorkerManifest.Update(original, [Leaflet("leaflet.js"), Leaflet("leaflet.css"), Module("BitSplitter"), Module("BitChart")], new Dictionary<string, string>());

        var result = Read(update);
        Assert.IsTrue(update!.Dropped == 3 && update.Rehashed == 0, $"It reported {update.Dropped} dropped and {update.Rehashed} replaced, where it listed three of the four.");
        Expect(result.Assets, (Module("Callouts"), FrameworkHash), ("index.html", OtherHash));
    }

    /// <summary>The separators are where a rewritten array breaks: the comma after the last entry left, the brackets once nothing is.</summary>
    [TestMethod]
    [DataRow(new[] { 0 }, DisplayName = "the first entry")]
    [DataRow(new[] { 2 }, DisplayName = "the last entry")]
    [DataRow(new[] { 0, 1, 2 }, DisplayName = "every entry")]
    public void DroppingEntriesShouldLeaveAManifestThatParses(int[] dropped)
    {
        (string Url, string Hash)[] assets = [(Module("Callouts"), FrameworkHash), (Module("BitNav"), FullHash), (Module("BitSplitter"), OtherHash)];
        var removed = dropped.Select(i => assets[i].Url).ToArray();

        var result = Read(ServiceWorkerManifest.Update(Manifest("\r\n", assets), removed, new Dictionary<string, string>()));
        Expect(result.Assets, [.. assets.Where(a => removed.Contains(a.Url) is false)]);
    }

    /// <summary>
    /// Nothing to change - a server trimming to what its client already listed - has to leave the file byte for byte as the
    /// SDK wrote it, under the SDK's own version: a new version would have every installed app download its precache again.
    /// </summary>
    [TestMethod]
    public void AManifestAlreadyListingWhatIsServedShouldBeLeftAsItIs()
    {
        var original = Manifest("\r\n", (Bundle, TrimmedHash), ("index.html", OtherHash));
        var update = ServiceWorkerManifest.Update(original, [Module("BitSplitter")], new Dictionary<string, string> { [Bundle] = TrimmedHash, ["_content/Other/other.js"] = OtherHash });

        Assert.IsNotNull(update, "It was refused.");
        Assert.IsFalse(update.Changed);
        Assert.AreEqual(original, update.Text);
        Assert.AreEqual(Version, update.Version);
    }

    /// <summary>
    /// A URL is matched as a URL, however the JSON spells it: an escaped slash is still a slash, a leading slash names the
    /// same file - and a URL the bundle's is a prefix of is a different file.
    /// </summary>
    [TestMethod]
    public void URLsShouldBeMatchedHoweverTheManifestSpellsThemAndOnlyWhole()
    {
        var original = Manifest("\r\n", (Bundle.Replace("/", "\\/"), FullHash), ("/" + Module("BitSplitter"), OtherHash), (Bundle + ".map", FullHash));
        var update = ServiceWorkerManifest.Update(original, [Module("BitSplitter")], new Dictionary<string, string> { ["/" + Bundle] = TrimmedHash });

        Expect(Read(update).Assets, (Bundle, TrimmedHash), (Bundle + ".map", FullHash));
    }

    /// <summary>
    /// The list ends at the bracket that closes it, not at the last bracket in the file: a statement appended after the
    /// object - by a build step or by the app - carries brackets of its own, and reading up to those would swallow the end of
    /// the object and the statement into the entries and drop them from the rewrite.
    /// </summary>
    [TestMethod]
    public void AStatementAfterTheManifestsObjectShouldBeKeptWhole()
    {
        const string Trailer = "self.assetsInclude = [/\\.js$/, /\\.wasm$/];\r\n";

        var original = Manifest("\r\n", (Module("BitSplitter"), OtherHash), (Bundle, FullHash), ("_content/Other/a]b.js", FrameworkHash)) + Trailer;
        var update = ServiceWorkerManifest.Update(original, [Module("BitSplitter")], new Dictionary<string, string> { [Bundle] = TrimmedHash });

        Expect(Read(update).Assets, (Bundle, TrimmedHash), ("_content/Other/a]b.js", FrameworkHash));
        Assert.IsTrue(update!.Text.EndsWith("  ]\r\n};\r\n" + Trailer, StringComparison.Ordinal), $"The file ends: {update.Text[Math.Max(0, update.Text.Length - 80)..]}");
    }

    /// <summary>
    /// The SDK writes the platform's line endings; a rewrite that mixed them would not break a parser, but it would make a
    /// file that diffs as wholly changed, and the separators it adds are where that would happen.
    /// </summary>
    [TestMethod]
    [DataRow("\r\n", DisplayName = "CRLF")]
    [DataRow("\n", DisplayName = "LF")]
    public void ARewrittenManifestShouldKeepItsLineEndings(string newline)
    {
        var original = Manifest(newline, (Module("Callouts"), FrameworkHash), (Module("BitSplitter"), OtherHash), (Bundle, FullHash));
        var update = ServiceWorkerManifest.Update(original, [Module("BitSplitter")], new Dictionary<string, string> { [Bundle] = TrimmedHash });

        Assert.IsNotNull(update, "It was refused.");
        var mixed = newline == "\n" ? update.Text.Contains('\r') : update.Text.Replace("\r\n", "").Contains('\n');
        Assert.IsFalse(mixed, "It mixes line endings.");
    }

    /// <summary>
    /// The version is what a browser installs an update over, so it has to change with what is listed and with nothing else:
    /// the same list must not churn installed apps, and a different one must not be missed.
    /// </summary>
    [TestMethod]
    public void TheVersionShouldFollowWhatIsListedAndNothingElse()
    {
        static string? VersionFor(string newline, string hash) => ServiceWorkerManifest.Update(
            Manifest(newline, ("index.html", OtherHash), (Bundle, FullHash)), [], new Dictionary<string, string> { [Bundle] = hash })?.Version;

        var version = VersionFor("\r\n", TrimmedHash);

        Assert.AreEqual(8, version?.Length, $"The version '{version}' does not have the SDK's eight-character shape.");
        Assert.AreEqual(version, VersionFor("\r\n", TrimmedHash), "The same list came out under two versions.");
        Assert.AreEqual(version, VersionFor("\n", TrimmedHash), "The same list, written with other line endings, came out under another version.");
        Assert.AreNotEqual(version, VersionFor("\r\n", OtherHash), "Two different lists came out under one version.");
    }

    /// <summary>
    /// What is not a manifest in the SDK's shape is refused - the task then warns (BITBLAZORUI004) and publishes it as it is -
    /// rather than rewritten on a guess.
    /// </summary>
    [TestMethod]
    [DataRow("", DisplayName = "an empty file")]
    [DataRow("self.addEventListener('fetch', () => { });", DisplayName = "a script that is not a manifest")]
    [DataRow("self.assetsManifest = {\r\n  \"version\": \"Ab1+/xyZ\"\r\n};", DisplayName = "a manifest with no assets")]
    [DataRow("self.assetsManifest = {\r\n  \"assets\": [\r\n  ]\r\n};", DisplayName = "a manifest with no version")]
    [DataRow("self.assetsManifest = {\r\n  \"version\": \"Ab1+/xyZ\",\r\n  \"assets\": [\r\n    { \"hash\": \"" + FullHash + "\", \"url\": \"" + Bundle + "\", \"extra\": { } }\r\n  ]\r\n};",
             DisplayName = "a manifest whose asset is not one flat object")]
    public void WhatIsNotAManifestShouldBeRefusedRatherThanRewritten(string text)
    {
        Assert.IsNull(ServiceWorkerManifest.Update(text, [Bundle], new Dictionary<string, string> { [Bundle] = TrimmedHash }));
    }

    /// <summary>
    /// The script names the manifest's version, and a byte-different script is what makes a browser install an update - so
    /// the version has to move in the script as well, and only where the SDK put it.
    /// </summary>
    [TestMethod]
    public void TheServiceWorkerScriptShouldTakeTheNewVersionOnlyWhereTheSdkPutTheOld()
    {
        const string Body = "self.importScripts('./service-worker-assets.js');\r\nself.addEventListener('fetch', () => { });\r\n";
        var script = $"/* {ServiceWorkerManifest.VersionMarker}{Version} */\r\n{Body}";

        Assert.AreEqual($"/* {ServiceWorkerManifest.VersionMarker}Cd2-new1 */\r\n{Body}", ServiceWorkerManifest.UpdateVersion(script, Version, "Cd2-new1"));
        Assert.IsNull(ServiceWorkerManifest.UpdateVersion(Body, Version, "Cd2-new1"), "A script carrying no version was changed rather than reported.");
        Assert.IsNull(ServiceWorkerManifest.UpdateVersion(script, "Zz9-othr", "Cd2-new1"), "A script carrying another manifest's version was changed rather than reported.");
    }

    /// <summary>
    /// The URLs the trimming's assets are matched on are computed from the SDK's path patterns, so each kind of fingerprint
    /// token has to come out the way the manifest spells the file: an optional one dropped, a required or preferred one
    /// filled in - from the token's own value when it carries one - and nothing of the token's syntax left behind. A URL that
    /// keeps a stray character matches no entry, and the entry stays stale in silence.
    /// </summary>
    [TestMethod]
    [DataRow("_content/Bit.BlazorUI", "scripts/bit.blazorui#[.{fingerprint}]?.js", Bundle, DisplayName = "an optional fingerprint is dropped")]
    [DataRow("_content/Bit.BlazorUI", "scripts/bit.blazorui#[.{fingerprint}]!.js", "_content/Bit.BlazorUI/scripts/bit.blazorui.abc123.js", DisplayName = "a preferred fingerprint is filled in, without its '!'")]
    [DataRow("/", "_framework/dotnet#[.{fingerprint}].js", "_framework/dotnet.abc123.js", DisplayName = "a required fingerprint is filled in")]
    [DataRow("_content/Bit.BlazorUI", "scripts/bit.blazorui#[.{fingerprint=xyz789}]!.js", "_content/Bit.BlazorUI/scripts/bit.blazorui.xyz789.js", DisplayName = "a fingerprint the token carries wins over the metadata")]
    [DataRow("_content/Bit.BlazorUI", "styles/bit.blazorui#[.{fingerprint=xyz789}]?.css.gz", Styles + ".gz", DisplayName = "an optional fingerprint carrying a value is still dropped")]
    [DataRow("\\_content\\Bit.BlazorUI\\", "\\scripts\\modules\\Callouts.js", "_content/Bit.BlazorUI/scripts/modules/Callouts.js", DisplayName = "backslashes and the base path's slashes are normalized")]
    [DataRow("/", "index.html", "index.html", DisplayName = "an asset at the root has no base path in its URL")]
    public void AnAssetsUrlShouldBeTheOneTheManifestSpells(string basePath, string relativePath, string expected)
    {
        Assert.AreEqual(expected, ServiceWorkerManifest.AssetUrl(basePath, relativePath, name => name == "Fingerprint" ? "abc123" : ""));
    }

    private static string Module(string name) => $"_content/Bit.BlazorUI/scripts/modules/{name}.js";

    private static string Leaflet(string file) => $"_content/Bit.BlazorUI.Extras/leaflet/{file}";

    /// <summary>A manifest as the SDK writes one: two-space indentation, one flat object per asset.</summary>
    private static string Manifest(string newline, params (string Url, string Hash)[] assets) =>
        $"self.assetsManifest = {{{newline}" +
        $"  \"version\": \"{Version}\",{newline}" +
        $"  \"assets\": [{newline}" +
        string.Join($",{newline}", assets.Select(a => $"    {{{newline}      \"hash\": \"{a.Hash}\",{newline}      \"url\": \"{a.Url}\"{newline}    }}")) +
        $"{newline}  ]{newline}}};{newline}";

    /// <summary>The rewritten manifest read back with a real JSON parser, failing when it was refused or no longer parses.</summary>
    private static (string Version, List<(string Url, string Hash)> Assets) Read(ServiceWorkerManifestUpdate? update)
    {
        Assert.IsNotNull(update, "The manifest was refused.");

        var text = update.Text;
        using var json = JsonDocument.Parse(text[text.IndexOf('{')..(text.LastIndexOf('}') + 1)]);
        var assets = json.RootElement.GetProperty("assets").EnumerateArray()
                         .Select(asset => (asset.GetProperty("url").GetString()!, asset.GetProperty("hash").GetString()!))
                         .ToList();

        return (json.RootElement.GetProperty("version").GetString()!, assets);
    }

    private static void Expect(List<(string Url, string Hash)> actual, params (string Url, string Hash)[] expected)
    {
        Assert.IsTrue(actual.SequenceEqual(expected),
                      $"It lists [{string.Join(", ", actual.Select(a => $"{a.Url} {a.Hash}"))}] where [{string.Join(", ", expected.Select(a => $"{a.Url} {a.Hash}"))}] was expected.");
    }
}
