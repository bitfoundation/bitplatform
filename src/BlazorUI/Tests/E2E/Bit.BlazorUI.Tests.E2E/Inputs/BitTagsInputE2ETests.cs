using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// End-to-end tests of BitTagsInput (<c>/e2e/tags-input</c>, <c>/e2e/tags-input/autofocus</c>): tags added with Enter,
/// Tab, a blur and the separators (typed and pasted, a column of lines included), removed with the dismiss button,
/// Backspace, Delete, the clear button and Escape; the suggestions datalist (free, restricted, capped, fetched with a
/// debounce and a loading indicator); MaxTags, MinLength, MaxLength and the counter; Transformer, Pattern, Validator,
/// Duplicates and the invalid messages; editing a tag in place (double click, F2, Escape, blur, OnEdit); reordering by
/// dragging, by the handle and by Alt with the arrow keys; folding the tags away; fixed tags; the keyboard walk over the
/// chips; the live announcements and every localizable label; prefix, suffix and their templates; TagTemplate,
/// GetTagName / GetTagStyle / GetTagClass; one-way, two-way and uncontrolled binding and EditForm validation; every
/// event; the public API; Escape inside a modal; the BitParams cascade; every Variant, TagVariant, Color and Size;
/// the icons; Style / Class / Styles / Classes; the public CSS variables; RTL; the disabled and read-only contracts;
/// runtime re-renders, prerendering, unmounting and reduced motion.
/// </summary>
[TestClass]
public class BitTagsInputE2ETests : E2ETestBase
{
    private const string Route = "/e2e/tags-input";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Input(string id) => Page.Locator($"#{id} input.bit-tgi-inp");

    private ILocator Tags(string id) => Page.Locator($"#{id} .bit-tgi-tag");

    private ILocator Tag(string id, string text) => Page.Locator($"#{id} .bit-tgi-tag").Filter(new() { HasText = text });

    private ILocator Live(string id) => Page.Locator($"#{id} .bit-tgi-lvr");

    private ILocator InvalidMessage(string id) => Page.Locator($"#{id} .bit-tgi-ivm");

    private Task ExpectTextAsync(string id, string text) => Expect(Page.Locator($"#{id}")).ToHaveTextAsync(text);

    private async Task AddAsync(string id, string text)
    {
        await Input(id).FillAsync(text);
        await Input(id).PressAsync("Enter");
    }

    private Task TypeAsync(string id, string text) => Input(id).PressSequentiallyAsync(text, new() { Delay = 40 });

    /// <summary>
    /// Types one tag after another, each closed by a separator, waiting for every one to be committed and the input to
    /// be emptied before the next one is typed: the input is emptied by a round trip, and what is typed while it is on
    /// its way lands on the text that is about to be cleared.
    /// </summary>
    private async Task TypeTagsAsync(string id, string valueId, params (string Typed, string Value)[] steps)
    {
        foreach (var (typed, value) in steps)
        {
            await TypeAsync(id, typed);
            await ExpectTextAsync(valueId, value);
            await Expect(Input(id)).ToHaveValueAsync("");
        }
    }

    private Task<string> CssAsync(ILocator locator, string property) => locator.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    private Task<string> ResolveColorAsync(string value)
    {
        return Page.EvaluateAsync<string>(@"value => {
            const probe = document.createElement('span');
            probe.style.color = value;
            document.body.appendChild(probe);
            const color = getComputedStyle(probe).color;
            probe.remove();
            return color;
        }", value);
    }

