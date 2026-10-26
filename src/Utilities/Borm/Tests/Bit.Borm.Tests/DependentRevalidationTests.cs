using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Borm.Tests;

/// <summary>
/// A cross-field rule lives on one of the two fields it compares, so only one of the two changes ever
/// runs it. Correcting the other one leaves a verdict on screen that is no longer true — the
/// confirm-password complaint that survives the password being fixed. React Hook Form's <c>deps</c>
/// and TanStack Form's <c>onChangeListenTo</c> exist for this; <c>RevalidateOn</c> is ours.
/// </summary>
[TestClass]
public class DependentRevalidationTests
{
    private static BormDefinition Passwords() => BormBuilder.Create()
        .Text("Password", f => f.AsPassword().Required())
        .Text("Confirm", f => f.AsPassword().MatchesField("Password", "Password"))
        .Build();

    private static BormState Form(BormDefinition form)
        => new(form, new BormDictionaryDataAccessor());

    [TestMethod]
    public void MatchesField_registers_the_path_it_reads()
    {
        var confirm = Passwords().FindByPath("Confirm")!;
        Assert.Contains("Password", confirm.RevalidateOn);
    }

    [TestMethod]
    public async Task Correcting_the_other_field_clears_a_stale_mismatch()
    {
        var state = Form(Passwords());
        state.SetValue("Password", "hunter2");
        state.SetValue("Confirm", "hunter3");
        await state.ValidateFieldAsync("Confirm");
        Assert.IsNotEmpty(state.MessagesFor("Confirm"));

        // The user goes back and fixes the *password* to agree with what they typed. Nothing touches
        // the confirmation box, so nothing but this would revalidate it.
        state.SetValue("Password", "hunter3");
        await state.ValidateDependentsAsync("Password");

        Assert.IsEmpty(state.MessagesFor("Confirm"));
    }

    [TestMethod]
    public async Task Before_the_first_submit_a_form_that_validates_on_submit_judges_no_dependent_either()
    {
        // Visited and left in agreement, then the password changed: "OnSubmit" means nothing is said until the
        // user sends the form, and the dependent's revalidation had said "do not match" all the same.
        var state = new BormState(Passwords(), new BormDictionaryDataAccessor()) { ValidationTrigger = BormValidationTrigger.OnSubmit };
        state.SetValue("Password", "hunter2");
        state.SetValue("Confirm", "hunter2");
        state.MarkTouched("Confirm");

        state.SetValue("Password", "hunter3");
        await state.ValidateDependentsAsync("Password");

        Assert.IsEmpty(state.MessagesFor("Confirm"));
    }

    [TestMethod]
    public async Task Checking_one_field_from_outside_the_form_shows_what_it_found()
    {
        // React Hook Form's trigger(name): asked for, so shown - not recorded for a field the user has not visited.
        var state = Form(Passwords());

        Assert.IsTrue(await state.ValidateFieldAsync("Password"));

        Assert.IsTrue(state.IsTouched("Password"));
        Assert.IsTrue(state.GetFieldState("Password").IsInvalid);
    }

    [TestMethod]
    public async Task Breaking_the_match_from_the_other_side_is_reported()
    {
        var state = Form(Passwords());
        state.SetValue("Password", "hunter2");
        state.SetValue("Confirm", "hunter2");
        await state.ValidateFieldAsync("Confirm");
        Assert.IsEmpty(state.MessagesFor("Confirm"));

        state.SetValue("Password", "something-else");
        await state.ValidateDependentsAsync("Password");

        Assert.IsNotEmpty(state.MessagesFor("Confirm"));
    }

    [TestMethod]
    public async Task A_dependent_nobody_has_reached_is_left_alone()
    {
        // The point is to correct a verdict already on screen, never to bring one forward: typing a
        // password must not make an untouched confirmation box start showing errors.
        var state = Form(Passwords());
        state.SetValueQuietly("Password", "hunter2");
        await state.ValidateDependentsAsync("Password");

        Assert.IsEmpty(state.MessagesFor("Confirm"));
        Assert.IsFalse(state.IsTouched("Confirm"));
    }

    [TestMethod]
    public async Task A_dependent_is_revalidated_once_the_form_has_been_submitted()
    {
        var state = Form(Passwords());
        state.SetValue("Password", "hunter2");
        state.SetValue("Confirm", "nope");
        await state.SubmitAsync();
        Assert.IsNotEmpty(state.MessagesFor("Confirm"));

        state.SetValue("Password", "nope");
        await state.ValidateDependentsAsync("Password");

        Assert.IsEmpty(state.MessagesFor("Confirm"));
    }

