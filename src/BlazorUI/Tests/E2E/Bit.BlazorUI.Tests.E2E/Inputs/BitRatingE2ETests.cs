using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// End-to-end tests of BitRating (<c>/e2e/rating</c>, <c>/e2e/rating/autofocus</c>): committing a value by click,
/// hover preview and keyboard (arrows, Shift, Page keys, Home/End, digits, Space/Enter, Delete), fractional precision
/// through the step slices, AllowZeroStars and AllowClear, HighlightSelectedOnly, Max and Vertical, icons, the item
/// template, one-way and two-way binding, OnChange / OnChanging / OnHoverChange / OnFocusIn / OnFocusOut, EditForm
/// validation and the hidden form input, the radiogroup / img accessibility tree, Escape inside a modal, Visibility,
/// the BitParams cascade, every Color and Size, Style / Class / Styles / Classes, the public CSS variables, RTL
/// (explicit and inherited), the disabled and read-only contracts, runtime re-renders, prerendering, unmounting and
/// reduced motion.
/// </summary>
[TestClass]
public class BitRatingE2ETests : E2ETestBase
{
    private const string Route = "/e2e/rating";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Items(string id) => Page.Locator($"#{id} .bit-rtg-btn");

    private ILocator Fill(string id, int index) => Items(id).Nth(index - 1).Locator(".bit-rtg-ifl");

    private ILocator Empty(string id, int index) => Items(id).Nth(index - 1).Locator(".bit-rtg-iem");

    private Task ExpectFillAsync(string id, int index, string percentage)
    {
        return Expect(Fill(id, index)).ToHaveAttributeAsync("style", new Regex($@"^width:{Regex.Escape(percentage)}%;"));
    }

    private async Task ExpectFillsAsync(string id, params string[] percentages)
    {
        for (var i = 0; i < percentages.Length; i++)
        {
            await ExpectFillAsync(id, i + 1, percentages[i]);
        }
    }

    private Task ExpectTextAsync(string id, string text) => Expect(Page.Locator($"#{id}")).ToHaveTextAsync(text);

