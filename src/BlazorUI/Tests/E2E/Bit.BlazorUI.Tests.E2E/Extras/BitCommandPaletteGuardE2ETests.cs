namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitCommandPalette's guarded commands at /e2e/command-palette-guards: refused from a row, a global shortcut and code
/// while the page's role is viewer, with the guard's message in the footer and the refusal reported to the page; run
/// once it is admin.
/// </summary>
[TestClass]
[E2ERetry]
public class BitCommandPaletteGuardE2ETests : BitCommandPaletteE2ETestBase
{
    private ILocator RoleButton(string role) => Page.Locator($"#cpl-role-{role}");

    private ILocator Status => Page.Locator("#cpl-refusals");

    [TestMethod]
    public async Task AGuardedCommand_IsRefusedUntilTheRoleAllowsIt_HoweverItIsStarted()
    {
        await OpenPageAsync(GuardsRoute);
        await Expect(RoleButton("viewer")).ToHaveAttributeAsync("aria-pressed", "true");

        // From a row: listed, refused with the guard's message, and the palette stays open to say so.
        await OpenPaletteAsync();
        await TypeAsync("purge the cache");
        await ExpectFirstItemAsync("demo-purge-cache");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Toast).ToHaveTextAsync("Only admins can purge the cache.");
        await Expect(Toast).ToHaveAttributeAsync("data-kind", "error");
        await ExpectOpenAsync();
        await Expect(Status).ToContainTextAsync("Purge the cache (palette)");
        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();

        // From its global shortcut, with the palette closed: refused, and reported.
        await Page.Locator("h1").ClickAsync();
        await Page.Keyboard.PressAsync("Alt+Shift+P");
        await Expect(Status).ToContainTextAsync("Purge the cache (shortcut)");
        await Expect(Status).ToContainTextAsync("ran: 0");

        // From code.
        await Page.Locator("#cpl-execute").ClickAsync();
        await Expect(Status).ToContainTextAsync("Purge the cache (programmatic)");

        // As an admin the same shortcut runs it.
        await RoleButton("admin").ClickAsync();
        await Expect(RoleButton("admin")).ToHaveAttributeAsync("aria-pressed", "true");
        await Page.Keyboard.PressAsync("Alt+Shift+P");
        await Expect(Status).ToContainTextAsync("ran: 1");
    }

    [TestMethod]
    public async Task AGuardedPagesChildren_AreRefused_AndRunOnceAllowed()
    {
        await OpenPageAsync(GuardsRoute);

        // Found from the root by nested search: the page's guard covers it.
        await OpenPaletteAsync();
        await TypeAsync("rotate api keys");
        await ExpectFirstItemAsync("demo-admin-tools.rotate-api-keys");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Toast).ToHaveTextAsync("Admin tools are for admins.");
        await Expect(Status).ToContainTextAsync("Rotate API keys (palette)");
        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();

        await RoleButton("admin").ClickAsync();
        await Expect(RoleButton("admin")).ToHaveAttributeAsync("aria-pressed", "true");
        await OpenPaletteAsync();
        await TypeAsync("rotate api keys");
        await ExpectFirstItemAsync("demo-admin-tools.rotate-api-keys");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Status).ToContainTextAsync("ran: 1");
    }
}
