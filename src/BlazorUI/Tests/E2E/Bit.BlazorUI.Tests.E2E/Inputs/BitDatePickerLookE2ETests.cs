using System.Globalization;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// How BitDatePicker looks in a real browser (/e2e/date-picker/look): every BitColor (the class on the root and on the
/// callout, and the role's color on today and on the highlighted current month), every BitSize (the class and the
/// field, cell and time sizes), DropDirection (flipping and going beside the field), Underlined, HasBorder,
/// IconPlacement, the icons by IconName and by Icon, ShowGoToToday, the public --bit-DatePicker-* variables set on an
/// ancestor, on Style and on Styles.Callout (and Color and Size winning over them), Style/Class and Styles/Classes on
/// their parts, Dir="Rtl" and an inherited dir (mirrored placement and arrow keys), the BitParams cascade, and the
/// callout's entry animation under prefers-reduced-motion and ForceAnimation.
/// </summary>
[TestClass]
public class BitDatePickerLookE2ETests : BitDatePickersE2ETestBase
{
    private const string Route = "/e2e/date-picker/look";

    // BitColor, in the enum's order, with the class suffix BitCssClasses.Color gives it and the token of its role.
    private static readonly (string Name, string Suffix, string Token)[] Colors =
    [
        ("Primary", "pri", "--bit-clr-pri"),
        ("Secondary", "sec", "--bit-clr-sec"),
        ("Tertiary", "ter", "--bit-clr-ter"),
        ("Info", "inf", "--bit-clr-inf"),
        ("Success", "suc", "--bit-clr-suc"),
        ("Warning", "wrn", "--bit-clr-wrn"),
        ("SevereWarning", "swr", "--bit-clr-swr"),
        ("Error", "err", "--bit-clr-err"),
        ("PrimaryBackground", "pbg", "--bit-clr-bg-pri"),
        ("SecondaryBackground", "sbg", "--bit-clr-bg-sec"),
        ("TertiaryBackground", "tbg", "--bit-clr-bg-ter"),
        ("PrimaryForeground", "pfg", "--bit-clr-fg-pri"),
        ("SecondaryForeground", "sfg", "--bit-clr-fg-sec"),
        ("TertiaryForeground", "tfg", "--bit-clr-fg-ter"),
        ("PrimaryBorder", "pbr", "--bit-clr-brd-pri"),
        ("SecondaryBorder", "sbr", "--bit-clr-brd-sec"),
        ("TertiaryBorder", "tbr", "--bit-clr-brd-ter"),
    ];

    private ILocator Input(string rootId) => Page.Locator($"#{rootId} .bit-dtp-inp");

    private async Task<string> OpenPickerAsync(string rootId)
    {
        await NoCalloutOpenAsync();

        var pid = await PickerIdAsync(rootId);

        await Input(rootId).ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await SettleAsync(Callout(pid));

        return pid;
    }

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    [TestMethod]
    public async Task Color_EveryColorClassesTheRootAndTheCalloutAndPaintsTodayInItsRole()
    {
        await OpenAsync(Route);

        var unset = await PickerIdAsync("dp-color-none");
        await Expect(Day(unset, Today)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));

        foreach (var (name, suffix, token) in Colors)
        {
            var rootId = $"dp-color-{name}";
            var pid = await PickerIdAsync(rootId);
            var expected = await ResolveColorAsync($"var({token})");

            await Expect(Page.Locator($"#{rootId}")).ToHaveClassAsync(HasClass($"bit-dtp-{suffix}"));
            await Expect(Callout(pid)).ToHaveClassAsync(HasClass($"bit-dtp-{suffix}"));
            await Expect(Day(pid, Today)).ToHaveCSSAsync("background-color", expected);
            await Expect(MonthCell(pid, 6)).ToHaveCSSAsync("background-color", expected);
        }

