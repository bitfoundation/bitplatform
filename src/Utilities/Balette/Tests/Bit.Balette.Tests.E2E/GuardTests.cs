using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// The guarded commands of the Options &amp; security page: refused from a row, a global shortcut and code while the page's
/// role is viewer, with the guard's message in the footer and the refusal reported to the page; run once it is admin.
/// </summary>
[TestClass]
public sealed class GuardTests : BaletteTest
{
    private static ILocator RoleButton(IPage page, string role) => page.Locator("#guards [role='group'] button", new() { HasTextString = role });

    private static ILocator Status(IPage page) => page.Locator("#guards [role='status']");

    [TestMethod]
    public async Task A_guarded_command_is_refused_until_the_role_allows_it_however_it_is_started()
    {
        var page = await OpenAsync("/options");
        await Assertions.Expect(RoleButton(page, "viewer")).ToHaveAttributeAsync("aria-pressed", "true");

        // From a row: listed, refused with the guard's message, and the palette stays open to say so.
        await OpenPaletteAsync(page);
        await TypeAsync(page, "purge the cache");
        await ExpectFirstItemAsync(page, "demo-purge-cache");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Toast(page)).ToHaveTextAsync("Only admins can purge the cache.");
        await Assertions.Expect(Toast(page)).ToHaveAttributeAsync("data-kind", "error");
        await ExpectOpenAsync(page);
        await Assertions.Expect(Status(page)).ToContainTextAsync("Purge the cache (palette)");
        await page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync(page);

        // From its global shortcut, with the palette closed: refused, and reported.
        await page.Locator("main").ClickAsync(new() { Position = new() { X = 4, Y = 4 } });
        await page.Keyboard.PressAsync("Alt+Shift+P");
        await Assertions.Expect(Status(page)).ToContainTextAsync("Purge the cache (shortcut)");
        await Assertions.Expect(Status(page)).ToContainTextAsync("ran: 0");

        // From code.
        await page.Locator("#guards button", new() { HasTextString = "ExecuteAsync(\"demo-purge-cache\")" }).ClickAsync();
        await Assertions.Expect(Status(page)).ToContainTextAsync("Purge the cache (programmatic)");

        // As an admin the same shortcut runs it.
        await RoleButton(page, "admin").ClickAsync();
        await Assertions.Expect(RoleButton(page, "admin")).ToHaveAttributeAsync("aria-pressed", "true");
        await page.Keyboard.PressAsync("Alt+Shift+P");
        await Assertions.Expect(Status(page)).ToContainTextAsync("ran: 1");
    }

    [TestMethod]
    public async Task A_guarded_pages_children_are_refused_and_run_once_allowed()
    {
        var page = await OpenAsync("/options");

        // Found from the root by nested search: the page's guard covers it.
        await OpenPaletteAsync(page);
        await TypeAsync(page, "rotate api keys");
        await ExpectFirstItemAsync(page, "demo-admin-tools.rotate-api-keys");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Toast(page)).ToHaveTextAsync("Admin tools are for admins.");
        await Assertions.Expect(Status(page)).ToContainTextAsync("Rotate API keys (palette)");
        await page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync(page);

        await RoleButton(page, "admin").ClickAsync();
        await Assertions.Expect(RoleButton(page, "admin")).ToHaveAttributeAsync("aria-pressed", "true");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "rotate api keys");
        await ExpectFirstItemAsync(page, "demo-admin-tools.rotate-api-keys");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Status(page)).ToContainTextAsync("ran: 1");
    }
}
