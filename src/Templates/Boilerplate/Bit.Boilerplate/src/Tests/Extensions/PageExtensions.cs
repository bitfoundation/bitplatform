namespace Microsoft.Playwright;

public static class PageExtensions
{
    public static async Task ChangeCulture(this IPage page, string culture)
    {
        var cultureName = CultureInfoManager.SupportedCultures.Single(c => c.Culture.Name == culture).DisplayName;
        await page.Locator(".culture-drp").ClickAsync();
        await page.Locator($".culture-drp button[role='option']:visible:has-text('{cultureName}')").ClickAsync();
    }
}
