using System.Text.RegularExpressions;
using Bit.Borm.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Borm.Tests.E2E;

/// <summary>
/// The account-setup wizard: Next judges only the step on screen, Back keeps the answers, a conditional step comes and
/// goes with the answer that gates it, every step already reached is clickable in the stepper (both ways) while one
/// never reached is not, and Enter in a text box means Next rather than Submit.
/// </summary>
[TestClass]
public sealed class WizardTests : BormTest
{
    [TestMethod]
    public async Task Next_validates_the_current_step_before_advancing()
    {
        var page = await OpenAsync("/wizard");

        await FormButton(page, "Next").ClickAsync();

        // Still on the first step, its own fields complained about and the focus on the first of them.
        await ExpectActiveStepAsync(page, "Contact");
        await Expect(Errors(page, "FullName")).ToHaveTextAsync("This field is required.");
        await Expect(Errors(page, "Email")).ToHaveTextAsync("This field is required.");
        await Expect(Control(page, "FullName")).ToBeFocusedAsync();

        await EnterAsync(Control(page, "FullName"), "Ada Lovelace");
        await EnterAsync(Control(page, "Email"), "ada@example.com");
        await FormButton(page, "Next").ClickAsync();

        // The step changed under the buttons, so the focus goes to the new content and the change is announced.
        await ExpectActiveStepAsync(page, "Account type");
        await Expect(Step(page, "Contact")).ToHaveClassAsync(new Regex(@"\bis-done\b"));
        await Expect(page.Locator(".bit-borm-step-body")).ToBeFocusedAsync();
        await Expect(page.Locator(".bit-borm-stepper-nav [aria-live=polite]")).ToHaveTextAsync("Step 2 of 3");
    }

    [TestMethod]
    public async Task Back_returns_to_the_previous_step_with_its_answers_kept()
    {
        var page = await OpenAsync("/wizard");
        await CompleteContactStepAsync(page);
        await Expect(FormButton(page, "Back")).ToBeEnabledAsync();

        await FormButton(page, "Back").ClickAsync();

        await ExpectActiveStepAsync(page, "Contact");
        await Expect(Control(page, "FullName")).ToHaveValueAsync("Ada Lovelace");
        await Expect(Control(page, "Email")).ToHaveValueAsync("ada@example.com");
        await Expect(FormButton(page, "Back")).ToBeDisabledAsync();
        await Expect(page.Locator(".bit-borm-step-body")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Choosing_a_business_account_adds_the_company_step()
    {
        var page = await OpenAsync("/wizard");
        await Expect(page.Locator(".bit-borm-stepper__step")).ToHaveTextAsync([new Regex("Contact"), new Regex("Account type"), new Regex("Confirm")]);

        await CompleteContactStepAsync(page);
        await Control(page, "AccountKind").GetByLabel("Business", new() { Exact = true }).CheckAsync();

        await Expect(page.Locator(".bit-borm-stepper__step")).ToHaveTextAsync(
            [new Regex("Contact"), new Regex("Account type"), new Regex("Company"), new Regex("Confirm")]);
        await FormButton(page, "Next").ClickAsync();
        await ExpectActiveStepAsync(page, "Company");
        await Expect(page.Locator(".bit-borm-stepper-nav [aria-live=polite]")).ToHaveTextAsync("Step 3 of 4");

        // The company step gates on its own required field.
        await FormButton(page, "Next").ClickAsync();
        await Expect(Errors(page, "CompanyName")).ToHaveTextAsync("Required for business accounts.");
        await ExpectActiveStepAsync(page, "Company");
    }

    [TestMethod]
    public async Task Every_step_already_reached_is_clickable_both_ways_and_one_never_reached_is_not()
    {
        var page = await OpenAsync("/wizard");

        // At the start nothing but the current step has been reached, so nothing in the stepper is a button.
        await Expect(page.Locator(".bit-borm-stepper__link")).ToHaveCountAsync(0);

        await CompleteContactStepAsync(page);
        await Expect(StepLink(page, "Contact")).ToBeVisibleAsync();
        await Expect(StepLink(page, "Confirm")).ToHaveCountAsync(0);
        await FormButton(page, "Next").ClickAsync();
        await ExpectActiveStepAsync(page, "Confirm");

        // Back to the first step from the stepper...
        await StepLink(page, "Contact").ClickAsync();
        await ExpectActiveStepAsync(page, "Contact");
        await Expect(page.Locator(".bit-borm-step-body")).ToBeFocusedAsync();

        // ...and forwards again past a step, without pressing Next through the rest.
        await StepLink(page, "Confirm").ClickAsync();
        await ExpectActiveStepAsync(page, "Confirm");
        await Expect(Control(page, "AcceptTerms")).ToBeVisibleAsync();

        // The last step finishes the form.
        await Control(page, "AcceptTerms").CheckAsync();
        await FormButton(page, "Finish").ClickAsync();
        await Expect(Result(page)).ToContainTextAsync("All done");
        await Expect(Result(page)).ToContainTextAsync("\"AcceptTerms\": true");
    }

    [TestMethod]
    public async Task Enter_in_a_text_box_advances_to_the_next_step_instead_of_submitting()
    {
        var page = await OpenAsync("/wizard");

        await Control(page, "FullName").FillAsync("Ada Lovelace");
        await Control(page, "Email").FillAsync("ada@example.com");
        await Control(page, "Email").PressAsync("Enter");

        // Enter is how a person says they are done with a step, and on a step that is not the last one it has to
        // mean Next: the view turns the form's submit into Next for exactly this. This step has two text boxes,
        // and the browser only submits such a form on Enter when it has a submit button to press. Nor may it
        // finish the form: a real submit would have judged the steps not reached yet and complained about the terms.
        await ExpectActiveStepAsync(page, "Account type");
        await Expect(page.Locator(".bit-borm-message--error")).ToHaveCountAsync(0);
        await Expect(Result(page)).ToHaveCountAsync(0);

        // And Enter still keeps a step from being left with something wrong on it.
        await FormButton(page, "Back").ClickAsync();
        await Control(page, "Email").FillAsync("not an address");
        await Control(page, "Email").PressAsync("Enter");
        await ExpectActiveStepAsync(page, "Contact");
        await Expect(Errors(page, "Email")).ToHaveCountAsync(1);
        await Expect(Control(page, "Email")).ToBeFocusedAsync();
    }

    private static ILocator Step(IPage page, string title) =>
        page.Locator(".bit-borm-stepper__step", new() { Has = page.Locator(".bit-borm-stepper__label", new() { HasTextRegex = new Regex($"^{Regex.Escape(title)}$") }) });

    private static ILocator StepLink(IPage page, string title) => Step(page, title).Locator("button.bit-borm-stepper__link");

    private static async Task ExpectActiveStepAsync(IPage page, string title)
    {
        await Expect(page.Locator(".bit-borm-stepper__step.is-active .bit-borm-stepper__label")).ToHaveTextAsync(title);
        await Expect(Step(page, title)).ToHaveAttributeAsync("aria-current", "step");
    }

    private static async Task CompleteContactStepAsync(IPage page)
    {
        await EnterAsync(Control(page, "FullName"), "Ada Lovelace");
        await EnterAsync(Control(page, "Email"), "ada@example.com");
        await FormButton(page, "Next").ClickAsync();
        await ExpectActiveStepAsync(page, "Account type");
    }
}
