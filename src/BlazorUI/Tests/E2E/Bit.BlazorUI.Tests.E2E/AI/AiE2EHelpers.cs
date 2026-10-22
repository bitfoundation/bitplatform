using System.Globalization;
using System.Text.Json;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// What the AI E2E tests share: the colors the theme tokens resolve to, the glyph of the AI icon font, the scroll
/// position of a transcript, and a recorder of how the text of an element grows while a reply streams in.
/// </summary>
internal static class AiE2EHelpers
{
    /// <summary>
    /// The family of the icon font the AI package ships its glyphs in (fabric.mdl2.bit.blazoui.ai.scss).
    /// </summary>
    public const string AiGlyphFontFamily = "Fabric MDL2 bit BlazorUI AI";

    /// <summary>
    /// Resolves a CSS color (a token, a system color) to the rgb() the browser computes for it at the document level.
    /// </summary>
    public static Task<string> ResolveColorAsync(this IPage page, string cssColor)
    {
        return page.EvaluateAsync<string>(
            """
            value => {
                const probe = document.createElement('div');
                probe.style.color = value;
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).color;
                probe.remove();
                return resolved;
            }
            """, cssColor);
    }

    /// <summary>
    /// Resolves a CSS font family (a token) to the family list the browser computes for it at the document level.
    /// </summary>
    public static Task<string> ResolveFontFamilyAsync(this IPage page, string cssFontFamily)
    {
        return page.EvaluateAsync<string>(
            """
            value => {
                const probe = document.createElement('div');
                probe.style.fontFamily = value;
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).fontFamily;
                probe.remove();
                return resolved;
            }
            """, cssFontFamily);
    }

    /// <summary>
    /// Resolves a CSS length (a token, a calc()) to the pixels the browser computes for it inside an element, so the
    /// custom properties declared on it and its ancestors are read the way its own rules read them.
    /// </summary>
    public static Task<string> ResolveLengthInAsync(this ILocator locator, string cssLength)
    {
        return locator.EvaluateAsync<string>(
            """
            (e, value) => {
                const probe = document.createElement('div');
                probe.style.position = 'absolute';
                probe.style.width = value;
                e.appendChild(probe);
                const resolved = getComputedStyle(probe).width;
                probe.remove();
                return resolved;
            }
            """, cssLength);
    }

    /// <summary>
    /// The font family and the ::before content of a glyph element of an icon font.
    /// </summary>
    public static async Task<(string FontFamily, string Content)> GlyphAsync(this ILocator icon)
    {
        var result = await icon.EvaluateAsync<string[]>(
            "e => { const s = getComputedStyle(e, '::before'); return [getComputedStyle(e).fontFamily, s.content]; }");

        return (result[0], result[1]);
    }

    /// <summary>
    /// Asserts an element is a glyph of the AI icon font: its family is the font's and its ::before draws a character.
    /// </summary>
    public static async Task AssertAiGlyphAsync(this ILocator icon, string glyphName)
    {
        await Assertions.Expect(icon).ToHaveClassAsync(new System.Text.RegularExpressions.Regex($@"(^|\s)bit-icon-ai--{glyphName}(\s|$)"));

        var (family, content) = await icon.GlyphAsync();
        StringAssert.Contains(family, AiGlyphFontFamily, $"The {glyphName} glyph is drawn from the AI font.");
        Assert.IsFalse(content is "none" or "normal" or "\"\"" or "", $"The {glyphName} glyph draws a character, was {content}.");
    }

    /// <summary>
    /// The scroll position of a scroll container: how far it is scrolled, how tall its content is and how tall it is.
    /// </summary>
    public static async Task<ScrollState> ScrollStateAsync(this ILocator scroller)
    {
        var r = await scroller.EvaluateAsync<double[]>("e => [e.scrollTop, e.scrollHeight, e.clientHeight]");

        return new(r[0], r[1], r[2]);
    }

    /// <summary>
    /// Waits until a scroll container is scrolled to the end of its content, within the given distance.
    /// </summary>
    public static Task WaitForScrolledToBottomAsync(this ILocator scroller, double within = 1)
    {
        return scroller.EvaluateAsync(
            """
            (e, within) => new Promise((resolve, reject) => {
                const started = performance.now();
                const check = () => {
                    if (e.scrollHeight - e.scrollTop - e.clientHeight <= within) return resolve();
                    if (performance.now() - started > 20000) return reject(new Error('Not scrolled to the bottom: ' + (e.scrollHeight - e.scrollTop - e.clientHeight)));
                    requestAnimationFrame(check);
                };
                check();
            })
            """, within);
    }

