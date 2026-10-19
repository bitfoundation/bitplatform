using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// Adding or removing a row changes the value the list binds to as surely as typing does, so it has
/// to run the same sweeps. Emptying a repeater used to tell only half the engine about itself.
/// </summary>
[TestClass]
public class ArrayChangePropagationTests
{
    private static BormDefinition WithHiddenSummary() => BormBuilder.Create()
        .ArrayOf("lines", BormFieldType.Text)
        .Text("summary", f => f
            .VisibleWhen("lines", BormConditionOperator.IsNotEmpty)
            .ClearOnHide())
        .Build();

    private static BormState Form(BormDefinition form)
        => new(form, new BormDictionaryDataAccessor());

    [TestMethod]
    public void Emptying_the_list_clears_a_field_the_list_was_keeping_visible()
    {
        var form = WithHiddenSummary();
        var state = Form(form);
        var lines = form.FindField("lines")!;
        state.AddArrayItem(lines, "lines");
        state.SetValue("summary", "one line so far");

        state.RemoveArrayItem("lines", 0);

        Assert.IsNull(state.GetValue("summary"));
    }

    [TestMethod]
    public void Clearing_the_list_in_one_go_does_the_same()
    {
        var form = WithHiddenSummary();
        var state = Form(form);
        var lines = form.FindField("lines")!;
        state.AddArrayItem(lines, "lines");
        state.SetValue("summary", "one line so far");

        state.ClearArrayItems("lines");

        Assert.IsNull(state.GetValue("summary"));
    }

    [TestMethod]
    public async Task A_cascading_select_that_reads_the_list_reloads_when_a_row_is_added()
    {
        var loads = 0;
        var form = BormBuilder.Create()
            .ArrayOf("lines", BormFieldType.Text)
            .Select("courier", f => f.OptionsFrom(_ =>
            {
                loads++;
                return new ValueTask<IReadOnlyList<BormSelectOption>>(
                    new List<BormSelectOption> { new("dhl", "DHL") });
            }, "lines"))
            .Build();

        var state = Form(form);
        var courier = form.FindField("courier")!;

        // The renderer loads once, then reloads whenever the dependency changes.
        await state.EnsureOptionsAsync(courier, "courier");
        Assert.AreEqual(1, loads);

        state.AddArrayItem(form.FindField("lines")!, "lines");
        await state.EnsureOptionsAsync(courier, "courier");

        Assert.AreEqual(2, loads);
    }

    [TestMethod]
    public void A_computed_total_still_follows_the_rows()
    {
        var form = BormBuilder.Create()
            .ArrayOf("lines", BormFieldType.Integer)
            .Field("count", BormFieldType.Integer, f => f
                .ReadOnly()
                .Computed(ctx => ctx.Value("lines") is System.Collections.ICollection c ? c.Count : 0, "lines"))
            .Build();

        var state = Form(form);
        var lines = form.FindField("lines")!;

        state.AddArrayItem(lines, "lines");
        state.AddArrayItem(lines, "lines");
        Assert.AreEqual(2, state.GetValue("count"));

        state.RemoveArrayItem("lines", 0);
        Assert.AreEqual(1, state.GetValue("count"));
    }

    [TestMethod]
    public void Duplicating_a_row_announces_the_list_once_it_holds_the_copy()
    {
        var form = BormBuilder.Create()
            .Array("lines", row => row.Field("name", BormFieldType.Text),
                f => f.Attr("duplicable", true))
            .Build();

        var state = Form(form);
        var lines = form.FindField("lines")!;
        state.AddArrayItem(lines, "lines");
        state.SetValue("lines[0].name", "first");

        var announced = new List<string>();
        state.FieldChanged += announced.Add;
        state.DuplicateArrayItem(lines, "lines", 0);

        Assert.AreSequenceEqual(["lines"], announced);
        Assert.AreEqual("first", state.GetValue("lines[1].name"));
    }
}

/// <summary>
/// Enter in a text box submits the form the browser thinks it belongs to. Halfway through a wizard,
/// that is not what the user meant.
/// </summary>
[TestClass]
public class WizardImplicitSubmitTests : ComponentTestBase
{
    private static BormDefinition Wizard() => BormBuilder.Create()
        .Text("name", f => f.Required())
        .Text("city", f => f.Required())
        .Step("who", s => s.Title("Who").Fields("name"))
        .Step("where", s => s.Title("Where").Fields("city"))
        .Build();

    [TestMethod]
    public async Task Enter_on_a_step_that_is_not_the_last_advances_instead_of_submitting()
    {
        var state = new BormState(Wizard(), new BormDictionaryDataAccessor());
        state.SetValue("name", "Ada");
        var submitted = 0;
        var cut = Render<BormView>(p => p
            .Add(x => x.State, state)
            .Add(x => x.OnValidSubmit, _ => submitted++));

        await cut.Find("form").SubmitAsync();

        Assert.AreEqual(0, submitted);
        Assert.AreEqual(1, state.CurrentStepIndex);
        // Nothing was "submitted", so nothing counts as a submit attempt either.
        Assert.AreEqual(0, state.SubmitCount);
    }

    [TestMethod]
    public async Task Enter_on_a_step_with_an_unanswered_question_stays_put_rather_than_reporting_the_whole_form()
    {
        var state = new BormState(Wizard(), new BormDictionaryDataAccessor());
        var cut = Render<BormView>(p => p.Add(x => x.State, state));

        await cut.Find("form").SubmitAsync();

        Assert.AreEqual(0, state.CurrentStepIndex);
        // The step's own field is reported; the one on a page the user has not reached is not.
        Assert.ContainsSingle(state.MessagesFor("name"));
        Assert.IsEmpty(state.MessagesFor("city"));
    }

