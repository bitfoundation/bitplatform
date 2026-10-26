namespace Boilerplate.Tests.E2E.Features.DownloadSize;

[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public class WebBitMinifierTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.TodoAot, DisplayName = nameof(App.TodoAot))]
    [DataRow(App.TodoSmall, DisplayName = nameof(App.TodoSmall))]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = nameof(App.AdminPanelWasmStandalone))]
    public async Task BlazorUIAssembly_Should_CarryTheMinifiedNames(App app)
    {
        using var httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });

        var url = new Uri(new Uri(DeployedApps.AddressOf(app)), "_framework/Bit.BlazorUI.wasm");

        using var response = await httpClient.GetAsync(url, TestContext.CancellationToken);
        var assembly = await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{url} answered {(int)response.StatusCode}.");

        Assert.AreEqual(-1, assembly.AsSpan().IndexOf("<BuildRenderTree>b__"u8),
            $"{url} still names its lambdas after BuildRenderTree: Bit.Minifier did not run on the publish.");
    }
}
