namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// BitFocusOnInvalidSubmit in a real page (/e2e/focusoninvalidsubmit): a submit that fails moves the focus to the first
/// invalid field of its form - past a valid field ahead of it, into a choice group (its checked radio when it has one, its
/// first otherwise) and a rating (the item the Tab key lands on), past a field in a collapsed part of the form, and never
/// into another form - while a submit that passes, and a field validated as it is typed in, leave the focus where it is.
/// </summary>
[TestClass]
public class BitFocusOnInvalidSubmitE2ETests : E2ETestBase
{
    private const string Route = "/e2e/focusoninvalidsubmit";

    private ILocator TextField(string id) => Page.Locator($"#{id} .bit-tfl-inp");

    [TestMethod]
    public async Task Fields_AFailedSubmitFocusesTheFirstInvalidField_PastAValidOne()
    {
        await OpenAsync(Route);

        await Page.Locator("#fields-submit").ClickAsync();

        // The name is filled in; the email is the first field that fails, the city the second.
        await Expect(TextField("tf-email")).ToBeFocusedAsync();
        await Expect(TextField("tf-email")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(TextField("tf-city")).ToHaveAttributeAsync("aria-invalid", "true");

        // The field the focus lands on is described by its message, which is what reads it out.
        var error = Page.Locator("#tf-email .bit-tfl-erm");
        await Expect(error).ToHaveTextAsync("The Email field is required.");
        Assert.Contains((await error.GetAttributeAsync("id"))!, (await TextField("tf-email").GetAttributeAsync("aria-describedby"))!);

        // Once the email is fixed, the next failed submit moves on to the city.
        await TextField("tf-email").FillAsync("ada@example.com");
        await TextField("tf-email").BlurAsync();
        await Page.Locator("#fields-submit").ClickAsync();

        await Expect(TextField("tf-city")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task GroupNone_AChoiceGroupWithNoRadioCheckedTakesTheFocusOnItsFirstRadio()
    {
        await OpenAsync(Route);

        await Page.Locator("#group-none-submit").ClickAsync();

        // The group carries the state, but a group cannot take the focus: its first radio does.
        await Expect(Page.Locator("#cg-none")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#cg-none .bit-chg-inp").Nth(0)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task GroupChecked_AChoiceGroupWithARejectedRadioTakesTheFocusOnThatRadio()
    {
        await OpenAsync(Route);

        await Page.Locator("#group-checked-submit").ClickAsync();

        // The radio the Tab key lands on: the checked one, the second here, rather than the first.
        await Expect(Page.Locator("#cg-checked .bit-chg-inp").Nth(1)).ToBeCheckedAsync();
        await Expect(Page.Locator("#cg-checked .bit-chg-inp").Nth(1)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Rating_ARatingTakesTheFocusOnTheItemTheTabKeyLandsOn()
    {
        await OpenAsync(Route);

        await Page.Locator("#rating-submit").ClickAsync();

        await Expect(Page.Locator("#rtg-score")).ToHaveAttributeAsync("aria-invalid", "true");

        // The rating's root holds the state; the one item in the tab sequence is where the focus goes.
        var focused = Page.Locator("#rtg-score :focus");
        await Expect(focused).ToHaveCountAsync(1);
        Assert.AreNotEqual("-1", await focused.GetAttributeAsync("tabindex"));
    }

    [TestMethod]
    public async Task Hidden_AFieldInACollapsedPartOfTheFormIsPassedOver()
    {
        await OpenAsync(Route);

        await Page.Locator("#hidden-submit").ClickAsync();

        // The collapsed field fails first in document order, but nothing in it can take the focus.
        await Expect(Page.Locator("#hidden-field")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(TextField("tf-visible")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Valid_ASubmitThatPassesLeavesTheFocusOnTheSubmitButton()
    {
        await OpenAsync(Route);

        // From the keyboard, so the focus is on the button in every engine (a click leaves it where it was in WebKit).
        var submit = Page.Locator("#valid-submit");
        await submit.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#valid-count")).ToHaveTextAsync("1");
        await Expect(submit).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Typing_AFieldValidatedAsItIsTypedInKeepsTheFocus()
    {
        await OpenAsync(Route);

        var typed = TextField("tf-typed");
        await typed.FocusAsync();
        await Page.Keyboard.TypeAsync("a");
        await Page.Keyboard.PressAsync("Backspace");

        // Emptied, the field fails as it is typed in - and the first field, which nothing has validated, does not take
        // the focus from it.
        await Expect(typed).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(typed).ToBeFocusedAsync();
        await Expect(TextField("tf-first")).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [TestMethod]
    public async Task TwoForms_EachFormLooksInItselfOnly()
    {
        await OpenAsync(Route);

        // Form A has no helper: its failed submit marks its field and leaves the focus alone.
        await Page.Locator("#form-a-submit").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(TextField("tf-form-a")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#form-a-submit")).ToBeFocusedAsync();

        // Form B's helper skips form A's invalid field, ahead of its own in the document.
        await Page.Locator("#form-b-submit").ClickAsync();

        await Expect(TextField("tf-form-b")).ToBeFocusedAsync();
    }
}
