using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the labels of filled and selected parts under forced colors (Windows contrast themes).
/// The forced palette fills the primary role with LinkText under a Canvas label, two system colors the mode keeps, and
/// Chromium draws a Canvas backplate behind every line of text whose element does not opt out of the mode - so a label
/// left on the role's on-color is Canvas on Canvas there, while Firefox, which draws no backplate, reads it against the
/// fill. Every line of text on the page is therefore read against both: the Canvas behind it unless it opts out, and
/// the nearest opaque background that is actually painted. Each component is a ?part= of its own, read at rest and with
/// every control of it hovered and pressed in turn.
///
/// Run them with BROWSER=firefox and BROWSER=webkit as well. WebKit implements no forced-colors mode - Safari never matches
/// the query, and the emulation only flips the media query there, without the palette or the backplate - so there it checks
/// that the pairs the forced-colors blocks paint are legible on their own.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitForcedColorsBrowserTests
public class BitForcedColorsBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/forced-colors";

    // Under WCAG's 3:1 for large text and UI components: a pair below it is one the backplate or the fill hides.
    private const double MinimumContrast = 3;

    // Every control a pointer can be on, of the part and of its popups. The cap keeps a calendar's grid of days from
    // turning one part into hundreds of reads; its first rows hold every kind of day button.
    private const string PointerTargets = "button, a[href], label, [role=button], [role=tab], [role=option], [role=menuitem], [role=radio], [role=checkbox], [role=switch], .bit-ico-int, .bit-bdg-clk, .bit-tgi-tag";
    private const int MaxPointerTargets = 48;

    private static readonly string[] Parts =
    [
        "button", "actionbutton", "menubutton", "menubutton-open", "buttongroup", "togglebutton",
        "calendar", "checkbox", "choicegroup", "circulartimepicker", "datepicker", "daterangepicker", "dropdown", "dropdown-open",
        "fileinput", "fileupload", "numberfield", "rating", "searchbox", "slider", "tagsinput", "textfield", "timepicker", "toggle",
        "header", "footer",
        "carousel", "swiper", "timeline",
        "breadcrumb", "breadcrumb-open", "dropmenu", "dropmenu-open", "nav", "navbar", "pagination", "pivot", "pivot-outline",
        "badge", "message", "persona", "snackbar", "tag",
        "progress",
        "card", "dialog", "dialog-background", "dialog-ok-off", "dialog-busy", "splitter", "tooltip",
        "icon", "text",
        "datagrid", "themeswitcher", "fullcalendar", "markdowneditor", "richtexteditor", "pdfviewer",
    ];

    [DataTestMethod]
    [DataRow("dark")]
    [DataRow("light")]
    public async Task BitComponents_UnderForcedColors_KeepEveryLabelReadableAtRest(string scheme)
    {
        var failures = new List<string>();

        foreach (var part in Parts)
        {
            if (await TryOpen(part, scheme, failures) is false) continue;

            failures.AddRange((await ReadUnreadableText()).Select(f => $"[{part}] {f}"));
        }

        AssertReadable(failures, $"at rest, {scheme} contrast theme");
    }

    [DataTestMethod]
    [DataRow("dark")]
    [DataRow("light")]
    public async Task BitComponents_UnderForcedColors_KeepEveryLabelReadableUnderThePointer(string scheme)
    {
        var failures = new List<string>();

        foreach (var part in Parts)
        {
            if (await TryOpen(part, scheme, failures) is false) continue;

            var targets = Page.Locator($"body :is({PointerTargets}):not(#test-controls *)");
            var count = Math.Min(await targets.CountAsync(), MaxPointerTargets);

            for (var i = 0; i < count; i++)
            {
                var target = targets.Nth(i);
                if (await target.IsVisibleAsync() is false) continue;

                // A part that lets the pointer through to its container (pointer-events: none) is never in the state, and
                // one a moving part (a dial's numbers) hides again before the pointer gets there is not either.
                try
                {
                    await target.HoverAsync(new() { Force = true, Timeout = 2000 });
                }
                catch (Exception exception) when (exception is TimeoutException or PlaywrightException)
                {
                    continue;
                }

                if (await target.EvaluateAsync<bool>("e => e.matches(':hover')") is false) continue;

                var name = await target.EvaluateAsync<string>("e => e.tagName.toLowerCase() + '.' + [...e.classList].join('.')");

                failures.AddRange((await ReadUnreadableText()).Select(f => $"[{part}] hovering #{i} ({name}): {f}"));

                // The press is released away from the control, so it never becomes a click: a click would toggle a
                // button, move a pagination or open a file chooser, and leave the rest of the part in another state.
                await Page.Mouse.DownAsync();
                failures.AddRange((await ReadUnreadableText()).Select(f => $"[{part}] pressing #{i} ({name}): {f}"));
                await Page.Mouse.MoveAsync(1, 1);
                await Page.Mouse.UpAsync();
            }
        }

        AssertReadable(failures, $"under the pointer, {scheme} contrast theme");
    }

    // A Transparent Fill header gets its solid fill back when transparency is reduced, in a rule that follows the
    // forced-colors one; only Chromium can emulate the preference.
    [DataTestMethod]
    [DataRow("dark")]
    [DataRow("light")]
    public async Task BitHeader_UnderForcedColorsAndReducedTransparency_KeepsItsLabelReadable(string scheme)
    {
        if (Page.Context.Browser?.BrowserType.Name != "chromium") Assert.Inconclusive("Only Chromium emulates prefers-reduced-transparency.");

        var session = await Page.Context.NewCDPSessionAsync(Page);
        await session.SendAsync("Emulation.setEmulatedMedia", new Dictionary<string, object>
        {
            ["features"] = new object[]
            {
                new Dictionary<string, string> { ["name"] = "forced-colors", ["value"] = "active" },
                new Dictionary<string, string> { ["name"] = "prefers-color-scheme", ["value"] = scheme },
                new Dictionary<string, string> { ["name"] = "prefers-reduced-transparency", ["value"] = "reduce" },
            }
        });

        await Page.GotoAsync($"{BaseUrl}{PageUrl}?part=header");
        await WaitForStatus("Ready");
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-transparency: reduce)').matches"), "The browser does not emulate reduced transparency.");

        AssertReadable([.. await ReadUnreadableText()], $"with reduced transparency, {scheme} contrast theme");
    }

    // A focus ring drawn by the keyboard is a state of its own (:focus-visible), which neither the pointer nor a scripted
    // focus brings up, so the first button is reached with Tab.
    [DataTestMethod]
    [DataRow("dark")]
    [DataRow("light")]
    public async Task BitButton_UnderForcedColors_KeepsItsLabelReadableUnderKeyboardFocus(string scheme)
    {
        await Open("button", scheme);

        var button = Page.Locator("#case .bit-btn").First;
        await Page.Keyboard.PressAsync("Tab");

        Assert.IsTrue(await button.EvaluateAsync<bool>("e => e === document.activeElement"), "Tab did not reach the first button.");
        Assert.IsTrue(await button.EvaluateAsync<bool>("e => e.matches(':focus-visible')"), "The first button is focused but not :focus-visible.");

        AssertReadable([.. await ReadUnreadableText()], $"under keyboard focus, {scheme} contrast theme");
    }

    // Every item reserves the bar of the selection with a transparent border, which the mode forces to a system color on
    // every row; only the selected item and a collapsed branch holding it may show it, and the bar keeps one width on
    // every item, never under the thick stroke - it is the one mark of the selection left - whatever the public width says.
    [DataTestMethod]
    [DataRow("dark", null)]
    [DataRow("light", null)]
    [DataRow("dark", "0")]
    [DataRow("dark", "0px")]
    [DataRow("dark", "6px")]
    public async Task BitNav_UnderForcedColors_DrawsTheBarOfTheSelectedItemAlone(string scheme, string? indicatorWidth)
    {
        await Open("nav", scheme);

        // The third nav holds its selection in the group, which collapses into a branch drawn as the selected item.
        await Page.Locator("#case .bit-nav").Nth(2).Locator("button.bit-nav-ict").ClickAsync();
        await Page.WaitForSelectorAsync("#case .bit-nav-sca");
        await Page.Mouse.MoveAsync(1, 1);

        if (indicatorWidth is not null)
        {
            await Page.EvaluateAsync("w => document.documentElement.style.setProperty('--bit-Nav-indicator-width', w)", indicatorWidth);
        }

        var palette = await ReadPalette();
        var items = await ReadEdges("#case .bit-nav-ict", ".bit-nav-sel, .bit-nav-sca");
        var thick = await Page.EvaluateAsync<double>("""
            () => {
                const probe = document.createElement('div');
                probe.style.cssText = 'border-left: var(--bit-shp-brd-width-thick) solid;';
                document.body.append(probe);
                const width = parseFloat(getComputedStyle(probe).borderLeftWidth);
                probe.remove();
                return width;
            }
            """);

        Assert.IsTrue(items.Count(i => i.Selected) >= 4, "The page holds no selected item or collapsed branch holding one.");
        Assert.IsTrue(items.Any(i => i.Selected is false), "The page holds no item that is not selected.");

        var failures = new List<string>();
        var widths = items.Select(i => i.InlineStart.Width).Distinct().ToArray();
        if (widths.Length != 1) failures.Add($"The bars differ in width, so the content moves with the selection: {string.Join(", ", widths)}.");
        if (widths.Min() < Math.Max(thick, indicatorWidth == "6px" ? 6 : 0)) failures.Add($"The bar is {widths.Min()}px wide, under the {thick}px of the thick stroke or the width asked for.");

        foreach (var item in items)
        {
            if (item.Selected)
            {
                if (item.InlineStart.Color != palette.Highlight || item.InlineStart.Color == item.Behind) failures.Add($"The bar of the selected {item.Name} is {item.InlineStart.Color} on {item.Behind}, not the system Highlight.");
            }
            else if (item.InlineStart.Color != (palette.Forcing ? item.Behind : palette.Canvas))
            {
                failures.Add($"The {item.Name} that is not selected draws its bar, {item.InlineStart.Color} on {item.Behind}.");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
    }

    // The rows of the overflow menu reserve a border's room, which the mode would draw around each of them.
    [DataTestMethod]
    [DataRow("dark")]
    [DataRow("light")]
    public async Task BitBreadcrumb_UnderForcedColors_DrawsNoFrameAroundTheOverflowRows(string scheme)
    {
        await Open("breadcrumb-open", scheme);

        var palette = await ReadPalette();
        var rows = await ReadEdges(".bit-brc-ofi, .bit-brc-ofn", null);

        Assert.IsTrue(rows.Length >= 2, "The overflow menu holds no rows.");

        var failures = rows.SelectMany(row => row.Sides)
                           .Where(side => side.Edge.Color != (palette.Forcing ? side.Row.Behind : palette.Canvas))
                           .Select(side => $"The {side.Name} edge of {side.Row.Name} is drawn, {side.Edge.Color} on {side.Row.Behind}.")
                           .ToArray();

        Assert.AreEqual(0, failures.Length, string.Join("\n", failures));
    }

    // Every Outline tab reserves the frame of the selection; the mode would box every tab alike and close the selected one
    // off from the panel. The tabs that are not selected hide every edge, the selected one is framed in CanvasText, open on
    // the panel's side and accented in Highlight (GrayText disabled), and the rule is laid over the tabs, the selected one
    // over the rule, so it runs on under every tab.
    [DataTestMethod]
    [DataRow("dark")]
    [DataRow("light")]
    public async Task BitPivot_UnderForcedColors_FramesTheSelectedOutlineTabAlone(string scheme)
    {
        await Open("pivot-outline", scheme);

        var palette = await ReadPalette();
        var tabs = await ReadEdges("#case .bit-pvt-oln > .bit-pvt-hwr > .bit-pvt-hct > .bit-pvti", ".bit-pvti-sel");
        var layers = await Page.EvaluateAsync<string[]>("""
            () => [...document.querySelectorAll('#case .bit-pvt-oln')].map(p =>
                getComputedStyle(p.querySelector(':scope > .bit-pvt-hwr'), '::before').zIndex + '/' + getComputedStyle(p.querySelector('.bit-pvti-sel')).zIndex)
            """);

        Assert.AreEqual(5, layers.Length, "The page holds no Outline pivot for every placement and a disabled one.");
        Assert.AreEqual(5, tabs.Count(t => t.Selected), "Not every Outline pivot holds a selected tab.");

        var failures = layers.Where(l => l != "1/2").Select(l => $"The rule and the selected tab are not stacked rule over tabs, selected tab over rule: {l}.").ToList();

        foreach (var tab in tabs)
        {
            var (far, near) = tab.Placement switch
            {
                "bit-pvt-btm" => ("bottom", "top"),
                "bit-pvt-sta" => ("left", "right"),
                "bit-pvt-end" => ("right", "left"),
                _ => ("top", "bottom"),
            };

            foreach (var side in tab.Sides)
            {
                var expected = tab.Selected is false ? (palette.Forcing ? tab.Behind : palette.Canvas)
                             : side.Name == near ? (palette.Forcing ? tab.Own : palette.Canvas)
                             : side.Name == far ? (tab.Disabled ? palette.GrayText : palette.Highlight)
                             : palette.CanvasText;

                if (side.Edge.Color != expected) failures.Add($"[{tab.Placement}] The {side.Name} edge of {tab.Name} is {side.Edge.Color} on {tab.Behind}, not {expected}.");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
    }

    private sealed record Edge(string Color, double Width);

    private sealed record EdgedPart(string Name, bool Selected, bool Disabled, string Placement, string Own, string Behind, Edge Top, Edge Right, Edge Bottom, Edge Left, Edge InlineStart)
    {
        public IEnumerable<(string Name, Edge Edge, EdgedPart Row)> Sides => [("top", Top, this), ("right", Right, this), ("bottom", Bottom, this), ("left", Left, this)];
    }

    private sealed record Palette(bool Forcing, string Canvas, string CanvasText, string Highlight, string GrayText);

    // Playwright builds a returned object through a parameterless constructor, which a positional record has none of.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // The system colors as this engine computes them, and whether it repaints the page from them at all: WebKit only
    // matches the query, so there the forced-colors blocks are checked for the colors they declare rather than against
    // backgrounds the engine never forces to Canvas.
    private async Task<Palette> ReadPalette()
    {
        return JsonSerializer.Deserialize<Palette>(await Page.EvaluateAsync<string>("""
            () => {
                const resolve = color => {
                    const probe = document.createElement('div');
                    probe.style.cssText = `color: ${color}; forced-color-adjust: none;`;
                    document.body.append(probe);
                    const value = getComputedStyle(probe).color;
                    probe.remove();
                    return value;
                };
                return JSON.stringify({
                    forcing: CSS.supports('forced-color-adjust', 'none'),
                    canvas: resolve('Canvas'),
                    canvasText: resolve('CanvasText'),
                    highlight: resolve('Highlight'),
                    grayText: resolve('GrayText'),
                });
            }
            """), Json)!;
    }

    // The border edges of every element the selector finds, with the background it paints and the nearest opaque one
    // behind its border - its own, or an ancestor's - which an edge meant to vanish has to match.
    private async Task<EdgedPart[]> ReadEdges(string selector, string? selected)
    {
        return JsonSerializer.Deserialize<EdgedPart[]>(await Page.EvaluateAsync<string>("""
            ([selector, selected]) => {
                const channels = value => (value.match(/[\d.]+/g) || []).map(Number);
                const opaque = color => { const alpha = channels(color)[3]; return alpha === undefined || alpha > 0.5; };
                const behind = element => {
                    for (let e = element; e; e = e.parentElement) {
                        const color = getComputedStyle(e).backgroundColor;
                        if (opaque(color)) return color;
                    }
                    // Nothing paints one: what shows is the canvas itself.
                    const probe = document.createElement('div');
                    probe.style.cssText = 'color: Canvas; forced-color-adjust: none;';
                    document.body.append(probe);
                    const canvas = getComputedStyle(probe).color;
                    probe.remove();
                    return canvas;
                };
                const edge = (style, side) => ({ color: style[`border${side}Color`], width: parseFloat(style[`border${side}Width`]) });
                return JSON.stringify([...document.querySelectorAll(selector)].map(e => {
                    const style = getComputedStyle(e);
                    const root = e.closest('.bit-pvt, .bit-nav, .bit-brc, .bit-brc-cal');
                    return {
                        name: `${e.tagName.toLowerCase()}.${[...e.classList].join('.')} "${e.textContent.trim().slice(0, 12)}"`,
                        selected: !!selected && e.matches(selected),
                        disabled: !!e.closest('.bit-dis'),
                        placement: [...(root?.classList ?? [])].find(c => /^bit-pvt-(top|btm|sta|end)$/.test(c)) ?? '',
                        own: style.backgroundColor,
                        behind: behind(e),
                        top: edge(style, 'Top'),
                        right: edge(style, 'Right'),
                        bottom: edge(style, 'Bottom'),
                        left: edge(style, 'Left'),
                        inlineStart: edge(style, 'InlineStart'),
                    };
                }));
            }
            """, new[] { selector, selected }), Json)!;
    }

    // A part that cannot be brought up is a failure of its own, reported with the others rather than ending the run.
    private async Task<bool> TryOpen(string part, string scheme, List<string> failures)
    {
        try
        {
            await Open(part, scheme);
            return true;
        }
        catch (Exception exception) when (exception is TimeoutException or PlaywrightException)
        {
            failures.Add($"[{part}] could not be opened: {exception.Message.Split('\n')[0]}");
            return false;
        }
    }

    private async Task Open(string part, string scheme)
    {
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active, ColorScheme = scheme == "dark" ? ColorScheme.Dark : ColorScheme.Light });
        await Page.GotoAsync($"{BaseUrl}{PageUrl}?part={part}");
        await WaitForStatus("Ready");

        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"), "The browser does not emulate forced colors.");

        // A state's colors arrive through a short transition, and until it ends a part still reports the colors it left.
        await Page.AddStyleTagAsync(new() { Content = "*, *::before, *::after { transition: none !important; animation-duration: 0s !important; }" });

        switch (part)
        {
            case "dialog-busy":
                // Pressed, the Ok button waits on a callback that never ends, held shut through aria-disabled alone.
                await Page.Locator(".bit-dlg-okb").ClickAsync();
                await Page.WaitForSelectorAsync(".bit-dlg-okb[aria-busy='true']");
                break;

            case "breadcrumb-open":
                await Page.Locator(".bit-brc-obt").ClickAsync();
                await Page.WaitForSelectorAsync(".bit-brc-ofi");
                break;

            case "header":
                // The skip link is only drawn while it has the focus.
                await Page.Locator(".bit-hdr-skp").First.FocusAsync();
                break;
        }

        // The parts open in a popup, and the snack bar's items, settle a frame after the render that shows them.
        await Page.WaitForTimeoutAsync(150);
    }

    private static void AssertReadable(List<string> unreadable, string when)
    {
        Assert.AreEqual(0, unreadable.Count, $"Unreadable text {when}:\n{string.Join("\n", unreadable)}");
    }

    /// <summary>
    /// Every visible line of text on the page outside the test's own controls - a text node or an icon glyph - whose color
    /// does not contrast with what is behind it: the Canvas backplate unless its element opts out of forced colors, and the
    /// nearest opaque background.
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
                // rgb()/rgba() report 0-255 channels; WebKit reports some system colors as color(srgb r g b) in 0-1.
                const channels = value => {
                    const numbers = (value.match(/[\d.]+/g) || []).map(Number);
                    return value.startsWith('color(') ? numbers.map((n, i) => i < 3 ? n * 255 : n) : numbers;
                };
                const luminance = value => {
                    const [r, g, b] = channels(value).map(c => { c /= 255; return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4; });
                    return 0.2126 * r + 0.7152 * g + 0.0722 * b;
                };
                const contrast = (a, b) => {
                    const [x, y] = [luminance(a), luminance(b)].sort((m, n) => n - m);
                    return (x + 0.05) / (y + 0.05);
                };
                const canvas = resolve('Canvas');
                // An engine that implements forced colors (Chromium, Firefox) repaints the page from the system palette;
                // WebKit only matches the media query, so the page keeps the theme's own colors there and no backplate
                // is drawn - the blocks are checked for the pairs they paint, and a disabled part keeps the theme's own
                // dimmed colors, which WCAG 1.4.3 exempts.
                const forcing = CSS.supports('forced-color-adjust', 'none');
                // The selection pairs are the contrast theme's own: the user picks them together, so whatever the
                // engine's built-in palette makes of them (Firefox's emulation pairs white, and its white Canvas, with
                // a mid blue), they are the pairs that theme promises for selected text and for a selected glyph.
                const selection = [resolve('HighlightText'), resolve('Highlight')];
                const highlight = [resolve('Highlight'), canvas];
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
                const controls = document.getElementById('test-controls');

                const found = [];
                for (const element of document.body.querySelectorAll('*')) {
                    if (controls?.contains(element)) continue;
                    if (element.closest('script, style, noscript, template')) continue;

                    const text = [...element.childNodes].some(n => n.nodeType === Node.TEXT_NODE && n.textContent.trim());
                    if (!text && !glyph(element)) continue;

                    const style = getComputedStyle(element);
                    if (hidden(element, style)) continue;
                    if (!forcing && element.closest('.bit-dis, :disabled, [aria-disabled="true"]')) continue;

                    const behind = backgroundOf(element);
                    const pairs = !forcing || style.forcedColorAdjust === 'none' ? [behind] : [canvas, behind];
                    for (const background of pairs) {
                        if (style.color === selection[0] && background === selection[1]) continue;
                        if (style.color === highlight[0] && background === highlight[1]) continue;

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
