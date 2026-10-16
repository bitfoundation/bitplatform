using System.Diagnostics;
using System.Globalization;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitDropMenu (/e2e/drop-menu) in a real browser: the prerendered markup, opening and closing by
/// click, keyboard and hover (with its delays), the callout really relocated, positioned (Placement, Alignment, Gap,
/// DropDirection), sized (Width, MinWidth, MaxWidth, MaxHeight, MatchWidth) and painted (Background, Border, NoShadow),
/// the responsive panel and its swipe at a phone width, the two-way IsOpen, DefaultIsOpen and the Open/Close/Toggle
/// methods, OnClick/OnOpen/OnDismiss, AutoClose, LazyRender, IsLoading, AutoFocus and TrapFocus, Tab out, Escape inside a
/// dialog, the disabled contract, every Color, Variant and Size, the external icons, Style/Class/Styles/Classes,
/// Visibility, HtmlAttributes, the BitParams cascade, the public css variables, RTL (explicit and inherited), reduced
/// motion and ForceAnimation, a page scroll dismissing it and an unmount while open.
/// </summary>
[TestClass]
public class BitDropMenuE2ETests : E2ETestBase
{
    private const string Url = "/e2e/drop-menu";

    private static readonly string[] Colors =
    [
        "Primary", "Secondary", "Tertiary", "Info", "Success", "Warning", "SevereWarning", "Error",
        "PrimaryBackground", "SecondaryBackground", "TertiaryBackground", "PrimaryForeground", "SecondaryForeground",
        "TertiaryForeground", "PrimaryBorder", "SecondaryBorder", "TertiaryBorder"
    ];

    // The class suffix (BitCssClasses.Color), the main and the on token of each color (color-role-maps.scss).
    private static readonly Dictionary<string, (string Suffix, string Main, string On)> ColorTokens = new()
    {
        ["Primary"] = ("pri", "--bit-clr-pri", "--bit-clr-pri-text"),
        ["Secondary"] = ("sec", "--bit-clr-sec", "--bit-clr-sec-text"),
        ["Tertiary"] = ("ter", "--bit-clr-ter", "--bit-clr-ter-text"),
        ["Info"] = ("inf", "--bit-clr-inf", "--bit-clr-inf-text"),
        ["Success"] = ("suc", "--bit-clr-suc", "--bit-clr-suc-text"),
        ["Warning"] = ("wrn", "--bit-clr-wrn", "--bit-clr-wrn-text"),
        ["SevereWarning"] = ("swr", "--bit-clr-swr", "--bit-clr-swr-text"),
        ["Error"] = ("err", "--bit-clr-err", "--bit-clr-err-text"),
        ["PrimaryBackground"] = ("pbg", "--bit-clr-bg-pri", "--bit-clr-fg-pri"),
        ["SecondaryBackground"] = ("sbg", "--bit-clr-bg-sec", "--bit-clr-fg-pri"),
        ["TertiaryBackground"] = ("tbg", "--bit-clr-bg-ter", "--bit-clr-fg-pri"),
        ["PrimaryForeground"] = ("pfg", "--bit-clr-fg-pri", "--bit-clr-bg-pri"),
        ["SecondaryForeground"] = ("sfg", "--bit-clr-fg-sec", "--bit-clr-bg-pri"),
        ["TertiaryForeground"] = ("tfg", "--bit-clr-fg-ter", "--bit-clr-bg-pri"),
        ["PrimaryBorder"] = ("pbr", "--bit-clr-brd-pri", "--bit-clr-fg-pri"),
        ["SecondaryBorder"] = ("sbr", "--bit-clr-brd-sec", "--bit-clr-fg-pri"),
        ["TertiaryBorder"] = ("tbr", "--bit-clr-brd-ter", "--bit-clr-fg-pri"),
    };

