namespace Bit.Borm.Tests;

/// <summary>
/// The state API a page reaches for once a form outlives a single render: rebasing onto a saved
/// record, triggering one field, asking for everything known about a field at once.
/// </summary>
[TestClass]
public class ResetWithValuesTests
{
    private static BormState Form(RegistrationModel model)
        => new(BormSchemaGenerator.Generate<RegistrationModel>(), new BormModelDataAccessor(model));

    [TestMethod]
    public void The_new_values_are_written_and_become_the_baseline()
    {
        var model = new RegistrationModel { FirstName = "Ada" };
        var state = Form(model);

        state.Reset(new Dictionary<string, object?> { ["FirstName"] = "Grace", ["Age"] = 45 });

        Assert.AreEqual("Grace", model.FirstName);
        Assert.AreEqual(45, model.Age);
        // Rebased, not merely written: the form opened on "Grace" as far as anything reading dirtiness
        // is concerned.
        Assert.IsFalse(state.IsFormDirty);
    }

    [TestMethod]
    public void Reset_afterwards_goes_back_to_the_rebased_values_not_the_original_ones()
    {
        var model = new RegistrationModel { FirstName = "Ada" };
        var state = Form(model);

        state.Reset(new Dictionary<string, object?> { ["FirstName"] = "Grace" });
        state.SetValue("FirstName", "Katherine");
        state.Reset();

        Assert.AreEqual("Grace", model.FirstName);
    }

    [TestMethod]
    public void Everything_the_previous_session_accumulated_is_cleared()
    {
        var model = new RegistrationModel();
        var state = Form(model);

        state.SetServerError("Email", "already registered");
        state.MarkTouched("Email");
        state.RegisterSubmitAttempt();

        state.Reset(new Dictionary<string, object?> { ["Email"] = "ada@example.com" });

        Assert.IsEmpty(state.MessagesFor("Email"));
        Assert.IsFalse(state.IsTouched("Email"));
        Assert.AreEqual(0, state.SubmitCount);
        Assert.IsFalse(state.HasValidated);
    }

    [TestMethod]
    public void A_field_the_caller_did_not_name_keeps_its_value_and_is_baselined_at_it()
    {
        var model = new RegistrationModel { FirstName = "Ada", Email = "ada@example.com" };
        var state = Form(model);

        state.Reset(new Dictionary<string, object?> { ["FirstName"] = "Grace" });

        Assert.AreEqual("ada@example.com", model.Email);
        Assert.IsFalse(state.IsDirty("Email"));
    }

    [TestMethod]
    public void Computed_fields_are_seeded_against_the_new_data_rather_than_left_dirty()
    {
        var model = new OrderModel();
        var form = BormBuilder.For<OrderModel>()
            .Field(x => x.FirstName)
            .Field(x => x.LastName)
            .Computed(x => x.FullName, m => $"{m.FirstName} {m.LastName}".Trim(),
                dependsOn: [nameof(OrderModel.FirstName), nameof(OrderModel.LastName)])
            .Build();
        var state = new BormState(form, new BormModelDataAccessor(model));

        state.Reset(new Dictionary<string, object?> { ["FirstName"] = "Grace", ["LastName"] = "Hopper" });

        Assert.AreEqual("Grace Hopper", model.FullName);
        Assert.IsFalse(state.IsFormDirty);
    }

    [TestMethod]
    public void A_null_collection_is_rejected_rather_than_silently_doing_nothing()
        => Assert.ThrowsExactly<ArgumentNullException>(() => Form(new RegistrationModel()).Reset(null!));
}

[TestClass]
public class ValidateFieldByPathTests
{
    private static BormState Form()
        => new(BormSchemaGenerator.Generate<RegistrationModel>(),
            new BormModelDataAccessor(new RegistrationModel()));

    [TestMethod]
    public async Task A_path_is_enough_to_validate_one_field()
    {
        var state = Form();

        Assert.IsTrue(await state.ValidateFieldAsync("FirstName"));

        Assert.IsNotEmpty(state.MessagesFor("FirstName"));
        // Only that field: nothing else was judged.
        Assert.IsEmpty(state.MessagesFor("Email"));
    }

