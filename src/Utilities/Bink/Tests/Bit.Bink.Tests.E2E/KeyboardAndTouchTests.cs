using Bit.Bink.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bink.Tests.E2E;

/// <summary>
/// Browser behaviour that only the browser can settle: which keys the canvas takes and which it leaves to the
/// page, and how touch contacts are routed between drawing and gestures.
/// </summary>
[TestClass]
public sealed class KeyboardAndTouchTests : BinkTest
{
    /// <summary>Sends a sequence of raw touch pointer events, which Playwright's mouse API cannot express.</summary>
    private static Task SendTouchAsync(IPage page, string script) => page.EvaluateAsync(
        """
        async (body) => {
          const target = document.querySelector('.bit-bink-input');
          const rect = target.getBoundingClientRect();
          const send = (type, id, kind, x, y, down, extra) => target.dispatchEvent(new PointerEvent(type, Object.assign({
            pointerId: id, pointerType: kind, isPrimary: id === 1, bubbles: true, cancelable: true,
            clientX: rect.left + x, clientY: rect.top + y, pressure: down ? 0.5 : 0, buttons: down ? 1 : 0,
          }, extra || {})));
          const wait = (ms) => new Promise(r => setTimeout(r, ms));
          await new Function('send', 'wait', 'return (async () => {' + body + '})()')(send, wait);
        }
        """, script);

    [TestMethod]
    public async Task Arrow_keys_scroll_the_page_when_nothing_is_selected()
    {
        var page = await OpenAsync("/whiteboard");
        await page.EvaluateAsync("() => { document.body.style.minHeight = '4000px'; window.scrollTo(0, 0); }");

        await page.Locator(".bit-bink-canvas").First.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.WaitForTimeoutAsync(200);

        // With nothing selected the arrow keys are not the canvas's to take: swallowing them would leave a
        // keyboard user unable to scroll the page while the canvas has focus.
        double scroll = await page.EvaluateAsync<double>("() => window.scrollY");
        Assert.IsTrue(scroll > 0, "the page did not scroll, so the canvas swallowed the arrow keys");
    }

