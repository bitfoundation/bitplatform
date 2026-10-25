using System.Text.RegularExpressions;
using Bit.Borm.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Borm.Tests.E2E;

/// <summary>
/// The workflows around a form: tabs the keyboard moves through and a failed submit turns, a review that reads the
/// answers back and leads back to them, a lookup that searches as the user types, a draft that survives a reload while
/// the guard asks before leaving, a schema read out in another language and mirrored right to left, Blazor's own inputs
/// showing Borm's validation, and totals, conditions and rules written as expressions.
/// </summary>
[TestClass]
public sealed class WorkflowTests : BormTest
{
    [TestMethod]
    public async Task Tabs_move_with_the_arrow_keys_and_a_failed_submit_turns_to_the_tab_that_needs_attention()
    {
        var page = await OpenAsync("/tabs-review");
        var tabs = Form(page).GetByRole(AriaRole.Tab);
        await Expect(tabs).ToHaveCountAsync(4);

        // One tab stop for the list; the arrow keys, Home and End move the selection and the focus with it.
        await tabs.Nth(0).ClickAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(tabs.Nth(1)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(tabs.Nth(1)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(tabs.Nth(3)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(tabs.Nth(0)).ToHaveAttributeAsync("tabindex", "-1");

        // The name and email are missing, on the first tab: that is where the form turns, and the tab says how many.
        await SubmitButton(page).ClickAsync();
        await Expect(tabs.Nth(0)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(tabs.Nth(0)).ToContainTextAsync("2 problems to fix");
        await Expect(tabs.Nth(1)).ToContainTextAsync("1 problem to fix");

        // Choosing the solo plan takes the onboarding tab away.
        await tabs.Nth(1).ClickAsync();
        await Form(page).GetByRole(AriaRole.Radio, new() { Name = "Solo", Exact = true }).CheckAsync();
        await Expect(tabs).ToHaveCountAsync(3);
    }

    [TestMethod]
    public async Task The_review_reads_the_answers_back_in_words_and_leads_back_to_the_step_to_change_them()
    {
        var page = await OpenAsync("/tabs-review");
        await EnterAsync(Control(page, "Name"), "Ada Lovelace");
        await EnterAsync(Control(page, "Email"), "ada@example.com");

        var tabs = Form(page).GetByRole(AriaRole.Tab);
        await tabs.Filter(new() { HasTextString = "Review" }).ClickAsync();

        var review = Form(page).Locator(".bit-borm-review");
        await Expect(review).ToContainTextAsync("Ada Lovelace");
        // An option's label, not its stored value; a box nobody unticked, in words.
        await Expect(review).ToContainTextAsync("Team");
        await Expect(review).ToContainTextAsync("Yes");
        await Expect(review).ToContainTextAsync("Not answered");

        await review.GetByRole(AriaRole.Button, new() { Name = "Change Contact", Exact = true }).ClickAsync();
        await Expect(tabs.Nth(0)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Control(page, "Name")).ToHaveValueAsync("Ada Lovelace");
    }

    [TestMethod]
    public async Task A_lookup_shows_the_stored_record_by_name_searches_as_the_user_types_and_refuses_what_names_none()
    {
        var page = await OpenAsync("/search-lookup");
        var customer = Control(page, "CustomerId");

        // Opened holding the id C-0042: the service was asked for that one record, and the box shows its name.
        await Expect(customer).ToHaveValueAsync("Northwind Bank · Oslo");

        await customer.FillAsync("");
        await customer.PressSequentiallyAsync("Lisb");
        var results = page.Locator("#borm_CustomerId_list > option");
        await EventuallyAsync(async () => await results.CountAsync() == 8, "eight customers in Lisbon were offered");
        foreach (var value in await results.EvaluateAllAsync<string[]>("os => os.map(o => o.value)"))
            StringAssert.Contains(value, "Lisbon");

        // Text committed before its results arrived is searched for exactly, and stored as the id behind it.
        await EnterAsync(customer, "Northwind Bank · Paris");
        await Expect(Errors(page, "CustomerId")).ToHaveCountAsync(0);
        await SubmitButton(page).ClickAsync();
        await Expect(Result(page)).ToContainTextAsync("\"CustomerId\": \"C-0031\"");

        // Text that names no customer is left in the box with a message, and the model keeps its last value.
        await EnterAsync(customer, "No such company");
        await Expect(customer).ToHaveValueAsync("No such company");
        await Expect(Errors(page, "CustomerId")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task A_draft_survives_a_reload_without_its_sensitive_answers_and_the_guard_asks_before_leaving()
    {
        var page = await OpenAsync("/drafts");
        var dialogs = new List<string>();
        var acceptDialogs = true;
        page.Dialog += async (_, dialog) =>
        {
            lock (dialogs) dialogs.Add(dialog.Type);
            if (acceptDialogs) await dialog.AcceptAsync();
            else await dialog.DismissAsync();
        };

        // Typed rather than filled: the browser only asks before unloading a page the user has interacted with.
        await Control(page, "FullName").ClickAsync();
        await Control(page, "FullName").PressSequentiallyAsync("Ada Lovelace");
        await EnterAsync(Control(page, "Email"), "ada@example.com");
        await EnterAsync(Control(page, "Iban"), "DE89370400440532013000");
        await EnterAsync(Control(page, "Password"), "correct horse battery");

        // Autosave writes the draft once the typing stops, and the sensitive answers are not in it.
        var draft = page.Locator(".demo-readout pre");
        await Expect(draft).ToContainTextAsync("ada@example.com", new() { Timeout = 10_000 });
        await Expect(draft).Not.ToContainTextAsync("DE89370400440532013000");
        await Expect(draft).Not.ToContainTextAsync("correct horse battery");

        // An in-app link asks first; declining stays on the form.
        acceptDialogs = false;
        await page.Locator(".bb-docs-sidebar a[href='/builder']").ClickAsync();
        await EventuallyAsync(() => Task.FromResult(dialogs.Contains("confirm")), "the guard asked before an in-app navigation");
        Assert.AreEqual(BaseUrl + "/drafts", page.Url);

        // A reload is an external navigation: the browser's own prompt, and then the draft comes back.
        acceptDialogs = true;
        await page.ReloadAsync();
        Assert.IsTrue(dialogs.Contains("beforeunload"), $"the browser was not asked before the reload ({string.Join(", ", dialogs)})");
        await WaitForInteractiveAsync(page);

        await Expect(Control(page, "FullName")).ToHaveValueAsync("Ada Lovelace");
        await Expect(Control(page, "Email")).ToHaveValueAsync("ada@example.com");
        await Expect(Control(page, "Iban")).ToHaveValueAsync("");
        await Expect(Control(page, "Password")).ToHaveValueAsync("");
        await Expect(page.Locator(".demo-status")).ToContainTextAsync("Restored a draft");

        // Restored as unsaved changes: leaving still asks, and accepting leaves.
        lock (dialogs) dialogs.Clear();
        await page.Locator(".bb-docs-sidebar a[href='/builder']").ClickAsync();
        await EventuallyAsync(() => Task.FromResult(page.Url == BaseUrl + "/builder"),
            $"accepting the guard's question left the form (still on {page.Url}, asked: {string.Join(", ", dialogs)})");
        Assert.IsTrue(dialogs.Contains("confirm"), "the guard did not ask after the draft came back");
    }

    [TestMethod]
    public async Task The_schema_is_read_out_in_the_chosen_language_and_mirrors_right_to_left()
    {
        var page = await OpenAsync("/localization");
        var company = page.Locator("label[for='borm_Company']");
        await Expect(company).ToContainTextAsync("Company");

        // The site runs with InvariantGlobalization: the form's own language needs no culture data.
        await page.GetByRole(AriaRole.Button, new() { Name = "Deutsch", Exact = true }).ClickAsync();
        await Expect(company).ToContainTextAsync("Firma");
        await Expect(Control(page, "Company")).ToHaveAttributeAsync("placeholder", "Contoso GmbH");
        await Expect(Form(page).GetByRole(AriaRole.Radio, new() { Name = "Einzelplatz", Exact = true })).ToBeVisibleAsync();
        await Expect(Form(page)).ToContainTextAsync("Bestellung");
        await Expect(Form(page)).ToContainTextAsync("Nutzer");
        await Expect(Form(page)).ToHaveAttributeAsync("lang", "de");
        // And the library's own words follow it: the button, and the message a blank required field gets.
        var submit = Form(page).Locator("button[type=submit]");
        await Expect(submit).ToHaveTextAsync("Absenden");
        await submit.ClickAsync();
        await Expect(Form(page).Locator(".bit-borm-message--error").First).ToHaveTextAsync("Dieses Feld ist erforderlich.");

        // A dialect with no table of its own reads its language's.
        await page.GetByRole(AriaRole.Button, new() { Name = "Deutsch (Schweiz)", Exact = true }).ClickAsync();
        await Expect(Form(page)).ToHaveAttributeAsync("lang", "de-CH");
        await Expect(company).ToContainTextAsync("Firma");

        // Back to the page's own language, and the text as written.
        await page.GetByRole(AriaRole.Button, new() { Name = "English", Exact = true }).ClickAsync();
        await Expect(company).ToContainTextAsync("Company");
        await Expect(Form(page)).Not.ToHaveAttributeAsync("lang", new Regex(".+"));

        // Logical properties throughout: under dir="rtl" the label sits at the right edge of its field.
        await page.GetByLabel("Right to left").CheckAsync();
        Assert.AreEqual("rtl", await ComputedAsync(Form(page), "direction"));
        var field = await FieldOf(page, "Company").BoundingBoxAsync();
        var label = await company.BoundingBoxAsync();
        Assert.IsNotNull(field);
        Assert.IsNotNull(label);
        Assert.IsTrue(label.X + label.Width > field.X + field.Width - 2, "the label does not start at the right edge");
    }

    [TestMethod]
    public async Task Blazors_own_inputs_and_ValidationSummary_show_the_forms_validation()
    {
        var page = await OpenAsync("/blazor-inputs");
        var name = Control(page, "Name");
        var summary = page.Locator(".demo-validation-summary li");

        await Expect(name).Not.ToHaveClassAsync(new Regex(@"(^|\s)invalid(\s|$)"));
        await Expect(summary).ToHaveCountAsync(0);

        await SubmitButton(page).ClickAsync();

        // InputText's own "invalid" and aria-invalid come from the edit context the view provides.
        await Expect(name).ToHaveClassAsync(new Regex(@"(^|\s)invalid(\s|$)"));
        await Expect(name).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Control(page, "Age")).ToHaveClassAsync(new Regex(@"(^|\s)invalid(\s|$)"));
        await Expect(summary).ToHaveCountAsync(4);
        await Expect(page.Locator(".demo-validation-summary")).ToContainTextAsync("Must be between 18 and 120.");

        // Typed through InputText, written through Borm: the field and the summary settle together.
        await EnterAsync(name, "Ada Lovelace");
        await Expect(name).Not.ToHaveClassAsync(new Regex(@"(^|\s)invalid(\s|$)"));
        await Expect(summary).ToHaveCountAsync(3);
    }

    [TestMethod]
    public async Task Totals_conditions_and_rules_written_as_expressions_follow_the_answers()
    {
        var page = await OpenAsync("/expressions");

        await EnterAsync(Control(page, "Lines[0].Product"), "Widget");
        await EnterAsync(Control(page, "Lines[0].Quantity"), "3");
        await EnterAsync(Control(page, "Lines[0].UnitPrice"), "2.5");
        await Expect(Control(page, "Lines[0].LineTotal")).ToHaveValueAsync(new Regex(@"^7\.50?$"));
        await Expect(Control(page, "Total")).ToHaveValueAsync(new Regex(@"^7\.50?$"));

        // {Total} >= 10000 shows the approver.
        await Expect(Control(page, "Approver")).ToHaveCountAsync(0);
        await EnterAsync(Control(page, "Lines[0].UnitPrice"), "5000");
        await Expect(Control(page, "Approver")).ToBeVisibleAsync();

        // {CustomerType} = 'company' shows the VAT number, judged by a pattern written in the expression.
        await Form(page).GetByRole(AriaRole.Radio, new() { Name = "Company", Exact = true }).CheckAsync();
        await EnterAsync(Control(page, "VatNumber"), "12");
        await SubmitButton(page).ClickAsync();
        await Expect(Errors(page, "VatNumber")).ToHaveTextAsync("Two letters, then 8 to 12 letters or digits.");
        await EnterAsync(Control(page, "VatNumber"), "NL123456789B01");
        await Expect(Errors(page, "VatNumber")).ToHaveCountAsync(0);

        // The console evaluates against the same answers.
        await page.Locator(".expression-probe input").FillAsync("count({Lines}) + 1");
        await Expect(page.Locator("#expression-answer")).ToHaveTextAsync("2");
        await page.Locator(".expression-probe input").FillAsync("{Lines[0].Quantity} *");
        await Expect(page.Locator(".demo-readout .bb-overline").First).ToHaveTextAsync("Does not parse");
    }
}
