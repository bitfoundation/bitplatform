using System.IO.Compression;

namespace Boilerplate.WorkbenchTests.Features.Screenshots;

/// <summary>The shell's screenshot buttons, its Checks tab and its Flows tab, used the way a reviewer uses them.</summary>
[TestClass, TestCategory("UITest")]
public class ScreenshotsUITests : WorkbenchPageTest
{
    private ILocator Button(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private async Task<byte[]> Download(Func<Task> click, string? expectedFileName = null)
    {
        var download = await Page.RunAndWaitForDownloadAsync(click, new() { Timeout = 120_000 });

        if (expectedFileName is not null)
        {
            Assert.AreEqual(expectedFileName, download.SuggestedFilename);
        }

        await using var stream = await download.CreateReadStreamAsync();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);

        return memory.ToArray();
    }

    [TestMethod]
    public async Task AViewsScreenshot_Should_BeAPngAtTheDevicesPixelSize()
    {
        await OpenWorkbench("theme", "?device=iphone-se");

        var png = await Download(() => Page.Locator(".pane-header").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ScreenshotThisView }).ClickAsync(),
                                 "theme-iphone-se-750x1334-en-us-light-live.png");

        var info = PngInfo.Read(png);

        Assert.AreEqual((750, 1334), (info.Width, info.Height));
    }

    [TestMethod]
    public async Task AScreenshot_Should_ShowWhatWasDoneOnThePage()
    {
        using var client = Server.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(2);

        var untouched = await client.GetByteArrayAsync("api/screenshots/pos-dashboard?device=iphone-se");

        await OpenWorkbench("pos-dashboard", "?device=iphone-se");

        await View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosCustomize, Exact = true }).ClickAsync();
        await Expect(View().GetByText(WorkbenchStrings.PosEditHint)).ToBeVisibleAsync();

        var customized = await Download(() => Page.Locator(".pane-header").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ScreenshotThisView }).ClickAsync());

        Assert.AreNotEqual(Convert.ToHexString(untouched), Convert.ToHexString(customized), "The screenshot shows the dashboard in edit mode, as the view did.");
    }

    [TestMethod]
    public async Task ScreenshotsOfAllViews_Should_ComeAsOneArchive()
    {
        await OpenWorkbench("theme", $"?panes={WorkbenchUrls.EncodePanes([new() { Device = "iphone-se" }, new() { Device = "iphone-se", Theme = WorkbenchThemeMode.Dark }])}");

        var zip = await Download(async () =>
        {
            await Button(WorkbenchStrings.Screenshot).ClickAsync();
            await Page.GetByText(WorkbenchStrings.ScreenshotAllViews, new() { Exact = true }).ClickAsync();
        }, "theme-screenshots.zip");

        using var archive = new ZipArchive(new MemoryStream(zip));

        Assert.AreSequenceEqual(new[] { "01-theme-iphone-se-750x1334-en-us-light-live.png", "02-theme-iphone-se-750x1334-en-us-dark-live.png" }, archive.Entries.Select(e => e.FullName).ToArray());
    }

    [TestMethod]
    public async Task TheChecksTab_Should_CheckTheSelectedView()
    {
        await OpenWorkbench("pos-dashboard", "?device=iphone-se");

        await Page.GetByRole(AriaRole.Tab, new() { Name = WorkbenchStrings.ChecksTab }).ClickAsync();
        await Button(WorkbenchStrings.RunChecks).ClickAsync();

        await Expect(Page.GetByText(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ThemeFindings, 0))).ToBeVisibleAsync();
        await Expect(Page.GetByText(new Regex($"^{Regex.Escape(WorkbenchStrings.AccessibilityFindings).Replace(@"\{0}", @"\d+")}$"))).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task TheFlowsTab_Should_AddTheSelectedViewToANewFlow()
    {
        var name = $"Checking out {Guid.NewGuid():N}";

        await OpenWorkbench("buildings", "?device=iphone-se&theme=dark");

        await Page.GetByRole(AriaRole.Tab, new() { Name = WorkbenchStrings.Flows }).ClickAsync();

        await Page.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.Flow }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = WorkbenchStrings.NewFlow, Exact = true }).ClickAsync();

        await Page.GetByLabel(WorkbenchStrings.NewFlowName, new() { Exact = true }).FillAsync(name);
        await Page.GetByLabel(WorkbenchStrings.ScreenNote, new() { Exact = true }).FillAsync("The list in dark");
        await Button(WorkbenchStrings.AddThisView).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.OpenBoard })).ToBeVisibleAsync();

        var flow = (await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetFlows(CancellationToken.None), flows => flows.Any(f => f.Name == name))).Single(f => f.Name == name);
        var screen = flow.Screens.Single();

        Assert.AreEqual("buildings", screen.PageKey);
        Assert.AreEqual("?theme=dark&device=iphone-se", screen.Query);
        Assert.AreEqual("The list in dark", screen.Note);
        Assert.IsTrue(screen.HasState, "The view's page state goes with the screen.");
    }
}
