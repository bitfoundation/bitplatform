namespace Bit.BlazorUI.Tests.E2E.Lists;

/// <summary>
/// BitSwiper in a real browser, read off the real scroll position of its natively scrolling row: the buttons and their
/// visibility at the ends, the keyboard (arrows, Home, End, PageUp, PageDown, and the keys left to an input inside an
/// item), OnChange / OnReachStart / OnReachEnd, ScrollItemsCount and Rewind, HideNextPrev, VisibleItemsCount / Gap /
/// Peek and the per breakpoint counts measured off the swiper, every SnapAlign, Vertical (and its height variable),
/// ShowDots / DotTemplate and the dot labels, AutoPlay with ShowPlayPause / PauseOnHover / PauseOnFocus / the hidden page
/// / StopOnInteraction / AutoPlayReverse / StopOnLastSlide / the rewind at the end / reduced motion and ForceAnimation,
/// the mouse wheel, a mouse drag, DragThreshold, NoDrag, ShowScrollbar, NoKeyboard, AnimationDuration, Disabled (and the
/// items staying live), DefaultItem and the public methods and state, loading more items OnReachEnd, the accessibility
/// attributes and the announcement, the BitParams cascade, every Color, Accent and Size, external icons and icon names,
/// Style / Class / Styles / Classes, the --bit-Swiper-* variables, RTL (explicit and inherited), items added and removed
/// at runtime, prerendering and unmounting a playing swiper. The touch swipe is in <see cref="BitSwiperTouchE2ETests"/>.
/// </summary>
[TestClass]
public class BitSwiperE2ETests : E2ETestBase
{
    private const double Tolerance = 1.5;

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
    public async Task Prerender_TheItemsAreInTheServerRenderedHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/swiper");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"basic\"");
        StringAssert.Contains(html, "bit-swpi");
        StringAssert.Contains(html, ">I12</div>");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_TheButtonsScrollAnItemAtATime_AndHideAtTheEnds()
    {
        await OpenAsync("/e2e/swiper");

        var prev = Page.Locator("#basic .bit-swp-lbt");
        var next = Page.Locator("#basic .bit-swp-rbt");
        await Expect(next).ToBeVisibleAsync();
        await Expect(prev).ToBeHiddenAsync();
        await Expect(next).ToHaveAttributeAsync("aria-label", "Next slide");
        await Expect(prev).ToHaveAttributeAsync("aria-label", "Previous slide");
        await Expect(Page.Locator("#basic .bit-swpi-cur")).ToHaveTextAsync(new[] { "I1" });

        await next.ClickAsync();
        await ExpectScrollAsync("basic", 100);
        await Expect(Page.Locator("#basic .bit-swpi-cur")).ToHaveTextAsync(new[] { "I2" });
        await Expect(Page.Locator("#basic-log")).ToHaveTextAsync("c1");
        await Expect(prev).ToBeVisibleAsync();

        await prev.ClickAsync();
        await ExpectScrollAsync("basic", 0);
        await Expect(Page.Locator("#basic .bit-swpi-cur")).ToHaveTextAsync(new[] { "I1" });
        await Expect(prev).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Keyboard_ArrowsHomeEndAndPages_AndTheReachEvents()
    {
        await OpenAsync("/e2e/swiper");

        var root = Page.Locator("#basic");
        await Expect(root).ToHaveAttributeAsync("tabindex", "0");
        await root.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectScrollAsync("basic", 100);
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectScrollAsync("basic", 0);

        // A page is a screenful: four 100px items.
        await Page.Keyboard.PressAsync("PageDown");
        await ExpectScrollAsync("basic", 400);
        await Page.Keyboard.PressAsync("PageUp");
        await ExpectScrollAsync("basic", 0);

        await Page.Keyboard.PressAsync("End");
        await ExpectScrollAsync("basic", 800);
        await Expect(Page.Locator("#basic .bit-swp-rbt")).ToBeHiddenAsync();
        await Expect(Page.Locator("#basic-log")).ToContainTextAsync("end");
        await Expect(Page.Locator("#basic .bit-swpi-cur")).ToHaveTextAsync(new[] { "I9" });

        await Page.Keyboard.PressAsync("Home");
        await ExpectScrollAsync("basic", 0);
        await Expect(Page.Locator("#basic-log")).ToContainTextAsync("start");
        await Expect(Page.Locator("#basic .bit-swpi-cur")).ToHaveTextAsync(new[] { "I1" });
        await Expect(root).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_TheArrowsOfAnInputInsideAnItem_AreTheInputs()
    {
        await OpenAsync("/e2e/swiper");

        var input = Page.Locator("#swp-input");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowRight");
        Assert.AreEqual(2, await input.EvaluateAsync<int>("el => el.selectionStart"), "the caret moved");
        await Page.Keyboard.PressAsync("End");
        Assert.AreEqual(4, await input.EvaluateAsync<int>("el => el.selectionStart"), "End moved the caret");
        await ExpectScrollAsync("with-input", 0);
    }

    [TestMethod]
    public async Task Navigation_ScrollItemsCountAndRewind_HideNextPrev_AndAFittingSwiper()
    {
        await OpenAsync("/e2e/swiper");

        var next = Page.Locator("#step .bit-swp-rbt");
        var prev = Page.Locator("#step .bit-swp-lbt");
        await next.ClickAsync();
        await ExpectScrollAsync("step", 200);
        await next.ClickAsync();
        await ExpectScrollAsync("step", 400);

        // At the end, Rewind keeps the button and wraps around.
        await Expect(next).ToBeVisibleAsync();
        await next.ClickAsync();
        await ExpectScrollAsync("step", 0);
        await Expect(prev).ToBeVisibleAsync();
        await prev.ClickAsync();
        await ExpectScrollAsync("step", 400);

        await Expect(Page.Locator("#no-buttons .bit-swp-rbt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#no-buttons .bit-swp-lbt")).ToHaveCountAsync(0);

        // Everything fits: nothing to scroll, no button.
        await Expect(Page.Locator("#fits .bit-swp-rbt")).ToBeHiddenAsync();
        await Expect(Page.Locator("#fits .bit-swp-lbt")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task MultipleItems_VisibleItemsCountGapAndPeek_SizeTheItems()
    {
        await OpenAsync("/e2e/swiper");

        var first = await BoxAsync("#multiple .bit-swpi >> nth=0");
        var second = await BoxAsync("#multiple .bit-swpi >> nth=1");
        AssertClose((400 - 2 * 10) / 3.0, first.W, "three items and two gaps fill the swiper");
        AssertClose(10, second.X - first.Right, "the gap");

        await Page.Locator("#multiple .bit-swp-rbt").ClickAsync();
        await ExpectScrollAsync("multiple", first.W + 10);

        // Peek keeps room at both ends for the neighbours.
        var container = Page.Locator("#peek .bit-swp-cnt");
        await Expect(container).ToHaveCSSAsync("padding-left", "40px");
        await Expect(container).ToHaveCSSAsync("padding-right", "40px");
        var peekFirst = await BoxAsync("#peek .bit-swpi >> nth=0");
        AssertClose((await BoxAsync("#peek .bit-swp-cnt")).X + 40, peekFirst.X, "the first item after the peek");
        AssertClose((400 - 80) / 2.0, peekFirst.W, "two items in what the peeks leave");
    }

    [TestMethod]
    public async Task Responsive_TheCountsFollowTheWidthOfTheSwiperItself()
    {
        await OpenAsync("/e2e/swiper");

        var expected = new Dictionary<int, int> { [500] = 1, [700] = 2, [1000] = 3, [1300] = 4, [2000] = 5, [2600] = 6 };
        foreach (var (width, count) in expected)
        {
            await WaitForItemWidthAsync($"responsive-{width}", width / (double)count);
        }

        // Only the breakpoints given apply; below them the plain count is the base.
        await WaitForItemWidthAsync("responsive-base", 700 / 3.0);
    }

    [TestMethod]
    [DataRow("None", "", 120)]
    [DataRow("Start", "bit-swp-sns", 100)]
    [DataRow("Center", "bit-swp-snc", 150)]
    [DataRow("End", "bit-swp-sne", 100)]
    public async Task SnapAlign_EveryValue_SettlesTheSwiperOnAnItem(string align, string cls, double settled)
    {
        await OpenAsync("/e2e/swiper");

        var id = $"snap-{align}";
        var root = Page.Locator($"#{id}");
        if (cls.Length > 0)
        {
            await Expect(root).ToHaveClassAsync(HasClass("bit-swp-snp"));
            await Expect(root).ToHaveClassAsync(HasClass(cls));
            await Expect(Page.Locator($"#{id} .bit-swp-cnt")).ToHaveCSSAsync("scroll-snap-type", "x mandatory");
        }
        else
        {
            await Expect(root).Not.ToHaveClassAsync(HasClass("bit-swp-snp"));
        }

        await Page.Locator($"#{id} .bit-swp-cnt").EvaluateAsync("el => el.scrollLeft = 120");
        await ExpectScrollAsync(id, settled);
    }

    [TestMethod]
    public async Task Vertical_ScrollsUpAndDown_WithTheVerticalKeys()
    {
        await OpenAsync("/e2e/swiper");

        var root = Page.Locator("#vertical");
        await Expect(root).ToHaveClassAsync(HasClass("bit-swp-vrt"));
        await Expect(root).ToHaveCSSAsync("height", "200px");
        var first = await BoxAsync("#vertical .bit-swpi >> nth=0");
        var second = await BoxAsync("#vertical .bit-swpi >> nth=1");
        AssertClose(first.Bottom, second.Y, "stacked");
        AssertClose((await BoxAsync("#vertical .bit-swp-vwp")).Bottom, (await BoxAsync("#vertical .bit-swp-rbt")).Bottom, "the next button at the bottom");
        await Expect(Page.Locator("#vertical .bit-swp-rbt i")).ToHaveClassAsync(HasClass("bit-icon--ChevronDown"));

        await root.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectScrollAsync("vertical", 80);
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectScrollAsync("vertical", 0);

        await Expect(Page.Locator("#vertical-default")).ToHaveCSSAsync("height", "200px");
        await Expect(Page.Locator("#vertical-var")).ToHaveCSSAsync("height", "150px");
    }

    [TestMethod]
    public async Task Dots_OnePerScreenful_AClickGoesToItsPage()
    {
        await OpenAsync("/e2e/swiper");

        var dots = Page.Locator("#dots .bit-swp-dot");
        await Expect(dots).ToHaveCountAsync(4);
        await Expect(dots.Nth(0)).ToHaveAttributeAsync("aria-current", "true");
        await Expect(dots.Nth(1)).ToHaveAttributeAsync("aria-label", "Page 2");
        await Expect(Page.Locator("#dots .bit-swp-dts")).ToHaveAttributeAsync("aria-label", "Pick a page");

        await dots.Nth(2).ClickAsync();
        await ExpectScrollAsync("dots", 800);
        await Expect(dots.Nth(2)).ToHaveAttributeAsync("aria-current", "true");
        await Expect(dots.Nth(2)).ToHaveClassAsync(HasClass("bit-swp-cud"));
        await Expect(Page.Locator("#dots .bit-swpi-cur")).ToHaveTextAsync(new[] { "I9" });

        await Expect(Page.Locator("#dot-template .bit-swp-dot")).ToHaveTextAsync(new[] { "1", "2" });
        await Expect(Page.Locator("#dot-template .bit-swp-dot").First).ToHaveClassAsync(HasClass("bit-swp-dtt"));

        // Hidden while everything fits.
        await Expect(Page.Locator("#dots-fit .bit-swp-dot")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task AutoPlay_ScrollsOnItsOwn_AndThePlayPauseButtonStopsIt()
    {
        await OpenAsync("/e2e/swiper");

        await Expect(Page.Locator("#autoplay .bit-swpi-cur")).ToHaveTextAsync(new[] { "I2" });
        await Expect(Page.Locator("#autoplay .bit-swpi-cur")).ToHaveTextAsync(new[] { "I3" });
        var button = Page.Locator("#autoplay .bit-swp-ppb");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Stop automatic slide show");
        await Expect(Page.Locator("#autoplay .bit-swp-sro")).ToHaveAttributeAsync("aria-live", "off");

        await button.ClickAsync();
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(button).ToHaveAttributeAsync("aria-label", "Start automatic slide show");
        await Expect(button.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Play"));
        await Expect(Page.Locator("#autoplay .bit-swp-sro")).ToHaveAttributeAsync("aria-live", "polite");
        await ExpectAutoPlayStateAsync("autoplay", "playing=False;paused=True");

        await button.ClickAsync();
        await ExpectAutoPlayStateAsync("autoplay", "playing=True;paused=False");
    }

    [TestMethod]
    public async Task AutoPlay_PausesOnHoverOnFocusAndOnAHiddenPage_UnlessTurnedOff()
    {
        await OpenAsync("/e2e/swiper");

        await Page.Locator("#autoplay .bit-swp-cnt").HoverAsync();
        await ExpectAutoPlayStateAsync("autoplay", "playing=False;paused=False");
        await Page.Mouse.MoveAsync(0, 0);
        await ExpectAutoPlayStateAsync("autoplay", "playing=True;paused=False");

        await Page.Locator("#autoplay").FocusAsync();
        await ExpectAutoPlayStateAsync("autoplay", "playing=False;paused=False");
        await Page.Locator("#btn-read-autoplay").FocusAsync();
        await ExpectAutoPlayStateAsync("autoplay", "playing=True;paused=False");

        await Page.Locator("#no-pause .bit-swp-cnt").HoverAsync();
        await Page.Locator("#no-pause").FocusAsync();
        await ExpectAutoPlayStateAsync("no-pause", "playing=True;paused=False");

        await SetPageHiddenAsync(true);
        await ExpectAutoPlayStateAsync("no-pause", "playing=False;paused=False");
        await SetPageHiddenAsync(false);
        await ExpectAutoPlayStateAsync("no-pause", "playing=True;paused=False");
    }

    [TestMethod]
    public async Task AutoPlay_StopOnInteractionReverseStopOnLastSlideAndTheRewind()
    {
        await OpenAsync("/e2e/swiper");

        await ExpectAutoPlayStateAsync("stop", "playing=True;paused=False");
        await Page.Locator("#stop-on-interaction .bit-swp-rbt").ClickAsync();
        await Page.Mouse.MoveAsync(0, 0);
        await ExpectAutoPlayStateAsync("stop", "playing=False;paused=True");

        // Backwards from the sixth item.
        await Expect(Page.Locator("#reverse .bit-swpi-cur")).ToHaveTextAsync(new[] { "I5" });
        await Expect(Page.Locator("#reverse .bit-swpi-cur")).ToHaveTextAsync(new[] { "I4" });

        await ExpectAutoPlayStateAsync("last", "index=2;playing=False;paused=True");

        // Without StopOnLastSlide it rewinds to the start at the end.
        await Expect(Page.Locator("#rewinding .bit-swpi-cur")).ToHaveTextAsync(new[] { "I2" });
        await Expect(Page.Locator("#rewinding .bit-swpi-cur")).ToHaveTextAsync(new[] { "I1" });
    }

    [TestMethod]
    public async Task AutoPlay_StartsPausedUnderReducedMotion_UnlessForceAnimation()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync("/e2e/swiper");

        await ExpectAutoPlayStateAsync("autoplay", "playing=False;paused=True");
        await ExpectAutoPlayStateAsync("forced", "playing=True;paused=False");
        await Expect(Page.Locator("#forced")).ToHaveClassAsync(HasClass("bit-fam"));

        // A move of its own is instant under reduced motion.
        await Page.Locator("#slow .bit-swp-rbt").ClickAsync();
        await ExpectScrollAsync("slow", 100, 200);
    }

    [TestMethod]
    public async Task Wheel_TheMouseWheelScrollsTheSwiper_AndNoDragTurnsTheMouseDragOff()
    {
        await OpenAsync("/e2e/swiper");

        var container = Page.Locator("#wheel .bit-swp-cnt");
        await container.HoverAsync();
        await Page.Mouse.WheelAsync(0, 100);
        await Page.WaitForFunctionAsync("() => document.querySelector('#wheel .bit-swp-cnt').scrollLeft >= 99");

        await Expect(Page.Locator("#wheel")).ToHaveClassAsync(HasClass("bit-swp-ndr"));
        var before = await container.EvaluateAsync<double>("el => el.scrollLeft");
        await DragAsync("#wheel .bit-swp-cnt", -150);
        await NextFramesAsync();
        AssertClose(before, await container.EvaluateAsync<double>("el => el.scrollLeft"), "a drag does not scroll it");
    }

    [TestMethod]
    public async Task Drag_AMouseDragScrollsTheRow_PastTheDragThreshold()
    {
        await OpenAsync("/e2e/swiper");

        await DragAsync("#drag .bit-swp-cnt", -150);
        await Page.WaitForFunctionAsync("() => document.querySelector('#drag .bit-swp-cnt').scrollLeft >= 100");
        await Expect(Page.Locator("#drag .bit-swp-cnt")).Not.ToHaveClassAsync(HasClass("bit-swp-drg"));

        // DragThreshold 60: a 30px press is a click, a 150px one drags.
        await DragAsync("#threshold .bit-swp-cnt", -30);
        await NextFramesAsync();
        Assert.AreEqual(0, await Page.Locator("#threshold .bit-swp-cnt").EvaluateAsync<double>("el => el.scrollLeft"), "below the threshold");
        await DragAsync("#threshold .bit-swp-cnt", -150);
        await Page.WaitForFunctionAsync("() => document.querySelector('#threshold .bit-swp-cnt').scrollLeft > 0");
    }

    [TestMethod]
    public async Task ShowScrollbarNoKeyboardAndAnimationDuration()
    {
        await OpenAsync("/e2e/swiper");

        await Expect(Page.Locator("#scrollbar")).ToHaveClassAsync(HasClass("bit-swp-scb"));
        await Expect(Page.Locator("#scrollbar .bit-swp-cnt")).ToHaveCSSAsync("scrollbar-width", "thin");
        await Expect(Page.Locator("#basic .bit-swp-cnt")).ToHaveCSSAsync("scrollbar-width", "none");

        Assert.IsNull(await Page.Locator("#no-keyboard").GetAttributeAsync("tabindex"));
        await Page.Locator("#no-keyboard .bit-swp-rbt").FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await NextFramesAsync();
        await ExpectScrollAsync("no-keyboard", 0);

        // A slow move is still on its way a few frames in; an instant one is already there.
        await Page.Locator("#slow .bit-swp-rbt").ClickAsync();
        await Page.WaitForFunctionAsync("() => { const s = document.querySelector('#slow .bit-swp-cnt').scrollLeft; return s > 0 && s < 99; }");
        await ExpectScrollAsync("slow", 100);

        await Page.Locator("#instant .bit-swp-rbt").ClickAsync();
        await ExpectScrollAsync("instant", 100, 300);
    }

    [TestMethod]
    public async Task Disabled_TheControlsAreDisabled_AndTheItemsStayLive()
    {
        await OpenAsync("/e2e/swiper");

        var root = Page.Locator("#disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-swp-ndr"));
        Assert.IsNull(await root.GetAttributeAsync("tabindex"));
        foreach (var selector in new[] { "#disabled .bit-swp-rbt", "#disabled .bit-swp-ppb", "#disabled .bit-swp-dot >> nth=1" })
        {
            await Expect(Page.Locator(selector)).ToBeDisabledAsync();
            await Expect(Page.Locator(selector)).ToHaveAttributeAsync("aria-disabled", "true");
        }

        await DragAsync("#disabled .bit-swp-cnt", -150);
        await Page.Locator("#disabled .bit-swp-rbt").DispatchEventAsync("click");
        await NextFramesAsync();
        await ExpectScrollAsync("disabled", 0);

        // The swiper hosts content: a click on an item still reaches the page's handler.
        await Page.Locator("#disabled .bit-swpi >> nth=0").ClickAsync();
        await Expect(Page.Locator("#root-clicks")).Not.ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Page.Locator("#disabled .bit-swp-rbt").ClickAsync();
        await ExpectScrollAsync("disabled", 200);
    }

    [TestMethod]
    public async Task PublicMembers_DefaultItemNavigationAndState()
    {
        await OpenAsync("/e2e/swiper");

        await ExpectScrollAsync("api", 400);
        await Page.ClickAsync("#api-read");
        await Expect(Page.Locator("#api-state")).ToHaveTextAsync("index=4;page=1;pages=4;items=16;start=False;end=False");

        await Page.ClickAsync("#api-next");
        await Expect(Page.Locator("#api-change")).ToHaveTextAsync("5");
        await Page.ClickAsync("#api-prev");
        await Expect(Page.Locator("#api-change")).ToHaveTextAsync("4");
        await Page.ClickAsync("#api-goto");
        await Expect(Page.Locator("#api-change")).ToHaveTextAsync("9");
        await ExpectScrollAsync("api", 900);
        await Page.ClickAsync("#api-goto-page");
        await Expect(Page.Locator("#api-change")).ToHaveTextAsync("4");
        await Page.ClickAsync("#api-end");
        await Expect(Page.Locator("#api-change")).ToHaveTextAsync("12");
        await Page.ClickAsync("#api-refresh");
        await Page.ClickAsync("#api-read");
        await Expect(Page.Locator("#api-state")).ToHaveTextAsync("index=12;page=3;pages=4;items=16;start=False;end=True");
        await Page.ClickAsync("#api-start");
        await Expect(Page.Locator("#api-change")).ToHaveTextAsync("0");
        await Page.ClickAsync("#api-read");
        await Expect(Page.Locator("#api-state")).ToHaveTextAsync("index=0;page=0;pages=4;items=16;start=True;end=False");

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
    public async Task OnReachEnd_LoadsMoreItems_EachTimeTheEndIsReached()
    {
        await OpenAsync("/e2e/swiper");

        // Everything fitting in counts as being at the end, once per set of items.
        await Expect(Page.Locator("#loaded-count")).ToHaveTextAsync("8");
        await Expect(Page.Locator("#load-more .bit-swpi")).ToHaveCountAsync(8);

        await Page.Locator("#load-more").FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Page.Locator("#loaded-count")).ToHaveTextAsync("12");
        await Expect(Page.Locator("#load-more .bit-swpi")).ToHaveCountAsync(12);
        await Page.Keyboard.PressAsync("End");
        await Expect(Page.Locator("#loaded-count")).ToHaveTextAsync("16");
    }

    [TestMethod]
    public async Task Accessibility_TheCarouselPattern_TheLabelsAndTheAnnouncement()
    {
        await OpenAsync("/e2e/swiper");

        var basic = Page.Locator("#basic");
        await Expect(basic).ToHaveAttributeAsync("role", "region");
        await Expect(basic).ToHaveAttributeAsync("aria-roledescription", "carousel");
        await Expect(Page.GetByRole(AriaRole.Region, new() { Name = "Basic swiper" })).ToHaveCountAsync(1);
        var items = Page.Locator("#basic .bit-swpi");
        await Expect(items.Nth(0)).ToHaveAttributeAsync("role", "group");
        await Expect(items.Nth(0)).ToHaveAttributeAsync("aria-roledescription", "slide");
        await Expect(items.Nth(0)).ToHaveAttributeAsync("aria-label", "1 of 12");
        var containerId = await Page.Locator("#basic .bit-swp-cnt").GetAttributeAsync("id");
        await Expect(Page.Locator("#basic .bit-swp-rbt")).ToHaveAttributeAsync("aria-controls", containerId!);

        // The item moved to is announced.
        await Expect(Page.Locator("#basic .bit-swp-sro")).ToHaveAttributeAsync("aria-live", "polite");
        await Page.Locator("#basic .bit-swp-rbt").ClickAsync();
        await Expect(Page.Locator("#basic .bit-swp-sro")).ToHaveTextAsync("2 of 12");

        var a11y = Page.Locator("#a11y");
        await Expect(a11y.Locator(".bit-swpi").Nth(1)).ToHaveAttributeAsync("aria-label", "Item 2 of 7");
        await Expect(a11y.Locator(".bit-swpi").Nth(3)).ToHaveAttributeAsync("aria-label", "The named item");
        await Expect(a11y.Locator(".bit-swp-rbt")).ToHaveAttributeAsync("aria-label", "Forwards");
        await Expect(a11y.Locator(".bit-swp-lbt")).ToHaveAttributeAsync("aria-label", "Backwards");
        await Expect(a11y.Locator(".bit-swp-ppb")).ToHaveAttributeAsync("aria-label", "Pause it");

        // The play/pause button is the first stop inside the swiper.
        await a11y.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(a11y.Locator(".bit-swp-ppb")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(a11y.Locator(".bit-swp-ppb")).ToHaveAttributeAsync("aria-label", "Play it");
    }

    [TestMethod]
    public async Task CascadingParameters_BitSwiperParams_AreDefaultsAndStopAtTheirScope()
    {
        await OpenAsync("/e2e/swiper");

        await Expect(Page.Locator("#casc .bit-swp-dot")).ToHaveCountAsync(4);
        await Expect(Page.Locator("#casc .bit-swp-rbt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#casc")).ToHaveClassAsync(HasClass("bit-swp-lg"));
        await WaitForItemWidthAsync("casc", 200);

        await Expect(Page.Locator("#casc-own .bit-swp-dot")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#casc-own .bit-swp-rbt")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#casc-out .bit-swp-dot")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#casc-out .bit-swp-rbt")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#casc-out")).Not.ToHaveClassAsync(HasClass("bit-swp-lg"));
    }

    [TestMethod]
    public async Task Color_EveryColor_PaintsTheCurrentDotAndTheButtons()
    {
        await OpenAsync("/e2e/swiper");

        foreach (var (name, suffix, main) in Colors)
        {
            var color = await ResolveAsync($"var({main})");
            await Expect(Page.Locator($"#color-{name}")).ToHaveClassAsync(HasClass($"bit-swp-{suffix}"));
            await Expect(Page.Locator($"#color-{name} .bit-swp-cud")).ToHaveCSSAsync("background-color", color);
            await Expect(Page.Locator($"#color-{name} .bit-swp-rbt")).ToHaveCSSAsync("color", color);
        }
    }

    [TestMethod]
    [DataRow("Primary", "bit-swp-apri", "var(--bit-clr-pri)")]
    [DataRow("Secondary", "bit-swp-asec", "var(--bit-clr-sec)")]
    [DataRow("Tertiary", "bit-swp-ater", "var(--bit-clr-ter)")]
    [DataRow("Transparent", "bit-swp-atra", "transparent")]
    public async Task Accent_EveryValue_PaintsTheCurrentDot(string accent, string cls, string value)
    {
        await OpenAsync("/e2e/swiper");

        await Expect(Page.Locator($"#accent-{accent}")).ToHaveClassAsync(HasClass(cls));
        await Expect(Page.Locator($"#accent-{accent} .bit-swp-cud")).ToHaveCSSAsync("background-color", await ResolveAsync(value));
    }

    [TestMethod]
    [DataRow("size-sm", "bit-swp-sm", "8px", "16px")]
    [DataRow("size-md", "bit-swp-md", "10px", "24px")]
    [DataRow("size-lg", "bit-swp-lg", "14px", "32px")]
    public async Task Size_EveryValue_SizesTheDotsAndTheButtons(string id, string cls, string dot, string button)
    {
        await OpenAsync("/e2e/swiper");

        await Expect(Page.Locator($"#{id}")).ToHaveClassAsync(HasClass(cls));
        await Expect(Page.Locator($"#{id} .bit-swp-dot").Nth(1)).ToHaveCSSAsync("width", dot);
        await Expect(Page.Locator($"#{id} .bit-swp-dot").Nth(1)).ToHaveCSSAsync("height", dot);
        await Expect(Page.Locator($"#{id} .bit-swp-rbt")).ToHaveCSSAsync("font-size", button);
    }

    [TestMethod]
    public async Task Icons_ExternalIconsAndIconNames()
    {
        await OpenAsync("/e2e/swiper");

        await Expect(Page.Locator("#icons .bit-swp-rbt i")).ToHaveClassAsync(HasClass("swp-ext-next"));
        await Expect(Page.Locator("#icons .bit-swp-lbt i")).ToHaveClassAsync(HasClass("swp-ext-prev"));
        await Expect(Page.Locator("#icons .bit-swp-ppb i")).ToHaveClassAsync(HasClass("swp-ext-pause"));
        await Page.Locator("#icons .bit-swp-ppb").ClickAsync();
        await Expect(Page.Locator("#icons .bit-swp-ppb i")).ToHaveClassAsync(HasClass("swp-ext-play"));

        await Expect(Page.Locator("#icon-names .bit-swp-rbt i")).ToHaveClassAsync(HasClass("bit-icon--Forward"));
        await Expect(Page.Locator("#icon-names .bit-swp-lbt i")).ToHaveClassAsync(HasClass("bit-icon--Back"));
        await Expect(Page.Locator("#icon-names .bit-swp-ppb i")).ToHaveClassAsync(HasClass("bit-icon--CirclePause"));
        await Page.Locator("#icon-names .bit-swp-ppb").ClickAsync();
        await Expect(Page.Locator("#icon-names .bit-swp-ppb i")).ToHaveClassAsync(HasClass("bit-icon--PlaySolid"));

        await Expect(Page.Locator("#basic .bit-swp-rbt i")).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
    }

    [TestMethod]
    public async Task StyleClassStylesClasses_ReachTheRootAndEveryPart()
    {
        await OpenAsync("/e2e/swiper");

        await Expect(Page.Locator("#styled")).ToHaveClassAsync(HasClass("swp-root-class"));
        await Expect(Page.Locator("#styled")).ToHaveClassAsync(HasClass("swp-extra-root"));
        await Expect(Page.Locator("#styled")).ToHaveCSSAsync("padding-top", "1px");
        await Expect(Page.Locator("#styled")).ToHaveCSSAsync("margin-top", "1px");

        var parts = new (string Selector, string Class, string Property, string Value)[]
        {
            ("#styled .bit-swp-cnt", "swp-cnt-class", "margin-top", "2px"),
            ("#styled .bit-swpi >> nth=0", "swp-item-class", "padding-top", "3px"),
            ("#styled .bit-swpi-cur", "swp-cur-class", "padding-bottom", "4px"),
            ("#styled .bit-swp-rbt", "swp-btns-class", "padding-left", "5px"),
            ("#styled .bit-swp-rbt i", "swp-btn-icons-class", "padding-left", "6px"),
            ("#styled .bit-swp-rbt", "swp-next-class", "padding-right", "7px"),
            ("#styled .bit-swp-rbt i", "swp-next-icon-class", "padding-right", "8px"),
            ("#styled .bit-swp-lbt", "swp-prev-class", "padding-right", "9px"),
            ("#styled .bit-swp-lbt i", "swp-prev-icon-class", "padding-right", "10px"),
            ("#styled .bit-swp-dcn", "swp-dcn-class", "padding-top", "11px"),
            ("#styled .bit-swp-dot >> nth=1", "swp-dots-class", "margin-top", "12px"),
            ("#styled .bit-swp-cud", "swp-cud-class", "margin-bottom", "13px"),
            ("#styled .bit-swp-ppb", "swp-ppb-class", "margin-left", "14px"),
            ("#styled .bit-swp-ppb i", "swp-ppb-icon-class", "margin-left", "15px"),
        };

        foreach (var (selector, cls, property, value) in parts)
        {
            await Expect(Page.Locator(selector)).ToHaveClassAsync(HasClass(cls));
            await Expect(Page.Locator(selector)).ToHaveCSSAsync(property, value);
        }

        // A button styled by the page still hides at the end it cannot pass.
        await Expect(Page.Locator("#styled-buttons .bit-swp-rbt")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(Page.Locator("#styled-buttons .bit-swp-lbt")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task CssVariables_ReskinTheSwiperItsButtonsAndItsDots()
    {
        await OpenAsync("/e2e/swiper");

        var container = Page.Locator("#vars .bit-swp-cnt");
        await Expect(container).ToHaveCSSAsync("column-gap", "10px");
        await Expect(container).ToHaveCSSAsync("padding-left", "30px");

        var button = Page.Locator("#vars .bit-swp-rbt");
        await Expect(button).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(7, 8, 9)");
        await Expect(button).ToHaveCSSAsync("opacity", "0.4");
        await Expect(button).ToHaveCSSAsync("width", "50px");
        await Expect(button).ToHaveCSSAsync("font-size", "33px");
        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(10, 11, 12)");

        var current = Page.Locator("#vars .bit-swp-cud");
        await Expect(current).ToHaveCSSAsync("background-color", "rgb(19, 20, 21)");
        await Expect(current).ToHaveCSSAsync("width", "40px");
        await Expect(current).ToHaveCSSAsync("height", "12px");
        await Expect(current).ToHaveCSSAsync("border-top-left-radius", "2px");
        var other = Page.Locator("#vars .bit-swp-dot:not(.bit-swp-cud)").First;
        await Expect(other).ToHaveCSSAsync("background-color", "rgb(13, 14, 15)");
        await Expect(Page.Locator("#vars .bit-swp-dts")).ToHaveCSSAsync("column-gap", "7px");
        await Expect(Page.Locator("#vars .bit-swp-dcn")).ToHaveCSSAsync("margin-top", "9px");
        await other.HoverAsync();
        await Expect(other).ToHaveCSSAsync("background-color", "rgb(16, 17, 18)");
        await current.HoverAsync();
        await Expect(current).ToHaveCSSAsync("background-color", "rgb(22, 23, 24)");

        // The focus ring of the keyboard-focused root.
        await Page.Mouse.MoveAsync(0, 0);
        await Page.Locator("#vars").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Page.Locator("#vars")).ToBeFocusedAsync();
        StringAssert.Contains(await ComputedStyleAsync("#vars", "box-shadow"), "rgb(9, 9, 9)");

        await Expect(Page.Locator("#vars-color .bit-swp-cud")).ToHaveCSSAsync("background-color", await ResolveAsync("var(--bit-clr-err)"));
    }

    [TestMethod]
    public async Task Rtl_TheRowRunsFromTheRight_AndTheArrowKeysFollowIt()
    {
        await OpenAsync("/e2e/swiper");

        var rtl = Page.Locator("#rtl");
        await Expect(rtl).ToHaveAttributeAsync("dir", "rtl");
        await Expect(rtl).ToHaveClassAsync(HasClass("bit-rtl"));
        var container = await BoxAsync("#rtl .bit-swp-cnt");
        AssertClose(container.Right, (await BoxAsync("#rtl .bit-swpi >> nth=0")).Right, "the first item at the right");
        var next = await BoxAsync("#rtl .bit-swp-rbt");
        AssertClose(container.X, next.X, "the next button at the left edge");

        await Page.Locator("#rtl .bit-swp-rbt").ClickAsync();
        await ExpectScrollAsync("rtl", 100);
        await Expect(Page.Locator("#rtl .bit-swpi-cur")).ToHaveTextAsync(new[] { "I2" });

        await rtl.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectScrollAsync("rtl", 200);
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectScrollAsync("rtl", 100);

        await Page.Locator("#rtl-inherited").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectScrollAsync("rtl-inherited", 100);
    }

    [TestMethod]
    public async Task Lifecycle_ItemsAddedAndRemoved_AndAPlayingSwiperUnmounted()
    {
        await OpenAsync("/e2e/swiper");

        await Expect(Page.Locator("#dynamic .bit-swp-dot")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-add-item");
        await Page.ClickAsync("#btn-add-item");
        await Expect(Page.Locator("#dynamic .bit-swpi")).ToHaveCountAsync(4);
        await Expect(Page.Locator("#dynamic .bit-swp-dot")).ToHaveCountAsync(2);
        await Expect(Page.Locator("#dynamic .bit-swpi").Nth(3)).ToHaveAttributeAsync("aria-label", "4 of 4");
        await Page.ClickAsync("#btn-remove-item");
        await Expect(Page.Locator("#dynamic .bit-swpi")).ToHaveCountAsync(3);

        await Page.Mouse.MoveAsync(0, 0);
        await Expect(Page.Locator("#mount-swiper .bit-swpi-cur")).ToHaveTextAsync(new[] { "I2" });
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mount-swiper")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-mount");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(Page.Locator("#mount-swiper .bit-swpi-cur")).ToHaveTextAsync(new[] { "I2" });

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private Task ExpectScrollAsync(string id, double expected, float? timeout = null)
    {
        return Page.WaitForFunctionAsync(@"([id, expected]) => {
            const c = document.querySelector(`#${id} .bit-swp-cnt`);
            const scroll = c.closest('.bit-swp-vrt') ? c.scrollTop : Math.abs(c.scrollLeft);
            return Math.abs(scroll - expected) <= 1.5;
        }", new object[] { id, expected }, new() { Timeout = timeout });
    }

    private Task WaitForItemWidthAsync(string id, double expected)
    {
        return Page.WaitForFunctionAsync(@"([id, expected]) => {
            const item = document.querySelector(`#${id} .bit-swpi`);
            return !!item && Math.abs(item.getBoundingClientRect().width - expected) <= 1.5;
        }", new object[] { id, expected });
    }

    private async Task ExpectAutoPlayStateAsync(string name, string state)
    {
        var escaped = System.Text.RegularExpressions.Regex.Escape(state);
        var pattern = state.StartsWith("index=", StringComparison.Ordinal)
            ? name + "\\[" + escaped + "\\]"
            : name + "\\[index=\\d+;" + escaped + "\\]";

        for (var attempt = 0; ; attempt++)
        {
            await Page.EvaluateAsync("() => document.getElementById('btn-read-autoplay').click()");
            try
            {
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

    private async Task<Box> BoxAsync(string selector)
    {
        var box = await Page.Locator(selector).First.BoundingBoxAsync();
        Assert.IsNotNull(box, $"{selector} has no box");
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
/// BitSwiper on a phone: a touch swipe scrolls the row natively and the swiper reports where it came to rest. The touch
/// points are dispatched through the DevTools protocol, so this runs on Chromium only.
/// </summary>
[TestClass]
public class BitSwiperTouchE2ETests : E2ETestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 430, Height = 900 },
        HasTouch = true,
        IsMobile = true,
    };

    [TestMethod]
    public async Task Swipe_ATouchSwipeScrollsTheRow()
    {
        if (IsChromium is false) Assert.Inconclusive("The touch points are dispatched through the Chromium DevTools protocol.");

        await OpenAsync("/e2e/swiper/touch");

        // A tap on the next button works with a finger. It comes first: Chromium turns a tap that follows a
        // synthesized swipe too closely into the end of that gesture rather than a click.
        await Page.Locator("#touch .bit-swp-rbt").TapAsync();
        await Expect(Page.Locator("#touch .bit-swpi-cur")).ToHaveTextAsync(new[] { "I2" });
        await Page.Locator("#touch .bit-swp-lbt").TapAsync();
        await Expect(Page.Locator("#touch .bit-swpi-cur")).ToHaveTextAsync(new[] { "I1" });

        var container = Page.Locator("#touch .bit-swp-cnt");
        await SwipeAsync("#touch .bit-swp-cnt", -200, 0);
        await Page.WaitForFunctionAsync("() => document.querySelector('#touch .bit-swp-cnt').scrollLeft >= 100");
        await Expect(Page.Locator("#touch .bit-swpi-cur")).Not.ToHaveTextAsync(new[] { "I1" });

        await SwipeAsync("#touch .bit-swp-cnt", 400, 0);
        await Page.WaitForFunctionAsync("() => document.querySelector('#touch .bit-swp-cnt').scrollLeft <= 1");
        await Expect(Page.Locator("#touch .bit-swpi-cur")).ToHaveTextAsync(new[] { "I1" });
        Assert.AreEqual(0, await container.EvaluateAsync<double>("el => el.scrollLeft"));
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
        for (var step = 1; step <= 10; step++)
        {
            await session.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
            {
                ["type"] = "touchMove",
                ["touchPoints"] = new[] { new Dictionary<string, object> { ["x"] = x + dx * step / 10, ["y"] = y + dy * step / 10 } },
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
