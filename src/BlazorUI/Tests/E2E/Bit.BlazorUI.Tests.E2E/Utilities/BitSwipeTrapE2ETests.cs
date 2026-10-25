using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// BitSwipeTrap in a real browser (/e2e/swipe-trap) with the mouse and the keyboard: OnStart / OnMove / OnEnd /
/// OnTrigger with their arguments (start point, distances, velocity, pointer type, duration, IsCanceled), every
/// direction, the swiping class, the click of a swipe swallowed while a tap still clicks, Escape and leaving the
/// trap cancelling a swipe, Trigger as a fraction and in pixels (and changed at runtime), TriggerVelocity flicks,
/// every OrientationLock (classes, touch-action, the zeroed axis, Auto, changed at runtime), Threshold, Throttle with
/// its trailing move, TouchOnly, SkipSelector, Disabled, KeyboardTrigger (tab stop, role, aria-keyshortcuts, the four
/// arrows, a lock limiting them, modifiers, a held key, a descendant's keys, TabIndex), Escape inside an open
/// Overlay, the BitParams cascade, Id / Style / Class / Visibility / attributes / Dir, the --bit-SwipeTrap-*
/// variables, the prerendered markup and a trap removed mid-gesture. Touch is in BitSwipeTrapTouchE2ETests.
/// </summary>
[TestClass]
public class BitSwipeTrapE2ETests : E2ETestBase
{
    private const string Route = "/e2e/swipe-trap";

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheTrapAndItsKeyboardAttributes()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "<div id=\"stp-basic\" class=\"bit-stp\">");
        StringAssert.Contains(html, "aria-keyshortcuts=\"ArrowLeft ArrowRight ArrowUp ArrowDown\"");
        StringAssert.Contains(html, "id=\"status\">Loading");

        await OpenAsync(Route);
        await Expect(Page.Locator("#stp-basic")).ToHaveCSSAsync("width", "300px");
    }

    [TestMethod]
    public async Task MouseSwipe_Right_ReportsEveryEventWithItsArguments()
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-basic");
        await DragAsync(x, y, 100, 0, steps: 10);

        await Expect(Page.Locator("#basic-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-starts")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-start")).ToHaveTextAsync($"{x},{y}|0,0|00|mouse|False|nodur");
        await Expect(Page.Locator("#basic-moves")).ToHaveTextAsync("10");
        await Expect(Page.Locator("#basic-move")).ToHaveTextAsync($"{x},{y}|100,0|+0|mouse|False|dur");
        await Expect(Page.Locator("#basic-end")).ToHaveTextAsync($"{x},{y}|100,0|+0|mouse|False|dur");
        await Expect(Page.Locator("#basic-triggers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-trigger")).ToHaveTextAsync("Right|100,0|+0|mouse|dur");
    }

    [TestMethod]
    [DataRow(100, 0, "Right")]
    [DataRow(-100, 0, "Left")]
    [DataRow(0, 70, "Bottom")]
    [DataRow(0, -70, "Top")]
    [DataRow(90, 60, "Right")]
    [DataRow(-30, -80, "Top")]
    public async Task MouseSwipe_EachDirection_TriggersInTheDominantDirection(int dx, int dy, string direction)
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-basic");
        await DragAsync(x, y, dx, dy);

        await Expect(Page.Locator("#basic-triggers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-trigger")).ToHaveTextAsync(new Regex($"^{direction}\\|{dx},{dy}\\|"));
        await Expect(Page.Locator("#basic-ends")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task MouseSwipe_ShortOfTheTrigger_EndsWithoutTriggering()
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-basic");
        // 70px of a 300px box is short of the default quarter.
        await DragAsync(x, y, 70, 0);

        await Expect(Page.Locator("#basic-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-end")).ToHaveTextAsync(new Regex(@"\|70,0\|.*\|False\|"));
        await Expect(Page.Locator("#basic-triggers")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Swiping_TheRootCarriesTheSwipingClassOnlyWhileTrapped()
    {
        await OpenAsync(Route);

        var trap = Page.Locator("#stp-basic");
        var (x, y) = await StartPointAsync("#stp-basic");

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Expect(trap).Not.ToHaveClassAsync(HasClass("bit-stp-swp"));

        await Page.Mouse.MoveAsync(x + 50, y, new() { Steps = 5 });
        await Expect(trap).ToHaveClassAsync(HasClass("bit-stp-swp"));
        await Expect(trap).ToHaveCSSAsync("user-select", "none");
        await Expect(trap).ToHaveCSSAsync("cursor", "grabbing");

        await Page.Mouse.UpAsync();
        await Expect(trap).Not.ToHaveClassAsync(HasClass("bit-stp-swp"));
        await Expect(Page.Locator("#basic-ends")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Click_ATapOnAChildStillClicks_TheReleaseOfASwipeOverItDoesNot()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#basic-child");
        await Expect(Page.Locator("#basic-child-clicks")).ToHaveTextAsync("1");

        var child = (await Page.Locator("#basic-child").BoundingBoxAsync())!;
        var x = MathF.Round(child.X + 5);
        var y = MathF.Round(child.Y + child.Height / 2);

        // A swipe that starts on the child and is released past the trigger.
        await DragAsync(x, y, 120, 0);
        await Expect(Page.Locator("#basic-triggers")).ToHaveTextAsync("1");

        // A tap that wobbles by a pixel or two is a tap.
        await DragAsync(x, y, 2, 1, steps: 2);
        await Expect(Page.Locator("#basic-ends")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#basic-child-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Escape_DuringATrappedSwipe_CancelsItAndTriggersNothing()
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-basic");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 100, y, new() { Steps = 5 });
        await Expect(Page.Locator("#stp-basic")).ToHaveClassAsync(HasClass("bit-stp-swp"));

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#basic-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-end")).ToHaveTextAsync(new Regex(@"\|100,0\|00\|mouse\|True\|dur$"));
        await Expect(Page.Locator("#stp-basic")).Not.ToHaveClassAsync(HasClass("bit-stp-swp"));

        // The release that follows is no gesture's.
        await Page.Mouse.UpAsync();
        await Page.ClickAsync("#btn-fraction");
        await Expect(Page.Locator("#fraction-value")).ToHaveTextAsync("0.9");
        await Expect(Page.Locator("#basic-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-triggers")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Leave_ThePointerLeavingBeforeTheSwipeIsTrapped_CancelsIt()
    {
        await OpenAsync(Route);

        var trap = (await Page.Locator("#stp-threshold").BoundingBoxAsync())!;
        var (x, y) = await StartPointAsync("#stp-threshold");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 10, y, new() { Steps = 2 });
        // Straight out of the box, below the 30px threshold on the last move the trap saw.
        await Page.Mouse.MoveAsync(trap.X + trap.Width + 40, y);

        await Expect(Page.Locator("#threshold-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#threshold-end")).ToHaveTextAsync(new Regex(@"\|10,0\|.*\|True\|"));
        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("#threshold-triggers")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Trigger_AFraction_WeighsTheBox_AndChangingItAtRuntimeTakesEffect()
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-fraction", fromLeft: 10);

        // Half of 300px is 150.
        await DragAsync(x, y, 120, 0);
        await Expect(Page.Locator("#fraction-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#fraction-triggers")).ToHaveTextAsync("0");

        await DragAsync(x, y, 170, 0);
        await Expect(Page.Locator("#fraction-ends")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#fraction-triggers")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-fraction");
        await Expect(Page.Locator("#fraction-value")).ToHaveTextAsync("0.9");

        await DragAsync(x, y, 170, 0);
        await Expect(Page.Locator("#fraction-ends")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#fraction-triggers")).ToHaveTextAsync("1");

        await DragAsync(x, y, 280, 0);
        await Expect(Page.Locator("#fraction-ends")).ToHaveTextAsync("4");
        await Expect(Page.Locator("#fraction-triggers")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Trigger_InPixels_IsAnAbsoluteDistance()
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-pixels");

        await DragAsync(x, y, 0, 70);
        await Expect(Page.Locator("#pixels-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#pixels-triggers")).ToHaveTextAsync("0");

        await DragAsync(x, y, 0, 90);
        await Expect(Page.Locator("#pixels-ends")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#pixels-triggers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#pixels-trigger")).ToHaveTextAsync(new Regex("^Bottom\\|0,90\\|0\\+"));
    }

    [TestMethod]
    public async Task TriggerVelocity_AFastFlickTriggers_ASlowDragOfTheSameDistanceDoesNot()
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-flick");

        // Slow: 40px over many frames.
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        for (var i = 1; i <= 8; i++)
        {
            await Page.Mouse.MoveAsync(x + i * 5, y);
            await NextFramesAsync();
            await NextFramesAsync();
        }
        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("#flick-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#flick-triggers")).ToHaveTextAsync("0");

        // Fast: the same 40px at once.
        await DragAsync(x, y, 40, 0, steps: 2);
        await Expect(Page.Locator("#flick-ends")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#flick-triggers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#flick-trigger")).ToHaveTextAsync(new Regex("^Right\\|40,0\\|\\+0\\|mouse"));

        // The same flick on a trap without a TriggerVelocity is short of its trigger.
        var (bx, by) = await StartPointAsync("#stp-basic");
        await DragAsync(bx, by, 40, 0, steps: 2);
        await Expect(Page.Locator("#basic-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-triggers")).ToHaveTextAsync("0");
    }

    [TestMethod]
    [DataRow("Unset", "", "pinch-zoom")]
    [DataRow("None", "", "pinch-zoom")]
    [DataRow("Horizontal", "bit-stp-hrz", "pan-y pinch-zoom")]
    [DataRow("Vertical", "bit-stp-vrt", "pan-x pinch-zoom")]
    [DataRow("Auto", "bit-stp-lck", "pinch-zoom")]
    public async Task OrientationLock_EachValue_RendersItsClassAndTouchAction(string name, string cssClass, string touchAction)
    {
        await OpenAsync(Route);

        var trap = Page.Locator($"#stp-lock-{name}");
        await Expect(trap).ToHaveAttributeAsync("class", cssClass.Length > 0 ? $"{cssClass} bit-stp" : "bit-stp");
        await Expect(trap).ToHaveCSSAsync("touch-action", touchAction);
    }

    [TestMethod]
    [DataRow("Unset")]
    [DataRow("None")]
    public async Task OrientationLock_None_ReportsBothAxes(string name)
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync($"#stp-lock-{name}");
        await DragAsync(x, y, 100, 60);

        await Expect(Page.Locator($"#lock-{name}-end")).ToHaveTextAsync(new Regex(@"\|100,60\|"));
        await Expect(Page.Locator($"#lock-{name}-trigger")).ToHaveTextAsync(new Regex("^Right\\|100,60\\|"));
    }

    [TestMethod]
    public async Task OrientationLock_Horizontal_ZeroesTheVerticalAxis_AndTriggersOnlyHorizontally()
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-lock-Horizontal");

        await DragAsync(x, y, 100, 60);
        await Expect(Page.Locator("#lock-Horizontal-end")).ToHaveTextAsync(new Regex(@"\|100,0\|"));
        await Expect(Page.Locator("#lock-Horizontal-move")).ToHaveTextAsync(new Regex(@"\|100,0\|"));
        await Expect(Page.Locator("#lock-Horizontal-trigger")).ToHaveTextAsync(new Regex("^Right\\|100,0\\|"));

        await DragAsync(x, y, 0, 90);
        await Expect(Page.Locator("#lock-Horizontal-ends")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#lock-Horizontal-end")).ToHaveTextAsync(new Regex(@"\|0,0\|"));
        await Expect(Page.Locator("#lock-Horizontal-triggers")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task OrientationLock_Vertical_ZeroesTheHorizontalAxis_AndTriggersOnlyVertically()
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-lock-Vertical");

        await DragAsync(x, y, 100, 60);
        await Expect(Page.Locator("#lock-Vertical-end")).ToHaveTextAsync(new Regex(@"\|0,60\|"));
        await Expect(Page.Locator("#lock-Vertical-trigger")).ToHaveTextAsync(new Regex("^Bottom\\|0,60\\|"));

        await DragAsync(x, y, 120, 0);
        await Expect(Page.Locator("#lock-Vertical-ends")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#lock-Vertical-end")).ToHaveTextAsync(new Regex(@"\|0,0\|"));
        await Expect(Page.Locator("#lock-Vertical-triggers")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task OrientationLock_Auto_LocksToTheFirstAxisTheGestureMovesAlong()
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-lock-Auto");

        // Sideways first: the vertical rest of it reads zero.
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 20, y, new() { Steps = 2 });
        await Page.Mouse.MoveAsync(x + 20, y + 90, new() { Steps = 5 });
        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("#lock-Auto-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#lock-Auto-end")).ToHaveTextAsync(new Regex(@"\|20,0\|"));
        await Expect(Page.Locator("#lock-Auto-triggers")).ToHaveTextAsync("0");

        // Down first: the horizontal rest of it reads zero.
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x, y + 20, new() { Steps = 2 });
        await Page.Mouse.MoveAsync(x + 100, y + 80, new() { Steps = 5 });
        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("#lock-Auto-ends")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#lock-Auto-end")).ToHaveTextAsync(new Regex(@"\|0,80\|"));
        await Expect(Page.Locator("#lock-Auto-trigger")).ToHaveTextAsync(new Regex("^Bottom\\|0,80\\|"));
    }

    [TestMethod]
    public async Task OrientationLock_ChangedAtRuntime_ReSetsTheTrapUp()
    {
        await OpenAsync(Route);

        var trap = Page.Locator("#stp-lock-runtime");
        await Expect(trap).ToHaveClassAsync(HasClass("bit-stp-hrz"));

        var (x, y) = await StartPointAsync("#stp-lock-runtime");
        await DragAsync(x, y, 0, 80);
        await Expect(Page.Locator("#lock-runtime-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#lock-runtime-triggers")).ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-lock-runtime");
        await Expect(Page.Locator("#runtime-lock")).ToHaveTextAsync("Vertical");
        await Expect(trap).ToHaveClassAsync(HasClass("bit-stp-vrt"));
        await Expect(trap).ToHaveCSSAsync("touch-action", "pan-x pinch-zoom");

        (x, y) = await StartPointAsync("#stp-lock-runtime");
        await DragAsync(x, y, 0, 80);
        await Expect(Page.Locator("#lock-runtime-ends")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#lock-runtime-triggers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#lock-runtime-trigger")).ToHaveTextAsync(new Regex("^Bottom"));
    }

    [TestMethod]
    public async Task Threshold_AMoveBelowItIsNotTrapped_OneAboveItIs()
    {
        await OpenAsync(Route);

        var trap = Page.Locator("#stp-threshold");
        var (x, y) = await StartPointAsync("#stp-threshold");

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 20, y, new() { Steps = 4 });
        await Expect(Page.Locator("#threshold-moves")).ToHaveTextAsync("4");
        await Expect(trap).Not.ToHaveClassAsync(HasClass("bit-stp-swp"));

        await Page.Mouse.MoveAsync(x + 50, y, new() { Steps = 3 });
        await Expect(trap).ToHaveClassAsync(HasClass("bit-stp-swp"));
        await Page.Mouse.UpAsync();

        await Expect(Page.Locator("#threshold-end")).ToHaveTextAsync(new Regex(@"\|50,0\|.*\|False\|"));
    }

    [TestMethod]
    public async Task Throttle_LimitsOnMove_AndDeliversTheMoveThePointerRestsOn()
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-throttle");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 60, y, new() { Steps = 20 });

        // The first move goes at once; the latest one of the window arrives once it closes.
        await Expect(Page.Locator("#throttle-move")).ToHaveTextAsync(new Regex(@"\|60,0\|"));
        var moves = int.Parse((await Page.Locator("#throttle-moves").TextContentAsync())!);
        Assert.IsTrue(moves is >= 1 and <= 3, $"20 moves inside one 200ms window reached .NET {moves} times.");

        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("#throttle-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#throttle-end")).ToHaveTextAsync(new Regex(@"\|60,0\|"));
    }

    [TestMethod]
    public async Task TouchOnly_AMouseSwipeIsIgnored()
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-touchonly");
        await DragAsync(x, y, 120, 0);

        // A round trip of the page, after which a start would have been reported.
        await Page.ClickAsync("#btn-fraction");
        await Expect(Page.Locator("#fraction-value")).ToHaveTextAsync("0.9");
        await Expect(Page.Locator("#touchonly-starts")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#touchonly-ends")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#touchonly-triggers")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task SkipSelector_ASwipeStartingOnASkippedDescendantIsIgnored()
    {
        await OpenAsync(Route);

        var skipped = (await Page.Locator("#skip-child").BoundingBoxAsync())!;
        await Page.Locator("#stp-skip").ScrollIntoViewIfNeededAsync();
        skipped = (await Page.Locator("#skip-child").BoundingBoxAsync())!;
        await DragAsync(MathF.Round(skipped.X + 10), MathF.Round(skipped.Y + 10), 120, 0);

        await Page.ClickAsync("#btn-fraction");
        await Expect(Page.Locator("#fraction-value")).ToHaveTextAsync("0.9");
        await Expect(Page.Locator("#skip-starts")).ToHaveTextAsync("0");

        var (x, y) = await StartPointAsync("#stp-skip");
        await DragAsync(x, y, 120, 0);
        await Expect(Page.Locator("#skip-starts")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#skip-triggers")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Disabled_TakesNoGestureAndNoKey_AndEnablingItAtRuntimeBringsThemBack()
    {
        await OpenAsync(Route);

        var trap = Page.Locator("#stp-disabled");
        await Expect(trap).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(trap).ToHaveCSSAsync("touch-action", "auto");
        await Expect(trap).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));
        await Expect(trap).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(trap).Not.ToHaveAttributeAsync("aria-keyshortcuts", new Regex(".*"));

        var (x, y) = await StartPointAsync("#stp-disabled");
        await DragAsync(x, y, 120, 0);
        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(Page.Locator("#disabled-value")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#disabled-starts")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-triggers")).ToHaveTextAsync("0");

        await Expect(trap).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(trap).ToHaveAttributeAsync("tabindex", "0");
        await Expect(trap).ToHaveAttributeAsync("role", "group");
        await Expect(trap).ToHaveCSSAsync("touch-action", "pinch-zoom");

        (x, y) = await StartPointAsync("#stp-disabled");
        await DragAsync(x, y, 120, 0);
        await Expect(Page.Locator("#disabled-triggers")).ToHaveTextAsync("1");

        await trap.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Page.Locator("#disabled-triggers")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#disabled-trigger")).ToHaveTextAsync(new Regex("^Left\\|0,0\\|00\\|keyboard"));
    }

    [TestMethod]
    public async Task Keyboard_TheTrapIsATabStop_ItsArrowsTriggerInTheirDirection_WithoutScrollingThePage()
    {
        await OpenAsync(Route);

        var trap = Page.Locator("#stp-keyboard");
        await Expect(trap).ToHaveAttributeAsync("role", "group");
        await Expect(trap).ToHaveAttributeAsync("aria-label", "Left: snooze, right: archive");
        await Expect(trap).ToHaveAttributeAsync("aria-keyshortcuts", "ArrowLeft ArrowRight ArrowUp ArrowDown");

        await Page.FocusAsync("#before-keyboard");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(trap).ToBeFocusedAsync();
        // The focus ring of a trap the keyboard reached, in the color its variable names.
        await Expect(trap).ToHaveCSSAsync("box-shadow", new Regex(Regex.Escape("rgb(255, 0, 0)")));

        var scrollY = await Page.EvaluateAsync<double>("() => window.scrollY");

        var expected = new[] { ("ArrowRight", "Right"), ("ArrowLeft", "Left"), ("ArrowUp", "Top"), ("ArrowDown", "Bottom") };
        for (var i = 0; i < expected.Length; i++)
        {
            await Page.Keyboard.PressAsync(expected[i].Item1);
            await Expect(Page.Locator("#keyboard-triggers")).ToHaveTextAsync($"{i + 1}");
            await Expect(Page.Locator("#keyboard-trigger")).ToHaveTextAsync($"{expected[i].Item2}|0,0|00|keyboard|nodur");
        }

        Assert.AreEqual(scrollY, await Page.EvaluateAsync<double>("() => window.scrollY"), "An arrow key scrolled the page.");
    }

    [TestMethod]
    public async Task Keyboard_AModifiedKeyAHeldKeyAndADescendantsKey_TriggerNothingMore()
    {
        await OpenAsync(Route);

        var trap = Page.Locator("#stp-keyboard");
        await trap.FocusAsync();

        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await Page.Keyboard.PressAsync("Control+ArrowLeft");

        // Held: the repeats are one swipe.
        await Page.Keyboard.DownAsync("ArrowRight");
        await Page.Keyboard.DownAsync("ArrowRight");
        await Page.Keyboard.DownAsync("ArrowRight");
        await Page.Keyboard.UpAsync("ArrowRight");

        // On the input inside the trap, the arrows are the input's.
        await Page.FocusAsync("#keyboard-input");
        await Page.Keyboard.PressAsync("ArrowLeft");

        await Page.ClickAsync("#btn-fraction");
        await Expect(Page.Locator("#fraction-value")).ToHaveTextAsync("0.9");
        await Expect(Page.Locator("#keyboard-triggers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#keyboard-trigger")).ToHaveTextAsync(new Regex("^Right"));
    }

    [TestMethod]
    public async Task Keyboard_ALockLimitsTheArrowsToItsAxis_AndTabIndexKeepsTheTrapOutOfTheTabOrder()
    {
        await OpenAsync(Route);

        var hrz = Page.Locator("#stp-keyboard-hrz");
        await Expect(hrz).ToHaveAttributeAsync("aria-keyshortcuts", "ArrowLeft ArrowRight");
        await hrz.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Page.Locator("#keyboard-hrz-triggers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#keyboard-hrz-trigger")).ToHaveTextAsync(new Regex("^Left"));

        var vrt = Page.Locator("#stp-keyboard-vrt");
        await Expect(vrt).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(vrt).ToHaveAttributeAsync("aria-keyshortcuts", "ArrowUp ArrowDown");

        // Tab passes it by.
        await hrz.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(vrt).Not.ToBeFocusedAsync();

        await vrt.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Page.Locator("#keyboard-vrt-triggers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#keyboard-vrt-trigger")).ToHaveTextAsync(new Regex("^Bottom"));

        // A trap without KeyboardTrigger is no tab stop at all.
        var plain = Page.Locator("#stp-no-keyboard");
        await Expect(plain).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));
        await Expect(plain).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(plain).Not.ToHaveAttributeAsync("aria-keyshortcuts", new Regex(".*"));
    }

    [TestMethod]
    public async Task Escape_PutsBackASwipeInsideAnOpenOverlay_WithoutClosingTheOverlay()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-overlay");
        await Expect(Page.Locator("#overlay-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#stp-overlay")).ToBeVisibleAsync();

        var box = (await Page.Locator("#stp-overlay").BoundingBoxAsync())!;
        var x = MathF.Round(box.X + 50);
        var y = MathF.Round(box.Y + box.Height / 2);
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 100, y, new() { Steps = 5 });
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#overlay-end")).ToHaveTextAsync(new Regex(@"\|True\|"));
        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("#overlay-triggers")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#overlay-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#overlay-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Cascade_BitParamsSetsTheDefaults_AnExplicitParameterWins_AndAChangeReachesThem()
    {
        await OpenAsync(Route);

        var cascaded = Page.Locator("#stp-cascade");
        var own = Page.Locator("#stp-cascade-own");
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-stp-hrz"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-stp-vrt"));

        // The cascaded 50px trigger.
        var (x, y) = await StartPointAsync("#stp-cascade");
        await DragAsync(x, y, 60, 0);
        await Expect(Page.Locator("#cascade-triggers")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-cascade-change");

        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-stp-lck"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-stp-vrt"));

        // Now 200px.
        (x, y) = await StartPointAsync("#stp-cascade", fromLeft: 10);
        await DragAsync(x, y, 120, 0);
        await Expect(Page.Locator("#cascade-ends")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#cascade-triggers")).ToHaveTextAsync("1");
        await DragAsync(x, y, 250, 0);
        await Expect(Page.Locator("#cascade-ends")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#cascade-triggers")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task StyleClassAndAttributes_LandOnTheRoot_AndANamedTrapIsAGroup()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#stp-style")).ToHaveCSSAsync("background-color", "rgb(200, 0, 0)");
        await Expect(Page.Locator("#stp-style")).ToHaveCSSAsync("padding-top", "4px");
        await Expect(Page.Locator("#stp-class")).ToHaveClassAsync(HasClass("e2e-stp-class"));
        await Expect(Page.Locator("#stp-class")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(Page.Locator("#stp-class")).ToHaveAttributeAsync("data-e2e", "trap-attr");
        await Expect(Page.Locator("#stp-class")).ToHaveAttributeAsync("title", "trap title");
        await Expect(Page.Locator("#stp-labelledby")).ToHaveAttributeAsync("role", "group");
        await Expect(Page.Locator("#stp-own-role")).ToHaveAttributeAsync("role", "region");
        await Expect(Page.Locator("#stp-own-role")).ToHaveAttributeAsync("aria-label", "Named");
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed()
    {
        await OpenAsync(Route);

        var trap = Page.Locator("#stp-vis");
        await Expect(trap).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-vis-hidden");
        await Expect(trap).ToHaveCSSAsync("visibility", "hidden");

        await Page.ClickAsync("#btn-vis-collapsed");
        await Expect(trap).ToHaveCSSAsync("display", "none");

        await Page.ClickAsync("#btn-vis-visible");
        await Expect(trap).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task CssVariables_TheCursorAtRestAndWhileSwiping()
    {
        await OpenAsync(Route);

        var trap = Page.Locator("#stp-vars");
        await Expect(trap).ToHaveCSSAsync("cursor", "pointer");

        var (x, y) = await StartPointAsync("#stp-vars");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 60, y, new() { Steps = 4 });
        await Expect(trap).ToHaveCSSAsync("cursor", "crosshair");
        await Page.Mouse.UpAsync();
        await Expect(trap).ToHaveCSSAsync("cursor", "pointer");
        await Expect(Page.Locator("#vars-ends")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Rtl_DirRendersTheClassAndTheAttribute_AndTheDirectionStaysPhysical()
    {
        await OpenAsync(Route);

        var trap = Page.Locator("#stp-rtl");
        await Expect(trap).ToHaveAttributeAsync("dir", "rtl");
        await Expect(trap).ToHaveClassAsync(HasClass("bit-rtl"));

        var (x, y) = await StartPointAsync("#stp-rtl");
        await DragAsync(x, y, 100, 0);
        await Expect(Page.Locator("#rtl-trigger")).ToHaveTextAsync(new Regex("^Right\\|100,0\\|"));
    }

    [TestMethod]
    public async Task Unmount_ATrapRemovedMidGesture_LeavesNoErrorBehind()
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-unmount");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 80, y, new() { Steps = 4 });
        await Expect(Page.Locator("#unmount-starts")).ToHaveTextAsync("1");

        await Page.Locator("#btn-unmount").DispatchEventAsync("click");
        await Expect(Page.Locator("#unmount-shown")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#stp-unmount")).ToHaveCountAsync(0);

        await Page.Mouse.MoveAsync(x + 120, y, new() { Steps = 2 });
        await Page.Mouse.UpAsync();

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#stp-unmount")).ToHaveCountAsync(1);
        (x, y) = await StartPointAsync("#stp-unmount");
        await DragAsync(x, y, 100, 0);
        await Expect(Page.Locator("#unmount-starts")).ToHaveTextAsync("2");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // A whole-pixel point of the trap's box: its middle, or the given distance in from its left edge.
    private async Task<(float X, float Y)> StartPointAsync(string selector, float? fromLeft = null)
    {
        var trap = Page.Locator(selector);
        await trap.ScrollIntoViewIfNeededAsync();
        var box = (await trap.BoundingBoxAsync())!;

        return (MathF.Round(fromLeft is null ? box.X + box.Width / 2 : box.X + fromLeft.Value), MathF.Round(box.Y + box.Height / 2));
    }

    private async Task DragAsync(float x, float y, float dx, float dy, int steps = 10)
    {
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + dx, y + dy, new() { Steps = steps });
        await Page.Mouse.UpAsync();
    }
}
