using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// A combobox that searches as the user types: only past a minimum length, never landing a stale answer,
/// showing the label of a value it opened with, and refusing text that names no result.
/// </summary>
[TestClass]
public class SearchLookupTests : ComponentTestBase
{
    private static readonly (string Id, string Name)[] Customers =
    [
        ("c1", "Contoso"), ("c2", "Contoso Pharma"), ("c3", "Fabrikam"), ("c4", "Northwind")
    ];

    private sealed class Lookup
    {
        public List<(string? Text, object? Current)> Calls { get; } = [];
        public Func<string?, Task>? Gate { get; set; }

        public async ValueTask<IReadOnlyList<BormSelectOption>> Search(BormOptionsContext ctx)
        {
            Calls.Add((ctx.SearchText, ctx.CurrentValue));
            if (Gate is not null) await Gate(ctx.SearchText);
            ctx.CancellationToken.ThrowIfCancellationRequested();
            return ctx.SearchText is { } text
                ? Customers.Where(c => c.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).Select(c => new BormSelectOption(c.Id, c.Name)).ToList()
                : Customers.Where(c => c.Id == ctx.CurrentValue as string).Select(c => new BormSelectOption(c.Id, c.Name)).ToList();
        }
    }

    private static (BormState State, BormFieldDefinition Field, Dictionary<string, object?> Data) Form(Lookup lookup, int minLength = 2, object? value = null)
    {
        var form = BormBuilder.Create()
            .Combobox("customer", f => f.Label("Customer").SearchFrom(lookup.Search, minLength, debounceMilliseconds: 0))
            .Build();
        var data = new Dictionary<string, object?> { ["customer"] = value };
        return (new BormState(form, new BormDictionaryDataAccessor(data)), form.Fields[0], data);
    }

    [TestMethod]
    public async Task Nothing_is_asked_until_enough_has_been_typed()
    {
        var lookup = new Lookup();
        var (state, field, _) = Form(lookup);

        await state.EnsureOptionsAsync(field, "customer");
        await state.SearchOptionsAsync(field, "customer", "c");
        Assert.IsEmpty(lookup.Calls);
        Assert.IsEmpty(state.OptionsFor(field, "customer"));

        await state.SearchOptionsAsync(field, "customer", "con");
        CollectionAssert.AreEqual(new[] { "Contoso", "Contoso Pharma" }, state.OptionsFor(field, "customer").Select(o => o.Label).ToArray());
        Assert.AreEqual("con", lookup.Calls.Single().Text);
    }

    [TestMethod]
    public async Task A_search_the_user_typed_past_never_lands_on_top_of_the_newer_one()
    {
        var slow = new TaskCompletionSource();
        var lookup = new Lookup { Gate = text => text == "co" ? slow.Task : Task.CompletedTask };
        var (state, field, _) = Form(lookup);

        var first = state.SearchOptionsAsync(field, "customer", "co").AsTask();
        await state.SearchOptionsAsync(field, "customer", "fab");
        slow.SetResult();
        await first;

        CollectionAssert.AreEqual(new[] { "Fabrikam" }, state.OptionsFor(field, "customer").Select(o => o.Label).ToArray());
        Assert.AreEqual("fab", state.SearchText("customer"));
    }

    [TestMethod]
    public async Task A_field_that_opens_with_a_value_asks_for_just_that_option()
    {
        var lookup = new Lookup();
        var (state, field, _) = Form(lookup, value: "c3");

        await state.EnsureOptionsAsync(field, "customer");

        Assert.AreEqual((null, (object?)"c3"), lookup.Calls.Single());
        Assert.AreEqual("Fabrikam", state.OptionsFor(field, "customer").Single().Label);
    }

    [TestMethod]
    public async Task A_minimum_of_zero_searches_the_top_results_before_anything_is_typed()
    {
        var lookup = new Lookup();
        var (state, field, _) = Form(lookup, minLength: 0);

        await state.EnsureOptionsAsync(field, "customer");

        Assert.AreEqual("", lookup.Calls.Single().Text);
        Assert.HasCount(4, state.OptionsFor(field, "customer"));
    }

