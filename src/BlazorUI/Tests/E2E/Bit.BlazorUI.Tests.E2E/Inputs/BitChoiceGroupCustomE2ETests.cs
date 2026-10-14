using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// End-to-end tests of BitChoiceGroup over a custom item class on <c>/e2e/choice-group/custom</c>: every member of
/// BitChoiceGroupNameSelectors - mapped by property name and by selector - reaching the rendered item (text, value,
/// title, description, aria label, class, style, disabled, prefix, suffix, id, image, selected image, image size and
/// alt, icon and icon name, template) and the Index / IsSelected written back into the items, plus the behaviour that
/// has to hold for any item type: selection and OnClick with the custom item, the arrow keys, disabled, read-only,
/// one-way binding, variant / color / size, dynamic items and EditForm validation.
/// </summary>
[TestClass]
public class BitChoiceGroupCustomE2ETests : E2ETestBase
{
    private const string Url = "/e2e/choice-group/custom";

    private static readonly Regex AnyValue = new(".*");

    private ILocator Root(string id) => Page.Locator($"#{id}");
    private ILocator Input(string id, int index) => Page.Locator($"#{id} .bit-chg-inp").Nth(index);
    private ILocator ItemLabel(string id, int index) => Page.Locator($"#{id} .bit-chg-itl").Nth(index);
    private ILocator Item(string id, int index) => Page.Locator($"#{id} .bit-chg-icn").Nth(index);
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

    private async Task AssertDetailedItemsAsync(string id, string firstInputId)
    {
        await Expect(Text(id, 0)).ToHaveTextAsync("Custom A");
        var values = await Page.Locator($"#{id} .bit-chg-inp").EvaluateAllAsync<string[]>("els => els.map(e => e.value)");
        CollectionAssert.AreEqual(new[] { "A", "B", "C", "D" }, values);

        await Expect(Item(id, 0)).ToHaveAttributeAsync("title", "Tooltip A");
        await Expect(Item(id, 0)).ToHaveClassAsync(HasClass("custom-a"));
        await Expect(ItemLabel(id, 0)).ToContainTextAsync("$0 -");
        await Expect(Input(id, 0)).ToHaveAttributeAsync("id", firstInputId);
        await Expect(ItemLabel(id, 0)).ToHaveAttributeAsync("for", firstInputId);

        var description = Page.Locator($"#{id} .bit-chg-dsc");
        await Expect(description).ToHaveTextAsync("B summary");
        await Expect(Input(id, 1)).ToHaveAttributeAsync("aria-describedby", (await description.GetAttributeAsync("id"))!);
        await Expect(Item(id, 1)).ToHaveCSSAsync("padding-top", "6px");
        await Expect(Item(id, 1)).ToHaveCSSAsync("border-top-color", "rgb(0, 0, 255)");
        await Expect(ItemLabel(id, 1).Locator("span").Last).ToHaveTextAsync("Free");

        await Expect(Input(id, 2)).ToHaveAttributeAsync("aria-label", "Custom C, the third plan");
        await Expect(Input(id, 2)).ToHaveAccessibleNameAsync("Custom C, the third plan");

        await Expect(Input(id, 3)).ToHaveAttributeAsync("disabled", "");
        await Expect(Item(id, 3)).ToHaveClassAsync(HasClass("bit-chg-ids"));
    }

