using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the colors a BitNav draws an item's parts in while it is hovered, pressed or selected.
/// A theme that gives a state a vivid fill moves the text, the icon and the description with it through the state's
/// own variables; an explicit Accent or Color still wins over them, a surface Accent included, whose tint they were
/// never set for. The stylesheet tests pin the source; these read what each engine resolves the var() chains to.
///
/// Run them with BROWSER=firefox and BROWSER=webkit as well.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitNavStateColorsBrowserTests
public class BitNavStateColorsBrowserTests : PerformanceTestBase
{
    internal const string PageUrl = "/regression/nav-state-colors";

    internal const string RestIcon = "rgb(10, 20, 30)";
    internal const string RestDescription = "rgb(40, 50, 60)";
    private const string HoverText = "rgb(255, 255, 255)";
    private const string HoverIcon = "rgb(255, 255, 1)";
    private const string HoverDescription = "rgb(255, 255, 2)";
    private const string SelectedText = "rgb(255, 255, 3)";
    private const string SelectedIcon = "rgb(255, 255, 4)";
    private const string SelectedDescription = "rgb(255, 255, 5)";

    [TestMethod]
    public async Task BitNav_Selected_TakesTheThemesSelectedColors()
    {
        await Open(Page);

        await AssertParts(Page, "theme-default", Selected, SelectedText, SelectedIcon, SelectedDescription);
    }

    [TestMethod]
    public async Task BitNav_Selected_UnderASurfaceAccent_KeepsTheIconAndDescriptionAtRest()
    {
        await Open(Page);

        // The theme's selected colors were set for its vivid fill, not for the accent's tint.
        await AssertParts(Page, "theme-surface", Selected, await Resolve(Page, "theme-surface", "var(--bit-nav-clr-text)"), RestIcon, RestDescription);
    }

    [TestMethod]
    public async Task BitNav_Selected_UnderASemanticAccent_DrawsEveryPartInTheAccentsOnColor()
    {
        await Open(Page);

        var on = await Resolve(Page, "theme-semantic", "var(--bit-nav-clr-text)");
        await AssertParts(Page, "theme-semantic", Selected, on, on, on);
    }

    [TestMethod]
    public async Task BitNav_Selected_UnderAnExplicitColor_DrawsTheIconInThatColor()
    {
        await Open(Page);

        await AssertParts(Page, "theme-color", Selected, SelectedText, await Resolve(Page, "theme-color", "var(--bit-nav-clr)"), SelectedDescription);
    }

    [TestMethod]
    public async Task BitNav_Selected_WithNoTheme_LooksAsItDid()
    {
        await Open(Page);

        await AssertParts(Page, "plain", Selected,
            await Resolve(Page, "plain", "var(--bit-clr-fg-pri)"),
            await Resolve(Page, "plain", "var(--bit-clr-pri)"),
            await Resolve(Page, "plain", "var(--bit-clr-fg-ter)"));
    }

    [TestMethod]
    public async Task BitNav_Hover_TakesTheThemesHoveredColors()
    {
        await Open(Page);

        await Hover(Page, "theme-default", Other);

        await AssertParts(Page, "theme-default", Other, HoverText, HoverIcon, HoverDescription);
    }

    [TestMethod]
    public async Task BitNav_Hover_UnderASurfaceAccent_KeepsTheIconAndDescriptionAtRest()
    {
        await Open(Page);

        await Hover(Page, "theme-surface", Other);

        await AssertParts(Page, "theme-surface", Other, await Resolve(Page, "theme-surface", "var(--bit-nav-clr-text)"), RestIcon, RestDescription);
    }

    [TestMethod]
    public async Task BitNav_Hover_UnderASemanticAccent_DrawsEveryPartInTheAccentsOnColor()
    {
        await Open(Page);

        await Hover(Page, "theme-semantic", Other);

        var on = await Resolve(Page, "theme-semantic", "var(--bit-nav-clr-text)");
        await AssertParts(Page, "theme-semantic", Other, on, on, on);
    }

