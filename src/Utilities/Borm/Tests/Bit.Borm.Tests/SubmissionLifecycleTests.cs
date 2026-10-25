using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// How a submit ends, said precisely: refused by the form, refused by the server, failed, cancelled or
/// saved — because a form that cannot tell those apart either claims success it did not have or loses
/// the user's work to a failure it did not report.
/// </summary>
[TestClass]
public class SubmissionLifecycleTests
{
    private static BormState Form(Dictionary<string, object?>? data = null)
        => new(BormBuilder.Create().Text("email", f => f.Required()).Build(),
            new BormDictionaryDataAccessor(data ?? new Dictionary<string, object?>()));

    [TestMethod]
    public async Task A_form_that_fails_its_own_rules_never_reaches_the_handler()
    {
        var state = Form();
        var called = false;

        var ok = await state.SubmitAsync(_ => { called = true; return Task.CompletedTask; });

        Assert.IsFalse(ok);
        Assert.IsFalse(called);
        Assert.AreEqual(BormSubmitOutcome.Invalid, state.SubmitOutcome);
        Assert.IsFalse(state.IsSubmitSuccessful);
    }

    [TestMethod]
    public async Task A_handler_that_records_the_servers_errors_is_a_rejected_submit_not_a_successful_one()
    {
        var state = Form(new Dictionary<string, object?> { ["email"] = "ada@example.com" });

        var ok = await state.SubmitAsync(s =>
        {
            s.SetServerErrors(new Dictionary<string, string[]> { ["email"] = ["Already registered."] });
            return Task.CompletedTask;
        });

        Assert.IsFalse(ok);
        Assert.AreEqual(BormSubmitOutcome.Rejected, state.SubmitOutcome);
        Assert.IsFalse(state.IsSubmitSuccessful);
        Assert.AreEqual(BormValidationSource.Server, state.MessagesFor("email").Single().Source);
    }

    [TestMethod]
    public async Task A_handler_that_throws_is_a_failed_submit_with_the_exception_kept_and_rethrown()
    {
        var state = Form(new Dictionary<string, object?> { ["email"] = "ada@example.com" });
        var failure = new HttpRequestException("503");

        var thrown = await Assert.ThrowsExactlyAsync<HttpRequestException>(() => state.SubmitAsync(_ => throw failure));

        Assert.AreSame(failure, thrown);
        Assert.AreEqual(BormSubmitOutcome.Failed, state.SubmitOutcome);
        Assert.AreSame(failure, state.SubmitError);
        Assert.IsFalse(state.IsSubmitting);
        // Nothing the user entered went anywhere.
        Assert.AreEqual("ada@example.com", state.GetValue("email"));

        // Trying again is just submitting again, and a success clears the failure.
        Assert.IsTrue(await state.SubmitAsync(_ => Task.CompletedTask));
        Assert.AreEqual(BormSubmitOutcome.Succeeded, state.SubmitOutcome);
        Assert.IsNull(state.SubmitError);
        Assert.IsTrue(state.IsSubmitSuccessful);
    }

    [TestMethod]
    public async Task Cancelling_a_submit_signals_the_handlers_token_and_ends_it_as_cancelled()
    {
        var state = Form(new Dictionary<string, object?> { ["email"] = "ada@example.com" });
        var started = new TaskCompletionSource();

        var submit = state.SubmitAsync(async (_, token) =>
        {
            Assert.AreEqual(token, state.SubmitCancellationToken);
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token);
        });
        await started.Task;
        state.CancelSubmit();

