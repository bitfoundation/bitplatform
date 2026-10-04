namespace Boilerplate.Tests.Infrastructure.Components;

/// <summary>
/// Helpers for the account menu in the app's header (<c>AppMenu</c>, a Bit.BlazorUI <c>BitDropMenu</c>) in Playwright
/// UI tests.
/// </summary>
public static class AppMenuUtils
{
    /// <summary>
    /// Opens the account menu and clicks its entry named <paramref name="itemName"/>, opening the menu again for as long
    /// as the entry cannot be clicked.
    /// <para>
    /// One click on the menu is not always enough. Bit.BlazorUI closes an open callout on any scroll of the page, so a
    /// scroll that arrives right after the click closes the menu again before the entry can be clicked, and a click
    /// that lands before the app has taken over a prerendered header is lost altogether. Either way the menu ends up
    /// closed, so the entry is only waited for briefly before the menu is opened again.
    /// </para>
    /// </summary>
    public static async Task ClickItem(IPage page, string itemName)
    {
        var menuButton = page.Locator("header .bit-drm-btn").First;
        var item = page.Locator(".app-menu-callout").GetByRole(AriaRole.Button, new() { Name = itemName, Exact = true });

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);

        while (true)
        {
            // The button toggles the menu, so it is only clicked while the menu is closed.
            if (await menuButton.GetAttributeAsync("aria-expanded") != "true")
            {
                await menuButton.ClickAsync();
            }

            try
            {
                await item.ClickAsync(new() { Timeout = (float)TimeSpan.FromSeconds(5).TotalMilliseconds });
                return;
            }
            catch (TimeoutException) when (DateTimeOffset.UtcNow < deadline)
            {
            }
        }
    }
}
