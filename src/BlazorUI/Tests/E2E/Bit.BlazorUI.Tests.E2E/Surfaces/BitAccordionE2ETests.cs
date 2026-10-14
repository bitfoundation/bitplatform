using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// Drives BitAccordion (/e2e/accordion) in a real browser: the header toggling the panel by pointer and keyboard and
/// the grid-row height transition actually playing to the content's height, two-way, controlled and one-way binding,
/// every event with its arguments (OnToggling refusing and being awaited with the header busy), the Expand / Collapse /
/// Toggle / FocusAsync methods and the focus handed back to the header, LazyContent / UnmountOnCollapse / Busy,
/// MaxHeight, TransitionDuration and reduced motion, HiddenUntilFound revealed by an anchor jump, ExpandOnPrint, the
/// three templates, ReadOnly and Disabled (the panel staying live), the heading / button / region wiring, the BitParams
/// cascade, Background / Border / NoBorder / Size, external icons, Style / Class / Styles / Classes and the public CSS
/// variables, RTL explicit and inherited, Visibility, HtmlAttributes, prerendering, re-rendering and unmounting.
/// </summary>
[TestClass]
public class BitAccordionE2ETests : E2ETestBase
{
    private const string Url = "/e2e/accordion";
    private const string Transparent = "rgba(0, 0, 0, 0)";

