using Bunit;

namespace Bit.Borm.Tests;

[TestClass]
public class SearchFieldTests : ComponentTestBase
{
    [TestMethod]
    public void A_search_field_renders_as_a_search_box()
    {
        // Browsers give type=search a clear affordance, a search key on the on-screen keyboard and
        // history from previous searches; none of that comes with a plain text box.
        var form = BormBuilder.Create().Text("q", f => f.AsSearch().HideLabel().Label("Search")).Build();

        var cut = Render<BormView>(p => p.Add(x => x.Definition, form));
        var input = cut.Find("input#borm_q");

        Assert.AreEqual("search", input.GetAttribute("type"));
        Assert.AreEqual("search", input.GetAttribute("inputmode"));
        // The label was suppressed, so the control still has to be named for assistive technology.
        Assert.AreEqual("Search", input.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void An_explicit_input_mode_is_not_overwritten()
    {
        var form = BormBuilder.Create().Text("q", f => f.InputMode("text").AsSearch()).Build();

        Assert.AreEqual("text", form.FindField("q")!.InputMode);
    }

    [TestMethod]
    public void It_survives_a_JSON_round_trip()
    {
        var form = BormBuilder.Create().Text("q", f => f.AsSearch()).Build();

        var reimported = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(form));

        Assert.AreEqual(BormFieldType.Search, reimported.FindField("q")!.Type);
    }

    [TestMethod]
    public void Adding_it_did_not_move_any_existing_field_type()
    {
        // The member is appended rather than slotted in beside Text, so nothing persisted as a number
        // changes meaning.
        Assert.AreEqual(0, (int)BormFieldType.Text);
        Assert.AreEqual(1, (int)BormFieldType.TextArea);
        Assert.AreEqual(22, (int)BormFieldType.Custom);
    }
}

[TestClass]
public class CrossFieldRuleDiagnosticTests
{
    [TestMethod]
    public void A_confirm_rule_pointing_at_nothing_is_reported()
    {
        // The other value reads as null, this one is compared against it, and the field passes for
        // entirely the wrong reason — which is the hardest kind of bug to see in a form.
        var form = BormBuilder.Create()
            .Password("password")
            .Password("confirm", f => f.MatchesField("passwrod"))
            .Build();

        Assert.Contains(d => d.Message.Contains("MatchesField refers to 'passwrod'", StringComparison.Ordinal), form.Validate());
    }

    [TestMethod]
    public void A_correct_one_is_not()
    {
        var form = BormBuilder.Create()
            .Password("password")
            .Password("confirm", f => f.MatchesField("password"))
            .Build();

        Assert.IsEmpty(form.Validate());
    }

    [TestMethod]
    public void A_sibling_reference_inside_a_repeater_resolves()
    {
        var form = BormBuilder.Create()
            .Array("logins", row => row
                .Password("password")
                .Password("confirm", f => f.MatchesField("password")))
            .Build();

        Assert.IsEmpty(form.Validate());
    }
}
