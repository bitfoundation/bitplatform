using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.WorkbenchTests.Features.Screenshots;

/// <summary>The shell's screenshot buttons, the review sheet and the home page's thumbnails, used the way a reviewer uses them.</summary>
[TestClass, TestCategory("UITest"), DoNotParallelize]
public class ScreenshotsUITests : WorkbenchPageTest
{
    private ILocator Customize(string paneId = "1") => View(paneId).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosCustomize, Exact = true });

    private async Task<(byte[] Content, IDownload Download)> Download(Func<Task> press, string? expectedFileName = null)
    {
        var download = await Page.RunAndWaitForDownloadAsync(press, new() { Timeout = 120_000 });

        if (expectedFileName is not null)
        {
            Assert.AreEqual(expectedFileName, download.SuggestedFilename);
        }

        await using var stream = await download.CreateReadStreamAsync();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);

        return (memory.ToArray(), download);
    }

    /// <summary>The view's screenshot button: in its header on a desktop, in the Tools panel's header on a phone.</summary>
    private async Task PressScreenshot()
    {
        if (IsPhone)
        {
            await OpenTools();
            await Press(Page.Locator(".tools-header").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ScreenshotThisView }));
        }
        else
        {
            await Press(Page.Locator(".pane-header").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ScreenshotThisView }));
        }
    }

    private async Task PressInTheMenu(string item)
    {
        await Press(Button(WorkbenchStrings.Screenshot));
        await Press(Page.GetByText(item, new() { Exact = true }));
    }

    private static string? QueryOf(string url, string key) => WorkbenchUrls.ReadQuery(new Uri(new("http://localhost/"), url).ToString(), key);

    /// <summary>Opens the address a snapshot id restores and says whether the dashboard came back in its edit mode.</summary>
    private async Task ExpectTheSnapshotToShowEditMode(string query, bool isEditing)
    {
        await OpenPreview("pos-dashboard", query);

        if (isEditing)
        {
            await Expect(Page.GetByText(WorkbenchStrings.PosEditHint)).ToBeVisibleAsync();
        }
        else
        {
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosCustomize, Exact = true })).ToBeVisibleAsync();
            await Expect(Page.GetByText(WorkbenchStrings.PosEditHint)).ToHaveCountAsync(0);
        }
    }

    [TestMethod, EverySurface]
    public async Task AViewsScreenshot_Should_BeAPngAtTheDevicesPixelSize(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=iphone-se");

        var (png, _) = await Download(PressScreenshot, "theme-iphone-se-750x1334-en-us-light-live.png");
        var info = PngInfo.Read(png);

        Assert.AreEqual((750, 1334), (info.Width, info.Height));
        Assert.IsFalse(info.HasAlpha);
    }

    [TestMethod, EverySurface]
    public async Task AViewsScreenshot_Should_BeNamedAfterItsThemeLanguageAndDataState(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=iphone-se&theme=dark&culture=fa-IR&data=empty");

        var (png, _) = await Download(PressScreenshot, "theme-iphone-se-750x1334-fa-ir-dark-empty.png");

        Assert.AreEqual(750, PngInfo.Read(png).Width);
    }

    [TestMethod, EverySurface]
    public async Task AScreenshot_Should_ShowWhatWasDoneOnThePage(WorkbenchSurface surface)
    {
        using var client = Server.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(2);

        var untouched = await client.GetByteArrayAsync("api/screenshots/pos-dashboard?device=iphone-se");

        await OpenWorkbench("pos-dashboard", "?device=iphone-se");

        await Press(Customize());
        await Expect(View().GetByText(WorkbenchStrings.PosEditHint)).ToBeVisibleAsync();

        var (customized, download) = await Download(PressScreenshot);

        Assert.AreNotEqual(Convert.ToHexString(untouched), Convert.ToHexString(customized), "The screenshot shows the dashboard in edit mode, as the view did.");

        var snapshot = QueryOf(download.Url, WorkbenchUrls.SnapshotQueryKey);

        Assert.IsNotNull(snapshot);

        await ExpectTheSnapshotToShowEditMode($"?device=iphone-se&{WorkbenchUrls.SnapshotQueryKey}={snapshot}", isEditing: true);
    }

    [TestMethod, EverySurface]
    public async Task AStoreScreenshot_Should_ShowItsBackdropAndCaptionInTheViewAndTheFile(WorkbenchSurface surface)
    {
        var caption = $"Made for you {Guid.NewGuid():N}"[..24];

        await OpenWorkbench("theme", "?device=store-play-phone");

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Switch, new() { Name = WorkbenchStrings.Backdrop }));
        await Press(Tools.GetByRole(AriaRole.Radio, new() { Name = WorkbenchStrings.BackdropOcean, Exact = true }));

        var field = Tools.GetByLabel(WorkbenchStrings.Caption);

        await field.FillAsync(caption);
        await field.BlurAsync();
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex("backdrop=ocean"));
        await WaitForViews();
        await Expect(View().Locator(".composition.ocean")).ToBeVisibleAsync();
        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = caption, Exact = true })).ToBeVisibleAsync();
        await Expect(View().Locator("iframe.screen-frame")).ToBeVisibleAsync();

        var (png, download) = await Download(PressScreenshot, "theme-store-play-phone-1080x1920-en-us-light-framed.png");
        var info = PngInfo.Read(png);

        Assert.AreEqual((1080, 1920), (info.Width, info.Height));
        Assert.IsFalse(info.HasAlpha);
        Assert.AreEqual(caption, QueryOf(download.Url, "caption"));
    }

    [TestMethod, EverySurface]
    public async Task AStoreScreenshot_Should_ChangeWithItsBackdrop(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=store-play-phone&backdrop=ocean&caption=Hello");

        var (ocean, _) = await Download(PressScreenshot);

        await CloseTools();

        await OpenTools();
        await Press(Tools.GetByRole(AriaRole.Radio, new() { Name = WorkbenchStrings.BackdropSunset, Exact = true }));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex("backdrop=sunset"));
        await WaitForViews();
        await Expect(View().Locator(".composition.sunset")).ToBeVisibleAsync();

        var (sunset, _) = await Download(PressScreenshot);

        Assert.AreNotEqual(Convert.ToHexString(ocean), Convert.ToHexString(sunset));
    }

    [TestMethod, OnDesktops]
    public async Task TheMenusThisView_Should_DownloadTheSelectedView(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", $"?panes={WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "iphone-se", Theme = WorkbenchThemeMode.Dark }])}");

        await Press(Page.Locator(".pane").Nth(1).Locator(".pane-header .pane-device"));
        await Expect(Page.Locator(".pane").Nth(1)).ToHaveAttributeAsync("aria-current", "true");

        await Download(() => PressInTheMenu(WorkbenchStrings.ScreenshotThisView), "theme-iphone-se-750x1334-en-us-dark-live.png");
    }

    [TestMethod, OnDesktops]
    public async Task EachViewsHeader_Should_DownloadThatView(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", $"?panes={WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "iphone-se", Theme = WorkbenchThemeMode.Dark }])}");

        await Download(() => Press(Page.Locator(".pane").Nth(1).Locator(".pane-header").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ScreenshotThisView })), "theme-iphone-se-750x1334-en-us-dark-live.png");
    }

    [TestMethod, OnDesktops]
    public async Task ScreenshotsOfAllViews_Should_ComeAsOneArchiveWithEachViewsState(WorkbenchSurface surface)
    {
        await OpenWorkbench("pos-dashboard", $"?panes={WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "iphone-se", Theme = WorkbenchThemeMode.Dark }])}");

        var panes = await Page.Locator("iframe[data-pane-id]").EvaluateAllAsync<string[]>("frames => frames.map(f => f.dataset.paneId)");

        await Press(Customize(panes[1]));
        await Expect(View(panes[1]).GetByText(WorkbenchStrings.PosEditHint)).ToBeVisibleAsync();

        var (zip, download) = await Download(() => PressInTheMenu(WorkbenchStrings.ScreenshotAllViews), "pos-dashboard-screenshots.zip");

        using var archive = new ZipArchive(new MemoryStream(zip));

        Assert.AreSequenceEqual(new[] { "01-pos-dashboard-iphone-se-750x1334-en-us-light-live.png", "02-pos-dashboard-iphone-se-750x1334-en-us-dark-live.png" }, archive.Entries.Select(e => e.FullName).ToArray());

        foreach (var entry in archive.Entries)
        {
            await using var stream = await entry.OpenAsync();
            using var png = new MemoryStream();
            await stream.CopyToAsync(png);

            Assert.AreEqual(750, PngInfo.Read(png.ToArray()).Width);
        }

        var shots = WorkbenchUrls.DecodeShots(QueryOf(download.Url, "shots"));

        Assert.HasCount(2, shots);

        var first = QueryHelpers.ParseQuery(shots[0].Query)[WorkbenchUrls.SnapshotQueryKey].ToString();
        var second = QueryHelpers.ParseQuery(shots[1].Query)[WorkbenchUrls.SnapshotQueryKey].ToString();

        await ExpectTheSnapshotToShowEditMode($"?device=iphone-se&{WorkbenchUrls.SnapshotQueryKey}={second}", isEditing: true);
        await ExpectTheSnapshotToShowEditMode($"?device=iphone-se&{WorkbenchUrls.SnapshotQueryKey}={first}", isEditing: false);
    }

    [TestMethod, OnDesktops]
    public async Task TheReviewSheet_Should_HoldEveryViewAndTheCommentsAsHtml(WorkbenchSurface surface)
    {
        var comment = $"Bigger please <{Guid.NewGuid():N}>";

        if (HasAgent)
        {
            await using var reviewer = await WorkbenchTestReviewer.Open(Server);

            await reviewer.Comment(Server, "theme", comment);
        }

        await OpenWorkbench("theme", $"?panes={WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "iphone-se", Theme = WorkbenchThemeMode.Dark }])}");

        var (html, _) = await Download(() => PressInTheMenu(WorkbenchStrings.ReviewSheetHtml), "theme-review.html");
        var sheet = Encoding.UTF8.GetString(html);

        Assert.Contains($"<h1>{WorkbenchStrings.ThemePageTitle}", sheet);
        Assert.AreEqual(2, Regex.Count(sheet, "<img [^>]*src=\"data:image/png;base64,"));
        Assert.AreEqual(2, Regex.Count(sheet, "<figcaption>"));
        Assert.Contains(WorkbenchStrings.ThemeDark, sheet);

        if (HasAgent)
        {
            Assert.Contains(System.Text.Encodings.Web.HtmlEncoder.Default.Encode(comment), sheet);
            Assert.DoesNotContain(WorkbenchStrings.ReviewSheetNoComments, sheet);
        }
        else
        {
            Assert.Contains(WorkbenchStrings.ReviewSheetNoComments, sheet);
        }
    }

    [TestMethod, OnDesktops]
    public async Task TheReviewSheet_Should_ComeAsAPdf(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?device=iphone-se");

        var (pdf, _) = await Download(() => PressInTheMenu(WorkbenchStrings.ReviewSheetPdf), "theme-review.pdf");

        Assert.AreEqual("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [TestMethod, OnPhones]
    public async Task APhone_Should_OfferOnlyTheScreenshotOfItsView(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenTools();

        await Expect(Page.Locator(".tools-header").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ScreenshotThisView })).ToBeVisibleAsync();
        await Expect(Button(WorkbenchStrings.Screenshot)).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.ScreenshotAllViews, new() { Exact = true })).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.ReviewSheetHtml, new() { Exact = true })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheHomePagesCards_Should_ShowEachPagesThumbnailInTheShellsTheme(WorkbenchSurface surface)
    {
        await Page.GotoAsync(Server.Address.ToString());
        await WaitUntilInteractive();

        var card = Page.Locator("a.page-card").First;

        await card.ScrollIntoViewIfNeededAsync();

        var light = card.Locator(".thumbnail img.light-only");
        var dark = card.Locator(".thumbnail img.dark-only");

        await Expect(light).ToBeVisibleAsync();
        await Expect(dark).ToBeHiddenAsync();
        await WorkbenchTestReviewer.WaitFor(() => light.EvaluateAsync<int>("image => image.naturalWidth"), width => width > 0, TimeSpan.FromMinutes(1));

        await Press(Button(WorkbenchStrings.ToggleWorkbenchTheme));

        await Expect(dark).ToBeVisibleAsync();
        await Expect(light).ToBeHiddenAsync();
        await card.ScrollIntoViewIfNeededAsync();
        await WorkbenchTestReviewer.WaitFor(() => dark.EvaluateAsync<int>("image => image.naturalWidth"), width => width > 0, TimeSpan.FromMinutes(1));
    }

    [TestMethod, EverySurface]
    public async Task TheHomePagesHero_Should_ShowALaptopAndAPhone(WorkbenchSurface surface)
    {
        await Page.GotoAsync(Server.Address.ToString());

        var laptop = Page.Locator(".laptop-holder img.light-only");
        var phone = Page.Locator(".phone-holder img.light-only");

        await Expect(laptop).ToBeVisibleAsync();
        await Expect(phone).ToBeVisibleAsync();
        await WorkbenchTestReviewer.WaitFor(() => laptop.EvaluateAsync<int>("image => image.naturalWidth"), width => width > 0, TimeSpan.FromMinutes(1));
        await WorkbenchTestReviewer.WaitFor(() => phone.EvaluateAsync<int>("image => image.naturalWidth"), width => width > 0, TimeSpan.FromMinutes(1));
    }
}
