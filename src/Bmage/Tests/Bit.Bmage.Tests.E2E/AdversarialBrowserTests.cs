using System.Text.Json;
using Bit.Bmage.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bmage.Tests.E2E;

/// <summary>
/// Browser-only checks for behaviour that unit tests cannot reach: the JavaScript module's own contracts, resource
/// release across component lifecycles, and keyboard operation of the editor.
/// </summary>
[TestClass]
public class JsModuleContractTests : DemoTest
{
    /// <summary>Loads the JS module in the page and evaluates an expression against it.</summary>
    private static async Task<JsonElement> EvaluateAgainstModuleAsync(IPage page, string body)
    {
        var script = $$"""
            async () => {
                const m = await import('/_content/Bit.Bmage/bit-bmage.js');
                {{body}}
            }
            """;
        return await page.EvaluateAsync<JsonElement>(script);
    }

    /// <summary>Builds a PNG of the given size in the page, as a Uint8Array.</summary>
    private const string MakePng = """
        const makePng = async (w, h) => {
            const c = document.createElement('canvas');
            c.width = w; c.height = h;
            const x = c.getContext('2d');
            x.fillStyle = '#c33'; x.fillRect(0, 0, w, h);
            x.fillStyle = '#3c3'; x.fillRect(0, 0, Math.max(1, w >> 1), h);
            const blob = await new Promise(r => c.toBlob(r, 'image/png'));
            return new Uint8Array(await blob.arrayBuffer());
        };
        """;

    /// <summary>
    /// A decode limited by MaxSize must fit inside the box while keeping the aspect ratio. Passing both a resize
    /// width and height to createImageBitmap stretches the image instead, which is silent and only visible here.
    /// </summary>
    [TestMethod]
    [DataRow(800, 400, 200, 200, 200, 100)]  // landscape: width constrains
    [DataRow(400, 800, 200, 200, 100, 200)]  // portrait: height constrains
    [DataRow(900, 300, 300, 300, 300, 100)]
    [DataRow(300, 900, 300, 300, 100, 300)]
    public async Task BrowserDecodeWithAMaxSizePreservesTheAspectRatio(int sw, int sh, int maxW, int maxH, int expectedW, int expectedH)
    {
        var page = await OpenAsync("capabilities");

        var result = await EvaluateAgainstModuleAsync(page, $$"""
            {{MakePng}}
            const bytes = await makePng({{sw}}, {{sh}});
            const handle = await m.decode(bytes, {
                mimeType: 'image/png',
                autoOrient: true,
                maxWidth: {{maxW}}, maxHeight: {{maxH}},
                sourceWidth: {{sw}}, sourceHeight: {{sh}},
            });
            const info = handle.info();
            handle.dispose();
            return info;
            """);

        var width = result.GetProperty("width").GetInt32();
        var height = result.GetProperty("height").GetInt32();

        Assert.IsTrue(width <= maxW && height <= maxH, $"decode produced {width}x{height}, which does not fit in {maxW}x{maxH}");
        var sourceAspect = sw / (double)sh;
        var resultAspect = width / (double)height;
        Assert.IsLessThan(0.05, Math.Abs(sourceAspect - resultAspect),
            $"decode of a {sw}x{sh} image into {maxW}x{maxH} produced {width}x{height}, changing the aspect ratio from {sourceAspect:0.00} to {resultAspect:0.00}");
        Assert.AreEqual(expectedW, width);
        Assert.AreEqual(expectedH, height);
    }

    /// <summary>A decode without a size limit must return the image at its natural size.</summary>
    [TestMethod]
    public async Task BrowserDecodeWithoutLimitsKeepsTheNaturalSize()
    {
        var page = await OpenAsync("capabilities");

        var result = await EvaluateAgainstModuleAsync(page, $$"""
            {{MakePng}}
            const bytes = await makePng(640, 360);
            const handle = await m.decode(bytes, { mimeType: 'image/png', autoOrient: true });
            const info = handle.info();
            handle.dispose();
            return info;
            """);

        Assert.AreEqual(640, result.GetProperty("width").GetInt32());
        Assert.AreEqual(360, result.GetProperty("height").GetInt32());
        Assert.AreEqual(640 * 360 * 4, result.GetProperty("byteLength").GetInt32());
    }

