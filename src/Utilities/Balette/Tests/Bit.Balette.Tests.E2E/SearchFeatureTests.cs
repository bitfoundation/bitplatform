using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// Searching the way people type, against the demo's own commands: a nested command found from the root with its path,
/// diacritics folded, a typo forgiven, a right-to-left language switching the page's direction; and the flows that
/// answer back - a prompt that validates, a server's refusal kept on the prompt, a page that searches as you type.
/// </summary>
[TestClass]
public sealed class SearchFeatureTests : BaletteTest
{
    [TestMethod]
    public async Task A_nested_command_with_a_diacritic_is_found_from_the_root_by_its_plain_spelling()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);

        await TypeAsync(page, "francais");

        await ExpectFirstItemAsync(page, "language:Français");
        var row = Item(page, "language:Français");
        await Assertions.Expect(row.Locator("[data-balette='path']")).ToHaveTextAsync("Language");
        // The highlight marks the ç and the a the plain query typed.
        await Assertions.Expect(row.Locator("[data-balette='mark']").First).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("Language: Français");
        // The option answers Back, and there is no page to go back to: the palette stays, showing the new choice.
        await Assertions.Expect(Item(page, "language:Français").Locator(".bit-balette-toggle")).ToHaveAttributeAsync("data-on", "true");
    }

    [TestMethod]
    public async Task A_query_with_a_typo_finds_the_command_and_says_the_match_is_close()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);

        await TypeAsync(page, "settigns");

        await ExpectFirstItemAsync(page, "navigation.settings");
        await Assertions.Expect(page.Locator("[data-balette='notice']")).ToHaveTextAsync("No exact matches. Showing close ones.");
        await Assertions.Expect(LiveRegion(page)).ToContainTextAsync("No exact matches");
    }

    [TestMethod]
    public async Task Choosing_a_right_to_left_language_lays_the_palette_out_from_the_right()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "persian");
        await ExpectFirstItemAsync(page, "language:Persian");

        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");

        // Still open on the root (the option answers Back, with nowhere to go back to), now right to left.
        await ExpectOpenAsync(page);
        var direction = await Dialog(page).EvaluateAsync<string>("e => getComputedStyle(e).direction");
        Assert.AreEqual("rtl", direction);
        // The search icon sits at the right edge of the input row, where a right-to-left line starts.
        var iconRight = await page.Locator(".bit-balette-input-icon").EvaluateAsync<double>("e => e.getBoundingClientRect().right");
        var inputRight = await Input(page).EvaluateAsync<double>("e => e.getBoundingClientRect().right");
        Assert.IsTrue(iconRight > inputRight, $"the icon ({iconRight}) is not right of the input ({inputRight})");

        // Back to English, so the page is left as it was found.
        await Input(page).FillAsync("english");
        await ExpectFirstItemAsync(page, "language:English");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", "ltr");
    }

    [TestMethod]
    public async Task The_rename_prompt_validates_and_a_refused_name_keeps_it_open()
    {
        var page = await OpenAsync("/");
        await page.Keyboard.PressAsync("F2");
        await ExpectOpenAsync(page);

        await Input(page).FillAsync("   ");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.Locator("[data-balette='prompt-error']")).ToHaveTextAsync("A project needs a name.");
        await Assertions.Expect(Input(page)).ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(page.Locator("[data-balette='alert']")).ToHaveTextAsync("A project needs a name.");

        // The message follows the text once shown.
        await Input(page).FillAsync("admin");
        await Assertions.Expect(page.Locator("[data-balette='prompt-error']")).ToHaveCountAsync(0);

        // The "server" refuses this one: the prompt stays, with the value, and says why.
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Toast(page)).ToHaveTextAsync("“admin” is reserved. Pick another name.");
        await Assertions.Expect(Prompt(page)).ToBeVisibleAsync();
        await Assertions.Expect(Input(page)).ToHaveValueAsync("admin");

        await Input(page).FillAsync("Orion");
        await page.Keyboard.PressAsync("Enter");
        await ExpectClosedAsync(page);
        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("Project: Orion");
    }

    [TestMethod]
    public async Task A_page_searches_the_directory_as_the_user_types()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "find a person");
        await ExpectFirstItemAsync(page, "actions.find-a-person");

        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Items(page)).ToHaveCountAsync(10);

        await TypeAsync(page, "grace");
        await Assertions.Expect(Items(page)).ToHaveCountAsync(1);
        await ExpectFirstItemAsync(page, "person:grace@example.com");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Toast(page)).ToHaveTextAsync("Opened Grace Hopper");
    }
}
