using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// Coming back to fix one answer must not cost the user the rest of the wizard. A step already walked
/// past has been validated, so it can be returned to in either direction.
/// </summary>
[TestClass]
public class VisitedStepNavigationTests : ComponentTestBase
{
    private static BormDefinition Schema() => BormBuilder.Create()
        .Text("a")
        .Text("b")
        .Text("c")
        .Step("one", s => s.Title("One").Fields("a"))
        .Step("two", s => s.Title("Two").Fields("b"))
        .Step("three", s => s.Title("Three").Fields("c"))
        .Build();

    [TestMethod]
    public async Task A_step_never_reached_is_not_reachable()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());

        Assert.IsTrue(state.IsStepReachable(0));
        Assert.IsFalse(state.IsStepReachable(1));

        await state.NextStepAsync();

        Assert.IsTrue(state.IsStepReachable(1));
        Assert.IsFalse(state.IsStepReachable(2));
    }

    [TestMethod]
    public async Task A_step_that_appears_behind_the_user_is_reached_by_Next_rather_than_skipped_past()
    {
        // Walked to the end as a personal account, then back to choose business: the company step that appears is
        // one the user has never seen. It is not done, and the step after it is no shortcut past its validation.
        var form = BormBuilder.Create()
            .Text("kind").Text("company").Text("confirm")
            .Step("kind", s => s.Fields("kind"))
            .Step("company", s => s.Fields("company").VisibleWhen("{kind} = 'business'"))
            .Step("confirm", s => s.Fields("confirm"))
            .Build();
        var data = new Dictionary<string, object?> { ["kind"] = "personal" };
        var state = new BormState(form, new BormDictionaryDataAccessor(data));

        await state.NextStepAsync();
        Assert.AreEqual(2, state.CurrentStepIndex);
        state.PreviousStep();
        state.SetValue("kind", "business");

        Assert.IsFalse(state.IsStepReachable(1));
        Assert.IsFalse(state.IsStepReachable(2));

        await state.NextStepAsync();
        Assert.AreEqual(1, state.CurrentStepIndex);
        Assert.IsTrue(state.IsStepReachable(2), "walked through the company step, the rest is reachable again");
    }

    [TestMethod]
    public async Task Going_back_does_not_forget_where_the_user_had_got_to()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());

        await state.NextStepAsync();
        await state.NextStepAsync();
        state.PreviousStep();
        state.PreviousStep();

        Assert.AreEqual(0, state.CurrentStepIndex);
        Assert.AreEqual(2, state.FurthestStepIndex);
        Assert.IsTrue(state.IsStepReachable(2));
    }

    [TestMethod]
    public async Task The_stepper_offers_a_link_forward_to_a_step_already_completed()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());
        await state.NextStepAsync();
        await state.NextStepAsync();
        state.PreviousStep();
        state.PreviousStep();

        var cut = Render<BormView>(p => p.Add(x => x.State, state));
        var links = cut.FindAll("button.bit-borm-stepper__link");

        // Two links: the steps either side of the one being shown, both already walked past.
        Assert.AreEqual(2, links.Count);

        links[1].Click();
        Assert.AreEqual(2, state.CurrentStepIndex);
    }

    [TestMethod]
    public void A_fresh_wizard_offers_no_links_at_all()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());
        var cut = Render<BormView>(p => p.Add(x => x.State, state));

        Assert.IsEmpty(cut.FindAll("button.bit-borm-stepper__link"));
    }

    [TestMethod]
    public async Task AllowStepNavigation_false_still_removes_every_link()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());
        await state.NextStepAsync();

        var cut = Render<BormView>(p => p
            .Add(x => x.State, state)
            .Add(x => x.AllowStepNavigation, false));

        Assert.IsEmpty(cut.FindAll("button.bit-borm-stepper__link"));
    }

    [TestMethod]
    public async Task Reset_puts_the_wizard_back_to_the_beginning_in_every_sense()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());
        await state.NextStepAsync();
        await state.NextStepAsync();

        state.Reset();

        Assert.AreEqual(0, state.CurrentStepIndex);
        Assert.AreEqual(0, state.FurthestStepIndex);
        Assert.IsFalse(state.IsStepReachable(1));
    }

    [TestMethod]
    public async Task A_step_that_a_condition_hides_is_never_reachable_however_far_the_user_got()
    {
        var form = BormBuilder.Create()
            .Checkbox("skip")
            .Text("a")
            .Text("b")
            .Step("one", s => s.Fields("skip", "a"))
            .Step("two", s => s.Fields("b").VisibleWhen("skip", BormConditionOperator.IsFalse))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        await state.NextStepAsync();
        Assert.IsTrue(state.IsStepReachable(1));

        state.SetValue("skip", true);

        Assert.IsFalse(state.IsStepReachable(1));
    }
}

