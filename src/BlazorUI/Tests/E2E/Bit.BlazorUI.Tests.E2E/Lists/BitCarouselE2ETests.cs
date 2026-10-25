namespace Bit.BlazorUI.Tests.E2E.Lists;

/// <summary>
/// BitCarousel in a real browser: the slide on screen and where it is, the next / previous buttons and their
/// visibility at the ends, the dots, the keyboard (arrows, Home, End, and the focus kept on dots), OnChange,
/// InfiniteScrolling, HideDots / HideNextPrev, VisibleItemsCount / ScrollItemsCount / Gap, the per breakpoint counts and
/// ResponsiveOptions measured off the carousel and re-measured on resize, Vertical and every DotsPlacement, Fade and
/// AnimationDuration, AutoPlay with ShowPlayPause / PauseOnHover / PauseOnFocus / the hidden page / StopOnInteraction /
/// AutoPlayReverse / StopOnLastSlide / reduced motion and ForceAnimation, the mouse wheel, a mouse drag and
/// DragThreshold, NoDrag, NoKeyboard, Disabled (and the slides staying live), @bind-SelectedPage, DefaultPage, the public
/// methods and state, DotTemplate and the dot labels, the accessibility attributes, the BitParams cascade, every Color,
/// Accent and Size, external icons and icon names, Style / Class / Styles / Classes, the --bit-Carousel-* variables,
/// RTL (explicit and inherited), slides added and removed at runtime, prerendering and unmounting a playing carousel.
/// The touch swipe is in <see cref="BitCarouselTouchE2ETests"/>.
/// </summary>
[TestClass]
public class BitCarouselE2ETests : E2ETestBase
{
    private const double Tolerance = 1;

    // Every BitColor, in the enum's order, with the class suffix and the theme token of its role.
    private static readonly (string Name, string Suffix, string Main)[] Colors =
    [
        ("Primary", "pri", "--bit-clr-pri"), ("Secondary", "sec", "--bit-clr-sec"), ("Tertiary", "ter", "--bit-clr-ter"),
        ("Info", "inf", "--bit-clr-inf"), ("Success", "suc", "--bit-clr-suc"), ("Warning", "wrn", "--bit-clr-wrn"),
        ("SevereWarning", "swr", "--bit-clr-swr"), ("Error", "err", "--bit-clr-err"),
        ("PrimaryBackground", "pbg", "--bit-clr-bg-pri"), ("SecondaryBackground", "sbg", "--bit-clr-bg-sec"), ("TertiaryBackground", "tbg", "--bit-clr-bg-ter"),
        ("PrimaryForeground", "pfg", "--bit-clr-fg-pri"), ("SecondaryForeground", "sfg", "--bit-clr-fg-sec"), ("TertiaryForeground", "tfg", "--bit-clr-fg-ter"),
        ("PrimaryBorder", "pbr", "--bit-clr-brd-pri"), ("SecondaryBorder", "sbr", "--bit-clr-brd-sec"), ("TertiaryBorder", "tbr", "--bit-clr-brd-ter"),
    ];

