using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Notifications;

/// <summary>
/// BitMessage in a real browser (/e2e/message): it prerenders, paints every variant, tint, color and size, clips one
/// line, wraps, caps at MaxLines and reflows (AutoMultiline) as the browser measures it, folds and unfolds through the
/// expander, the Expanded binding and its methods, lays its title, alignment, actions and icons out, dismisses by the
/// button, by Escape (unmodified, and claimed inside a dialog so the dialog waits for the next press), by
/// DismissAsync and by its countdown - which holds on hover, focus, PauseAutoDismiss and a blurred or hidden window,
/// is drawn as a bar and starts over when refused - says what it is to assistive technologies (roles, politeness,
/// icon label, delayed announcement, focus), stops its buttons, Escape and countdown while disabled, re-skins through
/// its public CSS variables and the BitParams cascade, mirrors in RTL and unmounts without an error.
/// </summary>
[TestClass]
public class BitMessageE2ETests : E2ETestBase
{
    private const string Transparent = "rgba(0, 0, 0, 0)";

    private static readonly Dictionary<string, string> OnTokens = new()
    {
        ["pri"] = "--bit-clr-pri-text", ["sec"] = "--bit-clr-sec-text", ["ter"] = "--bit-clr-ter-text", ["inf"] = "--bit-clr-inf-text",
        ["suc"] = "--bit-clr-suc-text", ["wrn"] = "--bit-clr-wrn-text", ["swr"] = "--bit-clr-swr-text", ["err"] = "--bit-clr-err-text",
        ["pbg"] = "--bit-clr-fg-pri", ["sbg"] = "--bit-clr-fg-pri", ["tbg"] = "--bit-clr-fg-pri",
        ["pfg"] = "--bit-clr-bg-pri", ["sfg"] = "--bit-clr-bg-pri", ["tfg"] = "--bit-clr-bg-pri",
        ["pbr"] = "--bit-clr-fg-pri", ["sbr"] = "--bit-clr-fg-pri", ["tbr"] = "--bit-clr-fg-pri",
    };

    private static readonly Dictionary<string, string> FgTokens = new()
    {
        ["pri"] = "--bit-clr-pri-fg", ["sec"] = "--bit-clr-sec-fg", ["ter"] = "--bit-clr-ter-fg", ["inf"] = "--bit-clr-inf-fg",
        ["suc"] = "--bit-clr-suc-fg", ["wrn"] = "--bit-clr-wrn-fg", ["swr"] = "--bit-clr-swr-fg", ["err"] = "--bit-clr-err-fg",
        ["pbg"] = "--bit-clr-fg-pri", ["sbg"] = "--bit-clr-fg-pri", ["tbg"] = "--bit-clr-fg-pri",
        ["pfg"] = "--bit-clr-fg-pri", ["sfg"] = "--bit-clr-fg-sec", ["tfg"] = "--bit-clr-fg-ter",
        ["pbr"] = "--bit-clr-fg-pri", ["sbr"] = "--bit-clr-fg-pri", ["tbr"] = "--bit-clr-fg-pri",
    };

    private static readonly HashSet<string> SemanticSuffixes = ["pri", "sec", "ter", "inf", "suc", "wrn", "swr", "err"];

    private static Regex Any => new(".*");

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Part(string id, string part) => Page.Locator($"#{id} .{part}").First;

    private Task<double> NumberAsync(string selector, string property)
        => ComputedStyleAsync(selector, property).ContinueWith(t => NotificationsE2EHelpers.Px(t.Result));

    private async Task<double> ScaleXAsync(string selector)
    {
        var transform = await ComputedStyleAsync(selector, "transform");
        if (transform is "none") return 1;

        var values = transform[(transform.IndexOf('(') + 1)..transform.IndexOf(')')].Split(',');
        return double.Parse(values[0], CultureInfo.InvariantCulture);
    }

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheMessageBeforeTheCircuitConnects()
    {
        var html = await Page.PrerenderedHtmlAsync($"{BaseUrl}/e2e/message");

        StringAssert.Contains(html, "id=\"basic-msg\"");
        StringAssert.Contains(html, "This is a Message.");
        StringAssert.Contains(html, "bit-msg-fil");

        await OpenAsync("/e2e/message");
        await Expect(Part("basic-msg", "bit-msg-cnt")).ToHaveTextAsync("This is a Message.");
    }

