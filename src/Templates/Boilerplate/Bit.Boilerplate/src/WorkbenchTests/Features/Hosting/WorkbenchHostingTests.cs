namespace Boilerplate.WorkbenchTests.Features.Hosting;

[TestClass, TestCategory("IntegrationTest")]
public class WorkbenchHostingTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow("")]
    [DataRow("workbench/theme")]
    [DataRow("preview/buildings?data=empty")]
    [DataRow("flows")]
    public async Task EveryShellAddress_Should_PrerenderItsPage(string url)
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        using var client = server.CreateClient();

        var response = await client.GetAsync(url, TestContext.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("_framework/blazor.web.js", html);
        Assert.Contains("scripts/workbench.js", html);
    }

    [TestMethod]
    [DataRow("styles/workbench.css")]
    [DataRow("scripts/workbench.js")]
    [DataRow("scripts/axe.min.js")]
    [DataRow("Boilerplate.Workbench.styles.css")]
    [DataRow("_content/Boilerplate.Client.Core/styles/app.css")]
    [DataRow("_content/Bit.BlazorUI/styles/bit.blazorui.css")]
    public async Task TheBuiltAssets_Should_BeServed(string url)
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        using var client = server.CreateClient();

        var response = await client.GetAsync(url, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{url} isn't served.");
    }

    [TestMethod]
    [TestCategory("Localization")]
    public async Task APreview_Should_TakeItsCultureFromItsQueryString()
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        using var client = server.CreateClient();

        var html = await client.GetStringAsync("preview/theme?culture=fa-IR", TestContext.CancellationToken);

        Assert.Contains("lang=\"fa-IR\"", html);
        Assert.Contains("dir=\"rtl\"", html);
    }
}
