namespace Bit.Borm.Tests;

/// <summary>
/// Expressions doing the jobs a schema gives them — deciding what shows, computing a value, judging an
/// answer — through the builders, the state engine and JSON Schema, the way an application meets them.
/// </summary>
[TestClass]
public class ExpressionBuilderTests
{
    private static BormState Dictionary(BormDefinition form, Dictionary<string, object?>? data = null)
        => new(form, new BormDictionaryDataAccessor(data ?? new Dictionary<string, object?>()));

    [TestMethod]
    public void A_field_shows_while_its_expression_holds_and_is_cleared_once_it_stops()
    {
        var form = BormBuilder.Create()
            .Checkbox("isBusiness")
            .Integer("employees")
            .Text("company", f => f.VisibleWhen("{isBusiness} and {employees} > 10").ClearOnHide())
            .Build();
        var state = Dictionary(form);
        var company = form.FindField("company")!;

        Assert.IsFalse(state.IsVisible(company));
        state.SetValue("isBusiness", true);
        state.SetValue("employees", 50L);
        Assert.IsTrue(state.IsVisible(company));

        state.SetValue("company", "Contoso");
        // The dependencies come from the expression, so changing one of them is what re-judges it.
        state.SetValue("employees", 5L);
        Assert.IsFalse(state.IsVisible(company));
        Assert.IsNull(state.GetValue("company"));
    }

    [TestMethod]
    public void A_hidden_field_is_not_validated_whatever_its_rules_say()
    {
        var form = BormBuilder.Create()
            .Checkbox("ship")
            .Text("address", f => f.Required().VisibleWhen("{ship}"))
            .Build();
        var state = Dictionary(form);

        Assert.IsTrue(state.ValidateAsync().AsTask().Result);
        state.SetValue("ship", true);
        Assert.IsFalse(state.ValidateAsync().AsTask().Result);
    }

    [TestMethod]
    public void Required_disabled_and_read_only_follow_their_expressions()
    {
        var form = BormBuilder.Create()
            .Number("amount")
            .Text("approver", f => f.RequiredWhen("{amount} >= 1000"))
            .Text("note", f => f.DisabledWhen("{amount} = null").ReadOnlyWhen("{amount} > 5000"))
            .Build();
        var state = Dictionary(form);
        var approver = form.FindField("approver")!;
        var note = form.FindField("note")!;

        Assert.IsFalse(state.IsRequired(approver));
        Assert.IsTrue(state.IsDisabled(note));
        state.SetValue("amount", 2500.0);
        Assert.IsTrue(state.IsRequired(approver));
        Assert.IsFalse(state.IsDisabled(note));
        Assert.IsFalse(state.IsReadOnly(note));
        state.SetValue("amount", 9000.0);
        Assert.IsTrue(state.IsReadOnly(note));
    }

    [TestMethod]
    public void A_computed_expression_keeps_each_line_and_the_invoice_total_up_to_date()
    {
        var invoice = new ExpressionInvoice
        {
            Lines = [new() { Product = "A", Quantity = 2, UnitPrice = 1.5m }, new() { Product = "B", Quantity = 1, UnitPrice = 10m }]
        };
        var form = BormBuilder.For<ExpressionInvoice>()
            .Field(x => x.Customer)
            .Array(x => x.Lines, line => line
                .Field(l => l.Product)
                .Field(l => l.Quantity)
                .Field(l => l.UnitPrice)
                .Computed(l => l.LineTotal, "{Quantity} * {UnitPrice}"))
            .Field(x => x.Discount)
            .Computed(x => x.Total, "sum({Lines[].LineTotal}) - coalesce({Discount}, 0)")
            .Build();
        var state = new BormState(form, new BormModelDataAccessor(invoice));

        // Seeded when the form opens, without the form calling itself dirty for it.
        Assert.AreEqual(3m, invoice.Lines[0].LineTotal);
        Assert.AreEqual(13m, invoice.Total);
        Assert.IsFalse(state.IsFormDirty);

        state.SetValue("Lines[1].Quantity", 3);
        Assert.AreEqual(30m, invoice.Lines[1].LineTotal);
        Assert.AreEqual(33m, invoice.Total);

        state.SetValue("Discount", 3m);
        Assert.AreEqual(30m, invoice.Total);

        state.RemoveArrayItem("Lines", 0);
        Assert.AreEqual(27m, invoice.Total);

        var total = form.FindField("Total")!;
        Assert.IsTrue(total.ReadOnly);
        CollectionAssert.AreEquivalent(new[] { "Lines", "Discount" }, total.ComputedDependencies.ToArray());
    }

