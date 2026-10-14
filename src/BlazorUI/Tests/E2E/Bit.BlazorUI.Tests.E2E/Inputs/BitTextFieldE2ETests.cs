using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// Drives BitTextField (/e2e/text-field and /e2e/text-field/autofocus) in a real browser: every section of its demo
/// page and every parameter of its table - the label, the description, the disabled, read-only and required states,
/// the variants, the multiline mode with its auto growing, the input types, the icon and the affixes, the password
/// reveal, one-way, two-way, immediate, debounced and throttled binding, the clear button and ClearOnEscape (inside a
/// BitDialog too), the counter, every template, the ghost text, the events, the selection API, the label positions,
/// the busy state, the accessibility wiring, validation in an EditForm and of its own, the BitParams cascade, the
/// Background, Border, Accent and Size values, external icons, Style/Class/Styles/Classes and the public CSS
/// variables, RTL, reduced motion, prerendering and unmounting.
/// </summary>
[TestClass]
public class BitTextFieldE2ETests : E2ETestBase
{
    private const string Url = "/e2e/text-field";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Input(string id) => Page.Locator($"#{id} .bit-tfl-inp");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    private Task BlurAsync() => Page.Locator("h1").ClickAsync();

    private static Task ExpectNoAttributeAsync(ILocator locator, string name) =>
        Expect(locator).Not.ToHaveAttributeAsync(name, new Regex("[\\s\\S]*"));

    // What a CSS value resolves to on this page, read off a probe element: how a theme token is turned into the
    // computed value a part of the field is expected to have.
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

    // A size token whose alias computes it from density steps when the token itself is not declared.
    private static string SizeToken(string token) =>
        $"var({token}, calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * var({token}-steps)))";

