using System.Diagnostics;

namespace Boilerplate.Tests.E2E.Features.AppUpdate;

/// <summary>
/// Self-update is the one path that only ever runs on a user's machine: <c>WindowsAppUpdateService</c> points a
/// Velopack <c>UpdateManager</c> at <c>WindowsUpdate.FilesUrl</c>, and Velopack's own installer, <c>Update.exe</c> and
/// package folder do the rest. <c>Boilerplate.Tests</c> can only scan the csproj and the tool manifest for it, and says
/// so - nothing in the suite has ever touched an installed app.
/// <para>
/// This asserts the two halves that make an update possible at all, against the machine's own installation and the
/// live feed: the app is laid out the way its setup leaves it, and the feed still serves a downloadable release of that
/// same package, no older than what is installed. Either half breaking - a moved bucket, a broken CNAME, a channel
/// renamed by a Velopack upgrade, a rolled-back upload - silently freezes every installed copy at its current version,
/// and the running app reports nothing: <c>Update()</c> logs "No newer release is available" and returns.
/// </para>
/// <para>
/// Applying an update is deliberately not driven here. It replaces the machine's installation with whatever the feed
/// currently holds, which is not a test's to do to a developer's machine, and Velopack restarts the app to finish.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Windows), Retry(2)]
public class WindowsAppUpdateFeedTests
{
    /// <summary>Velopack's own feed file for the <c>win</c> channel, next to the packages at the feed root.</summary>
    private const string releasesFile = "releases.win.json";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel")]
    [DataRow(App.Sales, DisplayName = "Sales")]
    public async Task AnInstalledApp_Should_BeAbleToUpdateItselfFromItsFeed(App app)
    {
        var appId = DeployedApps.WindowsAppIdOf(app);
        var feed = DeployedApps.WindowsUpdateFeedOf(app);

        if (appId is null || feed is null)
            Assert.Inconclusive($"{app} has no Windows build.");

        var installed = InstalledVersionOf(appId!);

        var assets = await ReadFeed(feed!, appId!);

        var fullReleases = assets.Where(asset => asset.Type is "Full").ToArray();

        Assert.IsNotEmpty(fullReleases,
            $"{feed}{releasesFile} lists no full release of {appId}. A delta alone cannot install onto a machine that is more than one release behind, and a fresh install has nothing to start from.");

        var newest = fullReleases.MaxBy(asset => asset.Version)!;

        Assert.IsGreaterThanOrEqualTo(installed, newest.Version,
            $"{appId} {installed} is installed while its feed offers nothing newer than {newest.Version}. The feed was rolled back or is serving another app's releases, and every installed copy is now stranded ahead of it.");

        // The feed listing a release is not the same as the bucket still holding it: a range request downloads a few
        // bytes and proves the file the UpdateManager would fetch is actually there.
        await AssertIsDownloadable(feed!, newest);
    }

    /// <summary>
    /// What the setup leaves behind, and what an update needs: the shim that launches the current version, the
    /// <c>Update.exe</c> Velopack re-launches the app through, and the package folder it stages a download in. An app
    /// copied into place rather than installed has the <c>current</c> folder and neither of the other two, and its
    /// <c>UpdateManager</c> refuses every update with "not installed".
    /// </summary>
    private Version InstalledVersionOf(string appId)
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appId);

        if (Directory.Exists(root) is false)
            Assert.Inconclusive($"{appId} is not installed on this machine, so there is no installation for a feed to update.");

        var executable = Path.Combine(root, "current", $"{appId}.exe");

        Assert.IsTrue(File.Exists(Path.Combine(root, "Update.exe")),
            $"{root} has no Update.exe. Velopack runs every install, update and uninstall hook through it, so this app cannot update itself however good its feed is.");

        Assert.IsTrue(Directory.Exists(Path.Combine(root, "packages")),
            $"{root} has no packages folder, which is where an update is downloaded before it is applied.");

        Assert.IsTrue(File.Exists(executable), $"{root} holds no current build of the app ({executable}).");

        var declared = FileVersionInfo.GetVersionInfo(executable).FileVersion;

        Assert.IsTrue(Version.TryParse(declared, out var parsed), $"{executable} declares '{declared}', which is not a version the feed can be compared against.");

        // vpk packs three-part versions; the file version carries a fourth component that is always zero.
        return new Version(parsed!.Major, parsed.Minor, parsed.Build);
    }

    private async Task<(string PackageId, string Type, Version Version, string FileName, long Size)[]> ReadFeed(string feed, string appId)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(1) };

        using var response = await httpClient.GetAsync($"{feed}{releasesFile}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            $"{feed}{releasesFile} answered {(int)response.StatusCode}. This is the url the installed app's UpdateManager reads, so nothing out there can update any more.");

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        var assets = JsonNode.Parse(body)?["Assets"]?.AsArray()
            ?? throw new AssertFailedException($"{feed}{releasesFile} is not a Velopack release feed: {body[..Math.Min(400, body.Length)]}");

        var mine = assets.Where(asset => asset!["PackageId"]!.GetValue<string>() == appId)
            .Select(asset => (
                PackageId: asset!["PackageId"]!.GetValue<string>(),
                Type: asset["Type"]!.GetValue<string>(),
                // A version vpk cannot have packed is a feed this app was never built for, not a comparison to guess at.
                Version: Version.Parse(asset["Version"]!.GetValue<string>()),
                FileName: asset["FileName"]!.GetValue<string>(),
                Size: asset["Size"]!.GetValue<long>()))
            .ToArray();

        Assert.IsNotEmpty(mine,
            $"{feed}{releasesFile} lists no release of {appId} at all, so its feed belongs to another app now.");

        return mine;
    }

    /// <summary>A ranged request: the file is many tens of megabytes, and what is in question is whether it is there.</summary>
    private async Task AssertIsDownloadable(string feed, (string PackageId, string Type, Version Version, string FileName, long Size) asset)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(1) };
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{feed}{asset.FileName}");
        request.Headers.Range = new(0, 1023);

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);

        Assert.IsTrue(response.IsSuccessStatusCode,
            $"{feed}{asset.FileName} answered {(int)response.StatusCode}, so the release the feed advertises cannot be downloaded.");

        var contentRange = response.Content.Headers.ContentRange;

        // A store that ignores Range answers 200 with the whole file, which is an answer too - what must not differ is
        // the size, since that is what Velopack checks the download against.
        var served = contentRange?.Length ?? response.Content.Headers.ContentLength;

        Assert.AreEqual(asset.Size, served,
            $"{asset.FileName} is {served} bytes where the feed says {asset.Size}. Velopack rejects a download whose length does not match, so this release cannot be applied.");
    }
}
