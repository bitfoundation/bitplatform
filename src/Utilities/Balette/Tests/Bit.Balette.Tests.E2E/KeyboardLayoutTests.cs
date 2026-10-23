using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// Key presses as keyboards other than a US one produce them. Playwright types with a US layout, so these tests dispatch
/// the events a Russian or Persian layout, a held key or an input method editor fire - the same KeyboardEvent the
/// browser would, with its <c>key</c>, <c>code</c>, <c>repeat</c> and <c>isComposing</c> - and check what the browser
/// layer makes of them.
/// </summary>
[TestClass]
public sealed class KeyboardLayoutTests : BaletteTest
{
    /// <summary>Dispatches a keydown on <paramref name="selector"/> (the focused element by default) and says whether it was claimed.</summary>
    private static Task<bool> KeyDownAsync(IPage page, string key, string code, bool ctrl = false, bool repeat = false, bool composing = false, int keyCode = 0, string? selector = null)
        => page.EvaluateAsync<bool>(
            """
            ([key, code, ctrl, repeat, composing, keyCode, selector]) => {
              const target = selector ? document.querySelector(selector) : (document.activeElement ?? document.body);
              const e = new KeyboardEvent('keydown', { key, code, ctrlKey: ctrl, repeat, isComposing: composing, keyCode, bubbles: true, cancelable: true });
              target.dispatchEvent(e);
              return e.defaultPrevented;
            }
            """,
            new object?[] { key, code, ctrl, repeat, composing, keyCode, selector });

    [TestMethod]
    public async Task Ctrl_K_on_a_Russian_layout_opens_the_palette()
    {
        var page = await OpenAsync("/");

        // On a Russian layout the K key types 'л': the browser layer reads the key it sits on.
        Assert.IsTrue(await KeyDownAsync(page, "л", "KeyK", ctrl: true, selector: "body"));

        await ExpectOpenAsync(page);
    }

    [TestMethod]
    public async Task A_sequence_typed_on_a_Persian_layout_runs()
    {
        var page = await OpenAsync("/");

        // G then S, typed on the standard Persian layout: 'ل' and 'س'.
        await KeyDownAsync(page, "ل", "KeyG", selector: "body");
        await KeyDownAsync(page, "س", "KeyS", selector: "body");

        await page.WaitForURLAsync("**/commands#settings");
    }

    [TestMethod]
    public async Task Ctrl_N_and_Ctrl_P_move_the_selection_on_a_Russian_layout()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        var ids = await ItemIdsAsync(page);
        await ExpectActiveAsync(page, ids[0]);

        Assert.IsTrue(await KeyDownAsync(page, "т", "KeyN", ctrl: true));
        await ExpectActiveAsync(page, ids[1]);

        Assert.IsTrue(await KeyDownAsync(page, "з", "KeyP", ctrl: true));
        await ExpectActiveAsync(page, ids[0]);
    }

    [TestMethod]
    public async Task A_held_toggle_shortcut_opens_the_palette_once()
    {
        var page = await OpenAsync("/");

        await KeyDownAsync(page, "k", "KeyK", ctrl: true, selector: "body");
        await ExpectOpenAsync(page);

        // The auto-repeats of the held keys are claimed - the browser must not act on them either - but run nothing.
        for (var i = 0; i < 5; i++)
        {
            Assert.IsTrue(await KeyDownAsync(page, "k", "KeyK", ctrl: true, repeat: true));
        }

        await page.WaitForTimeoutAsync(300);
        await ExpectOpenAsync(page);
    }

    [TestMethod]
    public async Task Keys_that_belong_to_an_input_method_composition_run_no_shortcut()
    {
        var page = await OpenAsync("/");

        // isComposing, and Safari's commit key (keyCode 229 with isComposing false).
        Assert.IsFalse(await KeyDownAsync(page, "g", "KeyG", composing: true, selector: "body"));
        Assert.IsFalse(await KeyDownAsync(page, "s", "KeyS", keyCode: 229, selector: "body"));
        await page.WaitForTimeoutAsync(300);
        Assert.IsTrue(page.Url.EndsWith('/'), $"a composition key ran a shortcut and moved to {page.Url}");

        // Not vacuous: the same keys outside a composition are the settings sequence.
        await KeyDownAsync(page, "g", "KeyG", selector: "body");
        await KeyDownAsync(page, "s", "KeyS", selector: "body");
        await page.WaitForURLAsync("**/commands#settings");
    }

    [TestMethod]
    public async Task Enter_that_commits_a_composition_inside_the_palette_runs_nothing()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "settings");
        await ExpectFirstItemAsync(page, "navigation.settings");

        Assert.IsFalse(await KeyDownAsync(page, "Enter", "Enter", keyCode: 229));
        await page.WaitForTimeoutAsync(300);
        await ExpectOpenAsync(page);
    }
}
