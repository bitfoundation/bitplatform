using System.Text;
using System.Text.RegularExpressions;
using Bit.Borm.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Borm.Tests.E2E;

/// <summary>
/// The controls beyond a plain text box: a field written on every keystroke driving a live preview, a character
/// counter that turns red instead of truncating, the clear and reveal buttons inside a field's border, files chosen
/// through the browser's own picker and judged by type and size, and a custom renderer taking part in validation
/// like any built-in one.
/// </summary>
[TestClass]
public sealed class FieldTests : BormTest
{
    [TestMethod]
    public async Task A_field_written_on_every_keystroke_drives_the_preview_and_its_counter_as_the_user_types()
    {
        var page = await OpenAsync("/field-polish");
        var title = Control(page, "Title");
        var preview = page.Locator(".demo-preview");

        // UpdateOnInput(): the model is written while the caret is still in the box - no blur, no Enter.
        await title.PressSequentiallyAsync("Sunny loft");
        await Expect(preview.Locator("strong")).ToHaveTextAsync("Sunny loft");
        await Expect(title).ToBeFocusedAsync();
        await Expect(FieldOf(page, "Title").Locator(".bit-borm-counter")).ToHaveTextAsync("10 / 60");

        // The summary's counter is non-destructive: there is no maxlength to cut a paste short, so the text is
        // kept whole and the count turns red.
        var summary = Control(page, "Summary");
        var tooLong = string.Concat(Enumerable.Repeat("Bright, quiet and close to the park. ", 6));
        await summary.FillAsync(tooLong);
        var counter = FieldOf(page, "Summary").Locator(".bit-borm-counter");
        await Expect(counter).ToHaveTextAsync($"{tooLong.Length} / 180");
        await Expect(counter).ToHaveClassAsync(new Regex(@"\bbit-borm-counter--over\b"));
        Assert.AreEqual(ErrorColour, await ComputedAsync(counter, "color"), "the counter over its limit");
        await Expect(summary).ToHaveValueAsync(tooLong);
        await Expect(summary).Not.ToHaveAttributeAsync("maxlength", new Regex(".*"));
        await Expect(preview.Locator("p").Nth(1)).ToHaveTextAsync(tooLong.Trim());

        // Leaving the box judges it against the same limit the counter showed.
        await summary.BlurAsync();
        await Expect(Errors(page, "Summary")).ToHaveTextAsync("Must be at most 180 characters.");
    }

    [TestMethod]
    public async Task The_clear_button_empties_the_field_and_leaves_the_caret_in_it()
    {
        var page = await OpenAsync("/field-polish");
        var city = Control(page, "City");
        var clear = FieldOf(page, "City").GetByRole(AriaRole.Button, new() { Name = "Clear", Exact = true });

        // Offered only while there is something to clear, and the schema's own attribute reaches the input.
        await Expect(clear).ToHaveCountAsync(0);
        await Expect(city).ToHaveAttributeAsync("data-testid", "city");
        await city.PressSequentiallyAsync("Lisbon");
        await Expect(clear).ToBeVisibleAsync();
        await Expect(page.Locator(".demo-preview")).ToContainTextAsync("Lisbon");

        await clear.ClickAsync();

        // The button took itself away with the text; the caret went back into the box rather than to the page body.
        await Expect(city).ToHaveValueAsync("");
        await Expect(city).ToBeFocusedAsync();
        await Expect(clear).ToHaveCountAsync(0);
        await Expect(page.Locator(".demo-preview")).Not.ToContainTextAsync("Lisbon");

        // The suggestions are hints for the browser's own list, not a closed set.
        Assert.AreEqual("Amsterdam,Berlin,Lisbon,Paris,Warsaw",
            string.Join(",", await page.Locator("#borm_City_list > option").EvaluateAllAsync<string[]>("os => os.map(o => o.value)")));
    }

    [TestMethod]
    public async Task The_reveal_button_shows_the_password_and_says_so()
    {
        var page = await OpenAsync("/field-polish");
        var passcode = Control(page, "Passcode");
        var reveal = FieldOf(page, "Passcode").Locator("button[aria-pressed]");

        await passcode.FillAsync("s3cret-code");
        await Expect(passcode).ToHaveAttributeAsync("type", "password");
        await Expect(reveal).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(reveal).ToHaveAttributeAsync("aria-label", "Show password");

        await reveal.ClickAsync();
        await Expect(passcode).ToHaveAttributeAsync("type", "text");
        await Expect(reveal).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(reveal).ToHaveAttributeAsync("aria-label", "Hide password");
        await Expect(passcode).ToHaveValueAsync("s3cret-code");

        await reveal.ClickAsync();
        await Expect(passcode).ToHaveAttributeAsync("type", "password");
    }

