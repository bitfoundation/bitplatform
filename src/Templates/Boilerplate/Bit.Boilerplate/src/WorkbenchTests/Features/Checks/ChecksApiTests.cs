namespace Boilerplate.WorkbenchTests.Features.Checks;

/// <summary>
/// The checks an agent runs through <c>/api/checks/&lt;page&gt;</c>: every page the Workbench ships passes the search
/// for colors outside the theme, and none has a critical accessibility finding, on a phone and on a desktop.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ChecksApiTests
{
    public TestContext TestContext { get; set; } = default!;

    public static IEnumerable<object[]> Views => WorkbenchPageCatalog.Pages.SelectMany(page => new[] { new object[] { page.Key, DevicePresets.DefaultKey }, [page.Key, "desktop-1536"] });

    [TestMethod]
    [DynamicData(nameof(Views))]
    public async Task EveryPage_Should_UseOnlyTheThemesColorsAndHaveNoCriticalFinding(string pageKey, string device)
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        using var client = server.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(2);

        var response = await client.GetAsync($"api/checks/{pageKey}?device={device}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken))!;
        var theme = result["theme"]!.AsArray();
        var accessibility = result["accessibility"]!.AsArray();

        Assert.IsEmpty(theme, $"Colors outside the theme: {theme.ToJsonString()}");
        Assert.DoesNotContain(finding => (string?)finding!["impact"] == "critical", accessibility, accessibility.ToJsonString());

        foreach (var finding in accessibility)
        {
            Assert.IsFalse(string.IsNullOrEmpty((string?)finding!["rule"]));
            Assert.IsFalse(string.IsNullOrEmpty((string?)finding["helpUrl"]));
            Assert.IsGreaterThan(0, (int)finding["count"]!);
        }
    }

    [TestMethod]
    public async Task AnAccentTriedOnTheView_Should_CountAsTheThemesColor()
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        var screenshots = server.Services.GetRequiredService<WorkbenchScreenshotService>();

        var json = await screenshots.RunChecks(WorkbenchPageCatalog.Find("theme")!, "?device=iphone-se&accent=ff00ff", TestContext.CancellationToken);
        var result = JsonNode.Parse(json)!;

        Assert.IsEmpty(result["theme"]!.AsArray(), "An accent tried on the view becomes the theme's primary, so it is the theme's color.");
    }
}
