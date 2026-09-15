namespace Boilerplate.Tests.E2E.Features.DownloadSize;

/// <summary>
/// A web publish rebuilds bit-butil.js from only the modules the app can call (BitButilTrimScripts in
/// Directory.Build.props): Server.Web from a scan of its assemblies, a WebAssembly standalone app from its trimmed
/// Bit.Butil.dll. Untrimmed, the bundle carries every module Bit.Butil has - about 328 KB of JavaScript. Web only: the
/// Windows and Android apps are not trimmed by the Bit.Butil package they are built with.
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebButilScriptTrimmingTests
{
    /// <summary>Half of the untrimmed bundle; every deployment is well under it.</summary>
    private const int maxTrimmedBytes = 160_000;

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.TodoAot, DisplayName = nameof(App.TodoAot))]
    [DataRow(App.TodoSmall, DisplayName = nameof(App.TodoSmall))]
    [DataRow(App.TodoOffline, DisplayName = nameof(App.TodoOffline))]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = nameof(App.AdminPanelWasmStandalone))]
    public async Task ButilScript_Should_OnlyCarryTheModulesTheAppUses(App app)
    {
        using var httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });

        var url = new Uri(new Uri(DeployedApps.AddressOf(app)), "_content/Bit.Butil/bit-butil.js");

        using var response = await httpClient.GetAsync(url, TestContext.CancellationToken);
        var script = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{url} answered {(int)response.StatusCode}.");

        // No page of the app keeps the screen awake, so a bundle that still has the module was not trimmed.
        Assert.DoesNotContain("BitButil.wakeLock", script, $"{url} carries the WakeLock module, which nothing in the app calls.");

        Assert.IsLessThan(maxTrimmedBytes, script.Length,
            $"{url} is {script.Length:N0} characters, close to the untrimmed bundle: the publish did not trim it.");
    }
}