    [TestMethod]
    public async Task BitNav_Hover_UnderAnExplicitColor_DrawsTheIconInThatColor()
    {
        await Open(Page);

        await Hover(Page, "theme-color", Other);

        await AssertParts(Page, "theme-color", Other, HoverText, await Resolve(Page, "theme-color", "var(--bit-nav-clr)"), HoverDescription);
    }

    [TestMethod]
    public async Task BitNav_Hover_EndsBackInTheColorsAtRest()
    {
        await Open(Page);

        await Hover(Page, "theme-default", Other);
        await AssertParts(Page, "theme-default", Other, HoverText, HoverIcon, HoverDescription);

        await Page.Mouse.MoveAsync(0, 0);
        await Page.WaitForFunctionAsync("selector => !document.querySelector(selector).matches(':hover')", Item("theme-default", Other));

        await AssertParts(Page, "theme-default", Other, await Resolve(Page, "theme-default", "var(--bit-clr-fg-pri)"), RestIcon, RestDescription);
    }

    [TestMethod]
    public async Task BitNav_PressedWithAMouse_FallsBackOnTheHoveredColors()
    {
        await Open(Page);
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => matchMedia('(hover: hover)').matches"), "The desktop context cannot hover.");

        await Press(Page, "theme-default", Other);

        await AssertParts(Page, "theme-default", Other, HoverText, HoverIcon, HoverDescription);
        await Page.Mouse.UpAsync();
    }

    [TestMethod]
    public async Task BitNav_PressedWithAMouse_TakesThePressedColorsWhereTheThemeSetsThem()
    {
        await Open(Page);

        await Press(Page, "theme-pressed", Other);

        await AssertParts(Page, "theme-pressed", Other, "rgb(255, 255, 6)", "rgb(255, 255, 7)", "rgb(255, 255, 8)");
        await Page.Mouse.UpAsync();
    }

    [TestMethod]
    public async Task BitNav_PressedWithAMouse_UnderASurfaceAccent_KeepsTheIconAndDescriptionAtRest()
    {
        await Open(Page);

        await Press(Page, "theme-surface", Other);

        await AssertParts(Page, "theme-surface", Other, await Resolve(Page, "theme-surface", "var(--bit-nav-clr-text)"), RestIcon, RestDescription);
        await Page.Mouse.UpAsync();
    }



    internal const string Selected = ".bit-nav-sel";
    internal const string Other = ".bit-nav-ict:not(.bit-nav-sel)";

    internal static string Item(string section, string item) => $"#{section} {item}";

    internal static async Task Open(IPage page)
    {
        await page.GotoAsync($"{BaseUrl}{PageUrl}");
        await page.WaitForFunctionAsync("() => document.getElementById('status')?.innerText === 'Ready'");
        await Expect(page.Locator(Item("theme-default", Selected))).ToHaveCountAsync(1);

        // The parts move to a state's colors through a short transition, and until the next frame a running one still
        // reports the color at rest - which a polled assertion of a part kept at rest would take for the answer, however
        // short the transition (reduced motion's collapsed tokens included). With none, a state's colors are computed
        // the moment it applies.
        await page.AddStyleTagAsync(new() { Content = ".bit-nav, .bit-nav * { transition: none !important; }" });
        await Expect(page.Locator($"{Item("theme-default", Other)} .bit-nav-iic").First).ToHaveCSSAsync("transition-property", "none");
    }

    /// <summary>Moves the pointer over an item and waits until it is :hover, so its colors are read in that state.</summary>
    internal static async Task Hover(IPage page, string section, string item)
    {
        await page.HoverAsync(Item(section, item));
        await WaitForState(page, section, item, ":hover");
    }

    /// <summary>Holds the primary button down over an item, so it stays :active while the colors are read.</summary>
    internal static async Task Press(IPage page, string section, string item)
    {
        await page.HoverAsync(Item(section, item));
        await page.Mouse.DownAsync();
        await WaitForState(page, section, item, ":active");
    }

