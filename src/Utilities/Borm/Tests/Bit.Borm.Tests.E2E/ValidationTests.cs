using System.Text.RegularExpressions;
using Bit.Borm.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Borm.Tests.E2E;

/// <summary>
/// Validation as a person meets it: a failed submit marks the fields, says in a sentence what is wrong, and takes the
/// focus to the problem - the first field, or the error summary whose links lead to each one and which never pulls the
/// focus away while the user is editing; fixing a value takes its complaint away; a rule that reads two fields is
/// settled from either side; an async rule answers after its lookup; values are tidied rather than reported; and the
/// FluentValidation and IValidatableObject integrations report on the fields they name.
/// </summary>
[TestClass]
public sealed class ValidationTests : BormTest
{
    [TestMethod]
    public async Task Submitting_an_empty_form_marks_the_required_fields_and_focuses_the_first_of_them()
    {
        var page = await OpenAsync("/builder");
        var fullName = Control(page, "FullName");

        await SubmitButton(page).ClickAsync();

        // Every required field says so, in words, in the attribute a screen reader reads and in the list the
        // control points aria-describedby at.
        await Expect(Errors(page, "FullName")).ToHaveTextAsync("This field is required.");
        await Expect(fullName).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(fullName).ToHaveAttributeAsync("aria-describedby", new Regex(@"\bborm_FullName_error\b"));
        await Expect(Control(page, "Email")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Errors(page, "AcceptTerms")).ToHaveTextAsync("You must accept the terms to continue.");

        // FocusFirstError: the caret is taken to the first problem rather than left on the submit button.
        await Expect(fullName).ToBeFocusedAsync();

        // The company fields are hidden for a personal account, so their rules did not run at all.
        await Expect(Control(page, "CompanyName")).ToHaveCountAsync(0);
        await Expect(page.Locator(".bit-borm-message--error")).ToHaveCountAsync(3);
        await Expect(Result(page)).ToHaveCountAsync(0);

        // The stylesheet paints the state: an invalid control's border and its label take the error colour. The
        // border is read off the email box, which does not have the focus: the focused one wears the focus ring's
        // border instead, and its label is what still says it is wrong.
        await Expect(fullName).ToHaveClassAsync(new Regex(@"\bbit-borm-input--invalid\b"));
        Assert.AreEqual(ErrorColour, await ComputedAsync(Control(page, "Email"), "border-top-color"), "the invalid control's border");
        Assert.AreEqual(ErrorColour, await ComputedAsync(page.Locator("#borm_FullName_label"), "color"), "the invalid field's label");

        // Fixing the value takes the complaint away as soon as the value is committed, and with it every trace of it.
        await EnterAsync(fullName, "Ada Lovelace");
        await Expect(Errors(page, "FullName")).ToHaveCountAsync(0);
        await Expect(fullName).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(fullName).Not.ToHaveClassAsync(new Regex(@"\bbit-borm-input--invalid\b"));
        Assert.AreNotEqual(ErrorColour, await ComputedAsync(fullName, "border-top-color"), "the fixed control is still painted as invalid");
        await Expect(Control(page, "Email")).ToHaveAttributeAsync("aria-invalid", "true");
    }

    [TestMethod]
    public async Task With_the_error_summary_on_a_failed_submit_focuses_the_summary_whose_links_reach_each_field()
    {
        var page = await OpenAsync("/array");

        await SubmitButton(page).ClickAsync();

        // The summary takes the focus itself, and lists every problem with the field it belongs to.
        var summary = Summary(page);
        await Expect(summary).ToBeFocusedAsync();
        await Expect(summary.Locator(".bit-borm-summary__title")).ToHaveTextAsync("There are 2 problems with this form:");
        var links = summary.Locator("a");
        await Expect(links).ToHaveTextAsync(["Customer: This field is required.", "Lines: Add at least one line."]);

        // Each link moves the focus to its control - and stays on this page: a bare "#id" would resolve against
        // <base href="/"> and take the user, and everything they typed, to the home page.
        await links.Nth(0).ClickAsync();
        await Expect(Control(page, "Customer")).ToBeFocusedAsync();
        await links.Nth(1).ClickAsync();
        await Expect(Control(page, "Lines")).ToBeFocusedAsync();
        Assert.AreEqual(BaseUrl + "/array", page.Url.Split('#')[0], "a summary link navigated away from the form");
        await Expect(Form(page)).ToBeVisibleAsync();

        // Fixing one problem shortens the list.
        await EnterAsync(Control(page, "Customer"), "Northwind Traders");
        await Expect(summary.Locator(".bit-borm-summary__title")).ToHaveTextAsync("There is a problem with this form:");
        await Expect(links).ToHaveTextAsync(["Lines: Add at least one line."]);
    }

