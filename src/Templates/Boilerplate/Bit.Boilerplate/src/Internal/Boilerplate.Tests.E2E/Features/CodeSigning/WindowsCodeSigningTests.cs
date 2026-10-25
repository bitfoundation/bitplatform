using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.CodeSigning;

[TestClass, TestCategory(TestCategories.Windows), DoNotParallelize, Retry(2)]
public partial class WindowsCodeSigningTests : AppTestBase
{
    private const string repository = "https://github.com/bitfoundation/bitplatform";

    private static readonly HttpClient feedClient = new();

    protected override IAppOpener AppOpener => new WindowsAppOpener();

    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    public void TheExecutable_Should_BeSigned(App app)
    {
        var installed = Installed(app);

        Assert.IsNotNull(Authenticode.VerifiedSignerOf(installed.Executable), $"{installed.Executable} has no valid Authenticode signature.");
    }

    [TestMethod]
    public async Task TheBitPackagesTheAppShips_Should_CarryAssembliesSignedLikeTheApp()
    {
        var installed = Installed(App.AdminPanel);

        var signer = Authenticode.VerifiedSignerOf(installed.Executable);

        Assert.IsNotNull(signer, $"{installed.Executable} has no valid Authenticode signature to compare the packages' with.");

        var dependencies = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(installed.Folder, $"{installed.AppId}.deps.json"), TestContext.CancellationToken))!;

        var packages = dependencies["libraries"]!.AsObject()
            .Where(library => library.Key.StartsWith("Bit.", StringComparison.Ordinal) && library.Value!["type"]!.GetValue<string>() is "package")
            .Select(library => library.Key.Split('/'))
            .ToArray();

        Assert.IsNotEmpty(packages, $"{installed.AppId}.deps.json lists no Bit package.");

        foreach (var package in packages)
        {
            var (id, version) = (package[0].ToLowerInvariant(), package[1].ToLowerInvariant());

            using var zip = new ZipArchive(new MemoryStream(await feedClient.GetByteArrayAsync($"https://api.nuget.org/v3-flatcontainer/{id}/{version}/{id}.{version}.nupkg", TestContext.CancellationToken)));

            var assemblies = zip.Entries.Where(entry => Path.GetFileName(entry.FullName) is var name && name.EndsWith(".dll", StringComparison.Ordinal) && (name.StartsWith("Bit.", StringComparison.Ordinal) || name is "bit.dll")).ToArray();

            Assert.IsNotEmpty(assemblies, $"{package[0]} {package[1]} on nuget.org holds no bit assembly.");

            foreach (var assembly in assemblies)
            {
                var file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{Path.GetFileName(assembly.FullName)}");

                try
                {
                    assembly.ExtractToFile(file);

                    Assert.AreEqual(signer, Authenticode.VerifiedSignerOf(file),
                        $"{assembly.FullName} in {package[0]} {package[1]} on nuget.org is not signed by {signer}, the signer of {installed.AppId}.");
                }
                finally
                {
                    File.Delete(file);
                }
            }
        }
    }

    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    public async Task TheAboutPage_Should_ShowTheSignerAndTheBuildTheAppCameFrom(App app)
    {
        var installed = Installed(app);

        var signer = Authenticode.VerifiedSignerOf(installed.Executable);

        Assert.IsNotNull(signer, $"{installed.Executable} has no valid Authenticode signature, so the About page has no signer to show.");

        var page = await OpenAboutPage(app);

        await Expect(InfoValue(page, "Signed by")).ToHaveTextAsync(signer);
        await Expect(InfoValue(page, "Source").Locator("a")).ToHaveAttributeAsync("href", $"{repository}/tree/{installed.Commit}");
        await Expect(InfoValue(page, "Built by").Locator("a")).ToHaveAttributeAsync("href", new Regex($"^{Regex.Escape(repository)}/actions/runs/\\d+$"));
        await Expect(InfoValue(page, "Verify").Locator("a")).ToHaveAttributeAsync("href", $"{repository}/attestations");
    }

    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    public async Task TheInstalledFiles_Should_BeTheOnesTheBuildOnTheAboutPageAttested(App app)
    {
        var installed = Installed(app);

        var page = await OpenAboutPage(app);

        var buildRun = await InfoValue(page, "Built by").Locator("a").GetAttributeAsync("href");

        Assert.IsNotNull(buildRun, $"The About page of {installed.AppId} names no build.");

        var package = await DownloadFullRelease(app, installed);

        var attestation = await VerifyAttestation(package);

        var attestedCommit = attestation["sourceRepositoryDigest"];

        Assert.IsNotNull(attestedCommit, $"The attestation of {Path.GetFileName(package)} names no commit.");
        Assert.AreEqual(installed.Commit, attestedCommit.GetValue<string>(),
            $"{Path.GetFileName(package)} is attested for another commit than the one {installed.AppId} {installed.Version} was built from.");

        using (var zip = ZipFile.OpenRead(package))
        {
            foreach (var file in new[] { $"{installed.AppId}.exe", "Bit.BlazorUI.dll" })
            {
                var entry = zip.GetEntry($"lib/app/{file}");

                Assert.IsNotNull(entry, $"{Path.GetFileName(package)} has no lib/app/{file}.");

                await using var packaged = entry.Open();
                await using var local = File.OpenRead(Path.Combine(installed.Folder, file));

                Assert.AreSequenceEqual(await SHA256.HashDataAsync(packaged), await SHA256.HashDataAsync(local),
                    $"The installed {file} of {installed.AppId} is not the one in its attested release.");
            }
        }

        var attestedRun = attestation["runInvocationURI"];

        Assert.IsNotNull(attestedRun, $"The attestation of {Path.GetFileName(package)} names no run.");
        Assert.StartsWith($"{buildRun}/attempts/", attestedRun.GetValue<string>(),
            $"{Path.GetFileName(package)} was attested by another run than the one the About page of {installed.AppId} names.");
    }

    private async Task<IPage> OpenAboutPage(App app)
    {
        var page = await OpenApp(app);

        await WaitUntilInteractive(page);
        await GoToWhenInteractive(page, PageUrls.About);
        await Expect(page.Locator(".info-item").First).ToBeVisibleAsync();

        return page;
    }

    private static ILocator InfoValue(IPage page, string label)
    {
        return page.Locator(".info-item", new() { Has = page.Locator(".info-label", new() { HasText = label }) }).Locator(".info-value");
    }

    private static InstalledApp Installed(App app)
    {
        var appId = DeployedApps.WindowsAppIdOf(app);

        if (appId is null)
            Assert.Inconclusive($"{app} has no Windows build.");

        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appId!, "current");
        var executable = Path.Combine(folder, $"{appId}.exe");

        if (File.Exists(executable) is false)
            Assert.Inconclusive($"{appId} is not installed on this machine.");

        var productVersion = FileVersionInfo.GetVersionInfo(executable).ProductVersion ?? "";
        var parts = productVersion.Split('+', 2);

        Assert.HasCount(2, parts, $"{executable} has the product version '{productVersion}', which names no commit.");

        return new(appId!, folder, executable, parts[0], parts[1]);
    }

    private async Task<string> DownloadFullRelease(App app, InstalledApp installed)
    {
        var feed = DeployedApps.WindowsUpdateFeedOf(app)!;

        var releases = JsonNode.Parse(await feedClient.GetStringAsync($"{feed}releases.win.json", TestContext.CancellationToken))!;

        var fileName = releases["Assets"]!.AsArray()
            .Where(asset => asset!["Type"]!.GetValue<string>() is "Full" && asset["Version"]!.GetValue<string>() == installed.Version)
            .Select(asset => asset!["FileName"]!.GetValue<string>())
            .FirstOrDefault();

        Assert.IsNotNull(fileName, $"{feed} has no full release of {installed.AppId} {installed.Version}, the installed version.");

        var package = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{fileName}");

        RegisterForCleanup(() =>
        {
            File.Delete(package);
            return Task.CompletedTask;
        });

        await using (var download = await feedClient.GetStreamAsync($"{feed}{fileName}", TestContext.CancellationToken))
        await using (var file = File.Create(package))
        {
            await download.CopyToAsync(file, TestContext.CancellationToken);
        }

        return package;
    }

    private async Task<JsonNode> VerifyAttestation(string package)
    {
        ProcessStartInfo startInfo = new("gh", ["attestation", "verify", package, "--repo", "bitfoundation/bitplatform", "--format", "json"])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process? gh;
        try
        {
            gh = Process.Start(startInfo);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            gh = null;
        }

        if (gh is null)
            Assert.Inconclusive("The GitHub CLI (gh) is not installed, so the release's attestation can't be verified.");

        using (gh)
        {
            var output = gh!.StandardOutput.ReadToEndAsync(TestContext.CancellationToken);
            var error = gh.StandardError.ReadToEndAsync(TestContext.CancellationToken);

            await gh.WaitForExitAsync(TestContext.CancellationToken);

            if (gh.ExitCode is not 0 && (await error).Contains("gh auth login", StringComparison.Ordinal))
                Assert.Inconclusive("The GitHub CLI is not signed in, so the release's attestation can't be verified.");

            Assert.AreEqual(0, gh.ExitCode, $"gh attestation verify {Path.GetFileName(package)} failed: {await error}");

            return JsonNode.Parse(await output)![0]!["verificationResult"]!["signature"]!["certificate"]!;
        }
    }

    private sealed record InstalledApp(string AppId, string Folder, string Executable, string Version, string Commit);
}
