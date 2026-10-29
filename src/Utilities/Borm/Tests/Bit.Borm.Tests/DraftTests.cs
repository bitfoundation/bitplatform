using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Borm.Tests;

public sealed class Application
{
    public string Name { get; set; } = "";
    public string Password { get; set; } = "";
    public string? CardNumber { get; set; }
    public AccountType Kind { get; set; }
    public List<Days> Availability { get; set; } = [];
    public DateOnly? Start { get; set; }
    public DateTimeOffset? Callback { get; set; }
    public List<LineItem> Lines { get; set; } = [];
    public decimal Total { get; set; }
}

/// <summary>
/// Drafts: the unfinished answers of a form, written out and put back — rows, steps and types
/// included, sensitive values and files left out, and a draft from an older version of the form
/// brought up to date rather than half applied.
/// </summary>
[TestClass]
public class DraftTests
{
    private static BormDefinition Form() => BormBuilder.For<Application>()
        .Version("2")
        .Field(x => x.Name, f => f.Required())
        .Field(x => x.Password, f => f.AsPassword())
        .Field(x => x.CardNumber, f => f.Sensitive())
        .Field(x => x.Kind)
        .Field(x => x.Availability)
        .Field(x => x.Start)
        .Field(x => x.Callback)
        .Array(x => x.Lines, l => l.Field(i => i.Product).Field(i => i.Quantity))
        .Computed(x => x.Total, "sum({Lines[].Quantity})")
        .Build();

    private static Application Filled() => new()
    {
        Name = "Ada",
        Password = "hunter2",
        CardNumber = "4111111111111111",
        Kind = AccountType.Business,
        Availability = [Days.Monday, Days.Wednesday],
        Start = new DateOnly(2026, 11, 2),
        Callback = new DateTimeOffset(2026, 11, 2, 9, 30, 0, TimeSpan.FromHours(2)),
        Lines = [new() { Product = "A", Quantity = 2 }, new() { Product = "B", Quantity = 3 }]
    };

    [TestMethod]
    public void A_draft_puts_every_answer_back_into_a_fresh_form_as_unsaved_changes()
    {
        var json = BormDraft.Serialize(new BormState(Form(), new BormModelDataAccessor(Filled())));

        var fresh = new Application();
        var state = new BormState(Form(), new BormModelDataAccessor(fresh));
        var result = BormDraft.Restore(state, json);

        Assert.IsTrue(result.Restored);
        Assert.AreEqual("2", result.Version);
        Assert.IsEmpty(result.Skipped);
        Assert.AreEqual("Ada", fresh.Name);
        Assert.AreEqual(AccountType.Business, fresh.Kind);
        CollectionAssert.AreEqual(new[] { Days.Monday, Days.Wednesday }, fresh.Availability);
        Assert.AreEqual(new DateOnly(2026, 11, 2), fresh.Start);
        Assert.AreEqual(new DateTimeOffset(2026, 11, 2, 9, 30, 0, TimeSpan.FromHours(2)), fresh.Callback);
        Assert.HasCount(2, fresh.Lines);
        Assert.AreEqual(3, fresh.Lines[1].Quantity);
        // Computed again rather than read back.
        Assert.AreEqual(5m, fresh.Total);
        // The user's work, not the record the form opened on: it is a change, and Reset takes it away.
        Assert.IsTrue(state.IsFormDirty);
        Assert.IsFalse(state.IsTouched("Name"));
        state.Reset();
        Assert.AreEqual("", fresh.Name);
        Assert.IsEmpty(fresh.Lines);
    }

    [TestMethod]
    public void Passwords_and_sensitive_fields_never_reach_the_draft_unless_asked_for()
    {
        var state = new BormState(Form(), new BormModelDataAccessor(Filled()));

        var json = BormDraft.Serialize(state);
        Assert.DoesNotContain("hunter2", json);
        Assert.DoesNotContain("4111", json);

        var withSensitive = BormDraft.Serialize(state, new BormDraftOptions { IncludeSensitive = true });
        StringAssert.Contains(withSensitive, "hunter2");
    }