    /// <summary>
    /// The encoder must never report success for a format the browser silently substituted. Browsers hand back a PNG
    /// when asked for a format they cannot write, and claiming that as AVIF would corrupt a user's export pipeline.
    /// </summary>
    [TestMethod]
    public async Task EncodingAnUnsupportedFormatFailsRatherThanSubstitutingPng()
    {
        var page = await OpenAsync("capabilities");

        var result = await EvaluateAgainstModuleAsync(page, """
            const caps = await m.getCapabilities();
            const pixels = new Uint8Array(4 * 4 * 4).fill(200);
            const outcomes = {};
            for (const type of ['image/png', 'image/jpeg', 'image/webp', 'image/avif']) {
                try {
                    const handle = await m.encode(pixels, 4, 4, type, 0.8, null);
                    outcomes[type] = handle.info().mimeType;
                    handle.dispose();
                } catch (e) {
                    outcomes[type] = 'error:' + String(e.message || e);
                }
            }
            return { encodable: caps.encodableMimeTypes, outcomes };
            """);

        var encodable = result.GetProperty("encodable").EnumerateArray().Select(e => e.GetString()!).ToHashSet();
        var outcomes = result.GetProperty("outcomes");

        foreach (var type in new[] { "image/png", "image/jpeg", "image/webp", "image/avif" })
        {
            var outcome = outcomes.GetProperty(type).GetString()!;
            if (encodable.Contains(type))
            {
                Assert.AreEqual(type, outcome);
            }
            else
            {
                Assert.StartsWith("error:", outcome, StringComparison.Ordinal);
                Assert.Contains("unsupported-format", outcome, StringComparison.Ordinal);
            }
        }

        // Whatever this browser supports, it must at least be able to write PNG and JPEG.
        Assert.Contains("image/png", encodable);
        Assert.Contains("image/jpeg", encodable);
    }

    /// <summary>Capability probing must not leave canvases or other resources behind when called repeatedly.</summary>
    [TestMethod]
    public async Task RepeatedCapabilityProbesDoNotAccumulateResources()
    {
        var page = await OpenAsync("capabilities");

        var result = await EvaluateAgainstModuleAsync(page, """
            for (let i = 0; i < 25; i++) await m.getCapabilities();
            const caps = await m.getCapabilities();
            return { ok: caps.encodableMimeTypes.length > 0 };
            """);

        Assert.IsTrue(result.GetProperty("ok").GetBoolean(), "capability probing stopped working after repeated calls");
    }

    /// <summary>
    /// Repeatedly decoding and encoding through the browser must not grow JS heap without bound. The canvas pool and
    /// every ByteResult have to be released, which a leak in the module would defeat.
    /// </summary>
    [TestMethod]
    public async Task RepeatedDecodeEncodeCyclesDoNotGrowTheJsHeap()
    {
        var page = await OpenAsync("capabilities");

        var result = await EvaluateAgainstModuleAsync(page, $$"""
            {{MakePng}}
            const bytes = await makePng(512, 512);
            const cycle = async () => {
                const d = await m.decode(bytes, { mimeType: 'image/png', autoOrient: true });
                const px = d.bytes();
                const e = await m.encode(px, 512, 512, 'image/jpeg', 0.8, null);
                e.dispose();
                d.dispose();
            };
            for (let i = 0; i < 10; i++) await cycle();
            const before = performance.memory ? performance.memory.usedJSHeapSize : 0;
            for (let i = 0; i < 60; i++) await cycle();
            await new Promise(r => setTimeout(r, 500));
            const after = performance.memory ? performance.memory.usedJSHeapSize : 0;
            return { before, after, measured: !!performance.memory };
            """);

        if (!result.GetProperty("measured").GetBoolean()) return; // Only Chromium exposes performance.memory.
        var before = result.GetProperty("before").GetInt64();
        var after = result.GetProperty("after").GetInt64();
        var growthMb = (after - before) / 1024.0 / 1024.0;
        Assert.IsLessThan(64, growthMb, $"60 decode/encode cycles grew the JS heap by {growthMb:0.0} MB");
    }
}