    [TestMethod]
    public void An_expression_rule_fails_only_when_it_is_false_and_revalidates_on_what_it_reads()
    {
        var form = BormBuilder.Create()
            .Date("start")
            .Date("end", f => f.Must("{end} >= {start}", "End must be on or after start."))
            .Build();
        var state = Dictionary(form);
        var end = form.FindField("end")!;

        // Nothing to compare yet: the rule has nothing to say.
        state.SetValue("end", new DateOnly(2024, 5, 1));
        Assert.IsTrue(state.ValidateAsync().AsTask().Result);

        state.SetValue("start", new DateOnly(2024, 5, 3));
        Assert.IsFalse(state.ValidateAsync().AsTask().Result);
        Assert.AreEqual("End must be on or after start.", state.MessagesFor("end").Single().Message);

        // Correcting the *other* field clears the message, because the rule registered what it reads.
        CollectionAssert.Contains(end.RevalidateOn.ToArray(), "start");
        state.SetValue("start", new DateOnly(2024, 4, 1));
        state.ValidateDependentsAsync("start").AsTask().Wait();
        Assert.IsEmpty(state.MessagesFor("end"));
    }

    [TestMethod]
    public void A_warning_expression_rule_advises_without_blocking_the_submit()
    {
        var form = BormBuilder.Create()
            .Integer("guests", f => f.Must("{guests} <= 8", "Large parties may wait longer.", BormValidationSeverity.Warning))
            .Build();
        var state = Dictionary(form, new Dictionary<string, object?> { ["guests"] = 12L });

        Assert.IsTrue(state.ValidateAsync().AsTask().Result);
        Assert.AreEqual(BormValidationSeverity.Warning, state.MessagesFor("guests").Single().Severity);
    }

    [TestMethod]
    public void A_form_level_expression_rule_puts_its_message_on_the_field_it_names()
    {
        var form = BormBuilder.Create()
            .Array("shares", s => s.Number("percent"))
            .MustAll("sum({shares[].percent}) = 100", "The shares must add up to 100%.", "shares")
            .Build();
        var state = Dictionary(form, new Dictionary<string, object?>
        {
            ["shares"] = new List<object?>
            {
                new Dictionary<string, object?> { ["percent"] = 60.0 },
                new Dictionary<string, object?> { ["percent"] = 30.0 }
            }
        });

        Assert.IsFalse(state.ValidateAsync().AsTask().Result);
        Assert.AreEqual("The shares must add up to 100%.", state.MessagesFor("shares").Single().Message);

        state.SetValue("shares[1].percent", 40.0);
        Assert.IsTrue(state.ValidateAsync().AsTask().Result);
    }

    [TestMethod]
    public void An_expression_rule_on_a_row_template_judges_each_row_on_its_own()
    {
        var form = BormBuilder.Create()
            .Array("rooms", r => r
                .Integer("adults")
                .Integer("children", f => f.Must("{children} <= {adults} * 2", "Too many children for the adults in this room.")))
            .Build();
        var state = Dictionary(form, new Dictionary<string, object?>
        {
            ["rooms"] = new List<object?>
            {
                new Dictionary<string, object?> { ["adults"] = 1L, ["children"] = 2L },
                new Dictionary<string, object?> { ["adults"] = 1L, ["children"] = 3L }
            }
        });

        Assert.IsFalse(state.ValidateAsync().AsTask().Result);
        Assert.IsEmpty(state.MessagesFor("rooms[0].children"));
        Assert.HasCount(1, state.MessagesFor("rooms[1].children"));
    }

    [TestMethod]
    public void A_wizard_step_shows_while_its_expression_holds()
    {
        var form = BormBuilder.Create()
            .Radio("kind", f => f.Options(("personal", "Personal"), ("business", "Business")))
            .Text("company")
            .Step("who", s => s.Fields("kind"))
            .Step("company", s => s.Fields("company").VisibleWhen("{kind} = 'business'"))
            .Build();
        var state = Dictionary(form);

        Assert.HasCount(1, state.VisibleSteps);
        state.SetValue("kind", "business");
        Assert.HasCount(2, state.VisibleSteps);
    }

    [TestMethod]
    public void A_typo_in_an_expression_fails_where_the_schema_is_written()
    {
        Assert.ThrowsExactly<BormExpressionException>(() =>
            BormBuilder.Create().Text("a", f => f.VisibleWhen("{b} = ")).Build());
        Assert.ThrowsExactly<BormExpressionException>(() =>
            BormBuilder.Create().Text("a", f => f.Computed("sum({b}")).Build());
    }
}