    /// <summary>
    /// Scrolls a container up by the wheel of the mouse over it, the way a reader scrolls away from the newest message.
    /// The container is brought into the viewport first: a wheel turned where the page shows nothing scrolls nothing.
    /// </summary>
    public static async Task WheelOverAsync(this IPage page, ILocator scroller, double deltaY)
    {
        await scroller.ScrollIntoViewIfNeededAsync();
        var box = await scroller.BoundingBoxAsync();
        await page.Mouse.MoveAsync((float)(box!.X + box.Width / 2), (float)(box.Y + box.Height / 2));
        await page.Mouse.WheelAsync(0, (float)deltaY);
    }

    /// <summary>
    /// Turns the wheel down over a scroll container until it is at the end of its content: one long turn is clamped
    /// short by some engines (Firefox).
    /// </summary>
    public static async Task WheelToBottomAsync(this IPage page, ILocator scroller)
    {
        for (var i = 0; i < 20 && (await scroller.ScrollStateAsync()).FromBottom > 1; i++)
        {
            await page.WheelOverAsync(scroller, 1000);
            await page.WaitForTimeoutAsync(100);
        }
    }

    /// <summary>
    /// Whether the stylesheets of the page match a media query, for an emulation an engine may not apply to them
    /// (prefers-contrast in Firefox, where matchMedia answers it but the rules under it do not apply). Read off a rule
    /// of a probe stylesheet rather than matchMedia for that reason.
    /// </summary>
    public static Task<bool> MatchesMediaAsync(this IPage page, string query) =>
        page.EvaluateAsync<bool>(
            """
            q => {
                const style = document.createElement('style');
                style.textContent = `@media ${q} { .__e2e-mq { width: 7px; } }`;
                document.head.appendChild(style);
                const probe = document.createElement('div');
                probe.className = '__e2e-mq';
                probe.style.position = 'absolute';
                document.body.appendChild(probe);
                const matches = getComputedStyle(probe).width === '7px';
                probe.remove();
                style.remove();
                return matches;
            }
            """, query);

    /// <summary>
    /// Whether the engine knows a CSS property with that value (scrollbar-color, overscroll-behavior in WebKit).
    /// </summary>
    public static Task<bool> SupportsCssAsync(this IPage page, string property, string value) =>
        page.EvaluateAsync<bool>("([p, v]) => CSS.supports(p, v)", new[] { property, value });

    /// <summary>
    /// Starts recording every distinct length the text of the elements a selector finds goes through - the newest of
    /// them, as they are added and as they grow - for <see cref="TextLengthsAsync"/> to read once the stream is over.
    /// </summary>
    public static Task RecordTextLengthsAsync(this IPage page, string key, string selector)
    {
        return page.EvaluateAsync(
            """
            ([key, selector]) => {
                window.__e2eLengths ??= {};
                const lengths = window.__e2eLengths[key] = [];
                const read = () => {
                    const all = document.querySelectorAll(selector);
                    const last = all[all.length - 1];
                    const length = last ? last.textContent.trim().length : 0;
                    if (lengths[lengths.length - 1] !== length) lengths.push(length);
                };
                read();
                new MutationObserver(read).observe(document.body, { childList: true, subtree: true, characterData: true });
            }
            """, new[] { key, selector });
    }

    /// <summary>
    /// The lengths <see cref="RecordTextLengthsAsync"/> has seen so far, in order.
    /// </summary>
    public static async Task<int[]> TextLengthsAsync(this IPage page, string key)
    {
        var result = await page.EvaluateAsync<JsonElement>("key => (window.__e2eLengths ?? {})[key] ?? []", key);

        return [.. result.EnumerateArray().Select(e => e.GetInt32())];
    }

    /// <summary>
    /// The duration, in milliseconds, of the running CSS animation of an element with that name, or null for none.
    /// </summary>
    public static async Task<double?> AnimationDurationAsync(this ILocator locator, string animationName)
    {
        var result = await locator.EvaluateAsync<JsonElement>(
            """
            (e, name) => {
                const a = e.getAnimations().find(x => x.animationName === name);
                return a ? Number(a.effect.getComputedTiming().duration) : null;
            }
            """, animationName);

        return result.ValueKind == JsonValueKind.Null ? null : result.GetDouble();
    }

