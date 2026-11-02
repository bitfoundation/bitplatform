namespace Bit.Bmotion.Tests.E2E;

/// <summary>
/// Drag with a real pointer: movement, axes, constraints, elasticity, momentum, snapping, handles, drag controls,
/// WhileDrag and direction lock. Positions are asserted on every runtime; how the element gets there differs (the
/// WebAssembly frame loop follows the pointer frame by frame; on Blazor Server the bridge shows the drag itself while
/// .NET catches up a round trip later).
/// </summary>
[TestClass]
public sealed class DragTests : BmotionPageTest
{
    protected override string Route => "drag";

    [TestMethod]
    public async Task FreeDrag_FollowsThePointer_AndReportsStartMovesAndEnd()
    {
        await ExpectEventAsync("armed:free");

        await DragByAsync("#free", 80, 30);

        await ExpectEventAsync("free:start");
        await ExpectEventAsync("free:end");
        // Once per pointer move on WebAssembly; on Blazor Server the moves are coalesced to one per frame.
        Assert.IsTrue(await Event("free:move").CountAsync() >= 1, "OnDrag should fire while the pointer moves.");
        await WaitForStateAsync("#free", "s => Math.abs(s.x - 80) < 1 && Math.abs(s.y - 30) < 1");
    }

    [TestMethod]
    public async Task WhileDragging_TheElementIsUnderThePointer_WithoutWaitingForDotNet()
    {
        await ExpectEventAsync("armed:free");
        // How far the element is from where the pointer has dragged it: in the pointermove itself (after Bmotion's
        // own listener) and on the next frame - from where the pointer is by then, the last move that frame.
        await Page.EvaluateAsync("""
            () => {
                const el = document.getElementById('free');
                window.__inEvent = []; window.__nextFrame = [];
                let start = null, last = null, queued = false;
                const offBy = e => {
                    const r = el.getBoundingClientRect();
                    return Math.hypot((r.left - start.left) - (e.clientX - start.x), (r.top - start.top) - (e.clientY - start.y));
                };
                el.addEventListener('pointerdown', e => {
                    const r = el.getBoundingClientRect();
                    start = { x: e.clientX, y: e.clientY, left: r.left, top: r.top };
                });
                el.addEventListener('pointermove', e => {
                    if (!start || !e.buttons) return;
                    last = e;
                    window.__inEvent.push(offBy(e));
                    if (queued) return;
                    queued = true;
                    requestAnimationFrame(() => { queued = false; window.__nextFrame.push(offBy(last)); });
                });
            }
            """);

        await DragByAsync("#free", 120, 40, steps: 10);
        await ExpectEventAsync("free:end");

        var inEvent = await Page.EvaluateAsync<double[]>("() => window.__inEvent");
        var nextFrame = await Page.EvaluateAsync<double[]>("() => window.__nextFrame");
        Assert.IsTrue(nextFrame.Length >= 5 && nextFrame.All(d => d < 1), $"Behind the pointer on the frame after a move: {string.Join(", ", nextFrame.Select(d => d.ToString("0.#")))}");
        // Without a frame loop (Blazor Server) .NET hears of a move a round trip later; the element mustn't wait for it.
        if (!FrameLoop)
            Assert.IsTrue(inEvent.All(d => d < 1), $"Waited for the server: {string.Join(", ", inEvent.Select(d => d.ToString("0.#")))}");

        // Once .NET has caught up, nothing of the local offset is left.
        await WaitForStateAsync("#free", "s => Math.abs(s.x - 120) < 1 && Math.abs(s.y - 40) < 1");
        await Page.WaitForFunctionAsync("() => document.getElementById('free').style.translate === ''");
    }

    [TestMethod]
    public async Task DragAgain_ContinuesFromWhereTheLastDragLeftTheElement()
    {
        await ExpectEventAsync("armed:free");

        await DragByAsync("#free", 50, 0);
        await ExpectEventAsync("free:end");
        await WaitForStateAsync("#free", "s => Math.abs(s.x - 50) < 1");

        await DragByAsync("#free", 40, 10);
        await ExpectEventAsync("free:end", 2);
        await WaitForStateAsync("#free", "s => Math.abs(s.x - 90) < 1 && Math.abs(s.y - 10) < 1");
    }

    [TestMethod]
    public async Task ADrag_SelectsNoText_SoTheNextOneIsNotTakenOverByTheBrowser()
    {
        await ExpectEventAsync("armed:texty");
        await Page.Locator("#texty").ScrollIntoViewIfNeededAsync();

        // Across the text on its left: WebKit would select it from the press on the element.
        await DragByAsync("#texty", -80, 0);
        await ExpectEventAsync("texty:end");
        Assert.AreEqual("", await Page.EvaluateAsync<string>("() => getSelection().toString()"), "The drag selected the text it passed over.");

        // A press inside a selection starts a native drag-and-drop of it there, after which no more pointer events
        // arrive: this drag would never move the element nor end.
        var before = await ProbeAsync("#texty");
        await DragByAsync("#texty", 60, 0);
        await ExpectEventAsync("texty:end", 2);
        // It followed the pointer: the browser didn't take the gesture over.
        await WaitForStateAsync("#texty", $"s => s.x > {(before.X + 40).ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }

    [TestMethod]
    public async Task AxisX_IgnoresVerticalMovement()
    {
        await ExpectEventAsync("armed:axis-x");

        await DragByAsync("#axis-x", 80, 40);

        await ExpectEventAsync("axis-x:end");
        await WaitForStateAsync("#axis-x", "s => Math.abs(s.x - 80) < 1 && Math.abs(s.y) < 0.5");
    }

    [TestMethod]
    public async Task PixelConstraints_WithoutElasticity_StopAtTheBound()
    {
        await ExpectEventAsync("armed:pixels");

        await DragByAsync("#pixels", 200, 0);
        await ExpectEventAsync("pixels:end");
        await WaitForStateAsync("#pixels", "s => Math.abs(s.x - 50) < 1");

        // Far enough past the -50 bound, while staying inside the viewport: Firefox under automation reports a move
        // outside it at a wrong position and never delivers the release.
        await DragByAsync("#pixels", -180, 0);
        await ExpectEventAsync("pixels:end", 2);
        await WaitForStateAsync("#pixels", "s => Math.abs(s.x + 50) < 1");
    }

    [TestMethod]
    public async Task ElasticConstraints_LetThePointerPullPastTheBound_ThenSpringBack()
    {
        await ExpectEventAsync("armed:elastic");

        // 150px against a 50px bound at elasticity 0.5: 50 + 100 * 0.5 while held.
        await DragByAsync("#elastic", 150, 0, release: false);
        await WaitForStateAsync("#elastic", "s => Math.abs(s.x - 100) < 2");

        await Page.Mouse.UpAsync();
        await ExpectEventAsync("elastic:end");
        await WaitForStateAsync("#elastic", "s => Math.abs(s.x - 50) < 1");
    }

    [TestMethod]
    public async Task ParentConstraints_KeepTheElementInsideItsContainer()
    {
        await ExpectEventAsync("armed:parent");

        // The track is 400x80 and the element 30x30: it can travel 370 across and 50 down. The pointer goes past both,
        // but stays inside the viewport (see PixelConstraints_WithoutElasticity_StopAtTheBound).
        await DragByAsync("#parent", 600, 150);

        await ExpectEventAsync("parent:end");
        await WaitForStateAsync("#parent", "s => Math.abs(s.x - 370) < 1 && Math.abs(s.y - 50) < 1");
    }

    [TestMethod]
    public async Task SnapToOrigin_ReturnsTheElementOnRelease()
    {
        await ExpectEventAsync("armed:snap");

        await DragByAsync("#snap", 120, 30, release: false);
        await WaitForStateAsync("#snap", "s => Math.abs(s.x - 120) < 1");

        await Page.Mouse.UpAsync();
        await ExpectEventAsync("snap:end");
        await WaitForStateAsync("#snap", "s => Math.abs(s.x) < 0.5 && Math.abs(s.y) < 0.5");
    }

    [TestMethod]
    public async Task DragHandle_OnlyTheHandleStartsTheDrag()
    {
        await ExpectEventAsync("armed:handle");

        // Pressing the body (right of the 16px grip) does nothing.
        var box = await BoxAsync("#handle");
        await Page.Mouse.MoveAsync(box.X + 60, box.Y + 20);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + 160, box.Y + 20, new() { Steps = 8 });
        await Page.Mouse.UpAsync();
        await ExpectEventAsync("handle:start", 0);
        Assert.AreEqual(0, (await ProbeAsync("#handle")).X, 0.5);

        await DragByAsync("#handle .grip", 70, 0);
        await ExpectEventAsync("handle:start");
        await WaitForStateAsync("#handle", "s => Math.abs(s.x - 70) < 1");
    }

    [TestMethod]
    public async Task DragControls_StartTheDragFromAnotherElement_AndTheElementItselfDoesNotListen()
    {
        await ExpectEventAsync("armed:controls");

        // DragListener=false: pressing the element itself starts nothing - it's the track's pointerdown,
        // reaching the controls, that starts it.
        var track = await BoxAsync("#controls-track");
        await Page.Mouse.MoveAsync(track.X + 20, track.Y + 40);
        await Page.Mouse.DownAsync();
        await ExpectEventAsync("controls:start");
        await Page.Mouse.MoveAsync(track.X + 80, track.Y + 40, new() { Steps = 8 });
        await Page.Mouse.UpAsync();

        await ExpectEventAsync("controls:end");
        await WaitForStateAsync("#controls", "s => Math.abs(s.x - 60) < 1.5");
    }

    [TestMethod]
    public async Task WhileDrag_AppliesTheOverlayOnlyWhileDragging()
    {
        await ExpectEventAsync("armed:whiledrag");

        await DragByAsync("#whiledrag", 60, 0, release: false);
        await WaitForStateAsync("#whiledrag", "s => Math.abs(s.scaleX - 1.5) < 0.01");

        await Page.Mouse.UpAsync();
        await ExpectEventAsync("whiledrag:end");
        await WaitForStateAsync("#whiledrag", "s => Math.abs(s.scaleX - 1) < 0.01 && Math.abs(s.x - 60) < 1");
    }

    [TestMethod]
    public async Task DirectionLock_LocksToTheFirstMovementsAxis_AndReportsIt()
    {
        await ExpectEventAsync("armed:lock");

        // Mostly vertical first, then sideways: the lock keeps it vertical.
        var (x, y) = await CentreAsync("#lock");
        await Page.Mouse.MoveAsync((float)x, (float)y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)x + 1, (float)y + 10, new() { Steps = 3 });
        await Page.Mouse.MoveAsync((float)x + 80, (float)y + 30, new() { Steps = 8 });
        await Page.Mouse.UpAsync();

        await ExpectEventAsync("lock:Y");
        await ExpectEventAsync("lock:end");
        await WaitForStateAsync("#lock", "s => Math.abs(s.x) < 0.5 && Math.abs(s.y - 30) < 1");
    }

    [TestMethod]
    public async Task Momentum_ThrownAtABound_BouncesOffIt_AndSettlesOnIt()
    {
        await ExpectEventAsync("armed:bounce");

        // A quick flick towards the bound, every frame after the release sampled in the page.
        await DragByAsync("#bounce", 100, 0, steps: 3, release: false);
        var sampling = SampleOnTheTimelineAsync("#bounce", 1500);
        await Page.Mouse.UpAsync();
        var samples = await sampling;

        // Braking from the release to stop exactly on the bound (as it used to) never passes it; a throw that keeps
        // its speed reaches the bound moving and the bounce spring carries it a little past before it settles.
        var furthest = samples.Max(s => s.X);
        var trace = string.Join(" ", samples.Where((_, i) => i % 3 == 0).Select(s => $"{s.Time:0}:{s.X:0.#}"));
        Assert.IsTrue(furthest > 151, $"It braked to a stop at the bound instead of bouncing off it (furthest {furthest:0.##}): {trace}");
        Assert.IsTrue(furthest < 150 + 60, $"It flew {furthest - 150:0.##}px past the bound: {trace}");
        var rest = await WaitForRestAsync("#bounce");
        Assert.AreEqual(150, rest.X, 0.5, "and it settles on the bound");
    }

    [TestMethod]
    public async Task Momentum_HeldStillBeforeTheRelease_ThrowsNothing()
    {
        await ExpectEventAsync("armed:bounce");

        // A quick move, then the pointer stands still a while before it lets go: nothing is left to throw.
        await DragByAsync("#bounce", 60, 0, steps: 3, release: false);
        await Page.WaitForTimeoutAsync(250);
        await Page.Mouse.UpAsync();

        await ExpectEventAsync("bounce:end");
        var rest = await WaitForRestAsync("#bounce");
        Assert.AreEqual(60, rest.X, 1, "it stays where it was let go");
    }

    [TestMethod]
    public async Task Momentum_WithASnapTarget_ComesToRestOnTheGrid()
    {
        await ExpectEventAsync("armed:momentum");

        // A quick flick: few, wide steps.
        await DragByAsync("#momentum", 130, 0, steps: 3);

        await ExpectEventAsync("momentum:end");
        // Wherever the throw ends, it ends on a multiple of 100 inside [0, 300] - and a flick goes somewhere.
        var rest = await WaitForRestAsync("#momentum");
        Assert.IsTrue(new[] { 100.0, 200, 300 }.Any(v => Math.Abs(rest.X - v) < 0.5), $"Came to rest off the grid: {rest}");
    }
}
