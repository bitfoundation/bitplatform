namespace Bit.Borm.Tests;

/// <summary>
/// The order a field's rules run in, and what a field can say about its validity beyond "has a message":
/// a remote check is not paid for when the value already fails its own format, and "valid" is told apart
/// from "nobody has looked yet".
/// </summary>
[TestClass]
public class ValidationOrderAndStatusTests
{
    private static (BormState State, Func<int> Lookups) Username()
    {
        var lookups = 0;
        var form = BormBuilder.Create()
            .Text("username", f => f
                .MustAsync(async _ => { lookups++; await Task.Yield(); return true; }, "That username is taken.")
                .Pattern("^[a-z]+$", "Lowercase letters only."))
            .Build();
        return (new BormState(form, new BormDictionaryDataAccessor()), () => lookups);
    }

    [TestMethod]
    public async Task A_value_that_fails_its_own_format_is_not_sent_to_the_remote_check()
    {
        var (state, lookups) = Username();
        var field = state.Definition.FindField("username")!;

        state.SetValue("username", "Ada Lovelace");
        await state.ValidateFieldAsync(field, "username");

        Assert.AreEqual(0, lookups());
        Assert.AreEqual("Lowercase letters only.", state.MessagesFor("username").Single().Message);

        state.SetValue("username", "ada");
        await state.ValidateFieldAsync(field, "username");
        Assert.AreEqual(1, lookups());
    }

    [TestMethod]
    public async Task The_remote_check_can_be_made_to_run_regardless()
    {
        var (state, lookups) = Username();
        state.RunAsyncRulesOnInvalidFields = true;

        state.SetValue("username", "Ada Lovelace");
        await state.ValidateAsync();

        Assert.AreEqual(1, lookups());
    }

    [TestMethod]
    public async Task A_warning_does_not_stop_the_remote_check()
    {
        var lookups = 0;
        var form = BormBuilder.Create()
            .Text("username", f => f
                .Must("len({username}) >= 8", "Longer is safer.", BormValidationSeverity.Warning)
                .MustAsync(_ => { lookups++; return ValueTask.FromResult(true); }, "Taken."))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["username"] = "ada" }));

        await state.ValidateAsync();

        Assert.AreEqual(1, lookups);
    }

    [TestMethod]
    public async Task A_field_is_unvalidated_until_judged_and_valid_only_until_it_changes_again()
    {
        var form = BormBuilder.Create().Text("name", f => f.Required()).Text("city").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        Assert.AreEqual(BormFieldStatus.Unvalidated, state.GetFieldState("name").Status);

        state.SetValue("name", "Ada");
        await state.ValidateFieldAsync("name");
        Assert.AreEqual(BormFieldStatus.Valid, state.GetFieldState("name").Status);
        Assert.AreEqual(BormFieldStatus.Unvalidated, state.GetFieldState("city").Status);

        state.SetValue("name", "");
        Assert.AreEqual(BormFieldStatus.Unvalidated, state.GetFieldState("name").Status);

        await state.ValidateAsync();
        Assert.AreEqual(BormFieldStatus.Invalid, state.GetFieldState("name").Status);
        Assert.AreEqual(BormFieldStatus.Valid, state.GetFieldState("city").Status);

        state.Reset();
        Assert.AreEqual(BormFieldStatus.Unvalidated, state.GetFieldState("city").Status);
    }

    [TestMethod]
    public async Task A_keystroke_that_skipped_the_remote_check_has_not_judged_the_field()
    {
        var gate = new TaskCompletionSource<bool>();
        var form = BormBuilder.Create().Text("username", f => f.MustAsync(_ => new ValueTask<bool>(gate.Task), "Taken.")).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var field = form.FindField("username")!;
        state.SetValue("username", "ada");

        await state.ValidateFieldAsync(field, "username", includeAsync: false);
        Assert.AreEqual(BormFieldStatus.Unvalidated, state.GetFieldState("username").Status);

        var pending = state.ValidateFieldAsync(field, "username", includeAsync: true);
        Assert.AreEqual(BormFieldStatus.Pending, state.GetFieldState("username").Status);
        gate.SetResult(true);
        await pending;
        Assert.AreEqual(BormFieldStatus.Valid, state.GetFieldState("username").Status);
    }

    [TestMethod]
    public async Task A_row_keeps_its_status_when_rows_move_and_a_changed_row_loses_its_lists_too()
    {
        var form = BormBuilder.Create().Array("lines", l => l.Text("product")).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(new Dictionary<string, object?>
        {
            ["lines"] = new List<object?>
            {
                new Dictionary<string, object?> { ["product"] = "a" },
                new Dictionary<string, object?> { ["product"] = "b" }
            }
        }));
        await state.ValidateAsync();

        state.MoveArrayItem("lines", 0, 1);
        Assert.AreEqual(BormFieldStatus.Valid, state.GetFieldState("lines[1].product").Status);

        state.SetValue("lines[0].product", "c");
        Assert.AreEqual(BormFieldStatus.Unvalidated, state.GetFieldState("lines[0].product").Status);
        // The list's own rules (its size, its uniqueness) read every row, so it has not been judged either.
        Assert.AreEqual(BormFieldStatus.Unvalidated, state.GetFieldState("lines").Status);
        Assert.AreEqual(BormFieldStatus.Valid, state.GetFieldState("lines[1].product").Status);
    }

    [TestMethod]
    public async Task A_field_a_condition_hid_during_a_full_validation_was_not_judged_by_it()
    {
        var form = BormBuilder.Create()
            .Checkbox("company")
            .Text("vat", f => f.Required().VisibleWhen("company", BormConditionOperator.IsTrue))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        await state.ValidateAsync();
        Assert.AreEqual(BormFieldStatus.Valid, state.GetFieldState("company").Status);

        // Shown now, it has never been looked at: "valid" would claim a check that did not happen.
        state.SetValue("company", true);
        Assert.AreEqual(BormFieldStatus.Unvalidated, state.GetFieldState("vat").Status);

        await state.ValidateAsync();
        Assert.AreEqual(BormFieldStatus.Invalid, state.GetFieldState("vat").Status);
    }
}
