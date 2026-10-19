namespace Bit.Borm.Tests;

[TestClass]
public class ValidationTriggerTests
{
    private static BormState Build(BormValidationTrigger trigger)
    {
        var form = BormBuilder.Create().Text("name", f => f.Required()).Build();
        return new BormState(form, new BormDictionaryDataAccessor())
        {
            ValidationTrigger = trigger,
            RevalidationTrigger = trigger
        };
    }

    [TestMethod]
    public void OnChange_validates_on_both_change_and_blur()
    {
        var state = Build(BormValidationTrigger.OnChange);
        Assert.IsTrue(state.ShouldValidate("name", BormValidationTrigger.OnChange));
        Assert.IsTrue(state.ShouldValidate("name", BormValidationTrigger.OnBlur));
    }

    [TestMethod]
    public void OnBlur_stays_quiet_while_typing()
    {
        var state = Build(BormValidationTrigger.OnBlur);
        Assert.IsFalse(state.ShouldValidate("name", BormValidationTrigger.OnChange));
        Assert.IsTrue(state.ShouldValidate("name", BormValidationTrigger.OnBlur));
    }

    [TestMethod]
    public void OnSubmit_never_validates_from_the_field()
    {
        var state = Build(BormValidationTrigger.OnSubmit);
        Assert.IsFalse(state.ShouldValidate("name", BormValidationTrigger.OnChange));
        Assert.IsFalse(state.ShouldValidate("name", BormValidationTrigger.OnBlur));
    }

    [TestMethod]
    public async Task Revalidation_trigger_takes_over_once_submitted()
    {
        var form = BormBuilder.Create().Text("name", f => f.Required()).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor())
        {
            ValidationTrigger = BormValidationTrigger.OnSubmit,
            RevalidationTrigger = BormValidationTrigger.OnChange
        };

        Assert.IsFalse(state.ShouldValidate("name", BormValidationTrigger.OnChange));

        await state.SubmitAsync();

        Assert.IsTrue(state.ShouldValidate("name", BormValidationTrigger.OnChange));
    }
}

[TestClass]
public class SubmissionTests
{
    [TestMethod]
    public async Task Submit_marks_every_field_touched_so_errors_become_visible()
    {
        var form = BormBuilder.Create()
            .Text("name", f => f.Required())
            .Object("address", a => a.Text("city", c => c.Required()))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        Assert.IsFalse(state.IsTouched("name"));

        var valid = await state.SubmitAsync();

        Assert.IsFalse(valid);
        Assert.IsTrue(state.IsTouched("name"));
        Assert.IsTrue(state.IsTouched("address.city"));
        Assert.AreEqual(1, state.SubmitCount);
        Assert.IsTrue(state.IsSubmitted);
    }

    [TestMethod]
    public async Task A_second_submit_is_ignored_while_the_first_is_running()
    {
        var form = BormBuilder.Create().Text("name").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        var gate = new TaskCompletionSource();
        var calls = 0;

        var first = state.SubmitAsync(async _ =>
        {
            Interlocked.Increment(ref calls);
            await gate.Task;
        });

        // Fires while the first submit is still awaiting its handler.
        var second = await state.SubmitAsync(_ =>
        {
            Interlocked.Increment(ref calls);
            return Task.CompletedTask;
        });

        gate.SetResult();
        await first;

        Assert.IsFalse(second);
        Assert.AreEqual(1, calls);
        Assert.AreEqual(1, state.SubmitCount);
    }

    [TestMethod]
    public async Task Valid_submit_invokes_the_valid_handler_only()
    {
        var form = BormBuilder.Create().Text("name", f => f.Required()).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("name", "Ada");

        var validCalls = 0;
        var invalidCalls = 0;

        var result = await state.SubmitAsync(
            _ => { validCalls++; return Task.CompletedTask; },
            _ => { invalidCalls++; return Task.CompletedTask; });

        Assert.IsTrue(result);
        Assert.AreEqual(1, validCalls);
        Assert.AreEqual(0, invalidCalls);
    }
}

