using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// End-to-end tests of BitChoiceGroup through its Items API (<c>BitChoiceGroupItem</c>) on <c>/e2e/choice-group</c>: the
/// prerendered radiogroup, selecting by pointer, the single tab stop and the arrow keys of a native radio group, NoCircle,
/// a disabled group and a disabled item, read-only, image and icon tiles (with SelectedImageSrc, ImageSize, ImageAlt)
/// and the Inline mode, Horizontal, every LabelPlacement, the label / prefix / suffix / label / item templates, one-way,
/// two-way and uncontrolled binding with a Comparer, EditForm validation, the form name, item and group descriptions,
/// Gap, Prefix / Suffix, every callback with its argument, dynamic items, FullWidth / StretchItemLabel, item titles,
/// AutoFocus, the three variants, the accessible names and FocusAsync, the BitParams cascade, every color and size
/// against the computed styles, external icons, Style / Class / Styles / Classes, the public CSS variables (with the
/// variant twins), RTL, the disabled-markup and activation contracts, runtime re-renders, unmounting and reduced motion.
/// The Custom and Option APIs are covered by <see cref="BitChoiceGroupCustomE2ETests"/> and
/// <see cref="BitChoiceGroupOptionE2ETests"/>.
/// </summary>
[TestClass]
public class BitChoiceGroupE2ETests : E2ETestBase
{
    private const string Url = "/e2e/choice-group";

    private static readonly (string Suffix, string Token)[] Colors =
    [
        ("pri", "pri"), ("sec", "sec"), ("ter", "ter"), ("inf", "inf"), ("suc", "suc"), ("wrn", "wrn"), ("swr", "swr"),
        ("err", "err"), ("pbg", "bg-pri"), ("sbg", "bg-sec"), ("tbg", "bg-ter"), ("pfg", "fg-pri"), ("sfg", "fg-sec"),
        ("tfg", "fg-ter"), ("pbr", "brd-pri"), ("sbr", "brd-sec"), ("tbr", "brd-ter")
    ];

    private static readonly Regex AnyValue = new(".*");

    private ILocator Root(string id) => Page.Locator($"#{id}");
    private ILocator Input(string id, int index) => Page.Locator($"#{id} .bit-chg-inp").Nth(index);
    private ILocator ItemLabel(string id, int index) => Page.Locator($"#{id} .bit-chg-itl").Nth(index);
    private ILocator Item(string id, int index) => Page.Locator($"#{id} .bit-chg-icn").Nth(index);
    private ILocator Radio(string id, int index) => Page.Locator($"#{id} .bit-chg-rad").Nth(index);
    private ILocator Text(string id, int index) => Page.Locator($"#{id} .bit-chg-itx").Nth(index);
    private ILocator Span(string id) => Page.Locator($"#{id}");

    private Task<string> ResolveColorAsync(string cssColor)
    {
        return Page.EvaluateAsync<string>(@"v => {
            const d = document.createElement('div');
            d.style.color = v;
            document.body.appendChild(d);
            const c = getComputedStyle(d).color;
            d.remove();
            return c;
        }", cssColor);
    }

    private static Task<string> StyleOfAsync(ILocator locator, string property, string? pseudo = null)
    {
        var pseudoArg = pseudo is null ? "null" : $"'{pseudo}'";
        return locator.EvaluateAsync<string>($"el => getComputedStyle(el, {pseudoArg}).getPropertyValue('{property}')");
    }

