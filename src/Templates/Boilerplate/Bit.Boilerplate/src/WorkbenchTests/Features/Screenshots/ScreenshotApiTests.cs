using System.IO.Compression;

namespace Boilerplate.WorkbenchTests.Features.Screenshots;

/// <summary>
/// Screenshots, archives, thumbnails and review sheets come from the headless browser of the Workbench under test, the
/// one a developer or an agent gets them from.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ScreenshotApiTests
{
    public TestContext TestContext { get; set; } = default!;

    private WorkbenchTestServer server = default!;
    private HttpClient client = default!;

    [TestInitialize]
    public async Task Initialize()
    {
        server = await WorkbenchTestsInitializer.SharedServer;
        client = server.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(2);
    }

    [TestCleanup]
    public void Cleanup() => client.Dispose();

    private static string? MediaType(HttpResponseMessage response) => response.Content.Headers.ContentType?.MediaType;

    private static string? FileName(HttpResponseMessage response) => response.Content.Headers.ContentDisposition?.FileName;

    private static string? CacheControl(HttpResponseMessage response) => response.Headers.CacheControl?.ToString();

    [TestMethod]
    [DataRow("?device=iphone-se", 750, 1334, "theme-iphone-se-750x1334-en-us-light-live.png")]
    [DataRow("?device=iphone-se&rotated=true&theme=dark&data=empty", 1334, 750, "theme-iphone-se-1334x750-en-us-dark-empty.png")]
    [DataRow("?device=store-play-phone&backdrop=ocean&caption=Hello", 1080, 1920, "theme-store-play-phone-1080x1920-en-us-light-framed.png")]
    [DataRow("?device=custom&size=640x480", 640, 480, "theme-custom-640x480-en-us-light-live.png")]
    public async Task AScreenshot_Should_HaveTheDevicesPixelSizeAndNoAlphaChannel(string query, int width, int height, string fileName)
    {
        var response = await client.GetAsync($"api/screenshots/theme{query}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("image/png", MediaType(response));
        Assert.AreEqual(fileName, FileName(response));

        var png = PngInfo.Read(await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken));

        Assert.AreEqual((width, height), (png.Width, png.Height));
        Assert.IsFalse(png.HasAlpha, "The App Store refuses screenshots with an alpha channel.");
    }

    [TestMethod]
    public async Task AScreenshotOfAPageTheWorkbenchLacks_Should_BeNotFound()
    {
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("api/screenshots/missing", TestContext.CancellationToken)).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("api/checks/missing", TestContext.CancellationToken)).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("api/thumbnails/missing", TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    public async Task AnArchive_Should_HoldEveryViewInOrder()
    {
        var url = WorkbenchUrls.ScreenshotArchive(
        [
            ("theme", new WorkbenchScenario { Device = "iphone-se" }, null),
            ("theme", new WorkbenchScenario { Device = "iphone-se", Theme = WorkbenchThemeMode.Dark }, null)
        ]);

        var response = await client.GetAsync(url.TrimStart('/'), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("theme-screenshots.zip", FileName(response));

        using var archive = new ZipArchive(await response.Content.ReadAsStreamAsync(TestContext.CancellationToken));

        Assert.AreSequenceEqual(new[] { "01-theme-iphone-se-750x1334-en-us-light-live.png", "02-theme-iphone-se-750x1334-en-us-dark-live.png" },
            archive.Entries.Select(e => e.FullName).ToArray());
    }

    [TestMethod]
    public async Task AnArchiveThatNamesNoViewTooManyOrAnUnknownPage_Should_BeRefused()
    {
        var tooMany = WorkbenchUrls.ScreenshotArchive(Enumerable.Range(0, 25).Select(_ => ("theme", new WorkbenchScenario(), (string?)null)));
        var unknown = WorkbenchUrls.ScreenshotArchive([("missing", new WorkbenchScenario(), null)]);

        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.GetAsync("api/screenshots/zip", TestContext.CancellationToken)).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.GetAsync("api/screenshots/zip?shots=garbage", TestContext.CancellationToken)).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.GetAsync(tooMany.TrimStart('/'), TestContext.CancellationToken)).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.GetAsync(unknown.TrimStart('/'), TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    [DataRow(true, 402, 874)]
    [DataRow(false, 720, 450)]
    public async Task AThumbnail_Should_BeCapturedOncePerDeviceKindAndRevalidated(bool phone, int width, int height)
    {
        var response = await client.GetAsync($"api/thumbnails/settings?phone={phone}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("no-cache", CacheControl(response));

        var png = await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken);
        var info = PngInfo.Read(png);

        Assert.AreEqual((width, height), (info.Width, info.Height));
        Assert.AreSequenceEqual(png, await client.GetByteArrayAsync($"api/thumbnails/settings?phone={phone}", TestContext.CancellationToken), "The second request gets the same capture.");
    }

    [TestMethod]
    public async Task AReviewSheet_Should_HoldTheViewsAndThePagesComments()
    {
        await using var reviewer = await WorkbenchTestReviewer.Open(server);

        var text = $"Sheet comment {Guid.NewGuid():N}";
        await reviewer.Comment(server, "settings", text);

        var url = WorkbenchUrls.ReviewSheet("settings", [("settings", new WorkbenchScenario { Device = "iphone-se" }, null)], "html");
        var response = await client.GetAsync(url.TrimStart('/'), TestContext.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("settings-review.html", FileName(response));
        Assert.Contains(text, html);
        Assert.Contains("data:image/png;base64,", html);
        Assert.Contains(WorkbenchStrings.SettingsTitle, html);
    }

    [TestMethod]
    public async Task AReviewSheet_Should_AlsoComeAsAPdf()
    {
        var url = WorkbenchUrls.ReviewSheet("theme", [("theme", new WorkbenchScenario { Device = "iphone-se" }, null)], "pdf");
        var response = await client.GetAsync(url.TrimStart('/'), TestContext.CancellationToken);
        var pdf = await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/pdf", MediaType(response));
        Assert.AreEqual("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [TestMethod]
    public async Task AReviewSheetForOtherPagesViews_Should_BeRefused()
    {
        var url = WorkbenchUrls.ReviewSheet("theme", [("settings", new WorkbenchScenario(), null)], "html");

        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.GetAsync(url.TrimStart('/'), TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    public async Task ACommentsScreenshot_Should_ShowTheViewItWasLeftOn()
    {
        await using var reviewer = await WorkbenchTestReviewer.Open(server);

        var id = await reviewer.Comment(server, "theme", "Look here", query: "?device=iphone-se&rotated=true");

        var response = await client.GetAsync($"api/review/{id}/screenshot", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var png = PngInfo.Read(await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken));

        Assert.AreEqual((1334, 750), (png.Width, png.Height));
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("api/review/missing/screenshot", TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    public async Task ASnapshot_Should_ChangeWhatTheScreenshotShows()
    {
        var snapshots = server.Services.GetRequiredService<WorkbenchSnapshots>();

        var plain = await client.GetByteArrayAsync("api/screenshots/pos-dashboard?device=iphone-se", TestContext.CancellationToken);
        var again = await client.GetByteArrayAsync("api/screenshots/pos-dashboard?device=iphone-se", TestContext.CancellationToken);

        Assert.AreSequenceEqual(plain, again, "The same view captures the same image.");

        var store = new PosDashboardStore();
        new BlexManager().Register(store);
        store.Edit(true);

        var editing = snapshots.Add(new JsonObject { [store.Name] = store.SerializeState() }.ToJsonString());
        var customized = await client.GetByteArrayAsync($"api/screenshots/pos-dashboard?device=iphone-se&snapshot={editing}", TestContext.CancellationToken);

        Assert.AreNotEqual(Convert.ToHexString(plain), Convert.ToHexString(customized), "The snapshot opened the dashboard in edit mode.");
    }
}
