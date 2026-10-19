namespace Bit.Borm.Tests;

[TestClass]
public class JsonSchemaImportTests
{
    [TestMethod]
    public void Resolves_local_refs_into_defs()
    {
        const string json = """
        {
          "type": "object",
          "properties": {
            "home": { "$ref": "#/$defs/address" },
            "work": { "$ref": "#/$defs/address" }
          },
          "$defs": {
            "address": {
              "type": "object",
              "title": "Address",
              "required": ["city"],
              "properties": {
                "street": { "type": "string" },
                "city":   { "type": "string" }
              }
            }
          }
        }
        """;

        var form = BormJsonSchemaImporter.Import(json);

        var home = form.FindField("home")!;
        Assert.AreEqual(BormFieldType.Object, home.Type);
        Assert.AreEqual(2, home.Children.Count);
        Assert.IsTrue(home.Children.Single(c => c.Name == "city").Required);
        Assert.AreEqual(2, form.FindField("work")!.Children.Count);
    }

    [TestMethod]
    public void A_reference_cycle_terminates_instead_of_recursing_forever()
    {
        const string json = """
        {
          "type": "object",
          "properties": { "node": { "$ref": "#/$defs/loop" } },
          "$defs": { "loop": { "$ref": "#/$defs/loop" } }
        }
        """;

        var form = BormJsonSchemaImporter.Import(json);
        Assert.IsNotNull(form.FindField("node"));
    }

    [TestMethod]
    public void Merges_allOf_branches_into_one_field_set()
    {
        const string json = """
        {
          "allOf": [
            { "type": "object", "required": ["id"], "properties": { "id": { "type": "string" } } },
            { "type": "object", "properties": { "name": { "type": "string", "minLength": 2 } } }
          ],
          "type": "object",
          "title": "Merged",
          "required": ["name"],
          "properties": { "email": { "type": "string", "format": "email" } }
        }
        """;

        var form = BormJsonSchemaImporter.Import(json);

        Assert.AreEqual("Merged", form.Title);
        Assert.AreEqual(3, form.Fields.Count);
        Assert.IsTrue(form.FindField("id")!.Required);
        Assert.IsTrue(form.FindField("name")!.Required);
        Assert.AreEqual(2, form.FindField("name")!.MinLength);
        Assert.AreEqual(BormFieldType.Email, form.FindField("email")!.Type);
    }

    [TestMethod]
    public void Nullable_type_unions_resolve_to_the_non_null_type()
    {
        const string json = """
        { "type": "object", "properties": { "age": { "type": ["integer", "null"] } } }
        """;

        Assert.AreEqual(BormFieldType.Integer, BormJsonSchemaImporter.Import(json).FindField("age")!.Type);
    }

    [TestMethod]
    public async Task Exclusive_bounds_reject_the_boundary_value()
    {
        const string json = """
        {
          "type": "object",
          "properties": { "count": { "type": "integer", "exclusiveMinimum": 0, "exclusiveMaximum": 10 } }
        }
        """;

        var form = BormJsonSchemaImporter.Import(json);
        var data = new BormDictionaryDataAccessor();
        var validator = new BormValidator();

        data.SetValue("count", 0);
        Assert.IsNotEmpty(await validator.ValidateAsync(form, data));

        data.SetValue("count", 1);
        Assert.IsEmpty(await validator.ValidateAsync(form, data));

        data.SetValue("count", 10);
        Assert.IsNotEmpty(await validator.ValidateAsync(form, data));
    }

    [TestMethod]
    public async Task MultipleOf_and_uniqueItems_become_rules()
    {
        const string json = """
        {
          "type": "object",
          "properties": {
            "quantity": { "type": "number", "multipleOf": 5 },
            "tags": { "type": "array", "uniqueItems": true, "items": { "type": "string" } }
          }
        }
        """;

        var form = BormJsonSchemaImporter.Import(json);
        var data = new BormDictionaryDataAccessor();
        data.SetValue("quantity", 7);
        data.SetValue("tags", new List<object?> { "a", "a" });

        var messages = await new BormValidator().ValidateAsync(form, data);

        Assert.Contains(m => m.FieldPath == "quantity", messages);
        Assert.Contains(m => m.FieldPath == "tags", messages);
    }