    [TestMethod]
    public async Task The_error_summary_takes_the_focus_after_a_failed_submit_and_never_while_the_user_is_editing()
    {
        var page = await OpenAsync("/validation");
        await EnterAsync(Control(page, "Email"), "ada@example.com");
        await EnterAsync(Control(page, "Username"), "ada-lovelace-1815");
        await EnterAsync(Control(page, "Password"), "analytical");
        await EnterAsync(Control(page, "ConfirmPassword"), "analytical");
        await Expect(page.Locator(".demo-status")).ToContainTextAsync("checking username: no");
        await SubmitButton(page).ClickAsync();
        await Expect(Result(page)).ToContainTextAsync("Account created");

        // The account is edited after it was saved: the email is emptied and the user tabs on to the next field.
        // The summary reappears to list the new problem, but the focus is where the user put it - a summary that
        // pulled it away would take them out of the field they are typing in, on a submit they never made.
        await Control(page, "Email").FocusAsync();
        await page.Keyboard.PressAsync("Control+A");
        await page.Keyboard.PressAsync("Backspace");
        await page.Keyboard.PressAsync("Tab");

        await Expect(Summary(page)).ToBeVisibleAsync();
        await Expect(Errors(page, "Email")).ToHaveCountAsync(1);
        await Expect(Control(page, "Username")).ToBeFocusedAsync(new() { Timeout = 5_000 });

        // The next failed submit is what takes the focus to the summary again.
        await SubmitButton(page).ClickAsync();
        await Expect(Summary(page)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Correcting_the_other_side_of_a_cross_field_rule_clears_its_message()
    {
        var page = await OpenAsync("/dependent-fields");

        // [Compare] lives on the confirmation, so only a change to the confirmation would run it...
        await EnterAsync(Control(page, "Email"), "ada@example.com");
        await EnterAsync(Control(page, "ConfirmEmail"), "ada@example.org");
        await Expect(Errors(page, "ConfirmEmail")).ToHaveTextAsync("The two addresses do not match.");

        // ...yet correcting the first box settles the complaint under the second.
        await EnterAsync(Control(page, "Email"), "ada@example.org");
        await Expect(Errors(page, "ConfirmEmail")).ToHaveCountAsync(0);
        await Expect(Control(page, "ConfirmEmail")).Not.ToHaveAttributeAsync("aria-invalid", "true");

        // The same for a rule written by hand with RevalidateOn: four travellers to a room at most.
        await EnterAsync(Control(page, "Travellers"), "9");
        await EnterAsync(Control(page, "Rooms"), "2");
        await Expect(Errors(page, "Rooms")).ToHaveTextAsync("That is not enough rooms for this many travellers.");
        await EnterAsync(Control(page, "Travellers"), "8");
        await Expect(Errors(page, "Rooms")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task An_attribute_rule_without_a_message_of_its_own_reads_as_a_sentence()
    {
        // The quickstart's schema is generated from the type alone, so the email rule comes from [EmailAddress],
        // which carries no message of its own: what the user reads has to be a finished sentence, not the
        // attribute's format string with its placeholder still in it.
        var page = await OpenAsync("/quickstart");

        await EnterAsync(Control(page, "Email"), "not an address");

        var message = Errors(page, "Email");
        await Expect(message).ToHaveCountAsync(1);
        await Expect(message).ToHaveTextAsync(new Regex("e-?mail", RegexOptions.IgnoreCase));
        await Expect(message).Not.ToContainTextAsync("{0}");
    }

    [TestMethod]
    public async Task A_debounced_async_rule_reports_a_taken_username_once_the_lookup_answers()
    {
        var page = await OpenAsync("/validation");
        var username = Control(page, "Username");

        // Typed one key at a time, as a person types: the field is debounced, so the lookup runs once for the
        // pause rather than once per character.
        await username.PressSequentiallyAsync("ada");
        await username.BlurAsync();

        await Expect(Errors(page, "Username")).ToHaveTextAsync("That username is taken.");
        await Expect(username).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.Locator(".demo-status")).ToContainTextAsync("checking username: no");

        await username.FillAsync("ada-lovelace-1815");
        await username.BlurAsync();
        await Expect(Errors(page, "Username")).ToHaveCountAsync(0);
        await Expect(username).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [TestMethod]
    public async Task Reset_restores_the_starting_values_and_clears_the_errors()
    {
        var page = await OpenAsync("/validation");

        await EnterAsync(Control(page, "Email"), "not an address");
        await SubmitButton(page).ClickAsync();
        await Expect(Summary(page)).ToBeVisibleAsync();
        await Expect(Errors(page, "Email")).ToHaveCountAsync(1);

        await FormButton(page, "Reset").ClickAsync();

        await Expect(Control(page, "Email")).ToHaveValueAsync("");
        await Expect(Summary(page)).ToHaveCountAsync(0);
        await Expect(page.Locator(".bit-borm-message--error")).ToHaveCountAsync(0);
        await Expect(Control(page, "Email")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.Locator(".demo-status")).ToContainTextAsync("dirty: False");
    }

    [TestMethod]
    public async Task Values_are_tidied_on_the_way_in_and_a_date_outside_its_window_is_reported()
    {
        var page = await OpenAsync("/settled-answers");
        var model = page.Locator(".demo-readout pre");

        // Normalize and Trim fix the value rather than complain about it, in the box and in the model alike.
        await EnterAsync(Control(page, "Reference"), "  bk-2048 ");
        await Expect(Control(page, "Reference")).ToHaveValueAsync("BK-2048");
        await EnterAsync(Control(page, "ContactEmail"), "   ada@example.com  ");
        await Expect(Control(page, "ContactEmail")).ToHaveValueAsync("ada@example.com");
        await Expect(model).ToContainTextAsync("\"Reference\": \"BK-2048\"");
        await Expect(model).ToContainTextAsync("\"ContactEmail\": \"ada@example.com\"");
        await Expect(page.Locator(".bit-borm-message--error")).ToHaveCountAsync(0);

        // The arrival window is enforced, not merely offered to the date picker.
        await AssumeInputTypeAsync(page, "date");
        await EnterAsync(Control(page, "Arrival"), DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        await Expect(Control(page, "Arrival")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Errors(page, "Arrival")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task A_FluentValidation_validator_judges_the_bound_model_including_its_cross_field_rule()
    {
        var page = await OpenAsync("/fluent-validation");

        await EnterAsync(Control(page, "FirstName"), "Grace");
        await EnterAsync(Control(page, "LastName"), "Hopper");
        await EnterAsync(Control(page, "Email"), "grace@example.com");
        await EnterAsync(Control(page, "YearsExperience"), "6");
        await EnterAsync(Control(page, "DesiredSalary"), "90000");
        await EnterAsync(Control(page, "Motivation"), "I like ships.");
        await SubmitButton(page).ClickAsync();

        // Six years of experience makes the motivation rule apply, and its failure lands on the motivation box.
        await Expect(Errors(page, "Motivation")).ToHaveTextAsync("Candidates with 5+ years should write at least 20 characters.");
        await Expect(Control(page, "Motivation")).ToBeFocusedAsync();
        await Expect(page.Locator(".bit-borm-message--error")).ToHaveCountAsync(1);
        await Expect(Result(page)).ToHaveCountAsync(0);

        await EnterAsync(Control(page, "Motivation"), "I want to build the first compiler for this team.");
        await Expect(Errors(page, "Motivation")).ToHaveCountAsync(0);
        await SubmitButton(page).ClickAsync();
        await Expect(Result(page)).ToContainTextAsync("Application received");
    }

    [TestMethod]
    public async Task An_IValidatableObject_rule_reports_on_every_member_it_names()
    {
        var page = await OpenAsync("/data-annotations");

        await EnterAsync(Control(page, "GuestName"), "Ada Lovelace");
        await EnterAsync(Control(page, "Email"), "ada@example.com");
        await EnterAsync(Control(page, "Guests"), "3");
        await SubmitButton(page).ClickAsync();

        // A rule no attribute can express - guests within the room's capacity - names two members, and both say so.
        await Expect(Summary(page)).ToBeFocusedAsync();
        await Expect(Errors(page, "Guests")).ToHaveTextAsync("A single room sleeps 1.");
        await Expect(Errors(page, "RoomType")).ToHaveTextAsync("A single room sleeps 1.");
        await Expect(Result(page)).ToHaveCountAsync(0);

        // A suite sleeps four: the booking goes through.
        await Control(page, "RoomType").GetByLabel("Suite (sleeps 4)").CheckAsync();
        await SubmitButton(page).ClickAsync();
        await Expect(Result(page)).ToContainTextAsync("Booked");
        await Expect(Result(page)).ToContainTextAsync("\"RoomType\": \"suite\"");
    }
}
