using System.Text.RegularExpressions;
using Bit.Borm.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Borm.Tests.E2E;

/// <summary>
/// What a browser proves about the newer parts of the engine: a form posted with script turned off and judged on the
/// server, the designer building a form, a mask, a Markdown preview, a signature drawn with a mouse and a file uploaded
/// with its progress, undo taking a change back, and a long list paged with every row still reachable.
/// </summary>
[TestClass]
public sealed class PlatformTests : BormTest
{
    [TestMethod]
    public async Task With_script_turned_off_the_form_posts_and_is_judged_on_the_server()
    {
        var options = DefaultContextOptions();
        options.JavaScriptEnabled = false;
        var page = await NewPageAsync(await NewContextAsync(options));
        await GotoAsync(page, "/static-form");
        var submit = SubmitButton(page);

        // Posted empty: the page comes back from the server with every message in place.
        await submit.ClickAsync();
        await Expect(Errors(page, "FullName")).ToHaveTextAsync("This field is required.");
        await Expect(Errors(page, "Hours")).ToHaveTextAsync("Must be between 1 and 40.");
        await Expect(Errors(page, "Address.City")).ToHaveTextAsync("This field is required.");
        await Expect(Result(page)).ToHaveCountAsync(0);
        // Nothing posted under no field: every complaint in the summary leads to the field it is about.
        await Expect(Summary(page).Locator("li:not(:has(a))")).ToHaveCountAsync(0);

        // Text model binding cannot read comes back on the field it was typed into, with the reason.
        await Control(page, "Hours").FillAsync("abc");
        await submit.ClickAsync();
        await Expect(Errors(page, "Hours")).ToHaveTextAsync("'abc' is not a valid value for this field.");
        await Expect(Control(page, "Hours")).ToHaveValueAsync("abc");
        await Expect(Summary(page).Locator("li:not(:has(a))")).ToHaveCountAsync(0);

        await Control(page, "FullName").FillAsync("Ada Lovelace");
        await Control(page, "Email").FillAsync("ada@example.com");
        await Control(page, "Hours").FillAsync("8");
        await Form(page).GetByRole(AriaRole.Radio, new() { Name = "Kitchen", Exact = true }).CheckAsync();
        // A combobox posts the name typed into it, which the form turns into the code it stands for.
        await Control(page, "Country").FillAsync("Germany");
        await Control(page, "Address.City").FillAsync("Berlin");
        await submit.ClickAsync();

        await Expect(Result(page)).ToContainTextAsync("\"Country\": \"de\"");
        await Expect(Result(page)).ToContainTextAsync("\"Team\": \"kitchen\"");
        await Expect(Result(page)).ToContainTextAsync("\"Newsletter\": false");
    }

    [TestMethod]
    public async Task The_designer_adds_a_field_whose_properties_the_preview_and_the_schema_follow()
    {
        var page = await OpenAsync("/designer");
        var items = page.Locator(".bit-borm-designer__item");
        await Expect(items).ToHaveCountAsync(5);

        await page.Locator(".bit-borm-designer__kinds button", new() { HasTextString = "Date" }).First.ClickAsync();
        await Expect(items).ToHaveCountAsync(6);
        // The new field is chosen, and the focus is on it, ready for its properties.
        await Expect(page.Locator("#designer_p_Name")).ToHaveValueAsync("date1");
        Assert.AreEqual("button.bit-borm-designer__select", await FocusedAsync(page));

        await EnterAsync(page.Locator("#designer_p_Label"), "Start date");
        await Expect(page.Locator("label[for='designer_v_date1']")).ToContainTextAsync("Start date");
        await Expect(page.Locator(".bit-borm-designer__json-text")).ToHaveValueAsync(new Regex("\"date1\""));

        await page.GetByRole(AriaRole.Button, new() { Name = "Move Start date up", Exact = true }).ClickAsync();
        await Expect(page.Locator(".bit-borm-designer__fields [aria-live]")).ToContainTextAsync("Start date is now number");
    }