    [TestMethod]
    public async Task A_nested_path_resolves_through_the_schema()
    {
        var state = Form();

        Assert.IsTrue(await state.ValidateFieldAsync("Address.City"));

        Assert.IsNotEmpty(state.MessagesFor("Address.City"));
    }

    [TestMethod]
    public async Task An_unknown_path_reports_false_instead_of_throwing()
        => Assert.IsFalse(await Form().ValidateFieldAsync("NotAField"));

    [TestMethod]
    public async Task An_empty_path_is_a_programming_error()
        => await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await Form().ValidateFieldAsync(""));
}

[TestClass]
public class FieldStateTests
{
    [TestMethod]
    public async Task Everything_known_about_a_field_arrives_in_one_read()
    {
        var model = new RegistrationModel { FirstName = "Ada" };
        var state = new BormState(BormSchemaGenerator.Generate<RegistrationModel>(),
            new BormModelDataAccessor(model));

        state.SetValue("FirstName", "");
        await state.ValidateFieldAsync("FirstName");

        var field = state.GetFieldState("FirstName");

        Assert.IsTrue(field.IsTouched);
        Assert.IsTrue(field.IsDirty);
        Assert.IsTrue(field.IsInvalid);
        Assert.IsNotNull(field.Error);
    }

    [TestMethod]
    public void An_untouched_valid_field_reports_nothing_at_all()
    {
        var state = new BormState(BormSchemaGenerator.Generate<RegistrationModel>(),
            new BormModelDataAccessor(new RegistrationModel()));

        var field = state.GetFieldState("FirstName");

        Assert.IsFalse(field.IsTouched);
        Assert.IsFalse(field.IsDirty);
        Assert.IsFalse(field.IsInvalid);
        Assert.IsNull(field.Error);
        Assert.IsEmpty(field.Messages);
    }

    [TestMethod]
    public void A_warning_does_not_make_a_field_invalid()
    {
        var form = BormBuilder.Create().Text("a").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.SetServerErrors([new BormValidationMessage("a", "Looks unusual.", BormValidationSeverity.Warning)]);

        var field = state.GetFieldState("a");

        Assert.IsFalse(field.IsInvalid);
        Assert.IsNull(field.Error);
        Assert.ContainsSingle(field.Messages);
    }
}

/// <summary>
/// A field breaking four rules at once reads as four problems rather than one field to go and fix.
/// React Hook Form defaults to showing the first; this library offers it as a choice.
/// </summary>
[TestClass]
public class SingleErrorPerFieldTests
{
    private static BormDefinition Schema() => BormBuilder.Create()
        .Text("password", f => f
            .Required()
            .MinLength(8)
            .Pattern(@"^\d+$", "Digits only."))
        .Build();

    [TestMethod]
    public async Task Off_by_default_every_broken_rule_is_reported()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());
        state.SetValue("password", "ab");

        await state.ValidateAsync();

        Assert.IsTrue(state.MessagesFor("password").Count > 1);
    }

    [TestMethod]
    public async Task On_only_the_first_error_is_kept()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor())
        {
            SingleErrorPerField = true
        };
        state.SetValue("password", "ab");

        await state.ValidateAsync();

        Assert.ContainsSingle(state.MessagesFor("password"));
    }

    [TestMethod]
    public async Task The_verdict_is_unchanged_only_the_reporting_is()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor())
        {
            SingleErrorPerField = true
        };
        state.SetValue("password", "ab");

        Assert.IsFalse(await state.ValidateAsync());
        Assert.IsFalse(state.IsValid);
    }

    [TestMethod]
    public async Task A_warning_still_sits_alongside_the_error()
    {
        var form = BormBuilder.Create()
            .Text("a", f => f
                .Required()
                .MinLength(5)
                .Validate(new BormDelegateRule(_ =>
                    BormRuleResult.Fail("Looks unusual.", BormValidationSeverity.Warning))))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor()) { SingleErrorPerField = true };
        state.SetValue("a", "ab");

        await state.ValidateAsync();

        var messages = state.MessagesFor("a");
        Assert.ContainsSingle(m => m.Severity == BormValidationSeverity.Error, messages);
        Assert.ContainsSingle(m => m.Severity == BormValidationSeverity.Warning, messages);
    }
}