[TestClass]
public class ResetTests
{
    [TestMethod]
    public void Reset_restores_initial_values_and_clears_tracking()
    {
        var model = new RegistrationModel { FirstName = "Ada", Age = 30 };
        var form = BormSchemaGenerator.Generate<RegistrationModel>();
        var state = new BormState(form, new BormModelDataAccessor(model));

        state.SetValue("FirstName", "Grace");
        state.SetValue("Age", 41);
        state.SetServerError("FirstName", "taken");
        Assert.IsTrue(state.IsFormDirty);

        state.Reset();

        Assert.AreEqual("Ada", model.FirstName);
        Assert.AreEqual(30, model.Age);
        Assert.IsFalse(state.IsFormDirty);
        Assert.IsFalse(state.IsTouched("FirstName"));
        Assert.IsEmpty(state.AllMessages);
        Assert.AreEqual(0, state.SubmitCount);
    }

    [TestMethod]
    public void Reset_removes_items_added_after_construction()
    {
        var model = new RegistrationModel();
        var form = BormSchemaGenerator.Generate<RegistrationModel>();
        var state = new BormState(form, new BormModelDataAccessor(model));

        state.AddArrayItem(form.FindField("Items")!, "Items");
        Assert.ContainsSingle(model.Items);

        state.Reset();

        Assert.IsEmpty(model.Items);
    }

    [TestMethod]
    public void AcceptChanges_rebases_the_reset_point()
    {
        var model = new RegistrationModel { FirstName = "Ada" };
        var form = BormSchemaGenerator.Generate<RegistrationModel>();
        var state = new BormState(form, new BormModelDataAccessor(model));

        state.SetValue("FirstName", "Grace");
        state.AcceptChanges();
        Assert.IsFalse(state.IsFormDirty);

        state.SetValue("FirstName", "Hedy");
        state.Reset();

        Assert.AreEqual("Grace", model.FirstName);
    }
}

[TestClass]
public class ArrayStateTests
{
    private static (BormState State, RegistrationModel Model, BormFieldDefinition Items) Setup()
    {
        var model = new RegistrationModel();
        var form = BormSchemaGenerator.Generate<RegistrationModel>();
        var state = new BormState(form, new BormModelDataAccessor(model));
        return (state, model, form.FindField("Items")!);
    }

    [TestMethod]
    public void Insert_places_the_item_at_the_requested_index()
    {
        var (state, model, items) = Setup();
        state.AddArrayItem(items, "Items");
        state.SetValue("Items[0].Product", "second");

        var index = state.InsertArrayItem(items, "Items", 0);
        state.SetValue("Items[0].Product", "first");

        Assert.AreEqual(0, index);
        Assert.AreEqual("first", model.Items[0].Product);
        Assert.AreEqual("second", model.Items[1].Product);
    }

    [TestMethod]
    public void Removing_an_item_moves_the_following_items_messages_up_with_them()
    {
        var (state, _, items) = Setup();
        state.AddArrayItem(items, "Items");
        state.AddArrayItem(items, "Items");
        state.SetServerError("Items[1].Product", "second item is wrong");

        state.RemoveArrayItem("Items", 0);

        Assert.IsEmpty(state.MessagesFor("Items[1].Product"));
        Assert.ContainsSingle(state.MessagesFor("Items[0].Product"));
        Assert.AreEqual("second item is wrong", state.MessagesFor("Items[0].Product")[0].Message);
    }

    [TestMethod]
    public void Inserting_before_an_item_moves_its_messages_down()
    {
        var (state, _, items) = Setup();
        state.AddArrayItem(items, "Items");
        state.SetServerError("Items[0].Product", "wrong");

        state.InsertArrayItem(items, "Items", 0);

        Assert.ContainsSingle(state.MessagesFor("Items[1].Product"));
        Assert.IsEmpty(state.MessagesFor("Items[0].Product"));
    }

    [TestMethod]
    public void Min_and_max_item_counts_gate_add_and_remove()
    {
        var model = new RegistrationModel();
        var form = BormBuilder.For<RegistrationModel>()
            .Array(x => x.Items, i => i.Field(l => l.Product), f => f.Items(min: 1, max: 2))
            .Build();
        var state = new BormState(form, new BormModelDataAccessor(model));
        var items = form.FindField("Items")!;

        state.AddArrayItem(items, "Items");
        Assert.IsFalse(state.CanRemoveArrayItem(items, "Items")); // at the minimum
        Assert.IsTrue(state.CanAddArrayItem(items, "Items"));

        state.AddArrayItem(items, "Items");
        Assert.IsFalse(state.CanAddArrayItem(items, "Items")); // at the maximum
        Assert.IsTrue(state.CanRemoveArrayItem(items, "Items"));
    }
}

