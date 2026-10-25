namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// BitDropdown in an EditForm (<c>/e2e/dropdown/validation</c>): an invalid submit marks a required single select and a
/// multi select below its MinLength invalid - bit-inv, aria-invalid, the error border and the ValidationMessage - a
/// pick clears its own error at once, a multi select past its MaxLength is invalid until a selection is taken back, a
/// NoValidate dropdown is never marked, and a form whose fields all hold valid values submits.
/// </summary>
[TestClass]
public class BitDropdownValidationE2ETests : BitDropdownE2ETestBase
{
    private const string Url = "/e2e/dropdown/validation";

    [TestMethod]
    public async Task InvalidSubmit_MarksEveryValidatedDropdownInvalid_WithItsMessage()
    {
        await OpenAsync(Url);

        var validBorder = await StyleAsync(Trigger("dd-val-cat"), "border-top-color");

        await Page.Locator("#btn-submit").ClickAsync();

        await Expect(Page.Locator("#invalid-submits")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#valid-submits")).ToHaveTextAsync("0");

        await Expect(Root("dd-val-cat")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Trigger("dd-val-cat")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#msg-cat .validation-message")).ToHaveTextAsync("Pick a category.");
        await Expect(Trigger("dd-val-cat")).Not.ToHaveCSSAsync("border-top-color", validBorder);

        await Expect(Root("dd-val-prod")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Trigger("dd-val-prod")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#msg-prod .validation-message")).ToHaveTextAsync("Pick at least 1 product.");

        await Expect(Root("dd-val-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Trigger("dd-val-novalidate")).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [TestMethod]
    public async Task APick_ClearsItsOwnErrorAtOnce()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-submit").ClickAsync();
        await Expect(Root("dd-val-cat")).ToHaveClassAsync(HasClass("bit-inv"));

        var callout = await OpenDropdownAsync("dd-val-cat");
        await Option(callout, "Banana").ClickAsync();

        await Expect(Root("dd-val-cat")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Trigger("dd-val-cat")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#msg-cat .validation-message")).ToHaveCountAsync(0);

        await Expect(Root("dd-val-prod")).ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task MaxLength_TooManySelections_MarkTheMultiSelectInvalidUntilOneIsTakenBack()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-val-prod");

        await Option(callout, "Apple").ClickAsync();
        await Expect(Root("dd-val-prod")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Option(callout, "Banana").ClickAsync();
        await Option(callout, "Grape").ClickAsync();

        await Expect(Root("dd-val-prod")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Trigger("dd-val-prod")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#msg-prod .validation-message")).ToHaveTextAsync("Pick at most 2 products.");

        await Option(callout, "Grape").ClickAsync();

        await Expect(Root("dd-val-prod")).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Page.Locator("#msg-prod .validation-message")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ValidSubmit_GoesThroughOnceEveryFieldHoldsAValidValue()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-val-cat");
        await Option(callout, "Apple").ClickAsync();
        await Expect(callout).ToBeHiddenAsync();

        callout = await OpenDropdownAsync("dd-val-prod");
        await Option(callout, "Grape").ClickAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        callout = await OpenDropdownAsync("dd-val-novalidate");
        await Option(callout, "Banana").ClickAsync();
        await Expect(callout).ToBeHiddenAsync();

        await Page.Locator("#btn-submit").ClickAsync();

        await Expect(Page.Locator("#valid-submits")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#invalid-submits")).ToHaveTextAsync("0");
        await Expect(Page.Locator(".validation-message")).ToHaveCountAsync(0);
    }
}
