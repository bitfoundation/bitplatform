namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// What every BitDropdown E2E class reaches the component through. A dropdown is found by the Id of its root; its
/// field is the root's first <c>.bit-drp-wrp</c>, and its callout - rendered beside the root and moved to the body
/// while it is open - is found by the id the component gives it, the field's id plus <c>-callout</c>.
/// </summary>
public abstract class BitDropdownE2ETestBase : E2ETestBase
{
    protected ILocator Root(string id) => Page.Locator($"#{id}");

    protected ILocator Trigger(string id) => Page.Locator($"#{id} > .bit-drp-wrp");

    protected ILocator TextContainer(string id) => Page.Locator($"#{id} .bit-drp-tcn");

    protected ILocator ComboInput(string id) => Page.Locator($"#{id} input.bit-drp-inp");

    protected ILocator HiddenSelect(string id) => Page.Locator($"#{id} select.bit-input-hidden");

    protected async Task<ILocator> CalloutAsync(string id)
    {
        var triggerId = await Trigger(id).GetAttributeAsync("id");

        Assert.IsFalse(string.IsNullOrEmpty(triggerId), $"The field of #{id} carries no id.");

        return Page.Locator($"[id='{triggerId}-callout']");
    }

    protected async Task<ILocator> OverlayAsync(string id)
    {
        var triggerId = await Trigger(id).GetAttributeAsync("id");

        return Page.Locator($"[id='{triggerId}-overlay']");
    }

    /// <summary>
    /// Opens the dropdown with a click on its field and waits for the callout to be on the screen.
    /// </summary>
    protected async Task<ILocator> OpenDropdownAsync(string id)
    {
        await Trigger(id).ClickAsync();

        var callout = await CalloutAsync(id);

        await Expect(callout).ToBeVisibleAsync();

        // The expanded state is on whichever element carries the combobox role: the field, or the input of the
        // ComboBox mode.
        var combobox = await Trigger(id).GetAttributeAsync("role") == "combobox" ? Trigger(id) : ComboInput(id);
        await Expect(combobox).ToHaveAttributeAsync("aria-expanded", "true");

        await WaitForOpeningFocusAsync(id, callout);

        return callout;
    }

    /// <summary>
    /// Waits for the focus an opening hands to the list, which arrives in a round trip of its own: a key pressed before
    /// it lands is answered by the field instead, and the focus it then moves lands on a list that may be closed by
    /// then. The ComboBox mode keeps the focus in its input, and a list with nothing to focus takes none.
    /// </summary>
    protected async Task WaitForOpeningFocusAsync(string id, ILocator callout)
    {
        var calloutId = await callout.GetAttributeAsync("id");

        try
        {
            await Page.WaitForFunctionAsync(@"([calloutId, rootId]) => {
                const callout = document.getElementById(calloutId);
                const root = document.getElementById(rootId);
                const active = document.activeElement;
                if (!callout || callout.contains(active)) return true;
                if (active && active.tagName === 'INPUT' && root && root.contains(active)) return true;
                return callout.querySelectorAll('[role=option]:not([disabled]), button.bit-drp-sab:not([disabled])').length === 0;
            }", new[] { calloutId, id }, new() { Timeout = 5000 });
        }
        catch (TimeoutException)
        {
            // A list whose rows the opening did not focus: the test goes on to say what it expects of it.
        }
    }

    protected static ILocator Option(ILocator callout, string name) =>
        callout.GetByRole(AriaRole.Option, new() { Name = name, Exact = true });

    protected static ILocator Options(ILocator callout) => callout.Locator("[role=option]");

    protected static ILocator ListBox(ILocator callout) => callout.Locator("[role=listbox]");

    /// <summary>
    /// Waits for every animation running on the element and inside it to finish, so a bounding box read afterwards is
    /// the one it settles at rather than a frame of its entry.
    /// </summary>
    protected static Task WaitForAnimationsAsync(ILocator locator) =>
        locator.EvaluateAsync("el => Promise.all(el.getAnimations({ subtree: true }).map(a => a.finished.catch(() => null)))");

    /// <summary>
    /// Waits for the rendered options of a callout to stop changing - a virtualized list re-renders its window as it
    /// measures itself after the opening, reusing its rows for other items, so a row pointed at before then can carry
    /// another item by the time the click lands.
    /// </summary>
    protected static Task WaitForStableListAsync(ILocator callout) =>
        callout.EvaluateAsync(@"el => new Promise(resolve => {
            const read = () => [...el.querySelectorAll('[role=option]')].map(o => o.id + ':' + o.textContent).join('|') + ':' + (el.querySelector('[role=listbox]')?.scrollTop ?? 0);
            let last = read(), held = 0, frames = 0;
            const tick = () => {
                const now = read();
                held = now === last && now.length > 0 ? held + 1 : 0;
                last = now;
                if (held >= 10 || ++frames > 300) resolve(null); else requestAnimationFrame(tick);
            };
            requestAnimationFrame(tick);
        })");

    protected static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();

        Assert.IsNotNull(box, "The element has no bounding box.");

        return box;
    }

    /// <summary>
    /// The text of the element that has the focus.
    /// </summary>
    protected Task<string> FocusedTextAsync() =>
        Page.EvaluateAsync<string>("() => (document.activeElement?.textContent ?? '').trim()");

    /// <summary>
    /// Clicks an element through script, for one the open callout's overlay covers.
    /// </summary>
    protected Task ScriptClickAsync(string selector) =>
        Page.Locator(selector).EvaluateAsync("el => el.click()");

    protected static async Task<double> PxAsync(ILocator locator, string property)
    {
        var value = await locator.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");

        return double.Parse(value.Replace("px", string.Empty).Trim(), System.Globalization.CultureInfo.InvariantCulture);
    }

    protected static Task<string> StyleAsync(ILocator locator, string property) =>
        locator.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");
}
