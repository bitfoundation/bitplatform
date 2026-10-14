namespace Bit.BlazorUI.Tests.E2E.Notifications;

/// <summary>
/// BitBadge in a real browser (/e2e/badge): the badge prerenders, lands on every one of the fifteen positions around
/// a real child in both directions (explicit and inherited), overlaps and offsets by measured lengths, caps, hides at
/// zero and hides on demand, draws a dot, a ring and a pulse (stopped under reduced motion unless forced), stands
/// alone or inline, is a real button or link with the keyboard, the hover, the focus ring and the disabled semantics
/// of one, carries its text alternative and its live region, re-skins through its public CSS variables and the
/// BitParams cascade, paints every color, size, shape and variant, lands Style/Class/Styles/Classes on every part, and
/// unmounts without an error.
/// </summary>
[TestClass]
public class BitBadgeE2ETests : E2ETestBase
{
    private static readonly (string Name, string Suffix, string Vertical, string Horizontal, string RtlHorizontal)[] Positions =
    [
        ("TopLeft", "tlf", "top", "left", "left"),
        ("TopCenter", "tcr", "top", "center", "center"),
        ("TopRight", "trg", "top", "right", "right"),
        ("TopStart", "tst", "top", "left", "right"),
        ("TopEnd", "ten", "top", "right", "left"),
        ("CenterLeft", "clf", "center", "left", "left"),
        ("Center", "ctr", "center", "center", "center"),
        ("CenterRight", "crg", "center", "right", "right"),
        ("CenterStart", "cst", "center", "left", "right"),
        ("CenterEnd", "cen", "center", "right", "left"),
        ("BottomLeft", "blf", "bottom", "left", "left"),
        ("BottomCenter", "bcr", "bottom", "center", "center"),
        ("BottomRight", "brg", "bottom", "right", "right"),
        ("BottomStart", "bst", "bottom", "left", "right"),
        ("BottomEnd", "ben", "bottom", "right", "left"),
    ];

    private static string Badge(string id) => $"#{id} > .bit-bdg-wrp > .bit-bdg-ctn";

    private static string Child(string id) => $"#{id} > .bdg-anchor";

    private static string Horizontal(Box badge, Box child)
    {
        if (Math.Abs(badge.CenterX - child.CenterX) <= 1.5) return "center";
        if (badge.Left < child.Left && badge.Right > child.Left) return "left";
        if (badge.Left < child.Right && badge.Right > child.Right) return "right";
        return $"? (badge {badge.Left}-{badge.Right}, child {child.Left}-{child.Right})";
    }

    private static string Vertical(Box badge, Box child)
    {
        if (Math.Abs(badge.CenterY - child.CenterY) <= 1.5) return "center";
        if (badge.Top < child.Top && badge.Bottom > child.Top) return "top";
        if (badge.Top < child.Bottom && badge.Bottom > child.Bottom) return "bottom";
        return $"? (badge {badge.Top}-{badge.Bottom}, child {child.Top}-{child.Bottom})";
    }

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheBadgeBeforeTheCircuitConnects()
    {
        var html = await Page.PrerenderedHtmlAsync($"{BaseUrl}/e2e/badge");

        StringAssert.Contains(html, "id=\"basic-badge\"");
        StringAssert.Contains(html, "bit-bdg-ctn");
        StringAssert.Contains(html, ">Loading<");

        await OpenAsync("/e2e/badge");
        await Expect(Page.Locator(Badge("basic-badge"))).ToHaveTextAsync("5");
    }

    [TestMethod]
    public async Task Basic_TheBadge_OverlaysTheTopRightCornerOfItsChild()
    {
        await OpenAsync("/e2e/badge");
        await Page.SettleAnimationsAsync();

        var root = Page.Locator("#basic-badge");
        await Expect(root).ToHaveClassAsync(HasClass("bit-bdg"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-bdg-trg"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-bdg-fil"));
        await Expect(Page.Locator(Badge("basic-badge"))).ToBeVisibleAsync();
        await Expect(Page.Locator(Badge("basic-badge"))).ToHaveTextAsync("5");

        var badge = await Page.BoxAsync(Badge("basic-badge"));
        var child = await Page.BoxAsync("#basic-anchor");
        Assert.AreEqual("top", Vertical(badge, child));
        Assert.AreEqual("right", Horizontal(badge, child));

        // The overlay lets the pointer through to the child and is lifted one layer above it.
        Assert.AreEqual("none", await ComputedStyleAsync("#basic-badge > .bit-bdg-wrp", "pointer-events"));
        Assert.AreEqual("1", await ComputedStyleAsync("#basic-badge > .bit-bdg-wrp", "z-index"));
    }

    [TestMethod]
    public async Task Basic_TheContentChangesAtRuntime_TheBadgeShowsItAndBumps()
    {
        await OpenAsync("/e2e/badge");

        var badge = Page.Locator(Badge("basic-badge"));
        await Expect(badge).Not.ToHaveClassAsync(HasClass("bit-bdg-bm1"));
        Assert.AreEqual("bit-bdg-anm", await ComputedStyleAsync(Badge("basic-badge"), "animation-name"));

        await Page.ClickAsync("#btn-inc");
        await Expect(badge).ToHaveTextAsync("6");
        await Expect(badge).ToHaveClassAsync(HasClass("bit-bdg-bm1"));
        Assert.AreEqual("bit-bdg-bmp-anm", await ComputedStyleAsync(Badge("basic-badge"), "animation-name"));

        // The two bump classes alternate, so every change restarts the animation.
        await Page.ClickAsync("#btn-inc");
        await Expect(badge).ToHaveTextAsync("7");
        await Expect(badge).ToHaveClassAsync(HasClass("bit-bdg-bm2"));

        await Page.ClickAsync("#btn-dec");
        await Expect(badge).ToHaveTextAsync("6");
        await Expect(badge).ToHaveClassAsync(HasClass("bit-bdg-bm1"));
    }

