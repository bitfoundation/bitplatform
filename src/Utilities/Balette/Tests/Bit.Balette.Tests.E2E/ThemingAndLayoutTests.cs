using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// The palette as the browser computes it: the colours the theme and the accent token resolve to, the detail pane
/// coming and going, and the full-screen layout on a phone-sized viewport. Every value is read back with
/// getComputedStyle or getBoundingClientRect, never from the markup.
/// </summary>
[TestClass]
public sealed class ThemingAndLayoutTests : BaletteTest
{
    private const string LightBackground = "rgb(255, 255, 255)";
    private const string DarkBackground = "rgb(23, 24, 28)";

    [TestMethod]
    public async Task The_theme_buttons_change_the_colours_the_dialog_is_painted_with()
    {
        var page = await OpenAsync("/theming");

        // The palette's own Theme parameter: a dark palette over the light page.
        await ThemeButton(page, "Palette theme", "Dark").ClickAsync();
        Assert.AreEqual(DarkBackground, await OpenAndReadAsync(page, "backgroundColor"));
        Assert.AreEqual("rgb(243, 244, 246)", await OpenAndReadAsync(page, "color"));
        await Assertions.Expect(Dialog(page)).ToHaveAttributeAsync("data-theme", "dark");

        await ThemeButton(page, "Palette theme", "Light").ClickAsync();
        Assert.AreEqual(LightBackground, await OpenAndReadAsync(page, "backgroundColor"));

        // Back to following the page, and the page turned dark: <html data-theme> restyles the palette with it.
        await ThemeButton(page, "Palette theme", "Follows the page").ClickAsync();
        await ThemeButton(page, "Site theme", "Dark").ClickAsync();
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", "dark");
        Assert.AreEqual(DarkBackground, await OpenAndReadAsync(page, "backgroundColor"));
    }

    [TestMethod]
    public async Task The_accent_token_recolours_the_mode_chip_and_the_highlights()
    {
        var page = await OpenAsync("/theming");
        await page.Locator(".topbar-modes button[title='Commands mode']").ClickAsync();
        await ExpectOpenAsync(page);
        await TypeAsync(page, "ren");
        var defaultMark = await ComputedAsync(page, "[data-balette='mark']", "backgroundColor");
        Assert.AreEqual("rgb(79, 70, 229)", await ComputedAsync(page, "[data-balette='mode']", "color"), "the default accent is not the stylesheet's");
        await page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Teal", Exact = true }).ClickAsync();
        await page.Locator(".topbar-modes button[title='Commands mode']").ClickAsync();
        await ExpectOpenAsync(page);
        await TypeAsync(page, "ren");
        await Assertions.Expect(page.Locator("[data-balette='mark']").First).ToBeVisibleAsync();

        Assert.AreEqual("rgb(13, 148, 136)", await ComputedAsync(page, "[data-balette='mode']", "color"), "the mode chip did not take the accent");
        var tealMark = await ComputedAsync(page, "[data-balette='mark']", "backgroundColor");
        TestContext.WriteLine($"mark background: default {defaultMark}, teal {tealMark}");
        Assert.AreNotEqual(defaultMark, tealMark, "the highlight did not follow the accent");
    }

    [TestMethod]
    public async Task The_detail_pane_comes_and_goes_with_its_toggle()
    {
        var page = await OpenAsync("/theming");

        await OpenPaletteAsync(page);
        await Assertions.Expect(Detail(page)).ToBeVisibleAsync();
        await Assertions.Expect(Detail(page)).ToContainTextAsync((await ItemIdsAsync(page))[0]);
        AssertInRange(await DialogWidthAsync(page), 900, 940, "the dialog's width with a detail pane");
        await page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync(page);

        // The checkbox on the page...
        await page.Locator("label.field", new() { HasTextString = "Detail pane" }).Locator("input").UncheckAsync();
        await OpenPaletteAsync(page);
        await Assertions.Expect(Detail(page)).ToHaveCountAsync(0);
        await Assertions.Expect(Dialog(page)).ToHaveAttributeAsync("data-has-detail", "false");
        AssertInRange(await DialogWidthAsync(page), 620, 660, "the dialog's width without a detail pane");

        // ...and the palette's own toggle, which changes the very palette it is listed in while it stays open.
        await TypeAsync(page, "show detail");
        await ExpectFirstItemAsync(page, "toggle:detail");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Detail(page)).ToBeVisibleAsync();
        await Assertions.Expect(Dialog(page)).ToBeVisibleAsync();
        await Assertions.Expect(Item(page, "toggle:detail").Locator(".bit-balette-toggle")).ToHaveAttributeAsync("data-on", "true");
    }

    [TestMethod]
    public async Task On_a_phone_the_dialog_fills_the_screen_and_nothing_overflows_sideways()
    {
        var page = await OpenAsync("/", new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            DeviceScaleFactor = 1,
            IsMobile = true,
            HasTouch = true,
            ColorScheme = ColorScheme.Light,
            ReducedMotion = ReducedMotion.Reduce,
        });
        Assert.AreEqual(0d, await HorizontalOverflowAsync(page), "the page overflows the phone's width before the palette opens");

        await page.Locator("[data-balette='trigger']").TapAsync();
        await ExpectOpenAsync(page);
        await TypeAsync(page, "a");

        var box = await page.EvaluateAsync<double[]>(
            "() => { const r = document.querySelector('dialog[data-balette=\"dialog\"]').getBoundingClientRect(); return [r.left, r.top, r.width, r.height]; }");
        TestContext.WriteLine($"dialog box: {string.Join(", ", box)}");
        AssertInRange(box[0], -0.5, 0.5, "the dialog's left edge");
        AssertInRange(box[1], -0.5, 0.5, "the dialog's top edge");
        AssertInRange(box[2], 389.5, 390.5, "the dialog's width");
        AssertInRange(box[3], 843.5, 844.5, "the dialog's height");

        // Below 640 px the detail pane is hidden rather than squeezed next to the list.
        await Assertions.Expect(Detail(page)).ToBeHiddenAsync();
        Assert.AreEqual(0d, await HorizontalOverflowAsync(page), "the page overflows the phone's width with the palette open");
        Assert.IsTrue(await page.EvaluateAsync<bool>(
            "() => { const d = document.querySelector('dialog[data-balette=\"dialog\"]'); return d.scrollWidth <= d.clientWidth; }"),
            "the dialog's content is wider than the dialog");
    }

    private static ILocator ThemeButton(IPage page, string group, string name) =>
        page.GetByRole(AriaRole.Group, new() { Name = group, Exact = true }).GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    /// <summary>Opens the palette with the page's button, reads a computed property of the dialog, and closes it again.</summary>
    private static async Task<string> OpenAndReadAsync(IPage page, string property)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Open the palette", Exact = true }).ClickAsync();
        await ExpectOpenAsync(page);
        var value = await ComputedAsync(page, "dialog[data-balette='dialog']", property);
        await page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync(page);
        return value;
    }

    private static Task<string> ComputedAsync(IPage page, string selector, string property) =>
        page.EvaluateAsync<string>("([s, p]) => getComputedStyle(document.querySelector(s))[p]", new[] { selector, property });

    private static Task<double> DialogWidthAsync(IPage page) =>
        page.EvaluateAsync<double>("() => document.querySelector('dialog[data-balette=\"dialog\"]').getBoundingClientRect().width");

    /// <summary>How far the document is wider than the viewport, in CSS pixels.</summary>
    private static Task<double> HorizontalOverflowAsync(IPage page) =>
        page.EvaluateAsync<double>("() => Math.max(0, document.documentElement.scrollWidth - document.documentElement.clientWidth)");
}