    /// <summary>
    /// The box of an element in the viewport: left, top, right, bottom.
    /// </summary>
    public static async Task<(double Left, double Top, double Right, double Bottom)> RectAsync(this ILocator locator)
    {
        var r = await locator.EvaluateAsync<double[]>(
            "e => { const r = e.getBoundingClientRect(); return [r.left, r.top, r.right, r.bottom]; }");

        return (r[0], r[1], r[2], r[3]);
    }

    /// <summary>
    /// The box of one character of the text of an element, by its offset in the text the element holds (its text nodes
    /// in document order): where the bidirectional layout actually put it, for telling which way a line reads.
    /// </summary>
    public static async Task<(double Left, double Top, double Right, double Bottom)> CharacterRectAsync(this ILocator locator, int index)
    {
        var r = await locator.EvaluateAsync<double[]>(
            """
            (e, index) => {
                const walker = document.createTreeWalker(e, NodeFilter.SHOW_TEXT);
                let node, offset = index;
                while ((node = walker.nextNode())) {
                    if (offset < node.length) {
                        const range = document.createRange();
                        range.setStart(node, offset);
                        range.setEnd(node, offset + 1);
                        const r = range.getBoundingClientRect();
                        return [r.left, r.top, r.right, r.bottom];
                    }
                    offset -= node.length;
                }
                throw new Error('The element holds no character ' + index + '.');
            }
            """, index);

        return (r[0], r[1], r[2], r[3]);
    }