    private ILocator Root(string id) => Page.Locator($"#{id}");
    private ILocator Header(string id) => Page.Locator($"#{id}-hdr");
    private ILocator Content(string id) => Page.Locator($"#{id}-cnt");
    private ILocator HeaderPart(string id, string cssClass) => Page.Locator($"#{id} > .bit-acd-hwr .{cssClass}");
    private ILocator Row(string id) => Page.Locator($"#{id} > .bit-acd-cnt");
    private ILocator Text(string id) => Page.Locator($"#{id}");

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheAccordionsBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"acd-basic\"");
        StringAssert.Contains(html, "id=\"acd-basic-hdr\"");
        StringAssert.Contains(html, "aria-expanded=\"false\"");
        StringAssert.Contains(html, "Basic title");
        Assert.IsTrue(Regex.IsMatch(html, "id=\"acd-default\"[^>]*class=\"[^\"]*bit-acd-exp"), "DefaultIsExpanded is not prerendered open.");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_ClickTheHeader_ExpandsAndCollapsesThePanel()
    {
        await OpenAsync(Url);

        await Expect(Header("acd-basic")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(HeaderPart("acd-basic", "bit-acd-ttl")).ToHaveTextAsync("Basic title");
        await Expect(HeaderPart("acd-basic", "bit-acd-des")).ToHaveTextAsync("Basic description");
        await Expect(HeaderPart("acd-basic", "bit-acd-ico")).ToHaveClassAsync(HasClass("bit-icon--Settings"));
        await Expect(Text("basic-content")).ToBeHiddenAsync();
        Assert.AreEqual(0, (await Row("acd-basic").BoundingBoxAsync())!.Height, 0.5);

        await Header("acd-basic").ClickAsync();

        await Expect(Header("acd-basic")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Root("acd-basic")).ToHaveClassAsync(HasClass("bit-acd-exp"));
        await Expect(Content("acd-basic")).ToHaveClassAsync(HasClass("bit-acd-cex"));
        await Expect(Text("basic-content")).ToBeVisibleAsync();
        await WaitForFullHeightAsync("acd-basic");

        await Header("acd-basic").ClickAsync();

        await Expect(Header("acd-basic")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Root("acd-basic")).Not.ToHaveClassAsync(HasClass("bit-acd-exp"));
        await Expect(Text("basic-content")).ToBeHiddenAsync();
        await PollAsync(async () => (await Row("acd-basic").BoundingBoxAsync())!.Height, h => h < 0.5, "the closed row keeps a height");
    }

    [TestMethod]
    public async Task DefaultIsExpanded_FirstRender_IsOpen()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-default")).ToHaveClassAsync(HasClass("bit-acd-exp"));
        await Expect(Header("acd-default")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Text("default-content")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Keyboard_TabThenEnterAndSpace_ToggleThePanel()
    {
        await OpenAsync(Url);

        await Header("acd-default").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Header("acd-keys")).ToBeFocusedAsync();

        // The focus ring of a header reached by the keyboard.
        await Expect(Header("acd-keys")).ToHaveCSSAsync("outline-style", "solid");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Header("acd-keys")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Text("keys-content")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync(" ");
        await Expect(Header("acd-keys")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Text("keys-content")).ToBeHiddenAsync();
        await Expect(Header("acd-keys")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Hover_PaintsTheHeaderLine_ButNotOfAReadOnlyAccordion()
    {
        await OpenAsync(Url);

        var line = Page.Locator("#acd-basic > .bit-acd-hwr");
        await Expect(line).ToHaveCSSAsync("background-color", Transparent);

        await Header("acd-basic").HoverAsync();
        await Expect(line).ToHaveCSSAsync("background-color", new Regex(@"^(?!rgba\(0, 0, 0, 0\)).+"));

        await Header("acd-ro").HoverAsync(new() { Force = true });
        await NextFramesAsync();
        Assert.AreEqual(Transparent, await StyleAsync(Page.Locator("#acd-ro > .bit-acd-hwr"), "background-color"));
        await Expect(Header("acd-ro")).ToHaveCSSAsync("cursor", "default");
        await Expect(Header("acd-basic")).ToHaveCSSAsync("cursor", "pointer");
    }

    [TestMethod]
    public async Task ExpanderIcon_TheDefaultChevron_TurnsOverWhenExpanded()
    {
        await OpenAsync(Url);

        var icon = HeaderPart("acd-exp-default", "bit-acd-eic");
        var wrapper = HeaderPart("acd-exp-default", "bit-acd-eiw");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        await Expect(icon).ToHaveClassAsync(HasClass("bit-ico-r90"));
        await Expect(wrapper).Not.ToHaveClassAsync(HasClass("bit-ico--r180"));
        await Expect(wrapper).ToHaveCSSAsync("transform", "none");

        await Header("acd-exp-default").ClickAsync();

        await Expect(wrapper).ToHaveClassAsync(HasClass("bit-ico--r180"));
        await Expect(wrapper).ToHaveCSSAsync("transform", new Regex(@"^matrix\(-1, "));
    }

    [TestMethod]
    public async Task ExpanderIconName_ReplacesTheChevron()
    {
        await OpenAsync(Url);

        var icon = HeaderPart("acd-exp-name", "bit-acd-eic");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--ChevronDown"));
        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
    }

    [TestMethod]
    public async Task ExpanderIconPlacement_Start_PutsTheExpanderBeforeTheIconAndTheTitle()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-exp-start")).ToHaveClassAsync(HasClass("bit-acd-sei"));

        var expander = (await HeaderPart("acd-exp-start", "bit-acd-eiw").BoundingBoxAsync())!;
        var icon = (await HeaderPart("acd-exp-start", "bit-acd-ico").BoundingBoxAsync())!;
        var title = (await HeaderPart("acd-exp-start", "bit-acd-ttl").BoundingBoxAsync())!;
        Assert.IsTrue(expander.X < icon.X && icon.X < title.X, $"expander {expander.X}, icon {icon.X}, title {title.X}");

        // At the end by default.
        var endExpander = (await HeaderPart("acd-exp-name", "bit-acd-eiw").BoundingBoxAsync())!;
        var endTitle = (await HeaderPart("acd-exp-name", "bit-acd-ttl").BoundingBoxAsync())!;
        Assert.IsTrue(endExpander.X > endTitle.X + endTitle.Width);
    }

    [TestMethod]
    public async Task HideExpanderIcon_RendersNoExpander()
    {
        await OpenAsync(Url);

        await Expect(HeaderPart("acd-exp-hide", "bit-acd-eiw")).ToHaveCountAsync(0);

        await Header("acd-exp-hide").ClickAsync();
        await Expect(Header("acd-exp-hide")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(HeaderPart("acd-exp-hide", "bit-acd-eiw")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task NoExpanderRotation_KeepsTheExpanderStill()
    {
        await OpenAsync(Url);

        await Header("acd-exp-norot").ClickAsync();

        await Expect(Header("acd-exp-norot")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(HeaderPart("acd-exp-norot", "bit-acd-eiw")).Not.ToHaveClassAsync(HasClass("bit-ico--r180"));
        await Expect(HeaderPart("acd-exp-norot", "bit-acd-eiw")).ToHaveCSSAsync("transform", "none");
    }

    [TestMethod]
    public async Task ExpandedExpanderIconName_SwapsTheIconWhileOpen_AndStopsTheRotation()
    {
        await OpenAsync(Url);

        var icon = HeaderPart("acd-exp-swap", "bit-acd-eic");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Add"));

        await Header("acd-exp-swap").ClickAsync();
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Remove"));
        await Expect(HeaderPart("acd-exp-swap", "bit-acd-eiw")).Not.ToHaveClassAsync(HasClass("bit-ico--r180"));

        await Header("acd-exp-swap").ClickAsync();
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Add"));
    }

    [TestMethod]
    public async Task Actions_ClickAnAction_RunsItWithoutTogglingTheAccordion()
    {
        await OpenAsync(Url);

        // Beside the toggle and its heading, never inside them.
        await Expect(Page.Locator("#acd-actions > .bit-acd-hwr > .bit-acd-act #action-rename")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#acd-actions .bit-acd-hdr #action-rename")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#acd-actions-cnt #actions-body")).ToHaveCountAsync(1);

        await Page.Locator("#action-rename").ClickAsync();

        await Expect(Text("rename-count")).ToHaveTextAsync("1");
        await Expect(HeaderPart("acd-actions", "bit-acd-des")).ToHaveTextAsync("Renamed 1 times");
        await Expect(Header("acd-actions")).ToHaveAttributeAsync("aria-expanded", "false");

        await Header("acd-actions").ClickAsync();
        await Expect(Text("actions-body")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Binding_TwoWay_TheUserAndThePageDriveTheSameState()
    {
        await OpenAsync(Url);

        await Header("acd-bound").ClickAsync();
        await Expect(Text("bound-value")).ToHaveTextAsync("True");
        await Expect(Root("acd-bound")).ToHaveClassAsync(HasClass("bit-acd-exp"));

        await Page.Locator("#btn-bound-toggle").ClickAsync();
        await Expect(Text("bound-value")).ToHaveTextAsync("False");
        await Expect(Header("acd-bound")).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.Locator("#btn-bound-toggle").ClickAsync();
        await Expect(Header("acd-bound")).ToHaveAttributeAsync("aria-expanded", "true");

        await Header("acd-bound").ClickAsync();
        await Expect(Text("bound-value")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Binding_Disabled_IgnoresTheHeaderWhileTheBindingStillDrivesIt()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-bound-disable").ClickAsync();
        await Expect(Root("acd-bound")).ToHaveClassAsync(HasClass("bit-dis"));

        await Header("acd-bound").ClickAsync(new() { Force = true });
        await Header("acd-bound").DispatchEventAsync("click");
        await NextFramesAsync();
        await Expect(Text("bound-value")).ToHaveTextAsync("False");
        await Expect(Header("acd-bound")).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.Locator("#btn-bound-toggle").ClickAsync();
        await Expect(Header("acd-bound")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Root("acd-bound")).ToHaveClassAsync(HasClass("bit-acd-exp"));
    }

    [TestMethod]
    public async Task Binding_Controlled_KeepsOnePanelOpenAtATime()
    {
        await OpenAsync(Url);

        await Header("acd-ctl-1").ClickAsync();
        await Expect(Text("ctl-value")).ToHaveTextAsync("1");
        await Expect(Header("acd-ctl-1")).ToHaveAttributeAsync("aria-expanded", "true");

        await Header("acd-ctl-2").ClickAsync();
        await Expect(Text("ctl-value")).ToHaveTextAsync("2");
        await Expect(Header("acd-ctl-2")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Header("acd-ctl-1")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Header("acd-ctl-3")).ToHaveAttributeAsync("aria-expanded", "false");

        await Header("acd-ctl-2").ClickAsync();
        await Expect(Text("ctl-value")).ToHaveTextAsync("0");
        await Expect(Header("acd-ctl-2")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Binding_OneWayWithNoHandler_TheHeaderNeverMovesIt()
    {
        await OpenAsync(Url);

        await Header("acd-fixed").ClickAsync();
        await Header("acd-fixed").ClickAsync();

        await Expect(Text("fixed-clicks")).ToHaveTextAsync("2");
        await Expect(Text("fixed-changes")).ToHaveTextAsync("0");
        await Expect(Header("acd-fixed")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Events_ClickToCollapseAndExpand_FireEveryCallbackOnceWithItsArguments()
    {
        await OpenAsync(Url);

        await Header("acd-events").ClickAsync();

        await Expect(Text("ev-click")).ToHaveTextAsync("1");
        await Expect(Text("ev-click-button")).ToHaveTextAsync("0");
        await Expect(Text("ev-toggling")).ToHaveTextAsync("1");
        await Expect(Text("ev-last-expanding")).ToHaveTextAsync("False");
        await Expect(Text("ev-last-reason")).ToHaveTextAsync("Click");
        await Expect(Text("ev-change")).ToHaveTextAsync("1");
        await Expect(Text("ev-last-change")).ToHaveTextAsync("False");
        await Expect(Text("ev-collapse")).ToHaveTextAsync("1");
        await Expect(Text("ev-expand")).ToHaveTextAsync("0");

        await Header("acd-events").ClickAsync();

        await Expect(Text("ev-click")).ToHaveTextAsync("2");
        await Expect(Text("ev-toggling")).ToHaveTextAsync("2");
        await Expect(Text("ev-last-expanding")).ToHaveTextAsync("True");
        await Expect(Text("ev-change")).ToHaveTextAsync("2");
        await Expect(Text("ev-last-change")).ToHaveTextAsync("True");
        await Expect(Text("ev-expand")).ToHaveTextAsync("1");
        await Expect(Text("ev-collapse")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Events_OnTogglingCancel_LeavesTheAccordionAsItIs()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-lock").ClickAsync();
        await Expect(Text("ev-lock")).ToHaveTextAsync("True");

        await Header("acd-events").ClickAsync();

        await Expect(Text("ev-refused")).ToHaveTextAsync("1");
        await Expect(Text("ev-click")).ToHaveTextAsync("1");
        await Expect(Text("ev-change")).ToHaveTextAsync("0");
        await Expect(Text("ev-collapse")).ToHaveTextAsync("0");
        await Expect(Header("acd-events")).ToHaveAttributeAsync("aria-expanded", "true");

        await Page.Locator("#btn-lock").ClickAsync();
        await Header("acd-events").ClickAsync();
        await Expect(Header("acd-events")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Text("ev-collapse")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Events_AnAwaitedOnToggling_ShowsTheHeaderBusyAndIgnoresClicksMeanwhile()
    {
        await OpenAsync(Url);

        await Header("acd-async").ClickAsync();

        await Expect(Header("acd-async")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Header("acd-async")).ToHaveClassAsync(HasClass("bit-acd-bsy"));
        await Expect(Header("acd-async")).ToHaveCSSAsync("cursor", "progress");
        await Expect(HeaderPart("acd-async", "bit-acd-spn")).ToBeVisibleAsync();
        await Expect(HeaderPart("acd-async", "bit-acd-eic")).ToHaveCountAsync(0);
        await Expect(Text("async-toggling")).ToHaveTextAsync("1");

        await Header("acd-async").ClickAsync();
        await NextFramesAsync();
        await Expect(Text("async-toggling")).ToHaveTextAsync("1");
        await Expect(Header("acd-async")).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.Locator("#btn-release").ClickAsync();

        await Expect(Header("acd-async")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Header("acd-async")).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
        await Expect(HeaderPart("acd-async", "bit-acd-spn")).ToHaveCountAsync(0);
        await Expect(Text("async-changes")).ToHaveTextAsync("1");
        await Expect(Text("async-content")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Methods_ExpandCollapseAndToggle_ReportLikeAClickWithTheMethodReason()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-expand").ClickAsync();
        await Expect(Header("acd-methods")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Text("methods-changes")).ToHaveTextAsync("1");
        await Expect(Text("methods-last")).ToHaveTextAsync("True");
        await Expect(Text("methods-expands")).ToHaveTextAsync("1");
        await Expect(Text("methods-reason")).ToHaveTextAsync("Method");

        // Expanding an open accordion reports nothing.
        await Page.Locator("#btn-expand").ClickAsync();
        await NextFramesAsync();
        await Expect(Text("methods-changes")).ToHaveTextAsync("1");

        await Page.Locator("#btn-toggle").ClickAsync();
        await Expect(Header("acd-methods")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Text("methods-collapses")).ToHaveTextAsync("1");
        await Expect(Text("methods-last")).ToHaveTextAsync("False");

        await Page.Locator("#btn-collapse").ClickAsync();
        await NextFramesAsync();
        await Expect(Text("methods-changes")).ToHaveTextAsync("2");

        await Page.Locator("#btn-toggle").ClickAsync();
        await Expect(Header("acd-methods")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Text("methods-changes")).ToHaveTextAsync("3");

        await Page.Locator("#btn-collapse").ClickAsync();
        await Expect(Header("acd-methods")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Text("methods-collapses")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Methods_FocusAsync_MovesTheKeyboardToTheHeader()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-expand-focus").ClickAsync();

        await Expect(Header("acd-methods")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Header("acd-methods")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Methods_ACollapseWhileTheFocusIsInThePanel_HandsTheFocusBackToTheHeader()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-expand").ClickAsync();
        await Expect(Page.Locator("#btn-inner-collapse")).ToBeVisibleAsync();

        await Page.Locator("#btn-inner-collapse").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Header("acd-methods")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Header("acd-methods")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Methods_DriveADisabledAccordion()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-dis-expand").ClickAsync();

        await Expect(Header("acd-methods-disabled")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Root("acd-methods-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
    }

    [TestMethod]
    public async Task LazyContent_RendersTheContentOnTheFirstExpand_AndKeepsIt()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#lazy-input")).ToHaveCountAsync(0);

        await Header("acd-lazy").ClickAsync();
        await Expect(Page.Locator("#lazy-input")).ToBeVisibleAsync();
        await Page.Locator("#lazy-input").FillAsync("kept");

        await Header("acd-lazy").ClickAsync();
        await Expect(Page.Locator("#lazy-input")).ToBeHiddenAsync();
        await Expect(Page.Locator("#lazy-input")).ToHaveCountAsync(1);

        await Header("acd-lazy").ClickAsync();
        await Expect(Page.Locator("#lazy-input")).ToHaveValueAsync("kept");
    }

    [TestMethod]
    public async Task UnmountOnCollapse_DropsTheContentOnEveryCollapse()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#unmount-input")).ToHaveCountAsync(0);

        await Header("acd-unmount").ClickAsync();
        await Expect(Page.Locator("#unmount-input")).ToBeVisibleAsync();
        await Page.Locator("#unmount-input").FillAsync("thrown away");

        await Header("acd-unmount").ClickAsync();
        await Expect(Page.Locator("#unmount-input")).ToHaveCountAsync(0);

        await Header("acd-unmount").ClickAsync();
        await Expect(Page.Locator("#unmount-input")).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task OnToggling_LoadsTheContentBeforeTheFirstOpen_WhileTheHeaderIsBusy()
    {
        await OpenAsync(Url);

        await Header("acd-load").ClickAsync();

        await Expect(Header("acd-load")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Header("acd-load")).ToHaveAttributeAsync("aria-expanded", "false");

        await Expect(Page.Locator("#acd-load .e2e-order")).ToHaveCountAsync(3);
        await Expect(Header("acd-load")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Header("acd-load")).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
        await Expect(Page.Locator("#acd-load .e2e-order").First).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Busy_ShowsTheSpinnerAndAnswersNoClickToggle()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-busy").ClickAsync();

        await Expect(Header("acd-busy")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(HeaderPart("acd-busy", "bit-acd-spn")).ToBeVisibleAsync();
        await Expect(HeaderPart("acd-busy", "bit-acd-spn")).ToHaveCSSAsync("animation-name", "bit-spin");

        await Header("acd-busy").ClickAsync();
        await Expect(Text("busy-clicks")).ToHaveTextAsync("1");
        await Expect(Text("busy-changes")).ToHaveTextAsync("0");
        await Expect(Header("acd-busy")).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.Locator("#btn-busy").ClickAsync();
        await Expect(HeaderPart("acd-busy", "bit-acd-spn")).ToHaveCountAsync(0);
        await Expect(HeaderPart("acd-busy", "bit-acd-eic")).ToHaveCountAsync(1);

        await Header("acd-busy").ClickAsync();
        await Expect(Header("acd-busy")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Text("busy-changes")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task MaxHeight_CapsTheOpenPanel_WhichScrollsAndTakesATabStop()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-maxh")).ToHaveClassAsync(HasClass("bit-acd-mxh"));
        await Expect(Content("acd-maxh")).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));

        await Header("acd-maxh").ClickAsync();

        await Expect(Content("acd-maxh")).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Content("acd-maxh")).ToHaveCSSAsync("max-height", "64px");
        await Expect(Content("acd-maxh")).ToHaveCSSAsync("overflow-y", "auto");
        // The cap is on the content box; the panel's padding comes on top of it once the opening has played.
        await PollAsync(() => Content("acd-maxh").EvaluateAsync<double>(@"el => {
            if (el.getAnimations().length > 0 || el.parentElement.parentElement.getAnimations().length > 0) return -1;
            const s = getComputedStyle(el);
            return el.clientHeight - parseFloat(s.paddingTop) - parseFloat(s.paddingBottom);
        }"), h => Math.Abs(h - 64) < 1, "the panel's content is not capped at 64px");

        var overflows = await Content("acd-maxh").EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight");
        Assert.IsTrue(overflows);

        await Content("acd-maxh").FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await PollAsync(() => Content("acd-maxh").EvaluateAsync<double>("el => el.scrollTop"), top => top > 0, "the keyboard does not scroll the panel");
    }

    [TestMethod]
    public async Task TransitionDuration_ASlowPanel_GrowsThroughIntermediateHeightsToTheContentHeight()
    {
        await OpenAsync(Url);

        Assert.AreEqual(1000, ToMs(await StyleAsync(Row("acd-slow"), "transition-duration")), 1);

        await StartHeightSamplerAsync("acd-slow");
        await Header("acd-slow").ClickAsync();
        await WaitForFullHeightAsync("acd-slow");

        var samples = await ReadHeightSamplesAsync();
        var full = await Content("acd-slow").EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        var intermediate = samples.Count(h => h > 1 && h < full - 1);
        Assert.IsTrue(intermediate >= 5, $"Only {intermediate} intermediate heights in [{string.Join(", ", samples)}], full {full}.");
    }

    [TestMethod]
    public async Task TransitionDuration_Zero_OpensThePanelAtOnce()
    {
        await OpenAsync(Url);

        Assert.AreEqual(0, ToMs(await StyleAsync(Row("acd-instant"), "transition-duration")), 0.001);

        await StartHeightSamplerAsync("acd-instant");
        await Header("acd-instant").ClickAsync();
        await WaitForFullHeightAsync("acd-instant");

        var samples = await ReadHeightSamplesAsync();
        var full = await Content("acd-instant").EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        var intermediate = samples.Count(h => h > 1 && h < full - 1);
        Assert.AreEqual(0, intermediate, $"[{string.Join(", ", samples)}], full {full}");
    }

    [TestMethod]
    public async Task Transition_TheThemeDuration_PlaysAndFinishesOnTheContentHeight()
    {
        await OpenAsync(Url);

        Assert.AreEqual(300, ToMs(await StyleAsync(Row("acd-theme-dur"), "transition-duration")), 1);

        await Header("acd-theme-dur").ClickAsync();
        await WaitForFullHeightAsync("acd-theme-dur");
        await Expect(Content("acd-theme-dur")).ToHaveCSSAsync("opacity", "1");

        await Header("acd-theme-dur").ClickAsync();
        await PollAsync(async () => (await Row("acd-theme-dur").BoundingBoxAsync())!.Height, h => h < 0.5, "the row never closes");
        await Expect(Content("acd-theme-dur")).ToHaveCSSAsync("visibility", "hidden");
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesEveryDuration_UnlessForceAnimation()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(Url);

        Assert.IsTrue(ToMs(await StyleAsync(Row("acd-theme-dur"), "transition-duration")) < 0.1);
        Assert.IsTrue(ToMs(await StyleAsync(Row("acd-slow"), "transition-duration")) < 0.1, "A TransitionDuration does not follow reduced motion.");
        Assert.AreEqual(300, ToMs(await StyleAsync(Row("acd-force"), "transition-duration")), 1);
        await Expect(Root("acd-force")).ToHaveClassAsync(HasClass("bit-fam"));

        await StartHeightSamplerAsync("acd-slow");
        await Header("acd-slow").ClickAsync();
        await WaitForFullHeightAsync("acd-slow");
        var samples = await ReadHeightSamplesAsync();
        var full = await Content("acd-slow").EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        Assert.AreEqual(0, samples.Count(h => h > 1 && h < full - 1), $"[{string.Join(", ", samples)}]");
    }

    [TestMethod]
    public async Task HiddenUntilFound_AnAnchorJump_RevealsTheMatchAndExpandsTheAccordion()
    {
        await OpenAsync(Url);
        await SkipWithoutHiddenUntilFoundAsync();

        await Expect(Content("acd-find")).ToHaveAttributeAsync("hidden", "until-found");
        await Expect(Content("acd-find")).ToHaveClassAsync(HasClass("bit-acd-huf"));
        await Expect(Text("find-target")).ToBeHiddenAsync();

        await Page.Locator("#link-find").ClickAsync();

        await Expect(Text("find-reason")).ToHaveTextAsync("Reveal");
        await Expect(Header("acd-find")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Content("acd-find")).Not.ToHaveAttributeAsync("hidden", new Regex(".*"));
        await Expect(Text("find-target")).ToBeVisibleAsync();
        await Expect(Text("find-target")).ToBeInViewportAsync();

        // Closing it again hands it back to find-in-page once the close has played.
        await Header("acd-find").ClickAsync();
        await Expect(Header("acd-find")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Content("acd-find")).ToHaveAttributeAsync("hidden", "until-found");
    }

    [TestMethod]
    public async Task HiddenUntilFound_ARevealOnTogglingRefuses_LeavesThePanelShut()
    {
        await OpenAsync(Url);
        await SkipWithoutHiddenUntilFoundAsync();

        await Expect(Content("acd-find-refuse")).ToHaveAttributeAsync("hidden", "until-found");

        await Page.EvaluateAsync("() => { location.hash = 'find-refused-target'; }");

        await Expect(Text("find-refused")).ToHaveTextAsync("1");
        await Expect(Header("acd-find-refuse")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Content("acd-find-refuse")).Not.ToHaveAttributeAsync("hidden", new Regex(".*"));
        await Expect(Content("acd-find-refuse")).Not.ToHaveClassAsync(HasClass("bit-acd-huf"));
        await Expect(Text("find-refused-target")).ToBeHiddenAsync();

        // A click still opens it, and the state change offers it to find-in-page again once it closes.
        await Header("acd-find-refuse").ClickAsync();
        await Expect(Header("acd-find-refuse")).ToHaveAttributeAsync("aria-expanded", "true");
        await Header("acd-find-refuse").ClickAsync();
        await Expect(Content("acd-find-refuse")).ToHaveAttributeAsync("hidden", "until-found");
    }

    [TestMethod]
    public async Task HiddenUntilFound_ADisabledAccordion_IsNotOfferedToFindInPage()
    {
        await OpenAsync(Url);

        await Expect(Content("acd-find-disabled")).Not.ToHaveAttributeAsync("hidden", new Regex(".*"));
        await Expect(Content("acd-find-disabled")).ToHaveCSSAsync("visibility", "hidden");
    }

    [TestMethod]
    public async Task ExpandOnPrint_PrintMedia_OpensThePanelAndLiftsMaxHeight()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-print")).ToHaveClassAsync(HasClass("bit-acd-eop"));
        await Expect(Content("acd-print")).ToHaveCSSAsync("visibility", "hidden");

        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await Expect(Content("acd-print")).ToHaveCSSAsync("visibility", "visible");
        await Expect(Content("acd-print")).ToHaveCSSAsync("opacity", "1");
        await Expect(Content("acd-print")).ToHaveCSSAsync("max-height", "none");
        await PollAsync(async () => (await Row("acd-print").BoundingBoxAsync())!.Height, h => h > 64, "the printed panel is not open in full");

        await Expect(Content("acd-noprint")).ToHaveCSSAsync("visibility", "hidden");

        await Page.EmulateMediaAsync(new() { Media = Media.Screen });
        await Expect(Content("acd-print")).ToHaveCSSAsync("visibility", "hidden");
    }

    [TestMethod]
    public async Task TitleTemplate_ReplacesTheTitleTextOnly()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#acd-title-tpl-ttl #title-tpl")).ToHaveTextAsync("Advanced settings");
        await Expect(HeaderPart("acd-title-tpl", "bit-acd-des")).ToHaveTextAsync("TitleTemplate");
        await Expect(HeaderPart("acd-title-tpl", "bit-acd-ico")).ToHaveCountAsync(1);
        await Expect(HeaderPart("acd-title-tpl", "bit-acd-eiw")).ToHaveCountAsync(1);
        await Expect(Header("acd-title-tpl")).ToHaveAttributeAsync("aria-labelledby", "acd-title-tpl-ttl");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Advanced settings", Exact = true })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task ExpanderTemplate_ReceivesTheExpandedState_AndIsStillTurnedOver()
    {
        await OpenAsync(Url);

        var template = Page.Locator("#acd-exp-tpl .bit-acd-eiw .e2e-exp-tpl");
        await Expect(template).ToHaveTextAsync("More");
        await Expect(HeaderPart("acd-exp-tpl", "bit-acd-eic")).ToHaveCountAsync(0);

        await Header("acd-exp-tpl").ClickAsync();

        await Expect(template).ToHaveTextAsync("Less");
        await Expect(HeaderPart("acd-exp-tpl", "bit-acd-eiw")).ToHaveClassAsync(HasClass("bit-ico--r180"));
    }

    [TestMethod]
    public async Task HeaderTemplate_ReplacesTheWholeHeader_AndReceivesTheState()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#acd-hdr-tpl-hdr #hdr-tpl-state")).ToHaveTextAsync("Closed");
        await Expect(HeaderPart("acd-hdr-tpl", "bit-acd-ttl")).ToHaveCountAsync(0);
        await Expect(HeaderPart("acd-hdr-tpl", "bit-acd-eiw")).ToHaveCountAsync(0);
        await Expect(Header("acd-hdr-tpl")).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".*"));

        await Header("acd-hdr-tpl").ClickAsync();

        await Expect(Text("hdr-tpl-state")).ToHaveTextAsync("Open");
        await Expect(Header("acd-hdr-tpl")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [TestMethod]
    public async Task ReadOnly_ReportsTheClick_KeepsItsTabStop_AndStaysAsItIs()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-ro")).ToHaveClassAsync(HasClass("bit-acd-rdo"));
        await Expect(Root("acd-ro")).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Header("acd-ro")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Header("acd-ro")).ToHaveAttributeAsync("tabindex", "0");

        await Header("acd-ro").ClickAsync(new() { Force = true });
        await Expect(Text("ro-clicks")).ToHaveTextAsync("1");
        await Expect(Text("ro-changes")).ToHaveTextAsync("0");
        await Expect(Header("acd-ro")).ToHaveAttributeAsync("aria-expanded", "true");

        await Header("acd-ro").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("ro-clicks")).ToHaveTextAsync("2");
        await Expect(Header("acd-ro")).ToHaveAttributeAsync("aria-expanded", "true");

        // Keeps the colors of a live accordion.
        Assert.AreEqual(await StyleAsync(HeaderPart("acd-basic", "bit-acd-ttl"), "color"), await StyleAsync(HeaderPart("acd-ro", "bit-acd-ttl"), "color"));
    }

    [TestMethod]
    public async Task Disabled_TheHeaderAnswersNothing_WhileThePanelStaysLive()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Header("acd-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Header("acd-disabled")).ToHaveAttributeAsync("tabindex", "-1");
        Assert.AreNotEqual(await StyleAsync(HeaderPart("acd-basic", "bit-acd-ttl"), "color"), await StyleAsync(HeaderPart("acd-disabled", "bit-acd-ttl"), "color"));
        Assert.AreNotEqual(await StyleAsync(Root("acd-basic"), "background-color"), await StyleAsync(Root("acd-disabled"), "background-color"));

        await Header("acd-disabled").ClickAsync(new() { Force = true });
        await Header("acd-disabled-closed").ClickAsync(new() { Force = true });
        await NextFramesAsync();
        await Expect(Text("disabled-clicks")).ToHaveTextAsync("0");
        await Expect(Text("disabled-closed-clicks")).ToHaveTextAsync("0");
        await Expect(Header("acd-disabled")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Header("acd-disabled-closed")).ToHaveAttributeAsync("aria-expanded", "false");

        // The open panel of a disabled accordion is still usable.
        await Page.Locator("#btn-disabled-inner").ClickAsync();
        await Expect(Text("disabled-inner")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Accessibility_TheHeadingTheButtonAndTheRegion_AreWiredTogether()
    {
        await OpenAsync(Url);

        await Expect(HeaderPart("acd-basic", "bit-acd-hed")).ToHaveAttributeAsync("role", "heading");
        await Expect(HeaderPart("acd-basic", "bit-acd-hed")).ToHaveAttributeAsync("aria-level", "3");
        await Expect(Header("acd-basic")).ToHaveAttributeAsync("type", "button");
        await Expect(Header("acd-basic")).ToHaveAttributeAsync("aria-controls", "acd-basic-cnt");
        await Expect(Header("acd-basic")).ToHaveAttributeAsync("aria-labelledby", "acd-basic-ttl");
        await Expect(Header("acd-basic")).ToHaveAttributeAsync("aria-describedby", "acd-basic-des");
        await Expect(Content("acd-basic")).ToHaveAttributeAsync("role", "region");
        await Expect(Content("acd-basic")).ToHaveAttributeAsync("aria-labelledby", "acd-basic-ttl");
        await Expect(Root("acd-basic")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Basic title", Exact = true })).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Top-level section", Level = 2 })).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Top-level section", Exact = true })).ToHaveAccessibleDescriptionAsync("aria-level 2");

        await Header("acd-basic").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Region, new() { Name = "Basic title" })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Accessibility_NestedAccordions_GoOneLevelDeeper_AndTheLevelIsClamped()
    {
        await OpenAsync(Url);

        await Expect(HeaderPart("acd-outer", "bit-acd-hed").First).ToHaveAttributeAsync("aria-level", "3");
        await Expect(Page.Locator("#acd-inner > .bit-acd-hwr .bit-acd-hed")).ToHaveAttributeAsync("aria-level", "4");
        await Expect(Page.Locator("#acd-inner-inner > .bit-acd-hwr .bit-acd-hed")).ToHaveAttributeAsync("aria-level", "5");
        await Expect(HeaderPart("acd-clamp", "bit-acd-hed")).ToHaveAttributeAsync("aria-level", "6");

        // NoContentRegion drops the landmark and its name together.
        await Expect(Content("acd-outer")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(Content("acd-outer")).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".*"));
        await Expect(Content("acd-inner")).ToHaveAttributeAsync("role", "region");

        // The outer one's state does not leak into the nested one.
        await Expect(Root("acd-inner")).Not.ToHaveClassAsync(HasClass("bit-acd-exp"));
        await Expect(Text("acd-inner-inner")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Accessibility_HeaderAriaLabelAndAriaLabel_NameTheHeaderAndTheGroup()
    {
        await OpenAsync(Url);

        await Expect(Header("acd-icon-only")).ToHaveAttributeAsync("aria-label", "Notifications");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Notifications", Exact = true })).ToHaveCountAsync(1);
        await Expect(Content("acd-icon-only")).ToHaveAttributeAsync("aria-labelledby", "acd-icon-only-hdr");

        await Expect(Root("acd-group")).ToHaveAttributeAsync("role", "group");
        await Expect(Root("acd-group")).ToHaveAttributeAsync("aria-label", "Frequently asked");
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Frequently asked" })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Accessibility_ACollapsedPanel_IsOutOfTheTabOrder()
    {
        await OpenAsync(Url);

        await Header("acd-lvl2").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#a11y-after")).ToBeFocusedAsync();

        await Header("acd-lvl2").ClickAsync();
        await Expect(Page.Locator("#a11y-link")).ToBeVisibleAsync();
        await Header("acd-lvl2").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#a11y-link")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task BitParams_FillsTheUnsetParameters_AndTheAccordionsOwnValuesWin()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-par-1")).ToHaveClassAsync(HasClass("bit-acd-sei"));
        await Expect(Root("acd-par-1")).ToHaveClassAsync(HasClass("bit-acd-lg"));
        await Expect(Root("acd-par-1")).ToHaveClassAsync(HasClass("bit-acd-tbg"));
        await Expect(HeaderPart("acd-par-1", "bit-acd-eic")).ToHaveClassAsync(HasClass("bit-icon--Add"));
        await Expect(HeaderPart("acd-par-1", "bit-acd-hed")).ToHaveAttributeAsync("aria-level", "5");
        Assert.AreEqual(await StyleAsync(Root("acd-bg-tertiary"), "background-color"), await StyleAsync(Root("acd-par-1"), "background-color"));

        await Expect(Root("acd-par-own")).Not.ToHaveClassAsync(HasClass("bit-acd-sei"));
        await Expect(Root("acd-par-own")).ToHaveClassAsync(HasClass("bit-acd-sm"));
        await Expect(Root("acd-par-own")).ToHaveClassAsync(HasClass("bit-acd-tbg"));

        await Header("acd-par-1").ClickAsync();
        await Expect(HeaderPart("acd-par-1", "bit-acd-eic")).ToHaveClassAsync(HasClass("bit-icon--Remove"));
    }

    [TestMethod]
    public async Task Background_EveryColorKind_PaintsItsOwnFill()
    {
        await OpenAsync(Url);

        var expected = new (string Id, string Class)[]
        {
            ("acd-bg-primary", "bit-acd-pbg"),
            ("acd-bg-secondary", "bit-acd-sbg"),
            ("acd-bg-tertiary", "bit-acd-tbg"),
            ("acd-bg-transparent", "bit-acd-rbg"),
        };

        var fills = new List<string>();
        foreach (var (id, cssClass) in expected)
        {
            await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));
            fills.Add(await StyleAsync(Root(id), "background-color"));
        }

        Assert.AreEqual(4, fills.Distinct().Count(), string.Join(" | ", fills));
        Assert.AreEqual(Transparent, fills[3]);

        // Unset publishes no class and takes the primary fill.
        await Expect(Root("acd-bg-unset")).Not.ToHaveClassAsync(new Regex(@"bit-acd-[pstr]bg"));
        Assert.AreEqual(fills[0], await StyleAsync(Root("acd-bg-unset"), "background-color"));
    }

    [TestMethod]
    public async Task Border_EveryColorKind_PaintsItsOwnOutline()
    {
        await OpenAsync(Url);

        var expected = new (string Id, string Class)[]
        {
            ("acd-br-primary", "bit-acd-pbr"),
            ("acd-br-secondary", "bit-acd-sbr"),
            ("acd-br-tertiary", "bit-acd-tbr"),
            ("acd-br-transparent", "bit-acd-rbr"),
        };

        var outlines = new List<string>();
        foreach (var (id, cssClass) in expected)
        {
            await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));
            outlines.Add(await StyleAsync(Root(id), "border-top-color"));
        }

        Assert.AreEqual(4, outlines.Distinct().Count(), string.Join(" | ", outlines));
        Assert.AreEqual(Transparent, outlines[3]);
        Assert.AreEqual(outlines[0], await StyleAsync(Root("acd-basic"), "border-top-color"));
    }

    [TestMethod]
    public async Task NoBorder_DropsTheOutline_AndFillsWithTheSecondaryBackground()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-noborder")).ToHaveClassAsync(HasClass("bit-acd-nbd"));
        await Expect(Root("acd-noborder")).ToHaveCSSAsync("border-top-style", "none");
        await Expect(Root("acd-noborder")).ToHaveCSSAsync("border-top-width", "0px");
        Assert.AreEqual(await StyleAsync(Root("acd-bg-secondary"), "background-color"), await StyleAsync(Root("acd-noborder"), "background-color"));
    }

    [TestMethod]
    public async Task ExternalIcons_RenderTheGivenClasses_AndIconWinsOverIconName()
    {
        await OpenAsync(Url);

        var icon = HeaderPart("acd-ext", "bit-acd-ico");
        await Expect(icon).ToHaveClassAsync(HasClass("e2e-gear"));
        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-icon"));
        await Expect(HeaderPart("acd-ext", "bit-acd-eic")).ToHaveClassAsync(HasClass("e2e-plus"));

        await Header("acd-ext").ClickAsync();
        await Expect(HeaderPart("acd-ext", "bit-acd-eic")).ToHaveClassAsync(HasClass("e2e-minus"));
        await Expect(HeaderPart("acd-ext", "bit-acd-eiw")).Not.ToHaveClassAsync(HasClass("bit-ico--r180"));

        await Expect(HeaderPart("acd-ext-bi", "bit-acd-ico")).ToHaveClassAsync(HasClass("bi-gear"));
        await Expect(HeaderPart("acd-ext-bi", "bit-acd-ico")).ToHaveClassAsync(HasClass("bi"));
        await Expect(HeaderPart("acd-ext-bi", "bit-acd-eic")).ToHaveClassAsync(HasClass("fa-chevron-down"));
        await Expect(HeaderPart("acd-ext-bi", "bit-acd-eic")).ToHaveClassAsync(HasClass("fa-solid"));

        await Expect(HeaderPart("acd-ext-wins", "bit-acd-ico")).ToHaveClassAsync(HasClass("e2e-wins"));
        await Expect(HeaderPart("acd-ext-wins", "bit-acd-ico")).Not.ToHaveClassAsync(HasClass("bit-icon--Settings"));
    }

    [TestMethod]
    public async Task Size_EverySize_ScalesThePaddingAndTheType()
    {
        await OpenAsync(Url);

        var ids = new[] { ("acd-sm", "bit-acd-sm"), ("acd-md", "bit-acd-md"), ("acd-lg", "bit-acd-lg") };
        var paddings = new List<double>();
        var titles = new List<double>();
        var icons = new List<double>();

        foreach (var (id, cssClass) in ids)
        {
            await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));
            paddings.Add(Px(await StyleAsync(Header(id), "padding-top")));
            titles.Add(Px(await StyleAsync(HeaderPart(id, "bit-acd-ttl"), "font-size")));
            icons.Add(Px(await StyleAsync(HeaderPart(id, "bit-acd-ico"), "font-size")));
        }

        Assert.IsTrue(paddings[0] < paddings[1] && paddings[1] < paddings[2], string.Join(", ", paddings));
        Assert.IsTrue(titles[0] < titles[1] && titles[1] < titles[2], string.Join(", ", titles));
        Assert.IsTrue(icons[0] <= icons[1] && icons[1] <= icons[2] && icons[0] < icons[2], string.Join(", ", icons));
    }

    [TestMethod]
    public async Task StyleAndClass_ApplyToTheRoot()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-style")).ToHaveCSSAsync("margin-top", "7px");
        await Expect(Root("acd-class")).ToHaveClassAsync(HasClass("e2e-acd-class"));
        await Expect(Root("acd-class")).ToHaveClassAsync(HasClass("bit-acd"));
        await Expect(Root("acd-class")).ToHaveCSSAsync("outline-style", "dashed");
    }

    [TestMethod]
    public async Task Styles_ApplyToTheirParts_AndTheExpandedOnesOnlyWhileOpen()
    {
        await OpenAsync(Url);

        await Expect(HeaderPart("acd-styles", "bit-acd-ttl")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");
        await Expect(HeaderPart("acd-styles", "bit-acd-des")).ToHaveCSSAsync("font-style", "italic");
        await Expect(HeaderPart("acd-styles", "bit-acd-eic")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");
        Assert.AreNotEqual("rgb(46, 139, 87)", await StyleAsync(Root("acd-styles"), "border-top-color"));

        await Header("acd-styles").ClickAsync();

        await Expect(HeaderPart("acd-styles", "bit-acd-eic")).ToHaveCSSAsync("color", "rgb(46, 139, 87)");
        await Expect(Root("acd-styles")).ToHaveCSSAsync("border-top-color", "rgb(46, 139, 87)");
        await Expect(Content("acd-styles")).ToHaveCSSAsync("font-weight", "700");
    }

    [TestMethod]
    public async Task Classes_ApplyToTheirParts_AndTheExpandedOneOnlyWhileOpen()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-classes")).ToHaveClassAsync(HasClass("e2e-acd-root"));
        await Expect(Root("acd-classes")).Not.ToHaveClassAsync(HasClass("e2e-acd-expanded"));
        await Expect(HeaderPart("acd-classes", "bit-acd-ttl")).ToHaveClassAsync(HasClass("e2e-acd-title"));
        await Expect(HeaderPart("acd-classes", "bit-acd-ttl")).ToHaveCSSAsync("color", "rgb(10, 20, 30)");
        await Expect(Header("acd-classes")).ToHaveCSSAsync("text-decoration-line", "underline");
        await Expect(Content("acd-classes")).ToHaveClassAsync(HasClass("e2e-acd-content"));
        await Expect(Content("acd-classes")).ToHaveCSSAsync("letter-spacing", "3px");

        await Header("acd-classes").ClickAsync();
        await Expect(Root("acd-classes")).ToHaveClassAsync(HasClass("e2e-acd-expanded"));
    }

    [TestMethod]
    public async Task CssVariables_OnTheInstance_RestyleTheAccordion()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-var-inst")).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(Root("acd-var-inst")).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(Root("acd-var-inst")).ToHaveCSSAsync("border-top-color", "rgb(46, 139, 87)");

        await Header("acd-var-inst").ClickAsync();

        var line = Page.Locator("#acd-var-inst > .bit-acd-hwr");
        await Expect(line).ToHaveCSSAsync("background-color", "rgb(46, 139, 87)");
        await Expect(line).ToHaveCSSAsync("color", "rgb(255, 255, 255)");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_ReachTheAccordion_AndExplicitParametersWinOverThem()
    {
        await OpenAsync(Url);

        await Expect(HeaderPart("acd-var-anc", "bit-acd-ttl")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");
        await Expect(Root("acd-var-anc")).ToHaveCSSAsync("background-color", "rgb(250, 240, 230)");
        await Expect(Header("acd-var-anc")).ToHaveCSSAsync("padding-top", "21px");

        // An explicit Background and Size are a choice the inherited variables do not restyle.
        Assert.AreEqual(await StyleAsync(Root("acd-bg-tertiary"), "background-color"), await StyleAsync(Root("acd-var-wins"), "background-color"));
        Assert.AreEqual(await StyleAsync(Header("acd-lg"), "padding-top"), await StyleAsync(Header("acd-var-wins"), "padding-top"));
        await Expect(HeaderPart("acd-var-wins", "bit-acd-ttl")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");
    }

    [TestMethod]
    public async Task Rtl_DirRtl_MirrorsTheHeader()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root("acd-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await AssertMirroredAsync("acd-rtl");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_MirrorsTheHeader()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-rtl-inh")).Not.ToHaveAttributeAsync("dir", new Regex(".*"));
        await Expect(Root("acd-rtl-inh")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        await AssertMirroredAsync("acd-rtl-inh");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheBox_CollapsedRemovesIt()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-vis-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Root("acd-vis-hidden")).ToBeHiddenAsync();
        Assert.IsTrue((await Root("acd-vis-hidden").BoundingBoxAsync())!.Height > 0);

        await Expect(Root("acd-vis-collapsed")).ToHaveCSSAsync("display", "none");
        await Expect(Root("acd-vis-collapsed")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task HtmlAttributes_AreSplattedOnTheRoot()
    {
        await OpenAsync(Url);

        await Expect(Root("acd-attrs")).ToHaveAttributeAsync("data-e2e", "custom");
        await Expect(Root("acd-attrs")).ToHaveAttributeAsync("data-extra", "from-dictionary");
        await Expect(Root("acd-attrs")).ToHaveAttributeAsync("aria-labelledby", "attrs-label");
        await Expect(Root("acd-attrs")).ToHaveAttributeAsync("role", "group");
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Attrs label" })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Rerender_ParametersChangedByThePage_UpdateTheAccordion()
    {
        await OpenAsync(Url);

        await Expect(HeaderPart("acd-rr", "bit-acd-ttl")).ToHaveTextAsync("Before");
        await Expect(Root("acd-rr")).ToHaveClassAsync(HasClass("bit-acd-sm"));

        await Page.Locator("#btn-rr").ClickAsync();

        await Expect(HeaderPart("acd-rr", "bit-acd-ttl")).ToHaveTextAsync("After");
        await Expect(Root("acd-rr")).ToHaveClassAsync(HasClass("bit-acd-lg"));
        await Expect(Root("acd-rr")).Not.ToHaveClassAsync(HasClass("bit-acd-sm"));
        await Expect(Root("acd-rr")).ToHaveClassAsync(HasClass("bit-acd-exp"));
        await Expect(Root("acd-rr")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Header("acd-rr")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [TestMethod]
    public async Task Unmount_InTheMiddleOfAClose_RemovesTheAccordionWithoutAnError()
    {
        await OpenAsync(Url);

        await Header("acd-um").ClickAsync();
        await Expect(Header("acd-um")).ToHaveAttributeAsync("aria-expanded", "true");
        await Page.Locator("#um-input").FocusAsync();

        // Closing starts the wait for the transitions that ends in hidden="until-found"; unmounting cuts it short.
        await Header("acd-um").ClickAsync();
        await Page.Locator("#btn-unmount").ClickAsync();

        await Expect(Root("acd-um")).ToHaveCountAsync(0);
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Root("acd-um")).ToHaveCountAsync(1);
        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private async Task AssertMirroredAsync(string id)
    {
        var icon = (await HeaderPart(id, "bit-acd-ico").BoundingBoxAsync())!;
        var title = (await HeaderPart(id, "bit-acd-ttl").BoundingBoxAsync())!;
        var expander = (await HeaderPart(id, "bit-acd-eiw").BoundingBoxAsync())!;

        Assert.IsTrue(icon.X > title.X, $"The icon ({icon.X}) does not lead from the right of the title ({title.X}).");
        Assert.IsTrue(expander.X + expander.Width <= title.X, $"The expander ({expander.X}) does not close the header on the left ({title.X}).");
    }

    private async Task SkipWithoutHiddenUntilFoundAsync()
    {
        if (await Page.EvaluateAsync<bool>("() => 'onbeforematch' in document.body") is false)
        {
            Assert.Inconclusive($"{BrowserName} does not support hidden=\"until-found\" (no beforematch event).");
        }
    }

    private async Task WaitForFullHeightAsync(string id)
    {
        await PollAsync(async () =>
        {
            var row = (await Row(id).BoundingBoxAsync())!.Height;
            var content = await Content(id).EvaluateAsync<double>("el => el.getBoundingClientRect().height");
            return (row, content);
        }, s => s.content > 0 && Math.Abs(s.row - s.content) < 0.5, $"The row of {id} never reaches the height of its content.");
    }

    private Task StartHeightSamplerAsync(string id)
    {
        return Page.EvaluateAsync(@"id => {
            const row = document.querySelector('#' + id + ' > .bit-acd-cnt');
            window.__e2eHeights = [];
            const tick = () => {
                window.__e2eHeights.push(row.getBoundingClientRect().height);
                if (window.__e2eHeights.length < 600) requestAnimationFrame(tick);
            };
            requestAnimationFrame(tick);
        }", id);
    }

    private async Task<double[]> ReadHeightSamplesAsync()
    {
        return await Page.EvaluateAsync<double[]>("() => window.__e2eHeights.slice()");
    }

    private static Task<string> StyleAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>("(el, p) => getComputedStyle(el).getPropertyValue(p)", property);
    }

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    private static double ToMs(string value)
    {
        value = value.Split(',')[0].Trim();

        return value.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(value[..^2], CultureInfo.InvariantCulture)
            : double.Parse(value[..^1], CultureInfo.InvariantCulture) * 1000;
    }

    private async Task<T> PollAsync<T>(Func<Task<T>> read, Func<T, bool> done, string message)
    {
        var started = DateTime.UtcNow;
        T value;
        do
        {
            value = await read();
            if (done(value)) return value;
            await NextFramesAsync();
        } while (DateTime.UtcNow - started < TimeSpan.FromMilliseconds(DefaultTimeout));

        Assert.Fail($"{message} (last value: {value})");
        return value;
    }
}
