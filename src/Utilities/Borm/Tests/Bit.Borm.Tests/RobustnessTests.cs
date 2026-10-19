namespace Bit.Borm.Tests;

[TestClass]
public class RequiredSemanticsTests
{
    [TestMethod]
    public async Task A_required_checkbox_must_be_ticked()
    {
        // HTML's own `required` on a checkbox means "must be checked", and "I accept the terms" is
        // the case every form has. Treating `false` as a present value would make the rule useless.
        var form = BormBuilder.Create().Checkbox("accept", f => f.Required("Please accept.")).Build();
        var data = new BormDictionaryDataAccessor();
        var validator = new BormValidator();

        data.SetValue("accept", false);
        Assert.Contains(m => m.Message == "Please accept.", await validator.ValidateAsync(form, data));

        data.SetValue("accept", true);
        Assert.IsEmpty(await validator.ValidateAsync(form, data));
    }

    [TestMethod]
    public async Task A_required_non_checkbox_boolean_is_satisfied_by_false()
    {
        // Outside a checkbox, `false` is a perfectly good answer.
        var form = BormBuilder.Create()
            .Radio("optIn", f => f.Options(("true", "Yes"), ("false", "No")).Required())
            .Build();

        var data = new BormDictionaryDataAccessor();
        data.SetValue("optIn", false);

        Assert.IsEmpty(await new BormValidator().ValidateAsync(form, data));
    }
}

[TestClass]
public class ConversionFeedbackTests
{
    [TestMethod]
    public void An_unconvertible_entry_is_reported_instead_of_vanishing()
    {
        var model = new TypedModel { Count = 7 };
        var form = BormSchemaGenerator.Generate<TypedModel>();
        var state = new BormState(form, new BormModelDataAccessor(model));

        state.SetValue("Count", "not-a-number");

        // The model keeps its last valid value, and the user is told why their entry did not stick.
        Assert.AreEqual(7, model.Count);
        var message = Assert.ContainsSingle(state.MessagesFor("Count"));
        Assert.Contains("not-a-number", message.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_valid_entry_clears_the_earlier_conversion_error()
    {
        var model = new TypedModel();
        var form = BormSchemaGenerator.Generate<TypedModel>();
        var state = new BormState(form, new BormModelDataAccessor(model));

        state.SetValue("Count", "nope");
        Assert.IsNotEmpty(state.MessagesFor("Count"));

        state.SetValue("Count", 12);

        Assert.IsEmpty(state.MessagesFor("Count"));
        Assert.AreEqual(12, model.Count);
    }

    [TestMethod]
    public async Task A_conversion_error_survives_revalidation()
    {
        var model = new TypedModel();
        var form = BormSchemaGenerator.Generate<TypedModel>();
        var state = new BormState(form, new BormModelDataAccessor(model));

        state.SetValue("Count", "nope");

        // A full validation pass rebuilds the message set; the conversion problem is still real.
        Assert.IsFalse(await state.ValidateAsync());
        Assert.IsNotEmpty(state.MessagesFor("Count"));
    }

    [TestMethod]
    [DataRow("a.b")]
    [DataRow("a[0]")]
    public void The_dictionary_store_reshapes_rather_than_throwing(string path)
    {
        // "a" holds a string, so it cannot also be the object (or list) the next path segment needs.
        // The old code cast blindly and threw; now the scalar is replaced by the required container.
        var data = new BormDictionaryDataAccessor();
        data.SetValue("a", "scalar");

        data.SetValue(path, 1);

        Assert.IsFalse(data.LastWriteFailed);
        Assert.AreEqual(1, data.GetValue(path));
    }
}

[TestClass]
public class OptionsCancellationTests
{
    [TestMethod]
    public async Task A_superseded_options_load_is_cancelled()
    {
        var started = new TaskCompletionSource();
        var observed = CancellationToken.None;

        var form = BormBuilder.Create()
            .Select("country")
            .Select("city", f => f.OptionsFrom(async ctx =>
            {
                observed = ctx.CancellationToken;
                started.TrySetResult();
                await Task.Delay(Timeout.Infinite, ctx.CancellationToken);
                return (IReadOnlyList<BormSelectOption>)Array.Empty<BormSelectOption>();
            }, "country"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var city = form.FindField("city")!;

        var load = state.EnsureOptionsAsync(city, "city").AsTask();
        await started.Task;

        state.InvalidateOptions("city");
        await load;

        Assert.IsTrue(observed.IsCancellationRequested);
    }

    [TestMethod]
    public async Task Disposing_the_state_cancels_an_in_flight_load()
    {
        var started = new TaskCompletionSource();
        var observed = CancellationToken.None;

        var form = BormBuilder.Create()
            .Select("city", f => f.OptionsFrom(async ctx =>
            {
                observed = ctx.CancellationToken;
                started.TrySetResult();
                await Task.Delay(Timeout.Infinite, ctx.CancellationToken);
                return (IReadOnlyList<BormSelectOption>)Array.Empty<BormSelectOption>();
            }))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var load = state.EnsureOptionsAsync(form.FindField("city")!, "city").AsTask();
        await started.Task;

        state.Dispose();
        await load;

        Assert.IsTrue(observed.IsCancellationRequested);
    }
}

[TestClass]
public class ConditionalRuleTests
{
    [TestMethod]
    public async Task When_scopes_a_group_of_rules_to_a_condition()
    {
        var form = BormBuilder.For<SignupModel>()
            .Field(x => x.IsBusiness)
            .Field(x => x.CompanyName, f => f
                .When(nameof(SignupModel.IsBusiness), BormConditionOperator.IsTrue, null, w => w
                    .Required("Business accounts need a company name.")
                    .MinLength(3)))
            .Build();

        var model = new SignupModel { CompanyName = "X" };
        var validator = new BormValidator();

        // Personal account: neither rule applies.
        Assert.IsEmpty(await validator.ValidateAsync(form, new BormModelDataAccessor(model)));

        model.IsBusiness = true;
        var messages = await validator.ValidateAsync(form, new BormModelDataAccessor(model));
        Assert.Contains(m => m.FieldPath == "CompanyName", messages);
    }
}

[TestClass]
public class StepPathTests
{
    [TestMethod]
    public async Task A_step_can_own_a_nested_field()
    {
        var form = BormBuilder.For<RegistrationModel>()
            .Field(x => x.FirstName, f => f.Required())
            .Field(x => x.Address.City, f => f.Required("City is required."))
            .Step("one", s => s.Field<RegistrationModel>(x => x.FirstName))
            .Step("two", s => s.Field<RegistrationModel>(x => x.Address.City))
            .Build();

        Assert.AreEqual("Address.City", form.Steps[1].Fields.Single());

        var model = new RegistrationModel { FirstName = "Ada" };
        var state = new BormState(form, new BormModelDataAccessor(model));

        // Step one passes, so we advance; step two then reports its own nested field.
        Assert.IsTrue(await state.NextStepAsync());
        Assert.IsFalse(await state.ValidateStepAsync());
        Assert.AreEqual("City is required.", state.MessagesFor("Address.City")[0].Message);
    }

    [TestMethod]
    public void Expression_paths_are_available_outside_the_builder()
    {
        Assert.AreEqual("Address.City", BormExpressionPath.Of<RegistrationModel, string>(x => x.Address.City));
        Assert.AreEqual("Age", BormExpressionPath.Of<RegistrationModel, object?>(x => x.Age));
    }
}
