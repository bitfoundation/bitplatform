using Bit.Borm.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Borm.Tests.E2E;

/// <summary>
/// The stylesheet as the browser resolves it. On the demo, the forms wear the site's theme: the site follows the
/// operating system's colour scheme until its toggle is used, keeps the choice, and the forms follow the site - Borm's
/// tokens re-tinted onto the site's (wwwroot/demo.css). On its own, the shipped stylesheet follows the operating system,
/// and a <c>data-theme</c> on an ancestor overrides it. Then the grid that collapses to one column on a narrow screen,
/// and the transitions that reduced motion turns off. Every value is read back with getComputedStyle or
/// getBoundingClientRect, never from the markup.
/// </summary>
[TestClass]
public sealed class ThemingAndLayoutTests : BormTest
{
    // The site's surfaces and text (app.css, --bit-clr-bg-pri / --bit-clr-fg-pri / --bit-clr-err), which the forms are
    // re-tinted to.
    private const string SiteLightSurface = "rgb(255, 255, 255)";
    private const string SiteDarkSurface = "rgb(15, 19, 24)";
    private const string SiteDarkText = "rgb(230, 236, 242)";
    private const string SiteDarkError = "rgb(230, 122, 115)";

    // The shipped stylesheet's own palettes (bit-borm.css), which a page without the demo's re-tint gets.
    private const string LightSurface = "rgb(255, 255, 255)";
    private const string DarkSurface = "rgb(17, 24, 39)";

    [TestMethod]
    public async Task A_dark_colour_scheme_preference_puts_the_site_and_its_forms_in_the_dark_theme_and_back()
    {
        var page = await OpenAsync("/builder", DefaultContextOptions(ColorScheme.Dark));
        var form = Form(page);
        var input = Control(page, "FullName");

        Assert.AreEqual("dark", await SiteThemeAsync(page), "the site does not follow the dark preference");
        Assert.AreEqual("#0F1318", await ComputedAsync(form, "--bit-borm-surface"), "the dark surface token");
        Assert.AreEqual("#E6ECF2", await ComputedAsync(form, "--bit-borm-text"), "the dark text token");
        Assert.AreEqual("dark", await ComputedAsync(form, "color-scheme"), "the browser-drawn parts of the controls stay light");
        Assert.AreEqual(SiteDarkSurface, await ComputedAsync(input, "background-color"), "the dark input background");
        Assert.AreEqual(SiteDarkText, await ComputedAsync(form, "color"), "the dark form text");

        // The error colour is lighter on the dark surface, where the light theme's red would be too dim to read.
        await SubmitButton(page).ClickAsync();
        await Expect(Errors(page, "Email")).ToHaveCountAsync(1);
        Assert.AreEqual(SiteDarkError, await ComputedAsync(Control(page, "Email"), "border-top-color"), "the dark error border");

        // The preference changing under an open page restyles it on the spot: no reload, no re-render. The browser
        // reports the change to the page's media query listener with its next frame, not within the emulation call.
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
        await EventuallyAsync(async () => await SiteThemeAsync(page) == "light", "the site followed the preference back to light");
        Assert.AreEqual("#FFFFFF", await ComputedAsync(form, "--bit-borm-surface"), "the light surface token");
        Assert.AreEqual(SiteLightSurface, await ComputedAsync(input, "background-color"), "the light input background");
        Assert.AreEqual(ErrorColour, await ComputedAsync(Control(page, "Email"), "border-top-color"), "the light error border");
        Assert.AreEqual("light", await ComputedAsync(form, "color-scheme"));
    }

    [TestMethod]
    public async Task The_site_toggle_wins_over_the_preference_is_kept_and_hands_back_to_it()
    {
        // A dark site on a light system...
        var page = await OpenAsync("/builder");
        var input = Control(page, "FullName");
        Assert.AreEqual(SiteLightSurface, await ComputedAsync(input, "background-color"));

        await ThemeToggle(page).ClickAsync();
        await EventuallyAsync(async () => await SiteThemeAsync(page) == "dark", "the toggle put the site in the dark theme");
        Assert.AreEqual(SiteDarkSurface, await ComputedAsync(input, "background-color"), "the toggle's dark under a light preference");
        Assert.AreEqual("dark", await ComputedAsync(Form(page), "color-scheme"));

        // ...and a light site on a dark one, which a page loaded afterwards keeps.
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        await ThemeToggle(page).ClickAsync();
        await EventuallyAsync(async () => await SiteThemeAsync(page) == "light", "the toggle put the site back in the light theme");
        Assert.AreEqual(SiteLightSurface, await ComputedAsync(input, "background-color"), "the toggle's light under a dark preference");
        Assert.AreEqual("light", await ComputedAsync(Form(page), "color-scheme"));

        await GotoAsync(page, "/layout");
        await WaitForInteractiveAsync(page);
        Assert.AreEqual("light", await SiteThemeAsync(page), "the choice did not survive a page load");
        Assert.AreEqual(SiteLightSurface, await ComputedAsync(Control(page, "FirstName"), "background-color"), "the kept light under a dark preference");

        // Toggling to what the system already asks for hands the choice back: the preference rules again.
        await ThemeToggle(page).ClickAsync();
        await EventuallyAsync(async () => await SiteThemeAsync(page) == "dark", "the toggle followed the preference again");
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
        await EventuallyAsync(async () => await SiteThemeAsync(page) == "light", "the site followed the light preference once the choice was handed back");
        Assert.AreEqual(SiteLightSurface, await ComputedAsync(Control(page, "FirstName"), "background-color"));
    }