    [TestMethod]
    public void Typing_searches_and_choosing_a_result_stores_its_value_and_keeps_its_label()
    {
        var lookup = new Lookup();
        var (state, _, data) = Form(lookup);
        var cut = Render<BormView>(p => p.Add(x => x.State, state));
        var input = cut.Find("#borm_customer");

        input.Input("north");
        cut.WaitForAssertion(() => Assert.HasCount(1, cut.FindAll("#borm_customer_list option")));
        input.Change("Northwind");
        Assert.AreEqual("c4", data["customer"]);

        // A later search that no longer returns it does not turn the box back into a bare id.
        input.Input("fab");
        cut.WaitForAssertion(() => Assert.AreEqual("Fabrikam", cut.Find("#borm_customer_list option").GetAttribute("value")));
        Assert.AreEqual("Northwind", cut.Find("#borm_customer").GetAttribute("value"));
    }

    [TestMethod]
    public void Text_committed_before_its_results_arrived_is_matched_by_searching_for_it()
    {
        var lookup = new Lookup();
        var (state, _, data) = Form(lookup);
        var cut = Render<BormView>(p => p.Add(x => x.State, state));

        cut.Find("#borm_customer").Change("fabrikam");

        cut.WaitForAssertion(() => Assert.AreEqual("c3", data["customer"]));
    }

    [TestMethod]
    public void Text_that_names_no_result_is_refused_and_the_model_keeps_its_value()
    {
        var lookup = new Lookup();
        var (state, _, data) = Form(lookup, value: "c1");
        var cut = Render<BormView>(p => p.Add(x => x.State, state));

        cut.Find("#borm_customer").Change("Nobody Ltd");

        cut.WaitForAssertion(() => Assert.HasCount(1, state.MessagesFor("customer")));
        Assert.AreEqual("c1", data["customer"]);
        Assert.AreEqual(BormValidationSource.Conversion, state.MessagesFor("customer").Single().Source);
        Assert.IsFalse(state.IsValid);
    }

    [TestMethod]
    public void A_search_field_that_takes_answers_of_its_own_stores_the_text()
    {
        var lookup = new Lookup();
        var form = BormBuilder.Create()
            .Combobox("company", f => f.SearchFrom(lookup.Search, minLength: 2, debounceMilliseconds: 0, allowCustom: true))
            .Build();
        var data = new Dictionary<string, object?>();
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data));

        cut.Find("#borm_company").Change("Brand New Co");

        cut.WaitForAssertion(() => Assert.AreEqual("Brand New Co", data["company"]));
    }

    [TestMethod]
    public async Task A_submit_pressed_while_committed_text_is_still_being_looked_up_sends_what_the_lookup_finds()
    {
        var gate = new TaskCompletionSource();
        var lookup = new Lookup { Gate = text => text == "fabrikam" ? gate.Task : Task.CompletedTask };
        var (state, _, data) = Form(lookup, value: "c1");
        var cut = Render<BormView>(p => p.Add(x => x.State, state));

        // Typed and tabbed away before its results arrived, then straight onto Submit: the click used to land
        // while the lookup was out, and the form judged and sent the customer from before.
        cut.Find("#borm_customer").Change("fabrikam");
        object? sent = null;
        Task<bool> submit = null!;
        await cut.InvokeAsync(() => { submit = state.SubmitAsync((_, _) => { sent = data["customer"]; return Task.CompletedTask; }); });
        Assert.IsFalse(submit.IsCompleted, "the submit did not wait for the entry being committed");
        Assert.IsTrue(state.IsSubmitting);

        gate.SetResult();

        Assert.IsTrue(await submit);
        Assert.AreEqual("c3", sent);
    }

    [TestMethod]
    public void Until_enough_is_typed_the_box_says_how_much_is_needed()
    {
        var lookup = new Lookup();
        var (state, _, _) = Form(lookup, minLength: 3);
        var cut = Render<BormView>(p => p.Add(x => x.State, state));

        StringAssert.Contains(cut.Find(".bit-borm-combobox [aria-live]").TextContent, "at least 3 characters");
    }
}
