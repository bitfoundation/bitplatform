using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// A combobox holds a label and the model holds the value the label stands for. Half of a label
/// stands for nothing, so writing on every keystroke would store "Fran" as the country — and then
/// flag it as an answer that does not exist.
/// </summary>
[TestClass]
public class ComboboxUpdateTriggerTests : ComponentTestBase
{
    private static BormDefinition Schema() => BormBuilder.Create()
        .Combobox("country", f => f
            .Options(("fr", "France"), ("gb", "United Kingdom"))
            .AsCombobox()
            .UpdateOnInput())
        .Build();

    [TestMethod]
    public void A_combobox_does_not_wire_the_input_event_even_when_asked_to()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Schema()));

        // Blazor omits an event attribute whose callback has no delegate, which is the whole point:
        // no handler, no keystroke-by-keystroke write of a partial label.
        Assert.DoesNotContain("oninput", cut.Find("input#borm_country").OuterHtml, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void It_still_commits_when_the_choice_is_made()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());
        var cut = Render<BormView>(p => p.Add(x => x.State, state));

        cut.Find("input#borm_country").Change("France");

        Assert.AreEqual("fr", state.GetValue("country"));
    }

    [TestMethod]
    public void A_plain_text_field_still_writes_as_the_user_types()
    {
        var form = BormBuilder.Create().Text("a", f => f.UpdateOnInput()).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var cut = Render<BormView>(p => p.Add(x => x.State, state));

        cut.Find("input#borm_a").Input("typing");

        Assert.AreEqual("typing", state.GetValue("a"));
    }
}

/// <summary>
/// A setting that is accepted and then ignored is the kind of thing that costs an afternoon. The
/// schema check reports it instead.
/// </summary>
[TestClass]
public class UpdateTriggerDiagnosticTests
{
    private static IReadOnlyList<BormSchemaDiagnostic> Check(BormFieldType type)
        => BormBuilder.Create()
            .Field("x", type, f => f.Options(("a", "A")).As(type).UpdateOnInput())
            .Build()
            .Validate();

    [TestMethod]
    [DataRow(BormFieldType.Select)]
    [DataRow(BormFieldType.Radio)]
    [DataRow(BormFieldType.MultiSelect)]
    [DataRow(BormFieldType.Combobox)]
    public void A_control_that_only_ever_commits_reports_the_setting(BormFieldType type)
        => Assert.Contains(d => d.Message.Contains("UpdateOnInput has no effect", StringComparison.Ordinal), Check(type));

    [TestMethod]
    [DataRow(BormFieldType.Text)]
    [DataRow(BormFieldType.TextArea)]
    [DataRow(BormFieldType.Number)]
    [DataRow(BormFieldType.Range)]
    public void A_control_the_user_types_into_is_not_reported(BormFieldType type)
    {
        var form = BormBuilder.Create().Field("x", type, f => f.UpdateOnInput()).Build();

        Assert.DoesNotContain(d => d.Message.Contains("UpdateOnInput", StringComparison.Ordinal), form.Validate());
    }

    [TestMethod]
    public void The_default_trigger_is_never_reported()
    {
        var form = BormBuilder.Create().Select("x", f => f.Options(("a", "A"))).Build();

        Assert.DoesNotContain(d => d.Message.Contains("UpdateOnInput", StringComparison.Ordinal), form.Validate());
    }

    [TestMethod]
    public void It_is_a_warning_not_an_error_because_the_form_still_works()
    {
        var diagnostic = Check(BormFieldType.Select)
            .First(d => d.Message.Contains("UpdateOnInput", StringComparison.Ordinal));

        Assert.AreEqual(BormSchemaDiagnosticSeverity.Warning, diagnostic.Severity);
    }
}