    private async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no layout box.");
        return box;
    }

    /// <summary>
    /// Clicks an item at the given fraction of its width (or, for a vertical rating, of its height from the bottom).
    /// </summary>
    private async Task ClickItemAtAsync(string id, int index, double fraction, bool vertical = false)
    {
        var item = Items(id).Nth(index - 1);
        var box = await BoxAsync(item);
        var position = vertical
            ? new Position { X = (float)(box.Width / 2), Y = (float)(box.Height * (1 - fraction)) }
            : new Position { X = (float)(box.Width * fraction), Y = (float)(box.Height / 2) };

        await item.ClickAsync(new() { Position = position });
    }

    /// <summary>
    /// Waits until the filled layer of an item covers the given fraction of its glyph, along the axis it fills on.
    /// </summary>
    private Task ExpectRenderedFillAsync(string id, int index, double ratio, bool vertical = false)
    {
        return Page.WaitForFunctionAsync(@"([id, index, ratio, vertical]) => {
            const item = document.querySelectorAll('#' + id + ' .bit-rtg-btn')[index - 1];
            if (!item) return false;
            const fill = item.querySelector('.bit-rtg-ifl').getBoundingClientRect();
            const glyph = item.querySelector('.bit-rtg-iem').getBoundingClientRect();
            const size = vertical ? fill.height : fill.width;
            const whole = vertical ? glyph.height : glyph.width;
            return Math.abs(size - whole * ratio) < 1.5;
        }", new object[] { id, index, ratio, vertical });
    }

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

    private static double Px(string value) => double.Parse(value.Replace("px", ""), System.Globalization.CultureInfo.InvariantCulture);

    // ---------------------------------------------------------------- lifecycle

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsTheRatingBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"rtg-basic\"");
        StringAssert.Contains(html, "role=\"radiogroup\"");
        StringAssert.Contains(html, "bit-rtg-btn");
        StringAssert.Contains(html, ">Loading<");

        await OpenAsync(Route);
        await Expect(Items("rtg-basic")).ToHaveCountAsync(5);
    }

    [TestMethod]
    public async Task Unmount_RemovingTheRating_LeavesNoServerOrConsoleError()
    {
        await OpenAsync(Route);

        await Expect(Root("rtg-unmount")).ToBeVisibleAsync();
        await Page.ClickAsync("#btn-unmount");
        await Expect(Root("rtg-unmount")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Items("rtg-unmount")).ToHaveCountAsync(5);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Root("rtg-unmount")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Runtime_ChangingParametersFromThePage_ReRendersTheRating()
    {
        await OpenAsync(Route);

        var root = Root("rtg-runtime");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-rtg-err"));
        var colorBefore = await Fill("rtg-runtime", 1).EvaluateAsync<string>("el => getComputedStyle(el).color");
        var sizeBefore = await ComputedStyleAsync("#rtg-runtime .bit-rtg-btn", "font-size");

        await Page.ClickAsync("#btn-runtime-color");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtg-err"));
        await Expect(Fill("rtg-runtime", 1)).Not.ToHaveCSSAsync("color", colorBefore);

        await Page.ClickAsync("#btn-runtime-size");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtg-lg"));
        Assert.IsTrue(Px(await ComputedStyleAsync("#rtg-runtime .bit-rtg-btn", "font-size")) > Px(sizeBefore));

        await Page.ClickAsync("#btn-runtime-vertical");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtg-vrt"));
        await Expect(root).ToHaveAttributeAsync("aria-orientation", "vertical");
        await Expect(Fill("rtg-runtime", 1)).ToHaveAttributeAsync("style", new Regex("^height:100%;"));

        await Page.ClickAsync("#btn-runtime-label");
        await Expect(root.Locator(".bit-rtg-lbl")).ToHaveTextAsync("Changed label");
    }

    // ---------------------------------------------------------------- basic

    [TestMethod]
    public async Task Basic_AnUnboundRating_StartsAtTheSmallestStepAndReportsIt()
    {
        await OpenAsync(Route);

        await Expect(Items("rtg-basic")).ToHaveCountAsync(5);
        await ExpectFillsAsync("rtg-basic", "100", "0", "0", "0", "0");
        await Expect(Items("rtg-basic").Nth(0)).ToHaveAttributeAsync("aria-checked", "true");
        await ExpectTextAsync("basic-change-count", "1");
        await ExpectTextAsync("basic-last", "1");
    }

    [TestMethod]
    public async Task Basic_ClickingAnItem_FillsUpToItAndFiresOnChangeOnce()
    {
        await OpenAsync(Route);
        await ExpectTextAsync("basic-change-count", "1");

        await Items("rtg-basic").Nth(3).ClickAsync();

        await ExpectFillsAsync("rtg-basic", "100", "100", "100", "100", "0");
        await Expect(Items("rtg-basic").Nth(3)).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Items("rtg-basic").Nth(0)).ToHaveAttributeAsync("aria-checked", "false");
        await ExpectTextAsync("basic-change-count", "2");
        await ExpectTextAsync("basic-last", "4");

        // The rendered fill really covers the glyph once its transition is over: the filled layer is as wide as the
        // empty one under it, and the fifth one has no width at all.
        await ExpectRenderedFillAsync("rtg-basic", 4, 1);
        await ExpectRenderedFillAsync("rtg-basic", 5, 0);

        // Clicking the committed value again changes nothing.
        await Items("rtg-basic").Nth(3).ClickAsync();
        await ExpectTextAsync("basic-change-count", "2");
    }

    [TestMethod]
    public async Task DefaultValue_StartsTheUncontrolledRatingThere()
    {
        await OpenAsync(Route);

        await ExpectFillsAsync("rtg-default", "100", "100", "100", "0", "0");
        await Expect(Items("rtg-default").Nth(2)).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Items("rtg-default").Nth(2)).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Items("rtg-default").Nth(0)).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Root("rtg-default").Locator("input.bit-input-hidden")).ToHaveValueAsync("3");
    }

    // ---------------------------------------------------------------- disabled & read-only

    [TestMethod]
    public async Task Disabled_RendersTheDisabledMarkupAndIgnoresARealClick()
    {
        await OpenAsync(Route);

        var root = Root("rtg-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(root).ToHaveAttributeAsync("aria-disabled", "true");

        for (var i = 0; i < 5; i++)
        {
            await Expect(Items("rtg-disabled").Nth(i)).ToBeDisabledAsync();
            await Expect(Items("rtg-disabled").Nth(i)).ToHaveAttributeAsync("aria-disabled", "true");
        }

        var hidden = root.Locator("input.bit-input-hidden");
        await Expect(hidden).ToBeDisabledAsync();
        await Expect(hidden).ToHaveAttributeAsync("aria-disabled", "true");

        Assert.AreEqual("none", await ComputedStyleAsync("#rtg-disabled .bit-rtg-btn", "pointer-events"));
        Assert.AreEqual("not-allowed", await ComputedStyleAsync("#rtg-disabled .bit-rtg-btn", "cursor"));

        // A real click on the item lands on the rating's own box: neither the rating nor the page's handler runs.
        await Items("rtg-disabled").Nth(3).ClickAsync(new() { Force = true });
        await root.ClickAsync(new() { Force = true });
        await NextFramesAsync();
        await ExpectTextAsync("disabled-page-clicks", "0");
        await ExpectFillsAsync("rtg-disabled", "100", "100", "0", "0", "0");

        // The disabled color paints both layers.
        var disabledColor = await Empty("rtg-disabled", 5).EvaluateAsync<string>("el => getComputedStyle(el).color");
        Assert.AreEqual(disabledColor, await Fill("rtg-disabled", 1).EvaluateAsync<string>("el => getComputedStyle(el).color"));

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(root).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));
        await Expect(Items("rtg-disabled").Nth(3)).ToBeEnabledAsync();

        await Items("rtg-disabled").Nth(3).ClickAsync();
        await ExpectTextAsync("disabled-page-clicks", "1");
        await ExpectTextAsync("disabled-value", "4");
        await ExpectFillsAsync("rtg-disabled", "100", "100", "100", "100", "0");
    }

    [TestMethod]
    public async Task ReadOnly_IsAnnouncedAsAnImageOfItsValueAndCannotBeChanged()
    {
        await OpenAsync(Route);

        var root = Root("rtg-readonly");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtg-rdl"));
        await Expect(root).ToHaveAttributeAsync("role", "img");
        await Expect(root).ToHaveAttributeAsync("aria-label", "3.5 of 5");
        await Expect(root).Not.ToHaveAttributeAsync("aria-orientation", new Regex(".*"));
        await Expect(root.Locator("input.bit-input-hidden")).ToHaveAttributeAsync("readonly", "");
        await Expect(root.Locator("[aria-live]")).ToHaveCountAsync(0);
        await Expect(root.Locator(".bit-rtg-seg")).ToHaveCountAsync(0);

        for (var i = 0; i < 5; i++)
        {
            var item = Items("rtg-readonly").Nth(i);
            await Expect(item).ToHaveAttributeAsync("aria-hidden", "true");
            await Expect(item).ToHaveAttributeAsync("tabindex", "-1");
            await Expect(item).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        }

        await ExpectFillsAsync("rtg-readonly", "100", "100", "100", "50", "0");

        // The half-filled fourth item really shows half of its glyph.
        await ExpectRenderedFillAsync("rtg-readonly", 4, 0.5);

        await Items("rtg-readonly").Nth(0).ClickAsync(new() { Force = true });
        await NextFramesAsync();
        await ExpectTextAsync("readonly-value", "0");
        await ExpectFillAsync("rtg-readonly", 4, "50");

        // Read-only items keep their full contrast, unlike disabled ones.
        Assert.AreNotEqual(
            await Fill("rtg-disabled", 1).EvaluateAsync<string>("el => getComputedStyle(el).color"),
            await Fill("rtg-readonly", 1).EvaluateAsync<string>("el => getComputedStyle(el).color"));

        await Page.ClickAsync("#btn-toggle-readonly");
        await Expect(root).ToHaveAttributeAsync("role", "radiogroup");
        await Items("rtg-readonly").Nth(0).ClickAsync();
        await ExpectTextAsync("readonly-value", "1");
    }

    // ---------------------------------------------------------------- label & description

    [TestMethod]
    public async Task Label_NamesTheGroupMarksItRequiredAndDescriptionDescribesIt()
    {
        await OpenAsync(Route);

        var root = Root("rtg-label");
        await Expect(root.Locator(".bit-rtg-lbl")).ToHaveTextAsync("How would you rate your stay?");
        await Expect(root).ToHaveAccessibleNameAsync("How would you rate your stay?");
        await Expect(root).ToHaveAccessibleDescriptionAsync("Hover a star to see what it means.");
        await Expect(root).ToHaveAttributeAsync("aria-required", "true");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtg-req"));
        await Expect(root.Locator(".bit-rtg-dsc")).ToHaveTextAsync("Hover a star to see what it means.");

        // The asterisk is drawn, with an empty alternative text so it does not join the accessible name above.
        var asterisk = await root.Locator(".bit-rtg-lbl").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content");
        StringAssert.StartsWith(asterisk, "\" *\"");

        // The label sits above the items and the description below them.
        var label = await BoxAsync(root.Locator(".bit-rtg-lbc"));
        var items = await BoxAsync(root.Locator(".bit-rtg-cnt"));
        var description = await BoxAsync(root.Locator(".bit-rtg-dsc"));
        Assert.IsTrue(label.Y + label.Height <= items.Y + 0.5);
        Assert.IsTrue(items.Y + items.Height <= description.Y + 0.5);
    }

    [TestMethod]
    public async Task ItemTitles_AreTheTooltipsAndNamesOfTheItems_AndTheRestFallBackToTheirPosition()
    {
        await OpenAsync(Route);

        string[] titles = ["Terrible", "Bad", "Normal", "Good"];
        for (var i = 0; i < titles.Length; i++)
        {
            await Expect(Items("rtg-label").Nth(i)).ToHaveAttributeAsync("title", titles[i]);
            await Expect(Items("rtg-label").Nth(i)).ToHaveAccessibleNameAsync(titles[i]);
        }

        await Expect(Items("rtg-label").Nth(4)).Not.ToHaveAttributeAsync("title", new Regex(".*"));
        await Expect(Items("rtg-label").Nth(4)).ToHaveAccessibleNameAsync("5 of 5");

        await Items("rtg-label").Nth(1).ClickAsync();
        await ExpectTextAsync("label-value", "2");
    }

    [TestMethod]
    [DataRow("rtg-lp-top", "", "above")]
    [DataRow("rtg-lp-bottom", "bit-rtg-lbm", "below")]
    [DataRow("rtg-lp-start", "bit-rtg-lst", "before")]
    [DataRow("rtg-lp-end", "bit-rtg-led", "after")]
    public async Task LabelPlacement_PutsTheLabelOnTheChosenSide(string id, string cssClass, string side)
    {
        await OpenAsync(Route);

        var root = Root(id);
        foreach (var other in new[] { "bit-rtg-lbm", "bit-rtg-lst", "bit-rtg-led" }.Where(c => c != cssClass))
        {
            await Expect(root).Not.ToHaveClassAsync(HasClass(other));
        }

        if (cssClass.Length > 0) await Expect(root).ToHaveClassAsync(HasClass(cssClass));

        var label = await BoxAsync(root.Locator(".bit-rtg-lbc"));
        var items = await BoxAsync(root.Locator(".bit-rtg-cnt"));

        switch (side)
        {
            case "above":
                Assert.IsTrue(label.Y + label.Height <= items.Y + 0.5, "The label is not above the items.");
                break;
            case "below":
                Assert.IsTrue(items.Y + items.Height <= label.Y + 0.5, "The label is not below the items.");
                break;
            case "before":
                Assert.IsTrue(label.X + label.Width <= items.X + 0.5, "The label is not before the items.");
                Assert.IsTrue(Math.Abs((label.Y + label.Height / 2) - (items.Y + items.Height / 2)) < items.Height, "Not on one row.");
                break;
            case "after":
                Assert.IsTrue(items.X + items.Width <= label.X + 0.5, "The label is not after the items.");
                Assert.IsTrue(Math.Abs((label.Y + label.Height / 2) - (items.Y + items.Height / 2)) < items.Height, "Not on one row.");
                break;
        }
    }

    [TestMethod]
    public async Task LabelAndDescriptionTemplates_RenderCustomMarkupThatStillNamesAndDescribes()
    {
        await OpenAsync(Route);

        var root = Root("rtg-templates");
        await Expect(root.Locator(".bit-rtg-lbc")).ToHaveClassAsync(HasClass("bit-rtg-ltp"));
        await Expect(Page.Locator("#tpl-label")).ToHaveTextAsync("Bad");
        await Expect(root).ToHaveAccessibleNameAsync("Bad");
        await Expect(root.Locator(".bit-rtg-dsc #tpl-description")).ToHaveTextAsync("1,034");
        await Expect(root).ToHaveAccessibleDescriptionAsync("Averaged over 1,034 stays.");

        await Items("rtg-templates").Nth(3).ClickAsync();
        await Expect(Page.Locator("#tpl-label")).ToHaveTextAsync("Good");
    }

    // ---------------------------------------------------------------- max & vertical

    [TestMethod]
    public async Task Max_RendersThatManyItemsAndNeverFewerThanOne()
    {
        await OpenAsync(Route);

        await Expect(Items("rtg-max-1")).ToHaveCountAsync(1);
        await Expect(Items("rtg-max-0")).ToHaveCountAsync(1);
        await Expect(Items("rtg-max-3")).ToHaveCountAsync(3);
        await Expect(Items("rtg-max-3").Nth(1)).ToHaveAttributeAsync("aria-setsize", "3");
        await Expect(Items("rtg-max-100")).ToHaveCountAsync(100);
        await ExpectFillAsync("rtg-max-100", 37, "100");
        await ExpectFillAsync("rtg-max-100", 38, "0");

        // A hundred items wrap inside their 200px box rather than overflowing it.
        var box = await BoxAsync(Page.Locator("#max-100-box"));
        var last = await BoxAsync(Items("rtg-max-100").Nth(99));
        var first = await BoxAsync(Items("rtg-max-100").Nth(0));
        Assert.IsTrue(last.X + last.Width <= box.X + box.Width + 1);
        Assert.IsTrue(last.Y > first.Y);
        await Expect(Root("rtg-max-100").Locator("input.bit-input-hidden")).ToHaveAttributeAsync("max", "100");
    }

    [TestMethod]
    public async Task Max_LoweredAtRuntime_PullsTheBoundValueDownToIt()
    {
        await OpenAsync(Route);

        await Expect(Items("rtg-max-runtime")).ToHaveCountAsync(6);
        await ExpectTextAsync("max-runtime-value", "5");

        await Page.ClickAsync("#btn-max-3");

        await Expect(Items("rtg-max-runtime")).ToHaveCountAsync(3);
        await ExpectTextAsync("max-runtime-value", "3");
        await ExpectFillsAsync("rtg-max-runtime", "100", "100", "100");

        // The focus still follows the value along the items that outlived the change.
        await Items("rtg-max-runtime").Nth(2).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectTextAsync("max-runtime-value", "2");
        await Expect(Items("rtg-max-runtime").Nth(1)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Vertical_StacksTheItemsBottomUpAndArrowUpRaisesTheValue()
    {
        await OpenAsync(Route);

        var root = Root("rtg-vertical");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtg-vrt"));
        await Expect(root).ToHaveAttributeAsync("aria-orientation", "vertical");

        var first = await BoxAsync(Items("rtg-vertical").Nth(0));
        var second = await BoxAsync(Items("rtg-vertical").Nth(1));
        Assert.IsTrue(first.Y > second.Y, "The first item is not below the second.");
        Assert.AreEqual(first.X, second.X, 1);

        await Items("rtg-vertical").Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectTextAsync("vertical-value", "3");
        await Expect(Items("rtg-vertical").Nth(2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectTextAsync("vertical-value", "1");
        await Expect(Fill("rtg-vertical", 1)).ToHaveAttributeAsync("style", new Regex("^height:100%;"));
    }

    [TestMethod]
    public async Task Vertical_AFractionalValueFillsFromTheBottomAndTheLowerHalfCommitsIt()
    {
        await OpenAsync(Route);

        await Expect(Fill("rtg-vertical-half", 3)).ToHaveAttributeAsync("style", new Regex("^height:50%;"));
        await ExpectRenderedFillAsync("rtg-vertical-half", 3, 0.5, vertical: true);

        var fill = await BoxAsync(Fill("rtg-vertical-half", 3));
        var glyph = await BoxAsync(Empty("rtg-vertical-half", 3));
        Assert.AreEqual(glyph.Y + glyph.Height, fill.Y + fill.Height, 1.5, "The partial fill does not sit on the bottom edge.");
        Assert.AreEqual(glyph.Height / 2, fill.Height, 2);

        await ClickItemAtAsync("rtg-vertical-half", 4, 0.2, vertical: true);
        await ExpectTextAsync("vertical-half-value", "3.5");
        await ClickItemAtAsync("rtg-vertical-half", 4, 0.8, vertical: true);
        await ExpectTextAsync("vertical-half-value", "4");
    }

    // ---------------------------------------------------------------- precision

    [TestMethod]
    [DataRow("rtg-half", "half-value", 2, "1.5", "2")]
    [DataRow("rtg-quarter", "quarter-value", 4, "3.25", "4")]
    [DataRow("rtg-tenth", "tenth-value", 10, "2.1", "3")]
    [DataRow("rtg-third", "third-value", 3, "0.33333", "1")]
    public async Task Precision_SplitsEachItemIntoStepsAndTheSliceClickedIsCommitted(string id, string valueId, int steps, string firstSliceValue, string lastSliceValue)
    {
        await OpenAsync(Route);

        await Expect(Root(id).Locator(".bit-rtg-seg")).ToHaveCountAsync(steps * 5);
        await Expect(Root(id).Locator("input.bit-input-hidden")).ToHaveAttributeAsync("step", "any");

        var index = id == "rtg-tenth" ? 3 : id == "rtg-third" ? 1 : id == "rtg-half" ? 2 : 4;

        // The leading edge of the item is its first slice, the trailing edge its last.
        await ClickItemAtAsync(id, index, 0.5 / steps);
        await ExpectTextAsync(valueId, firstSliceValue);

        await ClickItemAtAsync(id, index, 1 - 0.5 / steps);
        await ExpectTextAsync(valueId, lastSliceValue);
    }

    [TestMethod]
    public async Task Precision_AFractionalValueFillsItsItemByTheFraction()
    {
        await OpenAsync(Route);

        await ClickItemAtAsync("rtg-half", 3, 0.25);
        await ExpectTextAsync("half-value", "2.5");
        await ExpectFillsAsync("rtg-half", "100", "100", "50", "0", "0");

        // A fractional value checks no radio: it is announced through the live region instead.
        await Expect(Root("rtg-half").Locator(".bit-rtg-btn[aria-checked='true']")).ToHaveCountAsync(0);
        await Expect(Root("rtg-half").Locator("[aria-live='polite']")).ToHaveTextAsync("2.5 of 5");
        await Expect(Items("rtg-half").Nth(2)).ToHaveAttributeAsync("tabindex", "0");
    }

    [TestMethod]
    public async Task Precision_OfOne_RendersNoSlicesAndAWholeStep()
    {
        await OpenAsync(Route);

        await Expect(Root("rtg-basic").Locator(".bit-rtg-seg")).ToHaveCountAsync(0);
        await Expect(Root("rtg-basic").Locator("input.bit-input-hidden")).ToHaveAttributeAsync("step", "1");
        await Expect(Root("rtg-basic").Locator("input.bit-input-hidden")).ToHaveAttributeAsync("min", "1");
        await Expect(Root("rtg-half").Locator("input.bit-input-hidden")).ToHaveAttributeAsync("min", "0.5");
    }

    // ---------------------------------------------------------------- clearing

    [TestMethod]
    public async Task Clearing_ByDefaultZeroIsPulledUpToTheSmallestStep()
    {
        await OpenAsync(Route);

        await ExpectTextAsync("no-zero-value", "1");
        await ExpectTextAsync("no-zero-half-value", "0.5");
        await ExpectFillsAsync("rtg-no-zero-half", "50", "0", "0", "0", "0");

        await Items("rtg-no-zero").Nth(2).ClickAsync();
        await ExpectTextAsync("no-zero-value", "3");
        await Items("rtg-no-zero").Nth(2).FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await ExpectTextAsync("no-zero-value", "1");
        await Page.Keyboard.PressAsync("0");
        await ExpectTextAsync("no-zero-value", "1");
        await Page.Keyboard.PressAsync("Delete");
        await ExpectTextAsync("no-zero-value", "1");
    }

    [TestMethod]
    public async Task AllowZeroStars_StartsEmptyAndOnlyHomeOrZeroReachesZero()
    {
        await OpenAsync(Route);

        await ExpectTextAsync("allow-zero-value", "0");
        await ExpectFillsAsync("rtg-allow-zero", "0", "0", "0", "0", "0");
        await Expect(Root("rtg-allow-zero").Locator(".bit-rtg-btn[aria-checked='true']")).ToHaveCountAsync(0);
        await Expect(Items("rtg-allow-zero").Nth(0)).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Root("rtg-allow-zero").Locator("[aria-live='polite']")).ToHaveTextAsync("0 of 5");

        await Items("rtg-allow-zero").Nth(1).ClickAsync();
        await ExpectTextAsync("allow-zero-value", "2");

        // Clicking the committed item does not clear without AllowClear, and neither does Delete.
        await Items("rtg-allow-zero").Nth(1).ClickAsync();
        await ExpectTextAsync("allow-zero-value", "2");
        await Items("rtg-allow-zero").Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("Delete");
        await ExpectTextAsync("allow-zero-value", "2");

        await Page.Keyboard.PressAsync("Home");
        await ExpectTextAsync("allow-zero-value", "0");
        await Page.Keyboard.PressAsync("3");
        await ExpectTextAsync("allow-zero-value", "3");
        await Page.Keyboard.PressAsync("0");
        await ExpectTextAsync("allow-zero-value", "0");
    }

    [TestMethod]
    public async Task AllowClear_HoveringTheCommittedItemPreviewsZeroAndClickingItClears()
    {
        await OpenAsync(Route);

        await ExpectTextAsync("allow-clear-value", "3");
        await Items("rtg-allow-clear").Nth(2).HoverAsync();
        await ExpectTextAsync("allow-clear-hover", "0");
        await ExpectFillsAsync("rtg-allow-clear", "0", "0", "0", "0", "0");

        await Items("rtg-allow-clear").Nth(2).ClickAsync();
        await ExpectTextAsync("allow-clear-value", "0");

        await Items("rtg-allow-clear").Nth(1).ClickAsync();
        await ExpectTextAsync("allow-clear-value", "2");
    }

    [TestMethod]
    [DataRow("Delete")]
    [DataRow("Backspace")]
    public async Task AllowClear_DeleteAndBackspaceClear(string key)
    {
        await OpenAsync(Route);

        await Items("rtg-allow-clear").Nth(2).FocusAsync();
        await Page.Keyboard.PressAsync(key);
        await ExpectTextAsync("allow-clear-value", "0");
        await ExpectFillsAsync("rtg-allow-clear", "0", "0", "0", "0", "0");

        // Escape is never a clearing key.
        await Page.Keyboard.PressAsync("4");
        await ExpectTextAsync("allow-clear-value", "4");
        await Page.Keyboard.PressAsync("Escape");
        await NextFramesAsync();
        await ExpectTextAsync("allow-clear-value", "4");
    }

    // ---------------------------------------------------------------- highlight

    [TestMethod]
    public async Task HighlightSelectedOnly_FillsTheSelectedItemAloneAndKeepsAFractionReadable()
    {
        await OpenAsync(Route);

        await ExpectFillsAsync("rtg-cumulative", "100", "100", "100", "0", "0");
        await ExpectFillsAsync("rtg-highlight", "0", "0", "100", "0", "0");

        await Items("rtg-highlight").Nth(4).ClickAsync();
        await ExpectTextAsync("highlight-value", "5");
        await ExpectFillsAsync("rtg-highlight", "0", "0", "0", "0", "100");
        await ExpectFillsAsync("rtg-cumulative", "100", "100", "100", "100", "100");

        await Page.ClickAsync("#btn-highlight-fraction");
        await ExpectFillsAsync("rtg-highlight", "0", "0", "0", "50", "0");
        await ExpectFillsAsync("rtg-cumulative", "100", "100", "100", "50", "0");
    }

    // ---------------------------------------------------------------- hover preview

    [TestMethod]
    public async Task HoverPreview_FollowsThePointerReportsItAndEndsOnLeave()
    {
        await OpenAsync(Route);

        await ExpectFillsAsync("rtg-hover", "100", "100", "0", "0", "0");

        await Items("rtg-hover").Nth(3).HoverAsync();
        await ExpectTextAsync("hover-preview", "4");
        await ExpectFillsAsync("rtg-hover", "100", "100", "100", "100", "0");
        await Expect(Items("rtg-hover").Nth(3)).ToHaveAttributeAsync("data-is-current", "true");

        // The preview paints in the hover shade, not the committed color.
        var restColor = await Fill("rtg-default", 1).EvaluateAsync<string>("el => getComputedStyle(el).color");
        await Expect(Fill("rtg-hover", 1)).Not.ToHaveCSSAsync("color", restColor);

        await Items("rtg-hover").Nth(0).HoverAsync();
        await ExpectTextAsync("hover-preview", "1");
        await ExpectFillsAsync("rtg-hover", "100", "0", "0", "0", "0");

        await Page.HoverAsync("#hover-away");
        await ExpectTextAsync("hover-preview", "null");
        await ExpectFillsAsync("rtg-hover", "100", "100", "0", "0", "0");
        await ExpectTextAsync("hover-events", "3");

        // The bound value never moved while previewing.
        await Expect(Root("rtg-hover").Locator("input.bit-input-hidden")).ToHaveValueAsync("2");
    }

    [TestMethod]
    public async Task NoHoverPreview_KeepsTheItemsStillButStillReportsTheHoveredValue()
    {
        await OpenAsync(Route);

        await Expect(Root("rtg-no-preview")).ToHaveClassAsync(HasClass("bit-rtg-nhp"));

        await Items("rtg-no-preview").Nth(4).HoverAsync();
        await ExpectTextAsync("no-preview-hover", "5");
        await ExpectFillsAsync("rtg-no-preview", "100", "100", "0", "0", "0");

        await Page.HoverAsync("#hover-away");
        await ExpectTextAsync("no-preview-hover", "null");

        await Items("rtg-no-preview").Nth(4).ClickAsync();
        await ExpectFillsAsync("rtg-no-preview", "100", "100", "100", "100", "100");
    }

    // ---------------------------------------------------------------- icons & template

    [TestMethod]
    public async Task Icons_IconNamesReplaceTheStars()
    {
        await OpenAsync(Route);

        await Expect(Empty("rtg-heart", 1)).ToHaveClassAsync(HasClass("bit-icon--HeartFill"));
        await Expect(Fill("rtg-heart", 1)).ToHaveClassAsync(HasClass("bit-icon--HeartFill"));
        await Expect(Empty("rtg-heart", 5)).ToHaveClassAsync(HasClass("bit-icon--Heart"));
        await Expect(Fill("rtg-heart", 5)).ToHaveClassAsync(HasClass("bit-icon--Heart"));

        // The default pair is the star.
        await Expect(Empty("rtg-default", 1)).ToHaveClassAsync(HasClass("bit-icon--FavoriteStarFill"));
        await Expect(Empty("rtg-default", 5)).ToHaveClassAsync(HasClass("bit-icon--FavoriteStar"));

        // The glyph is a real icon-font glyph with a size.
        var glyph = await BoxAsync(Empty("rtg-heart", 1));
        Assert.IsTrue(glyph.Width > 4 && glyph.Height > 4);
    }

    [TestMethod]
    public async Task Icons_PerItemCallbacksChooseEachGlyphAndNullFallsBack()
    {
        await OpenAsync(Route);

        await Expect(Fill("rtg-per-item", 1)).ToHaveClassAsync(HasClass("bit-icon--DislikeSolid"));
        await Expect(Fill("rtg-per-item", 3)).ToHaveClassAsync(HasClass("bit-icon--LikeSolid"));
        await Expect(Empty("rtg-per-item", 4)).ToHaveClassAsync(HasClass("bit-icon--Like"));
        await Expect(Empty("rtg-per-item", 5)).ToHaveClassAsync(HasClass("bit-icon--FavoriteStar"));

        await Items("rtg-per-item").Nth(4).ClickAsync();
        await Expect(Empty("rtg-per-item", 5)).ToHaveClassAsync(HasClass("bit-icon--LikeSolid"));
    }

    [TestMethod]
    public async Task ExternalIcons_IconInfoWinsOverTheNameAndBuildsTheLibraryClasses()
    {
        await OpenAsync(Route);

        await Expect(Empty("rtg-external", 1)).ToHaveClassAsync(HasClass("ext-star-fill"));
        await Expect(Empty("rtg-external", 1)).Not.ToHaveClassAsync(HasClass("bit-icon--HeartFill"));
        await Expect(Fill("rtg-external", 4)).ToHaveClassAsync(HasClass("ext-star-fill"));
        await Expect(Empty("rtg-external", 4)).ToHaveClassAsync(HasClass("fa-regular"));
        await Expect(Empty("rtg-external", 4)).ToHaveClassAsync(HasClass("fa-star"));
        await ExpectFillAsync("rtg-external", 4, "50");

        await Expect(Empty("rtg-bi", 1)).ToHaveClassAsync(new Regex(@"(^|\s)bi\s+bi-heart-fill(\s|$)"));
        await Expect(Empty("rtg-bi", 5)).ToHaveClassAsync(HasClass("plain-string-icon"));
    }

    [TestMethod]
    public async Task ItemTemplate_DrawsEachItemFromItsContextAndKeepsTheItemBehaviour()
    {
        await OpenAsync(Route);

        var templates = Page.Locator("#rtg-template .tpl-item");
        await Expect(templates).ToHaveCountAsync(10);
        await Expect(Root("rtg-template").Locator(".bit-rtg-iem")).ToHaveCountAsync(0);

        var fourth = templates.Nth(3);
        await Expect(fourth).ToHaveTextAsync("4");
        await Expect(fourth).ToHaveAttributeAsync("data-index", "4");
        await Expect(fourth).ToHaveAttributeAsync("data-max", "10");
        await Expect(fourth).ToHaveAttributeAsync("data-pct", "100");
        await Expect(fourth).ToHaveAttributeAsync("data-full", "yes");
        await Expect(fourth).ToHaveAttributeAsync("data-selected", "yes");
        await Expect(fourth).ToHaveAttributeAsync("data-current", "yes");
        await Expect(fourth).ToHaveAttributeAsync("data-value", "4");
        await Expect(templates.Nth(4)).ToHaveAttributeAsync("data-full", "no");
        await Expect(templates.Nth(4)).ToHaveAttributeAsync("data-current", "no");

        // The template stays hidden from assistive technologies: the item keeps its own name.
        await Expect(Items("rtg-template").Nth(3)).ToHaveAccessibleNameAsync("4 of 10");

        await Items("rtg-template").Nth(6).HoverAsync();
        await Expect(templates.Nth(6)).ToHaveAttributeAsync("data-display", "7");
        await Expect(templates.Nth(6)).ToHaveAttributeAsync("data-current", "yes");
        await Expect(templates.Nth(6)).ToHaveAttributeAsync("data-value", "4");

        await Items("rtg-template").Nth(6).ClickAsync();
        await ExpectTextAsync("item-template-value", "7");
        await Expect(templates.Nth(6)).ToHaveAttributeAsync("data-value", "7");
    }

    // ---------------------------------------------------------------- binding

    [TestMethod]
    public async Task OneWayBinding_TheUserCannotChangeItButThePageCan()
    {
        await OpenAsync(Route);

        await ExpectFillsAsync("rtg-one-way", "0", "0", "0", "0", "0");
        await Items("rtg-one-way").Nth(3).ClickAsync();
        await NextFramesAsync();
        await ExpectFillsAsync("rtg-one-way", "0", "0", "0", "0", "0");
        await ExpectTextAsync("one-way-value", "0");

        await Page.ClickAsync("#btn-one-way-5");
        await ExpectFillsAsync("rtg-one-way", "100", "100", "100", "100", "100");
    }

    [TestMethod]
    public async Task TwoWayBinding_FlowsBothWaysAndAnOutOfRangeValueIsClamped()
    {
        await OpenAsync(Route);

        await Items("rtg-two-way").Nth(3).ClickAsync();
        await ExpectTextAsync("two-way-value", "4");

        await Page.ClickAsync("#btn-two-way-half");
        await ExpectTextAsync("two-way-value", "2.5");
        await ExpectFillsAsync("rtg-two-way", "100", "100", "50", "0", "0");
        await Expect(Root("rtg-two-way").Locator("[aria-live='polite']")).ToHaveTextAsync("2.5 of 5");

        await Page.ClickAsync("#btn-two-way-over");
        await ExpectTextAsync("two-way-value", "5");
        await ExpectFillsAsync("rtg-two-way", "100", "100", "100", "100", "100");
    }

    // ---------------------------------------------------------------- events

    [TestMethod]
    public async Task OnChange_FiresOncePerRealChangeWithTheNewValue()
    {
        await OpenAsync(Route);

        await ExpectTextAsync("on-change-count", "0");
        await Items("rtg-on-change").Nth(3).ClickAsync();
        await ExpectTextAsync("on-change-count", "1");
        await ExpectTextAsync("on-change-value", "4");

        await Items("rtg-on-change").Nth(3).ClickAsync();
        await Items("rtg-on-change").Nth(3).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("on-change-count", "2");
        await ExpectTextAsync("on-change-value", "5");

        // At the end of the scale another ArrowRight changes nothing and fires nothing.
        await Page.Keyboard.PressAsync("ArrowRight");
        await NextFramesAsync();
        await ExpectTextAsync("on-change-count", "2");
    }

    [TestMethod]
    public async Task OnChanging_CarriesBothValuesAndCancelKeepsTheCurrentOne()
    {
        await OpenAsync(Route);

        await Items("rtg-on-changing").Nth(4).ClickAsync();
        await ExpectTextAsync("on-changing-args", "3->5");
        await ExpectTextAsync("on-changing-value", "5");
        await ExpectTextAsync("on-changing-rejected", "False");

        await Items("rtg-on-changing").Nth(1).ClickAsync();
        await ExpectTextAsync("on-changing-args", "5->2");
        await ExpectTextAsync("on-changing-rejected", "True");
        await ExpectTextAsync("on-changing-value", "5");
        await ExpectFillsAsync("rtg-on-changing", "100", "100", "100", "100", "100");

        // The refused preview does not linger either.
        await Page.HoverAsync("#hover-away");
        await ExpectFillAsync("rtg-on-changing", 5, "100");
    }

    [TestMethod]
    public async Task OnFocusInAndOut_ReportTheFocusCrossingTheRatingNotMovingInsideIt()
    {
        await OpenAsync(Route);

        await Page.FocusAsync("#before-focus");
        await Page.Keyboard.PressAsync("Tab");

        await Expect(Items("rtg-focus").Nth(2)).ToBeFocusedAsync();
        await ExpectTextAsync("focus-in", "1");
        await ExpectTextAsync("focus-type", "focusin");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Items("rtg-focus").Nth(3)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Items("rtg-focus").Nth(2)).ToBeFocusedAsync();
        await NextFramesAsync();
        await ExpectTextAsync("focus-in", "1");
        await ExpectTextAsync("focus-out", "0");

        // The whole rating is a single tab stop.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-focus")).ToBeFocusedAsync();
        await ExpectTextAsync("focus-out", "1");
        await ExpectTextAsync("focus-type", "focusout");

        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Items("rtg-focus").Nth(2)).ToBeFocusedAsync();
        await ExpectTextAsync("focus-in", "2");
    }

    // ---------------------------------------------------------------- validation & form

    [TestMethod]
    public async Task Validation_AnUnratedRequiredRatingFailsTheFormUntilAStarIsPicked()
    {
        await OpenAsync(Route);

        var root = Root("rtg-form");
        await Expect(root).ToHaveAttributeAsync("aria-required", "true");
        await Expect(root).ToHaveAccessibleNameAsync("Your rate");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Page.ClickAsync("#rtg-form-submit");

        await ExpectTextAsync("form-result", "invalid");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(root).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#rtg-form-msg")).ToHaveTextAsync("Pick a rating.");
        await Expect(root).ToHaveAccessibleDescriptionAsync("Pick a rating.");

        // The invalid color paints the items, here through --bit-Rating-invalid-color.
        await Expect(Empty("rtg-form", 1)).ToHaveCSSAsync("color", "rgb(4, 5, 6)");

        await Items("rtg-form").Nth(3).ClickAsync();
        await ExpectTextAsync("form-value", "4");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(root).Not.ToHaveAttributeAsync("aria-invalid", new Regex(".*"));
        await Expect(Page.Locator("#rtg-form-msg")).ToHaveTextAsync("");

        await Page.ClickAsync("#rtg-form-submit");
        await ExpectTextAsync("form-result", "valid");
    }

    [TestMethod]
    public async Task HiddenInput_CarriesTheValueNameAndConstraintsForAPlainFormPost()
    {
        await OpenAsync(Route);

        var input = Root("rtg-form").Locator("input.bit-input-hidden");
        await Expect(input).ToHaveAttributeAsync("type", "number");
        await Expect(input).ToHaveAttributeAsync("name", "rate");
        await Expect(input).ToHaveAttributeAsync("required", "");
        await Expect(input).ToHaveAttributeAsync("min", "0");
        await Expect(input).ToHaveAttributeAsync("max", "5");
        await Expect(input).ToHaveValueAsync("0");
        await Expect(input).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(input).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(input).ToHaveAttributeAsync("data-hidden", "carrier");

        await Items("rtg-form").Nth(2).ClickAsync();
        await Expect(input).ToHaveValueAsync("3");
    }

    // ---------------------------------------------------------------- accessibility & keyboard

    [TestMethod]
    public async Task Accessibility_TheGroupAndItsRadiosAreNamedFromTheFormats()
    {
        await OpenAsync(Route);

        var root = Root("rtg-a11y");
        await Expect(root).ToHaveAttributeAsync("role", "radiogroup");
        await Expect(root).ToHaveAttributeAsync("aria-label", "Rate this product");
        await Expect(root).ToHaveAccessibleNameAsync("Rate this product");

        for (var i = 0; i < 5; i++)
        {
            var item = Items("rtg-a11y").Nth(i);
            await Expect(item).ToHaveAttributeAsync("role", "radio");
            await Expect(item).ToHaveAttributeAsync("aria-setsize", "5");
            await Expect(item).ToHaveAttributeAsync("aria-posinset", (i + 1).ToString());
            await Expect(item).ToHaveAccessibleNameAsync($"Select {i + 1} of 5 stars");
        }

        await Expect(Page.GetByRole(AriaRole.Radiogroup, new() { Name = "Rate this product" })).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Radio, new() { Name = "Select 2 of 5 stars", Exact = true })).ToBeCheckedAsync();

        var live = root.Locator("[aria-live='polite']");
        await Expect(live).ToHaveAttributeAsync("aria-atomic", "true");
        await Expect(live).ToHaveTextAsync("");
        await Items("rtg-a11y").Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("a11y-value", "2.5");
        await Expect(live).ToHaveTextAsync("2.5 out of 5 stars");
    }

    [TestMethod]
    public async Task Accessibility_GetAriaLabelAndAriaLabelledByNameTheRating()
    {
        await OpenAsync(Route);

        await Expect(Root("rtg-get-aria")).ToHaveAttributeAsync("role", "img");
        await Expect(Root("rtg-get-aria")).ToHaveAccessibleNameAsync("Rated 4.2 out of 5 by 1,034 people");
        await ExpectFillAsync("rtg-get-aria", 5, "20");

        await Expect(Root("rtg-get-aria-interactive")).ToHaveAttributeAsync("aria-label", "Group 2/5");
        await Items("rtg-get-aria-interactive").Nth(3).ClickAsync();
        await Expect(Root("rtg-get-aria-interactive")).ToHaveAttributeAsync("aria-label", "Group 4/5");

        // AriaLabelledBy wins over the visible label.
        var labelledBy = Root("rtg-labelledby");
        await Expect(labelledBy).ToHaveAttributeAsync("aria-labelledby", "rtg-external-label");
        await Expect(labelledBy).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
        await Expect(labelledBy).ToHaveAccessibleNameAsync("Overall satisfaction");

        // A read-only rating named by reference adds its value to the name.
        await Expect(Root("rtg-labelledby-readonly")).ToHaveAccessibleNameAsync("Overall satisfaction 2.5 of 5");
    }

    [TestMethod]
    public async Task Keyboard_ArrowsStepByThePrecisionAndTheFocusFollowsTheValue()
    {
        await OpenAsync(Route);

        // TabIndex is the tab stop's tabindex, on the item the value is on.
        await Expect(Items("rtg-keys").Nth(1)).ToHaveAttributeAsync("tabindex", "3");
        await Expect(Items("rtg-keys").Nth(0)).ToHaveAttributeAsync("tabindex", "-1");

        await Items("rtg-keys").Nth(1).FocusAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("keys-value", "2.5");
        await Expect(Items("rtg-keys").Nth(2)).ToBeFocusedAsync();
        await Expect(Items("rtg-keys").Nth(2)).ToHaveAttributeAsync("tabindex", "3");

        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectTextAsync("keys-value", "3");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectTextAsync("keys-value", "2.5");
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectTextAsync("keys-value", "2");
        await Expect(Items("rtg-keys").Nth(1)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_ShiftPageHomeEndAndDigitsJumpAcrossTheScale()
    {
        await OpenAsync(Route);

        await Items("rtg-keys").Nth(1).FocusAsync();

        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await ExpectTextAsync("keys-value", "3");
        await Page.Keyboard.PressAsync("PageUp");
        await ExpectTextAsync("keys-value", "4");
        await Page.Keyboard.PressAsync("PageDown");
        await ExpectTextAsync("keys-value", "3");
        await Page.Keyboard.PressAsync("Shift+ArrowLeft");
        await ExpectTextAsync("keys-value", "2");
        await Page.Keyboard.PressAsync("End");
        await ExpectTextAsync("keys-value", "5");
        await Expect(Items("rtg-keys").Nth(4)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await ExpectTextAsync("keys-value", "0.5");
        await Expect(Items("rtg-keys").Nth(0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("4");
        await ExpectTextAsync("keys-value", "4");
        await Page.Keyboard.PressAsync("9");
        await ExpectTextAsync("keys-value", "5");
        await Page.Keyboard.PressAsync("0");
        await ExpectTextAsync("keys-value", "0.5");
    }

    [TestMethod]
    public async Task Keyboard_ShiftMovesAWholeItemOnAFineScale()
    {
        await OpenAsync(Route);

        await Items("rtg-keys-tenth").Nth(0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("keys-tenth-value", "1.1");
        // Shift moves along the grid of whole items: from 1.1 to the next whole item, then by a whole item.
        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await ExpectTextAsync("keys-tenth-value", "2");
        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await ExpectTextAsync("keys-tenth-value", "3");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectTextAsync("keys-tenth-value", "2.9");
        await ExpectFillsAsync("rtg-keys-tenth", "100", "100", "90", "0", "0");
    }

    [TestMethod]
    [DataRow(" ")]
    [DataRow("Enter")]
    public async Task Keyboard_SpaceAndEnterCommitTheFocusedItemWhole(string key)
    {
        await OpenAsync(Route);

        await Items("rtg-keys").Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("keys-value", "2.5");

        await Page.Keyboard.PressAsync(key == " " ? "Space" : key);
        await ExpectTextAsync("keys-value", "3");
    }

    [TestMethod]
    public async Task Keyboard_AHeldModifierHandsTheKeyBackToThePage()
    {
        await OpenAsync(Route);

        await Items("rtg-keys").Nth(1).FocusAsync();

        await Page.Keyboard.PressAsync("Control+ArrowRight");
        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Page.Keyboard.PressAsync("Control+End");
        await NextFramesAsync();
        await ExpectTextAsync("keys-value", "2");

        // The navigation keys do not scroll the page while the rating handles them.
        var prevented = await Items("rtg-keys").Nth(1).EvaluateAsync<bool>(@"el => {
            const e = new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true });
            el.dispatchEvent(e);
            return e.defaultPrevented;
        }");
        Assert.IsTrue(prevented);

        var notPrevented = await Items("rtg-keys").Nth(1).EvaluateAsync<bool>(@"el => {
            const e = new KeyboardEvent('keydown', { key: 'ArrowDown', ctrlKey: true, bubbles: true, cancelable: true });
            el.dispatchEvent(e);
            return e.defaultPrevented;
        }");
        Assert.IsFalse(notPrevented);
    }

    [TestMethod]
    public async Task FocusAsync_MovesTheFocusToTheItemHoldingTheTabStop()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-focus-api");

        await Expect(Items("rtg-focus-api").Nth(3)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AutoFocus_TheTabStopItemHasTheFocusWhenThePageRenders()
    {
        await OpenAsync("/e2e/rating/autofocus");

        await Expect(Items("rtg-autofocus").Nth(2)).ToHaveAttributeAsync("autofocus", "");
        await Expect(Root("rtg-autofocus").Locator("[autofocus]")).ToHaveCountAsync(1);
        await Expect(Root("rtg-no-autofocus").Locator("[autofocus]")).ToHaveCountAsync(0);
        await Expect(Items("rtg-autofocus").Nth(2)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task EscapeInsideAModal_ClosesTheModalAndLeavesTheRatingAlone()
    {
        await OpenAsync(Route);

        await Page.ClickAsync("#btn-open-modal");
        await Expect(Root("rtg-modal")).ToBeVisibleAsync();

        // The modal moves the focus into itself once it has opened; the test waits for that before it types.
        await Page.WaitForFunctionAsync("() => document.activeElement && document.activeElement.closest('.bit-mdl')");

        await Items("rtg-modal").Nth(3).ClickAsync();
        await ExpectTextAsync("modal-value", "4");
        await Items("rtg-modal").Nth(3).FocusAsync();
        await Expect(Items("rtg-modal").Nth(3)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await ExpectTextAsync("modal-open", "False");
        await Expect(Root("rtg-modal")).ToHaveCountAsync(0);
        await ExpectTextAsync("modal-value", "4");
    }

    // ---------------------------------------------------------------- visibility, cascade, score

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheBoxAndCollapsedRemovesIt()
    {
        await OpenAsync(Route);

        await Expect(Root("rtg-visible")).ToBeVisibleAsync();

        var hidden = Root("rtg-hidden");
        await Expect(hidden).ToHaveCSSAsync("visibility", "hidden");
        await Expect(hidden).Not.ToBeVisibleAsync();
        Assert.IsTrue((await BoxAsync(hidden)).Width > 0);

        var collapsed = Root("rtg-collapsed");
        await Expect(collapsed).ToHaveCSSAsync("display", "none");
        Assert.IsNull(await collapsed.BoundingBoxAsync());
    }

    [TestMethod]
    public async Task ScoreBasedStyling_TheColorFollowsTheBoundValue()
    {
        await OpenAsync(Route);

        var root = Root("rtg-score");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtg-err"));
        var low = await Fill("rtg-score", 1).EvaluateAsync<string>("el => getComputedStyle(el).color");

        await Items("rtg-score").Nth(2).ClickAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtg-wrn"));
        await Page.HoverAsync("#hover-away");
        var mid = await Fill("rtg-score", 1).EvaluateAsync<string>("el => getComputedStyle(el).color");

        await Items("rtg-score").Nth(4).ClickAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtg-suc"));
        await Page.HoverAsync("#hover-away");
        await Expect(Fill("rtg-score", 1)).Not.ToHaveCSSAsync("color", mid);
        Assert.AreNotEqual(low, mid);
    }

    [TestMethod]
    public async Task CascadingParameters_FillWhatTheRatingLeavesUnsetAndYieldToWhatItSets()
    {
        await OpenAsync(Route);

        var cascaded = Root("rtg-cascade-1");
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-rtg-rdl"));
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-rtg-sm"));
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-rtg-wrn"));
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-rtg-lst"));
        await Expect(cascaded).ToHaveAttributeAsync("role", "img");
        await Expect(Empty("rtg-cascade-1", 1)).ToHaveClassAsync(HasClass("bit-icon--HeartFill"));

        var own = Root("rtg-cascade-own");
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-rtg-rdl"));
        await Expect(own).ToHaveAttributeAsync("role", "radiogroup");
        await Expect(own).ToHaveClassAsync(HasClass("bit-rtg-suc"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-rtg-wrn"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-rtg-sm"));
        await Expect(own.Locator(".bit-rtg-seg")).ToHaveCountAsync(10);

        await ClickItemAtAsync("rtg-cascade-own", 2, 0.25);
        await ExpectTextAsync("cascade-value", "1.5");

        var outside = Root("rtg-cascade-outside");
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-rtg-rdl"));
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-rtg-sm"));
        await Expect(Empty("rtg-cascade-outside", 1)).ToHaveClassAsync(HasClass("bit-icon--FavoriteStarFill"));
    }

    // ---------------------------------------------------------------- color & size

    [TestMethod]
    public async Task Color_EveryValuePaintsTheFillInItsRole()
    {
        await OpenAsync(Route);

        string[] slugs = ["pri", "sec", "ter", "inf", "suc", "wrn", "swr", "err", "pbg", "sbg", "tbg", "pfg", "sfg", "tfg", "pbr", "sbr", "tbr"];

        var unset = await Fill("rtg-color-none", 1).EvaluateAsync<string>("el => getComputedStyle(el).color");
        await Expect(Root("rtg-color-none")).Not.ToHaveClassAsync(new Regex(@"bit-rtg-(pri|sec|ter|inf|suc|wrn|swr|err)\b"));

        var colors = new Dictionary<string, string>();
        foreach (var slug in slugs)
        {
            var id = $"rtg-color-{slug}";
            await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-rtg-{slug}"));

            var role = await Root(id).EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--bit-rtg-clr').trim()");
            Assert.IsFalse(string.IsNullOrEmpty(role), $"{slug} publishes no role color.");
            var expected = await ResolveColorAsync(role);
            var actual = await Fill(id, 1).EvaluateAsync<string>("el => getComputedStyle(el).color");

            Assert.AreEqual(expected, actual, $"The fill of {slug} is not its role color.");
            colors[slug] = actual;

            // The unfilled part stays neutral whatever the color.
            Assert.AreEqual(
                await Empty("rtg-color-none", 5).EvaluateAsync<string>("el => getComputedStyle(el).color"),
                await Empty(id, 5).EvaluateAsync<string>("el => getComputedStyle(el).color"),
                $"The unfilled part of {slug} is colored.");
        }

        Assert.AreEqual(unset, colors["pri"], "Unset is not primary.");
        Assert.IsTrue(colors.Values.Distinct().Count() >= 12, $"Too few distinct colors: {string.Join(", ", colors.Select(c => $"{c.Key}={c.Value}"))}");
        foreach (var slug in new[] { "sec", "ter", "inf", "suc", "wrn", "swr", "err", "pbg", "pfg", "pbr" })
        {
            Assert.AreNotEqual(colors["pri"], colors[slug], $"{slug} paints like primary.");
        }
    }

    [TestMethod]
    [DataRow("sm", "md")]
    [DataRow("md", "lg")]
    public async Task Size_ScalesTheGlyphsLabelAndDescription(string smaller, string larger)
    {
        await OpenAsync(Route);

        await Expect(Root($"rtg-size-{smaller}")).ToHaveClassAsync(HasClass($"bit-rtg-{smaller}"));
        await Expect(Root($"rtg-size-{larger}")).ToHaveClassAsync(HasClass($"bit-rtg-{larger}"));

        foreach (var part in new[] { ".bit-rtg-btn", ".bit-rtg-lbl", ".bit-rtg-dsc" })
        {
            var small = Px(await ComputedStyleAsync($"#rtg-size-{smaller} {part}", "font-size"));
            var large = Px(await ComputedStyleAsync($"#rtg-size-{larger} {part}", "font-size"));
            Assert.IsTrue(large > small, $"{part}: {larger} ({large}) is not larger than {smaller} ({small}).");
        }

        var smallGlyph = await BoxAsync(Empty($"rtg-size-{smaller}", 1));
        var largeGlyph = await BoxAsync(Empty($"rtg-size-{larger}", 1));
        Assert.IsTrue(largeGlyph.Height > smallGlyph.Height);
    }

    [TestMethod]
    public async Task Size_TheTargetNeverDropsBelow24Pixels()
    {
        await OpenAsync(Route);

        var item = await BoxAsync(Items("rtg-size-sm").Nth(0));
        Assert.IsTrue(item.Width >= 23.9 && item.Height >= 23.9, $"{item.Width}x{item.Height}");
    }

    // ---------------------------------------------------------------- style & class

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRoot()
    {
        await OpenAsync(Route);

        await Expect(Root("rtg-style")).ToHaveCSSAsync("margin-left", "13px");
        await Expect(Root("rtg-style")).ToHaveCSSAsync("box-shadow", new Regex("rgb\\(255, 99, 71\\)"));
        await Expect(Root("rtg-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Root("rtg-class")).ToHaveClassAsync(HasClass("bit-rtg"));
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart()
    {
        await OpenAsync(Route);

        var root = Root("rtg-styles");
        await Expect(root).ToHaveCSSAsync("padding-top", "5px");
        await Expect(root.Locator(".bit-rtg-lbc")).ToHaveCSSAsync("padding-top", "2px");
        await Expect(root.Locator(".bit-rtg-lbl")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(root.Locator(".bit-rtg-dsc")).ToHaveCSSAsync("font-style", "italic");
        await Expect(root.Locator(".bit-rtg-cnt")).ToHaveCSSAsync("column-gap", "7px");
        await Expect(Items("rtg-styles").Nth(0)).ToHaveCSSAsync("padding-top", "6px");
        await Expect(Items("rtg-styles").Nth(0).Locator(".bit-rtg-ict")).ToHaveCSSAsync("opacity", "0.9");
        await Expect(Fill("rtg-styles", 1)).ToHaveCSSAsync("color", "rgb(138, 43, 226)");
        await Expect(Empty("rtg-styles", 5)).ToHaveCSSAsync("color", "rgb(221, 160, 221)");
        await ExpectFillAsync("rtg-styles", 4, "50");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart()
    {
        await OpenAsync(Route);

        var root = Root("rtg-classes");
        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        await Expect(root.Locator(".bit-rtg-lbc")).ToHaveClassAsync(HasClass("c-lbc"));
        await Expect(root.Locator(".bit-rtg-lbl")).ToHaveClassAsync(HasClass("c-lbl"));
        await Expect(root.Locator(".bit-rtg-dsc")).ToHaveClassAsync(HasClass("c-dsc"));
        await Expect(root.Locator(".bit-rtg-cnt")).ToHaveClassAsync(HasClass("c-cnt"));
        await Expect(root.Locator(".c-btn")).ToHaveCountAsync(5);
        await Expect(root.Locator(".bit-rtg-ict.c-ict")).ToHaveCountAsync(5);
        await Expect(root.Locator(".bit-rtg-ifl.c-sel")).ToHaveCountAsync(5);
        await Expect(root.Locator(".bit-rtg-iem.c-uns")).ToHaveCountAsync(5);
    }

    [TestMethod]
    public async Task DataIsCurrent_MarksTheItemTheShownValueLandsIn()
    {
        await OpenAsync(Route);

        var current = Root("rtg-current").Locator(".bit-rtg-btn[data-is-current='true']");
        await Expect(current).ToHaveCountAsync(1);
        await Expect(Items("rtg-current").Nth(3)).ToHaveAttributeAsync("data-is-current", "true");

        await ClickItemAtAsync("rtg-current", 2, 0.75);
        await Page.HoverAsync("#hover-away");
        await Expect(Items("rtg-current").Nth(1)).ToHaveAttributeAsync("data-is-current", "true");
        await Expect(current).ToHaveCountAsync(1);

        await Items("rtg-current").Nth(4).HoverAsync();
        await Expect(Items("rtg-current").Nth(4)).ToHaveAttributeAsync("data-is-current", "true");
    }

    [TestMethod]
    public async Task HtmlAttributes_AreSplattedAndADescribedByIsMergedWithTheDescription()
    {
        await OpenAsync(Route);

        var root = Root("rtg-attrs");
        await Expect(root).ToHaveAttributeAsync("data-test", "attr-value");
        await Expect(root).ToHaveAttributeAsync("title", "root title");
        await Expect(root).ToHaveAttributeAsync("aria-describedby", new Regex(@"^rtg-external-label BitRating-.+-description$"));
        await Expect(root).ToHaveAccessibleDescriptionAsync("Overall satisfaction Own description");
    }

    // ---------------------------------------------------------------- css variables

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheRating()
    {
        await OpenAsync(Route);

        var root = Root("rtg-vars");
        await Expect(Fill("rtg-vars", 1)).ToHaveCSSAsync("color", "rgb(218, 165, 32)");
        await Expect(Empty("rtg-vars", 5)).ToHaveCSSAsync("color", "rgb(216, 201, 163)");
        await Expect(Items("rtg-vars").Nth(0)).ToHaveCSSAsync("font-size", "32px");
        await Expect(Items("rtg-vars").Nth(0)).ToHaveCSSAsync("padding-top", "3px");
        await Expect(Items("rtg-vars").Nth(0)).ToHaveCSSAsync("min-width", "40px");
        await Expect(Items("rtg-vars").Nth(0)).ToHaveCSSAsync("border-top-left-radius", "6px");
        await Expect(root.Locator(".bit-rtg-cnt")).ToHaveCSSAsync("column-gap", "8px");
        await Expect(root).ToHaveCSSAsync("row-gap", "11px");
        await Expect(root.Locator(".bit-rtg-lbl")).ToHaveCSSAsync("color", "rgb(220, 20, 60)");
        await Expect(root.Locator(".bit-rtg-lbl")).ToHaveCSSAsync("font-size", "19px");
        await Expect(root.Locator(".bit-rtg-lbl")).ToHaveCSSAsync("font-weight", "700");
        await Expect(root.Locator(".bit-rtg-dsc")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(root.Locator(".bit-rtg-dsc")).ToHaveCSSAsync("font-size", "9px");

        var second = await BoxAsync(Items("rtg-vars").Nth(1));
        var first = await BoxAsync(Items("rtg-vars").Nth(0));
        Assert.AreEqual(8, second.X - (first.X + first.Width), 1);
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColorOrSizeWinsOverThem()
    {
        await OpenAsync(Route);

        await Expect(Fill("rtg-vars-explicit", 1)).Not.ToHaveCSSAsync("color", "rgb(218, 165, 32)");
        Assert.AreEqual(
            await Fill("rtg-color-err", 1).EvaluateAsync<string>("el => getComputedStyle(el).color"),
            await Fill("rtg-vars-explicit", 1).EvaluateAsync<string>("el => getComputedStyle(el).color"));
        await Expect(Items("rtg-vars-explicit").Nth(0)).Not.ToHaveCSSAsync("font-size", "32px");
        Assert.AreEqual(
            await ComputedStyleAsync("#rtg-size-sm .bit-rtg-btn", "font-size"),
            await ComputedStyleAsync("#rtg-vars-explicit .bit-rtg-btn", "font-size"));
    }

    [TestMethod]
    public async Task CssVariables_DisabledHoverActiveFocusAndScaleVariablesApplyInTheirState()
    {
        await OpenAsync(Route);

        await Expect(Empty("rtg-vars-disabled", 5)).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(Fill("rtg-vars-disabled", 1)).ToHaveCSSAsync("color", "rgb(1, 2, 3)");

        await Items("rtg-vars-hover").Nth(2).HoverAsync();
        await Expect(Fill("rtg-vars-hover", 1)).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(Items("rtg-vars-hover").Nth(2).Locator(".bit-rtg-ict")).ToHaveCSSAsync("transform", "matrix(1.5, 0, 0, 1.5, 0, 0)");

        await Page.Mouse.DownAsync();
        await Expect(Fill("rtg-vars-hover", 1)).ToHaveCSSAsync("color", "rgb(40, 50, 60)");
        await Expect(Items("rtg-vars-hover").Nth(2).Locator(".bit-rtg-ict")).ToHaveCSSAsync("transform", "matrix(0.5, 0, 0, 0.5, 0, 0)");
        await Page.Mouse.UpAsync();

        await Page.HoverAsync("#hover-away");
        await Page.FocusAsync("#before-vars-hover");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Items("rtg-vars-hover").Nth(2)).ToBeFocusedAsync();
        await Expect(Items("rtg-vars-hover").Nth(2)).ToHaveCSSAsync("box-shadow", new Regex(@"rgb\(10, 20, 30\)"));
    }

    // ---------------------------------------------------------------- rtl

    [TestMethod]
    public async Task Rtl_MirrorsTheItemsAndSwapsTheHorizontalArrows()
    {
        await OpenAsync(Route);

        var root = Root("rtg-rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");

        var first = await BoxAsync(Items("rtg-rtl").Nth(0));
        var second = await BoxAsync(Items("rtg-rtl").Nth(1));
        Assert.IsTrue(first.X > second.X, "The first item is not on the right.");

        await Items("rtg-rtl").Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectTextAsync("rtl-value", "3");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("rtl-value", "1");
    }

    [TestMethod]
    public async Task Rtl_AFractionGrowsFromTheRightEdgeAndTheRightHalfIsTheLeadingSlice()
    {
        await OpenAsync(Route);
        await ExpectRenderedFillAsync("rtg-rtl-half", 4, 0.5);

        var fill = await BoxAsync(Fill("rtg-rtl-half", 4));
        var glyph = await BoxAsync(Empty("rtg-rtl-half", 4));
        Assert.AreEqual(glyph.X + glyph.Width, fill.X + fill.Width, 1.5, "The partial fill is not anchored on the right.");
        Assert.AreEqual(glyph.Width / 2, fill.Width, 1.5);

        // In right-to-left the leading half of an item is its right half.
        await ClickItemAtAsync("rtg-rtl-half", 2, 0.8);
        await ExpectTextAsync("rtl-half-value", "1.5");
        await ClickItemAtAsync("rtg-rtl-half", 2, 0.2);
        await ExpectTextAsync("rtl-half-value", "2");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_LaysOutAndNavigatesRightToLeft()
    {
        await OpenAsync(Route);

        var root = Root("rtg-rtl-inherited");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(root).Not.ToHaveAttributeAsync("dir", new Regex(".*"));

        var first = await BoxAsync(Items("rtg-rtl-inherited").Nth(0));
        var second = await BoxAsync(Items("rtg-rtl-inherited").Nth(1));
        Assert.IsTrue(first.X > second.X, "The first item is not on the right.");

        await Items("rtg-rtl-inherited").Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectTextAsync("rtl-inherited-value", "3");
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectTextAsync("rtl-inherited-value", "2");
    }

    // ---------------------------------------------------------------- motion

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheTransitionsUnlessForceAnimationRestoresThem()
    {
        await OpenAsync(Route);

        var full = await ComputedStyleAsync("#rtg-motion .bit-rtg-ict", "transition-duration");
        Assert.AreEqual("0.1s", full);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Expect(Items("rtg-motion").Nth(0).Locator(".bit-rtg-ict")).Not.ToHaveCSSAsync("transition-duration", "0.1s");
        var reduced = await ComputedStyleAsync("#rtg-motion .bit-rtg-ict", "transition-duration");
        Assert.IsTrue(double.Parse(reduced.TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture) < 0.001, reduced);

        await Expect(Root("rtg-motion-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        Assert.AreEqual("0.1s", await ComputedStyleAsync("#rtg-motion-forced .bit-rtg-ict", "transition-duration"));
    }
}
