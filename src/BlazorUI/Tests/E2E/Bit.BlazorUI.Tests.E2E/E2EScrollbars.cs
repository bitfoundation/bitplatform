namespace Bit.BlazorUI.Tests.E2E;

/// <summary>
/// The scrollbar-width an element ends up with, read in every engine. Headless, Playwright's Firefox hides every
/// scrollbar with an agent stylesheet of its own - <c>* { scrollbar-width: none !important }</c>, Juggler's
/// hidden-scrollbars.css - so its computed value there is none whatever the page says, auto included. Where that is so,
/// the value the page declares is what is read instead: the element's own style, or else the most specific of the page's
/// rules that match it (the later of two that are as specific), the way the cascade picks it.
/// </summary>
internal static class E2EScrollbars
{
    private const string DeclaredScript = """
        el => {
            const probe = document.createElement('div');
            probe.style.scrollbarWidth = 'auto';
            document.body.appendChild(probe);
            const forced = getComputedStyle(probe).scrollbarWidth !== 'auto';
            probe.remove();
            if (forced === false) return null;

            if (el.style.scrollbarWidth) return el.style.scrollbarWidth;

            const specificity = selector => {
                const plain = selector.replace(/::?[\w-]+\(/g, ' (');
                return [(plain.match(/#[\w-]+/g) || []).length,
                        (plain.match(/\.[\w-]+|\[[^\]]*\]|:(?!:)[\w-]+/g) || []).length,
                        (plain.match(/(^|[\s>+~(])[a-z][\w-]*/gi) || []).length];
            };
            let best = null, bestImportant = false, bestSpecificity = [-1, -1, -1];
            const walk = rules => {
                for (const rule of rules) {
                    if (rule instanceof CSSMediaRule && matchMedia(rule.conditionText).matches === false) continue;
                    if (rule instanceof CSSStyleRule) {
                        const value = rule.style.getPropertyValue('scrollbar-width').trim();
                        const important = rule.style.getPropertyPriority('scrollbar-width') === 'important';
                        if (value) {
                            for (const selector of rule.selectorText.split(',')) {
                                let matches = false;
                                try { matches = el.matches(selector); } catch { }
                                if (matches === false) continue;
                                const s = specificity(selector);
                                const order = (important === bestImportant ? 0 : important ? 1 : -1)
                                           || s[0] - bestSpecificity[0] || s[1] - bestSpecificity[1] || s[2] - bestSpecificity[2];
                                if (order >= 0) { best = value; bestImportant = important; bestSpecificity = s; }
                            }
                        }
                    }
                    if (rule.cssRules) walk(rule.cssRules);
                }
            };
            for (const sheet of document.styleSheets) {
                try { walk(sheet.cssRules); } catch { }
            }
            return best ?? 'auto';
        }
        """;

    /// <summary>
    /// Expects the element's scrollbar-width to be the given one: the computed value where the engine computes it, and
    /// the declared one where it is forced to none (see the class).
    /// </summary>
    public static async Task ExpectScrollbarWidthAsync(this ILocator locator, string expected)
    {
        var declared = await locator.EvaluateAsync<string?>(DeclaredScript);
        if (declared is null)
        {
            await Assertions.Expect(locator).ToHaveCSSAsync("scrollbar-width", expected);
            return;
        }

        // Read again until it holds, the way the assertion it stands in for retries: a parameter can still be on its way.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (declared != expected && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
            declared = await locator.EvaluateAsync<string?>(DeclaredScript);
        }

        Assert.AreEqual(expected, declared, "The scrollbar-width the page declares for the element.");
    }
}
