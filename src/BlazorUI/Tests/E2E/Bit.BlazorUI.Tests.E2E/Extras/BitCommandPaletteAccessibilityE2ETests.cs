namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitCommandPalette audited by axe-core in each of its states - the root list, search results with highlights, a
/// nested page, a multi-select page, a prompt - in the light and the dark theme; then what axe cannot check: forced
/// colours, right-to-left text and the arrows that follow it, and the focus staying in the input when a row is clicked.
/// </summary>
/// <remarks>An automated audit finds a part of the problems; it is not a claim of WCAG conformance.</remarks>
[TestClass]
[E2ERetry]
public class BitCommandPaletteAccessibilityE2ETests : BitCommandPaletteE2ETestBase
{
    private const string DialogScope = "dialog[data-bit-cpl='dialog']";

    /// <summary>Escape back to the root, which writes back the query the page was opened with, then a search for <paramref name="query"/>.</summary>
    private async Task StepBackAndSearchAsync(string restoredQuery, string query)
    {
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Crumbs).ToHaveCountAsync(0);
        await ReplaceQueryAsync(restoredQuery, query);
    }

    private async Task AuditEveryStateAsync(bool dark)
    {
        await OpenPageAsync();
        if (dark)
        {
            await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
            await Page.EvaluateAsync("() => document.documentElement.setAttribute('bit-theme', 'dark')");
        }

        await OpenPaletteAsync();
        await Expect(Items.First).ToBeVisibleAsync();
        await AssertNoAxeViolationsAsync(DialogScope);

        await TypeAsync("set");
        await Expect(Page.Locator("[data-bit-cpl='mark']").First).ToBeVisibleAsync();
        await AssertNoAxeViolationsAsync(DialogScope);

        await ReplaceQueryAsync("set", "theme");
        await ExpectFirstItemAsync("preferences.theme");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Crumbs).ToHaveCountAsync(1);
        await AssertNoAxeViolationsAsync(DialogScope);

        await StepBackAndSearchAsync("theme", "set labels");
        await ExpectFirstItemAsync("actions.set-labels");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Dialog).ToHaveAttributeAsync("data-multiselect", "true");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Item("label:bug")).ToHaveAttributeAsync("aria-checked", "true");
        await AssertNoAxeViolationsAsync(DialogScope);

        await StepBackAndSearchAsync("set labels", "rename");
        await ExpectFirstItemAsync("actions.rename-project");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Prompt).ToBeVisibleAsync();
        await AssertNoAxeViolationsAsync(DialogScope);
    }

    [TestMethod]
    public Task EveryState_PassesAxe_InTheLightTheme() => AuditEveryStateAsync(dark: false);

    [TestMethod]
    public Task EveryState_PassesAxe_InTheDarkTheme() => AuditEveryStateAsync(dark: true);

    [TestMethod]
    public async Task InForcedColors_TheActiveRowIsOutlinedInTheSystemHighlight()
    {
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });
        await OpenPageAsync();
        await OpenPaletteAsync();
        await Expect(ActiveItem).ToBeVisibleAsync();

        var outline = await ActiveItem.EvaluateAsync<string[]>(
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

        CollectionAssert.AreEqual(new[] { "solid", "Highlight" }, outline[..2], $"got [{string.Join(", ", outline)}]");
        // 2px as authored; Firefox snaps a width to whole device pixels and can report a fraction under it.
        AssertInRange(double.Parse(outline[2].Replace("px", ""), System.Globalization.CultureInfo.InvariantCulture), 1.5, 2, "the outline's width");
        Assert.AreEqual("solid", await Dialog.EvaluateAsync<string>("e => getComputedStyle(e).borderTopStyle"));
    }

    [TestMethod]
    public async Task InRightToLeftText_TheArrowsAndTheChevronsFollowTheReadingDirection()
    {
        await OpenPageAsync();
        await Page.EvaluateAsync("() => document.documentElement.setAttribute('dir', 'rtl')");
        await OpenPaletteAsync();
        await TypeAsync("theme");
        await ExpectFirstItemAsync("preferences.theme");

        await Expect(Item("preferences.theme").Locator(".bit-cpl-chevron svg")).ToHaveCSSAsync("transform", "matrix(-1, 0, 0, 1, 0, 0)");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Crumbs).ToHaveCountAsync(1);

        // On an empty query → points back to the start of a right-to-left line: back a page. ← would open the actions.
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Crumbs).ToHaveCountAsync(0);
        await Expect(Input).ToHaveValueAsync("theme");

        // The key caps of a shortcut keep their left-to-right order.
        await Expect(Item("navigation.theming-and-templates").Locator("[data-bit-cpl='shortcut']")).ToHaveCSSAsync("direction", "ltr");
    }

    [TestMethod]
    public async Task ClickingARowThatKeepsThePaletteOpen_LeavesTheFocusInTheInput()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("set labels");
        await ExpectFirstItemAsync("actions.set-labels");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Dialog).ToHaveAttributeAsync("data-multiselect", "true");

        await Item("label:bug").ClickAsync();

        await Expect(Item("label:bug")).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Input).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Control+Enter");
        await ExpectClosedAsync();
    }
}
