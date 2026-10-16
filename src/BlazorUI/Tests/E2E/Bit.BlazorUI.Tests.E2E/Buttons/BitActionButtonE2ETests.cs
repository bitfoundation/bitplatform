using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Buttons;

/// <summary>
/// The end-to-end tests of BitActionButton against /e2e/action-button and /e2e/action-button/autofocus: the content
/// (ChildContent, Body, icons by name, by url and from external libraries, IconOnly) and IconPlacement, links (Href,
/// Target, Rel, Download, the disabled and loading anchor), the form types inside an EditForm and through FormId,
/// FullWidth and NoWrap, the loading states (IsLoading, LoadingLabel and its live region, AutoLoading, Reclickable,
/// LoadingDelay, the two-way bound IsLoading, LoadingTemplate), Underlined, OnClick with its MouseEventArgs and
/// StopPropagation, the accessibility attributes, FocusAsync and AutoFocus, the disabled contract with and without
/// AllowDisabledFocus, the BitParams cascade, every Color and Size with the colors and lengths the stylesheet
/// resolves, Style/Class/Styles/Classes/HtmlAttributes, the public CSS variables, RTL, Visibility, reduced motion,
/// forced colors, prerendering, re-rendering and unmounting.
/// </summary>
[TestClass]
public class BitActionButtonE2ETests : E2ETestBase
{
    private const string Transparent = "rgba(0, 0, 0, 0)";

    public static IEnumerable<object[]> ColorRows => ButtonsE2EHelpers.ColorRows;

    private ILocator Btn(string id) => Page.Locator($"#{id}");

    private Task<string> Token(string token) => Page.ResolveColorAsync($"var(--bit-clr-{token})");

    // ---------- content ----------

    [TestMethod]
    public async Task Basic_ClickingTheButton_RaisesOnClickWithTheMouseEventArgs()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("acb-basic");

        await Expect(button).ToHaveClassAsync(HasClass("bit-acb"));
        await Expect(button).ToHaveAttributeAsync("type", "button");
        await Expect(button.Locator(".bit-acb-con")).ToHaveTextAsync("Create account");
        await Expect(button.Locator("i.bit-acb-ico")).ToHaveClassAsync(HasClass("bit-icon--AddFriend"));
        await Expect(button.Locator("i.bit-acb-ico")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(button).ToHaveCSSAsync("background-color", Transparent);
        await Expect(button).ToHaveCSSAsync("color", await Token("fg-pri"));
        await Expect(button.Locator(".bit-acb-ico")).ToHaveCSSAsync("color", await Token("pri"));

        await button.ClickAsync();

        await Expect(Btn("basic-clicks")).ToHaveTextAsync("1");
        await Expect(Btn("basic-detail")).ToHaveTextAsync("click:0:1");
    }

    [TestMethod]
    public async Task Content_TextOnlyBodyIconOnlyAndIconUrl_RenderTheirParts()
    {
        await OpenAsync("/e2e/action-button");

        await Expect(Btn("acb-text-only").Locator(".bit-acb-ico")).ToHaveCountAsync(0);
        await Expect(Btn("acb-text-only").Locator(".bit-acb-con")).ToHaveTextAsync("Text only");

        await Expect(Btn("acb-body").Locator(".bit-acb-con .body-bold")).ToHaveTextAsync("Body");

        var iconOnly = Btn("acb-icon-only");
        await Expect(iconOnly).ToHaveClassAsync(HasClass("bit-acb-ion"));
        await Expect(iconOnly.Locator(".bit-acb-con")).ToHaveCountAsync(0);
        await Expect(iconOnly).ToHaveAccessibleNameAsync("Call");
        var box = await iconOnly.BoxAsync();
        Assert.IsTrue(box.Width >= box.Height - 0.5, "An icon-only action button is at least as wide as it is tall.");

        // IconOnly drops the content altogether - which is why the docs ask for an AriaLabel beside it
        var iconOnlyText = Btn("acb-icon-only-text");
        await Expect(iconOnlyText.Locator(".bit-acb-con")).ToHaveCountAsync(0);
        await Expect(iconOnlyText).Not.ToContainTextAsync("Mail");

        var image = Btn("acb-icon-url").Locator("img.bit-acb-icu");
        await Expect(image).ToHaveAttributeAsync("alt", "");
        await Expect(image).ToHaveAttributeAsync("aria-hidden", "true");
        Assert.AreEqual(await Page.ResolvePxAsync("var(--bit-siz-icon-md)"), (await image.BoxAsync()).Width, 0.5);
    }

