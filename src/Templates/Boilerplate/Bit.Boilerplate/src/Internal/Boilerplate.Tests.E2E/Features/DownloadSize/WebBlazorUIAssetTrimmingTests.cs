namespace Boilerplate.Tests.E2E.Features.DownloadSize;

[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebBlazorUIAssetTrimmingTests
{
    private static readonly (string Path, string[] Used, string[] Unused)[] bundles =
    [
        ("_content/Bit.BlazorUI/scripts/bit.blazorui.js", ["Callouts"], ["Splitter", "Ratings", "CircularTimePicker"]),
        ("_content/Bit.BlazorUI/styles/bit.blazorui.css", [".bit-btn"], [".bit-spl", ".bit-rtg", ".bit-ctp"]),
        ("_content/Bit.BlazorUI.Extras/scripts/bit.blazorui.extras.js", ["AppShell"], ["FullCalendar"]),
        ("_content/Bit.BlazorUI.Extras/styles/bit.blazorui.extras.css", [".bit-ash"], [".bit-bfc"])
    ];

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.TodoAot, DisplayName = nameof(App.TodoAot))]
    [DataRow(App.TodoSmall, DisplayName = nameof(App.TodoSmall))]
    [DataRow(App.TodoOffline, DisplayName = nameof(App.TodoOffline))]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = nameof(App.AdminPanelWasmStandalone))]
    public async Task BlazorUIBundles_Should_OnlyCarryWhatTheAppUses(App app)
    {
        using var httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });

        foreach (var (path, used, unused) in bundles)
        {
            var url = new Uri(new Uri(DeployedApps.AddressOf(app)), path);

            using var response = await httpClient.GetAsync(url, TestContext.CancellationToken);
            var content = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{url} answered {(int)response.StatusCode}.");

            foreach (var name in used)
            {
                Assert.IsTrue(content.Contains(name, StringComparison.Ordinal), $"{url} lacks {name}, which a component every page of the app renders needs: the publish trimmed too much.");
            }

            foreach (var name in unused)
            {
                Assert.IsFalse(content.Contains(name, StringComparison.Ordinal), $"{url} carries {name}, which belongs to a component no page of the app renders: the publish did not trim it.");
            }
        }
    }
}
