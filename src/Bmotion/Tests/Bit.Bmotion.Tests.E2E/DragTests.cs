namespace Bit.Bmotion.Tests.E2E;

/// <summary>
/// Drag with a real pointer: movement, axes, constraints, elasticity, momentum, snapping, handles, drag controls,
/// WhileDrag and direction lock. Positions are asserted on every runtime; how the element gets there differs (the
/// WebAssembly frame loop follows the pointer frame by frame, Blazor Server follows it a round trip behind).
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

        await DragByAsync("#pixels", -300, 0);
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

        // The track is 400x80 and the element 30x30: it can travel 370 across and 50 down.
        await DragByAsync("#parent", 600, 200);

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