/// <summary>
/// A yes/no question has two branches, and an unanswered one has to fall into exactly one of them.
/// Reading a missing value as neither hid a field under both.
/// </summary>
[TestClass]
public class UnansweredBooleanTests
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(false)]
    public void An_unanswered_checkbox_counts_as_false(object? value)
    {
        Assert.IsTrue(BormConditionEvaluator.Compare(value, BormConditionOperator.IsFalse, null));
        Assert.IsFalse(BormConditionEvaluator.Compare(value, BormConditionOperator.IsTrue, null));
    }

    [TestMethod]
    public void A_ticked_checkbox_is_only_true()
    {
        Assert.IsTrue(BormConditionEvaluator.Compare(true, BormConditionOperator.IsTrue, null));
        Assert.IsFalse(BormConditionEvaluator.Compare(true, BormConditionOperator.IsFalse, null));
    }

    [TestMethod]
    public void A_value_that_is_not_a_boolean_at_all_is_still_neither()
    {
        Assert.IsFalse(BormConditionEvaluator.Compare("maybe", BormConditionOperator.IsTrue, null));
        Assert.IsFalse(BormConditionEvaluator.Compare("maybe", BormConditionOperator.IsFalse, null));
    }

    [TestMethod]
    public void The_two_branches_of_one_question_are_mutually_exclusive_on_an_untouched_form()
    {
        var form = BormBuilder.Create()
            .Checkbox("isBusiness")
            .Text("company", f => f.VisibleWhen("isBusiness", BormConditionOperator.IsTrue))
            .Text("personal", f => f.VisibleWhen("isBusiness", BormConditionOperator.IsFalse))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        Assert.IsFalse(state.IsVisible(form.FindField("company")!));
        Assert.IsTrue(state.IsVisible(form.FindField("personal")!));
    }
}

[TestClass]
public class RepeaterRowLabellingTests : ComponentTestBase
{
    [TestMethod]
    public void Each_row_is_a_group_a_screen_reader_can_name()
    {
        // Every row repeats the same field labels, so without a name on the row a screen reader
        // announces "Product, edit" once per line with nothing to tell them apart.
        var model = new RegistrationModel { Items = { new LineItem(), new LineItem() } };
        var form = BormBuilder.Create()
            .Array("Items", item => item.Text("Product"), f => f.Attr("itemNoun", "line"))
            .Build();

        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Model, model));
        var rows = cut.FindAll("div.bit-borm-array__item[role=group]");

        Assert.AreSequenceEqual(["line 1", "line 2"], rows.Select(r => r.GetAttribute("aria-label")));
    }

    [TestMethod]
    public void The_row_name_goes_through_the_message_provider()
    {
        var model = new RegistrationModel { Items = { new LineItem() } };
        var form = BormBuilder.Create().Array("Items", item => item.Text("Product")).Build();

        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Model, model));

        Assert.AreEqual("item 1", cut.Find("div.bit-borm-array__item[role=group]").GetAttribute("aria-label"));
    }
}
