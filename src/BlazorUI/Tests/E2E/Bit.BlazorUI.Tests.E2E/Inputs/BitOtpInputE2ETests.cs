using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// Drives BitOtpInput (/e2e/otp-input and /e2e/otp-input/autofocus) in a real browser: every section of its demo page
/// and every parameter of its table - typing that moves the focus box by box, Backspace and Delete with and without
/// AutoShift, the arrows, Home and End, AutoFocus, BlurOnFill, Sequential, disabled and read-only, label and
/// description, every Type with its keyboard, NormalizeDigits, Placeholder and Mask, Pattern with Uppercase and
/// Lowercase, the separators, the three variants, Reversed, Vertical, FullWidth and Merged, paste (cleaned up,
/// transformed, landing where it belongs), copy and cut, the SMS auto fill opt-out, one-way and two-way binding,
/// every event with its index, the public methods, EditForm validation and AutoSubmit, IsLoading and Invalid with
/// their announcements, the group's accessibility and SingleTabStop, the BitParams cascade, Accent and Size,
/// Style/Class/Styles/Classes and the public CSS variables with their variant tiers, RTL (explicit and inherited),
/// reduced motion, prerendering and unmounting.
/// </summary>
[TestClass]
public class BitOtpInputE2ETests : E2ETestBase
{
    private const string Url = "/e2e/otp-input";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Inputs(string id) => Page.Locator($"#{id} input.bit-otp-inp");

    private ILocator Box(string id, int index) => Inputs(id).Nth(index);

    private ILocator Group(string id) => Page.Locator($"#{id} [role=group]");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    private Task BlurAsync() => Page.Locator("h1").ClickAsync();

    private static Task ExpectNoAttributeAsync(ILocator locator, string name) =>
        Expect(locator).Not.ToHaveAttributeAsync(name, new Regex("[\\s\\S]*"));

    // A code is typed one character at a time, the way a person types it: each keystroke lands, and the component
    // moves the focus on, before the next one is pressed.
    private async Task TypeCodeAsync(string id, string code, int from = 0)
    {
        var length = await Inputs(id).CountAsync();

        for (var i = 0; i < code.Length; i++)
        {
            var index = from + i;
            await Expect(Box(id, index)).ToBeFocusedAsync();
            await Page.Keyboard.TypeAsync(code[i].ToString());

            if (index + 1 < length)
            {
                await Expect(Box(id, index + 1)).ToBeFocusedAsync();
            }
        }
    }

    private async Task StartTypingAsync(string id, int index = 0)
    {
        await Box(id, index).ClickAsync();
        await Expect(Box(id, index)).ToBeFocusedAsync();
    }

    // A paste is dispatched with a clipboard of its own: Playwright cannot write the system clipboard of every engine.
    // Firefox hands a synthetic paste event no clipboard data to read, so the tests that paste skip there.
    private static Task PasteAsync(ILocator input, string text)
    {
        if (IsFirefox) Assert.Inconclusive("Firefox gives a synthetic paste event no clipboard data, and Playwright cannot write its clipboard.");

        return input.EvaluateAsync(@"(el, text) => {
            el.focus();
            const data = new DataTransfer();
            data.setData('text/plain', text);
            el.dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
        }", text);
    }

    // What the component wrote onto the clipboard of the event is read back off that same event once it has bubbled
    // past the component's own listener on its root.
    private static Task<ClipboardResult> ClipboardAsync(ILocator input, string type) =>
        input.EvaluateAsync<ClipboardResult>(@"(el, type) => {
            el.focus();
            let written = '';
            const read = ev => { written = ev.clipboardData ? ev.clipboardData.getData('text/plain') : ''; };
            document.addEventListener(type, read);
            const e = new ClipboardEvent(type, { clipboardData: new DataTransfer(), bubbles: true, cancelable: true });
            el.dispatchEvent(e);
            document.removeEventListener(type, read);
            return { data: written, prevented: e.defaultPrevented };
        }", type);

    private sealed class ClipboardResult
    {
        public string Data { get; set; } = "";

        public bool Prevented { get; set; }
    }

    private Task<string> ResolveAsync(string property, string value) =>
        Page.EvaluateAsync<string>(@"([p, v]) => {
            const d = document.createElement('div');
            d.style.setProperty(p, v);
            document.body.appendChild(d);
            const r = getComputedStyle(d).getPropertyValue(p);
            d.remove();
            return r;
        }", new[] { property, value });

    private static string SizeToken(string token) =>
        $"var({token}, calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * var({token}-steps)))";

