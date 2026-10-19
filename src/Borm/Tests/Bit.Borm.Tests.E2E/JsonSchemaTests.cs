using System.Text.RegularExpressions;
using Bit.Borm.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Borm.Tests.E2E;

/// <summary>
/// The JSON Schema editor: an edited schema regenerates the form, a broken one is reported without losing the form
/// that worked, the conditional example's <c>x-visibleWhen</c>, <c>dependentRequired</c> and two-column layout come
/// through, and the wizard example survives the round trip out to JSON and back.
/// </summary>
[TestClass]
public sealed class JsonSchemaTests : BormTest
{
    private const string FeedbackSchema = """
        {
          "title": "Feedback",
          "type": "object",
          "required": ["comment"],
          "properties": {
            "score":   { "type": "integer", "title": "Score", "minimum": 1, "maximum": 10 },
            "comment": { "type": "string", "title": "Comment", "minLength": 5 }
          }
        }
        """;

    [TestMethod]
    public async Task Editing_the_schema_and_applying_it_regenerates_the_form()
    {
        var page = await OpenAsync("/json-schema");
        await Expect(Title(page)).ToHaveTextAsync("Event registration");
        await Expect(Control(page, "name")).ToBeVisibleAsync();

        await Editor(page).FillAsync(FeedbackSchema);
        await page.GetByRole(AriaRole.Button, new() { Name = "Apply schema", Exact = true }).ClickAsync();

        // A new form, from the new schema: its title, its fields and its rules - and nothing of the old one.
        await Expect(Title(page)).ToHaveTextAsync("Feedback");
        await Expect(Control(page, "score")).ToHaveAttributeAsync("type", "number");
        await Expect(Control(page, "score")).ToHaveAttributeAsync("max", "10");
        await Expect(page.Locator("#borm_comment_label")).ToContainTextAsync("Comment");
        await Expect(Control(page, "name")).ToHaveCountAsync(0);

        await SubmitButton(page).ClickAsync();
        await Expect(Errors(page, "comment")).ToHaveTextAsync("This field is required.");
        await EnterAsync(Control(page, "comment"), "Good");
        await Expect(Errors(page, "comment")).ToHaveTextAsync("Must be at least 5 characters.");
        await EnterAsync(Control(page, "comment"), "Great talk");
        await SubmitButton(page).ClickAsync();
        await Expect(Result(page)).ToContainTextAsync("\"comment\": \"Great talk\"");
    }

    [TestMethod]
    public async Task An_invalid_schema_is_reported_and_the_last_good_form_stays()
    {
        var page = await OpenAsync("/json-schema");

        await Editor(page).FillAsync("{ \"type\": \"object\", \"properties\": ");
        await page.GetByRole(AriaRole.Button, new() { Name = "Apply schema", Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Alert)).ToHaveTextAsync(new Regex("^Invalid JSON Schema: .+"));
        await Expect(Title(page)).ToHaveTextAsync("Event registration");
        await Expect(Control(page, "email")).ToBeVisibleAsync();

        // A good schema afterwards clears the error.
        await Editor(page).FillAsync(FeedbackSchema);
        await page.GetByRole(AriaRole.Button, new() { Name = "Apply schema", Exact = true }).ClickAsync();
        await Expect(Title(page)).ToHaveTextAsync("Feedback");
        await Expect(page.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task The_conditional_example_brings_its_conditions_and_layout_through()
    {
        var page = await OpenAsync("/json-schema");
        await page.GetByRole(AriaRole.Button, new() { Name = "Load conditional example", Exact = true }).ClickAsync();
        await Expect(Title(page)).ToHaveTextAsync("Order");

        // x-columns: 2 lays the form out on a two-column grid; x-colSpan: 2 takes a field across both, and a field
        // without it fills one column.
        var body = Form(page).Locator(".bit-borm-form__body");
        Assert.HasCount(2, (await ComputedAsync(body, "grid-template-columns")).Split(' '), "the form is not on two columns");
        var bodyBox = await body.BoundingBoxAsync();
        var email = await Control(page, "email").BoundingBoxAsync();
        var creditCard = await Control(page, "creditCard").BoundingBoxAsync();
        Assert.IsNotNull(bodyBox);
        Assert.IsNotNull(email);
        Assert.IsNotNull(creditCard);
        Assert.AreEqual(bodyBox.Width, email.Width, 2, "the email (x-colSpan: 2) does not span the form");
        Assert.IsTrue(creditCard.Width < bodyBox.Width * 0.6, $"the card number ({creditCard.Width}px of {bodyBox.Width}px) is wider than one column");

        // x-visibleWhen: the company fields only for a business customer.
        await Expect(Control(page, "companyName")).ToHaveCountAsync(0);
        await Control(page, "kind").GetByLabel("Business", new() { Exact = true }).CheckAsync();
        await Expect(Control(page, "companyName")).ToBeVisibleAsync();
        await Expect(Control(page, "vatNumber")).ToBeVisibleAsync();

        // dependentRequired: a card number makes the billing reference required; $ref brings the address's own rule.
        await EnterAsync(Control(page, "email"), "ada@example.com");
        await EnterAsync(Control(page, "companyName"), "Analytical Engines");
        await EnterAsync(Control(page, "creditCard"), "4111 1111 1111 1111");
        await SubmitButton(page).ClickAsync();
        await Expect(Errors(page, "billingReference")).ToHaveTextAsync("This field is required.");
        await Expect(Errors(page, "billing.city")).ToHaveTextAsync("This field is required.");
        await Expect(Errors(page, "companyName")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task The_wizard_example_survives_the_round_trip_out_to_JSON_and_back()
    {
        var page = await OpenAsync("/json-schema");
        await page.GetByRole(AriaRole.Button, new() { Name = "Load wizard example", Exact = true }).ClickAsync();

        var steps = page.Locator(".bit-borm-stepper__label");
        await Expect(steps).ToHaveTextAsync(["Contact", "Type", "Confirm"]);

        // Export writes the form back out - steps, their condition and all - and applying the export rebuilds it.
        await page.GetByRole(AriaRole.Button, new() { Name = "Export (round-trip)", Exact = true }).ClickAsync();
        var exported = await Editor(page).InputValueAsync();
        StringAssert.Contains(exported, "\"x-steps\"", "the export lost the wizard steps");
        StringAssert.Contains(exported, "\"visibleWhen\"", "the export lost the company step's condition");

        await page.GetByRole(AriaRole.Button, new() { Name = "Apply schema", Exact = true }).ClickAsync();
        await Expect(steps).ToHaveTextAsync(["Contact", "Type", "Confirm"]);

        // The re-imported wizard still validates per step, and its conditional step still appears.
        await FormButton(page, "Next").ClickAsync();
        await Expect(Errors(page, "email")).ToHaveTextAsync("This field is required.");

        // The contact step has a single text box, so Enter in it is the browser's implicit submission, which the
        // wizard turns into Next.
        await Control(page, "email").FillAsync("ada@example.com");
        await Control(page, "email").PressAsync("Enter");
        await Expect(page.Locator(".bit-borm-stepper__step.is-active .bit-borm-stepper__label")).ToHaveTextAsync("Type");
        await Control(page, "kind").GetByLabel("business", new() { Exact = true }).CheckAsync();
        await Expect(steps).ToHaveTextAsync(["Contact", "Type", "Company", "Confirm"]);
    }

    private static ILocator Editor(IPage page) => page.Locator("textarea[aria-label='JSON Schema']");

    private static ILocator Title(IPage page) => Form(page).Locator(".bit-borm-form__title");
}