    private static async Task<LocatorBoundingBoxResult> BoxOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no layout box.");
        return box;
    }

    private static double Px(string value) => double.Parse(value.Replace("px", string.Empty), CultureInfo.InvariantCulture);

    /// <summary>
    /// Waits until a pseudo-element of an element computes the given value, the way ToHaveCSSAsync waits on an element.
    /// </summary>
    private async Task ExpectPseudoAsync(ILocator locator, string pseudo, string property, string expected)
    {
        var handle = await locator.ElementHandleAsync();
        await Page.WaitForFunctionAsync("([el, pseudo, prop, expected]) => getComputedStyle(el, pseudo).getPropertyValue(prop) === expected",
            new object[] { handle, pseudo, property, expected });
    }

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheRadioGroupBeforeTheCircuitStarts()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"cg-basic\"");
        StringAssert.Contains(html, "role=\"radiogroup\"");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
        Assert.IsTrue(Regex.IsMatch(html, "<input[^>]*checked[^>]*value=\"B\""), "The default value is prerendered checked.");

        await OpenAsync(Url);
        await Expect(Input("cg-basic", 1)).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Basic_ClickingAnItem_SelectsItAndFiresOnChangeOnlyOnARealChange()
    {
        await OpenAsync(Url);

        await Expect(Input("cg-basic", 1)).ToBeCheckedAsync();
        await Expect(Item("cg-basic", 1)).ToHaveClassAsync(HasClass("bit-chg-ich"));
        await Expect(Input("cg-basic", 0)).ToHaveAttributeAsync("type", "radio");

        await ItemLabel("cg-basic", 2).ClickAsync();

        await Expect(Input("cg-basic", 2)).ToBeCheckedAsync();
        await Expect(Input("cg-basic", 1)).Not.ToBeCheckedAsync();
        await Expect(Item("cg-basic", 2)).ToHaveClassAsync(HasClass("bit-chg-ich"));
        await Expect(Item("cg-basic", 1)).Not.ToHaveClassAsync(HasClass("bit-chg-ich"));
        await Expect(Span("basic-change-count")).ToHaveTextAsync("1");
        await Expect(Span("basic-last")).ToHaveTextAsync("C");
        await Expect(Text("cg-basic", 2)).ToHaveCSSAsync("font-weight", "700");

        await ItemLabel("cg-basic", 2).ClickAsync();
        await ItemLabel("cg-basic", 3).ClickAsync();
        await Expect(Span("basic-last")).ToHaveTextAsync("D");
        await Expect(Span("basic-change-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Basic_TheRadiosShareOneNameAndTheGroupIsOneTabStopOnTheCheckedItem()
    {
        await OpenAsync(Url);

        var names = await Page.Locator("#cg-basic .bit-chg-inp").EvaluateAllAsync<string[]>("els => els.map(e => e.name)");
        Assert.AreEqual(1, names.Distinct().Count(), "Every radio of the group carries the same name.");

        await Page.Locator("#btn-before-basic").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Input("cg-basic", 1)).ToBeFocusedAsync();
        await Expect(ItemLabel("cg-basic", 1)).ToHaveCSSAsync("box-shadow", new Regex("^(?!none$).+"));

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#btn-after-basic")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_ArrowKeys_MoveTheSelectionAndWrapAroundTheEnds()
    {
        await OpenAsync(Url);

        await Input("cg-basic", 1).FocusAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Input("cg-basic", 2)).ToBeCheckedAsync();
        await Expect(Input("cg-basic", 2)).ToBeFocusedAsync();
        await Expect(Span("basic-last")).ToHaveTextAsync("C");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Input("cg-basic", 3)).ToBeCheckedAsync();

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Input("cg-basic", 2)).ToBeCheckedAsync();

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Input("cg-basic", 1)).ToBeCheckedAsync();
        await Expect(Span("basic-change-count")).ToHaveTextAsync("4");

        if (IsWebKit) Assert.Inconclusive("WebKit keeps the arrow keys of a radio group on the markup order and stops at the ends.");

        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Input("cg-basic", 3)).ToBeCheckedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Input("cg-basic", 0)).ToBeCheckedAsync();
        await Expect(Span("basic-last")).ToHaveTextAsync("A");
    }

    [TestMethod]
    public async Task Keyboard_WithNothingChecked_TabLandsOnTheFirstItemAndSpaceSelectsIt()
    {
        await OpenAsync(Url);

        await Input("cg-nocircle", 1).FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Input("cg-none", 0)).ToBeFocusedAsync();
        await Expect(Input("cg-none", 0)).Not.ToBeCheckedAsync();

        await Page.Keyboard.PressAsync("Space");
        await Expect(Input("cg-none", 0)).ToBeCheckedAsync();
        await Expect(Item("cg-none", 0)).ToHaveClassAsync(HasClass("bit-chg-ich"));
    }

    [TestMethod]
    public async Task NoCircle_DropsTheRadioCircleButKeepsTheItemsSelectable()
    {
        await OpenAsync(Url);

        await Expect(Root("cg-nocircle")).ToHaveClassAsync(HasClass("bit-chg-ncr"));
        await Expect(Root("cg-basic")).ToHaveClassAsync(HasClass("bit-chg-wcr"));
        await Expect(Page.Locator("#cg-nocircle .bit-chg-rad")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cg-basic .bit-chg-rad")).ToHaveCountAsync(4);

        await Text("cg-nocircle", 3).ClickAsync();
        await Expect(Input("cg-nocircle", 3)).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Disabled_TheWholeGroupRendersTheDisabledMarkupAndIgnoresClicks()
    {
        await OpenAsync(Url);

        await Expect(Root("cg-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Root("cg-disabled")).ToHaveAttributeAsync("aria-disabled", "true");

        for (var i = 0; i < 4; i++)
        {
            await Expect(Input("cg-disabled", i)).ToHaveAttributeAsync("disabled", "");
            await Expect(Input("cg-disabled", i)).ToHaveAttributeAsync("aria-disabled", "true");
            await Expect(Item("cg-disabled", i)).ToHaveClassAsync(HasClass("bit-chg-ids"));
        }

        await ItemLabel("cg-disabled", 2).ClickAsync(new() { Force = true });
        await ItemLabel("cg-basic", 3).ClickAsync();
        await Expect(Span("basic-last")).ToHaveTextAsync("D");

        await Expect(Input("cg-disabled", 0)).ToBeCheckedAsync();
        await Expect(Input("cg-disabled", 2)).Not.ToBeCheckedAsync();
        await ExpectPseudoAsync(Radio("cg-disabled", 0), "::after", "background-color", await ResolveColorAsync("var(--bit-clr-pri-dis)"));
        await Expect(ItemLabel("cg-disabled", 1)).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-pri-dis-text)"));
    }

    [TestMethod]
    public async Task DisabledItem_CannotBeChosenAndTheArrowKeysSkipIt()
    {
        await OpenAsync(Url);

        await Expect(Input("cg-dis-item", 2)).ToHaveAttributeAsync("disabled", "");
        await Expect(Input("cg-dis-item", 2)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Item("cg-dis-item", 2)).ToHaveClassAsync(HasClass("bit-chg-ids"));
        await Expect(Input("cg-dis-item", 1)).Not.ToHaveAttributeAsync("disabled", AnyValue);
        await Expect(Root("cg-dis-item")).Not.ToHaveClassAsync(HasClass("bit-dis"));

        await ItemLabel("cg-dis-item", 2).ClickAsync(new() { Force = true });
        await Expect(Input("cg-dis-item", 1)).ToBeCheckedAsync();

        await Input("cg-dis-item", 1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        await Expect(Input("cg-dis-item", 3)).ToBeCheckedAsync();
        await Expect(Span("dis-item-value")).ToHaveTextAsync("D");
    }

    [TestMethod]
    public async Task ReadOnly_KeepsTheValueAgainstClicksAndKeysButFollowsThePage()
    {
        await OpenAsync(Url);

        await Expect(Root("cg-readonly")).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Root("cg-readonly")).ToHaveClassAsync(HasClass("bit-chg-rdo"));
        await Expect(Root("cg-readonly")).ToHaveAttributeAsync("aria-required", "true");
        await Expect(Input("cg-readonly", 0)).Not.ToHaveAttributeAsync("required", AnyValue);
        await Expect(Input("cg-readonly", 0)).Not.ToHaveAttributeAsync("disabled", AnyValue);

        await ItemLabel("cg-readonly", 3).ClickAsync();
        await Input("cg-readonly", 0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await ItemLabel("cg-basic", 3).ClickAsync();
        await Expect(Span("basic-last")).ToHaveTextAsync("D");

        await Expect(Span("readonly-value")).ToHaveTextAsync("A");
        await Expect(Input("cg-readonly", 0)).ToBeCheckedAsync();
        await Expect(Input("cg-readonly", 3)).Not.ToBeCheckedAsync();
        await Expect(Span("readonly-clicks")).ToHaveTextAsync("0");

        await Page.Locator("#btn-readonly-set").ClickAsync();
        await Expect(Input("cg-readonly", 3)).ToBeCheckedAsync();
        await Expect(Item("cg-readonly", 3)).ToHaveClassAsync(HasClass("bit-chg-ich"));
    }

    [TestMethod]
    public async Task Images_RenderTilesSwapToTheSelectedImageAndHonourSizeAndAlt()
    {
        await OpenAsync(Url);

        var barImage = Page.Locator("#cg-images .bit-chg-icn").Nth(0).Locator("img");
        var pieImage = Page.Locator("#cg-images .bit-chg-icn").Nth(1).Locator("img");
        var plainImage = Page.Locator("#cg-images .bit-chg-icn").Nth(2).Locator("img");

        await Expect(Item("cg-images", 0)).ToHaveClassAsync(HasClass("bit-chg-ihi"));
        await Expect(ItemLabel("cg-images", 0)).ToHaveClassAsync(HasClass("bit-chg-ili"));
        await Expect(barImage).ToHaveAttributeAsync("src", "samples/choicegroup-bar-selected.svg");
        await Expect(pieImage).ToHaveAttributeAsync("src", "samples/choicegroup-pie.svg");
        await Expect(barImage).ToHaveAttributeAsync("alt", "alt for Bar image");
        await Expect(plainImage).ToHaveAttributeAsync("alt", "");

        var wrapper = Page.Locator("#cg-images .bit-chg-iiw").Nth(0);
        await Expect(wrapper).ToHaveCSSAsync("width", "32px");
        await Expect(wrapper).ToHaveCSSAsync("height", "32px");
        await Page.WaitForFunctionAsync("() => [...document.querySelectorAll('#cg-images img')].every(i => i.complete && i.naturalWidth > 0)");

        await ItemLabel("cg-images", 1).ClickAsync();

        await Expect(pieImage).ToHaveAttributeAsync("src", "samples/choicegroup-pie-selected.svg");
        await Expect(barImage).ToHaveAttributeAsync("src", "samples/choicegroup-bar.svg");

        // Without a SelectedImageSrc the item keeps its image when it is checked.
        await ItemLabel("cg-images", 2).ClickAsync();
        await Expect(plainImage).ToHaveAttributeAsync("src", "samples/choicegroup-pie.svg");
        await Expect(Input("cg-images", 2)).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Icons_RenderIconTilesAndADisabledIconItemIsDimmed()
    {
        await OpenAsync(Url);

        var icon = Page.Locator("#cg-icons .bit-chg-ico").Nth(0);
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--CalendarDay"));
        await Expect(icon).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Page.Locator("#cg-icons .bit-chg-ico").Nth(1)).ToHaveClassAsync(HasClass("bit-icon--CalendarWeek"));
        await Expect(ItemLabel("cg-icons", 0)).ToHaveClassAsync(HasClass("bit-chg-ili"));
        await Expect(Item("cg-icons", 0)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-bg-sec)"));

        await Expect(Item("cg-icons", 2)).ToHaveClassAsync(HasClass("bit-chg-ids"));
        await Expect(Page.Locator("#cg-icons .bit-chg-ico").Nth(2)).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-pri-dis-text)"));

        await ItemLabel("cg-icons", 1).ClickAsync();
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(Input("cg-icons", 1)).ToBeCheckedAsync();
        await Expect(ItemLabel("cg-icons", 1)).ToHaveCSSAsync("border-top-color", await ResolveColorAsync("var(--bit-clr-pri)"));
    }

    [TestMethod]
    public async Task Inline_PutsTheImageOrIconOnTheLineOfTheText()
    {
        await OpenAsync(Url);

        await Expect(Root("cg-images-inline")).ToHaveClassAsync(HasClass("bit-chg-inl"));
        await Expect(ItemLabel("cg-images-inline", 0)).Not.ToHaveClassAsync(HasClass("bit-chg-ili"));
        await Expect(Page.Locator("#cg-images-inline .bit-chg-iiw").Nth(0)).ToHaveCSSAsync("width", "20px");

        var icon = await BoxOfAsync(Page.Locator("#cg-icons-inline .bit-chg-icw").Nth(0));
        var text = await BoxOfAsync(Text("cg-icons-inline", 0));
        Assert.IsTrue(icon.X + icon.Width <= text.X + 1, "The icon sits before the text.");
        Assert.AreEqual(icon.Y + icon.Height / 2, text.Y + text.Height / 2, 6, "The icon and the text share one line.");

        var tileIcon = await BoxOfAsync(Page.Locator("#cg-icons .bit-chg-icw").Nth(0));
        var tileText = await BoxOfAsync(Text("cg-icons", 0));
        Assert.IsTrue(tileText.Y >= tileIcon.Y + tileIcon.Height - 1, "In a tile the text sits under the icon.");
    }

    [TestMethod]
    public async Task Horizontal_LaysTheItemsOutInARowAndSaysSoInTheOrientation()
    {
        await OpenAsync(Url);

        await Expect(Root("cg-horizontal")).ToHaveClassAsync(HasClass("bit-chg-hor"));
        await Expect(Root("cg-horizontal")).ToHaveAttributeAsync("aria-orientation", "horizontal");
        await Expect(Root("cg-basic")).ToHaveAttributeAsync("aria-orientation", "vertical");

        var first = await BoxOfAsync(Item("cg-horizontal", 0));
        var second = await BoxOfAsync(Item("cg-horizontal", 1));
        Assert.AreEqual(first.Y, second.Y, 1, "Horizontal items share a row.");
        Assert.IsTrue(second.X > first.X);

        var v1 = await BoxOfAsync(Item("cg-basic", 0));
        var v2 = await BoxOfAsync(Item("cg-basic", 1));
        Assert.IsTrue(v2.Y > v1.Y, "Vertical items are stacked.");

        var i1 = await BoxOfAsync(Item("cg-horizontal-icons", 0));
        var i2 = await BoxOfAsync(Item("cg-horizontal-icons", 1));
        Assert.AreEqual(i1.Y, i2.Y, 1, "Horizontal icon tiles share a row.");
    }

    [TestMethod]
    public async Task LabelPlacement_EachValue_PutsTheContentOnThatSideOfTheCircle()
    {
        await OpenAsync(Url);

        var cases = new (string Id, string Class, string Side)[]
        {
            ("cg-place-end", "bit-chg-led", "end"),
            ("cg-place-start", "bit-chg-lst", "start"),
            ("cg-place-top", "bit-chg-ltp", "top"),
            ("cg-place-bottom", "bit-chg-lbm", "bottom"),
        };

        foreach (var (id, cls, side) in cases)
        {
            await Expect(Root(id)).ToHaveClassAsync(HasClass(cls));

            var radio = await BoxOfAsync(Radio(id, 0));
            var text = await BoxOfAsync(Text(id, 0));

            var holds = side switch
            {
                "end" => radio.X + radio.Width <= text.X + 0.5,
                "start" => text.X + text.Width <= radio.X + 0.5,
                "top" => text.Y + text.Height <= radio.Y + 0.5,
                _ => radio.Y + radio.Height <= text.Y + 0.5,
            };

            Assert.IsTrue(holds, $"{id}: the content is on the {side} side of the circle.");
        }

        await Expect(Root("cg-basic")).ToHaveClassAsync(HasClass("bit-chg-led"));
    }

    [TestMethod]
    public async Task LabelTemplate_RendersTheLabelAndNamesTheGroup()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cg-label-tpl #custom-label")).ToHaveTextAsync("Custom label");
        await Expect(Page.Locator("#cg-label-tpl .bit-chg-lbl")).ToHaveCountAsync(0);
        await Expect(Root("cg-label-tpl")).ToHaveAccessibleNameAsync("Custom label");

        var labelId = await Page.Locator("#cg-label-tpl #custom-label").EvaluateAsync<string>("el => el.parentElement.id");
        await Expect(Root("cg-label-tpl")).ToHaveAttributeAsync("aria-labelledby", labelId);
    }

    [TestMethod]
    public async Task ItemTemplates_EachLevelReplacesItsOwnPartOfTheItem()
    {
        await OpenAsync(Url);

        // Prefix and suffix templates wrap the built-in content and leave the circle in place.
        await Expect(Page.Locator("#cg-affix-tpl .tpl-prefix").Nth(2)).ToHaveTextAsync("3.");
        await Expect(Page.Locator("#cg-affix-tpl .tpl-suffix").Nth(2)).ToHaveTextAsync("(C)");
        await Expect(Page.Locator("#cg-affix-tpl .bit-chg-rad")).ToHaveCountAsync(4);
        await Expect(Page.Locator("#cg-affix-tpl .bit-chg-inp:checked")).ToHaveCountAsync(0);

        // ItemLabelTemplate replaces the content beside the circle.
        await Expect(Page.Locator("#cg-label-item-tpl .bit-chg-rad")).ToHaveCountAsync(3);
        await Expect(Page.Locator("#cg-label-item-tpl .bit-chg-itx")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cg-label-item-tpl .tpl-label").Nth(0)).ToHaveClassAsync(HasClass("selected"));
        await Page.Locator("#cg-label-item-tpl .tpl-label").Nth(1).ClickAsync();
        await Expect(Page.Locator("#cg-label-item-tpl .tpl-label").Nth(1)).ToHaveClassAsync(HasClass("selected"));
        await Expect(Page.Locator("#cg-label-item-tpl .tpl-label").Nth(0)).Not.ToHaveClassAsync(HasClass("selected"));

        // ItemTemplate takes over the whole item, circle included.
        await Expect(Page.Locator("#cg-item-tpl .bit-chg-rad")).ToHaveCountAsync(0);
        await Page.Locator("#cg-item-tpl .tpl-item").Nth(2).ClickAsync();
        await Expect(Span("item-template-value")).ToHaveTextAsync("Month");
        await Expect(Page.Locator("#cg-item-tpl .tpl-item").Nth(2)).ToHaveClassAsync(HasClass("selected"));

        // An item's own Template.
        await Expect(Page.Locator("#cg-own-tpl .own-tpl").Nth(0)).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        await Expect(Page.Locator("#cg-own-tpl .own-tpl").Nth(1)).ToHaveTextAsync("Week");
        await Expect(Page.Locator("#cg-own-tpl .bit-chg-rad")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Binding_UncontrolledOneWayAndTwoWay_BehaveAsTheirShapesSay()
    {
        await OpenAsync(Url);

        await ItemLabel("cg-uncontrolled", 2).ClickAsync();
        await Expect(Span("uncontrolled-value")).ToHaveTextAsync("C");
        await Expect(Input("cg-uncontrolled", 2)).ToBeCheckedAsync();

        // One way: a click cannot write back, the page moves it.
        await ItemLabel("cg-oneway", 1).ClickAsync();
        await ItemLabel("cg-basic", 3).ClickAsync();
        await Expect(Span("basic-last")).ToHaveTextAsync("D");
        await Expect(Span("oneway-value")).ToHaveTextAsync("A");
        await Expect(Item("cg-oneway", 0)).ToHaveClassAsync(HasClass("bit-chg-ich"));
        // The native selection stays with the bound value too: the radio that was clicked is not left checked.
        await Expect(Input("cg-oneway", 0)).ToBeCheckedAsync();
        await Expect(Input("cg-oneway", 1)).Not.ToBeCheckedAsync();
        await Page.Locator("#btn-oneway-c").ClickAsync();
        await Expect(Input("cg-oneway", 2)).ToBeCheckedAsync();

        await ItemLabel("cg-twoway", 1).ClickAsync();
        await Expect(Span("twoway-value")).ToHaveTextAsync("B");
        await Page.Locator("#btn-twoway-d").ClickAsync();
        await Expect(Input("cg-twoway", 3)).ToBeCheckedAsync();
        await Expect(Item("cg-twoway", 1)).Not.ToHaveClassAsync(HasClass("bit-chg-ich"));
        await Page.Locator("#btn-twoway-null").ClickAsync();
        await Expect(Page.Locator("#cg-twoway .bit-chg-ich")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cg-twoway .bit-chg-inp:checked")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Comparer_MatchesTheValueByKeyRatherThanByReference()
    {
        await OpenAsync(Url);

        await Expect(Input("cg-comparer", 1)).ToBeCheckedAsync();
        await Expect(Span("fruit-value")).ToHaveTextAsync("Banana (copy)");

        await ItemLabel("cg-comparer", 0).ClickAsync();

        await Expect(Span("fruit-value")).ToHaveTextAsync("Apple");
        await Expect(Input("cg-comparer", 0)).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Validation_ANothingCheckedRequiredGroup_RefusesTheSubmitAndMarksTheGroupInvalid()
    {
        await OpenAsync(Url);

        await Expect(Root("cg-validation")).ToHaveAttributeAsync("aria-required", "true");
        await Expect(Root("cg-validation")).ToHaveClassAsync(HasClass("bit-chg-req"));
        await Expect(Input("cg-validation", 0)).ToHaveAttributeAsync("required", "");
        await Expect(Input("cg-validation", 0)).ToHaveAttributeAsync("data-input", "radio");
        var asterisk = await StyleOfAsync(Page.Locator("#cg-validation .bit-chg-lbl"), "content", "::after");
        StringAssert.Contains(asterisk, "*");

        await Page.Locator("#btn-submit").ClickAsync();

        await Expect(Span("submit-result")).ToHaveTextAsync("invalid:1");
        await Expect(Page.Locator("#cg-item-error")).ToHaveTextAsync("Pick one");
        await Expect(Root("cg-validation")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("cg-validation")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("cg-validation", 2)).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("cg-validation")).ToHaveAttributeAsync("aria-describedby", new Regex(@"(^|\s)cg-item-error(\s|$)"));
        await Expect(Root("cg-validation")).ToHaveAccessibleDescriptionAsync("Pick one");
        await ExpectPseudoAsync(Radio("cg-validation", 0), "::before", "border-top-color", await ResolveColorAsync("var(--bit-clr-err)"));

        await Expect(Root("cg-novalidate")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("cg-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await ItemLabel("cg-validation", 1).ClickAsync();

        await Expect(Page.Locator("#cg-item-error")).ToHaveTextAsync(string.Empty);
        await Expect(Root("cg-validation")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Input("cg-validation", 2)).Not.ToHaveAttributeAsync("aria-invalid", AnyValue);

        await ItemLabel("cg-novalidate", 0).ClickAsync();
        await Page.Locator("#btn-submit").ClickAsync();
        await Expect(Span("submit-result")).ToHaveTextAsync("valid:1");
    }

    [TestMethod]
    public async Task Name_NamesTheRadiosAndTheCheckedOneIsPosted()
    {
        await OpenAsync(Url);

        await Expect(Input("cg-named", 0)).ToHaveAttributeAsync("name", "plan");
        Assert.AreEqual("B", await Page.EvaluateAsync<string?>("() => new FormData(document.getElementById('plain-form')).get('plan')"));

        await ItemLabel("cg-named", 3).ClickAsync();
        await Expect(Input("cg-named", 3)).ToBeCheckedAsync();
        Assert.AreEqual("D", await Page.EvaluateAsync<string?>("() => new FormData(document.getElementById('plain-form')).get('plan')"));
    }

    [TestMethod]
    public async Task ItemDescriptions_DescribeEachRadioWhichIsNamedByItsTextAlone()
    {
        await OpenAsync(Url);

        var description = Page.Locator("#cg-descriptions .bit-chg-dsc").Nth(0);
        await Expect(description).ToHaveTextAsync("Backs up every night at 2 AM.");
        await Expect(Input("cg-descriptions", 0)).ToHaveAttributeAsync("aria-describedby", (await description.GetAttributeAsync("id"))!);
        await Expect(Input("cg-descriptions", 0)).ToHaveAccessibleNameAsync("Daily");
        await Expect(Input("cg-descriptions", 0)).ToHaveAccessibleDescriptionAsync("Backs up every night at 2 AM.");

        var text = await BoxOfAsync(Text("cg-descriptions", 0));
        var line = await BoxOfAsync(description);
        Assert.IsTrue(line.Y >= text.Y + text.Height - 1, "The description sits under the item text.");
    }

    [TestMethod]
    public async Task Gap_SetsTheRoomBetweenTheItemsAndWinsOverSizeAndTheVariable()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cg-gap .bit-chg-cnt")).ToHaveCSSAsync("row-gap", "16px");
        await Expect(Page.Locator("#cg-gap-hor .bit-chg-cnt")).ToHaveCSSAsync("column-gap", "48px");
        await Expect(Page.Locator("#cg-gap-wins .bit-chg-cnt")).ToHaveCSSAsync("row-gap", "20px");

        var first = await BoxOfAsync(Item("cg-gap-hor", 0));
        var second = await BoxOfAsync(Item("cg-gap-hor", 1));
        Assert.AreEqual(48, second.X - (first.X + first.Width), 1);
    }

    [TestMethod]
    public async Task PrefixAndSuffix_WrapTheItemTextAndASuffixCanFormATrailingColumn()
    {
        await OpenAsync(Url);

        await Expect(ItemLabel("cg-prefix", 1)).ToContainTextAsync("$10 -");
        var prefix = await BoxOfAsync(ItemLabel("cg-prefix", 1).Locator("span").First);
        var text = await BoxOfAsync(Text("cg-prefix", 1));
        Assert.IsTrue(prefix.X < text.X, "The prefix comes before the text.");

        var suffix = ItemLabel("cg-suffix", 2).Locator("span").Last;
        await Expect(suffix).ToHaveTextAsync("$25");
        var suffixBox = await BoxOfAsync(suffix);
        var labelBox = await BoxOfAsync(ItemLabel("cg-suffix", 2));
        Assert.AreEqual(labelBox.X + labelBox.Width, suffixBox.X + suffixBox.Width, 2, "The suffix sits at the far edge.");
    }

    [TestMethod]
    public async Task Events_FireWithTheItemOnClickKeyAndFocusAndStaySilentWhileDisabled()
    {
        await OpenAsync(Url);

        await ItemLabel("cg-events", 1).ClickAsync();
        await Expect(Span("ev-state")).ToContainTextAsync("change=1:B;click=1:Item B;");

        // A click on the checked item reports a click and no change.
        await ItemLabel("cg-events", 1).ClickAsync();
        await Expect(Span("ev-state")).ToContainTextAsync("change=1:B;click=2:Item B;");

        await Page.Locator("#btn-before-events").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Span("ev-state")).ToContainTextAsync(new Regex(@"focus=\d+:Item B"));

        // An arrow key is reported as a click by the browser, and changes the value.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Span("ev-state")).ToContainTextAsync("change=2:C;click=3:Item C;");
        await Expect(Span("ev-state")).ToContainTextAsync(new Regex(@"blur=\d+:Item B"));

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Span("ev-state")).ToContainTextAsync(new Regex(@"blur=\d+:Item C"));

        await ItemLabel("cg-events-disabled", 2).ClickAsync(new() { Force = true });
        await ItemLabel("cg-basic", 3).ClickAsync();
        await Expect(Span("basic-last")).ToHaveTextAsync("D");
        await Expect(Span("disabled-events")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task DynamicItems_ReIndexTheItemsAndKeepTheSelectionThatSurvives()
    {
        await OpenAsync(Url);

        var indexes = Page.Locator("#cg-dynamic .dyn-index");
        await Expect(indexes).ToHaveTextAsync(new[] { "1.", "2.", "3." });
        await Expect(Input("cg-dynamic", 0)).ToBeCheckedAsync();

        await Page.Locator("#btn-dyn-add").ClickAsync();
        await Expect(indexes).ToHaveTextAsync(new[] { "1.", "2.", "3.", "4." });
        await Expect(Text("cg-dynamic", 3)).ToHaveTextAsync("Item 4");

        await Page.Locator("#btn-dyn-reverse").ClickAsync();
        await Expect(Text("cg-dynamic", 0)).ToHaveTextAsync("Item 4");
        await Expect(indexes.Nth(0)).ToHaveTextAsync("1.");
        await Expect(Input("cg-dynamic", 3)).ToBeCheckedAsync();
        await Expect(Span("dynamic-value")).ToHaveTextAsync("1");

        await Page.Locator("#btn-dyn-remove").ClickAsync();
        await Expect(Page.Locator("#cg-dynamic .bit-chg-icn")).ToHaveCountAsync(3);
        await Expect(Page.Locator("#cg-dynamic .bit-chg-inp:checked")).ToHaveCountAsync(0);

        await ItemLabel("cg-dynamic", 2).ClickAsync();
        await Expect(Span("dynamic-value")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task GroupDescription_DescribesTheGroupAndItsTemplateWins()
    {
        await OpenAsync(Url);

        var description = Page.Locator("#cg-group-desc .bit-chg-gds");
        await Expect(description).ToHaveTextAsync("Only the selected environment receives the new build.");
        await Expect(Root("cg-group-desc")).ToHaveAttributeAsync("aria-describedby", (await description.GetAttributeAsync("id"))!);
        await Expect(Root("cg-group-desc")).ToHaveAccessibleDescriptionAsync("Only the selected environment receives the new build.");
        await Expect(description).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-fg-sec)"));

        await Expect(Page.Locator("#cg-group-desc-tpl .bit-chg-gds #custom-description")).ToBeVisibleAsync();
        await Expect(Page.Locator("#cg-group-desc-tpl .bit-chg-gds")).Not.ToContainTextAsync("Ignored");
    }

    [TestMethod]
    public async Task FullWidth_StretchesTheGroupSharesColumnsAndPushesTheCirclesToTheFarEdge()
    {
        await OpenAsync(Url);

        var box = await BoxOfAsync(Page.Locator("#fw-box"));

        Assert.IsTrue((await BoxOfAsync(Root("cg-fw-default"))).Width < box.Width - 50, "By default the group hugs its items.");

        await Expect(Root("cg-fw-hor")).ToHaveClassAsync(HasClass("bit-chg-flw"));
        Assert.AreEqual(box.Width, (await BoxOfAsync(Root("cg-fw-hor"))).Width, 1);
        var widths = new List<double>();
        for (var i = 0; i < 4; i++) widths.Add((await BoxOfAsync(Item("cg-fw-hor", i))).Width);
        Assert.IsTrue(widths.Max() - widths.Min() < 1.5, $"Horizontal full-width items share equal columns ({string.Join(", ", widths)}).");

        var container = await BoxOfAsync(Page.Locator("#cg-fw-start .bit-chg-cnt"));
        var radio = await BoxOfAsync(Radio("cg-fw-start", 0));
        Assert.AreEqual(container.X + container.Width, radio.X + radio.Width, 2, "Start pushes the circles to the far edge.");

        await Expect(Root("cg-fw-stretch")).ToHaveClassAsync(HasClass("bit-chg-sil"));
        var stretched = await BoxOfAsync(ItemLabel("cg-fw-stretch", 0));
        var stretchContainer = await BoxOfAsync(Page.Locator("#cg-fw-stretch .bit-chg-cnt"));
        Assert.AreEqual(stretchContainer.Width, stretched.Width, 1.5, "StretchItemLabel stretches the label over its row.");
        var stretchedText = await BoxOfAsync(Text("cg-fw-stretch", 0));
        Assert.AreEqual(stretchContainer.X, stretchedText.X, 2, "The text starts at the reading edge.");
    }

    [TestMethod]
    public async Task ItemTitle_IsTheNativeTooltipOfTheItem()
    {
        await OpenAsync(Url);

        await Expect(Item("cg-titles", 0)).ToHaveAttributeAsync("title", "Delivered within one hour of dispatch");
        await Expect(Item("cg-titles", 2)).ToHaveAttributeAsync("title", "Delivered within three business days");
    }

    [TestMethod]
    public async Task AutoFocus_FocusesTheTabStopOnTheFirstRenderButNotOfAReadOnlyGroup()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-mount-autofocus").ClickAsync();
        await Expect(Input("cg-autofocus", 1)).ToBeFocusedAsync();

        await Page.Locator("#btn-mount-autofocus-none").ClickAsync();
        await Expect(Input("cg-autofocus-none", 1)).ToBeFocusedAsync();

        await Page.Locator("#btn-mount-autofocus-readonly").ClickAsync();
        await Expect(Root("cg-autofocus-readonly")).ToBeVisibleAsync();
        await NextFramesAsync();
        await Expect(Page.Locator("#btn-mount-autofocus-readonly")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Variant_OutlineAndFillDrawCardsTintedWhenChecked()
    {
        await OpenAsync(Url);

        await Expect(Root("cg-outline")).ToHaveClassAsync(HasClass("bit-chg-otl"));
        await Expect(Root("cg-fill")).ToHaveClassAsync(HasClass("bit-chg-fil"));
        await Expect(Root("cg-text")).ToHaveClassAsync(HasClass("bit-chg-txt"));
        await Expect(Root("cg-basic")).ToHaveClassAsync(HasClass("bit-chg-txt"));

        await Page.Mouse.MoveAsync(0, 0);
        await Expect(ItemLabel("cg-outline", 1)).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(ItemLabel("cg-outline", 1)).ToHaveCSSAsync("border-top-color", await ResolveColorAsync("var(--bit-clr-brd-sec)"));
        await Expect(ItemLabel("cg-outline", 0)).ToHaveCSSAsync("border-top-color", await ResolveColorAsync("var(--bit-clr-pri)"));
        await Expect(ItemLabel("cg-outline", 1)).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(ItemLabel("cg-outline", 0)).Not.ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

        await Expect(ItemLabel("cg-fill", 1)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-bg-sec)"));
        Assert.AreNotEqual(await StyleOfAsync(ItemLabel("cg-fill", 1), "background-color"), await StyleOfAsync(ItemLabel("cg-fill", 0), "background-color"));
        var card = await BoxOfAsync(ItemLabel("cg-fill", 0));
        var radio = await BoxOfAsync(Radio("cg-fill", 0));
        Assert.IsTrue(radio.X > card.X + card.Width / 2, "Fill with Start puts the circle at the far edge of the card.");

        await Expect(ItemLabel("cg-text", 0)).ToHaveCSSAsync("border-top-style", "none");

        // The card is the click target.
        await ItemLabel("cg-outline", 2).ClickAsync(new() { Position = new() { X = 3, Y = 3 } });
        await Expect(Input("cg-outline", 2)).ToBeCheckedAsync();

        var c1 = await BoxOfAsync(ItemLabel("cg-outline-icons", 0));
        var c2 = await BoxOfAsync(ItemLabel("cg-outline-icons", 1));
        Assert.AreEqual(c1.Width, c2.Width, 1.5, "Horizontal full-width cards share equal columns.");
    }

    [TestMethod]
    public async Task Accessibility_TheGroupAndItsItemsAreNamedAndFocusAsyncFocusesTheTabStop()
    {
        await OpenAsync(Url);

        await Expect(Root("cg-arialabel")).ToHaveAttributeAsync("aria-label", "Sort order");
        await Expect(Root("cg-arialabel")).Not.ToHaveAttributeAsync("aria-labelledby", AnyValue);
        await Expect(Page.GetByRole(AriaRole.Radiogroup, new() { Name = "Sort order" })).ToHaveCountAsync(1);

        await Expect(Root("cg-labelledby")).ToHaveAttributeAsync("aria-labelledby", "cg-density-question");
        await Expect(Root("cg-labelledby")).ToHaveAccessibleNameAsync("How dense should the rows be?");

        await Expect(Root("cg-basic")).ToHaveAccessibleNameAsync("Basic Items");
        await Expect(Input("cg-rating", 2)).ToHaveAttributeAsync("aria-label", "3 stars");
        await Expect(Page.GetByRole(AriaRole.Radio, new() { Name = "3 stars" })).ToBeCheckedAsync();

        await Page.Locator("#btn-focus").ClickAsync();
        await Expect(Input("cg-focus-target", 2)).ToBeFocusedAsync();

        await Expect(Input("cg-tabindex-host", 0)).ToHaveAttributeAsync("id", "my-radio-a");
        await Expect(ItemLabel("cg-tabindex-host", 0)).ToHaveAttributeAsync("for", "my-radio-a");
        await Expect(Input("cg-tabindex-host", 1)).ToHaveAttributeAsync("id", new Regex("-Input-1$"));
    }

    [TestMethod]
    public async Task Cascading_BitParams_FillsWhatTheGroupLeavesUnset()
    {
        await OpenAsync(Url);

        await Expect(Root("cg-cascade-1")).ToHaveClassAsync(HasClass("bit-chg-otl"));
        await Expect(Root("cg-cascade-1")).ToHaveClassAsync(HasClass("bit-chg-flw"));
        await Expect(Page.Locator("#cg-cascade-1 .bit-chg-cnt")).ToHaveCSSAsync("row-gap", "8px");

        await Expect(Root("cg-cascade-own")).ToHaveClassAsync(HasClass("bit-chg-fil"));
        await Expect(Root("cg-cascade-own")).Not.ToHaveClassAsync(HasClass("bit-chg-otl"));
        await Expect(Page.Locator("#cg-cascade-own .bit-chg-cnt")).ToHaveCSSAsync("row-gap", "8px");

        await Expect(Root("cg-cascade-outside")).ToHaveClassAsync(HasClass("bit-chg-txt"));
        await Expect(Root("cg-cascade-outside")).Not.ToHaveClassAsync(HasClass("bit-chg-flw"));
    }

    [TestMethod]
    public async Task Color_EachValue_PaintsTheCheckedCircleAndTheDisabledOne()
    {
        await OpenAsync(Url);

        foreach (var (suffix, token) in Colors)
        {
            var id = $"cg-color-{suffix}";
            await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-chg-{suffix}"));

            var main = await ResolveColorAsync($"var(--bit-clr-{token})");
            Assert.AreEqual(main, await StyleOfAsync(Radio(id, 0), "background-color", "::after"), $"{suffix}: the dot");
            Assert.AreEqual(main, await StyleOfAsync(Radio(id, 0), "border-top-color", "::before"), $"{suffix}: the ring");

            var disabled = await ResolveColorAsync($"var(--bit-clr-{token}-dis)");
            Assert.AreEqual(disabled, await StyleOfAsync(Radio($"{id}-dis", 0), "background-color", "::after"), $"{suffix}: the disabled dot");
        }

        await Expect(Root("cg-color-default")).Not.ToHaveClassAsync(HasClass("bit-chg-pri"));
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-pri)"), await StyleOfAsync(Radio("cg-color-default", 0), "background-color", "::after"));
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-err)"), await StyleOfAsync(Radio("cg-color-wins", 0), "background-color", "::after"), "An explicit Color wins over --bit-ChoiceGroup-color.");
    }

    [TestMethod]
    public async Task ExternalIcons_TheIconOfAnItem_RendersItsClassesAndWinsOverIconName()
    {
        await OpenAsync(Url);

        var icons = Page.Locator("#cg-ext .bit-chg-ico");
        await Expect(icons.Nth(0)).ToHaveClassAsync(HasClass("fa-sun"));
        await Expect(icons.Nth(0)).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(icons.Nth(1)).ToHaveClassAsync(HasClass("fa-calendar-week"));
        await Expect(icons.Nth(2)).ToHaveClassAsync(HasClass("bi-calendar-month"));
        await Expect(icons.Nth(3)).ToHaveClassAsync(HasClass("bi-star"));
        await Expect(icons.Nth(3)).Not.ToHaveClassAsync(HasClass("bit-icon--Heart"));
    }

    [TestMethod]
    public async Task Size_EachValue_ScalesTheCircleTheTextTheGapAndTheIcons()
    {
        await OpenAsync(Url);

        foreach (var suffix in new[] { "sm", "md", "lg" })
        {
            await Expect(Root($"cg-size-{suffix}")).ToHaveClassAsync(HasClass($"bit-chg-{suffix}"));
        }

        var sm = await BoxOfAsync(Radio("cg-size-sm", 0));
        var md = await BoxOfAsync(Radio("cg-size-md", 0));
        var lg = await BoxOfAsync(Radio("cg-size-lg", 0));
        var dflt = await BoxOfAsync(Radio("cg-size-default", 0));
        Assert.IsTrue(sm.Width < md.Width && md.Width < lg.Width, $"Circles {sm.Width} < {md.Width} < {lg.Width}.");
        Assert.AreEqual(md.Width, dflt.Width, 0.5, "Medium is the default.");

        var fonts = new List<double>();
        var gaps = new List<double>();
        foreach (var suffix in new[] { "sm", "md", "lg" })
        {
            fonts.Add(Px(await StyleOfAsync(Root($"cg-size-{suffix}"), "font-size")));
            gaps.Add(Px(await StyleOfAsync(Page.Locator($"#cg-size-{suffix} .bit-chg-cnt"), "column-gap")));
        }
        Assert.IsTrue(fonts[0] < fonts[1] && fonts[1] < fonts[2], $"Fonts {string.Join(" < ", fonts)}.");
        Assert.IsTrue(gaps[0] < gaps[1] && gaps[1] < gaps[2], $"Gaps {string.Join(" < ", gaps)}.");

        var smIcon = await BoxOfAsync(Page.Locator("#cg-size-sm-icons .bit-chg-icw").Nth(0));
        var lgIcon = await BoxOfAsync(Page.Locator("#cg-size-lg-icons .bit-chg-icw").Nth(0));
        Assert.IsTrue(smIcon.Height < lgIcon.Height, "The tile icons scale.");
        var smInline = await BoxOfAsync(Page.Locator("#cg-size-sm-inline .bit-chg-icw").Nth(0));
        var lgInline = await BoxOfAsync(Page.Locator("#cg-size-lg-inline .bit-chg-icw").Nth(0));
        Assert.IsTrue(smInline.Height < lgInline.Height, "The inline icons scale.");

        var wins = await BoxOfAsync(Radio("cg-size-wins", 0));
        Assert.AreEqual(lg.Width, wins.Width, 0.5, "An explicit Size wins over --bit-ChoiceGroup-circle-size.");
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRootAndOnEachItem()
    {
        await OpenAsync(Url);

        await Expect(Root("cg-style")).ToHaveCSSAsync("color", "rgb(32, 178, 170)");
        await Expect(Root("cg-style")).ToHaveCSSAsync("margin-left", "16px");
        await Expect(Root("cg-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Item("cg-item-style-class", 0)).ToHaveClassAsync(HasClass("custom-item"));
        await Expect(Item("cg-item-style-class", 1)).ToHaveCSSAsync("border-top-left-radius", "20px");
        await Expect(Item("cg-item-style-class", 1)).ToHaveCSSAsync("padding-top", "8px");
    }

    [TestMethod]
    public async Task Styles_ReachEveryPartAndTheStateEntriesFollowTheItems()
    {
        await OpenAsync(Url);

        const string id = "cg-styles";
        await Expect(Root(id)).ToHaveCSSAsync("padding-top", "5px");
        await Expect(Page.Locator($"#{id} .bit-chg-lbl")).ToHaveCSSAsync("color", "rgb(0, 0, 255)");
        await Expect(Page.Locator($"#{id} .bit-chg-lbl").Locator("..")).ToHaveCSSAsync("padding-top", "1px");
        await Expect(Page.Locator($"#{id} .bit-chg-gds")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Page.Locator($"#{id} .bit-chg-cnt")).ToHaveCSSAsync("padding-top", "2px");
        await Expect(Item(id, 0)).ToHaveCSSAsync("padding-top", "8px");
        await Expect(Item(id, 1)).ToHaveCSSAsync("background-color", "rgb(135, 206, 250)");
        await Expect(Item(id, 0)).Not.ToHaveCSSAsync("background-color", "rgb(135, 206, 250)");
        await Expect(Item(id, 2)).ToHaveCSSAsync("opacity", "0.4");
        await Expect(ItemLabel(id, 0)).ToHaveCSSAsync("cursor", "crosshair");
        await Expect(Radio(id, 0)).ToHaveCSSAsync("outline-color", "rgb(255, 0, 0)");
        await Expect(Text(id, 0)).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(Page.Locator($"#{id} .bit-chg-dsc")).ToHaveCSSAsync("font-weight", "700");
        await Expect(ItemLabel(id, 0).Locator("span").First).ToHaveCSSAsync("color", "rgb(1, 1, 1)");
        await Expect(ItemLabel(id, 0).Locator("span").Last).ToHaveCSSAsync("color", "rgb(2, 2, 2)");
        await Expect(Page.Locator($"#{id} .bit-chg-txd")).ToHaveCSSAsync("padding-top", "3px");

        await ItemLabel(id, 0).ClickAsync();
        await Expect(Item(id, 0)).ToHaveCSSAsync("background-color", "rgb(135, 206, 250)");
        await Expect(Item(id, 1)).Not.ToHaveCSSAsync("background-color", "rgb(135, 206, 250)");

        await Expect(Page.Locator("#cg-classes-media .bit-chg-iic")).ToHaveCSSAsync("padding-top", "4px");
        await Expect(Page.Locator("#cg-classes-media .bit-chg-iiw")).ToHaveCSSAsync("outline-color", "rgb(0, 128, 0)");
        await Expect(Page.Locator("#cg-classes-media img")).ToHaveCSSAsync("opacity", "0.9");
        await Expect(Page.Locator("#cg-classes-media .bit-chg-icw")).ToHaveCSSAsync("padding-top", "6px");
        await Expect(Page.Locator("#cg-classes-media .bit-chg-ico")).ToHaveCSSAsync("color", "rgb(128, 0, 128)");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPartAndTheStateEntriesFollowTheItems()
    {
        await OpenAsync(Url);

        const string id = "cg-classes";
        await Expect(Root(id)).ToHaveClassAsync(HasClass("c-root"));
        await Expect(Page.Locator($"#{id} .c-lblc .bit-chg-lbl")).ToHaveClassAsync(HasClass("c-label"));
        await Expect(Page.Locator($"#{id} .bit-chg-gds")).ToHaveClassAsync(HasClass("c-desc"));
        await Expect(Page.Locator($"#{id} .bit-chg-cnt")).ToHaveClassAsync(HasClass("c-cnt"));
        await Expect(Item(id, 0)).ToHaveClassAsync(HasClass("c-icn"));
        await Expect(ItemLabel(id, 0)).ToHaveClassAsync(HasClass("c-itl"));
        await Expect(Radio(id, 0)).ToHaveClassAsync(HasClass("c-rad"));
        await Expect(Text(id, 0)).ToHaveClassAsync(HasClass("c-text"));
        await Expect(Page.Locator($"#{id} .bit-chg-dsc")).ToHaveClassAsync(HasClass("c-idesc"));
        await Expect(Page.Locator($"#{id} .c-prefix")).ToHaveTextAsync("P");
        await Expect(Page.Locator($"#{id} .c-suffix")).ToHaveTextAsync("S");
        await Expect(Page.Locator($"#{id} .bit-chg-txd")).ToHaveClassAsync(HasClass("c-txw"));
        await Expect(Item(id, 1)).ToHaveClassAsync(HasClass("c-checked"));
        await Expect(Item(id, 0)).Not.ToHaveClassAsync(HasClass("c-checked"));
        await Expect(Item(id, 2)).ToHaveClassAsync(HasClass("c-disabled"));
        await Expect(Item(id, 1)).Not.ToHaveClassAsync(HasClass("c-disabled"));

        await ItemLabel(id, 0).ClickAsync();
        await Expect(Item(id, 0)).ToHaveClassAsync(HasClass("c-checked"));
        await Expect(Item(id, 1)).Not.ToHaveClassAsync(HasClass("c-checked"));

        await Expect(Page.Locator("#cg-classes-media .bit-chg-iic")).ToHaveClassAsync(HasClass("c-iic"));
        await Expect(Page.Locator("#cg-classes-media .bit-chg-iiw")).ToHaveClassAsync(HasClass("c-iiw"));
        await Expect(Page.Locator("#cg-classes-media img")).ToHaveClassAsync(HasClass("c-img"));
        await Expect(Page.Locator("#cg-classes-media .bit-chg-icw")).ToHaveClassAsync(HasClass("c-icw"));
        await Expect(Page.Locator("#cg-classes-media .bit-chg-ico")).ToHaveClassAsync(HasClass("c-ico"));
    }

    [TestMethod]
    public async Task CssVariables_OnTheInstance_RestyleTheGeometryAndThePalette()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        await Expect(Radio("cg-var-geometry", 0)).ToHaveCSSAsync("width", "24px");
        Assert.AreEqual("12px", await StyleOfAsync(Radio("cg-var-geometry", 0), "width", "::after"));
        Assert.AreEqual("3px", await StyleOfAsync(Radio("cg-var-geometry", 1), "border-top-width", "::before"));
        await Expect(Page.Locator("#cg-var-geometry .bit-chg-cnt")).ToHaveCSSAsync("row-gap", "16px");
        await Expect(ItemLabel("cg-var-geometry", 0)).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(Root("cg-var-geometry")).ToHaveCSSAsync("font-size", "18px");
        await Expect(Root("cg-var-geometry")).ToHaveCSSAsync("padding-top", "10px");

        Assert.AreEqual("rgb(102, 51, 153)", await StyleOfAsync(Radio("cg-var-color", 1), "background-color", "::after"));
        Assert.AreEqual("rgb(147, 112, 219)", await StyleOfAsync(Radio("cg-var-color", 0), "border-top-color", "::before"));
        await Expect(Text("cg-var-color", 1)).ToHaveCSSAsync("font-weight", "400");
        await Expect(Page.Locator("#cg-var-color .bit-chg-lbl")).ToHaveCSSAsync("color", "rgb(10, 20, 30)");
        await Expect(Page.Locator("#cg-var-color .bit-chg-lbl")).ToHaveCSSAsync("font-size", "20px");
        await Expect(ItemLabel("cg-var-color", 0)).ToHaveCSSAsync("color", "rgb(40, 50, 60)");
        await Expect(ItemLabel("cg-var-color", 1)).ToHaveCSSAsync("color", "rgb(70, 80, 90)");

        // The hover tier of the accent.
        await ItemLabel("cg-var-color", 1).HoverAsync();
        await ExpectPseudoAsync(Radio("cg-var-color", 1), "::after", "background-color", "rgb(147, 112, 219)");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_AreInheritedAndEachVariantReadsItsTwinFirst()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        await Expect(ItemLabel("cg-var-outline", 0)).ToHaveCSSAsync("border-top-color", "rgb(46, 139, 87)");
        await Expect(ItemLabel("cg-var-outline", 0)).ToHaveCSSAsync("background-color", "rgb(200, 230, 210)");
        await Expect(ItemLabel("cg-var-outline", 1)).ToHaveCSSAsync("border-top-color", "rgb(150, 190, 160)");
        await Expect(Page.Locator("#cg-var-outline .bit-chg-gds")).ToHaveCSSAsync("color", "rgb(0, 128, 128)");
        await Expect(Page.Locator("#cg-var-outline .bit-chg-dsc").Nth(0)).ToHaveCSSAsync("color", "rgb(128, 0, 0)");
        Assert.AreEqual("rgb(0, 0, 128)", await StyleOfAsync(Page.Locator("#cg-var-outline .bit-chg-lbl"), "color", "::after"));
        await Expect(ItemLabel("cg-var-fill", 1)).ToHaveCSSAsync("background-color", "rgb(200, 230, 210)");

        await Expect(ItemLabel("cg-var-twin-fill", 0)).ToHaveCSSAsync("background-color", "rgb(250, 240, 230)");
        await Expect(ItemLabel("cg-var-twin-outline", 0)).ToHaveCSSAsync("border-top-color", "rgb(255, 0, 255)");
        await Expect(ItemLabel("cg-var-twin-outline", 0)).Not.ToHaveCSSAsync("background-color", "rgb(250, 240, 230)");

        Assert.AreEqual("rgb(200, 0, 0)", await StyleOfAsync(Radio("cg-var-disabled", 1), "background-color", "::after"));
        await Expect(ItemLabel("cg-var-disabled", 0)).ToHaveCSSAsync("color", "rgb(0, 0, 200)");
    }

    [TestMethod]
    public async Task Rtl_MirrorsTheItemsAndTheArrowKeysFollowTheDirection()
    {
        await OpenAsync(Url);

        await Expect(Root("cg-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root("cg-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        var radio = await BoxOfAsync(Radio("cg-rtl", 0));
        var text = await BoxOfAsync(Text("cg-rtl", 0));
        Assert.IsTrue(radio.X > text.X, "In RTL the circle is to the right of the text.");

        var first = await BoxOfAsync(Item("cg-rtl-inherited", 0));
        var second = await BoxOfAsync(Item("cg-rtl-inherited", 1));
        Assert.IsTrue(first.X > second.X, "Inherited RTL flows the horizontal items from the right.");
        await Expect(Root("cg-rtl-inherited")).Not.ToHaveAttributeAsync("dir", AnyValue);

        if (IsWebKit) Assert.Inconclusive("WebKit does not mirror the arrow keys of a radio group in RTL.");

        await Input("cg-rtl-inherited", 0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Input("cg-rtl-inherited", 1)).ToBeCheckedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Input("cg-rtl-inherited", 0)).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Base_VisibilityAndHtmlAttributes_AreAppliedToTheRoot()
    {
        await OpenAsync(Url);

        await Expect(Root("cg-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Root("cg-collapsed")).ToHaveCSSAsync("display", "none");
        await Expect(Root("cg-html-attrs")).ToHaveAttributeAsync("data-custom", "abc");
        await Expect(Root("cg-html-attrs")).ToHaveAttributeAsync("aria-required", "true");
        await Expect(Root("cg-html-attrs")).ToHaveAttributeAsync("role", "radiogroup");
    }

    [TestMethod]
    public async Task Disabled_DropsThePageClickHandlersUntilEnabledAgain()
    {
        await OpenAsync(Url);

        await ItemLabel("cg-handler-disabled", 1).ClickAsync(new() { Force = true });
        await Root("cg-handler-disabled").DispatchEventAsync("click");

        await ItemLabel("cg-handler-enabled", 1).ClickAsync();
        await Expect(Span("enabled-root-clicks")).ToHaveTextAsync(new Regex("^[12]$"));
        await Expect(Span("disabled-root-clicks")).ToHaveTextAsync("0");

        await Page.Locator("#btn-toggle-handler-disabled").ClickAsync();
        await Expect(Root("cg-handler-disabled")).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await ItemLabel("cg-handler-disabled", 1).ClickAsync();
        await Expect(Span("disabled-root-clicks")).ToHaveTextAsync(new Regex("^[12]$"));
        await Expect(Input("cg-handler-disabled", 1)).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Runtime_ParameterChanges_ReRenderTheGroup()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cg-runtime .bit-chg-lbl")).ToHaveTextAsync("Initial label");

        await Page.Locator("#btn-runtime-label").ClickAsync();
        await Expect(Page.Locator("#cg-runtime .bit-chg-lbl")).ToHaveTextAsync("Changed label");

        await Page.Locator("#btn-runtime-color").ClickAsync();
        await Expect(Root("cg-runtime")).ToHaveClassAsync(HasClass("bit-chg-suc"));

        await Page.Locator("#btn-runtime-horizontal").ClickAsync();
        await Expect(Root("cg-runtime")).ToHaveAttributeAsync("aria-orientation", "horizontal");

        await Page.Locator("#btn-runtime-items").ClickAsync();
        await Expect(Page.Locator("#cg-runtime .bit-chg-icn")).ToHaveCountAsync(2);
        await Expect(Text("cg-runtime", 1)).ToHaveTextAsync("Descending");

        await ItemLabel("cg-runtime", 1).ClickAsync();
        await Expect(Span("runtime-value")).ToHaveTextAsync("Desc");

        await Page.Locator("#btn-runtime-disable").ClickAsync();
        await Expect(Root("cg-runtime")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("cg-runtime", 0)).ToHaveAttributeAsync("disabled", "");

        await Page.Locator("#btn-runtime-disable").ClickAsync();
        await Expect(Input("cg-runtime", 0)).Not.ToHaveAttributeAsync("disabled", AnyValue);
    }

    [TestMethod]
    public async Task Unmount_RemovesTheGroupWithoutAnError()
    {
        await OpenAsync(Url);

        await ItemLabel("cg-runtime", 2).ClickAsync();
        await Expect(Span("runtime-value")).ToHaveTextAsync("C");

        await Page.Locator("#btn-runtime-unmount").ClickAsync();
        await Expect(Root("cg-runtime")).ToHaveCountAsync(0);

        await Page.Locator("#btn-runtime-unmount").ClickAsync();
        await Expect(Input("cg-runtime", 2)).ToBeCheckedAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheDotTransitionUnlessForceAnimation()
    {
        await OpenAsync(Url);

        StringAssert.StartsWith(await StyleOfAsync(Radio("cg-motion", 0), "transition-duration", "::after"), "0.2s");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        var reduced = await StyleOfAsync(Radio("cg-motion", 0), "transition-duration", "::after");
        Assert.IsFalse(reduced.StartsWith("0.2s"), $"Reduced motion collapses the dot ({reduced}).");
        await Expect(Root("cg-motion-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        StringAssert.StartsWith(await StyleOfAsync(Radio("cg-motion-forced", 0), "transition-duration", "::after"), "0.2s");
    }

    [TestMethod]
    public async Task Hover_PreviewsTheDotOfAnUncheckedItem()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        await ExpectPseudoAsync(Radio("cg-basic", 3), "::after", "opacity", "0");

        await ItemLabel("cg-basic", 3).HoverAsync();
        await ExpectPseudoAsync(Radio("cg-basic", 3), "::after", "opacity", "1");

        await Page.Mouse.MoveAsync(0, 0);
        await ExpectPseudoAsync(Radio("cg-basic", 3), "::after", "opacity", "0");
    }
}
