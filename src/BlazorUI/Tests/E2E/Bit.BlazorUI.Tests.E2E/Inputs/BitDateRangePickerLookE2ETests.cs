using System.Globalization;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// How BitDateRangePicker looks in a real browser (/e2e/date-range-picker/look): every BitColor (the class on the
/// root and the callout, the role's color on the ends of the range and its lighter body between them), every BitSize,
/// DropDirection (flipping above and going beside the field), Underlined, HasBorder, IconPlacement, every icon by
/// Icon and by IconName, the public --bit-DateRangePicker-* variables set on an ancestor, on Style and on
/// Styles.Callout (and Color and Size winning over them), Style/Class and Styles/Classes on their parts, Dir="Rtl" and
/// an inherited dir, the BitParams cascade, and the entry animation under prefers-reduced-motion and ForceAnimation.
/// </summary>
[TestClass]
public class BitDateRangePickerLookE2ETests : BitDatePickersE2ETestBase
{
    private const string Route = "/e2e/date-range-picker/look";

    // BitColor, in the enum's order, with the class suffix BitCssClasses.Color gives it, the token of its role and,
    // for the accent roles, the token of the lighter body the days between the ends are painted with.
    private static readonly (string Name, string Suffix, string Token, string? Light)[] Colors =
    [
        ("Primary", "pri", "--bit-clr-pri", "--bit-clr-pri-light"),
        ("Secondary", "sec", "--bit-clr-sec", "--bit-clr-sec-light"),
        ("Tertiary", "ter", "--bit-clr-ter", "--bit-clr-ter-light"),
        ("Info", "inf", "--bit-clr-inf", "--bit-clr-inf-light"),
        ("Success", "suc", "--bit-clr-suc", "--bit-clr-suc-light"),
        ("Warning", "wrn", "--bit-clr-wrn", "--bit-clr-wrn-light"),
        ("SevereWarning", "swr", "--bit-clr-swr", "--bit-clr-swr-light"),
        ("Error", "err", "--bit-clr-err", "--bit-clr-err-light"),
        ("PrimaryBackground", "pbg", "--bit-clr-bg-pri", null),
        ("SecondaryBackground", "sbg", "--bit-clr-bg-sec", null),
        ("TertiaryBackground", "tbg", "--bit-clr-bg-ter", null),
        ("PrimaryForeground", "pfg", "--bit-clr-fg-pri", null),
        ("SecondaryForeground", "sfg", "--bit-clr-fg-sec", null),
        ("TertiaryForeground", "tfg", "--bit-clr-fg-ter", null),
        ("PrimaryBorder", "pbr", "--bit-clr-brd-pri", null),
        ("SecondaryBorder", "sbr", "--bit-clr-brd-sec", null),
        ("TertiaryBorder", "tbr", "--bit-clr-brd-ter", null),
    ];

    private ILocator Input(string rootId) => Page.Locator($"#{rootId} .bit-dtrp-inp");

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
    public async Task Color_EveryColorClassesThePickerAndPaintsTheRangeInItsRole()
    {
        await OpenAsync(Route);

        var unset = await PickerIdAsync("drp-color-none");
        await Expect(Day(unset, 2024, 6, 10)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));

        foreach (var (name, suffix, token, light) in Colors)
        {
            var rootId = $"drp-color-{name}";
            var pid = await PickerIdAsync(rootId);

            await Expect(Page.Locator($"#{rootId}")).ToHaveClassAsync(HasClass($"bit-dtrp-{suffix}"));
            await Expect(Callout(pid)).ToHaveClassAsync(HasClass($"bit-dtrp-{suffix}"));
            await Expect(Day(pid, 2024, 6, 10)).ToHaveCSSAsync("background-color", await ResolveColorAsync($"var({token})"));
            await Expect(Day(pid, 2024, 6, 14)).ToHaveCSSAsync("background-color", await ResolveColorAsync($"var({token})"));

            if (light is not null)
            {
                await Expect(Day(pid, 2024, 6, 11)).ToHaveCSSAsync("background-color", await ResolveColorAsync($"var({light})"));
            }
        }