    private static async Task<LocatorBoundingBoxResult> BoxOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no box.");
        return box;
    }

    private async Task<string[]> ValuesAsync(string id) =>
        await Inputs(id).EvaluateAllAsync<string[]>("inputs => inputs.map(i => i.value)");


    // ---------- Lifecycle ----------

    [TestMethod]
    public async Task Prerender_TheServerHtmlCarriesTheBoxesBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"otp-basic\"");
        StringAssert.Contains(html, "bit-otp-inp");
        StringAssert.Contains(html, "id=\"status\">Loading<");

        await OpenAsync(Url);
        await Expect(Inputs("otp-basic")).ToHaveCountAsync(5);
    }

    [TestMethod]
    public async Task Unmount_RemovingABusyOtpInput_LeavesNoError()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#otp-mount .bit-otp-ldr")).ToBeVisibleAsync();
        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Root("otp-mount")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Inputs("otp-mount")).ToHaveCountAsync(4);
        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Root("otp-mount")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task AutoFocus_LandsInTheFirstEmptyBox()
    {
        await OpenAsync("/e2e/otp-input/autofocus");

        await Expect(Box("otp-autofocus", 2)).ToBeFocusedAsync();
        await Page.Keyboard.TypeAsync("3");
        await Expect(Box("otp-autofocus", 3)).ToBeFocusedAsync();
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "", "" }, await ValuesAsync("otp-autofocus"));
    }


    // ---------- Typing and keys ----------

    [TestMethod]
    public async Task Typing_EachCharacterFillsABoxAndMovesTheFocusOn_AndAFullCodeRaisesOnFill()
    {
        await OpenAsync(Url);

        await Expect(Inputs("otp-basic")).ToHaveCountAsync(5);
        await StartTypingAsync("otp-basic");
        await TypeCodeAsync("otp-basic", "1234");
        await Expect(Text("basic-value")).ToHaveTextAsync("[1234]");
        await Expect(Box("otp-basic", 0)).ToHaveClassAsync(HasClass("bit-otp-fld"));
        await Expect(Box("otp-basic", 4)).Not.ToHaveClassAsync(HasClass("bit-otp-fld"));
        await Expect(Text("fill-count")).ToHaveTextAsync("0");

        await TypeCodeAsync("otp-basic", "5", from: 4);
        await Expect(Text("basic-value")).ToHaveTextAsync("[12345]");
        await Expect(Text("fill-count")).ToHaveTextAsync("1");
        await Expect(Text("fill-value")).ToHaveTextAsync("[12345]");
        await Expect(Box("otp-basic", 4)).ToBeFocusedAsync();

        // A box that takes the focus selects its character, so typing replaces it - and a changed code is a new fill.
        await Box("otp-basic", 2).ClickAsync();
        await Expect(Box("otp-basic", 2)).ToHaveJSPropertyAsync("selectionEnd", 1);
        await Page.Keyboard.TypeAsync("9");
        await Expect(Text("basic-value")).ToHaveTextAsync("[12945]");
        await Expect(Text("fill-count")).ToHaveTextAsync("2");

        await Expect(Inputs("otp-length4")).ToHaveCountAsync(4);
    }

    [TestMethod]
    public async Task Backspace_ClearsTheBoxItIsPressedIn_OnAnEmptyBoxTheOneBeforeIt()
    {
        await OpenAsync(Url);

        await StartTypingAsync("otp-noshift", 4);
        await Page.Keyboard.PressAsync("Backspace");
        await Expect(Text("noshift-value")).ToHaveTextAsync("[1234]");
        await Expect(Box("otp-noshift", 4)).ToHaveValueAsync("");
        await Expect(Box("otp-noshift", 4)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Backspace");
        await Expect(Text("noshift-value")).ToHaveTextAsync("[123]");
        await Expect(Box("otp-noshift", 3)).ToHaveValueAsync("");
        await Expect(Box("otp-noshift", 3)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Delete_ClearsInPlace_AndAutoShiftPullsTheRestOfTheCodeAlong()
    {
        await OpenAsync(Url);

        await StartTypingAsync("otp-noshift", 1);
        await Page.Keyboard.PressAsync("Delete");
        await Expect(Box("otp-noshift", 1)).ToHaveValueAsync("");
        await Expect(Box("otp-noshift", 1)).ToBeFocusedAsync();
        await Expect(Text("noshift-value")).ToHaveTextAsync("[1345]");
        CollectionAssert.AreEqual(new[] { "1", "", "3", "4", "5" }, await ValuesAsync("otp-noshift"));

        await StartTypingAsync("otp-autoshift", 1);
        await Page.Keyboard.PressAsync("Delete");
        await Expect(Text("autoshift-value")).ToHaveTextAsync("[1345]");
        CollectionAssert.AreEqual(new[] { "1", "3", "4", "5", "" }, await ValuesAsync("otp-autoshift"));

        await Page.Keyboard.PressAsync("Backspace");
        await Expect(Text("autoshift-value")).ToHaveTextAsync("[145]");
        CollectionAssert.AreEqual(new[] { "1", "4", "5", "", "" }, await ValuesAsync("otp-autoshift"));
    }

    [TestMethod]
    public async Task Arrow_FollowedAtOnceByTab_NeverPullsTheFocusBack()
    {
        await OpenAsync(Url);

        // The direction of a Dir left unset is read in a round trip before the arrow moves the focus. A Tab pressed
        // while it is out has taken the focus on by the time it comes back, and the arrow must not pull it back.
        await Page.EvaluateAsync(@"() => {
            const utils = BitBlazorUI.Utils;
            const isRtl = utils.isRtl;
            window.__isRtlCalls = 0;
            utils.isRtl = function (...args) { window.__isRtlCalls++; return isRtl.apply(this, args); };
        }");

        await StartTypingAsync("otp-noshift", 2);
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Page.Keyboard.PressAsync("Tab");

        // Once the direction has been asked for, the answer is on its way, and an event raised after it reaches the
        // page behind it: by the time that event has rendered, the arrow has done whatever it was going to do. A
        // script's click() moves no focus of its own.
        await Page.WaitForFunctionAsync("() => window.__isRtlCalls > 0");
        await Page.Locator("#btn-set-oneway").EvaluateAsync("b => b.click()");
        await Expect(Text("oneway-value")).ToHaveTextAsync("[98765]");
        await NextFramesAsync();

        // Either the arrow landed before the Tab (box 1, then the Tab on to box 2) or the Tab overtook it and the
        // arrow was dropped (the Tab from box 2 on to box 3); never back on box 1.
        var focused = await Inputs("otp-noshift").EvaluateAllAsync<int>("els => els.indexOf(document.activeElement)");
        Assert.IsTrue(focused is 2 or 3, $"The focus was pulled back to box {focused}.");
    }

    [TestMethod]
    public async Task Arrows_HomeAndEnd_WalkTheCode()
    {
        await OpenAsync(Url);

        await StartTypingAsync("otp-noshift", 2);
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Box("otp-noshift", 3)).ToBeFocusedAsync();
        // Each arrow moves the focus through a round trip, so the next one is pressed once it has landed.
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Box("otp-noshift", 2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Box("otp-noshift", 1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Box("otp-noshift", 4)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Box("otp-noshift", 4)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Box("otp-noshift", 0)).ToBeFocusedAsync();

        // The vertical pair means nothing in a row.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Box("otp-noshift", 0)).ToBeFocusedAsync();
        await Expect(Text("noshift-value")).ToHaveTextAsync("[12345]");
    }

    [TestMethod]
    public async Task BlurOnFill_DropsTheFocusOnTheLastCharacter()
    {
        await OpenAsync(Url);

        await StartTypingAsync("otp-blur");
        await TypeCodeAsync("otp-blur", "123");
        await Page.Keyboard.TypeAsync("4");
        await Expect(Box("otp-blur", 3)).ToHaveValueAsync("4");
        await Page.WaitForFunctionAsync("() => !document.getElementById('otp-blur').contains(document.activeElement)");
    }

    [TestMethod]
    public async Task Sequential_PullsAClickPastTheFirstEmptyBoxBackToIt()
    {
        await OpenAsync(Url);

        await Box("otp-sequential", 4).ClickAsync();
        await Expect(Box("otp-sequential", 0)).ToBeFocusedAsync();
        await TypeCodeAsync("otp-sequential", "1");

        await Box("otp-sequential", 5).ClickAsync();
        await Expect(Box("otp-sequential", 1)).ToBeFocusedAsync();
        await Expect(Text("sequential-value")).ToHaveTextAsync("[1]");
    }

    [TestMethod]
    public async Task AClickOnTheGapOfTheRow_FocusesTheFirstEmptyBox()
    {
        await OpenAsync(Url);

        var first = await BoxOfAsync(Box("otp-length4", 0));
        var second = await BoxOfAsync(Box("otp-length4", 1));
        await Page.Mouse.ClickAsync((first.X + first.Width + second.X) / 2, first.Y + first.Height / 2);
        await Expect(Box("otp-length4", 0)).ToBeFocusedAsync();
    }


    // ---------- Disabled and read-only ----------

    [TestMethod]
    public async Task Disabled_RendersTheDisabledMarkupAndRunsNoActivationHandler()
    {
        await OpenAsync(Url);

        await Expect(Root("otp-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        for (var i = 0; i < 5; i++)
        {
            await Expect(Box("otp-disabled", i)).ToBeDisabledAsync();
            await Expect(Box("otp-disabled", i)).ToHaveAttributeAsync("aria-disabled", "true");
        }
        await Expect(Page.Locator("#otp-disabled input[type=hidden]")).ToBeDisabledAsync();
        await Expect(Box("otp-disabled", 0)).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-bg-dis)"));

        await Root("otp-disabled").ClickAsync(new() { Position = new() { X = 2, Y = 2 } });
        await Box("otp-disabled", 1).ClickAsync(new() { Force = true });
        await Root("otp-disabled").DispatchEventAsync("click");
        await Root("otp-disabled").HoverAsync();
        await Expect(Text("disabled-root-hovers")).Not.ToHaveTextAsync("0");
        await Expect(Text("disabled-root-clicks")).ToHaveTextAsync("0");
        await Expect(Text("disabled-focuses")).ToHaveTextAsync("0");

        await Page.Locator("#before-disabled").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-disabled")).ToBeFocusedAsync();

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(Root("otp-disabled")).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Box("otp-disabled", 1).ClickAsync();
        await Expect(Text("disabled-root-clicks")).ToHaveTextAsync("1");
        await Expect(Text("disabled-focuses")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#otp-disabled input[type=hidden]")).ToBeEnabledAsync();
    }

    [TestMethod]
    public async Task ReadOnly_ShowsTheCodeButRefusesEveryEdit_WhileTheArrowsStillMove()
    {
        await OpenAsync(Url);

        await Expect(Root("otp-readonly")).ToHaveClassAsync(HasClass("bit-otp-rdl"));
        await Expect(Box("otp-readonly", 0)).ToHaveAttributeAsync("readonly", "");

        await StartTypingAsync("otp-readonly", 1);
        await Page.Keyboard.TypeAsync("9");
        await Page.Keyboard.PressAsync("Backspace");
        await Page.Keyboard.PressAsync("Delete");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Box("otp-readonly", 2)).ToBeFocusedAsync();
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "4", "5" }, await ValuesAsync("otp-readonly"));
        await Expect(Text("readonly-changes")).ToHaveTextAsync("0");
    }


    // ---------- Base parameters ----------

    [TestMethod]
    public async Task BaseParameters_IdClassStyleAttributesAndTabIndex()
    {
        await OpenAsync(Url);

        var root = Root("otp-base");
        await Expect(root).ToHaveClassAsync(HasClass("my-root-class"));
        await Expect(root).ToHaveCSSAsync("margin-top", "7px");
        await Expect(root).ToHaveAttributeAsync("data-test", "base-attr");
        await Expect(Box("otp-base", 2)).ToHaveAttributeAsync("data-input-attr", "yes");
        await Expect(Box("otp-base", 2)).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Box("otp-base", 0)).ToHaveAttributeAsync("autocorrect", "off");
        await Expect(Box("otp-base", 0)).ToHaveAttributeAsync("spellcheck", "false");
        await Expect(Box("otp-base", 0)).ToHaveAttributeAsync("autocapitalize", "none");
        await ExpectNoAttributeAsync(Box("otp-base", 0), "maxlength");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheSpace_CollapsedRemovesIt()
    {
        await OpenAsync(Url);

        var root = Root("otp-visibility");
        await Page.ClickAsync("#btn-vis-hidden");
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await BoxOfAsync(root)).Height > 20);
        await Page.ClickAsync("#btn-vis-collapsed");
        await Expect(root).ToHaveCSSAsync("display", "none");
        await Page.ClickAsync("#btn-vis-visible");
        await Expect(root).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Rerender_ALengthChangedAtRuntimeKeepsTheTypedCharacters()
    {
        await OpenAsync(Url);

        await Expect(Inputs("otp-rerender")).ToHaveCountAsync(4);
        await Page.ClickAsync("#btn-change-params");
        await Expect(Inputs("otp-rerender")).ToHaveCountAsync(6);
        await Expect(Page.Locator("#otp-rerender .bit-otp-lbl")).ToHaveTextAsync("Second label");
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "4", "", "" }, await ValuesAsync("otp-rerender"));
        await Expect(Box("otp-rerender", 5)).ToHaveAttributeAsync("aria-label", "6 of 6");

        await StartTypingAsync("otp-rerender", 4);
        await TypeCodeAsync("otp-rerender", "56", from: 4);
        await Expect(Text("rerender-value")).ToHaveTextAsync("[123456]");
    }


    // ---------- Label and description ----------

    [TestMethod]
    public async Task Label_NamesTheGroupAndFocusesTheFirstBox()
    {
        await OpenAsync(Url);

        var label = Page.Locator("#otp-label .bit-otp-lbl");
        await Expect(label).ToHaveTextAsync("OTP");
        await Expect(label).ToHaveAttributeAsync("for", (await Box("otp-label", 0).GetAttributeAsync("id"))!);
        await Expect(Group("otp-label")).ToHaveAttributeAsync("aria-labelledby", (await label.GetAttributeAsync("id"))!);
        await label.ClickAsync();
        await Expect(Box("otp-label", 0)).ToBeFocusedAsync();

        await Expect(Page.Locator("#otp-label-template .bit-otp-lbl .tpl-label")).ToHaveTextAsync("Custom label");
    }

    [TestMethod]
    public async Task Required_MarksEveryBoxAndTheLabel()
    {
        await OpenAsync(Url);

        await Expect(Root("otp-required")).ToHaveClassAsync(HasClass("bit-otp-req"));
        for (var i = 0; i < 5; i++)
        {
            await Expect(Box("otp-required", i)).ToHaveAttributeAsync("required", "");
        }
        StringAssert.StartsWith(await Page.Locator("#otp-required .bit-otp-lbl").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content"), "\"*\"");
    }

    [TestMethod]
    public async Task Description_IsReferencedByTheGroupOnly()
    {
        await OpenAsync(Url);

        var description = Page.Locator("#otp-desc .bit-otp-dsc");
        await Expect(description).ToHaveTextAsync("We sent a 6 digit code to +1 555 0100.");
        await Expect(Group("otp-desc")).ToHaveAttributeAsync("aria-describedby", (await description.GetAttributeAsync("id"))!);
        await ExpectNoAttributeAsync(Box("otp-desc", 0), "aria-describedby");

        await Expect(Page.Locator("#otp-desc-template .bit-otp-dsc .tpl-desc")).ToHaveTextAsync("Didn't get it? Send it again");
    }


    // ---------- Type and keyboard ----------

    [TestMethod]
    [DataRow("text", "text", "text", "a")]
    [DataRow("number", "text", "numeric", "7")]
    [DataRow("password", "password", "text", "a")]
    [DataRow("email", "text", "email", "a")]
    [DataRow("tel", "tel", "tel", "5")]
    [DataRow("url", "text", "url", "a")]
    public async Task Type_PicksTheElementTheKeyboardAndTheCharacters(string type, string htmlType, string inputMode, string accepted)
    {
        await OpenAsync(Url);

        var id = $"otp-type-{type}";
        await Expect(Box(id, 0)).ToHaveAttributeAsync("type", htmlType);
        await Expect(Box(id, 0)).ToHaveAttributeAsync("inputmode", inputMode);

        await StartTypingAsync(id);
        await Page.Keyboard.TypeAsync(accepted);
        await Expect(Box(id, 0)).ToHaveValueAsync(accepted);
        await Expect(Box(id, 1)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Number_RejectsAnythingButADigitAndReportsIt()
    {
        await OpenAsync(Url);

        await StartTypingAsync("otp-type-number");
        await Page.Keyboard.TypeAsync("e");
        await Expect(Text("type-invalid")).ToHaveTextAsync("[e@0]");
        await Expect(Box("otp-type-number", 0)).ToHaveValueAsync("");
        await Expect(Box("otp-type-number", 0)).ToBeFocusedAsync();

        await Expect(Box("otp-tel-keypad", 0)).ToHaveAttributeAsync("inputmode", "tel");
        await Expect(Box("otp-tel-keypad", 0)).ToHaveAttributeAsync("type", "text");
    }

    [TestMethod]
    public async Task NormalizeDigits_FoldsOtherNumberingSystemsIntoAscii()
    {
        await OpenAsync(Url);

        await StartTypingAsync("otp-normalize");
        await Page.Keyboard.TypeAsync("۱");
        await Expect(Box("otp-normalize", 0)).ToHaveValueAsync("1");
        await Expect(Text("normalize-value")).ToHaveTextAsync("[1]");

        // A chunk of Arabic-Indic digits, written into a box the way an autofill writes it.
        await Box("otp-normalize", 0).FillAsync("١٢٣٤٥٦");
        await Expect(Text("normalize-value")).ToHaveTextAsync("[123456]");
    }


    // ---------- Mask and placeholder ----------

    [TestMethod]
    public async Task Placeholder_IsTheHintOfEveryEmptyBox_OrOneCharacterPerBox()
    {
        await OpenAsync(Url);

        for (var i = 0; i < 5; i++)
        {
            await Expect(Box("otp-placeholder", i)).ToHaveAttributeAsync("placeholder", "•");
        }

        for (var i = 0; i < 6; i++)
        {
            await Expect(Box("otp-placeholder-spread", i)).ToHaveAttributeAsync("placeholder", "123456"[i].ToString());
        }
    }

    [TestMethod]
    public async Task Mask_HidesTheCharactersButNotTheValue()
    {
        await OpenAsync(Url);

        await StartTypingAsync("otp-mask");
        await TypeCodeAsync("otp-mask", "12");
        await Expect(Box("otp-mask", 0)).ToHaveValueAsync("●");
        await Expect(Box("otp-mask", 1)).ToHaveValueAsync("●");
        await Expect(Text("mask-value")).ToHaveTextAsync("[12]");

        CollectionAssert.AreEqual(Enumerable.Repeat("🔒", 5).ToArray(), await ValuesAsync("otp-mask-emoji"));
    }


    // ---------- Pattern and casing ----------

    [TestMethod]
    public async Task Pattern_RejectsANonMatchingKeystroke()
    {
        await OpenAsync(Url);

        await StartTypingAsync("otp-hex");
        await Page.Keyboard.TypeAsync("g");
        await Expect(Text("hex-invalid")).ToHaveTextAsync("[g@0]");
        await Expect(Box("otp-hex", 0)).ToHaveValueAsync("");
        await Page.Keyboard.TypeAsync("F");
        await Expect(Text("hex-value")).ToHaveTextAsync("[F]");

        await StartTypingAsync("otp-upper-strict");
        await Page.Keyboard.TypeAsync("a");
        await Expect(Box("otp-upper-strict", 0)).ToHaveValueAsync("");
        await Page.Keyboard.TypeAsync("A");
        await Expect(Text("upper-strict-value")).ToHaveTextAsync("[A]");
    }

    [TestMethod]
    public async Task UppercaseAndLowercase_FoldTheCaseBeforeThePatternIsAsked()
    {
        await OpenAsync(Url);

        await StartTypingAsync("otp-upper");
        await TypeCodeAsync("otp-upper", "ab");
        await Expect(Text("upper-value")).ToHaveTextAsync("[AB]");
        await Expect(Box("otp-upper", 0)).ToHaveValueAsync("A");

        await StartTypingAsync("otp-lower");
        await TypeCodeAsync("otp-lower", "XY");
        await Expect(Text("lower-value")).ToHaveTextAsync("[xy]");
    }


    // ---------- Separator ----------

    [TestMethod]
    public async Task Separator_IsDrawnBetweenTheBoxesAndHiddenFromAssistiveTechnologies()
    {
        await OpenAsync(Url);

        var separators = Page.Locator("#otp-sep .bit-otp-sep");
        await Expect(separators).ToHaveCountAsync(5);
        await Expect(separators.First).ToHaveTextAsync("-");
        await Expect(separators.First).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(separators.First).ToHaveCSSAsync("pointer-events", "none");

        var first = await BoxOfAsync(Box("otp-sep", 0));
        var separator = await BoxOfAsync(separators.First);
        var second = await BoxOfAsync(Box("otp-sep", 1));
        Assert.IsTrue(first.X + first.Width <= separator.X && separator.X + separator.Width <= second.X, "box, separator, box");
    }

    [TestMethod]
    public async Task SeparatorInterval_GroupsTheBoxes_AndTheTypingSkipsTheSeparator()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#otp-sep-3 .bit-otp-sep")).ToHaveCountAsync(1);
        var third = await BoxOfAsync(Box("otp-sep-3", 2));
        var separator = await BoxOfAsync(Page.Locator("#otp-sep-3 .bit-otp-sep"));
        var fourth = await BoxOfAsync(Box("otp-sep-3", 3));
        Assert.IsTrue(third.X < separator.X && separator.X < fourth.X);

        await StartTypingAsync("otp-sep-3");
        await TypeCodeAsync("otp-sep-3", "1234");
        await Expect(Text("sep-value")).ToHaveTextAsync("[1234]");

        await Expect(Page.Locator("#otp-sep-template .bit-otp-sep .tpl-sep")).ToHaveTextAsync("|3|");
    }


    // ---------- Variant ----------

    [TestMethod]
    [DataRow("fill", "bit-otp-fil", "var(--bit-clr-bg-sec)", "transparent")]
    [DataRow("outline", "bit-otp-otl", "var(--bit-clr-bg-pri)", "var(--bit-clr-brd-pri)")]
    [DataRow("text", "bit-otp-txt", "transparent", "var(--bit-clr-brd-pri)")]
    [DataRow("none", "bit-otp-otl", "var(--bit-clr-bg-pri)", "var(--bit-clr-brd-pri)")]
    public async Task Variant_DecidesHowMuchFrameABoxCarries(string variant, string cssClass, string background, string border)
    {
        await OpenAsync(Url);

        var id = $"otp-var-{variant}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));
        var empty = Box(id, 4);
        await Expect(empty).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", background));
        await Expect(empty).ToHaveCSSAsync("border-bottom-color", await ResolveAsync("border-bottom-color", border));

        if (variant == "text")
        {
            await Expect(empty).ToHaveCSSAsync("border-top-width", "0px");
            Assert.AreNotEqual("0px", await empty.EvaluateAsync<string>("el => getComputedStyle(el).borderBottomWidth"));
        }
    }


    // ---------- Layout ----------

    [TestMethod]
    public async Task Reversed_LaysTheBoxesOutTheOtherWayAndTheArrowsFollow()
    {
        await OpenAsync(Url);

        await Expect(Root("otp-reversed")).ToHaveClassAsync(HasClass("bit-otp-rvs"));
        var first = await BoxOfAsync(Box("otp-reversed", 0));
        var last = await BoxOfAsync(Box("otp-reversed", 4));
        Assert.IsTrue(first.X > last.X, "The first character is on the right.");

        await StartTypingAsync("otp-reversed");
        await TypeCodeAsync("otp-reversed", "12");
        await Expect(Text("reversed-value")).ToHaveTextAsync("[12]");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Box("otp-reversed", 1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Box("otp-reversed", 2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Box("otp-reversed", 0)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Vertical_StacksTheBoxesAndTheUpAndDownArrowsWalkThem()
    {
        await OpenAsync(Url);

        await Expect(Root("otp-vertical")).ToHaveClassAsync(HasClass("bit-otp-vrt"));
        var first = await BoxOfAsync(Box("otp-vertical", 0));
        var second = await BoxOfAsync(Box("otp-vertical", 1));
        Assert.IsTrue(second.Y > first.Y + first.Height - 1 && Math.Abs(second.X - first.X) < 1, "stacked");

        await StartTypingAsync("otp-vertical");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Box("otp-vertical", 1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Box("otp-vertical", 0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Box("otp-vertical", 0)).ToBeFocusedAsync();

        var reversedFirst = await BoxOfAsync(Box("otp-vertical-reversed", 0));
        var reversedLast = await BoxOfAsync(Box("otp-vertical-reversed", 4));
        Assert.IsTrue(reversedFirst.Y > reversedLast.Y, "Vertical and Reversed puts the first character at the bottom.");
    }

    [TestMethod]
    public async Task FullWidth_StretchesTheRowAndSharesItEvenly()
    {
        await OpenAsync(Url);

        await Expect(Root("otp-fullwidth")).ToHaveClassAsync(HasClass("bit-otp-fwi"));
        var container = await BoxOfAsync(Page.Locator("#fullwidth-box"));
        var first = await BoxOfAsync(Box("otp-fullwidth", 0));
        var last = await BoxOfAsync(Box("otp-fullwidth", 5));
        Assert.AreEqual(container.X + container.Width, last.X + last.Width, 1.5);
        Assert.AreEqual(first.Width, last.Width, 1);
        Assert.IsTrue(first.Width > 40, $"The boxes grew to share the row ({first.Width}px).");
    }


    // ---------- Merged ----------

    [TestMethod]
    public async Task Merged_GluesTheBoxesOfEachGroupIntoOneField()
    {
        await OpenAsync(Url);

        await Expect(Root("otp-merged")).ToHaveClassAsync(HasClass("bit-otp-mrg"));
        await Expect(Page.Locator("#otp-merged .bit-otp-iwr")).ToHaveCSSAsync("column-gap", "0px");
        var first = await BoxOfAsync(Box("otp-merged", 0));
        var second = await BoxOfAsync(Box("otp-merged", 1));
        Assert.IsTrue(second.X - (first.X + first.Width) <= 0.5, "No gap between glued boxes.");
        await Expect(Box("otp-merged", 0)).ToHaveClassAsync(HasClass("bit-otp-gst"));
        await Expect(Box("otp-merged", 5)).ToHaveClassAsync(HasClass("bit-otp-gnd"));
        await Expect(Box("otp-merged", 2)).Not.ToHaveClassAsync(new Regex("bit-otp-g(st|nd)"));
        await Expect(Box("otp-merged", 0)).ToHaveCSSAsync("border-top-right-radius", "0px");
        Assert.AreNotEqual("0px", await Box("otp-merged", 0).EvaluateAsync<string>("el => getComputedStyle(el).borderTopLeftRadius"));

        await Expect(Box("otp-merged-3", 0)).ToHaveClassAsync(HasClass("bit-otp-gst"));
        await Expect(Box("otp-merged-3", 2)).ToHaveClassAsync(HasClass("bit-otp-gnd"));
        await Expect(Box("otp-merged-3", 3)).ToHaveClassAsync(HasClass("bit-otp-gst"));
        await Expect(Box("otp-merged-3", 5)).ToHaveClassAsync(HasClass("bit-otp-gnd"));

        var top = await BoxOfAsync(Box("otp-merged-vertical", 0));
        var below = await BoxOfAsync(Box("otp-merged-vertical", 1));
        Assert.IsTrue(below.Y - (top.Y + top.Height) <= 0.5, "Glued along the block axis.");
    }


    // ---------- Paste, copy, autofill ----------

    [TestMethod]
    [DataRow("123 456", "123456")]
    [DataRow("12-34-56", "123456")]
    [DataRow("your code is 987654", "987654")]
    public async Task Paste_IsCleanedUpAndAFullCodeStartsAtTheFirstBox(string pasted, string expected)
    {
        await OpenAsync(Url);

        await PasteAsync(Box("otp-paste", 3), pasted);
        await Expect(Text("paste-value")).ToHaveTextAsync($"[{expected}]");
        CollectionAssert.AreEqual(expected.Select(c => c.ToString()).ToArray(), await ValuesAsync("otp-paste"));
        await Expect(Text("paste-events")).ToHaveTextAsync("1");
        await Expect(Text("paste-index")).ToHaveTextAsync("3");
        await Expect(Box("otp-paste", 5)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Paste_AShortChunkLandsWhereItWasPasted_AndARejectedOneIsReported()
    {
        await OpenAsync(Url);

        await PasteAsync(Box("otp-paste", 0), "123456");
        await Expect(Text("paste-value")).ToHaveTextAsync("[123456]");

        await PasteAsync(Box("otp-paste", 2), "78");
        await Expect(Text("paste-value")).ToHaveTextAsync("[127856]");

        await PasteAsync(Box("otp-paste", 1), "ab");
        await Expect(Text("paste-invalid")).ToHaveTextAsync("[ab@1]");
        await Expect(Text("paste-value")).ToHaveTextAsync("[127856]");
    }

    [TestMethod]
    public async Task PasteTransformer_PullsTheCodeOutOfTheMessage_AndSequentialKeepsAPasteFromPunchingAHole()
    {
        await OpenAsync(Url);

        await PasteAsync(Box("otp-transformer", 0), "your code is a1b2c3, do not share it");
        await Expect(Text("transformer-value")).ToHaveTextAsync("[A1B2C3]");

        await PasteAsync(Box("otp-sequential-paste", 4), "12");
        await Expect(Text("sequential-paste-value")).ToHaveTextAsync("[12]");
        CollectionAssert.AreEqual(new[] { "1", "2", "", "", "", "" }, await ValuesAsync("otp-sequential-paste"));
        await Expect(Box("otp-sequential-paste", 2)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Autofill_AChunkWrittenIntoABoxIsCleanedUpAndSpreadOverTheBoxes()
    {
        await OpenAsync(Url);

        // What a browser or a password manager does with autocomplete="one-time-code": the whole code is written into
        // one box and an input event raised, which reaches the component past the clipboard handler.
        await Box("otp-paste", 2).FillAsync("12 34 56");
        await Expect(Text("paste-value")).ToHaveTextAsync("[123456]");
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "4", "5", "6" }, await ValuesAsync("otp-paste"));
        await Expect(Box("otp-paste", 5)).ToBeFocusedAsync();

        // A chunk that holds nothing the Type accepts is refused as a whole and reported.
        await Box("otp-paste", 1).FillAsync("xyz");
        await Expect(Text("paste-invalid")).ToHaveTextAsync("[xyz@1]");
        await Expect(Text("paste-value")).ToHaveTextAsync("[123456]");
    }

    [TestMethod]
    public async Task Copy_TakesTheWholeCode_UnlessItIsHidden()
    {
        await OpenAsync(Url);

        var copied = await ClipboardAsync(Box("otp-copy", 3), "copy");
        Assert.AreEqual("A1B2C3", copied.Data);
        Assert.IsTrue(copied.Prevented);

        var masked = await ClipboardAsync(Box("otp-copy-masked", 3), "copy");
        Assert.AreEqual("", masked.Data);
        Assert.IsTrue(masked.Prevented, "A masked code is kept off the clipboard.");
        await Expect(Root("otp-copy-masked")).ToHaveAttributeAsync("data-bit-otp-nocopy", "true");

        var password = await ClipboardAsync(Box("otp-copy-password", 0), "copy");
        Assert.AreEqual("", password.Data);
        Assert.IsTrue(password.Prevented);
    }

    [TestMethod]
    public async Task Cut_TakesTheWholeCodeAndClearsIt()
    {
        await OpenAsync(Url);

        var cut = await ClipboardAsync(Box("otp-copy", 4), "cut");
        Assert.AreEqual("A1B2C3", cut.Data);
        await Expect(Text("copy-value")).ToHaveTextAsync("[]");
        CollectionAssert.AreEqual(Enumerable.Repeat("", 6).ToArray(), await ValuesAsync("otp-copy"));
        await Expect(Box("otp-copy", 0)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task NoSmsAutoFill_KeepsThePlatformAndThePasswordManagersOut()
    {
        await OpenAsync(Url);

        var input = Box("otp-nosms", 0);
        await Expect(input).ToHaveAttributeAsync("autocomplete", "off");
        await Expect(input).ToHaveAttributeAsync("data-1p-ignore", "true");
        await Expect(input).ToHaveAttributeAsync("data-bwignore", "true");
        await Expect(input).ToHaveAttributeAsync("data-lpignore", "true");
        await Expect(input).ToHaveAttributeAsync("data-form-type", "other");

        await Expect(Box("otp-basic", 0)).ToHaveAttributeAsync("autocomplete", "one-time-code");
        await ExpectNoAttributeAsync(Box("otp-basic", 0), "data-1p-ignore");
    }


    // ---------- Binding ----------

    [TestMethod]
    public async Task OneWay_TheBoxesKeepTheBoundCodeAndThePageDrivesThem()
    {
        await OpenAsync(Url);

        await StartTypingAsync("otp-oneway", 2);
        await Page.Keyboard.TypeAsync("9");
        await Expect(Text("oneway-value")).ToHaveTextAsync("[11111]");
        await Expect(Box("otp-oneway", 2)).ToHaveValueAsync("1");

        await Page.ClickAsync("#btn-set-oneway");
        await Expect(Box("otp-oneway", 4)).ToHaveValueAsync("5");
        CollectionAssert.AreEqual(new[] { "9", "8", "7", "6", "5" }, await ValuesAsync("otp-oneway"));
    }

    [TestMethod]
    public async Task TwoWay_TypingUpdatesThePage_AndThePageSetsTrimsAndClearsTheBoxes()
    {
        await OpenAsync(Url);

        await StartTypingAsync("otp-twoway", 0);
        await Page.Keyboard.TypeAsync("7");
        await Expect(Text("twoway-value")).ToHaveTextAsync("[72222]");

        await Page.ClickAsync("#btn-set-twoway");
        await Expect(Box("otp-twoway", 4)).ToHaveValueAsync("1");
        CollectionAssert.AreEqual(new[] { "5", "4", "3", "2", "1" }, await ValuesAsync("otp-twoway"));

        // A longer code loses what the boxes cannot hold, and the binding follows what is shown.
        await Page.ClickAsync("#btn-set-twoway-long");
        await Expect(Text("twoway-value")).ToHaveTextAsync("[12345]");
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "4", "5" }, await ValuesAsync("otp-twoway"));

        await Page.ClickAsync("#btn-clear-twoway");
        await Expect(Box("otp-twoway", 0)).ToHaveValueAsync("");
        CollectionAssert.AreEqual(Enumerable.Repeat("", 5).ToArray(), await ValuesAsync("otp-twoway"));
    }


    // ---------- Events ----------

    [TestMethod]
    public async Task Events_EveryCallbackCarriesTheIndexOfItsBox_AndOnFillFiresOncePerCode()
    {
        await OpenAsync(Url);

        await StartTypingAsync("otp-events");
        await Expect(Text("ev-focusin")).ToHaveTextAsync("focusin@0");

        await Page.Keyboard.TypeAsync("a");
        await Expect(Text("ev-input")).ToHaveTextAsync("a@0");
        await Expect(Text("ev-key")).ToHaveTextAsync("a|KeyA@0");
        await Expect(Text("ev-change")).ToHaveTextAsync("[a]");
        await Expect(Text("ev-change-count")).ToHaveTextAsync("1");
        await Expect(Box("otp-events", 1)).ToBeFocusedAsync();
        await Expect(Text("ev-focusout")).ToHaveTextAsync("focusout@0");
        await Expect(Text("ev-focusin")).ToHaveTextAsync("focusin@1");

        await TypeCodeAsync("otp-events", "bc", from: 1);
        await Expect(Text("ev-change")).ToHaveTextAsync("[abc]");
        await Expect(Text("ev-fill-count")).ToHaveTextAsync("1");

        // The same character typed again into the last box, which keeps the focus and holds it with the caret after it,
        // leaves the code as it was: no second fill. The input event reports what the input received - the character
        // added to the one it held, which the box is then cut back to (the first event's "c@2" is no proof the second
        // one was handled, so the raw value is what is waited for).
        await Page.Keyboard.TypeAsync("c");
        await Expect(Text("ev-input")).ToHaveTextAsync("cc@2");
        await Expect(Box("otp-events", 2)).ToHaveValueAsync("c");
        await Expect(Text("ev-change")).ToHaveTextAsync("[abc]");
        await Expect(Text("ev-fill-count")).ToHaveTextAsync("1");
    }


    // ---------- Public API ----------

    [TestMethod]
    public async Task PublicApi_FocusAnyBoxBlurClearAndInputElements()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-api-first");
        await Expect(Box("otp-api", 0)).ToBeFocusedAsync();
        await Page.ClickAsync("#btn-api-last");
        await Expect(Box("otp-api", 5)).ToBeFocusedAsync();
        await Page.ClickAsync("#btn-api-clamp");
        await Expect(Box("otp-api", 5)).ToBeFocusedAsync();

        await Page.ClickAsync("#btn-api-count");
        await Expect(Text("api-inputs")).ToHaveTextAsync("6");

        await Page.ClickAsync("#btn-api-clear");
        await Expect(Text("api-value")).ToHaveTextAsync("[]");
        // Read through the auto-waiting assertions: the clear re-renders the boxes, which a one-off read can catch midway.
        await Expect(Inputs("otp-api")).ToHaveCountAsync(6);
        for (var box = 0; box < 6; box++)
        {
            await Expect(Box("otp-api", box)).ToHaveValueAsync("");
        }

        await Box("otp-api", 2).FocusAsync();
        await Page.Locator("#btn-api-blur").DispatchEventAsync("click");
        await Expect(Box("otp-api", 2)).Not.ToBeFocusedAsync();
    }


    // ---------- Validation ----------

    [TestMethod]
    public async Task EditForm_AnIncompleteCodeIsReportedAndACompleteOneSubmitsTheNamedField()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-submit");
        await Expect(Text("form-status")).ToHaveTextAsync("Invalid");
        await Expect(Root("otp-form")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Box("otp-form", 0)).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#validation .validation-message", new() { HasText = "Enter the code." })).ToBeVisibleAsync();
        await Expect(Box("otp-form", 0)).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-err)"));

        await StartTypingAsync("otp-form");
        await TypeCodeAsync("otp-form", "abc");
        await Expect(Page.Locator("#validation .validation-message", new() { HasText = "The code has 6 characters." })).ToBeVisibleAsync();
        await TypeCodeAsync("otp-form", "def", from: 3);
        await Expect(Root("otp-form")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await ExpectNoAttributeAsync(Box("otp-form", 0), "aria-invalid");

        await Expect(Page.Locator("#otp-form input[type=hidden][name=code]")).ToHaveValueAsync("abcdef");
        await Page.ClickAsync("#btn-submit");
        await Expect(Text("form-status")).ToHaveTextAsync("Valid");
    }

    [TestMethod]
    public async Task AutoSubmit_SubmitsTheFormTheMomentTheCodeIsComplete()
    {
        await OpenAsync(Url);

        await StartTypingAsync("otp-autosubmit");
        await TypeCodeAsync("otp-autosubmit", "12345");
        await Expect(Text("auto-status")).ToHaveTextAsync("None");
        await Page.Keyboard.TypeAsync("6");
        await Expect(Text("auto-status")).ToHaveTextAsync("Submitted 123456");
    }


    // ---------- Loading and invalid ----------

    [TestMethod]
    public async Task IsLoading_DrawsTheBarHoldsTheCodeAndAnnouncesTheDescription()
    {
        await OpenAsync(Url);

        var status = Page.Locator("#otp-loading .bit-otp-sts");
        await Expect(status).ToHaveAttributeAsync("role", "status");
        await Expect(status).ToHaveTextAsync("");

        await Page.ClickAsync("#btn-toggle-loading");
        await Expect(Root("otp-loading")).ToHaveClassAsync(HasClass("bit-otp-ldg"));
        var bar = Page.Locator("#otp-loading .bit-otp-ldr");
        await Expect(bar).ToBeVisibleAsync();
        await Expect(bar).ToHaveAttributeAsync("aria-hidden", "true");
        StringAssert.Contains(await bar.EvaluateAsync<string>("el => getComputedStyle(el, '::after').animationName"), "bit-otp-ldr-anm");
        await Expect(Group("otp-loading")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Box("otp-loading", 0)).ToHaveAttributeAsync("readonly", "");
        await Expect(status).ToHaveTextAsync("Checking the code...");

        await Box("otp-loading", 0).ClickAsync();
        await Page.Keyboard.TypeAsync("1");
        await Expect(Text("loading-value")).ToHaveTextAsync("[]");

        await Page.ClickAsync("#btn-toggle-loading");
        await Expect(bar).ToHaveCountAsync(0);
        await ExpectNoAttributeAsync(Group("otp-loading"), "aria-busy");
        await Expect(status).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task Invalid_PaintsTheErrorStateAndAnnouncesTheDescription()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-toggle-invalid");
        await Expect(Root("otp-loading")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Box("otp-loading", 0)).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Box("otp-loading", 0)).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-err)"));
        await Expect(Page.Locator("#otp-loading .bit-otp-dsc")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-err)"));
        await Expect(Page.Locator("#otp-loading .bit-otp-sts")).ToHaveTextAsync("That code has expired.");

        await Page.ClickAsync("#btn-toggle-invalid");
        await Expect(Root("otp-loading")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Page.Locator("#otp-loading .bit-otp-sts")).ToHaveTextAsync("");
    }


    // ---------- Accessibility ----------

    [TestMethod]
    public async Task Accessibility_TheGroupIsNamedAndEveryBoxIsAnnouncedByPosition()
    {
        await OpenAsync(Url);

        await Expect(Group("otp-a11y-label")).ToHaveAttributeAsync("aria-labelledby", (await Page.Locator("#otp-a11y-label .bit-otp-lbl").GetAttributeAsync("id"))!);
        await Expect(Box("otp-a11y-label", 0)).ToHaveAttributeAsync("aria-label", "1 of 6");
        await Expect(Box("otp-a11y-label", 5)).ToHaveAttributeAsync("aria-label", "6 of 6");

        await Expect(Group("otp-a11y-arialabel")).ToHaveAttributeAsync("aria-label", "Enter the 6 digit code sent to your phone");
        await ExpectNoAttributeAsync(Root("otp-a11y-arialabel"), "aria-label");

        await Expect(Box("otp-a11y-format", 2)).ToHaveAttributeAsync("aria-label", "رقم 3 از 6");
    }

    [TestMethod]
    public async Task Tab_WalksTheBoxes_UnlessSingleTabStopMakesTheCodeOneStop()
    {
        await OpenAsync(Url);

        await Box("otp-a11y-label", 0).FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Box("otp-a11y-label", 1)).ToBeFocusedAsync();

        await Page.Locator("#before-single").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Box("otp-a11y-single", 0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-single")).ToBeFocusedAsync();
        await Expect(Box("otp-a11y-single", 3)).ToHaveAttributeAsync("tabindex", "-1");
    }


    // ---------- Cascading parameters ----------

    [TestMethod]
    public async Task BitParams_CascadesItsDefaults_AndAnOtpInputKeepsWhatItSetsItself()
    {
        await OpenAsync(Url);

        await Expect(Inputs("otp-casc-1")).ToHaveCountAsync(6);
        await Expect(Root("otp-casc-1")).ToHaveClassAsync(HasClass("bit-otp-fil"));
        await Expect(Box("otp-casc-1", 0)).ToHaveAttributeAsync("inputmode", "numeric");
        await Expect(Page.Locator("#otp-casc-1 .bit-otp-sep")).ToHaveCountAsync(1);

        await Expect(Inputs("otp-casc-own")).ToHaveCountAsync(4);
        await Expect(Root("otp-casc-own")).ToHaveClassAsync(HasClass("bit-otp-fil"));

        await Expect(Inputs("otp-casc-outside")).ToHaveCountAsync(5);
        await Expect(Root("otp-casc-outside")).ToHaveClassAsync(HasClass("bit-otp-otl"));
        await Expect(Page.Locator("#otp-casc-outside .bit-otp-sep")).ToHaveCountAsync(0);
    }


    // ---------- Accent and Size ----------

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
    public async Task Accent_ColorsTheBorderOfTheFocusedBox(string color, string suffix, string token)
    {
        await OpenAsync(Url);

        var id = $"otp-acc-{color}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-otp-{suffix}"));
        await Expect(Root("otp-acc-none")).Not.ToHaveClassAsync(HasClass($"bit-otp-{suffix}"));

        await Box(id, 0).ClickAsync();
        await Expect(Box(id, 0)).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", $"var({token})"));
        await Expect(Box(id, 1)).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-brd-pri)"));
    }

    [TestMethod]
    [DataRow("small", "bit-otp-sm", "--bit-siz-ctrl-sm", "--bit-tpg-fs-xs")]
    [DataRow("medium", "bit-otp-md", "--bit-siz-ctrl-md", "--bit-tpg-fs-sm")]
    [DataRow("large", "bit-otp-lg", "--bit-siz-ctrl-lg", "--bit-tpg-fs-md")]
    public async Task Size_ScalesTheBoxesAndTheirText(string size, string cssClass, string boxToken, string fontToken)
    {
        await OpenAsync(Url);

        var id = $"otp-size-{size}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));
        var expected = await ResolveAsync("width", SizeToken(boxToken));
        await Expect(Box(id, 0)).ToHaveCSSAsync("width", expected);
        await Expect(Box(id, 0)).ToHaveCSSAsync("height", expected);
        await Expect(Box(id, 0)).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", $"var({fontToken})"));
    }


    // ---------- Style and Class ----------

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRoot()
    {
        await OpenAsync(Url);

        StringAssert.Contains(await ComputedStyleAsync("#otp-style", "box-shadow"), "rgb(0, 255, 255)");
        await Expect(Root("otp-style")).ToHaveCSSAsync("margin-left", "16px");
        await Expect(Root("otp-class")).ToHaveClassAsync(HasClass("custom-class"));
    }

    [TestMethod]
    public async Task Styles_ReachEveryPartAndTheFilledAndFocusedBoxes()
    {
        await OpenAsync(Url);

        await Expect(Root("otp-styles")).ToHaveCSSAsync("margin-left", "16px");
        await Expect(Page.Locator("#otp-styles .bit-otp-lbl")).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(Page.Locator("#otp-styles .bit-otp-dsc")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Group("otp-styles")).ToHaveCSSAsync("column-gap", "4px");
        await Expect(Box("otp-styles", 1)).ToHaveCSSAsync("border-top-color", "rgb(138, 43, 226)");
        await Expect(Box("otp-styles", 0)).ToHaveCSSAsync("background-color", "rgb(243, 232, 255)");
        Assert.AreNotEqual("rgb(243, 232, 255)", await Box("otp-styles", 1).EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor"));

        await Box("otp-styles", 2).ClickAsync();
        await Expect(Box("otp-styles", 2)).ToHaveAttributeAsync("style", new Regex("box-shadow: rgb\\(138, 43, 226\\)"));
        await Expect(Box("otp-styles", 3)).Not.ToHaveAttributeAsync("style", new Regex("box-shadow"));
    }

    [TestMethod]
    public async Task Classes_ReachEveryPartAndTheFilledAndFocusedBoxes()
    {
        await OpenAsync(Url);

        await Expect(Root("otp-classes")).ToHaveClassAsync(HasClass("custom-root"));
        await Expect(Page.Locator("#otp-classes .bit-otp-lbl")).ToHaveClassAsync(HasClass("custom-label"));
        await Expect(Page.Locator("#otp-classes .bit-otp-dsc")).ToHaveClassAsync(HasClass("custom-description"));
        await Expect(Group("otp-classes")).ToHaveClassAsync(HasClass("custom-wrapper"));
        await Expect(Page.Locator("#otp-classes .bit-otp-sep").First).ToHaveClassAsync(HasClass("custom-separator"));
        await Expect(Box("otp-classes", 0)).ToHaveClassAsync(HasClass("custom-input"));
        await Expect(Box("otp-classes", 0)).ToHaveClassAsync(HasClass("custom-filled"));
        await Expect(Box("otp-classes", 1)).Not.ToHaveClassAsync(HasClass("custom-filled"));

        await Box("otp-classes", 1).ClickAsync();
        await Expect(Box("otp-classes", 1)).ToHaveClassAsync(HasClass("custom-focused"));
        await Expect(Box("otp-classes", 0)).Not.ToHaveClassAsync(HasClass("custom-focused"));
        await BlurAsync();
        await Expect(Box("otp-classes", 1)).Not.ToHaveClassAsync(HasClass("custom-focused"));
    }

    [TestMethod]
    public async Task CssVariables_OnTheStyle_ReskinTheBoxesAndTheFilledPair()
    {
        await OpenAsync(Url);

        var first = Box("otp-cssvar-style", 0);
        var second = Box("otp-cssvar-style", 1);
        await Expect(second).ToHaveCSSAsync("width", "56px");
        await Expect(second).ToHaveCSSAsync("height", "56px");
        await Expect(second).ToHaveCSSAsync("border-top-left-radius", "16px");
        await Expect(second).ToHaveCSSAsync("font-size", "24px");
        await Expect(Group("otp-cssvar-style")).ToHaveCSSAsync("column-gap", "8px");
        await Expect(first).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(first).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");
        Assert.AreNotEqual("rgb(1, 2, 3)", await second.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor"));

        await Expect(Box("otp-cssvar-wh", 0)).ToHaveCSSAsync("width", "44px");
        await Expect(Box("otp-cssvar-wh", 0)).ToHaveCSSAsync("height", "60px");
        await Expect(Box("otp-cssvar-wh", 0)).ToHaveCSSAsync("border-top-width", "2px");

        // An explicit Size wins over the variable for what it sets.
        await Expect(Box("otp-cssvar-explicit", 0)).ToHaveCSSAsync("width", await ResolveAsync("width", SizeToken("--bit-siz-ctrl-sm")));
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_AndAVariantReadsItsOwnTierFirst()
    {
        await OpenAsync(Url);

        var outline = Box("otp-cssvar-ancestor", 1);
        await Expect(outline).ToHaveCSSAsync("border-top-left-radius", "999px");
        await Expect(outline).ToHaveCSSAsync("border-top-color", "rgb(10, 11, 12)");
        await Expect(Box("otp-cssvar-fill", 1)).ToHaveCSSAsync("border-top-color", "rgb(13, 14, 15)");

        await Box("otp-cssvar-ancestor", 0).ClickAsync();
        await Expect(Box("otp-cssvar-ancestor", 0)).ToHaveCSSAsync("border-top-color", "rgb(7, 8, 9)");
    }

    [TestMethod]
    public async Task Hover_DarkensTheBorderOfABoxThatCanBeTypedIn()
    {
        await OpenAsync(Url);

        var box = Box("otp-length4", 2);
        await Expect(box).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-brd-pri)"));
        await box.HoverAsync();
        await Expect(box).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-brd-pri-hover)"));

        await Box("otp-readonly", 2).HoverAsync();
        await Expect(Box("otp-readonly", 2)).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-brd-pri)"));
    }


    // ---------- RTL ----------

    [TestMethod]
    public async Task Rtl_LaysTheBoxesOutFromTheRightAndSwapsTheArrows()
    {
        await OpenAsync(Url);

        await Expect(Root("otp-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("otp-rtl")).ToHaveAttributeAsync("dir", "rtl");
        var first = await BoxOfAsync(Box("otp-rtl", 0));
        var last = await BoxOfAsync(Box("otp-rtl", 4));
        Assert.IsTrue(first.X > last.X, "The first character is on the right.");

        await StartTypingAsync("otp-rtl");
        await TypeCodeAsync("otp-rtl", "12");
        await Expect(Text("rtl-value")).ToHaveTextAsync("[12]");

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Box("otp-rtl", 3)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Box("otp-rtl", 2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Box("otp-rtl", 1)).ToBeFocusedAsync();

        var reversedFirst = await BoxOfAsync(Box("otp-rtl-reversed", 0));
        var reversedLast = await BoxOfAsync(Box("otp-rtl-reversed", 4));
        Assert.IsTrue(reversedFirst.X < reversedLast.X, "Reversed in RTL puts the first character on the left.");

        var sepFirst = await BoxOfAsync(Box("otp-rtl-sep", 2));
        var separator = await BoxOfAsync(Page.Locator("#otp-rtl-sep .bit-otp-sep"));
        var sepNext = await BoxOfAsync(Box("otp-rtl-sep", 3));
        Assert.IsTrue(sepFirst.X > separator.X && separator.X > sepNext.X, "The separator sits between the groups, right to left.");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_TheArrowsFollowTheDirectionTheRowIsLaidOutIn()
    {
        await OpenAsync(Url);

        await Expect(Root("otp-rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        Assert.IsTrue((await BoxOfAsync(Box("otp-rtl-inherited", 0))).X > (await BoxOfAsync(Box("otp-rtl-inherited", 4))).X);

        await StartTypingAsync("otp-rtl-inherited", 1);
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Box("otp-rtl-inherited", 2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Box("otp-rtl-inherited", 1)).ToBeFocusedAsync();
    }


    // ---------- Reduced motion ----------

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheBoxTransition_UnlessForceAnimation()
    {
        await OpenAsync(Url);

        Assert.IsTrue(MaxSeconds(await ComputedStyleAsync("#otp-motion .bit-otp-inp", "transition-duration")) > 0.01);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        var reduced = await ComputedStyleAsync("#otp-motion .bit-otp-inp", "transition-duration");
        Assert.IsTrue(MaxSeconds(reduced) < 0.011, reduced);

        await Expect(Root("otp-motion-force")).ToHaveClassAsync(HasClass("bit-fam"));
        var forced = await ComputedStyleAsync("#otp-motion-force .bit-otp-inp", "transition-duration");
        Assert.IsTrue(MaxSeconds(forced) > 0.01, forced);
    }

    private static double MaxSeconds(string durations) =>
        durations.Split(',').Select(d => d.Trim()).Select(d => d.EndsWith("ms") ? double.Parse(d[..^2], System.Globalization.CultureInfo.InvariantCulture) / 1000
                                                                                : double.Parse(d.TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture)).Max();
}