    [TestMethod]
    public async Task IconPlacement_StartOrEnd_PutsTheIconBeforeOrAfterTheContent()
    {
        await OpenAsync("/e2e/action-button");

        var start = Btn("acb-start");
        Assert.IsTrue((await start.Locator(".bit-acb-ico").BoxAsync()).X < (await start.Locator(".bit-acb-con").BoxAsync()).X);

        var end = Btn("acb-end");
        await Expect(end).ToHaveClassAsync(HasClass("bit-acb-eni"));
        await Expect(end.Locator(".bit-acb-inn")).ToHaveCSSAsync("flex-direction", "row-reverse");
        Assert.IsTrue((await end.Locator(".bit-acb-ico").BoxAsync()).X > (await end.Locator(".bit-acb-con").BoxAsync()).X);
    }

    [TestMethod]
    [DataRow("acb-ext-string", "fa-solid fa-house")]
    [DataRow("acb-ext-css", "fa-solid fa-heart")]
    [DataRow("acb-ext-fa", "fa-solid fa-rocket")]
    [DataRow("acb-ext-bi", "bi bi-gear-fill")]
    public async Task Icon_ExternalLibraryClasses_AreRenderedOnTheIconElement(string id, string classes)
    {
        await OpenAsync("/e2e/action-button");

        var icon = Btn(id).Locator("i.bit-acb-ico");
        foreach (var cssClass in classes.Split(' '))
        {
            await Expect(icon).ToHaveClassAsync(HasClass(cssClass));
        }

        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-icon"));
    }

    // ---------- links ----------

    [TestMethod]
    public async Task Href_WithATarget_RendersAnAnchorThatOpensANewTabWithNoOpener()
    {
        await OpenAsync("/e2e/action-button");

        var link = Btn("acb-href-blank");
        Assert.AreEqual("A", await link.EvaluateAsync<string>("el => el.tagName"));
        await Expect(link).ToHaveAttributeAsync("href", "/");
        await Expect(link).ToHaveAttributeAsync("rel", "noopener");
        await Expect(link).ToHaveAttributeAsync("target", "_blank");

        var popup = await Page.RunAndWaitForPopupAsync(() => link.ClickAsync());
        await popup.WaitForLoadStateAsync();
        Assert.AreEqual($"{BaseUrl}/", popup.Url);
        await popup.CloseAsync();
    }

    [TestMethod]
    [DataRow("acb-href-nofollow", "nofollow noopener")]
    [DataRow("acb-href-noreferrer", "nofollow noreferrer")]
    [DataRow("acb-href-handwritten", "external noopener")]
    public async Task Rel_EachCombination_RendersTheRelWithNoOpenerAddedOnlyWhereNeeded(string id, string rel)
    {
        await OpenAsync("/e2e/action-button");

        await Expect(Btn(id)).ToHaveAttributeAsync("rel", rel);
    }

    [TestMethod]
    public async Task Href_SameTabOrHashOnly_CarriesNoRel()
    {
        await OpenAsync("/e2e/action-button");

        await Expect(Btn("acb-href-same-tab")).Not.ToHaveAttributeAsync("rel", ButtonsE2EHelpers.AnyValue);
        await Expect(Btn("acb-href-hash")).Not.ToHaveAttributeAsync("rel", ButtonsE2EHelpers.AnyValue);
        await Expect(Btn("acb-href-hash")).ToHaveAttributeAsync("href", "#href");
    }

    [TestMethod]
    public async Task Href_Clicked_RaisesOnClickAndNavigates()
    {
        await OpenAsync("/e2e/action-button");

        await Btn("acb-href-nav").ClickAsync();

        await Expect(Btn("href-clicks")).ToHaveTextAsync("1");
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/action-button#href$"));
    }

    [TestMethod]
    public async Task Download_OnALink_DownloadsTheFileUnderItsName()
    {
        await OpenAsync("/e2e/action-button");

        var link = Btn("acb-href-download");
        await Expect(link).ToHaveAttributeAsync("download", "icon.svg");

        var download = await Page.RunAndWaitForDownloadAsync(() => link.ClickAsync());
        Assert.AreEqual("icon.svg", download.SuggestedFilename);
    }

    [TestMethod]
    public async Task Href_DisabledOrLoading_DropsTheHrefAndKeepsTheLinkFocusable()
    {
        await OpenAsync("/e2e/action-button");

        var disabled = Btn("acb-href-disabled");
        await Expect(disabled).Not.ToHaveAttributeAsync("href", ButtonsE2EHelpers.AnyValue);
        await Expect(disabled).ToHaveAttributeAsync("role", "link");
        await Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(disabled).ToHaveAttributeAsync("tabindex", "0");

        var loading = Btn("acb-href-loading");
        await Expect(loading).Not.ToHaveAttributeAsync("href", ButtonsE2EHelpers.AnyValue);
        await Expect(loading).ToHaveAttributeAsync("role", "link");
        await Expect(loading).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(loading).ToHaveAttributeAsync("tabindex", "0");
        await loading.FocusAsync();
        await Expect(loading).ToBeFocusedAsync();
    }

    // ---------- forms ----------

