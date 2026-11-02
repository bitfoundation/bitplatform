using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiModelSelector at /e2e/aimodelselector in a real browser: the picker over the registered catalog falling
/// back to its default and saying so, the models grouped under their families with their descriptions as tooltips,
/// picking with the pointer and with the keyboard (the arrows, Enter, Escape back on the field), a bound ModelId and one
/// naming no model, RequiredCapabilities, a catalog with nothing left to offer, deprecated and ungrouped models,
/// Disabled, the Styles/Classes of the dropdown, and right-to-left.
/// </summary>
[TestClass]
public class BitAiModelSelectorE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aimodelselector";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Field(string id) => Page.Locator($"#{id} .bit-drp-wrp");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    private async Task<ILocator> CalloutAsync(string id)
    {
        var fieldId = await Field(id).GetAttributeAsync("id");
        Assert.IsFalse(string.IsNullOrEmpty(fieldId), $"The field of #{id} carries no id.");

        return Page.Locator($"[id='{fieldId}-callout']");
    }

    private async Task<ILocator> OpenListAsync(string id, bool byKeyboard = false)
    {
        if (byKeyboard)
        {
            await Field(id).FocusAsync();
            await Page.Keyboard.PressAsync("ArrowDown");
        }
        else
        {
            await Field(id).ClickAsync();
        }

        var callout = await CalloutAsync(id);
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Field(id)).ToHaveAttributeAsync("aria-expanded", "true");

        // The focus an opening hands to the list arrives in a round trip of its own.
        await Page.WaitForFunctionAsync("id => document.getElementById(id)?.contains(document.activeElement)", await callout.GetAttributeAsync("id"));

        return callout;
    }

    private static ILocator Option(ILocator callout, string name) => callout.GetByRole(AriaRole.Option, new() { Name = name, Exact = true });

    private static ILocator Options(ILocator callout) => callout.Locator("[role=option]");

    private static ILocator Headers(ILocator callout) => callout.Locator(".bit-drp-ihd");

    [TestMethod]
    public async Task Unbound_ThePickerFallsBackToTheDefaultModel_AndRaisesTheChange()
    {
        await OpenAsync(Route);

        await Expect(Text("ms-basic-id")).ToHaveTextAsync("e2e-large");
        await Expect(Text("ms-basic-selected")).ToHaveTextAsync("E2E Large");
        await Expect(Text("ms-basic-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ms-basic .bit-drp-tcn")).ToHaveTextAsync("E2E Large");

        // The tooltip of the field is the description of the model it shows.
        await Expect(Field("ms-basic")).ToHaveAttributeAsync("title", "The general model.");
        await Expect(Root("ms-basic")).ToHaveClassAsync(HasClass("bit-aimds"));
        await Expect(Root("ms-basic")).ToHaveCSSAsync("display", "inline-flex");
    }

    [TestMethod]
    public async Task TheList_GroupsTheModelsUnderTheirFamilies_WithTheirDescriptionsAsTooltips()
    {
        await OpenAsync(Route);
        await Expect(Text("ms-basic-id")).ToHaveTextAsync("e2e-large");

        var callout = await OpenListAsync("ms-basic");

        await Expect(Headers(callout)).ToHaveTextAsync(["E2E Provider", "Local Runtime"]);
        await Expect(Options(callout).Locator(".bit-drp-itx")).ToHaveTextAsync(["E2E Large", "E2E Fast", "E2E Local"]);
        await Expect(Option(callout, "E2E Large")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Option(callout, "E2E Fast")).ToHaveAttributeAsync("title", "Cheaper and quicker.");
        await Expect(Option(callout, "E2E Local")).Not.ToHaveAttributeAsync("title", new Regex(".*"));

        // Each option is described by the header of its group.
        var headerId = await Headers(callout).Nth(1).GetAttributeAsync("id");
        await Expect(Option(callout, "E2E Local")).ToHaveAttributeAsync("aria-describedby", headerId!);
        await Expect(callout.Locator("[role=listbox]")).ToHaveAttributeAsync("aria-label", "Model");
    }

    [TestMethod]
    public async Task Pointer_PickingAModel_RaisesTheChangeWithTheModel_AndClosesTheList()
    {
        await OpenAsync(Route);
        await Expect(Text("ms-basic-count")).ToHaveTextAsync("1");

        var callout = await OpenListAsync("ms-basic");
        await Option(callout, "E2E Fast").ClickAsync();

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Text("ms-basic-id")).ToHaveTextAsync("e2e-fast");
        await Expect(Text("ms-basic-selected")).ToHaveTextAsync("E2E Fast");
        await Expect(Text("ms-basic-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#ms-basic .bit-drp-tcn")).ToHaveTextAsync("E2E Fast");
        await Expect(Field("ms-basic")).ToHaveAttributeAsync("title", "Cheaper and quicker.");
    }

    [TestMethod]
    public async Task Keyboard_TheArrowsOpenAndWalkTheList_EnterPicks_AndEscapeClosesBackOnTheField()
    {
        await OpenAsync(Route);
        await Expect(Text("ms-basic-id")).ToHaveTextAsync("e2e-large");

        // A named combobox in the tab order.
        var field = Page.Locator("#ms-basic").GetByRole(AriaRole.Combobox, new() { Name = "Model" });
        await Expect(field).ToHaveCountAsync(1);
        await Expect(field).ToHaveAttributeAsync("tabindex", "0");

        var callout = await OpenListAsync("ms-basic", byKeyboard: true);
        await Expect(Option(callout, "E2E Large")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "E2E Fast")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "E2E Local")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Text("ms-basic-id")).ToHaveTextAsync("e2e-local");
        await Expect(Text("ms-basic-selected")).ToHaveTextAsync("E2E Local");
        await Expect(Field("ms-basic")).ToBeFocusedAsync();

        // Escape closes the list and changes nothing.
        callout = await OpenListAsync("ms-basic", byKeyboard: true);
        await Expect(Option(callout, "E2E Local")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Option(callout, "E2E Fast")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Field("ms-basic")).ToBeFocusedAsync();
        await Expect(Text("ms-basic-id")).ToHaveTextAsync("e2e-local");
        await Expect(Text("ms-basic-count")).ToHaveTextAsync("2");

        // Tab moves on from the field.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#ms-basic-after")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Bound_TheModelIdIsShownAsGiven_AndAChangeOfItFollows()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#ms-bound .bit-drp-tcn")).ToHaveTextAsync("E2E Fast");
        await Expect(Text("ms-bound-id")).ToHaveTextAsync("e2e-fast");

        await Page.Locator("#ms-bound-set").ClickAsync();

        await Expect(Page.Locator("#ms-bound .bit-drp-tcn")).ToHaveTextAsync("E2E Local");
        await Expect(Text("ms-bound-id")).ToHaveTextAsync("e2e-local");

        // Neither the value it was given nor a change of it is a selection the picker made.
        await Expect(Text("ms-bound-count")).ToHaveTextAsync("0");
        var callout = await OpenListAsync("ms-bound");
        await Expect(Option(callout, "E2E Local")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod]
    public async Task AModelIdNamingNoModel_FallsBackToTheDefault_AndRaisesIt()
    {
        await OpenAsync(Route);

        await Expect(Text("ms-unknown-id")).ToHaveTextAsync("e2e-large");
        await Expect(Text("ms-unknown-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ms-unknown .bit-drp-tcn")).ToHaveTextAsync("E2E Large");
    }

    [TestMethod]
    public async Task RequiredCapabilities_OffersOnlyTheModelsThatHaveThem()
    {
        await OpenAsync(Route);

        var vision = await OpenListAsync("ms-vision");
        await Expect(Options(vision).Locator(".bit-drp-itx")).ToHaveTextAsync(["E2E Large"]);
        await Expect(Headers(vision)).ToHaveTextAsync(["E2E Provider"]);
        await Expect(Text("ms-vision-id")).ToHaveTextAsync("e2e-large");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(vision).ToBeHiddenAsync();

        // A model filtered out is not shown as the selection: the picker falls back and says so.
        await Expect(Text("ms-tools-id")).ToHaveTextAsync("e2e-large");
        var tools = await OpenListAsync("ms-tools");
        await Expect(Options(tools).Locator(".bit-drp-itx")).ToHaveTextAsync(["E2E Large", "E2E Fast"]);
    }

    [TestMethod]
    public async Task NoModelLeftToOffer_RendersNothing()
    {
        await OpenAsync(Route);

        // Rendered nothing in the prerender either, and nothing once interactive.
        await Expect(Text("ms-vision-id")).ToHaveTextAsync("e2e-large");
        await Expect(Page.Locator("#ms-none")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ms-none-host .bit-aimds")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ms-none-host [role=combobox]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Models_DeprecatedOnesAreHiddenUnlessShown_AndModelsWithNoFamilyGoUnderOther()
    {
        await OpenAsync(Route);

        await Expect(Text("ms-models-id")).ToHaveTextAsync("alpha");
        var models = await OpenListAsync("ms-models");
        await Expect(Headers(models)).ToHaveTextAsync(["Family A", "Other"]);
        await Expect(Options(models).Locator(".bit-drp-itx")).ToHaveTextAsync(["Alpha", "Loner"]);
        await Page.Keyboard.PressAsync("Escape");
        await Expect(models).ToBeHiddenAsync();

        await Expect(Page.Locator("#ms-deprecated .bit-drp-tcn")).ToHaveTextAsync("Old Model (deprecated)");
        var deprecated = await OpenListAsync("ms-deprecated");
        await Expect(Options(deprecated).Locator(".bit-drp-itx")).ToHaveTextAsync(["Alpha", "Old Model (deprecated)", "Loner"]);
        await Page.Keyboard.PressAsync("Escape");
        await Expect(deprecated).ToBeHiddenAsync();

        // With no family anywhere there is no header to draw.
        var flat = await OpenListAsync("ms-flat");
        await Expect(Headers(flat)).ToHaveCountAsync(0);
        await Expect(Options(flat).Locator(".bit-drp-itx")).ToHaveTextAsync(["One", "Two"]);
    }

    [TestMethod]
    public async Task Disabled_ThePickerCannotBeOpened()
    {
        await OpenAsync(Route);

        await Expect(Root("ms-dis")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Field("ms-dis")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Field("ms-dis")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Page.Locator("#ms-dis .bit-drp-tcn")).ToHaveTextAsync("E2E Fast");

        await Field("ms-dis").ClickAsync(new() { Force = true });
        await Field("ms-dis").EvaluateAsync("e => e.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }))");

        // An enabled picker opened after it proves the presses were answered.
        var other = await OpenListAsync("ms-basic");
        await Expect(other).ToBeVisibleAsync();
        await Expect(await CalloutAsync("ms-dis")).ToBeHiddenAsync();
        await Expect(Field("ms-dis")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Style_TheStylesAndClassesReachTheDropdown()
    {
        await OpenAsync(Route);

        await Expect(Root("ms-style")).ToHaveClassAsync(HasClass("ms-custom-root"));
        await Expect(Root("ms-style")).ToHaveCSSAsync("width", "260px");
        await Expect(Field("ms-style")).ToHaveCSSAsync("border-top-color", "rgb(1, 2, 3)");
    }

    [TestMethod]
    public async Task Rtl_ThePickerReadsRightToLeft_ExplicitOrInherited()
    {
        await OpenAsync(Route);

        await Expect(Root("ms-rtl")).ToHaveAttributeAsync("dir", "rtl");

        foreach (var id in new[] { "ms-rtl", "ms-rtl-inherited" })
        {
            await Expect(Field(id)).ToHaveCSSAsync("direction", "rtl");

            // The chevron sits at the inline end: the left.
            var text = await Page.Locator($"#{id} .bit-drp-tcn").RectAsync();
            var chevron = await Page.Locator($"#{id} .bit-drp-icn").RectAsync();
            Assert.IsTrue(chevron.Right <= text.Left + 1, $"#{id}: the chevron is on the left of the text.");
        }

        var callout = await OpenListAsync("ms-rtl");
        await Expect(callout.Locator("[role=listbox]")).ToHaveCSSAsync("direction", "rtl");
    }
}
