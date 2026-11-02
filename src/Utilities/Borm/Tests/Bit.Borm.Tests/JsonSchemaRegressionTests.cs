using System.Text;
using System.Text.Json;

namespace Bit.Borm.Tests;

/// <summary>
/// Schemas the importer used to crash on, misread, or send back out changed. Each test names the
/// shape that went wrong and pins what it should do instead.
/// </summary>
[TestClass]
public class JsonSchemaRegressionTests
{
    private static BormDefinition Import(string json) => BormJsonSchemaImporter.Import(json);

    private static BormDefinition RoundTrip(BormDefinition form)
        => Import(BormJsonSchemaExporter.Export(form));

    private static JsonElement Exported(BormDefinition form, string field)
    {
        using var doc = JsonDocument.Parse(BormJsonSchemaExporter.Export(form));
        return doc.RootElement.GetProperty("properties").GetProperty(field).Clone();
    }

    private static async Task<IReadOnlyList<BormValidationMessage>> Validate(
        BormDefinition form, params (string Path, object? Value)[] values)
    {
        var data = new BormDictionaryDataAccessor();
        foreach (var (path, value) in values) data.SetValue(path, value);
        return await new BormValidator().ValidateAsync(form, data);
    }

    // ---------------------------------------------------------------- recursion and budgets