    [TestMethod]
    public async Task NamesByProperty_MapEveryPropertyOfACustomItemOntoTheRenderedItem()
    {
        await OpenAsync(Url);

        await AssertDetailedItemsAsync("cu-names", "names-plan-a");
        await Expect(Input("cu-names", 1)).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task NamesByProperty_WriteTheIndexAndTheSelectionBackIntoTheItems()
    {
        await OpenAsync(Url);

        await ItemLabel("cu-names", 2).ClickAsync();

        await Expect(Span("names-value")).ToHaveTextAsync("C");
        await Expect(Span("names-clicked")).ToHaveTextAsync("Custom C");
        await Expect(Span("names-changes")).ToHaveTextAsync("1");
        await Expect(Span("names-written")).ToHaveTextAsync("0:0,1:0,2:1,3:0");

        await ItemLabel("cu-names", 0).ClickAsync();
        await Expect(Span("names-written")).ToHaveTextAsync("0:1,1:0,2:0,3:0");

        // A value the page pushes in is written back by the group as it renders, after the page has rendered the
        // line that shows it; the next render of the page shows it.
        await Page.Locator("#btn-names-c").ClickAsync();
        await Expect(Input("cu-names", 2)).ToBeCheckedAsync();
        await Page.Locator("#btn-names-refresh").ClickAsync();
        await Expect(Span("names-written")).ToHaveTextAsync("0:0,1:0,2:1,3:0");

        // The disabled item can never be chosen.
        await ItemLabel("cu-names", 3).ClickAsync(new() { Force = true });
        await Expect(Span("names-value")).ToHaveTextAsync("C");
    }

    [TestMethod]
    public async Task NamesBySelector_MapEveryPropertyThroughLambdas()
    {
        await OpenAsync(Url);

        await AssertDetailedItemsAsync("cu-selectors", "selectors-plan-a");
        await Expect(Input("cu-selectors", 1)).ToBeCheckedAsync();

        await ItemLabel("cu-selectors", 0).ClickAsync();
        await Expect(Span("selectors-value")).ToHaveTextAsync("A");
    }

    [TestMethod]
    public async Task Media_ImagesAndIconsMapThroughNamesAndSelectors()
    {
        await OpenAsync(Url);

        var bar = Page.Locator("#cu-images img").Nth(0);
        var pie = Page.Locator("#cu-images img").Nth(1);
        await Expect(bar).ToHaveAttributeAsync("src", "samples/choicegroup-bar-selected.svg");
        await Expect(pie).ToHaveAttributeAsync("src", "samples/choicegroup-pie.svg");
        await Expect(bar).ToHaveAttributeAsync("alt", "alt for Bar image");
        await Expect(Page.Locator("#cu-images .bit-chg-iiw").Nth(0)).ToHaveCSSAsync("width", "32px");
        await Expect(Page.Locator("#cu-images .bit-chg-iiw").Nth(1)).ToHaveCSSAsync("height", "24px");

        await ItemLabel("cu-images", 1).ClickAsync();
        await Expect(pie).ToHaveAttributeAsync("src", "samples/choicegroup-pie-selected.svg");
        await Expect(bar).ToHaveAttributeAsync("src", "samples/choicegroup-bar.svg");

        await Expect(Page.Locator("#cu-images-selector img").Nth(1)).ToHaveAttributeAsync("src", "samples/choicegroup-pie-selected.svg");
        await Expect(Page.Locator("#cu-images-selector img").Nth(1)).ToHaveAttributeAsync("alt", "alt for Pie image");
        await Expect(Page.Locator("#cu-images-selector .bit-chg-iiw").Nth(1)).ToHaveCSSAsync("width", "24px");

        foreach (var id in new[] { "cu-icons", "cu-icons-selector" })
        {
            var icons = Page.Locator($"#{id} .bit-chg-ico");
            await Expect(icons.Nth(0)).ToHaveClassAsync(HasClass("bit-icon--CalendarDay"));
            await Expect(icons.Nth(1)).ToHaveClassAsync(HasClass("bi-calendar-week"));
            await Expect(icons.Nth(2)).ToHaveClassAsync(HasClass("fa-calendar"));
            await Expect(icons.Nth(2)).Not.ToHaveClassAsync(HasClass("bit-icon--Calendar"));
        }

        await Expect(Root("cu-icons-selector")).ToHaveClassAsync(HasClass("bit-chg-inl"));
        await Expect(ItemLabel("cu-icons", 0)).ToHaveClassAsync(HasClass("bit-chg-ili"));
    }

    [TestMethod]
    public async Task Templates_TheItemTemplateMapsThroughNamesAndSelectorsAndItemTemplateRendersTheCustomItem()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cu-template .custom-tpl").Nth(0)).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        await Expect(Page.Locator("#cu-template .bit-chg-rad")).ToHaveCountAsync(0);

        await Page.Locator("#cu-template .custom-tpl").Nth(1).ClickAsync();
        await Expect(Span("template-value")).ToHaveTextAsync("Week");
        await Expect(Item("cu-template", 1)).ToHaveClassAsync(HasClass("bit-chg-ich"));

        await Expect(Page.Locator("#cu-template-selector .custom-tpl").Nth(1)).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(Input("cu-template-selector", 1)).ToBeCheckedAsync();

        await Expect(Page.Locator("#cu-item-template .custom-item-tpl").Nth(0)).ToHaveTextAsync("CUSTOM A");
    }

