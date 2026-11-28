namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// The base of the BitCommandPalette suites: opens a page of the palette and waits until every palette on it listens for
/// keys, and holds the locators and probes the suites share. Every locator is the modal palette's (<c>#cpl</c>, the
/// layout-like one each page has); an inline palette is reached through its own section.
/// </summary>
public abstract class BitCommandPaletteE2ETestBase : E2ETestBase
{
    protected const string MainRoute = "/e2e/command-palette";
    protected const string InlineRoute = "/e2e/command-palette-inline";
    protected const string GuardsRoute = "/e2e/command-palette-guards";

    private const string InDialog = "dialog[data-bit-cpl='dialog'] ";

    /// <summary>
    /// Reduced motion, so a box measured right after the dialog opened is its resting box rather than a frame of its
    /// pop-in, and a viewport tall enough for the list to show a good part of the commands.
    /// </summary>
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 1280, Height = 900 },
        ReducedMotion = ReducedMotion.Reduce,
    };

    /// <summary>
    /// Opens a page and waits until every palette on it listens for keys: the page turns interactive first, and the
    /// browser layer that hears Ctrl+K starts a moment after (data-ready on each palette's root).
    /// </summary>
    protected async Task OpenPageAsync(string path = MainRoute)
    {
        await OpenAsync(path);
        await WaitForPalettesAsync();
    }

    /// <summary>Reloads the page, then waits for its palettes as <see cref="OpenPageAsync"/> does.</summary>
    protected async Task ReloadPageAsync()
    {
        await ReloadAsync();
        await WaitForPalettesAsync();
    }

    private Task WaitForPalettesAsync() => Page.WaitForFunctionAsync(
        """
        () => [...document.querySelectorAll('[data-bit-cpl="dialog"], [data-bit-cpl="inline"]')].every(e => e.dataset.ready === 'true')
           && document.querySelector('[data-bit-cpl="dialog"]') !== null
        """, null, new() { Timeout = DefaultTimeout });

    // ---- The palette's hooks -----------------------------------------------------------------------------------------

    protected ILocator Dialog => Page.Locator("dialog[data-bit-cpl='dialog']");

    protected ILocator Input => Page.Locator(InDialog + "[data-bit-cpl='input']");

    protected ILocator List => Page.Locator(InDialog + "[data-bit-cpl='list']");

    protected ILocator Items => Page.Locator(InDialog + "[data-bit-cpl='item']");

    protected ILocator Item(string id) => Page.Locator(InDialog + $"[data-bit-cpl='item'][data-value='{id}']");

    protected ILocator ActiveItem => Page.Locator(InDialog + "[data-bit-cpl='item'][data-active='true']");

    protected ILocator GroupHeadings => Page.Locator(InDialog + "[data-bit-cpl='group-heading']");

    protected ILocator Crumbs => Page.Locator(InDialog + "[data-bit-cpl='breadcrumbs'] [data-bit-cpl='crumb']");

    protected ILocator ModeChip => Page.Locator(InDialog + "[data-bit-cpl='mode']");

    protected ILocator Toast => Page.Locator(InDialog + "[data-bit-cpl='toast']");

    protected ILocator Prompt => Page.Locator(InDialog + "[data-bit-cpl='prompt']");

    protected ILocator Detail => Page.Locator(InDialog + "[data-bit-cpl='detail']");

    /// <summary>The polite live region the palette announces result counts in.</summary>
    protected ILocator LiveRegion => Page.Locator(InDialog + "[aria-live='polite']");

    protected ILocator Text(string id) => Page.Locator($"#{id}");

    // ---- Driving it --------------------------------------------------------------------------------------------------

    /// <summary>Presses Ctrl+K with the palette closed, and waits until it is open with the input focused.</summary>
    protected async Task OpenPaletteAsync()
    {
        await Page.Keyboard.PressAsync("Control+k");
        await ExpectOpenAsync();
    }

    /// <summary>The native dialog is shown (a closed one is display: none) and the input has the focus.</summary>
    protected async Task ExpectOpenAsync()
    {
        await Expect(Dialog).ToBeVisibleAsync();
        await Expect(Input).ToBeFocusedAsync();
    }

    protected Task ExpectClosedAsync() => Expect(Dialog).ToBeHiddenAsync();

    /// <summary>Types into the palette's input one key at a time, as a person would: every key is its own input event.</summary>
    protected Task TypeAsync(string text) => Input.PressSequentiallyAsync(text);

    /// <summary>
    /// Waits until the input reads <paramref name="current"/>, deletes it key by key and types <paramref name="text"/>.
    /// Not FillAsync where the palette has just written the input itself: it moves the caret to the end after the render,
    /// a round trip later on the server, and when that lands between a fill's select-all and its typing the text is
    /// appended to the old one. One Backspace per character and no more: one on an empty query goes back a page.
    /// </summary>
    protected async Task ReplaceQueryAsync(string current, string text)
    {
        await Expect(Input).ToHaveValueAsync(current);
        for (var i = 0; i < current.Length; i++)
        {
            await Page.Keyboard.PressAsync("Backspace");
        }

        await Expect(Input).ToHaveValueAsync("");
        await TypeAsync(text);
    }

    protected Task ExpectFirstItemAsync(string id) => Expect(Items.First).ToHaveAttributeAsync("data-value", id);

    /// <summary>
    /// Waits until <paramref name="id"/> is the active row, and checks the combobox says so: the input's
    /// aria-activedescendant names that row, which is marked selected, and the focus never left the input.
    /// </summary>
    protected async Task ExpectActiveAsync(string id)
    {
        await Expect(ActiveItem).ToHaveAttributeAsync("data-value", id);
        await Expect(Input).ToHaveAttributeAsync("aria-activedescendant", (await ActiveItem.GetAttributeAsync("id"))!);
        await Expect(ActiveItem).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Input).ToBeFocusedAsync();
    }

    /// <summary>The ids of the rows listed right now, in order.</summary>
    protected Task<string[]> ItemIdsAsync() =>
        Page.EvaluateAsync<string[]>("() => [...document.querySelectorAll('dialog[data-bit-cpl=\"dialog\"] [data-bit-cpl=\"item\"]')].map(e => e.dataset.value)");

    /// <summary>What has the focus, described by its data-bit-cpl hook, else its tag and aria-label.</summary>
    protected Task<string> FocusedAsync() => Page.EvaluateAsync<string>(
        """
        () => {
          const e = document.activeElement;
          if (!e) return '';
          return e.dataset?.bitCpl ? 'cpl:' + e.dataset.bitCpl : `${e.tagName.toLowerCase()}[${e.getAttribute('aria-label') ?? ''}]`;
        }
        """);

    /// <summary>Waits until the address ends with <paramref name="path"/>.</summary>
    protected Task ExpectUrlAsync(string path) => Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(System.Text.RegularExpressions.Regex.Escape(path) + "$"));

    /// <summary>
    /// Dispatches a keydown on the element <paramref name="selector"/> finds (the focused one by default) as a keyboard
    /// other than Playwright's US one produces it - its key, code, modifiers, repeat and composition state - and says
    /// whether it was claimed (its default prevented).
    /// </summary>
    protected Task<bool> KeyDownAsync(string key, string code, bool ctrl = false, bool alt = false, bool repeat = false, bool composing = false, int keyCode = 0, string? selector = null)
        => Page.EvaluateAsync<bool>(
            """
            ([key, code, ctrl, alt, repeat, composing, keyCode, selector]) => {
              const target = selector ? document.querySelector(selector) : (document.activeElement ?? document.body);
              const e = new KeyboardEvent('keydown', { key, code, ctrlKey: ctrl, altKey: alt, repeat, isComposing: composing, keyCode, bubbles: true, cancelable: true });
              target.dispatchEvent(e);
              return e.defaultPrevented;
            }
            """,
            new object?[] { key, code, ctrl, alt, repeat, composing, keyCode, selector });

    protected static void AssertInRange(double value, double low, double high, string what) =>
        Assert.IsTrue(value >= low && value <= high, $"{what} is {value}, expected it between {low} and {high}.");
}