    [TestMethod]
    public async Task Tab_is_left_to_the_page_when_nothing_is_selected()
    {
        var page = await OpenAsync("/whiteboard");

        await page.Locator(".bit-bink-canvas").First.FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await page.WaitForTimeoutAsync(150);

        Assert.DoesNotContain("bit-bink-canvas", await FocusedClassAsync(page), StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task The_canvas_does_not_trap_the_keyboard_when_nothing_is_selected()
    {
        var page = await OpenAsync("/whiteboard");

        // Select something, then press Escape: the selection clears and Tab must take focus out again, so the
        // canvas is never a keyboard trap.
        await DrawStrokeAsync(page, (120, 120), (260, 200));
        await page.Keyboard.PressAsync("Control+a");
        await page.WaitForTimeoutAsync(200);
        await page.Keyboard.PressAsync("Escape");
        await page.WaitForTimeoutAsync(200);
        await page.Keyboard.PressAsync("Tab");
        await page.WaitForTimeoutAsync(150);

        Assert.DoesNotContain("bit-bink-canvas", await FocusedClassAsync(page), StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task The_canvas_takes_the_editing_keys_once_something_is_selected()
    {
        var page = await OpenAsync("/whiteboard");
        await page.EvaluateAsync("() => { document.body.style.minHeight = '4000px'; window.scrollTo(0, 0); }");

        await DrawStrokeAsync(page, (140, 140), (300, 220));
        int drawn = await CountInkAsync(page);
        Assert.IsTrue(drawn > 100, $"the stroke covers only {drawn} pixels");

        await page.Keyboard.PressAsync("Control+a");
        await page.WaitForTimeoutAsync(250);

        // With a selection the arrow keys nudge it instead of scrolling the page.
        await page.Keyboard.PressAsync("ArrowDown");
        await page.WaitForTimeoutAsync(200);
        Assert.AreEqual(0d, await page.EvaluateAsync<double>("() => window.scrollY"), "the arrow key scrolled the page instead of nudging the selection");

        await page.Keyboard.PressAsync("Delete");
        await page.WaitForTimeoutAsync(250);
        Assert.IsTrue(await CountInkAsync(page) < drawn / 4, "Delete did not remove the selection");
    }

    [TestMethod]
    public async Task Delete_removes_the_selection_and_undo_restores_it()
    {
        var page = await OpenAsync("/whiteboard");

        await DrawStrokeAsync(page, (120, 120), (300, 220));
        int drawn = await CountInkAsync(page);
        Assert.IsTrue(drawn > 100, $"the stroke covers only {drawn} pixels");

        await page.Keyboard.PressAsync("Control+a");
        await page.WaitForTimeoutAsync(200);
        await page.Keyboard.PressAsync("Delete");
        await page.WaitForTimeoutAsync(250);
        Assert.IsTrue(await CountInkAsync(page) < drawn / 4, "Delete did not remove the selection");

        await page.Keyboard.PressAsync("Control+z");
        await page.WaitForTimeoutAsync(300);
        Assert.IsTrue(await CountInkAsync(page) > drawn / 2, "undo did not bring the deleted stroke back");
    }

    [TestMethod]
    public async Task Typing_in_child_content_is_not_hijacked_by_the_canvas()
    {
        var page = await OpenAsync("/whiteboard");

        // ChildContent renders inside the canvas element, so a text field in it would otherwise have Ctrl+A,
        // Delete and the arrow keys taken away.
        await page.EvaluateAsync(
            """
            () => {
              const host = document.querySelector('.bit-bink-canvas');
              const input = document.createElement('input');
              input.id = 'child-field';
              input.className = 'bit-bink-content';
              input.style.position = 'absolute';
              input.style.left = '10px';
              input.style.top = '10px';
              host.appendChild(input);
            }
            """);

        // Focused rather than clicked: the input layer sits above the child content, and this test is about
        // where the keystrokes go, not about hit testing.
        var field = page.Locator("#child-field");
        await field.FocusAsync();
        await page.Keyboard.TypeAsync("caption text");
        await page.Keyboard.PressAsync("Control+a");
        await page.Keyboard.PressAsync("Backspace");
        await page.Keyboard.TypeAsync("second");
        await page.WaitForTimeoutAsync(200);

        Assert.AreEqual("second", await field.InputValueAsync());
    }

    [TestMethod]
    public async Task Touch_input_while_a_pen_is_in_contact_is_ignored()
    {
        var page = await OpenAsync("/handwriting");

        // The pen draws a short horizontal stroke while a palm rests far below it. Only the pen's ink may appear.
        await SendTouchAsync(page,
            """
            send('pointerdown', 4, 'pen', 100, 120, true);
            for (let i = 1; i <= 10; i++) { send('pointermove', 4, 'pen', 100 + i * 12, 120, true); await wait(8); }
            send('pointerdown', 5, 'touch', 500, 500, true);
            for (let i = 1; i <= 10; i++) { send('pointermove', 5, 'touch', 500, 500 + i * 12, true); await wait(8); }
            send('pointerup', 5, 'touch', 500, 620, false);
            for (let i = 11; i <= 20; i++) { send('pointermove', 4, 'pen', 100 + i * 12, 120, true); await wait(8); }
            send('pointerup', 4, 'pen', 340, 120, false);
            """);
        await page.WaitForTimeoutAsync(500);

        int[] box = await page.EvaluateAsync<int[]>(
            """
            () => {
              const canvas = document.querySelector('canvas[data-bink-layer="document"]');
              const ctx = canvas.getContext('2d', { willReadFrequently: true });
              const w = canvas.width, h = canvas.height;
              const data = ctx.getImageData(0, 0, w, h).data;
              const br = data[0], bg = data[1], bb = data[2], ba = data[3];
              let minY = h, maxY = -1, count = 0;
              for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
                const i = (y * w + x) * 4;
                if (Math.abs(data[i]-br)+Math.abs(data[i+1]-bg)+Math.abs(data[i+2]-bb)+Math.abs(data[i+3]-ba) > 24) {
                  if (y < minY) minY = y;
                  if (y > maxY) maxY = y;
                  count++;
                }
              }
              return [minY, maxY, count, Math.round(window.devicePixelRatio || 1)];
            }
            """);

        Assert.IsTrue(box[2] > 100, "the pen stroke was not drawn");
        // The palm's path ran from y=500 downwards; nothing may have been drawn anywhere near it.
        Assert.IsTrue(box[1] < 400 * box[3], $"ink reached y={box[1]}, so the resting palm drew as well");
    }

    [TestMethod]
    public async Task A_second_finger_pans_the_view_even_while_a_drawing_tool_is_active()
    {
        var page = await OpenAsync("/whiteboard");

        await DrawStrokeAsync(page, (150, 150), (350, 250));
        int[] before = await InkBoxAsync(page);

        // One finger starts what looks like a stroke; a second arrives, and from then on the gesture is a pan.
        await SendTouchAsync(page,
            """
            send('pointerdown', 1, 'touch', 200, 300, true);
            for (let i = 1; i <= 5; i++) { send('pointermove', 1, 'touch', 200 + i * 4, 300, true); await wait(10); }
            send('pointerdown', 2, 'touch', 400, 300, true);
            await wait(20);
            for (let i = 1; i <= 12; i++) {
              send('pointermove', 1, 'touch', 220 + i * 8, 300 + i * 6, true);
              send('pointermove', 2, 'touch', 400 + i * 8, 300 + i * 6, true);
              await wait(12);
            }
            send('pointerup', 1, 'touch', 316, 372, false);
            send('pointerup', 2, 'touch', 496, 372, false);
            """);
        await page.WaitForTimeoutAsync(500);

        int[] after = await InkBoxAsync(page);
        int inkNow = await CountInkAsync(page);

        // The view moved, so the existing ink is somewhere else on screen, and the first finger left no mark.
        Assert.IsTrue(after[0] != before[0] || after[1] != before[1], "the two-finger gesture did not pan the view");
        Assert.IsTrue(inkNow > 100, "the existing stroke disappeared");
        Assert.IsTrue(after[2] - after[0] < (before[2] - before[0]) * 2 + 60, "a stray stroke was committed by the first finger");
    }
}