[TestClass]
public class ConditionalStateTests
{
    [TestMethod]
    public void ClearOnHide_empties_a_field_that_becomes_invisible()
    {
        var form = BormBuilder.For<SignupModel>()
            .Field(x => x.IsBusiness)
            .Field(x => x.CompanyName, f => f
                .VisibleWhen(nameof(SignupModel.IsBusiness), BormConditionOperator.IsTrue)
                .ClearOnHide())
            .Build();

        var model = new SignupModel { IsBusiness = true };
        var state = new BormState(form, new BormModelDataAccessor(model));

        state.SetValue("CompanyName", "Acme");
        Assert.AreEqual("Acme", model.CompanyName);

        state.SetValue("IsBusiness", false);

        Assert.IsNull(model.CompanyName);
    }

    [TestMethod]
    public void A_value_survives_a_hidden_field_without_ClearOnHide()
    {
        var form = BormBuilder.For<SignupModel>()
            .Field(x => x.IsBusiness)
            .Field(x => x.CompanyName, f => f.VisibleWhen(nameof(SignupModel.IsBusiness), BormConditionOperator.IsTrue))
            .Build();

        var model = new SignupModel { IsBusiness = true, CompanyName = "Acme" };
        var state = new BormState(form, new BormModelDataAccessor(model));

        state.SetValue("IsBusiness", false);

        Assert.AreEqual("Acme", model.CompanyName);
    }

    [TestMethod]
    public async Task RequiredWhen_only_bites_while_its_condition_holds()
    {
        var form = BormBuilder.For<SignupModel>()
            .Field(x => x.IsBusiness)
            .Field(x => x.CompanyName, f => f.RequiredWhen(nameof(SignupModel.IsBusiness), BormConditionOperator.IsTrue))
            .Build();

        var model = new SignupModel { IsBusiness = false };
        var state = new BormState(form, new BormModelDataAccessor(model));

        Assert.IsTrue(await state.ValidateAsync());

        state.SetValue("IsBusiness", true);

        Assert.IsFalse(await state.ValidateAsync());
        Assert.ContainsSingle(state.MessagesFor("CompanyName"));
    }

    [TestMethod]
    public void IsRequired_reflects_the_conditional_rule()
    {
        var form = BormBuilder.For<SignupModel>()
            .Field(x => x.IsBusiness)
            .Field(x => x.CompanyName, f => f.RequiredWhen(nameof(SignupModel.IsBusiness), BormConditionOperator.IsTrue))
            .Build();

        var model = new SignupModel();
        var state = new BormState(form, new BormModelDataAccessor(model));
        var company = form.FindField("CompanyName")!;

        Assert.IsFalse(state.IsRequired(company));
        state.SetValue("IsBusiness", true);
        Assert.IsTrue(state.IsRequired(company));
    }

    [TestMethod]
    public void ReadOnly_is_not_the_same_as_disabled()
    {
        // A read-only control must stay focusable and readable by assistive tech, so it is never
        // rendered as `disabled`.
        var form = BormBuilder.Create().Text("name", f => f.ReadOnly()).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var field = form.FindField("name")!;

        Assert.IsTrue(state.IsReadOnly(field));
        Assert.IsFalse(state.IsDisabled(field));
    }

    [TestMethod]
    public void Form_level_readonly_covers_every_field()
    {
        var form = BormBuilder.Create().Text("name").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor()) { ReadOnly = true };

        Assert.IsTrue(state.IsReadOnly(form.FindField("name")!));
    }
}

[TestClass]
public class OptionsProviderTests
{
    [TestMethod]
    public async Task Loads_options_on_demand_and_caches_them()
    {
        var calls = 0;
        var form = BormBuilder.Create()
            .Select("country", f => f.OptionsFrom(_ =>
            {
                calls++;
                return new ValueTask<IReadOnlyList<BormSelectOption>>(
                    new List<BormSelectOption> { new("fr", "France") });
            }))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var field = form.FindField("country")!;

        await state.EnsureOptionsAsync(field, "country");
        await state.EnsureOptionsAsync(field, "country");

        Assert.AreEqual(1, calls);
        Assert.AreEqual("France", state.OptionsFor(field, "country")[0].Label);
    }

