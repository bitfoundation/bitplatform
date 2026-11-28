using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// What the BitSortable suites share: the pages they drive (/e2e/sortable/...), a 1280 x 1400 viewport every page of
/// them fits (a native drag cannot be aimed below the fold, and the browser's own auto-scroll near an edge would move
/// the lists under a pointer held still), native HTML5 drags with the real mouse, touch and pen drags, and the probes
/// that read what a list shows. Every assertion reads what the page actually shows: the order readouts the pages bind
/// to the lists (what .NET holds), the items in DOM order (what the browser shows), the classes the drag states put on
/// them.
/// </summary>
/// <remarks>
/// <para>
/// A drag is driven the way a hand makes one: press on the item, move a few pixels (Chromium starts a native drag, and
/// fires dragstart, only once the pointer has moved more than 4 pixels with the button held), move to the target in
/// steps, release. A hand's browser repeats dragover every 50 ms while the pointer rests; under Playwright it fires only
/// when the pointer moves, and the script skips a position measured on an item still sliding into place. So after
/// every move the pointer is nudged a pixel off and back, each time once the page has gone quiet, until a nudge changes
/// nothing: every list has then acted on where the pointer is (<see cref="SettleAsync"/>).
/// </para>
/// <para>
/// The items of a page are all the same size, so wherever the pointer comes to rest the dragged item (or its
/// placeholder in another list) ends up in the slot under it: a drag that ends on B3 lands where B3 was, and pushes B3
/// down. The nested tree is the exception; its tests move the pointer to the target in one jump.
/// </para>
/// <para>
/// The classes of these suites are not run beside each other (<c>[DoNotParallelize]</c>): every drag races the .NET
/// handlers of its dragover events against the browser, and a page booting in the next context over is exactly the
/// load that turns those races into flakes.
/// </para>
/// </remarks>
public abstract class BitSortableE2ETestBase : E2ETestBase
{
    /// <summary>The marker every list renders on its root.</summary>
    protected const string ListSelector = "[data-bit-srt-list]";

    /// <summary>The preview that follows a finger, a pen or a ForceFallback mouse.</summary>
    protected const string Preview = ".bit-srt-drg";