    [TestMethod]
    public async Task A_mask_a_markdown_preview_a_drawn_signature_and_an_upload_with_progress()
    {
        var page = await OpenAsync("/rich-fields");

        await EnterAsync(Control(page, "Phone"), "5551234567");
        await Expect(Control(page, "Phone")).ToHaveValueAsync("(555) 123-4567");
        await EnterAsync(Control(page, "Iban"), "de89370400440532013000");
        await Expect(Control(page, "Iban")).ToHaveValueAsync("DE89 3704 0044 0532 0130 00");

        await Control(page, "CoverLetter").FillAsync("Hello **world**, see [the docs](javascript:alert(1)).");
        await page.GetByRole(AriaRole.Button, new() { Name = "Show preview", Exact = true }).ClickAsync();
        var preview = page.Locator("#borm_CoverLetter_preview");
        await Expect(preview.Locator("strong")).ToHaveTextAsync("world");
        await Expect(preview.Locator("a")).ToHaveCountAsync(0);

        // Drawn with the mouse: one stroke on the pad.
        var pad = page.Locator(".bit-borm-signature__pad");
        var box = (await pad.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + 20, box.Y + 40);
        await page.Mouse.DownAsync();
        for (var step = 1; step <= 10; step++) await page.Mouse.MoveAsync(box.X + 20 + step * 15, box.Y + 40 + (step % 2) * 20);
        await page.Mouse.UpAsync();
        await Expect(page.Locator(".bit-borm-signature__pad path")).ToHaveCountAsync(1);

        // Sent as soon as it is chosen; the bar moves, and the file ends up uploaded.
        await page.Locator("#borm_Resume").SetInputFilesAsync(new FilePayload
        {
            Name = "resume.txt", MimeType = "text/plain", Buffer = new byte[3 * 1024 * 1024]
        });
        await Expect(page.Locator(".bit-borm-file__progress")).ToHaveCountAsync(1);
        await Expect(page.Locator(".bit-borm-file__done")).ToHaveCountAsync(1, new() { Timeout = 30_000 });
        await Expect(page.Locator(".bit-borm-file [aria-live]")).ToContainTextAsync("resume.txt uploaded");
    }

    [TestMethod]
    public async Task Undo_takes_a_change_back_and_a_problem_on_another_page_is_reached_through_the_summary()
    {
        var page = await OpenAsync("/undo-long-lists");
        await Expect(page.Locator(".bit-borm-array__range")).ToHaveTextAsync("1–20 of 500");

        var quantity = Control(page, "Lines[0].Quantity");
        var before = await quantity.InputValueAsync();
        await EnterAsync(quantity, "100");
        // Visible text "Undo"; its accessible name says which change: "Undo the change to Quantity".
        var undo = Form(page).Locator("button", new() { HasTextString = "Undo" });
        await Expect(undo).ToHaveAttributeAsync("aria-label", new Regex("^Undo the change to "));
        await Expect(undo).ToBeEnabledAsync();
        // From the keyboard: WebKit never focuses a button a mouse clicks, so only there does the focus have a place to stay.
        await undo.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(quantity).ToHaveValueAsync(before);
        await Expect(page.Locator(".bit-borm-form__actions [aria-live]")).ToContainTextAsync("Undone:");
        await Expect(undo).ToBeFocusedAsync();

        // A problem on page 7, the form back on page 1: the summary's link turns to the page and puts the caret there.
        // By value: a bare string matches labels too, and the page labelled "6" is the one before.
        await page.Locator(".bit-borm-array__page select").SelectOptionAsync(new SelectOptionValue { Value = "6" });
        await EnterAsync(Control(page, "Lines[130].Description"), "");
        await page.Locator(".bit-borm-array__page select").SelectOptionAsync(new SelectOptionValue { Value = "0" });
        await SubmitButton(page).ClickAsync();
        await Expect(page.Locator(".bit-borm-array__elsewhere")).ToContainTextAsync("1 line on another page needs attention.");
        await Summary(page).GetByRole(AriaRole.Link).First.ClickAsync();
        await Expect(page.Locator(".bit-borm-array__range")).ToHaveTextAsync("121–140 of 500");
        await Expect(Control(page, "Lines[130].Description")).ToBeFocusedAsync();
    }
}