    private async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no layout box.");
        return box;
    }

    // ---------------------------------------------------------------- lifecycle

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsTheTagsBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"tgi-tags-placeholder\"");
        StringAssert.Contains(html, "bit-tgi-tag");
        StringAssert.Contains(html, ">blazor<");
        StringAssert.Contains(html, ">Loading<");

        await OpenAsync(Route);
        await Expect(Tags("tgi-tags-placeholder")).ToHaveTextAsync(["blazor"]);
    }

    [TestMethod]
    public async Task Unmount_RemovingTheInputWithAPendingDebounce_LeavesNoServerOrConsoleError()
    {
        await OpenAsync(Route);

        await TypeAsync("tgi-unmount", "abc");
        await Page.ClickAsync("#btn-unmount");
        await Expect(Root("tgi-unmount")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Tags("tgi-unmount")).ToHaveCountAsync(1);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Root("tgi-unmount")).ToHaveCountAsync(0);

        await Page.WaitForTimeoutAsync(400); // the debounce of the unmounted input elapses; nothing may throw then
        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Runtime_ChangingParametersFromThePage_ReRendersTheInput()
    {
        await OpenAsync(Route);

        var root = Root("tgi-runtime");
        await Page.ClickAsync("#btn-runtime-color");
        await Expect(root).ToHaveClassAsync(HasClass("bit-tgi-err"));
        var before = Px(await CssAsync(root.Locator(".bit-tgi-cnt"), "min-height"));
        await Page.ClickAsync("#btn-runtime-size");
        await Expect(root).ToHaveClassAsync(HasClass("bit-tgi-lg"));
        Assert.IsTrue(Px(await CssAsync(root.Locator(".bit-tgi-cnt"), "min-height")) > before);

        await Expect(root.Locator(".bit-tgi-dbt")).ToHaveCountAsync(3);
        await Page.ClickAsync("#btn-runtime-readonly");
        await Expect(root).ToHaveClassAsync(HasClass("bit-tgi-rdl"));
        await Expect(root.Locator(".bit-tgi-dbt")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-runtime-readonly");
        await Expect(root.Locator(".bit-tgi-dbt")).ToHaveCountAsync(3);

        await Page.ClickAsync("#btn-runtime-fold");
        await Expect(Tags("tgi-runtime")).ToHaveCountAsync(1);
        await Expect(root.Locator(".bit-tgi-tgl")).ToHaveTextAsync("+2");

        await Page.ClickAsync("#btn-runtime-label");
        await Expect(root.Locator(".bit-tgi-lbl")).ToHaveTextAsync("Changed label");
    }

    // ---------------------------------------------------------------- basic

    [TestMethod]
    public async Task Basic_TypingATagAndPressingEnter_AddsAChipAndClearsTheInput()
    {
        await OpenAsync(Route);

        await Expect(Input("tgi-basic")).ToHaveAttributeAsync("placeholder", "Type a tag and press Enter");
        await Expect(Root("tgi-basic").Locator("[role='list']")).ToHaveCountAsync(0);

        await AddAsync("tgi-basic", "blazor");
        await ExpectTextAsync("basic-value", "blazor");
        await Expect(Input("tgi-basic")).ToHaveValueAsync("");
        await Expect(Tags("tgi-basic")).ToHaveTextAsync(["blazor"]);

        await AddAsync("tgi-basic", "  dotnet  ");
        await ExpectTextAsync("basic-value", "blazor|dotnet");
        await Expect(Live("tgi-basic")).ToHaveTextAsync("dotnet added.");

        var list = Root("tgi-basic").Locator("[role='list']");
        await Expect(list).ToHaveAttributeAsync("aria-label", "Tags");
        await Expect(list.Locator("[role='listitem']")).ToHaveCountAsync(2);
        await Expect(Tags("tgi-basic").Nth(1)).ToHaveAttributeAsync("aria-posinset", "2");
        await Expect(Tags("tgi-basic").Nth(1)).ToHaveAttributeAsync("aria-setsize", "2");
        await Expect(Tags("tgi-basic").Nth(1)).ToHaveAccessibleNameAsync("dotnet");

        // An empty or blank entry adds nothing.
        await AddAsync("tgi-basic", "   ");
        await ExpectTextAsync("basic-value", "blazor|dotnet");
    }

    [TestMethod]
    public async Task Basic_TheDismissButtonRemovesItsTagAndHandsTheFocusToTheInput()
    {
        await OpenAsync(Route);

        await AddAsync("tgi-basic", "a1");
        await AddAsync("tgi-basic", "b2");

        var dismiss = Tag("tgi-basic", "a1").Locator(".bit-tgi-dbt");
        await Expect(dismiss).ToHaveAttributeAsync("aria-label", "Remove a1");
        await Expect(dismiss).ToHaveAttributeAsync("title", "Remove");
        await Expect(dismiss).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(dismiss.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));

        await dismiss.ClickAsync();

        await ExpectTextAsync("basic-value", "b2");
        await Expect(Input("tgi-basic")).ToBeFocusedAsync();
        await Expect(Live("tgi-basic")).ToHaveTextAsync("a1 removed.");
    }

    [TestMethod]
    public async Task TagsPlaceholder_ReplacesThePlaceholderOnceThereAreTags()
    {
        await OpenAsync(Route);

        await Expect(Input("tgi-tags-placeholder")).ToHaveAttributeAsync("placeholder", "add another...");
        await Input("tgi-tags-placeholder").FocusAsync();
        await Page.Keyboard.PressAsync("Backspace");
        await Expect(Tags("tgi-tags-placeholder")).ToHaveCountAsync(0);
        await Expect(Input("tgi-tags-placeholder")).ToHaveAttributeAsync("placeholder", "Type a tag");
    }

    [TestMethod]
    public async Task ClickingTheField_FocusesTheInputAndMarksTheRootFocused()
    {
        await OpenAsync(Route);

        var root = Root("tgi-tags-placeholder");
        var container = root.Locator(".bit-tgi-cnt");
        var box = await BoxAsync(container);
        await Page.Mouse.ClickAsync(box.X + box.Width - 6, box.Y + box.Height / 2);

        await Expect(Input("tgi-tags-placeholder")).ToBeFocusedAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-tgi-fcs"));

        await Page.FocusAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-tgi-fcs"));
    }

    // ---------------------------------------------------------------- disabled & read-only

    [TestMethod]
    public async Task ReadOnly_ShowsTheTagsButTakesNothingAndStillOpensATag()
    {
        await OpenAsync(Route);

        var root = Root("tgi-readonly");
        await Expect(root).ToHaveClassAsync(HasClass("bit-tgi-rdl"));
        await Expect(Input("tgi-readonly")).ToHaveAttributeAsync("readonly", "");
        await Expect(root.Locator(".bit-tgi-dbt")).ToHaveCountAsync(0);

        await TypeAsync("tgi-readonly", "new");
        await Input("tgi-readonly").PressAsync("Enter");
        await Input("tgi-readonly").PressAsync("Backspace");
        await Expect(Tags("tgi-readonly")).ToHaveTextAsync(["Tag 1", "Tag 2"]);

        await Tags("tgi-readonly").Nth(0).ClickAsync();
        await ExpectTextAsync("readonly-clicked", "Tag 1");

        await Tags("tgi-readonly").Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await ExpectTextAsync("readonly-clicked", "Tag 2");
        await Page.Keyboard.PressAsync("Delete");
        await Expect(Tags("tgi-readonly")).ToHaveCountAsync(2);

        await Expect(Tags("tgi-readonly").Nth(0)).ToHaveAccessibleDescriptionAsync("Press Enter to open.");
    }

    [TestMethod]
    public async Task Disabled_RendersTheDisabledMarkupAndIgnoresARealClick()
    {
        await OpenAsync(Route);

        var root = Root("tgi-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("tgi-disabled")).ToBeDisabledAsync();
        await Expect(Input("tgi-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(root.Locator(".bit-tgi-dbt")).ToHaveCountAsync(0);
        await Expect(Tags("tgi-disabled").Nth(0)).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Tags("tgi-disabled").Nth(1)).ToHaveAttributeAsync("tabindex", "-1");

        await root.Locator(".bit-tgi-cnt").ClickAsync(new() { Force = true });
        await Tags("tgi-disabled").Nth(0).ClickAsync(new() { Force = true });
        await NextFramesAsync();
        await ExpectTextAsync("disabled-clicks", "0");
        await Expect(Input("tgi-disabled")).Not.ToBeFocusedAsync();

        Assert.AreNotEqual(await CssAsync(Root("tgi-readonly").Locator(".bit-tgi-cnt"), "background-color"),
                           await CssAsync(root.Locator(".bit-tgi-cnt"), "background-color"));

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("tgi-disabled")).ToBeEnabledAsync();
        await Expect(root.Locator(".bit-tgi-dbt")).ToHaveCountAsync(2);

        await root.Locator(".bit-tgi-dbt").First.ClickAsync();
        await ExpectTextAsync("disabled-value", "Tag 2");
        await Input("tgi-disabled").ClickAsync();
        await Expect(Page.Locator("#disabled-clicks")).Not.ToHaveTextAsync("0");
    }

    // ---------------------------------------------------------------- label & description

    [TestMethod]
    public async Task Label_NamesTheInputAndMarksItRequired()
    {
        await OpenAsync(Route);

        var root = Root("tgi-label");
        var label = root.Locator("label.bit-tgi-lbl");
        await Expect(label).ToHaveTextAsync("Tags");
        await Expect(label).ToHaveAttributeAsync("for", await Input("tgi-label").GetAttributeAsync("id") ?? "");
        await Expect(root).ToHaveClassAsync(HasClass("bit-tgi-req"));
        await Expect(Input("tgi-label")).ToHaveAttributeAsync("aria-required", "true");
        await Expect(Input("tgi-label")).ToHaveAccessibleNameAsync(new Regex("^Tags"));
        var asterisk = await label.EvaluateAsync<string>("el => getComputedStyle(el, '::after').content");
        StringAssert.Contains(asterisk, "*");

        await label.ClickAsync();
        await Expect(Input("tgi-label")).ToBeFocusedAsync();

        await Expect(Input("tgi-description")).ToHaveAccessibleDescriptionAsync("Press Enter after each skill.");
        await Expect(Root("tgi-description").Locator(".bit-tgi-dsc")).ToHaveTextAsync("Press Enter after each skill.");

        await Expect(Root("tgi-label-template").Locator("label .lbl-tpl")).ToHaveTextAsync("Recipients list");
        await Expect(Root("tgi-label-template").Locator(".bit-tgi-dsc .dsc-tpl")).ToHaveTextAsync("Separate with Enter");
        await Expect(Input("tgi-label-template")).ToHaveAccessibleNameAsync("Recipients list");

        await Expect(Input("tgi-aria-label")).ToHaveAttributeAsync("aria-label", "Keywords");
    }

    // ---------------------------------------------------------------- variants

    [TestMethod]
    public async Task Variants_StyleTheFieldAndTheTagsApart()
    {
        await OpenAsync(Route);

        await Expect(Root("tgi-var-default")).ToHaveClassAsync(HasClass("bit-tgi-otl"));
        await Expect(Root("tgi-var-default")).ToHaveClassAsync(HasClass("bit-tgi-tgf"));
        await Expect(Root("tgi-var-fill")).ToHaveClassAsync(HasClass("bit-tgi-fil"));
        await Expect(Root("tgi-var-fill")).ToHaveClassAsync(HasClass("bit-tgi-tgo"));
        await Expect(Root("tgi-var-text")).ToHaveClassAsync(HasClass("bit-tgi-txt"));
        await Expect(Root("tgi-var-text")).ToHaveClassAsync(HasClass("bit-tgi-tgt"));
        await Expect(Root("tgi-var-outline")).ToHaveClassAsync(HasClass("bit-tgi-otl"));
        await Expect(Root("tgi-var-outline")).ToHaveClassAsync(HasClass("bit-tgi-tgf"));

        var outlineField = Root("tgi-var-outline").Locator(".bit-tgi-cnt");
        var fillField = Root("tgi-var-fill").Locator(".bit-tgi-cnt");
        var textField = Root("tgi-var-text").Locator(".bit-tgi-cnt");
        Assert.AreNotEqual(await CssAsync(outlineField, "background-color"), await CssAsync(fillField, "background-color"));
        Assert.AreEqual("0px", await CssAsync(textField, "border-top-width"));
        Assert.AreNotEqual("0px", await CssAsync(textField, "border-bottom-width"));
        Assert.AreNotEqual("0px", await CssAsync(outlineField, "border-top-width"));

        // Fill tags are filled, Outline tags are ringed on a clear background, Text tags are bare.
        var fillTag = Tags("tgi-var-outline").First;
        var outlineTag = Tags("tgi-var-fill").First;
        var textTag = Tags("tgi-var-text").First;
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", await CssAsync(fillTag, "background-color"));
        Assert.AreEqual("rgba(0, 0, 0, 0)", await CssAsync(outlineTag, "background-color"));
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", await CssAsync(outlineTag, "border-top-color"));
        Assert.AreEqual("rgba(0, 0, 0, 0)", await CssAsync(textTag, "background-color"));
        Assert.AreEqual("rgba(0, 0, 0, 0)", await CssAsync(textTag, "border-top-color"));
        Assert.AreEqual(await CssAsync(fillTag, "background-color"), await CssAsync(outlineTag, "color"));

        await Expect(Root("tgi-noborder")).ToHaveClassAsync(HasClass("bit-tgi-nbd"));
        Assert.AreEqual("rgba(0, 0, 0, 0)", await CssAsync(Root("tgi-noborder").Locator(".bit-tgi-cnt"), "border-top-color"));
    }

    // ---------------------------------------------------------------- separators & pasting

    [TestMethod]
    public async Task Separators_TypedCommitTheTagAndNeverReachTheInput()
    {
        await OpenAsync(Route);

        await TypeAsync("tgi-sep", "a1,");
        await ExpectTextAsync("sep-value", "a1");
        await Expect(Input("tgi-sep")).ToHaveValueAsync("");

        await TypeTagsAsync("tgi-sep", "sep-value", ("b2;", "a1|b2"));
        await TypeAsync("tgi-sep", "c3");
        await Input("tgi-sep").PressAsync("Enter");
        await ExpectTextAsync("sep-value", "a1|b2|c3");

        await TypeTagsAsync("tgi-space", "space-value", ("one ", "one"), ("two ", "one|two"), ("three ", "one|two|three"));
        await Expect(Input("tgi-space")).ToHaveAttributeAsync("data-separators", "[\" \"]");
    }

    [TestMethod]
    public async Task Separators_APastedListBecomesATagEach_AndAPastedColumnToo()
    {
        await OpenAsync(Route);

        await Input("tgi-sep").FocusAsync();
        await Page.Keyboard.InsertTextAsync("red, green;blue");
        await ExpectTextAsync("sep-value", "red|green|blue");
        await Expect(Live("tgi-sep")).ToHaveTextAsync("3 tags added.");

        // A column copied out of a spreadsheet: its line breaks are joined over the first separator.
        await Input("tgi-sep").EvaluateAsync(@"el => {
            const data = new DataTransfer();
            data.setData('text/plain', 'x1\r\ny2\nz3');
            el.dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
        }");
        await ExpectTextAsync("sep-value", "red|green|blue|x1|y2|z3");
    }

    [TestMethod]
    public async Task NoTrim_KeepsTheWhitespaceAroundATag()
    {
        await OpenAsync(Route);

        await AddAsync("tgi-notrim", "  spaced  ");
        await ExpectTextAsync("notrim-value", "[  spaced  ]");
        await Expect(Input("tgi-notrim")).ToHaveAttributeAsync("data-no-trim", "true");
    }

    // ---------------------------------------------------------------- suggestions

    [TestMethod]
    public async Task Suggestions_AreTheInputsDatalistAndDropTheTagsAlreadyChosen()
    {
        await OpenAsync(Route);

        var options = Root("tgi-suggest").Locator("datalist option");
        await Expect(options).ToHaveCountAsync(5);
        var listId = await Root("tgi-suggest").Locator("datalist").GetAttributeAsync("id");
        await Expect(Input("tgi-suggest")).ToHaveAttributeAsync("list", listId!);

        // Picking a suggestion fills the input; Enter makes it a tag.
        await AddAsync("tgi-suggest", "blazor");
        await ExpectTextAsync("suggest-value", "blazor");
        await Expect(options).ToHaveCountAsync(4);
        await Expect(Root("tgi-suggest").Locator("datalist option[value='blazor']")).ToHaveCountAsync(0);

        // Free text is still accepted.
        await AddAsync("tgi-suggest", "jquery");
        await ExpectTextAsync("suggest-value", "blazor|jquery");
    }

    [TestMethod]
    public async Task RestrictToSuggestions_RefusesAnythingElseAndStoresTheSuggestionsSpelling()
    {
        await OpenAsync(Route);

        await AddAsync("tgi-restrict", "BLAZOR");
        await ExpectTextAsync("restrict-value", "blazor");

        await AddAsync("tgi-restrict", "foo");
        await ExpectTextAsync("restrict-value", "blazor");
        await Expect(InvalidMessage("tgi-restrict")).ToHaveTextAsync("foo was not added. It is not one of the suggestions.");
        await Expect(Root("tgi-restrict")).ToHaveClassAsync(HasClass("bit-tgi-rjd"));
        await Expect(Input("tgi-restrict")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Input("tgi-restrict")).ToHaveAccessibleDescriptionAsync(new Regex("foo was not added"));

        // Typing again takes the refusal back.
        await TypeAsync("tgi-restrict", "v");
        await Expect(InvalidMessage("tgi-restrict")).ToHaveCountAsync(0);
        await Expect(Root("tgi-restrict")).Not.ToHaveClassAsync(HasClass("bit-tgi-rjd"));
    }

    [TestMethod]
    public async Task MaxSuggestions_OffersOnlyTheBestMatchesOfWhatIsTyped()
    {
        await OpenAsync(Route);

        var options = Root("tgi-max-suggest").Locator("datalist option");
        await Expect(options).ToHaveCountAsync(5);

        await Input("tgi-max-suggest").FillAsync("tan");
        await Expect(options).ToHaveCountAsync(2);
        await Input("tgi-max-suggest").FillAsync("tanz");
        await Expect(options).ToHaveCountAsync(1);
        await Expect(options.First).ToHaveAttributeAsync("value", "Tanzania");

        await Input("tgi-max-suggest").FillAsync("ar");
        await Expect(options).ToHaveCountAsync(5);
    }

    [TestMethod]
    public async Task AsyncSuggestions_AreFetchedOnceTheTypingStopsWhileALoadingIndicatorShows()
    {
        await OpenAsync(Route);

        await TypeAsync("tgi-async", "ger");
        var spinner = Root("tgi-async").Locator("[role='progressbar']");
        await Expect(spinner).ToBeVisibleAsync();
        await Expect(spinner).ToHaveAttributeAsync("aria-label", "Fetching countries");
        await Expect(spinner).ToHaveClassAsync(HasClass("bit-tgi-spn"));

        await Expect(spinner).ToHaveCountAsync(0);
        await Expect(Root("tgi-async").Locator("datalist option")).ToHaveCountAsync(1);
        await Expect(Root("tgi-async").Locator("datalist option").First).ToHaveAttributeAsync("value", "Germany");
        await ExpectTextAsync("async-calls", "1");
    }

    // ---------------------------------------------------------------- limits

    [TestMethod]
    public async Task MaxTags_StopsAtTheCeilingCountsTowardsItAndSaysWhy()
    {
        await OpenAsync(Route);

        var counter = Root("tgi-max").Locator(".bit-tgi-cnr");
        await Expect(counter).ToHaveTextAsync("2/3");
        await Expect(counter).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Root("tgi-max").Locator("datalist")).ToHaveCountAsync(1);

        await TypeTagsAsync("tgi-max", "max-value", ("three,", "one|two|three"));
        await TypeAsync("tgi-max", "four,");
        await ExpectTextAsync("max-value", "one|two|three");
        await Expect(counter).ToHaveTextAsync("3/3");
        await Expect(InvalidMessage("tgi-max")).ToHaveTextAsync("four was not added. No more than 3 tags are allowed.");
        await Expect(Root("tgi-max").Locator("datalist")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task MinAndMaxLength_CutTheTypingAtTheMaximumAndRefuseAShortTag()
    {
        await OpenAsync(Route);

        await TypeAsync("tgi-minmax", "abcdefghijkl");
        await Expect(Input("tgi-minmax")).ToHaveValueAsync("abcdefghij");
        await Input("tgi-minmax").PressAsync("Enter");
        await ExpectTextAsync("minmax-value", "abcdefghij");
        await Expect(Root("tgi-minmax").Locator(".bit-tgi-cnr")).ToHaveTextAsync("1");

        await AddAsync("tgi-minmax", "ab");
        await ExpectTextAsync("minmax-value", "abcdefghij");
        await Expect(InvalidMessage("tgi-minmax")).ToHaveTextAsync("ab was not added. A tag must be at least 3 characters.");
        await Expect(Live("tgi-minmax")).ToHaveTextAsync("ab was not added. A tag must be at least 3 characters.");
    }

    // ---------------------------------------------------------------- rules

    [TestMethod]
    public async Task Transformer_NormalizesTheTagAndADuplicateIsFlaggedOnTheExistingChip()
    {
        await OpenAsync(Route);

        await TypeAsync("tgi-hashtag", "#Blazor,");
        await ExpectTextAsync("hashtag-value", "blazor");

        await TypeAsync("tgi-hashtag", "# Bla zor,");
        await ExpectTextAsync("hashtag-value", "blazor");
        await ExpectTextAsync("hashtag-exists", "blazor");
        await Expect(InvalidMessage("tgi-hashtag")).ToHaveTextAsync("blazor was not added. It is already in the list.");
        await Expect(Tags("tgi-hashtag").First).ToHaveClassAsync(HasClass("bit-tgi-tag-dup"));
    }

    [TestMethod]
    public async Task PatternAndGetInvalidMessage_RefuseAMalformedTagInTheirOwnWords()
    {
        await OpenAsync(Route);

        await TypeAsync("tgi-email", "not-an-email ");
        await ExpectTextAsync("email-value", "null");
        await Expect(InvalidMessage("tgi-email")).ToHaveTextAsync("'not-an-email' is not an email address.");

        await Input("tgi-email").FillAsync("");
        await TypeAsync("tgi-email", "ada@example.com;");
        await ExpectTextAsync("email-value", "ada@example.com");
    }

    [TestMethod]
    public async Task ValidatorAndDuplicates_AcceptOnlyTheKnownValuesAsOftenAsTyped()
    {
        await OpenAsync(Route);

        await AddAsync("tgi-validator", "blazor");
        await AddAsync("tgi-validator", "blazor");
        await ExpectTextAsync("validator-value", "blazor|blazor");

        await AddAsync("tgi-validator", "jquery");
        await ExpectTextAsync("validator-value", "blazor|blazor");
        await Expect(InvalidMessage("tgi-validator")).ToHaveTextAsync("jquery was not added. It is not allowed.");

        // NoInvalidHighlight keeps the field itself from turning red.
        await Expect(Root("tgi-validator")).Not.ToHaveClassAsync(HasClass("bit-tgi-rjd"));
        await Expect(Input("tgi-validator")).Not.ToHaveAttributeAsync("aria-invalid", new Regex(".*"));

        await AddAsync("tgi-invalid-format", "ab");
        await Expect(InvalidMessage("tgi-invalid-format")).ToHaveTextAsync("Refused ab: A tag must be at least 3 characters.");
    }

    // ---------------------------------------------------------------- editable

    [TestMethod]
    public async Task EditableTags_ADoubleClickOpensTheEditAndEnterCommitsIt()
    {
        await OpenAsync(Route);

        await Tag("tgi-edit", "dotnet").DblClickAsync();

        var editor = Root("tgi-edit").Locator("input.bit-tgi-eip");
        await Expect(editor).ToBeVisibleAsync();
        await Expect(editor).ToBeFocusedAsync();
        await Expect(editor).ToHaveValueAsync("dotnet");
        await Expect(editor).ToHaveAttributeAsync("aria-label", "Edit dotnet");
        await Expect(editor).ToHaveAttributeAsync("maxlength", "12");
        await Expect(editor).ToHaveAttributeAsync("data-bit-esc", "claim");
        await Expect(Tags("tgi-edit").Nth(1)).ToHaveClassAsync(HasClass("bit-tgi-tag-edt"));
        Assert.AreEqual(6, await editor.EvaluateAsync<int>("el => el.selectionEnd - el.selectionStart"));

        await editor.FillAsync("csharp");
        await editor.PressAsync("Enter");

        await ExpectTextAsync("edit-value", "blazor|csharp|web");
        await ExpectTextAsync("edit-args", "dotnet->csharp");
        await Expect(editor).ToHaveCountAsync(0);
        await Expect(Tags("tgi-edit").Nth(1)).ToBeFocusedAsync();
        await Expect(Live("tgi-edit")).ToHaveTextAsync("csharp updated.");
    }

    [TestMethod]
    public async Task EditableTags_F2OpensEscapeCancelsAndARefusedEditIsHandedBack()
    {
        await OpenAsync(Route);

        await Tags("tgi-edit").Nth(0).FocusAsync();
        await Page.Keyboard.PressAsync("F2");
        var editor = Root("tgi-edit").Locator("input.bit-tgi-eip");
        await Expect(editor).ToBeFocusedAsync();
        await editor.FillAsync("changed");
        await editor.PressAsync("Escape");
        await Expect(editor).ToHaveCountAsync(0);
        await ExpectTextAsync("edit-value", "blazor|dotnet|web");
        await Expect(Tags("tgi-edit").Nth(0)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(editor).ToBeFocusedAsync();
        await editor.FillAsync("ab");
        await editor.PressAsync("Enter");
        await Expect(InvalidMessage("tgi-edit")).ToHaveTextAsync("ab was not added. A tag must be at least 3 characters.");
        await Expect(editor).ToBeFocusedAsync();
        await Expect(editor).ToHaveValueAsync("ab");

        // A duplicate is refused the same way.
        await editor.FillAsync("web");
        await editor.PressAsync("Enter");
        await Expect(InvalidMessage("tgi-edit")).ToHaveTextAsync("web was not added. It is already in the list.");

        // Emptied, the edit removes the tag.
        await editor.FillAsync("");
        await editor.PressAsync("Enter");
        await ExpectTextAsync("edit-value", "dotnet|web");
    }

    [TestMethod]
    public async Task EditableTags_OnEditCancelsOrRewritesAndABlurCommits()
    {
        await OpenAsync(Route);

        var editor = Root("tgi-edit").Locator("input.bit-tgi-eip");

        await Tag("tgi-edit", "web").DblClickAsync();
        await editor.FillAsync("cancelme");
        await editor.PressAsync("Enter");
        await ExpectTextAsync("edit-args", "web->cancelme");
        await ExpectTextAsync("edit-value", "blazor|dotnet|web");

        await Tag("tgi-edit", "web").DblClickAsync();
        await editor.FillAsync("rewrite");
        await editor.PressAsync("Enter");
        await ExpectTextAsync("edit-value", "blazor|dotnet|rewritten");

        await Tag("tgi-edit", "blazor").DblClickAsync();
        await editor.FillAsync("razor");
        await Page.FocusAsync("#btn-toggle-disabled");
        await ExpectTextAsync("edit-value", "razor|dotnet|rewritten");
    }

    // ---------------------------------------------------------------- reorder

    [TestMethod]
    public async Task Reorder_AltWithTheArrowKeysMovesTheFocusedTag()
    {
        await OpenAsync(Route);

        await Tag("tgi-reorder", "first").FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowRight");
        await ExpectTextAsync("reorder-value", "second|first|third|fourth");
        await ExpectTextAsync("reorder-args", "first:0>1");
        await Expect(Tags("tgi-reorder").Nth(1)).ToBeFocusedAsync();
        await Expect(Live("tgi-reorder")).ToHaveTextAsync("first moved to position 2 of 4.");

        await Page.Keyboard.PressAsync("Alt+End");
        await ExpectTextAsync("reorder-value", "second|third|fourth|first");
        await Page.Keyboard.PressAsync("Alt+ArrowLeft");
        await ExpectTextAsync("reorder-value", "second|third|first|fourth");
        await Page.Keyboard.PressAsync("Alt+Home");
        await ExpectTextAsync("reorder-value", "first|second|third|fourth");
        await ExpectTextAsync("reorder-args", "first:2>0");
    }

    [TestMethod]
    public async Task Reorder_DraggingAChipOntoAnotherTakesItsPlace()
    {
        await OpenAsync(Route);

        await Expect(Tags("tgi-reorder").First).ToHaveAttributeAsync("draggable", "true");
        await Tag("tgi-reorder", "first").DragToAsync(Tag("tgi-reorder", "third"));

        await ExpectTextAsync("reorder-value", "second|third|first|fourth");
        await ExpectTextAsync("reorder-args", "first:0>2");
        await Expect(Root("tgi-reorder").Locator(".bit-tgi-tag-drg")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Reorder_TheHandlePicksATagUpAndATapPutsItDown()
    {
        await OpenAsync(Route);

        var handle = Tag("tgi-reorder", "fourth").Locator(".bit-tgi-rbt");
        await Expect(handle).ToHaveAttributeAsync("aria-label", "Move fourth");
        await Expect(handle).ToHaveAttributeAsync("title", "Move");
        await Expect(handle).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(handle.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--GripperBarVertical"));

        await handle.ClickAsync();
        await Expect(handle).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Root("tgi-reorder")).ToHaveClassAsync(HasClass("bit-tgi-pck"));
        await Expect(Tag("tgi-reorder", "fourth")).ToHaveClassAsync(HasClass("bit-tgi-tag-lft"));
        await Expect(Tag("tgi-reorder", "first").Locator(".bit-tgi-rbt")).ToHaveAttributeAsync("aria-label", "Move fourth here");
        await Expect(Root("tgi-reorder").Locator("[role='list']")).ToHaveAttributeAsync("data-bit-esc", "claim");
        await Expect(Live("tgi-reorder")).ToHaveTextAsync(new Regex("^fourth picked up\\."));

        await Tag("tgi-reorder", "first").ClickAsync();
        await ExpectTextAsync("reorder-value", "fourth|first|second|third");
        await Expect(Root("tgi-reorder")).Not.ToHaveClassAsync(HasClass("bit-tgi-pck"));

        // Picked up and put back: by the handle again, or by Escape.
        await Tag("tgi-reorder", "second").Locator(".bit-tgi-rbt").ClickAsync();
        await Tag("tgi-reorder", "second").Locator(".bit-tgi-rbt").ClickAsync();
        await Expect(Root("tgi-reorder")).Not.ToHaveClassAsync(HasClass("bit-tgi-pck"));
        await Expect(Live("tgi-reorder")).ToHaveTextAsync("second put back.");

        await Tag("tgi-reorder", "third").Locator(".bit-tgi-rbt").ClickAsync();
        await Tag("tgi-reorder", "third").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Root("tgi-reorder")).Not.ToHaveClassAsync(HasClass("bit-tgi-pck"));
        await ExpectTextAsync("reorder-value", "fourth|first|second|third");
    }

    // ---------------------------------------------------------------- fold

    [TestMethod]
    public async Task MaxDisplayedTags_FoldsTheRestBehindAChipThatUnfoldsThem()
    {
        await OpenAsync(Route);

        var toggle = Root("tgi-fold").Locator(".bit-tgi-tgl");
        await Expect(Tags("tgi-fold")).ToHaveCountAsync(3);
        await Expect(toggle).ToHaveTextAsync("+3");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(toggle).ToHaveAttributeAsync("aria-label", "Show 3 more tags");
        await Expect(Tags("tgi-fold").First).ToHaveAttributeAsync("aria-setsize", "6");

        await toggle.ClickAsync();
        await Expect(Tags("tgi-fold")).ToHaveCountAsync(6);
        await Expect(toggle).ToHaveTextAsync("Show less");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");

        await toggle.ClickAsync();
        await Expect(Tags("tgi-fold")).ToHaveCountAsync(3);

        var custom = Root("tgi-fold-custom").Locator(".bit-tgi-tgl");
        await Expect(custom).ToHaveTextAsync("2 more...");
        await Expect(custom).ToHaveAttributeAsync("aria-label", "Unfold 2 tags");
        await custom.ClickAsync();
        await Expect(custom).ToHaveTextAsync("Fold back");
    }

    // ---------------------------------------------------------------- clear

    [TestMethod]
    public async Task ClearButton_RemovesEveryTagAndReportsThem()
    {
        await OpenAsync(Route);

        var clear = Root("tgi-clear").Locator(".bit-tgi-cbt");
        await Expect(clear).ToHaveAttributeAsync("aria-label", "Clear all tags");
        await Expect(clear).ToHaveAttributeAsync("title", "Clear all tags");
        await Expect(clear).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(clear.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Clear"));

        await clear.ClickAsync();

        await ExpectTextAsync("clear-value", "null");
        await ExpectTextAsync("cleared", "blazor|dotnet|web");
        await Expect(clear).ToHaveCountAsync(0);
        await Expect(Input("tgi-clear")).ToBeFocusedAsync();
        await Expect(Live("tgi-clear")).ToHaveTextAsync("3 tags removed.");

        // Text being typed brings the button back, to take the text away.
        await TypeAsync("tgi-clear", "ab");
        await Expect(clear).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Escape_TakesTheTypedTextBackFirstAndThenClearsTheTags()
    {
        await OpenAsync(Route);

        var input = Input("tgi-clear");
        await Expect(input).ToHaveAttributeAsync("data-bit-esc", "claim");
        await input.PressSequentiallyAsync("draft", new() { Delay = 40 });
        await input.PressAsync("Escape");
        await Expect(input).ToHaveValueAsync("");
        await ExpectTextAsync("clear-value", "blazor|dotnet|web");

        await input.PressAsync("Escape");
        await ExpectTextAsync("clear-value", "null");
        await Expect(input).Not.ToHaveAttributeAsync("data-bit-esc", new Regex(".*"));
    }

    [TestMethod]
    public async Task OnBeforeClear_CanRefuseTheClear()
    {
        await OpenAsync(Route);

        await Root("tgi-clear-guard").Locator(".bit-tgi-cbt").ClickAsync();
        await ExpectTextAsync("clear-guard-args", "blazor|dotnet|web");
        await ExpectTextAsync("clear-guard-value", "blazor|dotnet|web");

        await Tag("tgi-clear-guard", "web").Locator(".bit-tgi-dbt").ClickAsync();
        await Root("tgi-clear-guard").Locator(".bit-tgi-cbt").ClickAsync();
        await ExpectTextAsync("clear-guard-value", "null");
    }

    // ---------------------------------------------------------------- fixed

    [TestMethod]
    public async Task CanRemoveTag_PinsATagAgainstEveryWayOfRemovingIt()
    {
        await OpenAsync(Route);

        var owner = Tag("tgi-fixed", "ada@example.com");
        await Expect(owner).ToHaveClassAsync(HasClass("bit-tgi-tag-fix"));
        await Expect(owner.Locator(".bit-tgi-dbt")).ToHaveCountAsync(0);
        await Expect(Tag("tgi-fixed", "grace").Locator(".bit-tgi-dbt")).ToHaveCountAsync(1);
        await Expect(owner).ToHaveAccessibleDescriptionAsync("The owner cannot be removed.");

        await owner.FocusAsync();
        await Page.Keyboard.PressAsync("Delete");
        await Page.Keyboard.PressAsync("Backspace");
        await NextFramesAsync();
        await ExpectTextAsync("fixed-value", "ada@example.com|grace@example.com");

        await Root("tgi-fixed").Locator(".bit-tgi-cbt").ClickAsync();
        await ExpectTextAsync("fixed-value", "ada@example.com");
        await Expect(Root("tgi-fixed").Locator(".bit-tgi-cbt")).ToHaveCountAsync(0);

        // Backspace on the empty input does not take the pinned tag either.
        await Input("tgi-fixed").FocusAsync();
        await Page.Keyboard.PressAsync("Backspace");
        await NextFramesAsync();
        await ExpectTextAsync("fixed-value", "ada@example.com");
    }

    // ---------------------------------------------------------------- keyboard

    [TestMethod]
    public async Task Keyboard_TheArrowKeysWalkTheChipsAndBackToTheInput()
    {
        await OpenAsync(Route);

        await Input("tgi-keys").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Tag("tgi-keys", "three")).ToBeFocusedAsync();
        await Expect(Tag("tgi-keys", "three")).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Tag("tgi-keys", "one")).ToHaveAttributeAsync("tabindex", "-1");

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Tag("tgi-keys", "two")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Tag("tgi-keys", "one")).ToBeFocusedAsync();

        // ArrowLeft stays on the first chip; the ArrowRight after it shows the press has been dealt with.
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tag("tgi-keys", "two")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Tag("tgi-keys", "three")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Input("tgi-keys")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Tag("tgi-keys", "three")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Input("tgi-keys")).ToBeFocusedAsync();

        // The chips are one tab stop, handed back to the first chip once the input has the focus: Shift+Tab from
        // the input lands on it, and Tab goes back.
        await Expect(Tag("tgi-keys", "one")).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Tag("tgi-keys", "three")).ToHaveAttributeAsync("tabindex", "-1");
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Tag("tgi-keys", "one")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Input("tgi-keys")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_DeleteAndBackspaceRemoveTheFocusedChipAndMoveTheFocus()
    {
        await OpenAsync(Route);

        await Tag("tgi-keys", "two").FocusAsync();
        await Page.Keyboard.PressAsync("Delete");
        await ExpectTextAsync("keys-value", "one|three");
        await Expect(Tag("tgi-keys", "three")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Backspace");
        await ExpectTextAsync("keys-value", "one");
        await Expect(Tag("tgi-keys", "one")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Delete");
        await ExpectTextAsync("keys-value", "null");
        await Expect(Input("tgi-keys")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_BackspaceTabAndABlurByDefault()
    {
        await OpenAsync(Route);

        await Input("tgi-keys").FocusAsync();
        await Page.Keyboard.PressAsync("Backspace");
        await ExpectTextAsync("keys-value", "one|two");

        await TypeAsync("tgi-keys", "tabbed");
        await Page.Keyboard.PressAsync("Tab");
        await ExpectTextAsync("keys-value", "one|two|tabbed");
        await Expect(Input("tgi-keys")).ToBeFocusedAsync();

        await TypeAsync("tgi-keys", "blurred");
        await Page.FocusAsync("#after-keys");
        await ExpectTextAsync("keys-value", "one|two|tabbed|blurred");
    }

    [TestMethod]
    public async Task NoAddOnBlurTabAndBackspace_LeaveOnlyEnter()
    {
        await OpenAsync(Route);

        var input = Input("tgi-no-adds");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Backspace");
        await input.PressSequentiallyAsync("x1", new() { Delay = 40 });
        await Page.Keyboard.PressAsync("Tab");
        await Expect(input).Not.ToBeFocusedAsync();
        await NextFramesAsync();
        await ExpectTextAsync("no-adds-value", "one|two");
        await Expect(input).ToHaveValueAsync("x1");

        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await ExpectTextAsync("no-adds-value", "one|two|x1");
    }

    [TestMethod]
    public async Task BackspaceEditsLastTagAndClearOnBlur()
    {
        await OpenAsync(Route);

        var input = Input("tgi-backspace-edits");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Backspace");
        await ExpectTextAsync("backspace-edits-value", "one");
        await Expect(input).ToHaveValueAsync("two");

        await Page.FocusAsync("#after-keys");
        await Expect(input).ToHaveValueAsync("");
        await ExpectTextAsync("backspace-edits-value", "one");
    }

    [TestMethod]
    public async Task InputAttributes_InputModeEnterKeyHintAutoCompleteAndSpellCheck()
    {
        await OpenAsync(Route);

        var phone = Input("tgi-phone");
        await Expect(phone).ToHaveAttributeAsync("inputmode", "email");
        await Expect(phone).ToHaveAttributeAsync("enterkeyhint", "done");
        await Expect(phone).ToHaveAttributeAsync("autocomplete", "email");
        await Expect(phone).ToHaveAttributeAsync("spellcheck", "true");

        var plain = Input("tgi-keys");
        await Expect(plain).ToHaveAttributeAsync("autocomplete", "off");
        await Expect(plain).ToHaveAttributeAsync("spellcheck", "false");
        await Expect(plain).Not.ToHaveAttributeAsync("inputmode", new Regex(".*"));
    }

    [TestMethod]
    public async Task CancelConfirmKeysOnEmpty_LeavesAnEmptyEnterToThePage()
    {
        await OpenAsync(Route);

        const string script = @"el => {
            const e = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
            el.dispatchEvent(e);
            return e.defaultPrevented;
        }";

        Assert.IsFalse(await Input("tgi-cancel-empty").EvaluateAsync<bool>(script));
        Assert.IsTrue(await Input("tgi-keys").EvaluateAsync<bool>(script));
    }

    [TestMethod]
    public async Task ThrottleTime_ReportsTheFirstKeystrokeAndThenTheLastOfTheBurst()
    {
        await OpenAsync(Route);

        await TypeAsync("tgi-throttle", "abcde");
        await ExpectTextAsync("throttle-text", "abcde");
        await ExpectTextAsync("throttle-calls", "2");
    }

    [TestMethod]
    public async Task NoClearOnEscape_LeavesTheTextAndTheEscapeAlone()
    {
        await OpenAsync(Route);

        var input = Input("tgi-no-escape");
        await input.PressSequentiallyAsync("keep", new() { Delay = 40 });
        await Expect(input).Not.ToHaveAttributeAsync("data-bit-esc", new Regex(".*"));
        await input.PressAsync("Escape");
        await NextFramesAsync();
        await Expect(input).ToHaveValueAsync("keep");
    }

    // ---------------------------------------------------------------- accessibility & localization

    [TestMethod]
    public async Task Announcements_AreSpokenInTheGivenWords()
    {
        await OpenAsync(Route);

        var live = Live("tgi-announce");
        await Expect(live).ToHaveAttributeAsync("role", "status");
        await Expect(live).ToHaveAttributeAsync("aria-live", "polite");

        await AddAsync("tgi-announce", "gamma");
        await Expect(live).ToHaveTextAsync("gamma was added to the list.");

        await AddAsync("tgi-announce", "g");
        await Expect(live).ToHaveTextAsync("No g: A tag must be at least 2 characters.");

        await Tag("tgi-announce", "gamma").Locator(".bit-tgi-dbt").ClickAsync();
        await Expect(live).ToHaveTextAsync("gamma was taken off.");

        await Tag("tgi-announce", "alpha").FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowRight");
        await Expect(live).ToHaveTextAsync("alpha now at 2/2.");

        await Tag("tgi-announce", "alpha").Locator(".bit-tgi-rbt").ClickAsync();
        await Expect(live).ToHaveTextAsync("Carrying alpha.");
        await Tag("tgi-announce", "alpha").Locator(".bit-tgi-rbt").ClickAsync();
        await Expect(live).ToHaveTextAsync("Dropped alpha back.");

        await Tag("tgi-announce", "beta").DblClickAsync();
        var editor = Root("tgi-announce").Locator("input.bit-tgi-eip");
        await Expect(editor).ToHaveAttributeAsync("aria-label", "Rename beta");
        await editor.FillAsync("delta");
        await editor.PressAsync("Enter");
        await Expect(live).ToHaveTextAsync("delta was renamed.");

        await Root("tgi-announce").Locator(".bit-tgi-cbt").ClickAsync();
        await Expect(live).ToHaveTextAsync("Wiped 2.");
    }

    [TestMethod]
    public async Task LocalizableLabels_NameTheDismissReorderAndClearButtons()
    {
        await OpenAsync(Route);

        var alpha = Tag("tgi-announce", "alpha");
        await Expect(alpha.Locator(".bit-tgi-dbt")).ToHaveAttributeAsync("aria-label", "Drop alpha");
        await Expect(alpha.Locator(".bit-tgi-dbt")).ToHaveAttributeAsync("title", "Drop it");
        await Expect(alpha.Locator(".bit-tgi-rbt")).ToHaveAttributeAsync("aria-label", "Carry alpha");
        await Expect(alpha.Locator(".bit-tgi-rbt")).ToHaveAttributeAsync("title", "Carry");
        await alpha.Locator(".bit-tgi-rbt").ClickAsync();
        await Expect(Tag("tgi-announce", "beta").Locator(".bit-tgi-rbt")).ToHaveAttributeAsync("aria-label", "Put alpha here");

        var clear = Root("tgi-announce").Locator(".bit-tgi-cbt");
        await Expect(clear).ToHaveAttributeAsync("aria-label", "Wipe everything");
        await Expect(clear).ToHaveAttributeAsync("title", "Wipe");
    }

    [TestMethod]
    public async Task Accessibility_TheListTheTagHintsAndTheCountAreNamed()
    {
        await OpenAsync(Route);

        await Expect(Root("tgi-names").Locator("[role='list']")).ToHaveAttributeAsync("aria-label", "Selected skills");
        await Expect(Tags("tgi-names").First).ToHaveAccessibleDescriptionAsync("Press Enter to rename this skill.");
        await Expect(Input("tgi-names")).ToHaveAccessibleDescriptionAsync("2 of 5 skills chosen.");

        await Expect(Tags("tgi-default-hints").First).ToHaveAccessibleDescriptionAsync("Press Enter to open, or Delete to remove.");
        await Expect(Input("tgi-default-hints")).ToHaveAccessibleDescriptionAsync("2 tags.");
        await Expect(Tags("tgi-default-hints").First).ToHaveClassAsync(HasClass("bit-tgi-tag-clk"));

        await Expect(Tags("tgi-reorder").First).ToHaveAccessibleDescriptionAsync("Press Alt with the arrow keys to move, or Delete to remove.");
        await Expect(Tags("tgi-edit").First).ToHaveAccessibleDescriptionAsync("Press Enter to edit, or Delete to remove.");
    }

    // ---------------------------------------------------------------- prefix & suffix

    [TestMethod]
    public async Task PrefixAndSuffix_FrameTheFieldAndDescribeTheInput()
    {
        await OpenAsync(Route);

        var prefix = Root("tgi-prefix").Locator(".bit-tgi-pre");
        await Expect(prefix).ToHaveTextAsync("To:");
        var prefixBox = await BoxAsync(prefix);
        var tagBox = await BoxAsync(Tags("tgi-prefix").First);
        Assert.IsTrue(prefixBox.X + prefixBox.Width <= tagBox.X + 0.5, "The prefix is not before the tags.");
        await Expect(Input("tgi-prefix")).ToHaveAccessibleDescriptionAsync(new Regex("To:"));

        var suffix = Root("tgi-suffix").Locator(".bit-tgi-suf");
        await Expect(suffix).ToHaveTextAsync("cm");
        var suffixBox = await BoxAsync(suffix);
        var inputBox = await BoxAsync(Input("tgi-suffix"));
        Assert.IsTrue(inputBox.X + inputBox.Width <= suffixBox.X + 0.5, "The suffix is not after the input.");
        await Expect(Input("tgi-suffix")).ToHaveAccessibleDescriptionAsync(new Regex("cm"));

        await AddAsync("tgi-suffix", "12a");
        await Expect(InvalidMessage("tgi-suffix")).ToHaveTextAsync("12a was not added. It is not in the expected format.");
        await AddAsync("tgi-suffix", "56");
        await Expect(Tags("tgi-suffix")).ToHaveTextAsync(["12", "34", "56"]);

        await Expect(Root("tgi-affix-templates").Locator(".bit-tgi-pre .pre-tpl")).ToHaveTextAsync("#");
        await Expect(Root("tgi-affix-templates").Locator(".bit-tgi-suf .suf-tpl")).ToHaveTextAsync("!");
    }

    // ---------------------------------------------------------------- tag template & styling

    [TestMethod]
    public async Task TagTemplateAndGetTagName_DrawAndNameTheChips()
    {
        await OpenAsync(Route);

        await Expect(Root("tgi-tag-template").Locator(".bit-tgi-tag .tag-tpl")).ToHaveTextAsync(["#blazor", "#dotnet"]);
        await Expect(Root("tgi-tag-template").Locator(".bit-tgi-ttx")).ToHaveCountAsync(0);

        var ada = Tags("tgi-tag-name").First;
        await Expect(ada.Locator(".person")).ToHaveTextAsync("Ada Lovelace");
        await Expect(ada).ToHaveAttributeAsync("aria-label", "Ada Lovelace");
        await Expect(ada.Locator(".bit-tgi-dbt")).ToHaveAttributeAsync("aria-label", "Remove Ada Lovelace");
    }

    [TestMethod]
    public async Task GetTagStyleAndGetTagClass_StyleEachTagByItsValue()
    {
        await OpenAsync(Route);

        await Expect(Tags("tgi-tag-style").Nth(1)).ToHaveCSSAsync("background-color", "rgb(253, 231, 233)");
        await Expect(Tags("tgi-tag-style").Nth(1)).ToHaveCSSAsync("color", "rgb(164, 38, 44)");
        await Expect(Tags("tgi-tag-style").Nth(0)).Not.ToHaveCSSAsync("background-color", "rgb(253, 231, 233)");

        await Expect(Tag("tgi-tag-class", "low")).ToHaveClassAsync(HasClass("priority-low"));
        await Expect(Tag("tgi-tag-class", "medium")).ToHaveClassAsync(HasClass("priority-medium"));
        await Expect(Tag("tgi-tag-class", "high")).ToHaveClassAsync(HasClass("priority-high"));
    }

    // ---------------------------------------------------------------- binding & forms

    [TestMethod]
    public async Task TwoWayBinding_FlowsBothWays()
    {
        await OpenAsync(Route);

        await AddAsync("tgi-two-way", "a1");
        await ExpectTextAsync("two-way-value", "a1");

        await Page.ClickAsync("#btn-two-way");
        await Expect(Tags("tgi-two-way")).ToHaveTextAsync(["x", "y", "z"]);

        await Page.ClickAsync("#btn-two-way-null");
        await Expect(Tags("tgi-two-way")).ToHaveCountAsync(0);
        await Expect(Root("tgi-two-way").Locator("[role='list']")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task UncontrolledAndOneWay_OnChangeReportsAndAOneWayValueTakesNothing()
    {
        await OpenAsync(Route);

        await AddAsync("tgi-uncontrolled", "vue");
        await ExpectTextAsync("uncontrolled-value", "blazor|vue");
        await Tag("tgi-uncontrolled", "blazor").Locator(".bit-tgi-dbt").ClickAsync();
        await ExpectTextAsync("uncontrolled-value", "vue");

        await AddAsync("tgi-one-way", "new");
        await Tags("tgi-one-way").First.Locator(".bit-tgi-dbt").ClickAsync();
        await NextFramesAsync();
        await Expect(Tags("tgi-one-way")).ToHaveTextAsync(["fixed"]);
    }

    [TestMethod]
    public async Task Validation_ARequiredListFailsTheFormUntilATagIsAdded()
    {
        await OpenAsync(Route);

        var root = Root("tgi-form");
        var input = Input("tgi-form");
        await Expect(input).ToHaveAttributeAsync("data-input", "tags");
        await Expect(root.Locator("input[type='hidden']")).ToHaveAttributeAsync("name", "tags");

        await Page.ClickAsync("#tgi-form-submit");
        await ExpectTextAsync("form-result", "invalid");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(input).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#tgi-form-msg")).ToHaveTextAsync("At least one tag is required.");
        await Expect(input).ToHaveAccessibleDescriptionAsync(new Regex("At least one tag is required."));
        await Expect(root.Locator(".bit-tgi-cnt")).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");

        await TypeAsync("tgi-form", "a1,");
        await Expect(root.Locator("input[type='hidden']")).ToHaveValueAsync("a1");
        await Expect(input).ToHaveValueAsync("");
        await TypeAsync("tgi-form", "b2,");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(root.Locator("input[type='hidden']")).ToHaveValueAsync("a1,b2");

        await Page.ClickAsync("#tgi-form-submit");
        await ExpectTextAsync("form-result", "valid");
    }

    // ---------------------------------------------------------------- events

    [TestMethod]
    public async Task Events_BeforeAddCanCancelOrRewriteAndEveryChangeIsReported()
    {
        await OpenAsync(Route);

        await Input("tgi-events").FocusAsync();
        await ExpectTextAsync("events-log", "focusin");

        await TypeAsync("tgi-events", "block");
        await ExpectTextAsync("last-input", "block");
        await Input("tgi-events").PressAsync("Enter");
        await ExpectTextAsync("last-key", "Enter");
        await ExpectTextAsync("events-log", "focusin;cancelled:block");
        await Expect(Tags("tgi-events")).ToHaveTextAsync(["blazor"]);

        await Input("tgi-events").FillAsync("me");
        await Input("tgi-events").PressAsync("Enter");
        await Expect(Tags("tgi-events")).ToHaveTextAsync(["blazor", "me-corrected"]);
        await ExpectTextAsync("events-log", "focusin;cancelled:block;add:me-corrected");

        await AddAsync("tgi-events", "blazor");
        await ExpectTextAsync("events-log", "focusin;cancelled:block;add:me-corrected;invalid:blazor:Duplicate");
    }

    [TestMethod]
    public async Task Events_BeforeRemoveCanKeepATagAndClicksAndFocusAreReported()
    {
        await OpenAsync(Route);

        await AddAsync("tgi-events", "vue");
        await Page.ClickAsync("#btn-clear-log");
        await ExpectTextAsync("events-log", "");

        await Tag("tgi-events", "blazor").Locator(".bit-tgi-dbt").ClickAsync();
        await ExpectTextAsync("events-log", "kept:blazor");
        await Expect(Tags("tgi-events")).ToHaveTextAsync(["blazor", "vue"]);

        await Tag("tgi-events", "vue").Locator(".bit-tgi-dbt").ClickAsync();
        // A removal hands the focus to the input, which reports it.
        await Expect(Page.Locator("#events-log")).ToHaveTextAsync("kept:blazor;remove:vue;focusin");

        await Tag("tgi-events", "blazor").ClickAsync();
        await Expect(Page.Locator("#events-log")).ToHaveTextAsync(new Regex("click:blazor"));

        await Input("tgi-events").FocusAsync();
        await Page.FocusAsync("#btn-clear-log");
        await Expect(Page.Locator("#events-log")).ToHaveTextAsync(new Regex("focusout$"));
    }

    // ---------------------------------------------------------------- public api

    [TestMethod]
    public async Task PublicApi_AddsRemovesMovesEditsAndClearsFromCode()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#api-add");
        await ExpectTextAsync("api-value", "blazor|added");
        await Page.ClickAsync("#api-add-many");
        await ExpectTextAsync("api-value", "blazor|added|one|two|pinned");
        await Page.ClickAsync("#api-remove");
        await ExpectTextAsync("api-value", "blazor|added|two|pinned");
        await Page.ClickAsync("#api-remove-at");
        await ExpectTextAsync("api-value", "added|two|pinned");
        await Page.ClickAsync("#api-move");
        await ExpectTextAsync("api-value", "two|added|pinned");

        // Clear respects CanRemoveTag; RemoveTagAsync does not.
        await Page.ClickAsync("#api-clear");
        await ExpectTextAsync("api-value", "pinned");
        await Page.ClickAsync("#api-remove-pinned");
        await ExpectTextAsync("api-value", "null");
    }

    [TestMethod]
    public async Task PublicApi_SetsTheTextOpensAnEditAndFocuses()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#api-set-text");
        await Expect(Input("tgi-api")).ToHaveValueAsync("typed");

        await Page.ClickAsync("#api-focus");
        await Expect(Input("tgi-api")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await ExpectTextAsync("api-value", "blazor|typed");

        await Page.ClickAsync("#api-edit");
        var editor = Root("tgi-api").Locator("input.bit-tgi-eip");
        await Expect(editor).ToBeFocusedAsync();
        await Expect(editor).ToHaveValueAsync("blazor");
    }

    // ---------------------------------------------------------------- escape in a modal

    [TestMethod]
    public async Task EscapeInsideAModal_TakesBackTheTypingFirstAndClosesTheModalNext()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-modal");
        var input = Input("tgi-modal");
        await Expect(input).ToBeVisibleAsync();

        // The modal moves the focus into itself once it has opened; the test waits for that before it types.
        await Page.WaitForFunctionAsync("() => document.activeElement && document.activeElement.closest('.bit-mdl')");

        await input.PressSequentiallyAsync("draft", new() { Delay = 40 });
        await Expect(input).ToHaveAttributeAsync("data-bit-esc", "claim");
        await input.PressAsync("Escape");
        await Expect(input).ToHaveValueAsync("");
        await ExpectTextAsync("modal-open", "True");

        // An edit's Escape is the edit's own as well.
        await Tag("tgi-modal", "m1").DblClickAsync();
        var editor = Root("tgi-modal").Locator("input.bit-tgi-eip");
        await Expect(editor).ToBeFocusedAsync();
        await editor.PressAsync("Escape");
        await Expect(editor).ToHaveCountAsync(0);
        await ExpectTextAsync("modal-open", "True");

        // A carried tag is put back by Escape without the modal closing.
        await Tag("tgi-modal", "m1").Locator(".bit-tgi-rbt").ClickAsync();
        await Expect(Root("tgi-modal").Locator("[role='list']")).ToHaveAttributeAsync("data-bit-esc", "claim");
        await Tag("tgi-modal", "m1").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Root("tgi-modal")).Not.ToHaveClassAsync(HasClass("bit-tgi-pck"));
        await ExpectTextAsync("modal-open", "True");

        // With nothing left to take back, Escape is the modal's.
        await input.FocusAsync();
        await input.PressAsync("Escape");
        await ExpectTextAsync("modal-open", "False");
        await ExpectTextAsync("modal-value", "m1|m2");
    }

    // ---------------------------------------------------------------- cascade, color, icons, size

    [TestMethod]
    public async Task CascadingParameters_FillWhatTheInputLeavesUnsetAndYieldToWhatItSets()
    {
        await OpenAsync(Route);

        var cascaded = Root("tgi-cascade-1");
        foreach (var cssClass in new[] { "bit-tgi-sm", "bit-tgi-fil", "bit-tgi-tgo", "bit-tgi-inf" })
        {
            await Expect(cascaded).ToHaveClassAsync(HasClass(cssClass));
        }

        await Expect(Input("tgi-cascade-1")).ToHaveAttributeAsync("placeholder", "cascaded placeholder");
        await TypeTagsAsync("tgi-cascade-1", "cascade-value", ("ab,", "AB"), ("cd,", "AB|CD"));

        var own = Root("tgi-cascade-own");
        await Expect(own).ToHaveClassAsync(HasClass("bit-tgi-suc"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-tgi-inf"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-tgi-sm"));

        var outside = Root("tgi-cascade-outside");
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-tgi-sm"));
        await Expect(outside).ToHaveClassAsync(HasClass("bit-tgi-otl"));
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-tgi-inf"));
    }

    [TestMethod]
    public async Task Color_EveryValuePaintsTheTagsInItsRole()
    {
        await OpenAsync(Route);

        string[] slugs = ["pri", "sec", "ter", "inf", "suc", "wrn", "swr", "err", "pbg", "sbg", "tbg", "pfg", "sfg", "tfg", "pbr", "sbr", "tbr"];

        var unset = await CssAsync(Tags("tgi-color-none").First, "background-color");
        var colors = new Dictionary<string, string>();

        foreach (var slug in slugs)
        {
            var id = $"tgi-color-{slug}";
            await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-tgi-{slug}"));

            var role = await Root(id).EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--bit-tgi-clr').trim()");
            var text = await Root(id).EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--bit-tgi-clr-txt').trim()");
            var expected = await ResolveColorAsync(role);

            await Expect(Tags(id).First).ToHaveCSSAsync("background-color", expected);
            await Expect(Tags(id).First).ToHaveCSSAsync("color", await ResolveColorAsync(text));
            colors[slug] = expected;
        }

        Assert.AreEqual(unset, colors["pri"], "Unset is not primary.");
        Assert.IsTrue(colors.Values.Distinct().Count() >= 12, string.Join(", ", colors.Select(c => $"{c.Key}={c.Value}")));
    }

    [TestMethod]
    public async Task Icons_ExternalIconsAndIconNamesReplaceTheDefaults()
    {
        await OpenAsync(Route);

        await Expect(Root("tgi-icons").Locator(".bit-tgi-dsi").First).ToHaveClassAsync(HasClass("ext-dismiss"));
        await Expect(Root("tgi-icons").Locator(".bit-tgi-cli")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid\s+fa-xmark(\s|$)"));
        await Expect(Root("tgi-icons").Locator(".bit-tgi-rdi").First).ToHaveClassAsync(new Regex(@"(^|\s)bi\s+bi-grip-vertical(\s|$)"));

        await Expect(Root("tgi-icon-names").Locator(".bit-tgi-dsi").First).ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));
        await Expect(Root("tgi-icon-names").Locator(".bit-tgi-cli")).ToHaveClassAsync(HasClass("bit-icon--Delete"));
        await Expect(Root("tgi-icon-names").Locator(".bit-tgi-rdi").First).ToHaveClassAsync(HasClass("bit-icon--Move"));
    }

    [TestMethod]
    [DataRow("sm", "md")]
    [DataRow("md", "lg")]
    public async Task Size_ScalesTheFieldAndTheTags(string smaller, string larger)
    {
        await OpenAsync(Route);

        await Expect(Root($"tgi-size-{smaller}")).ToHaveClassAsync(HasClass($"bit-tgi-{smaller}"));
        await Expect(Root($"tgi-size-{larger}")).ToHaveClassAsync(HasClass($"bit-tgi-{larger}"));

        Assert.IsTrue(Px(await CssAsync(Root($"tgi-size-{larger}").Locator(".bit-tgi-cnt"), "min-height")) >
                      Px(await CssAsync(Root($"tgi-size-{smaller}").Locator(".bit-tgi-cnt"), "min-height")), "field height");
        Assert.IsTrue(Px(await CssAsync(Root($"tgi-size-{larger}"), "font-size")) >
                      Px(await CssAsync(Root($"tgi-size-{smaller}"), "font-size")), "field font size");
        Assert.IsTrue((await BoxAsync(Root($"tgi-size-{larger}").Locator(".bit-tgi-cnt"))).Height >
                      (await BoxAsync(Root($"tgi-size-{smaller}").Locator(".bit-tgi-cnt"))).Height, "field box");

        // The chips step up with the field from medium to large; small and medium share the small chip.
        var smallTag = Px(await CssAsync(Tags($"tgi-size-{smaller}").First, "font-size"));
        var largeTag = Px(await CssAsync(Tags($"tgi-size-{larger}").First, "font-size"));
        if (larger == "lg") Assert.IsTrue(largeTag > smallTag, "tag font size");
        else Assert.AreEqual(smallTag, largeTag, "tag font size");
    }

    // ---------------------------------------------------------------- style & class

    [TestMethod]
    public async Task StyleClassAndHtmlAttributes_LandOnTheRoot()
    {
        await OpenAsync(Route);

        await Expect(Root("tgi-style")).ToHaveCSSAsync("margin-left", "13px");
        await Expect(Root("tgi-style")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Root("tgi-attrs")).ToHaveAttributeAsync("data-test", "attr-value");
        await Expect(Root("tgi-attrs")).ToHaveAttributeAsync("title", "root title");
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart()
    {
        await OpenAsync(Route);

        var root = Root("tgi-styles");
        await Expect(root).ToHaveCSSAsync("padding-top", "5px");
        await Expect(root.Locator(".bit-tgi-lbl")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(root.Locator(".bit-tgi-cnt")).ToHaveCSSAsync("column-gap", "7px");
        await Expect(root.Locator(".bit-tgi-pre")).ToHaveCSSAsync("color", "rgb(2, 2, 2)");
        await Expect(root.Locator(".bit-tgi-suf")).ToHaveCSSAsync("color", "rgb(3, 3, 3)");
        await Expect(root.Locator(".bit-tgi-tag").First).ToHaveCSSAsync("margin-top", "2px");
        await Expect(root.Locator(".bit-tgi-tag").First).ToHaveCSSAsync("font-style", "italic");
        await Expect(root.Locator(".bit-tgi-ttx").First).ToHaveCSSAsync("text-decoration-line", "underline");
        await Expect(root.Locator(".bit-tgi-rbt").First).ToHaveCSSAsync("opacity", "0.7");
        await Expect(root.Locator(".bit-tgi-rdi").First).ToHaveCSSAsync("color", "rgb(5, 5, 5)");
        await Expect(root.Locator(".bit-tgi-dbt").First).ToHaveCSSAsync("opacity", "0.6");
        await Expect(root.Locator(".bit-tgi-dsi").First).ToHaveCSSAsync("color", "rgb(6, 6, 6)");
        await Expect(root.Locator(".bit-tgi-tgl")).ToHaveCSSAsync("color", "rgb(7, 7, 7)");
        await Expect(root.Locator("input.bit-tgi-inp")).ToHaveCSSAsync("color", "rgb(8, 8, 8)");
        await Expect(root.Locator(".bit-tgi-cnr")).ToHaveCSSAsync("color", "rgb(10, 10, 10)");
        await Expect(root.Locator(".bit-tgi-spn")).ToHaveCSSAsync("opacity", "0.5");
        await Expect(root.Locator(".bit-tgi-cbt")).ToHaveCSSAsync("opacity", "0.4");
        await Expect(root.Locator(".bit-tgi-cli")).ToHaveCSSAsync("color", "rgb(11, 11, 11)");
        await Expect(root.Locator(".bit-tgi-dsc")).ToHaveCSSAsync("color", "rgb(12, 12, 12)");

        // The states: focused root, focused tag, the tag being edited and its input.
        await root.Locator("input.bit-tgi-inp").FocusAsync();
        await Expect(root).ToHaveCSSAsync("outline-color", "rgb(1, 1, 1)");
        await root.Locator(".bit-tgi-tag").Nth(1).FocusAsync();
        await Expect(root.Locator(".bit-tgi-tag").Nth(1)).ToHaveCSSAsync("outline-color", "rgb(4, 4, 4)");
        await Page.Keyboard.PressAsync("F2");
        await Expect(root.Locator(".bit-tgi-tag").Nth(1)).ToHaveCSSAsync("opacity", "0.8");
        await Expect(root.Locator("input.bit-tgi-eip")).ToHaveCSSAsync("color", "rgb(9, 9, 9)");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart()
    {
        await OpenAsync(Route);

        var root = Root("tgi-classes");
        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        foreach (var (part, cssClass) in new[]
        {
            (".bit-tgi-lbl", "c-lbl"), (".bit-tgi-cnt", "c-cnt"), (".bit-tgi-pre", "c-pre"), (".bit-tgi-suf", "c-suf"),
            (".bit-tgi-tgs", "c-tgs"), (".bit-tgi-tag", "c-tag"), (".bit-tgi-tag-fix", "c-fix"), (".bit-tgi-ttx", "c-ttx"),
            (".bit-tgi-rbt", "c-rbt"), (".bit-tgi-rdi", "c-rdi"), (".bit-tgi-dbt", "c-dbt"), (".bit-tgi-dsi", "c-dsi"),
            (".bit-tgi-tgl", "c-tgl"), ("input.bit-tgi-inp", "c-inp"), (".bit-tgi-cnr", "c-cnr"), (".bit-tgi-spn", "c-spn"),
            (".bit-tgi-cbt", "c-cbt"), (".bit-tgi-cli", "c-cli"), (".bit-tgi-dsc", "c-dsc")
        })
        {
            await Expect(root.Locator(part).First).ToHaveClassAsync(HasClass(cssClass));
        }

        await root.Locator("input.bit-tgi-inp").FocusAsync();
        await Expect(root).ToHaveClassAsync(HasClass("c-fcs"));
        await root.Locator(".bit-tgi-tag").Nth(1).FocusAsync();
        await Expect(root.Locator(".bit-tgi-tag").Nth(1)).ToHaveClassAsync(HasClass("c-ftag"));
        await Page.Keyboard.PressAsync("F2");
        await Expect(root.Locator(".bit-tgi-tag").Nth(1)).ToHaveClassAsync(HasClass("c-edt"));
        await Expect(root.Locator("input.bit-tgi-eip")).ToHaveClassAsync(HasClass("c-eip"));
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachThePickedUpDuplicateAndInvalidStates()
    {
        await OpenAsync(Route);

        var root = Root("tgi-states");
        await Expect(root.Locator(".bit-tgi-tgs")).ToHaveCSSAsync("color", "rgb(16, 16, 16)");

        await Tag("tgi-states", "abc").Locator(".bit-tgi-rbt").ClickAsync();
        await Expect(Tag("tgi-states", "abc")).ToHaveClassAsync(HasClass("c-pck"));
        await Expect(Tag("tgi-states", "abc")).ToHaveCSSAsync("outline-color", "rgb(13, 13, 13)");
        await Tag("tgi-states", "abc").Locator(".bit-tgi-rbt").ClickAsync();

        await AddAsync("tgi-states", "def");
        await Expect(Tag("tgi-states", "def")).ToHaveClassAsync(HasClass("c-dup"));
        await Expect(Tag("tgi-states", "def")).ToHaveCSSAsync("outline-color", "rgb(14, 14, 14)");
        await Expect(InvalidMessage("tgi-states")).ToHaveClassAsync(HasClass("c-ivm"));
        await Expect(InvalidMessage("tgi-states")).ToHaveCSSAsync("color", "rgb(15, 15, 15)");
    }

    // ---------------------------------------------------------------- css variables

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheField()
    {
        await OpenAsync(Route);

        var root = Root("tgi-vars");
        var field = root.Locator(".bit-tgi-cnt");
        await Expect(root).ToHaveCSSAsync("font-size", "17px");
        await Expect(Input("tgi-vars")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(root.Locator(".bit-tgi-lbl")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        Assert.AreEqual("rgb(7, 8, 9)", await root.Locator(".bit-tgi-lbl").EvaluateAsync<string>("el => getComputedStyle(el, '::after').color"));
        await Expect(root.Locator(".bit-tgi-dsc")).ToHaveCSSAsync("color", "rgb(10, 11, 12)");
        await Expect(root.Locator(".bit-tgi-dsc")).ToHaveCSSAsync("font-size", "9px");
        await Expect(root.Locator(".bit-tgi-cnr")).ToHaveCSSAsync("color", "rgb(13, 14, 15)");
        await Expect(root.Locator(".bit-tgi-pre")).ToHaveCSSAsync("color", "rgb(16, 17, 18)");
        await Expect(field).ToHaveCSSAsync("background-color", "rgb(19, 20, 21)");
        await Expect(field).ToHaveCSSAsync("border-top-color", "rgb(22, 23, 24)");
        await Expect(field).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(field).ToHaveCSSAsync("border-top-left-radius", "7px");
        await Expect(field).ToHaveCSSAsync("min-height", "50px");
        await Expect(field).ToHaveCSSAsync("padding-top", "6px");
        await Expect(field).ToHaveCSSAsync("column-gap", "11px");

        var tag = Tags("tgi-vars").First;
        await Expect(tag).ToHaveCSSAsync("color", "rgb(25, 26, 27)");
        await Expect(tag).ToHaveCSSAsync("background-color", "rgb(28, 29, 30)");
        await Expect(tag).ToHaveCSSAsync("border-top-color", "rgb(31, 32, 33)");
        await Expect(tag).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(tag).ToHaveCSSAsync("border-top-left-radius", "9px");
        await Expect(tag).ToHaveCSSAsync("padding-left", "8px");
        await Expect(tag).ToHaveCSSAsync("column-gap", "5px");
        await Expect(tag).ToHaveCSSAsync("font-size", "15px");
        await Expect(tag).ToHaveCSSAsync("min-height", "30px");
        await Expect(Tags("tgi-vars").Nth(1)).ToHaveCSSAsync("max-width", "120px");
        Assert.IsTrue((await BoxAsync(Tags("tgi-vars").Nth(1))).Width <= 120.5);
        await Expect(root.Locator(".bit-tgi-dsi").First).ToHaveCSSAsync("font-size", "13px");
        await Expect(root.Locator(".bit-tgi-cbt")).ToHaveCSSAsync("color", "rgb(34, 35, 36)");
    }

    [TestMethod]
    public async Task CssVariables_EachVariantReadsItsOwnTierAheadOfTheSharedOne()
    {
        await OpenAsync(Route);

        await Expect(Root("tgi-vars-fill").Locator(".bit-tgi-cnt")).ToHaveCSSAsync("background-color", "rgb(40, 41, 42)");
        await Expect(Root("tgi-vars-outline").Locator(".bit-tgi-cnt")).ToHaveCSSAsync("background-color", "rgb(43, 44, 45)");
        await Expect(Root("tgi-vars-text").Locator(".bit-tgi-cnt")).ToHaveCSSAsync("background-color", "rgb(46, 47, 48)");
        await Expect(Tags("tgi-vars-fill").First).ToHaveCSSAsync("background-color", "rgb(49, 50, 51)");
        await Expect(Tags("tgi-vars-text").First).ToHaveCSSAsync("color", "rgb(55, 56, 57)");

        var placeholder = await Input("tgi-vars-outline").EvaluateAsync<string>("el => getComputedStyle(el, '::placeholder').color");
        Assert.AreEqual("rgb(67, 68, 69)", placeholder);

        await Expect(Root("tgi-vars-disabled").Locator(".bit-tgi-cnt")).ToHaveCSSAsync("background-color", "rgb(61, 62, 63)");

        await Root("tgi-vars-outline").HoverAsync();
        await Expect(Root("tgi-vars-outline").Locator(".bit-tgi-cnt")).ToHaveCSSAsync("border-top-color", "rgb(58, 59, 60)");

        await Tags("tgi-vars-fill").First.HoverAsync();
        await Expect(Tags("tgi-vars-fill").First).ToHaveCSSAsync("background-color", "rgb(64, 65, 66)");
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColorOrSizeWinsOverThem()
    {
        await OpenAsync(Route);

        await Expect(Tags("tgi-vars-explicit").First).Not.ToHaveCSSAsync("background-color", "rgb(28, 29, 30)");
        Assert.AreEqual(await CssAsync(Tags("tgi-color-err").First, "background-color"), await CssAsync(Tags("tgi-vars-explicit").First, "background-color"));
        await Expect(Tags("tgi-vars-explicit").First).Not.ToHaveCSSAsync("font-size", "15px");
    }

    // ---------------------------------------------------------------- rtl

    [TestMethod]
    public async Task Rtl_LaysTheTagsOutFromTheRightAndMirrorsTheArrows()
    {
        await OpenAsync(Route);

        var root = Root("tgi-rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");

        var first = await BoxAsync(Tag("tgi-rtl", "one"));
        var second = await BoxAsync(Tag("tgi-rtl", "two"));
        Assert.IsTrue(first.X > second.X, "The first tag is not on the right.");

        await Input("tgi-rtl").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Input("tgi-rtl")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tag("tgi-rtl", "three")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tag("tgi-rtl", "two")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Tag("tgi-rtl", "three")).ToBeFocusedAsync();

        await Tag("tgi-rtl", "one").FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowLeft");
        await ExpectTextAsync("rtl-value", "two|one|three");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_MirrorsTheArrowsToo()
    {
        await OpenAsync(Route);

        await Expect(Root("tgi-rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        var first = await BoxAsync(Tag("tgi-rtl-inherited", "one"));
        var second = await BoxAsync(Tag("tgi-rtl-inherited", "two"));
        Assert.IsTrue(first.X > second.X, "The first tag is not on the right.");

        await Input("tgi-rtl-inherited").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tag("tgi-rtl-inherited", "three")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Delete");
        await ExpectTextAsync("rtl-inherited-value", "one|two");
    }

    // ---------------------------------------------------------------- autofocus & motion

    [TestMethod]
    public async Task AutoFocus_TheInputHasTheFocusWhenThePageRenders()
    {
        await OpenAsync("/e2e/tags-input/autofocus");

        await Expect(Input("tgi-autofocus")).ToHaveAttributeAsync("autofocus", "");
        await Expect(Input("tgi-autofocus")).ToBeFocusedAsync();
        await Expect(Input("tgi-no-autofocus")).Not.ToHaveAttributeAsync("autofocus", new Regex(".*"));
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheTransitionsUnlessForceAnimationRestoresThem()
    {
        await OpenAsync(Route);

        var field = Root("tgi-motion").Locator(".bit-tgi-cnt");
        StringAssert.StartsWith(await CssAsync(field, "transition-duration"), "0.1s");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Expect(field).Not.ToHaveCSSAsync("transition-duration", new Regex("^0\\.1s"));
        var reduced = (await CssAsync(field, "transition-duration")).Split(',')[0].Trim();
        Assert.IsTrue(double.Parse(reduced.TrimEnd('s'), CultureInfo.InvariantCulture) < 0.001, reduced);

        await Expect(Root("tgi-motion-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        StringAssert.StartsWith(await CssAsync(Root("tgi-motion-forced").Locator(".bit-tgi-cnt"), "transition-duration"), "0.1s");
    }
}