    [TestMethod]
    public async Task A_row_revalidates_against_its_own_sibling_not_the_first_row()
    {
        // Paths resolve against the object that owns the field, exactly as conditions and computed
        // dependencies do, so row 2's rule reads row 2's value.
        var form = BormBuilder.Create()
            .ArrayOf("Rows", BormFieldType.Object, a => a.Items(min: 0))
            .Build();
        var rows = form.FindByPath("Rows")!;
        rows.ItemTemplate = new BormFieldDefinition("item", BormFieldType.Object)
        {
            Children =
            {
                new BormFieldDefinition("Email", BormFieldType.Email),
                new BormFieldDefinition("ConfirmEmail", BormFieldType.Email)
                {
                    Validators = { new BormCompareRule("Email", "Email") },
                    RevalidateOn = { "Email" }
                }
            }
        };

        var state = Form(form);
        state.AddArrayItem(rows, "Rows");
        state.AddArrayItem(rows, "Rows");

        state.SetValue("Rows[0].Email", "a@example.com");
        state.SetValue("Rows[0].ConfirmEmail", "a@example.com");
        state.SetValue("Rows[1].Email", "b@example.com");
        state.SetValue("Rows[1].ConfirmEmail", "typo@example.com");
        await state.ValidateFieldAsync("Rows[1].ConfirmEmail");
        Assert.IsNotEmpty(state.MessagesFor("Rows[1].ConfirmEmail"));

        state.SetValue("Rows[1].Email", "typo@example.com");
        await state.ValidateDependentsAsync("Rows[1].Email");

        Assert.IsEmpty(state.MessagesFor("Rows[1].ConfirmEmail"));
        // Row 0 was never in question and was not disturbed.
        Assert.IsEmpty(state.MessagesFor("Rows[0].ConfirmEmail"));
    }

    [TestMethod]
    public async Task A_schema_with_no_dependencies_pays_nothing()
    {
        var form = BormBuilder.Create().Text("a", f => f.Required()).Build();
        var state = Form(form);
        state.SetValue("a", "");
        await state.ValidateFieldAsync("a");

        // A no-op, but it must not throw or disturb what is already recorded.
        await state.ValidateDependentsAsync("a");
        Assert.IsNotEmpty(state.MessagesFor("a"));
    }

    [TestMethod]
    public void The_generator_wires_up_a_Compare_attribute()
    {
        var form = BormSchemaGenerator.Generate<SignupModel>();
        Assert.Contains("Password", form.FindByPath("ConfirmPassword")!.RevalidateOn);
    }

    [TestMethod]
    public void A_comparison_that_never_revalidates_is_reported()
    {
        var form = BormBuilder.Create()
            .Text("a")
            .Text("b")
            .Build();
        // Added directly rather than through MatchesField, which wires itself up.
        form.FindByPath("b")!.Validators.Add(new BormCompareRule("a"));

        Assert.Contains(d => d.Message.Contains("does not revalidate", StringComparison.Ordinal), form.Validate());
        Assert.DoesNotContain(d => d.Message.Contains("does not revalidate", StringComparison.Ordinal), Passwords().Validate());
    }

    [TestMethod]
    public void RevalidateOn_survives_a_clone_and_a_json_round_trip()
    {
        var form = Passwords();
        Assert.Contains("Password", form.Clone().FindByPath("Confirm")!.RevalidateOn);

        var json = BormJsonSchemaExporter.Export(form);
        Assert.Contains("x-revalidateOn", json, StringComparison.Ordinal);
        Assert.Contains("Password", BormJsonSchemaImporter.Import(json).FindByPath("Confirm")!.RevalidateOn);
    }

    [TestMethod]
    public void A_path_that_names_nothing_is_reported()
    {
        var form = BormBuilder.Create()
            .Text("a", f => f.RevalidateOn("nowhere"))
            .Build();

        Assert.Contains(d => d.Message.Contains("RevalidateOn", StringComparison.Ordinal), form.Validate());
    }
}

/// <summary>The same thing again, through the real controls — the path an actual user takes.</summary>
[TestClass]
public class DependentRevalidationRenderingTests : BunitContext
{
    public DependentRevalidationRenderingTests() => Services.AddBorm();

    [TestMethod]
    public async Task Adding_a_repeater_row_revalidates_what_depends_on_the_list()
    {
        // A repeater operation changes the value of the field the list binds to, but a repeater has no
        // field context to route through — so a rule elsewhere that counts the rows was judged once and
        // never again.
        var form = BormBuilder.Create()
            .Integer("Seats", f => f
                .RevalidateOn("Guests")
                .Must(ctx => ctx.Value is not int seats
                             || ctx.Data.GetValue("Guests") is not System.Collections.ICollection guests
                             || seats >= guests.Count,
                      "Not enough seats for the guests listed."))
            .ArrayOf("Guests", BormFieldType.Text)
            .Build();

        var cut = Render<BormView>(p => p.Add(x => x.Definition, form));
        var state = cut.Instance.Form;
        var guests = form.FindByPath("Guests")!;

        state.SetValue("Seats", 1);
        state.AddArrayItem(guests, "Guests");
        await state.ValidateFieldAsync("Seats");
        Assert.IsEmpty(state.MessagesFor("Seats"));

        // Through the button, so the repeater's own wiring is what is under test.
        cut.Find(".bit-borm-btn--add").Click();

        Assert.IsNotEmpty(state.MessagesFor("Seats"));
    }

    [TestMethod]
    public async Task Editing_the_password_control_clears_the_confirmation_error()
    {
        var form = BormBuilder.Create()
            .Text("Password", f => f.Label("Password").AsPassword())
            .Text("Confirm", f => f.Label("Confirm").AsPassword().MatchesField("Password", "Password"))
            .Build();

        var cut = Render<BormView>(p => p.Add(x => x.Definition, form));
        var state = cut.Instance.Form;

        cut.Find("input#borm_Password").Change("hunter2");
        cut.Find("input#borm_Confirm").Change("hunter3");
        await cut.Find("input#borm_Confirm").BlurAsync(new());
        Assert.IsNotEmpty(state.MessagesFor("Confirm"));

        cut.Find("input#borm_Password").Change("hunter3");

        Assert.IsEmpty(state.MessagesFor("Confirm"));
    }
}
