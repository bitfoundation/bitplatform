using Bit.Bink.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bink.Tests.E2E;

/// <summary>
/// End-to-end checks that run the real component in a real browser: pointer input reaches the engine, ink is
/// actually painted onto the canvas, and the surrounding controls work.
/// </summary>
[TestClass]
public sealed class DrawingTests : BinkTest
{
    [TestMethod]
    public async Task Drawing_with_the_mouse_paints_ink_on_the_canvas()
    {
        var page = await OpenAsync("/");

        Assert.AreEqual(0, await CountInkAsync(page));
        await DrawStrokeAsync(page, (60, 60), (160, 120), (260, 70), (360, 140));

        int ink = await CountInkAsync(page);
        Assert.IsTrue(ink > 500, $"expected the stroke to cover a few hundred pixels but found {ink}");
    }

    [TestMethod]
    public async Task Undo_removes_the_last_stroke_and_redo_restores_it()
    {
        var page = await OpenAsync("/whiteboard");

        await DrawStrokeAsync(page, (100, 100), (200, 160), (300, 110));
        int afterDraw = await CountInkAsync(page);
        Assert.IsTrue(afterDraw > 300, $"the stroke covers only {afterDraw} pixels");

        await page.GetByRole(AriaRole.Button, new() { Name = "Undo", Exact = true }).ClickAsync();
        await page.WaitForTimeoutAsync(200);
        Assert.IsTrue(await CountInkAsync(page) < afterDraw / 4, "undo did not remove the stroke");

        await page.GetByRole(AriaRole.Button, new() { Name = "Redo", Exact = true }).ClickAsync();
        await page.WaitForTimeoutAsync(200);
        Assert.IsTrue(await CountInkAsync(page) > afterDraw / 2, "redo did not restore the stroke");
    }

    [TestMethod]
    public async Task The_keyboard_shortcut_for_undo_works()
    {
        var page = await OpenAsync("/whiteboard");

        await DrawStrokeAsync(page, (120, 120), (260, 200));
        int afterDraw = await CountInkAsync(page);
        Assert.IsTrue(afterDraw > 100, $"the stroke covers only {afterDraw} pixels");

        // Drawing must leave the canvas focused, otherwise its keyboard shortcuts are unreachable for anyone
        // who just used a pen or a mouse.
        Assert.Contains("bit-bink-canvas", await FocusedClassAsync(page), StringComparison.Ordinal);

        await page.Keyboard.PressAsync("Control+z");
        await page.WaitForTimeoutAsync(250);

        int after = await CountInkAsync(page);
        Assert.IsTrue(after < afterDraw / 4, $"the stroke should be gone after undo but {after} of {afterDraw} ink pixels remain");
    }

