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
        /// <para>
        /// <paramref name="until"/> is what makes that check trustworthy. The value sitting in the DOM only says the
        /// browser kept it; it does not say the app ever saw it, and hydration that lands after this method returns
        /// takes it away again. Pass something only the running app can produce - a button the model enables - and the
        /// filling continues until the app itself has reacted.
        /// </para>
        /// </summary>
        public async Task FillEnsuringStable(string value, Func<Task<bool>>? until = null)
        {
            var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);

            while (true)
            {
                await field.FillAsync(value);

                // Each attempt is given time to settle rather than judged on one reading: the sign in panel debounces
                // its e-mail field by 500ms, so re-filling any sooner would keep restarting that debounce instead of
                // waiting it out. A value that gets wiped meanwhile ends the attempt early - that is hydration, and
                // the answer to it is to fill again.
                var attemptDeadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(3);

                // The first reading keeps the settle this method has always used; the rest only shorten the wait.
                var settle = TimeSpan.FromMilliseconds(500);

                while (DateTimeOffset.UtcNow < attemptDeadline)
                {
                    await field.Page.WaitForTimeoutAsync((float)settle.TotalMilliseconds);
                    settle = TimeSpan.FromMilliseconds(250);

                    if (await field.InputValueAsync() != value)
                        break;

                    if (until is null || await until())
                        return;
                }

                if (DateTimeOffset.UtcNow >= deadline)
                    throw new InvalidOperationException($"Could not keep the field filled with '{value}'; something keeps resetting it.");
            }
        }
    }
}
