using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiMessageActions at /e2e/aimessageactions in a real browser: the row of copy, regenerate and feedback
/// buttons with their names and their glyphs of the AI font, feedback pressed and withdrawn (aria-pressed, the solid
/// glyph, the primary color, OnFeedback) by pointer and keyboard, regenerate replacing the answer, the reveal on hover
/// and on focus and Pinned, the metadata (model, usage, time) shown by the options and kept when the actions are
/// switched off, ShowFeedback, CanRegenerate, ExtraActionsTemplate, Disabled, every --bit-AiMessageActions-* variable,
/// Styles/Classes, RTL and print.
/// </summary>
[TestClass]
public class BitAiMessageActionsE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aimessageactions";

    private ILocator Row(string id) => Page.Locator($"#{id}");

    private ILocator Button(string id, string name) => Row(id).Locator($"button[aria-label='{name}']");

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_TheRow_OffersCopyRegenerateAndFeedbackInOrder()
    {
        await OpenAsync(Route);

        var row = Row("mac-basic");
        await Expect(row).ToHaveClassAsync(HasClass("bit-aimac"));
        await Expect(row).ToHaveClassAsync(HasClass("bit-aimac-pin"));
        Assert.AreEqual("Copy,Regenerate,Good response,Bad response",
                        string.Join(",", await row.Locator(".bit-aimac-act button").EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('aria-label'))")));

        foreach (var name in new[] { "Copy", "Regenerate", "Good response", "Bad response" })
        {
            await Expect(Button("mac-basic", name)).ToHaveAttributeAsync("title", name);
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true }).First).ToBeVisibleAsync();
        }

        await Expect(Button("mac-basic", "Good response")).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Button("mac-basic", "Bad response")).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [TestMethod]
    public async Task Basic_TheGlyphs_AreDrawnFromTheAiFont()
    {
        await OpenAsync(Route);

        await Button("mac-basic", "Regenerate").Locator("i.bit-btn-icn").AssertAiGlyphAsync("Refresh");
        await Button("mac-basic", "Good response").Locator("i.bit-btn-icn").AssertAiGlyphAsync("Like");
        await Button("mac-basic", "Bad response").Locator("i.bit-btn-icn").AssertAiGlyphAsync("Dislike");
        await Button("mac-basic", "Copy").Locator("i.bit-btn-icn").AssertAiGlyphAsync("Copy");
    }

    [TestMethod]
    public async Task Basic_TheThemeTokens_PaintTheButtonsNeutralAndTheMetadataDim()
    {
        await OpenAsync(Route);

        await Expect(Button("mac-basic", "Regenerate")).ToHaveClassAsync(HasClass("bit-btn-dft-ter"));
        await Expect(Row("mac-meta")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-ter)"));
        await Expect(Row("mac-meta")).ToHaveCSSAsync("font-size", await Row("mac-meta").ResolveLengthInAsync("var(--bit-tpg-fs-xs)"));
    }

    // ---------- feedback ----------

    [TestMethod]
    public async Task Feedback_GoodPressed_IsPressedSolidAndPrimaryAndPressingItAgainWithdrawsIt()
    {
        await OpenAsync(Route);

        var good = Button("mac-feedback", "Good response");
        await good.ClickAsync();

        await Expect(good).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(good).ToHaveClassAsync(HasClass("bit-btn-dft-pri"));
        await good.Locator("i.bit-btn-icn").AssertAiGlyphAsync("LikeSolid");
        await Expect(good.Locator("i.bit-btn-icn")).Not.ToHaveClassAsync(HasClass("bit-icon-ai--Like"));
        await Expect(Page.Locator("#mac-feedback-log")).ToHaveTextAsync("Positive");
        await Expect(Page.Locator("#mac-feedback-state")).ToHaveTextAsync("Positive");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(good).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-pri)"));

        await good.ClickAsync();
        await Expect(good).ToHaveAttributeAsync("aria-pressed", "false");
        await good.Locator("i.bit-btn-icn").AssertAiGlyphAsync("Like");
        await Expect(Page.Locator("#mac-feedback-log")).ToHaveTextAsync("Positive,None");
        await Expect(Page.Locator("#mac-feedback-state")).ToHaveTextAsync("None");
    }

    [TestMethod]
    public async Task Feedback_BadAfterGood_SwitchesThePressedButton()
    {
        await OpenAsync(Route);

        await Button("mac-feedback", "Good response").ClickAsync();
        await Expect(Button("mac-feedback", "Good response")).ToHaveAttributeAsync("aria-pressed", "true");

        await Button("mac-feedback", "Bad response").ClickAsync();
        await Expect(Button("mac-feedback", "Bad response")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Button("mac-feedback", "Good response")).ToHaveAttributeAsync("aria-pressed", "false");
        await Button("mac-feedback", "Bad response").Locator("i.bit-btn-icn").AssertAiGlyphAsync("DislikeSolid");
        await Expect(Page.Locator("#mac-feedback-log")).ToHaveTextAsync("Positive,Negative");
    }

    [TestMethod]
    public async Task Feedback_WithTheKeyboard_EnterAndSpaceTogglePressed()
    {
        await OpenAsync(Route);

        await Button("mac-feedback", "Copy").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Button("mac-feedback", "Good response")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Button("mac-feedback", "Good response")).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Button("mac-feedback", "Bad response")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Button("mac-feedback", "Bad response")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Button("mac-feedback", "Good response")).ToHaveAttributeAsync("aria-pressed", "false");

        // The focus stays on the button through the re-render.
        await Expect(Button("mac-feedback", "Bad response")).ToBeFocusedAsync();
        await Expect(Page.Locator("#mac-feedback-log")).ToHaveTextAsync("Positive,Negative");
    }

    // ---------- regenerate ----------

    [TestMethod]
    public async Task Regenerate_Pressed_ReplacesTheAnswerWithANewOne()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#mac-regen-text")).ToHaveTextAsync("The original answer");
        await Expect(Page.Locator("#mac-regen-id")).ToHaveTextAsync("regen-a1");

        await Button("mac-regen", "Regenerate").ClickAsync();

        await Expect(Page.Locator("#mac-regen-text")).ToHaveTextAsync("You said: The question");
        await Expect(Page.Locator("#mac-regen-id")).Not.ToHaveTextAsync("regen-a1");
        await Expect(Page.Locator("#mac-regen-count")).ToHaveTextAsync("2");
        await Expect(Button("mac-regen", "Regenerate")).ToBeVisibleAsync();
    }

    // ---------- reveal ----------

    [TestMethod]
    public async Task Reveal_TheActions_AppearUnderThePointerOrTheFocusAndStayWhenPinned()
    {
        await OpenAsync(Route);
        await Page.Mouse.MoveAsync(0, 0);

        var actions = Row("mac-hover").Locator(".bit-aimac-act");
        await Expect(actions).ToHaveCSSAsync("opacity", "0");
        await Expect(Row("mac-pinned").Locator(".bit-aimac-act")).ToHaveCSSAsync("opacity", "1");

        await Row("mac-hover").HoverAsync();
        await Expect(actions).ToHaveCSSAsync("opacity", "1");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(actions).ToHaveCSSAsync("opacity", "0");

        await Button("mac-hover", "Good response").FocusAsync();
        await Expect(actions).ToHaveCSSAsync("opacity", "1");
        await Button("mac-hover", "Good response").BlurAsync();
        await Expect(actions).ToHaveCSSAsync("opacity", "0");

        // The reveal is a transition of the motion tokens.
        StringAssert.Contains(await ComputedStyleAsync("#mac-hover .bit-aimac-act", "transition-property"), "opacity");
    }

    [TestMethod]
    public async Task Reveal_UnderReducedMotion_TheTransitionCollapses()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        Assert.IsTrue(AiE2EHelpers.ParseSeconds(await ComputedStyleAsync("#mac-hover .bit-aimac-act", "transition-duration")) <= 0.001, "The reveal is immediate.");
    }

    // ---------- metadata, options ----------

    [TestMethod]
    public async Task Metadata_ShownByTheOptions_NamesTheModelTheUsageAndTheTime()
    {
        await OpenAsync(Route);

        var row = Row("mac-meta");
        await Expect(row.Locator(".bit-aimac-mdl")).ToHaveTextAsync("E2E Large");
        await Expect(row.Locator(".bit-aituv")).ToBeVisibleAsync();
        var time = row.Locator("time.bit-aimac-tim");
        await Expect(time).ToHaveAttributeAsync("datetime", new Regex(@"^2026-03-04T09:30:00Z$"));
        await Expect(time).ToHaveCSSAsync("font-variant-numeric", "tabular-nums");
        await Expect(time).Not.ToHaveTextAsync("");

        // The metadata is always shown, even while the actions are hidden.
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(row.Locator(".bit-aimac-act")).ToHaveCSSAsync("opacity", "0");
        await Expect(row.Locator(".bit-aimac-mdl")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Metadata_ActionsSwitchedOff_LeaveTheMetadataAlone()
    {
        await OpenAsync(Route);

        await Expect(Row("mac-noactions").Locator(".bit-aimac-act")).ToHaveCountAsync(0);
        await Expect(Row("mac-noactions").Locator(".bit-aimac-mdl")).ToHaveTextAsync("E2E Large");

        // With the default options none of the metadata is shown.
        await Expect(Row("mac-basic").Locator(".bit-aimac-mdl")).ToHaveCountAsync(0);
        await Expect(Row("mac-basic").Locator("time")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Options_ShowFeedbackCanRegenerateAndExtraActions_ShapeTheRow()
    {
        await OpenAsync(Route);

        await Expect(Button("mac-nofeedback", "Good response")).ToHaveCountAsync(0);
        await Expect(Button("mac-nofeedback", "Regenerate")).ToHaveCountAsync(1);
        await Expect(Button("mac-noregen", "Regenerate")).ToHaveCountAsync(0);
        await Expect(Button("mac-noregen", "Good response")).ToHaveCountAsync(1);

        // No session, no regeneration; the extra action goes at the end of the row.
        await Expect(Button("mac-extra", "Regenerate")).ToHaveCountAsync(0);
        Assert.IsTrue(await Page.Locator("#mac-extra-btn").EvaluateAsync<bool>("e => e === e.parentElement.lastElementChild && e.parentElement.classList.contains('bit-aimac-act')"));
        await Page.Locator("#mac-extra-btn").ClickAsync();
        await Expect(Page.Locator("#mac-extra-clicks")).ToHaveTextAsync("1");
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_TheRow_TakesNoFeedback()
    {
        await OpenAsync(Route);

        foreach (var name in new[] { "Regenerate", "Good response", "Bad response" })
        {
            await Expect(Button("mac-disabled", name)).ToBeDisabledAsync();
        }

        await Button("mac-disabled", "Good response").DispatchEventAsync("click");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Button("mac-disabled", "Good response")).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Page.Locator("#mac-disabled-log")).ToHaveTextAsync("0");
    }

    // ---------- style ----------

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheRow()
    {
        await OpenAsync(Route);

        var row = Row("mac-var");
        await Expect(row).ToHaveCSSAsync("column-gap", "25px");
        await Expect(row).ToHaveCSSAsync("color", "rgb(90, 80, 70)");
        await Expect(row).ToHaveCSSAsync("font-size", "19px");
        await Expect(row.Locator(".bit-aimac-act")).ToHaveCSSAsync("column-gap", "9px");

        var copy = await Button("mac-var", "Copy").RectAsync();
        var regenerate = await row.Locator(".bit-aimac-act > *").Nth(1).RectAsync();
        Assert.AreEqual(9, regenerate.Left - copy.Right, 1, "The actions are 9px apart.");
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheActionsAndTheButtons()
    {
        await OpenAsync(Route);

        var row = Row("mac-classes");
        await Expect(row).ToHaveClassAsync(HasClass("e2e-mac-root"));
        await Expect(row.Locator(".bit-aimac-act")).ToHaveClassAsync(HasClass("e2e-mac-actions"));
        await Expect(row.Locator(".bit-aimac-act")).ToHaveCSSAsync("outline-color", "rgb(3, 2, 1)");
        await Expect(Button("mac-classes", "Regenerate")).ToHaveClassAsync(HasClass("e2e-mac-regen"));

        // The app's icon class is kept beside the glyph of the AI font.
        var icon = Button("mac-classes", "Regenerate").Locator("i.bit-btn-icn");
        await Expect(icon).ToHaveClassAsync(HasClass("e2e-mac-regen-icon"));
        await icon.AssertAiGlyphAsync("Refresh");
    }

    [TestMethod]
    public async Task Rtl_TheRow_ReadsFromTheRight()
    {
        await OpenAsync(Route);

        await Expect(Row("mac-rtl")).ToHaveAttributeAsync("dir", "rtl");
        var copy = await Button("mac-rtl", "Copy").RectAsync();
        var bad = await Button("mac-rtl", "Bad response").RectAsync();
        Assert.IsTrue(copy.Left > bad.Left, "Copy is the rightmost action in right-to-left text.");
    }

    [TestMethod]
    public async Task Print_TheActions_AreLeftOutAndTheMetadataKept()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await Expect(Row("mac-meta").Locator(".bit-aimac-act")).ToHaveCSSAsync("display", "none");
        await Expect(Row("mac-meta").Locator(".bit-aimac-mdl")).ToBeVisibleAsync();
    }
}
