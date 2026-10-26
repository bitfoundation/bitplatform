using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// The palette audited by axe-core in each of its states - the root list, search results with highlights, a nested
/// page, a multi-select page, a prompt - in light and dark, painted by the demo's theme and by the library's own
/// defaults; then what axe cannot check: forced colours, right-to-left text and the arrows that follow it, and the focus
/// staying in the input when a row is clicked.
/// </summary>
/// <remarks>An automated audit finds a part of the problems; it is not a claim of WCAG conformance.</remarks>
[TestClass]
public sealed class AccessibilityTests : BaletteTest
{
    private static async Task ExpectNoViolationsAsync(IPage page, string state)
    {
        var violations = await Axe.ViolationsAsync(Dialog(page));
        Assert.IsEmpty(violations, $"axe-core found violations in {state}:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>Drops the demo's stylesheet, which re-themes the palette, so the library's own default colours are what is audited.</summary>
    private static Task UseLibraryDefaultsAsync(IPage page) => page.EvaluateAsync(
        "() => document.querySelectorAll('link[rel=stylesheet][href*=\"demo\"]').forEach(l => l.remove())");

    private static async Task StepBackAndSearchAsync(IPage page, string restoredQuery, string query)
    {
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Crumbs(page)).ToHaveCountAsync(0);
        await Assertions.Expect(Input(page)).ToHaveValueAsync(restoredQuery);
        for (var i = 0; i < restoredQuery.Length; i++)
        {
            await page.Keyboard.PressAsync("Backspace");
        }
        await TypeAsync(page, query);
    }

    private async Task AuditEveryStateAsync(ColorScheme scheme, bool libraryDefaults)
    {
        var options = DefaultContextOptions();
        options.ColorScheme = scheme;
        var page = await OpenAsync("/", options);
        if (scheme == ColorScheme.Dark)
        {
            // The site follows its own toggle rather than the OS; the palette's Theme command follows the site.
            await page.EvaluateAsync("() => document.documentElement.setAttribute('bit-theme', 'dark')");
        }

        if (libraryDefaults)
        {
            await UseLibraryDefaultsAsync(page);
        }

        var what = $"{scheme} {(libraryDefaults ? "library defaults" : "demo theme")}";

        await OpenPaletteAsync(page);
        await ExpectNoViolationsAsync(page, $"the root list ({what})");

        await TypeAsync(page, "set");
        await Assertions.Expect(page.Locator("[data-balette='mark']").First).ToBeVisibleAsync();
        await ExpectNoViolationsAsync(page, $"search results ({what})");

        await Input(page).FillAsync("theme");
        await ExpectFirstItemAsync(page, "preferences.theme");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Crumbs(page)).ToHaveCountAsync(1);
        await ExpectNoViolationsAsync(page, $"a nested page ({what})");

        await StepBackAndSearchAsync(page, "theme", "set labels");
        await ExpectFirstItemAsync(page, "actions.set-labels");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Item(page, "label:bug")).ToHaveAttributeAsync("aria-checked", "true");
        await ExpectNoViolationsAsync(page, $"a multi-select page ({what})");

        await StepBackAndSearchAsync(page, "set labels", "rename");
        await ExpectFirstItemAsync(page, "actions.rename-project");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Prompt(page)).ToBeVisibleAsync();
        await ExpectNoViolationsAsync(page, $"a prompt ({what})");
    }

    [TestMethod]
    public Task Every_state_passes_axe_in_the_light_demo_theme() => AuditEveryStateAsync(ColorScheme.Light, libraryDefaults: false);

    [TestMethod]
    public Task Every_state_passes_axe_in_the_dark_demo_theme() => AuditEveryStateAsync(ColorScheme.Dark, libraryDefaults: false);

    [TestMethod]
    public Task Every_state_passes_axe_with_the_light_library_defaults() => AuditEveryStateAsync(ColorScheme.Light, libraryDefaults: true);

    [TestMethod]
    public Task Every_state_passes_axe_with_the_dark_library_defaults() => AuditEveryStateAsync(ColorScheme.Dark, libraryDefaults: true);

    [TestMethod]
    public async Task In_forced_colors_the_active_row_is_outlined_in_the_system_highlight()
    {
        var options = DefaultContextOptions();
        options.ForcedColors = ForcedColors.Active;
        var page = await OpenAsync("/", options);
        await OpenPaletteAsync(page);

        var outline = await ActiveItem(page).EvaluateAsync<string[]>(
            """
            e => {
              const s = getComputedStyle(e);
              const probe = document.body.appendChild(document.createElement('span'));
              probe.style.color = 'Highlight';
              const highlight = getComputedStyle(probe).color;
              probe.remove();
              return [s.outlineStyle, s.outlineColor === highlight ? 'Highlight' : s.outlineColor, s.outlineWidth];
            }
            """);

        AssertSequence(["solid", "Highlight"], outline[..2]);
        // 2px as authored; Firefox snaps a width to whole device pixels and can report a fraction under it (1.83333px).
        AssertInRange(double.Parse(outline[2].TrimEnd('p', 'x'), System.Globalization.CultureInfo.InvariantCulture), 1.5, 2, "the outline's width");
        var border = await Dialog(page).EvaluateAsync<string>("e => getComputedStyle(e).borderTopStyle");
        Assert.AreEqual("solid", border);
    }

    [TestMethod]
    public async Task In_right_to_left_text_the_arrows_and_the_chevrons_follow_the_reading_direction()
    {
        var page = await OpenAsync("/");
        await page.EvaluateAsync("() => document.documentElement.setAttribute('dir', 'rtl')");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "theme");
        await ExpectFirstItemAsync(page, "preferences.theme");

        var chevron = await Item(page, "preferences.theme").Locator(".bit-balette-chevron svg")
            .EvaluateAsync<string>("e => getComputedStyle(e).transform");
        Assert.AreEqual("matrix(-1, 0, 0, 1, 0, 0)", chevron);

        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Crumbs(page)).ToHaveCountAsync(1);

        // On an empty query → points back to the start of an RTL line: back a page. ← would open the action panel.
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(Crumbs(page)).ToHaveCountAsync(0);
        await Assertions.Expect(Input(page)).ToHaveValueAsync("theme");

        // The key caps of a shortcut keep their left-to-right order.
        var hint = page.Locator("[data-balette='shortcut']").First;
        Assert.AreEqual("ltr", await hint.EvaluateAsync<string>("e => getComputedStyle(e).direction"));
    }

    [TestMethod]
    public async Task Clicking_a_row_that_keeps_the_palette_open_leaves_the_focus_in_the_input()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "set labels");
        await ExpectFirstItemAsync(page, "actions.set-labels");
        await page.Keyboard.PressAsync("Enter");

        await Item(page, "label:bug").ClickAsync();

        await Assertions.Expect(Item(page, "label:bug")).ToHaveAttributeAsync("aria-checked", "true");
        await Assertions.Expect(Input(page)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Control+Enter");
        await ExpectClosedAsync(page);
    }

    private static void AssertSequence(string[] expected, string[] actual)
        => CollectionAssert.AreEqual(expected, actual, $"expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}]");
}
