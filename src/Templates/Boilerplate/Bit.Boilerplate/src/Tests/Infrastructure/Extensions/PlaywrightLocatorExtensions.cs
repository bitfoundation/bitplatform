namespace Microsoft.Playwright;

public static class PlaywrightLocatorExtensions
{
    extension(ILocator field)
    {
        /// <summary>
        /// Fills <paramref name="field"/> and keeps filling it until the value is still there a moment later.
        /// <para>
        /// A plain <c>FillAsync</c> writes into whatever input is on screen right now, and there are two routine ways
        /// for the app to throw that away immediately afterwards: a component that resets its form as it renders (See
        /// <c>ManageMyTenantsPage.OnSectionExpand</c>), and - with pre-rendering on - hydration replacing the whole
        /// pre-rendered subtree with a freshly rendered, empty one. Either leaves the value gone and any button gated on
        /// it disabled forever, which reads as a hang on the NEXT step rather than as a failure to fill.
        /// </para>
        /// <para>
        /// Deliberately not a fixed number of attempts: on a loaded CI runner a WebAssembly boot can take longer than
        /// any attempt count worth hard-coding, so this waits against a deadline instead.
        /// </para>
        /// </summary>
        public async Task FillEnsuringStable(string value)
        {
            var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);

            while (true)
            {
                await field.FillAsync(value);

                // Give any pending reset / hydration a moment to land, then confirm our value survived it.
                await field.Page.WaitForTimeoutAsync((float)TimeSpan.FromMilliseconds(500).TotalMilliseconds);

                if (await field.InputValueAsync() == value)
                    return;

                if (DateTimeOffset.UtcNow >= deadline)
                    throw new InvalidOperationException($"Could not keep the field filled with '{value}'; something keeps resetting it.");
            }
        }
    }
}