    [TestMethod]
    public async Task Variant_EachVariant_PaintsItsOwnLook()
    {
        await OpenAsync("/e2e/badge");

        var primary = await Page.ResolveColorAsync("--bit-clr-pri");
        var primaryText = await Page.ResolveColorAsync("--bit-clr-pri-text");
        const string transparent = "rgba(0, 0, 0, 0)";

        await Expect(Page.Locator("#var-fill")).ToHaveClassAsync(HasClass("bit-bdg-fil"));
        await Expect(Page.Locator(Badge("var-fill"))).ToHaveCSSAsync("background-color", primary);
        await Expect(Page.Locator(Badge("var-fill"))).ToHaveCSSAsync("border-top-color", primary);
        await Expect(Page.Locator(Badge("var-fill"))).ToHaveCSSAsync("color", primaryText);

        await Expect(Page.Locator("#var-outline")).ToHaveClassAsync(HasClass("bit-bdg-otl"));
        await Expect(Page.Locator(Badge("var-outline"))).ToHaveCSSAsync("background-color", transparent);
        await Expect(Page.Locator(Badge("var-outline"))).ToHaveCSSAsync("border-top-color", primary);
        await Expect(Page.Locator(Badge("var-outline"))).ToHaveCSSAsync("color", primary);

        await Expect(Page.Locator("#var-text")).ToHaveClassAsync(HasClass("bit-bdg-txt"));
        await Expect(Page.Locator(Badge("var-text"))).ToHaveCSSAsync("background-color", transparent);
        await Expect(Page.Locator(Badge("var-text"))).ToHaveCSSAsync("border-top-color", transparent);
        await Expect(Page.Locator(Badge("var-text"))).ToHaveCSSAsync("color", primary);

        // An unset variant is the Fill one.
        await Expect(Page.Locator("#var-default")).ToHaveClassAsync(HasClass("bit-bdg-fil"));
        await Expect(Page.Locator(Badge("var-default"))).ToHaveCSSAsync("background-color", primary);
    }

    [TestMethod]
    public async Task Variant_Disabled_KeepsTheVariantAndTakesTheDisabledColors()
    {
        await OpenAsync("/e2e/badge");

        var disabled = await Page.ResolveColorAsync("--bit-clr-pri-dis");
        var disabledText = await Page.ResolveColorAsync("--bit-clr-pri-dis-text");
        const string transparent = "rgba(0, 0, 0, 0)";

        foreach (var id in new[] { "dis-fill", "dis-outline", "dis-text" })
        {
            await Expect(Page.Locator($"#{id}")).ToHaveClassAsync(HasClass("bit-dis"));
            await Expect(Page.Locator(Badge(id))).ToHaveCSSAsync("color", disabledText);
        }

        await Expect(Page.Locator(Badge("dis-fill"))).ToHaveCSSAsync("background-color", disabled);
        await Expect(Page.Locator(Badge("dis-outline"))).ToHaveCSSAsync("background-color", transparent);
        await Expect(Page.Locator(Badge("dis-outline"))).ToHaveCSSAsync("border-top-color", disabled);
        await Expect(Page.Locator(Badge("dis-text"))).ToHaveCSSAsync("border-top-color", transparent);
    }

