using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// End-to-end tests of BitChoiceGroup over BitChoiceGroupOption children on <c>/e2e/choice-group/option</c>: options
/// written as ChildContent and through the Options parameter, every parameter of an option reaching the rendered item
/// (text, value, title, class, style, prefix, suffix, id, description, aria label, disabled, image, selected image,
/// image size and alt, icon and icon name, template) and its Index, ItemLabelTemplate, selection and OnClick with the
/// option, the arrow keys, disabled, read-only, one-way binding, variant / color / size, dynamic options with @key,
/// AutoReorderOptions against a conditionally rendered option (and the order without it), EditForm validation and
/// unmounting a group whose options unregister themselves.
/// </summary>
[TestClass]
public class BitChoiceGroupOptionE2ETests : E2ETestBase
{
    private const string Url = "/e2e/choice-group/option";

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

    [TestMethod]
    public async Task Basic_OptionsAsChildContentOrThroughTheOptionsParameter_AreSelectable()
    {
        await OpenAsync(Url);

        await Expect(Root("op-basic")).ToHaveAttributeAsync("role", "radiogroup");
        await Expect(Input("op-basic", 1)).ToBeCheckedAsync();

        await ItemLabel("op-basic", 2).ClickAsync();

        await Expect(Span("basic-value")).ToHaveTextAsync("C");
        await Expect(Span("basic-clicked")).ToHaveTextAsync("Option C");
        await Expect(Item("op-basic", 2)).ToHaveClassAsync(HasClass("bit-chg-ich"));

        await Expect(Input("op-options-param", 1)).ToBeCheckedAsync();
        await ItemLabel("op-options-param", 0).ClickAsync();
        await Expect(Input("op-options-param", 0)).ToBeCheckedAsync();

        await Expect(Page.Locator("#op-nocircle .bit-chg-rad")).ToHaveCountAsync(0);
        await Expect(Root("op-nocircle")).ToHaveClassAsync(HasClass("bit-chg-ncr"));
    }

    [TestMethod]
    public async Task OptionParameters_EachParameterReachesTheRenderedItem()
    {
        await OpenAsync(Url);

        const string id = "op-params";
        var values = await Page.Locator($"#{id} .bit-chg-inp").EvaluateAllAsync<string[]>("els => els.map(e => e.value)");
        CollectionAssert.AreEqual(new[] { "A", "B", "C", "D" }, values);
        await Expect(Text(id, 0)).ToHaveTextAsync("Option A");

        await Expect(Item(id, 0)).ToHaveAttributeAsync("title", "Tooltip A");
        await Expect(Item(id, 0)).ToHaveClassAsync(HasClass("custom-a"));
        await Expect(ItemLabel(id, 0)).ToContainTextAsync("$0 -");
        await Expect(Input(id, 0)).ToHaveAttributeAsync("id", "option-a");
        await Expect(ItemLabel(id, 0)).ToHaveAttributeAsync("for", "option-a");

        var description = Page.Locator($"#{id} .bit-chg-dsc");
        await Expect(description).ToHaveTextAsync("B description");
        await Expect(Input(id, 1)).ToHaveAttributeAsync("aria-describedby", (await description.GetAttributeAsync("id"))!);
        await Expect(Input(id, 1)).ToHaveAccessibleNameAsync("Option B Free");
        await Expect(Item(id, 1)).ToHaveCSSAsync("padding-top", "6px");
        await Expect(Item(id, 1)).ToHaveCSSAsync("border-top-color", "rgb(0, 0, 255)");
        await Expect(ItemLabel(id, 1).Locator("span").Last).ToHaveTextAsync("Free");

        await Expect(Input(id, 2)).ToHaveAttributeAsync("aria-label", "Option C, the third one");

        await Expect(Input(id, 3)).ToHaveAttributeAsync("disabled", "");
        await Expect(Item(id, 3)).ToHaveClassAsync(HasClass("bit-chg-ids"));
        await ItemLabel(id, 3).ClickAsync(new() { Force = true });
        await ItemLabel(id, 2).ClickAsync();
        await Expect(Span("params-value")).ToHaveTextAsync("C");
    }

    [TestMethod]
    public async Task Media_ImageAndIconParametersRenderTiles()
    {
        await OpenAsync(Url);

        var bar = Page.Locator("#op-images img").Nth(0);
        var pie = Page.Locator("#op-images img").Nth(1);
        await Expect(bar).ToHaveAttributeAsync("src", "samples/choicegroup-bar-selected.svg");
        await Expect(bar).ToHaveAttributeAsync("alt", "alt for Bar image");
        await Expect(pie).ToHaveAttributeAsync("src", "samples/choicegroup-pie.svg");
        await Expect(Page.Locator("#op-images .bit-chg-iiw").Nth(1)).ToHaveCSSAsync("width", "24px");

        await ItemLabel("op-images", 1).ClickAsync();
        await Expect(pie).ToHaveAttributeAsync("src", "samples/choicegroup-pie-selected.svg");
        await Expect(bar).ToHaveAttributeAsync("src", "samples/choicegroup-bar.svg");

        await Expect(Page.Locator("#op-icons .bit-chg-ico").Nth(0)).ToHaveClassAsync(HasClass("bit-icon--CalendarDay"));
        await Expect(Page.Locator("#op-icons .bit-chg-ico").Nth(1)).ToHaveClassAsync(HasClass("bi-calendar-week"));
        await Expect(Item("op-icons", 2)).ToHaveClassAsync(HasClass("bit-chg-ids"));
    }

