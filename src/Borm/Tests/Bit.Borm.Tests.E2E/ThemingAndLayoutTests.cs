using Bit.Borm.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Borm.Tests.E2E;

/// <summary>
/// The stylesheet as the browser resolves it: the dark theme following the operating system, a <c>data-theme</c>
/// on an ancestor overriding it, the grid that collapses to one column on a narrow screen, and the transitions that
/// reduced motion turns off. Every value is read back with getComputedStyle or getBoundingClientRect, never from the
/// markup.
/// </summary>
[TestClass]
public sealed class ThemingAndLayoutTests : BormTest
{
    private const string LightSurface = "rgb(255, 255, 255)";
    private const string DarkSurface = "rgb(17, 24, 39)";

    [TestMethod]
    public async Task A_dark_colour_scheme_preference_switches_the_form_tokens_and_the_controls_with_them()
    {
        var page = await OpenAsync("/builder", DefaultContextOptions(ColorScheme.Dark));
        var form = Form(page);
        var input = Control(page, "FullName");

        Assert.AreEqual("#111827", await ComputedAsync(form, "--bit-borm-surface"), "the dark surface token");
        Assert.AreEqual("#f9fafb", await ComputedAsync(form, "--bit-borm-text"), "the dark text token");
        Assert.AreEqual("dark", await ComputedAsync(form, "color-scheme"), "the browser-drawn parts of the controls stay light");
        Assert.AreEqual(DarkSurface, await ComputedAsync(input, "background-color"), "the dark input background");
        Assert.AreEqual("rgb(249, 250, 251)", await ComputedAsync(form, "color"), "the dark form text");

        // The error colour is lighter on the dark surface, where the light theme's red would be too dim to read.
        await SubmitButton(page).ClickAsync();
        await Expect(Errors(page, "Email")).ToHaveCountAsync(1);
        Assert.AreEqual("rgb(248, 113, 113)", await ComputedAsync(Control(page, "Email"), "border-top-color"), "the dark error border");

        // The preference changing under an open page restyles it on the spot: no reload, no re-render.
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
        Assert.AreEqual("#ffffff", await ComputedAsync(form, "--bit-borm-surface"), "the light surface token");
        Assert.AreEqual(LightSurface, await ComputedAsync(input, "background-color"), "the light input background");
        Assert.AreEqual(ErrorColour, await ComputedAsync(Control(page, "Email"), "border-top-color"), "the light error border");
        Assert.AreEqual("light", await ComputedAsync(form, "color-scheme"));
    }

    [TestMethod]
    public async Task A_data_theme_on_an_ancestor_wins_over_the_operating_system_preference()
    {
        // A dark site on a light system...
        var page = await OpenAsync("/builder");
        var input = Control(page, "FullName");
        Assert.AreEqual(LightSurface, await ComputedAsync(input, "background-color"));

        await page.Locator(".app-shell").EvaluateAsync("e => e.dataset.theme = 'dark'");
        Assert.AreEqual(DarkSurface, await ComputedAsync(input, "background-color"), "data-theme=dark under a light preference");

        // ...and a light site on a dark one.
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        await page.Locator(".app-shell").EvaluateAsync("e => e.dataset.theme = 'light'");
        Assert.AreEqual(LightSurface, await ComputedAsync(input, "background-color"), "data-theme=light under a dark preference");
        Assert.AreEqual("light", await ComputedAsync(Form(page), "color-scheme"));

        // Without it, the preference rules again.
        await page.Locator(".app-shell").EvaluateAsync("e => delete e.dataset.theme");
        Assert.AreEqual(DarkSurface, await ComputedAsync(input, "background-color"), "the dark preference once data-theme is gone");
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

    private static async Task<LocatorBoundingBoxResult> BoxOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, $"{locator} is not laid out");
        return box;
    }
}
