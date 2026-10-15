using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// Drives BitNumberField (/e2e/number-field) in a real browser: every section of its demo page and every parameter of
/// its table - typing and committing, one-way, two-way and uncontrolled binding, the three spin button layouts with
/// press-and-hold, HideInput, Min/Max clamping and NoClamp, Step, PageStep, Precision and SnapToStep, every numeric
/// type, the label positions, description, error and loading, the variants, icon and affixes, the clear button and its
/// Escape (inside a BitDialog too), NumberFormat and Culture in en-US, de-DE and fa-IR, Immediate, DebounceTime and
/// ThrottleTime with real typing, ReadOnly and IsInputReadOnly, the mouse wheel, NormalizeDigits, the events, the public
/// methods, EditForm validation, the spinbutton accessibility, the BitParams cascade, Background, Border, Accent and
/// Size, external icons, Style/Class/Styles/Classes and the public CSS variables, RTL, reduced motion, prerendering
/// and unmounting. The touch behaviour is in <see cref="BitNumberFieldTouchE2ETests"/>.
/// </summary>
[TestClass]
public class BitNumberFieldE2ETests : E2ETestBase
{
    private const string Url = "/e2e/number-field";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Input(string id) => Page.Locator($"#{id} .bit-nfl-inp");

    private ILocator Box(string id) => Page.Locator($"#{id} .bit-nfl-cnt");

    private ILocator Increment(string id) => Page.Locator($"#{id} button[aria-label='Increase value']");

    private ILocator Decrement(string id) => Page.Locator($"#{id} button[aria-label='Decrease value']");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    private ILocator Status(string id) => Page.Locator($"#{id} [role=status]");

    private Task BlurAsync() => Page.Locator("h1").ClickAsync();

    private static Task ExpectNoAttributeAsync(ILocator locator, string name) =>
        Expect(locator).Not.ToHaveAttributeAsync(name, new Regex("[\\s\\S]*"));

    // Puts the focus in the field the way a user does, waits for the render the focus causes (the select-all, a
    // formatted value turned plain), selects whatever is there and types over it.
    private async Task TypeOverAsync(string id, string text)
    {
        await Input(id).ClickAsync();
        await Expect(Root(id)).ToHaveClassAsync(HasClass("bit-nfl-fcs"));
        await Page.Keyboard.PressAsync("ControlOrMeta+A");
        await Page.Keyboard.TypeAsync(text);
    }

    private async Task FocusAsync(string id)
    {
        await Input(id).FocusAsync();
        await Expect(Root(id)).ToHaveClassAsync(HasClass("bit-nfl-fcs"));
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

    private Task<string> ColorOfAsync(string token) => ResolveAsync("color", $"var({token})");

    private static string SizeToken(string token) =>
        $"var({token}, calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * var({token}-steps)))";

    private static async Task<LocatorBoundingBoxResult> BoxOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no box.");
        return box;
    }

    private static Task<string> PseudoContentAsync(ILocator locator, string pseudo) =>
        locator.EvaluateAsync<string>($"el => getComputedStyle(el, '{pseudo}').content");

    private async Task ShiftWheelAsync(string id, float deltaY)
    {
        await Input(id).HoverAsync();
        await Page.Keyboard.DownAsync("Shift");
        await Page.Mouse.WheelAsync(0, deltaY);
        await Page.Keyboard.UpAsync("Shift");
    }


    // ---------- Lifecycle ----------