    private static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no box.");
        return box;
    }

    private static Task<string> PseudoContentAsync(ILocator locator, string pseudo) =>
        locator.EvaluateAsync<string>($"el => getComputedStyle(el, '{pseudo}').content");

    private static Task<double> HeightAsync(ILocator locator) =>
        locator.EvaluateAsync<double>("el => el.getBoundingClientRect().height");

    // Waits, the way an expectation does, for the height of an element to pass a bound: how a growth that a script
    // measures after the keystroke is waited for.
    private static Task WaitForHeightAsync(ILocator locator, string comparison, double bound) =>
        locator.EvaluateAsync(@"(el, [op, bound]) => new Promise((resolve, reject) => {
            const started = performance.now();
            const check = () => {
                const h = el.getBoundingClientRect().height;
                if (op === '>' ? h > bound : h < bound) return resolve();
                if (performance.now() - started > 10000) return reject(new Error('height ' + h + ' is not ' + op + ' ' + bound));
                requestAnimationFrame(check);
            };
            check();
        })", new object[] { comparison, bound });


    // ---------- Lifecycle ----------

    [TestMethod]
    public async Task Prerender_TheServerHtmlCarriesTheFieldBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"tf-basic\"");
        StringAssert.Contains(html, "class=\"bit-tfl");
        StringAssert.Contains(html, "id=\"status\">Loading<");

        await OpenAsync(Url);
        await Expect(Root("tf-basic")).ToHaveClassAsync(HasClass("bit-tfl"));
    }

    [TestMethod]
    public async Task Unmount_RemovingAFieldWithEveryScriptFeatureWired_LeavesNoError()
    {
        await OpenAsync(Url);

        await Input("tf-mount").FocusAsync();
        await Expect(Root("tf-mount")).ToHaveClassAsync(HasClass("bit-tfl-fcs"));

        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Text("mounted")).ToHaveTextAsync("False");
        await Expect(Root("tf-mount")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Root("tf-mount")).ToHaveCountAsync(1);
        await Expect(Input("tf-mount")).ToHaveValueAsync("mounted");

        await Page.ClickAsync("#btn-toggle-mount");
        await Expect(Root("tf-mount")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }


    // ---------- Basics ----------

    [TestMethod]
    public async Task Basic_TypingAndBlurring_CommitsTheValue()
    {
        await OpenAsync(Url);

        await Input("tf-basic").PressSequentiallyAsync("hello");
        await Expect(Input("tf-basic")).ToHaveValueAsync("hello");
        await Expect(Text("basic-value")).ToHaveTextAsync("[]");

        await BlurAsync();
        await Expect(Text("basic-value")).ToHaveTextAsync("[hello]");
    }

    [TestMethod]
    public async Task AutoFocus_PutsTheCaretInTheFieldOnTheFirstRender_AndSelectOnFocusSelectsIt()
    {
        await OpenAsync("/e2e/text-field/autofocus");

        var input = Input("tf-autofocus");
        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveAttributeAsync("autofocus", "");
        await Expect(input).ToHaveJSPropertyAsync("selectionStart", 0);
        await Expect(input).ToHaveJSPropertyAsync("selectionEnd", "Selected on load".Length);
        await ExpectNoAttributeAsync(Input("tf-plain"), "autofocus");

        await Page.Keyboard.TypeAsync("new");
        await Expect(input).ToHaveValueAsync("new");
    }

    [TestMethod]
    public async Task Label_ClickingTheLabel_FocusesTheInputThatItNames()
    {
        await OpenAsync(Url);

        var label = Page.Locator("#tf-basic .bit-tfl-lbl");
        var input = Input("tf-basic");
        var inputId = await input.GetAttributeAsync("id");
        var labelId = await label.GetAttributeAsync("id");

        await Expect(label).ToHaveTextAsync("Basic");
        await Expect(label).ToHaveAttributeAsync("for", inputId!);
        await Expect(input).ToHaveAttributeAsync("aria-labelledby", labelId!);

        await label.ClickAsync();
        await Expect(input).ToBeFocusedAsync();
        await Expect(Root("tf-basic")).ToHaveClassAsync(HasClass("bit-tfl-fcs"));
    }

    [TestMethod]
    public async Task Placeholder_IsShownUntilSomethingIsTyped()
    {
        await OpenAsync(Url);

        var input = Input("tf-placeholder");
        await Expect(input).ToHaveAttributeAsync("placeholder", "Enter a text...");
        Assert.IsTrue(await input.EvaluateAsync<bool>("el => el.matches(':placeholder-shown')"));

        await input.PressSequentiallyAsync("x");
        Assert.IsFalse(await input.EvaluateAsync<bool>("el => el.matches(':placeholder-shown')"));
    }

    [TestMethod]
    public async Task Disabled_RendersTheDisabledMarkupOnTheRootAndTheControls()
    {
        await OpenAsync(Url);

        await Expect(Root("tf-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("tf-disabled")).ToBeDisabledAsync();
        await Expect(Input("tf-disabled")).ToHaveAttributeAsync("aria-disabled", "true");

        var clear = Page.Locator("#tf-disabled .bit-tfl-cbt");
        await Expect(clear).ToBeDisabledAsync();
        await Expect(clear).ToHaveAttributeAsync("aria-disabled", "true");

        // The disabled frame is painted from the disabled tokens.
        var fgp = Page.Locator("#tf-disabled .bit-tfl-fgp");
        await Expect(fgp).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-bg-dis)"));
        await Expect(Input("tf-disabled")).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-fg-dis"));
    }

    [TestMethod]
    public async Task Disabled_ARealClickRunsNoneOfThePagesActivationHandlers_ButTheHoverOnesStay()
    {
        await OpenAsync(Url);

        // Force: Playwright counts the label of a disabled input as disabled itself and would wait for it, while a
        // user can still press it.
        await Page.Locator("#tf-disabled .bit-tfl-lbl").ClickAsync(new() { Force = true });
        await Page.Locator("#tf-disabled .bit-tfl-fgp").ClickAsync(new() { Force = true });

        await Page.Locator("#tf-disabled .bit-tfl-lbl").HoverAsync();
        await Expect(Text("disabled-root-hovers")).Not.ToHaveTextAsync("0");

        await Expect(Text("disabled-root-clicks")).ToHaveTextAsync("0");
        await Expect(Text("disabled-clicks")).ToHaveTextAsync("0");
        await Expect(Text("disabled-focuses")).ToHaveTextAsync("0");
        await Expect(Input("tf-disabled")).Not.ToBeFocusedAsync();

        // Enabled again, the same click reaches the page's handler and the field's own.
        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(Root("tf-disabled")).Not.ToHaveClassAsync(HasClass("bit-dis"));

        await Input("tf-disabled").ClickAsync();
        await Expect(Text("disabled-root-clicks")).ToHaveTextAsync("1");
        await Expect(Text("disabled-clicks")).ToHaveTextAsync("1");
        await Expect(Text("disabled-focuses")).ToHaveTextAsync("1");

        await Page.Keyboard.PressAsync("a");
        await Expect(Text("disabled-root-keys")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Disabled_TheTabKeySkipsTheField()
    {
        await OpenAsync(Url);

        await Page.Locator("#before-disabled").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        await Expect(Page.Locator("#after-disabled")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Disabled_TurnedOffAtRuntime_TheFieldBecomesEditable()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-toggle-disabled");

        await Expect(Root("tf-disabled")).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("tf-disabled")).ToBeEnabledAsync();
        await ExpectNoAttributeAsync(Input("tf-disabled"), "aria-disabled");
        await Expect(Page.Locator("#tf-disabled .bit-tfl-cbt")).ToBeEnabledAsync();

        await Input("tf-disabled").PressSequentiallyAsync("!");
        await Expect(Input("tf-disabled")).ToHaveValueAsync(new Regex("!"));

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(Root("tf-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("tf-disabled")).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task ReadOnly_KeepsTheFieldFocusableButRefusesEdits_UntilItIsTurnedOff()
    {
        await OpenAsync(Url);

        var input = Input("tf-readonly");

        await Expect(Root("tf-readonly")).ToHaveClassAsync(HasClass("bit-tfl-rdl"));
        await Expect(input).ToHaveAttributeAsync("readonly", "");
        await Expect(input).ToHaveCSSAsync("cursor", "default");

        await input.ClickAsync();
        await Expect(input).ToBeFocusedAsync();
        await Page.Keyboard.TypeAsync("xyz");
        await Expect(input).ToHaveValueAsync("This is ReadOnly");
        await BlurAsync();
        await Expect(Text("readonly-changes")).ToHaveTextAsync("0");

        await Page.ClickAsync("#btn-toggle-readonly");
        await Expect(Root("tf-readonly")).Not.ToHaveClassAsync(HasClass("bit-tfl-rdl"));
        await ExpectNoAttributeAsync(input, "readonly");

        await input.ClickAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.TypeAsync("!");
        await BlurAsync();
        await Expect(Text("readonly-changes")).ToHaveTextAsync("1");
        await Expect(Text("readonly-value")).ToHaveTextAsync("[This is ReadOnly!]");
    }

    [TestMethod]
    public async Task Description_IsRenderedUnderTheFieldAndReferencedByTheInput()
    {
        await OpenAsync(Url);

        var description = Page.Locator("#tf-description .bit-tfl-des");
        await Expect(description).ToHaveTextAsync("This is Description");

        var id = await description.GetAttributeAsync("id");
        await Expect(Input("tf-description")).ToHaveAttributeAsync("aria-describedby", id!);

        var frame = await BoxAsync(Page.Locator("#tf-description .bit-tfl-fgp"));
        var under = await BoxAsync(description);
        Assert.IsTrue(under.Y >= frame.Y + frame.Height - 1, "The description sits under the frame.");
    }

    [TestMethod]
    public async Task Required_MarksTheInputAndTheLabel_OrTheFrameWhenThereIsNoLabel()
    {
        await OpenAsync(Url);

        await Expect(Input("tf-required")).ToHaveAttributeAsync("required", "");
        await Expect(Root("tf-required")).ToHaveClassAsync(HasClass("bit-tfl-req"));
        Assert.AreEqual("\"*\"", await PseudoContentAsync(Page.Locator("#tf-required .bit-tfl-lbl"), "::after"));
        Assert.AreEqual(await ColorOfAsync("--bit-clr-req"),
                        await Page.Locator("#tf-required .bit-tfl-lbl").EvaluateAsync<string>("el => getComputedStyle(el, '::after').color"));

        await Expect(Root("tf-required-nolabel")).ToHaveClassAsync(HasClass("bit-tfl-rnl"));
        Assert.AreEqual("\"*\"", await PseudoContentAsync(Page.Locator("#tf-required-nolabel .bit-tfl-fgp"), "::before"));

        // Neither marker on a field that is not required.
        Assert.AreEqual("none", await PseudoContentAsync(Page.Locator("#tf-basic .bit-tfl-lbl"), "::after"));
    }

    [TestMethod]
    public async Task MaxLength_TheKeyboardStopsAtTheLimit()
    {
        await OpenAsync(Url);

        var input = Input("tf-maxlength");
        await Expect(input).ToHaveAttributeAsync("maxlength", "5");

        await input.PressSequentiallyAsync("abcdefgh");
        await Expect(input).ToHaveValueAsync("abcde");

        await BlurAsync();
        await Expect(Text("maxlength-value")).ToHaveTextAsync("[abcde]");
    }

    [TestMethod]
    public async Task MinLength_TheBrowserReportsAValueThatIsTooShort()
    {
        await OpenAsync(Url);

        var input = Input("tf-minlength");
        await Expect(input).ToHaveAttributeAsync("minlength", "3");

        await input.PressSequentiallyAsync("ab");
        Assert.IsTrue(await input.EvaluateAsync<bool>("el => el.validity.tooShort"));

        await input.PressSequentiallyAsync("c");
        Assert.IsFalse(await input.EvaluateAsync<bool>("el => el.validity.tooShort"));

        // No constraint is rendered at all when the limits are left at their defaults.
        await ExpectNoAttributeAsync(Input("tf-basic"), "maxlength");
        await ExpectNoAttributeAsync(Input("tf-basic"), "minlength");
    }

    [TestMethod]
    public async Task FullWidth_FillsTheWidthOfItsContainer()
    {
        await OpenAsync(Url);

        await Expect(Root("tf-fullwidth")).ToHaveClassAsync(HasClass("bit-tfl-fwd"));

        var container = await BoxAsync(Page.Locator("#fullwidth-box"));
        var full = await BoxAsync(Root("tf-fullwidth"));
        var notFull = await BoxAsync(Root("tf-notfullwidth"));

        Assert.AreEqual(container.Width, full.Width, 1);
        Assert.IsTrue(notFull.Width < container.Width - 50, $"An inline field sizes to its content ({notFull.Width} of {container.Width}).");
    }

    [TestMethod]
    public async Task BaseParameters_IdClassStyleHtmlAttributesAndTabIndexReachTheirElements()
    {
        await OpenAsync(Url);

        var root = Root("tf-base");
        await Expect(root).ToHaveClassAsync(HasClass("my-root-class"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-tfl"));
        await Expect(root).ToHaveCSSAsync("margin-top", "7px");
        await Expect(root).ToHaveAttributeAsync("data-test", "base-attr");
        await Expect(Input("tf-base")).ToHaveAttributeAsync("tabindex", "-1");

        // A tabindex of -1 takes the input out of the tab sequence.
        await Input("tf-rerender").FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Input("tf-base")).Not.ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheSpace_CollapsedRemovesIt()
    {
        await OpenAsync(Url);

        var root = Root("tf-visibility");
        var height = await HeightAsync(root);
        Assert.IsTrue(height > 20);

        await Page.ClickAsync("#btn-vis-hidden");
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");
        await Expect(root).Not.ToBeVisibleAsync();
        Assert.AreEqual(height, await HeightAsync(root), 1);

        await Page.ClickAsync("#btn-vis-collapsed");
        await Expect(root).ToHaveCSSAsync("display", "none");
        Assert.AreEqual(0, await HeightAsync(root), 0.5);

        await Page.ClickAsync("#btn-vis-visible");
        await Expect(root).ToBeVisibleAsync();
        await Expect(root).ToHaveCSSAsync("visibility", "visible");
    }

    [TestMethod]
    public async Task Rerender_ChangingParametersAtRuntime_UpdatesTheField()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#tf-rerender .bit-tfl-lbl")).ToHaveTextAsync("First label");
        await Expect(Input("tf-rerender")).ToHaveAttributeAsync("placeholder", "First placeholder");
        await Expect(Page.Locator("#tf-rerender .bit-tfl-des")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-change-params");

        await Expect(Page.Locator("#tf-rerender .bit-tfl-lbl")).ToHaveTextAsync("Second label");
        await Expect(Input("tf-rerender")).ToHaveAttributeAsync("placeholder", "Second placeholder");
        await Expect(Page.Locator("#tf-rerender .bit-tfl-des")).ToHaveTextAsync("A description added at runtime");
    }


    // ---------- Variants ----------

    [TestMethod]
    public async Task Underlined_DropsTheBoxKeepsTheRuleAndPutsTheLabelOnTheRow()
    {
        await OpenAsync(Url);

        await Expect(Root("tf-underlined")).ToHaveClassAsync(HasClass("bit-tfl-und"));
        await Expect(Page.Locator("#tf-underlined .bit-tfl-fgp")).ToHaveCSSAsync("border-top-width", "0px");
        Assert.AreNotEqual("0px", await ComputedStyleAsync("#tf-underlined .bit-tfl-wrp", "border-bottom-width"));
        Assert.AreNotEqual("0px", await ComputedStyleAsync("#tf-default-variant .bit-tfl-fgp", "border-top-width"));

        var label = await BoxAsync(Page.Locator("#tf-underlined .bit-tfl-lbl"));
        var frame = await BoxAsync(Page.Locator("#tf-underlined .bit-tfl-fgp"));
        Assert.IsTrue(label.X + label.Width <= frame.X + 1, "The label sits before the input on its row.");
        Assert.AreEqual(label.Y + label.Height / 2, frame.Y + frame.Height / 2, 4);

        var defaultLabel = await BoxAsync(Page.Locator("#tf-default-variant .bit-tfl-lbl"));
        var defaultFrame = await BoxAsync(Page.Locator("#tf-default-variant .bit-tfl-fgp"));
        Assert.IsTrue(defaultLabel.Y + defaultLabel.Height <= defaultFrame.Y + 1, "The default variant puts the label above.");
    }

    [TestMethod]
    public async Task NoBorder_TakesTheFrameAway()
    {
        await OpenAsync(Url);

        await Expect(Root("tf-noborder")).ToHaveClassAsync(HasClass("bit-tfl-nbd"));
        await Expect(Page.Locator("#tf-noborder .bit-tfl-fgp")).ToHaveCSSAsync("border-top-width", "0px");
        await Expect(Page.Locator("#tf-noborder .bit-tfl-fgp")).ToHaveCSSAsync("border-left-width", "0px");
    }

    [TestMethod]
    public async Task Underlined_RequiredAndDisabled_KeepTheirMarkings()
    {
        await OpenAsync(Url);

        Assert.AreEqual("\"*\"", await PseudoContentAsync(Page.Locator("#tf-und-req .bit-tfl-lbl"), "::after"));
        await Expect(Root("tf-und-dis")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("tf-und-dis")).ToBeDisabledAsync();
        // The rule under a disabled underlined field stays, in the disabled border color, and no frame comes back.
        await Expect(Page.Locator("#tf-und-dis .bit-tfl-wrp")).ToHaveCSSAsync("border-bottom-color", await ResolveAsync("border-bottom-color", "var(--bit-clr-brd-dis)"));
        await Expect(Page.Locator("#tf-und-dis .bit-tfl-fgp")).ToHaveCSSAsync("border-top-width", "0px");
    }

    [TestMethod]
    public async Task Underlined_ALongLabelIsCutShortAndOfferedAsItsTooltip()
    {
        await OpenAsync(Url);

        var label = Page.Locator("#tf-und-long .bit-tfl-lbl");
        await Expect(label).ToHaveAttributeAsync("title", "A rather long label that the underlined variant cuts short with an ellipsis");
        await Expect(Page.Locator("#tf-und-long .bit-tfl-ltx")).ToHaveCSSAsync("text-overflow", "ellipsis");

        var text = Page.Locator("#tf-und-long .bit-tfl-ltx");
        Assert.IsTrue(await text.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"), "The label is cut short.");

        // The default variant keeps its label whole and has no tooltip to offer.
        await ExpectNoAttributeAsync(Page.Locator("#tf-default-variant .bit-tfl-lbl"), "title");
    }


    // ---------- Multiline ----------

    [TestMethod]
    public async Task Multiline_RendersATextareaWithoutAGripThatTakesNewLines()
    {
        await OpenAsync(Url);

        var input = Input("tf-multiline");
        Assert.AreEqual("textarea", await input.EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
        await Expect(Root("tf-multiline")).ToHaveClassAsync(HasClass("bit-tfl-mlf"));
        await Expect(input).ToHaveCSSAsync("resize", "none");

        await input.ClickAsync();
        await Page.Keyboard.TypeAsync("a");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.TypeAsync("b");
        await Expect(input).ToHaveValueAsync("a\nb");
    }

    [TestMethod]
    public async Task Resizable_GivesBackTheVerticalGrip()
    {
        await OpenAsync(Url);

        await Expect(Root("tf-resizable")).ToHaveClassAsync(HasClass("bit-tfl-mln"));
        await Expect(Input("tf-resizable")).ToHaveCSSAsync("resize", "vertical");
    }

    [TestMethod]
    public async Task Rows_SetsTheHeightOfTheTextarea()
    {
        await OpenAsync(Url);

        await Expect(Input("tf-rows")).ToHaveAttributeAsync("rows", "10");
        await Expect(Input("tf-multiline")).ToHaveAttributeAsync("rows", "1");

        var tall = await HeightAsync(Input("tf-rows"));
        var single = await HeightAsync(Input("tf-multiline"));
        Assert.IsTrue(tall > single * 3, $"Ten rows ({tall}px) are much taller than one ({single}px).");
    }

    [TestMethod]
    public async Task AutoHeight_GrowsWithTheContentWithoutAScrollbar()
    {
        await OpenAsync(Url);

        var input = Input("tf-autoheight");
        await Expect(Root("tf-autoheight")).ToHaveClassAsync(HasClass("bit-tfl-mla"));
        await Expect(input).ToHaveCSSAsync("resize", "none");

        var before = await HeightAsync(input);

        await input.ClickAsync();
        for (var i = 0; i < 5; i++)
        {
            await Page.Keyboard.TypeAsync($"line {i}");
            await Page.Keyboard.PressAsync("Enter");
        }

        await WaitForHeightAsync(input, ">", before + 60);
        Assert.IsTrue(await input.EvaluateAsync<bool>("el => el.scrollHeight <= el.clientHeight + 1"), "The whole content fits.");
    }

    [TestMethod]
    public async Task AutoHeight_ShrinksBackButNeverBelowItsRows()
    {
        await OpenAsync(Url);

        var input = Input("tf-autoheight-rows");
        var floor = await HeightAsync(input);
        var oneRow = await HeightAsync(Input("tf-autoheight"));
        Assert.IsTrue(floor > oneRow * 2, $"Four rows ({floor}px) are the starting height, not one ({oneRow}px).");

        await input.ClickAsync();
        for (var i = 0; i < 8; i++)
        {
            await Page.Keyboard.TypeAsync($"line {i}");
            await Page.Keyboard.PressAsync("Enter");
        }

        await WaitForHeightAsync(input, ">", floor + 40);

        await Page.Keyboard.PressAsync("Control+A");
        await Page.Keyboard.PressAsync("Delete");
        await Expect(input).ToHaveValueAsync("");
        await WaitForHeightAsync(input, "<", floor + 1);
        Assert.AreEqual(floor, await HeightAsync(input), 1);
    }

    [TestMethod]
    public async Task MaxRows_StopsTheGrowthAndScrollsInside()
    {
        await OpenAsync(Url);

        var input = Input("tf-maxrows");

        await input.ClickAsync();
        await Page.Keyboard.TypeAsync("1");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.TypeAsync("2");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.TypeAsync("3");
        await Expect(input).ToHaveCSSAsync("overflow-y", "hidden");
        var threeRows = await HeightAsync(input);

        for (var i = 4; i <= 9; i++)
        {
            await Page.Keyboard.PressAsync("Enter");
            await Page.Keyboard.TypeAsync(i.ToString());
        }

        await Expect(input).ToHaveCSSAsync("overflow-y", "auto");
        Assert.AreEqual(threeRows, await HeightAsync(input), 2);
        Assert.IsTrue(await input.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight + 10"), "The rest of the content scrolls.");
    }

    [TestMethod]
    public async Task AutoHeight_IsMeasuredAgainWhenTheFieldChangesWidth()
    {
        await OpenAsync(Url);

        var input = Input("tf-autoheight-resize");
        var wide = await HeightAsync(input);

        await Page.ClickAsync("#btn-narrow");
        await WaitForHeightAsync(input, ">", wide + 20);
        Assert.IsTrue(await input.EvaluateAsync<bool>("el => el.scrollHeight <= el.clientHeight + 1"), "The re-wrapped text still fits.");

        await Page.ClickAsync("#btn-narrow");
        await WaitForHeightAsync(input, "<", wide + 1);
    }

    [TestMethod]
    public async Task Resizable_TheGripDragsTheHeight()
    {
        await OpenAsync(Url);

        var input = Input("tf-resizable");
        await input.ScrollIntoViewIfNeededAsync();
        var box = await BoxAsync(input);

        await Page.Mouse.MoveAsync(box.X + box.Width - 3, box.Y + box.Height - 3);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width - 3, box.Y + box.Height + 80, new() { Steps = 8 });
        await Page.Mouse.UpAsync();

        Assert.IsTrue(await HeightAsync(input) > box.Height + 50, "The textarea grew with the drag.");
    }

    [TestMethod]
    public async Task Composition_AnImeSessionReachesNeitherTheValueNorTheKeyHandlers()
    {
        await OpenAsync(Url);

        // The half-composed steps of an input method editor are kept from the field; the committed text arrives once.
        var input = Input("tf-oninput");
        await input.FocusAsync();
        await input.EvaluateAsync(@"el => {
            el.dispatchEvent(new CompositionEvent('compositionstart', { bubbles: true, data: '' }));
            el.value = 'n';
            el.dispatchEvent(new InputEvent('input', { bubbles: true, isComposing: true, data: 'n', inputType: 'insertCompositionText' }));
            el.value = 'に';
            el.dispatchEvent(new InputEvent('input', { bubbles: true, isComposing: true, data: 'に', inputType: 'insertCompositionText' }));
            el.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: 'に' }));
        }");
        await Expect(Text("oninput-text")).ToHaveTextAsync("[に]");
        await Expect(Text("oninput-count")).ToHaveTextAsync("1");

        // Enter and Escape commit and cancel a candidate while composing, so the field's own meaning of them is held back.
        await Input("tf-events").FocusAsync();
        await Input("tf-events").EvaluateAsync(@"el => {
            el.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true, isComposing: true }));
            el.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true, isComposing: true }));
        }");
        await Page.Keyboard.TypeAsync("a");
        await Expect(Text("ev-last-keydown")).ToHaveTextAsync("a");
        await Expect(Text("ev-enter")).ToHaveTextAsync("0");
        await Expect(Text("ev-escape")).ToHaveTextAsync("0");

        // ClearOnEscape leaves the value of a field whose Escape belongs to the input method editor.
        await Input("tf-clear-escape").FocusAsync();
        await Input("tf-clear-escape").EvaluateAsync("el => el.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true, isComposing: true }))");
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.TypeAsync("!");
        await Expect(Input("tf-clear-escape")).ToHaveValueAsync("Press Escape!");
        await Expect(Text("escape-clears")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task PreventEnter_EnterAddsNoLine_ShiftEnterDoes()
    {
        await OpenAsync(Url);

        var input = Input("tf-prevententer");
        await input.ClickAsync();
        await Page.Keyboard.TypeAsync("a");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.TypeAsync("b");
        await Expect(input).ToHaveValueAsync("ab");

        await Page.Keyboard.PressAsync("Shift+Enter");
        await Page.Keyboard.TypeAsync("c");
        await Expect(input).ToHaveValueAsync("ab\nc");

        await BlurAsync();
        await Expect(Text("prevententer-value")).ToHaveTextAsync("[ab\nc]");
    }

    [TestMethod]
    public async Task Wrap_OffScrollsLongLinesSideways()
    {
        await OpenAsync(Url);

        var input = Input("tf-wrap");
        await Expect(input).ToHaveAttributeAsync("wrap", "off");
        Assert.IsTrue(await input.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth + 20"), "The long line is not wrapped.");
        Assert.IsTrue(await Input("tf-multiline").EvaluateAsync<bool>("el => el.scrollWidth <= el.clientWidth + 1"));
    }

    [TestMethod]
    public async Task Multiline_IsIgnoredForAPasswordType()
    {
        await OpenAsync(Url);

        var input = Input("tf-multiline-password");
        Assert.AreEqual("input", await input.EvaluateAsync<string>("el => el.tagName.toLowerCase()"));
        await Expect(input).ToHaveAttributeAsync("type", "password");
        await Expect(Root("tf-multiline-password")).Not.ToHaveClassAsync(HasClass("bit-tfl-mlf"));
    }

    [TestMethod]
    public async Task AutoHeight_AValueSetFromCodeIsMeasuredToo()
    {
        await OpenAsync(Url);

        var input = Input("tf-autoheight-code");
        var before = await HeightAsync(input);

        await Page.ClickAsync("#btn-set-autoheight");
        await Expect(input).ToHaveValueAsync("1\n2\n3\n4\n5\n6");
        await WaitForHeightAsync(input, ">", before + 60);
    }

    [TestMethod]
    public async Task Multiline_TurnedOnAtRuntime_SwapsTheElementAndKeepsTheValue()
    {
        await OpenAsync(Url);

        var input = Input("tf-dyn-multiline");
        Assert.AreEqual("input", await input.EvaluateAsync<string>("el => el.tagName.toLowerCase()"));

        await Page.ClickAsync("#btn-toggle-multiline");
        await Expect(Page.Locator("#tf-dyn-multiline textarea")).ToHaveCountAsync(1);
        await Expect(input).ToHaveValueAsync("kept");

        await input.ClickAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.TypeAsync("more");
        await BlurAsync();
        await Expect(Text("dyn-multiline-value")).ToHaveTextAsync("[kept\nmore]");
    }


    // ---------- Input type ----------

    [TestMethod]
    [DataRow("text", "abc", "abc")]
    [DataRow("number", "12", "12")]
    [DataRow("password", "abc", "abc")]
    [DataRow("email", "abc", "abc")]
    [DataRow("tel", "123", "123")]
    [DataRow("url", "abc", "abc")]
    [DataRow("search", "abc", "abc")]
    public async Task Type_MapsToTheHtmlTypeTheBrowserActsOn(string type, string typed, string expected)
    {
        await OpenAsync(Url);

        var input = Input($"tf-type-{type}");
        await Expect(input).ToHaveAttributeAsync("type", type);
        Assert.AreEqual(type, await input.EvaluateAsync<string>("el => el.type"));

        await input.PressSequentiallyAsync(typed);
        await Expect(input).ToHaveValueAsync(expected);

        // The browser validates what the type is about: an email or a url without its shape is a mismatch,
        // a number is read as one.
        var mismatch = await input.EvaluateAsync<bool>("el => el.validity.typeMismatch");
        Assert.AreEqual(type is "email" or "url", mismatch);

        if (type == "number")
        {
            Assert.AreEqual(12, await input.EvaluateAsync<double>("el => el.valueAsNumber"));
        }
    }

    [TestMethod]
    public async Task HtmlAttributes_InputModeEnterKeyHintPatternSpellCheckAutoCorrectAutoCapitalize()
    {
        await OpenAsync(Url);

        await Expect(Input("tf-inputmode")).ToHaveAttributeAsync("inputmode", "numeric");
        await Expect(Input("tf-inputmode")).ToHaveAttributeAsync("type", "text");
        await Expect(Input("tf-enterkeyhint")).ToHaveAttributeAsync("enterkeyhint", "search");

        var pattern = Input("tf-pattern");
        await Expect(pattern).ToHaveAttributeAsync("pattern", "[A-Za-z]{3}");
        await pattern.PressSequentiallyAsync("12");
        Assert.IsTrue(await pattern.EvaluateAsync<bool>("el => el.validity.patternMismatch"));
        await pattern.FillAsync("abc");
        Assert.IsFalse(await pattern.EvaluateAsync<bool>("el => el.validity.patternMismatch"));

        await Expect(Input("tf-nospell")).ToHaveAttributeAsync("spellcheck", "false");
        await Expect(Input("tf-nospell")).ToHaveAttributeAsync("autocorrect", "off");
        await Expect(Input("tf-nospell")).ToHaveAttributeAsync("autocapitalize", "none");
        Assert.IsFalse(await Input("tf-nospell").EvaluateAsync<bool>("el => el.spellcheck"));

        await Expect(Input("tf-spell")).ToHaveAttributeAsync("spellcheck", "true");
        await Expect(Input("tf-spell")).ToHaveAttributeAsync("autocorrect", "on");
        await Expect(Input("tf-spell")).ToHaveAttributeAsync("autocapitalize", "words");

        // Left unset, none of them is rendered.
        foreach (var name in new[] { "inputmode", "enterkeyhint", "pattern", "spellcheck", "autocorrect", "autocapitalize", "autocomplete", "name" })
        {
            await ExpectNoAttributeAsync(Input("tf-basic"), name);
        }
    }

    [TestMethod]
    public async Task AutoCompleteAndName_PassThroughToTheInput()
    {
        await OpenAsync(Url);

        await Expect(Input("tf-autocomplete")).ToHaveAttributeAsync("autocomplete", "email");
        await Expect(Input("tf-autocomplete")).ToHaveAttributeAsync("name", "user-email");
    }


    // ---------- Icon and affixes ----------

    [TestMethod]
    public async Task Icon_SitsAfterTheInputByDefault_AndIsDecorative()
    {
        await OpenAsync(Url);

        var icon = Page.Locator("#tf-icon-end .bit-tfl-ico");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--EditMail"));
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon"));
        await Expect(icon).ToHaveAttributeAsync("aria-hidden", "true");
        await ExpectNoAttributeAsync(icon, "role");
        Assert.AreNotEqual("none", await PseudoContentAsync(icon, "::before"), "The glyph is drawn by the icon font.");

        var input = await BoxAsync(Input("tf-icon-end"));
        var iconBox = await BoxAsync(icon);
        Assert.IsTrue(iconBox.X >= input.X + input.Width - 1, "The icon trails the input.");
    }

    [TestMethod]
    public async Task IconPlacement_Start_PutsTheIconInFrontOfTheInput()
    {
        await OpenAsync(Url);

        var input = await BoxAsync(Input("tf-icon-start"));
        var icon = await BoxAsync(Page.Locator("#tf-icon-start .bit-tfl-ico"));
        Assert.IsTrue(icon.X + icon.Width <= input.X + 1, "The icon leads the input.");
        await Expect(Page.Locator("#tf-icon-start .bit-tfl-ico")).ToHaveClassAsync(HasClass("bit-icon--Search"));
    }

    [TestMethod]
    public async Task PrefixAndSuffix_SitBesideTheInputAndStayOutOfTheValue()
    {
        await OpenAsync(Url);

        var prefix = Page.Locator("#tf-prefix .bit-tfl-pre");
        await Expect(prefix).ToHaveTextAsync("https://");
        var prefixBox = await BoxAsync(prefix);
        var input = await BoxAsync(Input("tf-prefix"));
        Assert.IsTrue(prefixBox.X + prefixBox.Width <= input.X + 1, "The prefix leads the input.");
        Assert.AreEqual(await ResolveAsync("background-color", "var(--bit-clr-bg-sec)"), await ComputedStyleAsync("#tf-prefix .bit-tfl-pre", "background-color"));

        var suffix = Page.Locator("#tf-suffix .bit-tfl-suf");
        await Expect(suffix).ToHaveTextAsync(".com");
        var suffixBox = await BoxAsync(suffix);
        var suffixInput = await BoxAsync(Input("tf-suffix"));
        Assert.IsTrue(suffixBox.X >= suffixInput.X + suffixInput.Width - 1, "The suffix trails the input.");

        await Input("tf-prefix").PressSequentiallyAsync("bitplatform.dev");
        await BlurAsync();
        await Expect(Text("prefix-value")).ToHaveTextAsync("[bitplatform.dev]");
    }

    [TestMethod]
    public async Task Affixes_AllOfThemAtOnce_KeepTheirOrder()
    {
        await OpenAsync(Url);

        var prefix = await BoxAsync(Page.Locator("#tf-affixes-all .bit-tfl-pre"));
        var icon = await BoxAsync(Page.Locator("#tf-affixes-all .bit-tfl-ico"));
        var input = await BoxAsync(Input("tf-affixes-all"));
        var suffix = await BoxAsync(Page.Locator("#tf-affixes-all .bit-tfl-suf"));

        Assert.IsTrue(prefix.X < icon.X && icon.X < input.X && input.X < suffix.X, "prefix, icon, input, suffix");
        await Expect(Input("tf-affixes-all")).ToHaveValueAsync("1,250.00");
    }

    [TestMethod]
    public async Task Affixes_APressOnThePrefixSuffixOrDecorativeIcon_PutsTheCaretInTheInput()
    {
        await OpenAsync(Url);

        await Page.Locator("#tf-affixes-all .bit-tfl-pre").ClickAsync();
        await Expect(Input("tf-affixes-all")).ToBeFocusedAsync();

        await BlurAsync();
        await Page.Locator("#tf-affixes-all .bit-tfl-suf").ClickAsync();
        await Expect(Input("tf-affixes-all")).ToBeFocusedAsync();

        await BlurAsync();
        await Page.Locator("#tf-icon-end .bit-tfl-ico").ClickAsync();
        await Expect(Input("tf-icon-end")).ToBeFocusedAsync();

        // A disabled field's chrome hands no focus to its input.
        await Page.Locator("#tf-affix-disabled .bit-tfl-pre").ClickAsync();
        await Expect(Input("tf-affix-disabled")).Not.ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Icon_AndAffixes_TakeTheAccentWhileTheFieldHasFocus()
    {
        await OpenAsync(Url);

        var icon = Page.Locator("#tf-icon-end .bit-tfl-ico");
        await Expect(icon).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-fg-pri"));

        await Input("tf-icon-end").FocusAsync();
        await Expect(icon).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-pri"));

        await Input("tf-prefix").FocusAsync();
        await Expect(Page.Locator("#tf-prefix .bit-tfl-pre")).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-pri"));
        await Expect(icon).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-fg-pri"));
    }

    [TestMethod]
    public async Task OnIconClick_RendersARealButtonThatAnswersClicksEnterAndSpace()
    {
        await OpenAsync(Url);

        var button = Page.Locator("#tf-icon-click button.bit-tfl-icb");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Search for what was typed");
        await Expect(button).ToHaveAttributeAsync("title", "Search");
        await Expect(button).ToHaveAttributeAsync("type", "button");
        await Expect(Page.Locator("#tf-icon-click .bit-tfl-icb .bit-tfl-ico")).ToHaveAttributeAsync("aria-hidden", "true");

        await button.ClickAsync();
        await Expect(Text("icon-clicks")).ToHaveTextAsync("1");

        // It takes a tab stop right after the input.
        await Input("tf-icon-click").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(button).ToBeFocusedAsync();
        Assert.AreNotEqual("none", await ComputedStyleAsync("#tf-icon-click .bit-tfl-icb", "box-shadow"), "The button draws a focus ring of its own.");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("icon-clicks")).ToHaveTextAsync("2");
        await Page.Keyboard.PressAsync(" ");
        await Expect(Text("icon-clicks")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task OnIconClick_KeepsWorkingOnAReadOnlyField_AndIsDisabledWithTheField()
    {
        await OpenAsync(Url);

        await Page.Locator("#tf-icon-click-ro .bit-tfl-icb").ClickAsync();
        await Expect(Text("icon-clicks-ro")).ToHaveTextAsync("1");

        var disabled = Page.Locator("#tf-icon-click-dis .bit-tfl-icb");
        await Expect(disabled).ToBeDisabledAsync();
        await Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");
        await disabled.ClickAsync(new() { Force = true });
        await Expect(Text("icon-clicks-dis")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task OnIconClick_TheButtonIsNamedByItsFallbacks()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#tf-icon-click-noname .bit-tfl-icb")).ToHaveAttributeAsync("aria-label", "Copy");
        await Expect(Page.Locator("#tf-icon-click-ext .bit-tfl-icb")).ToHaveAttributeAsync("aria-label", "Field action");

        var titled = Page.Locator("#tf-icon-click-title .bit-tfl-icb");
        await ExpectNoAttributeAsync(titled, "aria-label");
        await Expect(titled).ToHaveAttributeAsync("title", "Find");

        // Without a handler the icon is no button and has no title.
        await Expect(Page.Locator("#tf-icon-end button")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task IconAriaLabel_TurnsTheIconIntoANamedImage()
    {
        await OpenAsync(Url);

        var icon = Page.Locator("#tf-icon-aria .bit-tfl-ico");
        await Expect(icon).ToHaveAttributeAsync("role", "img");
        await Expect(icon).ToHaveAttributeAsync("aria-label", "This value is encrypted");
        await ExpectNoAttributeAsync(icon, "aria-hidden");
    }


    // ---------- Password ----------

    [TestMethod]
    public async Task Password_MasksTheValue()
    {
        await OpenAsync(Url);

        var input = Input("tf-password");
        await Expect(input).ToHaveAttributeAsync("type", "password");
        await input.PressSequentiallyAsync("secret");
        await Expect(input).ToHaveValueAsync("secret");
        await Expect(Page.Locator("#tf-password .bit-tfl-rpb")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task CanRevealPassword_TheButtonSwapsTheInputToTextAndBackAndReturnsTheFocus()
    {
        await OpenAsync(Url);

        var input = Input("tf-reveal");
        var button = Page.Locator("#tf-reveal .bit-tfl-rpb");

        await input.PressSequentiallyAsync("secret");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Reveal password");
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Page.Locator("#tf-reveal .bit-tfl-rpi")).ToHaveClassAsync(HasClass("bit-icon--View"));

        await button.ClickAsync();
        await Expect(input).ToHaveAttributeAsync("type", "text");
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Reveal password");
        await Expect(Page.Locator("#tf-reveal .bit-tfl-rpi")).ToHaveClassAsync(HasClass("bit-icon--Hide3"));
        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveValueAsync("secret");

        await button.ClickAsync();
        await Expect(input).ToHaveAttributeAsync("type", "password");
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(input).ToBeFocusedAsync();

        await BlurAsync();
        await Expect(Text("reveal-value")).ToHaveTextAsync("[secret]");
    }

    [TestMethod]
    public async Task CanRevealPassword_CustomAriaLabelAndIcons()
    {
        await OpenAsync(Url);

        var button = Page.Locator("#tf-pw-custom .bit-tfl-rpb");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Show the password");
        await Expect(Page.Locator("#tf-pw-custom .bit-tfl-rpi")).ToHaveClassAsync(HasClass("bit-icon--RedEye"));

        await button.ClickAsync();
        await Expect(Page.Locator("#tf-pw-custom .bit-tfl-rpi")).ToHaveClassAsync(HasClass("bit-icon--Hide"));
        await Expect(button).ToHaveAttributeAsync("aria-label", "Show the password");

        var ext = Page.Locator("#tf-pw-ext .bit-tfl-rpi");
        await Expect(ext).ToHaveClassAsync(HasClass("my-eye"));
        await Expect(ext).Not.ToHaveClassAsync(HasClass("bit-icon"));
        await Page.Locator("#tf-pw-ext .bit-tfl-rpb").ClickAsync();
        await Expect(ext).ToHaveClassAsync(HasClass("my-eye-off"));
        Assert.AreEqual("\"-\"", await PseudoContentAsync(ext, "::before"));
    }

    [TestMethod]
    public async Task ToggleRevealPassword_RevealsAndHidesFromOutside()
    {
        await OpenAsync(Url);

        var input = Input("tf-pw-api");
        await Expect(input).ToHaveAttributeAsync("type", "password");

        await Page.ClickAsync("#btn-toggle-reveal");
        await Expect(input).ToHaveAttributeAsync("type", "text");
        await Expect(Page.Locator("#tf-pw-api .bit-tfl-rpb")).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.ClickAsync("#btn-toggle-reveal");
        await Expect(input).ToHaveAttributeAsync("type", "password");
    }

    [TestMethod]
    public async Task CanRevealPassword_TakingItAway_HidesARevealedValueForGood()
    {
        await OpenAsync(Url);

        var input = Input("tf-pw-canreveal");
        await Page.Locator("#tf-pw-canreveal .bit-tfl-rpb").ClickAsync();
        await Expect(input).ToHaveAttributeAsync("type", "text");

        await Page.ClickAsync("#btn-toggle-canreveal");
        await Expect(Page.Locator("#tf-pw-canreveal .bit-tfl-rpb")).ToHaveCountAsync(0);
        await Expect(input).ToHaveAttributeAsync("type", "password");

        await Page.ClickAsync("#btn-toggle-canreveal");
        await Expect(Page.Locator("#tf-pw-canreveal .bit-tfl-rpb")).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(input).ToHaveAttributeAsync("type", "password");
    }

    [TestMethod]
    public async Task ToggleRevealPassword_IsIgnoredOnAFieldThatIsNotAPassword()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-toggle-reveal-text");
        await Expect(Input("tf-pw-text")).ToHaveAttributeAsync("type", "text");
        await Expect(Page.Locator("#tf-pw-text .bit-tfl-rpb")).ToHaveCountAsync(0);
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task CanRevealPassword_TheButtonIsDisabledWithTheFieldAndAutoCompleteReachesTheInput()
    {
        await OpenAsync(Url);

        var button = Page.Locator("#tf-pw-disabled .bit-tfl-rpb");
        await Expect(button).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await button.ClickAsync(new() { Force = true });
        await Expect(Input("tf-pw-disabled")).ToHaveAttributeAsync("type", "password");

        await Expect(Input("tf-pw-autocomplete")).ToHaveAttributeAsync("autocomplete", "current-password");
    }


    // ---------- Binding ----------

    [TestMethod]
    public async Task OneWay_ATypedValueIsWrittenBack_AndThePageStillDrivesTheField()
    {
        await OpenAsync(Url);

        var input = Input("tf-oneway");
        await Expect(input).ToHaveValueAsync("one-way");

        await input.ClickAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.TypeAsync("XYZ");
        await BlurAsync();

        await Expect(input).ToHaveValueAsync("one-way");
        await Expect(Text("oneway-value")).ToHaveTextAsync("[one-way]");

        await Page.ClickAsync("#btn-set-oneway");
        await Expect(input).ToHaveValueAsync("set from page");
    }

    [TestMethod]
    public async Task TwoWay_TypingUpdatesThePage_AndThePageUpdatesTheField()
    {
        await OpenAsync(Url);

        var input = Input("tf-twoway");
        await input.FillAsync("");
        await input.PressSequentiallyAsync("typed");
        await BlurAsync();
        await Expect(Text("twoway-value")).ToHaveTextAsync("[typed]");

        await Page.ClickAsync("#btn-set-twoway");
        await Expect(input).ToHaveValueAsync("set from page");
        await Expect(Text("twoway-value")).ToHaveTextAsync("[set from page]");
    }

    [TestMethod]
    public async Task OnChange_FiresOnceOnBlurWithTheValue()
    {
        await OpenAsync(Url);

        await Input("tf-onchange").PressSequentiallyAsync("abc");
        await Expect(Text("onchange-count")).ToHaveTextAsync("0");

        await BlurAsync();
        await Expect(Text("onchange-count")).ToHaveTextAsync("1");
        await Expect(Text("onchange-value")).ToHaveTextAsync("[abc]");
    }

    [TestMethod]
    public async Task DefaultValue_SeedsAnUncontrolledFieldThatReportsThroughOnChange()
    {
        await OpenAsync(Url);

        var input = Input("tf-default");
        await Expect(input).ToHaveValueAsync("Initial value");

        await input.ClickAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.TypeAsync("!");
        await BlurAsync();

        await Expect(Text("default-value")).ToHaveTextAsync("[Initial value!]");
        await Expect(input).ToHaveValueAsync("Initial value!");
    }

    [TestMethod]
    public async Task Immediate_EveryKeystrokeCommitsTheValue()
    {
        await OpenAsync(Url);

        await Input("tf-immediate").PressSequentiallyAsync("abc");

        await Expect(Text("immediate-value")).ToHaveTextAsync("[abc]");
        await Expect(Text("immediate-count")).ToHaveTextAsync("3");
        await Expect(Input("tf-immediate")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task DebounceTime_CommitsOnceTheTypingPauses_WhileOnInputHearsEveryKeystroke()
    {
        await OpenAsync(Url);

        await Input("tf-debounce").PressSequentiallyAsync("abcdef", new() { Delay = 20 });

        await Expect(Text("debounce-inputs")).ToHaveTextAsync("6");
        Assert.AreEqual("[]", await Text("debounce-value").TextContentAsync(), "Nothing is committed while the typing goes on.");

        await Expect(Text("debounce-value")).ToHaveTextAsync("[abcdef]");
        await Expect(Text("debounce-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ThrottleTime_CommitsTheFirstKeystrokeAndThenOncePerInterval()
    {
        await OpenAsync(Url);

        await Input("tf-throttle").PressSequentiallyAsync("abcdef", new() { Delay = 40 });

        await Expect(Text("throttle-first")).ToHaveTextAsync("[a]");
        await Expect(Text("throttle-value")).ToHaveTextAsync("[abcdef]");
        await Expect(Text("throttle-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Trim_StripsTheWhitespaceOffWhatIsCommitted()
    {
        await OpenAsync(Url);

        await Input("tf-trim").PressSequentiallyAsync("  hi there  ");
        await BlurAsync();
        await Expect(Text("trim-value")).ToHaveTextAsync("[hi there]");
        await Expect(Input("tf-trim")).ToHaveValueAsync("hi there");

        await Input("tf-notrim").PressSequentiallyAsync("  hi  ");
        await BlurAsync();
        await Expect(Text("notrim-value")).ToHaveTextAsync("[  hi  ]");
    }

    [TestMethod]
    public async Task Trim_LetsASpaceBeTypedInTheMiddleOfAnImmediateField()
    {
        await OpenAsync(Url);

        var input = Input("tf-trim-immediate");
        await input.PressSequentiallyAsync("a ");
        await Expect(Text("trim-immediate-value")).ToHaveTextAsync("[a ]");
        await input.PressSequentiallyAsync("b ");
        await Expect(input).ToHaveValueAsync("a b ");

        await BlurAsync();
        await Expect(Text("trim-immediate-value")).ToHaveTextAsync("[a b]");
        await Expect(input).ToHaveValueAsync("a b");
    }


    // ---------- Clear button ----------

    [TestMethod]
    public async Task ClearButton_EmptiesTheFieldRaisesOnClearAnnouncesItAndRefocusesTheInput()
    {
        await OpenAsync(Url);

        var button = Page.Locator("#tf-clear .bit-tfl-cbt");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Clear text");
        await Expect(button).ToHaveAttributeAsync("type", "button");
        await Expect(Page.Locator("#tf-clear .bit-tfl-cbt i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));
        await Expect(Root("tf-clear")).ToHaveClassAsync(HasClass("bit-tfl-clb"));

        await button.ClickAsync();

        await Expect(Input("tf-clear")).ToHaveValueAsync("");
        await Expect(Text("clear-value")).ToHaveTextAsync("[]");
        await Expect(Text("clear-count")).ToHaveTextAsync("1");
        await Expect(Input("tf-clear")).ToBeFocusedAsync();
        await Expect(button).ToHaveCountAsync(0);
        await Expect(Page.Locator("#tf-clear [role=status]")).ToHaveTextAsync("Cleared");
    }

    [TestMethod]
    public async Task ClearButton_AppearsFromTheFirstKeystrokeOfAFieldThatCommitsOnBlur()
    {
        await OpenAsync(Url);

        var button = Page.Locator("#tf-clear-firstkey .bit-tfl-cbt");
        await Expect(button).ToHaveCountAsync(0);

        await Input("tf-clear-firstkey").PressSequentiallyAsync("a");
        await Expect(button).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Backspace");
        await Expect(button).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ClearButton_CustomAriaLabelAndIcons()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#tf-clear-custom .bit-tfl-cbt")).ToHaveAttributeAsync("aria-label", "Empty this field");
        await Expect(Page.Locator("#tf-clear-custom .bit-tfl-cbt i")).ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));

        var ext = Page.Locator("#tf-clear-ext .bit-tfl-cbt i");
        await Expect(ext).ToHaveClassAsync(HasClass("my-ext-clear"));
        Assert.AreEqual("\"C\"", await PseudoContentAsync(ext, "::before"));

        await Page.Locator("#tf-clear-custom .bit-tfl-cbt").ClickAsync();
        await Expect(Input("tf-clear-custom")).ToHaveValueAsync("");
    }

    private const string HidesTheNativeCancelButton = @"el => {
        const pseudo = '::-webkit-search-cancel-button';
        const walk = list => {
            for (const rule of list) {
                if (!rule.selectorText) {
                    if (rule.cssRules && walk(rule.cssRules)) return true;
                    continue;
                }
                if (!rule.selectorText.includes(pseudo)) continue;
                if (rule.style.appearance !== 'none' && rule.style.webkitAppearance !== 'none') continue;
                for (const selector of rule.selectorText.split(',')) {
                    if (!selector.includes(pseudo)) continue;
                    try {
                        if (el.matches(selector.replace(pseudo, '').trim())) return true;
                    } catch { }
                }
            }
            return false;
        };
        for (const sheet of document.styleSheets) {
            let rules;
            try { rules = sheet.cssRules; } catch { continue; }
            if (walk(rules)) return true;
        }
        return false;
    }";

    [TestMethod]
    public async Task ClearButton_OnASearchInput_TheNativeAffordanceIsHidden()
    {
        if (IsFirefox) Assert.Inconclusive("Firefox draws no native clear affordance on a search input.");

        await OpenAsync(Url);

        await Expect(Input("tf-clear-search")).ToHaveAttributeAsync("type", "search");
        await Expect(Root("tf-clear-search")).ToHaveClassAsync(HasClass("bit-tfl-clb"));

        // getComputedStyle cannot read a -webkit- pseudo-element, so what is checked is that the loaded stylesheet
        // carries a rule taking the native affordance away and that the input matches it.
        Assert.IsTrue(await Input("tf-clear-search").EvaluateAsync<bool>(HidesTheNativeCancelButton), "The native clear affordance is hidden.");
        Assert.IsFalse(await Input("tf-search-noclear").EvaluateAsync<bool>(HidesTheNativeCancelButton), "Without the clear button the native one stays.");
    }

    [TestMethod]
    public async Task ClearButton_WorksInTheMultilineMode()
    {
        await OpenAsync(Url);

        await Page.Locator("#tf-clear-multiline .bit-tfl-cbt").ClickAsync();
        await Expect(Input("tf-clear-multiline")).ToHaveValueAsync("");
        await Expect(Text("clear-multiline-value")).ToHaveTextAsync("[]");
    }

    [TestMethod]
    public async Task ClearButton_IsDisabledOnAReadOnlyField()
    {
        await OpenAsync(Url);

        var button = Page.Locator("#tf-clear-readonly .bit-tfl-cbt");
        await Expect(button).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");

        await button.ClickAsync(new() { Force = true });
        await Expect(Input("tf-clear-readonly")).ToHaveValueAsync("Read only value");
    }

    [TestMethod]
    public async Task ClearedAnnouncement_IsWhatTheLiveRegionSays_AndAnEmptyOneSaysNothing()
    {
        await OpenAsync(Url);

        var status = Page.Locator("#tf-clear-announce [role=status]");
        await Expect(status).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(status).ToHaveTextAsync("");

        await Page.Locator("#tf-clear-announce .bit-tfl-cbt").ClickAsync();
        await Expect(status).ToHaveTextAsync("The email address was cleared.");

        await Page.Locator("#tf-clear-silent .bit-tfl-cbt").ClickAsync();
        await Expect(Input("tf-clear-silent")).ToHaveValueAsync("");
        await Expect(Page.Locator("#tf-clear-silent [role=status]")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task ClearOnEscape_EscapeClearsRaisesOnClearAndThenOnEscapeOnAnEmptyField()
    {
        await OpenAsync(Url);

        var input = Input("tf-clear-escape");
        await Expect(input).ToHaveAttributeAsync("data-bit-esc", "claim");

        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Expect(input).ToHaveValueAsync("");
        await Expect(Text("escape-value")).ToHaveTextAsync("[]");
        await Expect(Text("escape-clears")).ToHaveTextAsync("1");
        await Expect(Text("escape-count")).ToHaveTextAsync("1");
        await Expect(Text("value-at-escape")).ToHaveTextAsync("[]");
        await ExpectNoAttributeAsync(input, "data-bit-esc");

        // An Escape in an empty field clears nothing and raises no OnClear.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Text("escape-count")).ToHaveTextAsync("2");
        await Expect(Text("escape-clears")).ToHaveTextAsync("1");

        // Typing claims the key again, from the first keystroke.
        await Page.Keyboard.TypeAsync("x");
        await Expect(input).ToHaveAttributeAsync("data-bit-esc", "claim");
    }

    [TestMethod]
    public async Task ClearOnEscape_AnEscapeWithAModifierClearsNothing()
    {
        await OpenAsync(Url);

        var input = Input("tf-clear-escape");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Escape");

        await Expect(Text("escape-count")).ToHaveTextAsync("1");
        await Expect(input).ToHaveValueAsync("Press Escape");
        await Expect(Text("escape-clears")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task ClearOnEscape_LeavesAOneWayBoundAndAReadOnlyFieldAlone()
    {
        await OpenAsync(Url);

        var oneWay = Input("tf-clear-escape-oneway");
        await ExpectNoAttributeAsync(oneWay, "data-bit-esc");
        await oneWay.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(oneWay).ToHaveValueAsync("fixed");

        var readOnly = Input("tf-clear-escape-readonly");
        await ExpectNoAttributeAsync(readOnly, "data-bit-esc");
        await readOnly.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(readOnly).ToHaveValueAsync("kept");
    }

    [TestMethod]
    public async Task ClearOnEscape_InsideADialog_TheFirstEscapeClearsAndTheSecondClosesTheDialog()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-open-dialog");
        var dialog = Root("tf-dialog-host");
        await Expect(dialog).ToBeVisibleAsync();

        // The dialog puts the focus in the field itself (AutoFocusSelector), so nothing moves it away afterwards.
        var input = Input("tf-dialog");
        await Expect(input).ToBeFocusedAsync();
        await Expect(Root("tf-dialog")).ToHaveClassAsync(HasClass("bit-tfl-fcs"));
        await Page.Keyboard.PressAsync("Escape");

        await Expect(input).ToHaveValueAsync("");
        await Expect(Text("dialog-open")).ToHaveTextAsync("True");
        await Expect(dialog).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Text("dialog-open")).ToHaveTextAsync("False");
        await Expect(dialog).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ClearAsync_EmptiesTheFieldFromCodeAndRaisesOnClear()
    {
        await OpenAsync(Url);

        await Expect(Input("tf-clear-api")).ToHaveValueAsync("clear me from code");
        await Page.ClickAsync("#btn-clear-api");

        await Expect(Input("tf-clear-api")).ToHaveValueAsync("");
        await Expect(Text("clear-api-value")).ToHaveTextAsync("[]");
        await Expect(Text("clear-api-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#tf-clear-api [role=status]")).ToHaveTextAsync("Cleared");

        // Nothing left to clear: no second OnClear.
        await Page.ClickAsync("#btn-clear-api");
        await Expect(Text("clear-api-count")).ToHaveTextAsync("1");
    }


    // ---------- Character count ----------

    [TestMethod]
    public async Task ShowCount_WithALimit_ReadsTypedOverLimitAndDescribesTheInput()
    {
        await OpenAsync(Url);

        var counter = Page.Locator("#tf-count-limit .bit-tfl-cnt");
        await Expect(counter).ToHaveTextAsync("0/30");

        await Input("tf-count-limit").PressSequentiallyAsync("hello");
        await Expect(counter).ToHaveTextAsync("5/30");

        var id = await counter.GetAttributeAsync("id");
        await Expect(Input("tf-count-limit")).ToHaveAttributeAsync("aria-describedby", id!);
        await Expect(counter).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-fg-sec"));
    }

    [TestMethod]
    public async Task ShowCount_WithoutALimit_ReadsTheBareCountAndDescribesNothing()
    {
        await OpenAsync(Url);

        var counter = Page.Locator("#tf-count-nolimit .bit-tfl-cnt");
        await Expect(counter).ToHaveTextAsync("0");
        await Input("tf-count-nolimit").PressSequentiallyAsync("abcd");
        await Expect(counter).ToHaveTextAsync("4");
        await ExpectNoAttributeAsync(Input("tf-count-nolimit"), "aria-describedby");
    }

    [TestMethod]
    public async Task ShowCount_SharesTheFooterRowWithTheDescription()
    {
        await OpenAsync(Url);

        var description = await BoxAsync(Page.Locator("#tf-count-desc .bit-tfl-des"));
        var counter = await BoxAsync(Page.Locator("#tf-count-desc .bit-tfl-cnt"));

        Assert.AreEqual(description.Y + description.Height / 2, counter.Y + counter.Height / 2, 4);
        Assert.IsTrue(counter.X > description.X + description.Width - 1, "The counter sits at the end of the row.");
    }

    [TestMethod]
    public async Task ShowCount_ACountOverTheLimitTurnsRed()
    {
        await OpenAsync(Url);

        var counter = Page.Locator("#tf-count-over .bit-tfl-cnt");
        await Expect(counter).ToHaveTextAsync($"{"A value longer than the limit of the field".Length}/10");
        await Expect(counter).ToHaveClassAsync(HasClass("bit-tfl-cno"));
        await Expect(counter).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-err"));
    }

    [TestMethod]
    public async Task ShowCount_CountsTheLinesOfAMultilineField()
    {
        await OpenAsync(Url);

        await Input("tf-count-multiline").ClickAsync();
        await Page.Keyboard.TypeAsync("ab");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.TypeAsync("cd");
        await Expect(Page.Locator("#tf-count-multiline .bit-tfl-cnt")).ToHaveTextAsync("5/140");
    }

    [TestMethod]
    public async Task CountStrategy_CountsWhatAReaderSeesInsteadOfUtf16Units()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#tf-count-utf16 .bit-tfl-cnt")).ToHaveTextAsync("7/20");
        await Expect(Page.Locator("#tf-count-strategy .bit-tfl-cnt")).ToHaveTextAsync("4/20");

        await Input("tf-count-strategy").ClickAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.TypeAsync("!");
        await Expect(Page.Locator("#tf-count-strategy .bit-tfl-cnt")).ToHaveTextAsync("5/20");
    }


    // ---------- Templates ----------

    [TestMethod]
    public async Task LabelTemplate_ReplacesTheLabelAndKeepsTheRequiredMarker()
    {
        await OpenAsync(Url);

        var label = Page.Locator("#tf-tpl-label label.bit-tfl-lbl");
        await Expect(label.Locator(".tpl-label")).ToHaveTextAsync("Custom Label");
        await Expect(label).ToHaveAttributeAsync("for", (await Input("tf-tpl-label").GetAttributeAsync("id"))!);

        Assert.AreEqual("\"*\"", await PseudoContentAsync(Page.Locator("#tf-tpl-label-req .bit-tfl-lbl"), "::after"));
    }

    [TestMethod]
    public async Task DescriptionPrefixAndSuffixTemplates_RenderTheirOwnContent()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#tf-tpl-desc .bit-tfl-des .tpl-desc")).ToHaveTextAsync("Templated description");
        await Expect(Input("tf-tpl-desc")).ToHaveAttributeAsync("aria-describedby", (await Page.Locator("#tf-tpl-desc .bit-tfl-des").GetAttributeAsync("id"))!);

        var prefix = await BoxAsync(Page.Locator("#tf-tpl-prefix .tpl-prefix"));
        var prefixInput = await BoxAsync(Input("tf-tpl-prefix"));
        Assert.IsTrue(prefix.X + prefix.Width <= prefixInput.X + 1);
        await Expect(Page.Locator("#tf-tpl-prefix .bit-tfl-pre")).ToHaveCountAsync(0);

        var suffix = await BoxAsync(Page.Locator("#tf-tpl-suffix .tpl-suffix"));
        var suffixInput = await BoxAsync(Input("tf-tpl-suffix"));
        Assert.IsTrue(suffix.X >= suffixInput.X + suffixInput.Width - 1);
    }

    [TestMethod]
    public async Task ClearButtonTemplate_ReplacesTheIconAndStillClears()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#tf-tpl-clear .bit-tfl-cbt .tpl-clear")).ToHaveTextAsync("✕");
        await Expect(Page.Locator("#tf-tpl-clear .bit-tfl-cbt i")).ToHaveCountAsync(0);

        await Page.Locator("#tf-tpl-clear .bit-tfl-cbt").ClickAsync();
        await Expect(Input("tf-tpl-clear")).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task RevealPasswordTemplate_ReceivesWhetherThePasswordIsRevealed()
    {
        await OpenAsync(Url);

        var content = Page.Locator("#tf-tpl-reveal .tpl-reveal");
        await Expect(content).ToHaveTextAsync("show");

        await Page.Locator("#tf-tpl-reveal .bit-tfl-rpb").ClickAsync();
        await Expect(content).ToHaveTextAsync("hide");
        await Expect(Input("tf-tpl-reveal")).ToHaveAttributeAsync("type", "text");
    }

    [TestMethod]
    public async Task CountTemplate_ReceivesTheCurrentCount()
    {
        await OpenAsync(Url);

        var content = Page.Locator("#tf-tpl-count .tpl-count");
        await Expect(content).ToHaveTextAsync("0 of 20 characters");
        await Input("tf-tpl-count").PressSequentiallyAsync("abc");
        await Expect(content).ToHaveTextAsync("3 of 20 characters");
    }

    [TestMethod]
    public async Task ErrorMessageTemplate_ReplacesTheMessageAndMarksTheFieldInvalid()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#tf-tpl-error .bit-tfl-erm .tpl-error")).ToHaveTextAsync("✖ This name is reserved.");
        await Expect(Root("tf-tpl-error")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("tf-tpl-error")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Input("tf-tpl-error")).ToHaveAttributeAsync("aria-describedby", (await Page.Locator("#tf-tpl-error .bit-tfl-erm").GetAttributeAsync("id"))!);
    }

    [TestMethod]
    public async Task LoadingTemplate_ReplacesTheSpinnerAndKeepsTheAnnouncement()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#tf-tpl-loading .bit-tfl-lod .tpl-loading")).ToHaveTextAsync("working");
        await Expect(Page.Locator("#tf-tpl-loading .bit-tfl-spn")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#tf-tpl-loading [role=status]")).ToHaveTextAsync("Checking the availability");
    }


    // ---------- GhostText ----------

    [TestMethod]
    public async Task GhostText_IsPaintedBehindTheTypedTextAndAnnounced()
    {
        await OpenAsync(Url);

        var overlay = Page.Locator("#tf-ghost .bit-tfl-gho");
        await Input("tf-ghost").PressSequentiallyAsync("app");

        await Expect(overlay).ToHaveTextAsync("apple");
        await Expect(overlay).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(overlay).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-fg-ter"));
        await Expect(Page.Locator("#tf-ghost [role=status]")).ToHaveTextAsync("le");
        await Expect(Input("tf-ghost")).ToHaveValueAsync("app");
        await Expect(Text("ghost-value")).ToHaveTextAsync("[app]");
    }

    [TestMethod]
    public async Task GhostText_TabAcceptsTheSuggestionAndKeepsTheFocus()
    {
        await OpenAsync(Url);

        var input = Input("tf-ghost");
        await input.PressSequentiallyAsync("ban");
        await Expect(Page.Locator("#tf-ghost .bit-tfl-gho")).ToHaveTextAsync("banana");

        await Page.Keyboard.PressAsync("Tab");

        await Expect(input).ToHaveValueAsync("banana");
        await Expect(input).ToBeFocusedAsync();
        await Expect(Text("ghost-value")).ToHaveTextAsync("[banana]");
        await Expect(Text("ghost-accepted")).ToHaveTextAsync("[ana]");
        await Expect(Text("ghost-accepted-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task GhostText_EnterAcceptsTheSuggestion()
    {
        await OpenAsync(Url);

        await Input("tf-ghost").PressSequentiallyAsync("car");
        await Expect(Page.Locator("#tf-ghost .bit-tfl-gho")).ToHaveTextAsync("carrot");

        await Page.Keyboard.PressAsync("Enter");

        await Expect(Input("tf-ghost")).ToHaveValueAsync("carrot");
        await Expect(Text("ghost-accepted")).ToHaveTextAsync("[rot]");
    }

    [TestMethod]
    public async Task GhostText_AClickAtTheEndAccepts_AndANonImmediateFieldStillCommits()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#tf-ghost-plain .bit-tfl-gho")).ToHaveTextAsync("hello world");

        await Input("tf-ghost-plain").ClickAsync();

        await Expect(Input("tf-ghost-plain")).ToHaveValueAsync("hello world");
        await Expect(Text("ghost-plain-value")).ToHaveTextAsync("[hello world]");
        await Expect(Text("ghost-plain-accepted")).ToHaveTextAsync("[ world]");
    }

    [TestMethod]
    public async Task GhostText_IsCutDownToTheRoomMaxLengthLeaves()
    {
        await OpenAsync(Url);

        await Input("tf-ghost-max").ClickAsync();

        await Expect(Input("tf-ghost-max")).ToHaveValueAsync("abc123");
        await Expect(Text("ghost-max-value")).ToHaveTextAsync("[abc123]");
        await Expect(Text("ghost-max-accepted")).ToHaveTextAsync("[123]");
    }

    [TestMethod]
    public async Task GhostText_EscapeRejectsTheSuggestionAndNothingElse()
    {
        await OpenAsync(Url);

        var input = Input("tf-ghost-escape");
        var overlay = Page.Locator("#tf-ghost-escape .bit-tfl-gho");
        await Expect(overlay).ToHaveTextAsync("typed suggestion");

        // The suggestion is pushed again after every render (the focus is one), so the focus is let settle first.
        await input.FocusAsync();
        await Expect(Root("tf-ghost-escape")).ToHaveClassAsync(HasClass("bit-tfl-fcs"));
        await NextFramesAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Expect(overlay).ToHaveTextAsync("typed");
        await Expect(input).ToHaveValueAsync("typed");
        await Expect(Text("ghost-escape-value")).ToHaveTextAsync("[typed]");
        await Expect(Text("ghost-escape-clears")).ToHaveTextAsync("0");
        await Expect(Text("ghost-escapes")).ToHaveTextAsync("0");
        await Expect(Text("ghost-escape-keys")).ToHaveTextAsync("0");

    }

    [TestMethod]
    public async Task GhostText_AnotherKeystrokeReplacesTheSuggestion_AModifierAloneKeepsIt()
    {
        await OpenAsync(Url);

        var overlay = Page.Locator("#tf-ghost .bit-tfl-gho");
        await Input("tf-ghost").PressSequentiallyAsync("dog");
        await Expect(overlay).ToHaveTextAsync("doghouse");

        await Page.Keyboard.PressAsync("Shift");
        await Expect(overlay).ToHaveTextAsync("doghouse");

        // "dogx" has no completion, so the suggestion is gone and so is the overlay that painted it.
        await Page.Keyboard.TypeAsync("x");
        await Expect(Text("ghost-value")).ToHaveTextAsync("[dogx]");
        await Expect(overlay).ToHaveCountAsync(0);
        await Expect(Text("ghost-accepted-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task PermanentGhost_ReservesTheGutterAndAMultilineSuggestionIsAccepted()
    {
        await OpenAsync(Url);

        await Expect(Root("tf-ghost-multiline")).ToHaveClassAsync(HasClass("bit-tfl-pgt"));
        await Expect(Input("tf-ghost-multiline")).ToHaveCSSAsync("scrollbar-gutter", "stable");
        await Expect(Page.Locator("#tf-ghost-multiline .bit-tfl-gho")).ToHaveTextAsync("first line and more");

        await Input("tf-ghost-multiline").FocusAsync();
        await Page.Keyboard.PressAsync("Control+End");
        await Expect(Page.Locator("#tf-ghost-multiline .bit-tfl-gho")).ToHaveTextAsync("first line and more");
        await Page.Keyboard.PressAsync("Tab");

        await Expect(Input("tf-ghost-multiline")).ToHaveValueAsync("first line and more");
        await Expect(Text("ghost-multiline-value")).ToHaveTextAsync("[first line and more]");
    }

    [TestMethod]
    public async Task GhostText_AReadOnlyFieldNeverAcceptsIt()
    {
        await OpenAsync(Url);

        var input = Input("tf-ghost-readonly");
        await input.ClickAsync();
        await Page.Keyboard.PressAsync("Tab");

        await Expect(input).ToHaveValueAsync("ro");
        await Expect(input).Not.ToBeFocusedAsync();
        await Expect(Text("ghost-readonly-accepted")).ToHaveTextAsync("0");
    }


    // ---------- Events ----------

    [TestMethod]
    public async Task Events_KeysReportKeyDownKeyUpEnterAndEscape()
    {
        await OpenAsync(Url);

        await Input("tf-events").ClickAsync();
        await Page.Keyboard.TypeAsync("q");
        await Expect(Text("ev-keydown")).ToHaveTextAsync("1");
        await Expect(Text("ev-last-keydown")).ToHaveTextAsync("q");
        await Expect(Text("ev-keyup")).ToHaveTextAsync("1");
        await Expect(Text("ev-last-keyup")).ToHaveTextAsync("q");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("ev-enter")).ToHaveTextAsync("1");
        await Expect(Text("ev-last-keydown")).ToHaveTextAsync("Enter");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Text("ev-escape")).ToHaveTextAsync("1");
        await Expect(Text("ev-enter")).ToHaveTextAsync("1");
        await Expect(Text("ev-keydown")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task Events_TheFocusLifecycleAndClicksAreReported()
    {
        await OpenAsync(Url);

        await Input("tf-events").ClickAsync();
        await Expect(Text("ev-focus")).ToHaveTextAsync("1");
        await Expect(Text("ev-focusin")).ToHaveTextAsync("1");
        await Expect(Text("ev-click")).ToHaveTextAsync("1");
        await Expect(Text("ev-blur")).ToHaveTextAsync("0");

        await BlurAsync();
        await Expect(Text("ev-blur")).ToHaveTextAsync("1");
        await Expect(Text("ev-focusout")).ToHaveTextAsync("1");
        await Expect(Root("tf-events")).Not.ToHaveClassAsync(HasClass("bit-tfl-fcs"));
    }

    [TestMethod]
    public async Task OnInput_ReportsEveryKeystrokeWithoutCommittingTheValue()
    {
        await OpenAsync(Url);

        await Input("tf-oninput").PressSequentiallyAsync("abc");
        await Expect(Text("oninput-text")).ToHaveTextAsync("[abc]");
        await Expect(Text("oninput-count")).ToHaveTextAsync("3");
        await Expect(Text("oninput-committed")).ToHaveTextAsync("[]");

        await BlurAsync();
        await Expect(Text("oninput-committed")).ToHaveTextAsync("[abc]");
    }

    [TestMethod]
    public async Task Focus_DrawsTheRingOnTheFrameOnlyWhileFocused()
    {
        await OpenAsync(Url);

        var frame = Page.Locator("#tf-basic .bit-tfl-fgp");
        await Expect(frame).ToHaveCSSAsync("box-shadow", "none");

        await Input("tf-basic").FocusAsync();
        await Expect(Root("tf-basic")).ToHaveClassAsync(HasClass("bit-tfl-fcs"));
        await Expect(frame).Not.ToHaveCSSAsync("box-shadow", "none");

        await BlurAsync();
        await Expect(frame).ToHaveCSSAsync("box-shadow", "none");
    }

    [TestMethod]
    public async Task Hover_DarkensTheFrameUnderThePointer()
    {
        await OpenAsync(Url);

        var frame = Page.Locator("#tf-placeholder .bit-tfl-fgp");
        await Expect(frame).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-brd-pri)"));

        await frame.HoverAsync();
        await Expect(frame).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-brd-pri-hover)"));
    }


    // ---------- Selection ----------

    [TestMethod]
    public async Task SelectOnFocus_AClickSelectsTheWholeValueSoTheNextKeyReplacesIt()
    {
        await OpenAsync(Url);

        var input = Input("tf-select-focus");
        await input.ClickAsync();

        await Expect(input).ToHaveJSPropertyAsync("selectionStart", 0);
        await Expect(input).ToHaveJSPropertyAsync("selectionEnd", "Focus me and start typing".Length);

        await Page.Keyboard.TypeAsync("x");
        await Expect(input).ToHaveValueAsync("x");
    }

    [TestMethod]
    public async Task SelectOnFocus_ASecondClickPlacesTheCaret()
    {
        await OpenAsync(Url);

        var input = Input("tf-select-focus");
        await input.ClickAsync();
        await Expect(input).ToHaveJSPropertyAsync("selectionEnd", "Focus me and start typing".Length);

        await input.ClickAsync(new() { Position = new() { X = 5, Y = 5 } });
        Assert.IsTrue(await input.EvaluateAsync<bool>("el => el.selectionStart === el.selectionEnd"), "The second click collapses the selection.");
    }

    [TestMethod]
    public async Task SelectOnFocus_ATabSelectsTooAndAReadOnlyFieldIsSelected()
    {
        await OpenAsync(Url);

        await Input("tf-select-ro").ClickAsync();
        await Expect(Input("tf-select-ro")).ToHaveJSPropertyAsync("selectionStart", 0);
        await Expect(Input("tf-select-ro")).ToHaveJSPropertyAsync("selectionEnd", "AB12-CD34-EF56".Length);

        await Input("tf-oninput").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Input("tf-select-focus")).ToBeFocusedAsync();
        await Expect(Input("tf-select-focus")).ToHaveJSPropertyAsync("selectionEnd", "Focus me and start typing".Length);
        await Expect(Input("tf-select-focus")).ToHaveJSPropertyAsync("selectionStart", 0);
    }

    [TestMethod]
    public async Task SelectAsyncAndSelectRangeAsync_DriveTheSelectionFromCode()
    {
        await OpenAsync(Url);

        var input = Input("tf-select-api");
        const int length = 23; // "bit BlazorUI components"

        await Page.ClickAsync("#btn-select-all");
        await Expect(input).ToHaveJSPropertyAsync("selectionStart", 0);
        await Expect(input).ToHaveJSPropertyAsync("selectionEnd", length);

        await Page.ClickAsync("#btn-select-range");
        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveJSPropertyAsync("selectionStart", 4);
        await Expect(input).ToHaveJSPropertyAsync("selectionEnd", 12);
        Assert.AreEqual("BlazorUI", await input.EvaluateAsync<string>("el => el.value.substring(el.selectionStart, el.selectionEnd)"));

        await Page.ClickAsync("#btn-select-caret");
        await Expect(input).ToHaveJSPropertyAsync("selectionStart", 0);
        await Expect(input).ToHaveJSPropertyAsync("selectionEnd", 0);

        await Page.ClickAsync("#btn-select-clamp");
        await Expect(input).ToHaveJSPropertyAsync("selectionStart", 10);
        await Expect(input).ToHaveJSPropertyAsync("selectionEnd", length);

        await Page.ClickAsync("#btn-select-null");
        await Expect(input).ToHaveJSPropertyAsync("selectionStart", 0);
        await Expect(input).ToHaveJSPropertyAsync("selectionEnd", 3);
    }

    [TestMethod]
    public async Task FocusAsync_PutsTheFocusInTheInput()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-focus-api");
        await Expect(Input("tf-select-api")).ToBeFocusedAsync();
        await Expect(Root("tf-select-api")).ToHaveClassAsync(HasClass("bit-tfl-fcs"));
    }


    // ---------- Label position ----------

    [TestMethod]
    [DataRow("top", "bit-tfl-ltp")]
    [DataRow("bottom", "bit-tfl-lbt")]
    [DataRow("start", "bit-tfl-lst")]
    [DataRow("end", "bit-tfl-led")]
    public async Task LabelPlacement_MovesTheLabelAroundTheInput(string placement, string cssClass)
    {
        await OpenAsync(Url);

        var id = $"tf-lp-{placement}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));

        var label = await BoxAsync(Page.Locator($"#{id} .bit-tfl-lbl"));
        var frame = await BoxAsync(Page.Locator($"#{id} .bit-tfl-fgp"));

        switch (placement)
        {
            case "top":
                Assert.IsTrue(label.Y + label.Height <= frame.Y + 1, "above");
                break;
            case "bottom":
                Assert.IsTrue(label.Y >= frame.Y + frame.Height - 1, "below");
                break;
            case "start":
                Assert.IsTrue(label.X + label.Width <= frame.X + 1, "before");
                Assert.AreEqual(label.Y + label.Height / 2, frame.Y + frame.Height / 2, 4);
                break;
            case "end":
                Assert.IsTrue(label.X >= frame.X + frame.Width - 1, "after");
                Assert.AreEqual(label.Y + label.Height / 2, frame.Y + frame.Height / 2, 4);
                break;
        }

        // Unset, no placement class is rendered.
        foreach (var other in new[] { "bit-tfl-ltp", "bit-tfl-lbt", "bit-tfl-lst", "bit-tfl-led" })
        {
            await Expect(Root("tf-basic")).Not.ToHaveClassAsync(HasClass(other));
        }
    }

    [TestMethod]
    public async Task LabelPlacement_Top_TakesTheLabelOfAnUnderlinedFieldOffItsRow()
    {
        await OpenAsync(Url);

        var label = await BoxAsync(Page.Locator("#tf-lp-und-top .bit-tfl-lbl"));
        var frame = await BoxAsync(Page.Locator("#tf-lp-und-top .bit-tfl-fgp"));

        Assert.IsTrue(label.Y + label.Height <= frame.Y + 1, "The label is stacked above the input.");
        await ExpectNoAttributeAsync(Page.Locator("#tf-lp-und-top .bit-tfl-lbl"), "title");
    }


    // ---------- Loading ----------

    [TestMethod]
    public async Task Loading_ShowsASpinnerMarksTheInputBusyAndAnnouncesIt()
    {
        await OpenAsync(Url);

        var spinner = Page.Locator("#tf-loading .bit-tfl-lod .bit-tfl-spn");
        await Expect(spinner).ToBeVisibleAsync();
        await Expect(spinner).ToHaveAttributeAsync("aria-hidden", "true");
        StringAssert.Contains(await ComputedStyleAsync("#tf-loading .bit-tfl-spn", "animation-name"), "bit-spin");
        await Expect(Input("tf-loading")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Page.Locator("#tf-loading [role=status]")).ToHaveTextAsync("Loading");

        // The field stays editable while it is busy.
        await Input("tf-loading").ClickAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.TypeAsync("!");
        await BlurAsync();
        await Expect(Text("loading-value")).ToHaveTextAsync("[Checking...!]");
    }

    [TestMethod]
    public async Task LoadingAriaLabel_IsWhatIsAnnounced()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#tf-loading-aria [role=status]")).ToHaveTextAsync("Checking the availability");
    }

    [TestMethod]
    public async Task Loading_TurnedOnAndOffAtRuntime()
    {
        await OpenAsync(Url);

        var spinner = Page.Locator("#tf-loading-toggle .bit-tfl-spn");
        await Expect(spinner).ToHaveCountAsync(0);
        await ExpectNoAttributeAsync(Input("tf-loading-toggle"), "aria-busy");

        await Page.ClickAsync("#btn-toggle-loading");
        await Expect(spinner).ToBeVisibleAsync();
        await Expect(Input("tf-loading-toggle")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Page.Locator("#tf-loading-toggle [role=status]")).ToHaveTextAsync("Loading");

        await Page.ClickAsync("#btn-toggle-loading");
        await Expect(spinner).ToHaveCountAsync(0);
        await ExpectNoAttributeAsync(Input("tf-loading-toggle"), "aria-busy");
    }


    // ---------- Accessibility ----------

    [TestMethod]
    public async Task AriaDescription_IsAVisuallyHiddenDescriptionReferencedAfterTheVisibleOne()
    {
        await OpenAsync(Url);

        var hidden = Page.Locator("#tf-aria-desc span.bit-tfl-dsc", new() { HasText = "Use the number" });
        await Expect(hidden).ToHaveTextAsync("Use the number on the back of your card.");
        Assert.IsTrue(await hidden.EvaluateAsync<bool>("el => el.getBoundingClientRect().width <= 1"), "It is visually hidden.");

        var descriptionId = await Page.Locator("#tf-aria-desc .bit-tfl-des").GetAttributeAsync("id");
        var hiddenId = await hidden.GetAttributeAsync("id");
        await Expect(Input("tf-aria-desc")).ToHaveAttributeAsync("aria-describedby", $"{descriptionId} {hiddenId}");
    }

    [TestMethod]
    public async Task Title_IsTheTooltipOfTheFrameAndOfTheInput()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#tf-title .bit-tfl-fgp")).ToHaveAttributeAsync("title", "The name your colleagues see.");
        await Expect(Input("tf-title")).ToHaveAttributeAsync("title", "The name your colleagues see.");
    }

    [TestMethod]
    public async Task AriaLabel_NamesTheInputAndWinsOverTheVisibleLabel()
    {
        await OpenAsync(Url);

        await Expect(Input("tf-arialabel")).ToHaveAttributeAsync("aria-label", "Search the documentation");
        await ExpectNoAttributeAsync(Input("tf-arialabel"), "aria-labelledby");

        var both = Input("tf-arialabel-label");
        await Expect(both).ToHaveAttributeAsync("aria-label", "Card number, sixteen digits");
        await ExpectNoAttributeAsync(both, "aria-labelledby");
        await Expect(Page.Locator("#tf-arialabel-label .bit-tfl-lbl")).ToHaveAttributeAsync("for", (await both.GetAttributeAsync("id"))!);
    }

    [TestMethod]
    public async Task InputHtmlAttributes_AnOwnDescribedByComesFirstAndAnOwnLabelledByNamesTheField()
    {
        await OpenAsync(Url);

        var descriptionId = await Page.Locator("#tf-own-describedby .bit-tfl-des").GetAttributeAsync("id");
        await Expect(Input("tf-own-describedby")).ToHaveAttributeAsync("aria-describedby", $"own-hint {descriptionId}");

        await Expect(Input("tf-own-labelledby")).ToHaveAttributeAsync("aria-labelledby", "own-heading");
        await Expect(Input("tf-own-labelledby")).ToHaveAttributeAsync("data-input-attr", "yes");
    }


    // ---------- Validation ----------

    [TestMethod]
    public async Task Invalid_GivesTheFieldTheErrorLookAndAriaInvalid()
    {
        await OpenAsync(Url);

        await Expect(Root("tf-invalid")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("tf-invalid")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#tf-invalid .bit-tfl-fgp")).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-err)"));

        await Expect(Root("tf-basic")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await ExpectNoAttributeAsync(Input("tf-basic"), "aria-invalid");
    }

    [TestMethod]
    public async Task Invalid_FollowsTheAppsRuleAsTheUserTypes()
    {
        await OpenAsync(Url);

        var root = Root("tf-invalid-dyn");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Input("tf-invalid-dyn").PressSequentiallyAsync("abc");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));

        await Input("tf-invalid-dyn").PressSequentiallyAsync(" d");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task ErrorMessage_IsShownReferencedFirstAndAnnounced()
    {
        await OpenAsync(Url);

        var message = Page.Locator("#tf-errmsg .bit-tfl-erm");
        await Expect(message).ToHaveTextAsync("That user name is already taken.");
        await Expect(message).ToHaveCSSAsync("color", await ColorOfAsync("--bit-clr-err"));
        await Expect(Root("tf-errmsg")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("tf-errmsg")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Input("tf-errmsg")).ToHaveAttributeAsync("aria-describedby", (await message.GetAttributeAsync("id"))!);
        await Expect(Page.Locator("#tf-errmsg [role=status]")).ToHaveTextAsync("That user name is already taken.");
    }

    [TestMethod]
    public async Task ErrorMessage_SitsAboveTheFooterAndIsReadBeforeTheDescriptionAndTheCounter()
    {
        await OpenAsync(Url);

        var message = Page.Locator("#tf-errmsg-all .bit-tfl-erm");
        var footer = Page.Locator("#tf-errmsg-all .bit-tfl-ftr");
        var messageBox = await BoxAsync(message);
        var footerBox = await BoxAsync(footer);
        Assert.IsTrue(messageBox.Y + messageBox.Height <= footerBox.Y + 1, "The message sits above the footer.");

        var errorId = await message.GetAttributeAsync("id");
        var descriptionId = await Page.Locator("#tf-errmsg-all .bit-tfl-des").GetAttributeAsync("id");
        var countId = await Page.Locator("#tf-errmsg-all .bit-tfl-cnt").GetAttributeAsync("id");
        await Expect(Input("tf-errmsg-all")).ToHaveAttributeAsync("aria-describedby", $"{errorId} {descriptionId} {countId}");
    }

    [TestMethod]
    public async Task ErrorMessage_SetAndTakenAwayAtRuntime()
    {
        await OpenAsync(Url);

        var root = Root("tf-errmsg-dyn");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Page.Locator("#tf-errmsg-dyn .bit-tfl-erm")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-set-error");
        await Expect(Page.Locator("#tf-errmsg-dyn .bit-tfl-erm")).ToHaveTextAsync("Rejected by the server.");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Page.Locator("#tf-errmsg-dyn [role=status]")).ToHaveTextAsync("Rejected by the server.");

        await Page.ClickAsync("#btn-set-error");
        await Expect(Page.Locator("#tf-errmsg-dyn .bit-tfl-erm")).ToHaveCountAsync(0);
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task EditForm_AnEmptySubmitMarksTheFailingFieldsOnly()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-submit");
        await Expect(Text("form-status")).ToHaveTextAsync("Invalid");

        await Expect(Root("tf-form-required")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("tf-form-required")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#validation .validation-message", new() { HasText = "This field is required." })).ToBeVisibleAsync();

        await Expect(Root("tf-form-numeric")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Root("tf-form-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await ExpectNoAttributeAsync(Input("tf-form-novalidate"), "aria-invalid");
    }

    [TestMethod]
    public async Task EditForm_EveryFieldIsValidatedAsItChanges_AndTheFixedFormSubmits()
    {
        await OpenAsync(Url);

        await Page.ClickAsync("#btn-submit");
        await Expect(Text("form-status")).ToHaveTextAsync("Invalid");

        await Input("tf-form-required").PressSequentiallyAsync("x");
        await BlurAsync();
        await Expect(Root("tf-form-required")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await ExpectNoAttributeAsync(Input("tf-form-required"), "aria-invalid");

        await Input("tf-form-numeric").PressSequentiallyAsync("12a");
        await BlurAsync();
        await Expect(Root("tf-form-numeric")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Page.Locator("#validation .validation-message", new() { HasText = "Only numeric values are allowed." })).ToBeVisibleAsync();
        await Input("tf-form-numeric").FillAsync("12");
        await BlurAsync();
        await Expect(Root("tf-form-numeric")).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Input("tf-form-email").PressSequentiallyAsync("bad");
        await BlurAsync();
        await Expect(Root("tf-form-email")).ToHaveClassAsync(HasClass("bit-inv"));
        await Input("tf-form-email").FillAsync("a@b.c");
        await BlurAsync();
        await Expect(Root("tf-form-email")).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Input("tf-form-range").PressSequentiallyAsync("ab");
        await BlurAsync();
        await Expect(Root("tf-form-range")).ToHaveClassAsync(HasClass("bit-inv"));
        await Input("tf-form-range").FillAsync("abcd");
        await BlurAsync();
        await Expect(Root("tf-form-range")).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Input("tf-form-novalidate").PressSequentiallyAsync("x");
        await BlurAsync();

        await Page.ClickAsync("#btn-submit");
        await Expect(Text("form-status")).ToHaveTextAsync("Valid");
        await Expect(Page.Locator("#validation form .bit-inv")).ToHaveCountAsync(0);
    }


    // ---------- Cascading parameters ----------

    [TestMethod]
    public async Task BitParams_CascadesItsDefaults_AndAFieldKeepsWhatItSetsItself()
    {
        await OpenAsync(Url);

        await Expect(Root("tf-casc-1")).ToHaveClassAsync(HasClass("bit-tfl-und"));
        await Expect(Page.Locator("#tf-casc-1 .bit-tfl-cbt")).ToBeVisibleAsync();
        await Expect(Input("tf-casc-1")).ToHaveAttributeAsync("placeholder", "cascaded placeholder");

        // Trim reaches it from the cascade too.
        await Input("tf-casc-2").ClickAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.TypeAsync("!");
        await BlurAsync();
        await Expect(Input("tf-casc-2")).ToHaveValueAsync("padded  !");

        await Expect(Root("tf-casc-own")).Not.ToHaveClassAsync(HasClass("bit-tfl-und"));
        await Expect(Page.Locator("#tf-casc-own .bit-tfl-cbt")).ToBeVisibleAsync();

        await Expect(Root("tf-casc-outside")).Not.ToHaveClassAsync(HasClass("bit-tfl-und"));
        await Expect(Page.Locator("#tf-casc-outside .bit-tfl-cbt")).ToHaveCountAsync(0);
        await ExpectNoAttributeAsync(Input("tf-casc-outside"), "placeholder");
    }


    // ---------- Background, Border, Accent, Size ----------

    [TestMethod]
    [DataRow("primary", "bit-tfl-bpr", "var(--bit-clr-bg-pri)")]
    [DataRow("secondary", "bit-tfl-bse", "var(--bit-clr-bg-sec)")]
    [DataRow("tertiary", "bit-tfl-btr", "var(--bit-clr-bg-ter)")]
    [DataRow("transparent", "bit-tfl-btn", "transparent")]
    public async Task Background_PaintsTheFrameFromItsColorKind(string kind, string cssClass, string token)
    {
        await OpenAsync(Url);

        var id = $"tf-bg-{kind}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));
        await Expect(Page.Locator($"#{id} .bit-tfl-fgp")).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", token));
    }

    [TestMethod]
    [DataRow("primary", "bit-tfl-brp", "var(--bit-clr-brd-pri)")]
    [DataRow("secondary", "bit-tfl-brs", "var(--bit-clr-brd-sec)")]
    [DataRow("tertiary", "bit-tfl-brt", "var(--bit-clr-brd-ter)")]
    [DataRow("transparent", "bit-tfl-brn", "transparent")]
    public async Task Border_PaintsTheFrameFromItsColorKind(string kind, string cssClass, string token)
    {
        await OpenAsync(Url);

        var id = $"tf-brd-{kind}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));

        var frame = Page.Locator($"#{id} .bit-tfl-fgp");
        await Expect(frame).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", token));
        Assert.AreNotEqual("0px", await ComputedStyleAsync($"#{id} .bit-tfl-fgp", "border-top-width"), "A transparent border keeps its box.");
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
    public async Task Accent_IsTheColorTheIconAndAffixesTakeWhileFocused(string color, string suffix, string token)
    {
        await OpenAsync(Url);

        var id = $"tf-acc-{color}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-tfl-{suffix}"));
        await Expect(Root("tf-acc-none")).Not.ToHaveClassAsync(HasClass($"bit-tfl-{suffix}"));

        await Input(id).FocusAsync();
        var expected = await ColorOfAsync(token);
        await Expect(Page.Locator($"#{id} .bit-tfl-ico")).ToHaveCSSAsync("color", expected);
        await Expect(Page.Locator($"#{id} .bit-tfl-pre")).ToHaveCSSAsync("color", expected);
    }

    [TestMethod]
    [DataRow("small", "bit-tfl-sm", "--bit-siz-ctrl-sm", "--bit-tpg-fs-xs")]
    [DataRow("medium", "bit-tfl-md", "--bit-siz-ctrl-md", "--bit-tpg-fs-sm")]
    [DataRow("large", "bit-tfl-lg", "--bit-siz-ctrl-lg", "--bit-tpg-fs-md")]
    public async Task Size_ScalesTheFrameAndTheText(string size, string cssClass, string heightToken, string fontToken)
    {
        await OpenAsync(Url);

        var id = $"tf-size-{size}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cssClass));
        await Expect(Page.Locator($"#{id} .bit-tfl-fgp")).ToHaveCSSAsync("min-height", await ResolveAsync("min-height", SizeToken(heightToken)));
        await Expect(Input(id)).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", $"var({fontToken})"));
    }

    [TestMethod]
    public async Task Size_SmallMediumAndLargeAreEachTallerThanTheLast()
    {
        await OpenAsync(Url);

        var small = await HeightAsync(Page.Locator("#tf-size-small .bit-tfl-fgp"));
        var medium = await HeightAsync(Page.Locator("#tf-size-medium .bit-tfl-fgp"));
        var large = await HeightAsync(Page.Locator("#tf-size-large .bit-tfl-fgp"));

        Assert.IsTrue(small < medium && medium < large, $"{small} < {medium} < {large}");
    }


    // ---------- External icons ----------

    [TestMethod]
    public async Task ExternalIcons_TakeTheClassesOfTheirLibrary()
    {
        await OpenAsync(Url);

        var css = Page.Locator("#tf-ext-css .bit-tfl-ico");
        await Expect(css).ToHaveClassAsync(HasClass("my-ext-icon"));
        await Expect(css).Not.ToHaveClassAsync(HasClass("bit-icon"));
        Assert.AreEqual("\"E\"", await PseudoContentAsync(css, "::before"));

        await Expect(Page.Locator("#tf-ext-string .bit-tfl-ico")).ToHaveClassAsync(HasClass("my-ext-icon"));
        await Expect(Page.Locator("#tf-ext-fa .bit-tfl-ico")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid fa-rocket(\s|$)"));
        await Expect(Page.Locator("#tf-ext-bi .bit-tfl-ico")).ToHaveClassAsync(new Regex(@"(^|\s)bi bi-gear-fill(\s|$)"));
    }


    // ---------- Style and Class ----------

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRoot()
    {
        await OpenAsync(Url);

        StringAssert.Contains(await ComputedStyleAsync("#tf-style", "box-shadow"), "rgb(0, 255, 255)");
        await Expect(Root("tf-style")).ToHaveCSSAsync("margin-left", "16px");
        await Expect(Root("tf-class")).ToHaveClassAsync(HasClass("custom-class"));
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart_AndFocusedOnlyWhileFocused()
    {
        await OpenAsync(Url);

        await Expect(Root("tf-styles")).ToHaveCSSAsync("margin-left", "16px");
        await Expect(Page.Locator("#tf-styles .bit-tfl-lbl")).ToHaveCSSAsync("font-weight", "900");
        await Expect(Input("tf-styles")).ToHaveCSSAsync("padding-left", "9px");

        var frame = Page.Locator("#tf-styles .bit-tfl-fgp");
        await Expect(frame).ToHaveCSSAsync("background-color", "rgb(200, 200, 200)");

        await Input("tf-styles").FocusAsync();
        await Expect(frame).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");

        await BlurAsync();
        await Expect(frame).ToHaveCSSAsync("background-color", "rgb(200, 200, 200)");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart_AndFocusedOnlyWhileFocused()
    {
        await OpenAsync(Url);

        await Expect(Root("tf-classes")).ToHaveClassAsync(HasClass("custom-root"));
        await Expect(Page.Locator("#tf-classes .bit-tfl-fgp")).ToHaveClassAsync(HasClass("custom-field"));
        await Expect(Input("tf-classes")).ToHaveClassAsync(HasClass("custom-input"));
        await Expect(Page.Locator("#tf-classes .bit-tfl-lbl")).ToHaveClassAsync(HasClass("custom-label"));
        await Expect(Root("tf-classes")).Not.ToHaveClassAsync(HasClass("custom-focus"));

        await Input("tf-classes").FocusAsync();
        await Expect(Root("tf-classes")).ToHaveClassAsync(HasClass("custom-focus"));

        await BlurAsync();
        await Expect(Root("tf-classes")).Not.ToHaveClassAsync(HasClass("custom-focus"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_ReskinTheFieldsInside()
    {
        await OpenAsync(Url);

        var frame = Page.Locator("#tf-cssvar-ancestor .bit-tfl-fgp");
        await Expect(frame).ToHaveCSSAsync("border-top-left-radius", "20px");
        await Expect(frame).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(frame).ToHaveCSSAsync("min-height", "50px");
        await Expect(Page.Locator("#tf-cssvar-ancestor .bit-tfl-lbl")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");

        // Outside the scope the theme's values stand.
        await Expect(Page.Locator("#tf-basic .bit-tfl-fgp")).Not.ToHaveCSSAsync("border-top-left-radius", "20px");
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitParameterWinsOverTheVariableForWhatItSets()
    {
        await OpenAsync(Url);

        var frame = Page.Locator("#tf-cssvar-explicit .bit-tfl-fgp");
        await Expect(frame).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-bg-sec)"));
        await Expect(frame).ToHaveCSSAsync("min-height", await ResolveAsync("min-height", SizeToken("--bit-siz-ctrl-sm")));
        await Expect(frame).ToHaveCSSAsync("border-top-left-radius", "20px");
    }

    [TestMethod]
    public async Task CssVariables_SetOnTheStyle_ReskinThatField()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#tf-cssvar-style .bit-tfl-fgp")).ToHaveCSSAsync("border-top-color", "rgb(7, 8, 9)");
        Assert.AreEqual("rgb(11, 12, 13)", await Input("tf-cssvar-style").EvaluateAsync<string>("el => getComputedStyle(el, '::placeholder').color"));
    }


    // ---------- RTL ----------

    [TestMethod]
    public async Task Rtl_MirrorsTheField()
    {
        await OpenAsync(Url);

        await Expect(Root("tf-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("tf-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root("tf-ltr")).Not.ToHaveClassAsync(HasClass("bit-rtl"));

        var ltrInput = await BoxAsync(Input("tf-ltr"));
        var ltrIcon = await BoxAsync(Page.Locator("#tf-ltr .bit-tfl-ico"));
        var ltrPrefix = await BoxAsync(Page.Locator("#tf-ltr .bit-tfl-pre"));
        Assert.IsTrue(ltrIcon.X > ltrInput.X && ltrPrefix.X < ltrInput.X, "LTR: prefix, input, icon from left to right.");

        var input = await BoxAsync(Input("tf-rtl"));
        var icon = await BoxAsync(Page.Locator("#tf-rtl .bit-tfl-ico"));
        var prefix = await BoxAsync(Page.Locator("#tf-rtl .bit-tfl-pre"));
        Assert.IsTrue(icon.X + icon.Width <= input.X + 1, "RTL: the trailing icon is on the left.");
        Assert.IsTrue(prefix.X >= input.X + input.Width - 1, "RTL: the prefix is on the right.");

        // The label text starts at the right edge, and the counter sits at the left end of the footer.
        var frame = await BoxAsync(Page.Locator("#tf-rtl .bit-tfl-fgp"));
        var labelText = await BoxAsync(Page.Locator("#tf-rtl .bit-tfl-ltx"));
        Assert.AreEqual(frame.X + frame.Width, labelText.X + labelText.Width, 20);
        var counter = await BoxAsync(Page.Locator("#tf-rtl .bit-tfl-cnt"));
        Assert.IsTrue(counter.X < frame.X + 40, "The counter is at the inline end, the left.");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestorWithoutDir_LaysTheFieldOutTheSameWay()
    {
        await OpenAsync(Url);

        await Expect(Root("tf-rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        await ExpectNoAttributeAsync(Root("tf-rtl-inherited"), "dir");

        var input = await BoxAsync(Input("tf-rtl-inherited"));
        var icon = await BoxAsync(Page.Locator("#tf-rtl-inherited .bit-tfl-ico"));
        var prefix = await BoxAsync(Page.Locator("#tf-rtl-inherited .bit-tfl-pre"));
        Assert.IsTrue(icon.X + icon.Width <= input.X + 1, "The icon is on the left.");
        Assert.IsTrue(prefix.X >= input.X + input.Width - 1, "The prefix is on the right.");
    }

    [TestMethod]
    public async Task Rtl_UnderlinedPutsTheLabelOnTheRightOfItsRow()
    {
        await OpenAsync(Url);

        var label = await BoxAsync(Page.Locator("#tf-rtl-und .bit-tfl-lbl"));
        var frame = await BoxAsync(Page.Locator("#tf-rtl-und .bit-tfl-fgp"));
        Assert.IsTrue(label.X >= frame.X + frame.Width - 1, "The label sits to the right of the input.");
    }


    // ---------- Reduced motion ----------

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheFrameTransition_UnlessForceAnimationIsSet()
    {
        await OpenAsync(Url);

        var normal = await ComputedStyleAsync("#tf-motion .bit-tfl-fgp", "transition-duration");
        Assert.IsTrue(MaxSeconds(normal) > 0.01, $"The frame cross-fades by default ({normal}).");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        var reduced = await ComputedStyleAsync("#tf-motion .bit-tfl-fgp", "transition-duration");
        Assert.IsTrue(MaxSeconds(reduced) < 0.011, $"The frame snaps under reduced motion ({reduced}).");

        await Expect(Root("tf-motion-force")).ToHaveClassAsync(HasClass("bit-fam"));
        var forced = await ComputedStyleAsync("#tf-motion-force .bit-tfl-fgp", "transition-duration");
        Assert.IsTrue(MaxSeconds(forced) > 0.01, $"ForceAnimation keeps the full duration ({forced}).");
    }

    private static double MaxSeconds(string durations) =>
        durations.Split(',').Select(d => d.Trim()).Select(d => d.EndsWith("ms") ? double.Parse(d[..^2], System.Globalization.CultureInfo.InvariantCulture) / 1000
                                                                                : double.Parse(d.TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture)).Max();
}