    [TestMethod]
    public async Task A_dependency_change_reloads_the_options_and_clears_the_stale_selection()
    {
        var form = BormBuilder.Create()
            .Select("country")
            .Select("city", f => f.OptionsFrom(ctx =>
            {
                var country = ctx.Value("country") as string;
                IReadOnlyList<BormSelectOption> options = country == "fr"
                    ? [new("paris", "Paris")]
                    : [new("london", "London")];
                return new ValueTask<IReadOnlyList<BormSelectOption>>(options);
            }, "country"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var city = form.FindField("city")!;

        state.SetValue("country", "fr");
        await state.EnsureOptionsAsync(city, "city");
        state.SetValue("city", "paris");
        Assert.AreEqual("Paris", state.OptionsFor(city, "city")[0].Label);

        state.SetValue("country", "uk");

        Assert.IsNull(state.GetValue("city")); // the stale selection is dropped
        await state.EnsureOptionsAsync(city, "city");
        Assert.AreEqual("London", state.OptionsFor(city, "city")[0].Label);
    }

    [TestMethod]
    public void Static_options_are_returned_when_there_is_no_provider()
    {
        var form = BormBuilder.Create().Select("size", f => f.Options(("s", "Small"), ("l", "Large"))).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        Assert.AreEqual(2, state.OptionsFor(form.FindField("size")!, "size").Count);
    }
}

[TestClass]
public class WizardTests
{
    private static BormDefinition ThreeSteps() => BormBuilder.For<SignupModel>()
        .Field(x => x.Email)
        .Field(x => x.IsBusiness)
        .Field(x => x.CompanyName)
        .Field(x => x.Country)
        .Step("contact", s => s.Title("Contact").Fields("Email"))
        .Step("company", s => s.Title("Company").Fields("CompanyName")
            .VisibleWhen(new BormFieldCondition(nameof(SignupModel.IsBusiness), BormConditionOperator.IsTrue)))
        .Step("done", s => s.Title("Done").Fields("Country"))
        .Build();

    [TestMethod]
    public void Step_numbering_skips_hidden_steps()
    {
        var state = new BormState(ThreeSteps(), new BormModelDataAccessor(new SignupModel()));

        Assert.AreEqual(2, state.VisibleSteps.Count);
        Assert.AreEqual(1, state.CurrentStepNumber);

        state.GoToStep("done");
        // "done" is the third declared step but the second visible one.
        Assert.AreEqual(2, state.CurrentStepNumber);
    }

    [TestMethod]
    public void GoToStep_refuses_a_hidden_step()
    {
        var state = new BormState(ThreeSteps(), new BormModelDataAccessor(new SignupModel()));

        state.GoToStep("company");

        Assert.AreEqual(0, state.CurrentStepIndex);
    }

    [TestMethod]
    public async Task Next_skips_a_hidden_step()
    {
        var model = new SignupModel { Email = "a@b.com", IsBusiness = false };
        var state = new BormState(ThreeSteps(), new BormModelDataAccessor(model));

        Assert.IsTrue(await state.NextStepAsync());

        Assert.AreEqual("done", state.CurrentStep!.Id);
        Assert.IsTrue(state.IsLastStep);
    }

    [TestMethod]
    public async Task A_step_only_reports_the_errors_of_its_own_fields()
    {
        var form = BormBuilder.For<SignupModel>()
            .Field(x => x.Email, f => f.Required())
            .Field(x => x.Country, f => f.Required())
            .Step("one", s => s.Fields("Email"))
            .Step("two", s => s.Fields("Country"))
            .Build();

        var state = new BormState(form, new BormModelDataAccessor(new SignupModel { Email = "a@b.com" }));

        Assert.IsTrue(await state.ValidateStepAsync());
        Assert.IsEmpty(state.MessagesFor("Country"));
    }
}

[TestClass]
public class MessageOrderingTests
{
    [TestMethod]
    public void Messages_are_ordered_by_their_position_in_the_schema()
    {
        var form = BormBuilder.Create()
            .Text("first", f => f.Required())
            .Text("second", f => f.Required())
            .Text("third", f => f.Required())
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.SetServerError("third", "c");
        state.SetServerError("first", "a");
        state.SetServerError("second", "b");

        Assert.AreSequenceEqual(["a", "b", "c"], state.OrderedMessages().Select(m => m.Message));
    }
}