    [TestMethod]
    public async Task On_its_own_the_stylesheet_follows_the_preference_and_a_data_theme_on_an_ancestor_wins_over_it()
    {
        // The library's colour modes, on a page of the demo's origin that links bit-borm.css and nothing else: the demo
        // re-tints its own forms to the site's theme, so this is where the shipped palettes are still what is drawn.
        var page = await OpenAsync("/builder");
        var stylesheet = await page.EvaluateAsync<string>("() => document.querySelector('link[href*=\"_content/Bit.Borm/bit-borm\"]').href");
        await page.RouteAsync(BaseUrl + "/stylesheet-fixture", route => route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 200,
            ContentType = "text/html",
            Body = $"""
                    <!DOCTYPE html>
                    <html><head><link rel="stylesheet" href="{stylesheet}" /></head>
                    <body><div id="host"><form class="bit-borm-form">
                        <input id="plain" class="bit-borm-input" />
                        <input id="invalid" class="bit-borm-input bit-borm-input--invalid" />
                    </form></div></body></html>
                    """,
        }));
        await page.GotoAsync(BaseUrl + "/stylesheet-fixture", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var form = page.Locator("form.bit-borm-form");
        var input = page.Locator("#plain");
        var host = page.Locator("#host");
        Assert.AreEqual("#ffffff", await ComputedAsync(form, "--bit-borm-surface"), "the light surface token");
        Assert.AreEqual(LightSurface, await ComputedAsync(input, "background-color"));

        // The operating system's dark preference switches the tokens and the controls with them.
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        Assert.AreEqual("#111827", await ComputedAsync(form, "--bit-borm-surface"), "the dark surface token");
        Assert.AreEqual("#f9fafb", await ComputedAsync(form, "--bit-borm-text"), "the dark text token");
        Assert.AreEqual("dark", await ComputedAsync(form, "color-scheme"), "the browser-drawn parts of the controls stay light");
        Assert.AreEqual(DarkSurface, await ComputedAsync(input, "background-color"), "the dark input background");
        Assert.AreEqual("rgb(249, 250, 251)", await ComputedAsync(form, "color"), "the dark form text");
        Assert.AreEqual("rgb(248, 113, 113)", await ComputedAsync(page.Locator("#invalid"), "border-top-color"), "the dark error border");

        // A light site on a dark system...
        await host.EvaluateAsync("e => e.dataset.theme = 'light'");
        Assert.AreEqual(LightSurface, await ComputedAsync(input, "background-color"), "data-theme=light under a dark preference");
        Assert.AreEqual("light", await ComputedAsync(form, "color-scheme"));
        Assert.AreEqual("rgb(220, 38, 38)", await ComputedAsync(page.Locator("#invalid"), "border-top-color"), "the light error border");

        // ...and a dark site on a light one.
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
        await host.EvaluateAsync("e => e.dataset.theme = 'dark'");
        Assert.AreEqual(DarkSurface, await ComputedAsync(input, "background-color"), "data-theme=dark under a light preference");

        // Without it, the preference rules again.
        await host.EvaluateAsync("e => delete e.dataset.theme");
        Assert.AreEqual(LightSurface, await ComputedAsync(input, "background-color"), "the light preference once data-theme is gone");
    }

    [TestMethod]
    public async Task Two_columns_side_by_side_collapse_into_one_on_a_narrow_screen()
    {
        var wide = await OpenAsync("/layout");
        var first = await BoxOfAsync(Control(wide, "FirstName"));
        var last = await BoxOfAsync(Control(wide, "LastName"));
        Assert.AreEqual(first.Y, last.Y, 1, "first and last name are not on one row");
        Assert.IsTrue(last.X > first.X + first.Width, "the last name is not to the right of the first");

        // A field spanning both columns is as wide as the two of them together.
        var bio = await BoxOfAsync(Control(wide, "Bio"));
        Assert.AreEqual(last.X + last.Width - first.X, bio.Width, 2, "the bio does not span both columns");

        var narrow = await OpenAsync("/layout", DefaultContextOptions(width: 600));
        first = await BoxOfAsync(Control(narrow, "FirstName"));
        last = await BoxOfAsync(Control(narrow, "LastName"));
        Assert.IsTrue(last.Y > first.Y + first.Height, "on a narrow screen the last name is not below the first");
        Assert.AreEqual(first.X, last.X, 1, "on a narrow screen the fields are not in one column");
        Assert.AreEqual(first.Width, last.Width, 1, "on a narrow screen the fields are not the same width");
        Assert.IsTrue(await narrow.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= document.documentElement.clientWidth"),
            "the page overflows a 600px screen sideways");
    }

    [TestMethod]
    public async Task Reduced_motion_turns_the_transitions_off()
    {
        var reduced = await OpenAsync("/builder");
        Assert.AreEqual("0s", await ComputedAsync(Control(reduced, "FullName"), "transition-duration"));

        var options = DefaultContextOptions();
        options.ReducedMotion = ReducedMotion.NoPreference;
        var moving = await OpenAsync("/builder", options);
        Assert.AreEqual("0.15s, 0.15s", await ComputedAsync(Control(moving, "FullName"), "transition-duration"));
    }

    /// <summary>The site's theme, as theme.js has put it on the root element.</summary>
    private static Task<string> SiteThemeAsync(IPage page) =>
        page.EvaluateAsync<string>("() => document.documentElement.getAttribute('bit-theme') ?? ''");

    /// <summary>The header's light/dark switch.</summary>
    private static ILocator ThemeToggle(IPage page) => page.Locator("header.bb-header .bb-theme-toggle");

    private static async Task<LocatorBoundingBoxResult> BoxOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, $"{locator} is not laid out");
        return box;
    }
}