    [TestMethod]
    public async Task Basic_TheMessage_FillsItsContainerInTheInfoColorWithItsIcon()
    {
        await OpenAsync("/e2e/message");

        var root = Root("basic-msg");
        await Expect(root).ToHaveClassAsync(HasClass("bit-msg"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-msg-fil"));
        await Expect(root).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-inf"));
        await Expect(root).ToHaveCSSAsync("color", await Page.ResolveColorAsync("--bit-clr-inf-text"));
        await Expect(Part("basic-msg", "bit-msg-ico")).ToHaveClassAsync(HasClass("bit-icon--Info"));
        await Expect(Part("basic-msg", "bit-msg-ict")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Part("basic-msg", "bit-msg-cnc")).ToHaveAttributeAsync("role", "status");

        // An unnamed message is a plain div rather than a group.
        await Expect(root).Not.ToHaveAttributeAsync("role", Any);

        var box = await Page.BoxAsync("#basic-box");
        var message = await Page.BoxAsync("#basic-msg");
        Assert.AreEqual(box.Width, message.Width, 0.5, "The message fills its container.");
    }

    [TestMethod]
    public async Task Variant_EachVariant_AndTinted_PaintTheirOwnLook()
    {
        await OpenAsync("/e2e/message");

        var info = await Page.ResolveColorAsync("--bit-clr-inf");
        var infoFg = await Page.ResolveColorAsync("--bit-clr-inf-fg");
        var tint = await Page.ResolveColorAsync("--bit-clr-inf-tint");

        await Expect(Root("var-fill")).ToHaveCSSAsync("background-color", info);
        await Expect(Root("var-fill")).ToHaveCSSAsync("border-top-color", info);

        await Expect(Root("var-outline")).ToHaveClassAsync(HasClass("bit-msg-otl"));
        await Expect(Root("var-outline")).ToHaveCSSAsync("background-color", Transparent);
        await Expect(Root("var-outline")).ToHaveCSSAsync("border-top-color", info);
        await Expect(Root("var-outline")).ToHaveCSSAsync("color", infoFg);

        await Expect(Root("var-text")).ToHaveClassAsync(HasClass("bit-msg-txt"));
        await Expect(Root("var-text")).ToHaveCSSAsync("background-color", Transparent);
        await Expect(Root("var-text")).ToHaveCSSAsync("border-top-color", Transparent);
        await Expect(Root("var-text")).ToHaveCSSAsync("color", infoFg);

        foreach (var id in new[] { "var-outline-tinted", "var-text-tinted" })
        {
            await Expect(Root(id)).ToHaveClassAsync(HasClass("bit-msg-tnt"));
            await Expect(Root(id)).ToHaveCSSAsync("background-color", tint);
        }

        // A tint only washes the variants that drop the fill.
        await Expect(Root("var-fill-tinted")).Not.ToHaveClassAsync(HasClass("bit-msg-tnt"));
        await Expect(Root("var-fill-tinted")).ToHaveCSSAsync("background-color", info);
    }

    [TestMethod]
    public async Task Multiline_OneLineIsClipped_MultilineWraps_AndMaxLinesCapsTheWrap()
    {
        await OpenAsync("/e2e/message");

        Assert.AreEqual("nowrap", await ComputedStyleAsync("#ml-single .bit-msg-cnc", "white-space"));
        Assert.IsTrue(await Part("ml-single", "bit-msg-cnt").EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"), "The single line is clipped.");
        Assert.AreEqual("ellipsis", await ComputedStyleAsync("#ml-single .bit-msg-cnt", "text-overflow"));

        var lineHeight = (await Page.BoxAsync("#ml-single .bit-msg-cnt")).Height;

        await Expect(Part("ml-multi", "bit-msg-cnt")).ToHaveClassAsync(HasClass("bit-msg-mcn"));
        var multiHeight = (await Page.BoxAsync("#ml-multi .bit-msg-cnt")).Height;
        Assert.IsTrue(multiHeight > lineHeight * 2.5, $"Multiline wraps onto several lines ({multiHeight} vs {lineHeight}).");

        await Expect(Part("ml-max", "bit-msg-cnt")).ToHaveClassAsync(HasClass("bit-msg-clp"));
        await Expect(Root("ml-max")).ToHaveAttributeAsync("style", new Regex("--bit-msg-maxlines:2"));
        var cappedHeight = (await Page.BoxAsync("#ml-max .bit-msg-cnt")).Height;
        Assert.IsTrue(cappedHeight < multiHeight && cappedHeight > lineHeight * 1.5, $"MaxLines=2 caps the wrap at two lines ({cappedHeight}).");
        Assert.IsTrue(await Part("ml-max", "bit-msg-cnt").EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight + 1"), "What is past the cap is clipped.");
    }

    [TestMethod]
    public async Task AutoMultiline_KeepsOneLineWhereItFits_AndReflowsWhereItDoesNot()
    {
        await OpenAsync("/e2e/message");

        await Expect(Part("ml-auto", "bit-msg-cnt")).Not.ToHaveClassAsync(HasClass("bit-msg-mcn"));
        await Expect(Page.Locator("#ml-auto-actions > .bit-msg-rct > .bit-msg-act")).ToHaveCountAsync(1);

        await Page.ClickAsync("#btn-auto-narrow");
        await Expect(Part("ml-auto", "bit-msg-cnt")).ToHaveClassAsync(HasClass("bit-msg-mcn"));
        Assert.AreEqual("normal", await ComputedStyleAsync("#ml-auto .bit-msg-cnt", "white-space"));
        Assert.IsFalse(await Part("ml-auto", "bit-msg-cnt").EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth + 1"), "Nothing is cut off once it wraps.");

        // The actions get a row of their own once the line runs out of room.
        await Expect(Page.Locator("#ml-auto-actions > .bit-msg-act.bit-msg-mac")).ToHaveCountAsync(1);
        await Page.ClickAsync("#auto-restore");

        await Page.ClickAsync("#btn-auto-wide");
        await Expect(Part("ml-auto", "bit-msg-cnt")).Not.ToHaveClassAsync(HasClass("bit-msg-mcn"));
        await Expect(Page.Locator("#ml-auto-actions > .bit-msg-rct > .bit-msg-act")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Truncate_TheExpander_UnfoldsTheContent_AndTheExpandedBindingFollowsBothWays()
    {
        await OpenAsync("/e2e/message");

        var expander = Page.Locator("#tr-bound .bit-msg-exb");
        await Expect(expander).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(expander).ToHaveAttributeAsync("aria-label", "Expand");
        await Expect(expander).ToHaveAttributeAsync("title", "Expand");
        await Expect(expander).ToHaveAttributeAsync("type", "button");
        await Expect(expander).ToHaveAttributeAsync("aria-controls", "tr-bound-cnt");
        await Expect(Page.Locator("#tr-bound-cnt")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#tr-bound .bit-msg-exi")).ToHaveClassAsync(HasClass("bit-icon--DoubleChevronUp"));
        await Expect(Page.Locator("#tr-bound .bit-msg-exi")).ToHaveClassAsync(HasClass("bit-ico-r180"));

        var folded = (await Page.BoxAsync("#tr-bound")).Height;

        await expander.ClickAsync();
        await Expect(expander).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(expander).ToHaveAttributeAsync("aria-label", "Collapse");
        await Expect(Page.Locator("#tr-expanded")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#tr-bound .bit-msg-cnc")).ToHaveClassAsync(HasClass("bit-msg-cnx"));
        await Expect(Page.Locator("#tr-bound .bit-msg-exi")).Not.ToHaveClassAsync(HasClass("bit-ico-r180"));
        Assert.IsTrue((await Page.BoxAsync("#tr-bound")).Height > folded + 10, "Unfolded, the content takes its lines.");

        // The page sets the binding back.
        await Page.ClickAsync("#btn-tr-set-expanded");
        await Expect(Page.Locator("#tr-expanded")).ToHaveTextAsync("False");
        await Expect(expander).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#tr-bound .bit-msg-cnc")).Not.ToHaveClassAsync(HasClass("bit-msg-cnx"));
    }

    [TestMethod]
    public async Task Truncate_ExpandCollapseAndToggleAsync_DriveTheFoldFromCode()
    {
        await OpenAsync("/e2e/message");

        var expander = Page.Locator("#tr-methods .bit-msg-exb");
        await Expect(expander).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.ClickAsync("#btn-tr-expand");
        await Expect(expander).ToHaveAttributeAsync("aria-expanded", "true");

        await Page.ClickAsync("#btn-tr-collapse");
        await Expect(expander).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.ClickAsync("#btn-tr-toggle");
        await Expect(expander).ToHaveAttributeAsync("aria-expanded", "true");

        await Page.ClickAsync("#btn-tr-toggle");
        await Expect(expander).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Truncate_AShortText_GetsNoExpander_AndACustomLabelIsUsed()
    {
        await OpenAsync("/e2e/message");

        // The browser measures the line and finds nothing clipped.
        await Expect(Page.Locator("#tr-short .bit-msg-exb")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#tr-bound .bit-msg-exb")).ToHaveCountAsync(1);

        var labelled = Page.Locator("#tr-labels .bit-msg-exb");
        await Expect(labelled).ToHaveAttributeAsync("aria-label", "Show more");
        await labelled.ClickAsync();
        await Expect(labelled).ToHaveAttributeAsync("aria-label", "Show less");
    }

    [TestMethod]
    public async Task Truncate_OnACappedMultiline_UnfoldsTheCap()
    {
        await OpenAsync("/e2e/message");

        await Expect(Part("tr-max", "bit-msg-cnt")).ToHaveClassAsync(HasClass("bit-msg-clp"));
        var capped = (await Page.BoxAsync("#tr-max .bit-msg-cnt")).Height;

        await Page.ClickAsync("#tr-max .bit-msg-exb");
        await Expect(Part("tr-max", "bit-msg-cnt")).Not.ToHaveClassAsync(HasClass("bit-msg-clp"));
        Assert.IsTrue((await Page.BoxAsync("#tr-max .bit-msg-cnt")).Height > capped + 10);
    }

    [TestMethod]
    public async Task Title_LeadsTheLineOnASingleLine_SitsAboveOnMultiline_AndNamesTheGroup()
    {
        await OpenAsync("/e2e/message");

        var inline = Root("ttl-inline");
        await Expect(inline).ToHaveAttributeAsync("role", "group");
        await Expect(inline).ToHaveAttributeAsync("aria-labelledby", "ttl-inline-ttl");
        await Expect(Page.Locator("#ttl-inline-ttl")).ToHaveTextAsync("Heads up");
        await Expect(Part("ttl-inline", "bit-msg-cnw")).ToHaveClassAsync(HasClass("bit-msg-cwi"));

        var title = await Page.BoxAsync("#ttl-inline .bit-msg-ttl");
        var content = await Page.BoxAsync("#ttl-inline .bit-msg-cnt");
        Assert.IsTrue(title.Right <= content.Left + 1, "The title leads the content on its line.");
        Assert.AreEqual(title.CenterY, content.CenterY, 3);
        Assert.AreEqual("600", await ComputedStyleAsync("#ttl-inline .bit-msg-ttl", "font-weight"));

        // The two boxes reach into each other by the room their focus rings are given, so the lines are compared.
        var stackedTitle = await Page.BoxAsync("#ttl-multi .bit-msg-ttl");
        var stackedContent = await Page.BoxAsync("#ttl-multi .bit-msg-cnt");
        Assert.IsTrue(stackedTitle.CenterY < stackedContent.Top && stackedTitle.Left <= stackedContent.Left + 1, "On a multiline message the title sits above.");

        await Expect(Page.Locator("#ttl-template .bit-msg-ttl #ttl-link")).ToHaveTextAsync("your workspace");

        var heading = Page.Locator("#ttl-h3 h3.bit-msg-ttl");
        await Expect(heading).ToHaveTextAsync("A heading title");
        Assert.AreEqual(await ComputedStyleAsync("#ttl-h3 .bit-msg-cnt", "font-size"), await ComputedStyleAsync("#ttl-h3 h3", "font-size"), "A heading title keeps the message's type.");
    }

    [TestMethod]
    public async Task Alignment_EachValue_PlacesTheContentAlongTheWidth()
    {
        await OpenAsync("/e2e/message");

        var expected = new (string Name, string Css)[]
        {
            ("Start", "flex-start"), ("End", "flex-end"), ("Center", "center"), ("SpaceBetween", "space-between"),
            ("SpaceAround", "space-around"), ("SpaceEvenly", "space-evenly"), ("Baseline", "baseline"), ("Stretch", "stretch"),
        };

        foreach (var (name, css) in expected)
        {
            await Expect(Root($"al-{name}")).ToHaveAttributeAsync("style", new Regex($"--bit-msg-justifycontent:{css}"));

            // justify-content has no baseline value, so the browser falls back to normal - the start of the line.
            var computed = name is "Baseline" ? "normal" : css;
            Assert.AreEqual(computed, await ComputedStyleAsync($"#al-{name} .bit-msg-con", "justify-content"), name);
        }

        Assert.AreEqual("flex-start", await ComputedStyleAsync("#al-default .bit-msg-con", "justify-content"));

        var container = await Page.BoxAsync("#al-Center .bit-msg-con");
        var centered = await Page.BoxAsync("#al-Center .bit-msg-cnc");
        Assert.AreEqual(container.CenterX, centered.CenterX, 2, "Center centres the content.");

        var endContainer = await Page.BoxAsync("#al-End .bit-msg-con");
        var end = await Page.BoxAsync("#al-End .bit-msg-cnc");
        var start = await Page.BoxAsync("#al-Start .bit-msg-cnc");
        Assert.IsTrue(end.Right > endContainer.Right - 12 && start.Left < endContainer.Left + 12, "End and Start sit at the two ends.");
    }

    [TestMethod]
    public async Task SquareAndElevation_SetTheCornerAndTheShadow_AtRuntimeToo()
    {
        await OpenAsync("/e2e/message");

        Assert.IsTrue(await NumberAsync("#sq-default", "border-top-left-radius") > 0, "A message is rounded by default.");
        await Expect(Root("sq-square")).ToHaveClassAsync(HasClass("bit-msg-sqr"));
        await Expect(Root("sq-square")).ToHaveCSSAsync("border-top-left-radius", "0px");

        await Expect(Root("el-runtime")).ToHaveCSSAsync("box-shadow", "none");
        await Page.ClickAsync("#btn-el-8");
        await Expect(Root("el-runtime")).ToHaveAttributeAsync("style", new Regex("--bit-msg-boxshadow:var\\(--bit-shd-8\\)"));
        await Expect(Root("el-runtime")).Not.ToHaveCSSAsync("box-shadow", "none");

        await Page.ClickAsync("#btn-el-0");
        await Expect(Root("el-runtime")).ToHaveCSSAsync("box-shadow", "none");

        // Outside the theme's 24 steps the message stays flat.
        await Expect(Root("el-out-of-range")).ToHaveCSSAsync("box-shadow", "none");
    }

    [TestMethod]
    public async Task Dismiss_Dismissible_RemovesItself_AndTheDismissedBindingFollowsBothWays()
    {
        await OpenAsync("/e2e/message");

        var button = Page.Locator("#dm-self .bit-msg-dmb");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Dismiss");
        await Expect(button).ToHaveAttributeAsync("title", "Dismiss");
        await Expect(Page.Locator("#dm-self .bit-msg-dmi")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));

        await button.ClickAsync();
        await Expect(Root("dm-self")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dm-self-dismissed")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#dm-self-count")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-dm-self-back");
        await Expect(Root("dm-self")).ToBeVisibleAsync();

        // Dismissed from the page: taken off without a report of a dismissal it did not do.
        await Page.ClickAsync("#btn-dm-self-dismiss");
        await Expect(Root("dm-self")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dm-self-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Dismiss_OnDismissAlone_ReportsTheDismissal_AndLeavesTheMessage()
    {
        await OpenAsync("/e2e/message");

        await Page.ClickAsync("#dm-report .bit-msg-dmb");
        await Expect(Page.Locator("#dm-report-count")).ToHaveTextAsync("1");
        await Expect(Root("dm-report")).ToBeVisibleAsync();

        await Page.ClickAsync("#dm-report .bit-msg-dmb");
        await Expect(Page.Locator("#dm-report-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Dismiss_OnDismissing_CancelsTheFirstAttempt_AndReportsTheReason()
    {
        await OpenAsync("/e2e/message");

        await Page.ClickAsync("#dm-guarded .bit-msg-dmb");
        await Expect(Page.Locator("#dm-attempts")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#dm-reasons")).ToHaveTextAsync("Button");
        await Expect(Root("dm-guarded")).ToBeVisibleAsync();

        // Escape anywhere inside the message is its own.
        await Page.Locator("#dm-guarded-inner").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dm-attempts")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#dm-reasons")).ToHaveTextAsync("Button,Escape");
        await Expect(Root("dm-guarded")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dm-guarded-dismissed")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task Dismiss_Escape_OnlyThePlainKeyInsideTheMessageDismissesIt()
    {
        await OpenAsync("/e2e/message");

        var root = Root("dm-escape-only");
        await Expect(root).ToHaveAttributeAsync("data-bit-esc", "claim");
        await Expect(root).ToHaveAttributeAsync("tabindex", "0");

        // Outside the message the key is not its own.
        await Page.Locator("#btn-dm-self-back").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        // A modified Escape is left to the surface around it.
        await root.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Escape");
        await Page.Keyboard.PressAsync("Control+Escape");
        await Expect(Page.Locator("#dm-escape-only-dismissed")).ToHaveTextAsync("False");
        await Expect(root).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dm-escape-only-dismissed")).ToHaveTextAsync("True");
        await Expect(root).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Dismiss_DismissAsync_DismissesFromCode_WithTheProgrammaticReason()
    {
        await OpenAsync("/e2e/message");

        await Page.ClickAsync("#btn-dm-method");
        await Expect(Page.Locator("#dm-method-reason")).ToHaveTextAsync("Programmatic");
        await Expect(Page.Locator("#dm-method-dismissed")).ToHaveTextAsync("True");
        await Expect(Root("dm-method")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#dm-labels .bit-msg-dmb")).ToHaveAttributeAsync("aria-label", "Close notice");
    }

    [TestMethod]
    public async Task AutoDismiss_TheCountdown_DismissesTheMessageAfterItsTime()
    {
        await OpenAsync("/e2e/message");

        await Expect(Root("ad-basic")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-ad-basic");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(Root("ad-basic")).ToBeVisibleAsync();

        await Expect(Page.Locator("#ad-basic-dismissed")).ToHaveTextAsync("True", new() { Timeout = 10000 });
        await Expect(Root("ad-basic")).ToHaveCountAsync(0);

        var elapsed = long.Parse(await Page.Locator("#ad-basic-elapsed").TextContentAsync() ?? "-1", CultureInfo.InvariantCulture);
        Assert.IsTrue(elapsed is >= 1150 and < 5000, $"Dismissed after its 1.2s ({elapsed}ms).");
    }

    [TestMethod]
    public async Task AutoDismiss_APointerOverTheMessage_HoldsTheCountdownUntilItLeaves()
    {
        await OpenAsync("/e2e/message");

        await Page.ClickAsync("#btn-ad-basic");
        await Page.HoverAsync("#ad-basic");
        await Expect(Root("ad-basic")).ToBeVisibleAsync();

        await Page.DwellAsync(2200);
        await Expect(Root("ad-basic")).ToBeVisibleAsync();
        await Expect(Page.Locator("#ad-basic-dismissed")).ToHaveTextAsync("False");

        await Page.Mouse.MoveAsync(0, 0);
        await Expect(Page.Locator("#ad-basic-dismissed")).ToHaveTextAsync("True", new() { Timeout = 10000 });

        var elapsed = long.Parse(await Page.Locator("#ad-basic-elapsed").TextContentAsync() ?? "-1", CultureInfo.InvariantCulture);
        Assert.IsTrue(elapsed >= 2800, $"The time under the pointer was not spent ({elapsed}ms).");
    }

    [TestMethod]
    public async Task AutoDismiss_TheFocusInsideTheMessage_HoldsTheCountdownAndTheBar()
    {
        await OpenAsync("/e2e/message");

        await Page.ClickAsync("#btn-ad-progress");
        await Page.Mouse.MoveAsync(0, 0);
        await Page.Locator("#ad-progress-inner").FocusAsync();
        await Expect(Part("ad-progress", "bit-msg-prb")).ToHaveCSSAsync("animation-play-state", "paused");

        await Page.DwellAsync(2200);
        await Expect(Page.Locator("#ad-progress-dismissed")).ToHaveTextAsync("False");

        await Page.Locator("#btn-ad-resume").FocusAsync();
        await Expect(Page.Locator("#ad-progress-dismissed")).ToHaveTextAsync("True", new() { Timeout = 10000 });

        var elapsed = long.Parse(await Page.Locator("#ad-progress-elapsed").TextContentAsync() ?? "-1", CultureInfo.InvariantCulture);
        Assert.IsTrue(elapsed >= 3000, $"The time with the focus inside was not spent ({elapsed}ms).");
    }

    [TestMethod]
    public async Task AutoDismiss_TheProgressBar_RunsDown_AndPauseAndResumeHoldIt()
    {
        await OpenAsync("/e2e/message");

        await Page.ClickAsync("#btn-ad-progress");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(Root("ad-progress")).ToBeVisibleAsync();

        // A Dismissed binding alone counts down too: there is no button.
        await Expect(Page.Locator("#ad-progress .bit-msg-cnt")).ToBeVisibleAsync();
        await Expect(Page.Locator("#ad-progress .bit-msg-dmb")).ToHaveCountAsync(0);

        var bar = Part("ad-progress", "bit-msg-prb");
        await Expect(Part("ad-progress", "bit-msg-prg")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(bar).ToHaveAttributeAsync("style", new Regex("animation-duration:1500ms"));
        await Expect(bar).ToHaveCSSAsync("animation-name", "bit-msg-countdown");

        await Page.ClickAsync("#btn-ad-pause");
        await Expect(bar).ToHaveClassAsync(HasClass("bit-msg-pau"));
        await Expect(bar).ToHaveCSSAsync("animation-play-state", "paused");
        var heldAt = await ScaleXAsync("#ad-progress .bit-msg-prb");
        Assert.IsTrue(heldAt < 1, $"The bar has run down some way ({heldAt}).");

        await Page.DwellAsync(1800);
        await Expect(Page.Locator("#ad-progress-dismissed")).ToHaveTextAsync("False");
        Assert.AreEqual(heldAt, await ScaleXAsync("#ad-progress .bit-msg-prb"), 0.02, "The paused bar stays where it was.");

        await Page.ClickAsync("#btn-ad-resume");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(bar).Not.ToHaveClassAsync(HasClass("bit-msg-pau"));
        await Expect(Page.Locator("#ad-progress-dismissed")).ToHaveTextAsync("True", new() { Timeout = 10000 });
    }

    [TestMethod]
    public async Task AutoDismiss_RefusedByOnDismissing_StartsTheCountdownOver()
    {
        await OpenAsync("/e2e/message");

        await Page.ClickAsync("#btn-ad-refused");
        await Page.Mouse.MoveAsync(0, 0);
        var firstBar = await Part("ad-refused", "bit-msg-prb").ElementHandleAsync();

        await Expect(Page.Locator("#ad-refusals")).ToHaveTextAsync("1", new() { Timeout = 10000 });
        await Expect(Root("ad-refused")).ToBeVisibleAsync();

        // The bar is a new element drawing the new countdown.
        await Page.WaitForFunctionAsync("el => !el.isConnected", firstBar);

        await Expect(Page.Locator("#ad-refused-dismissed")).ToHaveTextAsync("True", new() { Timeout = 10000 });
        await Expect(Page.Locator("#ad-refusals")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task AutoDismiss_ABlurredWindowOrAHiddenPage_HoldsTheCountdown()
    {
        await OpenAsync("/e2e/message");

        await Page.ClickAsync("#btn-ad-blur");
        await Page.Mouse.MoveAsync(0, 0);
        await Page.EvaluateAsync("() => window.dispatchEvent(new Event('blur'))");

        await Page.DwellAsync(2000);
        await Expect(Page.Locator("#ad-blur-dismissed")).ToHaveTextAsync("False");

        // The page hidden holds it as well, while the window has the focus back.
        await Page.EvaluateAsync(@"() => {
            Object.defineProperty(document, 'hidden', { configurable: true, get: () => true });
            document.dispatchEvent(new Event('visibilitychange'));
            window.dispatchEvent(new Event('focus'));
        }");

        await Page.DwellAsync(2000);
        await Expect(Page.Locator("#ad-blur-dismissed")).ToHaveTextAsync("False");

        await Page.EvaluateAsync(@"() => {
            Object.defineProperty(document, 'hidden', { configurable: true, get: () => false });
            document.dispatchEvent(new Event('visibilitychange'));
        }");

        await Expect(Page.Locator("#ad-blur-dismissed")).ToHaveTextAsync("True", new() { Timeout = 10000 });
    }

    [TestMethod]
    public async Task AutoDismiss_ATimeSetAtRuntime_ArmsTheCountdown()
    {
        await OpenAsync("/e2e/message");

        await Expect(Root("ad-time")).ToBeVisibleAsync();
        await Page.ClickAsync("#btn-ad-time");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(Page.Locator("#ad-time-dismissed")).ToHaveTextAsync("True", new() { Timeout = 10000 });
    }

    [TestMethod]
    public async Task Actions_ShareTheLineOnASingleLine_AndGetARowOfTheirOwnOnMultiline()
    {
        await OpenAsync("/e2e/message");

        await Expect(Page.Locator("#act-single > .bit-msg-rct > .bit-msg-act")).ToHaveCountAsync(1);
        var single = await Page.BoxAsync("#act-single .bit-msg-cnc");
        var singleAction = await Page.BoxAsync("#act-restore");
        Assert.IsTrue(singleAction.Left >= single.Right - 1, "The action follows the content on its line.");
        Assert.IsTrue(singleAction.Top < single.Bottom && singleAction.Bottom > single.Top, "On the same line.");

        await Expect(Page.Locator("#act-multi > .bit-msg-act.bit-msg-mac")).ToHaveCountAsync(1);
        var multi = await Page.BoxAsync("#act-multi .bit-msg-cnc");
        var multiAction = await Page.BoxAsync("#act-retry");
        Assert.IsTrue(multiAction.Top >= multi.Bottom - 1, "On a multiline message the actions sit below the content.");

        await Page.ClickAsync("#act-restore");
        await Page.ClickAsync("#act-retry");
        await Expect(Page.Locator("#action-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Icons_CustomGlyphsTemplateAndHideIcon_ReplaceOrRemoveTheIcons()
    {
        await OpenAsync("/e2e/message");

        await Expect(Part("ic-custom", "bit-msg-ico")).ToHaveClassAsync(HasClass("bit-icon--CheckMark"));
        await Expect(Part("ic-dismiss", "bit-msg-dmi")).ToHaveClassAsync(HasClass("bit-icon--Blocked2Solid"));

        var expanderIcon = Part("ic-expander", "bit-msg-exi");
        await Expect(expanderIcon).ToHaveClassAsync(HasClass("bit-icon--ChevronDownEnd"));
        await Expect(expanderIcon).Not.ToHaveClassAsync(HasClass("bit-ico-r180"));
        await Page.ClickAsync("#ic-expander .bit-msg-exb");
        await Expect(expanderIcon).ToHaveClassAsync(HasClass("bit-icon--ChevronUpEnd"));

        await Expect(Page.Locator("#ic-template .bit-msg-ict #ic-template-mark")).ToBeVisibleAsync();
        await Expect(Page.Locator("#ic-template .bit-msg-ico")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#ic-hidden .bit-msg-ict")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Color_EachColor_HasItsDefaultIconAndItsUrgency()
    {
        await OpenAsync("/e2e/message");

        var icons = new Dictionary<string, string> { ["Success"] = "Completed", ["Warning"] = "Warning", ["SevereWarning"] = "WarningSolid", ["Error"] = "ErrorBadge" };

        foreach (var (name, _, _) in NotificationsE2EHelpers.Colors)
        {
            var icon = icons.GetValueOrDefault(name, "Info");
            await Expect(Part($"clr-{name}", "bit-msg-ico")).ToHaveClassAsync(HasClass($"bit-icon--{icon}"));

            var role = name is "Warning" or "SevereWarning" or "Error" ? "alert" : "status";
            await Expect(Part($"clr-{name}", "bit-msg-cnc")).ToHaveAttributeAsync("role", role);
        }
    }

    [TestMethod]
    public async Task Color_EachColor_PaintsItsRoleInEveryVariant()
    {
        await OpenAsync("/e2e/message");

        foreach (var (name, suffix, token) in NotificationsE2EHelpers.Colors)
        {
            var main = await Page.ResolveColorAsync(token);
            var on = await Page.ResolveColorAsync(OnTokens[suffix]);
            var fg = await Page.ResolveColorAsync(FgTokens[suffix]);
            var tint = SemanticSuffixes.Contains(suffix)
                ? await Page.ResolveColorAsync($"--bit-clr-{suffix}-tint")
                : await Page.ResolveCssColorAsync($"color-mix(in srgb, var({token}) 10%, transparent)");

            await Expect(Root($"clr-{name}")).ToHaveClassAsync(HasClass($"bit-msg-{suffix}"));
            await Expect(Root($"clr-{name}")).ToHaveCSSAsync("background-color", main);
            await Expect(Root($"clr-{name}")).ToHaveCSSAsync("color", on);

            await Expect(Root($"clr-otl-{name}")).ToHaveCSSAsync("border-top-color", main);
            await Expect(Root($"clr-otl-{name}")).ToHaveCSSAsync("color", fg);

            await Expect(Root($"clr-tnt-{name}")).ToHaveCSSAsync("background-color", tint);
            await Expect(Root($"clr-tnt-{name}")).ToHaveCSSAsync("color", fg);
        }
    }

    [TestMethod]
    public async Task Color_ChangedAtRuntime_RepaintsTheMessageAndSwapsTheIcon()
    {
        await OpenAsync("/e2e/message");

        await Expect(Root("clr-runtime")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-inf"));
        await Page.ClickAsync("#btn-color-success");
        await Expect(Root("clr-runtime")).ToHaveClassAsync(HasClass("bit-msg-suc"));
        await Expect(Root("clr-runtime")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-suc"));
        await Expect(Part("clr-runtime", "bit-msg-ico")).ToHaveClassAsync(HasClass("bit-icon--Completed"));
    }

    [TestMethod]
    public async Task Accessibility_RoleAndPoliteness_SetHowTheTextIsAnnounced()
    {
        await OpenAsync("/e2e/message");

        await Expect(Part("a11y-role", "bit-msg-cnc")).ToHaveAttributeAsync("role", "status");

        await Expect(Part("a11y-polite", "bit-msg-cnc")).ToHaveAttributeAsync("role", "alert");
        await Expect(Part("a11y-polite", "bit-msg-cnc")).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(Part("a11y-assertive", "bit-msg-cnc")).ToHaveAttributeAsync("aria-live", "assertive");
        await Expect(Part("a11y-off", "bit-msg-cnc")).ToHaveAttributeAsync("aria-live", "off");
        await Expect(Part("basic-msg", "bit-msg-cnc")).Not.ToHaveAttributeAsync("aria-live", Any);

        await Expect(Part("a11y-none", "bit-msg-cnc")).ToHaveAttributeAsync("role", "none");
    }

    [TestMethod]
    public async Task Accessibility_IconAriaLabel_IsSaidFirstInTheRegion_AndNeverSeen()
    {
        await OpenAsync("/e2e/message");

        var label = Page.Locator("#a11y-icon-label .bit-msg-cnw > .bit-msg-ilb");
        await Expect(label).ToHaveTextAsync("Error");
        Assert.IsTrue(await label.EvaluateAsync<bool>("el => el.parentElement.firstElementChild === el"), "The label comes first.");
        Assert.IsTrue((await label.BoxAsync()).Width <= 1, "The label is visually hidden.");
        await Expect(Part("a11y-icon-label", "bit-msg-cnc")).ToHaveAttributeAsync("role", "alert");
    }

    [TestMethod]
    public async Task Accessibility_AriaLabel_NamesTheMessageAsAGroup()
    {
        await OpenAsync("/e2e/message");

        await Expect(Root("a11y-aria-label")).ToHaveAttributeAsync("role", "group");
        await Expect(Root("a11y-aria-label")).ToHaveAttributeAsync("aria-label", "Import report");
        await Expect(Root("a11y-aria-label")).Not.ToHaveAttributeAsync("aria-labelledby", Any);
        await Expect(Root("a11y-aria-label")).ToHaveAccessibleNameAsync("Import report");
    }

    [TestMethod]
    public async Task Accessibility_DelayedAnnouncement_PutsTheRegionOnThePageBeforeItsText()
    {
        await OpenAsync("/e2e/message");

        await Page.EvaluateAsync(@"() => {
            window.__delayed = [];
            new MutationObserver(() => {
                const region = document.querySelector('#a11y-delayed .bit-msg-cnc');
                if (region) window.__delayed.push(region.textContent.trim());
            }).observe(document.body, { subtree: true, childList: true, characterData: true });
        }");

        await Page.ClickAsync("#btn-delayed");
        await Expect(Part("a11y-delayed", "bit-msg-cnt")).ToHaveTextAsync("Announced one render later.");

        var seen = await Page.EvaluateAsync<string[]>("() => window.__delayed");
        Assert.IsTrue(seen.Length >= 2, $"The region was seen {seen.Length} time(s).");
        Assert.AreEqual("", seen[0], "The region reached the page empty.");
        StringAssert.Contains(seen[^1], "Announced one render later.");
    }

    [TestMethod]
    public async Task Accessibility_AutoFocus_TakesTheFocusEveryTimeTheMessageIsShown()
    {
        await OpenAsync("/e2e/message");

        await Page.ClickAsync("#btn-autofocus");
        var root = Root("a11y-autofocus");
        await Expect(root).ToBeFocusedAsync();
        await Expect(root).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(root).ToHaveAttributeAsync("aria-describedby", "a11y-autofocus-cnt");
        await Expect(root).ToHaveAttributeAsync("aria-labelledby", "a11y-autofocus-ttl");

        // Its button is described by the title, so each "Dismiss" is told apart.
        var dismiss = Page.Locator("#a11y-autofocus .bit-msg-dmb");
        await Expect(dismiss).ToHaveAttributeAsync("aria-describedby", "a11y-autofocus-ttl");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(dismiss).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(root).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-autofocus");
        await Expect(Root("a11y-autofocus")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Accessibility_FocusAsync_AndTheTabKey_ReachAFocusableMessage_WithItsRing()
    {
        await OpenAsync("/e2e/message");

        await Page.ClickAsync("#btn-focus");
        await Expect(Root("a11y-focusable")).ToBeFocusedAsync();
        await Expect(Root("a11y-focusable")).ToHaveAttributeAsync("aria-describedby", "a11y-focusable-cnt");

        await Page.Locator("#btn-autofocus").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Root("a11y-focusable")).ToBeFocusedAsync();
        Assert.AreNotEqual("none", await ComputedStyleAsync("#a11y-focusable", "box-shadow"), "The focus ring is drawn.");
    }

    [TestMethod]
    public async Task Disabled_ButtonsEscapeAndCountdownStop_AndEnablingStartsTheCountdown()
    {
        await OpenAsync("/e2e/message");

        var root = Root("dis-msg");
        var dismiss = Page.Locator("#dis-msg .bit-msg-dmb");
        var expander = Page.Locator("#dis-msg .bit-msg-exb");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        foreach (var button in new[] { dismiss, expander })
        {
            await Expect(button).ToBeDisabledAsync();
            await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
            await Expect(button).ToHaveCSSAsync("pointer-events", "none");
        }

        Assert.IsTrue(await NumberAsync("#dis-msg .bit-msg-dmb", "opacity") < 1, "The disabled buttons are dimmed.");
        await Expect(root).Not.ToHaveAttributeAsync("data-bit-esc", Any);

        await dismiss.ClickAsync(new() { Force = true });
        await expander.DispatchEventAsync("click");
        await Expect(expander).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#dis-dismiss-count")).ToHaveTextAsync("0");

        // No countdown runs while disabled, and no bar is drawn.
        await Page.ClickAsync("#btn-dis-arm");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(Page.Locator("#dis-msg .bit-msg-prg")).ToHaveCountAsync(0);
        await Page.DwellAsync(1500);
        await Expect(Page.Locator("#dis-dismissed")).ToHaveTextAsync("False");

        // The text stays readable.
        await Expect(Part("dis-msg", "bit-msg-cnt")).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-dis-toggle");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(root).ToHaveAttributeAsync("data-bit-esc", "claim");
        await Expect(Page.Locator("#dis-dismissed")).ToHaveTextAsync("True", new() { Timeout = 10000 });
        await Expect(Page.Locator("#dis-dismiss-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Cascade_BitParams_SetsTheDefaultsAndAnOwnValueWins()
    {
        await OpenAsync("/e2e/message");

        await Expect(Root("cas-1")).ToHaveClassAsync(HasClass("bit-msg-otl"));
        await Expect(Root("cas-1")).ToHaveClassAsync(HasClass("bit-msg-sqr"));
        await Expect(Root("cas-1")).ToHaveCSSAsync("border-top-left-radius", "0px");

        // Truncate comes down the cascade, and a long text gets its expander.
        await Expect(Page.Locator("#cas-2 .bit-msg-exb")).ToHaveCountAsync(1);

        await Expect(Root("cas-3")).ToHaveClassAsync(HasClass("bit-msg-fil"));
        await Expect(Root("cas-3")).Not.ToHaveClassAsync(HasClass("bit-msg-otl"));
        await Expect(Root("cas-3")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-err"));
    }

    [TestMethod]
    public async Task ExternalIcons_TheIconInfos_RenderTheirClassesOnEveryIcon()
    {
        await OpenAsync("/e2e/message");

        await Expect(Part("ext-string", "bit-msg-ico")).ToHaveClassAsync(HasClass("fa-circle-info"));
        await Expect(Part("ext-string", "bit-msg-ico")).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(Part("ext-dismiss", "bit-msg-dmi")).ToHaveClassAsync(HasClass("fa-xmark"));
        await Expect(Part("ext-bi", "bit-msg-ico")).ToHaveClassAsync(HasClass("bi-check-circle-fill"));
        await Expect(Part("ext-expander", "bit-msg-ico")).ToHaveClassAsync(HasClass("bi-x-octagon-fill"));

        var expanderIcon = Part("ext-expander", "bit-msg-exi");
        await Expect(expanderIcon).ToHaveClassAsync(HasClass("bi-chevron-double-down"));
        await Expect(expanderIcon).Not.ToHaveClassAsync(HasClass("bit-ico-r180"));
        await Page.ClickAsync("#ext-expander .bit-msg-exb");
        await Expect(expanderIcon).ToHaveClassAsync(HasClass("bi-chevron-double-up"));
    }

    [TestMethod]
    public async Task Size_EachSize_ScalesTheTypeIconButtonsAndTrackTogether()
    {
        await OpenAsync("/e2e/message");

        var fonts = new List<double>();
        var icons = new List<double>();
        var buttons = new List<double>();
        var tracks = new List<double>();
        foreach (var (name, suffix) in new[] { ("Small", "sm"), ("Medium", "md"), ("Large", "lg") })
        {
            await Expect(Root($"size-{name}")).ToHaveClassAsync(HasClass($"bit-msg-{suffix}"));
            fonts.Add(await NumberAsync($"#size-{name}", "font-size"));
            icons.Add(await NumberAsync($"#size-{name} .bit-msg-ico", "font-size"));
            buttons.Add((await Page.BoxAsync($"#size-{name} .bit-msg-dmb")).Height);
            tracks.Add((await Page.BoxAsync($"#size-{name} .bit-msg-prg")).Height);
        }

        foreach (var (what, values) in new[] { ("type", fonts), ("icon", icons), ("button", buttons), ("track", tracks) })
        {
            Assert.IsTrue(values[0] < values[1] && values[1] < values[2], $"The {what} grows with the size ({string.Join(", ", values)}).");
        }
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRootAndStylesAndClassesOnEveryPart()
    {
        await OpenAsync("/e2e/message");

        await Expect(Root("sc-style")).ToHaveCSSAsync("padding-top", "8px");
        await Expect(Root("sc-style")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Root("sc-class")).ToHaveClassAsync(HasClass("custom-class"));

        var styles = new (string Part, string Property, string Value)[]
        {
            ("#sc-styles", "margin-top", "3px"),
            ("#sc-styles .bit-msg-rct", "padding-top", "1px"),
            ("#sc-styles .bit-msg-con", "padding-bottom", "2px"),
            ("#sc-styles .bit-msg-ict", "opacity", "0.8"),
            ("#sc-styles .bit-msg-ico", "color", "rgb(255, 0, 0)"),
            ("#sc-styles .bit-msg-ilb", "font-size", "5px"),
            ("#sc-styles .bit-msg-cnc", "padding-left", "3px"),
            ("#sc-styles .bit-msg-cnw", "padding-right", "4px"),
            ("#sc-styles .bit-msg-ttl", "color", "rgb(139, 0, 0)"),
            ("#sc-styles .bit-msg-cnt", "color", "rgb(138, 43, 226)"),
            ("#sc-styles .bit-msg-act", "column-gap", "13px"),
            ("#sc-styles .bit-msg-exb", "opacity", "0.7"),
            ("#sc-styles .bit-msg-exi", "color", "rgb(0, 128, 0)"),
            ("#sc-styles .bit-msg-dmb", "opacity", "0.6"),
            ("#sc-styles .bit-msg-dmi", "font-size", "17px"),
            ("#sc-styles .bit-msg-prg", "height", "7px"),
            ("#sc-styles .bit-msg-prb", "opacity", "0.5"),
        };

        foreach (var (part, property, value) in styles)
        {
            await Expect(Page.Locator(part).First).ToHaveCSSAsync(property, value);
        }

        var classes = new (string Part, string Class)[]
        {
            ("#sc-classes", "c-root"), ("#sc-classes .bit-msg-rct", "c-rct"), ("#sc-classes .bit-msg-con", "c-con"),
            ("#sc-classes .bit-msg-ict", "c-ict"), ("#sc-classes .bit-msg-ico", "c-ico"), ("#sc-classes .bit-msg-ilb", "c-ilb"),
            ("#sc-classes .bit-msg-cnc", "c-cnc"), ("#sc-classes .bit-msg-cnw", "c-cnw"), ("#sc-classes .bit-msg-ttl", "c-ttl"),
            ("#sc-classes .bit-msg-cnt", "c-cnt"), ("#sc-classes .bit-msg-act", "c-act"), ("#sc-classes .bit-msg-exb", "c-exb"),
            ("#sc-classes .bit-msg-exi", "c-exi"), ("#sc-classes .bit-msg-dmb", "c-dmb"), ("#sc-classes .bit-msg-dmi", "c-dmi"),
            ("#sc-classes .bit-msg-prg", "c-prg"), ("#sc-classes .bit-msg-prb", "c-prb"),
        };

        foreach (var (part, @class) in classes)
        {
            await Expect(Page.Locator(part).First).ToHaveClassAsync(HasClass(@class));
        }
    }

    [TestMethod]
    public async Task Rtl_ExplicitAndInherited_MirrorTheIconTheButtonsAndTheBar()
    {
        await OpenAsync("/e2e/message");

        await Expect(Root("rtl-msg")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root("rtl-msg")).ToHaveClassAsync(HasClass("bit-rtl"));

        foreach (var id in new[] { "rtl-msg", "rtl-inherited" })
        {
            var icon = await Page.BoxAsync($"#{id} .bit-msg-ict");
            var content = await Page.BoxAsync($"#{id} .bit-msg-cnc");
            var dismiss = await Page.BoxAsync($"#{id} .bit-msg-dmb");
            Assert.IsTrue(icon.Left >= content.Right - 1, $"{id}: the icon is on the right.");
            Assert.IsTrue(dismiss.Right <= content.Left + 1, $"{id}: the button is on the left.");

            var origin = await ComputedStyleAsync($"#{id} .bit-msg-prb", "transform-origin");
            var width = (await Page.BoxAsync($"#{id} .bit-msg-prb")).Width;
            Assert.AreEqual(width, NotificationsE2EHelpers.Px(origin.Split(' ')[0]), 1, $"{id}: the bar runs down towards the right ({origin}).");
        }

        var ltrIcon = await Page.BoxAsync("#ltr-msg .bit-msg-ict");
        var ltrContent = await Page.BoxAsync("#ltr-msg .bit-msg-cnc");
        Assert.IsTrue(ltrIcon.Right <= ltrContent.Left + 1, "Left to right the icon leads from the left.");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RestyleTheDefault_AndTheParametersWinOverThem()
    {
        await OpenAsync("/e2e/message");

        var root = Root("cv-msg");
        await Expect(root).ToHaveCSSAsync("background-color", "rgb(102, 51, 153)");
        await Expect(root).ToHaveCSSAsync("color", "rgb(255, 255, 255)");
        await Expect(root).ToHaveCSSAsync("border-top-color", "rgb(0, 128, 0)");
        await Expect(root).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(root).ToHaveCSSAsync("border-top-left-radius", "9px");
        await Expect(root).ToHaveCSSAsync("font-size", "19px");
        await Expect(root).ToHaveCSSAsync("box-shadow", "rgb(0, 0, 0) 0px 0px 5px 0px");
        await Expect(Part("cv-msg", "bit-msg-con")).ToHaveCSSAsync("line-height", "30px");
        await Expect(Part("cv-msg", "bit-msg-ttl")).ToHaveCSSAsync("color", "rgb(255, 215, 0)");
        await Expect(Part("cv-msg", "bit-msg-ttl")).ToHaveCSSAsync("font-weight", "300");
        await Expect(Part("cv-msg", "bit-msg-ict")).ToHaveCSSAsync("color", "rgb(0, 255, 255)");
        await Expect(Part("cv-msg", "bit-msg-ico")).ToHaveCSSAsync("font-size", "26px");
        await Expect(Part("cv-msg", "bit-msg-prb")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
        await Expect(Part("cv-msg", "bit-msg-prg")).ToHaveCSSAsync("height", "6px");

        await Page.Locator("#cv-msg .bit-msg-dmb").FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#cv-msg .bit-msg-dmb")).ToBeFocusedAsync();
        StringAssert.Contains(await ComputedStyleAsync("#cv-msg .bit-msg-dmb", "box-shadow"), "rgb(255, 0, 255)");

        var explicitRoot = Root("cv-explicit");
        await Expect(explicitRoot).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-err"));
        await Expect(explicitRoot).ToHaveCSSAsync("border-top-left-radius", "0px");
        Assert.IsTrue(await NumberAsync("#cv-explicit", "font-size") < 19, "An explicit Size wins over the font-size variable.");
        Assert.AreNotEqual("rgb(0, 0, 0) 0px 0px 5px 0px", await ComputedStyleAsync("#cv-explicit", "box-shadow"), "Elevation wins over the shadow variable.");

        await Expect(Root("cv-twin-outline")).ToHaveCSSAsync("border-top-color", "rgb(255, 0, 0)");
        await Expect(Root("cv-twin-fill")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("--bit-clr-inf"));
    }

    [TestMethod]
    public async Task Dialog_EscapeInTheMessage_DismissesIt_AndTheDialogClosesOnTheNextPress()
    {
        await OpenAsync("/e2e/message");

        await Page.ClickAsync("#btn-open-dialog");
        var message = Root("dlg-msg");
        await Expect(message).ToBeVisibleAsync();

        await Page.Locator("#dlg-msg .bit-msg-dmb").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-message-dismissed")).ToHaveTextAsync("True");
        await Expect(message).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#dlg-inner-btn")).ToBeVisibleAsync();

        await Page.Locator("#dlg-inner-btn").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#dlg-inner-btn")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task BaseParameters_VisibilityAndHtmlAttributes_LandOnTheRoot()
    {
        await OpenAsync("/e2e/message");

        await Expect(Root("vis-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Root("vis-collapsed")).ToHaveCSSAsync("display", "none");
        await Expect(Root("html-attrs")).ToHaveAttributeAsync("data-test", "msg-attr");

        // An aria attribute the page wrote is kept rather than wiped by the message's own.
        await Expect(Root("html-attrs")).ToHaveAttributeAsync("aria-describedby", "basic");
    }

    [TestMethod]
    public async Task Unmount_RemovingMessagesWithObserversAndCountdowns_LeavesNoError()
    {
        await OpenAsync("/e2e/message");

        await Expect(Page.Locator("#unmount-host .bit-msg")).ToHaveCountAsync(2);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-host")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-host .bit-msg")).ToHaveCountAsync(2);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-host")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