/// <summary>
/// Swap and clear: the two repeater operations that were missing, and that doing by hand with the
/// existing ones gets subtly wrong.
/// </summary>
[TestClass]
public class RepeaterSwapAndClearTests
{
    private static (BormState State, RegistrationModel Model) FormWithRows(int rows)
    {
        var model = new RegistrationModel();
        var form = BormSchemaGenerator.Generate<RegistrationModel>();
        var state = new BormState(form, new BormModelDataAccessor(model));
        var items = form.FindField("Items")!;

        for (var i = 0; i < rows; i++)
        {
            state.AddArrayItem(items, "Items");
            state.SetValue($"Items[{i}].Product", $"P{i}");
        }
        return (state, model);
    }

    [TestMethod]
    public void Swapping_exchanges_two_rows_and_leaves_the_rest_where_they_were()
    {
        var (state, model) = FormWithRows(4);

        state.SwapArrayItems("Items", 0, 3);

        Assert.AreSequenceEqual(["P3", "P1", "P2", "P0"], model.Items.Select(i => i.Product));
    }

    [TestMethod]
    public void A_swapped_row_takes_its_errors_with_it()
    {
        var (state, _) = FormWithRows(3);
        state.SetServerError("Items[0].Product", "duplicate");

        state.SwapArrayItems("Items", 0, 2);

        Assert.IsEmpty(state.MessagesFor("Items[0].Product"));
        Assert.ContainsSingle(state.MessagesFor("Items[2].Product"));
    }

    [TestMethod]
    public void Swapping_differs_from_moving_which_shuffles_everything_in_between()
    {
        var (swapState, swapModel) = FormWithRows(4);
        var (moveState, moveModel) = FormWithRows(4);

        swapState.SwapArrayItems("Items", 0, 3);
        moveState.MoveArrayItem("Items", 0, 3);

        Assert.AreSequenceEqual(["P3", "P1", "P2", "P0"], swapModel.Items.Select(i => i.Product));
        Assert.AreSequenceEqual(["P1", "P2", "P3", "P0"], moveModel.Items.Select(i => i.Product));
    }

    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(0, 9)]
    [DataRow(1, 1)]
    public void An_impossible_swap_is_ignored(int first, int second)
    {
        var (state, model) = FormWithRows(3);

        state.SwapArrayItems("Items", first, second);

        Assert.AreSequenceEqual(["P0", "P1", "P2"], model.Items.Select(i => i.Product));
    }

    [TestMethod]
    public void Clearing_empties_the_list_and_everything_keyed_to_its_rows()
    {
        var (state, model) = FormWithRows(3);
        state.SetServerError("Items[1].Product", "duplicate");
        state.MarkTouched("Items[1].Product");

        state.ClearArrayItems("Items");

        Assert.IsEmpty(model.Items);
        Assert.IsEmpty(state.MessagesFor("Items[1].Product"));
        Assert.IsFalse(state.IsTouched("Items[1].Product"));
    }

    [TestMethod]
    public void Clearing_a_list_the_form_opened_with_is_a_change()
    {
        var model = new RegistrationModel { Items = { new LineItem { Product = "P0" } } };
        var state = new BormState(BormSchemaGenerator.Generate<RegistrationModel>(),
            new BormModelDataAccessor(model));

        state.ClearArrayItems("Items");

        Assert.IsTrue(state.IsDirty("Items"));
        Assert.IsTrue(state.IsFormDirty);
    }

    [TestMethod]
    public void Clearing_rows_added_since_construction_puts_the_list_back_where_it_started()
    {
        // Dirtiness is a comparison, so emptying a list that opened empty is not a change — the same
        // reasoning that makes a row added and removed again leave the form clean.
        var (state, _) = FormWithRows(3);

        state.ClearArrayItems("Items");

        Assert.IsFalse(state.IsDirty("Items"));
    }

    [TestMethod]
    public void Clearing_an_already_empty_list_does_nothing()
    {
        var model = new RegistrationModel();
        var state = new BormState(BormSchemaGenerator.Generate<RegistrationModel>(),
            new BormModelDataAccessor(model));

        state.ClearArrayItems("Items");

        Assert.IsFalse(state.IsFormDirty);
    }

    [TestMethod]
    public void Clearing_refreshes_a_total_computed_over_the_list()
    {
        var model = new OrderModel();
        var form = BormBuilder.For<OrderModel>()
            .Array(x => x.Lines, line => line.Field(l => l.UnitPrice))
            .Computed(x => x.Total, m => m.Lines.Sum(l => l.UnitPrice), dependsOn: [nameof(OrderModel.Lines)])
            .Build();
        var state = new BormState(form, new BormModelDataAccessor(model));

        state.AddArrayItem(form.FindField("Lines")!, "Lines");
        state.SetValue("Lines[0].UnitPrice", 12m);
        Assert.AreEqual(12m, model.Total);

        state.ClearArrayItems("Lines");

        Assert.AreEqual(0m, model.Total);
    }
}