    [TestMethod]
    public void A_draft_saved_against_an_older_version_is_migrated_before_it_is_restored()
    {
        var oldDraft = """
        {"format":"borm-draft","formatVersion":1,"version":"1","values":{"FullName":"Grace","Lines[0].Item":"X","Legacy":"gone"},"rows":{"Lines":1}}
        """;
        var fresh = new Application();
        var state = new BormState(Form(), new BormModelDataAccessor(fresh));
        string? from = null, to = null;

        var result = BormDraft.Restore(state, oldDraft, new BormDraftOptions
        {
            Migrate = m =>
            {
                (from, to) = (m.FromVersion, m.ToVersion);
                m.Rename("FullName", "Name").Rename("Lines[0].Item", "Lines[0].Product").Remove("Legacy");
            }
        });

        Assert.IsTrue(result.Migrated);
        Assert.AreEqual(("1", "2"), (from, to));
        Assert.AreEqual("Grace", fresh.Name);
        Assert.AreEqual("X", fresh.Lines.Single().Product);
        Assert.IsEmpty(result.Skipped);
    }

    [TestMethod]
    public void What_the_form_has_no_field_for_or_can_no_longer_hold_is_reported_not_left_as_an_error()
    {
        var draft = """
        {"format":"borm-draft","values":{"Name":"Ada","Nickname":"A","Start":"not a date","Lines[3].Product":"far"},"rows":{"Lines":1}}
        """;
        var state = new BormState(Form(), new BormModelDataAccessor(new Application()));

        var result = BormDraft.Restore(state, draft);

        CollectionAssert.AreEquivalent(new[] { "Nickname", "Start", "Lines[3].Product" }, result.Skipped.ToArray());
        Assert.IsEmpty(state.MessagesFor("Start"));
        Assert.AreEqual("Ada", state.GetValue("Name"));
    }

    [TestMethod]
    public void A_draft_claiming_more_rows_than_any_form_holds_is_capped_and_says_so()
    {
        var model = new Application();
        var state = new BormState(Form(), new BormModelDataAccessor(model));
        var clock = System.Diagnostics.Stopwatch.StartNew();

        var result = BormDraft.Restore(state, """{"format":"borm-draft","values":{},"rows":{"Lines":2000000000}}""");

        Assert.HasCount(BormDraft.MaxRows, model.Lines);
        CollectionAssert.Contains(result.Skipped.ToArray(), "Lines");
        Assert.IsLessThan(30_000, clock.ElapsedMilliseconds);
    }

    [TestMethod]
    public void Nested_repeaters_share_one_bound_and_rows_for_a_row_that_is_not_there_are_skipped()
    {
        var form = BormBuilder.Create()
            .Array("Orders", o => o.Text("Ref").Array("Lines", l => l.Text("Product")))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        const string json = """
            {"format":"borm-draft","values":{},
             "rows":{"Orders":3,"Orders[0].Lines":9000,"Orders[1].Lines":9000,"Orders[5].Lines":10}}
            """;

        var result = BormDraft.Restore(state, json);

        Assert.AreEqual(3, state.ArrayCount("Orders"));
        Assert.AreEqual(9_000, state.ArrayCount("Orders[0].Lines"));
        // What is left of the bound once the orders and the first order's lines are counted - not another 9,000.
        Assert.AreEqual(BormDraft.MaxRows - 3 - 9_000, state.ArrayCount("Orders[1].Lines"));
        CollectionAssert.Contains(result.Skipped.ToArray(), "Orders[1].Lines");
        // The draft has no sixth order, and its lines are not filled into a list that belongs to nothing.
        CollectionAssert.Contains(result.Skipped.ToArray(), "Orders[5].Lines");
    }

