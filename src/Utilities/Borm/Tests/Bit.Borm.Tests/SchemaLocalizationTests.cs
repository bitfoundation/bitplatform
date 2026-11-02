using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Borm.Tests;

/// <summary>
/// The schema's own text in the reader's language: labels, help, placeholders, options and titles,
/// from a registered localizer or from translations the document carries, following the culture.
/// </summary>
[TestClass]
public class SchemaLocalizationTests : ComponentTestBase
{
    private sealed class Dutch : IBormSchemaLocalizer
    {
        public List<string> Asked { get; } = [];

        public string? Localize(string key, string? text)
        {
            Asked.Add(key);
            return key switch
            {
                "name.label" => "Naam",
                "name.help" => "Zoals in uw paspoort.",
                "name.placeholder" => "Ada Lovelace",
                "plan.options.team" => "Team",
                "plan.options.solo" => "Alleen",
                "Lines.Product.label" => "Artikel",
                "steps.you.title" => "Over u",
                "form.title" => "Aanmelden",
                _ => null
            };
        }
    }

    private static BormDefinition Form() => BormBuilder.Create()
        .Title("Sign up")
        .Text("name", f => f.Label("Name").Help("As in your passport.").Placeholder("Your name"))
        .Select("plan", f => f.Label("Plan").Options(("solo", "Solo"), ("team", "Team")))
        .Array("Lines", l => l.Text("Product", f => f.Label("Product")))
        .Step("you", s => s.Title("About you").Fields("name", "plan", "Lines"))
        .Build();

