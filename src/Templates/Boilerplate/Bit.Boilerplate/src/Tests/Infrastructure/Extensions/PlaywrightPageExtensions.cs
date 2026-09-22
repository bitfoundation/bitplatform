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

        /// <summary>
        /// Waits until Blazor's interactive renderer has attached to the document.
        /// <para>
        /// With pre-rendering on, the whole page is on screen long before that, as markup nobody is listening to.
        /// Playwright cannot tell the difference - the elements are there, visible and perfectly actionable - so a test
        /// that acts on first sight types into fields that are about to be replaced and clicks buttons wired to
        /// nothing. Prefer a signal from the page itself where one exists (a button the model enables says more than
        /// this does); reach for this where the page offers none.
        /// </para>
        /// <para>
        /// The signal is Blazor's own <c>_bl_*</c> attribute, which the renderer stamps on each element it registers a
        /// handler for. Server-rendered HTML carries no handlers, so the first one to appear is the attach. That is an
        /// internal of the framework rather than a promise, which is why running out of time here is not itself a
        /// failure: this is a precaution against acting too early, and a test that then acts too early fails on its own
        /// assertion, where the message actually says something. A page that never reports interactive is either one
        /// that has no interactivity to wait for, or a broken app the next line will report far better than this can.
        /// </para>
        /// </summary>
        public async Task WaitForBlazorInteractive()
        {
            try
            {
                await page.WaitForFunctionAsync(
                    "() => [...document.querySelectorAll('*')].some(e => e.getAttributeNames().some(n => n.startsWith('_bl_')))",
                    null,
                    // As long as the cold WebAssembly boot GoToInApp allows for: a shorter wait would expire on a
                    // loaded runner exactly when it is needed most.
                    new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds });
            }
            catch (Exception exception) when (exception is TimeoutException or PlaywrightException)
            {
            }
        }
    }

    // The exact message shape events.ts listens for; it hands anything with this key to App.publishMessage.
    private const string publishNavigateToScript = $$"""
        route => window.postMessage(
            { key: 'PUBLISH_MESSAGE', message: '{{ClientAppMessages.NAVIGATE_TO}}', payload: route },
            window.location.origin)
        """;
}
