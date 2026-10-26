using System.Buffers.Binary;

namespace Boilerplate.Tests.E2E.Features.DownloadSize;

[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebBlazorUIIconTrimmingTests
{
    private const int maxKeptIcons = 500;
    private const int maxFontSize = 150_000;

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.TodoAot, DisplayName = nameof(App.TodoAot))]
    [DataRow(App.TodoSmall, DisplayName = nameof(App.TodoSmall))]
    [DataRow(App.TodoOffline, DisplayName = nameof(App.TodoOffline))]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = nameof(App.AdminPanelWasmStandalone))]
    public async Task IconStylesheet_Should_OnlyCarryTheIconsTheAppNames(App app)
    {
        using var httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });

        var url = new Uri(new Uri(DeployedApps.AddressOf(app)), "_content/Bit.BlazorUI.Icons/styles/bit.blazorui.icons.css");

        using var response = await httpClient.GetAsync(url, TestContext.CancellationToken);
        var stylesheet = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{url} answered {(int)response.StatusCode}.");

        Assert.Contains(".bit-icon--ClearNight::before", stylesheet, $"{url} lacks the ClearNight icon of the theme toggle: the publish trimmed too much.");

        Assert.DoesNotContain(".bit-icon--12PointStar::before", stylesheet, $"{url} carries the 12PointStar icon, which nothing in the app names.");
        Assert.DoesNotContain(".bit-icon--Snowflake::before", stylesheet, $"{url} carries the Snowflake icon, which nothing in the app names.");

        var keptIcons = IconRule().Count(stylesheet);

        Assert.IsLessThan(maxKeptIcons, keptIcons,
            $"{url} keeps {keptIcons:N0} icons, close to the 2,313 of Bit.BlazorUI.Icons: the publish did not trim it.");
    }

    [TestMethod]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.TodoAot, DisplayName = nameof(App.TodoAot))]
    [DataRow(App.TodoSmall, DisplayName = nameof(App.TodoSmall))]
    [DataRow(App.TodoOffline, DisplayName = nameof(App.TodoOffline))]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = nameof(App.AdminPanelWasmStandalone))]
    public async Task IconFont_Should_OnlyCarryTheGlyphsTheAppNames(App app)
    {
        using var httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });

        var stylesheetUrl = new Uri(new Uri(DeployedApps.AddressOf(app)), "_content/Bit.BlazorUI.Icons/styles/bit.blazorui.icons.css");
        var stylesheet = await httpClient.GetStringAsync(stylesheetUrl, TestContext.CancellationToken);

        var fontPath = FontPath().Match(stylesheet);

        Assert.IsTrue(fontPath.Success, $"{stylesheetUrl} points at no .woff2 font.");

        var url = new Uri(stylesheetUrl, fontPath.Groups["path"].Value);

        using var response = await httpClient.GetAsync(url, TestContext.CancellationToken);
        var font = await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{url} answered {(int)response.StatusCode}.");
        Assert.IsTrue(font.AsSpan().StartsWith("wOF2"u8), $"{url} is not a WOFF2 font.");

        var fontSize = BinaryPrimitives.ReadInt32BigEndian(font.AsSpan(16, 4));

        Assert.IsLessThan(maxFontSize, fontSize,
            $"{url} unpacks to {fontSize:N0} bytes, against 525,660 for Bit.BlazorUI.Icons' whole font: the publish did not cut it down to the icons the app names.");
    }

    [GeneratedRegex(@"\.bit-icon--[A-Za-z0-9]+::before")]
    private static partial Regex IconRule();

    [GeneratedRegex(@"url\(\s*[""']?(?<path>[^""')]+\.woff2)")]
    private static partial Regex FontPath();
}