    [TestMethod]
    public void Const_pins_a_value_and_makes_the_field_read_only()
    {
        const string json = """
        { "type": "object", "properties": { "kind": { "type": "string", "const": "invoice" } } }
        """;

        var kind = BormJsonSchemaImporter.Import(json).FindField("kind")!;
        Assert.AreEqual("invoice", kind.DefaultValue);
        Assert.IsTrue(kind.ReadOnly);
    }

    [TestMethod]
    public void ReadOnly_and_extra_formats_are_honoured()
    {
        const string json = """
        {
          "type": "object",
          "properties": {
            "id":     { "type": "string", "format": "uuid", "readOnly": true },
            "avatar": { "type": "string", "contentEncoding": "base64", "contentMediaType": "image/png" },
            "site":   { "type": "string", "format": "uri" }
          }
        }
        """;

        var form = BormJsonSchemaImporter.Import(json);

        Assert.IsTrue(form.FindField("id")!.ReadOnly);
        Assert.AreEqual(BormFieldType.File, form.FindField("avatar")!.Type);
        Assert.AreEqual(BormFieldType.Url, form.FindField("site")!.Type);
        Assert.Contains(v => v.Key == "url", form.FindField("site")!.Validators);
    }

    [TestMethod]
    public async Task DependentRequired_becomes_conditional_requiredness()
    {
        const string json = """
        {
          "type": "object",
          "properties": {
            "creditCard": { "type": "string" },
            "billingAddress": { "type": "string" }
          },
          "dependentRequired": { "creditCard": ["billingAddress"] }
        }
        """;

        var form = BormJsonSchemaImporter.Import(json);
        var data = new BormDictionaryDataAccessor();
        var validator = new BormValidator();

        // No card, no obligation.
        Assert.IsEmpty(await validator.ValidateAsync(form, data));

        data.SetValue("creditCard", "4111111111111111");
        var messages = await validator.ValidateAsync(form, data);
        Assert.Contains(m => m.FieldPath == "billingAddress", messages);
    }

    [TestMethod]
    public async Task If_then_else_becomes_conditional_requiredness()
    {
        const string json = """
        {
          "type": "object",
          "properties": {
            "kind":        { "type": "string", "enum": ["personal", "business"] },
            "companyName": { "type": "string" },
            "nickname":    { "type": "string" }
          },
          "if":   { "properties": { "kind": { "const": "business" } } },
          "then": { "required": ["companyName"] },
          "else": { "required": ["nickname"] }
        }
        """;

        var form = BormJsonSchemaImporter.Import(json);
        var data = new BormDictionaryDataAccessor();
        var validator = new BormValidator();

        data.SetValue("kind", "business");
        var business = await validator.ValidateAsync(form, data);
        Assert.Contains(m => m.FieldPath == "companyName", business);
        Assert.DoesNotContain(m => m.FieldPath == "nickname", business);

        data.SetValue("kind", "personal");
        var personal = await validator.ValidateAsync(form, data);
        Assert.Contains(m => m.FieldPath == "nickname", personal);
        Assert.DoesNotContain(m => m.FieldPath == "companyName", personal);
    }