/// <summary>
/// A derived value is a change to the form like any other. Everything a typed-in value triggers has to
/// happen for a computed one too, or the schema's own calculations are the one kind of change the rest
/// of the engine cannot see.
/// </summary>
[TestClass]
public class ComputedValueSideEffectTests
{
    [TestMethod]
    public void A_field_hidden_by_a_computed_value_is_cleared()
    {
        var form = BormBuilder.Create()
            .Number("price")
            .Number("quantity")
            .Number("total", f => f.Computed(
                ctx => Convert.ToDouble(ctx.Sibling("price") ?? 0d) * Convert.ToDouble(ctx.Sibling("quantity") ?? 0d),
                "price", "quantity"))
            .Text("approver", f => f
                .VisibleWhen("total", BormConditionOperator.GreaterThan, 100d)
                .ClearOnHide())
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("price", 60d);
        state.SetValue("quantity", 2d);
        state.SetValue("approver", "Ada");
        Assert.AreEqual("Ada", state.GetValue("approver"));

        // The total drops below the threshold, hiding the approver — through a computed value, which is
        // the only path that used to skip the sweep.
        state.SetValue("quantity", 1d);

        Assert.IsNull(state.GetValue("approver"));
    }

    [TestMethod]
    public void A_change_to_a_computed_value_is_reported_through_FieldChanged()
    {
        var form = BormBuilder.Create()
            .Text("first")
            .Text("full", f => f.Computed(ctx => ctx.Sibling("first"), "first"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var changed = new List<string>();
        state.FieldChanged += changed.Add;

        state.SetValue("first", "Ada");

        Assert.Contains("first", changed);
        Assert.Contains("full", changed);
    }

    [TestMethod]
    public async Task Options_that_depend_on_a_computed_value_reload_when_it_changes()
    {
        var loads = 0;
        var form = BormBuilder.Create()
            .Integer("a")
            .Integer("doubled", f => f.Computed(ctx => Convert.ToInt32(ctx.Sibling("a") ?? 0) * 2, "a"))
            .Select("tier", f => f.OptionsFrom(_ =>
            {
                loads++;
                return ValueTask.FromResult<IReadOnlyList<BormSelectOption>>(
                    [new BormSelectOption("x", "X")]);
            }, "doubled"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var tier = form.FindField("tier")!;

        await state.EnsureOptionsAsync(tier, "tier");
        Assert.AreEqual(1, loads);

        state.SetValue("a", 5);
        await state.EnsureOptionsAsync(tier, "tier");

        Assert.AreEqual(2, loads);
    }
}
