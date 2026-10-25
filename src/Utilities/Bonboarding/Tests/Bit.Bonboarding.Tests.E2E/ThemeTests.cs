using Bit.Bonboarding.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bonboarding.Tests.E2E;

/// <summary>
/// The default stylesheet in a real cascade: the system theme follows the operating system, an application's
/// own palette still wins over it, and reduced motion is honoured.
/// </summary>
[TestClass]
public sealed class ThemeTests : BonboardingTest
{
    /// <summary>
    /// The demo restyles the card in demo.css; removing it leaves the library's own stylesheet alone. The link is
    /// fingerprinted (demo.{hash}.css), hence the pattern.
    /// </summary>
    private static async Task RemoveDemoStylesAsync(IPage page)
    {
        var removed = await page.EvaluateAsync<int>(
            """
            () => {
              const links = [...document.querySelectorAll('link[rel="stylesheet"]')]
                .filter(link => /(^|\/)demo\.([a-z0-9]+\.)?css/i.test(link.getAttribute('href') || ''));
              links.forEach(link => link.remove());
              return links.length;
            }
            """);

        Assert.AreEqual(1, removed, "The demo's stylesheet link was not found.");
    }

    private async Task<IPage> OpenTourAsync(ColorScheme scheme, bool withDemoStyles)
    {
        var options = DefaultContextOptions();
        options.ColorScheme = scheme;

        var page = await OpenAsync("/", options);
        if (withDemoStyles is false) await RemoveDemoStylesAsync(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).ClickAsync();
        await WaitForStepAsync(page, "Welcome aboard");
        return page;
    }

    private static Task<string> SurfaceColorAsync(IPage page) =>
        page.EvaluateAsync<string>("() => getComputedStyle(document.querySelector('.bit-bonboarding-popover')).backgroundColor");

    [TestMethod]
    public async Task The_system_theme_is_dark_on_a_dark_system()
    {
        var page = await OpenTourAsync(ColorScheme.Dark, withDemoStyles: false);

        Assert.AreEqual("system", await page.Locator(".bit-bonboarding-root").GetAttributeAsync("data-bonboarding-theme"));
        Assert.AreEqual("rgb(22, 24, 29)", await SurfaceColorAsync(page), "The dark surface, #16181d.");
    }

    [TestMethod]
    public async Task The_system_theme_is_light_on_a_light_system()
    {
        var page = await OpenTourAsync(ColorScheme.Light, withDemoStyles: false);

        Assert.AreEqual("rgb(255, 255, 255)", await SurfaceColorAsync(page));
    }

    [TestMethod]
    public async Task An_application_palette_on_the_root_still_wins_on_a_dark_system()
    {
        // The demo maps the system theme from its own switch, and declares its light palette on a plain
        // .bit-bonboarding-root. Switched to light on a dark system, its light card must stay light.
        var options = DefaultContextOptions();
        options.ColorScheme = ColorScheme.Dark;
        var page = await OpenAsync("/", options);

        // The site's own toggle: it follows the dark system until switched.
        Assert.AreEqual("light", await page.EvaluateAsync<string>("() => window.bitDemoTheme.toggle()"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).ClickAsync();
        await WaitForStepAsync(page, "Welcome aboard");

        Assert.AreEqual("rgb(255, 255, 255)", await SurfaceColorAsync(page), "The demo's own light surface.");
    }

    [TestMethod]
    public async Task Focus_stays_visible_in_forced_colors()
    {
        var options = DefaultContextOptions();
        options.ForcedColors = ForcedColors.Active;
        var page = await OpenAsync("/", options);

        if (!await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"))
        {
            Assert.Inconclusive($"{E2EEnvironment.Browser} cannot emulate forced colors under automation.");
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await WaitForStepAsync(page, "Welcome aboard");

        // Keyboard focus on the primary button. Forced colors do not paint the box-shadow ring, so it has to be an outline.
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("Tab");
        var outline = await page.EvaluateAsync<string>("() => { const s = getComputedStyle(document.activeElement); return s.outlineStyle + ' ' + s.outlineWidth; }");

        StringAssert.StartsWith(outline, "solid", $"The focused control's outline is '{outline}'.");
    }

    [TestMethod]
    public async Task Reduced_motion_takes_the_transitions_out()
    {
        var options = DefaultContextOptions();
        options.ReducedMotion = ReducedMotion.Reduce;
        var page = await OpenAsync("/", options);

        await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).ClickAsync();
        await WaitForStepAsync(page, "Welcome aboard");

        Assert.AreEqual("true", await page.Locator(".bit-bonboarding-root").GetAttributeAsync("data-bonboarding-reduced-motion"));
        var duration = await page.EvaluateAsync<string>("() => getComputedStyle(document.querySelector('.bit-bonboarding-popover')).transitionDuration");
        Assert.IsTrue(duration.Split(',').All(part => part.Trim() == "0s"), $"The popover still transitions: {duration}.");
    }
}