        // The unset picker publishes no color class of its own.
        var classes = await Page.Locator("#dp-color-none").GetAttributeAsync("class");
        Assert.IsFalse(Colors.Any(c => classes!.Split(' ').Contains($"bit-dtp-{c.Suffix}")), classes);
    }

    [TestMethod]
    public async Task Color_TheOpenFieldTakesTheColorOfItsPicker()
    {
        await OpenAsync(Route);

        await OpenPickerAsync("dp-color-Error");

        await Expect(Page.Locator("#dp-color-Error .bit-dtp-icn")).ToHaveCSSAsync("border-top-color", await ResolveColorAsync("var(--bit-clr-err)"));
        await Expect(Page.Locator("#dp-color-Error .bit-dtp-ico")).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-err)"));
    }

    [TestMethod]
    public async Task Size_EverySizeClassesThePickerAndScalesTheFieldTheCellsAndTheTime()
    {
        await OpenAsync(Route);

        var heights = new Dictionary<string, double>();
        var cells = new Dictionary<string, double>();
        var times = new Dictionary<string, double>();

        foreach (var (name, suffix) in new[] { ("Small", "sm"), ("Medium", "md"), ("Large", "lg"), ("none", "") })
        {
            var rootId = $"dp-size-{name}";
            var pid = await OpenPickerAsync(rootId);

            if (suffix.Length > 0)
            {
                await Expect(Page.Locator($"#{rootId}")).ToHaveClassAsync(HasClass($"bit-dtp-{suffix}"));
                await Expect(Callout(pid)).ToHaveClassAsync(HasClass($"bit-dtp-{suffix}"));
            }

            heights[name] = Px(await StyleOfAsync(Page.Locator($"#{rootId} .bit-dtp-icn"), "height"));
            cells[name] = Px(await StyleOfAsync(Day(pid, Today), "width"));
            times[name] = Px(await StyleOfAsync(Callout(pid).Locator(".bit-dtp-tin").First, "font-size"));

            await Page.Keyboard.PressAsync("Escape");
            await Expect(Callout(pid)).ToBeHiddenAsync();
        }

        Assert.IsTrue(heights["Small"] < heights["Medium"] && heights["Medium"] < heights["Large"], string.Join(", ", heights));
        Assert.IsTrue(cells["Small"] < cells["Medium"] && cells["Medium"] < cells["Large"], string.Join(", ", cells));
        Assert.IsTrue(times["Small"] < times["Medium"] && times["Medium"] < times["Large"], string.Join(", ", times));
        Assert.AreEqual(heights["Medium"], heights["none"], "An unset Size is the medium one.");
        Assert.AreEqual(cells["Medium"], cells["none"]);
    }

    [TestMethod]
    public async Task DropDirection_TopAndBottomFlipsAboveWhenThereIsMoreRoomThere()
    {
        await OpenAsync(Route);

        var wrapper = Page.Locator("#dp-drop-tb .bit-dtp-wrp");

        // The field sits at the bottom edge of the viewport, with the page above it.
        await wrapper.EvaluateAsync("el => { el.scrollIntoView({ block: 'end' }); window.scrollBy(0, 4); }");

        var pid = await OpenPickerAsync("dp-drop-tb");
        var field = await BoxAsync(wrapper);
        var callout = await BoxAsync(Callout(pid));

        await Expect(Callout(pid)).ToHaveAttributeAsync("data-bit-cal-pos", "above");
        Assert.AreEqual(field.Y - 1, callout.Y + callout.Height, 2, "The callout's bottom sits on the field's top edge.");
    }

    [TestMethod]
    public async Task DropDirection_AllGoesBesideTheFieldWhenNeitherSideHasRoom()
    {
        await Page.SetViewportSizeAsync(1280, 300);
        await OpenAsync(Route);

        await Page.Locator("#dp-drop-all .bit-dtp-wrp").EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");

        var pid = await OpenPickerAsync("dp-drop-all");
        var field = await BoxAsync(Page.Locator("#dp-drop-all .bit-dtp-wrp"));
        var callout = await BoxAsync(Callout(pid));

        await Expect(Callout(pid)).ToHaveAttributeAsync("data-bit-cal-pos", "right");
        Assert.AreEqual(field.X + field.Width + 1, callout.X, 2, "The callout starts right after the field.");
        Assert.IsTrue(callout.Y >= 0 && callout.Y + callout.Height <= 300 + 1, "The callout stays on screen.");

        await Page.Keyboard.PressAsync("Escape");

        var other = await OpenPickerAsync("dp-drop-tb");
        var position = await Callout(other).GetAttributeAsync("data-bit-cal-pos");
        Assert.IsTrue(position is "below" or "above", $"TopAndBottom never goes beside the field ({position}).");
    }

    [TestMethod]
    public async Task Appearance_UnderlinedAndHasBorderShapeTheField()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#dp-underlined")).ToHaveClassAsync(HasClass("bit-dtp-und"));
        await Expect(Page.Locator("#dp-underlined .bit-dtp-icn")).ToHaveCSSAsync("border-top-width", "0px");
        await Expect(Page.Locator("#dp-underlined .bit-dtp-wrp")).ToHaveCSSAsync("border-bottom-width", "1px");

        await Expect(Page.Locator("#dp-noborder")).ToHaveClassAsync(HasClass("bit-dtp-nbd"));
        await Expect(Page.Locator("#dp-noborder .bit-dtp-icn")).ToHaveCSSAsync("border-top-width", "0px");

        await Expect(Page.Locator("#dp-color-none .bit-dtp-icn")).ToHaveCSSAsync("border-top-width", "1px");
    }

    [TestMethod]
    public async Task Appearance_IconPlacementStartPutsTheIconBeforeTheText()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#dp-iconstart")).ToHaveClassAsync(HasClass("bit-dtp-lic"));

        var startIcon = await BoxAsync(Page.Locator("#dp-iconstart .bit-dtp-ico"));
        var startInput = await BoxAsync(Input("dp-iconstart"));
        Assert.IsTrue(startIcon.X < startInput.X, "The icon leads the input.");

        var endIcon = await BoxAsync(Page.Locator("#dp-color-none .bit-dtp-ico"));
        var endInput = await BoxAsync(Input("dp-color-none"));
        Assert.IsTrue(endIcon.X > endInput.X, "By default the icon follows the input.");
    }

    [TestMethod]
    public async Task Appearance_IconNameAndIconPickTheGlyphs()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#dp-color-none .bit-dtp-ico")).ToHaveClassAsync(HasClass("bit-icon--CalendarMirrored"));
        await Expect(Page.Locator("#dp-iconname .bit-dtp-ico")).ToHaveClassAsync(HasClass("bit-icon--Airplane"));
        await Expect(Page.Locator("#dp-icon-ext .bit-dtp-ico")).ToHaveClassAsync(HasClass("e2e-ext-icon"));
        await Expect(Page.Locator("#dp-icon-ext .bit-dtp-ico")).Not.ToHaveClassAsync(HasClass("bit-icon--Airplane"));
        await Expect(Page.Locator("#dp-nav-iconnames .bit-dtp-clr i")).ToHaveClassAsync(HasClass("bit-icon--Delete"));

        var names = await OpenPickerAsync("dp-nav-iconnames");
        await Expect(Callout(names).Locator(".bit-dtp-nbt[title='Go to previous month'] i")).ToHaveClassAsync(HasClass("bit-icon--ChevronLeft"));
        await Expect(Callout(names).Locator(".bit-dtp-nbt[title='Go to next month'] i")).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        await Expect(Callout(names).Locator(".bit-dtp-gtb i")).ToHaveClassAsync(HasClass("bit-icon--Home"));
        await Page.Keyboard.PressAsync("Escape");

        var pid = await OpenPickerAsync("dp-nav-icons");
        var callout = Callout(pid);
        await Expect(callout.Locator(".bit-dtp-nbt[title='Go to previous month'] i")).ToHaveClassAsync(HasClass("e2e-prev-month"));
        await Expect(callout.Locator(".bit-dtp-nbt[title='Go to next month'] i")).ToHaveClassAsync(HasClass("e2e-next-month"));
        await Expect(callout.Locator(".bit-dtp-gtb i")).ToHaveClassAsync(HasClass("e2e-today"));
        await Expect(callout.Locator(".bit-dtp-nbt[title='Go to previous year 2023'] i")).ToHaveClassAsync(HasClass("e2e-prev-year"));
        await Expect(callout.Locator(".bit-dtp-nbt[title='Go to next year 2025'] i")).ToHaveClassAsync(HasClass("e2e-next-year"));
        await callout.Locator(".bit-dtp-mwp .bit-dtp-ptb").ClickAsync();
        await Expect(callout.Locator(".bit-dtp-nbt[title='Previous year range 2011 - 2022'] i")).ToHaveClassAsync(HasClass("e2e-prev-range"));
        await Expect(callout.Locator(".bit-dtp-nbt[title='Next year range 2035 - 2046'] i")).ToHaveClassAsync(HasClass("e2e-next-range"));
        await Page.Keyboard.PressAsync("Escape");

        var time = await OpenPickerAsync("dp-time-icons");
        await Expect(Page.Locator("#dp-time-icons .bit-dtp-clr i")).ToHaveClassAsync(HasClass("e2e-clear"));
        foreach (var (title, icon) in new[]
                 {
                     ("Increase hour", "e2e-inc-hour"), ("Decrease hour", "e2e-dec-hour"),
                     ("Increase minute", "e2e-inc-minute"), ("Decrease minute", "e2e-dec-minute"),
                     ("Increase second", "e2e-inc-second"), ("Decrease second", "e2e-dec-second"),
                 })
        {
            await Expect(Callout(time).Locator($".bit-dtp-tbt[title='{title}'] i")).ToHaveClassAsync(HasClass(icon));
        }
        await Expect(Callout(time).Locator(".bit-dtp-gtn i")).ToHaveClassAsync(HasClass("e2e-now"));
        await Expect(Callout(time).Locator(".bit-dtp-nbt[title='Close date picker'] i")).ToHaveClassAsync(HasClass("e2e-close"));
        await Page.Keyboard.PressAsync("Escape");

        var overlay = await OpenPickerAsync("dp-overlay-icons");
        await Expect(Callout(overlay).Locator(".bit-dtp-nbt[title='Show time picker'] i")).ToHaveClassAsync(HasClass("e2e-show-time"));
        await Callout(overlay).Locator(".bit-dtp-nbt[title='Show time picker']").ClickAsync();
        await Expect(Callout(overlay).Locator(".bit-dtp-nbt[title='Hide time picker'] i")).ToHaveClassAsync(HasClass("e2e-hide-time"));
    }

    [TestMethod]
    public async Task Appearance_EveryIconNameOfTheCalloutPicksItsGlyph()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-time-iconnames");
        var callout = Callout(pid);

        foreach (var (title, icon) in new[]
                 {
                     ("Increase hour", "CaretUp8"), ("Decrease hour", "CaretDown8"),
                     ("Increase minute", "CaretUpSolid8"), ("Decrease minute", "CaretDownSolid8"),
                     ("Increase second", "ChevronUp"), ("Decrease second", "ChevronDown"),
                 })
        {
            await Expect(callout.Locator($".bit-dtp-tbt[title='{title}'] i")).ToHaveClassAsync(HasClass($"bit-icon--{icon}"));
        }
        await Expect(callout.Locator(".bit-dtp-gtn i")).ToHaveClassAsync(HasClass("bit-icon--Timer"));
        await Expect(callout.Locator(".bit-dtp-nbt[title='Close date picker'] i")).ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));

        // With the time picker beside the days, the month picker opens from the title.
        await callout.Locator(".bit-dtp-dwp .bit-dtp-ptb").ClickAsync();
        await Expect(callout.Locator(".bit-dtp-nbt[title='Go to previous year 2023'] i")).ToHaveClassAsync(HasClass("bit-icon--Back"));
        await Expect(callout.Locator(".bit-dtp-nbt[title='Go to next year 2025'] i")).ToHaveClassAsync(HasClass("bit-icon--Forward"));
        await callout.Locator(".bit-dtp-mwp .bit-dtp-ptb").ClickAsync();
        await Expect(callout.Locator(".bit-dtp-nbt[title='Previous year range 2011 - 2022'] i")).ToHaveClassAsync(HasClass("bit-icon--Rewind"));
        await Expect(callout.Locator(".bit-dtp-nbt[title='Next year range 2035 - 2046'] i")).ToHaveClassAsync(HasClass("bit-icon--FastForward"));

        // Closed through the overlay: WebKit does not focus a clicked button, so the focus may be on the body.
        await Page.Mouse.ClickAsync(1260, 700);

        var overlay = await OpenPickerAsync("dp-overlay-iconnames");
        await Expect(Callout(overlay).Locator(".bit-dtp-nbt[title='Show time picker'] i")).ToHaveClassAsync(HasClass("bit-icon--Stopwatch"));
        await Callout(overlay).Locator(".bit-dtp-nbt[title='Show time picker']").ClickAsync();
        await Expect(Callout(overlay).Locator(".bit-dtp-nbt[title='Hide time picker'] i")).ToHaveClassAsync(HasClass("bit-icon--Calendar"));
    }

    [TestMethod]
    public async Task Appearance_ShowGoToTodayOffRemovesTheButton()
    {
        await OpenAsync(Route);

        var pid = await OpenPickerAsync("dp-nogotoday");
        await Expect(Callout(pid).Locator(".bit-dtp-gtb")).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("Escape");

        var other = await OpenPickerAsync("dp-color-none");
        await Expect(Callout(other).Locator(".bit-dtp-gtb")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestorTheyRestyleTheFieldAndTheRelocatedCallout()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#dp-vars");

        await Expect(root.Locator(".bit-dtp-icn")).ToHaveCSSAsync("border-top-left-radius", "13px");
        await Expect(root.Locator(".bit-dtp-icn")).ToHaveCSSAsync("border-top-color", "rgb(1, 2, 3)");
        await Expect(root.Locator(".bit-dtp-icn")).ToHaveCSSAsync("height", "44px");
        await Expect(root.Locator(".bit-dtp-ico")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(root.Locator(".bit-dtp-lbl")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");

        var pid = await OpenPickerAsync("dp-vars");
        await Page.Mouse.MoveAsync(1, 1);

        // The callout lives under the body while open, and the variables still reach it.
        Assert.IsFalse(await Callout(pid).EvaluateAsync<bool>("el => !!el.closest('#vars-host')"));
        await Expect(Callout(pid)).ToHaveCSSAsync("border-top-left-radius", "11px");
        await Expect(Callout(pid).Locator(".bit-dtp-cac")).ToHaveCSSAsync("background-color", "rgb(250, 240, 230)");
        await Expect(Day(pid, Today)).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(Day(pid, Today)).ToHaveCSSAsync("color", "rgb(200, 210, 220)");
        await Expect(Day(pid, 2024, 6, 5)).ToHaveCSSAsync("background-color", "rgb(40, 50, 60)");
        await Expect(Day(pid, 2024, 6, 6)).ToHaveCSSAsync("border-top-left-radius", "3px");
        await Expect(Callout(pid).Locator(".bit-dtp-wnm").First).ToHaveCSSAsync("background-color", "rgb(70, 80, 90)");
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColorAndSizeWinOverTheVariables()
    {
        await OpenAsync(Route);

        var large = Px(await StyleOfAsync(Page.Locator("#dp-size-Large .bit-dtp-icn"), "height"));
        await Expect(Page.Locator("#dp-vars-color .bit-dtp-icn")).ToHaveCSSAsync("height", $"{large.ToString(CultureInfo.InvariantCulture)}px");

        var pid = await OpenPickerAsync("dp-vars-color");
        await Page.Mouse.MoveAsync(1, 1);
        await Expect(Day(pid, Today)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-err)"));
    }

    [TestMethod]
    public async Task CssVariables_StyleAndStylesCalloutReachTheirOwnPicker()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#dp-vars-style .bit-dtp-icn")).ToHaveCSSAsync("background-color", "rgb(255, 250, 200)");

        var pid = await OpenPickerAsync("dp-vars-style");
        await Page.Mouse.MoveAsync(1, 1);
        await Expect(Day(pid, 2024, 6, 20)).ToHaveCSSAsync("background-color", "rgb(1, 100, 1)");
        await Page.Keyboard.PressAsync("Escape");

        var roomy = await OpenPickerAsync("dp-vars-callout");
        await Expect(Day(roomy, 2024, 6, 20)).ToHaveCSSAsync("width", "40px");
        await Expect(Day(roomy, 2024, 6, 20)).ToHaveCSSAsync("height", "40px");
        var field = await StyleOfAsync(Page.Locator("#dp-vars-callout .bit-dtp-icn"), "height");
        Assert.AreEqual(await StyleOfAsync(Page.Locator("#dp-color-none .bit-dtp-icn"), "height"), field, "The field keeps its own size.");
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheRootAndEveryPart()
    {
        await OpenAsync(Route);

        var styled = Page.Locator("#dp-styles");
        await Expect(styled).ToHaveCSSAsync("margin-left", "3px");
        await Expect(styled.Locator(".bit-dtp-lbl")).ToHaveCSSAsync("color", "rgb(11, 22, 33)");
        await Expect(Input("dp-styles")).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(styled.Locator(".bit-dtp-icn")).ToHaveCSSAsync("background-color", "rgb(240, 255, 240)");
        await Expect(styled.Locator(".bit-dtp-ico")).ToHaveCSSAsync("color", "rgb(1, 1, 1)");
        await Expect(styled).Not.ToHaveCSSAsync("background-color", "rgb(255, 0, 255)");

        // Styles.Focused is applied while the field holds the focus.
        await Input("dp-styles").FocusAsync();
        await Expect(styled).ToHaveCSSAsync("background-color", "rgb(255, 0, 255)");

        var pid = await OpenPickerAsync("dp-styles");
        await Expect(Callout(pid)).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(Callout(pid).Locator(".bit-dtp-grp")).ToHaveCSSAsync("padding-top", "3px");
        await Expect(Day(pid, 2024, 6, 3)).ToHaveCSSAsync("font-style", "italic");
        await Expect(Day(pid, Today)).ToHaveCSSAsync("text-transform", "uppercase");
        await Expect(Day(pid, 2024, 6, 20)).ToHaveCSSAsync("outline-color", "rgb(0, 128, 0)");
        await Expect(Callout(pid).Locator(".bit-dtp-dwp .bit-dtp-pkt")).ToHaveCSSAsync("color", "rgb(0, 100, 0)");
        await Expect(Callout(pid).Locator(".bit-dtp-nbt[title='Go to previous month']")).ToHaveCSSAsync("opacity", "0.5");
        await Expect(MonthCell(pid, 3)).ToHaveCSSAsync("font-weight", "700");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();

        var classed = Page.Locator("#dp-classes");
        await Expect(classed).ToHaveClassAsync(HasClass("e2e-c-root"));
        await Expect(classed.Locator(".bit-dtp-lbl")).ToHaveClassAsync(HasClass("e2e-c-label"));
        await Expect(Input("dp-classes")).ToHaveClassAsync(HasClass("e2e-c-input"));
        await Expect(classed.Locator(".bit-dtp-icn")).ToHaveClassAsync(HasClass("e2e-c-container"));
        await Expect(classed.Locator(".bit-dtp-ico")).ToHaveClassAsync(HasClass("e2e-c-icon"));
        await Expect(classed).Not.ToHaveClassAsync(HasClass("e2e-c-focused"));
        await Input("dp-classes").FocusAsync();
        await Expect(classed).ToHaveClassAsync(HasClass("e2e-c-focused"));

        var c = await OpenPickerAsync("dp-classes");
        await Expect(Overlay(c)).ToHaveClassAsync(HasClass("e2e-c-overlay"));
        await Expect(Callout(c)).ToHaveClassAsync(HasClass("e2e-c-callout"));
        await Expect(Day(c, 2024, 6, 3)).ToHaveClassAsync(HasClass("e2e-c-day"));
        await Expect(Day(c, Today)).ToHaveClassAsync(HasClass("e2e-c-today"));
        await Expect(Day(c, 2024, 6, 20)).ToHaveClassAsync(HasClass("e2e-c-selected"));
        await Expect(Day(c, 2024, 6, 5)).ToHaveClassAsync(HasClass("e2e-c-highlighted"));
        await Expect(Day(c, 2024, 6, 3)).Not.ToHaveClassAsync(HasClass("e2e-c-selected"));
        await Expect(Callout(c).Locator(".bit-dtp-nbt[title='Go to previous month']")).ToHaveClassAsync(HasClass("e2e-c-prev"));
        await Expect(Callout(c).Locator(".bit-dtp-nbt[title='Go to next month']")).ToHaveClassAsync(HasClass("e2e-c-next"));
        await Expect(Callout(c).Locator(".bit-dtp-dwp .bit-dtp-pkt")).ToHaveClassAsync(HasClass("e2e-c-month"));
        await Expect(Callout(c).Locator(".bit-dtp-dwp .bit-dtp-pkh")).ToHaveClassAsync(HasClass("e2e-c-header"));
        await Expect(Callout(c).Locator(".bit-dtp-dgh [role=columnheader]").First).ToHaveClassAsync(HasClass("e2e-c-dayname"));
        await Expect(Callout(c).Locator(".bit-dtp-dvd")).ToHaveClassAsync(HasClass("e2e-c-divider"));
        await Expect(Callout(c).Locator(".bit-dtp-mwp")).ToHaveClassAsync(HasClass("e2e-c-ym"));
        await Expect(MonthCell(c, 3)).ToHaveClassAsync(HasClass("e2e-c-monthbtn"));
    }

    [TestMethod]
    public async Task Rtl_DirRtlMirrorsTheFieldTheCalloutAndTheArrowKeys()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#dp-rtl");
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));

        var icon = await BoxAsync(root.Locator(".bit-dtp-ico"));
        var input = await BoxAsync(Input("dp-rtl"));
        Assert.IsTrue(icon.X < input.X, "Right to left, the icon at the end of the field is on its left.");

        var pid = await OpenPickerAsync("dp-rtl");
        await Expect(Callout(pid)).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Callout(pid)).ToHaveClassAsync(HasClass("bit-dtp-rtl"));

        var field = await BoxAsync(root.Locator(".bit-dtp-wrp"));
        var callout = await BoxAsync(Callout(pid));
        Assert.AreEqual(field.X + field.Width, callout.X + callout.Width, 2, "The callout ends where the field ends, on the right.");

        var day = await BoxAsync(Day(pid, Today));
        var next = await BoxAsync(Day(pid, 2024, 6, 13));
        Assert.IsTrue(next.X < day.X, "The days of a week run from right to left.");

        await Expect(Day(pid, Today)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day(pid, 2024, 6, 11)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Day(pid, 2024, 6, 12)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Day(pid, 2024, 6, 13)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Rtl_ADirectionInheritedFromThePageIsFollowedToo()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#dp-rtl-inherited");
        await Expect(root).Not.ToHaveAttributeAsync("dir", "rtl");

        var pid = await OpenPickerAsync("dp-rtl-inherited");
        await Expect(Callout(pid)).ToHaveCSSAsync("direction", "rtl");

        var field = await BoxAsync(root.Locator(".bit-dtp-wrp"));
        var callout = await BoxAsync(Callout(pid));
        Assert.AreEqual(field.X + field.Width, callout.X + callout.Width, 2, "The callout is aligned to the field's right edge.");

        await Expect(Day(pid, Today)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day(pid, 2024, 6, 11)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Day(pid, 2024, 6, 12)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Cascade_BitParamsHandsItsDefaultsToThePickersInsideIt()
    {
        await OpenAsync(Route);

        await Expect(Input("dp-casc-1")).ToHaveAttributeAsync("placeholder", "From the cascade");
        await Expect(Input("dp-casc-2")).ToHaveAttributeAsync("placeholder", "Own");
        await Expect(Input("dp-casc-out")).ToHaveAttributeAsync("placeholder", "");

        var first = await OpenPickerAsync("dp-casc-1");
        await Expect(Callout(first).Locator(".bit-dtp-wnm").First).ToBeVisibleAsync();
        await Expect(Callout(first).Locator(".bit-dtp-dgh [role=columnheader]").Nth(1)).ToHaveAttributeAsync("aria-label", "Monday");
        await Day(first, 2024, 6, 20).ClickAsync();
        await Expect(Page.Locator("#dp-casc-1 .bit-dtp-clr")).ToBeVisibleAsync();

        var second = await OpenPickerAsync("dp-casc-2");
        await Expect(Callout(second).Locator(".bit-dtp-wnm")).ToHaveCountAsync(0);
        await Expect(Callout(second).Locator(".bit-dtp-dgh [role=columnheader]").First).ToHaveAttributeAsync("aria-label", "Monday");
        await Page.Keyboard.PressAsync("Escape");

        var outside = await OpenPickerAsync("dp-casc-out");
        await Expect(Callout(outside).Locator(".bit-dtp-wnm")).ToHaveCountAsync(0);
        await Expect(Callout(outside).Locator(".bit-dtp-dgh [role=columnheader]").First).ToHaveAttributeAsync("aria-label", "Sunday");
    }

    [TestMethod]
    public async Task Motion_TheEntryAnimationCollapsesUnderReducedMotionUnlessForced()
    {
        static double Seconds(string value) => value.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(value[..^2], CultureInfo.InvariantCulture) / 1000
            : double.Parse(value.TrimEnd('s'), CultureInfo.InvariantCulture);

        await OpenAsync(Route);

        var normal = await PickerIdAsync("dp-motion");
        Assert.IsTrue(Seconds(await StyleOfAsync(Callout(normal), "animation-duration")) >= 0.1, "The callout animates in.");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        var reduced = await OpenPickerAsync("dp-motion");
        Assert.IsTrue(Seconds(await StyleOfAsync(Callout(reduced), "animation-duration")) < 0.001, "Reduced motion collapses the entry animation.");
        await Page.Keyboard.PressAsync("Escape");

        var forced = await OpenPickerAsync("dp-motion-force");
        await Expect(Page.Locator("#dp-motion-force")).ToHaveClassAsync(HasClass("bit-fam"));
        Assert.IsTrue(Seconds(await StyleOfAsync(Callout(forced), "animation-duration")) >= 0.1, "ForceAnimation brings the animation back.");
    }
}