        Assert.IsFalse(await submit);
        Assert.AreEqual(BormSubmitOutcome.Cancelled, state.SubmitOutcome);
        Assert.AreEqual(CancellationToken.None, state.SubmitCancellationToken);
        Assert.IsNull(state.SubmitError);
    }

    [TestMethod]
    public async Task A_cancellation_the_handler_raises_by_itself_is_a_failure_not_a_cancellation()
    {
        // A timed-out HTTP call throws a cancellation of its own; nobody asked the submit to stop.
        var state = Form(new Dictionary<string, object?> { ["email"] = "ada@example.com" });

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => state.SubmitAsync(_ => throw new TaskCanceledException()));
        Assert.AreEqual(BormSubmitOutcome.Failed, state.SubmitOutcome);
    }

    [TestMethod]
    public async Task Disposing_the_form_cancels_a_save_still_running_for_it()
    {
        var state = Form(new Dictionary<string, object?> { ["email"] = "ada@example.com" });
        var started = new TaskCompletionSource();
        CancellationToken seen = default;

        var submit = state.SubmitAsync(async (_, token) =>
        {
            seen = token;
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token);
        });
        await started.Task;
        state.Dispose();

        Assert.IsFalse(await submit);
        Assert.IsTrue(seen.IsCancellationRequested);
    }

    [TestMethod]
    public async Task Fields_can_be_held_still_while_a_save_is_in_flight()
    {
        var state = Form(new Dictionary<string, object?> { ["email"] = "ada@example.com" });
        state.ReadOnlyWhileSubmitting = true;
        var field = state.Definition.FindField("email")!;
        var duringSave = false;

        await state.SubmitAsync(s => { duringSave = s.IsReadOnly(field); return Task.CompletedTask; });

        Assert.IsTrue(duringSave);
        Assert.IsFalse(state.IsReadOnly(field));
    }

    [TestMethod]
    public void Reset_forgets_how_the_last_submit_ended()
    {
        var state = Form();
        state.SubmitAsync().Wait();
        Assert.AreEqual(BormSubmitOutcome.Invalid, state.SubmitOutcome);

        state.Reset();
        Assert.AreEqual(BormSubmitOutcome.None, state.SubmitOutcome);
    }

    [TestMethod]
    public async Task An_error_the_user_makes_while_the_handler_runs_leaves_a_save_that_went_through_successful()
    {
        var state = new BormState(BormBuilder.Create().Number("qty").Build(), new BormDictionaryDataAccessor());
        var started = new TaskCompletionSource();
        var saving = new TaskCompletionSource();

        var submit = state.SubmitAsync(async _ =>
        {
            started.SetResult();
            await saving.Task;
        });
        await started.Task;
        // Typed into a form that is not held still while it saves.
        state.SetValue("qty", "abc");
        Assert.IsTrue(state.HasErrors);
        saving.SetResult();

        Assert.IsTrue(await submit);
        Assert.AreEqual(BormSubmitOutcome.Succeeded, state.SubmitOutcome);
    }

    [TestMethod]
    public async Task A_call_passing_no_valid_handler_means_what_it_did_before_handlers_could_take_a_token()
    {
        var state = Form();
        var invalid = 0;

        Assert.IsFalse(await state.SubmitAsync(null, _ => { invalid++; return Task.CompletedTask; }));
        Assert.IsFalse(await state.SubmitAsync(onValid: null, onInvalid: _ => { invalid++; return Task.CompletedTask; }));
        Assert.IsFalse(await state.SubmitAsync(null));
        Assert.AreEqual(2, invalid);

        // And a handler that takes the token is handed it for either outcome.
        var token = CancellationToken.None;
        Assert.IsFalse(await state.SubmitAsync((_, _) => Task.CompletedTask, (_, t) => { token = t; return Task.CompletedTask; }));
        Assert.IsTrue(token.CanBeCanceled);
    }
}

/// <summary>Server errors in the shape a validation response actually arrives in.</summary>
[TestClass]
public class ServerErrorMappingTests
{
    private static BormState Form()
        => new(BormBuilder.Create()
                .Text("Email")
                .Array("Lines", l => l.Text("Product"))
                .Build(),
            new BormDictionaryDataAccessor(new Dictionary<string, object?>
            {
                ["Lines"] = new List<object?> { new Dictionary<string, object?> { ["Product"] = "A" } }
            }));