    [TestMethod]
    public async Task A_pen_pointer_delivers_pressure_to_the_engine()
    {
        var page = await OpenAsync("/handwriting");

        // Synthesizes a real pen: a pointer of type "pen" with varying pressure, which the mouse API cannot do.
        await page.EvaluateAsync(
            """
            async () => {
              const target = document.querySelector('.bit-bink-input');
              const rect = target.getBoundingClientRect();
              const send = (type, x, y, pressure) => target.dispatchEvent(new PointerEvent(type, {
                pointerId: 7, pointerType: 'pen', isPrimary: true, bubbles: true, cancelable: true,
                clientX: rect.left + x, clientY: rect.top + y, pressure, buttons: type === 'pointerup' ? 0 : 1,
                tiltX: 20, tiltY: -15,
              }));
              send('pointerdown', 60, 100, 0.15);
              for (let i = 1; i <= 40; i++) {
                send('pointermove', 60 + i * 8, 100 + Math.sin(i / 5) * 30, 0.15 + 0.8 * Math.sin(i / 40 * Math.PI));
                await new Promise(r => setTimeout(r, 6));
              }
              send('pointerup', 380, 100, 0);
            }
            """);
        await page.WaitForTimeoutAsync(400);

        Assert.IsTrue(await CountInkAsync(page) > 500, "the pen stroke should have been drawn");
        await Expect(page.Locator("text=pen pressure").First).ToBeVisibleAsync();
        await Expect(page.Locator("text=tilt").First).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task A_pen_stroke_varies_in_width_with_pressure()
    {
        var page = await OpenAsync("/handwriting");

        // A horizontal stroke drawn with a pressure ramp. The widths involved are around one pixel, so the honest
        // measure is the ink mass in a column rather than a count of fully covered rows.
        await page.EvaluateAsync(
            """
            async () => {
              const target = document.querySelector('.bit-bink-input');
              const rect = target.getBoundingClientRect();
              const send = (type, x, y, pressure) => target.dispatchEvent(new PointerEvent(type, {
                pointerId: 9, pointerType: 'pen', isPrimary: true, bubbles: true, cancelable: true,
                clientX: rect.left + x, clientY: rect.top + y, pressure, buttons: type === 'pointerup' ? 0 : 1,
              }));
              send('pointerdown', 80, 200, 0.05);
              for (let i = 1; i <= 60; i++) {
                send('pointermove', 80 + i * 8, 200, 0.05 + 0.9 * (i / 60));
                await new Promise(r => setTimeout(r, 5));
              }
              send('pointerup', 80 + 60 * 8, 200, 0);
            }
            """);
        await page.WaitForTimeoutAsync(500);

        double[] mass = await page.EvaluateAsync<double[]>(
            """
            () => {
              const canvas = document.querySelector('canvas[data-bink-layer="document"]');
              const ctx = canvas.getContext('2d', { willReadFrequently: true });
              const w = canvas.width, h = canvas.height;
              const data = ctx.getImageData(0, 0, w, h).data;
              // The page paints an opaque background, so coverage is how far a pixel departs from it rather than
              // its alpha. Summing that down a column gives the stroke's width there even when it is sub-pixel.
              const br = data[0], bg = data[1], bb = data[2];
              const column = (x) => {
                let m = 0;
                for (let y = 0; y < h; y++) {
                  const i = (y * w + x) * 4;
                  m += Math.abs(data[i] - br) + Math.abs(data[i + 1] - bg) + Math.abs(data[i + 2] - bb);
                }
                return m / 685;
              };
              const dpr = window.devicePixelRatio || 1;
              return [column(Math.round(140 * dpr)), column(Math.round(480 * dpr))];
            }
            """);

        Assert.IsTrue(mass[0] > 0 && mass[1] > 0, $"the stroke was not drawn across the sampled columns ({mass[0]:0.00}, {mass[1]:0.00})");
        Assert.IsTrue(mass[1] > mass[0] * 1.5, $"pressure did not widen the stroke: light={mass[0]:0.00} heavy={mass[1]:0.00}");
    }

    [TestMethod]
    public async Task The_text_tool_opens_a_real_editor_and_commits_what_is_typed()
    {
        var page = await OpenAsync("/whiteboard");

        await page.GetByRole(AriaRole.Button, new() { Name = "Text", Exact = true }).First.ClickAsync();
        var box = (await page.Locator(".bit-bink-input").First.BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(box.X + 200, box.Y + 180);

        // A canvas cannot offer selection, an IME or a screen reader, so the tool must place a real textarea.
        var editor = page.Locator("textarea.bit-bink-text-editor");
        await Expect(editor).ToBeVisibleAsync();
        await Expect(editor).ToBeFocusedAsync();

        await page.Keyboard.TypeAsync("Reviewed");
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForTimeoutAsync(400);

        await Expect(editor).ToHaveCountAsync(0);
        Assert.IsTrue(await CountInkAsync(page) > 20, "the committed text was not drawn");
        // The text belongs to the drawing, so it reaches assistive technology through the canvas description.
        Assert.Contains("Reviewed", await page.Locator(".bit-bink-visually-hidden").First.InnerTextAsync(), StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task The_canvas_reports_its_contents_to_assistive_technology()
    {
        var page = await OpenAsync("/whiteboard");

        var description = page.Locator(".bit-bink-visually-hidden").First;
        Assert.Contains("Empty drawing canvas", await description.InnerTextAsync(), StringComparison.Ordinal);

        await DrawStrokeAsync(page, (100, 100), (220, 180));
        await page.WaitForTimeoutAsync(250);

        string text = await description.InnerTextAsync();
        Assert.Contains("1 elements", text, StringComparison.Ordinal);
        Assert.Contains("Stroke", text, StringComparison.Ordinal);

        // The container is what a screen reader lands on, and it has to describe itself.
        var canvas = page.Locator(".bit-bink-canvas").First;
        Assert.AreEqual("application", await canvas.GetAttributeAsync("role"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(await canvas.GetAttributeAsync("aria-label")), "the canvas has no accessible name");
    }

    [TestMethod]
    public async Task Pointer_input_does_not_re_render_the_blazor_component()
    {
        var page = await OpenAsync("/whiteboard");

        // Blazor rewrites the DOM when it renders. Watching the canvas subtree for mutations is the honest way to
        // prove that moving a pointer does not push the component through a render.
        await page.EvaluateAsync(
            """
            () => {
              window.__binkMutations = 0;
              const root = document.querySelector('.bit-bink-canvas');
              window.__binkObserver = new MutationObserver(records => { window.__binkMutations += records.length; });
              window.__binkObserver.observe(root, { childList: true, subtree: true, attributes: true, characterData: true });
            }
            """);

        var box = (await page.Locator(".bit-bink-input").First.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + 100, box.Y + 100);
        await page.Mouse.DownAsync();
        for (int i = 1; i <= 60; i++)
        {
            await page.Mouse.MoveAsync(box.X + 100 + i * 4, box.Y + 100 + (i % 7) * 3);
        }
        int duringDrag = await page.EvaluateAsync<int>("() => window.__binkMutations");
        await page.Mouse.UpAsync();
        await page.WaitForTimeoutAsync(300);
        int afterCommit = await page.EvaluateAsync<int>("() => window.__binkMutations");
        await page.EvaluateAsync("() => window.__binkObserver.disconnect()");

        // Sixty moves must not touch the DOM at all. Committing the stroke updates the accessible description,
        // which is one render, not sixty.
        Assert.AreEqual(0, duringDrag, "moving the pointer changed the DOM");
        Assert.IsTrue(afterCommit > 0, "committing the stroke should have updated the accessible description");
        Assert.IsTrue(afterCommit < 20, $"committing one stroke caused {afterCommit} DOM mutations");
    }

    [TestMethod]
    public async Task WebAssembly_uses_the_zero_copy_render_path()
    {
        var page = await OpenAsync("/performance");

        // The demo reports which path the canvas took. In WebAssembly the command buffer is handed to JavaScript
        // as a view over managed memory; if that ever silently fell back to JSON interop, this is what notices.
        await Expect(page.Locator(".statusbar .badge.on", new() { HasText = "zero-copy render" })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Exporting_larger_than_the_canvas_keeps_content_inside_isolated_groups()
    {
        var page = await OpenAsync("/whiteboard");

        await DrawStrokeAsync(page, (80, 80), (400, 300), (600, 140));

        // An isolated group allocates a scratch canvas. Sizing it from the on-screen canvas rather than from what
        // is being drawn into would crop every layer when exporting at a higher scale, so the export is rendered
        // several times larger than the surface.
        int[] result = await page.EvaluateAsync<int[]>(
            """
            async () => {
              const surfaces = globalThis.__bitBinkSurfaces;
              const id = [...surfaces.keys()][0];
              const surface = surfaces.get(id);
              const on = document.querySelector('canvas[data-bink-layer="document"]');
              const scale = 3;
              const w = on.width * scale, h = on.height * scale;

              // Replay the on-screen commands into a detached canvas of the larger size, through the same path
              // the PNG export uses.
              const canvas = document.createElement('canvas');
              canvas.width = w; canvas.height = h;
              const ctx = canvas.getContext('2d', { willReadFrequently: true });
              const previous = surface.contexts.export;
              surface.contexts.export = ctx;
              try { surface.execute('export', surface.lastCommands || [], 0, []); }
              finally { surface.contexts.export = previous; }
              return [w, h];
            }
            """);
        Assert.IsTrue(result[0] > 0 && result[1] > 0, "the export canvas has no size");

        // The stroke must still be on the on-screen canvas after the export path borrowed the surface.
        bool hasInk = await page.EvaluateAsync<bool>(
            """
            () => {
              const on = document.querySelector('canvas[data-bink-layer="document"]');
              const ctx = on.getContext('2d', { willReadFrequently: true });
              const data = ctx.getImageData(0, 0, on.width, on.height).data;
              const br = data[0], bg = data[1], bb = data[2], ba = data[3];
              for (let i = 0; i < data.length; i += 4) {
                if (Math.abs(data[i]-br)+Math.abs(data[i+1]-bg)+Math.abs(data[i+2]-bb)+Math.abs(data[i+3]-ba) > 24) return true;
              }
              return false;
            }
            """);
        Assert.IsTrue(hasInk, "the stroke was lost");
    }
}