    [TestMethod]
    public void A_recursive_reference_is_followed_one_level_and_then_cut_off()
    {
        // A tree: following `child` again expanded the definition forever and overflowed the stack,
        // which no catch can stop — TryImport took the whole process down with it. The recursion is
        // followed once (a node and its child) and the property that would repeat it again is left out.
        const string json = """
        {
          "$defs": { "Node": { "type": "object", "properties": {
            "name": { "type": "string" },
            "child": { "$ref": "#/$defs/Node" } } } },
          "$ref": "#/$defs/Node"
        }
        """;

        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json, out var form, out _));
        Assert.IsNotNull(form.FindField("name"));
        var child = form.FindField("child")!;
        Assert.AreSequenceEqual(["name"], child.Children.Select(c => c.Name));
    }

    [TestMethod]
    public void An_optional_recursive_reference_terminates_too()
    {
        // pydantic's spelling of `child: Optional[Node]`: the reference sits inside a null union.
        const string json = """
        {
          "$defs": { "Node": { "type": "object", "properties": {
            "name": { "type": "string" },
            "child": { "anyOf": [ { "$ref": "#/$defs/Node" }, { "type": "null" } ] },
            "described": { "allOf": [ { "$ref": "#/$defs/Node" } ], "description": "pydantic v1" } } } },
          "type": "object",
          "properties": { "root": { "$ref": "#/$defs/Node" } }
        }
        """;

        var root = Import(json).FindField("root")!;

        Assert.AreSequenceEqual(["name", "child", "described"], root.Children.Select(c => c.Name));
        Assert.AreSequenceEqual(["name"], root.Children.Single(c => c.Name == "child").Children.Select(c => c.Name));
        Assert.AreSequenceEqual(["name"], root.Children.Single(c => c.Name == "described").Children.Select(c => c.Name));
    }

    [TestMethod]
    public void An_allOf_that_includes_itself_terminates()
    {
        const string json = """
        {
          "$defs": { "A": { "allOf": [ { "$ref": "#/$defs/A" } ], "properties": { "x": { "type": "string" } } } },
          "$ref": "#/$defs/A"
        }
        """;

        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json, out var form, out _));
        Assert.IsNotNull(form.FindField("x"));
    }

    [TestMethod]
    public void A_recursive_array_item_falls_back_to_a_text_row()
    {
        const string json = """
        { "type": "object", "properties": { "list": { "type": "array", "items": { "$ref": "#" } } } }
        """;

        var list = Import(json).FindField("list")!;

        // One level of the document per row, and the rows of those rows are text boxes.
        Assert.AreEqual(BormFieldType.Array, list.Type);
        var nested = list.ItemTemplate!.Children.Single(c => c.Name == "list");
        Assert.AreEqual(BormFieldType.Text, nested.ItemTemplate!.Type);
    }

    [TestMethod]
    public void A_definition_that_extends_another_and_also_holds_one_is_not_mistaken_for_recursion()
    {
        // An employee is a person and has a manager, who is a person. Nothing here repeats forever.
        const string json = """
        {
          "$defs": {
            "Person": { "type": "object", "properties": { "name": { "type": "string" } } },
            "Employee": { "allOf": [ { "$ref": "#/$defs/Person" } ],
                          "properties": { "manager": { "$ref": "#/$defs/Person" } } }
          },
          "$ref": "#/$defs/Employee"
        }
        """;

        var form = Import(json);

        Assert.IsNotNull(form.FindField("name"));
        Assert.AreSequenceEqual(["name"], form.FindField("manager")!.Children.Select(c => c.Name));
    }

    [TestMethod]
    public void A_schema_that_fans_out_to_millions_of_fields_is_refused_rather_than_built()
    {
        // Each level refers to the next twice: 1.5 KB of JSON describing 2^16 fields.
        var defs = new StringBuilder();
        for (var i = 0; i < 16; i++)
            defs.Append($"\"L{i}\":{{\"type\":\"object\",\"properties\":{{\"a\":{{\"$ref\":\"#/$defs/L{i + 1}\"}},\"b\":{{\"$ref\":\"#/$defs/L{i + 1}\"}}}}}},");
        defs.Append("\"L16\":{\"type\":\"string\"}");
        var json = "{\"$defs\":{" + defs + "},\"$ref\":\"#/$defs/L0\"}";

        Assert.IsFalse(BormJsonSchemaImporter.TryImport(json, out _, out var error));
        Assert.Contains("fields", error!, StringComparison.Ordinal);
        Assert.ThrowsExactly<FormatException>(() => Import(json));
    }

    [TestMethod]
    public void A_schema_nested_beyond_any_real_form_is_refused()
    {
        var defs = new StringBuilder();
        for (var i = 0; i < 40; i++)
            defs.Append($"\"L{i}\":{{\"type\":\"object\",\"properties\":{{\"next\":{{\"$ref\":\"#/$defs/L{i + 1}\"}}}}}},");
        defs.Append("\"L40\":{\"type\":\"string\"}");
        var json = "{\"$defs\":{" + defs + "},\"$ref\":\"#/$defs/L0\"}";

        Assert.IsFalse(BormJsonSchemaImporter.TryImport(json, out _, out var error));
        Assert.Contains("levels deep", error!, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- values of the wrong kind

    [TestMethod]
    public async Task The_draft04_boolean_exclusive_bound_makes_the_inclusive_bound_exclusive()
    {
        // OpenAPI 3.0 still speaks draft-04; reading a number out of `true` failed the whole import.
        const string json = """
        { "type": "object", "properties": { "n": { "type": "number", "minimum": 0, "exclusiveMinimum": true,
                                                   "maximum": 10, "exclusiveMaximum": false } } }
        """;

        var form = Import(json);
        var rule = Assert.ContainsSingle(form.FindField("n")!.Validators.OfType<BormRangeRule>());

        Assert.IsTrue(rule.MinExclusive);
        Assert.IsFalse(rule.MaxExclusive);
        Assert.IsNotEmpty(await Validate(form, ("n", 0d)));
        Assert.IsEmpty(await Validate(form, ("n", 0.5)));
        Assert.IsEmpty(await Validate(form, ("n", 10d)));
    }

    [TestMethod]
    public void A_keyword_holding_the_wrong_kind_of_value_is_ignored_rather_than_failing_the_import()
    {
        const string json = """
        {
          "type": "object", "x-columns": "2", "title": 5, "required": [1, "a"],
          "properties": {
            "a": { "type": [1, "string"], "x-order": "1", "minLength": "5", "pattern": 7,
                   "enum": ["x", "y"], "enumNames": [1, "Why"], "x-step": "big" }
          },
          "x-steps": [ "not a step", { "id": "s", "fields": ["a", 2] } ],
          "dependentRequired": { "a": [3] }
        }
        """;

        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json, out var form, out var error), error);
        var a = form.FindField("a")!;
        Assert.IsTrue(a.Required);
        Assert.IsNull(a.MinLength);
        Assert.AreSequenceEqual(["1", "Why"], a.Options.Select(o => o.Label));
        Assert.AreSequenceEqual(["a"], Assert.ContainsSingle(form.Steps).Fields);
        Assert.IsNull(form.Title);
        Assert.AreEqual(1, form.Columns);
    }

    [TestMethod]
    public void Boolean_subschemas_mean_anything_and_nothing()
    {
        const string json = """
        { "type": "object", "properties": { "anything": true, "nothing": false,
                                            "list": { "type": "array", "items": true } } }
        """;

        var form = Import(json);

        Assert.AreEqual(BormFieldType.Text, form.FindField("anything")!.Type);
        Assert.IsNull(form.FindField("nothing"));
        Assert.AreEqual(BormFieldType.Text, form.FindField("list")!.ItemTemplate!.Type);
    }

    [TestMethod]
    public void A_document_that_is_not_a_schema_is_reported_and_a_boolean_one_is_empty()
    {
        Assert.IsFalse(BormJsonSchemaImporter.TryImport("[]", out _, out var error));
        Assert.IsNotNull(error);
        Assert.ThrowsExactly<FormatException>(() => Import("42"));
        Assert.IsEmpty(Import("true").Fields);
    }

    // ---------------------------------------------------------------- allOf

    [TestMethod]
    public async Task Every_conditional_in_an_allOf_applies()
    {
        // The standard way to write more than one if/then. Merging kept only the last.
        const string json = """
        {
          "type": "object",
          "properties": { "kind": { "type": "string" }, "vat": { "type": "string" }, "ssn": { "type": "string" } },
          "allOf": [
            { "if": { "properties": { "kind": { "const": "business" } } }, "then": { "required": ["vat"] } },
            { "if": { "properties": { "kind": { "const": "person" } } }, "then": { "required": ["ssn"] } }
          ]
        }
        """;

        var form = Import(json);

        var business = await Validate(form, ("kind", "business"));
        Assert.Contains(m => m.FieldPath == "vat", business);
        Assert.DoesNotContain(m => m.FieldPath == "ssn", business);

        var person = await Validate(form, ("kind", "person"));
        Assert.Contains(m => m.FieldPath == "ssn", person);
        Assert.DoesNotContain(m => m.FieldPath == "vat", person);
    }

    [TestMethod]
    public void A_property_two_branches_describe_keeps_both_descriptions()
    {
        const string json = """
        {
          "type": "object",
          "allOf": [
            { "properties": { "name": { "type": "string", "maxLength": 5 } } },
            { "properties": { "name": { "title": "Full name" } } }
          ]
        }
        """;

        var name = Import(json).FindField("name")!;

        Assert.AreEqual(5, name.MaxLength);
        Assert.AreEqual("Full name", name.Label);
    }

    [TestMethod]
    public void The_stricter_bound_wins_a_merge()
    {
        const string json = """
        { "type": "object", "properties": { "n": { "type": "integer", "allOf": [ { "minimum": 5, "maximum": 50 } ],
                                                   "minimum": 0, "maximum": 100 } } }
        """;

        var n = Import(json).FindField("n")!;

        Assert.AreEqual(5, n.Min);
        Assert.AreEqual(50, n.Max);
    }

    [TestMethod]
    public async Task DependentRequired_from_two_branches_is_unioned()
    {
        const string json = """
        {
          "type": "object",
          "properties": { "card": { "type": "string" }, "expiry": { "type": "string" }, "cvc": { "type": "string" } },
          "allOf": [ { "dependentRequired": { "card": ["expiry"] } }, { "dependentRequired": { "card": ["cvc"] } } ]
        }
        """;

        var messages = await Validate(Import(json), ("card", "4111"));

        Assert.Contains(m => m.FieldPath == "expiry", messages);
        Assert.Contains(m => m.FieldPath == "cvc", messages);
    }

    // ---------------------------------------------------------------- multi-select and tags

    [TestMethod]
    public void A_multi_select_exports_its_choices_on_items_where_validators_look_for_them()
    {
        // `enum` beside `"type": "array"` describes the array itself: no list of answers satisfied it.
        var form = BormBuilder.Create()
            .MultiSelect("days", f => f.Options(("mon", "Monday"), ("tue", "Tuesday")))
            .Build();

        var days = Exported(form, "days");

        Assert.IsFalse(days.TryGetProperty("enum", out _));
        Assert.IsTrue(days.GetProperty("uniqueItems").GetBoolean());
        Assert.AreSequenceEqual(["mon", "tue"], days.GetProperty("items").GetProperty("enum").EnumerateArray().Select(e => e.GetString()));
        Assert.AreSequenceEqual(["Monday", "Tuesday"], days.GetProperty("items").GetProperty("enumNames").EnumerateArray().Select(e => e.GetString()));

        var back = RoundTrip(form).FindField("days")!;
        Assert.AreEqual(BormFieldType.MultiSelect, back.Type);
        Assert.AreSequenceEqual(["Monday", "Tuesday"], back.Options.Select(o => o.Label));
        // A set cannot hold an answer twice, so it carries no rule saying so.
        Assert.DoesNotContain(v => v.Key == "uniqueItems", back.Validators);
    }

    [TestMethod]
    public void A_named_multi_select_reads_its_choices_from_items()
    {
        const string json = """
        { "type": "object", "properties": { "days": { "type": "array", "x-widget": "multiselect", "uniqueItems": true,
            "items": { "type": "string", "enum": ["mon", "tue"] } } } }
        """;

        var days = Import(json).FindField("days")!;

        Assert.AreEqual(BormFieldType.MultiSelect, days.Type);
        Assert.AreSequenceEqual(["mon", "tue"], days.Options.Select(o => o.Value));
    }

    [TestMethod]
    public void The_older_top_level_enum_multi_select_still_imports()
    {
        const string json = """
        { "type": "object", "properties": { "days": { "type": "array", "x-widget": "multiselect",
            "enum": ["mon", "tue"], "enumNames": ["Monday", "Tuesday"] } } }
        """;

        var days = Import(json).FindField("days")!;

        Assert.AreEqual(BormFieldType.MultiSelect, days.Type);
        Assert.AreSequenceEqual(["Monday", "Tuesday"], days.Options.Select(o => o.Label));
    }

    [TestMethod]
    public void A_tag_lists_per_tag_constraints_travel_on_items()
    {
        var form = BormBuilder.Create()
            .Tags("labels", f => f.MinLength(2).MaxLength(10).Pattern("^[a-z]+$").Suggest("bug", "idea"))
            .Build();

        var labels = Exported(form, "labels");

        Assert.IsFalse(labels.TryGetProperty("maxLength", out _));
        Assert.IsFalse(labels.TryGetProperty("pattern", out _));
        var items = labels.GetProperty("items");
        Assert.AreEqual(2, items.GetProperty("minLength").GetInt32());
        Assert.AreEqual(10, items.GetProperty("maxLength").GetInt32());
        Assert.AreEqual("^[a-z]+$", items.GetProperty("pattern").GetString());
        Assert.AreSequenceEqual(["bug", "idea"], items.GetProperty("examples").EnumerateArray().Select(e => e.GetString()));

        var back = RoundTrip(form).FindField("labels")!;
        Assert.AreEqual(BormFieldType.Tags, back.Type);
        Assert.AreEqual(2, back.MinLength);
        Assert.AreEqual(10, back.MaxLength);
        Assert.AreEqual("^[a-z]+$", back.Pattern);
        Assert.AreSequenceEqual(["bug", "idea"], back.Suggestions);
    }

    // ---------------------------------------------------------------- numeric constraints

    [TestMethod]
    public async Task A_step_and_a_multipleOf_constraint_both_survive()
    {
        var form = BormBuilder.Create()
            .Number("price", f => f.Step(0.1).MultipleOf(0.5))
            .Build();

        var price = Exported(form, "price");
        Assert.AreEqual(0.5, price.GetProperty("multipleOf").GetDouble());
        Assert.AreEqual(0.1, price.GetProperty("x-step").GetDouble());

        var back = RoundTrip(form);
        var field = back.FindField("price")!;
        Assert.AreEqual(0.1, field.NumericStep);
        Assert.AreEqual(0.5, Assert.ContainsSingle(field.Validators.OfType<BormMultipleOfRule>()).Factor);
        Assert.IsNotEmpty(await Validate(back, ("price", 0.3)));
    }

    [TestMethod]
    public async Task An_inclusive_bound_stricter_than_the_exclusive_one_wins()
    {
        var form = Import("""
        { "type": "object", "properties": { "n": { "type": "integer", "minimum": 10, "exclusiveMinimum": 0 } } }
        """);

        Assert.IsNotEmpty(await Validate(form, ("n", 5L)));
        Assert.IsEmpty(await Validate(form, ("n", 10L)));
    }

    [TestMethod]
    public async Task A_fractional_exclusive_bound_on_an_integer_accepts_the_next_whole_number()
    {
        var form = Import("""
        { "type": "object", "properties": { "n": { "type": "integer", "exclusiveMinimum": 0.5 } } }
        """);

        Assert.IsEmpty(await Validate(form, ("n", 1L)));
        Assert.IsNotEmpty(await Validate(form, ("n", 0L)));
    }

    [TestMethod]
    public async Task Exclusive_bounds_stay_exclusive_and_keep_their_own_number()
    {
        var form = Import("""
        { "type": "object", "properties": { "n": { "type": "number", "exclusiveMaximum": 100 } } }
        """);

        var n = form.FindField("n")!;
        Assert.AreEqual(100, n.Max);
        Assert.IsEmpty(await Validate(form, ("n", 99.99999999)));
        Assert.IsNotEmpty(await Validate(form, ("n", 100d)));

        var exported = Exported(form, "n");
        Assert.AreEqual(100, exported.GetProperty("exclusiveMaximum").GetDouble());
        Assert.IsFalse(exported.TryGetProperty("maximum", out _));
        Assert.IsTrue(Assert.ContainsSingle(RoundTrip(form).FindField("n")!.Validators.OfType<BormRangeRule>()).MaxExclusive);
    }

    [TestMethod]
    public void Non_finite_numbers_do_not_fail_the_export()
    {
        // Range(0, +∞) means "no upper bound"; writing the infinity threw ArgumentException.
        var form = BormBuilder.Create()
            .Number("n", f => f.Range(0, double.PositiveInfinity).Default(double.NaN).Attr("weight", double.NaN))
            .Build();

        var n = Exported(form, "n");

        Assert.AreEqual(0, n.GetProperty("minimum").GetDouble());
        Assert.IsFalse(n.TryGetProperty("maximum", out _));
        Assert.IsFalse(n.TryGetProperty("default", out _));
        Assert.IsFalse(n.TryGetProperty("x-attributes", out _));
    }

    // ---------------------------------------------------------------- $ref

    [TestMethod]
    public void Keywords_beside_a_ref_are_kept_and_win_over_the_definitions()
    {
        const string json = """
        {
          "$defs": { "Address": { "type": "object", "title": "Address", "properties": { "city": { "type": "string" } } } },
          "type": "object",
          "properties": {
            "billing": { "$ref": "#/$defs/Address", "title": "Billing address", "description": "Where invoices go",
                         "x-order": 5, "readOnly": true }
          }
        }
        """;

        var billing = Import(json).FindField("billing")!;

        Assert.AreEqual("Billing address", billing.Label);
        Assert.AreEqual("Where invoices go", billing.HelpText);
        Assert.AreEqual(5, billing.Order);
        Assert.IsTrue(billing.ReadOnly);
        Assert.AreSequenceEqual(["city"], billing.Children.Select(c => c.Name));
    }

    [TestMethod]
    public void A_percent_encoded_reference_resolves()
    {
        const string json = """
        { "$defs": { "My Type": { "type": "integer" } },
          "type": "object", "properties": { "n": { "$ref": "#/$defs/My%20Type" } } }
        """;

        Assert.AreEqual(BormFieldType.Integer, Import(json).FindField("n")!.Type);
    }

    // ---------------------------------------------------------------- arrays

    [TestMethod]
    [DataRow("""{ "type": "array" }""")]
    [DataRow("""{ "type": "array", "items": [ { "type": "string" }, { "type": "integer" } ] }""")]
    [DataRow("""{ "type": "array", "prefixItems": [ { "type": "string" } ], "items": false }""")]
    public void An_array_without_a_single_item_schema_still_gets_a_row_to_render(string arraySchema)
    {
        // The repeater threw "has no item template" on the first Add.
        var form = Import($$"""{ "type": "object", "properties": { "list": {{arraySchema}} } }""");

        var list = form.FindField("list")!;
        Assert.AreEqual(BormFieldType.Array, list.Type);
        Assert.AreEqual(BormFieldType.Text, list.ItemTemplate!.Type);
        Assert.DoesNotContain(d => d.Message.Contains("ItemTemplate", StringComparison.Ordinal), form.Validate());
    }

    [TestMethod]
    public async Task Conditional_requiredness_inside_array_items_is_honoured()
    {
        const string json = """
        { "type": "object", "properties": { "rows": { "type": "array", "items": {
            "type": "object",
            "properties": { "a": { "type": "string" }, "b": { "type": "string" } },
            "dependentRequired": { "a": ["b"] } } } } }
        """;

        var form = Import(json);
        Assert.IsNotNull(form.FindField("rows")!.ItemTemplate!.Children.Single(c => c.Name == "b").RequiredWhen);

        var row = BormDictionaryDataAccessor.NewObject();
        row["a"] = "filled";
        var messages = await Validate(form, ("rows", new List<object?> { row }));

        Assert.Contains(m => m.FieldPath == "rows[0].b", messages);
    }

    // ---------------------------------------------------------------- choices

    [TestMethod]
    public async Task A_closed_combobox_stays_closed_across_a_round_trip()
    {
        var form = BormBuilder.Create()
            .Combobox("city", f => f.Options(("a", "A"), ("b", "B")).AsCombobox())
            .Build();

        var back = RoundTrip(form);

        Assert.Contains(v => v.Key == "oneOf", back.FindField("city")!.Validators);
        Assert.IsNotEmpty(await Validate(back, ("city", "somewhere else")));
    }

    [TestMethod]
    public async Task An_open_combobox_does_not_claim_to_be_a_closed_set()
    {
        var form = BormBuilder.Create()
            .Combobox("city", f => f.Options(("a", "A"), ("b", "B")).AsCombobox(allowCustom: true))
            .Build();

        var city = Exported(form, "city");
        Assert.IsFalse(city.TryGetProperty("enum", out _));
        Assert.IsTrue(city.GetProperty("x-allowCustom").GetBoolean());
        Assert.AreSequenceEqual(["a", "b"], city.GetProperty("x-enum").EnumerateArray().Select(e => e.GetString()));

        var back = RoundTrip(form);
        var field = back.FindField("city")!;
        Assert.AreEqual(BormFieldType.Combobox, field.Type);
        Assert.AreSequenceEqual(["A", "B"], field.Options.Select(o => o.Label));
        Assert.IsEmpty(await Validate(back, ("city", "somewhere else")));
    }

    [TestMethod]
    public void A_select_fed_by_a_provider_comes_back_as_a_select()
    {
        var form = BormBuilder.Create()
            .Select("city", f => f.OptionsFrom(_ => ValueTask.FromResult<IReadOnlyList<BormSelectOption>>([]), "country"))
            .Build();

        Assert.AreEqual(BormFieldType.Select, RoundTrip(form).FindField("city")!.Type);
    }

    [TestMethod]
    public void An_integer_enum_keeps_its_type()
    {
        const string json = """
        { "type": "object", "properties": { "n": { "type": "integer", "enum": [1, 2, 3], "default": 2 } } }
        """;

        var form = Import(json);
        var n = form.FindField("n")!;

        Assert.AreEqual(BormFieldType.Select, n.Type);
        Assert.AreEqual(typeof(long?), n.ValueType);
        Assert.AreSequenceEqual(["1", "2", "3"], n.Options.Select(o => o.Value));
        // Choosing an option stores the number, and the stored number shows as that option.
        Assert.AreEqual(2L, BormValueConverter.FromInputString("2", n.ValueType, n.Type));
        Assert.AreEqual("2", BormValueConverter.ToInputString(n.DefaultValue, n.Type));

        var exported = Exported(form, "n");
        Assert.AreEqual("integer", exported.GetProperty("type").GetString());
        foreach (var e in exported.GetProperty("enum").EnumerateArray())
        {
            Assert.AreEqual(JsonValueKind.Number, e.ValueKind);
        }
        Assert.AreEqual(JsonValueKind.Number, exported.GetProperty("default").ValueKind);
        Assert.AreEqual(typeof(long?), RoundTrip(form).FindField("n")!.ValueType);
    }

    [TestMethod]
    public void Boolean_and_decimal_enum_values_match_how_the_control_renders_them()
    {
        const string json = """
        { "type": "object", "properties": {
            "b": { "enum": [true, false, null] },
            "x": { "type": "number", "enum": [1.0, 2.5] } } }
        """;

        var form = Import(json);

        var b = form.FindField("b")!;
        Assert.AreSequenceEqual(["true", "false"], b.Options.Select(o => o.Value));
        Assert.AreEqual(typeof(bool?), b.ValueType);
        Assert.AreEqual("true", BormValueConverter.ToInputString(true, b.Type));

        var x = form.FindField("x")!;
        Assert.AreSequenceEqual(["1", "2.5"], x.Options.Select(o => o.Value));
        Assert.AreEqual(2.5, BormValueConverter.FromInputString("2.5", x.ValueType, x.Type));
    }

    [TestMethod]
    public void A_typed_multi_select_keeps_its_item_type()
    {
        const string json = """
        { "type": "object", "properties": { "levels": { "type": "array", "uniqueItems": true,
            "items": { "type": "integer", "enum": [1, 2] } } } }
        """;

        var form = Import(json);
        Assert.AreEqual(typeof(List<long>), form.FindField("levels")!.ValueType);

        var items = Exported(form, "levels").GetProperty("items");
        Assert.AreEqual("integer", items.GetProperty("type").GetString());
        foreach (var e in items.GetProperty("enum").EnumerateArray())
        {
            Assert.AreEqual(JsonValueKind.Number, e.ValueKind);
        }
    }

    [TestMethod]
    public void A_labelled_enum_with_a_null_branch_or_behind_refs_imports_as_a_select()
    {
        const string json = """
        {
          "$defs": { "R": { "const": "r", "title": "Red" } },
          "type": "object",
          "properties": {
            "nullable": { "oneOf": [ { "const": "r", "title": "Red" }, { "const": "g", "title": "Green" }, { "type": "null" } ] },
            "single": { "anyOf": [ { "const": "r", "title": "Red" }, { "type": "null" } ] },
            "referenced": { "oneOf": [ { "$ref": "#/$defs/R" }, { "const": "g", "title": "Green" } ] }
          }
        }
        """;

        var form = Import(json);

        Assert.AreSequenceEqual(["Red", "Green"], form.FindField("nullable")!.Options.Select(o => o.Label));
        var single = form.FindField("single")!;
        Assert.AreEqual(BormFieldType.Select, single.Type);
        Assert.IsFalse(single.ReadOnly);
        Assert.AreSequenceEqual(["Red", "Green"], form.FindField("referenced")!.Options.Select(o => o.Label));
    }

    // ---------------------------------------------------------------- if / then

    [TestMethod]
    public async Task An_if_with_a_constraint_that_has_no_condition_form_is_ignored_as_a_whole()
    {
        // Keeping only the `const` made the guardian required of every adult too.
        const string json = """
        {
          "type": "object",
          "properties": { "kind": { "type": "string" }, "age": { "type": "integer" }, "guardian": { "type": "string" } },
          "if": { "properties": { "kind": { "const": "person" }, "age": { "maximum": 17 } } },
          "then": { "required": ["guardian"] }
        }
        """;

        var form = Import(json);

        Assert.IsNull(form.FindField("guardian")!.RequiredWhen);
        Assert.IsEmpty(await Validate(form, ("kind", "person"), ("age", 40L)));
    }

    [TestMethod]
    public async Task Required_inside_an_if_reads_as_presence()
    {
        const string json = """
        {
          "type": "object",
          "properties": { "card": { "type": "string" }, "expiry": { "type": "string" } },
          "if": { "required": ["card"] },
          "then": { "required": ["expiry"] }
        }
        """;

        var form = Import(json);

        Assert.IsEmpty(await Validate(form));
        Assert.Contains(m => m.FieldPath == "expiry", await Validate(form, ("card", "4111")));
    }

    // ---------------------------------------------------------------- written values

    [TestMethod]
    public void Dates_and_times_are_written_as_iso_8601()
    {
        var form = BormBuilder.Create()
            .Date("start", f => f.Default(new DateTime(2026, 10, 18)))
            .Field("at", BormFieldType.DateTime, f => f.Default(new DateTime(2026, 10, 18, 9, 30, 0)))
            .Field("when", BormFieldType.Time, f => f.Default(new TimeOnly(14, 30)))
            .Date("day", f => f.Default(new DateOnly(2026, 1, 2)))
            .Text("note", f => f.VisibleWhen("start", BormConditionOperator.GreaterThan, new DateTime(2026, 1, 1)))
            .Build();

        Assert.AreEqual("2026-10-18", Exported(form, "start").GetProperty("default").GetString());
        Assert.AreEqual("2026-10-18T09:30:00", Exported(form, "at").GetProperty("default").GetString());
        Assert.AreEqual("14:30:00", Exported(form, "when").GetProperty("default").GetString());
        Assert.AreEqual("2026-01-02", Exported(form, "day").GetProperty("default").GetString());
        Assert.AreEqual("2026-01-01T00:00:00",
            Exported(form, "note").GetProperty("x-visibleWhen").GetProperty("value").GetString());

        // The date input shows what came back, which it could not do with "10/18/2026 00:00:00".
        var start = RoundTrip(form).FindField("start")!;
        Assert.AreEqual("2026-10-18", BormValueConverter.ToInputString(start.DefaultValue, start.Type));
    }

    [TestMethod]
    public void Every_integral_type_is_written_as_a_number()
    {
        var form = BormBuilder.Create()
            .Integer("small", f => f.Default((short)5).Attr("rows", (byte)3))
            .Integer("unsigned", f => f.Default(7u))
            .Build();

        Assert.AreEqual(5, Exported(form, "small").GetProperty("default").GetInt32());
        Assert.AreEqual(3, Exported(form, "small").GetProperty("x-attributes").GetProperty("rows").GetInt32());
        Assert.AreEqual(7, Exported(form, "unsigned").GetProperty("default").GetInt32());
        Assert.AreEqual(5L, RoundTrip(form).FindField("small")!.DefaultValue);
    }

    [TestMethod]
    public void Examples_offer_values_not_the_text_of_a_list()
    {
        // An example is a whole value, so a tag list's example is a list — and its members are what a
        // datalist should propose, not "System.Collections.Generic.List`1[System.Object]".
        const string json = """
        { "type": "object", "properties": {
            "t": { "type": "array", "x-widget": "tags", "examples": [["red", "blue"], ["red"]] },
            "s": { "type": "string", "examples": [true, 1.0, "x", {"no": 1}] } } }
        """;

        var form = Import(json);

        Assert.AreSequenceEqual(["red", "blue"], form.FindField("t")!.Suggestions);
        Assert.AreSequenceEqual(["true", "1", "x"], form.FindField("s")!.Suggestions);
    }

    // ---------------------------------------------------------------- conditions

    [TestMethod]
    [DataRow("neq")]
    [DataRow("!=")]
    [DataRow("1")]
    [DataRow("Equals, NotEquals")]
    public void An_operator_that_names_no_operator_makes_the_condition_unreadable(string op)
    {
        // It used to fall back to Equals, turning "hide when not X" into "hide when X".
        var json = $$"""
        { "type": "object", "properties": { "x": { "type": "string",
            "x-visibleWhen": { "field": "y", "op": "{{op}}", "value": "a" } } } }
        """;

        Assert.IsNull(Import(json).FindField("x")!.VisibleWhen);
    }

    [TestMethod]
    public void A_condition_without_an_op_still_means_equals_and_operator_names_ignore_case()
    {
        var form = Import("""
        { "type": "object", "properties": {
            "x": { "type": "string", "x-visibleWhen": { "field": "y", "value": "a" } },
            "z": { "type": "string", "x-visibleWhen": { "field": "y", "op": "notequals", "value": "a" } } } }
        """);

        Assert.AreEqual(BormConditionOperator.Equals, Assert.IsExactInstanceOfType<BormFieldCondition>(form.FindField("x")!.VisibleWhen).Operator);
        Assert.AreEqual(BormConditionOperator.NotEquals, Assert.IsExactInstanceOfType<BormFieldCondition>(form.FindField("z")!.VisibleWhen).Operator);
    }

    [TestMethod]
    public void A_group_with_an_unreadable_member_is_unreadable_as_a_whole()
    {
        // Keeping the readable half widens an `all` and narrows an `any` — the writer refuses to export
        // such a group, and the reader now refuses to import one.
        var form = Import("""
        { "type": "object", "properties": { "x": { "type": "string",
            "x-visibleWhen": { "all": [ { "field": "y", "op": "Equals", "value": "a" }, { "fld": "z" } ] } } } }
        """);

        Assert.IsNull(form.FindField("x")!.VisibleWhen);
    }

    // ---------------------------------------------------------------- formats and objects

    [TestMethod]
    public async Task A_uuid_format_and_a_pattern_are_both_enforced()
    {
        var form = Import("""
        { "type": "object", "properties": { "id": { "type": "string", "pattern": "^[a-f0-9-]+$", "format": "uuid" } } }
        """);

        Assert.IsNotEmpty(await Validate(form, ("id", "abc")));                                   // matches the pattern, not a UUID
        Assert.IsNotEmpty(await Validate(form, ("id", "0F8FAD5B-D9CB-469F-A165-70867728950E")));  // a UUID, not the pattern
        Assert.IsEmpty(await Validate(form, ("id", "0f8fad5b-d9cb-469f-a165-70867728950e")));
    }

    [TestMethod]
    public async Task A_uuid_format_round_trips_as_a_format()
    {
        var form = Import("""{ "type": "object", "properties": { "id": { "type": "string", "format": "uuid" } } }""");

        var id = Exported(form, "id");
        Assert.AreEqual("uuid", id.GetProperty("format").GetString());
        Assert.IsFalse(id.TryGetProperty("pattern", out _));
        Assert.IsNotEmpty(await Validate(RoundTrip(form), ("id", "not-a-uuid")));
    }

    [TestMethod]
    public void An_object_fields_read_only_flag_survives()
    {
        var form = BormBuilder.Create()
            .Object("address", a => a.Text("city"))
            .Build();
        form.FindField("address")!.ReadOnly = true;

        Assert.IsTrue(RoundTrip(form).FindField("address")!.ReadOnly);
    }

    // ---------------------------------------------------------------- review: depth, recursion, cost

    /// <summary>A chain of definitions, each of which is nothing but an allOf of the next.</summary>
    private static string AllOfChain(int length, bool insideUnion = false)
    {
        var defs = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            defs.Append(insideUnion
                ? $"\"D{i}\":{{\"anyOf\":[{{\"allOf\":[{{\"$ref\":\"#/$defs/D{i + 1}\"}}]}},{{\"type\":\"null\"}}]}},"
                : $"\"D{i}\":{{\"allOf\":[{{\"$ref\":\"#/$defs/D{i + 1}\"}}]}},");
        }
        defs.Append($"\"D{length}\":{{\"type\":\"string\"}}");
        return "{\"$defs\":{" + defs + "},\"type\":\"object\",\"properties\":{\"x\":{\"$ref\":\"#/$defs/D0\"}}}";
    }

    /// <summary>
    /// Runs an import on a thread with a deliberately small stack. Every chain the importer follows has
    /// to be capped well inside it, or a stack overflow — which no catch can stop — ends the process.
    /// </summary>
    private static (bool Ok, string? Error, BormDefinition Form) ImportOnSmallStack(string json)
    {
        (bool, string?, BormDefinition) result = default;
        var thread = new Thread(() =>
        {
            var ok = BormJsonSchemaImporter.TryImport(json, out var form, out var error);
            result = (ok, error, form);
        }, maxStackSize: 256 * 1024);
        thread.Start();
        thread.Join();
        return result;
    }

    [TestMethod]
    public void A_chain_of_allOf_thousands_deep_is_refused_rather_than_overflowing_the_stack()
    {
        var (ok, error, _) = ImportOnSmallStack(AllOfChain(3000));

        Assert.IsFalse(ok);
        Assert.Contains("allOf", error!, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_chain_of_allOf_inside_null_unions_terminates_on_a_small_stack()
    {
        var (ok, _, form) = ImportOnSmallStack(AllOfChain(3000, insideUnion: true));

        Assert.IsTrue(ok);
        Assert.IsNotNull(form.FindField("x"));
    }

    [TestMethod]
    public void A_short_chain_of_allOf_still_imports_on_a_small_stack()
    {
        var (ok, error, form) = ImportOnSmallStack(AllOfChain(40));

        Assert.IsTrue(ok, error);
        Assert.AreEqual(BormFieldType.Text, form.FindField("x")!.Type);
    }

    [TestMethod]
    [DataRow("""
        { "definitions": { "Node": { "type": "object", "properties": {
            "name": { "type": "string" },
            "next": { "description": "n", "allOf": [ { "$ref": "#/definitions/Node" } ] } } } },
          "type": "object",
          "properties": { "root": { "description": "r", "allOf": [ { "$ref": "#/definitions/Node" } ] } } }
        """)]
    [DataRow("""
        { "components": { "schemas": { "Node": { "type": "object", "properties": {
            "name": { "type": "string" },
            "next": { "allOf": [ { "$ref": "#/components/schemas/Node" } ], "nullable": true } } } } },
          "type": "object",
          "properties": { "root": { "allOf": [ { "$ref": "#/components/schemas/Node" } ], "nullable": true } } }
        """)]
    [DataRow("""
        { "$defs": { "Node": { "type": "object", "properties": {
            "name": { "type": "string" },
            "next": { "anyOf": [ { "allOf": [ { "$ref": "#/$defs/Node" } ] }, { "type": "null" } ] } } } },
          "type": "object",
          "properties": { "root": { "anyOf": [ { "allOf": [ { "$ref": "#/$defs/Node" } ] }, { "type": "null" } ] } } }
        """)]
    [DataRow("""
        { "$defs": {
            "Base": { "type": "object", "properties": { "next": { "$ref": "#/$defs/Node" } } },
            "Node": { "allOf": [ { "$ref": "#/$defs/Base" } ],
                      "properties": { "name": { "type": "string" }, "next": { "description": "n" } } } },
          "type": "object",
          "properties": { "root": { "allOf": [ { "$ref": "#/$defs/Node" } ] } } }
        """)]
    public void Recursion_spelled_through_allOf_is_cut_off_like_any_other(string json)
    {
        // pydantic v1 and Swashbuckle wrap every reference that carries a description in an allOf.
        // Those definitions never counted as the field's own, so the recursion ran on until the nesting
        // budget refused the whole document.
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json, out var form, out var error), error);

        var root = form.FindField("root")!;
        var next = root.Children.Single(c => c.Name == "next");
        Assert.Contains(c => c.Name == "name", next.Children);
        Assert.DoesNotContain(c => c.Name == "next", next.Children);
    }

    [TestMethod]
    public void A_definition_that_both_extends_and_contains_a_recursive_one_keeps_every_branch()
    {
        // An employee is a person, a person has a deputy who is a person, and an employee also has a
        // manager who is a person. Each of them is followed one level.
        const string json = """
        {
          "$defs": {
            "Person": { "type": "object", "properties": { "name": { "type": "string" }, "deputy": { "$ref": "#/$defs/Person" } } },
            "Employee": { "allOf": [ { "$ref": "#/$defs/Person" } ],
                          "properties": { "manager": { "$ref": "#/$defs/Person" } } }
          },
          "$ref": "#/$defs/Employee"
        }
        """;

        var form = Import(json);

        Assert.IsNotNull(form.FindField("name"));
        Assert.AreSequenceEqual(["name"], form.FindField("deputy")!.Children.Select(c => c.Name));
        Assert.AreSequenceEqual(["name"], form.FindField("manager")!.Children.Select(c => c.Name));
    }

    [TestMethod]
    public void A_wide_union_of_described_references_costs_almost_nothing_per_field()
    {
        // Each branch was resolved, merged with its title and materialised, several times per field:
        // 5,000 branches used by 300 fields took the better part of a minute.
        var json = new StringBuilder("""{"$defs":{"A":{"type":"object","properties":{"a":{"type":"string"}}},"U":{"anyOf":[""");
        for (var i = 0; i < 5000; i++) json.Append($"{{\"$ref\":\"#/$defs/A\",\"title\":\"t{i}\"}},");
        json.Length--;
        json.Append("]}},\"type\":\"object\",\"properties\":{");
        for (var f = 0; f < 300; f++) json.Append($"\"f{f}\":{{\"$ref\":\"#/$defs/U\"}},");
        json.Length--;
        json.Append("}}");

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);

        Assert.AreEqual(300, form.Fields.Count);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void Ordinary_use_of_allOf_on_thousands_of_fields_stays_inside_the_merge_budget()
    {
        var json = new StringBuilder("""{"$defs":{"A":{"minLength":1},"B":{"maxLength":50},"C":{"title":"t"},"D":{"type":"string"}},"type":"object","properties":{""");
        for (var i = 0; i < 3000; i++)
            json.Append($"\"f{i}\":{{\"allOf\":[{{\"$ref\":\"#/$defs/A\"}},{{\"$ref\":\"#/$defs/B\"}},{{\"$ref\":\"#/$defs/C\"}},{{\"$ref\":\"#/$defs/D\"}}]}},");
        json.Length--;
        json.Append("}}");

        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);
        Assert.AreEqual(3000, form.Fields.Count);
        Assert.AreEqual(50, form.Fields[0].MaxLength);
    }

    [TestMethod]
    public void An_allOf_that_doubles_at_every_level_runs_out_of_merges()
    {
        var json = new StringBuilder("""{"$defs":{"L0":{"type":"string"},""");
        for (var i = 1; i <= 25; i++)
            json.Append($"\"L{i}\":{{\"allOf\":[{{\"$ref\":\"#/$defs/L{i - 1}\"}},{{\"$ref\":\"#/$defs/L{i - 1}\"}}]}},");
        json.Length--;
        json.Append("""},"type":"object","properties":{"x":{"$ref":"#/$defs/L25"}}}""");

        Assert.IsFalse(BormJsonSchemaImporter.TryImport(json.ToString(), out _, out var error));
        Assert.Contains("merges", error!, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_shared_enum_used_by_a_thousand_fields_runs_out_of_choices()
    {
        var json = new StringBuilder("""{"$defs":{"E":{"type":"string","enum":[""");
        for (var i = 0; i < 500; i++) json.Append($"\"v{i}\",");
        json.Length--;
        json.Append("]}},\"type\":\"object\",\"properties\":{");
        for (var f = 0; f < 1000; f++) json.Append($"\"f{f}\":{{\"$ref\":\"#/$defs/E\"}},");
        json.Length--;
        json.Append("}}");

        Assert.IsFalse(BormJsonSchemaImporter.TryImport(json.ToString(), out _, out var error));
        Assert.Contains("choices", error!, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_shared_labelled_choice_used_by_a_thousand_fields_runs_out_of_choices()
    {
        var json = new StringBuilder("""{"$defs":{"E":{"oneOf":[""");
        for (var i = 0; i < 500; i++) json.Append($"{{\"const\":\"v{i}\",\"title\":\"Value {i}\"}},");
        json.Length--;
        json.Append("]}},\"type\":\"object\",\"properties\":{");
        for (var f = 0; f < 1000; f++) json.Append($"\"f{f}\":{{\"$ref\":\"#/$defs/E\"}},");
        json.Length--;
        json.Append("}}");

        Assert.IsFalse(BormJsonSchemaImporter.TryImport(json.ToString(), out _, out var error));
        Assert.Contains("choices", error!, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Ordinary_choice_lists_on_many_fields_stay_inside_the_choice_budget()
    {
        var json = new StringBuilder("""{"$defs":{"Country":{"oneOf":[""");
        for (var i = 0; i < 250; i++) json.Append($"{{\"const\":\"c{i}\",\"title\":\"Country {i}\"}},");
        json.Length--;
        json.Append("]}},\"type\":\"object\",\"properties\":{");
        for (var f = 0; f < 100; f++) json.Append($"\"f{f}\":{{\"$ref\":\"#/$defs/Country\"}},");
        json.Length--;
        json.Append("}}");

        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);
        Assert.AreEqual(100, form.Fields.Count);
        Assert.HasCount(250, form.Fields[0].Options);
    }

    [TestMethod]
    public void Examples_nested_in_one_list_count_every_member_against_the_choice_budget()
    {
        var json = new StringBuilder("""{"type":"object","properties":{"f":{"type":"array","items":{"type":"string"},"examples":[[""");
        for (var i = 0; i < 260_000; i++) json.Append($"\"e{i}\",");
        json.Length--;
        json.Append("]]}}}");

        Assert.IsFalse(BormJsonSchemaImporter.TryImport(json.ToString(), out _, out var error));
        Assert.Contains("choices", error!, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_labelled_enum_with_as_many_names_as_values_counts_each_choice_once()
    {
        var json = new StringBuilder("""{"type":"object","properties":{"f":{"type":"string","enum":[""");
        for (var i = 0; i < 200_000; i++) json.Append($"\"v{i}\",");
        json.Length--;
        json.Append("],\"enumNames\":[");
        for (var i = 0; i < 200_000; i++) json.Append($"\"Value {i}\",");
        json.Length--;
        json.Append("]}}}");

        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);
        Assert.HasCount(200_000, form.Fields[0].Options);
    }

    [TestMethod]
    public void Many_distinct_examples_are_read_in_time_linear_in_their_number()
    {
        var json = new StringBuilder("""{"type":"object","properties":{"f":{"type":"string","examples":[""");
        for (var i = 0; i < 100_000; i++) json.Append($"\"e{i}\",");
        json.Length--;
        json.Append("]}}}");

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);

        Assert.HasCount(100_000, form.Fields[0].Suggestions);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void References_into_a_wide_set_of_definitions_cost_almost_nothing_each()
    {
        var json = new StringBuilder("""{"$defs":{""");
        for (var i = 0; i < 40_000; i++) json.Append($"\"d{i}\":{{\"const\":{i}}},");
        json.Length--;
        json.Append("},\"type\":\"object\",\"properties\":{\"f\":{\"oneOf\":[");
        for (var i = 0; i < 40_000; i++) json.Append($"{{\"$ref\":\"#/$defs/d{i}\"}},");
        json.Length--;
        json.Append("]}}}");

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);

        Assert.HasCount(40_000, form.Fields[0].Options);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void A_property_name_that_reads_as_a_path_is_refused()
    {
        foreach (var name in new[] { "a[999999999]", "a.b", "a]" })
        {
            var json = "{\"type\":\"object\",\"properties\":{\"" + name + "\":{\"type\":\"string\"}}}";

            Assert.IsFalse(BormJsonSchemaImporter.TryImport(json, out _, out var error), name);
            Assert.Contains("property name", error!, StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void A_labelled_choice_counts_once_against_the_choice_budget_however_often_it_is_looked_at()
    {
        // Charged as each pass asked whether the union was an enum, 100,000 labelled choices came to 300,000.
        var json = new StringBuilder("""{"type":"object","properties":{"f":{"oneOf":[""");
        for (var i = 0; i < 100_000; i++) json.Append($"{{\"const\":\"v{i}\",\"title\":\"V{i}\"}},");
        json.Length--;
        json.Append("]}}}");

        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);
        Assert.HasCount(100_000, form.Fields[0].Options);
    }

    [TestMethod]
    public void A_shared_labelled_union_behind_many_tag_lists_is_looked_at_once()
    {
        // A tag list reads its items' rules and never makes options of them, so the choice budget never saw
        // the union every one of them walked.
        var json = new StringBuilder("""{"$defs":{"U":{"oneOf":[""");
        for (var i = 0; i < 50_000; i++) json.Append($"{{\"const\":\"v{i}\"}},");
        json.Length--;
        json.Append("]}},\"type\":\"object\",\"properties\":{");
        for (var f = 0; f < 2000; f++) json.Append($"\"f{f}\":{{\"type\":\"array\",\"x-widget\":\"tags\",\"items\":{{\"$ref\":\"#/$defs/U\"}}}},");
        json.Length--;
        json.Append("}}");

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);

        Assert.HasCount(2000, form.Fields);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void A_shared_labelled_union_with_long_titles_behind_many_tag_lists_reads_its_titles_once()
    {
        // Every tag list asked whether its items were an enum, and every answer read each title again.
        var json = new StringBuilder("""{"$defs":{"U":{"oneOf":[""");
        for (var i = 0; i < 9; i++) json.Append($"{{\"const\":\"v{i}\",\"title\":\"{new string('t', 1_000_000)}\"}},");
        json.Length--;
        json.Append("]}},\"type\":\"object\",\"properties\":{");
        for (var f = 0; f < 2000; f++) json.Append($"\"f{f}\":{{\"type\":\"array\",\"x-widget\":\"tags\",\"items\":{{\"$ref\":\"#/$defs/U\"}}}},");
        json.Length--;
        json.Append("}}");

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);

        Assert.HasCount(2000, form.Fields);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void A_shared_union_padded_with_null_branches_is_looked_at_once()
    {
        // Null branches are skipped past on the way to the ones that say something, and every field that
        // used the union skipped past all of them again.
        var json = new StringBuilder("""{"$defs":{"U":{"anyOf":[""");
        for (var i = 0; i < 50_000; i++) json.Append("{\"type\":\"null\"},");
        json.Append("{\"type\":\"string\"},{\"type\":\"number\"}]}},\"type\":\"object\",\"properties\":{");
        for (var f = 0; f < 5000; f++) json.Append($"\"f{f}\":{{\"$ref\":\"#/$defs/U\"}},");
        json.Length--;
        json.Append("}}");

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);

        Assert.HasCount(5000, form.Fields);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void A_shared_union_of_wide_branches_is_looked_at_once()
    {
        // Each question about the union — an enum? a discriminated union? an optional value? — looked its
        // branches' keywords up one by one, past every keyword they had, for every field using it.
        var json = new StringBuilder("""{"$defs":{"U":{"anyOf":[""");
        foreach (var type in new[] { "string", "number" })
        {
            json.Append($"{{\"type\":\"{type}\"");
            for (var i = 0; i < 50_000; i++) json.Append($",\"x{i}\":1");
            json.Append("},");
        }
        json.Length--;
        json.Append("]}},\"type\":\"object\",\"properties\":{");
        for (var f = 0; f < 5000; f++) json.Append($"\"f{f}\":{{\"$ref\":\"#/$defs/U\"}},");
        json.Length--;
        json.Append("}}");

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);

        Assert.HasCount(5000, form.Fields);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void A_union_whose_first_branch_has_many_properties_is_settled_in_one_walk_of_it()
    {
        // Each property was asked for by name to see whether it was pinned, a walk of all of them per property.
        var json = new StringBuilder("""{"type":"object","properties":{"f":{"oneOf":[{"properties":{""");
        for (var i = 0; i < 100_000; i++) json.Append($"\"p{i}\":{{}},");
        json.Length--;
        json.Append("}},{\"properties\":{}}]}}}");

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);

        Assert.ContainsSingle(form.Fields);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void A_definition_copied_for_every_field_that_refers_to_it_runs_out_of_copies()
    {
        // A string field never builds the properties it carries, but a label beside the reference copies
        // the definition, properties and all, once for every field.
        var json = new StringBuilder("""{"$defs":{"E":{"type":"string","properties":{""");
        for (var i = 0; i < 20_000; i++) json.Append($"\"p{i}\":{{}},");
        json.Length--;
        json.Append("}}},\"type\":\"object\",\"properties\":{");
        for (var f = 0; f < 2000; f++) json.Append($"\"f{f}\":{{\"$ref\":\"#/$defs/E\",\"title\":\"t\"}},");
        json.Length--;
        json.Append("}}");

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.IsFalse(BormJsonSchemaImporter.TryImport(json.ToString(), out _, out var error));

        Assert.Contains("copied", error!, StringComparison.Ordinal);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void A_conditional_behind_a_reference_shared_by_many_objects_runs_out_of_schema_text()
    {
        // Each object read the referenced `then` in full, while only the reference was charged.
        var json = new StringBuilder("""{"$defs":{"T":{"required":[""");
        for (var i = 0; i < 100_000; i++) json.Append($"\"n{i}\",");
        json.Length--;
        json.Append("""]},"O":{"type":"object","properties":{"a":{}},"if":{"properties":{"a":{"const":1}}},"then":{"$ref":"#/$defs/T"}}},"type":"object","properties":{""");
        for (var f = 0; f < 2000; f++) json.Append($"\"f{f}\":{{\"$ref\":\"#/$defs/O\"}},");
        json.Length--;
        json.Append("}}");

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.IsFalse(BormJsonSchemaImporter.TryImport(json.ToString(), out _, out var error));

        Assert.Contains("bytes of it", error!, StringComparison.Ordinal);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void A_long_dependent_required_list_is_applied_by_name_and_once_per_trigger()
    {
        // Each name was found by walking every field, and each trigger folded in as it came nested the
        // condition one level deeper.
        var json = new StringBuilder("""{"type":"object","properties":{""");
        for (var f = 0; f < 5000; f++) json.Append($"\"f{f}\":{{\"type\":\"string\"}},");
        json.Length--;
        json.Append("},\"dependentRequired\":{");
        for (var t = 0; t < 1000; t++)
        {
            json.Append($"\"t{t}\":[");
            for (var i = 0; i < 200; i++) json.Append(i % 2 == 0 ? "\"f1\"," : "\"F1\",");
            json.Length--;
            json.Append("],");
        }
        json.Length--;
        json.Append("}}");

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");

        var group = Assert.IsInstanceOfType<BormConditionGroup>(form.Fields.Single(f => f.Name == "f1").RequiredWhen);
        Assert.AreEqual(BormConditionLogic.Or, group.Logic);
        Assert.HasCount(1000, group.Conditions);
        Assert.IsTrue(group.Conditions.All(c => c is BormFieldCondition));
    }

    [TestMethod]
    public void An_enum_counts_against_the_choice_budget_and_not_the_schema_text_budget_as_well()
    {
        // Seven million characters of choices and four of description each fit their own budget; counted
        // against both, the eleven million together were refused as schema text.
        var json = new StringBuilder("""{"type":"object","properties":{"f":{"type":"string","description":""");
        json.Append('"').Append('d', 4_000_000).Append("\",\"enum\":[");
        for (var i = 0; i < 200_000; i++) json.Append($"\"{i:D32}\",");
        json.Length--;
        json.Append("]}}}");

        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);
        Assert.HasCount(200_000, form.Fields[0].Options);
    }

    [TestMethod]
    public void A_shared_bag_of_attributes_used_by_many_fields_runs_out_of_schema_text()
    {
        var bag = new StringBuilder();
        for (var i = 0; i < 20_000; i++) bag.Append($"\"a{i}\":1,");
        bag.Length--;
        var json = new StringBuilder("{\"$defs\":{\"E\":{\"type\":\"string\",\"x-attributes\":{" + bag + "}}},\"type\":\"object\",\"properties\":{");
        for (var f = 0; f < 2000; f++) json.Append($"\"f{f}\":{{\"$ref\":\"#/$defs/E\"}},");
        json.Length--;
        json.Append("}}");

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.IsFalse(BormJsonSchemaImporter.TryImport(json.ToString(), out _, out var error));

        Assert.Contains("bytes of it", error!, StringComparison.Ordinal);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void A_shared_description_used_by_many_fields_runs_out_of_schema_text()
    {
        var json = new StringBuilder("{\"$defs\":{\"E\":{\"type\":\"string\",\"description\":\"" + new string('d', 200_000) + "\"}},\"type\":\"object\",\"properties\":{");
        for (var f = 0; f < 2000; f++) json.Append($"\"f{f}\":{{\"$ref\":\"#/$defs/E\"}},");
        json.Length--;
        json.Append("}}");

        Assert.IsFalse(BormJsonSchemaImporter.TryImport(json.ToString(), out _, out var error));
        Assert.Contains("bytes of it", error!, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_shared_definition_wide_with_unknown_keywords_runs_out_of_schema_text()
    {
        // Every keyword a field looks up walks past every keyword the schema has, known or not.
        var json = new StringBuilder("""{"$defs":{"E":{"type":"string",""");
        for (var i = 0; i < 100_000; i++) json.Append($"\"k{i}\":0,");
        json.Length--;
        json.Append("}},\"type\":\"object\",\"properties\":{");
        for (var f = 0; f < 5000; f++) json.Append($"\"f{f}\":{{\"$ref\":\"#/$defs/E\"}},");
        json.Length--;
        json.Append("}}");

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.IsFalse(BormJsonSchemaImporter.TryImport(json.ToString(), out _, out var error));

        Assert.Contains("bytes of it", error!, StringComparison.Ordinal);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void A_definition_shared_by_ordinary_fields_stays_inside_the_schema_text_budget()
    {
        var json = new StringBuilder("""{"$defs":{"Name":{"type":"string","title":"Name","description":"Given and family name, as on the passport.","maxLength":200,"x-attributes":{"autocapitalize":"words"}}},"type":"object","properties":{""");
        for (var f = 0; f < 5000; f++) json.Append($"\"f{f}\":{{\"$ref\":\"#/$defs/Name\"}},");
        json.Length--;
        json.Append("}}");

        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json.ToString(), out var form, out var error), error);
        Assert.HasCount(5000, form.Fields);
    }

    // ---------------------------------------------------------------- review: typed choices

    [TestMethod]
    public void A_typed_choice_whose_options_are_not_canonical_numbers_stays_text()
    {
        // "1.50" written as 1.5 would be a different option; written as "1.50" under "type": "number"
        // the schema contradicts itself, and the re-import stored 1.5 — which matched no option.
        var rate = new BormFieldDefinition("rate", BormFieldType.Select) { ValueType = typeof(decimal?) };
        rate.Options.Add(new BormSelectOption("0.50", "50c"));
        rate.Options.Add(new BormSelectOption("1.50", "1.50"));
        var form = new BormDefinition();
        form.Fields.Add(rate);

        var exported = Exported(form, "rate");
        Assert.AreEqual("string", exported.GetProperty("type").GetString());
        Assert.AreSequenceEqual(["0.50", "1.50"], exported.GetProperty("enum").EnumerateArray().Select(e => e.GetString()));

        var back = RoundTrip(form).FindField("rate")!;
        var picked = BormValueConverter.FromInputString("1.50", back.ValueType, back.Type);
        Assert.Contains(o => o.Value == BormValueConverter.ToInputString(picked, back.Type), back.Options);
    }

    [TestMethod]
    public void A_declared_type_the_enum_contradicts_is_not_believed()
    {
        var field = Import("""
        { "type": "object", "properties": { "n": { "type": "integer", "enum": ["007", 1] } } }
        """).FindField("n")!;

        Assert.IsNull(field.ValueType);
        Assert.AreSequenceEqual(["007", "1"], field.Options.Select(o => o.Value));
    }

    [TestMethod]
    public void A_typed_choice_fed_by_a_provider_keeps_its_type_across_round_trips()
    {
        var level = new BormFieldDefinition("level", BormFieldType.Select) { ValueType = typeof(int?) };
        var ids = new BormFieldDefinition("ids", BormFieldType.MultiSelect) { ValueType = typeof(List<int>) };
        var form = new BormDefinition();
        form.Fields.Add(level);
        form.Fields.Add(ids);

        var reimported = RoundTrip(form);
        var second = BormJsonSchemaExporter.Export(reimported);
        var third = BormJsonSchemaExporter.Export(Import(second));

        Assert.AreEqual("integer", Exported(form, "level").GetProperty("type").GetString());
        Assert.AreEqual("integer", Exported(form, "ids").GetProperty("items").GetProperty("type").GetString());
        Assert.AreEqual(typeof(long?), reimported.FindField("level")!.ValueType);
        Assert.AreEqual(typeof(List<long>), reimported.FindField("ids")!.ValueType);
        Assert.AreEqual(second, third);
    }

    // ---------------------------------------------------------------- review: merging

    [TestMethod]
    public void A_draft04_exclusive_bound_is_folded_into_its_own_branch_before_merging()
    {
        var form = Import("""
        { "type": "object", "properties": {
            "n": { "type": "number", "allOf": [ { "minimum": 5, "exclusiveMinimum": true }, { "minimum": 10 } ] },
            "m": { "type": "number", "allOf": [ { "minimum": 5, "exclusiveMinimum": true } ] } } }
        """);

        var n = Assert.ContainsSingle(form.FindField("n")!.Validators.OfType<BormRangeRule>());
        Assert.AreEqual(10, n.Min);
        Assert.IsFalse(n.MinExclusive);

        var m = Assert.ContainsSingle(form.FindField("m")!.Validators.OfType<BormRangeRule>());
        Assert.AreEqual(5, m.Min);
        Assert.IsTrue(m.MinExclusive);
    }

    [TestMethod]
    public async Task Two_patterns_in_an_allOf_are_both_enforced_and_survive_a_round_trip()
    {
        var form = Import("""
        { "type": "object", "properties": { "p": { "type": "string", "allOf": [ { "pattern": "^a" }, { "pattern": "z$" } ] } } }
        """);

        Assert.IsNotEmpty(await Validate(form, ("p", "abc")));
        Assert.IsNotEmpty(await Validate(form, ("p", "xyz")));
        Assert.IsEmpty(await Validate(form, ("p", "abz")));

        var once = BormJsonSchemaExporter.Export(form);
        var back = Import(once);
        Assert.AreEqual(once, BormJsonSchemaExporter.Export(back));
        Assert.IsNotEmpty(await Validate(back, ("p", "abc")));
        Assert.IsNotEmpty(await Validate(back, ("p", "xyz")));
    }

    [TestMethod]
    public void Two_enums_in_an_allOf_keep_only_what_both_allow_with_their_labels()
    {
        var p = Import("""
        { "type": "object", "properties": { "p": { "allOf": [
            { "enum": ["a", "b", "c"], "enumNames": ["A", "B", "C"] },
            { "enum": ["b", "c", "d"] } ] } } }
        """).FindField("p")!;

        Assert.AreSequenceEqual(["b", "c"], p.Options.Select(o => o.Value));
        Assert.AreSequenceEqual(["B", "C"], p.Options.Select(o => o.Label));
    }

    [TestMethod]
    public void Item_schemas_from_two_branches_are_both_applied()
    {
        var t = Import("""
        { "type": "object", "properties": { "t": { "type": "array", "x-widget": "tags", "allOf": [
            { "items": { "type": "string", "maxLength": 5 } },
            { "items": { "examples": ["a"] } } ] } } }
        """).FindField("t")!;

        Assert.AreEqual(5, t.MaxLength);
        Assert.AreSequenceEqual(["a"], t.Suggestions);
    }

    [TestMethod]
    public void A_condition_comparing_with_a_number_that_has_no_json_form_is_omitted()
    {
        // Written as null, "less than infinity" became "never".
        var form = BormBuilder.Create()
            .Text("a", f => f.VisibleWhen("b", BormConditionOperator.LessThan, double.PositiveInfinity))
            .Build();

        Assert.IsFalse(Exported(form, "a").TryGetProperty("x-visibleWhen", out _));
        Assert.IsFalse(BormConditionJson.CanWrite(form.FindField("a")!.VisibleWhen));
    }
}