    /// <summary>
    /// Waits until a condition read off the page holds, for what no auto-waiting assertion reads (a scroll position, the
    /// rendered count of a virtualized list that a script has to scroll again and again).
    /// </summary>
    public static async Task PollAsync(Func<Task<bool>> condition, string message, int timeout = 15000)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        while (await condition() is false)
        {
            if (watch.ElapsedMilliseconds > timeout) Assert.Fail(message);

            await Task.Delay(100);
        }
    }

    /// <summary>
    /// A value in pixels as a number.
    /// </summary>
    public static double ParsePx(string value) => double.Parse(value.Replace("px", "").Trim(), CultureInfo.InvariantCulture);

    /// <summary>
    /// The longest of a list of CSS times ("0.2s, 150ms") in seconds.
    /// </summary>
    public static double ParseSeconds(string value) => value.Split(',').Select(v => v.Trim()).Max(v =>
        v.EndsWith("ms", StringComparison.Ordinal) ? double.Parse(v[..^2], CultureInfo.InvariantCulture) / 1000
        : v.EndsWith('s') ? double.Parse(v[..^1], CultureInfo.InvariantCulture)
        : 0);

    /// <summary>
    /// Whether the AI icon font has been fetched and can draw a glyph (by its code point, e.g. ""), once the
    /// browser was asked to load it.
    /// </summary>
    public static Task<bool> AiGlyphFontLoadsAsync(this IPage page, string glyph)
    {
        return page.EvaluateAsync<bool>(
            """
            async ([family, glyph]) => {
                const font = `16px '${family}'`;
                await document.fonts.load(font, glyph);
                return document.fonts.check(font, glyph) && [...document.fonts].some(f => f.family.replace(/["']/g, '') === family && f.status === 'loaded');
            }
            """, new[] { AiGlyphFontFamily, glyph });
    }

    /// <summary>
    /// An init script (<c>Page.AddInitScriptAsync</c>) standing a scripted recognizer in for the speech API of the
    /// browser, in every engine: <c>window.__e2eSpeech</c> keeps the recognizers made (<c>instances</c>, <c>current</c>)
    /// and plays what one hears - <c>result(final, interim)</c>, <c>error(name)</c>, <c>end()</c>. Stopping one ends
    /// it a moment later, the way a real recognizer reports its end.
    /// </summary>
    public const string FakeSpeechRecognitionScript =
        """
        (() => {
            class E2ESpeechRecognition {
                constructor() {
                    this.lang = '';
                    this.continuous = false;
                    this.interimResults = false;
                    this.started = false;
                    this.stopped = false;
                    window.__e2eSpeech.instances.push(this);
                }
                start() { this.started = true; }
                stop() { this.stopped = true; setTimeout(() => this.onend && this.onend(), 0); }
                abort() { this.stop(); }
            }
            window.__e2eSpeech = {
                instances: [],
                get current() { return this.instances[this.instances.length - 1]; },
                result(finalText, interimText) {
                    const results = [];
                    if (finalText) { const r = [{ transcript: finalText, confidence: 1 }]; r.isFinal = true; results.push(r); }
                    if (interimText) { const r = [{ transcript: interimText, confidence: 0.5 }]; r.isFinal = false; results.push(r); }
                    this.current.onresult({ resultIndex: 0, results });
                },
                error(name) { this.current.onerror({ error: name }); },
                end() { this.current.onend(); },
            };
            window.SpeechRecognition = E2ESpeechRecognition;
            window.webkitSpeechRecognition = E2ESpeechRecognition;
        })();
        """;

    /// <summary>
    /// An init script taking the speech API away, the way a browser without it is - Firefox, or any WebView without it.
    /// </summary>
    public const string NoSpeechRecognitionScript =
        """
        (() => {
            for (const name of ['SpeechRecognition', 'webkitSpeechRecognition']) {
                try { delete window[name]; } catch { }
                try { Object.defineProperty(window, name, { value: undefined, configurable: true, writable: true }); } catch { }
            }
        })();
        """;

    /// <summary>
    /// Dispatches a drag-and-drop event (dragenter, dragover, dragleave, drop) carrying these files on an element, the
    /// way the browser does when files are dragged over it from the desktop, and answers whether the page let the
    /// default action run (false once a listener prevented it). A file is (name, MIME type, size in bytes), filled with
    /// zeros, or <c>bytes</c> when given.
    /// </summary>
    public static Task<bool> DispatchFileDragAsync(this ILocator target, string type, params E2EFile[] files)
    {
        return target.EvaluateAsync<bool>(
            """
            (el, [type, files]) => {
                const dt = new DataTransfer();
                for (const f of files) dt.items.add(new File([f.bytes ? new Uint8Array(f.bytes) : new Uint8Array(f.size)], f.name, { type: f.type }));
                return el.dispatchEvent(new DragEvent(type, { dataTransfer: dt, bubbles: true, cancelable: true }));
            }
            """, new object[] { type, files.Select(f => f.ToScript()).ToArray() });
    }

    /// <summary>
    /// Dispatches a paste of these files (and of a text, when given) on an element, and answers whether the page let the
    /// default action run. Null where the engine does not let a page build a clipboard event carrying that data - WebKit
    /// carries none, Firefox no files.
    /// </summary>
    public static async Task<bool?> DispatchPasteAsync(this ILocator target, string? text, params E2EFile[] files)
    {
        var result = await target.EvaluateAsync<JsonElement>(
            """
            (el, [text, files]) => {
                const dt = new DataTransfer();
                if (text) dt.setData('text/plain', text);
                for (const f of files) dt.items.add(new File([f.bytes ? new Uint8Array(f.bytes) : new Uint8Array(f.size)], f.name, { type: f.type }));
                const e = new ClipboardEvent('paste', { clipboardData: dt, bubbles: true, cancelable: true });
                const data = e.clipboardData;
                if (!data) return null;
                if (Array.from(data.items ?? []).filter(i => i.kind === 'file').length !== files.length) return null;
                if (text && data.getData('text/plain') !== text) return null;
                return el.dispatchEvent(e);
            }
            """, new object?[] { text, files.Select(f => f.ToScript()).ToArray() });

        return result.ValueKind is JsonValueKind.Null ? null : result.GetBoolean();
    }
}

/// <summary>
/// A file a test hands the page through a drop or a paste: its name, its MIME type and its size, filled with zeros, or
/// its bytes when given.
/// </summary>
internal sealed record E2EFile(string Name, string Type, int Size = 1, byte[]? Bytes = null)
{
    public object ToScript() => new { name = Name, type = Type, size = Bytes?.Length ?? Size, bytes = Bytes?.Select(b => (int)b).ToArray() };
}

/// <summary>
/// Where a scroll container is scrolled to.
/// </summary>
internal readonly record struct ScrollState(double Top, double Height, double ClientHeight)
{
    /// <summary>
    /// How far the view is from the end of the content.
    /// </summary>
    public double FromBottom => Height - Top - ClientHeight;
}