    [TestMethod]
    public async Task Prerender_TheFirstSlideIsInTheServerRenderedHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/carousel");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"basic\"");
        StringAssert.Contains(html, "bit-crsi-fst");
        StringAssert.Contains(html, "aria-roledescription=\"carousel\"");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_TheButtonsMoveOneSlideAndHideAtTheEnds()
    {
        await OpenAsync("/e2e/carousel");

        await ExpectCurrentAsync("basic", "S1");
        await ExpectSlideInPlaceAsync("basic", "S1");
        var next = Page.Locator("#basic .bit-csl-lbt");
        var prev = Page.Locator("#basic .bit-csl-rbt");
        await Expect(next).ToBeVisibleAsync();
        await Expect(prev).ToBeHiddenAsync();
        await Expect(next).ToHaveAttributeAsync("aria-label", "Next slide");
        await Expect(prev).ToHaveAttributeAsync("aria-label", "Previous slide");

        await next.ClickAsync();
        await ExpectCurrentAsync("basic", "S2");
        await ExpectSlideInPlaceAsync("basic", "S2");
        await Expect(Page.Locator("#changes")).ToHaveTextAsync("1");
        await Expect(prev).ToBeVisibleAsync();

        // A click while the slides are still moving is dropped, so each move is waited for.
        await next.ClickAsync();
        await ExpectCurrentAsync("basic", "S3");
        await ExpectSlideInPlaceAsync("basic", "S3");
        await next.ClickAsync();
        await ExpectCurrentAsync("basic", "S4");
        await Expect(next).ToBeHiddenAsync();
        await Expect(Page.Locator("#changes")).ToHaveTextAsync("1,2,3");

        await prev.ClickAsync();
        await ExpectCurrentAsync("basic", "S3");
        await Expect(Page.Locator("#changes")).ToHaveTextAsync("1,2,3,2");
    }

    [TestMethod]
    public async Task Dots_OnePerPage_TheCurrentOneMarked_AndAClickGoesToItsPage()
    {
        await OpenAsync("/e2e/carousel");

        var dots = Page.Locator("#basic .bit-csl-dot");
        await Expect(dots).ToHaveCountAsync(4);
        await Expect(dots.Nth(0)).ToHaveAttributeAsync("aria-current", "true");
        await Expect(dots.Nth(0)).ToHaveClassAsync(HasClass("bit-csl-cud"));
        await Expect(dots.Nth(0)).ToHaveAttributeAsync("aria-label", "Slide 1");
        await Expect(Page.Locator("#basic .bit-csl-dts")).ToHaveAttributeAsync("aria-label", "Choose slide to display");

        var current = await BoxAsync("#basic .bit-csl-dot >> nth=0");
        var other = await BoxAsync("#basic .bit-csl-dot >> nth=1");
        Assert.IsTrue(current.W > other.W + 2, "the current dot is drawn longer");

        await dots.Nth(2).ClickAsync();
        await ExpectCurrentAsync("basic", "S3");
        await Expect(dots.Nth(2)).ToHaveAttributeAsync("aria-current", "true");
        Assert.IsNull(await dots.Nth(0).GetAttributeAsync("aria-current"));
        await Expect(dots.Nth(2)).ToHaveAttributeAsync("tabindex", "0");
        await Expect(dots.Nth(0)).ToHaveAttributeAsync("tabindex", "-1");
    }

    [TestMethod]
    public async Task Keyboard_ArrowsHomeAndEnd_MoveTheFocusedCarousel()
    {
        await OpenAsync("/e2e/carousel");

        var root = Page.Locator("#basic");
        await Expect(root).ToHaveAttributeAsync("tabindex", "0");
        await WaitForKeysAsync("basic");
        await root.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectCurrentAsync("basic", "S2");
        await Page.Keyboard.PressAsync("End");
        await ExpectCurrentAsync("basic", "S4");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectCurrentAsync("basic", "S3");
        await Page.Keyboard.PressAsync("Home");
        await ExpectCurrentAsync("basic", "S1");
        await Expect(root).ToBeFocusedAsync();

        // From a dot, the focus follows the current dot.
        await Page.Locator("#basic .bit-csl-dot").Nth(0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectCurrentAsync("basic", "S2");
        await Expect(Page.Locator("#basic .bit-csl-dot").Nth(1)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task InfiniteScrolling_WrapsAroundBothEnds()
    {
        await OpenAsync("/e2e/carousel");

        var prev = Page.Locator("#infinite .bit-csl-rbt");
        var next = Page.Locator("#infinite .bit-csl-lbt");
        await Expect(prev).ToBeVisibleAsync();

        await prev.ClickAsync();
        await ExpectCurrentAsync("infinite", "S4");
        await ExpectSlideInPlaceAsync("infinite", "S4");

        await next.ClickAsync();
        await ExpectCurrentAsync("infinite", "S1");
        await ExpectSlideInPlaceAsync("infinite", "S1");
    }

    [TestMethod]
    public async Task HideDotsAndHideNextPrev_LeaveTheControlsOut()
    {
        await OpenAsync("/e2e/carousel");

        await Expect(Page.Locator("#hide-dots .bit-csl-dot")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#hide-dots .bit-csl-lbt")).ToBeVisibleAsync();
        await Expect(Page.Locator("#hide-buttons .bit-csl-lbt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#hide-buttons .bit-csl-rbt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#hide-buttons .bit-csl-dot")).ToHaveCountAsync(3);

        // A single slide has nothing to page through.
        await Expect(Page.Locator("#single .bit-csl-dot")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#single .bit-csl-lbt")).ToBeHiddenAsync();
        await ExpectCurrentAsync("single", "S1");
    }

    [TestMethod]
    public async Task MultipleItems_VisibleItemsCountShowsSeveral_ScrollItemsCountMovesSeveral()
    {
        await OpenAsync("/e2e/carousel");

        await Expect(Page.Locator("#multiple .bit-crsi-cur")).ToHaveTextAsync(new[] { "S1", "S2", "S3" });
        await Expect(Page.Locator("#multiple .bit-csl-dot")).ToHaveCountAsync(4);
        var container = await BoxAsync("#multiple .bit-csl-cnt");
        for (var i = 0; i < 3; i++)
        {
            var slide = await BoxAsync($"#multiple .bit-crsi-cur >> nth={i}");
            AssertClose(container.W / 3, slide.W, $"slide {i} is a third");
            AssertClose(container.X + i * container.W / 3, slide.X, $"slide {i} in place");
        }

        await Page.Locator("#multiple .bit-csl-lbt").ClickAsync();
        await Expect(Page.Locator("#multiple .bit-crsi-cur")).ToHaveTextAsync(new[] { "S2", "S3", "S4" });

        await Expect(Page.Locator("#multiple-scroll .bit-csl-dot")).ToHaveCountAsync(2);
        await Page.Locator("#multiple-scroll .bit-csl-lbt").ClickAsync();
        await Expect(Page.Locator("#multiple-scroll .bit-crsi-cur")).ToHaveTextAsync(new[] { "S4", "S5", "S6" });
    }

    [TestMethod]
    public async Task Gap_PadsEverySlideByHalfOnEachSide()
    {
        await OpenAsync("/e2e/carousel");

        await Expect(Page.Locator("#gap")).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("--bit-csl-gap:20px"));
        await Expect(Page.Locator("#gap .bit-crsi").First).ToHaveCSSAsync("padding-left", "10px");
        var first = await BoxAsync("#gap .bit-crsi-cur >> nth=0", ".csl-slide");
        var second = await BoxAsync("#gap .bit-crsi-cur >> nth=1", ".csl-slide");
        AssertClose(20, second.X - first.Right, "20px between two slides");
    }

    [TestMethod]
    public async Task Responsive_TheCountsFollowTheWidthOfTheCarouselItself()
    {
        await OpenAsync("/e2e/carousel");

        var expected = new Dictionary<int, int> { [500] = 1, [700] = 2, [1000] = 3, [1300] = 4, [2000] = 5, [2600] = 6 };
        foreach (var (width, count) in expected)
        {
            await Expect(Page.Locator($"#responsive-{width} .bit-crsi-cur")).ToHaveCountAsync(count);
        }

        // A breakpoint carries upwards, and the plain count is the base.
        await Expect(Page.Locator("#responsive-carry .bit-crsi-cur")).ToHaveCountAsync(2);

        // Resized at runtime, the carousel measures itself again.
        await Expect(Page.Locator("#responsive .bit-crsi-cur")).ToHaveCountAsync(1);
        await Page.ClickAsync("#btn-responsive-width");
        await Expect(Page.Locator("#responsive .bit-crsi-cur")).ToHaveCountAsync(3);
        await Page.ClickAsync("#btn-responsive-width");
        await Expect(Page.Locator("#responsive .bit-crsi-cur")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task ResponsiveOptions_TheSmallestMatchingBreakpointWins()
    {
        await OpenAsync("/e2e/carousel");

        await Expect(Page.Locator("#options-500 .bit-crsi-cur")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#options-800 .bit-crsi-cur")).ToHaveCountAsync(2);
        await Expect(Page.Locator("#options-1000 .bit-crsi-cur")).ToHaveCountAsync(3);

        // The option's ScrollItemsCount moves two at once.
        await Page.Locator("#options-800 .bit-csl-lbt").ClickAsync();
        await Expect(Page.Locator("#options-800 .bit-crsi-cur")).ToHaveTextAsync(new[] { "S3", "S4" });
    }

    [TestMethod]
    public async Task Vertical_SlidesMoveUpAndDown_WithTheVerticalKeys()
    {
        await OpenAsync("/e2e/carousel");

        var root = Page.Locator("#vertical");
        await Expect(root).ToHaveClassAsync(HasClass("bit-csl-vrt"));
        var container = await BoxAsync("#vertical .bit-csl-cnt");
        AssertClose(container.Bottom, (await BoxAsync("#vertical .bit-csl-lbt")).Bottom, "the next button at the bottom");

        await WaitForKeysAsync("vertical");
        await root.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectCurrentAsync("vertical", "S2");
        await Page.WaitForFunctionAsync(@"() => {
            const c = document.querySelector('#vertical .bit-csl-cnt').getBoundingClientRect();
            const s = document.querySelector('#vertical .bit-crsi-cur').getBoundingClientRect();
            return Math.abs(s.top - c.top) <= 1 && Math.abs(s.left - c.left) <= 1;
        }");

        // The horizontal keys do nothing to a vertical carousel.
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectCurrentAsync("vertical", "S1");
    }

    [TestMethod]
    public async Task DotsPlacement_EveryPlacement_PutsTheDotsOnItsSide()
    {
        await OpenAsync("/e2e/carousel");

        var top = await BoxAsync("#dots-top .bit-csl-dcn");
        Assert.IsTrue(top.Bottom <= (await BoxAsync("#dots-top .bit-csl-cnt")).Y + Tolerance, "Top: above the slides");
        await Expect(Page.Locator("#dots-top")).ToHaveClassAsync(HasClass("bit-csl-dtop"));

        var bottom = await BoxAsync("#dots-bottom .bit-csl-dcn");
        Assert.IsTrue(bottom.Y >= (await BoxAsync("#dots-bottom .bit-csl-cnt")).Bottom - Tolerance, "Bottom: below the slides");

        var start = await BoxAsync("#dots-start .bit-csl-dcn");
        Assert.IsTrue(start.Right <= (await BoxAsync("#dots-start .bit-csl-cnt")).X + Tolerance, "Start: before the slides");
        await Expect(Page.Locator("#dots-start .bit-csl-dts")).ToHaveCSSAsync("flex-direction", "column");
        var d0 = await BoxAsync("#dots-start .bit-csl-dot >> nth=0");
        var d1 = await BoxAsync("#dots-start .bit-csl-dot >> nth=1");
        Assert.IsTrue(d1.Y > d0.Bottom - Tolerance, "Start: the dots stacked");
        Assert.IsTrue(d0.H > d1.H + 2, "Start: the current dot drawn taller");

        var end = await BoxAsync("#dots-end .bit-csl-dcn");
        Assert.IsTrue(end.X >= (await BoxAsync("#dots-end .bit-csl-cnt")).Right - Tolerance, "End: after the slides");
    }

    [TestMethod]
    public async Task Fade_CrossFadesTheSlidesInPlace_OneAtATime()
    {
        await OpenAsync("/e2e/carousel");

        await Expect(Page.Locator("#fade .bit-crsi-cur")).ToHaveCountAsync(1);
        var slides = Page.Locator("#fade .bit-crsi");
        await Expect(slides.Nth(0)).ToHaveCSSAsync("opacity", "1");
        await Expect(slides.Nth(1)).ToHaveCSSAsync("opacity", "0");
        AssertClose((await BoxAsync("#fade .bit-crsi >> nth=0")).X, (await BoxAsync("#fade .bit-crsi >> nth=1")).X, "the slides share one place");

        await Page.Locator("#fade .bit-csl-lbt").ClickAsync();
        await ExpectCurrentAsync("fade", "S2");
        await Expect(slides.Nth(1)).ToHaveCSSAsync("opacity", "1");
        await Expect(slides.Nth(0)).ToHaveCSSAsync("opacity", "0");
        await Expect(slides.Nth(1)).ToHaveCSSAsync("transition-property", "opacity");
        await Expect(slides.Nth(1)).ToHaveCSSAsync("transition-duration", "0.2s");
    }

    [TestMethod]
    public async Task AnimationDuration_SetsTheSlideTransition_AndZeroMovesAtOnce()
    {
        await OpenAsync("/e2e/carousel");

        await Expect(Page.Locator("#duration")).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("--bit-csl-dur-full:0.3s"));
        await Page.Locator("#duration .bit-csl-lbt").ClickAsync();
        await ExpectCurrentAsync("duration", "S2");
        await Expect(Page.Locator("#duration .bit-crsi-cur")).ToHaveCSSAsync("transition-duration", "0.3s");
        await ExpectSlideInPlaceAsync("duration", "S2");

        await Page.Locator("#duration-zero .bit-csl-lbt").ClickAsync();
        await ExpectCurrentAsync("duration-zero", "S2");
        await Expect(Page.Locator("#duration-zero .bit-crsi-cur")).ToHaveCSSAsync("transition-duration", "0s");
        await ExpectSlideInPlaceAsync("duration-zero", "S2");
    }

    [TestMethod]
    public async Task AutoPlay_AdvancesOnItsOwn_AndThePlayPauseButtonStopsIt()
    {
        await OpenAsync("/e2e/carousel");

        await ExpectCurrentAsync("autoplay", "S2");
        await ExpectCurrentAsync("autoplay", "S3");
        var button = Page.Locator("#autoplay .bit-csl-ppb");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Stop automatic slide show");
        await Expect(Page.Locator("#autoplay .bit-csl-cnt")).ToHaveAttributeAsync("aria-live", "off");
        await Expect(button.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Pause"));

        await button.ClickAsync();
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(button).ToHaveAttributeAsync("aria-label", "Start automatic slide show");
        await Expect(button.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Play"));
        await Expect(Page.Locator("#autoplay .bit-csl-cnt")).ToHaveAttributeAsync("aria-live", "polite");
        await ExpectAutoPlayStateAsync("autoplay", "playing=False;paused=True");

        // Resumed by the button, even with the pointer over it.
        await button.ClickAsync();
        await ExpectAutoPlayStateAsync("autoplay", "playing=True;paused=False");
    }

    [TestMethod]
    public async Task AutoPlay_PausesOnHoverAndOnFocus_UnlessTurnedOff()
    {
        await OpenAsync("/e2e/carousel");

        await Page.Locator("#autoplay .bit-csl-cnt").HoverAsync();
        await ExpectAutoPlayStateAsync("autoplay", "playing=False;paused=False");
        await Page.Mouse.MoveAsync(0, 0);
        await ExpectAutoPlayStateAsync("autoplay", "playing=True;paused=False");

        await Page.Locator("#autoplay").FocusAsync();
        await ExpectAutoPlayStateAsync("autoplay", "playing=False;paused=False");
        await Page.Locator("#btn-read-autoplay").FocusAsync();
        await ExpectAutoPlayStateAsync("autoplay", "playing=True;paused=False");

        // PauseOnHover and PauseOnFocus off: it keeps playing.
        await Page.Locator("#no-pause .bit-csl-cnt").HoverAsync();
        await Page.Locator("#no-pause").FocusAsync();
        await ExpectAutoPlayStateAsync("no-pause", "playing=True;paused=False");
        var before = await CurrentTextAsync("no-pause");
        await Expect(Page.Locator("#no-pause .bit-crsi-cur")).Not.ToHaveTextAsync(before);
    }

    [TestMethod]
    public async Task AutoPlay_PausesWhileThePageIsHidden()
    {
        await OpenAsync("/e2e/carousel");

        await ExpectAutoPlayStateAsync("no-pause", "playing=True;paused=False");
        await SetPageHiddenAsync(true);
        await ExpectAutoPlayStateAsync("no-pause", "playing=False;paused=False");
        await SetPageHiddenAsync(false);
        await ExpectAutoPlayStateAsync("no-pause", "playing=True;paused=False");
    }

    [TestMethod]
    public async Task AutoPlay_StopOnInteraction_StopsForGoodOnceNavigatedByHand()
    {
        await OpenAsync("/e2e/carousel");

        await ExpectAutoPlayStateAsync("stop", "playing=True;paused=False");
        await Page.Locator("#stop-on-interaction .bit-csl-lbt").ClickAsync();
        await Page.Mouse.MoveAsync(0, 0);
        await ExpectAutoPlayStateAsync("stop", "playing=False;paused=True");
    }

    [TestMethod]
    public async Task AutoPlay_ReverseGoesBackwards_AndStopOnLastSlideStopsAtTheEnd()
    {
        await OpenAsync("/e2e/carousel");

        await ExpectCurrentAsync("reverse", "S4");
        await ExpectCurrentAsync("reverse", "S3");

        await ExpectCurrentAsync("stop-on-last", "S3");
        await ExpectAutoPlayStateAsync("last", "page=2;playing=False;paused=True");
    }

    [TestMethod]
    public async Task AutoPlay_StartsPausedUnderReducedMotion_UnlessForceAnimation()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync("/e2e/carousel");

        await ExpectAutoPlayStateAsync("autoplay", "playing=False;paused=True");
        await Expect(Page.Locator("#autoplay .bit-csl-ppb")).ToHaveAttributeAsync("aria-label", "Start automatic slide show");
        await ExpectAutoPlayStateAsync("forced", "playing=True;paused=False");
        await Expect(Page.Locator("#forced")).ToHaveClassAsync(HasClass("bit-fam"));

        // The slide transition collapses with the motion tokens (to a hundred-thousandth of a second).
        await Page.Locator("#duration .bit-csl-lbt").ClickAsync();
        await ExpectCurrentAsync("duration", "S2");
        var duration = await ComputedStyleAsync("#duration .bit-crsi-cur", "transition-duration");
        Assert.IsTrue(double.Parse(duration.TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture) < 0.001, $"the transition collapsed ({duration})");
    }

    [TestMethod]
    public async Task Wheel_TheMouseWheelPagesTheCarousel()
    {
        await OpenAsync("/e2e/carousel");

        var container = Page.Locator("#wheel .bit-csl-cnt");
        await container.HoverAsync();
        await Page.Mouse.WheelAsync(0, 100);
        await ExpectCurrentAsync("wheel", "S2");

        // A burst of wheel events is one move; the next one moves again once the throttle has passed.
        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Page.Mouse.WheelAsync(0, -100);
            try
            {
                await Expect(Page.Locator("#wheel .bit-crsi-cur")).ToHaveTextAsync("S1", new() { Timeout = 300 });
                break;
            }
            catch (PlaywrightException) when (attempt < 19) { }
        }
        await ExpectCurrentAsync("wheel", "S1");

        // NoDrag is on: a drag does not move it.
        await DragAsync("#wheel .bit-csl-cnt", -150);
        await Expect(Page.Locator("#wheel .bit-csl-cnt")).ToHaveClassAsync(HasClass("bit-csl-ndr"));
        await ExpectCurrentAsync("wheel", "S1");
    }

    [TestMethod]
    public async Task Drag_AMouseDragPagesTheCarousel_PastTheDragThreshold()
    {
        await OpenAsync("/e2e/carousel");

        await DragAsync("#drag .bit-csl-cnt", -100);
        await ExpectCurrentAsync("drag", "S2");
        await DragAsync("#drag .bit-csl-cnt", 100);
        await ExpectCurrentAsync("drag", "S1");

        // DragThreshold 80: a 50px drag is not enough, a 150px one is.
        await DragAsync("#threshold .bit-csl-cnt", -50);
        await Page.Locator("#threshold .bit-csl-dot").Nth(0).EvaluateAsync("el => el.focus()");
        await ExpectCurrentAsync("threshold", "S1");
        await DragAsync("#threshold .bit-csl-cnt", -150);
        await ExpectCurrentAsync("threshold", "S2");

        // A drag across the axis is left to the page.
        await DragAsync("#drag .bit-csl-cnt", 0, 120);
        await ExpectCurrentAsync("drag", "S1");
    }

    [TestMethod]
    public async Task NoKeyboard_TakesTheCarouselOutOfTheTabOrder()
    {
        await OpenAsync("/e2e/carousel");

        Assert.IsNull(await Page.Locator("#no-keyboard").GetAttributeAsync("tabindex"));
        Assert.IsNull(await Page.Locator("#no-keyboard .bit-csl-dot").First.GetAttributeAsync("tabindex"));
        await Page.Locator("#no-keyboard .bit-csl-dot").First.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("End");
        await Page.Locator("#no-keyboard .bit-csl-lbt").ClickAsync();
        await ExpectCurrentAsync("no-keyboard", "S2");
    }

    [TestMethod]
    public async Task Disabled_TheControlsAreDisabled_AndTheSlidesStayLive()
    {
        await OpenAsync("/e2e/carousel");

        var root = Page.Locator("#disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        Assert.IsNull(await root.GetAttributeAsync("tabindex"));
        foreach (var selector in new[] { "#disabled .bit-csl-lbt", "#disabled .bit-csl-ppb", "#disabled .bit-csl-dot >> nth=0" })
        {
            await Expect(Page.Locator(selector)).ToBeDisabledAsync();
            await Expect(Page.Locator(selector)).ToHaveAttributeAsync("aria-disabled", "true");
        }
        await Expect(Page.Locator("#disabled .bit-csl-cnt")).ToHaveClassAsync(HasClass("bit-csl-ndr"));

        // A drag and a click delivered anyway move nothing.
        await DragAsync("#disabled .bit-csl-cnt", -150);
        await Page.Locator("#disabled .bit-csl-lbt").DispatchEventAsync("click");
        await ExpectCurrentAsync("disabled", "S1");

        // The carousel hosts content: a click on a slide still reaches the page's handler.
        await Page.Locator("#disabled .bit-crsi-cur .csl-slide").ClickAsync();
        await Expect(Page.Locator("#root-clicks")).Not.ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(root).ToHaveAttributeAsync("tabindex", "0");
        await Page.Locator("#disabled .bit-csl-lbt").ClickAsync();
        await ExpectCurrentAsync("disabled", "S2");
    }

    [TestMethod]
    public async Task SelectedPage_TwoWayBinding_AndDefaultPage()
    {
        await OpenAsync("/e2e/carousel");

        await Expect(Page.Locator("#selected-page")).ToHaveTextAsync("1");
        await Page.ClickAsync("#btn-select-3");
        await ExpectCurrentAsync("bound", "S3");

        await Page.Locator("#bound .bit-csl-dot").Nth(1).ClickAsync();
        await ExpectCurrentAsync("bound", "S2");
        await Expect(Page.Locator("#selected-page")).ToHaveTextAsync("2");

        await ExpectCurrentAsync("default-page", "S2");
        await Expect(Page.Locator("#default-page .bit-csl-dot").Nth(1)).ToHaveAttributeAsync("aria-current", "true");
    }

    [TestMethod]
    public async Task PublicMembers_NavigateAndReportTheState()
    {
        await OpenAsync("/e2e/carousel");

        await Page.ClickAsync("#api-next");
        await ExpectCurrentAsync("bound", "S2");
        await Expect(Page.Locator("#selected-page")).ToHaveTextAsync("2");
        await Page.ClickAsync("#api-goto");
        await ExpectCurrentAsync("bound", "S4");
        await Page.ClickAsync("#api-prev");
        await ExpectCurrentAsync("bound", "S3");
        await Page.ClickAsync("#api-refresh");
        await Page.ClickAsync("#api-read");
        await Expect(Page.Locator("#api-state")).ToHaveTextAsync("current=2;pages=4;items=4");

        await Page.ClickAsync("#api-read-play");
        await Expect(Page.Locator("#api-play-state")).ToHaveTextAsync("playing=True;paused=False");
        await Page.ClickAsync("#api-pause");
        await Page.ClickAsync("#api-read-play");
        await Expect(Page.Locator("#api-play-state")).ToHaveTextAsync("playing=False;paused=True");
        await Page.ClickAsync("#api-resume");
        await Page.ClickAsync("#api-read-play");
        await Expect(Page.Locator("#api-play-state")).ToHaveTextAsync("playing=True;paused=False");
        await Page.ClickAsync("#api-toggle");
        await Page.ClickAsync("#api-read-play");
        await Expect(Page.Locator("#api-play-state")).ToHaveTextAsync("playing=False;paused=True");
        await Page.ClickAsync("#api-toggle");
        await Page.ClickAsync("#api-read-play");
        await Expect(Page.Locator("#api-play-state")).ToHaveTextAsync("playing=True;paused=False");
    }

    [TestMethod]
    public async Task DotTemplate_FillsEveryDot_WhichKeepsItsLabelAndCurrentMarker()
    {
        await OpenAsync("/e2e/carousel");

        var dots = Page.Locator("#dot-template .bit-csl-dot");
        await Expect(dots).ToHaveTextAsync(new[] { "1", "2", "3" });
        await Expect(dots.Nth(0)).ToHaveClassAsync(HasClass("bit-csl-dtt"));
        await Expect(dots.Nth(1)).ToHaveAttributeAsync("aria-label", "Picture 2");
        await Expect(Page.Locator("#dot-template .bit-csl-dts")).ToHaveAttributeAsync("aria-label", "Pick a picture");
        await Expect(dots.Nth(0)).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(dots.Nth(0)).ToHaveCSSAsync("color", await ResolveAsync("var(--bit-clr-pri)"));

        await dots.Nth(2).ClickAsync();
        await ExpectCurrentAsync("dot-template", "S3");
        await Expect(dots.Nth(2)).ToHaveAttributeAsync("aria-current", "true");
    }

    [TestMethod]
    public async Task Accessibility_TheCarouselPatternOfTheAuthoringPractices()
    {
        await OpenAsync("/e2e/carousel");

        var basic = Page.Locator("#basic");
        await Expect(basic).ToHaveAttributeAsync("role", "region");
        await Expect(basic).ToHaveAttributeAsync("aria-roledescription", "carousel");
        await Expect(Page.GetByRole(AriaRole.Region, new() { Name = "Basic carousel" })).ToHaveCountAsync(1);

        var slides = Page.Locator("#basic .bit-crsi");
        await Expect(slides.Nth(0)).ToHaveAttributeAsync("role", "group");
        await Expect(slides.Nth(0)).ToHaveAttributeAsync("aria-roledescription", "slide");
        await Expect(slides.Nth(0)).ToHaveAttributeAsync("aria-label", "1 of 4");
        Assert.IsNull(await slides.Nth(0).GetAttributeAsync("inert"));
        await Expect(slides.Nth(1)).ToHaveAttributeAsync("inert", "");
        await Expect(slides.Nth(1)).ToHaveAttributeAsync("aria-hidden", "true");

        // The controls name what they control.
        var containerId = await Page.Locator("#basic .bit-csl-cnt").GetAttributeAsync("id");
        await Expect(Page.Locator("#basic .bit-csl-lbt")).ToHaveAttributeAsync("aria-controls", containerId!);
        await Expect(Page.Locator("#basic .bit-csl-dot").First).ToHaveAttributeAsync("aria-controls", containerId!);
        await Expect(Page.Locator("#basic .bit-csl-cnt")).ToHaveAttributeAsync("aria-live", "polite");

        // The custom labels.
        var a11y = Page.Locator("#a11y");
        await Expect(a11y.Locator(".bit-crsi").Nth(1)).ToHaveAttributeAsync("aria-label", "Slide 2 of 4");
        await Expect(a11y.Locator(".bit-crsi").Nth(3)).ToHaveAttributeAsync("aria-label", "The named slide");
        await Expect(a11y.Locator(".bit-csl-lbt")).ToHaveAttributeAsync("aria-label", "Forwards");
        await Expect(a11y.Locator(".bit-csl-rbt")).ToHaveAttributeAsync("aria-label", "Backwards");
        await Expect(a11y.Locator(".bit-csl-ppb")).ToHaveAttributeAsync("aria-label", "Pause it");
        await a11y.Locator(".bit-csl-ppb").ClickAsync();
        await Expect(a11y.Locator(".bit-csl-ppb")).ToHaveAttributeAsync("aria-label", "Play it");

        // The play/pause button is the first control the keyboard reaches.
        await a11y.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(a11y.Locator(".bit-csl-ppb")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task CascadingParameters_BitCarouselParams_AreDefaultsAndStopAtTheirScope()
    {
        await OpenAsync("/e2e/carousel");

        await Expect(Page.Locator("#casc .bit-csl-dot")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#casc")).ToHaveClassAsync(HasClass("bit-csl-lg"));
        await Expect(Page.Locator("#casc .bit-csl-rbt")).ToBeVisibleAsync();

        await Expect(Page.Locator("#casc-own .bit-csl-dot")).ToHaveCountAsync(3);
        await Expect(Page.Locator("#casc-own .bit-csl-rbt")).ToBeVisibleAsync();

        await Expect(Page.Locator("#casc-out .bit-csl-dot")).ToHaveCountAsync(3);
        await Expect(Page.Locator("#casc-out .bit-csl-rbt")).ToBeHiddenAsync();
        await Expect(Page.Locator("#casc-out")).Not.ToHaveClassAsync(HasClass("bit-csl-lg"));
    }

    [TestMethod]
    public async Task Color_EveryColor_PaintsTheCurrentDotAndTheButtons()
    {
        await OpenAsync("/e2e/carousel");

        foreach (var (name, suffix, main) in Colors)
        {
            var color = await ResolveAsync($"var({main})");
            await Expect(Page.Locator($"#color-{name}")).ToHaveClassAsync(HasClass($"bit-csl-{suffix}"));
            await Expect(Page.Locator($"#color-{name} .bit-csl-cud")).ToHaveCSSAsync("background-color", color);
            await Expect(Page.Locator($"#color-{name} .bit-csl-lbt")).ToHaveCSSAsync("color", color);
        }
    }

    [TestMethod]
    [DataRow("Primary", "bit-csl-apri", "var(--bit-clr-pri)")]
    [DataRow("Secondary", "bit-csl-asec", "var(--bit-clr-sec)")]
    [DataRow("Tertiary", "bit-csl-ater", "var(--bit-clr-ter)")]
    [DataRow("Transparent", "bit-csl-atra", "transparent")]
    public async Task Accent_EveryValue_PaintsTheCurrentDot(string accent, string cls, string value)
    {
        await OpenAsync("/e2e/carousel");

        await Expect(Page.Locator($"#accent-{accent}")).ToHaveClassAsync(HasClass(cls));
        await Expect(Page.Locator($"#accent-{accent} .bit-csl-cud")).ToHaveCSSAsync("background-color", await ResolveAsync(value));
    }

    [TestMethod]
    [DataRow("size-sm", "bit-csl-sm", "8px")]
    [DataRow("size-md", "bit-csl-md", "10px")]
    [DataRow("size-lg", "bit-csl-lg", "14px")]
    public async Task Size_EveryValue_SizesTheDots(string id, string cls, string dot)
    {
        await OpenAsync("/e2e/carousel");

        await Expect(Page.Locator($"#{id}")).ToHaveClassAsync(HasClass(cls));
        await Expect(Page.Locator($"#{id} .bit-csl-dot").Nth(1)).ToHaveCSSAsync("width", dot);
        await Expect(Page.Locator($"#{id} .bit-csl-dot").Nth(1)).ToHaveCSSAsync("height", dot);
    }

    [TestMethod]
    public async Task Icons_ExternalIconsAndIconNames()
    {
        await OpenAsync("/e2e/carousel");

        await Expect(Page.Locator("#icons .bit-csl-lbt i")).ToHaveClassAsync(HasClass("csl-ext-left"));
        await Expect(Page.Locator("#icons .bit-csl-rbt i")).ToHaveClassAsync(HasClass("csl-ext-right"));
        await Expect(Page.Locator("#icons .bit-csl-ppb i")).ToHaveClassAsync(HasClass("csl-ext-pause"));
        await Page.Locator("#icons .bit-csl-ppb").ClickAsync();
        await Expect(Page.Locator("#icons .bit-csl-ppb i")).ToHaveClassAsync(HasClass("csl-ext-play"));

        await Expect(Page.Locator("#icon-names .bit-csl-lbt i")).ToHaveClassAsync(HasClass("bit-icon--Forward"));
        await Expect(Page.Locator("#icon-names .bit-csl-rbt i")).ToHaveClassAsync(HasClass("bit-icon--Back"));
        await Expect(Page.Locator("#icon-names .bit-csl-ppb i")).ToHaveClassAsync(HasClass("bit-icon--CirclePause"));
        await Page.Locator("#icon-names .bit-csl-ppb").ClickAsync();
        await Expect(Page.Locator("#icon-names .bit-csl-ppb i")).ToHaveClassAsync(HasClass("bit-icon--PlaySolid"));

        // The defaults.
        await Expect(Page.Locator("#basic .bit-csl-lbt i")).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        await Expect(Page.Locator("#vertical .bit-csl-lbt i")).ToHaveClassAsync(HasClass("bit-icon--ChevronDown"));
    }

    [TestMethod]
    public async Task StyleClassStylesClasses_ReachTheRootAndEveryPart()
    {
        await OpenAsync("/e2e/carousel");

        await Expect(Page.Locator("#styled")).ToHaveClassAsync(HasClass("csl-root-class"));
        await Expect(Page.Locator("#styled")).ToHaveClassAsync(HasClass("csl-extra-root"));
        await Expect(Page.Locator("#styled")).ToHaveCSSAsync("height", "150px");
        await Expect(Page.Locator("#styled")).ToHaveCSSAsync("padding-top", "1px");

        var parts = new (string Selector, string Class, string Property, string Value)[]
        {
            ("#styled .bit-csl-cnt", "csl-cnt-class", "margin-top", "2px"),
            ("#styled .bit-crsi >> nth=1", "csl-item-class", "padding-top", "3px"),
            ("#styled .bit-crsi-cur", "csl-cur-class", "padding-bottom", "4px"),
            ("#styled .bit-csl-lbt", "csl-btns-class", "padding-left", "5px"),
            ("#styled .bit-csl-lbt i", "csl-btn-icons-class", "padding-left", "6px"),
            ("#styled .bit-csl-lbt", "csl-left-class", "padding-right", "7px"),
            ("#styled .bit-csl-lbt i", "csl-left-icon-class", "padding-right", "8px"),
            ("#styled .bit-csl-rbt", "csl-right-class", "padding-right", "9px"),
            ("#styled .bit-csl-rbt i", "csl-right-icon-class", "padding-right", "10px"),
            ("#styled .bit-csl-dcn", "csl-dcn-class", "padding-top", "11px"),
            ("#styled .bit-csl-dot >> nth=1", "csl-dots-class", "margin-top", "12px"),
            ("#styled .bit-csl-cud", "csl-cud-class", "margin-bottom", "13px"),
            ("#styled .bit-csl-ppb", "csl-ppb-class", "margin-left", "14px"),
            ("#styled .bit-csl-ppb i", "csl-ppb-icon-class", "margin-left", "15px"),
        };

        foreach (var (selector, cls, property, value) in parts)
        {
            await Expect(Page.Locator(selector)).ToHaveClassAsync(HasClass(cls));
            await Expect(Page.Locator(selector)).ToHaveCSSAsync(property, value);
        }

        // A slide that is not current carries neither the current class nor the current style.
        await Expect(Page.Locator("#styled .bit-crsi").Nth(1)).Not.ToHaveClassAsync(HasClass("csl-cur-class"));
        await Expect(Page.Locator("#styled .bit-csl-dot").Nth(1)).Not.ToHaveClassAsync(HasClass("csl-cud-class"));
    }

    [TestMethod]
    public async Task Styles_OnTheButtons_KeepTheButtonsHiddenAtTheEnds()
    {
        await OpenAsync("/e2e/carousel");

        // At the first slide there is nothing to go back to, whatever the page styled the buttons with.
        var prev = Page.Locator("#styled-buttons .bit-csl-rbt");
        var next = Page.Locator("#styled-buttons .bit-csl-lbt");
        await Expect(next).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(prev).ToBeHiddenAsync();

        await next.ClickAsync();
        await ExpectCurrentAsync("styled-buttons", "S2");
        await Expect(next).ToBeHiddenAsync();
        await Expect(prev).ToBeVisibleAsync();
        await Expect(prev).ToHaveCSSAsync("color", "rgb(1, 2, 3)");

        // The style of every dot and the style of the current one together.
        await Expect(Page.Locator("#styled-dots .bit-csl-dot").Nth(0)).ToHaveCSSAsync("outline-color", "rgb(4, 5, 6)");
        await Expect(Page.Locator("#styled-dots .bit-csl-dot").Nth(0)).ToHaveCSSAsync("background-color", "rgb(7, 8, 9)");
        await Expect(Page.Locator("#styled-dots .bit-csl-dot").Nth(1)).ToHaveCSSAsync("outline-color", "rgb(4, 5, 6)");
    }

    [TestMethod]
    public async Task CssVariables_ReskinTheCarouselItsButtonsAndItsDots()
    {
        await OpenAsync("/e2e/carousel");

        var root = Page.Locator("#vars");
        await Expect(root).ToHaveCSSAsync("height", "150px");
        await Expect(root).ToHaveCSSAsync("row-gap", "9px");
        await Expect(Page.Locator("#vars .bit-crsi").First).ToHaveCSSAsync("padding-left", "5px");

        var button = Page.Locator("#vars .bit-csl-lbt");
        await Expect(button).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(7, 8, 9)");
        await Expect(button).ToHaveCSSAsync("opacity", "0.3");
        await Expect(button).ToHaveCSSAsync("font-size", "33px");
        await Expect(button).ToHaveCSSAsync("width", "50px");
        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(10, 11, 12)");
        await Expect(button).ToHaveCSSAsync("opacity", "0.7");

        var dots = Page.Locator("#vars .bit-csl-dot");
        await Expect(dots.Nth(1)).ToHaveCSSAsync("background-color", "rgb(13, 14, 15)");
        await Expect(dots.Nth(1)).ToHaveCSSAsync("width", "12px");
        await Expect(dots.Nth(1)).ToHaveCSSAsync("border-top-left-radius", "2px");
        await Expect(dots.Nth(0)).ToHaveCSSAsync("background-color", "rgb(19, 20, 21)");
        await Expect(dots.Nth(0)).ToHaveCSSAsync("width", "40px");
        AssertClose(7, (await BoxAsync("#vars .bit-csl-dot >> nth=1")).X - (await BoxAsync("#vars .bit-csl-dot >> nth=0")).Right, "the dots gap");
        await dots.Nth(1).HoverAsync();
        await Expect(dots.Nth(1)).ToHaveCSSAsync("background-color", "rgb(16, 17, 18)");
        await dots.Nth(0).HoverAsync();
        await Expect(dots.Nth(0)).ToHaveCSSAsync("background-color", "rgb(22, 23, 24)");

        // The easing of the slide transition.
        await Page.Mouse.MoveAsync(0, 0);
        await Page.Locator("#vars .bit-csl-rbt").ClickAsync();
        await Expect(Page.Locator("#vars .bit-crsi-cur").First).ToHaveCSSAsync("transition-timing-function", "linear");

        // The focus ring of the keyboard-focused root.
        await Page.Locator("#vars-color").FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        while (await Page.EvaluateAsync<bool>("() => document.activeElement && document.activeElement.id !== 'vars'"))
        {
            await Page.Keyboard.PressAsync("Shift+Tab");
        }
        StringAssert.Contains(await ComputedStyleAsync("#vars", "box-shadow"), "rgb(9, 9, 9)");

        // An explicit Color wins over the dot variable.
        await Expect(Page.Locator("#vars-color .bit-csl-cud")).ToHaveCSSAsync("background-color", await ResolveAsync("var(--bit-clr-err)"));
    }

    [TestMethod]
    public async Task Rtl_TheButtonsAndTheArrowKeysGoTheOtherWay()
    {
        await OpenAsync("/e2e/carousel");

        var rtl = Page.Locator("#rtl");
        await Expect(rtl).ToHaveAttributeAsync("dir", "rtl");
        await Expect(rtl).ToHaveClassAsync(HasClass("bit-rtl"));

        // The button at the left edge goes forwards in a right-to-left carousel.
        var leftEdge = Page.Locator("#rtl .bit-csl-rbt");
        await Expect(leftEdge).ToHaveAttributeAsync("aria-label", "Next slide");
        await Expect(Page.Locator("#rtl .bit-csl-lbt")).ToBeHiddenAsync();
        await leftEdge.ClickAsync();
        await ExpectCurrentAsync("rtl", "S2");
        await ExpectSlideInPlaceAsync("rtl", "S2");

        await WaitForKeysAsync("rtl");
        await rtl.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectCurrentAsync("rtl", "S3");
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectCurrentAsync("rtl", "S2");

        // Inherited from an ancestor, read off the page.
        await Expect(Page.Locator("#rtl-inherited .bit-csl-rbt")).ToHaveAttributeAsync("aria-label", "Next slide");
        await Page.Locator("#rtl-inherited .bit-csl-rbt").ClickAsync();
        await ExpectCurrentAsync("rtl-inherited", "S2");
    }

    [TestMethod]
    public async Task Lifecycle_SlidesAddedAndRemoved_AndAPlayingCarouselUnmounted()
    {
        await OpenAsync("/e2e/carousel");

        await Expect(Page.Locator("#dynamic .bit-csl-dot")).ToHaveCountAsync(2);
        await Page.ClickAsync("#btn-add-slide");
        await Expect(Page.Locator("#dynamic .bit-csl-dot")).ToHaveCountAsync(3);
        await Expect(Page.Locator("#dynamic .bit-crsi").Nth(2)).ToHaveAttributeAsync("aria-label", "3 of 3");
        await Page.ClickAsync("#btn-remove-slide");
        await Page.ClickAsync("#btn-remove-slide");
        await Expect(Page.Locator("#dynamic .bit-csl-dot")).ToHaveCountAsync(0);

        // The pointer is moved off the page after each click, so the layout shifting under it cannot hover the playing
        // carousel and pause it.
        await Page.Mouse.MoveAsync(0, 0);
        await ExpectCurrentAsync("mount-carousel", "S2");
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mount-carousel")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-mount");
        await Page.Mouse.MoveAsync(0, 0);
        await ExpectCurrentAsync("mount-carousel", "S2");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // The navigation keys are wired up by the carousel's script once its first render has been laid out.
    private Task WaitForKeysAsync(string id)
    {
        return Page.WaitForFunctionAsync("id => document.getElementById(id).__bitNavigationKeysRegistered === true", id);
    }

    private Task ExpectCurrentAsync(string id, string text)
    {
        return Expect(Page.Locator($"#{id} .bit-crsi-cur")).ToHaveTextAsync(new[] { text });
    }

    private async Task<string> CurrentTextAsync(string id)
    {
        return (await Page.Locator($"#{id} .bit-crsi-cur").First.InnerTextAsync()).Trim();
    }

    // Waits until the slide has come to rest at the start of the slides container.
    private Task ExpectSlideInPlaceAsync(string id, string text)
    {
        return Page.WaitForFunctionAsync(@"([id, text]) => {
            const container = document.querySelector(`#${id} .bit-csl-cnt`);
            const slide = [...container.querySelectorAll('.bit-crsi')].find(s => s.textContent.trim() === text);
            if (!slide) return false;
            const c = container.getBoundingClientRect(), s = slide.getBoundingClientRect();
            return Math.abs(s.left - c.left) <= 1 && Math.abs(s.top - c.top) <= 1;
        }", new[] { id, text });
    }

    // Reads the autoplay state through a script click, which neither moves the pointer nor the focus.
    private async Task ExpectAutoPlayStateAsync(string name, string state)
    {
        for (var attempt = 0; ; attempt++)
        {
            await Page.EvaluateAsync("() => document.getElementById('btn-read-autoplay').click()");
            try
            {
                var escaped = System.Text.RegularExpressions.Regex.Escape(state);
                var pattern = state.StartsWith("page=", StringComparison.Ordinal)
                    ? name + "\\[" + escaped + "\\]"
                    : name + "\\[page=\\d+;" + escaped + "\\]";
                await Expect(Page.Locator("#autoplay-state")).ToHaveTextAsync(new System.Text.RegularExpressions.Regex(pattern), new() { Timeout = 500 });
                return;
            }
            catch (PlaywrightException) when (attempt < 40) { }
        }
    }

    private Task SetPageHiddenAsync(bool hidden)
    {
        return Page.EvaluateAsync(@"hidden => {
            Object.defineProperty(document, 'hidden', { configurable: true, get: () => hidden });
            Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => hidden ? 'hidden' : 'visible' });
            document.dispatchEvent(new Event('visibilitychange'));
        }", hidden);
    }

    private async Task DragAsync(string selector, double dx, double dy = 0)
    {
        var target = Page.Locator(selector);
        await target.ScrollIntoViewIfNeededAsync();
        var box = await target.BoundingBoxAsync();
        var x = box!.X + box.Width / 2;
        var y = box.Y + box.Height / 2;
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)(x + dx / 2), (float)(y + dy / 2), new() { Steps = 5 });
        await Page.Mouse.MoveAsync((float)(x + dx), (float)(y + dy), new() { Steps = 5 });
        await Page.Mouse.UpAsync();
    }

    private Task<string> ResolveAsync(string value)
    {
        return Page.EvaluateAsync<string>(@"value => {
            const probe = document.createElement('div');
            probe.style.setProperty('background-color', value);
            document.body.appendChild(probe);
            const resolved = getComputedStyle(probe).getPropertyValue('background-color');
            probe.remove();
            return resolved;
        }", value);
    }

    private async Task<Box> BoxAsync(string selector, string? inner = null)
    {
        var locator = Page.Locator(selector);
        if (inner is not null) locator = locator.Locator(inner);
        var box = await locator.First.BoundingBoxAsync();
        Assert.IsNotNull(box, $"{selector} {inner} has no box");
        return new(box.X, box.Y, box.Width, box.Height);
    }

    private static void AssertClose(double expected, double actual, string what)
    {
        Assert.IsTrue(Math.Abs(expected - actual) <= Tolerance, $"{what}: expected {expected}, got {actual}");
    }

    private sealed record Box(double X, double Y, double W, double H)
    {
        public double Right => X + W;
        public double Bottom => Y + H;
    }
}

/// <summary>
/// BitCarousel on a phone: a touch swipe in either direction pages the carousel, and a swipe across its axis is left to
/// the page. The touch points are dispatched through the DevTools protocol, so this runs on Chromium only.
/// </summary>
[TestClass]
public class BitCarouselTouchE2ETests : E2ETestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 430, Height = 900 },
        HasTouch = true,
        IsMobile = true,
    };

    [TestMethod]
    public async Task Swipe_ATouchSwipePagesTheCarousel()
    {
        if (IsChromium is false) Assert.Inconclusive("The touch points are dispatched through the Chromium DevTools protocol.");

        await OpenAsync("/e2e/carousel/touch");

        // A tap on a dot and on a button works with a finger. They come first: Chromium turns a tap that follows a
        // synthesized swipe too closely into the end of that gesture rather than a click.
        await Page.Locator("#touch .bit-csl-dot").Nth(3).TapAsync();
        await Expect(Page.Locator("#touch .bit-crsi-cur")).ToHaveTextAsync(new[] { "S4" });
        await Page.Locator("#touch .bit-csl-rbt").TapAsync();
        await Expect(Page.Locator("#touch .bit-crsi-cur")).ToHaveTextAsync(new[] { "S3" });

        await SwipeAsync("#touch .bit-csl-cnt", -150, 0);
        await Expect(Page.Locator("#touch .bit-crsi-cur")).ToHaveTextAsync(new[] { "S4" });
        await Expect(Page.Locator("#touch .bit-csl-dot").Nth(3)).ToHaveAttributeAsync("aria-current", "true");
        await SwipeAsync("#touch .bit-csl-cnt", 150, 0);
        await Expect(Page.Locator("#touch .bit-crsi-cur")).ToHaveTextAsync(new[] { "S3" });
        await SwipeAsync("#touch .bit-csl-cnt", 150, 0);
        await Expect(Page.Locator("#touch .bit-crsi-cur")).ToHaveTextAsync(new[] { "S2" });

        // A vertical swipe is the page's to scroll, and leaves the carousel where it is.
        await SwipeAsync("#touch .bit-csl-cnt", 0, -120);
        await Page.WaitForFunctionAsync("() => window.scrollY > 0");
        await Expect(Page.Locator("#touch .bit-crsi-cur")).ToHaveTextAsync(new[] { "S2" });
    }

    private async Task SwipeAsync(string selector, double dx, double dy)
    {
        var target = Page.Locator(selector);
        await target.ScrollIntoViewIfNeededAsync();
        var box = await target.BoundingBoxAsync();
        var x = box!.X + box.Width / 2;
        var y = box.Y + box.Height / 2;

        var session = await Page.Context.NewCDPSessionAsync(Page);
        await session.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = "touchStart",
            ["touchPoints"] = new[] { new Dictionary<string, object> { ["x"] = x, ["y"] = y } },
        });
        for (var step = 1; step <= 8; step++)
        {
            await session.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
            {
                ["type"] = "touchMove",
                ["touchPoints"] = new[] { new Dictionary<string, object> { ["x"] = x + dx * step / 8, ["y"] = y + dy * step / 8 } },
            });
        }
        await session.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = "touchEnd",
            ["touchPoints"] = Array.Empty<object>(),
        });
        await session.DetachAsync();
    }
}