    [TestMethod]
    public void A_problem_details_error_dictionary_lands_on_the_fields_it_names_whatever_their_case()
    {
        var state = Form();

        state.SetServerErrors(new Dictionary<string, string[]>
        {
            ["email"] = ["Already registered.", "Blocked domain."],
            ["$.lines[0].product"] = ["Discontinued."],
            [""] = ["The order could not be placed."],
            ["$"] = ["Try again later."]
        });

        Assert.HasCount(2, state.MessagesFor("Email"));
        Assert.AreEqual("Discontinued.", state.MessagesFor("Lines[0].Product").Single().Message);
        Assert.HasCount(2, state.MessagesFor(string.Empty));
        Assert.IsTrue(state.AllMessages.All(m => m.Source == BormValidationSource.Server));
        Assert.IsTrue(state.IsTouched("Email"));
    }

    [TestMethod]
    public void An_api_whose_names_differ_maps_them_and_an_unmapped_key_lands_on_the_form()
    {
        var state = Form();

        state.SetServerErrors(
            new Dictionary<string, string[]> { ["request.Email"] = ["Taken."], ["request.Unknown"] = ["Hmm."] },
            key => key == "request.Email" ? "Email" : null);

        Assert.AreEqual("Taken.", state.MessagesFor("Email").Single().Message);
        Assert.AreEqual("Hmm.", state.MessagesFor(string.Empty).Single().Message);
    }
}

/// <summary>What a message says about where it came from, separately from what it says.</summary>
[TestClass]
public class MessageCodeAndSourceTests
{
    [TestMethod]
    public async Task A_rules_message_carries_the_rules_key_as_its_code()
    {
        var form = BormBuilder.Create()
            .Text("name", f => f.Required().MaxLength(3))
            .Text("company", f => f.When("{name} = 'Ada'", w => w.MinLength(10)))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["name"] = "Ada" }));

        state.SetValue("name", "Grace");
        state.SetValue("company", "x");
        await state.ValidateAsync();
        Assert.AreEqual("maxLength", state.MessagesFor("name").Single().Code);
        Assert.AreEqual(BormValidationSource.Rule, state.MessagesFor("name").Single().Source);

        state.SetValue("name", "Ada");
        await state.ValidateAsync();
        // A rule wrapped by When(...) still reports the code of the rule inside it.
        Assert.AreEqual("minLength", state.MessagesFor("company").Single().Code);
    }

    [TestMethod]
    public void An_entry_the_model_refused_is_a_conversion_message()
    {
        var form = BormBuilder.Create().Integer("age").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.SetValue("age", "twelve");

        var message = state.MessagesFor("age").Single();
        Assert.AreEqual(BormMessageKeys.Conversion, message.Code);
        Assert.AreEqual(BormValidationSource.Conversion, message.Source);
    }

    [TestMethod]
    public async Task An_external_validators_messages_are_marked_as_external()
    {
        var form = BormBuilder.Create().Text("name").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor())
        {
            ExternalValidator = (_, _, _) => new ValueTask<IReadOnlyList<BormValidationMessage>>([new BormValidationMessage("name", "No.")])
        };

        await state.ValidateAsync();

        Assert.AreEqual(BormValidationSource.External, state.MessagesFor("name").Single().Source);
    }

    [TestMethod]
    public void Two_messages_saying_the_same_thing_are_equal_whatever_produced_them()
    {
        var a = new BormValidationMessage("x", "Required.") { Code = "required", Source = BormValidationSource.Rule };
        var b = new BormValidationMessage("x", "Required.") { Source = BormValidationSource.External };

        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        Assert.AreNotEqual(a, a with { Severity = BormValidationSeverity.Warning });
    }
}

[TestClass]
public class SubmitErrorRenderingTests : ComponentTestBase
{
    [TestMethod]
    public async Task A_view_with_a_submit_error_handler_reports_the_failure_instead_of_throwing_into_the_renderer()
    {
        Exception? reported = null;
        var form = BormBuilder.Create().Text("name").Build();
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.OnValidSubmit, _ => throw new InvalidOperationException("save failed"))
            .Add(x => x.OnSubmitError, ex => reported = ex));

        var ok = await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        Assert.IsFalse(ok);
        Assert.IsInstanceOfType<InvalidOperationException>(reported);
        Assert.AreEqual(BormSubmitOutcome.Failed, cut.Instance.Form.SubmitOutcome);
    }
}