    [TestMethod]
    public async Task ButtonType_InsideAndOutsideAForm_RendersTheNativeTypeItDefaultsToOrIsGiven()
    {
        await OpenAsync("/e2e/action-button");

        await Expect(Btn("acb-outside")).ToHaveAttributeAsync("type", "button");
        await Expect(Btn("form-default")).ToHaveAttributeAsync("type", "submit");
        await Expect(Btn("form-submit")).ToHaveAttributeAsync("type", "submit");
        await Expect(Btn("form-reset")).ToHaveAttributeAsync("type", "reset");
        await Expect(Btn("form-button")).ToHaveAttributeAsync("type", "button");
    }

    [TestMethod]
    public async Task Submit_WithAnInvalidThenAValidModel_RunsTheInvalidThenTheValidSubmit()
    {
        await OpenAsync("/e2e/action-button");

        await Btn("form-submit").ClickAsync();
        await Expect(Btn("form-invalid")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#form .validation-message")).ToBeVisibleAsync();

        await Page.Locator("#form-required input").FillAsync("hello");
        await Btn("form-default").ClickAsync();
        await Expect(Btn("form-valid")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ResetAndButton_InsideTheForm_ClearOrLeaveTheFormWithoutSubmitting()
    {
        await OpenAsync("/e2e/action-button");

        var input = Page.Locator("#form-required input");
        await input.FillAsync("typed");
        await Expect(Btn("form-model")).ToHaveTextAsync("typed");

        await Btn("form-reset").ClickAsync();
        await Expect(input).ToHaveValueAsync("");
        await Expect(Btn("form-model")).ToHaveTextAsync("");

        await Btn("form-button").ClickAsync();
        await Expect(Btn("form-button-clicks")).ToHaveTextAsync("1");

        await Btn("form-loading").DispatchEventAsync("click");
        await Btn("form-button").ClickAsync();
        await Expect(Btn("form-button-clicks")).ToHaveTextAsync("2");

        await Expect(Btn("form-valid")).ToHaveTextAsync("0");
        await Expect(Btn("form-invalid")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task FormId_OnAButtonOutsideTheForm_SubmitsThatForm()
    {
        await OpenAsync("/e2e/action-button");

        var submit = Btn("ext-submit");
        await Expect(submit).ToHaveAttributeAsync("form", "acb-ext-form");

        await submit.ClickAsync();
        await Expect(Page.Locator("#ext-required")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Btn("ext-submits")).ToHaveTextAsync("0");

        await Page.Locator("#ext-required input").FillAsync("external");
        await submit.ClickAsync();
        await Expect(Btn("ext-submits")).ToHaveTextAsync("1");
    }

    // ---------- layout ----------

    [TestMethod]
    public async Task FullWidth_InAContainer_StretchesTheButtonAndItsContentAcrossIt()
    {
        await OpenAsync("/e2e/action-button");

        var container = await Btn("fw-container").BoxAsync();

        await Expect(Btn("acb-fw")).ToHaveClassAsync(HasClass("bit-acb-fwi"));
        ButtonsE2EHelpers.AreClose(container.Width, (await Btn("acb-fw").BoxAsync()).Width, 0.5, "full width");
        Assert.IsTrue((await Btn("acb-not-fw").BoxAsync()).Width < container.Width - 50);

        // the content grows, so an End icon is pushed to the far edge of the row
        var end = Btn("acb-fw-end");
        var endBox = await end.BoxAsync();
        var icon = await end.Locator(".bit-acb-ico").BoxAsync();
        Assert.IsTrue(endBox.X + endBox.Width - (icon.X + icon.Width) < 20, "The End icon sits at the far edge of a full-width row.");
    }

    [TestMethod]
    public async Task NoWrap_OnALongLabel_EndsTheLineWithAnEllipsis()
    {
        await OpenAsync("/e2e/action-button");

        var noWrap = Btn("acb-nowrap");
        var content = noWrap.Locator(".bit-acb-con");

        await Expect(noWrap).ToHaveClassAsync(HasClass("bit-acb-nwr"));
        await Expect(content).ToHaveCSSAsync("white-space", "nowrap");
        await Expect(content).ToHaveCSSAsync("text-overflow", "ellipsis");
        Assert.IsTrue(await content.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"));
        Assert.IsTrue((await Btn("acb-wrap").BoxAsync()).Height > (await noWrap.BoxAsync()).Height);
    }

    // ---------- loading ----------

    [TestMethod]
    public async Task IsLoading_TurnedOn_SwapsTheIconForASpinnerMarksItBusyAndIgnoresClicks()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("acb-loading");

        await button.ClickAsync();
        await Expect(Btn("loading-clicks")).ToHaveTextAsync("1");

        await Btn("btn-toggle-loading").ClickAsync();

        await Expect(button).ToHaveClassAsync(HasClass("bit-acb-lod"));
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button.Locator(".bit-acb-spn")).ToBeVisibleAsync();
        await Expect(button.Locator(".bit-acb-spn")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(button.Locator(".bit-acb-ico")).ToHaveCountAsync(0);
        await Expect(button.Locator(".bit-acb-con")).ToHaveTextAsync("Save changes");
        await Expect(button).ToHaveCSSAsync("cursor", "default");
        Assert.AreEqual("bit-spin", await button.Locator(".bit-acb-spn").StyleAsync("animation-name"));

        // a pointer click cannot reach it, and a click an assistive technology dispatches is refused in .NET
        await button.DispatchEventAsync("click");
        await Btn("btn-toggle-loading").ClickAsync();

        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-acb-lod"));
        await Expect(button.Locator(".bit-acb-ico")).ToBeVisibleAsync();
        await Expect(Btn("loading-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task LoadingLabel_WhileLoading_ReplacesTheContentAndIsAnnouncedFromALiveRegion()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("acb-loading-label");
        var status = Page.Locator("#acb-loading-label + .bit-acb-sts");

        await Expect(status).ToHaveAttributeAsync("role", "status");
        await Expect(status).ToHaveTextAsync("");

        await Btn("btn-toggle-loading").ClickAsync();

        await Expect(button.Locator(".bit-acb-lbl")).ToHaveTextAsync("Saving...");
        await Expect(button.Locator(".bit-acb-con")).ToHaveCountAsync(0);
        await Expect(status).ToHaveTextAsync("Saving...");

        var iconOnly = Btn("acb-loading-icon-only");
        await Expect(iconOnly.Locator(".bit-acb-spn")).ToBeVisibleAsync();
        await Expect(iconOnly.Locator(".bit-acb-lbl")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#acb-loading-icon-only + .bit-acb-sts")).ToHaveTextAsync("Saving...");
    }

    [TestMethod]
    public async Task AutoLoading_WhileOnClickRuns_IsLoadingAndRefusesASecondClick()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("acb-auto");

        await button.ClickAsync();
        await Expect(Btn("auto-clicks")).ToHaveTextAsync("1");
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button.Locator(".bit-acb-spn")).ToBeVisibleAsync();

        await button.DispatchEventAsync("click");
        await Btn("btn-release-auto").ClickAsync();

        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
        await Expect(Btn("auto-clicks")).ToHaveTextAsync("1");

        await button.ClickAsync();
        await Expect(Btn("auto-clicks")).ToHaveTextAsync("2");
        await Btn("btn-cancel-auto").ClickAsync();
        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task AutoLoadingReclickable_ClickedAgainWhileLoading_RaisesOnClickAgain()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("acb-reclickable");

        await button.ClickAsync();
        await Expect(Btn("reclick-clicks")).ToHaveTextAsync("1");
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button).ToHaveCSSAsync("cursor", "pointer");

        await button.ClickAsync();
        await Expect(Btn("reclick-clicks")).ToHaveTextAsync("2");
        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");

        await Btn("btn-release-reclick").ClickAsync();
        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
    }

    [TestMethod]
    public async Task LoadingDelay_ALongRun_ShowsTheSpinnerOnlyOnceTheDelayIsUp()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("acb-delay");

        await button.EvaluateAsync(
            """
            el => {
                window.__delay = {};
                const start = performance.now();
                new MutationObserver(() => {
                    if (el.getAttribute('aria-busy') === 'true' && window.__delay.busy === undefined) window.__delay.busy = performance.now() - start;
                    if (el.querySelector('.bit-acb-spn') && window.__delay.spinner === undefined) window.__delay.spinner = performance.now() - start;
                }).observe(el, { attributes: true, childList: true, subtree: true });
            }
            """);

        await button.ClickAsync();
        await Expect(button.Locator(".bit-acb-spn")).ToBeVisibleAsync();

        var busy = await Page.EvaluateAsync<double>("() => window.__delay.busy");
        var spinner = await Page.EvaluateAsync<double>("() => window.__delay.spinner");
        Assert.IsTrue(spinner - busy >= 450, $"The spinner came {spinner - busy}ms after the busy state; the delay is 500ms.");

        await Btn("btn-release-delay").ClickAsync();
        await Expect(button.Locator(".bit-acb-spn")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task LoadingDelay_ARunEndingInsideTheDelay_NeverShowsTheSpinner()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("acb-delay-fast");

        await button.EvaluateAsync(
            """
            el => {
                window.__fast = { busy: false, spinner: false };
                new MutationObserver(() => {
                    if (el.getAttribute('aria-busy') === 'true') window.__fast.busy = true;
                    if (el.querySelector('.bit-acb-spn')) window.__fast.spinner = true;
                }).observe(el, { attributes: true, childList: true, subtree: true });
            }
            """);

        await button.ClickAsync();
        await Expect(Btn("delay-fast-done")).ToHaveTextAsync("1");
        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);

        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__fast.busy"));
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => window.__fast.spinner"));
    }

    [TestMethod]
    public async Task IsLoading_BoundTwoWay_FollowsTheAutoLoadingRun()
    {
        await OpenAsync("/e2e/action-button");

        await Expect(Btn("bound-loading")).ToHaveTextAsync("False");
        await Btn("acb-bound").ClickAsync();
        await Expect(Btn("bound-loading")).ToHaveTextAsync("True");
        await Btn("btn-release-bound").ClickAsync();
        await Expect(Btn("bound-loading")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task LoadingTemplate_WhileLoading_TakesOverTheWholeLoadingVisual()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("acb-template");
        await Expect(button.Locator(".bit-acb-con")).ToHaveTextAsync("Download");

        await Btn("btn-toggle-template").ClickAsync();

        await Expect(button.Locator(".tpl-loading")).ToHaveTextAsync("Downloading...");
        await Expect(button.Locator(".bit-acb-spn")).ToHaveCountAsync(0);
        await Expect(button.Locator(".bit-acb-con")).ToHaveCountAsync(0);

        await Btn("btn-toggle-template").ClickAsync();
        await Expect(button.Locator(".tpl-loading")).ToHaveCountAsync(0);
        await Expect(button.Locator(".bit-acb-con")).ToHaveTextAsync("Download");
    }

    // ---------- underlined ----------

    [TestMethod]
    public async Task Underlined_SetOrNot_UnderlinesTheContentAndThickensItOnHover()
    {
        await OpenAsync("/e2e/action-button");

        var underlined = Btn("acb-underlined");
        var content = underlined.Locator(".bit-acb-con");

        await Expect(underlined).ToHaveClassAsync(HasClass("bit-acb-und"));
        await Expect(content).ToHaveCSSAsync("text-decoration-line", "underline");
        await Expect(Btn("acb-not-underlined").Locator(".bit-acb-con")).ToHaveCSSAsync("text-decoration-line", "none");
        await Expect(Btn("acb-underlined-loading").Locator(".bit-acb-lbl")).ToHaveCSSAsync("text-decoration-line", "underline");

        var rest = await content.PxAsync("text-decoration-thickness");
        await underlined.HoverAsync();
        var thick = await Page.ResolvePxAsync("var(--bit-shp-brd-width-thick)");
        await Expect(content).ToHaveCSSAsync("text-decoration-thickness", $"{thick}px");
        Assert.IsTrue(thick > rest, $"The hover thickens the underline ({rest}px -> {thick}px).");
    }

    // ---------- events ----------

    [TestMethod]
    public async Task OnClick_ByPointerEnterAndSpace_IsRaisedOncePerPress()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("evt-click");

        await button.ClickAsync();
        await Expect(Btn("evt-clicks")).ToHaveTextAsync("1");

        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Btn("evt-clicks")).ToHaveTextAsync("2");

        await Page.Keyboard.PressAsync("Space");
        await Expect(Btn("evt-clicks")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task StopPropagation_SetOrNot_KeepsTheClickFromTheParentOrLetsItBubble()
    {
        await OpenAsync("/e2e/action-button");

        await Btn("evt-stop").ClickAsync();
        await Expect(Btn("evt-child-clicks")).ToHaveTextAsync("1");
        await Btn("evt-bubble").ClickAsync();
        await Expect(Btn("evt-child-clicks")).ToHaveTextAsync("2");
        await Expect(Btn("evt-parent-clicks")).ToHaveTextAsync("1");
    }

    // ---------- accessibility ----------

    [TestMethod]
    public async Task Accessibility_TitleAriaLabelDescriptionAndHidden_AreRendered()
    {
        await OpenAsync("/e2e/action-button");

        await Expect(Btn("a11y-title")).ToHaveAttributeAsync("title", "Save your changes");
        await Expect(Btn("a11y-aria-label")).ToHaveAccessibleNameAsync("Delete the item");

        var described = Btn("a11y-desc");
        await Expect(described).ToHaveAttributeAsync("aria-describedby", "a11y-extra a11y-desc-dsc");
        await Expect(Btn("a11y-desc-dsc")).ToHaveTextAsync("PDF, 2 megabytes");
        await Expect(described).ToHaveAccessibleDescriptionAsync("Author description PDF, 2 megabytes");

        await Expect(Btn("a11y-hidden")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Btn("a11y-hidden")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Btn("a11y-tabindex")).ToHaveAttributeAsync("tabindex", "-1");

        await described.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Btn("btn-focus")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task FocusAsync_CalledFromCode_MovesTheFocusOntoTheButton()
    {
        await OpenAsync("/e2e/action-button");

        await Btn("btn-focus").ClickAsync();
        await Expect(Btn("a11y-focus-target")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AutoFocus_OnTheFirstRender_FocusesTheButtonButNeverAHiddenOrDisabledOne()
    {
        await OpenAsync("/e2e/action-button/autofocus");

        await Expect(Btn("autofocus-hidden")).Not.ToHaveAttributeAsync("autofocus", ButtonsE2EHelpers.AnyValue);
        await Expect(Btn("autofocus-disabled")).Not.ToHaveAttributeAsync("autofocus", ButtonsE2EHelpers.AnyValue);
        await Expect(Btn("autofocus-btn")).ToHaveAttributeAsync("autofocus", "");
        await Expect(Btn("autofocus-btn")).ToBeFocusedAsync();
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_ByDefault_RendersTheNativeAttributeAndLeavesTheTabOrder()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("dis-native");

        await Expect(button).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(button).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(button).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToHaveCSSAsync("pointer-events", "none");
        await Expect(button).ToHaveCSSAsync("color", await Token("fg-dis"));
        await Expect(button.Locator(".bit-acb-ico")).ToHaveCSSAsync("color", await Token("pri-dis-text"));

        await Page.Locator("#dis-before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Btn("dis-focusable")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Disabled_WithAllowDisabledFocus_StaysFocusableAndRunsNoHandler()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("dis-focusable");

        await Expect(button).Not.ToHaveAttributeAsync("disabled", ButtonsE2EHelpers.AnyValue);
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");

        await button.FocusAsync();
        await Expect(button).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Space");
        await button.DispatchEventAsync("click");
        await button.DispatchEventAsync("mousedown");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#dis-after")).ToBeFocusedAsync();

        await Btn("btn-toggle-disabled").ClickAsync();
        await Btn("dis-toggle").ClickAsync();
        await Expect(Btn("dis-toggle-clicks")).ToHaveTextAsync("1");

        await Expect(Btn("dis-clicks")).ToHaveTextAsync("0");
        await Expect(Btn("dis-page-events")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Disabled_ToggledAtRuntime_RestoresTheHandlersAndTheColors()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("dis-toggle");

        await Expect(button).ToHaveClassAsync(HasClass("bit-dis"));
        await button.DispatchEventAsync("click");
        await button.DispatchEventAsync("mousedown");

        await Btn("btn-toggle-disabled").ClickAsync();
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToBeEnabledAsync();
        await Expect(button).ToHaveCSSAsync("color", await Token("fg-pri"));
        await Expect(Btn("dis-toggle-clicks")).ToHaveTextAsync("0");
        await Expect(Btn("dis-toggle-page-events")).ToHaveTextAsync("0");

        await button.ClickAsync();
        await Expect(Btn("dis-toggle-clicks")).ToHaveTextAsync("1");
        await Expect(Btn("dis-toggle-page-events")).ToHaveTextAsync("1");
    }

    // ---------- params, color, size ----------

    [TestMethod]
    public async Task BitParams_AroundButtons_FillsInWhatEachLeftUnset()
    {
        await OpenAsync("/e2e/action-button");

        var inherit = Btn("params-inherit");
        await Expect(inherit).ToHaveClassAsync(HasClass("bit-acb-und"));
        await Expect(inherit).ToHaveClassAsync(HasClass("bit-acb-eni"));
        await Expect(inherit).ToHaveClassAsync(HasClass("bit-acb-ter"));
        await Expect(inherit).ToHaveClassAsync(HasClass("bit-acb-lg"));
        await Expect(inherit).ToHaveAttributeAsync("target", "_blank");
        await Expect(inherit).ToHaveAttributeAsync("rel", "noopener");
        await Expect(inherit.Locator(".bit-acb-ico")).ToHaveCSSAsync("color", await Token("ter"));

        var own = Btn("params-own");
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-acb-und"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-acb-err"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-acb-ter"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-acb-eni"));
        await Expect(own).ToHaveAttributeAsync("target", "_blank");

        var outside = Btn("params-outside");
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-acb-und"));
        await Expect(outside).Not.ToHaveAttributeAsync("target", ButtonsE2EHelpers.AnyValue);
    }

    [TestMethod]
    [DynamicData(nameof(ColorRows))]
    public async Task Color_EachValue_PaintsTheIconAndTheHoverFromItsRole(string name, string suffix, string token, string onToken)
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn($"color-{name}");

        await Expect(button).ToHaveClassAsync(HasClass($"bit-acb-{suffix}"));
        await Expect(button.Locator(".bit-acb-ico")).ToHaveCSSAsync("color", await Token(token));
        await Expect(button).ToHaveCSSAsync("color", await Token("fg-pri"));

        var disabled = Btn($"color-dis-{name}");
        await Expect(disabled.Locator(".bit-acb-ico")).ToHaveCSSAsync("color", await Token($"{token}-dis-text"));

        await button.HoverAsync();
        var hover = await Token($"{token}-hover");
        await Expect(button).ToHaveCSSAsync("color", hover);
        await Expect(button.Locator(".bit-acb-ico")).ToHaveCSSAsync("color", hover);
    }

    [TestMethod]
    public async Task Color_Unset_PaintsThePrimaryRoleWithoutAClassOfItsOwn()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("color-default");
        var classes = await button.GetAttributeAsync("class") ?? string.Empty;
        foreach (var color in ButtonsE2EHelpers.Colors)
        {
            Assert.IsFalse(Regex.IsMatch(classes, $@"(^|\s)bit-acb-{color.Suffix}(\s|$)"));
        }

        await Expect(button.Locator(".bit-acb-ico")).ToHaveCSSAsync("color", await Token("pri"));
        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("color", await Token("pri-hover"));
    }

    [TestMethod]
    [DataRow("Small", "sm", "xs")]
    [DataRow("Medium", "md", "sm")]
    [DataRow("Large", "lg", "md")]
    public async Task Size_EachValue_SetsTheHeightTypeAndIconOfItsClass(string size, string suffix, string font)
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn($"size-{size}");
        var height = await Page.ResolvePxAsync(ButtonsE2EHelpers.ControlHeight(suffix));

        await Expect(button).ToHaveClassAsync(HasClass($"bit-acb-{suffix}"));
        Assert.AreEqual(height, await button.PxAsync("min-height"), "min-height");
        Assert.AreEqual(await Page.ResolvePxAsync($"var(--bit-tpg-fs-{font})"), await button.PxAsync("font-size"), "font-size");
        Assert.AreEqual(await Page.ResolvePxAsync($"var(--bit-siz-icon-{suffix})"), await button.Locator(".bit-acb-ico").PxAsync("font-size"), "icon size");

        var iconOnly = Btn($"size-icon-{size}");
        Assert.AreEqual(height, await iconOnly.PxAsync("min-width"), "An icon-only button is at least as wide as the control height.");
    }

    [TestMethod]
    public async Task Size_Unset_IsTheMediumButtonWithoutAClassOfItsOwn()
    {
        await OpenAsync("/e2e/action-button");

        await Expect(Btn("size-default")).Not.ToHaveClassAsync(HasClass("bit-acb-md"));
        Assert.AreEqual(await Btn("size-Medium").PxAsync("min-height"), await Btn("size-default").PxAsync("min-height"));
        Assert.AreEqual(await Btn("size-Medium").PxAsync("font-size"), await Btn("size-default").PxAsync("font-size"));
    }

    // ---------- styles and variables ----------

    [TestMethod]
    public async Task StyleClassStylesAndClasses_ReachTheRootAndEveryPart()
    {
        await OpenAsync("/e2e/action-button");

        await Expect(Btn("sc-style")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(Btn("sc-class")).ToHaveClassAsync(HasClass("e2e-custom-class"));
        await Expect(Btn("sc-attrs")).ToHaveAttributeAsync("data-e2e", "custom-attribute");

        var styles = Btn("sc-styles");
        await Expect(styles).ToHaveCSSAsync("outline-style", "dotted");
        await Expect(styles.Locator(".bit-acb-ico")).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        await Expect(styles.Locator(".bit-acb-con")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");

        var stylesLoading = Btn("sc-styles-loading");
        await Expect(stylesLoading.Locator(".bit-acb-spn")).ToHaveCSSAsync("border-top-color", "rgb(218, 165, 32)");
        await Expect(stylesLoading.Locator(".bit-acb-lbl")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");

        var classes = Btn("sc-classes");
        await Expect(classes).ToHaveClassAsync(HasClass("c-root"));
        await Expect(classes.Locator(".bit-acb-ico")).ToHaveClassAsync(HasClass("c-icon"));
        await Expect(classes.Locator(".bit-acb-con")).ToHaveClassAsync(HasClass("c-content"));

        var classesLoading = Btn("sc-classes-loading");
        await Expect(classesLoading.Locator(".bit-acb-spn")).ToHaveClassAsync(HasClass("c-spinner"));
        await Expect(classesLoading.Locator(".bit-acb-lbl")).ToHaveClassAsync(HasClass("c-label"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheDefaultsAndLoseToExplicitParameters()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("vars-default");
        await Expect(button).ToHaveCSSAsync("color", "rgb(10, 20, 30)");
        await Expect(button.Locator(".bit-acb-ico")).ToHaveCSSAsync("color", "rgb(200, 0, 0)");
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(240, 240, 200)");
        await Expect(button).ToHaveCSSAsync("border-top-left-radius", "7px");
        await Expect(button).ToHaveCSSAsync("min-height", "50px");
        await Expect(button).ToHaveCSSAsync("padding-left", "20px");
        await Expect(button).ToHaveCSSAsync("font-size", "19px");
        await Expect(button.Locator(".bit-acb-inn")).ToHaveCSSAsync("column-gap", "15px");
        await Expect(button.Locator(".bit-acb-ico")).ToHaveCSSAsync("font-size", "26px");

        await Expect(Btn("vars-color").Locator(".bit-acb-ico")).ToHaveCSSAsync("color", await Token("err"));
        Assert.AreEqual(await Page.ResolvePxAsync(ButtonsE2EHelpers.ControlHeight("sm")), await Btn("vars-size").PxAsync("min-height"));
        await Expect(Btn("vars-disabled")).ToHaveCSSAsync("color", "rgb(120, 120, 120)");

        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(255, 255, 0)");
    }

    // ---------- direction, visibility, motion ----------

    [TestMethod]
    public async Task Rtl_ExplicitOrInherited_PutsTheIconOnTheRight()
    {
        await OpenAsync("/e2e/action-button");

        async Task<bool> IconAfterContent(string id)
        {
            var button = Btn(id);
            return (await button.Locator(".bit-acb-ico").BoxAsync()).X > (await button.Locator(".bit-acb-con").BoxAsync()).X;
        }

        Assert.IsFalse(await IconAfterContent("rtl-ltr"));

        await Expect(Btn("rtl-explicit")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Btn("rtl-explicit")).ToHaveClassAsync(HasClass("bit-rtl"));
        Assert.IsTrue(await IconAfterContent("rtl-explicit"));
        Assert.IsFalse(await IconAfterContent("rtl-explicit-end"));

        await Expect(Btn("rtl-inherited")).Not.ToHaveAttributeAsync("dir", ButtonsE2EHelpers.AnyValue);
        Assert.IsTrue(await IconAfterContent("rtl-inherited"));
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed_HideTheButtonKeepingOrDroppingItsBox()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("vis-btn");

        await Btn("btn-vis-hidden").ClickAsync();
        await Expect(button).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue(await button.EvaluateAsync<double>("el => el.getBoundingClientRect().width") > 0);

        await Btn("btn-vis-collapsed").ClickAsync();
        await Expect(button).ToHaveCSSAsync("display", "none");

        await Btn("btn-vis-visible").ClickAsync();
        await Expect(button).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ReducedMotion_Requested_CollapsesTheTransitionsUnlessForceAnimationIsSet()
    {
        await OpenAsync("/e2e/action-button");

        var full = ButtonsE2EHelpers.ParseSeconds(await Btn("motion-default").StyleAsync("transition-duration"));
        Assert.IsTrue(full >= 0.05);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        Assert.IsTrue(ButtonsE2EHelpers.ParseSeconds(await Btn("motion-default").StyleAsync("transition-duration")) < 0.001);
        Assert.AreEqual(full, ButtonsE2EHelpers.ParseSeconds(await Btn("motion-forced").StyleAsync("transition-duration")), 0.0001);
    }

    [TestMethod]
    public async Task ForcedColors_Active_PaintsADisabledButtonInGrayText()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync("/e2e/action-button");
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var grayText = await Page.ResolveColorAsync("GrayText");
        await Expect(Btn("dis-native")).ToHaveCSSAsync("color", grayText);
        await Expect(Btn("dis-native").Locator(".bit-acb-ico")).ToHaveCSSAsync("color", grayText);
    }

    // ---------- lifecycle ----------

    [TestMethod]
    public async Task Prerender_ThePagesHtml_ContainsTheButtonsBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/action-button");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Matches(html, new Regex(@"<button id=""acb-basic"" class=""[^""]*\bbit-acb\b"));
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Parameters_ChangedAtRuntime_ReRenderTheButton()
    {
        await OpenAsync("/e2e/action-button");

        var button = Btn("rerender-btn");
        await Expect(button.Locator(".bit-acb-ico")).ToHaveCountAsync(0);

        await Btn("btn-rerender").ClickAsync();

        await Expect(button).ToHaveClassAsync(HasClass("bit-acb-err"));
        await Expect(button).ToHaveClassAsync(HasClass("bit-acb-lg"));
        await Expect(button).ToHaveClassAsync(HasClass("bit-acb-und"));
        await Expect(button).ToHaveClassAsync(HasClass("bit-acb-eni"));
        await Expect(button.Locator(".bit-acb-ico")).ToHaveCSSAsync("color", await Token("err"));
        await Expect(button.Locator(".bit-acb-con")).ToHaveTextAsync("After");
    }

    [TestMethod]
    public async Task Unmount_WhileAnAutoLoadingRunIsInFlight_LeavesNoErrorBehind()
    {
        await OpenAsync("/e2e/action-button");

        await Btn("mounted-btn").ClickAsync();
        await Expect(Btn("mounted-btn")).ToHaveAttributeAsync("aria-busy", "true");
        await Btn("btn-unmount").ClickAsync();
        await Expect(Btn("mounted-btn")).ToHaveCountAsync(0);
        await Expect(Btn("mounted-done")).ToHaveTextAsync("1");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
