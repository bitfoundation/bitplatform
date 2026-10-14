using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitMessageBox at /e2e/message-box in a real browser: the inline box (title, body, every Buttons set and the
/// callbacks each button raises in order, the close button, Reversed and the tab order, DefaultButton and AutoFocus,
/// the button colors, the icon in every form and its accessible name, TitleElement, the templates, OnBeforeResult with
/// AutoLoading and the answer taken back, AnswerAsync / FocusAsync / Result, the disabled contract of a content host),
/// the box inside a BitModal and through the BitModalService, and BitMessageBoxService: Show and the severity presets
/// with their dialog / alertdialog roles, the focus on the default button, Enter, Escape, the overlay, the close
/// button, a cancellation token, every BitMessageBoxParameters member, Confirm and Prompt (the field, Enter and
/// Ctrl+Enter, Required, Validator, AsyncValidator, the password field, a throwing validator). Also every Color and
/// Size, Styles/Classes of every part, every --bit-MessageBox-* variable, the BitParams cascade, RTL, forced colors,
/// print, the narrow screen, prerendering, re-rendering and unmounting.
/// </summary>
[TestClass]
public class BitMessageBoxE2ETests : E2ETestBase
{
    private const string Route = "/e2e/message-box";

    private ILocator Box(string id) => Page.Locator($"#{id}");

    private ILocator Actions(string id) => Box(id).Locator(".bit-msb-ftr > .bit-btn");

    private ILocator CloseButton(string id) => Box(id).Locator(".bit-msb-hdr > .bit-btn");

    private ILocator Log => Page.Locator("#msb-log");

    private ILocator Dialog => Page.Locator(".bit-mdl-ctn");

    private ILocator DialogBox => Page.Locator(".bit-mdl-ctn > .bit-msb");

    private ILocator DialogActions => DialogBox.Locator(".bit-msb-ftr > .bit-btn");

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_TitleAndBody_RenderTheMessageWithAnOkButtonAndAClose()
    {
        await OpenAsync(Route);

        var box = Box("msb-basic");
        await Expect(box).ToHaveClassAsync(HasClass("bit-msb"));

        var title = box.Locator(".bit-msb-ttl");
        await Expect(title).ToHaveTextAsync("It's a title");
        await Expect(title).ToHaveAttributeAsync("id", "msb-basic-ttl");
        Assert.AreEqual("H5", await title.EvaluateAsync<string>("e => e.tagName"));
        await Expect(title).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-fg-pri)"));

        // A body of text keeps its line breaks.
        var body = box.Locator(".bit-msb-bdy");
        await Expect(body).ToHaveAttributeAsync("id", "msb-basic-bdy");
        await Expect(body).ToHaveClassAsync(HasClass("bit-msb-txt"));
        await Expect(body).ToHaveCSSAsync("white-space", "pre-wrap");
        Assert.AreEqual("It's a body.\nOn two lines.", await body.InnerTextAsync());
        await Expect(body).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-fg-sec)"));
        var lineHeight = ParsePx(await body.EvaluateAsync<string>("e => getComputedStyle(e).lineHeight"));
        Assert.IsTrue((await body.BoundingBoxAsync())!.Height >= lineHeight * 2 - 1, "The two lines are laid out on two lines.");

        await Expect(Actions("msb-basic")).ToHaveCountAsync(1);
        await Expect(Actions("msb-basic")).ToHaveTextAsync(["Ok"]);
        await Expect(Actions("msb-basic").First).ToHaveClassAsync(HasClass("bit-btn-fil"));

        var close = CloseButton("msb-basic");
        await Expect(close).ToHaveAttributeAsync("title", "Close");
        await Expect(close).ToHaveAttributeAsync("aria-label", "Close");
        await Expect(close.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));
        await Expect(box.Locator(".bit-msb-ict")).ToHaveCountAsync(0);

        // The close button sits at the end of the header, beyond the title.
        var titleBox = await title.BoundingBoxAsync();
        var closeBox = await close.BoundingBoxAsync();
        Assert.IsTrue(closeBox!.X >= titleBox!.X + titleBox.Width - 1);
    }

    [TestMethod]
    public async Task Basic_ALongWord_WrapsInsideTheBox()
    {
        await OpenAsync(Route);

        var body = Box("msb-long").Locator(".bit-msb-bdy");
        var overflow = await body.EvaluateAsync<bool>("e => e.scrollWidth > e.clientWidth");
        Assert.IsFalse(overflow, "A long word wraps instead of running off the side.");
        Assert.IsTrue((await Box("msb-long").BoundingBoxAsync())!.Width <= 241);
    }

    [TestMethod]
    public async Task Basic_NothingSet_RendersTheCloseButtonAndTheOkButtonOnly()
    {
        await OpenAsync(Route);

        await Expect(Box("msb-empty").Locator(".bit-msb-bdy")).ToHaveCountAsync(0);
        await Expect(Box("msb-empty").Locator(".bit-msb-ttl")).ToHaveCountAsync(0);
        await Expect(CloseButton("msb-empty")).ToHaveCountAsync(1);
        await Expect(Actions("msb-empty")).ToHaveTextAsync(["Ok"]);
    }

    // ---------- buttons ----------

    [DataTestMethod]
    [DataRow("msb-ok", new[] { "Got it" })]
    [DataRow("msb-okcancel", new[] { "Ok", "Not now" })]
    [DataRow("msb-yesno", new[] { "Sure", "Nope" })]
    [DataRow("msb-yesnocancel", new[] { "Yes", "No", "Cancel" })]
    [DataRow("msb-none", new string[0])]
    public async Task Buttons_EachSet_RendersItsButtonsInOrder(string id, string[] texts)
    {
        await OpenAsync(Route);

        await Expect(Actions(id)).ToHaveCountAsync(texts.Length);
        if (texts.Length > 0)
        {
            await Expect(Actions(id)).ToHaveTextAsync(texts);

            // The affirmative answer is the filled one, the others are outlined in the neutral.
            await Expect(Actions(id).First).ToHaveClassAsync(HasClass("bit-btn-fil"));
            for (var i = 1; i < texts.Length; i++)
            {
                await Expect(Actions(id).Nth(i)).ToHaveClassAsync(HasClass("bit-btn-otl"));
                await Expect(Actions(id).Nth(i)).ToHaveClassAsync(HasClass("bit-btn-dft-ter"));
            }
        }
        else
        {
            await Expect(Box(id).Locator(".bit-msb-ftr")).ToHaveCountAsync(0, new() { });
        }
    }

    [DataTestMethod]
    [DataRow("msb-ok", 0, "ok:OnOk,ok:Ok,ok:OnClose")]
    [DataRow("msb-okcancel", 0, "okcancel:OnOk,okcancel:Ok,okcancel:OnClose")]
    [DataRow("msb-okcancel", 1, "okcancel:OnCancel,okcancel:Cancel,okcancel:OnClose")]
    [DataRow("msb-yesno", 0, "yesno:OnYes,yesno:Yes,yesno:OnClose")]
    [DataRow("msb-yesno", 1, "yesno:OnNo,yesno:No,yesno:OnClose")]
    [DataRow("msb-yesnocancel", 0, "ync:OnYes,ync:Yes,ync:OnClose")]
    [DataRow("msb-yesnocancel", 1, "ync:OnNo,ync:No,ync:OnClose")]
    [DataRow("msb-yesnocancel", 2, "ync:OnCancel,ync:Cancel,ync:OnClose")]
    public async Task Buttons_APressedButton_RaisesItsCallbackThenOnResultThenOnClose(string id, int index, string log)
    {
        await OpenAsync(Route);

        await Actions(id).Nth(index).ClickAsync();

        await Expect(Log).ToHaveTextAsync(log);
    }

    [DataTestMethod]
    [DataRow("msb-ok", "ok:None,ok:OnClose")]
    [DataRow("msb-yesnocancel", "ync:None,ync:OnClose")]
    [DataRow("msb-none", "none:None,none:OnClose")]
    public async Task Buttons_TheCloseButton_AnswersNoneAndCloses(string id, string log)
    {
        await OpenAsync(Route);

        await CloseButton(id).ClickAsync();

        await Expect(Log).ToHaveTextAsync(log);
    }

    [TestMethod]
    public async Task Buttons_TheKeyboard_PressesTheFocusedButton()
    {
        await OpenAsync(Route);

        await Actions("msb-yesno").Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Log).ToHaveTextAsync("yesno:OnNo,yesno:No,yesno:OnClose");

        await Page.Locator("#btn-clear-log").ClickAsync();
        await Actions("msb-yesno").First.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Log).ToHaveTextAsync("yesno:OnYes,yesno:Yes,yesno:OnClose");
    }

    // ---------- order, focus & emphasis ----------

    [TestMethod]
    public async Task Reversed_TheButtons_AreRenderedAndTabbedInTheReverseOrder()
    {
        await OpenAsync(Route);

        await Expect(Actions("msb-reversed")).ToHaveTextAsync(["Cancel", "Ok"]);
        await Expect(Actions("msb-reversed-ync")).ToHaveTextAsync(["Cancel", "No", "Yes"]);

        // Ok stays the primary answer, filled, wherever it sits.
        await Expect(Actions("msb-reversed").Last).ToHaveClassAsync(HasClass("bit-btn-fil"));

        await CloseButton("msb-reversed").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Actions("msb-reversed").First).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Actions("msb-reversed").Last).ToBeFocusedAsync();

        var first = await Actions("msb-reversed").First.BoundingBoxAsync();
        var last = await Actions("msb-reversed").Last.BoundingBoxAsync();
        Assert.IsTrue(first!.X < last!.X);
    }

    [TestMethod]
    public async Task PrimaryButtonColor_PaintsTheAffirmativeButtonOnly()
    {
        await OpenAsync(Route);

        var delete = Actions("msb-primary-color").First;
        var keep = Actions("msb-primary-color").Last;
        await Expect(delete).ToHaveTextAsync("Delete");
        await Expect(delete).ToHaveClassAsync(HasClass("bit-btn-err"));
        await Expect(delete).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-err)"));
        await Expect(keep).Not.ToHaveClassAsync(HasClass("bit-btn-err"));
        await Expect(keep).ToHaveClassAsync(HasClass("bit-btn-dft-ter"));
    }

    [TestMethod]
    public async Task ButtonColor_PaintsTheWholeSetAndPrimaryButtonColorWinsForTheAffirmativeOne()
    {
        await OpenAsync(Route);

        var primary = await ResolveColorAsync("var(--bit-clr-pri)");
        for (var i = 0; i < 3; i++)
        {
            await Expect(Actions("msb-button-color").Nth(i)).ToHaveClassAsync(HasClass("bit-btn-pri"));
            await Expect(Actions("msb-button-color").Nth(i)).Not.ToHaveClassAsync(HasClass("bit-btn-dft-ter"));
        }

        await Expect(Actions("msb-button-color").First).ToHaveCSSAsync("background-color", primary);
        await Expect(Actions("msb-button-color").Nth(1)).ToHaveCSSAsync("border-top-color", primary);

        await Expect(Actions("msb-both-colors").First).ToHaveClassAsync(HasClass("bit-btn-suc"));
        await Expect(Actions("msb-both-colors").Last).ToHaveClassAsync(HasClass("bit-btn-inf"));
    }

    [TestMethod]
    public async Task AutoFocus_MarksTheDefaultButtonForTheLayerAroundIt()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-show-autofocus").ClickAsync();

        await Expect(Actions("msb-af-default").First).ToHaveAttributeAsync("autofocus", "");
        await Expect(Actions("msb-af-default").Last).Not.ToHaveAttributeAsync("autofocus", new Regex(".*"));

        // DefaultButton names the button; None is the close button, and a button the set lacks falls back to the primary one.
        await Expect(Actions("msb-af-no").Nth(1)).ToHaveAttributeAsync("autofocus", "");
        await Expect(Actions("msb-af-no").First).Not.ToHaveAttributeAsync("autofocus", new Regex(".*"));
        await Expect(CloseButton("msb-af-close")).ToHaveAttributeAsync("autofocus", "");
        await Expect(CloseButton("msb-af-none")).ToHaveAttributeAsync("autofocus", "");
        await Expect(Actions("msb-af-stray").First).ToHaveAttributeAsync("autofocus", "");
    }

    [TestMethod]
    public async Task AutoFocus_ABoxRenderedWithIt_TakesTheFocusOntoTheDefaultButton()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-show-af-single").ClickAsync();
        await Expect(Actions("msb-af-single").Last).ToBeFocusedAsync();
        await Expect(Actions("msb-af-single").Last).ToHaveTextAsync("No");
    }

    [TestMethod]
    public async Task AutoFocus_Off_TheFocusStaysWhereItWas()
    {
        await OpenAsync(Route);

        var toggle = Page.Locator("#btn-show-noaf");
        await toggle.ClickAsync();
        await Expect(Box("msb-noaf")).ToBeVisibleAsync();
        await Expect(toggle).ToBeFocusedAsync();
        await Expect(Actions("msb-noaf").First).Not.ToHaveAttributeAsync("autofocus", new Regex(".*"));
    }

    // ---------- icon ----------

    [TestMethod]
    public async Task Icon_IconName_DrawsADecorativeGlyph()
    {
        await OpenAsync(Route);

        var container = Box("msb-icon-name").Locator(".bit-msb-ict");
        await Expect(container).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(container).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        var icon = container.Locator("i.bit-msb-ico");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Lightbulb"));
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon"));

        // Without a Color the glyph takes the text color, and it sits before the title.
        var iconBox = await icon.BoundingBoxAsync();
        var titleBox = await Box("msb-icon-name").Locator(".bit-msb-ttl").BoundingBoxAsync();
        Assert.IsTrue(iconBox!.X < titleBox!.X);
    }

    [TestMethod]
    public async Task Icon_IconAriaLabel_TurnsTheGlyphIntoANamedImage()
    {
        await OpenAsync(Route);

        var container = Box("msb-icon-aria").Locator(".bit-msb-ict");
        await Expect(container).ToHaveAttributeAsync("role", "img");
        await Expect(container).ToHaveAttributeAsync("aria-label", "Warning");
        await Expect(container).Not.ToHaveAttributeAsync("aria-hidden", new Regex(".*"));
        await Expect(Page.GetByRole(AriaRole.Img, new() { Name = "Warning" })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Icon_IconTemplate_StandsInForTheGlyph()
    {
        await OpenAsync(Route);

        await Expect(Box("msb-icon-template").Locator(".bit-msb-ict #msb-icon-emoji")).ToHaveTextAsync("🎉");
        await Expect(Box("msb-icon-template").Locator("i.bit-msb-ico")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Icon_HideIcon_RemovesTheGlyph()
    {
        await OpenAsync(Route);

        await Expect(Box("msb-icon-hidden").Locator(".bit-msb-ict")).ToHaveCountAsync(0);
        await Expect(Box("msb-icon-hidden").Locator(".bit-msb-ttl")).ToHaveTextAsync("HideIcon");
    }

    [TestMethod]
    public async Task Icon_IconNameOverColor_KeepsTheNamedGlyphInTheColor()
    {
        await OpenAsync(Route);

        var icon = Box("msb-icon-name-over-color").Locator("i.bit-msb-ico");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Lightbulb"));
        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-icon--ErrorBadge"));
        await Expect(icon).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-err)"));
    }

    [TestMethod]
    public async Task Icon_ExternalIcons_WinOverTheNamesAndTakeTheirClasses()
    {
        await OpenAsync(Route);

        var icon = Box("msb-icon-external").Locator("i.bit-msb-ico");
        await Expect(icon).ToHaveClassAsync(HasClass("e2e-ext-ico"));
        await Expect(icon).ToHaveClassAsync(HasClass("e2e-ext-ico-warn"));
        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-icon--Lightbulb"));
        await Expect(icon).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-wrn)"));
        await Expect(CloseButton("msb-icon-external").Locator("i")).ToHaveClassAsync(HasClass("e2e-ext-close"));
        await Expect(CloseButton("msb-icon-external").Locator("i")).Not.ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));

        await Expect(Box("msb-icon-fa").Locator("i.bit-msb-ico")).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(Box("msb-icon-fa").Locator("i.bit-msb-ico")).ToHaveClassAsync(HasClass("fa-triangle-exclamation"));
        await Expect(Box("msb-icon-bi").Locator("i.bit-msb-ico")).ToHaveClassAsync(HasClass("bi-info-circle"));
        await Expect(Box("msb-icon-bi").Locator("i.bit-msb-ico")).ToHaveClassAsync(HasClass("bi"));
        await Expect(CloseButton("msb-icon-bi").Locator("i")).ToHaveClassAsync(HasClass("bi-x-lg"));
    }

    // ---------- close button & header ----------

    [TestMethod]
    public async Task ShowCloseButton_False_LeavesTheHeaderWithoutIt()
    {
        await OpenAsync(Route);

        await Expect(CloseButton("msb-noclose")).ToHaveCountAsync(0);
        await Expect(Box("msb-noclose").Locator(".bit-msb-ttl")).ToHaveTextAsync("No close button");
        await Expect(Box("msb-no-header").Locator(".bit-msb-hdr")).ToHaveCountAsync(0);
        await Expect(Box("msb-no-header").Locator(".bit-msb-bdy")).ToHaveTextAsync("No header at all.");
    }

    [TestMethod]
    public async Task CloseButton_CustomIconAndTitle_AreItsGlyphAndAccessibleName()
    {
        await OpenAsync(Route);

        var close = CloseButton("msb-custom-close");
        await Expect(close).ToHaveAttributeAsync("title", "Dismiss this message");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Dismiss this message" })).ToHaveCountAsync(1);
        await Expect(close.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));

        await close.ClickAsync();
        await Expect(Log).ToHaveTextAsync("custom-close:None,custom-close:OnClose");
    }

    [TestMethod]
    public async Task CloseButton_WithoutATitle_IsPushedToTheEndByTheSpacer()
    {
        await OpenAsync(Route);

        var box = await Box("msb-only-close").Locator(".bit-msb-hdr").BoundingBoxAsync();
        var close = await CloseButton("msb-only-close").BoundingBoxAsync();
        Assert.AreEqual(box!.X + box.Width, close!.X + close.Width, 1.5);
        await Expect(Box("msb-only-close").Locator(".bit-msb-spc")).ToBeVisibleAsync();
    }

    [DataTestMethod]
    [DataRow("msb-te-default", "H5")]
    [DataRow("msb-te-h2", "H2")]
    [DataRow("msb-te-void", "H5")]
    [DataRow("msb-te-bad", "H5")]
    public async Task TitleElement_PicksTheHeadingLevelAndKeepsTheDialogTitleType(string id, string tag)
    {
        await OpenAsync(Route);

        var title = Box(id).Locator(".bit-msb-ttl");
        Assert.AreEqual(tag, await title.EvaluateAsync<string>("e => e.tagName"));
        Assert.AreEqual(await ComputedStyleAsync("#msb-te-default .bit-msb-ttl", "font-size"), await title.EvaluateAsync<string>("e => getComputedStyle(e).fontSize"));
        await Expect(title).ToHaveCSSAsync("margin-top", "0px");
    }

    // ---------- templates ----------

    [TestMethod]
    public async Task Templates_BodyAndFooter_TakeMarkupAndTheFooterAnswersThroughAnswerAsync()
    {
        await OpenAsync(Route);

        var box = Box("msb-templates");
        await Expect(box.Locator(".bit-msb-bdy")).Not.ToHaveClassAsync(HasClass("bit-msb-txt"));
        await Expect(box.Locator(".bit-msb-bdy li")).ToHaveCountAsync(2);
        await Expect(box.Locator(".bit-msb-ftr .bit-btn")).ToHaveCountAsync(0);

        await Page.Locator("#msb-templates-delete").ClickAsync();
        await Expect(Log).ToHaveTextAsync("templates:Yes");
        await Page.Locator("#msb-templates-keep").ClickAsync();
        await Expect(Log).ToHaveTextAsync("templates:Yes,templates:No");
    }

    [TestMethod]
    public async Task Templates_HeaderTemplate_ReplacesTheIconTheTitleAndTheCloseButton()
    {
        await OpenAsync(Route);

        var header = Box("msb-header-template").Locator(".bit-msb-hdr");
        await Expect(header.Locator("#msb-custom-header")).ToHaveTextAsync("A header of my own");
        await Expect(header.Locator(".bit-msb-ttl")).ToHaveCountAsync(0);
        await Expect(header.Locator(".bit-msb-ict")).ToHaveCountAsync(0);
        await Expect(header.Locator(".bit-btn")).ToHaveCountAsync(0);
        await Expect(Page.GetByText("Never shown")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Templates_ChildContent_IsTheBodyAndBodyTemplateWinsOverIt()
    {
        await OpenAsync(Route);

        await Expect(Box("msb-child-content").Locator(".bit-msb-bdy #msb-child")).ToHaveTextAsync("The child content is the body.");
        await Expect(Box("msb-body-wins").Locator("#msb-body-template")).ToHaveCountAsync(1);
        await Expect(Box("msb-body-wins").Locator("#msb-body-child")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Templates_AComponentInTheFooter_GetsTheBoxCascadedAndAnswersIt()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#inline-footer-has-box")).ToHaveTextAsync("True");
        await Page.Locator("#inline-footer-answer-no").ClickAsync();
        await Expect(Log).ToHaveTextAsync("cascaded:No");
    }

    // ---------- guarding the answer ----------

    [TestMethod]
    public async Task OnBeforeResult_Refusing_KeepsTheBoxAndReportsNothing()
    {
        await OpenAsync(Route);

        var delete = Actions("msb-guard").First;
        await delete.ClickAsync();
        await Expect(Page.Locator("#guard-waiting")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#guard-args")).ToHaveTextAsync("Yes");

        // AutoLoading spins the pressed button while the guard runs.
        await Expect(delete).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(delete).ToHaveClassAsync(HasClass("bit-btn-lda"));

        // Another answer than a dismissal is refused while the first one is pending.
        await Actions("msb-guard").Last.ClickAsync();
        await Page.Locator("#btn-guard-release").ClickAsync();

        await Expect(Log).ToHaveTextAsync("guard:refused");
        await Expect(Page.Locator("#guard-waiting")).ToHaveTextAsync("False");
        await Expect(delete).Not.ToHaveAttributeAsync("aria-busy", "true");
    }

    [TestMethod]
    public async Task OnBeforeResult_Allowing_HandsTheAnswerOver()
    {
        await OpenAsync(Route);

        await Page.Locator("#guard-check").CheckAsync();
        await Actions("msb-guard").First.ClickAsync();
        await Expect(Page.Locator("#guard-waiting")).ToHaveTextAsync("True");
        await Page.Locator("#btn-guard-release").ClickAsync();

        await Expect(Log).ToHaveTextAsync("guard:OnYes,guard:Yes,guard:OnClose");
    }

    [TestMethod]
    public async Task OnBeforeResult_TheCloseButtonWhileItRuns_TakesTheAnswerBack()
    {
        await OpenAsync(Route);

        await Page.Locator("#guard-check").CheckAsync();
        await Actions("msb-guard").First.ClickAsync();
        await Expect(Page.Locator("#guard-waiting")).ToHaveTextAsync("True");

        await CloseButton("msb-guard").ClickAsync();

        await Expect(Log).ToHaveTextAsync("guard:token-cancelled,guard:None,guard:OnClose");
        await Expect(Page.Locator("#guard-args")).ToHaveTextAsync("None");

        // The guard released afterwards hands nothing over.
        await Page.Locator("#btn-guard-release").ClickAsync();
        await Expect(Log).ToHaveTextAsync("guard:token-cancelled,guard:None,guard:OnClose");
    }

    [TestMethod]
    public async Task OnBeforeResult_AnUnguardedAnswer_GoesStraightThrough()
    {
        await OpenAsync(Route);

        await Actions("msb-guard").Last.ClickAsync();

        await Expect(Log).ToHaveTextAsync("guard:OnNo,guard:No,guard:OnClose");
        await Expect(Page.Locator("#guard-args")).ToHaveTextAsync("No");
    }

    [TestMethod]
    public async Task AutoLoading_ASlowCallback_SpinsThePressedButtonAndAnswersOnce()
    {
        await OpenAsync(Route);

        var ok = Actions("msb-slow").First;
        await ok.ClickAsync();
        await Expect(Log).ToHaveTextAsync("slow:OnOk-start");
        await Expect(ok).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(ok).ToHaveAttributeAsync("aria-disabled", "true");

        // A second press, of the same button or another one, hands nothing in while the first answer is worked out.
        await ok.DispatchEventAsync("click");
        await Actions("msb-slow").Last.ClickAsync();

        await Page.Locator("#btn-slow-release").ClickAsync();
        await Expect(Log).ToHaveTextAsync("slow:OnOk-start,slow:OnOk-end,slow:Ok");
        await Expect(ok).Not.ToHaveAttributeAsync("aria-busy", "true");
    }

    // ---------- public members ----------

    [TestMethod]
    public async Task AnswerAsync_AnswersAsThoughTheButtonWasPressedAndResultHoldsIt()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-read-result").ClickAsync();
        await Expect(Page.Locator("#members-result")).ToHaveTextAsync("None");

        await Page.Locator("#btn-answer-yes").ClickAsync();
        await Expect(Log).ToHaveTextAsync("members:Yes");
        await Page.Locator("#btn-read-result").ClickAsync();
        await Expect(Page.Locator("#members-result")).ToHaveTextAsync("Yes");

        // A result the current set does not render is answered all the same.
        await Page.Locator("#btn-answer-ok").ClickAsync();
        await Expect(Log).ToHaveTextAsync("members:Yes,members:Ok");
        await Page.Locator("#btn-read-result").ClickAsync();
        await Expect(Page.Locator("#members-result")).ToHaveTextAsync("Ok");

        await CloseButton("msb-members").ClickAsync();
        await Page.Locator("#btn-read-result").ClickAsync();
        await Expect(Page.Locator("#members-result")).ToHaveTextAsync("None");
    }

    [TestMethod]
    public async Task FocusAsync_MovesTheFocusOntoTheDefaultButtonOrTheCloseButton()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-focus").ClickAsync();
        await Expect(Actions("msb-members").Nth(1)).ToBeFocusedAsync();

        await Page.Locator("#btn-focus-footer").ClickAsync();
        await Expect(CloseButton("msb-focus-footer")).ToBeFocusedAsync();

        await Page.Locator("#btn-focus-nothing").ClickAsync();
        await Expect(Page.Locator("#btn-focus-nothing")).ToBeFocusedAsync();
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_TheBox_DisablesItsButtonsAndAnswersNothing()
    {
        await OpenAsync(Route);

        var box = Box("msb-disabled");
        await Expect(box).ToHaveClassAsync(HasClass("bit-dis"));
        for (var i = 0; i < 2; i++)
        {
            await Expect(Actions("msb-disabled").Nth(i)).ToBeDisabledAsync();
            await Expect(Actions("msb-disabled").Nth(i)).ToHaveAttributeAsync("aria-disabled", "true");
        }

        await Expect(CloseButton("msb-disabled")).ToBeDisabledAsync();

        // A click dispatched the way a screen reader does it, and AnswerAsync, are both ignored.
        await Actions("msb-disabled").First.DispatchEventAsync("click");
        await CloseButton("msb-disabled").DispatchEventAsync("click");
        await Page.Locator("#btn-disabled-answer").ClickAsync();

        // The text stays readable: a notice, not a control.
        await Expect(box).ToHaveCSSAsync("opacity", "1");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(box).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Actions("msb-disabled").First).ToBeEnabledAsync();
        await Actions("msb-disabled").First.ClickAsync();
        await Expect(Log).ToHaveTextAsync("disabled:Ok");
    }

    [TestMethod]
    public async Task Disabled_AContentHost_KeepsThePagesHandlersForItsLiveContent()
    {
        await OpenAsync(Route);

        await Expect(Box("msb-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Page.Locator("#msb-disabled-inner").ClickAsync();
        await Expect(Page.Locator("#root-clicks")).ToHaveTextAsync("1");
    }

    // ---------- in a BitModal, through the modal service ----------

    [TestMethod]
    public async Task Modal_TheBoxInside_NamesTheDialogTakesTheFocusAndClosesIt()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-modal").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Session expiring" });
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog).ToHaveAttributeAsync("aria-describedby", "modal-msb-bdy");
        Assert.AreEqual("H2", await Page.Locator("#modal-msb-ttl").EvaluateAsync<string>("e => e.tagName"));
        await Expect(Actions("modal-msb").First).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#modal-open")).ToHaveTextAsync("False");
        await Expect(Box("modal-msb")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Modal_Escape_ClosesTheModalAroundTheBox()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-modal").ClickAsync();
        await Expect(Actions("modal-msb").First).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#modal-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task ModalService_TheBoxAsContent_ClosesTheModalWithTheAnswer()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-modal-service").ClickAsync();
        await Expect(Actions("modal-svc-msb").First).ToBeFocusedAsync();
        await Actions("modal-svc-msb").Last.ClickAsync();

        await Expect(Page.Locator("#modal-service-result")).ToHaveTextAsync("Cancel");
        await Expect(Box("modal-svc-msb")).ToHaveCountAsync(0);
    }

    // ---------- BitMessageBoxService ----------

    [TestMethod]
    public async Task Service_Show_OpensAnAlertDialogFocusedOnOkThatEnterAnswers()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-svc-show").ClickAsync();
        await Expect(Dialog).ToBeVisibleAsync();
        await Expect(Page.Locator("#svc-result")).ToHaveTextAsync("pending");

        // A question is an alertdialog, named by its title and described by its body.
        await Expect(Dialog).ToHaveAttributeAsync("role", "alertdialog");
        var id = await DialogBox.GetAttributeAsync("id");
        StringAssert.StartsWith(id, "bit-msb-");
        await Expect(Dialog).ToHaveAttributeAsync("aria-labelledby", $"{id}-ttl");
        await Expect(Dialog).ToHaveAttributeAsync("aria-describedby", $"{id}-bdy");
        await Expect(Page.GetByRole(AriaRole.Alertdialog, new() { Name = "TITLE" })).ToBeVisibleAsync();
        await Expect(DialogBox.Locator(".bit-msb-bdy")).ToHaveTextAsync("BODY");
        await Expect(DialogActions).ToHaveTextAsync(["Ok", "Cancel"]);

        await Expect(DialogActions.First).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#svc-result")).ToHaveTextAsync("Ok");
        await Expect(Page.Locator("#svc-count")).ToHaveTextAsync("1");
        await Expect(Dialog).ToHaveCountAsync(0);
    }

    [DataTestMethod]
    [DataRow("escape")]
    [DataRow("overlay")]
    [DataRow("close")]
    [DataRow("cancel")]
    public async Task Service_Dismissed_AnswersNoneOrCancel(string how)
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-svc-show").ClickAsync();
        await Expect(DialogActions.First).ToBeFocusedAsync();

        switch (how)
        {
            case "escape": await Page.Keyboard.PressAsync("Escape"); break;
            case "overlay": await Page.Locator(".bit-mdl > .bit-mdl-ovl").ClickAsync(new() { Position = new() { X = 4, Y = 4 } }); break;
            case "close": await DialogBox.Locator(".bit-msb-hdr > .bit-btn").ClickAsync(); break;
            case "cancel": await DialogActions.Last.ClickAsync(); break;
        }

        await Expect(Page.Locator("#svc-result")).ToHaveTextAsync(how == "cancel" ? "Cancel" : "None");
        await Expect(Page.Locator("#svc-count")).ToHaveTextAsync("1");
        await Expect(Dialog).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Service_TheBoxInAModal_IsAsWideAsTheThemesDialogs()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-svc-show").ClickAsync();
        await Expect(DialogBox).ToBeVisibleAsync();

        var expected = await DialogBox.EvaluateAsync<string>(
            """
            e => {
                const probe = document.createElement('div');
                probe.style.width = 'calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * var(--bit-siz-dialog-max-width-steps))';
                document.body.appendChild(probe);
                const w = getComputedStyle(probe).width;
                probe.remove();
                return w;
            }
            """);
        await Expect(DialogBox).ToHaveCSSAsync("max-width", expected);
        await Expect(Box("msb-basic")).ToHaveCSSAsync("max-width", "100%");
    }

    [TestMethod]
    public async Task Service_ShowTitleBody_IsAPlainDialogWithOneOk()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-svc-show-plain").ClickAsync();
        await Expect(Dialog).ToHaveAttributeAsync("role", "dialog");
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Plain title" })).ToBeVisibleAsync();
        await Expect(DialogActions).ToHaveTextAsync(["Ok"]);
        await Expect(DialogActions.First).ToBeFocusedAsync();

        await DialogActions.First.ClickAsync();
        await Expect(Page.Locator("#svc-result")).ToHaveTextAsync("Ok");
    }

    [DataTestMethod]
    [DataRow("info", "dialog", "Info", "inf")]
    [DataRow("success", "dialog", "Completed", "suc")]
    [DataRow("warning", "alertdialog", "Warning", "wrn")]
    [DataRow("severewarning", "alertdialog", "WarningSolid", "swr")]
    [DataRow("error", "alertdialog", "ErrorBadge", "err")]
    public async Task Service_TheSeverityPresets_ColorTheGlyphAndPickTheRole(string severity, string role, string glyph, string suffix)
    {
        await OpenAsync(Route);

        await Page.Locator($"#btn-svc-{severity}").ClickAsync();

        await Expect(Dialog).ToHaveAttributeAsync("role", role);
        await Expect(DialogBox).ToHaveClassAsync(HasClass($"bit-msb-{suffix}"));
        var icon = DialogBox.Locator("i.bit-msb-ico");
        await Expect(icon).ToHaveClassAsync(HasClass($"bit-icon--{glyph}"));
        await Expect(icon).ToHaveCSSAsync("color", await ResolveColorAsync($"var(--bit-clr-{suffix})"));
        await Expect(DialogActions.First).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#svc-result")).ToHaveTextAsync("Ok");
    }

    [TestMethod]
    public async Task Service_ACancellationToken_ClosesTheBoxFromCode()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-svc-timed").ClickAsync();
        await Expect(Dialog).ToBeVisibleAsync();
        await Expect(Page.Locator("#svc-result")).ToHaveTextAsync("pending");

        await Page.Locator("#btn-svc-cancel-timed").DispatchEventAsync("click");

        await Expect(Page.Locator("#svc-result")).ToHaveTextAsync("None");
        await Expect(Dialog).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Service_AnAlreadyCancelledToken_ShowsNothing()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-svc-cancelled-before").ClickAsync();

        await Expect(Page.Locator("#svc-result")).ToHaveTextAsync("None");
        await Expect(Dialog).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Service_Parameters_ReachTheBoxAndTheModal()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-svc-params").ClickAsync();

        var box = Box("svc-params");
        await Expect(box).ToBeVisibleAsync();
        await Expect(box).ToHaveClassAsync(HasClass("svc-params-root"));
        await Expect(box).ToHaveClassAsync(HasClass("bit-msb-lg"));
        await Expect(box).ToHaveClassAsync(HasClass("bit-msb-suc"));
        await Expect(Page.Locator(".bit-mdl.svc-params-modal")).ToHaveCountAsync(1);
        Assert.AreEqual("H2", await box.Locator(".bit-msb-ttl").EvaluateAsync<string>("e => e.tagName"));
        await Expect(box.Locator(".bit-msb-ttl")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(box.Locator(".bit-msb-ict")).ToHaveAttributeAsync("aria-label", "Success");
        await Expect(CloseButton("svc-params")).ToHaveAttributeAsync("title", "Shut");

        // Reversed, renamed, colored, and the focus on the Cancel it names.
        await Expect(Actions("svc-params")).ToHaveTextAsync(["C", "N", "Y"]);
        await Expect(Actions("svc-params").Last).ToHaveClassAsync(HasClass("bit-btn-suc"));
        await Expect(Actions("svc-params").First).ToHaveClassAsync(HasClass("bit-btn-ter"));
        await Expect(Actions("svc-params").First).ToBeFocusedAsync();
        await Expect(Dialog).ToHaveAttributeAsync("aria-labelledby", "svc-params-ttl");

        // Blocking: a click beside it does not dismiss it.
        await Page.Locator(".bit-mdl > .bit-mdl-ovl").ClickAsync(new() { Position = new() { X = 4, Y = 4 } });
        await Expect(box).ToBeVisibleAsync();
        await Expect(Page.Locator("#svc-result")).ToHaveTextAsync("pending");

        await Actions("svc-params").Nth(1).ClickAsync();
        await Expect(Page.Locator("#svc-result")).ToHaveTextAsync("No");
    }

    [TestMethod]
    public async Task Service_NoTitle_NamesTheDialogByItsBody()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-svc-no-title").ClickAsync();

        await Expect(Dialog).ToHaveAttributeAsync("aria-label", "Only a body names this dialog.");
        await Expect(Dialog).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".*"));
        await Expect(Dialog).ToHaveAttributeAsync("aria-describedby", "svc-no-title-bdy");
    }

    [TestMethod]
    public async Task Service_AHeaderTemplate_NamesTheDialogByTheTitleWords()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-svc-header").ClickAsync();

        await Expect(Page.Locator("#svc-custom-header")).ToHaveTextAsync("Custom header");
        await Expect(Dialog).ToHaveAttributeAsync("aria-label", "Named by its title");
        await Expect(Dialog).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".*"));
        await Expect(Box("svc-header").Locator(".bit-msb-ttl")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Service_AFooterComponent_AnswersTheBoxItIsCascaded()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-svc-footer").ClickAsync();
        await Expect(Page.Locator("#svc-footer-has-box")).ToHaveTextAsync("True");

        // With a footer of its own the box focuses its close button.
        await Expect(CloseButton("svc-footer")).ToBeFocusedAsync();

        await Page.Locator("#svc-footer-answer-yes").ClickAsync();
        await Expect(Page.Locator("#svc-result")).ToHaveTextAsync("Yes");
        await Expect(Dialog).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Service_OnBeforeResult_KeepsTheModalOpenWhileItRefuses()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-svc-guarded").ClickAsync();
        await DialogActions.First.ClickAsync();
        await Expect(Box("svc-guarded")).ToBeVisibleAsync();
        await Expect(Page.Locator("#svc-result")).ToHaveTextAsync("pending");

        await Page.Locator("#svc-guard-check").CheckAsync();
        await DialogActions.First.ClickAsync();
        await Expect(Page.Locator("#svc-result")).ToHaveTextAsync("Ok");
        await Expect(Dialog).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Service_Rtl_LaysTheBoxAndTheModalOutRightToLeft()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-svc-rtl").ClickAsync();

        var box = Box("svc-rtl");
        await Expect(box).ToHaveAttributeAsync("dir", "rtl");
        await Expect(box).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Page.Locator(".bit-mdl")).ToHaveAttributeAsync("dir", "rtl");
        var title = await box.Locator(".bit-msb-ttl").BoundingBoxAsync();
        var close = await CloseButton("svc-rtl").BoundingBoxAsync();
        Assert.IsTrue(close!.X < title!.X, "The close button is on the left in right-to-left.");
    }

    // ---------- Confirm ----------

    [DataTestMethod]
    [DataRow("#btn-confirm", 0, "True")]
    [DataRow("#btn-confirm", 1, "False")]
    [DataRow("#btn-confirm-yesno", 0, "True")]
    [DataRow("#btn-confirm-yesno", 1, "False")]
    public async Task Confirm_ReturnsTrueOnlyForOkOrYes(string button, int index, string expected)
    {
        await OpenAsync(Route);

        await Page.Locator(button).ClickAsync();
        await Expect(Dialog).ToHaveAttributeAsync("role", "alertdialog");
        await DialogActions.Nth(index).ClickAsync();

        await Expect(Page.Locator("#confirm-result")).ToHaveTextAsync(expected);
    }

    [TestMethod]
    public async Task Confirm_Escape_IsFalse()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-confirm").ClickAsync();
        await Expect(DialogActions.First).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#confirm-result")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Confirm_ADangerousQuestion_FocusesTheHarmlessAnswerAndIsBlocking()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-confirm-danger").ClickAsync();

        var keep = DialogActions.Last;
        await Expect(keep).ToHaveTextAsync("Keep it");
        await Expect(keep).ToBeFocusedAsync();
        await Expect(DialogActions.First).ToHaveClassAsync(HasClass("bit-btn-err"));
        await Expect(Page.GetByRole(AriaRole.Img, new() { Name = "Error" })).ToBeVisibleAsync();

        await Page.Locator(".bit-mdl > .bit-mdl-ovl").ClickAsync(new() { Position = new() { X = 4, Y = 4 } });
        await Expect(Dialog).ToBeVisibleAsync();

        await keep.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#confirm-result")).ToHaveTextAsync("False");

        await Page.Locator("#btn-confirm-danger").ClickAsync();
        await DialogActions.First.ClickAsync();
        await Expect(Page.Locator("#confirm-result")).ToHaveTextAsync("True");
    }

    // ---------- Prompt ----------

    [TestMethod]
    public async Task Prompt_TheField_TakesTheFocusWithItsValueSelectedAndEnterAnswers()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-prompt").ClickAsync();

        // A prompt is a form, never an alert, and its field is named by the question.
        await Expect(Dialog).ToHaveAttributeAsync("role", "dialog");
        var input = DialogBox.Locator(".bit-msb-pfl input");
        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveValueAsync("report.pdf");
        await Expect(input).ToHaveAttributeAsync("aria-label", "The new name of the file:");
        await Expect(input).ToHaveAttributeAsync("enterkeyhint", "done");
        Assert.AreEqual(10, await input.EvaluateAsync<int>("e => e.selectionEnd - e.selectionStart"), "The value is selected.");
        await Expect(Dialog).Not.ToHaveAttributeAsync("aria-describedby", new Regex(".*"));
        await Expect(DialogActions).ToHaveTextAsync(["Ok", "Cancel"]);

        await Page.Keyboard.TypeAsync("summary.pdf");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#prompt-result")).ToHaveTextAsync("summary.pdf");
        await Expect(Dialog).ToHaveCountAsync(0);
    }

    [DataTestMethod]
    [DataRow("cancel")]
    [DataRow("escape")]
    [DataRow("close")]
    public async Task Prompt_Dismissed_ReturnsNull(string how)
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-prompt").ClickAsync();
        var input = DialogBox.Locator(".bit-msb-pfl input");
        await Expect(input).ToBeFocusedAsync();
        await input.FillAsync("ignored");

        switch (how)
        {
            case "cancel": await DialogActions.Last.ClickAsync(); break;
            case "escape": await Page.Keyboard.PressAsync("Escape"); break;
            case "close": await DialogBox.Locator(".bit-msb-hdr > .bit-btn").ClickAsync(); break;
        }

        await Expect(Page.Locator("#prompt-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#prompt-result")).ToHaveTextAsync("null");
        await Expect(Dialog).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Prompt_Validation_RefusesTheValueWithAMessageUnderTheField()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-prompt-validated").ClickAsync();

        var box = Box("svc-folder");
        var input = box.Locator(".bit-msb-pfl input");
        await Expect(input).ToBeFocusedAsync();
        await Expect(box.Locator(".bit-msb-pfl .bit-tfl-lbl")).ToHaveTextAsync(new Regex("Name"));
        await Expect(box.Locator(".bit-msb-pfl")).ToContainTextAsync("Up to 40 characters, no slashes.");
        await Expect(input).ToHaveAttributeAsync("placeholder", "Untitled folder");
        await Expect(input).ToHaveAttributeAsync("maxlength", "40");
        await Expect(input).ToHaveAttributeAsync("autocomplete", "off");
        await Expect(input).ToHaveAttributeAsync("required", "");
        await Expect(Dialog).ToHaveAttributeAsync("aria-describedby", "svc-folder-msg");
        await Expect(Actions("svc-folder").First).ToHaveTextAsync("Create");

        // Required: an empty value is refused and the box stays.
        await Page.Keyboard.PressAsync("Enter");
        var error = box.Locator(".bit-msb-pfl .bit-tfl-erm");
        await Expect(error).ToHaveTextAsync("Give the folder a name.");
        await Expect(Page.Locator("#prompt-result")).ToHaveTextAsync("pending");

        // Once refused, every edit re-checks the value.
        await input.PressSequentiallyAsync("a/b");
        await Expect(error).ToHaveTextAsync("A name cannot hold a slash.");
        await input.FillAsync("Projects");
        await Expect(error).ToHaveCountAsync(0);

        // The server check runs on answering and refuses a name that is taken.
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#prompt-validating")).ToHaveTextAsync("1");
        await Expect(error).ToHaveTextAsync("A folder with this name already exists.");
        await Expect(input).ToBeFocusedAsync();

        await input.FillAsync("Docs");
        await Actions("svc-folder").First.ClickAsync();
        await Expect(Page.Locator("#prompt-result")).ToHaveTextAsync("Docs");
        await Expect(Page.Locator("#prompt-validating")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Prompt_APasswordField_HidesTheTypingAndCanRevealIt()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-prompt-password").ClickAsync();

        var box = Box("svc-password");
        var input = box.Locator(".bit-msb-pfl input");
        await Expect(input).ToHaveAttributeAsync("type", "password");
        await Expect(input).ToHaveAttributeAsync("autocomplete", "current-password");
        await Expect(Actions("svc-password").First).ToHaveClassAsync(HasClass("bit-btn-err"));
        await Expect(Dialog).ToHaveAttributeAsync("role", "dialog");

        await input.FillAsync("s3cret");
        await box.Locator(".bit-tfl-rpb").ClickAsync();
        await Expect(input).ToHaveAttributeAsync("type", "text");

        await Actions("svc-password").First.ClickAsync();
        await Expect(Page.Locator("#prompt-result")).ToHaveTextAsync("s3cret");
    }

    [TestMethod]
    public async Task Prompt_Multiline_KeepsEnterForNewLinesAndAnswersOnCtrlEnter()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-prompt-multiline").ClickAsync();

        var area = Box("svc-feedback").Locator(".bit-msb-pfl textarea");
        await Expect(area).ToBeFocusedAsync();
        await Expect(area).ToHaveAttributeAsync("rows", "3");
        await Page.Keyboard.TypeAsync("First");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.TypeAsync("Second");
        await Expect(Dialog).ToBeVisibleAsync();
        await Expect(area).ToHaveValueAsync("First\nSecond");

        await Page.Keyboard.PressAsync("Control+Enter");
        await Expect(Page.Locator("#prompt-result")).ToHaveTextAsync("First\nSecond");
    }

    [TestMethod]
    public async Task Prompt_YesNo_YesAcceptsTheValueAndNoReturnsNull()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-prompt-yesno").ClickAsync();
        await Expect(Box("svc-yesno-prompt").Locator(".bit-msb-pfl input")).ToHaveValueAsync("abc");
        await Expect(Actions("svc-yesno-prompt")).ToHaveTextAsync(["Yes", "No"]);
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#prompt-result")).ToHaveTextAsync("abc");

        await Page.Locator("#btn-prompt-yesno").ClickAsync();
        await Actions("svc-yesno-prompt").Last.ClickAsync();
        await Expect(Page.Locator("#prompt-result")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task Prompt_AThrowingValidator_ClosesTheBoxAndHandsTheErrorToTheCaller()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-prompt-throws").ClickAsync();
        await Expect(Box("svc-throws")).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#prompt-result")).ToHaveTextAsync("threw");
        await Expect(Page.Locator("#prompt-error")).ToHaveTextAsync("The validator failed.");
        await Expect(Dialog).ToHaveCountAsync(0);
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    // ---------- color & size ----------

    [DataTestMethod]
    [DataRow("primary", "pri", "--bit-clr-pri", "Info")]
    [DataRow("secondary", "sec", "--bit-clr-sec", "Info")]
    [DataRow("tertiary", "ter", "--bit-clr-ter", "Info")]
    [DataRow("info", "inf", "--bit-clr-inf", "Info")]
    [DataRow("success", "suc", "--bit-clr-suc", "Completed")]
    [DataRow("warning", "wrn", "--bit-clr-wrn", "Warning")]
    [DataRow("severewarning", "swr", "--bit-clr-swr", "WarningSolid")]
    [DataRow("error", "err", "--bit-clr-err", "ErrorBadge")]
    [DataRow("primarybackground", "pbg", "--bit-clr-bg-pri", "Info")]
    [DataRow("secondarybackground", "sbg", "--bit-clr-bg-sec", "Info")]
    [DataRow("tertiarybackground", "tbg", "--bit-clr-bg-ter", "Info")]
    [DataRow("primaryforeground", "pfg", "--bit-clr-fg-pri", "Info")]
    [DataRow("secondaryforeground", "sfg", "--bit-clr-fg-sec", "Info")]
    [DataRow("tertiaryforeground", "tfg", "--bit-clr-fg-ter", "Info")]
    [DataRow("primaryborder", "pbr", "--bit-clr-brd-pri", "Info")]
    [DataRow("secondaryborder", "sbr", "--bit-clr-brd-sec", "Info")]
    [DataRow("tertiaryborder", "tbr", "--bit-clr-brd-ter", "Info")]
    public async Task Color_EachRole_PicksTheGlyphAndPaintsIt(string name, string suffix, string token, string glyph)
    {
        await OpenAsync(Route);

        var id = $"msb-color-{name}";
        await Expect(Box(id)).ToHaveClassAsync(HasClass($"bit-msb-{suffix}"));
        var icon = Box(id).Locator("i.bit-msb-ico");
        await Expect(icon).ToHaveClassAsync(HasClass($"bit-icon--{glyph}"));
        await Expect(icon).ToHaveCSSAsync("color", await ResolveColorAsync($"var({token})"));

        // Only the glyph is painted: the title keeps its own color.
        await Expect(Box(id).Locator(".bit-msb-ttl")).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-fg-pri)"));
    }

    [TestMethod]
    public async Task Size_EachSize_ScalesTheInsetTheTypeAndTheGlyphTogether()
    {
        await OpenAsync(Route);

        await Expect(Box("msb-size-small")).ToHaveClassAsync(HasClass("bit-msb-sm"));
        await Expect(Box("msb-size-medium")).ToHaveClassAsync(HasClass("bit-msb-md"));
        await Expect(Box("msb-size-large")).ToHaveClassAsync(HasClass("bit-msb-lg"));
        await Expect(Box("msb-size-unset")).Not.ToHaveClassAsync(new Regex(@"bit-msb-(sm|md|lg)\b"));

        foreach (var (selector, property) in new[]
                 {
                     ("", "padding-top"), (" .bit-msb-ttl", "font-size"), (" .bit-msb-bdy", "font-size"), (" i.bit-msb-ico", "font-size")
                 })
        {
            var small = ParsePx(await ComputedStyleAsync($"#msb-size-small{selector}", property));
            var medium = ParsePx(await ComputedStyleAsync($"#msb-size-medium{selector}", property));
            var large = ParsePx(await ComputedStyleAsync($"#msb-size-large{selector}", property));
            var unset = ParsePx(await ComputedStyleAsync($"#msb-size-unset{selector}", property));

            Assert.IsTrue(small < medium && medium < large, $"{selector} {property}: {small} < {medium} < {large}");
            Assert.AreEqual(medium, unset, 0.01, $"An unset size is the medium one ({selector} {property}).");
        }

        // The floor of the width follows the size as well (read as written, since it is a min() of the container).
        var minWidths = new List<string>();
        foreach (var size in new[] { "small", "medium", "large" })
        {
            minWidths.Add(await ComputedStyleAsync($"#msb-size-{size}", "min-width"));
        }

        Assert.AreEqual(3, minWidths.Distinct().Count(), string.Join(" | ", minWidths));
        Assert.AreEqual(minWidths[1], await ComputedStyleAsync("#msb-size-unset", "min-width"));

        // The buttons follow the size too.
        await Expect(Actions("msb-size-small").First).ToHaveClassAsync(HasClass("bit-btn-sm"));
        await Expect(Actions("msb-size-large").First).ToHaveClassAsync(HasClass("bit-btn-lg"));
    }

    // ---------- style, class, variables ----------

    [TestMethod]
    public async Task Styles_EveryPart_TakesItsStyle()
    {
        await OpenAsync(Route);

        var id = "#msb-styles";
        await Expect(Box("msb-styles")).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(Box("msb-styles")).ToHaveClassAsync(HasClass("e2e-msb-root-class"));
        await Expect(Box("msb-styles")).ToHaveCSSAsync("background-color", "rgb(0, 0, 0)");
        await Expect(Page.Locator($"{id} .bit-msb-con")).ToHaveCSSAsync("outline-color", "rgb(4, 5, 6)");
        await Expect(Page.Locator($"{id} .bit-msb-hdr")).ToHaveCSSAsync("padding-top", "7px");
        await Expect(Page.Locator($"{id} .bit-msb-ict")).ToHaveCSSAsync("padding-left", "3px");
        await Expect(Page.Locator($"{id} .bit-msb-ico")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(Page.Locator($"{id} .bit-msb-ttl")).ToHaveCSSAsync("color", "rgb(255, 255, 255)");
        await Expect(Page.Locator($"{id} .bit-msb-spc")).ToHaveCSSAsync("min-width", "5px");
        await Expect(CloseButton("msb-styles")).ToHaveCSSAsync("color", "rgb(10, 11, 12)");
        await Expect(Page.Locator($"{id} .bit-msb-bdy")).ToHaveCSSAsync("color", "rgb(13, 14, 15)");
        await Expect(Page.Locator($"{id} .bit-msb-ftr")).ToHaveCSSAsync("padding-bottom", "9px");

        // A button's own set stands on its own, and ActionButton is the fallback of the others.
        await Expect(Actions("msb-styles").Nth(0)).ToHaveCSSAsync("border-top-left-radius", "16px");
        await Expect(Actions("msb-styles").Nth(1)).ToHaveCSSAsync("border-top-left-radius", "2px");
        await Expect(Actions("msb-styles").Nth(2)).ToHaveCSSAsync("border-top-left-radius", "11px");
        await Expect(Actions("msb-classes-yn").Last).ToHaveCSSAsync("border-top-left-radius", "6px");
    }

    [TestMethod]
    public async Task Classes_EveryPart_TakesItsClass()
    {
        await OpenAsync(Route);

        var id = "#msb-classes";
        await Expect(Box("msb-classes")).ToHaveClassAsync(HasClass("c-root"));
        foreach (var (part, cls) in new[]
                 {
                     (".bit-msb-con", "c-con"), (".bit-msb-hdr", "c-hdr"), (".bit-msb-ict", "c-ict"), (".bit-msb-ico", "c-ico"),
                     (".bit-msb-ttl", "c-ttl"), (".bit-msb-spc", "c-spc"), (".bit-msb-bdy", "c-bdy"), (".bit-msb-ftr", "c-ftr")
                 })
        {
            await Expect(Page.Locator($"{id} {part}")).ToHaveClassAsync(HasClass(cls));
        }

        await Expect(CloseButton("msb-classes")).ToHaveClassAsync(HasClass("c-cls"));
        await Expect(Actions("msb-classes").First).ToHaveClassAsync(HasClass("c-ok"));
        await Expect(Actions("msb-classes").First).Not.ToHaveClassAsync(HasClass("c-act"));
        await Expect(Actions("msb-classes").Last).ToHaveClassAsync(HasClass("c-act"));

        await Expect(Actions("msb-classes-yn").Nth(0)).ToHaveClassAsync(HasClass("c-yes"));
        await Expect(Actions("msb-classes-yn").Nth(1)).ToHaveClassAsync(HasClass("c-no"));
        await Expect(Actions("msb-classes-yn").Nth(2)).ToHaveClassAsync(HasClass("c-cancel"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheBox()
    {
        await OpenAsync(Route);

        var id = "#msb-vars";
        await Expect(Box("msb-vars")).ToHaveCSSAsync("padding-top", "31px");
        await Expect(Box("msb-vars")).ToHaveCSSAsync("min-width", "333px");
        await Expect(Box("msb-vars")).ToHaveCSSAsync("max-width", "444px");
        await Expect(Box("msb-vars")).ToHaveCSSAsync("max-height", "222px");
        await Expect(Page.Locator($"{id} .bit-msb-con")).ToHaveCSSAsync("row-gap", "13px");
        await Expect(Page.Locator($"{id} .bit-msb-ttl")).ToHaveCSSAsync("text-align", "center");
        await Expect(Page.Locator($"{id} .bit-msb-bdy")).ToHaveCSSAsync("text-align", "center");
        await Expect(Page.Locator($"{id} .bit-msb-ico")).ToHaveCSSAsync("color", "rgb(1, 100, 1)");
        await Expect(Page.Locator($"{id} .bit-msb-ico")).ToHaveCSSAsync("font-size", "41px");
        await Expect(Page.Locator($"{id} .bit-msb-ttl")).ToHaveCSSAsync("color", "rgb(2, 2, 200)");
        await Expect(Page.Locator($"{id} .bit-msb-ttl")).ToHaveCSSAsync("font-size", "27px");
        await Expect(Page.Locator($"{id} .bit-msb-ttl")).ToHaveCSSAsync("font-weight", "300");
        await Expect(Page.Locator($"{id} .bit-msb-bdy")).ToHaveCSSAsync("color", "rgb(3, 3, 3)");
        await Expect(Page.Locator($"{id} .bit-msb-bdy")).ToHaveCSSAsync("font-size", "19px");
        await Expect(Page.Locator($"{id} .bit-msb-ftr")).ToHaveCSSAsync("column-gap", "17px");
        await Expect(Page.Locator($"{id} .bit-msb-ftr")).ToHaveCSSAsync("justify-content", "center");

        // The box stops at its max height and the body scrolls inside it, the header and footer staying put.
        Assert.IsTrue((await Box("msb-vars").BoundingBoxAsync())!.Height <= 222.5);
        Assert.IsTrue(await Page.Locator($"{id} .bit-msb-bdy").EvaluateAsync<bool>("e => e.scrollHeight > e.clientHeight"));
        var footer = await Page.Locator($"{id} .bit-msb-ftr").BoundingBoxAsync();
        var root = await Box("msb-vars").BoundingBoxAsync();
        Assert.IsTrue(footer!.Y + footer.Height <= root!.Y + root.Height + 0.5, "The footer stays inside the box.");
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColorAndSize_WinOverThem()
    {
        await OpenAsync(Route);

        var id = "#msb-vars-own";
        await Expect(Page.Locator($"{id} .bit-msb-ico")).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-err)"));
        Assert.AreNotEqual("31px", await ComputedStyleAsync(id, "padding-top"));
        Assert.AreNotEqual("27px", await ComputedStyleAsync($"{id} .bit-msb-ttl", "font-size"));
        Assert.AreNotEqual("19px", await ComputedStyleAsync($"{id} .bit-msb-bdy", "font-size"));
        Assert.AreNotEqual("41px", await ComputedStyleAsync($"{id} .bit-msb-ico", "font-size"));
        Assert.AreNotEqual("333px", await ComputedStyleAsync(id, "min-width"));

        // What a parameter does not set stays the variable's.
        await Expect(Page.Locator($"{id} .bit-msb-ttl")).ToHaveCSSAsync("color", "rgb(2, 2, 200)");
    }

    // ---------- cascading parameters ----------

    [TestMethod]
    public async Task Cascading_BitParams_SetTheDefaultsAndTheBoxsOwnValuesWin()
    {
        await OpenAsync(Route);

        await Expect(Box("msb-cas-1")).ToHaveClassAsync(HasClass("bit-msb-sm"));
        await Expect(Actions("msb-cas-1")).ToHaveTextAsync(["Go back", "Don't save", "Save"]);
        await Expect(Actions("msb-cas-1").Last).ToHaveClassAsync(HasClass("bit-btn-pri"));
        await Expect(Actions("msb-cas-1").First).ToHaveClassAsync(HasClass("bit-btn-sec"));
        await Expect(CloseButton("msb-cas-1")).ToHaveAttributeAsync("title", "Dismiss");
        Assert.AreEqual("H3", await Box("msb-cas-1").Locator(".bit-msb-ttl").EvaluateAsync<string>("e => e.tagName"));

        await Expect(Actions("msb-cas-2")).ToHaveTextAsync(["Go back", "Don't save", "Discard"]);
        await Expect(Box("msb-cas-2")).ToHaveClassAsync(HasClass("bit-msb-lg"));

        await Expect(Actions("msb-cas-ok")).ToHaveTextAsync(["Go back", "Fine"]);
    }

    // ---------- RTL ----------

    [DataTestMethod]
    [DataRow("msb-rtl")]
    [DataRow("msb-rtl-inherited")]
    public async Task Rtl_TheBox_LaysItsHeaderAndFooterOutRightToLeft(string id)
    {
        await OpenAsync(Route);

        await Expect(Box(id)).ToHaveCSSAsync("direction", "rtl");
        var icon = await Box(id).Locator(".bit-msb-ict").BoundingBoxAsync();
        var title = await Box(id).Locator(".bit-msb-ttl").BoundingBoxAsync();
        var close = await CloseButton(id).BoundingBoxAsync();
        Assert.IsTrue(icon!.X > title!.X, "The glyph leads on the right.");
        Assert.IsTrue(close!.X < title.X, "The close button ends on the left.");

        var yes = await Actions(id).First.BoundingBoxAsync();
        var no = await Actions(id).Last.BoundingBoxAsync();
        Assert.IsTrue(yes!.X > no!.X, "The first button is the rightmost.");
    }

    [TestMethod]
    public async Task Rtl_AnExplicitDir_RendersTheAttributeAndTheClass()
    {
        await OpenAsync(Route);

        await Expect(Box("msb-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Box("msb-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(CloseButton("msb-rtl")).ToHaveAttributeAsync("aria-label", "بستن");
        await Expect(Actions("msb-rtl")).ToHaveTextAsync(["بله", "خیر"]);
        await Expect(Box("msb-rtl-inherited")).Not.ToHaveAttributeAsync("dir", new Regex(".*"));
    }

    // ---------- media ----------

    [TestMethod]
    public async Task ForcedColors_Active_PaintsTheGlyphInCanvasText()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        await Expect(Page.Locator("#msb-color-error .bit-msb-ico")).ToHaveCSSAsync("color", await ResolveColorAsync("CanvasText"));
    }

    [TestMethod]
    public async Task Print_TheBody_PrintsWhole()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await Expect(Page.Locator("#msb-vars .bit-msb-bdy")).ToHaveCSSAsync("overflow-y", "visible");
        await Expect(Box("msb-vars")).ToHaveCSSAsync("max-height", "none");
    }

    [TestMethod]
    public async Task NarrowScreen_TheMinWidth_IsDropped()
    {
        await OpenAsync(Route);

        StringAssert.StartsWith(await ComputedStyleAsync("#msb-basic", "min-width"), "min(");
        await Page.SetViewportSizeAsync(400, 800);
        await Expect(Box("msb-basic")).ToHaveCSSAsync("min-width", "0px");
    }

    // ---------- lifecycle ----------

    [TestMethod]
    public async Task Prerender_ThePagesHtml_ContainsTheBoxBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "<div id=\"msb-basic\" class=\"bit-msb\">");
        StringAssert.Contains(html, "<h5 id=\"msb-basic-ttl\" class=\"bit-msb-ttl\">It&#x27;s a title</h5>");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Parameters_ChangedAtRuntime_ReRenderTheBox()
    {
        await OpenAsync(Route);

        var box = Box("msb-rerender");
        await Expect(box.Locator(".bit-msb-ttl")).ToHaveTextAsync("Before");
        await Expect(Actions("msb-rerender")).ToHaveTextAsync(["Ok"]);
        await Expect(box.Locator(".bit-msb-ict")).ToHaveCountAsync(0);

        await Page.Locator("#btn-rerender").ClickAsync();

        await Expect(box.Locator(".bit-msb-ttl")).ToHaveTextAsync("After");
        await Expect(box.Locator(".bit-msb-bdy")).ToHaveTextAsync("Body after");
        await Expect(Actions("msb-rerender")).ToHaveTextAsync(["Yes", "No"]);
        await Expect(box).ToHaveClassAsync(HasClass("bit-msb-err"));
        await Expect(box.Locator("i.bit-msb-ico")).ToHaveClassAsync(HasClass("bit-icon--ErrorBadge"));
    }

    [TestMethod]
    public async Task Unmount_TheBox_LeavesNoErrorBehind()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Box("msb-mounted")).ToHaveCountAsync(0);
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Box("msb-mounted")).ToBeVisibleAsync();

        // A service box still open when the page goes away is torn down with it.
        await Page.Locator("#btn-svc-show").ClickAsync();
        await Expect(Dialog).ToBeVisibleAsync();
        await Page.GotoAsync($"{BaseUrl}/");
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Bit.BlazorUI E2E Test Host");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // ---------- helpers ----------

    private Task<string> ResolveColorAsync(string cssColor)
    {
        return Page.EvaluateAsync<string>(
            """
            value => {
                const probe = document.createElement('div');
                probe.style.color = value;
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).color;
                probe.remove();
                return resolved;
            }
            """, cssColor);
    }

    private static double ParsePx(string value) => double.Parse(value.Replace("px", "").Trim(), CultureInfo.InvariantCulture);
}