    [TestMethod]
    public async Task Chosen_files_are_listed_and_judged_by_type_and_size()
    {
        var page = await OpenAsync("/files");
        var resume = Control(page, "Resume");

        await EnterAsync(Control(page, "Applicant"), "Ada Lovelace");
        await SubmitButton(page).ClickAsync();
        await Expect(Errors(page, "Resume")).ToHaveTextAsync("Please attach your CV.");

        // The accept attribute only filters the picker; the field judges what actually arrives.
        await resume.SetInputFilesAsync(File("cv.txt", "text/plain", 2_000));
        await Expect(Errors(page, "Resume")).ToHaveTextAsync(new Regex("^Only these file types are allowed: "));
        await resume.SetInputFilesAsync(File("cv.pdf", "application/pdf", 3 * 1024 * 1024));
        await Expect(Errors(page, "Resume")).ToHaveTextAsync(new Regex("2 MB or smaller"));

        await resume.SetInputFilesAsync(File("cv.pdf", "application/pdf", 12_345));
        await Expect(Errors(page, "Resume")).ToHaveCountAsync(0);
        await Expect(FieldOf(page, "Resume").Locator(".bit-borm-file__name")).ToHaveTextAsync("cv.pdf");
        await Expect(FieldOf(page, "Resume").Locator(".bit-borm-file__size")).ToHaveTextAsync("12.1 KB");

        // Clearing the selection replaces the browser's file input, and the focus returns to the field rather
        // than being dropped on the page body with the button that took itself away.
        await FieldOf(page, "Resume").GetByRole(AriaRole.Button, new() { Name = "Clear selection", Exact = true }).ClickAsync();
        await Expect(FieldOf(page, "Resume").Locator(".bit-borm-file__item")).ToHaveCountAsync(0);
        await Expect(FieldOf(page, "Resume").Locator(".bit-borm-file")).ToBeFocusedAsync();
        await Expect(Errors(page, "Resume")).ToHaveTextAsync("Please attach your CV.");

        // The same file can be chosen again: the fresh input does not think nothing changed.
        await resume.SetInputFilesAsync(File("cv.pdf", "application/pdf", 12_345));
        await Expect(FieldOf(page, "Resume").Locator(".bit-borm-file__name")).ToHaveTextAsync("cv.pdf");

        // A multiple-file field lists every file picked.
        await Control(page, "Attachments").SetInputFilesAsync([File("scan-1.png", "image/png", 4_000), File("scan-2.pdf", "application/pdf", 6_000)]);
        await Expect(FieldOf(page, "Attachments").Locator(".bit-borm-file__name")).ToHaveTextAsync(["scan-1.png", "scan-2.pdf"]);

        await SubmitButton(page).ClickAsync();
        await Expect(Result(page)).ToContainTextAsync("CV: cv.pdf (12,345 bytes)");
        await Expect(Result(page)).ToContainTextAsync("Attachment: scan-2.pdf (6,000 bytes)");
    }

    [TestMethod]
    public async Task A_custom_renderer_takes_part_in_validation_like_a_built_in_control()
    {
        var page = await OpenAsync("/custom-renderer");
        var stars = page.Locator(".star-rating .star");

        await SubmitButton(page).ClickAsync();
        await Expect(Errors(page, "rating")).ToHaveTextAsync("Please pick a rating.");
        await Expect(Result(page)).ToHaveCountAsync(0);

        await stars.Nth(3).ClickAsync();
        await Expect(page.Locator(".star-rating__value")).ToHaveTextAsync("4 / 5");
        await Expect(page.Locator(".star-rating .star--on")).ToHaveCountAsync(4);
        await Expect(Errors(page, "rating")).ToHaveCountAsync(0);

        await SubmitButton(page).ClickAsync();
        await Expect(Result(page)).ToContainTextAsync("\"rating\": 4");
    }

    private static FilePayload File(string name, string mimeType, int size) => new()
    {
        Name = name,
        MimeType = mimeType,
        Buffer = Encoding.ASCII.GetBytes(new string('x', size)),
    };
}