[TestClass]
public class ExpressionJsonTests
{
    private const string Document = """
    {
      "type": "object",
      "properties": {
        "quantity": { "type": "integer" },
        "unitPrice": { "type": "number" },
        "total": { "type": "number", "x-computed": "{quantity} * {unitPrice}" },
        "notes": {
          "type": "string",
          "x-visibleWhen": "{total} > 100",
          "x-requiredWhen": { "any": [ "{total} > 500", { "field": "quantity", "op": "GreaterThan", "value": 10 } ] }
        },
        "code": {
          "type": "string",
          "x-rules": [
            { "expression": "len({code}) = 6", "message": "Codes have six characters." },
            { "expression": "startsWith({code}, 'X')", "severity": "warning", "message": "Codes usually start with X." },
            "len({code}) > 3"
          ]
        }
      },
      "x-rules": [ { "expression": "{quantity} > 0 or {notes} != null", "message": "Order something or say why.", "field": "quantity" } ],
      "x-steps": [
        { "id": "order", "fields": ["quantity", "unitPrice", "total"] },
        { "id": "notes", "fields": ["notes", "code"], "visibleWhen": "{total} > 100" }
      ]
    }
    """;

    [TestMethod]
    public void Expressions_in_a_document_become_conditions_computed_values_and_rules()
    {
        var form = BormJsonSchemaImporter.Import(Document);
        var total = form.FindField("total")!;
        var notes = form.FindField("notes")!;
        var code = form.FindField("code")!;

        Assert.IsNotNull(total.Computed);
        Assert.IsTrue(total.ReadOnly);
        CollectionAssert.AreEquivalent(new[] { "quantity", "unitPrice" }, total.ComputedDependencies.ToArray());
        Assert.IsInstanceOfType<BormExpressionCondition>(notes.VisibleWhen);
        Assert.IsInstanceOfType<BormConditionGroup>(notes.RequiredWhen);
        Assert.HasCount(3, code.Validators.OfType<BormExpressionRule>().ToList());
        Assert.HasCount(1, form.Validators.OfType<BormExpressionRule>().ToList());
        Assert.IsInstanceOfType<BormExpressionCondition>(form.Steps[1].VisibleWhen);

        var data = new Dictionary<string, object?> { ["quantity"] = 3L, ["unitPrice"] = 50.0 };
        var state = new BormState(form, new BormDictionaryDataAccessor(data));
        Assert.AreEqual(150m, data["total"]);
        Assert.IsTrue(state.IsVisible(notes));
        Assert.IsFalse(state.IsRequired(notes));
        Assert.HasCount(2, state.VisibleSteps);

        state.SetValue("code", "abc");
        state.ValidateAsync().AsTask().Wait();
        var messages = state.MessagesFor("code").Select(m => m.Message).ToList();
        CollectionAssert.Contains(messages, "Codes have six characters.");
        CollectionAssert.Contains(messages, "Codes usually start with X.");
        // A rule given no message of its own speaks through the message provider.
        CollectionAssert.Contains(messages, BormDefaultMessageProvider.Instance.Get(BormMessageKeys.Rule));
    }

    [TestMethod]
    public void Every_expression_survives_a_round_trip_through_the_exporter()
    {
        var once = BormJsonSchemaImporter.Import(Document);
        var json = BormJsonSchemaExporter.Export(once);
        var twice = BormJsonSchemaImporter.Import(json);

        Assert.AreEqual(json, BormJsonSchemaExporter.Export(twice));
        StringAssert.Contains(json, "\"x-computed\": \"{quantity} * {unitPrice}\"");
        StringAssert.Contains(json, "\"x-visibleWhen\": \"{total} \\u003E 100\"");
        StringAssert.Contains(json, "\"severity\": \"warning\"");
        StringAssert.Contains(json, "\"field\": \"quantity\"");
        Assert.HasCount(3, twice.FindField("code")!.Validators.OfType<BormExpressionRule>().ToList());
    }

    [TestMethod]
    public void A_form_built_in_code_exports_its_expressions_too()
    {
        var form = BormBuilder.Create()
            .Number("net")
            .Number("gross", f => f.Computed("round({net} * 1.2, 2)"))
            .Text("reason", f => f.VisibleWhen("{gross} > 1000").Must("len({reason}) >= 10", "Say a little more."))
            .MustAll("{net} >= 0", "The net amount cannot be negative.", "net")
            .Build();

        var back = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(form));
        var data = new Dictionary<string, object?> { ["net"] = 1000.0 };
        var state = new BormState(back, new BormDictionaryDataAccessor(data));