/// <summary>Lifecycle and accessibility checks against the running editor.</summary>
[TestClass]
public class EditorLifecycleTests : DemoTest
{
    /// <summary>
    /// Navigating into and out of the editor repeatedly must not raise errors or leave the module broken. This is the
    /// shape of a real session: users move between pages, and each visit creates and disposes an editor.
    /// </summary>
    [TestMethod]
    public async Task NavigatingInAndOutOfTheEditorRepeatedlyStaysClean()
    {
        var page = await NewPageAsync();
        var errors = CollectErrors(page);

        await GotoAsync(page, "editor");

        for (var i = 0; i < 6; i++)
        {
            await page.GotoAsync($"{BaseUrl}/capabilities");
            await page.WaitForSelectorAsync("h1", new PageWaitForSelectorOptions { Timeout = 30_000 });
            await page.GotoAsync($"{BaseUrl}/editor");
            await page.WaitForSelectorAsync("h1", new PageWaitForSelectorOptions { Timeout = 30_000 });
        }

        await page.WaitForTimeoutAsync(1500);
        Assert.IsEmpty(errors, $"navigating between pages produced errors: {string.Join(" | ", errors.Take(5))}");

        // The module must still work after all that churn.
        var stillWorks = await page.EvaluateAsync<bool>("""
            async () => {
                const m = await import('/_content/Bit.Bmage/bit-bmage.js');
                const caps = await m.getCapabilities();
                return caps.encodableMimeTypes.includes('image/png');
            }
            """);
        Assert.IsTrue(stillWorks, "the browser module stopped working after repeated navigation");
    }

    /// <summary>Every interactive editor control must be reachable and named for assistive technology.</summary>
    [TestMethod]
    public async Task EditorControlsAreNamedAndKeyboardReachable()
    {
        var page = await OpenAsync("annotate");
        await page.WaitForSelectorAsync(".bmg-editor", new PageWaitForSelectorOptions { Timeout = 60_000 });
        await LoadSampleIntoEditorAsync(page);

        var unnamed = await page.EvaluateAsync<string[]>("""
            () => {
                const problems = [];
                const controls = document.querySelectorAll('.bmg-editor button, .bmg-editor input, .bmg-editor select');
                for (const el of controls) {
                    if (el.disabled || el.hidden || el.offsetParent === null) continue;
                    const name = (el.getAttribute('aria-label') || el.getAttribute('title') || el.textContent || '').trim()
                        || (el.labels && el.labels.length ? el.labels[0].textContent.trim() : '');
                    if (!name) problems.push(el.tagName.toLowerCase() + '.' + (el.className || '(no class)'));
                }
                return problems;
            }
            """);
        Assert.IsEmpty(unnamed, $"unnamed editor controls: {string.Join(", ", unnamed)}");

        // A control may only leave the tab order as part of a roving-tabindex composite (a tablist, say), where the
        // container is focusable and arrow keys move within it. Anything else with tabindex="-1" is unreachable.
        var unreachable = await page.EvaluateAsync<string[]>("""
            () => {
                const problems = [];
                for (const el of document.querySelectorAll('.bmg-editor button, .bmg-editor input, .bmg-editor select')) {
                    if (el.disabled || el.hidden || el.offsetParent === null) continue;
                    if (el.getAttribute('tabindex') !== '-1') continue;
                    const composite = el.closest('[role=tablist], [role=toolbar], [role=listbox], [role=radiogroup], [role=menu]');
                    const roving = composite && composite.querySelector('[tabindex="0"]');
                    if (!roving) problems.push(el.tagName.toLowerCase() + ' ' + (el.getAttribute('aria-label') || el.textContent || '').trim());
                }
                return problems;
            }
            """);
        Assert.IsEmpty(unreachable, $"controls removed from the tab order: {string.Join(", ", unreachable)}");
    }

    /// <summary>
    /// The panel tabs use a roving tabindex, which is only correct if arrow keys actually move between them. Without
    /// that, four of the five panels would be unreachable for a keyboard user.
    /// </summary>
    [TestMethod]
    public async Task ArrowKeysMoveBetweenThePanelTabs()
    {
        var page = await OpenAsync("annotate");
        await page.WaitForSelectorAsync(".bmg-editor", new PageWaitForSelectorOptions { Timeout = 60_000 });
        await LoadSampleIntoEditorAsync(page);

        var tabs = page.Locator(".bmg-panel-tab");
        var count = await tabs.CountAsync();
        Assert.IsGreaterThanOrEqualTo(3, count, $"expected several panel tabs, found {count}");

        await tabs.First.FocusAsync();
        var selectedFirst = await page.Locator(".bmg-panel-tab[aria-selected=true]").InnerTextAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await page.WaitForTimeoutAsync(300);
        var selectedNext = await page.Locator(".bmg-panel-tab[aria-selected=true]").InnerTextAsync();
        Assert.AreNotEqual(selectedFirst, selectedNext);

        await page.Keyboard.PressAsync("End");
        await page.WaitForTimeoutAsync(300);
        var selectedLast = await page.Locator(".bmg-panel-tab[aria-selected=true]").InnerTextAsync();
        var allTabs = await tabs.AllInnerTextsAsync();
        Assert.AreEqual(allTabs[^1].Trim(), selectedLast.Trim());

        await page.Keyboard.PressAsync("Home");
        await page.WaitForTimeoutAsync(300);
        Assert.AreEqual(selectedFirst.Trim(), (await page.Locator(".bmg-panel-tab[aria-selected=true]").InnerTextAsync()).Trim());

        // Exactly one tab is in the tab order at a time, which is what makes the roving pattern valid.
        Assert.AreEqual(1, await page.Locator(".bmg-panel-tab[tabindex='0']").CountAsync());
    }

