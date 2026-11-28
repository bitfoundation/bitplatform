namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitCommandPalette next to another search of the page - a docs search an app has of its own, a modal dialog answering
/// to "/" - joined by the truce such an app writes: whichever of the two is open owns the keys. The palette's side of
/// it is the contract these check: it leaves alone a key another surface claimed (its default prevented), never runs a
/// plain key typed in a field, and types "/" like any other character while it is open.
/// </summary>
[TestClass]
[E2ERetry]
public class BitCommandPaletteTwoPalettesE2ETests : BitCommandPaletteE2ETestBase
{
    /// <summary>
    /// The app's own docs search, and its glue with the palette: on the window in the capture phase, the one place that
    /// hears a key before the palette's listener on the document does. A key pressed in the docs search is marked claimed
    /// for the listeners after this one and keeps its default, so typing, the editing chords and the browser's own
    /// shortcuts still work there; "/" opens the search from anywhere but a text field and an open palette.
    /// </summary>
    private const string DocsSearch =
        """
        () => {
          const dialog = document.createElement('dialog');
          dialog.className = 'e2e-docs-search';
          dialog.setAttribute('aria-label', 'Search the docs');
          const input = document.createElement('input');
          input.type = 'search';
          input.setAttribute('aria-label', 'Search the docs');
          dialog.appendChild(input);
          document.body.appendChild(dialog);

          // WebKit leaves the focus in a dialog that closed: the keys pressed next are the page's again.
          dialog.addEventListener('close', () => { if (dialog.contains(document.activeElement)) document.activeElement.blur(); });

          const isEditable = e => e instanceof Element && (['INPUT', 'TEXTAREA', 'SELECT'].includes(e.tagName) || e.isContentEditable);
          window.addEventListener('keydown', e => {
            if (dialog.open && e.target instanceof Element && e.target.closest('.e2e-docs-search')) {
              Object.defineProperty(e, 'defaultPrevented', { configurable: true, value: true });
              if (e.key === 'Escape') dialog.close();
              return;
            }
            if (e.key !== '/' || e.ctrlKey || e.metaKey || e.altKey) return;
            if (document.querySelector('dialog[data-bit-cpl="dialog"][open]') !== null) {
              if (!isEditable(document.activeElement)) { e.preventDefault(); e.stopPropagation(); }
              return;
            }
            if (isEditable(document.activeElement)) return;
            e.preventDefault();
            dialog.showModal();
            input.focus();
          }, true);
        }
        """;

    private ILocator SiteSearch => Page.Locator("dialog.e2e-docs-search");

    private ILocator SiteSearchInput => SiteSearch.Locator("input");

    private async Task OpenWithTheDocsSearchAsync()
    {
        await OpenPageAsync();
        await Page.EvaluateAsync(DocsSearch);
    }

    [TestMethod]
    public async Task CtrlK_OpensThePalette_AndSlashOpensTheDocsSearch_NeverBoth()
    {
        await OpenWithTheDocsSearchAsync();

        await OpenPaletteAsync();
        await Expect(SiteSearch).ToBeHiddenAsync();
        await Page.Keyboard.PressAsync("Escape");
        await ExpectClosedAsync();

        await Page.Locator("h1").ClickAsync();
        await Page.Keyboard.PressAsync("/");
        await Expect(SiteSearch).ToBeVisibleAsync();
        await Expect(SiteSearchInput).ToBeFocusedAsync();
        await ExpectClosedAsync();
    }

    [TestMethod]
    public async Task WhileTheDocsSearchIsOpen_ThePalettesShortcutsStayQuiet_AndItsKeysStillWork()
    {
        await OpenWithTheDocsSearchAsync();

        await Page.Locator("h1").ClickAsync();
        await Page.Keyboard.PressAsync("/");
        await Expect(SiteSearchInput).ToBeFocusedAsync();

        // The palette's toggle and one of its chords, pressed in the docs search: neither opens the palette on top of it,
        // and the page-scoped Ctrl+Up behind it does not run either.
        await Page.Keyboard.PressAsync("Control+k");
        await Page.Keyboard.PressAsync("Alt+z");
        await Page.Keyboard.PressAsync("Control+ArrowUp");
        await Page.WaitForTimeoutAsync(300);
        await ExpectClosedAsync();
        await Expect(Text("cpl-counter")).ToHaveTextAsync("Counter: 0");
        await Expect(Page.Locator("#cpl-wordwrap")).ToBeCheckedAsync();

        // ...while the docs search keeps every key of its own: typing a "g s" that is no sequence here.
        await SiteSearchInput.PressSequentiallyAsync("g s");
        await Expect(SiteSearchInput).ToHaveValueAsync("g s");
        Assert.IsTrue(Page.Url.EndsWith("/e2e/command-palette", StringComparison.Ordinal), $"typing in the docs search ran a palette shortcut and moved to {Page.Url}");

        // Closed, the same chord is the palette's again.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(SiteSearch).ToBeHiddenAsync();
        await OpenPaletteAsync();
    }

    [TestMethod]
    public async Task WhileThePaletteIsOpen_ASlashIsTypedIntoIt_AndNeverOpensTheDocsSearch()
    {
        await OpenWithTheDocsSearchAsync();
        await OpenPaletteAsync();

        await TypeAsync("/");
        await Expect(Input).ToHaveValueAsync("/");

        // Even with the focus on one of the dialog's own buttons rather than its input.
        await ReplaceQueryAsync("/", "theme");
        await ExpectFirstItemAsync("preferences.theme");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Crumbs).ToHaveTextAsync(["Theme"]);
        await Dialog.Locator(".bit-cpl-crumb-home").FocusAsync();
        await Page.Keyboard.PressAsync("/");
        await Page.WaitForTimeoutAsync(300);

        await Expect(SiteSearch).ToBeHiddenAsync();
        await Expect(Dialog).ToBeVisibleAsync();
    }
}