    private static Task WaitForState(IPage page, string section, string item, string state)
    {
        return page.WaitForFunctionAsync("([selector, state]) => document.querySelector(selector)?.matches(state)",
            new[] { Item(section, item), state }, new() { Timeout = 5000 });
    }

    /// <summary>What a color expression resolves to inside the root of a section's nav, its private variables included.</summary>
    internal static Task<string> Resolve(IPage page, string section, string expression)
    {
        return page.EvaluateAsync<string>(@"([selector, expression]) => {
            const probe = document.createElement('span');
            probe.style.color = expression;
            document.querySelector(selector).appendChild(probe);
            const color = getComputedStyle(probe).color;
            probe.remove();
            return color;
        }", new[] { $"#{section} .bit-nav", expression });
    }

    /// <summary>
    /// The colors the item's text, icon and description are drawn in. Expect polls, so the short color transition of
    /// a state change has run out before a value counts as wrong.
    /// </summary>
    internal static async Task AssertParts(IPage page, string section, string item, string text, string icon, string description)
    {
        var container = page.Locator(Item(section, item)).First;

        await Expect(container.Locator(".bit-nav-itm").First).ToHaveCSSAsync("color", text);
        await Expect(container.Locator(".bit-nav-iic").First).ToHaveCSSAsync("color", icon);
        await Expect(container.Locator(".bit-nav-des").First).ToHaveCSSAsync("color", description);
    }
}

/// <summary>
/// The touch-screen half of <see cref="BitNavStateColorsBrowserTests"/>: with no hover a press falls back on the colors
/// at rest, not on the hovered ones a theme set for a fill the tap never shows. A class of its own because the touch
/// screen is a property of the browser context, not of the page. An engine that still reports a pointer that can hover
/// with touch on is inconclusive rather than failing, since it would be testing the desktop rule again.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser]
public class BitNavStateColorsTouchBrowserTests : PerformanceTestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        HasTouch = true,
        IsMobile = (System.Environment.GetEnvironmentVariable("BROWSER") ?? "chromium").ToLowerInvariant() is not "firefox",
        ViewportSize = new() { Width = 412, Height = 800 }
    };

    [TestMethod]
    public async Task BitNav_PressedOnATouchScreen_FallsBackOnTheColorsAtRest()
    {
        await BitNavStateColorsBrowserTests.Open(Page);

        if (await Page.EvaluateAsync<bool>("() => matchMedia('(hover: hover)').matches"))
        {
            Assert.Inconclusive("This engine reports a pointer that can hover even with touch on.");
        }

        await BitNavStateColorsBrowserTests.Press(Page, "theme-default", BitNavStateColorsBrowserTests.Other);

        await BitNavStateColorsBrowserTests.AssertParts(Page, "theme-default", BitNavStateColorsBrowserTests.Other,
            await BitNavStateColorsBrowserTests.Resolve(Page, "theme-default", "var(--bit-clr-fg-pri)"),
            BitNavStateColorsBrowserTests.RestIcon,
            BitNavStateColorsBrowserTests.RestDescription);
        await Page.Mouse.UpAsync();
    }

    [TestMethod]
    public async Task BitNav_PressedOnATouchScreen_TakesThePressedColorsWhereTheThemeSetsThem()
    {
        await BitNavStateColorsBrowserTests.Open(Page);

        if (await Page.EvaluateAsync<bool>("() => matchMedia('(hover: hover)').matches"))
        {
            Assert.Inconclusive("This engine reports a pointer that can hover even with touch on.");
        }

        await BitNavStateColorsBrowserTests.Press(Page, "theme-pressed", BitNavStateColorsBrowserTests.Other);

        await BitNavStateColorsBrowserTests.AssertParts(Page, "theme-pressed", BitNavStateColorsBrowserTests.Other,
            "rgb(255, 255, 6)", "rgb(255, 255, 7)", "rgb(255, 255, 8)");
        await Page.Mouse.UpAsync();
    }
}
