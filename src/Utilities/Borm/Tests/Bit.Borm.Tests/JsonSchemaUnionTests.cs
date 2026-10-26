namespace Bit.Borm.Tests;

/// <summary>
/// Unions of object shapes and subforms that hang off an answer — the JSON Schema constructs that
/// describe "a different set of questions depending on what you chose".
/// </summary>
[TestClass]
public class JsonSchemaUnionTests
{
    private const string Payment = """
    {
      "type": "object",
      "properties": {
        "payment": {
          "title": "Payment",
          "oneOf": [
            {
              "title": "Card",
              "type": "object",
              "properties": {
                "kind": { "const": "card" },
                "number": { "type": "string", "minLength": 12 },
                "holder": { "type": "string" }
              },
              "required": ["number"]
            },
            { "$ref": "#/$defs/Bank" }
          ]
        }
      },
      "$defs": {
        "Bank": {
          "title": "Bank transfer",
          "type": "object",
          "properties": {
            "kind": { "const": "bank" },
            "iban": { "type": "string" },
            "holder": { "type": "string" }
          },
          "required": ["iban", "holder"]
        }
      }
    }
    """;

    [TestMethod]
    public void A_union_told_apart_by_a_constant_becomes_a_choice_and_the_fields_each_branch_adds()
    {
        var form = BormJsonSchemaImporter.Import(Payment);
        var payment = form.FindField("payment")!;

        Assert.AreEqual(BormFieldType.Object, payment.Type);
        CollectionAssert.AreEqual(new[] { "kind", "number", "holder", "iban" }, payment.Children.Select(c => c.Name).ToArray());

        var kind = payment.Children[0];
        Assert.IsTrue(kind.Required);
        CollectionAssert.AreEqual(new[] { "card", "bank" }, kind.Options.Select(o => o.Value).ToArray());
        CollectionAssert.AreEqual(new[] { "Card", "Bank transfer" }, kind.Options.Select(o => o.Label).ToArray());

        var number = payment.Children[1];
        Assert.IsTrue(number.ClearOnHide);
        Assert.IsNotNull(number.VisibleWhen);
        // Shared by both branches: always shown.
        Assert.IsNull(payment.Children[2].VisibleWhen);
    }

    [TestMethod]
    public void Choosing_a_branch_shows_its_fields_requires_what_it_requires_and_clears_what_it_abandons()
    {
        var form = BormJsonSchemaImporter.Import(Payment);
        var data = new Dictionary<string, object?>();
        var state = new BormState(form, new BormDictionaryDataAccessor(data));
        var payment = form.FindField("payment")!;
        var number = payment.Children.Single(c => c.Name == "number");
        var iban = payment.Children.Single(c => c.Name == "iban");
        var holder = payment.Children.Single(c => c.Name == "holder");

        state.SetValue("payment.kind", "card");
        Assert.IsTrue(state.IsVisible(number, "payment.number"));
        Assert.IsFalse(state.IsVisible(iban, "payment.iban"));
        Assert.IsFalse(state.IsRequired(holder, "payment.holder"));

        state.SetValue("payment.number", "4111111111111111");
        state.SetValue("payment.kind", "bank");
        Assert.IsFalse(state.IsVisible(number, "payment.number"));
        Assert.IsNull(state.GetValue("payment.number"));
        Assert.IsTrue(state.IsRequired(holder, "payment.holder"));

        Assert.IsFalse(state.ValidateAsync().AsTask().Result);
        CollectionAssert.AreEquivalent(new[] { "payment.iban", "payment.holder" },
            state.AllMessages.Select(m => m.FieldPath).ToArray());
    }

    [TestMethod]
    public void An_openapi_discriminator_names_the_property_that_chooses()
    {
        var form = BormJsonSchemaImporter.Import("""
        {
          "type": "object",
          "properties": {
            "pet": {
              "discriminator": { "propertyName": "petType" },
              "anyOf": [
                { "properties": { "petType": { "enum": ["cat"] }, "lives": { "type": "integer" } } },
                { "properties": { "petType": { "enum": ["dog"] }, "bark": { "type": "boolean" } } }
              ]
            }
          }
        }
        """);

        var pet = form.FindField("pet")!;
        Assert.AreEqual("petType", pet.Children[0].Name);
        CollectionAssert.AreEqual(new[] { "cat", "dog" }, pet.Children[0].Options.Select(o => o.Value).ToArray());
        Assert.AreEqual(BormFieldType.Integer, pet.Children.Single(c => c.Name == "lives").Type);
    }