        Assert.AreEqual(1200m, data["gross"]);
        Assert.IsTrue(state.IsVisible(back.FindField("reason")!));
        Assert.HasCount(1, back.Validators.OfType<BormExpressionRule>().ToList());
    }

    [TestMethod]
    public void A_broken_expression_fails_the_import_and_says_which_field_and_keyword()
    {
        foreach (var (document, keyword) in new[]
                 {
                     ("""{"type":"object","properties":{"a":{"type":"string","x-visibleWhen":"{b} >"}}}""", "x-visibleWhen of 'a'"),
                     ("""{"type":"object","properties":{"a":{"type":"number","x-computed":"nope({b})"}}}""", "x-computed of 'a'"),
                     ("""{"type":"object","properties":{"a":{"type":"string","x-rules":[{"expression":"(1"}]}}}""", "x-rules of 'a'"),
                     ("""{"type":"object","properties":{},"x-steps":[{"id":"s","fields":[],"visibleWhen":"{x"}]}""", "visibleWhen of 'step s'")
                 })
        {
            Assert.IsFalse(BormJsonSchemaImporter.TryImport(document, out _, out var error), document);
            StringAssert.Contains(error, keyword);
        }
    }

    [TestMethod]
    public void A_rule_entry_with_no_expression_is_skipped()
    {
        var form = BormJsonSchemaImporter.Import("""{"type":"object","properties":{"a":{"type":"string","x-rules":[{"message":"m"},"", 5]}}}""");
        Assert.IsEmpty(form.FindField("a")!.Validators.OfType<BormExpressionRule>().ToList());
    }
}

[TestClass]
public class ExpressionDiagnosticTests
{
    [TestMethod]
    public void An_expression_naming_a_field_that_does_not_exist_is_reported_even_past_a_row_reference()
    {
        var form = BormBuilder.Create()
            .Array("lines", l => l.Number("total"))
            .Number("sum", f => f.Computed("sum({lines[].totl})"))
            .Text("a", f => f.VisibleWhen("{nope} = 1").Must("{alsoNope} > 1", "m"))
            .MustAll("{missing} > 0", "m", "notAField")
            .Build();

        var problems = form.Validate().Select(d => d.Message).ToList();
        Assert.IsTrue(problems.Any(p => p.Contains("lines[0].totl")), string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("'nope'")));
        Assert.IsTrue(problems.Any(p => p.Contains("'alsoNope'")));
        Assert.IsTrue(problems.Any(p => p.Contains("'missing'")));
        Assert.IsTrue(problems.Any(p => p.Contains("'notAField'")));
    }

    [TestMethod]
    public void A_healthy_expression_schema_reports_nothing()
    {
        var form = BormBuilder.Create()
            .Array("lines", l => l.Number("qty").Number("price").Number("total", f => f.Computed("{qty} * {price}")))
            .Number("sum", f => f.Computed("sum({lines[].total})"))
            .Text("note", f => f.VisibleWhen("{sum} > 10"))
            .Build();

        Assert.IsEmpty(form.Validate());
    }

    [TestMethod]
    public void Computed_values_that_feed_each_other_in_a_circle_are_reported_once()
    {
        var form = BormBuilder.Create()
            .Number("a", f => f.Computed("{c} + 1"))
            .Number("b", f => f.Computed("{a} + 1"))
            .Number("c", f => f.Computed("{b} + 1"))
            .Number("d", f => f.Computed("{a} * 2"))
            .Build();

        var cycles = form.Validate().Where(d => d.Message.Contains("cycle")).ToList();
        Assert.HasCount(1, cycles);
        StringAssert.Contains(cycles[0].Message, "a → c → b → a");
        Assert.AreEqual(BormSchemaDiagnosticSeverity.Warning, cycles[0].Severity);
    }

    [TestMethod]
    public void A_cycle_through_a_row_template_is_found_and_a_total_over_rows_is_not_one()
    {
        var circular = BormBuilder.Create()
            .Array("rows", r => r
                .Number("x", f => f.Computed("{y}"))
                .Number("y", f => f.Computed(ctx => ctx.Sibling("x"), "x")))
            .Build();
        Assert.HasCount(1, circular.Validate().Where(d => d.Message.Contains("cycle")).ToList());

        var invoice = BormBuilder.Create()
            .Array("rows", r => r.Number("qty").Number("total", f => f.Computed("{qty} * 2")))
            .Number("sum", f => f.Computed("sum({rows[].total})"))
            .Build();
        Assert.IsEmpty(invoice.Validate().Where(d => d.Message.Contains("cycle")).ToList());
    }
}