    [TestMethod]
    public async Task Templates_AnOptionsTemplateAndItemLabelTemplate_RenderTheOption()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#op-template .option-tpl").Nth(0)).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        await Expect(Page.Locator("#op-template .bit-chg-rad")).ToHaveCountAsync(0);
        await Page.Locator("#op-template .option-tpl").Nth(1).ClickAsync();
        await Expect(Span("template-value")).ToHaveTextAsync("Week");

        var labels = Page.Locator("#op-label-template .option-label-tpl");
        await Expect(labels).ToHaveTextAsync(new[] { "Day (0)", "Week (1)" });
        await Expect(Page.Locator("#op-label-template .bit-chg-rad")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task Behaviour_TheArrowKeysDisabledReadOnlyOneWayAndStylingHoldForOptions()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        await Input("op-keyboard", 1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Input("op-keyboard", 3)).ToBeCheckedAsync();

        await Expect(Root("op-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Input("op-disabled", 0)).ToHaveAttributeAsync("disabled", "");

        await ItemLabel("op-readonly", 1).ClickAsync();
        await ItemLabel("op-oneway", 1).ClickAsync();
        await ItemLabel("op-keyboard", 0).ClickAsync();
        await Expect(Input("op-keyboard", 0)).ToBeCheckedAsync();

        await Expect(Span("readonly-value")).ToHaveTextAsync("A");
        await Expect(Input("op-readonly", 0)).ToBeCheckedAsync();
        await Expect(Input("op-oneway", 0)).ToBeCheckedAsync();
        await Page.Locator("#btn-oneway-b").ClickAsync();
        await Expect(Input("op-oneway", 1)).ToBeCheckedAsync();

        await Expect(Root("op-styled")).ToHaveClassAsync(HasClass("bit-chg-fil"));
        await Expect(Root("op-styled")).ToHaveClassAsync(HasClass("bit-chg-wrn"));
        await Expect(Root("op-styled")).ToHaveClassAsync(HasClass("bit-chg-sm"));
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(ItemLabel("op-styled", 1)).ToHaveCSSAsync("border-top-color", await ResolveColorAsync("var(--bit-clr-wrn)"));
    }

    [TestMethod]
    public async Task DynamicOptions_AddRemoveAndReverseKeepIndexesAndTheSelection()
    {
        await OpenAsync(Url);

        var indexes = Page.Locator("#op-dynamic .dyn-index");
        await Expect(indexes).ToHaveTextAsync(new[] { "1.", "2.", "3." });
        await Expect(Input("op-dynamic", 0)).ToBeCheckedAsync();

        await Page.Locator("#btn-dyn-add").ClickAsync();
        await Expect(indexes).ToHaveTextAsync(new[] { "1.", "2.", "3.", "4." });

        await Page.Locator("#btn-dyn-reverse").ClickAsync();
        await Expect(Text("op-dynamic", 0)).ToHaveTextAsync("Option 4");
        await Expect(indexes).ToHaveTextAsync(new[] { "1.", "2.", "3.", "4." });
        await Expect(Input("op-dynamic", 3)).ToBeCheckedAsync();

        await Page.Locator("#btn-dyn-remove").ClickAsync();
        await Expect(Page.Locator("#op-dynamic .bit-chg-icn")).ToHaveCountAsync(3);
        await Expect(Page.Locator("#op-dynamic .bit-chg-inp:checked")).ToHaveCountAsync(0);

        await ItemLabel("op-dynamic", 1).ClickAsync();
        await Expect(Span("dynamic-value")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task AutoReorderOptions_KeepsTheIndexesInTheOrderOfTheMarkup()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#op-reorder .reorder-index")).ToHaveTextAsync(new[] { "0", "1" });

        await Page.Locator("#btn-toggle-middle").ClickAsync();

        await Expect(Text("op-reorder", 1)).ToHaveTextAsync("Middle");
        await Expect(Page.Locator("#op-reorder .reorder-index")).ToHaveTextAsync(new[] { "0", "1", "2" });

        // Without it the option rendered later registers at the end, whatever its place in the markup.
        await Expect(Text("op-no-reorder", 1)).ToHaveTextAsync("Middle");
        await Expect(Page.Locator("#op-no-reorder .reorder-index")).ToHaveTextAsync(new[] { "0", "2", "1" });

        await ItemLabel("op-reorder", 1).ClickAsync();
        await Expect(Input("op-reorder", 1)).ToBeCheckedAsync();

        await Page.Locator("#btn-toggle-middle").ClickAsync();
        await Expect(Page.Locator("#op-reorder .reorder-index")).ToHaveTextAsync(new[] { "0", "1" });
        await Expect(Page.Locator("#op-reorder .bit-chg-inp:checked")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Validation_ARequiredOptionGroup_RefusesTheSubmitUntilAChoiceIsMade()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-submit").ClickAsync();

        await Expect(Span("submit-result")).ToHaveTextAsync("-1");
        await Expect(Page.Locator("#op-error")).ToHaveTextAsync("Pick one");
        await Expect(Root("op-validation")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Input("op-validation", 1)).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("op-validation")).ToHaveClassAsync(HasClass("bit-inv"));

        await ItemLabel("op-validation", 1).ClickAsync();
        await Expect(Root("op-validation")).Not.ToHaveAttributeAsync("aria-invalid", AnyValue);

        await Page.Locator("#btn-submit").ClickAsync();
        await Expect(Span("submit-result")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Unmount_AGroupWhoseOptionsUnregisterThemselves_LeavesNoError()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Root("op-unmount")).ToHaveCountAsync(0);

        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Input("op-unmount", 0)).ToBeCheckedAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
