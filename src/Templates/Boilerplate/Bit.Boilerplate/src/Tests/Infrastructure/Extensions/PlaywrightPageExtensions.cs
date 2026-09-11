namespace Microsoft.Playwright;

public static class PlaywrightPageExtensions
{
    extension(IPage page)
    {
        /// <summary>
        /// Navigates inside a running app without reloading the page, by posting its own NAVIGATE_TO message (see
        /// <c>events.ts</c>) - which <c>AppClientCoordinator</c> turns into a <c>NavigationManager.NavigateTo</c>,
        /// the same client side routing a user gets by clicking a link.
        /// <para>
        /// Use <c>GotoAsync</c> when the reload itself is the point: opening the app for the first time, following a
        /// mailed link, or forcing a page to fetch its data again.
        /// </para>
        /// </summary>
        public async Task GoToInApp(string path, string? culture = null)
        {
            // App relative, or AppClientCoordinator ignores it; culture in front, as the route templates expect it.
            var route = culture is null ? path : $"/{culture}{path}";
            var target = new Uri(new Uri(page.Url), route).ToString();

            // The message goes nowhere until the app has started; a cold first visit waits for the WebAssembly boot.
            await page.Locator("main .main-container").First
                .WaitForAsync(new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds });

            await page.EvaluateAsync(publishNavigateToScript, route);

            // Poll the url instead of using WaitForURLAsync: client side routing never reloads the document, so the
            // load event that method waits for never fires (chromium lets it pass, webkit times out).
            await Assertions.Expect(page).ToHaveURLAsync(target);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    // The exact message shape events.ts listens for; it hands anything with this key to App.publishMessage.
    private const string publishNavigateToScript = $$"""
        route => window.postMessage(
            { key: 'PUBLISH_MESSAGE', message: '{{ClientAppMessages.NAVIGATE_TO}}', payload: route },
            window.location.origin)
        """;
}
