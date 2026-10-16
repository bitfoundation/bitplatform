using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// Drives BitSearchBox (/e2e/search-box) in a real browser: every section of its demo page and every parameter of its
/// table - typing and committing, read-only, disabled and required, label and descriptions, the variants and
/// backgrounds, the icon that collapses on focus, the search button and the busy state, the clear button, Escape
/// (inside a BitDialog too: the list, then the value, then the dialog), prefix and suffix, MaxLength, Trim and the
/// keyboard attributes, Immediate, DebounceTime and ThrottleTime with real typing, the events, the suggest callout
/// (positioned, filtered, highlighted, templated, empty, failed, loading, auto-selected, auto-filled, opened on focus,
/// announced, navigated with every key of the combobox pattern), the focus shortcut, the public methods, validation,
/// the BitParams cascade, Color, external icons, Size, Style/Class/Styles/Classes and the public CSS variables (which
/// reach the relocated callout too), RTL, reduced motion, prerendering and unmounting.
/// </summary>
[TestClass]
public class BitSearchBoxE2ETests : E2ETestBase
{
    private const string Url = "/e2e/search-box";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Input(string id) => Page.Locator($"#{id} .bit-srb-inp");

    private ILocator Box(string id) => Page.Locator($"#{id} .bit-srb-cnt");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    private Task BlurAsync() => Page.Locator("h1").ClickAsync();

    // Typed at a person's pace: a keystroke of an Immediate field is committed through a round trip, and a burst
    // faster than that can have a render write an older term back under the caret (Firefox shows it most).
    private static readonly LocatorPressSequentiallyOptions Typing = new() { Delay = 60 };

    private static Task ExpectNoAttributeAsync(ILocator locator, string name) =>
        Expect(locator).Not.ToHaveAttributeAsync(name, new Regex("[\\s\\S]*"));

    // The suggest list is relocated to the body while it is open, so it is found through the input that controls it.
    private async Task<ILocator> ListboxAsync(string id)
    {
        var listboxId = await Input(id).GetAttributeAsync("aria-controls");
        Assert.IsNotNull(listboxId, "The input controls no list.");
        return Page.Locator($"[id='{listboxId}']");
    }

    private async Task<ILocator> CalloutAsync(string id)
    {
        var listboxId = await Input(id).GetAttributeAsync("aria-controls");
        return Page.Locator($".bit-srb-cal:has([id='{listboxId}'])");
    }

    private async Task<ILocator> OptionsAsync(string id) => (await ListboxAsync(id)).Locator("[role=option]");

    private Task<string> ResolveAsync(string property, string value) =>
        Page.EvaluateAsync<string>(@"([p, v]) => {
            const d = document.createElement('div');
            d.style.setProperty(p, v);
            document.body.appendChild(d);
            const r = getComputedStyle(d).getPropertyValue(p);
            d.remove();
            return r;
        }", new[] { property, value });

    private Task<string> ColorOfAsync(string token) => ResolveAsync("color", $"var({token})");

    private static string SizeToken(string token) =>
        $"var({token}, calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * var({token}-steps)))";