    [TestMethod]
    public async Task Enter_on_the_last_step_submits()
    {
        var state = new BormState(Wizard(), new BormDictionaryDataAccessor());
        state.SetValue("name", "Ada");
        state.SetValue("city", "London");
        await state.NextStepAsync();

        var submitted = 0;
        var cut = Render<BormView>(p => p
            .Add(x => x.State, state)
            .Add(x => x.OnValidSubmit, _ => submitted++));

        await cut.Find("form").SubmitAsync();

        Assert.AreEqual(1, submitted);
    }

    [TestMethod]
    public async Task A_single_page_form_is_untouched_by_any_of_this()
    {
        var form = BormBuilder.Create().Text("name", f => f.Required()).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("name", "Ada");

        var submitted = 0;
        var cut = Render<BormView>(p => p
            .Add(x => x.State, state)
            .Add(x => x.OnValidSubmit, _ => submitted++));

        await cut.Find("form").SubmitAsync();

        Assert.AreEqual(1, submitted);
    }
}

/// <summary>
/// Debouncing exists so a remote check can run once per pause instead of once per character. It had
/// been doing the waiting without ever running the check.
/// </summary>
[TestClass]
public class DebouncedAsyncValidationTests : ComponentTestBase
{
    [TestMethod]
    public async Task A_pause_runs_the_async_rules_the_keystrokes_skipped()
    {
        var checks = 0;
        var form = BormBuilder.Create()
            .Text("username", f => f
                .UpdateOnInput(debounceMilliseconds: 20)
                .MustAsync(ctx =>
                {
                    checks++;
                    return new ValueTask<bool>((ctx.Value as string) != "taken");
                }, "That username is taken."))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var cut = Render<BormView>(p => p.Add(x => x.State, state));
        var input = cut.Find("input#borm_username");

        await input.InputAsync(new() { Value = "taken" });
        await WaitFor(() => checks > 0);

        Assert.AreEqual("taken", state.GetValue("username"));
        Assert.ContainsSingle(state.MessagesFor("username"));
    }

    [TestMethod]
    public async Task An_undebounced_field_still_waits_for_blur_rather_than_calling_out_per_character()
    {
        var checks = 0;
        var form = BormBuilder.Create()
            .Text("username", f => f
                .UpdateOnInput()
                .MustAsync(_ =>
                {
                    checks++;
                    return new ValueTask<bool>(false);
                }, "Taken."))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var cut = Render<BormView>(p => p.Add(x => x.State, state));
        var input = cut.Find("input#borm_username");

        await input.InputAsync(new() { Value = "a" });
        await input.InputAsync(new() { Value = "ab" });

        Assert.AreEqual(0, checks);

        await input.BlurAsync(new());

        Assert.IsTrue(checks > 0);
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(10);
        Assert.IsTrue(condition(), "the condition never became true");
    }
}

/// <summary>The remaining round-2 corrections: server errors, step diagnostics and the busy button.</summary>
[TestClass]
public class ServerErrorAndDiagnosticTests : ComponentTestBase
{
    [TestMethod]
    public void Server_errors_do_not_take_a_conversion_failure_with_them()
    {
        var form = BormBuilder.Create()
            .Field("age", BormFieldType.Integer)
            .Text("name")
            .Build();
        var state = new BormState(form, new BormModelDataAccessor(new Person()));

        // "abc" cannot reach an int: the model keeps its last value and the form says so.
        state.SetValue("age", "abc");
        Assert.ContainsSingle(state.MessagesFor("age"));

        state.SetServerErrors([new BormValidationMessage("name", "That name is taken.")]);

        Assert.ContainsSingle(state.MessagesFor("name"));
        // The server has no opinion about a value that never reached it, and the box still shows it.
        Assert.ContainsSingle(state.MessagesFor("age"));
    }

    private sealed class Person
    {
        public int Age { get; set; }
        public string? Name { get; set; }
    }

    [TestMethod]
    public void A_step_condition_pointing_at_nothing_is_reported()
    {
        var form = BormBuilder.Create()
            .Text("name")
            .Step("who", s => s.Title("Who").Fields("name")
                .VisibleWhen("nope", BormConditionOperator.IsTrue))
            .Build();

        Assert.Contains(d => d.Message.Contains("VisibleWhen", StringComparison.Ordinal)
                                              && d.Message.Contains("nope", StringComparison.Ordinal), form.Validate());
    }

    [TestMethod]
    public void A_step_condition_that_does_resolve_is_not_reported()
    {
        var form = BormBuilder.Create()
            .Checkbox("business")
            .Text("vat")
            .Step("who", s => s.Title("Who").Fields("vat")
                .VisibleWhen("business", BormConditionOperator.IsTrue))
            .Build();

        Assert.DoesNotContain(d => d.Message.Contains("VisibleWhen", StringComparison.Ordinal), form.Validate());
    }

    [TestMethod]
    public async Task The_submit_button_says_it_is_working_rather_than_only_going_grey()
    {
        var gate = new TaskCompletionSource();
        var form = BormBuilder.Create()
            .Text("name", f => f.MustAsync(async _ => { await gate.Task; return true; }, "no"))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var cut = Render<BormView>(p => p.Add(x => x.State, state));

        var submitting = state.SubmitAsync();
        cut.Render();

        var button = cut.Find("button[type=submit]");
        Assert.AreEqual("Submitting…", button.TextContent.Trim());
        Assert.IsTrue(button.HasAttribute("disabled"));

        gate.SetResult();
        await submitting;
        cut.Render();

        Assert.AreEqual("Submit", cut.Find("button[type=submit]").TextContent.Trim());
    }
}