    [TestMethod]
    public async Task Prerender_TheServerHtmlCarriesTheFieldBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"nf-basic\"");
        StringAssert.Contains(html, "role=\"spinbutton\"");
        StringAssert.Contains(html, "id=\"status\">Loading<");

        await OpenAsync(Url);
        await Expect(Root("nf-basic")).ToHaveClassAsync(HasClass("bit-nfl"));
    }

    [TestMethod]
    public async Task Unmount_RemovingAFieldWhileItIsBeingSpun_LeavesNoError()
    {
        await OpenAsync(Url);

        await Increment("nf-mount").HoverAsync();
        await Page.Mouse.DownAsync();
        await Expect(Input("nf-mount")).Not.ToHaveValueAsync("0");
        await Page.Mouse.UpAsync();

        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Root("nf-mount")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Input("nf-mount")).ToHaveValueAsync("0");
        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Root("nf-mount")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }


    // ---------- Basic ----------

    [TestMethod]
    public async Task Basic_TypingANumberAndBlurring_CommitsIt()
    {
        await OpenAsync(Url);

        var input = Input("nf-basic");
        await Expect(input).ToHaveAttributeAsync("role", "spinbutton");
        await Expect(input).ToHaveAttributeAsync("type", "text");
        await Expect(input).ToHaveAttributeAsync("inputmode", "numeric");

        await FocusAsync("nf-basic");
        await Page.Keyboard.TypeAsync("42");
        await Expect(Text("basic-value")).ToHaveTextAsync("[]");
        await BlurAsync();
        await Expect(Text("basic-value")).ToHaveTextAsync("[42]");
        await Expect(input).ToHaveAttributeAsync("aria-valuenow", "42");

        // Text that is not a number is never dropped: it stays to be corrected and the value keeps its own (inside an
        // EditForm it is reported as well, see the validation tests).
        await TypeOverAsync("nf-basic", "abc");
        await BlurAsync();
        await Expect(input).ToHaveValueAsync("abc");
        await Expect(Text("basic-value")).ToHaveTextAsync("[42]");
    }

    [TestMethod]
    public async Task AutoFocus_PutsTheFocusInTheField_AndInAStepperOnItsIncreaseButton()
    {
        await OpenAsync("/e2e/number-field/autofocus");

        var input = Input("nf-autofocus");
        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveAttributeAsync("autofocus", "");
        await Expect(Root("nf-autofocus")).ToHaveClassAsync(HasClass("bit-nfl-fcs"));
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(input).ToHaveValueAsync("43");

        await Expect(Increment("nf-autofocus-hide")).ToHaveAttributeAsync("autofocus", "");
    }

    [TestMethod]
    public async Task Label_NamesTheSpinbuttonAndFocusesIt()
    {
        await OpenAsync(Url);

        var label = Page.Locator("#nf-basic .bit-nfl-lbl");
        var input = Input("nf-basic");
        await Expect(label).ToHaveTextAsync("Basic");
        await Expect(label).ToHaveAttributeAsync("for", (await input.GetAttributeAsync("id"))!);
        await Expect(input).ToHaveAttributeAsync("aria-labelledby", (await label.GetAttributeAsync("id"))!);

        await label.ClickAsync();
        await Expect(input).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task PlaceholderAndRequired_AreRenderedAndMarked()
    {
        await OpenAsync(Url);

        await Expect(Input("nf-placeholder")).ToHaveAttributeAsync("placeholder", "Enter a number...");

        await Expect(Input("nf-required")).ToHaveAttributeAsync("required", "");
        await Expect(Input("nf-required")).ToHaveAttributeAsync("aria-required", "true");
        await Expect(Root("nf-required")).ToHaveClassAsync(HasClass("bit-nfl-req"));
        Assert.AreEqual("\"*\"", await PseudoContentAsync(Page.Locator("#nf-required .bit-nfl-lbl"), "::after"));

        await Expect(Root("nf-required-nolabel")).ToHaveClassAsync(HasClass("bit-nfl-rnl"));
        Assert.AreEqual("\"*\"", await PseudoContentAsync(Box("nf-required-nolabel"), "::before"));
        await ExpectNoAttributeAsync(Input("nf-basic"), "aria-required");
    }

    [TestMethod]
    public async Task Focus_SelectsTheText_UnlessNoSelectOnFocus()
    {
        await OpenAsync(Url);

        await Input("nf-select").ClickAsync();
        await Expect(Input("nf-select")).ToHaveJSPropertyAsync("selectionStart", 0);
        await Expect(Input("nf-select")).ToHaveJSPropertyAsync("selectionEnd", 4);

        await Input("nf-noselect").ClickAsync(new() { Position = new() { X = 5, Y = 5 } });
        await Expect(Root("nf-noselect")).ToHaveClassAsync(HasClass("bit-nfl-fcs"));
        await Page.Keyboard.TypeAsync("9");
        await Expect(Input("nf-noselect")).ToHaveValueAsync("91363");
    }

    [TestMethod]
    public async Task Disabled_RendersTheDisabledMarkupAndRunsNoHandler()
    {
        await OpenAsync(Url);

        await Expect(Root("nf-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("nf-disabled")).ToBeDisabledAsync();
        await Expect(Input("nf-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Increment("nf-disabled")).ToBeDisabledAsync();
        await Expect(Increment("nf-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Page.Locator("#nf-disabled .bit-nfl-cbt")).ToBeDisabledAsync();
        await Expect(Input("nf-disabled")).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-bg-dis)"));
        await Expect(Input("nf-disabled")).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-fg-dis"));

        // Force: Playwright counts the label of a disabled input as disabled itself, while a user can still press it.
        await Page.Locator("#nf-disabled .bit-nfl-lbl").ClickAsync(new() { Force = true });
        await Increment("nf-disabled").ClickAsync(new() { Force = true });
        await Page.Locator("#nf-disabled .bit-nfl-lbl").HoverAsync();
        await Expect(Text("disabled-root-hovers")).Not.ToHaveTextAsync("0");
        await Expect(Text("disabled-root-clicks")).ToHaveTextAsync("0");
        await Expect(Text("disabled-spins")).ToHaveTextAsync("0");
        await Expect(Input("nf-disabled")).ToHaveValueAsync("1363");

        // Tab passes it by.
        await Page.Locator("#before-disabled").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-disabled")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Disabled_TurnedOffAtRuntime_TheFieldSpinsAndReportsAgain()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(Root("nf-disabled")).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await ExpectNoAttributeAsync(Input("nf-disabled"), "aria-disabled");

        await Increment("nf-disabled").ClickAsync();
        await Expect(Input("nf-disabled")).ToHaveValueAsync("1364");
        await Expect(Text("disabled-spins")).ToHaveTextAsync("1");
        await Expect(Text("disabled-root-clicks")).ToHaveTextAsync("1");

        await Input("nf-disabled").ClickAsync();
        await Expect(Text("disabled-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task BaseParameters_IdClassStyleAttributesAndName()
    {
        await OpenAsync(Url);

        var root = Root("nf-base");
        await Expect(root).ToHaveClassAsync(HasClass("my-root-class"));
        await Expect(root).ToHaveCSSAsync("margin-top", "7px");
        await Expect(root).ToHaveAttributeAsync("data-test", "base-attr");
        await Expect(Input("nf-base")).ToHaveAttributeAsync("data-input-attr", "yes");
        await Expect(Input("nf-base")).ToHaveAttributeAsync("name", "quantity");

        // The text-editing helpers of a soft keyboard are off for a number.
        await Expect(Input("nf-base")).ToHaveAttributeAsync("spellcheck", "false");
        await Expect(Input("nf-base")).ToHaveAttributeAsync("autocorrect", "off");
        await Expect(Input("nf-base")).ToHaveAttributeAsync("autocapitalize", "off");
        await Expect(Input("nf-base")).ToHaveAttributeAsync("autocomplete", "off");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheSpace_CollapsedRemovesIt()
    {
        await OpenAsync(Url);

        var root = Root("nf-visibility");
        await Page.ClickAsync("#btn-vis-hidden");
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await BoxOfAsync(root)).Height > 20);

        await Page.ClickAsync("#btn-vis-collapsed");
        await Expect(root).ToHaveCSSAsync("display", "none");

        await Page.ClickAsync("#btn-vis-visible");
        await Expect(root).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Rerender_ChangingParametersAtRuntime_UpdatesTheField()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#nf-rerender .bit-nfl-lbl")).ToHaveTextAsync("First label");
        await Expect(Page.Locator("#nf-rerender button")).ToHaveCountAsync(0);
        await ExpectNoAttributeAsync(Input("nf-rerender"), "aria-valuemax");

        await Page.ClickAsync("#btn-change-params");

        await Expect(Page.Locator("#nf-rerender .bit-nfl-lbl")).ToHaveTextAsync("Second label");
        await Expect(Root("nf-rerender")).ToHaveClassAsync(HasClass("bit-nfl-min"));
        await Expect(Page.Locator("#nf-rerender .bit-nfl-sbn")).ToHaveCountAsync(2);
        await Expect(Input("nf-rerender")).ToHaveAttributeAsync("aria-valuemax", "3");
    }


    // ---------- Binding ----------

    [TestMethod]
    public async Task OneWay_ATypedValueIsWrittenBackAndThePageStillDrivesIt()
    {
        await OpenAsync(Url);

        await TypeOverAsync("nf-oneway", "99");
        await BlurAsync();
        await Expect(Input("nf-oneway")).ToHaveValueAsync("5");
        await Expect(Text("oneway-value")).ToHaveTextAsync("[5]");

        await Page.ClickAsync("#btn-set-oneway");
        await Expect(Input("nf-oneway")).ToHaveValueAsync("77");
    }

    [TestMethod]
    public async Task TwoWay_TypingAndSpinningUpdateThePage_AndThePageUpdatesTheField()
    {
        await OpenAsync(Url);

        await TypeOverAsync("nf-twoway", "8");
        await BlurAsync();
        await Expect(Text("twoway-value")).ToHaveTextAsync("[8]");

        await Increment("nf-twoway").ClickAsync();
        await Expect(Text("twoway-value")).ToHaveTextAsync("[9]");

        await Page.ClickAsync("#btn-set-twoway");
        await Expect(Input("nf-twoway")).ToHaveValueAsync("42");
        await Expect(Input("nf-twoway")).ToHaveAttributeAsync("aria-valuenow", "42");
    }

    [TestMethod]
    public async Task Uncontrolled_DefaultValueSeedsTheFieldAndOnChangeReports()
    {
        await OpenAsync(Url);

        await Expect(Input("nf-uncontrolled")).ToHaveValueAsync("7");
        await TypeOverAsync("nf-uncontrolled", "11");
        await BlurAsync();
        await Expect(Text("uncontrolled-value")).ToHaveTextAsync("[11]");
        await Expect(Text("uncontrolled-count")).ToHaveTextAsync("1");
    }


    // ---------- Spin buttons ----------

    [TestMethod]
    public async Task Mode_Compact_StacksTheButtonsAtTheEnd()
    {
        await OpenAsync(Url);

        await Expect(Root("nf-compact")).ToHaveClassAsync(HasClass("bit-nfl-mcp"));
        var up = await BoxOfAsync(Page.Locator("#nf-compact .bit-nfl-aup"));
        var down = await BoxOfAsync(Page.Locator("#nf-compact .bit-nfl-adn"));
        var input = await BoxOfAsync(Input("nf-compact"));

        Assert.IsTrue(up.Y + up.Height <= down.Y + 1, "Increase is stacked above decrease.");
        Assert.AreEqual(up.X, down.X, 1);
        Assert.IsTrue(up.X >= input.X + input.Width - 1, "The stack sits after the input.");

        await Page.Locator("#nf-compact .bit-nfl-aup").ClickAsync();
        await Expect(Text("compact-value")).ToHaveTextAsync("[1]");
        await Expect(Input("nf-compact")).ToBeFocusedAsync();
        await Page.Locator("#nf-compact .bit-nfl-adn").ClickAsync();
        await Expect(Text("compact-value")).ToHaveTextAsync("[0]");
    }

    [TestMethod]
    public async Task Mode_Inline_PutsBothButtonsAfterTheInputSideBySide()
    {
        await OpenAsync(Url);

        await Expect(Root("nf-inline")).ToHaveClassAsync(HasClass("bit-nfl-min"));
        var input = await BoxOfAsync(Input("nf-inline"));
        var down = await BoxOfAsync(Decrement("nf-inline"));
        var up = await BoxOfAsync(Increment("nf-inline"));

        Assert.IsTrue(down.X >= input.X + input.Width - 1 && up.X >= down.X + down.Width - 1, "input, decrease, increase");
        Assert.AreEqual(down.Y, up.Y, 1);

        await Increment("nf-inline").ClickAsync();
        await Increment("nf-inline").ClickAsync();
        await Expect(Text("inline-value")).ToHaveTextAsync("[2]");
        await Decrement("nf-inline").ClickAsync();
        await Expect(Text("inline-value")).ToHaveTextAsync("[1]");
    }

    [TestMethod]
    public async Task Mode_Spread_PutsOneButtonOnEachSide()
    {
        await OpenAsync(Url);

        await Expect(Root("nf-spread")).ToHaveClassAsync(HasClass("bit-nfl-msp"));
        var input = await BoxOfAsync(Input("nf-spread"));
        var down = await BoxOfAsync(Decrement("nf-spread"));
        var up = await BoxOfAsync(Increment("nf-spread"));

        Assert.IsTrue(down.X + down.Width <= input.X + 1, "Decrease leads.");
        Assert.IsTrue(up.X >= input.X + input.Width - 1, "Increase trails.");
        await Expect(Input("nf-spread")).ToHaveClassAsync(HasClass("bit-nfl-cin"));
        await Expect(Input("nf-spread")).ToHaveCSSAsync("text-align", "center");

        await Increment("nf-spread").ClickAsync();
        await Expect(Text("spread-value")).ToHaveTextAsync("[1]");
        await Decrement("nf-spread").ClickAsync();
        await Decrement("nf-spread").ClickAsync();
        await Expect(Text("spread-value")).ToHaveTextAsync("[-1]");
    }

    [TestMethod]
    public async Task Mode_Unset_RendersNoButtons_AndTheButtonsStayOutOfTheTabOrder()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#nf-nomode button")).ToHaveCountAsync(0);
        await Expect(Root("nf-nomode")).Not.ToHaveClassAsync(HasClass("bit-nfl-mcp"));

        await Expect(Increment("nf-compact")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Increment("nf-compact")).ToHaveAttributeAsync("aria-controls", (await Input("nf-compact").GetAttributeAsync("id"))!);
        await FocusAsync("nf-compact");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Increment("nf-compact")).Not.ToBeFocusedAsync();
        await Expect(Input("nf-inline")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task SpinButtons_CustomIconsAndTitles()
    {
        await OpenAsync(Url);

        await Expect(Increment("nf-custom-icons")).ToHaveAttributeAsync("title", "Like it more");
        await Expect(Decrement("nf-custom-icons")).ToHaveAttributeAsync("title", "Like it less");
        await Expect(Page.Locator("#nf-custom-icons .bit-nfl-aup i")).ToHaveClassAsync(HasClass("bit-icon--LikeSolid"));
        await Expect(Page.Locator("#nf-custom-icons .bit-nfl-adn i")).ToHaveClassAsync(HasClass("bit-icon--DislikeSolid"));

        // The defaults: a chevron, turned upside down for the increase.
        await Expect(Page.Locator("#nf-compact .bit-nfl-aup i")).ToHaveClassAsync(HasClass("bit-ico-r180"));
        await Expect(Page.Locator("#nf-compact .bit-nfl-adn i")).ToHaveClassAsync(HasClass("bit-icon--ChevronDownSmall"));
    }

    [TestMethod]
    public async Task ContinuousSpin_HoldingAButtonKeepsSpinningUntilItIsReleased()
    {
        await OpenAsync(Url);

        await Increment("nf-continuous").HoverAsync();
        await Page.Mouse.DownAsync();
        await Expect(Text("continuous-value")).ToHaveTextAsync(new Regex(@"^([5-9]|\d{2,})$"));
        await Page.Mouse.UpAsync();

        // After the release the value settles: a round trip later it has moved by at most the one step in flight.
        var released = int.Parse((await Text("continuous-value").TextContentAsync())!);
        await Page.ClickAsync("#btn-set-oneway");
        await Expect(Input("nf-oneway")).ToHaveValueAsync("77");
        await Page.ClickAsync("#btn-set-twoway");
        await Expect(Input("nf-twoway")).ToHaveValueAsync("42");
        var settled = int.Parse((await Text("continuous-value").TextContentAsync())!);
        Assert.IsTrue(settled - released <= 1, $"The spin stopped ({released} then {settled}).");
    }

    [TestMethod]
    public async Task HideInput_LeavesAStepperWhoseButtonsAreTheTabStops()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#nf-hideinput input[type=hidden]")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#nf-hideinput input[type=hidden]")).ToHaveValueAsync("0");
        var group = Box("nf-hideinput");
        await Expect(group).ToHaveAttributeAsync("role", "group");
        await Expect(group).ToHaveAttributeAsync("aria-labelledby", (await Page.Locator("#nf-hideinput label").GetAttributeAsync("id"))!);
        await ExpectNoAttributeAsync(Page.Locator("#nf-hideinput label"), "for");
        await Expect(Increment("nf-hideinput")).ToHaveAttributeAsync("tabindex", "0");
        await ExpectNoAttributeAsync(Increment("nf-hideinput"), "aria-controls");

        await Increment("nf-hideinput").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("hideinput-value")).ToHaveTextAsync("[1]");
        await Expect(Page.Locator("#nf-hideinput label")).ToHaveTextAsync("HideInput: 1");
        await Expect(Status("nf-hideinput")).ToHaveTextAsync("1");

        await Increment("nf-hideinput").ClickAsync();
        await Expect(Text("hideinput-value")).ToHaveTextAsync("[2]");
        await Expect(Increment("nf-hideinput")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Decrement("nf-hideinput")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync(" ");
        await Expect(Text("hideinput-value")).ToHaveTextAsync("[1]");

        // Without a Mode a stepper falls back to the compact stack.
        await Expect(Root("nf-hideinput-default")).ToHaveClassAsync(HasClass("bit-nfl-mcp"));
    }


    // ---------- Min and Max ----------

    [TestMethod]
    public async Task Min_TheButtonAndTheKeyThatWouldPassItGoInert()
    {
        await OpenAsync(Url);

        var input = Input("nf-min");
        await Expect(input).ToHaveAttributeAsync("aria-valuemin", "0");
        await ExpectNoAttributeAsync(input, "aria-valuemax");
        await Expect(Decrement("nf-min")).ToHaveClassAsync(HasClass("bit-nfl-sbb"));
        await Expect(Decrement("nf-min")).ToHaveAttributeAsync("aria-disabled", "true");
        // Inert, not disabled: it keeps its place in the accessibility tree and its focus.
        Assert.IsFalse(await Decrement("nf-min").EvaluateAsync<bool>("el => el.disabled"));

        await FocusAsync("nf-min");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Text("min-value")).ToHaveTextAsync("[1]");
        await Expect(Decrement("nf-min")).Not.ToHaveClassAsync(HasClass("bit-nfl-sbb"));

        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Text("min-value")).ToHaveTextAsync("[0]");
    }

    [TestMethod]
    public async Task HomeAndEnd_JumpToTheExplicitBounds()
    {
        await OpenAsync(Url);

        var input = Input("nf-minmax");
        await Expect(input).ToHaveAttributeAsync("aria-valuemin", "-10");
        await Expect(input).ToHaveAttributeAsync("aria-valuemax", "10");

        await FocusAsync("nf-minmax");
        await Page.Keyboard.PressAsync("End");
        await Expect(Text("minmax-value")).ToHaveTextAsync("[10]");
        await Expect(Increment("nf-minmax")).ToHaveClassAsync(HasClass("bit-nfl-sbb"));
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.ClickAsync("#btn-set-oneway");
        await Expect(Input("nf-oneway")).ToHaveValueAsync("77");
        await Expect(Text("minmax-value")).ToHaveTextAsync("[10]");

        await FocusAsync("nf-minmax");
        await Page.Keyboard.PressAsync("Home");
        await Expect(Text("minmax-value")).ToHaveTextAsync("[-10]");
        await Expect(input).ToHaveAttributeAsync("aria-valuenow", "-10");
    }

    [TestMethod]
    public async Task Clamping_ATypedValueOutsideTheRangeCommitsTheBound()
    {
        await OpenAsync(Url);

        await TypeOverAsync("nf-clamp", "500");
        await BlurAsync();
        await Expect(Text("clamp-value")).ToHaveTextAsync("[100]");
        await Expect(Input("nf-clamp")).ToHaveValueAsync("100");

        await TypeOverAsync("nf-clamp", "-5");
        await BlurAsync();
        await Expect(Text("clamp-value")).ToHaveTextAsync("[0]");
    }

    [TestMethod]
    public async Task NoClamp_LetsATypedValueThroughButStepsStayInside()
    {
        await OpenAsync(Url);

        await TypeOverAsync("nf-noclamp", "500");
        await BlurAsync();
        await Expect(Text("noclamp-value")).ToHaveTextAsync("[500]");
        await Expect(Input("nf-noclamp")).ToHaveValueAsync("500");

        await FocusAsync("nf-noclamp");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Text("noclamp-value")).ToHaveTextAsync("[500]");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Text("noclamp-value")).ToHaveTextAsync("[100]");
    }


    // ---------- Step, precision, snapping ----------

    [TestMethod]
    public async Task Step_IsWhatASpinAdds()
    {
        await OpenAsync(Url);

        await FocusAsync("nf-step");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Text("step-value")).ToHaveTextAsync("[4]");
        await Increment("nf-step").ClickAsync();
        await Expect(Text("step-value")).ToHaveTextAsync("[6]");
    }

    [TestMethod]
    public async Task FractionalStep_SpinsWithoutFloatingPointDrift()
    {
        await OpenAsync(Url);

        await FocusAsync("nf-fraction");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("ArrowUp");
        // The bound value is rounded to the precision the step implies, not only what the input shows.
        await Expect(Text("fraction-value")).ToHaveTextAsync("[0.3]");
        await Expect(Input("nf-fraction")).ToHaveValueAsync("0.3");
        await Expect(Input("nf-fraction")).ToHaveAttributeAsync("aria-valuenow", "0.3");
        await Expect(Root("nf-fraction")).ToHaveClassAsync(HasClass("bit-nfl-fcs"));
        await Expect(Input("nf-fraction")).ToHaveAttributeAsync("inputmode", "decimal");
    }

    [TestMethod]
    public async Task PageUpAndPageDown_JumpByThePageStep_OrTenStepsByDefault()
    {
        await OpenAsync(Url);

        await FocusAsync("nf-pagestep");
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(Text("pagestep-value")).ToHaveTextAsync("[20]");
        await Page.Keyboard.PressAsync("PageUp");
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Text("pagestep-value")).ToHaveTextAsync("[20]");

        await FocusAsync("nf-pagestep-default");
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(Text("pagestep-default-value")).ToHaveTextAsync("[30]");
        await Page.Keyboard.PressAsync("PageDown");
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Text("pagestep-default-value")).ToHaveTextAsync("[-30]");

        // The arrows with a modifier are text editing, not a spin.
        await Page.Keyboard.PressAsync("Shift+ArrowUp");
        await Expect(Text("pagestep-default-value")).ToHaveTextAsync("[-30]");
    }

    [TestMethod]
    public async Task Precision_RoundsWhatIsTyped()
    {
        await OpenAsync(Url);

        await TypeOverAsync("nf-precision", "1234");
        await BlurAsync();
        await Expect(Text("precision-value")).ToHaveTextAsync("[1200]");

        await TypeOverAsync("nf-precision2", "1.23456");
        await BlurAsync();
        await Expect(Text("precision2-value")).ToHaveTextAsync("[1.23]");
        await Expect(Input("nf-precision2")).ToHaveValueAsync("1.23");
    }

    [TestMethod]
    public async Task SnapToStep_PutsTypedValuesOnTheGridAnchoredAtMin()
    {
        await OpenAsync(Url);

        await TypeOverAsync("nf-snap", "7");
        await BlurAsync();
        await Expect(Text("snap-value")).ToHaveTextAsync("[8]");

        await TypeOverAsync("nf-snap-fraction", "0.3");
        await BlurAsync();
        await Expect(Text("snap-fraction-value")).ToHaveTextAsync("[0.25]");
    }


    // ---------- Numeric types ----------

    [TestMethod]
    [DataRow("byte", "300", "255", "numeric")]
    [DataRow("sbyte", "-200", "-128", "numeric")]
    [DataRow("short", "40000", "32767", "numeric")]
    [DataRow("ushort", "70000", "65535", "numeric")]
    [DataRow("int", "3000000000", "2147483647", "numeric")]
    [DataRow("uint", "-5", "0", "numeric")]
    [DataRow("long", "9999999999", "9999999999", "numeric")]
    [DataRow("ulong", "18446744073709551615", "18446744073709551615", "numeric")]
    [DataRow("float", "1.5", "1.5", "decimal")]
    [DataRow("double", "2.25", "2.25", "decimal")]
    [DataRow("decimal", "12.344", "12.34", "decimal")]
    [DataRow("nint", "-7", "-7", "numeric")]
    [DataRow("ndouble", "0.5", "0.5", "decimal")]
    [DataRow("ndecimal", "3.75", "3.75", "decimal")]
    public async Task NumericTypes_CommitAValueOfTheirOwnRange(string type, string typed, string expected, string inputMode)
    {
        await OpenAsync(Url);

        var id = $"nf-type-{type}";
        await Expect(Input(id)).ToHaveAttributeAsync("inputmode", inputMode);

        await TypeOverAsync(id, typed);
        await BlurAsync();
        await Expect(Text($"type-{type}-value")).ToHaveTextAsync($"[{expected}]");
        await Expect(Input(id)).ToHaveAttributeAsync("aria-valuenow", expected);
    }

    [TestMethod]
    public async Task NullableTypes_AnEmptyFieldIsNull()
    {
        await OpenAsync(Url);

        await Expect(Text("type-nint-value")).ToHaveTextAsync("[5]");
        await TypeOverAsync("nf-type-nint", "");
        await Page.Keyboard.PressAsync("Delete");
        await BlurAsync();
        await Expect(Text("type-nint-value")).ToHaveTextAsync("[null]");
        await ExpectNoAttributeAsync(Input("nf-type-nint"), "aria-valuenow");
    }

    [TestMethod]
    public async Task InputModeText_LetsANegativeValueBeTyped()
    {
        await OpenAsync(Url);

        await Expect(Input("nf-signed")).ToHaveAttributeAsync("inputmode", "text");
        await TypeOverAsync("nf-signed", "-50");
        await BlurAsync();
        await Expect(Text("signed-value")).ToHaveTextAsync("[-50]");
    }


    // ---------- Label position ----------

    [TestMethod]
    [DataRow("top", "bit-nfl-ltp")]
    [DataRow("bottom", "bit-nfl-lbt")]
    [DataRow("start", "bit-nfl-lst")]
    [DataRow("end", "bit-nfl-led")]
    public async Task LabelPlacement_MovesTheLabelAroundTheField(string placement, string cssClass)
    {
        await OpenAsync(Url);

        var id = $"nf-lp-{placement}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));
        var label = await BoxOfAsync(Page.Locator($"#{id} .bit-nfl-lbl"));
        var box = await BoxOfAsync(Box(id));

        switch (placement)
        {
            case "top": Assert.IsTrue(label.Y + label.Height <= box.Y + 1, "above"); break;
            case "bottom": Assert.IsTrue(label.Y >= box.Y + box.Height - 1, "below"); break;
            case "start": Assert.IsTrue(label.X + label.Width <= box.X + 1, "before"); break;
            case "end": Assert.IsTrue(label.X >= box.X + box.Width - 1, "after"); break;
        }
    }

    [TestMethod]
    public async Task LabelTemplate_ReplacesTheLabelAndStillNamesTheInput()
    {
        await OpenAsync(Url);

        var label = Page.Locator("#nf-label-template label");
        await Expect(label.Locator(".tpl-label")).ToHaveTextAsync("This is custom Label");
        await Expect(label).ToHaveAttributeAsync("for", (await Input("nf-label-template").GetAttributeAsync("id"))!);
        await Expect(Input("nf-label-template")).ToHaveAttributeAsync("aria-labelledby", (await label.GetAttributeAsync("id"))!);
    }


    // ---------- Description, error, loading ----------

    [TestMethod]
    public async Task Description_IsRenderedAndReferenced()
    {
        await OpenAsync(Url);

        var description = Page.Locator("#nf-desc .bit-nfl-des");
        await Expect(description).ToHaveTextAsync("Between 1 and 99 items per order.");
        await Expect(Input("nf-desc")).ToHaveAttributeAsync("aria-describedby", (await description.GetAttributeAsync("id"))!);

        await Expect(Page.Locator("#nf-desc-template .bit-nfl-des .tpl-desc")).ToHaveTextAsync("Anything above 50% needs approval.");
    }

    [TestMethod]
    public async Task ErrorMessage_AppearsWithTheValueThatTriggersItAndIsAnnounced()
    {
        await OpenAsync(Url);

        var message = Page.Locator("#nf-error .bit-nfl-erm");
        await Expect(message).ToHaveCountAsync(0);

        await TypeOverAsync("nf-error", "5");
        await Expect(message).ToHaveTextAsync("Only 4 seats are left for this show.");
        await Expect(Root("nf-error")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("nf-error")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Status("nf-error")).ToHaveTextAsync("Only 4 seats are left for this show.");
        await Expect(Box("nf-error")).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-err)"));

        var errorId = await message.GetAttributeAsync("id");
        var descriptionId = await Page.Locator("#nf-error .bit-nfl-des").GetAttributeAsync("id");
        await Expect(Input("nf-error")).ToHaveAttributeAsync("aria-describedby", $"{errorId} {descriptionId}");
        var messageBox = await BoxOfAsync(message);
        var descriptionBox = await BoxOfAsync(Page.Locator("#nf-error .bit-nfl-des"));
        Assert.IsTrue(messageBox.Y < descriptionBox.Y, "The error sits above the description.");

        await TypeOverAsync("nf-error", "3");
        await Expect(message).ToHaveCountAsync(0);
        await Expect(Root("nf-error")).Not.ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task InvalidAndErrorMessageTemplate_MarkTheFieldInvalid()
    {
        await OpenAsync(Url);

        await Expect(Root("nf-invalid")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("nf-invalid")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#nf-invalid .bit-nfl-erm")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#nf-error-template .bit-nfl-erm .tpl-error")).ToHaveTextAsync("Rejected.");
        await Expect(Root("nf-error-template")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Status("nf-error-template")).ToHaveTextAsync("Invalid input");
    }

    [TestMethod]
    public async Task Loading_ShowsASpinnerMarksTheInputBusyAndKeepsItEditable()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#nf-loading .bit-nfl-lod")).ToHaveCountAsync(0);
        await ExpectNoAttributeAsync(Input("nf-loading"), "aria-busy");

        await Page.ClickAsync("#btn-toggle-loading");
        await Expect(Page.Locator("#nf-loading .bit-nfl-spn")).ToBeVisibleAsync();
        await Expect(Page.Locator("#nf-loading .bit-nfl-lod")).ToHaveAttributeAsync("aria-hidden", "true");
        StringAssert.Contains(await ComputedStyleAsync("#nf-loading .bit-nfl-spn", "animation-name"), "bit-spin");
        await Expect(Input("nf-loading")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Status("nf-loading")).ToHaveTextAsync("Recalculating the price");

        await TypeOverAsync("nf-loading", "25.5");
        await Expect(Input("nf-loading")).ToHaveValueAsync("25.5");

        await Expect(Status("nf-loading-default")).ToHaveTextAsync("Loading");
        await Expect(Page.Locator("#nf-loading-template .bit-nfl-lod .tpl-loading")).ToHaveTextAsync("busy");
        await Expect(Page.Locator("#nf-loading-template .bit-nfl-spn")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task AriaDescription_IsAHiddenDescriptionReadAfterTheVisibleOne()
    {
        await OpenAsync(Url);

        var hidden = Page.Locator("#nf-aria-desc span.bit-nfl-dsc", new() { HasText = "Use the arrow keys." });
        Assert.IsTrue(await hidden.EvaluateAsync<bool>("el => el.getBoundingClientRect().width <= 1"));
        var descriptionId = await Page.Locator("#nf-aria-desc .bit-nfl-des").GetAttributeAsync("id");
        await Expect(Input("nf-aria-desc")).ToHaveAttributeAsync("aria-describedby", $"{descriptionId} {await hidden.GetAttributeAsync("id")}");
    }

    [TestMethod]
    public async Task Description_WithASideLabel_WrapsOntoALineOfItsOwn()
    {
        await OpenAsync(Url);

        await Expect(Root("nf-desc-side")).ToHaveClassAsync(HasClass("bit-nfl-hds"));
        var box = await BoxOfAsync(Box("nf-desc-side"));
        var description = await BoxOfAsync(Page.Locator("#nf-desc-side .bit-nfl-des"));
        Assert.IsTrue(description.Y >= box.Y + box.Height - 1, "The hint is under the row.");
    }


    // ---------- Appearance ----------

    [TestMethod]
    public async Task Underlined_DrawsOnlyTheBottomRule()
    {
        await OpenAsync(Url);

        await Expect(Root("nf-underlined")).ToHaveClassAsync(HasClass("bit-nfl-und"));
        await Expect(Box("nf-underlined")).ToHaveCSSAsync("border-top-width", "0px");
        Assert.AreNotEqual("0px", await ComputedStyleAsync("#nf-underlined .bit-nfl-cnt", "border-bottom-width"));
        Assert.AreNotEqual("0px", await ComputedStyleAsync("#nf-default-look .bit-nfl-cnt", "border-top-width"));
        Assert.AreEqual("\"*\"", await PseudoContentAsync(Page.Locator("#nf-und-req .bit-nfl-lbl"), "::after"));
    }

    [TestMethod]
    public async Task NoBorderAndFullWidth()
    {
        await OpenAsync(Url);

        await Expect(Root("nf-noborder")).ToHaveClassAsync(HasClass("bit-nfl-nbd"));
        await Expect(Box("nf-noborder")).ToHaveCSSAsync("border-top-width", "0px");

        await Expect(Root("nf-fullwidth")).ToHaveClassAsync(HasClass("bit-nfl-fwd"));
        var container = await BoxOfAsync(Page.Locator("#fullwidth-box"));
        Assert.AreEqual(container.Width, (await BoxOfAsync(Root("nf-fullwidth"))).Width, 1);
        Assert.IsTrue((await BoxOfAsync(Root("nf-notfullwidth"))).Width < container.Width - 50);
    }


    // ---------- Icon and affixes ----------

    [TestMethod]
    public async Task Icon_IsDecorativeUnlessNamed()
    {
        await OpenAsync(Url);

        var icon = Page.Locator("#nf-icon .bit-nfl-lic");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Lightbulb"));
        await Expect(icon).ToHaveAttributeAsync("aria-hidden", "true");

        var named = Page.Locator("#nf-icon-aria .bit-nfl-lic");
        await Expect(named).ToHaveAttributeAsync("role", "img");
        await Expect(named).ToHaveAttributeAsync("aria-label", "An idea");

        await Expect(icon).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-fg-pri"));
        await FocusAsync("nf-icon");
        await Expect(icon).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-pri"));
    }

    [TestMethod]
    public async Task PrefixAndSuffix_SitAroundTheInputAreReadWithItAndFocusIt()
    {
        await OpenAsync(Url);

        var prefix = Page.Locator("#nf-affix .bit-nfl-pre");
        var suffix = Page.Locator("#nf-affix .bit-nfl-suf");
        await Expect(prefix).ToHaveTextAsync("Distance:");
        await Expect(suffix).ToHaveTextAsync("km");

        var input = await BoxOfAsync(Input("nf-affix"));
        Assert.IsTrue((await BoxOfAsync(prefix)).X < input.X);
        Assert.IsTrue((await BoxOfAsync(suffix)).X > input.X + input.Width - 1);

        var prefixId = await prefix.Locator("span").GetAttributeAsync("id");
        var suffixId = await suffix.Locator("span").GetAttributeAsync("id");
        await Expect(Input("nf-affix")).ToHaveAttributeAsync("aria-describedby", $"{prefixId} {suffixId}");

        await prefix.ClickAsync();
        await Expect(Input("nf-affix")).ToBeFocusedAsync();
        await Expect(prefix).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-pri"));

        await Page.Locator("#nf-affix-disabled .bit-nfl-pre").ClickAsync();
        await Expect(Input("nf-affix-disabled")).Not.ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task PrefixAndSuffixTemplates_TakeTheirSlots()
    {
        await OpenAsync(Url);

        var prefix = await BoxOfAsync(Page.Locator("#nf-affix-template .tpl-prefix"));
        var suffix = await BoxOfAsync(Page.Locator("#nf-affix-template .tpl-suffix"));
        var input = await BoxOfAsync(Input("nf-affix-template"));
        Assert.IsTrue(prefix.X < input.X && suffix.X > input.X + input.Width - 1);
        await ExpectNoAttributeAsync(Input("nf-affix-template"), "aria-describedby");
    }


    // ---------- Clear button ----------

    [TestMethod]
    public async Task ClearButton_EmptiesTheFieldRaisesOnClearAndRefocuses()
    {
        await OpenAsync(Url);

        var button = Page.Locator("#nf-clear .bit-nfl-cbt");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Clear value");
        await Expect(button).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(button.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));

        await button.ClickAsync();
        await Expect(Input("nf-clear")).ToHaveValueAsync("");
        await Expect(Text("clear-value")).ToHaveTextAsync("[null]");
        await Expect(Text("clear-count")).ToHaveTextAsync("1");
        await Expect(Input("nf-clear")).ToBeFocusedAsync();
        await Expect(button).ToHaveCountAsync(0);

        // It also clears text that failed to parse, once that text is committed.
        await Page.Keyboard.TypeAsync("abc");
        await BlurAsync();
        await Expect(Input("nf-clear")).ToHaveValueAsync("abc");
        await button.ClickAsync();
        await Expect(Input("nf-clear")).ToHaveValueAsync("");
        await Expect(Text("clear-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task ClearButton_CustomIconAriaLabelAndTemplate()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#nf-clear-icon .bit-nfl-cbt")).ToHaveAttributeAsync("aria-label", "Remove the age");
        await Expect(Page.Locator("#nf-clear-icon .bit-nfl-cbt i")).ToHaveClassAsync(HasClass("bit-icon--Delete"));
        await Expect(Page.Locator("#nf-clear-ext .bit-nfl-cbt i")).ToHaveClassAsync(HasClass("my-nf-clear"));
        await Expect(Page.Locator("#nf-clear-template .bit-nfl-cbt .tpl-clear")).ToHaveTextAsync("×");

        await Page.Locator("#nf-clear-template .bit-nfl-cbt").ClickAsync();
        await Expect(Input("nf-clear-template")).ToHaveValueAsync("");

        await Expect(Page.Locator("#nf-clear-readonly .bit-nfl-cbt")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Escape_RaisesOnEscapeFirstAndThenClears()
    {
        await OpenAsync(Url);

        var input = Input("nf-clear-escape");
        await Expect(input).ToHaveAttributeAsync("data-bit-esc", "claim");

        await FocusAsync("nf-clear-escape");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Text("escape-value")).ToHaveTextAsync("[null]");
        await Expect(Text("escape-count")).ToHaveTextAsync("1");
        await Expect(Text("value-at-escape")).ToHaveTextAsync("[9]");
        await Expect(Text("escape-clears")).ToHaveTextAsync("1");
        await ExpectNoAttributeAsync(input, "data-bit-esc");

        // Without a clear button Escape clears nothing.
        await ExpectNoAttributeAsync(Input("nf-escape-noclear"), "data-bit-esc");
        await FocusAsync("nf-escape-noclear");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Input("nf-escape-noclear")).ToHaveValueAsync("5");
    }

    [TestMethod]
    public async Task Escape_InsideADialog_TheFirstClearsTheFieldAndTheSecondClosesTheDialog()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-open-dialog");
        await Expect(Root("nf-dialog-host")).ToBeVisibleAsync();

        // The dialog puts the focus in the field itself (AutoFocusSelector), so nothing moves it away afterwards.
        await Expect(Input("nf-dialog")).ToBeFocusedAsync();
        await Expect(Root("nf-dialog")).ToHaveClassAsync(HasClass("bit-nfl-fcs"));
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Input("nf-dialog")).ToHaveValueAsync("");
        await Expect(Text("dialog-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Text("dialog-open")).ToHaveTextAsync("False");
        await Expect(Root("nf-dialog-host")).ToHaveCountAsync(0);
    }


    // ---------- Number format and culture ----------

    [TestMethod]
    public async Task NumberFormat_IsShownAtRestAndThePlainNumberWhileEditing()
    {
        await OpenAsync(Url);

        var input = Input("nf-n0");
        await Expect(input).ToHaveValueAsync("1,234,567,890");
        await Expect(input).ToHaveAttributeAsync("aria-valuenow", "1234567890");
        await Expect(input).ToHaveAttributeAsync("aria-valuetext", "1,234,567,890");

        await FocusAsync("nf-n0");
        await Expect(input).ToHaveValueAsync("1234567890");

        await BlurAsync();
        await Expect(input).ToHaveValueAsync("1,234,567,890");

        await Expect(Input("nf-000000")).ToHaveValueAsync("001363");
    }

    [TestMethod]
    public async Task NumberFormat_Currency_ParsesTypedNegativeAmounts()
    {
        await OpenAsync(Url);

        var expected = await Text("c2-us-expected").TextContentAsync();
        await Expect(Input("nf-c2-us")).ToHaveValueAsync(expected!);

        await TypeOverAsync("nf-c2-us", "-50");
        await BlurAsync();
        await Expect(Text("c2-us-value")).ToHaveTextAsync("[-50]");
        await Expect(Input("nf-c2-us")).ToHaveValueAsync((await Text("c2-us-negative").TextContentAsync())!);
    }

    [TestMethod]
    public async Task Culture_DeDe_FormatsAndParsesWithGermanSeparators()
    {
        await OpenAsync(Url);

        var expected = await Text("c2-de-expected").TextContentAsync();
        StringAssert.Contains(expected, "1.234,50");
        await Expect(Input("nf-c2-de")).ToHaveValueAsync(expected!);

        await TypeOverAsync("nf-de", "1.234,5");
        await BlurAsync();
        await Expect(Text("de-value")).ToHaveTextAsync("[1234.5]");
        await Expect(Input("nf-de")).ToHaveValueAsync("1234,5");
    }

    [TestMethod]
    public async Task Culture_EnUs_ParsesThousandsSeparators()
    {
        await OpenAsync(Url);

        await TypeOverAsync("nf-us", "2,345.6");
        await BlurAsync();
        await Expect(Text("us-value")).ToHaveTextAsync("[2345.6]");
        await Expect(Input("nf-us")).ToHaveValueAsync("2345.6");
    }

    [TestMethod]
    public async Task Culture_FaIr_FormatsAndParsesWithPersianSeparators()
    {
        await OpenAsync(Url);

        var decimalSeparator = (await Text("fa-decimal").TextContentAsync())!;
        var expectedDisplay = (await Text("fa-12-5").TextContentAsync())!;
        var expectedN2 = (await Text("fa-n2-expected").TextContentAsync())!;

        await Expect(Input("nf-fa-n2")).ToHaveValueAsync(expectedN2);

        await TypeOverAsync("nf-fa", $"12{decimalSeparator}5");
        await BlurAsync();
        await Expect(Text("fa-value")).ToHaveTextAsync("[12.5]");
        await Expect(Input("nf-fa")).ToHaveValueAsync(expectedDisplay);
    }

    [TestMethod]
    public async Task Culture_FaIr_WithNormalizeDigits_AcceptsPersianDigitsAndTheirDecimalSeparator()
    {
        await OpenAsync(Url);

        await TypeOverAsync("nf-fa-normalize", "۱۲٫۵");
        await BlurAsync();
        await Expect(Text("fa-normalize-value")).ToHaveTextAsync("[12.5]");

        // The typed digits stay as they were written, since they read as the very number committed.
        await Expect(Input("nf-fa-normalize")).ToHaveValueAsync("۱۲٫۵");
        await Expect(Input("nf-fa-normalize")).ToHaveAttributeAsync("aria-valuenow", "12.5");
    }


    // ---------- Input behavior ----------

    [TestMethod]
    public async Task Immediate_CommitsEveryKeystroke()
    {
        await OpenAsync(Url);

        await FocusAsync("nf-immediate");
        await Page.Keyboard.TypeAsync("123");
        await Expect(Text("immediate-value")).ToHaveTextAsync("[123]");
        await Expect(Text("immediate-count")).ToHaveTextAsync("3");
        await Expect(Input("nf-immediate")).ToHaveValueAsync("123");
    }

    [TestMethod]
    public async Task Immediate_KeepsTheTypedTextSoADecimalCanBeTyped()
    {
        await OpenAsync(Url);

        var input = Input("nf-immediate-decimal");
        await FocusAsync("nf-immediate-decimal");
        await Page.Keyboard.TypeAsync("1.");
        await Expect(Text("immediate-decimal-value")).ToHaveTextAsync("[1]");
        await Expect(input).ToHaveValueAsync("1.");

        await Page.Keyboard.TypeAsync("25");
        await Expect(Text("immediate-decimal-value")).ToHaveTextAsync("[1.25]");
        await Expect(input).ToHaveValueAsync("1.25");
    }

    [TestMethod]
    public async Task DebounceTime_CommitsOnceTheTypingPausesAndKeepsTheTypedText()
    {
        await OpenAsync(Url);

        var input = Input("nf-debounce");
        await FocusAsync("nf-debounce");
        await Page.Keyboard.TypeAsync("4567", new() { Delay = 20 });
        Assert.AreEqual("[]", await Text("debounce-value").TextContentAsync(), "Nothing is committed while the typing goes on.");
        await Expect(input).ToHaveValueAsync("4567");

        await Expect(Text("debounce-value")).ToHaveTextAsync("[4567]");
        await Expect(Text("debounce-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ThrottleTime_CommitsTheFirstKeystrokeAndThenOncePerInterval()
    {
        await OpenAsync(Url);

        await FocusAsync("nf-throttle");
        await Page.Keyboard.TypeAsync("1234", new() { Delay = 40 });
        await Expect(Text("throttle-first")).ToHaveTextAsync("[1]");
        await Expect(Text("throttle-value")).ToHaveTextAsync("[1234]");
        await Expect(Text("throttle-count")).ToHaveTextAsync("2");
        await Expect(Input("nf-throttle")).ToHaveValueAsync("1234");
    }

    [TestMethod]
    public async Task ReadOnly_BlocksTypingSpinningAndClearing()
    {
        await OpenAsync(Url);

        var input = Input("nf-readonly");
        await Expect(input).ToHaveAttributeAsync("readonly", "");
        await Expect(input).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Increment("nf-readonly")).ToBeDisabledAsync();
        await Expect(Page.Locator("#nf-readonly .bit-nfl-cbt")).ToHaveCountAsync(0);

        await FocusAsync("nf-readonly");
        await Page.Keyboard.TypeAsync("9");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("Escape");
        await BlurAsync();
        await Expect(Text("readonly-value")).ToHaveTextAsync("[5]");
        await Expect(input).ToHaveValueAsync("5");
    }

    [TestMethod]
    public async Task IsInputReadOnly_BlocksOnlyTheTyping()
    {
        await OpenAsync(Url);

        var input = Input("nf-inputreadonly");
        await Expect(input).ToHaveAttributeAsync("readonly", "");
        await ExpectNoAttributeAsync(input, "aria-readonly");

        await FocusAsync("nf-inputreadonly");
        await Page.Keyboard.TypeAsync("9");
        await Expect(input).ToHaveValueAsync("5");

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Text("inputreadonly-value")).ToHaveTextAsync("[6]");
        await Increment("nf-inputreadonly").ClickAsync();
        await Expect(Text("inputreadonly-value")).ToHaveTextAsync("[7]");
    }

    [TestMethod]
    public async Task MouseWheel_ShiftWheelSpinsAFocusedField_InvertMouseWheelReversesIt()
    {
        await OpenAsync(Url);

        // A hovered field that is not focused is left alone.
        await ShiftWheelAsync("nf-wheel", -100);
        await FocusAsync("nf-twoway");
        await Expect(Text("wheel-value")).ToHaveTextAsync("[0]");

        await FocusAsync("nf-wheel");
        await ShiftWheelAsync("nf-wheel", -100);
        await Expect(Text("wheel-value")).ToHaveTextAsync("[1]");
        await Expect(Text("wheel-increments")).ToHaveTextAsync("1");
        await ShiftWheelAsync("nf-wheel", 100);
        await Expect(Text("wheel-value")).ToHaveTextAsync("[0]");

        // Without Shift the wheel scrolls rather than spins.
        await Input("nf-wheel").HoverAsync();
        await Page.Mouse.WheelAsync(0, -100);
        await Page.ClickAsync("#btn-invert-wheel");
        await Expect(Text("wheel-value")).ToHaveTextAsync("[0]");

        await FocusAsync("nf-wheel");
        await ShiftWheelAsync("nf-wheel", -100);
        await Expect(Text("wheel-value")).ToHaveTextAsync("[-1]");
    }

    [TestMethod]
    public async Task NoMouseWheel_TheWheelNeverChangesTheValue()
    {
        await OpenAsync(Url);

        await FocusAsync("nf-nowheel");
        await ShiftWheelAsync("nf-nowheel", -100);
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Text("nowheel-value")).ToHaveTextAsync("[1]");
    }

    [TestMethod]
    public async Task EnterKeyHint_ReachesTheInput()
    {
        await OpenAsync(Url);

        await Expect(Input("nf-enterkeyhint")).ToHaveAttributeAsync("enterkeyhint", "done");
        await Expect(Input("nf-enterkeyhint")).ToHaveAttributeAsync("autocomplete", "on");
        await ExpectNoAttributeAsync(Input("nf-basic"), "enterkeyhint");
    }


    // ---------- NormalizeDigits ----------

    [TestMethod]
    public async Task NormalizeDigits_Off_PersianDigitsAreNotANumber()
    {
        await OpenAsync(Url);

        await TypeOverAsync("nf-norm-off", "۱۲۳");
        await BlurAsync();
        await Expect(Text("norm-off-value")).ToHaveTextAsync("[0]");
        await Expect(Input("nf-norm-off")).ToHaveValueAsync("۱۲۳");
    }

    [TestMethod]
    public async Task NormalizeDigits_On_PersianDigitsCommitTheNumberAndStayVisible()
    {
        await OpenAsync(Url);

        await TypeOverAsync("nf-norm-on", "۱۲۳");
        await BlurAsync();
        await Expect(Text("norm-on-value")).ToHaveTextAsync("[123]");
        await Expect(Input("nf-norm-on")).ToHaveValueAsync("۱۲۳");
        await Expect(Input("nf-norm-on")).ToHaveAttributeAsync("aria-valuenow", "123");
        await Expect(Input("nf-norm-on")).ToHaveAttributeAsync("aria-valuetext", "۱۲۳");

        await TypeOverAsync("nf-norm-decimal", "۱۲٫۵");
        await BlurAsync();
        await Expect(Text("norm-decimal-value")).ToHaveTextAsync("[12.5]");

        await TypeOverAsync("nf-norm-custom", "۱٬۲۳۴");
        await BlurAsync();
        await Expect(Text("norm-custom-value")).ToHaveTextAsync("[1234]");
    }

    [TestMethod]
    public async Task NormalizeDigits_AppliesToMinMaxAndStepToo()
    {
        await OpenAsync(Url);

        var input = Input("nf-norm-params");
        await Expect(input).ToHaveAttributeAsync("aria-valuemin", "10");
        await Expect(input).ToHaveAttributeAsync("aria-valuemax", "20");

        await FocusAsync("nf-norm-params");
        await Page.Keyboard.PressAsync("Home");
        await Expect(Text("norm-params-value")).ToHaveTextAsync("[10]");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Text("norm-params-value")).ToHaveTextAsync("[15]");
    }


    // ---------- Events ----------

    [TestMethod]
    public async Task OnIncrementAndOnDecrement_FireForEverySpinWhateverTheDevice()
    {
        await OpenAsync(Url);

        await Increment("nf-ev-spin").ClickAsync();
        await Expect(Text("increment-count")).ToHaveTextAsync("1");
        await Expect(Text("last-spin")).ToHaveTextAsync("1");

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Text("increment-count")).ToHaveTextAsync("2");

        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Text("decrement-count")).ToHaveTextAsync("1");
        await Expect(Text("last-spin")).ToHaveTextAsync("-8");

        await ShiftWheelAsync("nf-ev-spin", -100);
        await Expect(Text("increment-count")).ToHaveTextAsync("3");

        await Decrement("nf-ev-spin").ClickAsync();
        await Expect(Text("decrement-count")).ToHaveTextAsync("2");
        await Expect(Text("last-spin")).ToHaveTextAsync("-8");
    }

    [TestMethod]
    public async Task OnChange_FiresForEachCommittedChange()
    {
        await OpenAsync(Url);

        await TypeOverAsync("nf-ev-change", "5");
        await BlurAsync();
        await Expect(Text("change-count")).ToHaveTextAsync("1");
        await Expect(Text("last-change")).ToHaveTextAsync("5");

        await Increment("nf-ev-change").ClickAsync();
        await Expect(Text("change-count")).ToHaveTextAsync("2");
        await Expect(Text("last-change")).ToHaveTextAsync("6");
    }

    [TestMethod]
    public async Task OnMinReachedAndOnMaxReached_FireOncePerApproach()
    {
        await OpenAsync(Url);

        // Each press is let land before the next, the way a user reads the value between them (see the known race
        // below for presses faster than the round trip).
        await FocusAsync("nf-ev-bounds");
        foreach (var expected in new[] { "1", "2", "3" })
        {
            await Page.Keyboard.PressAsync("ArrowUp");
            await Expect(Input("nf-ev-bounds")).ToHaveValueAsync(expected);
        }
        await Expect(Text("max-reached")).ToHaveTextAsync("1");

        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.ClickAsync("#btn-set-oneway");
        await Expect(Input("nf-oneway")).ToHaveValueAsync("77");
        await Expect(Input("nf-ev-bounds")).ToHaveValueAsync("3");
        await Expect(Text("max-reached")).ToHaveTextAsync("1");

        await FocusAsync("nf-ev-bounds");
        await Page.Keyboard.PressAsync("Home");
        await Expect(Input("nf-ev-bounds")).ToHaveValueAsync("0");
        await Expect(Text("min-reached")).ToHaveTextAsync("1");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Text("min-reached")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task RapidKeys_HomeRightAfterABlockedArrowUp_EndsAtTheMin()
    {
        await OpenAsync(Url);

        await FocusAsync("nf-ev-bounds");
        await Page.Keyboard.PressAsync("End");
        await Expect(Input("nf-ev-bounds")).ToHaveValueAsync("3");

        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("Home");
        await Expect(Input("nf-ev-bounds")).ToHaveValueAsync("0");

        await Page.ClickAsync("#btn-set-oneway");
        await Expect(Input("nf-oneway")).ToHaveValueAsync("77");
        await Expect(Input("nf-ev-bounds")).ToHaveValueAsync("0");
    }

    [TestMethod]
    public async Task OnEnter_RunsAfterTheTypedTextIsCommitted()
    {
        await OpenAsync(Url);

        await FocusAsync("nf-ev-enter");
        await Page.Keyboard.TypeAsync("12");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("enter-count")).ToHaveTextAsync("1");
        await Expect(Text("value-at-enter")).ToHaveTextAsync("[12]");
    }

    [TestMethod]
    public async Task KeyClickAndFocusEvents_AreReported()
    {
        await OpenAsync(Url);

        await Input("nf-ev-keys").ClickAsync();
        await Expect(Text("ev-click")).ToHaveTextAsync("1");
        await Expect(Text("ev-focus")).ToHaveTextAsync("1");
        await Expect(Text("ev-focusin")).ToHaveTextAsync("1");

        await Page.Keyboard.PressAsync("7");
        await Expect(Text("ev-last-key")).ToHaveTextAsync("7");
        await Expect(Text("ev-keyup")).ToHaveTextAsync("1");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Text("ev-last-key")).ToHaveTextAsync("ArrowUp");

        await BlurAsync();
        await Expect(Text("ev-blur")).ToHaveTextAsync("1");
        await Expect(Text("ev-focusout")).ToHaveTextAsync("1");
    }


    // ---------- Public API ----------

    [TestMethod]
    public async Task PublicApi_IncrementDecrementClearAndFocus()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-api-inc");
        await Expect(Text("api-value")).ToHaveTextAsync("[2]");
        await Page.ClickAsync("#btn-api-inc");
        await Expect(Text("api-value")).ToHaveTextAsync("[4]");
        await Page.ClickAsync("#btn-api-dec");
        await Expect(Text("api-value")).ToHaveTextAsync("[2]");

        for (var i = 0; i < 5; i++) await Page.ClickAsync("#btn-api-inc");
        await Expect(Text("api-value")).ToHaveTextAsync("[10]");
        await Expect(Text("api-max-reached")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-api-clear");
        await Expect(Text("api-value")).ToHaveTextAsync("[null]");
        await Expect(Input("nf-api")).ToHaveValueAsync("");
        await Expect(Text("api-clears")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-api-focus");
        await Expect(Input("nf-api")).ToBeFocusedAsync();

        await Page.ClickAsync("#btn-api-focus-hide");
        await Expect(Increment("nf-api-hide")).ToBeFocusedAsync();
    }


    // ---------- Validation ----------

    [TestMethod]
    public async Task EditForm_AnEmptySubmitReportsTheRequiredField()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-submit");
        await Expect(Text("form-status")).ToHaveTextAsync("Invalid");
        await Expect(Root("nf-form-age")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("nf-form-age")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#validation .validation-message", new() { HasText = "Enter an age" })).ToBeVisibleAsync();
        await Expect(Root("nf-form-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task EditForm_ParsingErrorMessageNamesTheField_AndNoClampLetsRangeReport()
    {
        await OpenAsync(Url);

        await TypeOverAsync("nf-form-weight", "abc");
        await BlurAsync();
        await Expect(Page.Locator("#validation .validation-message", new() { HasText = "Weight must be a number." })).ToBeVisibleAsync();
        await Expect(Root("nf-form-weight")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("nf-form-weight")).ToHaveValueAsync("abc");

        await TypeOverAsync("nf-form-percentage", "500");
        await BlurAsync();
        await Expect(Input("nf-form-percentage")).ToHaveValueAsync("500");
        await Expect(Page.Locator("#validation .validation-message", new() { HasText = "The percentage must be between 0 and 100" })).ToBeVisibleAsync();
        await Expect(Root("nf-form-percentage")).ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task EditForm_TheCorrectedFormSubmits()
    {
        await OpenAsync(Url);

        await TypeOverAsync("nf-form-age", "200");
        await BlurAsync();
        await Expect(Page.Locator("#validation .validation-message", new() { HasText = "Nobody is that old" })).ToBeVisibleAsync();
        await TypeOverAsync("nf-form-age", "30");
        await BlurAsync();
        await Expect(Root("nf-form-age")).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await TypeOverAsync("nf-form-weight", "70.5");
        await BlurAsync();
        await TypeOverAsync("nf-form-percentage", "50");
        await BlurAsync();
        await TypeOverAsync("nf-form-novalidate", "5");
        await BlurAsync();

        await Page.ClickAsync("#btn-submit");
        await Expect(Text("form-status")).ToHaveTextAsync("Valid");
        await Expect(Page.Locator("#validation .bit-inv")).ToHaveCountAsync(0);
    }


    // ---------- Accessibility ----------

    [TestMethod]
    public async Task Accessibility_TheSpinbuttonStateAndTheNamedButtons()
    {
        await OpenAsync(Url);

        var input = Input("nf-a11y-volume");
        await Expect(input).ToHaveAttributeAsync("aria-valuemin", "0");
        await Expect(input).ToHaveAttributeAsync("aria-valuemax", "10");
        await Expect(input).ToHaveAttributeAsync("aria-valuenow", "0");
        await ExpectNoAttributeAsync(input, "aria-valuetext");
        await Expect(Page.Locator("#nf-a11y-volume button[aria-label='Louder']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#nf-a11y-volume button[aria-label='Quieter']")).ToHaveCountAsync(1);

        await Page.Locator("#nf-a11y-volume button[aria-label='Louder']").ClickAsync();
        await Expect(input).ToHaveAttributeAsync("aria-valuenow", "1");

        await Expect(Input("nf-a11y-rating")).ToHaveAttributeAsync("aria-valuetext", "3 out of 5 stars");

        await Expect(Input("nf-a11y-set")).ToHaveAttributeAsync("aria-setsize", "3");
        await Expect(Input("nf-a11y-set")).ToHaveAttributeAsync("aria-posinset", "2");

        await Expect(Input("nf-a11y-arialabel")).ToHaveAttributeAsync("aria-label", "Unlabelled quantity");
        await ExpectNoAttributeAsync(Input("nf-a11y-arialabel"), "aria-labelledby");
        await Expect(Box("nf-a11y-arialabel")).ToHaveAttributeAsync("title", "A tooltip");

        await Expect(Input("nf-a11y-valuenow")).ToHaveAttributeAsync("aria-valuenow", "99");
        await Expect(Input("nf-a11y-valuenow")).ToHaveValueAsync("5");
    }


    // ---------- Cascading parameters ----------

    [TestMethod]
    public async Task BitParams_CascadesItsDefaults_AndAFieldKeepsWhatItSetsItself()
    {
        await OpenAsync(Url);

        var root = Root("nf-casc-1");
        await Expect(root).ToHaveClassAsync(HasClass("bit-nfl-und"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-nfl-mcp"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-nfl-inf"));
        await Expect(Page.Locator("#nf-casc-1 .bit-nfl-suf")).ToHaveTextAsync("cm");
        await Expect(Page.Locator("#nf-casc-1 .bit-nfl-des")).ToHaveTextAsync("Between 0 and 500, in steps of 5.");
        await Expect(Input("nf-casc-1")).ToHaveAttributeAsync("aria-valuemax", "500");

        await FocusAsync("nf-casc-1");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Input("nf-casc-1")).ToHaveValueAsync("5");
        await Expect(Page.Locator("#nf-casc-1 .bit-nfl-cbt")).ToBeVisibleAsync();

        await Expect(Page.Locator("#nf-casc-own .bit-nfl-suf")).ToHaveTextAsync("m");

        var outside = Root("nf-casc-outside");
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-nfl-und"));
        await Expect(Page.Locator("#nf-casc-outside button")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#nf-casc-outside .bit-nfl-suf")).ToHaveCountAsync(0);
    }


    // ---------- Background, Border, Accent, Size ----------

    [TestMethod]
    [DataRow("primary", "bit-nfl-bpr", "var(--bit-clr-bg-pri)")]
    [DataRow("secondary", "bit-nfl-bse", "var(--bit-clr-bg-sec)")]
    [DataRow("tertiary", "bit-nfl-btr", "var(--bit-clr-bg-ter)")]
    [DataRow("transparent", "bit-nfl-btn", "transparent")]
    public async Task Background_PaintsTheBoxFromItsColorKind(string kind, string cssClass, string token)
    {
        await OpenAsync(Url);

        var id = $"nf-bg-{kind}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));
        await Expect(Box(id)).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", token));
    }

    [TestMethod]
    [DataRow("primary", "bit-nfl-brp", "var(--bit-clr-brd-pri)")]
    [DataRow("secondary", "bit-nfl-brs", "var(--bit-clr-brd-sec)")]
    [DataRow("tertiary", "bit-nfl-brt", "var(--bit-clr-brd-ter)")]
    [DataRow("transparent", "bit-nfl-brn", "transparent")]
    public async Task Border_PaintsTheBoxFromItsColorKind(string kind, string cssClass, string token)
    {
        await OpenAsync(Url);

        var id = $"nf-brd-{kind}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));
        await Expect(Box(id)).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", token));
        Assert.AreNotEqual("0px", await ComputedStyleAsync($"#{id} .bit-nfl-cnt", "border-top-width"));
    }

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
    public async Task Accent_ColorsTheIconAndPrefixWhileFocused(string color, string suffix, string token)
    {
        await OpenAsync(Url);

        var id = $"nf-acc-{color}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-nfl-{suffix}"));
        await Expect(Root("nf-acc-none")).Not.ToHaveClassAsync(HasClass($"bit-nfl-{suffix}"));

        await FocusAsync(id);
        var expected = await ColorOfAsync(token);
        await Expect(Page.Locator($"#{id} .bit-nfl-lic")).ToHaveCSSAsync("color", expected);
        await Expect(Page.Locator($"#{id} .bit-nfl-pre")).ToHaveCSSAsync("color", expected);
    }

    [TestMethod]
    [DataRow("small", "bit-nfl-sm", "--bit-siz-ctrl-sm", "--bit-tpg-fs-xs")]
    [DataRow("medium", "bit-nfl-md", "--bit-siz-ctrl-md", "--bit-tpg-fs-sm")]
    [DataRow("large", "bit-nfl-lg", "--bit-siz-ctrl-lg", "--bit-tpg-fs-md")]
    public async Task Size_ScalesTheBoxAndTheText(string size, string cssClass, string heightToken, string fontToken)
    {
        await OpenAsync(Url);

        var id = $"nf-size-{size}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));
        await Expect(Box(id)).ToHaveCSSAsync("height", await ResolveAsync("height", SizeToken(heightToken)));
        await Expect(Input(id)).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", $"var({fontToken})"));
    }


    // ---------- External icons ----------

    [TestMethod]
    public async Task ExternalIcons_ForTheIconAndTheSpinButtons()
    {
        await OpenAsync(Url);

        var icon = Page.Locator("#nf-ext-string .bit-nfl-lic");
        await Expect(icon).ToHaveClassAsync(HasClass("my-nf-icon"));
        Assert.AreEqual("\"N\"", await PseudoContentAsync(icon, "::before"));
        await Expect(Page.Locator("#nf-ext-fa .bit-nfl-lic")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid fa-lightbulb(\s|$)"));
        await Expect(Page.Locator("#nf-ext-bi .bit-nfl-lic")).ToHaveClassAsync(new Regex(@"(^|\s)bi bi-calculator(\s|$)"));
        await Expect(Page.Locator("#nf-ext-spin .bit-nfl-aup i")).ToHaveClassAsync(HasClass("my-nf-up"));
        await Expect(Page.Locator("#nf-ext-spin .bit-nfl-adn i")).ToHaveClassAsync(HasClass("my-nf-down"));
    }


    // ---------- Style and Class ----------

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRoot()
    {
        await OpenAsync(Url);

        StringAssert.Contains(await ComputedStyleAsync("#nf-style", "box-shadow"), "rgb(0, 255, 255)");
        await Expect(Root("nf-class")).ToHaveClassAsync(HasClass("custom-class"));
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachEveryPart_AndFocusedOnlyWhileFocused()
    {
        await OpenAsync(Url);

        await Expect(Root("nf-styles")).ToHaveCSSAsync("margin-left", "16px");
        await Expect(Page.Locator("#nf-styles .bit-nfl-lbl")).ToHaveCSSAsync("font-weight", "900");
        await Expect(Page.Locator("#nf-styles .bit-nfl-des span")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Input("nf-styles")).ToHaveCSSAsync("padding-left", "9px");
        await Expect(Box("nf-styles")).ToHaveCSSAsync("background-color", "rgb(200, 200, 200)");
        await FocusAsync("nf-styles");
        await Expect(Box("nf-styles")).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");

        await Expect(Root("nf-classes")).ToHaveClassAsync(HasClass("custom-root"));
        await Expect(Box("nf-classes")).ToHaveClassAsync(HasClass("custom-input-wrapper"));
        await Expect(Input("nf-classes")).ToHaveClassAsync(HasClass("custom-input"));
        await Expect(Page.Locator("#nf-classes .bit-nfl-lbl")).ToHaveClassAsync(HasClass("custom-label"));
        await Expect(Root("nf-classes")).Not.ToHaveClassAsync(HasClass("custom-focus"));
        await FocusAsync("nf-classes");
        await Expect(Root("nf-classes")).ToHaveClassAsync(HasClass("custom-focus"));
        await BlurAsync();
        await Expect(Root("nf-classes")).Not.ToHaveClassAsync(HasClass("custom-focus"));
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestorAndOnTheStyle_ReskinTheField_AndAnExplicitParameterWins()
    {
        await OpenAsync(Url);

        var box = Box("nf-cssvar-ancestor");
        await Expect(box).ToHaveCSSAsync("border-top-left-radius", "20px");
        await Expect(box).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(box).ToHaveCSSAsync("height", "50px");
        await Expect(Page.Locator("#nf-cssvar-ancestor .bit-nfl-lbl")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");

        var explicitBox = Box("nf-cssvar-explicit");
        await Expect(explicitBox).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-bg-sec)"));
        await Expect(explicitBox).ToHaveCSSAsync("height", await ResolveAsync("height", SizeToken("--bit-siz-ctrl-sm")));
        await Expect(explicitBox).ToHaveCSSAsync("border-top-left-radius", "20px");

        await Expect(Box("nf-cssvar-style")).ToHaveCSSAsync("border-top-color", "rgb(7, 8, 9)");
        await Expect(Box("nf-cssvar-style")).ToHaveCSSAsync("border-top-width", "2px");
    }

    [TestMethod]
    public async Task Hover_DarkensTheBorderOfAnEditableField()
    {
        await OpenAsync(Url);

        var box = Box("nf-default-look");
        await Expect(box).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-brd-pri)"));
        await box.HoverAsync();
        await Expect(box).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-brd-pri-hover)"));
    }

    [TestMethod]
    public async Task Focus_DrawsTheRingOnTheBox()
    {
        await OpenAsync(Url);

        await Expect(Box("nf-default-look")).ToHaveCSSAsync("box-shadow", "none");
        await FocusAsync("nf-default-look");
        await Expect(Box("nf-default-look")).Not.ToHaveCSSAsync("box-shadow", "none");
    }


    // ---------- RTL ----------

    [TestMethod]
    public async Task Rtl_MirrorsTheButtonsTheSuffixAndTheLabel()
    {
        await OpenAsync(Url);

        await Expect(Root("nf-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("nf-rtl")).ToHaveAttributeAsync("dir", "rtl");

        var ltrInput = await BoxOfAsync(Input("nf-ltr"));
        Assert.IsTrue((await BoxOfAsync(Decrement("nf-ltr"))).X < ltrInput.X, "LTR: decrease on the left.");

        var input = await BoxOfAsync(Input("nf-rtl"));
        var down = await BoxOfAsync(Decrement("nf-rtl"));
        var up = await BoxOfAsync(Increment("nf-rtl"));
        var suffix = await BoxOfAsync(Page.Locator("#nf-rtl .bit-nfl-suf"));
        var label = await BoxOfAsync(Page.Locator("#nf-rtl .bit-nfl-lbl"));
        var box = await BoxOfAsync(Box("nf-rtl"));

        Assert.IsTrue(down.X >= input.X + input.Width - 1, "RTL: decrease on the right.");
        Assert.IsTrue(up.X + up.Width <= input.X + 1, "RTL: increase on the left.");
        Assert.IsTrue(suffix.X < input.X, "RTL: the suffix on the left.");
        Assert.IsTrue(label.X >= box.X + box.Width - 1, "RTL: a Start label on the right.");
    }

    [TestMethod]
    public async Task Rtl_FromACascadingDirOrAnAncestor_LaysTheFieldOutTheSameWay()
    {
        await OpenAsync(Url);

        await Expect(Root("nf-rtl-cascade")).ToHaveClassAsync(HasClass("bit-rtl"));
        var input = await BoxOfAsync(Input("nf-rtl-cascade"));
        Assert.IsTrue((await BoxOfAsync(Decrement("nf-rtl-cascade"))).X >= input.X + input.Width - 1);

        await Expect(Root("nf-rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        var inherited = await BoxOfAsync(Input("nf-rtl-inherited"));
        Assert.IsTrue((await BoxOfAsync(Decrement("nf-rtl-inherited"))).X >= inherited.X + inherited.Width - 1);
        Assert.IsTrue((await BoxOfAsync(Page.Locator("#nf-rtl-inherited .bit-nfl-suf"))).X < inherited.X);
    }


    // ---------- Reduced motion ----------

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheBorderTransition_UnlessForceAnimation()
    {
        await OpenAsync(Url);

        var normal = await ComputedStyleAsync("#nf-motion .bit-nfl-cnt", "transition-duration");
        Assert.IsTrue(MaxSeconds(normal) > 0.01, normal);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        var reduced = await ComputedStyleAsync("#nf-motion .bit-nfl-cnt", "transition-duration");
        Assert.IsTrue(MaxSeconds(reduced) < 0.011, reduced);

        await Expect(Root("nf-motion-force")).ToHaveClassAsync(HasClass("bit-fam"));
        var forced = await ComputedStyleAsync("#nf-motion-force .bit-nfl-cnt", "transition-duration");
        Assert.IsTrue(MaxSeconds(forced) > 0.01, forced);
    }

    internal static double MaxSeconds(string durations) =>
        durations.Split(',').Select(d => d.Trim()).Select(d => d.EndsWith("ms") ? double.Parse(d[..^2], System.Globalization.CultureInfo.InvariantCulture) / 1000
                                                                                : double.Parse(d.TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture)).Max();
}

/// <summary>
/// Drives BitNumberField (/e2e/number-field/touch) on a phone: a tap on a spin button steps the value without focusing the
/// input (no soft keyboard) and announces it, and the compact stack grows to a usable target on a coarse pointer.
/// </summary>
[TestClass]
public class BitNumberFieldTouchE2ETests : E2ETestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
        IsMobile = IsChromium
    };

    [TestMethod]
    public async Task Touch_ATapOnASpinButtonStepsWithoutFocusingTheInputAndAnnouncesTheValue()
    {
        await OpenAsync("/e2e/number-field/touch");

        await Page.Locator("#nf-touch .bit-nfl-aup").TapAsync();

        await Expect(Page.Locator("#touch-value")).ToHaveTextAsync("[1]");
        await Expect(Page.Locator("#nf-touch .bit-nfl-inp")).Not.ToBeFocusedAsync();
        await Expect(Page.Locator("#nf-touch [role=status]")).ToHaveTextAsync("1");

        await Page.Locator("#nf-touch .bit-nfl-adn").TapAsync();
        await Expect(Page.Locator("#touch-value")).ToHaveTextAsync("[0]");
    }

    [TestMethod]
    public async Task Touch_TheCompactStackGrowsToAUsableTarget()
    {
        if (IsFirefox) Assert.Inconclusive("Firefox does not emulate a coarse pointer for a touch context.");

        await OpenAsync("/e2e/number-field/touch");

        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => matchMedia('(pointer: coarse)').matches"), "The context has a coarse pointer.");
        var height = await Page.Locator("#nf-touch .bit-nfl-cnt").EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        Assert.IsTrue(height >= 47.5, $"Each half of the stack clears 24px ({height}px).");
        foreach (var button in new[] { "#nf-touch .bit-nfl-aup", "#nf-touch .bit-nfl-adn", "#nf-touch-hide .bit-nfl-aup", "#nf-touch-hide .bit-nfl-adn" })
        {
            var size = await Page.Locator(button).EvaluateAsync<double>("el => el.getBoundingClientRect().height");
            Assert.IsTrue(size >= 23.9, $"{button} is at least 24px tall ({size}px).");
        }
    }
}
