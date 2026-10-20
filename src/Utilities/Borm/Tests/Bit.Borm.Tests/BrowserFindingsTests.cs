using System.ComponentModel.DataAnnotations;
using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// Defects the browser suite (Bit.Borm.Tests.E2E) found, pinned here as well so they are caught on every build
/// rather than only when that suite is started by hand.
/// </summary>
[TestClass]
public class BrowserFindingsTests : ComponentTestBase
{
    private sealed class Contact
    {
        [EmailAddress]
        public string? Email { get; set; }

        [Url]
        public string? Site { get; set; }

        [EmailAddress(ErrorMessage = "Use your work address.")]
        public string? Work { get; set; }
    }

    [TestMethod]
    public async Task A_DataType_attribute_without_a_message_of_its_own_reads_as_a_sentence()
    {
        // [EmailAddress] and [Url] keep their default text where ErrorMessage reads it, so the raw template
        // "The {0} field is not a valid e-mail address." reached the form, braces and all.
        var model = new Contact { Email = "nope", Site = "nope", Work = "nope" };
        var state = new BormState(BormSchemaGenerator.Generate<Contact>(), new BormModelDataAccessor(model));

        await state.ValidateAsync();

        var email = Assert.ContainsSingle(state.MessagesFor("Email")).Message;
        var site = Assert.ContainsSingle(state.MessagesFor("Site")).Message;
        Assert.DoesNotContain("{0}", email, StringComparison.Ordinal);
        Assert.DoesNotContain("{0}", site, StringComparison.Ordinal);
        Assert.AreEqual(BormDefaultMessageProvider.Instance.Get(BormMessageKeys.Email), email);
    }

    [TestMethod]
    public async Task A_DataType_attribute_with_a_message_of_its_own_keeps_it()
    {
        var model = new Contact { Work = "nope" };
        var state = new BormState(BormSchemaGenerator.Generate<Contact>(), new BormModelDataAccessor(model));

        await state.ValidateAsync();

        Assert.AreEqual("Use your work address.", Assert.ContainsSingle(state.MessagesFor("Work")).Message);
    }

    [TestMethod]
    public void Next_is_the_default_button_of_a_wizard_step()
    {
        // Without a submit button the browser submits on Enter only when the form holds a single text box,
        // so Enter advanced a one-question step and did nothing on a step with two.
        var form = BormBuilder.Create()
            .Text("first", f => f.Label("First name"))
            .Text("last", f => f.Label("Last name"))
            .Text("email", f => f.Label("Email"))
            .Step("name", s => s.Title("Name").Fields("first", "last"))
            .Step("contact", s => s.Title("Contact").Fields("email"))
            .Build();
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form));

        var next = cut.FindAll("button").First(b => b.TextContent.Trim() == "Next");

        Assert.AreEqual("submit", next.GetAttribute("type"));
        Assert.AreSame(next, cut.FindAll("form button[type=submit]").First(), "the first submit button is the one Enter clicks");
        Assert.IsTrue(next.HasAttribute("blazor:onclick:preventdefault"),
            "the click advances by itself; it must not also submit the form");

        next.Click();

        Assert.IsNotNull(cut.Find("input#borm_email"));
    }

    [TestMethod]
    public async Task The_error_summary_never_takes_the_focus_from_an_edit_after_a_submit_that_passed()
    {
        var form = BormBuilder.Create().Text("name", f => f.Required()).Build();
        var data = new Dictionary<string, object?> { ["name"] = "Ada" };
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.Data, data)
            .Add(x => x.ShowErrorSummary, true));

        await cut.Find("form").SubmitAsync();
        var before = FocusCalls();

        // The user empties the field: the form is invalid again and the summary says so, but the user is
        // typing, and pulling the focus away now would put their next keystrokes nowhere.
        cut.Find("input#borm_name").Change("");

        Assert.IsNotEmpty(cut.FindAll(".bit-borm-summary"));
        Assert.AreEqual(before, FocusCalls());
    }

    [TestMethod]
    public async Task The_error_summary_still_takes_the_focus_after_a_failed_submit_that_follows_a_passing_one()
    {
        var form = BormBuilder.Create().Text("name", f => f.Required()).Build();
        var data = new Dictionary<string, object?> { ["name"] = "Ada" };
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.Data, data)
            .Add(x => x.ShowErrorSummary, true));

        await cut.Find("form").SubmitAsync();
        cut.Find("input#borm_name").Change("");
        var before = FocusCalls();

        await cut.Find("form").SubmitAsync();

        Assert.IsTrue(FocusCalls() > before);
    }

    private int FocusCalls()
        => JSInterop.Invocations.Count(i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));
}