    [TestMethod]
    public void A_document_that_is_not_a_draft_changes_nothing()
    {
        var model = new Application { Name = "Ada" };
        var state = new BormState(Form(), new BormModelDataAccessor(model));

        foreach (var json in new[] { "not json", "{}", """{"format":"other"}""", """{"format":"borm-draft","formatVersion":99}""" })
        {
            var result = BormDraft.Restore(state, json);
            Assert.IsFalse(result.Restored, json);
            Assert.IsNotNull(result.Error);
        }
        Assert.AreEqual("Ada", model.Name);
        Assert.IsFalse(state.IsFormDirty);
    }

    [TestMethod]
    public void Restoring_does_not_set_off_change_handlers_or_empty_a_cascading_answer()
    {
        var form = BormBuilder.Create()
            .Select("plan", f => f.Options(("solo", "Solo"), ("team", "Team")).OnChange(ctx => ctx.SetSibling("seats", 5L)))
            .Integer("seats")
            .Text("country")
            .Select("city", f => f.OptionsFrom(_ => ValueTask.FromResult<IReadOnlyList<BormSelectOption>>([]), "country"))
            .Build();
        var source = new BormState(form, new BormDictionaryDataAccessor(new Dictionary<string, object?>
        {
            ["plan"] = "team", ["seats"] = 12L, ["country"] = "NL", ["city"] = "ams"
        }));

        var target = new Dictionary<string, object?>();
        BormDraft.Restore(new BormState(form, new BormDictionaryDataAccessor(target)), BormDraft.Serialize(source));

        Assert.AreEqual(12L, target["seats"]);
        Assert.AreEqual("ams", target["city"]);
    }

    [TestMethod]
    public void A_wizard_draft_comes_back_on_the_step_it_was_saved_on()
    {
        var form = BormBuilder.Create()
            .Text("a").Text("b").Text("c")
            .Step("one", s => s.Fields("a")).Step("two", s => s.Fields("b")).Step("three", s => s.Fields("c"))
            .Build();
        var source = new BormState(form, new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["a"] = "x", ["b"] = "y" }));
        source.GoToStep(2);

        var state = new BormState(form, new BormDictionaryDataAccessor());
        BormDraft.Restore(state, BormDraft.Serialize(source));

        Assert.AreEqual(2, state.CurrentStepIndex);
        Assert.IsTrue(state.IsStepReachable(1));
    }

    [TestMethod]
    public void A_snapshot_can_leave_out_what_the_form_hides_and_what_is_sensitive()
    {
        var form = BormBuilder.Create()
            .Checkbox("ship")
            .Text("address", f => f.VisibleWhen("{ship}"))
            .Password("pin")
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(new Dictionary<string, object?>
        {
            ["ship"] = false, ["address"] = "kept while hidden", ["pin"] = "1234"
        }));

        Assert.IsTrue(state.Snapshot().ContainsKey("address"));
        var payload = state.Snapshot(new BormSnapshotOptions { ExcludeHidden = true, ExcludeSensitive = true });
        CollectionAssert.AreEquivalent(new[] { "ship" }, payload.Keys.ToArray());
    }

    [TestMethod]
    public void A_sensitive_list_leaves_out_every_row_and_how_many_there_were()
    {
        var form = BormBuilder.Create()
            .Text("name")
            .ArrayOf("codes", BormFieldType.Text, f => f.Sensitive())
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(new Dictionary<string, object?>
        {
            ["name"] = "Ada", ["codes"] = new List<object?> { "111111", "222222" }
        }));

        var draft = BormDraft.Serialize(state);

        Assert.DoesNotContain("111111", draft);
        Assert.DoesNotContain("codes", draft);
        CollectionAssert.AreEquivalent(new[] { "name" }, state.Snapshot(new BormSnapshotOptions { ExcludeSensitive = true }).Keys.ToArray());
    }

    [TestMethod]
    public void A_password_in_a_repeater_row_stays_out_of_a_snapshot_that_leaves_out_sensitive_values()
    {
        var form = BormBuilder.Create()
            .Array("users", u => u.Text("login").Password("secret"))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(new Dictionary<string, object?>
        {
            ["users"] = new List<object?> { new Dictionary<string, object?> { ["login"] = "ada", ["secret"] = "hunter2" } }
        }));

        var payload = state.Snapshot(new BormSnapshotOptions { ExcludeSensitive = true });

        CollectionAssert.AreEquivalent(new[] { "users[0].login" }, payload.Keys.ToArray());
        Assert.DoesNotContain("hunter2", BormDraft.Serialize(state));
    }

    [TestMethod]
    public void The_version_and_the_sensitive_flag_travel_through_json_schema()
    {
        var back = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(Form()));

        Assert.AreEqual("2", back.Version);
        Assert.IsTrue(back.FindField("CardNumber")!.Sensitive);
        Assert.IsTrue(back.FindField("Password")!.IsSensitive);
    }
}

