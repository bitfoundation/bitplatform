namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Searching BitCommandPalette the way people type: a nested command found from the root with its path, diacritics
/// folded, a typo forgiven, a right-to-left language switching the page's direction; and the flows that answer back - a
/// prompt that validates, a server's refusal kept on the prompt, a page that searches as the user types.
/// </summary>
[TestClass]
[E2ERetry]
public class BitCommandPaletteSearchFeatureE2ETests : BitCommandPaletteE2ETestBase
{
    [TestMethod]
    public async Task ANestedCommandWithADiacritic_IsFoundFromTheRoot_ByItsPlainSpelling()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();

        await TypeAsync("francais");

        await ExpectFirstItemAsync("language:Français");
        var row = Item("language:Français");
        await Expect(row.Locator("[data-bit-cpl='path']")).ToHaveTextAsync("Language");
        // The highlight marks the ç and the a the plain query typed.
        await Expect(row.Locator("[data-bit-cpl='mark']").First).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("cpl-language")).ToHaveTextAsync("Language: Français");
        // The option answers Back, and there is no page to go back to: the palette stays, showing the new choice.
        await Expect(Item("language:Français").Locator(".bit-cpl-toggle")).ToHaveAttributeAsync("data-on", "true");
    }

    [TestMethod]
    public async Task AQueryWithATypo_FindsTheCommand_AndSaysTheMatchIsClose()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();

        await TypeAsync("settigns");

        await ExpectFirstItemAsync("navigation.settings");
        await Expect(Page.Locator("[data-bit-cpl='notice']")).ToHaveTextAsync("No exact matches. Showing close ones.");
        await Expect(LiveRegion).ToContainTextAsync("No exact matches");
    }

    [TestMethod]
    public async Task ChoosingARightToLeftLanguage_LaysThePaletteOutFromTheRight()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("persian");
        await ExpectFirstItemAsync("language:Persian");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");

        // Still open on the root (the option answers Back, with nowhere to go back to), now right to left.
        await ExpectOpenAsync();
        await Expect(Dialog).ToHaveCSSAsync("direction", "rtl");
        // The search icon sits at the right edge of the input row, where a right-to-left line starts.
        var iconRight = await Dialog.Locator(".bit-cpl-input-icon").EvaluateAsync<double>("e => e.getBoundingClientRect().right");
        var inputRight = await Input.EvaluateAsync<double>("e => e.getBoundingClientRect().right");
        Assert.IsTrue(iconRight > inputRight, $"the icon ({iconRight}) is not right of the input ({inputRight})");

        // Back to English, so the page is left as it was found.
        await ReplaceQueryAsync("persian", "english");
        await ExpectFirstItemAsync("language:English");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("dir", "ltr");
    }

    [TestMethod]
    public async Task TheRenamePrompt_Validates_AndARefusedNameKeepsItOpen()
    {
        await OpenPageAsync();
        await Page.Keyboard.PressAsync("F2");
        await ExpectOpenAsync();

        await ReplaceQueryAsync("Acme Dashboard", "   ");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("[data-bit-cpl='prompt-error']")).ToHaveTextAsync("A project needs a name.");
        await Expect(Input).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("[data-bit-cpl='alert']")).ToHaveTextAsync("A project needs a name.");

        // The message follows the text once shown.
        await ReplaceQueryAsync("   ", "admin");
        await Expect(Page.Locator("[data-bit-cpl='prompt-error']")).ToHaveCountAsync(0);

        // The "server" refuses this one: the prompt stays, with the value, and says why.
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Toast).ToHaveTextAsync("“admin” is reserved. Pick another name.");
        await Expect(Prompt).ToBeVisibleAsync();
        await Expect(Input).ToHaveValueAsync("admin");

        await ReplaceQueryAsync("admin", "Orion");
        await Page.Keyboard.PressAsync("Enter");
        await ExpectClosedAsync();
        await Expect(Text("cpl-project")).ToHaveTextAsync("Project: Orion");
    }

    [TestMethod]
    public async Task APage_SearchesTheDirectoryAsTheUserTypes()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("find a person");
        await ExpectFirstItemAsync("actions.find-a-person");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Items).ToHaveCountAsync(10);

        await TypeAsync("grace");
        await Expect(Items).ToHaveCountAsync(1);
        await ExpectFirstItemAsync("person:grace@example.com");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Toast).ToHaveTextAsync("Opened Grace Hopper");
    }
}