        var classes = await Page.Locator("#drp-color-none").GetAttributeAsync("class");
        Assert.IsFalse(Colors.Any(c => classes!.Split(' ').Contains($"bit-dtrp-{c.Suffix}")), classes);
    }

    [TestMethod]
    public async Task Size_EverySizeClassesThePickerAndScalesTheFieldAndTheCells()
    {
        await OpenAsync(Route);

        var heights = new Dictionary<string, double>();
        var cells = new Dictionary<string, double>();

        foreach (var (name, suffix) in new[] { ("Small", "sm"), ("Medium", "md"), ("Large", "lg"), ("none", "") })
        {
            var rootId = $"drp-size-{name}";
            var pid = await OpenPickerAsync(rootId);

            if (suffix.Length > 0)
            {
                await Expect(Page.Locator($"#{rootId}")).ToHaveClassAsync(HasClass($"bit-dtrp-{suffix}"));
                await Expect(Callout(pid)).ToHaveClassAsync(HasClass($"bit-dtrp-{suffix}"));
            }

            heights[name] = Px(await StyleOfAsync(Page.Locator($"#{rootId} .bit-dtrp-icn"), "height"));
            cells[name] = Px(await StyleOfAsync(Day(pid, Today), "width"));

            await Page.Keyboard.PressAsync("Escape");
            await Expect(Callout(pid)).ToBeHiddenAsync();
        }

        Assert.IsTrue(heights["Small"] < heights["Medium"] && heights["Medium"] < heights["Large"], string.Join(", ", heights));
        Assert.IsTrue(cells["Small"] < cells["Medium"] && cells["Medium"] < cells["Large"], string.Join(", ", cells));
        Assert.AreEqual(heights["Medium"], heights["none"], "An unset Size is the medium one.");
        Assert.AreEqual(cells["Medium"], cells["none"]);
    }

    [TestMethod]
    public async Task DropDirection_TopAndBottomFlipsAboveAndAllGoesBeside()
    {
        await OpenAsync(Route);

        var wrapper = Page.Locator("#drp-drop-tb .bit-dtrp-wrp");
        await wrapper.EvaluateAsync("el => { el.scrollIntoView({ block: 'end' }); window.scrollBy(0, 4); }");

        var pid = await OpenPickerAsync("drp-drop-tb");
        var field = await BoxAsync(wrapper);
        var callout = await BoxAsync(Callout(pid));
        await Expect(Callout(pid)).ToHaveAttributeAsync("data-bit-cal-pos", "above");
        Assert.AreEqual(field.Y - 1, callout.Y + callout.Height, 2, "The callout's bottom sits on the field's top edge.");
        await Page.Keyboard.PressAsync("Escape");

        await Page.SetViewportSizeAsync(1280, 300);
        await Page.Locator("#drp-drop-all .bit-dtrp-wrp").EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");

        var all = await OpenPickerAsync("drp-drop-all");
        var allField = await BoxAsync(Page.Locator("#drp-drop-all .bit-dtrp-wrp"));
        var allCallout = await BoxAsync(Callout(all));
        await Expect(Callout(all)).ToHaveAttributeAsync("data-bit-cal-pos", "right");
        Assert.AreEqual(allField.X + allField.Width + 1, allCallout.X, 2, "The callout starts right after the field.");
    }

    [TestMethod]
    public async Task Appearance_UnderlinedHasBorderAndIconPlacement()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#drp-underlined")).ToHaveClassAsync(HasClass("bit-dtrp-und"));
        await Expect(Page.Locator("#drp-underlined .bit-dtrp-icn")).ToHaveCSSAsync("border-top-width", "0px");
        await Expect(Page.Locator("#drp-noborder")).ToHaveClassAsync(HasClass("bit-dtrp-nbd"));
        await Expect(Page.Locator("#drp-noborder .bit-dtrp-icn")).ToHaveCSSAsync("border-top-width", "0px");
        await Expect(Page.Locator("#drp-size-none .bit-dtrp-icn")).ToHaveCSSAsync("border-top-width", "1px");

        await Expect(Page.Locator("#drp-iconstart")).ToHaveClassAsync(HasClass("bit-dtrp-lic"));
        var icon = await BoxAsync(Page.Locator("#drp-iconstart .bit-dtrp-ico"));
        var input = await BoxAsync(Input("drp-iconstart"));
        Assert.IsTrue(icon.X < input.X, "The icon leads the input.");
        var endIcon = await BoxAsync(Page.Locator("#drp-size-none .bit-dtrp-ico"));
        var endInput = await BoxAsync(Input("drp-size-none"));
        Assert.IsTrue(endIcon.X > endInput.X, "By default the icon follows the input.");
    }

    [TestMethod]
    public async Task Appearance_EveryIconByIconAndByIconName()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#drp-size-none .bit-dtrp-ico")).ToHaveClassAsync(HasClass("bit-icon--CalendarMirrored"));
        await Expect(Page.Locator("#drp-iconname .bit-dtrp-ico")).ToHaveClassAsync(HasClass("bit-icon--Airplane"));
        await Expect(Page.Locator("#drp-icon-ext .bit-dtrp-ico")).ToHaveClassAsync(HasClass("e2e-ext-icon"));
        await Expect(Page.Locator("#drp-nav-icons .bit-dtrp-clr i")).ToHaveClassAsync(HasClass("e2e-clear"));
        await Expect(Page.Locator("#drp-nav-iconnames .bit-dtrp-clr i")).ToHaveClassAsync(HasClass("bit-icon--Delete"));

        foreach (var (rootId, icons) in new[]
                 {
                     ("drp-nav-icons", new[] { "e2e-prev-month", "e2e-next-month", "e2e-today", "e2e-prev-year", "e2e-next-year", "e2e-close", "e2e-prev-range", "e2e-next-range" }),
                     ("drp-nav-iconnames", new[] { "bit-icon--ChevronLeft", "bit-icon--ChevronRight", "bit-icon--Home", "bit-icon--Back", "bit-icon--Forward", "bit-icon--ChromeClose", "bit-icon--Rewind", "bit-icon--FastForward" }),
                 })
        {
            var pid = await OpenPickerAsync(rootId);
            var callout = Callout(pid);

            await Expect(callout.Locator(".bit-dtrp-nbt[title='Go to previous month'] i")).ToHaveClassAsync(HasClass(icons[0]));
            await Expect(callout.Locator(".bit-dtrp-nbt[title='Go to next month'] i")).ToHaveClassAsync(HasClass(icons[1]));
            await Expect(callout.Locator(".bit-dtrp-gtb i").First).ToHaveClassAsync(HasClass(icons[2]));
            await Expect(callout.Locator(".bit-dtrp-nbt[title='Go to previous year 2023'] i")).ToHaveClassAsync(HasClass(icons[3]));
            await Expect(callout.Locator(".bit-dtrp-nbt[title='Go to next year 2025'] i")).ToHaveClassAsync(HasClass(icons[4]));
            await Expect(callout.Locator(".bit-dtrp-nbt[title='Close date range picker'] i")).ToHaveClassAsync(HasClass(icons[5]));
            await callout.Locator(".bit-dtrp-mwp .bit-dtrp-ptb").ClickAsync();
            await Expect(callout.Locator(".bit-dtrp-nbt[title^='Previous year range'] i")).ToHaveClassAsync(HasClass(icons[6]));
            await Expect(callout.Locator(".bit-dtrp-nbt[title^='Next year range'] i")).ToHaveClassAsync(HasClass(icons[7]));

            // The toggle that was clicked is gone with the month grid, so the callout is closed through its overlay.
            await Page.Mouse.ClickAsync(1260, 700);
            await Expect(callout).ToBeHiddenAsync();
        }

        foreach (var (rootId, icons) in new[]
                 {
                     ("drp-time-icons", new[] { "e2e-show-time", "e2e-hide-time", "e2e-s-inc-hour", "e2e-s-dec-hour", "e2e-s-inc-minute", "e2e-s-dec-minute", "e2e-e-inc-hour", "e2e-e-dec-hour", "e2e-e-inc-minute", "e2e-e-dec-minute" }),
                     ("drp-time-iconnames", new[] { "bit-icon--Stopwatch", "bit-icon--Calendar", "bit-icon--CaretUp8", "bit-icon--CaretDown8", "bit-icon--CaretUpSolid8", "bit-icon--CaretDownSolid8", "bit-icon--ChevronUp", "bit-icon--ChevronDown", "bit-icon--ChevronUpSmall", "bit-icon--ChevronDownSmall" }),
                 })
        {
            var pid = await OpenPickerAsync(rootId);
            var callout = Callout(pid);

            await Expect(callout.Locator(".bit-dtrp-nbt[title='Show time picker'] i").First).ToHaveClassAsync(HasClass(icons[0]));
            await callout.Locator(".bit-dtrp-nbt[title='Show time picker']").First.ClickAsync();
            await Expect(callout.Locator(".bit-dtrp-nbt[title='Hide time picker'] i")).ToHaveClassAsync(HasClass(icons[1]));

            var titles = new[]
            {
                "Increase start hour", "Decrease start hour", "Increase start minute", "Decrease start minute",
                "Increase end hour", "Decrease end hour", "Increase end minute", "Decrease end minute"
            };

            for (var i = 0; i < titles.Length; i++)
            {
                await Expect(callout.Locator($".bit-dtrp-tbt[title='{titles[i]}'] i")).ToHaveClassAsync(HasClass(icons[i + 2]));
            }

            await Page.Keyboard.PressAsync("Escape");
            await Expect(callout).ToBeHiddenAsync();
        }
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestorTheyRestyleTheFieldAndTheRelocatedCallout()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#drp-vars");
        await Expect(root.Locator(".bit-dtrp-icn")).ToHaveCSSAsync("border-top-left-radius", "13px");
        await Expect(root.Locator(".bit-dtrp-icn")).ToHaveCSSAsync("border-top-color", "rgb(1, 2, 3)");
        await Expect(root.Locator(".bit-dtrp-icn")).ToHaveCSSAsync("height", "44px");
        await Expect(root.Locator(".bit-dtrp-ico")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(root.Locator(".bit-dtrp-lbl")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");

        var pid = await OpenPickerAsync("drp-vars");
        await Page.Mouse.MoveAsync(1, 1);

        Assert.IsFalse(await Callout(pid).EvaluateAsync<bool>("el => !!el.closest('#vars-host')"));
        await Expect(Callout(pid)).ToHaveCSSAsync("border-top-left-radius", "11px");
        await Expect(Callout(pid).Locator(".bit-dtrp-cac")).ToHaveCSSAsync("background-color", "rgb(250, 240, 230)");
        await Expect(Day(pid, 2024, 6, 10)).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(Day(pid, 2024, 6, 10)).ToHaveCSSAsync("color", "rgb(200, 210, 220)");
        await Expect(Day(pid, 2024, 6, 10)).ToHaveCSSAsync("border-top-left-radius", "5px");
        await Expect(Day(pid, 2024, 6, 11)).ToHaveCSSAsync("background-color", "rgb(40, 50, 60)");
        await Expect(Callout(pid).Locator(".bit-dtrp-wnm").First).ToHaveCSSAsync("background-color", "rgb(70, 80, 90)");
    }

    [TestMethod]
    public async Task CssVariables_ColorAndSizeWinStyleAndStylesCalloutReachTheirPicker()
    {
        await OpenAsync(Route);

        var large = await StyleOfAsync(Page.Locator("#drp-size-Large .bit-dtrp-icn"), "height");
        await Expect(Page.Locator("#drp-vars-color .bit-dtrp-icn")).ToHaveCSSAsync("height", large);

        var color = await OpenPickerAsync("drp-vars-color");
        await Page.Mouse.MoveAsync(1, 1);
        await Expect(Day(color, 2024, 6, 10)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-err)"));
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#drp-vars-style .bit-dtrp-icn")).ToHaveCSSAsync("background-color", "rgb(255, 250, 200)");
        var style = await OpenPickerAsync("drp-vars-style");
        await Page.Mouse.MoveAsync(1, 1);
        await Expect(Day(style, 2024, 6, 11)).ToHaveCSSAsync("background-color", "rgb(1, 100, 1)");
        await Page.Keyboard.PressAsync("Escape");

        var roomy = await OpenPickerAsync("drp-vars-callout");
        await Expect(Day(roomy, 2024, 6, 20)).ToHaveCSSAsync("width", "40px");
        Assert.AreEqual(await StyleOfAsync(Page.Locator("#drp-size-none .bit-dtrp-icn"), "height"),
                        await StyleOfAsync(Page.Locator("#drp-vars-callout .bit-dtrp-icn"), "height"),
                        "The field keeps its own size.");
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheRootAndEveryPart()
    {
        await OpenAsync(Route);

        var styled = Page.Locator("#drp-styles");
        await Expect(styled).ToHaveCSSAsync("margin-left", "3px");
        await Expect(styled.Locator(".bit-dtrp-lbl")).ToHaveCSSAsync("color", "rgb(11, 22, 33)");
        await Expect(Input("drp-styles")).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(styled.Locator(".bit-dtrp-icn")).ToHaveCSSAsync("background-color", "rgb(240, 255, 240)");

        await Input("drp-styles").FocusAsync();
        await Expect(styled).ToHaveCSSAsync("background-color", "rgb(255, 0, 255)");

        var pid = await OpenPickerAsync("drp-styles");
        await Expect(Callout(pid)).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(Day(pid, 2024, 6, 3)).ToHaveCSSAsync("font-style", "italic");
        await Expect(Day(pid, Today)).ToHaveCSSAsync("text-transform", "uppercase");
        await Expect(Day(pid, 2024, 6, 10)).ToHaveCSSAsync("outline-color", "rgb(0, 128, 0)");
        await Expect(Day(pid, 2024, 6, 14)).ToHaveCSSAsync("outline-color", "rgb(0, 128, 0)");
        await Expect(Day(pid, 2024, 6, 11)).ToHaveCSSAsync("text-decoration-line", "underline");
        await Expect(Callout(pid).Locator(".bit-dtrp-prb").First).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(Callout(pid).Locator(".bit-dtrp-nbt[title='Go to previous month']")).ToHaveCSSAsync("opacity", "0.5");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();

        var classed = Page.Locator("#drp-classes");
        await Expect(classed).ToHaveClassAsync(HasClass("e2e-c-root"));
        await Expect(classed.Locator(".bit-dtrp-lbl")).ToHaveClassAsync(HasClass("e2e-c-label"));
        await Expect(Input("drp-classes")).ToHaveClassAsync(HasClass("e2e-c-input"));
        await Expect(classed.Locator(".bit-dtrp-icn")).ToHaveClassAsync(HasClass("e2e-c-container"));
        await Expect(classed.Locator(".bit-dtrp-ico")).ToHaveClassAsync(HasClass("e2e-c-icon"));
        await Input("drp-classes").FocusAsync();
        await Expect(classed).ToHaveClassAsync(HasClass("e2e-c-focused"));

        var c = await OpenPickerAsync("drp-classes");
        await Expect(Overlay(c)).ToHaveClassAsync(HasClass("e2e-c-overlay"));
        await Expect(Callout(c)).ToHaveClassAsync(HasClass("e2e-c-callout"));
        await Expect(Callout(c).Locator(".bit-dtrp-prc")).ToHaveClassAsync(HasClass("e2e-c-presets"));
        await Expect(Callout(c).Locator(".bit-dtrp-prb").First).ToHaveClassAsync(HasClass("e2e-c-preset"));
        await Expect(Callout(c).Locator(".bit-dtrp-prb").First).ToHaveClassAsync(HasClass("e2e-c-preset-selected"));
        await Expect(Callout(c).Locator(".bit-dtrp-prb").Nth(1)).Not.ToHaveClassAsync(HasClass("e2e-c-preset-selected"));
        await Expect(Day(c, 2024, 6, 3)).ToHaveClassAsync(HasClass("e2e-c-day"));
        await Expect(Day(c, Today)).ToHaveClassAsync(HasClass("e2e-c-today"));
        await Expect(Day(c, 2024, 6, 10)).ToHaveClassAsync(HasClass("e2e-c-start"));
        await Expect(Day(c, 2024, 6, 10)).ToHaveClassAsync(HasClass("e2e-c-ends"));
        await Expect(Day(c, 2024, 6, 14)).ToHaveClassAsync(HasClass("e2e-c-end"));
        await Expect(Day(c, 2024, 6, 14)).ToHaveClassAsync(HasClass("e2e-c-ends"));
        await Expect(Day(c, 2024, 6, 11)).ToHaveClassAsync(HasClass("e2e-c-between"));
        await Expect(Day(c, 2024, 6, 5)).ToHaveClassAsync(HasClass("e2e-c-highlighted"));
        await Expect(Callout(c).Locator(".bit-dtrp-nbt[title='Go to previous month']")).ToHaveClassAsync(HasClass("e2e-c-prev"));
        await Expect(Callout(c).Locator(".bit-dtrp-nbt[title='Go to next month']")).ToHaveClassAsync(HasClass("e2e-c-next"));
        await Expect(Callout(c).Locator(".bit-dtrp-dwp .bit-dtrp-pkt")).ToHaveClassAsync(HasClass("e2e-c-month"));
        await Expect(Callout(c).Locator(".bit-dtrp-dgh [role=columnheader]").First).ToHaveClassAsync(HasClass("e2e-c-dayname"));
        await Expect(Callout(c).Locator(".bit-dtrp-mwp")).ToHaveClassAsync(HasClass("e2e-c-ym"));
        await Expect(MonthCell(c, 3)).ToHaveClassAsync(HasClass("e2e-c-monthbtn"));
    }

    [TestMethod]
    public async Task Rtl_DirRtlMirrorsTheFieldTheCalloutAndTheArrowKeys()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#drp-rtl");
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));

        var pid = await OpenPickerAsync("drp-rtl");
        await Expect(Callout(pid)).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Callout(pid)).ToHaveClassAsync(HasClass("bit-dtrp-rtl"));

        var field = await BoxAsync(root.Locator(".bit-dtrp-wrp"));
        var callout = await BoxAsync(Callout(pid));
        Assert.AreEqual(field.X + field.Width, callout.X + callout.Width, 2, "The callout ends where the field ends, on the right.");

        await Expect(Day(pid, Today)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day(pid, 2024, 6, 11)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Day(pid, 2024, 6, 12)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");

        var inherited = Page.Locator("#drp-rtl-inherited");
        await Expect(inherited).Not.ToHaveAttributeAsync("dir", "rtl");
        var other = await OpenPickerAsync("drp-rtl-inherited");
        await Expect(Callout(other)).ToHaveCSSAsync("direction", "rtl");
        var otherField = await BoxAsync(inherited.Locator(".bit-dtrp-wrp"));
        var otherCallout = await BoxAsync(Callout(other));
        Assert.AreEqual(otherField.X + otherField.Width, otherCallout.X + otherCallout.Width, 2, "Inherited, the callout is aligned to the right too.");
        await Expect(Day(other, Today)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day(other, 2024, 6, 11)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Cascade_BitParamsHandsItsDefaultsToThePickersInsideIt()
    {
        await OpenAsync(Route);

        await Expect(Input("drp-casc-1")).ToHaveAttributeAsync("placeholder", "From the cascade");
        await Expect(Input("drp-casc-2")).ToHaveAttributeAsync("placeholder", "Own");
        await Expect(Input("drp-casc-out")).ToHaveAttributeAsync("placeholder", "");

        var first = await OpenPickerAsync("drp-casc-1");
        await Expect(Callout(first).Locator(".bit-dtrp-dwp")).ToHaveCountAsync(2);
        await Expect(Callout(first).Locator(".bit-dtrp-prb")).ToHaveTextAsync("Cascaded preset");
        await Page.Keyboard.PressAsync("Escape");

        var second = await OpenPickerAsync("drp-casc-2");
        await Expect(Callout(second).Locator(".bit-dtrp-dwp")).ToHaveCountAsync(1);
        await Expect(Callout(second).Locator(".bit-dtrp-prb")).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("Escape");

        var outside = await OpenPickerAsync("drp-casc-out");
        await Expect(Callout(outside).Locator(".bit-dtrp-dwp")).ToHaveCountAsync(1);
        await Expect(Callout(outside).Locator(".bit-dtrp-prb")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Motion_TheEntryAnimationCollapsesUnderReducedMotionUnlessForced()
    {
        static double Seconds(string value) => value.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(value[..^2], CultureInfo.InvariantCulture) / 1000
            : double.Parse(value.TrimEnd('s'), CultureInfo.InvariantCulture);

        await OpenAsync(Route);

        var normal = await PickerIdAsync("drp-motion");
        Assert.IsTrue(Seconds(await StyleOfAsync(Callout(normal), "animation-duration")) >= 0.1, "The callout animates in.");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        var reduced = await OpenPickerAsync("drp-motion");
        Assert.IsTrue(Seconds(await StyleOfAsync(Callout(reduced), "animation-duration")) < 0.001, "Reduced motion collapses the entry animation.");
        await Page.Keyboard.PressAsync("Escape");

        var forced = await OpenPickerAsync("drp-motion-force");
        await Expect(Page.Locator("#drp-motion-force")).ToHaveClassAsync(HasClass("bit-fam"));
        Assert.IsTrue(Seconds(await StyleOfAsync(Callout(forced), "animation-duration")) >= 0.1, "ForceAnimation brings the animation back.");
    }
}