    [TestMethod]
    public void Conditions_travel_in_x_extensions()
    {
        const string json = """
        {
          "type": "object",
          "properties": {
            "isBusiness": { "type": "boolean" },
            "companyName": {
              "type": "string",
              "x-clearOnHide": true,
              "x-visibleWhen": { "field": "isBusiness", "op": "IsTrue" },
              "x-requiredWhen": { "field": "isBusiness", "op": "IsTrue" }
            },
            "vat": {
              "type": "string",
              "x-disabledWhen": { "any": [
                 { "field": "isBusiness", "op": "IsFalse" },
                 { "field": "companyName", "op": "IsEmpty" }
              ] }
            }
          }
        }
        """;

        var form = BormJsonSchemaImporter.Import(json);
        var company = form.FindField("companyName")!;

        Assert.IsTrue(company.ClearOnHide);
        Assert.IsNotNull(company.VisibleWhen);
        Assert.IsNotNull(company.RequiredWhen);

        var vat = form.FindField("vat")!;
        var group = Assert.IsExactInstanceOfType<BormConditionGroup>(vat.DisabledWhen);
        Assert.AreEqual(BormConditionLogic.Or, group.Logic);
        Assert.AreEqual(2, group.Conditions.Count);
    }

    [TestMethod]
    public void Wizard_steps_travel_in_x_steps()
    {
        const string json = """
        {
          "type": "object",
          "properties": { "a": { "type": "string" }, "b": { "type": "string" } },
          "x-steps": [
            { "id": "one", "title": "First", "fields": ["a"] },
            { "id": "two", "title": "Second", "fields": ["b"],
              "visibleWhen": { "field": "a", "op": "IsNotEmpty" } }
          ]
        }
        """;

        var form = BormJsonSchemaImporter.Import(json);

        Assert.IsTrue(form.IsWizard);
        Assert.AreEqual(2, form.Steps.Count);
        Assert.AreEqual("First", form.Steps[0].Title);
        Assert.IsNotNull(form.Steps[1].VisibleWhen);
    }

    [TestMethod]
    public void Malformed_json_is_reported_rather_than_thrown()
    {
        Assert.IsFalse(BormJsonSchemaImporter.TryImport("{ not json", out _, out var error));
        Assert.IsNotNull(error);

        Assert.IsTrue(BormJsonSchemaImporter.TryImport("""{"type":"object","properties":{}}""", out var form, out _));
        Assert.IsEmpty(form.Fields);
    }
}

[TestClass]
public class JsonSchemaRoundTripTests
{
    [TestMethod]
    public void A_built_form_survives_export_and_import()
    {
        var original = BormBuilder.Create()
            .Title("Order")
            .Description("Place an order")
            .Columns(2)
            .Text("customer", f => f.Required().Placeholder("Who is ordering?").Autocomplete("name").ColumnSpan(2))
            .Select("kind", f => f.Options(("personal", "Personal"), ("business", "Business")))
            .Text("companyName", f => f
                .VisibleWhen("kind", BormConditionOperator.Equals, "business")
                .RequiredWhen("kind", BormConditionOperator.Equals, "business")
                .ClearOnHide())
            .Integer("quantity", f => f.Range(1, 99).Step(1))
            .Field("notes", BormFieldType.TextArea, f => f.ReadOnly())
            .ArrayOf("tags", BormFieldType.Text, f => f.Items(min: 1, max: 3).UniqueItems())
            .Object("address", a => a.Text("city", c => c.Required()).Text("zip"))
            .Step("s1", s => s.Title("Who").Fields("customer", "kind"))
            .Step("s2", s => s.Title("What").Fields("quantity", "tags")
                .VisibleWhen(new BormFieldCondition("kind", BormConditionOperator.IsNotEmpty)))
            .Build();

        var json = BormJsonSchemaExporter.Export(original);
        var reimported = BormJsonSchemaImporter.Import(json);

        Assert.AreEqual(original.Title, reimported.Title);
        Assert.AreEqual(original.Description, reimported.Description);
        Assert.AreEqual(2, reimported.Columns);
        Assert.AreEqual(original.Fields.Count, reimported.Fields.Count);

        var customer = reimported.FindField("customer")!;
        Assert.IsTrue(customer.Required);
        Assert.AreEqual("Who is ordering?", customer.Placeholder);
        Assert.AreEqual("name", customer.Autocomplete);
        Assert.AreEqual(2, customer.ColumnSpan);

        var company = reimported.FindField("companyName")!;
        Assert.IsTrue(company.ClearOnHide);
        var visible = Assert.IsExactInstanceOfType<BormFieldCondition>(company.VisibleWhen);
        Assert.AreEqual("kind", visible.FieldPath);
        Assert.AreEqual(BormConditionOperator.Equals, visible.Operator);
        Assert.AreEqual("business", visible.Value);
        Assert.IsNotNull(company.RequiredWhen);

        var quantity = reimported.FindField("quantity")!;
        Assert.AreEqual(BormFieldType.Integer, quantity.Type);
        Assert.AreEqual(1, quantity.Min);
        Assert.AreEqual(99, quantity.Max);

        Assert.AreEqual(BormFieldType.TextArea, reimported.FindField("notes")!.Type);
        Assert.IsTrue(reimported.FindField("notes")!.ReadOnly);

        var tags = reimported.FindField("tags")!;
        Assert.AreEqual(1, tags.MinItems);
        Assert.AreEqual(3, tags.MaxItems);
        Assert.Contains(v => v.Key == "uniqueItems", tags.Validators);

        var address = reimported.FindField("address")!;
        Assert.AreEqual(BormFieldType.Object, address.Type);
        Assert.IsTrue(address.Children.Single(c => c.Name == "city").Required);

        Assert.IsTrue(reimported.IsWizard);
        Assert.AreEqual(2, reimported.Steps.Count);
        Assert.AreEqual("Who", reimported.Steps[0].Title);
        Assert.AreSequenceEqual(["customer", "kind"], reimported.Steps[0].Fields);
        Assert.IsNotNull(reimported.Steps[1].VisibleWhen);
    }

