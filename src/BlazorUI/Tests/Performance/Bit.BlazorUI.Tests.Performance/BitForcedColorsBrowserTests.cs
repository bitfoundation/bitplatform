using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the labels of filled and selected parts under forced colors (Windows contrast themes).
/// The forced palette fills the primary role with LinkText under a Canvas label, two system colors the mode keeps, and
/// Chromium draws a Canvas backplate behind every line of text whose element does not opt out of the mode - so a label
/// left on the role's on-color is Canvas on Canvas there, while Firefox, which draws no backplate, reads it against the
/// fill. Every line of text in the fixture is therefore read against both: the Canvas behind it unless it opts out, and
/// the nearest opaque background that is actually painted.
///
/// Run them with BROWSER=firefox as well.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitForcedColorsBrowserTests
public class BitForcedColorsBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/forced-colors";

    // Under WCAG's 3:1 for large text and UI components: a pair below it is one the backplate or the fill hides.
    private const double MinimumContrast = 3;

    private const string HoverTargets = ".bit-btn, .bit-btg-itm, .bit-tgb, .bit-pgn-btn, .bit-fin-lbl, .bit-upl-lbl, .bit-srb-sbn";

    [DataTestMethod]
    [DataRow("dark")]
    [DataRow("light")]
    public async Task BitComponents_UnderForcedColors_KeepEveryLabelReadableAtRest(string scheme)
    {
        await Open(scheme);

        AssertReadable(await ReadUnreadableText(), $"at rest, {scheme} contrast theme");
    }

    [DataTestMethod]
    [DataRow("dark")]
    [DataRow("light")]
    public async Task BitComponents_UnderForcedColors_KeepEveryLabelReadableUnderThePointer(string scheme)
    {
        await Open(scheme);

        var targets = Page.Locator($"#fixture :is({HoverTargets})");
        var count = await targets.CountAsync();
        Assert.IsTrue(count > 0, "The fixture has nothing to hover.");

        for (var i = 0; i < count; i++)
        {
            var target = targets.Nth(i);
            if (await target.IsVisibleAsync() is false) continue;

            // A part that lets the pointer through to its container (pointer-events: none) is never in the state.
            await target.HoverAsync(new() { Force = true });
            if (await target.EvaluateAsync<bool>("e => e.matches(':hover')") is false) continue;

            AssertReadable(await ReadUnreadableText(), $"hovering #{i} ({await target.GetAttributeAsync("class")}), {scheme} contrast theme");

            await Page.Mouse.DownAsync();
            AssertReadable(await ReadUnreadableText(), $"pressing #{i} ({await target.GetAttributeAsync("class")}), {scheme} contrast theme");
            await Page.Mouse.UpAsync();
        }
    }



    private async Task Open(string scheme)
    {
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active, ColorScheme = scheme == "dark" ? ColorScheme.Dark : ColorScheme.Light });
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await Page.WaitForFunctionAsync("() => document.getElementById('status')?.innerText === 'Ready'");

        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"), "The browser does not emulate forced colors.");

        // A state's colors arrive through a short transition, and until it ends a part still reports the colors it left.
        await Page.AddStyleTagAsync(new() { Content = "#fixture, #fixture * { transition: none !important; }" });
    }

    private static void AssertReadable(string[] unreadable, string when)
    {
        Assert.AreEqual(0, unreadable.Length, $"Unreadable text {when}:\n{string.Join("\n", unreadable)}");
    }

    /// <summary>
    /// Every visible line of text in the fixture - a text node or an icon glyph - whose color does not contrast with what is
    /// behind it: the Canvas backplate unless its element opts out of forced colors, and the nearest opaque background.
    /// </summary>
    private async Task<string[]> ReadUnreadableText()
    {
        return await Page.EvaluateAsync<string[]>($$"""
            () => {
                const minimum = {{MinimumContrast}};
                const resolve = color => {
                    const probe = document.createElement('div');
                    probe.style.cssText = `color: ${color}; forced-color-adjust: none;`;
                    document.body.append(probe);
                    const value = getComputedStyle(probe).color;
                    probe.remove();
                    return value;
                };
                const channels = value => (value.match(/[\d.]+/g) || []).map(Number);
                const luminance = value => {
                    const [r, g, b] = channels(value).map(c => { c /= 255; return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4; });
                    return 0.2126 * r + 0.7152 * g + 0.0722 * b;
                };
                const contrast = (a, b) => {
                    const [x, y] = [luminance(a), luminance(b)].sort((m, n) => n - m);
                    return (x + 0.05) / (y + 0.05);
                };
                const canvas = resolve('Canvas');
                // The selection pair is the contrast theme's own: the user picks the two together, so whatever the
                // engine's built-in palette makes of it (Firefox's emulation pairs white with a mid blue), it is the
                // pair that theme promises for selected text.
                const selection = [resolve('HighlightText'), resolve('Highlight')];
                const backgroundOf = element => {
                    for (let e = element; e; e = e.parentElement) {
                        const color = getComputedStyle(e).backgroundColor;
                        const alpha = channels(color)[3];
                        if (alpha === undefined || alpha > 0.5) return color;
                    }
                    return canvas;
                };
                const glyph = element => ['::before', '::after'].some(p => {
                    const content = getComputedStyle(element, p).content;
                    return content !== 'none' && content !== 'normal' && /[^\s"']/.test(content);
                });
                const hidden = (element, style) => {
                    const rect = element.getBoundingClientRect();
                    return rect.width <= 1 || rect.height <= 1 || style.visibility !== 'visible' || +style.opacity === 0;
                };

                const found = [];
                for (const element of document.querySelectorAll('#fixture *')) {
                    const text = [...element.childNodes].some(n => n.nodeType === Node.TEXT_NODE && n.textContent.trim());
                    if (!text && !glyph(element)) continue;

                    const style = getComputedStyle(element);
                    if (hidden(element, style)) continue;

                    const behind = backgroundOf(element);
                    const pairs = style.forcedColorAdjust === 'none' ? [behind] : [canvas, behind];
                    for (const background of pairs) {
                        if (style.color === selection[0] && background === selection[1]) continue;

                        const ratio = contrast(style.color, background);
                        if (ratio < minimum) {
                            found.push(`${element.tagName.toLowerCase()}.${[...element.classList].join('.')} "${element.textContent.trim().slice(0, 20)}": ${style.color} on ${background} (${ratio.toFixed(2)}:1, forced-color-adjust: ${style.forcedColorAdjust})`);
                            break;
                        }
                    }
                }
                return found;
            }
            """);
    }
}
