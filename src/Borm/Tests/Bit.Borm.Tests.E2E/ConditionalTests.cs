using Bit.Borm.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Borm.Tests.E2E;

/// <summary>
/// Conditions decided in C# and seen in the browser: a field that appears only for one answer and is only required
/// while it shows, a condition on a repeater row that means that row alone, and a hidden field cleared on the way out
/// - with the computed value that read it following along.
/// </summary>
[TestClass]
public sealed class ConditionalTests : BormTest
{
    [TestMethod]
    public async Task The_company_fields_appear_only_for_a_business_account_and_are_only_then_required()
    {
        var page = await OpenAsync("/builder");
        var kind = Control(page, "AccountKind");

        await Expect(Control(page, "CompanyName")).ToHaveCountAsync(0);
        await Expect(Control(page, "VatNumber")).ToHaveCountAsync(0);

        await kind.GetByLabel("Business", new() { Exact = true }).CheckAsync();
        await Expect(Control(page, "CompanyName")).ToBeVisibleAsync();
        await Expect(Control(page, "VatNumber")).ToBeVisibleAsync();

        // Shown, the company name is required: a submit with everything else in order stops on it.
        await EnterAsync(Control(page, "FullName"), "Ada Lovelace");
        await EnterAsync(Control(page, "Email"), "ada@example.com");
        await Control(page, "AcceptTerms").CheckAsync();
        await SubmitButton(page).ClickAsync();
        await Expect(Errors(page, "CompanyName")).ToHaveTextAsync("Company name is required for business accounts.");
        await Expect(Control(page, "CompanyName")).ToBeFocusedAsync();
        await Expect(Result(page)).ToHaveCountAsync(0);

        // Back to a personal account: the fields go, their rule with them, and the same submit goes through.
        await kind.GetByLabel("Personal", new() { Exact = true }).CheckAsync();
        await Expect(Control(page, "CompanyName")).ToHaveCountAsync(0);
        await Expect(Control(page, "VatNumber")).ToHaveCountAsync(0);
        await SubmitButton(page).ClickAsync();
        await Expect(Result(page)).ToContainTextAsync("Submitted");
        await Expect(Result(page)).ToContainTextAsync("\"FullName\": \"Ada Lovelace\"");
    }

    [TestMethod]
    public async Task A_condition_on_a_repeater_row_switches_that_row_alone()
    {
        var page = await OpenAsync("/per-row-conditions");

        // The list is seeded with its minimum of one contact, reached by email.
        await Expect(Rows(page, "Contacts")).ToHaveCountAsync(1);
        await page.GetByRole(AriaRole.Button, new() { Name = "Add contact", Exact = true }).ClickAsync();
        await Expect(Rows(page, "Contacts")).ToHaveCountAsync(2);
        await Expect(Control(page, "Contacts[1].Email")).ToBeVisibleAsync();

        // "Kind" inside a row is that row's Kind: switching the second contact to phone switches only the second.
        await Control(page, "Contacts[1].Kind").GetByLabel("Phone", new() { Exact = true }).CheckAsync();
        await Expect(Control(page, "Contacts[1].Phone")).ToBeVisibleAsync();
        await Expect(Control(page, "Contacts[1].Email")).ToHaveCountAsync(0);
        await Expect(Control(page, "Contacts[0].Email")).ToBeVisibleAsync();
        await Expect(Control(page, "Contacts[0].Phone")).ToHaveCountAsync(0);

        // Only the field each row shows is required, and the summary says which row each complaint is about.
        await EnterAsync(Control(page, "Name"), "Q3 suppliers");
        await SubmitButton(page).ClickAsync();
        await Expect(Summary(page).Locator("a")).ToHaveTextAsync(
            ["Email address (1): This field is required.", "Phone number (2): This field is required."]);
        await Expect(Errors(page, "Contacts[0].Email")).ToHaveTextAsync("This field is required.");
        await Expect(Errors(page, "Contacts[1].Phone")).ToHaveTextAsync("This field is required.");

        // A hidden answer is cleared (ClearOnHide), so switching back does not resurrect it.
        await EnterAsync(Control(page, "Contacts[1].Phone"), "+44 20 7946 0000");
        await Control(page, "Contacts[1].Kind").GetByLabel("Post", new() { Exact = true }).CheckAsync();
        await Expect(Control(page, "Contacts[1].Address")).ToBeVisibleAsync();
        await Control(page, "Contacts[1].Kind").GetByLabel("Phone", new() { Exact = true }).CheckAsync();
        await Expect(Control(page, "Contacts[1].Phone")).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task Unticking_the_hire_car_hides_and_clears_its_days_and_the_estimate_drops_the_car_again()
    {
        var page = await OpenAsync("/dependent-fields");
        var estimate = Control(page, "Estimate");

        // 120 per traveller, 80 per room: one of each.
        await Expect(estimate).ToHaveValueAsync("200");
        await Expect(Control(page, "CarDays")).ToHaveCountAsync(0);

        await Control(page, "HireCar").CheckAsync();
        await Expect(Control(page, "CarDays")).ToBeVisibleAsync();
        await EnterAsync(Control(page, "CarDays"), "3");
        await Expect(estimate).ToHaveValueAsync("335");

        // Clearing a hidden field is a change like any other: the computed estimate hears about it.
        await Control(page, "HireCar").UncheckAsync();
        await Expect(Control(page, "CarDays")).ToHaveCountAsync(0);
        await Expect(estimate).ToHaveValueAsync("200");

        await Control(page, "HireCar").CheckAsync();
        await Expect(Control(page, "CarDays")).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task A_conditionally_required_field_blocks_the_submit_only_while_it_shows()
    {
        var page = await OpenAsync("/cascading");

        await Control(page, "IsBusiness").CheckAsync();
        await Expect(Control(page, "CompanyName")).ToBeVisibleAsync();
        await SubmitButton(page).ClickAsync();
        await Expect(Errors(page, "CompanyName")).ToHaveTextAsync("This field is required.");

        await Control(page, "IsBusiness").UncheckAsync();
        await Expect(Control(page, "CompanyName")).ToHaveCountAsync(0);
        await Expect(page.Locator("#borm_CompanyName_error")).ToHaveCountAsync(0);
        await Expect(Errors(page, "Country")).ToHaveTextAsync("This field is required.");
    }
}