    [TestMethod]
    public void An_openapi_hierarchy_is_told_apart_by_its_mapping_or_by_the_name_of_each_definition()
    {
        // What Swashbuckle and NSwag write for a polymorphic model: a base extended through allOf, and
        // a discriminator whose values come from the mapping rather than from a const in each branch.
        var form = BormJsonSchemaImporter.Import("""
        {
          "type": "object",
          "properties": {
            "pet": {
              "oneOf": [ { "$ref": "#/$defs/Cat" }, { "$ref": "#/$defs/Dog" } ],
              "discriminator": { "propertyName": "petType", "mapping": { "cat": "#/$defs/Cat" } }
            }
          },
          "$defs": {
            "Pet": { "type": "object", "properties": { "petType": { "type": "string" }, "name": { "type": "string" } }, "required": ["petType", "name"] },
            "Cat": { "allOf": [ { "$ref": "#/$defs/Pet" }, { "properties": { "lives": { "type": "integer" } } } ] },
            "Dog": { "allOf": [ { "$ref": "#/$defs/Pet" }, { "properties": { "bark": { "type": "boolean" } } } ] }
          }
        }
        """);

        var pet = form.FindField("pet")!;
        CollectionAssert.AreEqual(new[] { "petType", "name", "lives", "bark" }, pet.Children.Select(c => c.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "cat", "Dog" }, pet.Children[0].Options.Select(o => o.Value).ToArray());

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var name = pet.Children[1];
        state.SetValue("pet.petType", "Dog");
        Assert.IsTrue(state.IsVisible(pet.Children.Single(c => c.Name == "bark"), "pet.bark"));
        Assert.IsFalse(state.IsVisible(pet.Children.Single(c => c.Name == "lives"), "pet.lives"));
        // The base's fields belong to every branch: always shown, required once a branch is chosen.
        Assert.IsNull(name.VisibleWhen);
        Assert.IsTrue(state.IsRequired(name, "pet.name"));
    }

    [TestMethod]
    public void A_union_with_nothing_in_the_data_to_tell_its_branches_apart_is_still_left_alone()
    {
        var form = BormJsonSchemaImporter.Import("""
        {
          "type": "object",
          "properties": {
            "contact": {
              "oneOf": [
                { "type": "object", "properties": { "email": { "type": "string" } } },
                { "type": "object", "properties": { "phone": { "type": "string" } } }
              ]
            }
          }
        }
        """);

        Assert.IsEmpty(form.FindField("contact")!.Children);
    }

    [TestMethod]
    public void A_discriminated_union_at_the_root_shapes_the_whole_form()
    {
        var form = BormJsonSchemaImporter.Import("""
        {
          "oneOf": [
            { "title": "Person", "properties": { "type": { "const": 1 }, "name": { "type": "string" } }, "required": ["name"] },
            { "title": "Company", "properties": { "type": { "const": 2 }, "vat": { "type": "string" } } }
          ]
        }
        """);

        CollectionAssert.AreEqual(new[] { "type", "name", "vat" }, form.Fields.Select(f => f.Name).ToArray());
        // The choice keeps the constants' type, so choosing "Company" stores the number 2.
        var data = new Dictionary<string, object?>();
        var state = new BormState(form, new BormDictionaryDataAccessor(data));
        var context = new BormFieldContext(state, form.Fields[0], "type");
        context.SetFromStringAsync("2").Wait();
        Assert.AreEqual(2L, data["type"]);
        Assert.IsTrue(state.IsVisible(form.Fields[2], "vat"));
    }

