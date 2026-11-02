using System.Globalization;
using Bunit;

namespace Bit.Borm.Tests;

/// <summary>"Check your answers": the form's answers read back in words, with a way back to each step.</summary>
[TestClass]
public class ReviewTests : ComponentTestBase
{
    private static BormDefinition Wizard() => BormBuilder.Create()
        .Text("name", f => f.Label("Your name"))
        .Password("pin", f => f.Label("PIN"))
        .Select("plan", f => f.Label("Plan").Options(("solo", "Solo"), ("team", "Team")))
        .Checkbox("newsletter", f => f.Label("Newsletter"))
        .Number("budget", f => f.Label("Budget").Prefix("€"))
        .Date("start", f => f.Label("Start"))
        .Text("notes", f => f.Label("Notes"))
        .Array("people", p => p.Text("who", f => f.Label("Who")), f => f.Label("People"))
        .Review("summary", "Check your answers")
        .Step("you", s => s.Title("You").Fields("name", "pin"))
        .Step("plan", s => s.Title("Your plan").Fields("plan", "newsletter", "budget", "start", "notes", "people"))
        .Step("review", s => s.Title("Review").Fields("summary"))
        .Build();

    private static Dictionary<string, object?> Answers() => new()
    {
        ["name"] = "Ada", ["pin"] = "1234", ["plan"] = "team", ["newsletter"] = true, ["budget"] = 1500.5,
        ["start"] = "2026-11-02",
        ["people"] = new List<object?> { new Dictionary<string, object?> { ["who"] = "Grace" } }
    };

    [TestMethod]
    public async Task The_last_step_reads_every_answer_back_in_words_and_never_shows_a_sensitive_one()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        try
        {
            var cut = Render<BormView>(p => p.Add(x => x.Definition, Wizard()).Add(x => x.Data, Answers()));
            await cut.InvokeAsync(() => cut.Instance.Form.GoToStep(2));

            var entries = cut.FindAll(".bit-borm-review__entry").ToDictionary(
                e => e.QuerySelector("dt")!.TextContent, e => e.QuerySelector("dd")!.TextContent.Trim());

            Assert.AreEqual("Ada", entries["Your name"]);
            Assert.AreEqual("Not shown", entries["PIN"]);
            Assert.AreEqual("Team", entries["Plan"]);
            Assert.AreEqual("Yes", entries["Newsletter"]);
            Assert.AreEqual("€ 1500.5", entries["Budget"]);
            Assert.AreEqual("Not answered", entries["Notes"]);
            Assert.AreEqual("Grace", entries["Who"]);
            // The review step does not review itself.
            CollectionAssert.AreEqual(new[] { "You", "Your plan" },
                cut.FindAll(".bit-borm-review__heading").Select(h => h.TextContent).ToArray());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [TestMethod]
    public async Task Each_step_has_a_button_back_to_it_named_for_the_step()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Wizard()).Add(x => x.Data, Answers()));
        await cut.InvokeAsync(() => cut.Instance.Form.GoToStep(2));

        var change = cut.FindAll(".bit-borm-review__header button")[1];
        Assert.AreEqual("Change Your plan", change.GetAttribute("aria-label"));
        change.Click();

        Assert.AreEqual(1, cut.Instance.Form.CurrentStepIndex);
    }

    [TestMethod]
    public async Task The_review_follows_answers_changed_after_it_first_rendered()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Wizard()).Add(x => x.Data, Answers()));
        var state = cut.Instance.Form;
        await cut.InvokeAsync(() => state.GoToStep(2));

        await cut.InvokeAsync(() => state.SetValue("plan", "solo"));

        Assert.IsTrue(cut.FindAll(".bit-borm-review__value").Any(v => v.TextContent.Trim() == "Solo"));
    }

    [TestMethod]
    public void A_review_can_be_placed_anywhere_and_lists_a_form_without_steps()
    {
        var form = BormBuilder.Create().Text("a", f => f.Label("A")).Checkbox("b", f => f.Label("B")).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["a"] = "x" }), Services);

        var cut = Render<BormReview>(p => p.Add(x => x.State, state));

        Assert.HasCount(2, cut.FindAll(".bit-borm-review__entry"));
        Assert.IsEmpty(cut.FindAll("button"));
        StringAssert.Contains(cut.Markup, "Not answered");
    }
}