    private ICDPSession? _cdp;

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 1280, Height = 1400 },
        DeviceScaleFactor = 1,
    };

    // ---- locating items ------------------------------------------------------------------------------

    /// <summary>
    /// The item of the list <paramref name="listId"/> labelled <paramref name="label"/>: one of the list's own items
    /// (never the drop placeholder) whose text is exactly the label, leading symbols (a handle's glyph) aside. Labels
    /// repeat across the lists of a page, so an item is always looked up in its list.
    /// </summary>
    protected ILocator Item(string listId, string label) =>
        Page.Locator($"#{listId} > [data-bit-srt-item]:not([data-bit-srt-placeholder])")
            .Filter(new() { HasTextRegex = new Regex(@"^\W*" + Regex.Escape(label) + @"\s*$") });

    /// <summary>The drop placeholder a list shows while an item from another list is previewed in it.</summary>
    protected ILocator Placeholder(string listId) => Page.Locator($"#{listId} > [data-bit-srt-placeholder]");

    /// <summary>
    /// The labels of a list's items in DOM order. With <paramref name="withPlaceholder"/> the drop placeholder is listed
    /// too, as <c>[placeholder: label]</c>, where it sits. A nested item is labelled by its own name, not by the text of
    /// the list it holds; a row with a handle by its text without the handle's glyph.
    /// </summary>
    protected Task<string[]> LabelsAsync(string listId, bool withPlaceholder = false) => Page.EvaluateAsync<string[]>(
        """
        ([listId, withPlaceholder]) => {
          const list = document.getElementById(listId);
          if (!list) return [];
          const label = el => {
            const name = el.querySelector(':scope > .nested-item__name');
            if (name) return name.textContent.trim();
            // Text and elements only: Blazor marks the bounds of components and regions with comment nodes.
            return [...el.childNodes]
              .filter(n => n.nodeType === Node.TEXT_NODE || n.nodeType === Node.ELEMENT_NODE && !n.matches('.handle'))
              .map(n => n.textContent).join('').trim();
          };
          return [...list.children]
            .filter(c => c.hasAttribute('data-bit-srt-item'))
            .filter(c => withPlaceholder || !c.hasAttribute('data-bit-srt-placeholder'))
            .map(c => c.hasAttribute('data-bit-srt-placeholder') ? `[placeholder: ${label(c)}]` : label(c));
        }
        """, new object[] { listId, withPlaceholder });

    /// <summary>The on-screen box of the element <paramref name="locator"/> picks.</summary>
    protected static async Task<Box> BoxAsync(ILocator locator)
    {
        var b = await locator.BoundingBoxAsync();
        Assert.IsNotNull(b, $"{locator} is not on screen");
        return new Box(b.X, b.Y, b.Width, b.Height);
    }

    /// <summary>The on-screen box of a list's root.</summary>
    protected Task<Box> ListBoxAsync(string listId) => BoxAsync(Page.Locator($"#{listId}"));

    /// <summary>The centre of the item of <paramref name="listId"/> labelled <paramref name="label"/>.</summary>
    protected async Task<Point> CentreOfAsync(string listId, string label) => (await BoxAsync(Item(listId, label))).Centre;

    // ---- waiting for the page to settle ----------------------------------------------------------------

    /// <summary>
    /// Installs (once per document) the mutation counter <see cref="SettleAsync"/> and <see cref="WaitForQuietAsync"/>
    /// read, and returns the count so far. It watches the whole document: a drag changes the lists, the readouts under
    /// them and the logs, and any of those is a sign that a handler acted.
    /// </summary>
    private Task<int> MutationCountAsync() => Page.EvaluateAsync<int>(
        """
        () => {
          if (!window.__srtE2E) {
            const state = { count: 0, last: performance.now() };
            new MutationObserver(records => { state.count += records.length; state.last = performance.now(); })
              .observe(document.documentElement, { subtree: true, childList: true, attributes: true, characterData: true });
            window.__srtE2E = state;
          }
          return window.__srtE2E.count;
        }
        """);

    /// <summary>
    /// Waits until nothing in the page has changed for <paramref name="quietMs"/>: a drag's effect has been rendered, and
    /// the slides it started have finished and cleaned up after themselves. Gives up quietly after a few seconds, for a
    /// page that never stops changing.
    /// </summary>
    protected async Task WaitForQuietAsync(int quietMs = 300)
    {
        await MutationCountAsync();
        await Page.EvaluateAsync(
            """
            (quietMs) => new Promise(resolve => {
              const state = window.__srtE2E;
              const started = performance.now();
              const check = () => {
                const now = performance.now();
                if (now - state.last >= quietMs && now - started >= quietMs || now - started > 5000) { resolve(); return; }
                setTimeout(check, 25);
              };
              check();
            })
            """, quietMs);
    }

    // ---- the mouse -----------------------------------------------------------------------------------

    /// <summary>Drags from <paramref name="from"/> to <paramref name="to"/> with the real mouse and drops there.</summary>
    protected async Task DragAsync(Point from, Point to, int steps = 12)
    {
        await PressAsync(from);
        await MoveAsync(to, steps);
        await ReleaseAsync();
    }

    /// <summary>
    /// Presses at <paramref name="at"/> and moves a few pixels away with the button held: a native drag starts only once
    /// the pointer has moved, and only on an element that is <c>draggable</c> at that moment.
    /// </summary>
    protected async Task PressAsync(Point at)
    {
        AssertInViewport(at);
        await Page.Mouse.MoveAsync((float)at.X, (float)at.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)at.X + 6, (float)at.Y + 6, new() { Steps = 3 });
        await SettleAsync(at.Offset(6, 6));
    }

    /// <summary>
    /// Moves the held pointer to <paramref name="to"/> in steps, as a hand would, and holds it there until the lists
    /// have acted on the final position.
    /// </summary>
    protected async Task MoveAsync(Point to, int steps = 12)
    {
        AssertInViewport(to);
        await Page.Mouse.MoveAsync((float)to.X, (float)to.Y, new() { Steps = steps });
        await SettleAsync(to);
    }

    /// <summary>Releases the button where the pointer is (a drop, when a drag is running) and waits for its effect.</summary>
    protected async Task ReleaseAsync()
    {
        await Page.Mouse.UpAsync();
        await WaitForQuietAsync();
    }

    /// <summary>
    /// Holds the pointer at <paramref name="at"/> until the lists stop changing: each nudge (a pixel off and back) fires
    /// a fresh dragover at the same point after the page went quiet; once a nudge changes nothing, every list has seen
    /// where the pointer is. Without a drag running, the first nudge changes nothing and this returns at once.
    /// </summary>
    protected async Task SettleAsync(Point at)
    {
        await WaitForQuietAsync(200);
        for (int i = 0; i < 15; i++)
        {
            var before = await MutationCountAsync();
            await Page.Mouse.MoveAsync((float)at.X + 1, (float)at.Y);
            await Page.Mouse.MoveAsync((float)at.X, (float)at.Y);
            await WaitForQuietAsync(200);
            if (await MutationCountAsync() == before) return;
        }
    }

    // A pointer position outside the viewport is clamped by the browser to its edge, where the drag would land somewhere
    // else entirely: fail with the reason instead (a page that grew past the viewport, usually).
    private void AssertInViewport(Point point)
    {
        var size = Page.ViewportSize;
        if (size is null) return;
        Assert.IsTrue(point.X >= 0 && point.Y >= 0 && point.X < size.Width && point.Y < size.Height,
            $"the pointer target {point} is outside the {size.Width} x {size.Height} viewport");
    }

    // ---- the keyboard --------------------------------------------------------------------------------

    /// <summary>Presses a key and waits until the page has acted on it.</summary>
    protected async Task PressKeyAsync(string key)
    {
        await Page.Keyboard.PressAsync(key);
        await WaitForQuietAsync();
    }

    /// <summary>The text of the focused element, which is an item during a keyboard drag.</summary>
    protected Task<string> FocusedAsync() => Page.EvaluateAsync<string>("() => document.activeElement?.textContent?.trim() ?? ''");

    /// <summary>What the live region last read out.</summary>
    protected Task<string> AnnouncedAsync() =>
        Page.EvaluateAsync<string>("() => document.querySelector('[data-bit-srt-live]')?.textContent?.trim() ?? ''");

    // ---- a finger and a pen ----------------------------------------------------------------------------

    /// <summary>
    /// Sends one step of a finger (<paramref name="pointerType"/> "touch") or a pen ("pen") drag: "start", "move" or
    /// "end". Chromium gets real input through the DevTools Protocol - touch events for a finger (which raise the pointer
    /// events, and the scrolling a touch screen does), mouse events of the pen pointer type for a pen. The other engines
    /// speak no such protocol, so they get the pointer events a finger or a pen raises, dispatched on the element under
    /// the point: what the script listens to, without the browser's own touch handling around it.
    /// </summary>
    protected async Task PointerStepAsync(string pointerType, string step, Point at)
    {
        if (IsChromium)
        {
            _cdp ??= await Page.Context.NewCDPSessionAsync(Page);
            if (pointerType == "touch")
            {
                await _cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
                {
                    ["type"] = step switch { "start" => "touchStart", "move" => "touchMove", _ => "touchEnd" },
                    ["touchPoints"] = step == "end"
                        ? Array.Empty<object>()
                        : new object[] { new Dictionary<string, object> { ["x"] = at.X, ["y"] = at.Y, ["id"] = 1 } },
                });
            }
            else
            {
                await _cdp.SendAsync("Input.dispatchMouseEvent", new Dictionary<string, object>
                {
                    ["type"] = step switch { "start" => "mousePressed", "move" => "mouseMoved", _ => "mouseReleased" },
                    ["x"] = at.X,
                    ["y"] = at.Y,
                    ["button"] = "left",
                    ["buttons"] = step == "end" ? 0 : 1,
                    ["clickCount"] = 1,
                    ["pointerType"] = pointerType,
                });
            }
            return;
        }

        await Page.EvaluateAsync(
            """
            ([pointerType, step, x, y]) => {
              const type = step === 'start' ? 'pointerdown' : step === 'move' ? 'pointermove' : 'pointerup';
              const target = document.elementFromPoint(x, y) ?? document.body;
              const pressed = step !== 'end';
              target.dispatchEvent(new PointerEvent(type, {
                bubbles: true, cancelable: true, composed: true, pointerId: 31, pointerType, isPrimary: true,
                clientX: x, clientY: y, button: step === 'move' ? -1 : 0, buttons: pressed ? 1 : 0, pressure: pressed ? 0.5 : 0,
              }));
            }
            """, new object[] { pointerType, step, at.X, at.Y });
    }

    /// <summary>Moves a finger (or a pen) from one point to another in small steps, as a hand would, then lets the page settle.</summary>
    protected async Task GlideAsync(string pointerType, Point from, Point to, int steps = 12)
    {
        for (int i = 1; i <= steps; i++)
        {
            await PointerStepAsync(pointerType, "move", new Point(from.X + (to.X - from.X) * i / steps, from.Y + (to.Y - from.Y) * i / steps));
            await Task.Delay(16);
        }
        await WaitForQuietAsync();
    }

    // ---- the script ------------------------------------------------------------------------------------

    /// <summary>
    /// What the script has sent .NET since the page loaded or the last reset (<c>BitBlazorUI.Sortable.diagnostics</c>):
    /// drags started, positions sent and how many changed a list, and the round trips of a position.
    /// </summary>
    protected Task<JsonElement> DiagnosticsAsync(bool reset = false) => Page.EvaluateAsync<JsonElement>(
        """
        reset => {
          const now = BitBlazorUI.Sortable.diagnostics();
          if (reset) BitBlazorUI.Sortable.resetDiagnostics();
          return now;
        }
        """, reset);

    // ---- asserting -----------------------------------------------------------------------------------

    /// <summary>
    /// Asserts the order of a list twice over: the readout the page binds to the list (what .NET holds), and the list's
    /// items in DOM order (what the browser shows). Both poll, so a render still on its way is waited for.
    /// </summary>
    protected async Task AssertOrderAsync(string listId, string readoutId, params string[] expected)
    {
        await Expect(Page.Locator($"#{readoutId}")).ToHaveTextAsync(string.Join(", ", expected));
        await AssertLabelsAsync(listId, expected);
    }

    /// <summary>Asserts what a list shows right now, mid-drag included (see <see cref="LabelsAsync"/>), polling.</summary>
    protected async Task AssertLabelsAsync(string listId, string[] expected, bool withPlaceholder = false, string? because = null)
    {
        string[] shown = [];
        await EventuallyAsync(async () => (shown = await LabelsAsync(listId, withPlaceholder)).SequenceEqual(expected),
            () => $"#{listId} shows [{string.Join(", ", expected)}] (it shows [{string.Join(", ", shown)}]){(because is null ? "" : ": " + because)}");
    }

    /// <summary>Polls <paramref name="condition"/> until it holds, failing with <paramref name="what"/> when it never does.</summary>
    protected static Task EventuallyAsync(Func<Task<bool>> condition, string what, int timeoutMs = 10_000)
        => EventuallyAsync(condition, () => what, timeoutMs);

    /// <summary>Polls <paramref name="condition"/> until it holds, failing with what <paramref name="what"/> says then when it never does.</summary>
    protected static async Task EventuallyAsync(Func<Task<bool>> condition, Func<string> what, int timeoutMs = 10_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            if (await condition()) return;
            if (DateTime.UtcNow > deadline) Assert.Fail($"timed out waiting until {what()}");
            await Task.Delay(50);
        }
    }

    protected readonly record struct Point(double X, double Y)
    {
        public Point Offset(double dx, double dy) => new(X + dx, Y + dy);

        public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X:0.#}, {Y:0.#})");
    }

    protected readonly record struct Box(double X, double Y, double Width, double Height)
    {
        public Point Centre => new(X + Width / 2, Y + Height / 2);

        public double Right => X + Width;

        public double Bottom => Y + Height;

        /// <summary>A point inside the box, as fractions of its width and height from its top left corner.</summary>
        public Point At(double fx, double fy) => new(X + Width * fx, Y + Height * fy);

        public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"[{X:0.#}, {Y:0.#}, {Width:0.#} x {Height:0.#}]");
    }
}