    [TestMethod]
    public async Task Behaviour_TheArrowKeysDisabledReadOnlyAndOneWayHoldForCustomItems()
    {
        await OpenAsync(Url);

        await Input("cu-keyboard", 0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Input("cu-keyboard", 1)).ToBeCheckedAsync();
        await Expect(Input("cu-keyboard", 1)).ToBeFocusedAsync();

        await Expect(Root("cu-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("cu-disabled", 1)).ToHaveAttributeAsync("disabled", "");
        await Expect(Input("cu-disabled", 1)).ToHaveAttributeAsync("aria-disabled", "true");

        await ItemLabel("cu-readonly", 3).ClickAsync();
        await ItemLabel("cu-oneway", 2).ClickAsync();
        await ItemLabel("cu-keyboard", 3).ClickAsync();
        await Expect(Input("cu-keyboard", 3)).ToBeCheckedAsync();

        await Expect(Span("readonly-value")).ToHaveTextAsync("A");
        await Expect(Input("cu-readonly", 0)).ToBeCheckedAsync();
        await Expect(Root("cu-readonly")).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Input("cu-oneway", 0)).ToBeCheckedAsync();

        await Page.Locator("#btn-oneway-d").ClickAsync();
        await Expect(Input("cu-oneway", 3)).ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task Styled_VariantColorAndSizeApplyToCustomItems()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        await Expect(Root("cu-styled")).ToHaveClassAsync(HasClass("bit-chg-otl"));
        await Expect(Root("cu-styled")).ToHaveClassAsync(HasClass("bit-chg-suc"));
        await Expect(Root("cu-styled")).ToHaveClassAsync(HasClass("bit-chg-lg"));
        await Expect(ItemLabel("cu-styled", 1)).ToHaveCSSAsync("border-top-color", await ResolveColorAsync("var(--bit-clr-suc)"));

        var dot = await Page.Locator("#cu-styled .bit-chg-rad").Nth(1).EvaluateAsync<string>("el => getComputedStyle(el, '::after').backgroundColor");
        Assert.AreEqual(await ResolveColorAsync("var(--bit-clr-suc)"), dot);
    }

    [TestMethod]
    public async Task DynamicItems_TheWrittenIndexFollowsTheNewOrderAndTheSelectionSurvives()
    {
        await OpenAsync(Url);

        var indexes = Page.Locator("#cu-dynamic .dyn-index");
        await Expect(indexes).ToHaveTextAsync(new[] { "1.", "2.", "3." });

        await Page.Locator("#btn-dyn-add").ClickAsync();
        await Expect(indexes).ToHaveTextAsync(new[] { "1.", "2.", "3.", "4." });

        await Page.Locator("#btn-dyn-reverse").ClickAsync();
        await Expect(Text("cu-dynamic", 0)).ToHaveTextAsync("Custom 4");
        await Expect(indexes).ToHaveTextAsync(new[] { "1.", "2.", "3.", "4." });
        await Expect(Input("cu-dynamic", 3)).ToBeCheckedAsync();
        await Expect(Span("dynamic-value")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Validation_ARequiredCustomGroup_RefusesTheSubmitUntilAChoiceIsMade()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-submit").ClickAsync();

        await Expect(Span("submit-result")).ToHaveTextAsync("-1");
        await Expect(Page.Locator("#cu-error")).ToHaveTextAsync("Pick one");
        await Expect(Root("cu-validation")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Input("cu-validation", 0)).ToHaveAttributeAsync("aria-invalid", "true");

        await ItemLabel("cu-validation", 2).ClickAsync();
        await Expect(Root("cu-validation")).Not.ToHaveAttributeAsync("aria-invalid", AnyValue);

        await Page.Locator("#btn-submit").ClickAsync();
        await Expect(Span("submit-result")).ToHaveTextAsync("1");
    }
}