    [TestMethod]
    public void Dependent_schemas_add_fields_that_show_once_their_trigger_is_answered()
    {
        var form = BormJsonSchemaImporter.Import("""
        {
          "type": "object",
          "properties": {
            "name": { "type": "string" },
            "creditCard": { "type": "string" },
            "address": { "type": "string" }
          },
          "dependentSchemas": {
            "creditCard": {
              "properties": { "billingAddress": { "type": "string" }, "address": {} },
              "required": ["billingAddress", "address"]
            }
          }
        }
        """);
        var data = new Dictionary<string, object?>();
        var state = new BormState(form, new BormDictionaryDataAccessor(data));
        var billing = form.FindField("billingAddress")!;
        var address = form.FindField("address")!;

        Assert.IsFalse(state.IsVisible(billing));
        // Declared by the schema itself, so it always shows — the subform only makes it required.
        Assert.IsTrue(state.IsVisible(address));
        Assert.IsFalse(state.IsRequired(address));

        state.SetValue("creditCard", "4111");
        Assert.IsTrue(state.IsVisible(billing));
        Assert.IsTrue(state.IsRequired(billing));
        Assert.IsTrue(state.IsRequired(address));

        state.SetValue("billingAddress", "1 Main St");
        state.SetValue("creditCard", null);
        Assert.IsNull(data["billingAddress"]);
    }

    [TestMethod]
    public void The_react_jsonschema_form_dependencies_idiom_asks_different_questions_per_answer()
    {
        var form = BormJsonSchemaImporter.Import("""
        {
          "type": "object",
          "properties": {
            "hasPet": { "type": "string", "enum": ["no", "yes: one", "yes: more"] }
          },
          "required": ["hasPet"],
          "dependencies": {
            "hasPet": {
              "oneOf": [
                { "properties": { "hasPet": { "enum": ["no"] } } },
                { "properties": { "hasPet": { "enum": ["yes: one"] }, "petName": { "type": "string" } }, "required": ["petName"] },
                { "properties": { "hasPet": { "enum": ["yes: more"] }, "howMany": { "type": "integer", "minimum": 2 } }, "required": ["howMany"] }
              ]
            }
          }
        }
        """);
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var petName = form.FindField("petName")!;
        var howMany = form.FindField("howMany")!;

        // The answer keeps its own three choices; the questions follow it.
        Assert.HasCount(3, form.FindField("hasPet")!.Options);
        state.SetValue("hasPet", "yes: one");
        Assert.IsTrue(state.IsVisible(petName));
        Assert.IsTrue(state.IsRequired(petName));
        Assert.IsFalse(state.IsVisible(howMany));

        state.SetValue("hasPet", "yes: more");
        Assert.IsFalse(state.IsVisible(petName));
        Assert.IsTrue(state.IsVisible(howMany));
        Assert.IsTrue(state.IsRequired(howMany));
    }

    [TestMethod]
    public void Array_valued_dependencies_keep_meaning_conditional_requiredness()
    {
        var form = BormJsonSchemaImporter.Import("""
        {
          "type": "object",
          "properties": { "a": { "type": "string" }, "b": { "type": "string" }, "c": { "type": "string" } },
          "dependencies": { "a": ["b"], "c": { "properties": { "d": { "type": "string" } } } }
        }
        """);
        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.SetValue("a", "x");
        Assert.IsTrue(state.IsRequired(form.FindField("b")!));
        Assert.IsFalse(state.IsVisible(form.FindField("d")!));
    }

    [TestMethod]
    public void A_recursive_discriminated_union_stays_within_the_import_budgets()
    {
        // A node that is either a leaf or a group of nodes: bounded, not expanded forever.
        var ok = BormJsonSchemaImporter.TryImport("""
        {
          "$defs": {
            "Node": {
              "oneOf": [
                { "properties": { "kind": { "const": "leaf" }, "value": { "type": "string" } } },
                { "properties": { "kind": { "const": "group" }, "children": { "type": "array", "items": { "$ref": "#/$defs/Node" } } } }
              ]
            }
          },
          "type": "object",
          "properties": { "root": { "$ref": "#/$defs/Node" } }
        }
        """, out var form, out var error);

        Assert.IsTrue(ok, error);
        Assert.IsNotNull(form.FindField("root"));
    }
}