[TestClass]
public class AutosaveAndGuardTests : ComponentTestBase
{
    [TestMethod]
    public async Task Autosave_writes_a_draft_after_a_pause_and_only_when_something_changed()
    {
        var form = BormBuilder.Create().Text("name").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(), Services);
        var saved = new List<string>();
        var cut = Render<BormAutosave>(p => p
            .Add(x => x.State, state)
            .Add(x => x.DelayMilliseconds, 20)
            .Add(x => x.OnSave, (string json) => saved.Add(json)));

        await cut.InvokeAsync(() => state.SetValue("name", "A"));
        await cut.InvokeAsync(() => state.SetValue("name", "Ad"));
        await cut.InvokeAsync(() => state.SetValue("name", "Ada"));
        // Polled: the component renders nothing, so there is no render for bUnit's waits to follow.
        for (var waited = 0; saved.Count == 0 && waited < 5_000; waited += 20) await Task.Delay(20);
        Assert.HasCount(1, saved);
        StringAssert.Contains(saved[0], "\"Ada\"");

        // Put back to how it opened: nothing unsaved, so nothing to save.
        await cut.InvokeAsync(() => state.SetValue("name", null));
        await Task.Delay(100);
        Assert.HasCount(1, saved);
    }

    [TestMethod]
    public async Task A_save_still_waiting_when_the_page_goes_away_is_made_then()
    {
        var form = BormBuilder.Create().Text("name").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(), Services);
        var saved = new List<string>();
        var cut = Render<BormAutosave>(p => p
            .Add(x => x.State, state)
            .Add(x => x.DelayMilliseconds, 60_000)
            .Add(x => x.OnSave, (string json) => saved.Add(json)));

        await cut.InvokeAsync(() => state.SetValue("name", "Ada"));
        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());

        Assert.HasCount(1, saved);
    }

    [TestMethod]
    public void The_guard_asks_before_leaving_a_dirty_form_and_lets_a_clean_one_go()
    {
        var form = BormBuilder.Create().Text("name").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(), Services);
        var asked = new List<string>();
        var answer = false;
        Render<BormUnsavedChangesGuard>(p => p
            .Add(x => x.State, state)
            .Add(x => x.OnConfirm, (string q) => { asked.Add(q); return Task.FromResult(answer); }));
        var navigation = Services.GetRequiredService<NavigationManager>();

        navigation.NavigateTo("/clean");
        Assert.IsEmpty(asked);
        Assert.EndsWith("/clean", navigation.Uri);

        state.SetValue("name", "Ada");
        navigation.NavigateTo("/elsewhere");
        Assert.HasCount(1, asked);
        Assert.AreEqual(BormDefaultMessageProvider.Instance.Get(BormMessageKeys.UnsavedChanges), asked[0]);
        Assert.EndsWith("/clean", navigation.Uri);

        answer = true;
        navigation.NavigateTo("/elsewhere");
        Assert.EndsWith("/elsewhere", navigation.Uri);
    }
}