    [TestMethod]
    public void A_generated_schema_survives_export_and_import()
    {
        var original = BormSchemaGenerator.Generate<RegistrationModel>();
        var reimported = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(original));

        Assert.AreEqual(original.Fields.Count, reimported.Fields.Count);
        Assert.AreEqual(BormFieldType.Email, reimported.FindField("Email")!.Type);
        Assert.AreEqual(BormFieldType.Select, reimported.FindField("AccountType")!.Type);
        Assert.AreEqual(BormFieldType.Object, reimported.FindField("Address")!.Type);
        Assert.AreEqual(BormFieldType.Array, reimported.FindField("Items")!.Type);
        Assert.AreEqual(2, reimported.FindField("Items")!.ItemTemplate!.Children.Count);
    }

    [TestMethod]
    public void Delegate_backed_conditions_are_omitted_rather_than_approximated()
    {
        var form = BormBuilder.Create()
            .Text("a")
            .Text("b", f => f.VisibleWhen(data => data.GetValue("a") is not null, "a"))
            .Build();

        var json = BormJsonSchemaExporter.Export(form);

        Assert.DoesNotContain("x-visibleWhen", json, StringComparison.Ordinal);
        Assert.IsNull(BormJsonSchemaImporter.Import(json).FindField("b")!.VisibleWhen);
    }

    [TestMethod]
    public void Numeric_enum_values_keep_their_type()
    {
        var form = BormBuilder.Create()
            .Integer("level", f => f.Options(("1", "Low"), ("2", "High")))
            .Build();
        // Options() flips the field to Select; force it back so the export uses the integer type.
        form.FindField("level")!.Type = BormFieldType.Integer;

        var json = BormJsonSchemaExporter.Export(form);

        Assert.Contains("\"enum\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"1\"", json, StringComparison.Ordinal);
    }

    [TestMethod]
    public void The_export_declares_its_schema_version()
        => Assert.Contains("json-schema.org", BormJsonSchemaExporter.Export(BormBuilder.Create().Text("a").Build()),
            StringComparison.Ordinal);
}
