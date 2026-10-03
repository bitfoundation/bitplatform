namespace Boilerplate.Tests.Infrastructure.Components;

public static class AppMenuUtils
{
    public static async Task ClickItem(IPage page, string itemName)
    {
        var menuButton = page.Locator("header .bit-drm-btn").First;
        var item = page.Locator(".app-menu-callout").GetByRole(AriaRole.Button, new() { Name = itemName, Exact = true });

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);

        while (true)
        {
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