    private static async Task<LocatorBoundingBoxResult> BoxOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no box.");
        return box;
    }

    private static Task<double> WidthAsync(ILocator locator) =>
        locator.EvaluateAsync<double>("el => el.getBoundingClientRect().width");

    // Waits, the way an expectation does, for a transition to bring the width of an element under a bound.
    private static Task WaitForWidthBelowAsync(ILocator locator, double bound) =>
        locator.EvaluateAsync(@"(el, bound) => new Promise((resolve, reject) => {
            const started = performance.now();
            const check = () => {
                const w = el.getBoundingClientRect().width;
                if (w < bound) return resolve();
                if (performance.now() - started > 10000) return reject(new Error('width ' + w + ' is not below ' + bound));
                requestAnimationFrame(check);
            };
            check();
        })", bound);


    // ---------- Lifecycle ----------

    [TestMethod]
    public async Task Prerender_TheServerHtmlCarriesTheSearchBoxBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"sb-basic\"");
        StringAssert.Contains(html, "role=\"searchbox\"");
        StringAssert.Contains(html, "id=\"status\">Loading<");

        await OpenAsync(Url);
        await Expect(Root("sb-basic")).ToHaveClassAsync(HasClass("bit-srb"));
    }

    [TestMethod]
    public async Task Unmount_RemovingASearchBoxWithItsListOpenAndItsShortcutArmed_LeavesNoError()
    {
        await OpenAsync(Url);

        await Input("sb-mount").PressSequentiallyAsync("a", Typing);
        var listbox = await ListboxAsync("sb-mount");
        await Expect(listbox).ToBeVisibleAsync();

        // The overlay of the open list covers the page, so the toggle is clicked from script, the way app code would
        // tear the search box down.
        var overlays = await Page.Locator(".bit-srb-ovl").CountAsync();
        await Page.Locator("#btn-toggle-mount").DispatchEventAsync("click");
        await Expect(Text("mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator(".bit-srb-ovl")).ToHaveCountAsync(overlays - 1);
        await Expect(Root("sb-mount")).ToHaveCountAsync(0);
        await Expect(listbox).ToHaveCountAsync(0);

        // The document-wide shortcut was taken off with it.
        await Page.Keyboard.PressAsync("Alt+M");

        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Root("sb-mount")).ToHaveCountAsync(1);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }


    // ---------- Basic ----------

    [TestMethod]
    public async Task Basic_TypingAndBlurringCommitsTheValue()
    {
        await OpenAsync(Url);

        var input = Input("sb-basic");
        await Expect(input).ToHaveAttributeAsync("role", "searchbox");
        await Expect(input).ToHaveAttributeAsync("type", "text");
        await Expect(input).ToHaveAttributeAsync("placeholder", "Search");
        await Expect(input).ToHaveAttributeAsync("enterkeyhint", "search");
        await ExpectNoAttributeAsync(input, "aria-expanded");

        await input.PressSequentiallyAsync("hello", Typing);
        await Expect(Text("basic-value")).ToHaveTextAsync("[]");
        await BlurAsync();
        await Expect(Text("basic-value")).ToHaveTextAsync("[hello]");
    }

    [TestMethod]
    public async Task AutoFocus_PutsTheFocusInTheInputOnTheFirstRender()
    {
        await OpenAsync("/e2e/search-box/autofocus");

        await Expect(Input("sb-autofocus")).ToBeFocusedAsync();
        await Expect(Input("sb-autofocus")).ToHaveAttributeAsync("autofocus", "");
        await Expect(Root("sb-autofocus")).ToHaveClassAsync(HasClass("bit-srb-foc"));
    }

    [TestMethod]
    [DataRow("enter")]
    [DataRow("done")]
    [DataRow("go")]
    [DataRow("next")]
    [DataRow("previous")]
    [DataRow("search")]
    [DataRow("send")]
    public async Task EnterKeyHint_LabelsTheReturnKeyOfAVirtualKeyboard(string hint)
    {
        await OpenAsync(Url);

        await Expect(Input($"sb-ekh-{hint}")).ToHaveAttributeAsync("enterkeyhint", hint);
        Assert.AreEqual(hint, await Input($"sb-ekh-{hint}").EvaluateAsync<string>("el => el.enterKeyHint"));
    }

    [TestMethod]
    public async Task Composition_TheKeysOfAnImeSessionNeverReachTheList()
    {
        await OpenAsync(Url);

        var input = Input("sb-suggest");
        await input.PressSequentiallyAsync("app", Typing);
        var options = await OptionsAsync("sb-suggest");
        await Expect(options).ToHaveCountAsync(4);

        // An arrow and an Enter that walk and commit the candidates of an input method editor are not the list's.
        await input.EvaluateAsync(@"el => {
            el.dispatchEvent(new CompositionEvent('compositionstart', { bubbles: true, data: '' }));
            el.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true, isComposing: true }));
            el.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true, isComposing: true }));
            el.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: '' }));
        }");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(options.First).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Text("suggest-select-count")).ToHaveTextAsync("0");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [TestMethod]
    public async Task Icon_CollapsesOnFocusAndAPressOnItFocusesTheInput()
    {
        await OpenAsync(Url);

        var wrapper = Page.Locator("#sb-basic .bit-srb-iwp");
        await Expect(wrapper.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Search"));
        await Expect(wrapper).ToHaveAttributeAsync("aria-hidden", "true");
        Assert.IsTrue(await WidthAsync(wrapper) > 10);

        await wrapper.ClickAsync();
        await Expect(Input("sb-basic")).ToBeFocusedAsync();
        await Expect(Root("sb-basic")).ToHaveClassAsync(HasClass("bit-srb-foc"));
        await Expect(wrapper.Locator("i")).ToHaveCSSAsync("opacity", "0");
        await WaitForWidthBelowAsync(wrapper, 1);
    }

    [TestMethod]
    public async Task ReadOnly_KeepsTheValueButBlocksEditingAndClearing()
    {
        await OpenAsync(Url);

        var input = Input("sb-readonly");
        await Expect(Root("sb-readonly")).ToHaveClassAsync(HasClass("bit-srb-rol"));
        await Expect(input).ToHaveAttributeAsync("readonly", "");
        await Expect(Page.Locator("#sb-readonly .bit-srb-cbt")).ToHaveCountAsync(0);

        await input.ClickAsync();
        await Page.Keyboard.TypeAsync("xyz");
        await Page.Keyboard.PressAsync("Escape");
        await BlurAsync();
        await Expect(input).ToHaveValueAsync("Read only value");
        await Expect(Text("readonly-changes")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Required_MarksTheInputAndTheLabel()
    {
        await OpenAsync(Url);

        await Expect(Input("sb-required")).ToHaveAttributeAsync("required", "");
        await Expect(Input("sb-required")).ToHaveAttributeAsync("aria-required", "true");
        await Expect(Root("sb-required")).ToHaveClassAsync(HasClass("bit-srb-req"));
        StringAssert.StartsWith(await Page.Locator("#sb-required .bit-srb-lbl").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content"), "\"*\"");
    }

    [TestMethod]
    public async Task Disabled_RendersTheDisabledMarkupAndRunsNoHandler()
    {
        await OpenAsync(Url);

        await Expect(Root("sb-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("sb-disabled")).ToBeDisabledAsync();
        await Expect(Input("sb-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Page.Locator("#sb-disabled .bit-srb-sbn")).ToBeDisabledAsync();
        await Expect(Page.Locator("#sb-disabled .bit-srb-sbn")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Page.Locator("#sb-disabled .bit-srb-cbt")).ToBeDisabledAsync();

        // A disabled search box takes no pointer events at all, so what is left to reach it is the click an
        // assistive technology dispatches: it runs neither the page's activation handlers nor the component's own,
        // while the page's hover handler stays.
        await Expect(Root("sb-disabled")).ToHaveCSSAsync("pointer-events", "none");
        await Root("sb-disabled").DispatchEventAsync("click");
        await Page.Locator("#sb-disabled .bit-srb-sbn").DispatchEventAsync("click");
        await Input("sb-disabled").DispatchEventAsync("click");
        await Root("sb-disabled").DispatchEventAsync("mouseover");
        await Expect(Text("disabled-root-hovers")).ToHaveTextAsync("1");
        await Expect(Text("disabled-root-clicks")).ToHaveTextAsync("0");
        await Expect(Text("disabled-searches")).ToHaveTextAsync("0");
        await Expect(Text("disabled-clicks")).ToHaveTextAsync("0");

        await Page.Locator("#before-disabled").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-disabled")).ToBeFocusedAsync();

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(Root("sb-disabled")).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Input("sb-disabled").ClickAsync();
        await Expect(Text("disabled-clicks")).ToHaveTextAsync("1");
        await Expect(Text("disabled-root-clicks")).ToHaveTextAsync("1");
        await Page.Locator("#sb-disabled .bit-srb-sbn").ClickAsync();
        await Expect(Text("disabled-searches")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task BaseParameters_IdClassStyleAttributesNameAndTabIndex()
    {
        await OpenAsync(Url);

        var root = Root("sb-base");
        await Expect(root).ToHaveClassAsync(HasClass("my-root-class"));
        await Expect(root).ToHaveCSSAsync("margin-top", "7px");
        await Expect(root).ToHaveAttributeAsync("data-test", "base-attr");
        await Expect(Input("sb-base")).ToHaveAttributeAsync("data-input-attr", "yes");
        await Expect(Input("sb-base")).ToHaveAttributeAsync("name", "q");
        await Expect(Input("sb-base")).ToHaveAttributeAsync("tabindex", "-1");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheSpace_CollapsedRemovesIt()
    {
        await OpenAsync(Url);

        var root = Root("sb-visibility");
        await Page.ClickAsync("#btn-vis-hidden");
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await BoxOfAsync(root)).Height > 20);
        await Page.ClickAsync("#btn-vis-collapsed");
        await Expect(root).ToHaveCSSAsync("display", "none");
        await Page.ClickAsync("#btn-vis-visible");
        await Expect(root).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Rerender_ChangingParametersAtRuntime_UpdatesTheSearchBox()
    {
        await OpenAsync(Url);

        await Expect(Input("sb-rerender")).ToHaveAttributeAsync("placeholder", "First placeholder");
        await Expect(Page.Locator("#sb-rerender .bit-srb-lbl")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#sb-rerender .bit-srb-sbn")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-change-params");
        await Expect(Input("sb-rerender")).ToHaveAttributeAsync("placeholder", "Second placeholder");
        await Expect(Page.Locator("#sb-rerender .bit-srb-lbl")).ToHaveTextAsync("Added label");
        await Expect(Page.Locator("#sb-rerender .bit-srb-sbn")).ToBeVisibleAsync();
        await Expect(Root("sb-rerender")).ToHaveClassAsync(HasClass("bit-srb-ssb"));
    }


    // ---------- Label and description ----------

    [TestMethod]
    public async Task Label_IsARealLabelThatNamesAndFocusesTheInput()
    {
        await OpenAsync(Url);

        var label = Page.Locator("#sb-label .bit-srb-lbl");
        await Expect(label).ToHaveTextAsync("Search products");
        await Expect(label).ToHaveAttributeAsync("for", (await Input("sb-label").GetAttributeAsync("id"))!);
        await Expect(Input("sb-label")).ToHaveAttributeAsync("aria-labelledby", (await label.GetAttributeAsync("id"))!);
        await label.ClickAsync();
        await Expect(Input("sb-label")).ToBeFocusedAsync();

        await Expect(Page.Locator("#sb-label-template .bit-srb-lbl .tpl-label")).ToHaveTextAsync("Advanced search");

        await Expect(Input("sb-arialabel")).ToHaveAttributeAsync("aria-label", "Search the site");
        await ExpectNoAttributeAsync(Input("sb-arialabel"), "aria-labelledby");
    }

    [TestMethod]
    public async Task DescriptionAndAriaDescription_AreReferencedInReadingOrder()
    {
        await OpenAsync(Url);

        var description = Page.Locator("#sb-desc .bit-srb-des");
        await Expect(description).ToHaveTextAsync("Searches component names, parameters and guides.");
        await Expect(Input("sb-desc")).ToHaveAttributeAsync("aria-describedby", (await description.GetAttributeAsync("id"))!);

        await Expect(Page.Locator("#sb-desc-template .bit-srb-des .tpl-desc")).ToHaveTextAsync("Order number, customer name or email.");
        var hidden = Page.Locator("#sb-desc-template .bit-srb-ard");
        await Expect(hidden).ToHaveTextAsync("Results update as you type.");
        Assert.IsTrue(await hidden.EvaluateAsync<bool>("el => el.getBoundingClientRect().width <= 1"), "It is visually hidden.");
        var ids = $"{await Page.Locator("#sb-desc-template .bit-srb-des").GetAttributeAsync("id")} {await hidden.GetAttributeAsync("id")}";
        await Expect(Input("sb-desc-template")).ToHaveAttributeAsync("aria-describedby", ids);
    }


    // ---------- Variants ----------

    [TestMethod]
    public async Task UnderlinedAndNoBorder_ChangeTheFrame()
    {
        await OpenAsync(Url);

        await Expect(Root("sb-underlined")).ToHaveClassAsync(HasClass("bit-srb-und"));
        await Expect(Box("sb-underlined")).ToHaveCSSAsync("border-top-width", "0px");
        Assert.AreNotEqual("0px", await ComputedStyleAsync("#sb-underlined .bit-srb-cnt", "border-bottom-width"));
        Assert.AreNotEqual("0px", await ComputedStyleAsync("#sb-default-look .bit-srb-cnt", "border-top-width"));

        await Expect(Root("sb-noborder")).ToHaveClassAsync(HasClass("bit-srb-nbr"));
        await Expect(Box("sb-noborder")).ToHaveCSSAsync("border-bottom-width", "0px");
    }

    [TestMethod]
    [DataRow("primary", "bit-srb-bpr", "var(--bit-clr-bg-pri)")]
    [DataRow("secondary", "bit-srb-bse", "var(--bit-clr-bg-sec)")]
    [DataRow("tertiary", "bit-srb-btr", "var(--bit-clr-bg-ter)")]
    [DataRow("transparent", "bit-srb-btn", "transparent")]
    public async Task Background_PaintsTheFieldFromItsColorKind(string kind, string cssClass, string token)
    {
        await OpenAsync(Url);

        var id = $"sb-bg-{kind}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));
        await Expect(Box(id)).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", token));
    }


    // ---------- Icon ----------

    [TestMethod]
    public async Task FixedIcon_KeepsTheIconOnFocus_DisableAnimationSnapsIt()
    {
        await OpenAsync(Url);

        await Expect(Root("sb-fixedicon")).ToHaveClassAsync(HasClass("bit-srb-fic"));
        await Input("sb-fixedicon").FocusAsync();
        await Expect(Root("sb-fixedicon")).ToHaveClassAsync(HasClass("bit-srb-foc"));
        await Expect(Page.Locator("#sb-fixedicon .bit-srb-iwp i")).ToHaveCSSAsync("opacity", "1");
        Assert.IsTrue(await WidthAsync(Page.Locator("#sb-fixedicon .bit-srb-iwp")) > 10);

        await Expect(Root("sb-noanim")).ToHaveClassAsync(HasClass("bit-srb-nan"));
        await Expect(Page.Locator("#sb-noanim .bit-srb-iwp")).ToHaveCSSAsync("transition-duration", "0s");
    }

    [TestMethod]
    public async Task IconNameAndHideIcon()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#sb-customicon .bit-srb-iwp i")).ToHaveClassAsync(HasClass("bit-icon--Filter"));
        await Expect(Page.Locator("#sb-hideicon .bit-srb-iwp")).ToHaveCountAsync(0);
        await Expect(Root("sb-hideicon")).ToHaveClassAsync(HasClass("bit-srb-hic"));
    }


    // ---------- Search button ----------

    [TestMethod]
    public async Task SearchButton_CommitsWhatIsTypedAndRaisesOnSearch()
    {
        await OpenAsync(Url);

        var button = Page.Locator("#sb-searchbtn .bit-srb-sbn");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Search");
        await Expect(button).ToHaveAttributeAsync("title", "Search");
        await Expect(button.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChromeBackMirrored"));
        await Expect(button.Locator("i")).ToHaveClassAsync(HasClass("bit-srb-dsi"));
        await Expect(Root("sb-searchbtn")).ToHaveClassAsync(HasClass("bit-srb-ssb"));

        await Input("sb-searchbtn").PressSequentiallyAsync("abc", Typing);
        await button.ClickAsync();
        await Expect(Text("searchbtn-search")).ToHaveTextAsync("[abc]");
        await Expect(Text("searchbtn-value")).ToHaveTextAsync("[abc]");
        await Expect(Text("searchbtn-count")).ToHaveTextAsync("1");

        // Enter does the same.
        await Input("sb-searchbtn").ClickAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.TypeAsync("d");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("searchbtn-search")).ToHaveTextAsync("[abcd]");
        await Expect(Text("searchbtn-count")).ToHaveTextAsync("2");

        await Expect(button).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-pri)"));
    }

    [TestMethod]
    public async Task SearchButton_TextIconTemplateAndAriaLabel()
    {
        await OpenAsync(Url);

        var withText = Page.Locator("#sb-searchbtn-text .bit-srb-sbn");
        await Expect(withText.Locator(".bit-srb-sbx")).ToHaveTextAsync("Search");
        await ExpectNoAttributeAsync(withText, "aria-label");
        await Expect(Root("sb-searchbtn-text")).ToHaveClassAsync(HasClass("bit-srb-sbt"));

        var icon = Page.Locator("#sb-searchbtn-icon .bit-srb-sbn i");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--PageListFilter"));
        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-srb-dsi"));

        await Expect(Page.Locator("#sb-searchbtn-template .bit-srb-sbn .tpl-go")).ToHaveTextAsync("GO");
        await Expect(Page.Locator("#sb-searchbtn-template .bit-srb-sbn")).ToHaveAttributeAsync("aria-label", "Search");
        await Expect(Page.Locator("#sb-searchbtn-aria .bit-srb-sbn")).ToHaveAttributeAsync("aria-label", "جستجو");
    }

    [TestMethod]
    public async Task Loading_ShowsASpinnerInPlaceOfTheClearButtonAndAnnouncesIt()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#sb-searchbtn-loading .bit-srb-cbt")).ToBeVisibleAsync();
        await ExpectNoAttributeAsync(Input("sb-searchbtn-loading"), "aria-busy");

        await Page.ClickAsync("#btn-toggle-loading");
        await Expect(Root("sb-searchbtn-loading")).ToHaveClassAsync(HasClass("bit-srb-lng"));
        await Expect(Page.Locator("#sb-searchbtn-loading .bit-srb-lsp .bit-srb-spn")).ToBeVisibleAsync();
        await Expect(Page.Locator("#sb-searchbtn-loading .bit-srb-cbt")).ToHaveCountAsync(0);
        await Expect(Input("sb-searchbtn-loading")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Page.Locator("#sb-searchbtn-loading .bit-srb-elv")).ToHaveTextAsync("Searching");
        StringAssert.Contains(await ComputedStyleAsync("#sb-searchbtn-loading .bit-srb-spn", "animation-name"), "bit-spin");

        await Expect(Page.Locator("#sb-searchbtn-loading-label .bit-srb-elv")).ToHaveTextAsync("Looking it up");

        await Page.ClickAsync("#btn-toggle-loading");
        await Expect(Page.Locator("#sb-searchbtn-loading .bit-srb-lsp")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#sb-searchbtn-loading .bit-srb-cbt")).ToBeVisibleAsync();
    }


    // ---------- Clear button ----------

    [TestMethod]
    public async Task ClearButton_EmptiesTheValueRaisesOnClearAndRefocuses()
    {
        await OpenAsync(Url);

        var button = Page.Locator("#sb-clear .bit-srb-cbt");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Clear");
        await Expect(button).ToHaveAttributeAsync("title", "Clear");
        await Expect(button).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(button.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));
        await Expect(Root("sb-clear")).ToHaveClassAsync(HasClass("bit-srb-hvl"));

        await button.ClickAsync();
        await Expect(Input("sb-clear")).ToHaveValueAsync("");
        await Expect(Text("clear-value")).ToHaveTextAsync("[null]");
        await Expect(Text("clear-count")).ToHaveTextAsync("1");
        await Expect(Input("sb-clear")).ToBeFocusedAsync();
        await Expect(button).ToHaveCountAsync(0);
        await Expect(Root("sb-clear")).Not.ToHaveClassAsync(HasClass("bit-srb-hvl"));
    }

    [TestMethod]
    public async Task ClearButton_AppearsOnTheFirstKeystroke_AndCanBeHiddenOrRestyled()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#sb-clear-firstkey .bit-srb-cbt")).ToHaveCountAsync(0);
        await Input("sb-clear-firstkey").PressSequentiallyAsync("a", Typing);
        await Expect(Page.Locator("#sb-clear-firstkey .bit-srb-cbt")).ToBeVisibleAsync();

        await Expect(Page.Locator("#sb-hideclear .bit-srb-cbt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#sb-clear-template .bit-srb-cbt .tpl-clear")).ToHaveTextAsync("CLR");
        await Expect(Page.Locator("#sb-clear-aria .bit-srb-cbt")).ToHaveAttributeAsync("aria-label", "پاک کردن");

        // Without the Escape route the button becomes a tab stop.
        await ExpectNoAttributeAsync(Page.Locator("#sb-noclearesc .bit-srb-cbt"), "tabindex");
    }

    [TestMethod]
    public async Task Escape_ClearsTheValueRaisesOnEscapeAndOnClear()
    {
        await OpenAsync(Url);

        var input = Input("sb-escape");
        await Expect(input).ToHaveAttributeAsync("data-bit-esc", "claim");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Text("escape-value")).ToHaveTextAsync("[null]");
        await Expect(Text("escape-count")).ToHaveTextAsync("1");
        await Expect(Text("escape-clears")).ToHaveTextAsync("1");
        await Expect(input).ToHaveValueAsync("");

        // Text typed but not committed is wiped too, and claimed as "text", but no OnClear is raised for it.
        await Page.Keyboard.TypeAsync("abc");
        await Expect(input).ToHaveAttributeAsync("data-bit-esc", "text");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(input).ToHaveValueAsync("");
        await Expect(Text("escape-count")).ToHaveTextAsync("2");
        await Expect(Text("escape-clears")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Escape_NoClearOnEscapeAndAOneWayValueAreLeftAlone()
    {
        await OpenAsync(Url);

        await ExpectNoAttributeAsync(Input("sb-noclearesc"), "data-bit-esc");
        await Input("sb-noclearesc").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Input("sb-noclearesc")).ToHaveValueAsync("stays");

        await ExpectNoAttributeAsync(Input("sb-oneway"), "data-bit-esc");
        await Input("sb-oneway").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Input("sb-oneway")).ToHaveValueAsync("fixed");
    }

    [TestMethod]
    public async Task Escape_InsideADialog_ClosesTheListThenClearsThenClosesTheDialog()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-open-dialog");
        await Expect(Input("sb-dialog")).ToBeFocusedAsync();
        await Expect(Root("sb-dialog")).ToHaveClassAsync(HasClass("bit-srb-foc"));

        await Page.Keyboard.TypeAsync("app");
        await Expect(Input("sb-dialog")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Input("sb-dialog")).ToHaveAttributeAsync("data-bit-esc", "claim");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Input("sb-dialog")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Text("dialog-value")).ToHaveTextAsync("[app]");
        await Expect(Text("dialog-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Text("dialog-value")).ToHaveTextAsync("[null]");
        await Expect(Text("dialog-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Text("dialog-open")).ToHaveTextAsync("False");
        await Expect(Root("sb-dialog-host")).ToHaveCountAsync(0);
    }


    // ---------- Prefix and suffix ----------

    [TestMethod]
    public async Task PrefixAndSuffix_SitAroundTheInputAndStayOutOfTheValue()
    {
        await OpenAsync(Url);

        var prefix = Page.Locator("#sb-prefix .bit-srb-pre");
        await Expect(prefix).ToHaveTextAsync("https://");
        Assert.IsTrue((await BoxOfAsync(prefix)).X < (await BoxOfAsync(Input("sb-prefix"))).X);
        var suffix = Page.Locator("#sb-suffix .bit-srb-suf");
        await Expect(suffix).ToHaveTextAsync(".com");
        var suffixInput = await BoxOfAsync(Input("sb-suffix"));
        Assert.IsTrue((await BoxOfAsync(suffix)).X >= suffixInput.X + suffixInput.Width - 1);

        await Input("sb-prefix").PressSequentiallyAsync("bitplatform.dev", Typing);
        await BlurAsync();
        await Expect(Text("prefix-value")).ToHaveTextAsync("[bitplatform.dev]");

        await Input("sb-prefix").FocusAsync();
        await Expect(prefix).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-pri"));

        var templatePrefix = await BoxOfAsync(Page.Locator("#sb-affix-templates .tpl-prefix"));
        var templateSuffix = await BoxOfAsync(Page.Locator("#sb-affix-templates .tpl-suffix"));
        var templateInput = await BoxOfAsync(Input("sb-affix-templates"));
        Assert.IsTrue(templatePrefix.X < templateInput.X && templateSuffix.X > templateInput.X);
    }


    // ---------- Input behavior ----------

    [TestMethod]
    public async Task MaxLengthAndTrim()
    {
        await OpenAsync(Url);

        await Expect(Input("sb-maxlength")).ToHaveAttributeAsync("maxlength", "10");
        await Input("sb-maxlength").PressSequentiallyAsync("abcdefghijklmno", Typing);
        await Expect(Input("sb-maxlength")).ToHaveValueAsync("abcdefghij");
        await BlurAsync();
        await Expect(Text("maxlength-value")).ToHaveTextAsync("[abcdefghij]");

        await Input("sb-trim").PressSequentiallyAsync("  hi  ", Typing);
        await BlurAsync();
        await Expect(Text("trim-value")).ToHaveTextAsync("[hi]");
    }

    [TestMethod]
    public async Task KeyboardAttributes_InputModeEnterKeyHintSpellCheckAutoCorrectAutoCapitalizeAutoComplete()
    {
        await OpenAsync(Url);

        await Expect(Input("sb-inputmode")).ToHaveAttributeAsync("inputmode", "search");
        await Expect(Input("sb-enterkeyhint")).ToHaveAttributeAsync("enterkeyhint", "go");
        await Expect(Input("sb-spell")).ToHaveAttributeAsync("spellcheck", "false");
        await Expect(Input("sb-spell")).ToHaveAttributeAsync("autocorrect", "off");
        await Expect(Input("sb-spell")).ToHaveAttributeAsync("autocapitalize", "none");
        await Expect(Input("sb-spell")).ToHaveAttributeAsync("autocomplete", "on");

        foreach (var name in new[] { "inputmode", "spellcheck", "autocorrect", "autocapitalize", "autocomplete" })
        {
            await ExpectNoAttributeAsync(Input("sb-basic"), name);
        }

        // With suggestions the browser's own autocomplete is off, so the two lists never overlap.
        await Expect(Input("sb-suggest")).ToHaveAttributeAsync("autocomplete", "off");
    }

    [TestMethod]
    public async Task SelectTextOnFocus_SelectsTheTermSoTypingReplacesIt()
    {
        await OpenAsync(Url);

        var input = Input("sb-selectfocus");
        await input.ClickAsync();
        await Expect(input).ToHaveJSPropertyAsync("selectionStart", 0);
        await Expect(input).ToHaveJSPropertyAsync("selectionEnd", "Click me to select this term".Length);
        await Page.Keyboard.TypeAsync("new");
        await Expect(input).ToHaveValueAsync("new");
    }

    [TestMethod]
    public async Task FullWidth_FillsTheFlexContainer()
    {
        await OpenAsync(Url);

        await Expect(Root("sb-fullwidth")).ToHaveClassAsync(HasClass("bit-srb-flw"));
        var full = await WidthAsync(Root("sb-fullwidth"));
        var notFull = await WidthAsync(Root("sb-notfullwidth"));
        Assert.IsTrue(full > notFull + 50, $"{full} fills more than {notFull}.");
        var container = await BoxOfAsync(Page.Locator("#fullwidth-box"));
        var button = await BoxOfAsync(Page.Locator("#fullwidth-box button"));
        Assert.AreEqual(container.Width - button.Width - 16, full, 2);
    }


    // ---------- Binding ----------

    [TestMethod]
    public async Task TwoWay_TypingUpdatesThePageAndThePageUpdatesTheField()
    {
        await OpenAsync(Url);

        await Input("sb-twoway").FillAsync("");
        await Input("sb-twoway").PressSequentiallyAsync("typed", Typing);
        await BlurAsync();
        await Expect(Text("twoway-value")).ToHaveTextAsync("[typed]");

        await Page.ClickAsync("#btn-set-twoway");
        await Expect(Input("sb-twoway")).ToHaveValueAsync("from page");
    }

    [TestMethod]
    public async Task Immediate_EveryKeystrokeCommits()
    {
        await OpenAsync(Url);

        await Input("sb-immediate").PressSequentiallyAsync("abc", Typing);
        await Expect(Text("immediate-value")).ToHaveTextAsync("[abc]");
        await Expect(Text("immediate-count")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task DebounceTime_CommitsOnceTheTypingPauses()
    {
        await OpenAsync(Url);

        await Input("sb-debounce").PressSequentiallyAsync("abcdef", new() { Delay = 20 });
        Assert.AreEqual("[]", await Text("debounce-value").TextContentAsync(), "Nothing is committed while the typing goes on.");
        await Expect(Text("debounce-value")).ToHaveTextAsync("[abcdef]");
        await Expect(Text("debounce-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ThrottleTime_CommitsTheFirstKeystrokeAndThenOncePerInterval()
    {
        await OpenAsync(Url);

        await Input("sb-throttle").PressSequentiallyAsync("abcdef", new() { Delay = 40 });
        await Expect(Text("throttle-first")).ToHaveTextAsync("[a]");
        await Expect(Text("throttle-value")).ToHaveTextAsync("[abcdef]");
        await Expect(Text("throttle-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task OnChangeAndDefaultValue()
    {
        await OpenAsync(Url);

        await Input("sb-onchange").PressSequentiallyAsync("ab", Typing);
        await Expect(Text("onchange-value")).ToHaveTextAsync("[ab]");
        await Expect(Text("onchange-count")).ToHaveTextAsync("2");

        await Expect(Input("sb-default")).ToHaveValueAsync("bit BlazorUI");
        await Input("sb-default").ClickAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.TypeAsync("!");
        await BlurAsync();
        await Expect(Text("default-value")).ToHaveTextAsync("[bit BlazorUI!]");
    }


    // ---------- Events ----------

    [TestMethod]
    public async Task Events_ClickFocusKeysEscapeClearAndSearchAreReported()
    {
        await OpenAsync(Url);

        await Input("sb-events").ClickAsync();
        await Expect(Text("ev-click")).ToHaveTextAsync("1");
        await Expect(Text("ev-focus")).ToHaveTextAsync("1");
        await Expect(Text("ev-focusin")).ToHaveTextAsync("1");

        await Page.Keyboard.TypeAsync("q");
        await Expect(Text("ev-last-keydown")).ToHaveTextAsync("q");
        await Expect(Text("ev-keyup")).ToHaveTextAsync("1");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("ev-search")).ToHaveTextAsync("1");
        await Expect(Text("ev-search-value")).ToHaveTextAsync("[q]");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Text("ev-escape")).ToHaveTextAsync("1");
        await Expect(Text("ev-clear")).ToHaveTextAsync("1");

        await BlurAsync();
        await Expect(Text("ev-focusout")).ToHaveTextAsync("1");
    }


    // ---------- Suggestions ----------

    [TestMethod]
    public async Task Suggestions_TypingOpensAPositionedListOfTheMatchesAndAnnouncesThem()
    {
        await OpenAsync(Url);

        var input = Input("sb-suggest");
        await Expect(input).ToHaveAttributeAsync("role", "combobox");
        await Expect(input).ToHaveAttributeAsync("aria-haspopup", "listbox");
        await Expect(input).ToHaveAttributeAsync("aria-autocomplete", "list");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");

        // Two characters are below the default minimum of three.
        await input.PressSequentiallyAsync("ap", Typing);
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");

        await input.PressSequentiallyAsync("p", Typing);
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "true");
        var listbox = await ListboxAsync("sb-suggest");
        await Expect(listbox).ToBeVisibleAsync();
        await Expect(listbox).ToHaveAttributeAsync("role", "listbox");
        await Expect(listbox).ToHaveAttributeAsync("aria-label", "Suggestions");
        var options = listbox.Locator("[role=option]");
        await Expect(options).ToHaveTextAsync(["Apple", "Red Apple", "Blue Apple", "Green Apple"]);
        await Expect(options.First).ToHaveAttributeAsync("aria-setsize", "4");
        await Expect(options.Nth(1)).ToHaveAttributeAsync("aria-posinset", "2");
        await Expect(Root("sb-suggest")).ToHaveClassAsync(HasClass("bit-srb-opn"));
        await Expect(Text("suggest-open")).ToHaveTextAsync("True");

        // Measured once the list has finished sliding in, not partway through its entry transition.
        await (await CalloutAsync("sb-suggest")).EvaluateAsync("el => Promise.all(el.getAnimations({ subtree: true }).map(a => a.finished))");
        var field = await BoxOfAsync(Box("sb-suggest"));
        var callout = await BoxOfAsync(await CalloutAsync("sb-suggest"));
        Assert.AreEqual(field.Y + field.Height, callout.Y, 3, "The list opens right under the field.");
        Assert.AreEqual(field.X, callout.X, 3, "The list is aligned with the field.");
        Assert.IsTrue(callout.Width >= field.Width - 2, "The list is at least as wide as the field.");

        await Expect(Page.Locator("#sb-suggest [role=status][aria-live=polite]")).ToHaveTextAsync(new Regex("^4 suggestions available\\. Use the up and down arrow keys"));
    }

    [TestMethod]
    public async Task Suggestions_AClickOnASuggestionPicksItAndRaisesTheEvents()
    {
        await OpenAsync(Url);

        var input = Input("sb-suggest");
        await input.PressSequentiallyAsync("app", Typing);
        var options = await OptionsAsync("sb-suggest");
        await options.Nth(1).ClickAsync();

        await Expect(input).ToHaveValueAsync("Red Apple");
        await Expect(Text("suggest-value")).ToHaveTextAsync("[Red Apple]");
        await Expect(Text("suggest-selected")).ToHaveTextAsync("[Red Apple]");
        await Expect(Text("suggest-select-count")).ToHaveTextAsync("1");
        await Expect(Text("suggest-search")).ToHaveTextAsync("[Red Apple]");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(await ListboxAsync("sb-suggest")).Not.ToBeVisibleAsync();
        await Expect(Text("suggest-open")).ToHaveTextAsync("False");
        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveJSPropertyAsync("selectionStart", "Red Apple".Length);
    }

    [TestMethod]
    public async Task Suggestions_TheArrowKeysMoveTheHighlightAndEnterPicksIt()
    {
        await OpenAsync(Url);

        var input = Input("sb-suggest");
        await input.PressSequentiallyAsync("app", Typing);
        var options = await OptionsAsync("sb-suggest");
        await Expect(options).ToHaveCountAsync(4);

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(options.First).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(options.First).ToHaveClassAsync(HasClass("bit-srb-sel"));
        await Expect(input).ToHaveAttributeAsync("aria-activedescendant", (await options.First.GetAttributeAsync("id"))!);

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(options.Nth(1)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(options.First).ToHaveAttributeAsync("aria-selected", "false");

        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(options.Nth(3)).ToHaveAttributeAsync("aria-selected", "true");

        await Page.Keyboard.PressAsync("Home");
        await Expect(options.First).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("End");
        await Expect(options.Nth(3)).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(options.First).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(options.Nth(3)).ToHaveAttributeAsync("aria-selected", "true");

        // The keys drove the list, not the caret: it is still at the end of the term.
        await Expect(input).ToHaveJSPropertyAsync("selectionStart", 3);

        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(input).ToHaveValueAsync("Blue Apple");
        await Expect(Text("suggest-selected")).ToHaveTextAsync("[Blue Apple]");
    }

    [TestMethod]
    public async Task Suggestions_AltArrowsOpenAndCloseTabAndEscapeDismiss()
    {
        await OpenAsync(Url);

        var input = Input("sb-suggest");
        await input.PressSequentiallyAsync("app", Typing);
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "true");


        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "true");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(input).ToHaveValueAsync("app");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "true");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Text("suggest-toggles")).Not.ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Suggestions_AClickOutsideClosesTheListThroughTheOverlay()
    {
        await OpenAsync(Url);

        var input = Input("sb-suggest");
        await input.PressSequentiallyAsync("app", Typing);
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "true");

        var field = await BoxOfAsync(Box("sb-suggest"));
        await Page.Mouse.ClickAsync(field.X + field.Width + 300, field.Y + 5);
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Text("suggest-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task SuggestFilterFunctionAndIgnoreDiacritics_DecideWhatMatches()
    {
        await OpenAsync(Url);

        await Input("sb-suggest-filter").PressSequentiallyAsync("app", Typing);
        await Expect(await OptionsAsync("sb-suggest-filter")).ToHaveTextAsync(["Apple"]);

        await Input("sb-suggest-diacritics").PressSequentiallyAsync("jose", Typing);
        var options = await OptionsAsync("sb-suggest-diacritics");
        await Expect(options).ToHaveTextAsync(["José Álvarez"]);
        await Expect(options.First.Locator("mark.bit-srb-hlt")).ToHaveTextAsync("José");
    }

    [TestMethod]
    public async Task MinSuggestTriggerCharsAndMaxSuggestCount()
    {
        await OpenAsync(Url);

        await Input("sb-suggest-min").PressSequentiallyAsync("a", Typing);
        var options = await OptionsAsync("sb-suggest-min");
        await Expect(options).ToHaveCountAsync(3);
        await Expect(options).ToHaveTextAsync(["Apple", "Red Apple", "Blue Apple"]);
    }

    [TestMethod]
    public async Task SuggestItemsProvider_LoadsShowsTheLoadingRowAndCancelsASupersededSearch()
    {
        await OpenAsync(Url);

        var input = Input("sb-suggest-provider");
        await input.PressSequentiallyAsync("p", Typing);
        var callout = await CalloutAsync("sb-suggest-provider");
        await Expect(callout.Locator(".bit-srb-lod")).ToHaveTextAsync("Searching...");
        await Expect(await ListboxAsync("sb-suggest-provider")).ToHaveAttributeAsync("aria-busy", "true");

        await input.PressSequentiallyAsync("ro", Typing);
        var options = await OptionsAsync("sb-suggest-provider");
        await Expect(options).ToHaveTextAsync(["Product 1", "Product 2", "Product 3", "Promotion", "Prototype"]);
        await Expect(callout.Locator(".bit-srb-lod")).ToHaveCountAsync(0);
        await ExpectNoAttributeAsync(await ListboxAsync("sb-suggest-provider"), "aria-busy");

        // Every call that was made for a superseded term was cancelled through its token, and only the last one
        // completed. A term superseded before it reached the provider - the first one, whose search waits for the
        // callout to open before it asks - is never asked about at all, rather than with a token already cancelled.
        await Expect(Text("provider-cancelled-on-arrival")).ToHaveTextAsync("0");
        var terms = (await Text("provider-terms").TextContentAsync())!.Split(',');
        Assert.AreEqual("pro", terms[^1]);
        CollectionAssert.IsSubsetOf(terms, new[] { "p", "pr", "pro" });
        await Expect(Text("provider-cancelled")).ToHaveTextAsync((terms.Length - 1).ToString());

        await options.Nth(3).ClickAsync();
        await Expect(Text("provider-selected")).ToHaveTextAsync("[Promotion]");
    }

    [TestMethod]
    public async Task ModelessAndFixedCalloutWidth_NoOverlayAndTheListIsAsWideAsTheField()
    {
        await OpenAsync(Url);

        await Input("sb-suggest-modeless").PressSequentiallyAsync("a", Typing);
        var callout = await CalloutAsync("sb-suggest-modeless");
        await Expect(callout).ToBeVisibleAsync();
        Assert.AreEqual(await WidthAsync(Root("sb-suggest-modeless")), await WidthAsync(callout), 2);

        // Modeless: the page around stays usable while the list is open, and the list closes once the field is left.
        await Page.Locator("#btn-set-twoway").ClickAsync();
        await Expect(Input("sb-twoway")).ToHaveValueAsync("from page");
        await Expect(Input("sb-suggest-modeless")).ToHaveAttributeAsync("aria-expanded", "false");

        // Without FixedCalloutWidth the list grows with its longest suggestion.
        await Input("sb-suggest-wide").PressSequentiallyAsync("a", Typing);
        var wide = await CalloutAsync("sb-suggest-wide");
        await Expect(wide).ToBeVisibleAsync();
        Assert.IsTrue(await WidthAsync(wide) > await WidthAsync(Root("sb-suggest-wide")) + 20, "The list is wider than the field.");
    }

    [TestMethod]
    public async Task HighlightSuggestItems_MarksTheMatchedPart()
    {
        await OpenAsync(Url);

        await Input("sb-highlight").PressSequentiallyAsync("app", Typing);
        var options = await OptionsAsync("sb-highlight");
        await Expect(options.First.Locator("mark.bit-srb-hlt")).ToHaveTextAsync("App");
        await Expect(options.Nth(1).Locator("mark.bit-srb-hlt")).ToHaveTextAsync("App");
        await Expect(options.First.Locator("mark.bit-srb-hlt")).ToHaveCSSAsync("text-decoration-line", "underline");
    }

    [TestMethod]
    public async Task SuggestItemTemplate_RendersEachRow()
    {
        await OpenAsync(Url);

        await Input("sb-item-template").PressSequentiallyAsync("ban", Typing);
        await Expect((await OptionsAsync("sb-item-template")).Locator(".tpl-item")).ToHaveTextAsync(["* Banana"]);
    }

    [TestMethod]
    public async Task NoResults_TextAndTemplateAreShownAndAnnounced()
    {
        await OpenAsync(Url);

        await Input("sb-noresults").PressSequentiallyAsync("xyz", Typing);
        var callout = await CalloutAsync("sb-noresults");
        await Expect(callout.Locator(".bit-srb-nrs")).ToHaveTextAsync("No matching item found.");
        await Expect(Page.Locator("#sb-noresults .bit-srb-lvr")).ToHaveTextAsync(new Regex("No matching item found\\."));

        await Input("sb-noresults-template").PressSequentiallyAsync("xyz", Typing);
        await Expect((await CalloutAsync("sb-noresults-template")).Locator(".tpl-noresults")).ToHaveTextAsync("Nothing here");
    }

    [TestMethod]
    public async Task SuggestFailed_TextTemplateAndOnSuggestFailed()
    {
        await OpenAsync(Url);

        await Input("sb-failed").PressSequentiallyAsync("a", Typing);
        await Expect((await CalloutAsync("sb-failed")).Locator(".bit-srb-fld")).ToHaveTextAsync("Suggestions are unavailable right now.");
        await Expect(Text("failure")).ToHaveTextAsync("[The suggestions endpoint is unreachable.]");
        await Expect(Page.Locator("#sb-failed .bit-srb-lvr")).ToHaveTextAsync(new Regex("Suggestions are unavailable right now\\."));

        await Input("sb-failed-template").PressSequentiallyAsync("a", Typing);
        await Expect((await CalloutAsync("sb-failed-template")).Locator(".tpl-failed")).ToHaveTextAsync("Offline");
    }

    [TestMethod]
    public async Task MinSuggestTriggerCharsText_SaysHowManyCharactersAreMissing()
    {
        await OpenAsync(Url);

        var input = Input("sb-mintext");
        await input.PressSequentiallyAsync("a", Typing);
        var hint = (await CalloutAsync("sb-mintext")).Locator(".bit-srb-hnt");
        await Expect(hint).ToHaveTextAsync("Type 2 more character(s) to search");
        await input.PressSequentiallyAsync("p", Typing);
        await Expect(hint).ToHaveTextAsync("Type 1 more character(s) to search");
        await input.PressSequentiallyAsync("p", Typing);
        await Expect(hint).ToHaveCountAsync(0);
        await Expect(await OptionsAsync("sb-mintext")).ToHaveCountAsync(4);
    }

    [TestMethod]
    public async Task LoadingTemplate_ReplacesTheSpinnerWhileTheProviderRuns()
    {
        await OpenAsync(Url);

        await Input("sb-loading-template").PressSequentiallyAsync("a", Typing);
        var loading = (await CalloutAsync("sb-loading-template")).Locator(".bit-srb-lod .tpl-loading");
        await Expect(loading).ToHaveTextAsync("Hold on");
        await Expect((await CalloutAsync("sb-loading-template")).Locator(".bit-srb-lod .bit-srb-spn")).ToHaveCountAsync(0);
        await Expect(loading).ToHaveCountAsync(0);
        await Expect(await OptionsAsync("sb-loading-template")).Not.ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task HeaderAndFooterTemplates_FrameTheList()
    {
        await OpenAsync(Url);

        await Input("sb-header-footer").PressSequentiallyAsync("a", Typing);
        await Expect(await ListboxAsync("sb-header-footer")).ToBeVisibleAsync();
        var callout = await CalloutAsync("sb-header-footer");
        await Expect(callout.Locator(".bit-srb-chd .tpl-header")).ToHaveTextAsync("Top matches");
        await Expect(callout.Locator(".bit-srb-cft .tpl-footer")).ToHaveTextAsync("See all results");
        var header = await BoxOfAsync(callout.Locator(".bit-srb-chd"));
        var list = await BoxOfAsync(await ListboxAsync("sb-header-footer"));
        var footer = await BoxOfAsync(callout.Locator(".bit-srb-cft"));
        Assert.IsTrue(header.Y < list.Y && list.Y < footer.Y, "header, list, footer");
    }

    [TestMethod]
    public async Task AutoSelectSuggestItem_HighlightsTheFirstSoEnterPicksIt()
    {
        await OpenAsync(Url);

        await Input("sb-autoselect").PressSequentiallyAsync("ban", Typing);
        await Expect((await OptionsAsync("sb-autoselect")).First).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("autoselect-value")).ToHaveTextAsync("[Banana]");
    }

    [TestMethod]
    public async Task AutoFillSuggestItem_CompletesInPlaceAndOnlyEnterAcceptsIt()
    {
        await OpenAsync(Url);

        var input = Input("sb-autofill");
        await Expect(input).ToHaveAttributeAsync("aria-autocomplete", "both");

        await input.PressSequentiallyAsync("ba", Typing);
        await Expect(input).ToHaveValueAsync("banana");
        await Expect(input).ToHaveJSPropertyAsync("selectionStart", 2);
        await Expect(input).ToHaveJSPropertyAsync("selectionEnd", 6);
        await Expect(Text("autofill-value")).ToHaveTextAsync("[ba]");

        // Backspace takes the completion away rather than having it come back.
        await Page.Keyboard.PressAsync("Backspace");
        await Expect(input).ToHaveValueAsync("ba");

        await Page.Keyboard.TypeAsync("n");
        await Expect(input).ToHaveValueAsync("banana");

        // Escape puts the typed term back and closes the list.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(input).ToHaveValueAsync("ban");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.Keyboard.TypeAsync("a");
        await Expect(input).ToHaveValueAsync("banana");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("autofill-value")).ToHaveTextAsync("[banana]");
    }

    [TestMethod]
    public async Task ShowSuggestItemsOnFocus_OpensTheListOnFocus()
    {
        await OpenAsync(Url);

        await Input("sb-onfocus").ClickAsync();
        await Expect(await OptionsAsync("sb-onfocus")).ToHaveTextAsync(["bit BlazorUI", "Blazor components", "Fluent UI"]);
        await Expect(Input("sb-onfocus")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [TestMethod]
    public async Task AnnouncementProvider_ReplacesTheBuiltInSentences()
    {
        await OpenAsync(Url);

        var region = Page.Locator("#sb-announce .bit-srb-lvr");
        await Input("sb-announce").PressSequentiallyAsync("a", Typing);
        await Expect(region).ToHaveTextAsync(new Regex("^Keep typing \\(2\\)"));
        await Input("sb-announce").PressSequentiallyAsync("p", Typing);
        await Expect(region).ToHaveTextAsync(new Regex("^5 results for ap"));
    }

    [TestMethod]
    public async Task NoWrapNavigation_TheArrowsStopAtBothEnds()
    {
        await OpenAsync(Url);

        await Input("sb-nowrap").PressSequentiallyAsync("app", Typing);
        var options = await OptionsAsync("sb-nowrap");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(options.First).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(options.First).ToHaveAttributeAsync("aria-selected", "true");
        for (var i = 0; i < 4; i++) await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(options.Nth(3)).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod]
    public async Task ReadOnly_TheListCanBeBrowsedButNotPicked()
    {
        await OpenAsync(Url);

        await Input("sb-suggest-readonly").ClickAsync();
        var options = await OptionsAsync("sb-suggest-readonly");
        // The list is rendered a moment before it is shown, which is when a click can reach a row of it.
        await Expect(await ListboxAsync("sb-suggest-readonly")).ToBeVisibleAsync();
        await Expect(options.First).ToHaveAttributeAsync("aria-disabled", "true");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(options.First).ToHaveAttributeAsync("aria-selected", "true");
        // aria-disabled, not disabled: the row stays in the accessibility tree, and a press on it is refused.
        await options.Nth(1).ClickAsync(new() { Force = true });
        await Expect(Input("sb-suggest-readonly")).ToHaveValueAsync("Apple");
    }

    [TestMethod]
    public async Task SuggestItemsAriaLabel_NamesTheList()
    {
        await OpenAsync(Url);

        await Input("sb-suggest-arialabel").PressSequentiallyAsync("a", Typing);
        await Expect(await ListboxAsync("sb-suggest-arialabel")).ToHaveAttributeAsync("aria-label", "Matching fruits");
    }


    // ---------- Focus shortcut ----------

    [TestMethod]
    public async Task FocusShortcut_MovesTheFocusInFromAnywhereAndIsAnnounced()
    {
        await OpenAsync(Url);

        await Expect(Input("sb-shortcut")).ToHaveAttributeAsync("aria-keyshortcuts", "Alt+K");
        await Page.Locator("h1").ClickAsync();
        await Page.Keyboard.PressAsync("Alt+K");
        await Expect(Input("sb-shortcut")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task FocusShortcut_ABareKeyNeverStealsACharacterBeingTypedElsewhere()
    {
        await OpenAsync(Url);

        await Page.Locator("#other-input").ClickAsync();
        await Page.Keyboard.PressAsync("/");
        await Expect(Page.Locator("#other-input")).ToHaveValueAsync("/");
        await Expect(Page.Locator("#other-input")).ToBeFocusedAsync();

        await Page.Locator("h1").ClickAsync();
        await Page.Keyboard.PressAsync("/");
        var input = Input("sb-shortcut-bare");
        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveValueAsync("previous term");
        await Expect(input).ToHaveJSPropertyAsync("selectionEnd", "previous term".Length);
    }

    [TestMethod]
    public async Task FocusShortcut_ChangedAtRuntime_ReplacesThePreviousOne()
    {
        await OpenAsync(Url);

        await Page.Locator("h1").ClickAsync();
        await Page.Keyboard.PressAsync("Alt+H");
        await Expect(Input("sb-shortcut-dyn")).ToBeFocusedAsync();

        await Page.ClickAsync("#btn-change-shortcut");
        await Expect(Input("sb-shortcut-dyn")).ToHaveAttributeAsync("aria-keyshortcuts", "Alt+J");
        await Page.Locator("h1").ClickAsync();
        await Page.Keyboard.PressAsync("Alt+H");
        await Expect(Input("sb-shortcut-dyn")).Not.ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Alt+J");
        await Expect(Input("sb-shortcut-dyn")).ToBeFocusedAsync();
    }


    // ---------- Programmatic control ----------

    [TestMethod]
    public async Task PublicApi_FocusShowHideClearAndIsSuggestItemsOpen()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-api-show");
        await Expect(Text("api-open")).ToHaveTextAsync("True");
        await Expect(await OptionsAsync("sb-api")).ToHaveTextAsync(["Apple", "Red Apple", "Blue Apple", "Green Apple"]);
        await Expect(Input("sb-api")).Not.ToBeFocusedAsync();

        // The overlay of the open list covers the page, so the API buttons are clicked from script while it is open.
        await Expect(Text("api-isopen-toggle")).ToHaveTextAsync("True");
        await Page.Locator("#btn-api-read").DispatchEventAsync("click");
        await Expect(Text("api-isopen")).ToHaveTextAsync("True");

        await Page.Locator("#btn-api-hide").DispatchEventAsync("click");
        await Expect(Text("api-open")).ToHaveTextAsync("False");
        await Expect(Text("api-isopen-toggle")).ToHaveTextAsync("False");
        await Page.ClickAsync("#btn-api-read");
        await Expect(Text("api-isopen")).ToHaveTextAsync("False");

        await Page.ClickAsync("#btn-api-clear");
        await Expect(Input("sb-api")).ToHaveValueAsync("");
        await Expect(Text("api-clears")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-api-focus");
        await Expect(Input("sb-api")).ToBeFocusedAsync();
    }


    // ---------- Validation ----------

    [TestMethod]
    public async Task EditForm_AFailingValueMarksTheFieldAndShowsTheMessage()
    {
        await OpenAsync(Url);

        await Input("sb-form").PressSequentiallyAsync("a", Typing);
        await Expect(Root("sb-form")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("sb-form")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#validation .validation-message", new() { HasText = "Text must be between 2 and 6 chars." })).ToBeVisibleAsync();
        await Expect(Box("sb-form")).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-err)"));

        await Input("sb-form").PressSequentiallyAsync("b", Typing);
        await Expect(Root("sb-form")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await ExpectNoAttributeAsync(Input("sb-form"), "aria-invalid");

        await Input("sb-form-novalidate").PressSequentiallyAsync("abc", Typing);
        await Expect(Root("sb-form-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task ErrorMessage_IsShownReferencedAndAnnounced()
    {
        await OpenAsync(Url);

        await Input("sb-errmsg").PressSequentiallyAsync("ab", Typing);
        var message = Page.Locator("#sb-errmsg .bit-srb-erm");
        await Expect(message).ToHaveTextAsync("Type at least 3 characters.");
        await Expect(Root("sb-errmsg")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Page.Locator("#sb-errmsg .bit-srb-elv")).ToHaveTextAsync("Type at least 3 characters.");
        var ids = $"{await message.GetAttributeAsync("id")} {await Page.Locator("#sb-errmsg .bit-srb-des").GetAttributeAsync("id")}";
        await Expect(Input("sb-errmsg")).ToHaveAttributeAsync("aria-describedby", ids);

        await Input("sb-errmsg").PressSequentiallyAsync("c", Typing);
        await Expect(message).ToHaveCountAsync(0);
        await Expect(Root("sb-errmsg")).Not.ToHaveClassAsync(HasClass("bit-inv"));
    }


    // ---------- Cascading parameters ----------

    [TestMethod]
    public async Task BitParams_CascadesItsDefaults_AndASearchBoxKeepsWhatItSetsItself()
    {
        await OpenAsync(Url);

        var root = Root("sb-casc-1");
        await Expect(root).ToHaveClassAsync(HasClass("bit-srb-fic"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-srb-flw"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-srb-sm"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-srb-und"));

        // Immediate, MinSuggestTriggerChars 1, the highlight, the list's name and the no-results text all cascade.
        await Input("sb-casc-1").PressSequentiallyAsync("a", Typing);
        var listbox = await ListboxAsync("sb-casc-1");
        await Expect(listbox).ToHaveAttributeAsync("aria-label", "Matching items");
        await Expect(listbox.Locator("[role=option]").First.Locator("mark.bit-srb-hlt")).ToHaveTextAsync("A");
        await Input("sb-casc-1").PressSequentiallyAsync("xyz", Typing);
        await Expect((await CalloutAsync("sb-casc-1")).Locator(".bit-srb-nrs")).ToHaveTextAsync("No matching item found.");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Root("sb-casc-own")).Not.ToHaveClassAsync(HasClass("bit-srb-und"));
        await Expect(Root("sb-casc-own")).ToHaveClassAsync(HasClass("bit-srb-sm"));

        var outside = Root("sb-casc-outside");
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-srb-fic"));
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-srb-sm"));
    }


    // ---------- Color, icons, size ----------

    [TestMethod]
    [DataRow("primary", "pri", "--bit-clr-pri")]
    [DataRow("secondary", "sec", "--bit-clr-sec")]
    [DataRow("tertiary", "ter", "--bit-clr-ter")]
    [DataRow("info", "inf", "--bit-clr-inf")]
    [DataRow("success", "suc", "--bit-clr-suc")]
    [DataRow("warning", "wrn", "--bit-clr-wrn")]
    [DataRow("severewarning", "swr", "--bit-clr-swr")]
    [DataRow("error", "err", "--bit-clr-err")]
    [DataRow("primarybackground", "pbg", "--bit-clr-bg-pri")]
    [DataRow("secondarybackground", "sbg", "--bit-clr-bg-sec")]
    [DataRow("tertiarybackground", "tbg", "--bit-clr-bg-ter")]
    [DataRow("primaryforeground", "pfg", "--bit-clr-fg-pri")]
    [DataRow("secondaryforeground", "sfg", "--bit-clr-fg-sec")]
    [DataRow("tertiaryforeground", "tfg", "--bit-clr-fg-ter")]
    [DataRow("primaryborder", "pbr", "--bit-clr-brd-pri")]
    [DataRow("secondaryborder", "sbr", "--bit-clr-brd-sec")]
    [DataRow("tertiaryborder", "tbr", "--bit-clr-brd-ter")]
    public async Task Color_PaintsTheIconAndTheSearchButton(string color, string suffix, string token)
    {
        await OpenAsync(Url);

        var id = $"sb-clr-{color}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-srb-{suffix}"));
        await Expect(Root("sb-clr-none")).Not.ToHaveClassAsync(HasClass($"bit-srb-{suffix}"));

        var expected = await ColorOfAsync(token);
        await Expect(Page.Locator($"#{id} .bit-srb-iwp i")).ToHaveCSSAsync("color", expected);
        await Expect(Page.Locator($"#{id} .bit-srb-sbn")).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", $"var({token})"));
    }

    [TestMethod]
    public async Task ExternalIcons_ForTheIconTheSearchButtonAndTheClearButton()
    {
        await OpenAsync(Url);

        var css = Page.Locator("#sb-ext-css .bit-srb-iwp i");
        await Expect(css).ToHaveClassAsync(HasClass("my-sb-icon"));
        Assert.AreEqual("\"S\"", await css.EvaluateAsync<string>("el => getComputedStyle(el, '::before').content"));
        await Expect(Page.Locator("#sb-ext-fa .bit-srb-iwp i")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid fa-house(\s|$)"));
        await Expect(Page.Locator("#sb-ext-bi .bit-srb-iwp i")).ToHaveClassAsync(new Regex(@"(^|\s)bi bi-gear-fill(\s|$)"));
        await Expect(Page.Locator("#sb-ext-buttons .bit-srb-sbn i")).ToHaveClassAsync(HasClass("my-sb-go"));
        await Expect(Page.Locator("#sb-ext-buttons .bit-srb-sbn i")).Not.ToHaveClassAsync(HasClass("bit-srb-dsi"));
        await Expect(Page.Locator("#sb-ext-buttons .bit-srb-cbt i")).ToHaveClassAsync(HasClass("my-sb-clear"));
    }

    [TestMethod]
    [DataRow("small", "bit-srb-sm", "--bit-siz-ctrl-sm", "--bit-tpg-fs-xs")]
    [DataRow("medium", "bit-srb-md", "--bit-siz-ctrl-md", "--bit-tpg-fs-sm")]
    [DataRow("large", "bit-srb-lg", "--bit-siz-ctrl-lg", "--bit-tpg-fs-md")]
    public async Task Size_ScalesTheFieldTheTextAndTheList(string size, string cssClass, string heightToken, string fontToken)
    {
        await OpenAsync(Url);

        var id = $"sb-size-{size}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));
        await Expect(Box(id)).ToHaveCSSAsync("height", await ResolveAsync("height", SizeToken(heightToken)));
        await Expect(Box(id)).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", $"var({fontToken})"));

        // The relocated list carries the size class of its own.
        await Input(id).PressSequentiallyAsync("a", Typing);
        var callout = await CalloutAsync(id);
        await Expect(callout).ToHaveClassAsync(HasClass(cssClass));
        await Expect(callout).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", $"var({fontToken})"));
    }


    // ---------- Style and Class ----------

    [TestMethod]
    public async Task StyleClassStylesAndClasses_ReachTheirParts()
    {
        await OpenAsync(Url);

        StringAssert.Contains(await ComputedStyleAsync("#sb-style", "box-shadow"), "rgb(30, 144, 255)");
        await Expect(Root("sb-class")).ToHaveClassAsync(HasClass("custom-class"));

        await Expect(Root("sb-styles")).ToHaveCSSAsync("margin-left", "16px");
        await Expect(Input("sb-styles")).ToHaveCSSAsync("padding-left", "9px");
        await Expect(Box("sb-styles")).ToHaveCSSAsync("background-color", "rgb(200, 200, 200)");
        await Input("sb-styles").FocusAsync();
        await Expect(Box("sb-styles")).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");

        await Expect(Root("sb-classes")).ToHaveClassAsync(HasClass("custom-root"));
        await Expect(Page.Locator("#sb-classes .bit-srb-iwp")).ToHaveClassAsync(HasClass("custom-icon-wrapper"));
        await Expect(Page.Locator("#sb-classes .bit-srb-iwp i")).ToHaveClassAsync(HasClass("custom-icon"));
        await Expect(Page.Locator("#sb-classes .bit-srb-cbt")).ToHaveClassAsync(HasClass("custom-clear"));
        await Expect(Box("sb-classes")).ToHaveClassAsync(HasClass("custom-input-container"));
        await Expect(Root("sb-classes")).Not.ToHaveClassAsync(HasClass("custom-focused"));
        await Input("sb-classes").FocusAsync();
        await Expect(Root("sb-classes")).ToHaveClassAsync(HasClass("custom-focused"));
    }

    [TestMethod]
    public async Task CalloutPartsClassesAndStyles_ReachTheRelocatedList()
    {
        await OpenAsync(Url);

        await Input("sb-callout-classes").PressSequentiallyAsync("app", Typing);
        var callout = await CalloutAsync("sb-callout-classes");
        await Expect(callout).ToHaveClassAsync(HasClass("custom-callout"));
        await Expect(callout).ToHaveCSSAsync("background-color", "rgb(250, 240, 230)");
        var option = (await OptionsAsync("sb-callout-classes")).First;
        await Expect(option).ToHaveClassAsync(HasClass("custom-suggest-item"));
        await Expect(option.Locator("mark")).ToHaveClassAsync(HasClass("custom-highlight"));
    }

    [TestMethod]
    public async Task CssVariables_OnTheStyle_ReskinTheFieldAndReachTheRelocatedList()
    {
        await OpenAsync(Url);

        var box = Box("sb-cssvar-style");
        await Expect(box).ToHaveCSSAsync("border-top-left-radius", "20px");
        await Expect(box).ToHaveCSSAsync("height", "44px");
        await Expect(box).ToHaveCSSAsync("border-top-color", "rgb(221, 214, 254)");
        await Expect(Page.Locator("#sb-cssvar-style .bit-srb-iwp i")).ToHaveCSSAsync("color", "rgb(109, 40, 217)");

        await Input("sb-cssvar-style").PressSequentiallyAsync("app", Typing);
        var option = (await OptionsAsync("sb-cssvar-style")).First;
        await Expect(option).ToBeVisibleAsync();
        await Expect(option).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        Assert.IsTrue(await option.EvaluateAsync<bool>("el => !document.getElementById('sb-cssvar-style').contains(el)"), "The list has been relocated out of the root.");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_ReachTheFieldAndTheList_AndAnExplicitParameterWins()
    {
        await OpenAsync(Url);

        await Expect(Box("sb-cssvar-ancestor")).ToHaveCSSAsync("background-color", "rgb(4, 5, 6)");
        await Input("sb-cssvar-ancestor").PressSequentiallyAsync("app", Typing);
        var callout = await CalloutAsync("sb-cssvar-ancestor");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(callout).ToHaveCSSAsync("background-color", "rgb(7, 8, 9)");
        await Page.Keyboard.PressAsync("Escape");

        var explicitBox = Box("sb-cssvar-explicit");
        await Expect(explicitBox).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-bg-sec)"));
        await Expect(explicitBox).ToHaveCSSAsync("height", await ResolveAsync("height", SizeToken("--bit-siz-ctrl-sm")));
    }

    [TestMethod]
    public async Task FocusAndHover_DrawTheRingAndDarkenTheBorder()
    {
        await OpenAsync(Url);

        var box = Box("sb-default-look");
        await Expect(box).ToHaveCSSAsync("box-shadow", "none");
        await box.HoverAsync();
        await Expect(box).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-brd-pri-hover)"));
        await Input("sb-default-look").FocusAsync();
        await Expect(box).Not.ToHaveCSSAsync("box-shadow", "none");
    }


    // ---------- RTL ----------

    [TestMethod]
    public async Task Rtl_MirrorsTheFieldTheSearchArrowAndTheList()
    {
        await OpenAsync(Url);

        await Expect(Root("sb-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("sb-rtl")).ToHaveAttributeAsync("dir", "rtl");

        var ltrInput = await BoxOfAsync(Input("sb-ltr"));
        Assert.IsTrue((await BoxOfAsync(Page.Locator("#sb-ltr .bit-srb-iwp"))).X < ltrInput.X, "LTR: the icon on the left.");
        Assert.AreEqual("none", await ComputedStyleAsync("#sb-ltr .bit-srb-sbn i", "transform"));

        var input = await BoxOfAsync(Input("sb-rtl"));
        var icon = await BoxOfAsync(Page.Locator("#sb-rtl .bit-srb-iwp"));
        var button = await BoxOfAsync(Page.Locator("#sb-rtl .bit-srb-sbn"));
        var prefix = await BoxOfAsync(Page.Locator("#sb-rtl .bit-srb-pre"));
        Assert.IsTrue(icon.X >= input.X + input.Width - 1, "RTL: the icon on the right.");
        Assert.IsTrue(prefix.X >= input.X + input.Width - 1, "RTL: the prefix on the right.");
        Assert.IsTrue(button.X + button.Width <= input.X + 1, "RTL: the search button on the left.");
        Assert.AreNotEqual("none", await ComputedStyleAsync("#sb-rtl .bit-srb-sbn i", "transform"), "The search arrow is mirrored.");

        await Input("sb-rtl").PressSequentiallyAsync("app", Typing);
        var callout = await CalloutAsync("sb-rtl");
        await Expect(callout).ToHaveAttributeAsync("dir", "rtl");
        await Expect((await OptionsAsync("sb-rtl")).First).ToHaveCSSAsync("direction", "rtl");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_LaysTheFieldOutTheSameWay()
    {
        await OpenAsync(Url);

        await Expect(Root("sb-rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        var input = await BoxOfAsync(Input("sb-rtl-inherited"));
        Assert.IsTrue((await BoxOfAsync(Page.Locator("#sb-rtl-inherited .bit-srb-iwp"))).X >= input.X + input.Width - 1);
        Assert.IsTrue((await BoxOfAsync(Page.Locator("#sb-rtl-inherited .bit-srb-sbn"))).X < input.X);
        Assert.AreNotEqual("none", await ComputedStyleAsync("#sb-rtl-inherited .bit-srb-sbn i", "transform"), "The search arrow is mirrored there too.");
    }


    // ---------- Reduced motion ----------

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheIconTransition_UnlessForceAnimation()
    {
        await OpenAsync(Url);

        Assert.IsTrue(MaxSeconds(await ComputedStyleAsync("#sb-motion .bit-srb-iwp", "transition-duration")) > 0.01);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        var reduced = await ComputedStyleAsync("#sb-motion .bit-srb-iwp", "transition-duration");
        Assert.IsTrue(MaxSeconds(reduced) < 0.011, reduced);

        await Expect(Root("sb-motion-force")).ToHaveClassAsync(HasClass("bit-fam"));
        var forced = await ComputedStyleAsync("#sb-motion-force .bit-srb-iwp", "transition-duration");
        Assert.IsTrue(MaxSeconds(forced) > 0.01, forced);
    }

    private static double MaxSeconds(string durations) =>
        durations.Split(',').Select(d => d.Trim()).Select(d => d.EndsWith("ms") ? double.Parse(d[..^2], System.Globalization.CultureInfo.InvariantCulture) / 1000
                                                                                : double.Parse(d.TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture)).Max();
}