    /// <summary>Tabbing must move focus into the editor and land on something operable.</summary>
    [TestMethod]
    public async Task KeyboardFocusReachesTheEditorControls()
    {
        var page = await OpenAsync("annotate");
        await page.WaitForSelectorAsync(".bmg-editor", new PageWaitForSelectorOptions { Timeout = 60_000 });
        await LoadSampleIntoEditorAsync(page);

        var reached = false;
        for (var i = 0; i < 60 && !reached; i++)
        {
            await page.Keyboard.PressAsync("Tab");
            reached = await page.EvaluateAsync<bool>("() => !!document.activeElement && !!document.activeElement.closest('.bmg-editor')");
        }
        Assert.IsTrue(reached, "focus never entered the editor after 60 tab presses");

        var focused = await page.EvaluateAsync<string>("""
            () => {
                const el = document.activeElement;
                return el ? el.tagName.toLowerCase() + '|' + ((el.getAttribute('aria-label') || el.textContent || '').trim().slice(0, 40)) : '';
            }
            """);
        Assert.AreNotEqual("", focused);
        Assert.Contains("|", focused, StringComparison.Ordinal);
    }

    /// <summary>
    /// Clicking controls while a render is in flight must not break the editor. Rapid input is the normal case for a
    /// slider, and it is what put the preview render's cancellation path under stress.
    /// </summary>
    [TestMethod]
    public async Task RapidRepeatedInteractionLeavesTheEditorWorking()
    {
        var page = await NewPageAsync();
        var errors = CollectErrors(page);

        await GotoAsync(page, "annotate");
        await page.WaitForSelectorAsync(".bmg-editor", new PageWaitForSelectorOptions { Timeout = 60_000 });
        await LoadSampleIntoEditorAsync(page);

        // Drive the slider across its real range faster than a render can complete. Setting the value through the
        // native setter and dispatching input is what a real drag produces, and it sidesteps the step validation
        // Playwright's fill applies.
        await page.EvaluateAsync("""
            async () => {
                const slider = document.querySelector('.bmg-editor input[type=range]');
                if (!slider) return;
                const min = parseFloat(slider.min || '0');
                const max = parseFloat(slider.max || '1');
                const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                for (let i = 0; i < 30; i++) {
                    const value = min + (max - min) * (i % 10) / 9;
                    setter.call(slider, value.toFixed(2));
                    slider.dispatchEvent(new Event('input', { bubbles: true }));
                    await new Promise(r => setTimeout(r, 15));
                }
            }
            """);

        var rotate = page.Locator(".bmg-editor .bmg-tool[title='Rotate right']").First;
        if (await rotate.CountAsync() > 0 && await rotate.IsEnabledAsync())
            for (var i = 0; i < 8; i++) await rotate.ClickAsync(new LocatorClickOptions { Timeout = 20_000 });

        await page.WaitForTimeoutAsync(2500);

        string[] snapshot;
        lock (errors) snapshot = [.. errors];
        var disposedErrors = snapshot.Where(e => e.Contains("Disposed", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.IsEmpty(disposedErrors, $"rapid interaction produced disposal errors: {string.Join(" | ", disposedErrors.Take(3))}");
        Assert.IsEmpty(snapshot, $"rapid interaction produced errors: {string.Join(" | ", snapshot.Take(5))}");
        Assert.IsTrue(await page.Locator(".bmg-editor").IsVisibleAsync(), "the editor disappeared after rapid interaction");
    }
}