    [TestMethod]
    public async Task Shape_EachShape_SetsTheCornerOfTheBadge()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator("#shape-pill")).ToHaveClassAsync(HasClass("bit-bdg-cir"));
        await Expect(Page.Locator("#shape-rounded")).ToHaveClassAsync(HasClass("bit-bdg-rnd"));
        await Expect(Page.Locator("#shape-square")).ToHaveClassAsync(HasClass("bit-bdg-sqr"));
        await Expect(Page.Locator("#shape-default")).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("bit-bdg-(cir|rnd|sqr)"));

        var pill = NotificationsE2EHelpers.Px(await ComputedStyleAsync(Badge("shape-pill"), "border-top-left-radius"));
        var defaultRadius = NotificationsE2EHelpers.Px(await ComputedStyleAsync(Badge("shape-default"), "border-top-left-radius"));
        var rounded = NotificationsE2EHelpers.Px(await ComputedStyleAsync(Badge("shape-rounded"), "border-top-left-radius"));
        var height = (await Page.BoxAsync(Badge("shape-pill"))).Height;

        await Expect(Page.Locator(Badge("shape-square"))).ToHaveCSSAsync("border-top-left-radius", "0px");
        Assert.IsTrue(pill >= height / 2, $"A pill's radius ({pill}) rounds its whole height ({height}).");
        Assert.AreEqual(pill, defaultRadius, "An unset shape is the pill.");
        Assert.IsTrue(rounded > 0 && rounded < height / 2, $"A rounded badge takes the control corner ({rounded}).");
    }

    [TestMethod]
    public async Task Content_StringIconAndTemplate_RenderWhatTheBadgeIsGiven()
    {
        await OpenAsync("/e2e/badge");
        await Page.SettleAnimationsAsync();

        await Expect(Page.Locator(Badge("content-string"))).ToHaveTextAsync("Text");

        var iconOnly = Page.Locator($"{Badge("content-icon")} > i.bit-bdg-ico");
        await Expect(iconOnly).ToHaveClassAsync(HasClass("bit-icon--Ringer"));
        await Expect(iconOnly).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Page.Locator($"{Badge("content-icon")} > .bit-bdg-con")).ToHaveCountAsync(0);

        // The glyph comes before the content, and after it when Reversed.
        var icon = await Page.BoxAsync($"{Badge("content-icon-text")} > .bit-bdg-ico");
        var text = await Page.BoxAsync($"{Badge("content-icon-text")} > .bit-bdg-con");
        Assert.IsTrue(icon.Right <= text.Left + 0.5, "The icon leads the content.");

        await Expect(Page.Locator("#content-reversed")).ToHaveClassAsync(HasClass("bit-bdg-rvs"));
        var reversedIcon = await Page.BoxAsync($"{Badge("content-reversed")} > .bit-bdg-ico");
        var reversedText = await Page.BoxAsync($"{Badge("content-reversed")} > .bit-bdg-con");
        Assert.IsTrue(reversedIcon.Left >= reversedText.Right - 0.5, "Reversed puts the icon after the content.");

        // A template replaces the content.
        await Expect(Page.Locator(Badge("content-template"))).ToHaveTextAsync("99%");
        await Expect(Page.Locator("#tpl-bold")).ToBeVisibleAsync();

        // Anything else prints through ToString(), a fraction with its separator.
        await Expect(Page.Locator(Badge("content-object"))).ToHaveTextAsync("obj-value");
        await Expect(Page.Locator(Badge("content-decimal"))).ToHaveTextAsync("2.5");

        // A badge given nothing to show is not rendered, and its child is.
        await Expect(Page.Locator("#content-nothing > .bit-bdg-wrp")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#nothing-anchor")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Max_ACountPastIt_IsCappedAndRevealedOnHover()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator(Badge("max-under"))).ToHaveTextAsync("60");
        await Expect(Page.Locator(Badge("max-under"))).Not.ToHaveAttributeAsync("title", new System.Text.RegularExpressions.Regex(".*"));

        await Expect(Page.Locator(Badge("max-over"))).ToHaveTextAsync("63+");
        await Expect(Page.Locator(Badge("max-over"))).ToHaveAttributeAsync("title", "100");

        // Any numeric type is counted, a long among them.
        await Expect(Page.Locator(Badge("max-long"))).ToHaveTextAsync("99+");
        await Expect(Page.Locator(Badge("max-long"))).ToHaveAttributeAsync("title", "12345");

        // A count at the max is not past it.
        await Expect(Page.Locator(Badge("max-equal"))).ToHaveTextAsync("9");
    }

    [TestMethod]
    public async Task ShowZero_False_TakesAnEmptiedCounterOffAndBringsItBack()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator(Badge("zero-hidden"))).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-zero-dec");
        await Expect(Page.Locator("#zero-count")).ToHaveTextAsync("0");

        // ShowZero (true by default) keeps the zero; false takes the badge off and keeps the child.
        await Expect(Page.Locator(Badge("zero-shown"))).ToHaveTextAsync("0");
        await Expect(Page.Locator("#zero-hidden > .bit-bdg-wrp")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#zero-hidden-anchor")).ToBeVisibleAsync();

        // A glyph is content of its own: the badge stays, without the zero.
        await Expect(Page.Locator($"{Badge("zero-icon")} > .bit-bdg-ico")).ToBeVisibleAsync();
        await Expect(Page.Locator($"{Badge("zero-icon")} > .bit-bdg-con")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-zero-inc");
        await Expect(Page.Locator(Badge("zero-hidden"))).ToHaveTextAsync("1");
        await Expect(Page.Locator($"{Badge("zero-icon")} > .bit-bdg-con")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Hidden_Toggled_RemovesTheBadgeAndKeepsTheChild()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator(Badge("hidden"))).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-toggle-hidden");
        await Expect(Page.Locator("#hidden > .bit-bdg-wrp")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#hidden-anchor")).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-toggle-hidden");
        await Expect(Page.Locator(Badge("hidden"))).ToHaveTextAsync("5");
    }

    [TestMethod]
    public async Task Dot_IsACircleOfItsOwnSize_WithoutContentAndWithItsDescription()
    {
        await OpenAsync("/e2e/badge");
        await Page.SettleAnimationsAsync();

        await Expect(Page.Locator("#dot-badge")).ToHaveClassAsync(HasClass("bit-bdg-dot"));
        await Expect(Page.Locator($"{Badge("dot-badge")} > .bit-bdg-con")).ToHaveCountAsync(0);

        var dot = await Page.BoxAsync(Badge("dot-badge"));
        Assert.AreEqual(9, dot.Width, 0.5, "The medium dot is 9px wide.");
        Assert.AreEqual(dot.Width, dot.Height, 0.5, "A dot is a circle.");
        Assert.IsTrue(NotificationsE2EHelpers.Px(await ComputedStyleAsync(Badge("dot-badge"), "border-top-left-radius")) >= dot.Width / 2, "A dot is fully rounded.");

        // The description is what a screen reader reads, kept in the tree but out of sight.
        var description = Page.Locator($"{Badge("dot-badge")} > .bit-bdg-vhd");
        await Expect(description).ToHaveTextAsync("New mail");
        Assert.IsTrue((await description.BoxAsync()).Width <= 1, "The description is visually hidden.");

        // The overlay leaves the child usable underneath it.
        await Page.ClickAsync("#dot-child-btn");
        await Expect(Page.Locator("#dot-child-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Position_EachOfTheFifteen_AnchorsTheBadgeAroundTheChild()
    {
        await OpenAsync("/e2e/badge");
        await Page.SettleAnimationsAsync();

        var failures = new List<string>();
        foreach (var (name, suffix, vertical, horizontal, _) in Positions)
        {
            await Expect(Page.Locator($"#pos-{name}")).ToHaveClassAsync(HasClass($"bit-bdg-{suffix}"));

            var badge = await Page.BoxAsync(Badge($"pos-{name}"));
            var child = await Page.BoxAsync(Child($"pos-{name}"));

            var v = Vertical(badge, child);
            var h = Horizontal(badge, child);
            if (v != vertical || h != horizontal) failures.Add($"{name}: expected {vertical}/{horizontal}, got {v}/{h}");
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public async Task Position_RightToLeft_StartAndEndFollowTheDirectionAndLeftRightStay()
    {
        await OpenAsync("/e2e/badge");
        await Page.SettleAnimationsAsync();

        var failures = new List<string>();
        foreach (var (name, _, vertical, _, rtlHorizontal) in Positions)
        {
            var root = Page.Locator($"#rtl-pos-{name}");
            await Expect(root).ToHaveAttributeAsync("dir", "rtl");
            await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));

            var badge = await Page.BoxAsync(Badge($"rtl-pos-{name}"));
            var child = await Page.BoxAsync(Child($"rtl-pos-{name}"));

            var v = Vertical(badge, child);
            var h = Horizontal(badge, child);
            if (v != vertical || h != rtlHorizontal) failures.Add($"{name}: expected {vertical}/{rtlHorizontal}, got {v}/{h}");
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public async Task Position_UnderAnAncestorDirRtl_FollowsTheInheritedDirection()
    {
        await OpenAsync("/e2e/badge");
        await Page.SettleAnimationsAsync();

        await Expect(Page.Locator("#inh-ten")).Not.ToHaveAttributeAsync("dir", new System.Text.RegularExpressions.Regex(".+"));

        Assert.AreEqual("left", Horizontal(await Page.BoxAsync(Badge("inh-ten")), await Page.BoxAsync(Child("inh-ten"))), "TopEnd is on the left in RTL.");
        Assert.AreEqual("right", Horizontal(await Page.BoxAsync(Badge("inh-tst")), await Page.BoxAsync(Child("inh-tst"))), "TopStart is on the right in RTL.");
        Assert.AreEqual("right", Horizontal(await Page.BoxAsync(Badge("inh-trg")), await Page.BoxAsync(Child("inh-trg"))), "TopRight stays on the right.");

        // The icon order follows the direction too: the leading icon sits on the right.
        var icon = await Page.BoxAsync($"{Badge("inh-rvs")} > .bit-bdg-ico");
        var text = await Page.BoxAsync($"{Badge("inh-rvs")} > .bit-bdg-con");
        Assert.IsTrue(icon.Left >= text.Right - 0.5, "In RTL the icon leads from the right.");
    }

    [TestMethod]
    public async Task Position_ChangedAtRuntime_MovesTheBadge()
    {
        await OpenAsync("/e2e/badge");
        await Page.SettleAnimationsAsync();

        var child = await Page.BoxAsync(Child("pos-runtime"));
        var before = await Page.BoxAsync(Badge("pos-runtime"));
        Assert.AreEqual("top", Vertical(before, child));
        Assert.AreEqual("right", Horizontal(before, child));

        await Page.ClickAsync("#btn-pos-bottom-left");
        await Expect(Page.Locator("#pos-runtime")).ToHaveClassAsync(HasClass("bit-bdg-blf"));
        await Page.SettleAnimationsAsync();

        // The click may have scrolled the page, so both are measured again.
        child = await Page.BoxAsync(Child("pos-runtime"));
        var after = await Page.BoxAsync(Badge("pos-runtime"));
        Assert.AreEqual("bottom", Vertical(after, child));
        Assert.AreEqual("left", Horizontal(after, child));
    }

    [TestMethod]
    public async Task Overlap_PullsTheBadgeFurtherInOverTheChild()
    {
        await OpenAsync("/e2e/badge");
        await Page.SettleAnimationsAsync();

        await Expect(Page.Locator("#ovl-on")).ToHaveClassAsync(HasClass("bit-bdg-orp"));

        var plainChild = await Page.BoxAsync(Child("ovl-none"));
        var plain = await Page.BoxAsync(Badge("ovl-none"));
        var overlapChild = await Page.BoxAsync(Child("ovl-on"));
        var overlap = await Page.BoxAsync(Badge("ovl-on"));

        // How far the badge reaches back in over the right and the top edge of its child.
        var plainInset = plainChild.Right - plain.Left;
        var overlapInset = overlapChild.Right - overlap.Left;
        Assert.AreEqual(4, plainInset, 0.5, "The regular inset is spacing(0.5).");
        Assert.AreEqual(12, overlapInset, 0.5, "Overlap deepens it to spacing(1.5).");
        Assert.AreEqual(12, overlap.Bottom - overlapChild.Top, 0.5);
    }

    [TestMethod]
    public async Task Offset_TranslatesTheBadgeByTheGivenLengths()
    {
        await OpenAsync("/e2e/badge");
        await Page.SettleAnimationsAsync();

        await Expect(Page.Locator("#ofs")).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("--bit-bdg-ofs-x:\\s*-8px"));

        var reference = await Page.BoxAsync(Badge("ovl-none"));
        var referenceChild = await Page.BoxAsync(Child("ovl-none"));
        var offset = await Page.BoxAsync(Badge("ofs"));
        var offsetChild = await Page.BoxAsync(Child("ofs"));

        Assert.AreEqual(-8, (offset.Left - offsetChild.Left) - (reference.Left - referenceChild.Left), 0.5, "OffsetX=-8px moves it 8px left.");
        Assert.AreEqual(8, (offset.Top - offsetChild.Top) - (reference.Top - referenceChild.Top), 0.5, "OffsetY=8px moves it 8px down.");
    }

    [TestMethod]
    public async Task Bordered_RingsTheBadgeInThePageColor()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator("#bordered-badge")).ToHaveClassAsync(HasClass("bit-bdg-brd"));
        Assert.AreEqual("none", await ComputedStyleAsync(Badge("plain"), "box-shadow"));

        var ring = await ComputedStyleAsync(Badge("bordered-badge"), "box-shadow");
        var page = await Page.ResolveColorAsync("--bit-clr-bg-pri");
        StringAssert.Contains(ring, page, "The ring is drawn in the page's background color.");
    }

    [TestMethod]
    public async Task Pulse_SendsALoopingRingOutOfTheBadge()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator("#pulse-badge")).ToHaveClassAsync(HasClass("bit-bdg-pls"));
        Assert.AreEqual("bit-bdg-pls-anm", await Page.PseudoStyleAsync(Badge("pulse-badge"), "::after", "animation-name"));
        Assert.AreEqual("infinite", await Page.PseudoStyleAsync(Badge("pulse-badge"), "::after", "animation-iteration-count"));
        Assert.AreEqual("none", await Page.PseudoStyleAsync(Badge("plain"), "::after", "animation-name"), "A badge without Pulse sends no ring.");
    }

    [TestMethod]
    public async Task ReducedMotion_StopsThePulseAndTheEntryAnimation_UnlessForceAnimation()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync("/e2e/badge");

        // The pulse stops, and stays as a still halo.
        Assert.AreEqual("none", await Page.PseudoStyleAsync(Badge("pulse-badge"), "::after", "animation-name"));
        Assert.AreEqual("0.5", await Page.PseudoStyleAsync(Badge("pulse-badge"), "::after", "opacity"));

        // ForceAnimation keeps it running.
        await Expect(Page.Locator("#pulse-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        Assert.AreEqual("bit-bdg-pls-anm", await Page.PseudoStyleAsync(Badge("pulse-forced"), "::after", "animation-name"));

        // The scale-in reads a motion duration, which collapses.
        var duration = await ComputedStyleAsync(Badge("basic-badge"), "animation-duration");
        Assert.IsTrue(duration is "1e-05s" or "0.00001s" or "0s", $"The entry animation collapses under reduced motion ({duration}).");
        Assert.AreEqual("0.1s", await ComputedStyleAsync(Badge("pulse-forced"), "animation-duration"), "A forced badge keeps its 100ms scale-in.");
    }

    [TestMethod]
    public async Task Standalone_WithoutChildContent_IsLaidOutInTheFlowAndTakesThePointer()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator("#sa-draft > .bit-bdg-wrp")).ToHaveClassAsync(HasClass("bit-bdg-stl"));
        Assert.AreEqual("relative", await ComputedStyleAsync(Badge("sa-draft"), "position"));
        Assert.AreEqual("auto", await ComputedStyleAsync(Badge("sa-draft"), "pointer-events"));
        await Expect(Page.Locator(Badge("sa-draft"))).ToHaveTextAsync("Draft");

        // The standalone badge's box is the root's: it is in the flow, not over anything.
        var root = await Page.BoxAsync("#sa-draft");
        var badge = await Page.BoxAsync(Badge("sa-draft"));
        Assert.IsTrue(badge.Left >= root.Left - 0.5 && badge.Right <= root.Right + 0.5);

        // A capped count reveals its figure on hover; Title replaces it.
        await Expect(Page.Locator(Badge("sa-capped"))).ToHaveTextAsync("999+");
        await Expect(Page.Locator(Badge("sa-capped"))).ToHaveAttributeAsync("title", "12345");
        await Expect(Page.Locator(Badge("sa-title"))).ToHaveAttributeAsync("title", "1,234 downloads this week");
        await Page.HoverAsync(Badge("sa-capped"));
        Assert.IsTrue(await Page.Locator(Badge("sa-capped")).EvaluateAsync<bool>("el => el.matches(':hover')"), "A standalone badge takes the pointer.");

        await Expect(Page.Locator(Badge("sa-dot"))).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Inline_LaysTheBadgeBesideTheChild_OnTheSideThePositionAndDirectionSay()
    {
        await OpenAsync("/e2e/badge");
        await Page.SettleAnimationsAsync();

        await Expect(Page.Locator("#inl-end")).ToHaveClassAsync(HasClass("bit-bdg-inl"));
        await Expect(Page.Locator("#inl-end")).ToHaveClassAsync(HasClass("bit-bdg-ten"));

        var end = await Page.BoxAsync(Badge("inl-end"));
        var endChild = await Page.BoxAsync("#inl-end-child");
        Assert.IsTrue(end.Left >= endChild.Right, "An unset side is after the child.");
        Assert.AreEqual(endChild.CenterY, end.CenterY, 1, "The badge is centred on the child's line.");

        var start = await Page.BoxAsync(Badge("inl-start"));
        var startChild = await Page.BoxAsync("#inl-start-child");
        Assert.IsTrue(start.Right <= startChild.Left, "CenterStart is before the child.");

        var rtl = await Page.BoxAsync(Badge("inl-rtl"));
        var rtlChild = await Page.BoxAsync("#inl-rtl-child");
        Assert.IsTrue(rtl.Right <= rtlChild.Left, "In RTL the end side is the left.");

        var rtlLeft = await Page.BoxAsync(Badge("inl-rtl-left"));
        var rtlLeftChild = await Page.BoxAsync("#inl-rtl-left-child");
        Assert.IsTrue(rtlLeft.Right <= rtlLeftChild.Left, "A physical Left stays on the left in RTL.");
    }

    [TestMethod]
    public async Task OnClick_ClickingTheBadge_FiresOnceAndTheClickStopsAtTheBadge()
    {
        await OpenAsync("/e2e/badge");

        var button = Page.Locator("#click-badge > .bit-bdg-wrp > button.bit-bdg-ctn");
        await Expect(button).ToHaveAttributeAsync("type", "button");
        await Expect(button).ToHaveClassAsync(HasClass("bit-bdg-clk"));
        Assert.AreEqual("pointer", await ComputedStyleAsync("#click-badge button", "cursor"));

        await button.ClickAsync();
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-click-button")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#parent-clicks")).ToHaveTextAsync("0");
        await Expect(button).ToHaveTextAsync("1");

        // A click on the child under the overlay still reaches the page.
        await Page.ClickAsync("#click-anchor", new() { Position = new() { X = 10, Y = 30 } });
        await Expect(Page.Locator("#parent-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("1");

        // TabIndex lands on the button.
        await Expect(Page.Locator("#click-tabindex button")).ToHaveAttributeAsync("tabindex", "-1");

        // A plain badge is no button and never takes the focus.
        await Expect(Page.Locator("#click-plain button")).ToHaveCountAsync(0);
        await Expect(Page.Locator(Badge("click-plain"))).Not.ToHaveAttributeAsync("tabindex", new System.Text.RegularExpressions.Regex(".*"));
    }

    [TestMethod]
    public async Task OnClick_TheKeyboard_ReachesTheBadgeAndEnterAndSpaceActivateIt()
    {
        await OpenAsync("/e2e/badge");

        var button = Page.Locator("#click-badge button");
        await Page.Locator("#btn-toggle-hidden").FocusAsync();
        var focusedId = "";
        for (var i = 0; i < 40 && focusedId != "click-badge"; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            focusedId = await Page.EvaluateAsync<string>("() => document.activeElement?.closest('.bit-bdg')?.id ?? ''");
        }

        await Expect(button).ToBeFocusedAsync();
        Assert.IsTrue(await button.EvaluateAsync<bool>("el => el.matches(':focus-visible')"));
        Assert.AreNotEqual("none", await ComputedStyleAsync("#click-badge button", "box-shadow"), "The focus ring is drawn.");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("1");

        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#parent-clicks")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task OnClick_Disabled_IsADisabledButtonThatDoesNotFire_UntilEnabledAgain()
    {
        await OpenAsync("/e2e/badge");

        var root = Page.Locator("#click-disabled");
        var button = Page.Locator("#click-disabled button");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("disabled", "");
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        Assert.AreEqual("none", await ComputedStyleAsync("#click-disabled button", "pointer-events"));

        await button.DispatchEventAsync("click");
        await button.ClickAsync(new() { Force = true });
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-toggle-click-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToBeEnabledAsync();
        await Expect(button).Not.ToHaveAttributeAsync("aria-disabled", new System.Text.RegularExpressions.Regex(".*"));

        await button.ClickAsync();
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task OnClick_HoverAndPress_MoveTheBackgroundToTheStateColors()
    {
        await OpenAsync("/e2e/badge");

        var button = Page.Locator("#click-badge button");
        await Expect(button).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-pri"));

        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-pri-hover"));

        await Page.Mouse.DownAsync();
        await Expect(button).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-pri-active"));
        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("#clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Href_RendersARealLink_WithTargetAndRel()
    {
        await OpenAsync("/e2e/badge");

        var link = Page.Locator("#href-badge > .bit-bdg-wrp > a.bit-bdg-ctn");
        await Expect(link).ToHaveAttributeAsync("href", "/e2e/badge#href-target");
        await Expect(link).Not.ToHaveAttributeAsync("rel", new System.Text.RegularExpressions.Regex(".*"));

        var blank = Page.Locator("#href-blank a");
        await Expect(blank).ToHaveAttributeAsync("target", "_blank");
        await Expect(blank).ToHaveAttributeAsync("rel", new System.Text.RegularExpressions.Regex("(^|\\s)noopener(\\s|$)"));

        // A rel of its own is kept, and noopener added to it - unless noreferrer, which implies it, is there.
        await Expect(Page.Locator("#href-nofollow a")).ToHaveAttributeAsync("rel", "nofollow noopener");
        await Expect(Page.Locator("#href-rel a")).ToHaveAttributeAsync("rel", "nofollow noreferrer");

        // A real link: the click navigates, here to a fragment of the same page.
        await link.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/e2e/badge#href-target$"));
        await Expect(Page.Locator("#href-badge")).ToBeVisibleAsync();
        await Expect(Page.Locator("#parent-clicks")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Href_Disabled_DropsTheHrefTargetAndFocus_AndStaysALink()
    {
        await OpenAsync("/e2e/badge");

        var link = Page.Locator("#href-disabled a");
        await Expect(Page.Locator("#href-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(link).Not.ToHaveAttributeAsync("href", new System.Text.RegularExpressions.Regex(".*"));
        await Expect(link).Not.ToHaveAttributeAsync("target", new System.Text.RegularExpressions.Regex(".*"));
        await Expect(link).ToHaveAttributeAsync("role", "link");
        await Expect(link).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(link).ToHaveAttributeAsync("aria-disabled", "true");
        Assert.AreEqual("default", await ComputedStyleAsync("#href-disabled a", "cursor"));
        Assert.AreEqual("none", await ComputedStyleAsync("#href-disabled-overlay a", "pointer-events"));

        var url = Page.Url;
        await link.ClickAsync(new() { Force = true });
        Assert.AreEqual(url, Page.Url, "A disabled link goes nowhere.");
    }

    [TestMethod]
    public async Task Accessibility_Description_IsTheTextAlternativeOfWhatTheBadgeShows()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator($"{Badge("a11y-live")} > .bit-bdg-con")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Page.Locator($"{Badge("a11y-live")} > .bit-bdg-vhd")).ToHaveTextAsync("1 unread messages");

        await Expect(Page.Locator($"{Badge("a11y-dot")} > .bit-bdg-vhd")).ToHaveTextAsync("Online");
        Assert.AreEqual("absolute", await ComputedStyleAsync($"{Badge("a11y-dot")} > .bit-bdg-vhd", "position"));
    }

    [TestMethod]
    public async Task Accessibility_Live_AnnouncesEveryChangeThroughARegionThatOutlivesTheBadge()
    {
        await OpenAsync("/e2e/badge");

        var region = Page.Locator("#a11y-live > .bit-bdg-lvr");
        await Expect(region).ToHaveAttributeAsync("role", "status");
        await Expect(region).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(region).ToHaveAttributeAsync("aria-atomic", "true");
        await Expect(region).ToHaveTextAsync("1 unread messages");

        // The region speaks for the badge, so the badge is hidden from assistive technologies.
        await Expect(Page.Locator("#a11y-live > .bit-bdg-wrp")).ToHaveAttributeAsync("aria-hidden", "true");

        await Page.ClickAsync("#btn-receive");
        await Expect(region).ToHaveTextAsync("2 unread messages");

        // A count without a description is announced as the count; the region stays while the badge goes.
        var countRegion = Page.Locator("#a11y-live-count > .bit-bdg-lvr");
        await Expect(countRegion).ToHaveTextAsync("2");

        await Page.ClickAsync("#btn-read-all");
        await Expect(Page.Locator("#a11y-live-count > .bit-bdg-wrp")).ToHaveCountAsync(0);
        await Expect(countRegion).ToHaveCountAsync(1);
        await Expect(countRegion).ToHaveTextAsync("");

        await Page.ClickAsync("#btn-receive");
        await Expect(countRegion).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Accessibility_AriaLabel_NamesALinkBadge_AndTheDescriptionDescribesIt()
    {
        await OpenAsync("/e2e/badge");

        var link = Page.Locator("#a11y-link a");
        await Expect(link).ToHaveAttributeAsync("aria-label", "Alerts");
        var describedBy = await link.GetAttributeAsync("aria-describedby");
        Assert.IsFalse(string.IsNullOrEmpty(describedBy));
        await Expect(Page.Locator($"[id='{describedBy}']")).ToHaveTextAsync("3 unread");
        await Expect(Page.Locator("#a11y-link")).Not.ToHaveAttributeAsync("aria-label", new System.Text.RegularExpressions.Regex(".*"));

        // A link badge is reachable by the keyboard and named.
        await Expect(link).ToHaveAccessibleNameAsync("Alerts");
    }

    [TestMethod]
    public async Task Accessibility_DecorativeAndPlainAriaLabel_ShapeWhatAScreenReaderGets()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator("#a11y-decorative > .bit-bdg-wrp")).ToHaveAttributeAsync("aria-hidden", "true");

        // A plain dot's label becomes its text alternative, since a span cannot be named.
        await Expect(Page.Locator($"{Badge("a11y-label-dot")} > .bit-bdg-vhd")).ToHaveTextAsync("Recording");
        await Expect(Page.Locator("#a11y-label-dot")).Not.ToHaveAttributeAsync("aria-label", new System.Text.RegularExpressions.Regex(".*"));

        // A root with a role of its own is named by the label.
        await Expect(Page.Locator("#a11y-label-role")).ToHaveAttributeAsync("role", "img");
        await Expect(Page.Locator("#a11y-label-role")).ToHaveAttributeAsync("aria-label", "4 new");

        // A count keeps saying the count.
        await Expect(Page.Locator($"{Badge("a11y-label-count")} > .bit-bdg-vhd")).ToHaveCountAsync(0);
        await Expect(Page.Locator($"{Badge("a11y-label-count")} > .bit-bdg-con")).ToHaveTextAsync("4");
    }

    [TestMethod]
    public async Task CssVariables_ColorsOnAnAncestor_RestyleTheDefault_NotAColorOrTheDisabledState()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator(Badge("cv-custom"))).ToHaveCSSAsync("background-color", "rgb(102, 51, 153)");
        await Expect(Page.Locator(Badge("cv-custom"))).ToHaveCSSAsync("color", "rgb(255, 255, 255)");
        await Expect(Page.Locator(Badge("cv-custom"))).ToHaveCSSAsync("border-top-color", "rgb(0, 128, 0)");

        await Expect(Page.Locator(Badge("cv-explicit"))).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-err"));
        await Expect(Page.Locator(Badge("cv-disabled"))).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-pri-dis"));
    }

    [TestMethod]
    public async Task CssVariables_SizeAndShapeOnAnAncestor_RestyleTheDefault_NotAnExplicitSizeOrShape()
    {
        await OpenAsync("/e2e/badge");

        var tall = Page.Locator(Badge("cv-size"));
        await Expect(tall).ToHaveCSSAsync("height", "32px");
        await Expect(tall).ToHaveCSSAsync("padding-left", "16px");
        await Expect(tall).ToHaveCSSAsync("border-top-left-radius", "8px");
        await Expect(tall).ToHaveCSSAsync("font-weight", "400");
        await Expect(tall).ToHaveCSSAsync("font-size", "18px");

        var small = Page.Locator(Badge("cv-size-explicit"));
        await Expect(small).ToHaveCSSAsync("height", "16px");
        await Expect(small).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(small).ToHaveCSSAsync("font-weight", "400");
    }

    [TestMethod]
    public async Task CssVariables_DotSizeRingAndPulse_AreRestyled()
    {
        await OpenAsync("/e2e/badge");

        var dot = Page.Locator(Badge("cv-dot"));
        await Expect(dot).ToHaveCSSAsync("width", "12px");
        await Expect(dot).ToHaveCSSAsync("height", "12px");

        var ring = await ComputedStyleAsync(Badge("cv-dot"), "box-shadow");
        StringAssert.Contains(ring, "rgb(255, 165, 0)");
        StringAssert.Contains(ring, "3px");

        Assert.AreEqual("rgb(220, 20, 60)", await Page.PseudoStyleAsync(Badge("cv-dot"), "::after", "border-top-color"));
    }

    [TestMethod]
    public async Task CssVariables_AVariantTwin_ReachesOnlyItsVariant()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator(Badge("cv-twin-outline"))).ToHaveCSSAsync("border-top-color", "rgb(255, 0, 0)");
        await Expect(Page.Locator(Badge("cv-twin-fill"))).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("--bit-clr-pri"));
    }

    [TestMethod]
    public async Task CssVariables_InsetLayerGapAndInlineGap_AreRestyled()
    {
        await OpenAsync("/e2e/badge");
        await Page.SettleAnimationsAsync();

        Assert.AreEqual("7", await ComputedStyleAsync("#cv-inset > .bit-bdg-wrp", "z-index"));
        await Expect(Page.Locator(Badge("cv-inset"))).ToHaveCSSAsync("gap", "10px");

        var badge = await Page.BoxAsync(Badge("cv-inset"));
        var child = await Page.BoxAsync(Child("cv-inset"));
        Assert.AreEqual(child.Right, badge.Left, 0.5, "An inset of 0 puts the badge just outside the corner.");
        Assert.AreEqual(child.Top, badge.Bottom, 0.5);

        await Expect(Page.Locator(Badge("cv-border"))).ToHaveCSSAsync("border-top-width", "3px");
        var overlapBadge = await Page.BoxAsync(Badge("cv-border"));
        var overlapChild = await Page.BoxAsync(Child("cv-border"));
        Assert.AreEqual(20, overlapChild.Right - overlapBadge.Left, 0.5, "The overlap inset variable deepens an Overlap badge.");

        var inline = await Page.BoxAsync(Badge("cv-inline-gap"));
        var inlineChild = await Page.BoxAsync("#cv-inline-gap-child");
        Assert.AreEqual(20, inline.Left - inlineChild.Right, 0.5);
    }

    [TestMethod]
    public async Task CssVariables_HoverActiveAndFocusColors_RestyleAClickableBadge()
    {
        await OpenAsync("/e2e/badge");

        var button = Page.Locator("#cv-hover button");
        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(0, 0, 255)");

        await Page.Mouse.DownAsync();
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(0, 255, 0)");
        await Page.Mouse.UpAsync();

        await Page.Mouse.MoveAsync(0, 0);
        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(button).ToBeFocusedAsync();
        StringAssert.Contains(await ComputedStyleAsync("#cv-hover button", "box-shadow"), "rgb(255, 0, 255)");
    }

    [TestMethod]
    public async Task Cascade_BitParams_SetsTheDefaultsAndAnOwnValueWins()
    {
        await OpenAsync("/e2e/badge");

        foreach (var id in new[] { "cas-1", "cas-2" })
        {
            await Expect(Page.Locator($"#{id}")).ToHaveClassAsync(HasClass("bit-bdg-otl"));
            await Expect(Page.Locator($"#{id}")).ToHaveClassAsync(HasClass("bit-bdg-brd"));
            await Expect(Page.Locator($"#{id}")).ToHaveClassAsync(HasClass("bit-bdg-rnd"));
        }

        await Expect(Page.Locator(Badge("cas-1"))).ToHaveTextAsync("4");
        await Expect(Page.Locator(Badge("cas-2"))).ToHaveTextAsync("99+");

        await Expect(Page.Locator("#cas-3")).ToHaveClassAsync(HasClass("bit-bdg-fil"));
        await Expect(Page.Locator("#cas-3")).Not.ToHaveClassAsync(HasClass("bit-bdg-otl"));
        await Expect(Page.Locator("#cas-3")).ToHaveClassAsync(HasClass("bit-bdg-brd"));
        await Expect(Page.Locator(Badge("cas-3"))).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-pri"));
    }

    [TestMethod]
    public async Task Color_EachColor_PaintsItsRoleInEveryVariant()
    {
        await OpenAsync("/e2e/badge");

        foreach (var (name, suffix, token) in NotificationsE2EHelpers.Colors)
        {
            var color = await Page.ResolveColorAsync(token);

            await Expect(Page.Locator($"#clr-{name}")).ToHaveClassAsync(HasClass($"bit-bdg-{suffix}"));
            await Expect(Page.Locator(Badge($"clr-{name}"))).ToHaveCSSAsync("background-color", color);
            await Expect(Page.Locator(Badge($"clr-otl-{name}"))).ToHaveCSSAsync("border-top-color", color);
            await Expect(Page.Locator(Badge($"clr-otl-{name}"))).ToHaveCSSAsync("color", color);
            await Expect(Page.Locator(Badge($"clr-txt-{name}"))).ToHaveCSSAsync("color", color);
        }
    }

    [TestMethod]
    public async Task Color_ChangedAtRuntime_RepaintsTheBadge()
    {
        await OpenAsync("/e2e/badge");

        var badge = Page.Locator(Badge("clr-runtime"));
        await Expect(badge).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-pri"));
        await Expect(Page.Locator("#clr-runtime")).Not.ToHaveClassAsync(HasClass("bit-bdg-pri"));

        await Page.ClickAsync("#btn-color-error");
        await Expect(Page.Locator("#clr-runtime")).ToHaveClassAsync(HasClass("bit-bdg-err"));
        await Expect(badge).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-err"));
    }

    [TestMethod]
    public async Task ExternalIcons_TheIconInfo_RendersTheLibrarysClasses()
    {
        await OpenAsync("/e2e/badge");

        var css = Page.Locator($"{Badge("ext-css")} > i.bit-bdg-ico");
        await Expect(css).ToHaveClassAsync(HasClass("bdg-ext-icon"));
        await Expect(css).ToHaveClassAsync(HasClass("custom-set"));
        Assert.AreEqual("\"*\"", await Page.PseudoStyleAsync($"{Badge("ext-css")} > i.bit-bdg-ico", "::before", "content"));
        await Expect(css).ToHaveCSSAsync("font-size", await ComputedStyleAsync(Badge("ext-css"), "font-size"));

        await Expect(Page.Locator($"{Badge("ext-fa")} > i")).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(Page.Locator($"{Badge("ext-fa")} > i")).ToHaveClassAsync(HasClass("fa-bell"));
        await Expect(Page.Locator($"{Badge("ext-bi")} > i")).ToHaveClassAsync(HasClass("bi"));
        await Expect(Page.Locator($"{Badge("ext-bi")} > i")).ToHaveClassAsync(HasClass("bi-gear-fill"));
    }

    [TestMethod]
    public async Task Size_EachSize_SetsTheHeightTheTypeAndTheDot()
    {
        await OpenAsync("/e2e/badge");

        var expected = new[] { ("Small", "sm", 16, 8), ("Medium", "md", 20, 9), ("Large", "lg", 24, 12) };
        var fontSizes = new List<double>();
        foreach (var (name, suffix, height, dot) in expected)
        {
            await Expect(Page.Locator($"#size-{name}")).ToHaveClassAsync(HasClass($"bit-bdg-{suffix}"));
            await Expect(Page.Locator(Badge($"size-{name}"))).ToHaveCSSAsync("height", $"{height}px");
            await Expect(Page.Locator(Badge($"size-dot-{name}"))).ToHaveCSSAsync("width", $"{dot}px");
            await Expect(Page.Locator(Badge($"size-dot-{name}"))).ToHaveCSSAsync("height", $"{dot}px");
            fontSizes.Add(NotificationsE2EHelpers.Px(await ComputedStyleAsync(Badge($"size-{name}"), "font-size")));
        }

        Assert.IsTrue(fontSizes[0] < fontSizes[1] && fontSizes[1] < fontSizes[2], $"The type grows with the size ({string.Join(", ", fontSizes)}).");

        // A single digit stays a circle.
        await Page.SettleAnimationsAsync();
        var oneDigit = await Page.BoxAsync(Badge("size-one-digit"));
        Assert.AreEqual(oneDigit.Height, oneDigit.Width, 0.5);
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRootAndStylesAndClassesOnEveryPart()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator("#sc-style")).ToHaveCSSAsync("margin-left", "12px");
        await Expect(Page.Locator("#sc-style")).ToHaveCSSAsync("outline-color", "rgb(30, 144, 255)");
        await Expect(Page.Locator("#sc-class")).ToHaveClassAsync(HasClass("custom-class"));

        await Expect(Page.Locator("#sc-styles")).ToHaveCSSAsync("margin-left", "13px");
        await Expect(Page.Locator("#sc-styles > .bit-bdg-wrp")).ToHaveCSSAsync("opacity", "0.9");
        await Expect(Page.Locator(Badge("sc-styles"))).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(Page.Locator($"{Badge("sc-styles")} > .bit-bdg-ico")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");
        await Expect(Page.Locator($"{Badge("sc-styles")} > .bit-bdg-con")).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(Page.Locator($"{Badge("sc-styles")} > .bit-bdg-vhd")).ToHaveCSSAsync("font-size", "3px");
        await Expect(Page.Locator("#sc-styles > .bit-bdg-lvr")).ToHaveCSSAsync("font-size", "4px");

        await Expect(Page.Locator("#sc-classes")).ToHaveClassAsync(HasClass("custom-root"));
        await Expect(Page.Locator("#sc-classes > .bit-bdg-wrp")).ToHaveClassAsync(HasClass("custom-wrapper"));
        await Expect(Page.Locator(Badge("sc-classes"))).ToHaveClassAsync(HasClass("custom-badge"));
        await Expect(Page.Locator($"{Badge("sc-classes")} > .bit-bdg-ico")).ToHaveClassAsync(HasClass("custom-icon"));
        await Expect(Page.Locator($"{Badge("sc-classes")} > .bit-bdg-con")).ToHaveClassAsync(HasClass("custom-content"));
        await Expect(Page.Locator($"{Badge("sc-classes")} > .bit-bdg-vhd")).ToHaveClassAsync(HasClass("custom-description"));
        await Expect(Page.Locator("#sc-classes > .bit-bdg-lvr")).ToHaveClassAsync(HasClass("custom-live"));
    }

    [TestMethod]
    public async Task BaseParameters_VisibilityHtmlAttributesDirAndId_LandOnTheRoot()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator("#vis-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Page.Locator("#vis-hidden")).ToBeHiddenAsync();
        await Expect(Page.Locator("#vis-collapsed")).ToHaveCSSAsync("display", "none");

        await Expect(Page.Locator("#html-attrs")).ToHaveAttributeAsync("data-test", "badge-attr");
        await Expect(Page.Locator("#html-attrs")).ToHaveAttributeAsync("title", "root title");

        await Expect(Page.Locator("#dir-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator("#dir-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));

        var autoId = await Page.Locator(".no-id-badge").GetAttributeAsync("id");
        Assert.IsFalse(string.IsNullOrWhiteSpace(autoId), "A badge without an Id gets a unique one.");
    }

    [TestMethod]
    public async Task Unmount_RemovingBadges_LeavesNoErrorOnThePageOrTheServer()
    {
        await OpenAsync("/e2e/badge");

        await Expect(Page.Locator("#unmount-host .bit-bdg")).ToHaveCountAsync(3);

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-host")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-host .bit-bdg")).ToHaveCountAsync(3);

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-host")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
