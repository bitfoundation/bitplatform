using Bit.Borm;

namespace Bit.Borm.Tests;

[TestClass]
public class SchemaGeneratorTests
{
    [TestMethod]
    public void Generates_fields_from_model()
    {
        var form = BormSchemaGenerator.Generate<RegistrationModel>();

        Assert.AreEqual("First name", form.FindField("FirstName")!.Label); // from [Display]
        Assert.IsTrue(form.FindField("FirstName")!.Required);
        Assert.AreEqual(BormFieldType.Email, form.FindField("Email")!.Type);
        Assert.AreEqual(BormFieldType.Select, form.FindField("AccountType")!.Type);
        Assert.AreEqual(2, form.FindField("AccountType")!.Options.Count);
        Assert.AreEqual(BormFieldType.Object, form.FindField("Address")!.Type);
        Assert.AreEqual(BormFieldType.Array, form.FindField("Items")!.Type);
        Assert.IsNotNull(form.FindField("Items")!.ItemTemplate);
    }

    [TestMethod]
    public void Generated_range_and_length_produce_constraints()
    {
        var form = BormSchemaGenerator.Generate<RegistrationModel>();
        var age = form.FindField("Age")!;
        Assert.AreEqual(18, age.Min);
        Assert.AreEqual(120, age.Max);
        Assert.AreEqual(50, form.FindField("FirstName")!.MaxLength);
    }
}

[TestClass]
public class FormBuilderTests
{
    [TestMethod]
    public void Builds_typed_form_with_inferred_types()
    {
        var form = BormBuilder.For<RegistrationModel>()
            .Title("Register")
            .Field(x => x.FirstName, f => f.Required().MinLength(2))
            .Field(x => x.Age, f => f.Range(18, 120))
            .Field(x => x.AccountType)
            .Build();

        Assert.AreEqual("Register", form.Title);
        Assert.AreEqual(BormFieldType.Integer, form.FindField("Age")!.Type);
        Assert.AreEqual(BormFieldType.Select, form.FindField("AccountType")!.Type);
        Assert.AreEqual(2, form.FindField("AccountType")!.Options.Count);
    }

    [TestMethod]
    public void Untyped_builder_supports_objects_and_arrays_and_steps()
    {
        var form = BormBuilder.Create()
            .Text("name", f => f.Required())
            .Object("address", a => a.Text("city"))
            .Array("items", i => i.Text("product").Number("qty"))
            .Step("s1", s => s.Title("Step 1").Fields("name"))
            .Build();

        Assert.IsTrue(form.IsWizard);
        Assert.AreEqual(BormFieldType.Object, form.FindField("address")!.Type);
        Assert.AreEqual(BormFieldType.Array, form.FindField("items")!.Type);
        Assert.AreEqual(2, form.FindField("items")!.ItemTemplate!.Children.Count);
    }
}

[TestClass]
public class FormStateTests
{
    [TestMethod]
    public void Tracks_dirty_and_value_changes()
    {
        var form = BormSchemaGenerator.Generate<RegistrationModel>();
        var state = new BormState(form, new BormModelDataAccessor(new RegistrationModel()));

        Assert.IsFalse(state.IsFormDirty);
        state.SetValue("FirstName", "Grace");
        Assert.IsTrue(state.IsFormDirty);
        Assert.AreEqual("Grace", state.GetValue("FirstName"));
    }

    [TestMethod]
    public void Array_add_and_remove_works_on_typed_model()
    {
        var form = BormSchemaGenerator.Generate<RegistrationModel>();
        var model = new RegistrationModel();
        var state = new BormState(form, new BormModelDataAccessor(model));
        var itemsField = form.FindField("Items")!;

        var idx = state.AddArrayItem(itemsField, "Items");
        Assert.AreEqual(0, idx);
        Assert.ContainsSingle(model.Items);
        Assert.IsExactInstanceOfType<LineItem>(model.Items[0]);

        state.SetValue("Items[0].Product", "Widget");
        Assert.AreEqual("Widget", model.Items[0].Product);

        state.RemoveArrayItem("Items", 0);
        Assert.IsEmpty(model.Items);
    }

    [TestMethod]
    public async Task Wizard_navigation_validates_each_step()
    {
        var form = BormBuilder.For<RegistrationModel>()
            .Field(x => x.FirstName, f => f.Required())
            .Field(x => x.Email, f => f.Required().Email())
            .Step("s1", s => s.Fields("FirstName"))
            .Step("s2", s => s.Fields("Email"))
            .Build();

        var model = new RegistrationModel();
        var state = new BormState(form, new BormModelDataAccessor(model));

        // Cannot advance while first step invalid.
        Assert.IsFalse(await state.NextStepAsync());
        Assert.AreEqual(0, state.CurrentStepIndex);

        state.SetValue("FirstName", "Ada");
        Assert.IsTrue(await state.NextStepAsync());
        Assert.AreEqual(1, state.CurrentStepIndex);
        Assert.IsTrue(state.IsLastStep);
    }

    [TestMethod]
    public void Applies_default_values()
    {
        var form = BormBuilder.Create()
            .Checkbox("subscribe", f => f.Default(true))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        Assert.AreEqual(true, state.GetValue("subscribe"));
    }
}

[TestClass]
public class JsonSchemaTests
{
    private const string Json = """
    {
      "title": "Contact",
      "type": "object",
      "required": ["name", "email"],
      "properties": {
        "name": { "type": "string", "title": "Full name", "minLength": 2 },
        "email": { "type": "string", "format": "email" },
        "age": { "type": "integer", "minimum": 0, "maximum": 120 },
        "role": { "type": "string", "enum": ["admin", "user"], "enumNames": ["Admin", "User"] },
        "bio": { "type": "string", "x-widget": "textarea" },
        "address": {
          "type": "object",
          "properties": { "city": { "type": "string" } }
        },
        "tags": { "type": "array", "items": { "type": "string" } }
      }
    }
    """;

    [TestMethod]
    public void Imports_json_schema()
    {
        var form = BormJsonSchemaImporter.Import(Json);

        Assert.AreEqual("Contact", form.Title);
        Assert.IsTrue(form.FindField("name")!.Required);
        Assert.AreEqual("Full name", form.FindField("name")!.Label);
        Assert.AreEqual(BormFieldType.Email, form.FindField("email")!.Type);
        Assert.AreEqual(BormFieldType.Integer, form.FindField("age")!.Type);
        Assert.AreEqual(BormFieldType.Select, form.FindField("role")!.Type);
        Assert.AreEqual("Admin", form.FindField("role")!.Options[0].Label);
        Assert.AreEqual(BormFieldType.TextArea, form.FindField("bio")!.Type);
        Assert.AreEqual(BormFieldType.Object, form.FindField("address")!.Type);
        Assert.AreEqual(BormFieldType.Array, form.FindField("tags")!.Type);
    }

    [TestMethod]
    public void Round_trips_through_export_and_import()
    {
        var original = BormJsonSchemaImporter.Import(Json);
        var exported = BormJsonSchemaExporter.Export(original);
        var reimported = BormJsonSchemaImporter.Import(exported);

        Assert.AreEqual(original.Fields.Count, reimported.Fields.Count);
        Assert.AreEqual(BormFieldType.Email, reimported.FindField("email")!.Type);
        Assert.AreEqual(BormFieldType.Select, reimported.FindField("role")!.Type);
        Assert.IsTrue(reimported.FindField("name")!.Required);
        Assert.AreEqual(BormFieldType.Object, reimported.FindField("address")!.Type);
    }
}