    [TestMethod]
    public void A_registered_localizer_translates_every_text_the_form_renders()
    {
        Services.AddBormSchemaLocalizer<Dutch>();
        var data = new Dictionary<string, object?>
        {
            ["Lines"] = new List<object?> { new Dictionary<string, object?> { ["Product"] = "x" } }
        };
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Form()).Add(x => x.Data, data));

        Assert.AreEqual("Aanmelden", cut.Find(".bit-borm-form__title").TextContent);
        StringAssert.Contains(cut.Find("label[for=borm_name]").TextContent, "Naam");
        Assert.AreEqual("Zoals in uw paspoort.", cut.Find("#borm_name_help").TextContent);
        Assert.AreEqual("Ada Lovelace", cut.Find("#borm_name").GetAttribute("placeholder"));
        CollectionAssert.AreEqual(new[] { "Alleen", "Team" },
            cut.FindAll("#borm_plan option").Skip(1).Select(o => o.TextContent).ToArray());
        // Every row of a repeater shares one key, its path with the row taken out.
        StringAssert.Contains(cut.Find("label[for=borm_Lines_0_Product]").TextContent, "Artikel");
        StringAssert.Contains(cut.Find(".bit-borm-stepper").TextContent, "Over u");
    }

    [TestMethod]
    public void Translations_carried_by_the_document_follow_the_culture_and_fall_back_to_its_parent()
    {
        var form = BormJsonSchemaImporter.Import("""
        {
          "type": "object",
          "title": "Order",
          "properties": { "qty": { "type": "integer", "title": "Quantity", "x-i18n": "shared.quantity" } },
          "x-translations": {
            "de": { "form.title": "Bestellung", "shared.quantity.label": "Menge" },
            "fr-CA": { "form.title": "Commande (CA)" }
          }
        }
        """);
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var qty = form.Fields[0];
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-CH");
            Assert.AreEqual("Bestellung", state.FormTitle);
            Assert.AreEqual("Menge", state.LabelOf(qty, "qty"));

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-CA");
            Assert.AreEqual("Commande (CA)", state.FormTitle);
            Assert.AreEqual("Quantity", state.LabelOf(qty, "qty"));

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
            Assert.AreEqual("Order", state.FormTitle);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [TestMethod]
    public void Translated_option_labels_are_what_a_combobox_matches_and_a_review_reads_back()
    {
        var form = BormBuilder.Create()
            .Combobox("plan", f => f.Options(("solo", "Solo"), ("team", "Team")).AsCombobox())
            .Build();
        form.Translations["nl"] = new Dictionary<string, string> { ["plan.options.solo"] = "Alleen" };
        var data = new Dictionary<string, object?>();
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nl-NL");
        try
        {
            var state = new BormState(form, new BormDictionaryDataAccessor(data), Services);
            var cut = Render<BormView>(p => p.Add(x => x.State, state));

            cut.Find("#borm_plan").Change("alleen");

            Assert.AreEqual("solo", data["plan"]);
            var review = Render<BormReview>(p => p.Add(x => x.State, state));
            StringAssert.Contains(review.Find(".bit-borm-review__value").TextContent, "Alleen");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [TestMethod]
    public void A_form_with_nothing_to_translate_never_asks()
    {
        var localizer = new Dutch();
        var state = new BormState(Form(), new BormDictionaryDataAccessor());

        Assert.AreEqual("Name", state.LabelOf(state.Definition.FindField("name")!, "name"));
        Assert.IsEmpty(localizer.Asked);
        Assert.AreSame(state.Definition.FindField("plan")!.Options[0], state.OptionsFor(state.Definition.FindField("plan")!, "plan")[0]);
    }

    [TestMethod]
    public void A_view_rendered_again_after_the_culture_changes_reads_every_field_out_in_the_new_language()
    {
        var form = BormBuilder.Create()
            .Text("name", f => f.Label("Name"))
            .Translate("de", new Dictionary<string, string> { ["name.label"] = "Vorname" })
            .Build();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var cut = Render<BormView>(p => p.Add(x => x.Definition, form));
            StringAssert.Contains(cut.Find("label[for=borm_name]").TextContent, "Name");

            // Nothing about the field changed but the language: a field that skips rendering until its value
            // or messages move kept the old label.
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            cut.Render();

            StringAssert.Contains(cut.Find("label[for=borm_name]").TextContent, "Vorname");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [TestMethod]
    public async Task A_form_given_a_language_reads_its_translations_by_name_whatever_the_ui_culture()
    {
        var form = BormBuilder.Create()
            .Title("Order")
            .Text("name", f => f.Label("Name"))
            .Translate("de", new Dictionary<string, string> { ["form.title"] = "Bestellung", ["name.label"] = "Vorname" })
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            // All an application under InvariantGlobalization ever has: no culture would carry a translation.
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            var cut = Render<BormView>(p => p.Add(x => x.State, state));
            Assert.IsFalse(cut.Find("form").HasAttribute("lang"));
            Assert.DoesNotContain("Vorname", cut.Find("label[for=borm_name]").TextContent);

            // A dialect with no table of its own is read from its language's.
            await cut.InvokeAsync(() => state.Language = "de-CH");
            StringAssert.Contains(cut.Find("label[for=borm_name]").TextContent, "Vorname");
            Assert.AreEqual("Bestellung", cut.Find(".bit-borm-form__title").TextContent);
            Assert.AreEqual("de-CH", cut.Find("form").GetAttribute("lang"));

            await cut.InvokeAsync(() => state.Language = null);
            Assert.DoesNotContain("Vorname", cut.Find("label[for=borm_name]").TextContent);
            Assert.IsFalse(cut.Find("form").HasAttribute("lang"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [TestMethod]
    public void The_builder_adds_translations_per_culture_and_a_second_call_adds_to_the_first()
    {
        var form = BormBuilder.For<BridgePerson>()
            .Title("Person")
            .Field(x => x.Name)
            .Translate("de", new Dictionary<string, string> { ["form.title"] = "Person (de)" })
            .Translate("DE", new Dictionary<string, string> { ["Name.label"] = "Name (de)" })
            .Build();
        var state = new BormState(form, new BormModelDataAccessor(new BridgePerson()));

        // One table per culture, whatever the casing it was named in, holding both calls' texts.
        Assert.HasCount(1, form.Translations);
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-AT");
            Assert.AreEqual("Person (de)", state.FormTitle);
            Assert.AreEqual("Name (de)", state.LabelOf(form.Fields[0], "Name"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }

        Assert.ThrowsExactly<ArgumentException>(() => BormBuilder.Create().Translate(" ", new Dictionary<string, string>()));
    }

    [TestMethod]
    public void The_text_key_and_the_translations_travel_through_json_schema()
    {
        var form = Form();
        form.FindField("name")!.TextKey = "person.name";
        form.Translations["de"] = new Dictionary<string, string> { ["person.name.label"] = "Name" };

        var back = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(form));

        Assert.AreEqual("person.name", back.FindField("name")!.TextKey);
        Assert.AreEqual("Name", back.Translations["de"]["person.name.label"]);
    }

    [TestMethod]
    public void A_message_says_its_severity_in_words_without_changing_its_text()
    {
        var form = BormBuilder.Create()
            .Text("name", f => f.Required().Must("len({name}) > 10", "Longer is clearer.", BormValidationSeverity.Warning))
            .Build();
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, new Dictionary<string, object?> { ["name"] = "Ada" }));

        cut.Find("form").Submit();

        var warning = cut.Find(".bit-borm-message--warning");
        Assert.AreEqual("Warning:", warning.GetAttribute("data-severity"));
        Assert.AreEqual("Longer is clearer.", warning.TextContent);
    }
}