    private const string Transparent = "rgba(0, 0, 0, 0)";

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheDropMenuBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"dm-basic\"");
        StringAssert.Contains(html, "bit-drm-btn");
        StringAssert.Contains(html, "Quick settings");
        StringAssert.Contains(html, "aria-haspopup=\"dialog\"");
        StringAssert.Contains(html, ">Loading<");

        await OpenAsync(Url);
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Basic_ClickingTheButton_OpensTheRelocatedCalloutUnderItAndFiresOnClickAndOnOpen()
    {
        await OpenAsync(Url);

        var callout = await CalloutAsync("dm-basic");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Button("dm-basic")).ToHaveAttributeAsync("aria-expanded", "false");

        await OpenMenuAsync("dm-basic");

        await Expect(Root("dm-basic")).ToHaveClassAsync(HasClass("bit-drm-omn"));
        await Expect(callout).ToHaveClassAsync(HasClass("bit-drm-ocl"));
        await Expect(Root("dm-basic").Locator(".bit-drm-chv")).ToHaveCSSAsync("rotate", "180deg");
        await Expect(callout.Locator("#basic-content")).ToBeVisibleAsync();

        // Relocated out of the section it is rendered in, to the end of the body.
        Assert.IsTrue(await callout.EvaluateAsync<bool>("el => el.closest('#basic') === null"));

        await WaitSettledAsync(callout);
        var root = await BoxAsync(Root("dm-basic"));
        var box = await BoxAsync(callout);
        Assert.AreEqual(root.X, box.X, 2, "The callout is aligned with the start of the button.");
        AssertBetween(box.Y - (root.Y + root.Height), -1, 3, "The callout is tucked under the button.");
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "below");

        await Expect(Page.Locator("#basic-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-opens")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-dismisses")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Basic_ClickingOutside_ClosesTheCalloutThroughTheOverlayAndFiresOnDismiss()
    {
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-basic");

        // The overlay covers the page while the callout is open, so the click closes the callout and never reaches
        // the button under it.
        var target = await BoxAsync(Page.Locator("#outside-target"));
        await Page.Mouse.ClickAsync(target.X + target.Width / 2, target.Y + target.Height / 2);

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Button("dm-basic")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Root("dm-basic")).Not.ToHaveClassAsync(HasClass("bit-drm-omn"));
        await Expect(Page.Locator("#basic-dismisses")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#outside-clicks")).ToHaveTextAsync("0");

        await Page.Locator("#outside-target").ClickAsync();
        await Expect(Page.Locator("#outside-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Basic_TheContentOfTheCallout_IsLiveAndKeepsItOpenWithoutAutoClose()
    {
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-basic");

        await callout.Locator("#basic-input").FillAsync("typed");
        await callout.Locator("#basic-save").ClickAsync();

        await Expect(Page.Locator("#basic-saves")).ToHaveTextAsync("1");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(callout.Locator("#basic-input")).ToHaveValueAsync("typed");
        await Expect(Page.Locator("#basic-dismisses")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Basic_OpeningAnotherDropMenu_ClosesTheFirstOne()
    {
        await OpenAsync(Url);

        var first = await OpenMenuAsync("dm-basic");

        // The keyboard reaches the second trigger past the overlay that would take a click.
        await Button("dm-second").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        var second = await CalloutAsync("dm-second");
        await Expect(second).ToBeVisibleAsync();
        await Expect(first).ToBeHiddenAsync();
        await Expect(Button("dm-basic")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#basic-dismisses")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Disabled_ARealClickDoesNotOpenIt_AndThePagesActivationHandlersAreDropped()
    {
        await OpenAsync(Url);

        var root = Root("dm-disabled");
        var button = Button("dm-disabled");

        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");

        var box = await BoxAsync(button);
        await Page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);

        // The click an assistive technology dispatches, and a press, reach the root and must not run the page's handlers.
        await root.DispatchEventAsync("dblclick");
        await root.DispatchEventAsync("pointerdown");
        // A hover is not an activation, so it is kept; it is also what tells the events above were processed.
        await root.DispatchEventAsync("mouseover");

        await Expect(Page.Locator("#disabled-hovers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-activations")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("0");
        await Expect(await CalloutAsync("dm-disabled")).ToBeHiddenAsync();

        await Page.Locator("#btn-toggle-disabled").ClickAsync();

        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToBeEnabledAsync();
        await Expect(button).Not.ToHaveAttributeAsync("aria-disabled", "true");

        await root.DispatchEventAsync("dblclick");
        await Expect(Page.Locator("#disabled-activations")).ToHaveTextAsync("1");

        await OpenMenuAsync("dm-disabled");
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Disabled_TurningItOnWhileOpen_ClosesTheCallout()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(Page.Locator("#disabled-state")).ToHaveTextAsync("False");

        var callout = await OpenMenuAsync("dm-disabled");

        // The page changes the parameter past the overlay.
        await Page.Locator("#btn-toggle-disabled").DispatchEventAsync("click");

        await Expect(Page.Locator("#disabled-state")).ToHaveTextAsync("True");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Root("dm-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
    }

    [TestMethod]
    public async Task FullWidth_StretchesTheButtonToItsContainer()
    {
        await OpenAsync(Url);

        await Expect(Root("dm-full-width")).ToHaveClassAsync(HasClass("bit-drm-flw"));

        var container = await BoxAsync(Page.Locator("#full-width-box"));
        var full = await BoxAsync(Root("dm-full-width"));
        var fit = await BoxAsync(Root("dm-fit-width"));

        Assert.AreEqual(container.Width, full.Width, 1);
        Assert.IsTrue(fit.Width < container.Width / 2, $"A drop menu without FullWidth fits its content ({fit.Width}px).");
    }

    [TestMethod]
    public async Task Icons_IconNameChevronDownIconNameNoChevronAndAnIconOnlyButton()
    {
        await OpenAsync(Url);

        await Expect(Root("dm-icon").Locator(".bit-drm-icn")).ToHaveClassAsync(HasClass("bit-icon--Share"));
        await Expect(Root("dm-icon").Locator(".bit-drm-icn")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Root("dm-icon").Locator(".bit-drm-chv")).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        await Expect(Root("dm-icon").Locator(".bit-drm-chv")).ToHaveClassAsync(HasClass("bit-ico-r90"));

        await Expect(Root("dm-chevron").Locator(".bit-drm-chv")).ToHaveClassAsync(HasClass("bit-icon--DoubleChevronDown"));
        await Expect(Root("dm-chevron").Locator(".bit-drm-icn")).ToHaveCountAsync(0);

        await Expect(Root("dm-no-chevron").Locator(".bit-drm-chv")).ToHaveCountAsync(0);
        await Expect(Root("dm-no-chevron").Locator(".bit-drm-icn")).ToHaveClassAsync(HasClass("bit-icon--Contact"));

        // An icon-only button is squared off to the control height and named through AriaLabel.
        await Expect(Root("dm-icon-only")).ToHaveClassAsync(HasClass("bit-drm-ion"));
        await Expect(Root("dm-icon")).Not.ToHaveClassAsync(HasClass("bit-drm-ion"));
        await Expect(Button("dm-icon-only")).ToHaveAttributeAsync("aria-label", "More actions");
        await Expect(Button("dm-icon-only")).ToHaveAttributeAsync("title", "More actions tooltip");
        await Expect(Root("dm-icon-only").Locator(".bit-drm-txt")).ToHaveCountAsync(0);

        var square = await BoxAsync(Root("dm-icon-only"));
        Assert.AreEqual(square.Height, square.Width, 2, "An icon-only drop menu is a square.");

        // The icon actually renders a glyph of the icon font.
        var font = await ComputedStyleAsync("#dm-icon .bit-drm-icn", "font-family");
        StringAssert.Contains(font.ToLowerInvariant(), "fabric");
    }

    [TestMethod]
    public async Task Template_ReplacesTheButtonContent_AndBodyFillsTheCallout()
    {
        await OpenAsync(Url);

        await Expect(Button("dm-template").Locator("#template-trigger")).ToHaveTextAsync("Flight BA 117");
        await Expect(Root("dm-template").Locator(".bit-drm-txt")).ToHaveCountAsync(0);
        await Expect(Root("dm-template").Locator(".bit-drm-chv")).ToHaveCountAsync(0);
        await Expect(Root("dm-template")).Not.ToHaveClassAsync(HasClass("bit-drm-ion"));

        var callout = await OpenMenuAsync("dm-template");
        await Expect(callout.Locator("#template-body")).ToHaveTextAsync("Departs 18:40 from gate B22");
    }

    [TestMethod]
    [DataRow("dm-variant-none", "", "main", "on", "transparent")]
    [DataRow("dm-variant-fill", "bit-drm-fil", "main", "on", "main")]
    [DataRow("dm-variant-outline", "bit-drm-otl", "transparent", "main", "main")]
    [DataRow("dm-variant-text", "bit-drm-tex", "transparent", "main", "transparent")]
    [DataRow("dm-transparent", "bit-drm-trn", "transparent", "main", "transparent")]
    public async Task Variant_EveryLook_PaintsThePrimaryColorItsOwnWay(string id, string cls, string background, string color, string border)
    {
        await OpenAsync(Url);

        var root = Root(id);
        if (cls.Length > 0) await Expect(root).ToHaveClassAsync(HasClass(cls));
        foreach (var other in new[] { "bit-drm-fil", "bit-drm-otl", "bit-drm-tex" }.Where(c => c != cls))
        {
            await Expect(root).Not.ToHaveClassAsync(HasClass(other));
        }

        var main = await ResolveColorAsync("var(--bit-clr-pri)");
        var on = await ResolveColorAsync("var(--bit-clr-pri-text)");
        string Expected(string slot) => slot switch { "main" => main, "on" => on, _ => Transparent };

        await Expect(root).ToHaveCSSAsync("background-color", Expected(background));
        await Expect(root).ToHaveCSSAsync("color", Expected(color));
        await Expect(root).ToHaveCSSAsync("border-top-color", Expected(border));
    }

    [TestMethod]
    public async Task Variant_ChangedAtRuntime_ReRendersTheLook()
    {
        await OpenAsync(Url);

        var root = Root("dm-variant-runtime");
        await Expect(root).ToHaveClassAsync(HasClass("bit-drm-fil"));
        var main = await ResolveColorAsync("var(--bit-clr-pri)");
        await Expect(root).ToHaveCSSAsync("background-color", main);

        await Page.Locator("#btn-variant-outline").ClickAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-drm-otl"));
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-drm-fil"));
        await Expect(root).ToHaveCSSAsync("background-color", Transparent);

        await Page.Locator("#btn-variant-text").ClickAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-drm-tex"));
        await Expect(root).ToHaveCSSAsync("border-top-color", Transparent);
    }

    [TestMethod]
    public async Task IsLoading_ShowsASpinner_KeepsTheFocus_AndKeepsTheCalloutClosed()
    {
        await OpenAsync(Url);

        var root = Root("dm-loading");
        var button = Button("dm-loading");

        await Expect(root.Locator(".bit-drm-icn")).ToHaveCountAsync(1);
        await Page.Locator("#btn-toggle-loading").ClickAsync();

        await Expect(root).ToHaveClassAsync(HasClass("bit-drm-ldg"));
        await Expect(root.Locator(".bit-drm-spn")).ToBeVisibleAsync();
        await Expect(root.Locator(".bit-drm-icn")).ToHaveCountAsync(0);
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        // Not the native attribute, which would drop the focus to the body.
        Assert.IsNull(await button.GetAttributeAsync("disabled"), "A loading button is aria-disabled only.");

        // The spinner turns.
        var animation = await ComputedStyleAsync("#dm-loading .bit-drm-spn", "animation-name");
        Assert.AreNotEqual("none", animation);

        await button.FocusAsync();
        await Expect(button).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(button).ToBeFocusedAsync();
        await Expect(Page.Locator("#loading-open")).ToHaveTextAsync("False");
        await Expect(await CalloutAsync("dm-loading")).ToBeHiddenAsync();

        await Page.Locator("#btn-toggle-loading").ClickAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-drm-ldg"));
        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", "true");
    }

    [TestMethod]
    public async Task IsLoading_SetWhileOpen_ClosesTheCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-loading");
        await Expect(Page.Locator("#loading-open")).ToHaveTextAsync("True");

        await Page.Locator("#btn-toggle-loading").DispatchEventAsync("click");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#loading-open")).ToHaveTextAsync("False");
        await Expect(Root("dm-loading")).ToHaveClassAsync(HasClass("bit-drm-ldg"));
    }

    [TestMethod]
    public async Task LazyRender_KeepsTheContentOutUntilTheFirstOpening_AndKeepsItsStateAfterAClose()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#lazy-content")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#eager-content")).ToHaveCountAsync(1);

        var callout = await OpenMenuAsync("dm-lazy");
        await Expect(callout.Locator("#lazy-content")).ToBeVisibleAsync();
        await callout.Locator("#lazy-check").CheckAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#lazy-content")).ToHaveCountAsync(1);

        await OpenMenuAsync("dm-lazy");
        await Expect(callout.Locator("#lazy-check")).ToBeCheckedAsync();
    }

    [TestMethod]
    [DataRow("Primary", "bit-drm-bpg", "var(--bit-clr-bg-pri)")]
    [DataRow("Secondary", "bit-drm-bsg", "var(--bit-clr-bg-sec)")]
    [DataRow("Tertiary", "bit-drm-btg", "var(--bit-clr-bg-ter)")]
    [DataRow("Transparent", "bit-drm-brg", "transparent")]
    public async Task Background_EveryColorKind_PaintsTheCallout(string kind, string cls, string expected)
    {
        await OpenAsync(Url);

        var callout = await OpenMenuAsync($"dm-bg-{kind}");

        await Expect(callout).ToHaveClassAsync(HasClass(cls));
        await Expect(callout).ToHaveCSSAsync("background-color", await ResolveColorAsync(expected));
    }

    [TestMethod]
    [DataRow("Primary", "bit-drm-bpr", "var(--bit-clr-brd-pri)")]
    [DataRow("Secondary", "bit-drm-bsr", "var(--bit-clr-brd-sec)")]
    [DataRow("Tertiary", "bit-drm-btr", "var(--bit-clr-brd-ter)")]
    [DataRow("Transparent", "bit-drm-brr", "transparent")]
    public async Task Border_EveryColorKind_DrawsABorderAroundTheCallout(string kind, string cls, string expected)
    {
        await OpenAsync(Url);

        var plain = await OpenMenuAsync("dm-bg-default");
        await Expect(plain).ToHaveCSSAsync("border-top-width", "0px");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(plain).ToBeHiddenAsync();

        var callout = await OpenMenuAsync($"dm-border-{kind}");

        await Expect(callout).ToHaveClassAsync(HasClass("bit-drm-brd"));
        await Expect(callout).ToHaveClassAsync(HasClass(cls));
        Assert.AreNotEqual("0px", await callout.EvaluateAsync<string>("el => getComputedStyle(el).borderTopWidth"));
        await Expect(callout).ToHaveCSSAsync("border-top-color", await ResolveColorAsync(expected));
    }

    [TestMethod]
    public async Task NoShadow_FlattensTheCallout()
    {
        await OpenAsync(Url);

        var plain = await OpenMenuAsync("dm-bg-default");
        Assert.AreNotEqual("none", await plain.EvaluateAsync<string>("el => getComputedStyle(el).boxShadow"));
        await Page.Keyboard.PressAsync("Escape");
        await Expect(plain).ToBeHiddenAsync();

        var flat = await OpenMenuAsync("dm-no-shadow");
        await Expect(flat).ToHaveClassAsync(HasClass("bit-drm-nsh"));
        await Expect(flat).ToHaveCSSAsync("box-shadow", "none");
    }

    [TestMethod]
    public async Task Sizing_MatchWidth_MakesTheCalloutAtLeastAsWideAsTheButton()
    {
        await OpenAsync(Url);

        var matched = await OpenMenuAsync("dm-match-width");
        await WaitSettledAsync(matched);
        var root = await BoxAsync(Root("dm-match-width"));
        var box = await BoxAsync(matched);
        Assert.IsTrue(box.Width >= root.Width - 1, $"MatchWidth: callout {box.Width}px, button {root.Width}px.");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(matched).ToBeHiddenAsync();

        var unmatched = await OpenMenuAsync("dm-no-match-width");
        var other = await BoxAsync(Root("dm-no-match-width"));
        var otherBox = await BoxAsync(unmatched);
        Assert.IsTrue(otherBox.Width < other.Width / 2, $"Without MatchWidth the callout fits its content ({otherBox.Width}px).");
    }

    [TestMethod]
    public async Task Sizing_WidthMinWidthAndMaxWidth_SizeTheCallout()
    {
        await OpenAsync(Url);

        var width = await OpenMenuAsync("dm-width");
        Assert.AreEqual(256, (await BoxAsync(width)).Width, 1, "Width=16rem.");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(width).ToBeHiddenAsync();

        var min = await OpenMenuAsync("dm-min-width");
        Assert.IsTrue((await BoxAsync(min)).Width >= 255, "MinWidth=16rem keeps a short content's callout 16rem wide.");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(min).ToBeHiddenAsync();

        var max = await OpenMenuAsync("dm-max-width");
        var maxBox = await BoxAsync(max);
        Assert.IsTrue(maxBox.Width <= 257, $"MaxWidth=16rem caps the callout ({maxBox.Width}px).");
        Assert.IsTrue(maxBox.Height > 40, "The long content wraps onto several lines instead.");
    }

    [TestMethod]
    public async Task Sizing_MaxHeight_CapsTheCalloutAndScrollsTheRest()
    {
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-max-height");

        await Expect(callout).ToHaveClassAsync(HasClass("bit-drm-mxh"));
        Assert.IsTrue((await BoxAsync(callout)).Height <= 161, "MaxHeight=10rem.");
        Assert.IsTrue(await callout.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight + 10"), "The rest of the content scrolls.");

        await callout.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Expect(callout.GetByText("Item 20", new() { Exact = true })).ToBeInViewportAsync();
        await Expect(callout).ToBeVisibleAsync();
    }

    [TestMethod]
    [DataRow("top", "above")]
    [DataRow("bottom", "below")]
    [DataRow("start", "left")]
    [DataRow("end", "right")]
    public async Task Placement_EverySide_PlacesTheCalloutThereWithTheGap(string side, string position)
    {
        await OpenAsync(Url);

        await Page.Locator($"#btn-place-{side}").ClickAsync();
        await Expect(Page.Locator("#placement-state")).ToContainTextAsync(side, new() { IgnoreCase = true });

        var callout = await OpenMenuAsync("dm-placement");
        await WaitSettledAsync(callout);

        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", position);

        var root = await BoxAsync(Root("dm-placement"));
        var box = await BoxAsync(callout);

        // Gap="8", plus the pixel the placement always leaves.
        var distance = side switch
        {
            "top" => root.Y - (box.Y + box.Height),
            "bottom" => box.Y - (root.Y + root.Height),
            "start" => root.X - (box.X + box.Width),
            _ => box.X - (root.X + root.Width)
        };
        AssertBetween(distance, 7, 11, $"The gap on the {side} side.");
    }

    [TestMethod]
    [DataRow("start")]
    [DataRow("center")]
    [DataRow("end")]
    public async Task Alignment_LinesTheCalloutUpWithTheButton(string alignment)
    {
        await OpenAsync(Url);

        await Page.Locator($"#btn-align-{alignment}").ClickAsync();
        await Expect(Page.Locator("#placement-state")).ToContainTextAsync(alignment, new() { IgnoreCase = true });

        var callout = await OpenMenuAsync("dm-placement");
        await WaitSettledAsync(callout);

        var root = await BoxAsync(Root("dm-placement"));
        var box = await BoxAsync(callout);

        switch (alignment)
        {
            case "start":
                Assert.AreEqual(root.X, box.X, 2);
                break;
            case "center":
                Assert.AreEqual(root.X + root.Width / 2, box.X + box.Width / 2, 2);
                break;
            default:
                Assert.AreEqual(root.X + root.Width, box.X + box.Width, 2);
                break;
        }
    }

    [TestMethod]
    public async Task DropDirection_All_PlacesATallCalloutBesideTheButton_TopAndBottomNever()
    {
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-drop-direction");
        await WaitSettledAsync(callout);
        var position = await callout.GetAttributeAsync("data-bit-cal-pos");
        Assert.IsTrue(position is "below" or "above", $"TopAndBottom keeps it above or below ({position}).");
        // Capped to the room the viewport leaves and scrollable.
        var viewport = Page.ViewportSize!;
        var box = await BoxAsync(callout);
        Assert.IsTrue(box.Y >= -1 && box.Y + box.Height <= viewport.Height + 1, "The callout stays within the viewport.");
        Assert.IsTrue(await callout.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"), "Its content scrolls.");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        await Page.Locator("#btn-drop-all").ClickAsync();
        await Expect(Page.Locator("#drop-direction-state")).ToHaveTextAsync("All");

        await OpenMenuAsync("dm-drop-direction");
        await WaitSettledAsync(callout);
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "right");
        var root = await BoxAsync(Root("dm-drop-direction"));
        var beside = await BoxAsync(callout);
        Assert.IsTrue(beside.X >= root.X + root.Width - 1, "Placed to the right of the button.");
    }

    [TestMethod]
    public async Task OpenOnHover_ThePointerOpensItAndLeavingClosesItAfterTheDefaultDelay()
    {
        await OpenAsync(Url);
        await CenterAsync("#hover");

        var away = await BoxAsync(Page.Locator("#hover-away"));
        var callout = await CalloutAsync("dm-hover");

        await Button("dm-hover").HoverAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#hover-opens")).ToHaveTextAsync("1");
        // A callout the pointer opened is not modal and leaves the focus alone.
        await Expect(callout).Not.ToHaveAttributeAsync("aria-modal", "true");

        // Moving into the callout keeps it open.
        var box = await BoxAsync(callout);
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2, new() { Steps = 4 });
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Button("dm-hover")).ToHaveAttributeAsync("aria-expanded", "true");

        // Leaving both closes it, through the overlay, which takes no pointer in this mode.
        await Page.Mouse.MoveAsync(away.X + away.Width - 5, away.Y + away.Height / 2, new() { Steps = 4 });
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#hover-dismisses")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task OpenOnHover_HoverOpenDelayAndHoverCloseDelay_AreWaitedOut()
    {
        await OpenAsync(Url);
        await CenterAsync("#hover");

        var away = await BoxAsync(Page.Locator("#hover-away"));
        var callout = await CalloutAsync("dm-hover-delay");

        var watch = Stopwatch.StartNew();
        await Button("dm-hover-delay").HoverAsync();
        Assert.AreEqual("false", await Button("dm-hover-delay").GetAttributeAsync("aria-expanded"), "Not open as soon as the pointer arrives.");
        await Expect(callout).ToBeVisibleAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds >= 550, $"HoverOpenDelay=600 (opened after {watch.ElapsedMilliseconds}ms).");

        await Page.Mouse.MoveAsync(away.X + away.Width - 5, away.Y + away.Height / 2);
        watch.Restart();
        await Expect(callout).ToBeVisibleAsync();
        Assert.AreEqual("true", await Button("dm-hover-delay").GetAttributeAsync("aria-expanded"), "Still open as soon as the pointer leaves.");
        await Expect(callout).ToBeHiddenAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds >= 800, $"HoverCloseDelay=900 (closed after {watch.ElapsedMilliseconds}ms).");
    }

    [TestMethod]
    public async Task OpenOnHover_EscapeClosesAHoverOpenedCalloutWhereverTheFocusIs()
    {
        await OpenAsync(Url);
        await CenterAsync("#hover");

        await Page.Locator("#hover-elsewhere").ClickAsync();
        await Expect(Page.Locator("#hover-elsewhere")).ToBeFocusedAsync();

        var callout = await CalloutAsync("dm-hover");
        await Button("dm-hover").HoverAsync();
        await Expect(callout).ToBeVisibleAsync();
        // OnOpen is raised once the callout's keyboard handling is in place, the Escape of a hover-opened one included.
        await Expect(Page.Locator("#hover-opens")).ToHaveTextAsync("1");
        // The pointer passing over the button does not pull the focus out of the field.
        await Expect(Page.Locator("#hover-elsewhere")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#hover-elsewhere")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task OpenOnHover_AnArrowKeyHandsTheCalloutToTheKeyboard_WhichThePointerLeavingNoLongerCloses()
    {
        await OpenAsync(Url);
        await CenterAsync("#hover");

        var away = await BoxAsync(Page.Locator("#hover-away"));
        var callout = await CalloutAsync("dm-hover");

        await Button("dm-hover").HoverAsync();
        await Expect(callout).ToBeVisibleAsync();

        await Button("dm-hover").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Page.Locator("#hover-link")).ToBeFocusedAsync();

        await Page.Mouse.MoveAsync(away.X + away.Width - 5, away.Y + away.Height / 2);
        // Longer than the default HoverCloseDelay, waited for on the page's own clock.
        await Page.WaitForFunctionAsync("() => new Promise(r => setTimeout(() => r(true), 400))");
        await Expect(callout).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Button("dm-hover")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task IsOpen_TwoWayBinding_FollowsThePageAndTheUser()
    {
        await OpenAsync(Url);

        var callout = await CalloutAsync("dm-bound");

        await CenterAsync("#dm-bound");
        await Page.Locator("#btn-bound-open").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Button("dm-bound")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Root("dm-bound")).ToContainTextAsync("IsOpen: True");

        // Closed by the user, the bound value follows.
        await Button("dm-bound").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#bound-state")).ToHaveTextAsync("False");

        // Opened by the user, the bound value follows.
        await Button("dm-bound").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#bound-state")).ToHaveTextAsync("True");

        // Closed by the page.
        await Page.Locator("#btn-bound-close").DispatchEventAsync("click");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Root("dm-bound")).ToContainTextAsync("IsOpen: False");
    }

    [TestMethod]
    public async Task IsOpen_OneWay_TheCalloutStaysOpenUntilThePageClosesIt()
    {
        await OpenAsync(Url);

        var callout = await CalloutAsync("dm-one-way");
        await CenterAsync("#dm-one-way");

        await Page.Locator("#btn-one-way-toggle").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();

        // Nothing changes the parent's value, so an outside click does not close it.
        await Page.Mouse.ClickAsync(5, 5);
        await Page.Locator("#btn-one-way-toggle").DispatchEventAsync("mouseover");
        await NextFramesAsync();
        await Expect(callout).ToBeVisibleAsync();

        await Page.Locator("#btn-one-way-toggle").DispatchEventAsync("click");
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task PublicMethods_OpenCloseAndToggle_DriveTheCallout()
    {
        await OpenAsync(Url);

        var callout = await CalloutAsync("dm-ref");
        await CenterAsync("#dm-ref");

        await Page.Locator("#btn-ref-open").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#ref-counts")).ToHaveTextAsync("1/0");

        await Page.Locator("#btn-ref-close").DispatchEventAsync("click");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#ref-counts")).ToHaveTextAsync("1/1");

        await Page.Locator("#btn-ref-toggle").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#ref-counts")).ToHaveTextAsync("2/1");

        await Page.Locator("#btn-ref-toggle").DispatchEventAsync("click");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#ref-counts")).ToHaveTextAsync("2/2");

        // Closing a closed drop menu dismisses nothing.
        await Page.Locator("#btn-ref-close").ClickAsync();
        await Page.Locator("#btn-ref-open").ClickAsync();
        await Expect(Page.Locator("#ref-counts")).ToHaveTextAsync("3/2");
    }

    [TestMethod]
    public async Task DefaultIsOpen_AMountedDropMenuStartsOpen_AndCanThenBeClosed()
    {
        await OpenAsync(Url);
        await CenterAsync("#btn-mount-default-open");

        await Page.Locator("#btn-mount-default-open").ClickAsync();

        var callout = await CalloutAsync("dm-default-open");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Button("dm-default-open")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(callout.Locator("#default-open-content")).ToHaveTextAsync("Starts open");

        await Page.Mouse.ClickAsync(5, 5);
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Button("dm-default-open")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task AutoClose_AClickInsideClosesTheCallout_WithoutItTheCalloutStays()
    {
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-auto-close");
        await callout.Locator("#auto-rename").ClickAsync();
        await Expect(Page.Locator("#auto-close-action")).ToHaveTextAsync("Renamed");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Root("dm-auto-close")).ToContainTextAsync("AutoClose (Renamed)");

        var stays = await OpenMenuAsync("dm-no-auto-close");
        await stays.Locator("#no-auto-check").CheckAsync();
        await Expect(stays).ToBeVisibleAsync();
        await Expect(stays.Locator("#no-auto-check")).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Accessibility_TheButtonAnnouncesTheDialogItOpens()
    {
        await OpenAsync(Url);

        var button = Button("dm-keyboard");
        var calloutId = await button.GetAttributeAsync("aria-controls");
        var buttonId = await button.GetAttributeAsync("id");

        await Expect(button).ToHaveAttributeAsync("aria-haspopup", "dialog");
        await Expect(button).ToHaveAttributeAsync("type", "button");
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "false");

        var callout = Page.Locator($"[id='{calloutId}']");
        await Expect(callout).ToHaveAttributeAsync("role", "dialog");
        await Expect(callout).ToHaveAttributeAsync("aria-labelledby", buttonId!);
        await Expect(callout).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(callout).Not.ToHaveAttributeAsync("aria-modal", "true");
        await Expect(Page.Locator($"[id='{calloutId!.Replace("-callout", "-overlay")}']")).ToHaveAttributeAsync("aria-hidden", "true");

        await Expect(Button("dm-aria")).ToHaveAttributeAsync("aria-label", "Filters");
        await Expect(Button("dm-aria")).ToHaveAttributeAsync("title", "Filters title");
        var describedBy = await Button("dm-aria").GetAttributeAsync("aria-describedby");
        await Expect(Page.Locator($"[id='{describedBy}']")).ToHaveTextAsync("Narrows the list below");
        // Visually hidden, still read.
        var description = await BoxAsync(Page.Locator($"[id='{describedBy}']"));
        Assert.IsTrue(description.Width <= 1 && description.Height <= 1);
        await Expect(Button("dm-keyboard")).Not.ToHaveAttributeAsync("aria-describedby", new System.Text.RegularExpressions.Regex(".+"));

        await Expect(Button("dm-aria-hidden")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Button("dm-aria-hidden")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Button("dm-tabindex")).ToHaveAttributeAsync("tabindex", "3");
    }

    [TestMethod]
    [DataRow("Enter")]
    [DataRow(" ")]
    [DataRow("ArrowDown")]
    public async Task Keyboard_EnterSpaceAndArrowDown_OpenTheCalloutAndMoveTheFocusIn_EscapeReturnsIt(string key)
    {
        await OpenAsync(Url);
        await CenterAsync("#dm-keyboard");

        await Page.Locator("#before-keyboard").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Button("dm-keyboard")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync(key);

        var callout = await CalloutAsync("dm-keyboard");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#kb-first")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Button("dm-keyboard")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_TabPastTheLastElement_ClosesTheCalloutAndCarriesOnAfterTheButton()
    {
        await OpenAsync(Url);
        await CenterAsync("#dm-keyboard");

        await Button("dm-keyboard").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        var callout = await CalloutAsync("dm-keyboard");
        await Expect(Page.Locator("#kb-first")).ToBeFocusedAsync();

        // Shift+Tab from the first goes back to the button, and Tab from the button goes back in.
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Button("dm-keyboard")).ToBeFocusedAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#kb-first")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#kb-last")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#after-keyboard")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AutoFocus_MovesTheFocusIntoTheCalloutOnEveryOpening()
    {
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-autofocus");
        await Expect(Page.Locator("#af-input")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Button("dm-autofocus")).ToBeFocusedAsync();

        // A callout with nothing focusable takes the focus itself.
        var empty = await OpenMenuAsync("dm-autofocus-empty");
        await Expect(empty).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task TrapFocus_KeepsTabCyclingInsideAModalCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-trap");
        await Expect(callout).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(Page.Locator("#trap-email")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#trap-subscribe")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#trap-email")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Page.Locator("#trap-subscribe")).ToBeFocusedAsync();
        await Expect(callout).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Button("dm-trap")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task EscapeInsideADialog_ClosesTheDropMenuFirst_AndTheDialogOnTheNextPress()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await Expect(Page.Locator("#dialog-state")).ToHaveTextAsync("open");

        await Button("dm-in-dialog").ClickAsync();
        var callout = await CalloutAsync("dm-in-dialog");
        await Expect(callout).ToBeVisibleAsync();
        await callout.Locator("#dialog-menu-input").ClickAsync();
        await Expect(callout.Locator("#dialog-menu-input")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#dialog-menu-state")).ToHaveTextAsync("closed");
        await Expect(Page.Locator("#dialog-state")).ToHaveTextAsync("open");
        await Expect(Button("dm-in-dialog")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-state")).ToHaveTextAsync("closed");
    }

    [TestMethod]
    public async Task Escape_APopupOpenedInsideTheCallout_ClosesFirst_AndTheCalloutOnTheNextPress()
    {
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-nested");
        var dropdown = callout.Locator(".bit-drp-wrp");
        await dropdown.ClickAsync();
        await Expect(dropdown).ToHaveAttributeAsync("aria-expanded", "true");
        // The dropdown hands the focus to its list a round trip after it opens, which is where its Escape is pressed.
        await Page.WaitForFunctionAsync("() => document.querySelector('.bit-drp-cal')?.contains(document.activeElement) === true");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(dropdown).ToHaveAttributeAsync("aria-expanded", "false");
        // The list is gone from the page's script too before the next press is the drop menu's.
        await Expect(Page.Locator(".bit-drp-cal")).ToBeHiddenAsync();
        await Expect(dropdown).ToBeFocusedAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Button("dm-nested")).ToHaveAttributeAsync("aria-expanded", "true");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Button("dm-nested")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task FocusRing_TheKeyboardFocusDrawsARingAroundTheDropMenu()
    {
        await OpenAsync(Url);
        await CenterAsync("#dm-keyboard");

        Assert.AreEqual("none", await ComputedStyleAsync("#dm-keyboard", "box-shadow"));

        await Page.Locator("#before-keyboard").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Button("dm-keyboard")).ToBeFocusedAsync();

        await Expect(Root("dm-keyboard")).Not.ToHaveCSSAsync("box-shadow", "none");
    }

    [TestMethod]
    public async Task BitParams_CascadesItsValues_AndAValueOnTheDropMenuWins()
    {
        await OpenAsync(Url);

        var file = Root("dm-params-file");
        await Expect(file).ToHaveClassAsync(HasClass("bit-drm-pri"));
        await Expect(file).ToHaveClassAsync(HasClass("bit-drm-otl"));

        var share = Root("dm-params-share");
        await Expect(share).ToHaveClassAsync(HasClass("bit-drm-pri"));
        await Expect(share).ToHaveClassAsync(HasClass("bit-drm-fil"));
        await Expect(share).Not.ToHaveClassAsync(HasClass("bit-drm-otl"));

        // Gap = 4 and AutoClose travel too.
        var callout = await OpenMenuAsync("dm-params-file");
        await WaitSettledAsync(callout);
        var root = await BoxAsync(file);
        var box = await BoxAsync(callout);
        AssertBetween(box.Y - (root.Y + root.Height), 3, 7, "Gap=4 from BitParams.");

        await callout.Locator("#params-file-new").ClickAsync();
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Color_EveryColor_PaintsTheButtonWithItsRole()
    {
        await OpenAsync(Url);

        var none = Root("dm-color-none");
        foreach (var token in ColorTokens.Values)
        {
            await Expect(none).Not.ToHaveClassAsync(HasClass($"bit-drm-{token.Suffix}"));
        }

        foreach (var color in Colors)
        {
            var (suffix, main, on) = ColorTokens[color];
            var root = Root($"dm-color-{color}");

            await Expect(root).ToHaveClassAsync(HasClass($"bit-drm-{suffix}"));
            await Expect(root).ToHaveCSSAsync("background-color", await ResolveColorAsync($"var({main})"));
            await Expect(root).ToHaveCSSAsync("color", await ResolveColorAsync($"var({on})"));

            var outline = Root($"dm-color-outline-{color}");
            await Expect(outline).ToHaveClassAsync(HasClass($"bit-drm-{suffix}"));
            await Expect(outline).ToHaveCSSAsync("border-top-color", await ResolveColorAsync($"var({main})"));
            await Expect(outline).ToHaveCSSAsync("background-color", Transparent);
        }
    }

    [TestMethod]
    public async Task ExternalIcons_IconAndChevronDownIcon_RenderTheGivenClasses()
    {
        await OpenAsync(Url);

        var css = Root("dm-ext-css").Locator(".bit-drm-icn");
        await Expect(css).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(css).ToHaveClassAsync(HasClass("fa-house"));

        var fa = Root("dm-ext-fa").Locator(".bit-drm-icn");
        await Expect(fa).ToHaveClassAsync(HasClass("fa-brands"));
        await Expect(fa).ToHaveClassAsync(HasClass("fa-github"));

        var bi = Root("dm-ext-bi").Locator(".bit-drm-icn");
        await Expect(bi).ToHaveClassAsync(HasClass("bi"));
        await Expect(bi).ToHaveClassAsync(HasClass("bi-gear-fill"));
        var chevron = Root("dm-ext-bi").Locator(".bit-drm-chv");
        await Expect(chevron).ToHaveClassAsync(HasClass("bi-chevron-down"));
        await Expect(chevron).Not.ToHaveClassAsync(HasClass("bit-ico-r90"));

        // Icon wins over IconName.
        var precedence = Root("dm-ext-precedence").Locator(".bit-drm-icn");
        await Expect(precedence).ToHaveClassAsync(HasClass("my-icon"));
        await Expect(precedence).Not.ToHaveClassAsync(HasClass("bit-icon--Share"));
    }

    [TestMethod]
    [DataRow("Small", "bit-drm-sm")]
    [DataRow("Medium", "bit-drm-md")]
    [DataRow("Large", "bit-drm-lg")]
    public async Task Size_EverySize_ScalesTheButton(string size, string cls)
    {
        await OpenAsync(Url);

        var root = Root($"dm-size-{size}");
        await Expect(root).ToHaveClassAsync(HasClass(cls));
        await Expect(Root("dm-size-none")).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"bit-drm-(sm|md|lg)\b"));

        var heights = new Dictionary<string, double>();
        var fonts = new Dictionary<string, double>();
        foreach (var name in new[] { "Small", "Medium", "Large", "none" })
        {
            heights[name] = (await BoxAsync(Root($"dm-size-{name}"))).Height;
            fonts[name] = ParsePx(await ComputedStyleAsync($"#dm-size-{name} .bit-drm-btn", "font-size"));
        }

        switch (size)
        {
            case "Small":
                Assert.IsTrue(heights["Small"] < heights["Medium"], $"Small {heights["Small"]} < Medium {heights["Medium"]}.");
                Assert.IsTrue(fonts["Small"] < fonts["Medium"]);
                break;
            case "Medium":
                Assert.AreEqual(heights["none"], heights["Medium"], 0.5, "An unset Size is the medium one.");
                Assert.AreEqual(fonts["none"], fonts["Medium"], 0.1);
                break;
            default:
                Assert.IsTrue(heights["Large"] > heights["Medium"], $"Large {heights["Large"]} > Medium {heights["Medium"]}.");
                Assert.IsTrue(fonts["Large"] > fonts["Medium"]);
                break;
        }
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRoot()
    {
        await OpenAsync(Url);

        await Expect(Root("dm-style")).ToHaveCSSAsync("border-top-color", "rgb(138, 43, 226)");
        await Expect(Root("dm-style")).ToHaveCSSAsync("color", "rgb(138, 43, 226)");
        await Expect(Root("dm-style")).ToHaveCSSAsync("margin-left", "7px");
        await Expect(Root("dm-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Root("dm-class")).ToHaveClassAsync(HasClass("bit-drm"));

        await Expect(Root("dm-html-attrs")).ToHaveAttributeAsync("data-test", "drop-menu-attr");
        await Expect(Root("dm-html-attrs")).ToHaveAttributeAsync("data-extra", "extra");
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart_AndOpenedOnlyWhileOpen()
    {
        await OpenAsync(Url);

        var root = Root("dm-styles");
        await Expect(root).ToHaveCSSAsync("background-color", "rgb(255, 218, 185)");
        await Expect(Button("dm-styles")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(root.Locator(".bit-drm-icn")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(root.Locator(".bit-drm-txt")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");
        await Expect(root.Locator(".bit-drm-txt")).ToHaveCSSAsync("font-weight", "700");
        await Expect(root.Locator(".bit-drm-chv")).ToHaveCSSAsync("color", "rgb(0, 0, 255)");

        var callout = await OpenMenuAsync("dm-styles");
        await Expect(callout).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(callout).ToHaveCSSAsync("border-top-color", "rgb(255, 99, 71)");
        await Expect(root).ToHaveCSSAsync("background-color", "rgb(218, 165, 32)");
        var overlayId = (await callout.GetAttributeAsync("id"))!.Replace("-callout", "-overlay");
        await Expect(Page.Locator($"[id='{overlayId}']")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0.25)");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(root).ToHaveCSSAsync("background-color", "rgb(255, 218, 185)");

        var spinner = Root("dm-styles-spinner").Locator(".bit-drm-spn");
        await Expect(spinner).ToHaveClassAsync(HasClass("custom-spinner"));
        await Expect(spinner).ToHaveCSSAsync("width", "30px");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart_AndOpenedOnlyWhileOpen()
    {
        await OpenAsync(Url);

        var root = Root("dm-classes");
        await Expect(root).ToHaveClassAsync(HasClass("custom-root"));
        await Expect(Button("dm-classes")).ToHaveClassAsync(HasClass("custom-button"));
        await Expect(root.Locator(".bit-drm-icn")).ToHaveClassAsync(HasClass("custom-icon"));
        await Expect(root.Locator(".bit-drm-txt")).ToHaveClassAsync(HasClass("custom-text"));
        await Expect(root.Locator(".bit-drm-chv")).ToHaveClassAsync(HasClass("custom-chevron"));
        await Expect(root).Not.ToHaveClassAsync(HasClass("custom-opened"));

        var callout = await OpenMenuAsync("dm-classes");
        await Expect(callout).ToHaveClassAsync(HasClass("custom-callout"));
        await Expect(root).ToHaveClassAsync(HasClass("custom-opened"));
        var overlayId = (await callout.GetAttributeAsync("id"))!.Replace("-callout", "-overlay");
        await Expect(Page.Locator($"[id='{overlayId}']")).ToHaveClassAsync(HasClass("custom-overlay"));

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("custom-opened"));
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed_HideTheDropMenuAtRuntime()
    {
        await OpenAsync(Url);

        var root = Root("dm-visibility");
        await Expect(root).ToBeVisibleAsync();

        await Page.Locator("#btn-visibility-hidden").ClickAsync();
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await BoxAsync(root)).Width > 0, "A hidden drop menu keeps its room.");

        await Page.Locator("#btn-visibility-collapsed").ClickAsync();
        await Expect(root).ToHaveCSSAsync("display", "none");
        await Expect(root).ToBeHiddenAsync();

        await Page.Locator("#btn-visibility-visible").ClickAsync();
        await Expect(root).ToBeVisibleAsync();
        await Expect(root).ToHaveCSSAsync("visibility", "visible");
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_ReachTheButtonAndTheRelocatedCallout()
    {
        await OpenAsync(Url);

        var root = Root("dm-css-vars");
        await Expect(root).ToHaveCSSAsync("border-top-left-radius", "999px");
        await Expect(root).ToHaveCSSAsync("background-color", "rgb(200, 100, 50)");
        Assert.IsTrue((await BoxAsync(root)).Height >= 49.5, "--bit-DropMenu-min-height: 50px.");

        var callout = await OpenMenuAsync("dm-css-vars");
        // The callout is moved to the body, yet it keeps inheriting from the ancestor.
        Assert.IsTrue(await callout.EvaluateAsync<bool>("el => el.closest('#css-vars-ancestor') === null"));
        await Expect(callout).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(callout).ToHaveCSSAsync("border-top-left-radius", "13px");
        await Expect(callout).ToHaveCSSAsync("padding-top", "6px");
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColorOrBackground_WinsOverTheVariable()
    {
        await OpenAsync(Url);

        await Expect(Root("dm-css-vars-color")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-err)"));

        var callout = await OpenMenuAsync("dm-css-vars-bg-kind");
        await Expect(callout).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-bg-ter)"));
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        // A variable on the instance's own Style restyles its callout and the twin of its variant.
        await Expect(Root("dm-css-vars-instance")).ToHaveCSSAsync("border-top-color", "rgb(1, 2, 3)");
        var instance = await OpenMenuAsync("dm-css-vars-instance");
        await Expect(instance).ToHaveCSSAsync("background-color", "rgb(40, 50, 60)");
    }

    [TestMethod]
    public async Task Rtl_AnExplicitDir_MirrorsTheButtonAndAnchorsTheCalloutToTheRightEdge()
    {
        await OpenAsync(Url);

        var root = Root("dm-rtl");
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));

        var icon = await BoxAsync(root.Locator(".bit-drm-icn"));
        var text = await BoxAsync(root.Locator(".bit-drm-txt"));
        Assert.IsTrue(icon.X > text.X, "The leading icon sits on the right, right to left.");

        var callout = await OpenMenuAsync("dm-rtl");
        await WaitSettledAsync(callout);
        await Expect(callout).ToHaveAttributeAsync("dir", "rtl");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-drm-rtl"));

        var rootBox = await BoxAsync(root);
        var box = await BoxAsync(callout);
        Assert.AreEqual(rootBox.X + rootBox.Width, box.X + box.Width, 2, "Aligned with the right edge of the button.");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_IsReadOffTheElement()
    {
        await OpenAsync(Url);

        var root = Root("dm-rtl-inherited");
        await Expect(root).Not.ToHaveAttributeAsync("dir", "rtl");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-rtl"));

        var icon = await BoxAsync(root.Locator(".bit-drm-icn"));
        var text = await BoxAsync(root.Locator(".bit-drm-txt"));
        Assert.IsTrue(icon.X > text.X, "The leading icon sits on the right in an rtl ancestor.");

        var callout = await OpenMenuAsync("dm-rtl-inherited");
        await WaitSettledAsync(callout);
        var rootBox = await BoxAsync(root);
        var box = await BoxAsync(callout);
        Assert.AreEqual(rootBox.X + rootBox.Width, box.X + box.Width, 2, "Aligned with the right edge of the button.");
        Assert.IsTrue(await callout.EvaluateAsync<bool>("el => el.matches(':dir(rtl)')"), "The relocated callout keeps the direction.");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        var ltr = await OpenMenuAsync("dm-ltr");
        await WaitSettledAsync(ltr);
        var ltrRoot = await BoxAsync(Root("dm-ltr"));
        Assert.AreEqual(ltrRoot.X, (await BoxAsync(ltr)).X, 2, "Left to right it is aligned with the left edge.");
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheEntryTransition_AndForceAnimationRestoresIt()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(Url);

        var reduced = await OpenMenuAsync("dm-motion");
        var durations = ParseDurations(await reduced.EvaluateAsync<string>("el => getComputedStyle(el).transitionDuration"));
        Assert.IsTrue(durations.All(d => d < 0.01), $"Reduced: {string.Join(", ", durations)}s.");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(reduced).ToBeHiddenAsync();

        var forced = await OpenMenuAsync("dm-force-animation");
        await Expect(forced).ToHaveClassAsync(HasClass("bit-fam"));
        await Expect(Root("dm-force-animation")).ToHaveClassAsync(HasClass("bit-fam"));
        var full = ParseDurations(await forced.EvaluateAsync<string>("el => getComputedStyle(el).transitionDuration"));
        Assert.IsTrue(full.All(d => d >= 0.05), $"ForceAnimation: {string.Join(", ", full)}s.");
    }

    [TestMethod]
    public async Task Motion_TheCalloutFadesAndSlidesIn()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-motion");
        var durations = ParseDurations(await callout.EvaluateAsync<string>("el => getComputedStyle(el).transitionDuration"));
        Assert.IsTrue(durations.All(d => d >= 0.05), $"Full motion: {string.Join(", ", durations)}s.");

        // The transition finishes on the open state.
        await Expect(callout).ToHaveCSSAsync("opacity", "1");
        await Expect(callout).ToHaveCSSAsync("transform", "matrix(1, 0, 0, 1, 0, 0)");
    }

    [TestMethod]
    public async Task Responsive_AtAPhoneWidth_TheCalloutIsAPanelPinnedToTheEnd_AndASwipeDismissesIt()
    {
        if (IsWebKit) Assert.Inconclusive("Playwright's WebKit reports a screenX of 0 on the pointer events of a synthesized mouse drag, and screenX is what the swipe measures.");

        await Page.SetViewportSizeAsync(400, 800);
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-responsive");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-drm-res"));
        await Expect(callout).ToHaveClassAsync(HasClass("bit-drm-end"));
        await WaitSettledAsync(callout);

        var box = await BoxAsync(callout);
        Assert.AreEqual(400, box.X + box.Width, 2, "Pinned to the right edge (End, left to right).");
        Assert.AreEqual(0, box.Y, 2);
        Assert.AreEqual(800, box.Height, 2, "A side panel runs the full height.");

        // Dragged toward the edge it is pinned to.
        var y = box.Y + 40;
        await Page.Mouse.MoveAsync(box.X + 20, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + 20 + box.Width * 0.6f, y, new() { Steps = 10 });
        await Page.Mouse.UpAsync();

        await Expect(Button("dm-responsive")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#responsive-dismisses")).ToHaveTextAsync("1");
    }

    [TestMethod]
    [DataRow("start", "bit-drm-sta")]
    [DataRow("left", "bit-drm-lft")]
    [DataRow("right", "bit-drm-rgt")]
    [DataRow("top", "bit-drm-top")]
    [DataRow("bottom", "bit-drm-btm")]
    public async Task Responsive_PanelPlacement_PinsThePanelToThatEdge(string placement, string cls)
    {
        await Page.SetViewportSizeAsync(400, 800);
        await OpenAsync(Url);

        await Page.Locator($"#btn-panel-{placement}").ClickAsync();
        await Expect(Page.Locator("#panel-placement")).ToHaveTextAsync(placement, new() { IgnoreCase = true });

        var callout = await OpenMenuAsync("dm-responsive");
        await Expect(callout).ToHaveClassAsync(HasClass(cls));
        await WaitSettledAsync(callout);

        var box = await BoxAsync(callout);
        switch (placement)
        {
            case "start":
            case "left":
                Assert.AreEqual(0, box.X, 2);
                Assert.AreEqual(800, box.Height, 2);
                break;
            case "right":
                Assert.AreEqual(400, box.X + box.Width, 2);
                break;
            case "top":
                Assert.AreEqual(0, box.Y, 2);
                Assert.AreEqual(400, box.Width, 2);
                Assert.IsTrue(box.Height < 800, "A sheet is as tall as its content.");
                break;
            default:
                Assert.AreEqual(800, box.Y + box.Height, 2);
                Assert.AreEqual(400, box.Width, 2);
                Assert.IsTrue(box.Height < 800, "A sheet is as tall as its content.");
                break;
        }

        // The scroll container is capped to the room the screen leaves where the panel comes to rest, not where its
        // slide had carried it when it was measured - a bottom sheet measured from below the screen would get none.
        var list = await BoxAsync(callout.Locator("#responsive-list"));
        Assert.IsTrue(list.Height > 150, $"The content of the panel shows ({list.Height}px).");
        await Expect(callout.GetByText("Row 5", new() { Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Responsive_RightToLeft_TheStartPanelSlidesInFromTheRight()
    {
        await Page.SetViewportSizeAsync(400, 800);
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-responsive-rtl");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-drm-sta"));
        await WaitSettledAsync(callout);

        var box = await BoxAsync(callout);
        Assert.AreEqual(400, box.X + box.Width, 2, "Start is the right edge right to left.");
    }

    [TestMethod]
    public async Task Responsive_AtADesktopWidth_TheCalloutStaysAnchoredToTheButton()
    {
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-responsive");
        await WaitSettledAsync(callout);

        var root = await BoxAsync(Root("dm-responsive"));
        var box = await BoxAsync(callout);
        Assert.AreEqual(root.X, box.X, 2);
        Assert.IsTrue(box.Height < 600, "Not a full-height panel.");

        // ScrollContainerId names the part of the content the placement caps to the room the viewport leaves.
        var cap = await Page.Locator("#responsive-list").EvaluateAsync<string>("el => el.style.maxHeight");
        StringAssert.EndsWith(cap, "px", $"The scroll container is capped in pixels ({cap}).");
    }

    [TestMethod]
    public async Task PageScroll_DismissesTheOpenCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-scroll");
        await Expect(Page.Locator("#scroll-state")).ToHaveTextAsync("True");

        await Page.Mouse.WheelAsync(0, 250);

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#scroll-state")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Unmount_WhileOpen_RemovesTheCalloutAndTheOverlayWithoutAnError()
    {
        await OpenAsync(Url);

        var callout = await OpenMenuAsync("dm-unmount");
        var calloutId = (await callout.GetAttributeAsync("id"))!;
        var overlayId = calloutId.Replace("-callout", "-overlay");

        await Page.Locator("#btn-toggle-mounted").DispatchEventAsync("click");

        await Expect(Page.Locator("#mounted-state")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#dm-unmount")).ToHaveCountAsync(0);
        await Expect(Page.Locator($"[id='{calloutId}']")).ToHaveCountAsync(0);
        await Expect(Page.Locator($"[id='{overlayId}']")).ToHaveCountAsync(0);

        // Nothing is left over the page to swallow a click.
        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#mounted-state")).ToHaveTextAsync("True");
        await OpenMenuAsync("dm-unmount");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }



    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Button(string id) => Page.Locator($"#{id} > .bit-drm-btn");

    private async Task<ILocator> CalloutAsync(string id)
    {
        var calloutId = await Button(id).GetAttributeAsync("aria-controls");
        return Page.Locator($"[id='{calloutId}']");
    }

    private async Task<ILocator> OpenMenuAsync(string id)
    {
        await CenterAsync($"#{id}");
        await Button(id).ClickAsync();

        var callout = await CalloutAsync(id);
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Button(id)).ToHaveAttributeAsync("aria-expanded", "true");

        // WebKit focuses no button it dispatches a click for, so the drop menu focuses its button itself, a round trip
        // later - and the Escape key is answered where the focus is. The focus is waited for on the button, or inside
        // the callout for the ones that move it there.
        await Page.WaitForFunctionAsync(@"([buttonId, calloutId]) => document.activeElement === document.getElementById(buttonId)
            || document.getElementById(calloutId)?.contains(document.activeElement)",
            new[] { await Button(id).GetAttributeAsync("id"), await Button(id).GetAttributeAsync("aria-controls") });

        return callout;
    }

    // Brings an element to the middle of the viewport, so a callout opened from it has room on every side, and lets
    // the scroll it caused settle before anything is opened (a scroll dismisses an open callout).
    private async Task CenterAsync(string selector)
    {
        await Page.Locator(selector).First.EvaluateAsync("el => el.scrollIntoView({ block: 'center', inline: 'center' })");
        await NextFramesAsync();
        await NextFramesAsync();
    }

    // The entry transition (an opacity fade and a short slide) has finished.
    private async Task WaitSettledAsync(ILocator callout)
    {
        await Expect(callout).ToHaveCSSAsync("opacity", "1");
        await Expect(callout).ToHaveCSSAsync("transform", new System.Text.RegularExpressions.Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));
    }

    private static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no box.");
        return box;
    }

    // The computed rgb() of any css color value, read off a probe element of the page, so a token the theme
    // resolves is compared as the browser resolves it.
    private Task<string> ResolveColorAsync(string value)
    {
        return Page.EvaluateAsync<string>(@"v => {
            const probe = document.createElement('div');
            probe.style.backgroundColor = v;
            document.body.appendChild(probe);
            const color = getComputedStyle(probe).backgroundColor;
            probe.remove();
            return color;
        }", value);
    }

    private static void AssertBetween(double value, double min, double max, string message)
    {
        Assert.IsTrue(value >= min && value <= max, $"{message} Expected {min}..{max}, was {value}.");
    }

    private static double ParsePx(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    private static double[] ParseDurations(string value)
    {
        return value.Split(',').Select(v => v.Trim()).Select(v => v.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(v[..^2], CultureInfo.InvariantCulture) / 1000
            : double.Parse(v[..^1], CultureInfo.InvariantCulture)).ToArray();
    }
}
