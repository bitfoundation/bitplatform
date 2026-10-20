namespace Bit.Borm.Tests;

[TestClass]
public class RuleDeduplicationTests
{
    [TestMethod]
    public async Task A_model_annotation_refined_by_the_builder_reports_once()
    {
        // FirstName carries [Required] and [StringLength(50, MinimumLength = 2)]. Restating those in
        // the builder used to stack a second copy of each rule and show the user two identical errors.
        var form = BormBuilder.For<RegistrationModel>()
            .Field(x => x.FirstName, f => f.Required("Please enter your first name.").MinLength(2))
            .Build();

        var messages = await new BormValidator()
            .ValidateAsync(form, new BormModelDataAccessor(new RegistrationModel()));

        var firstName = messages.Where(m => m.FieldPath == "FirstName").ToList();
        Assert.ContainsSingle(firstName);
        Assert.AreEqual("Please enter your first name.", firstName[0].Message);
    }

    [TestMethod]
    public void The_last_message_wins_for_a_repeated_rule()
    {
        var field = new BormFieldDefinition("x", BormFieldType.Text);
        new BormFieldBuilder(field).Required("first").Required("second");

        Assert.ContainsSingle(field.Validators);
    }

    [TestMethod]
    public void Custom_rules_without_a_key_still_accumulate()
    {
        var field = new BormFieldDefinition("x", BormFieldType.Text);
        new BormFieldBuilder(field)
            .Must(v => v is not null, "one")
            .Must(v => v is string, "two");

        Assert.AreEqual(2, field.Validators.Count);
    }
}

[TestClass]
public class BuiltInRuleBehaviourTests
{
    private static BormValidationContext Ctx(object? value, IBormDataReader? data = null)
        => new("f", value, data ?? new BormDictionaryDataAccessor());

    [TestMethod]
    public async Task Url_rule_accepts_absolute_http_urls_only()
    {
        var rule = new BormUrlRule();
        Assert.IsTrue((await rule.ValidateAsync(Ctx("https://example.com"))).IsValid);
        Assert.IsTrue((await rule.ValidateAsync(Ctx(""))).IsValid);
        Assert.IsFalse((await rule.ValidateAsync(Ctx("example.com"))).IsValid);
        Assert.IsFalse((await rule.ValidateAsync(Ctx("javascript:alert(1)"))).IsValid);
    }

    [TestMethod]
    public async Task Compare_rule_matches_another_field()
    {
        var data = new BormDictionaryDataAccessor();
        data.SetValue("Password", "hunter2");
        var rule = new BormCompareRule("Password", "Password");

        Assert.IsTrue((await rule.ValidateAsync(Ctx("hunter2", data))).IsValid);
        Assert.IsFalse((await rule.ValidateAsync(Ctx("hunter3", data))).IsValid);
        // An empty value is the required rule's business, not this one's.
        Assert.IsTrue((await rule.ValidateAsync(Ctx("", data))).IsValid);
    }

    [TestMethod]
    public async Task MultipleOf_tolerates_binary_floating_point()
    {
        var rule = new BormMultipleOfRule(0.1);
        Assert.IsTrue((await rule.ValidateAsync(Ctx(0.3))).IsValid);
        Assert.IsFalse((await rule.ValidateAsync(Ctx(0.35))).IsValid);
    }

    [TestMethod]
    public async Task UniqueItems_rejects_duplicates()
    {
        var rule = new BormUniqueItemsRule();
        Assert.IsTrue((await rule.ValidateAsync(Ctx(new List<string> { "a", "b" }))).IsValid);
        Assert.IsFalse((await rule.ValidateAsync(Ctx(new List<string> { "a", "a" }))).IsValid);
    }

    [TestMethod]
    public async Task An_invalid_pattern_is_treated_as_no_constraint_rather_than_throwing()
    {
        // Patterns arrive from untrusted JSON Schema documents; one that does not compile must not
        // take the import (or the render) down.
        var rule = new BormPatternRule("([unclosed");

        Assert.IsFalse(rule.IsPatternValid);
        Assert.IsTrue((await rule.ValidateAsync(Ctx("anything"))).IsValid);
    }

    [TestMethod]
    public async Task Range_message_names_both_bounds()
    {
        var result = await new BormRangeRule(1, 10).ValidateAsync(Ctx(50));
        Assert.Contains("1", result.Message!, StringComparison.Ordinal);
        Assert.Contains("10", result.Message!, StringComparison.Ordinal);
    }
}

[TestClass]
public class MessageProviderTests
{
    private sealed class Dutch : IBormMessageProvider
    {
        public string Get(string key, params object?[] args) => key switch
        {
            BormMessageKeys.Required => "Dit veld is verplicht.",
            _ => key
        };
    }

    private sealed class Services(object service) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(IBormMessageProvider) ? service : null;
    }

    [TestMethod]
    public async Task A_registered_provider_replaces_the_built_in_english()
    {
        var ctx = new BormValidationContext("f", null, new BormDictionaryDataAccessor(), new Services(new Dutch()));
        var result = await new BormRequiredRule().ValidateAsync(ctx);

        Assert.AreEqual("Dit veld is verplicht.", result.Message);
    }

    [TestMethod]
    public async Task An_explicit_message_still_wins_over_the_provider()
    {
        var ctx = new BormValidationContext("f", null, new BormDictionaryDataAccessor(), new Services(new Dutch()));
        var result = await new BormRequiredRule("Custom").ValidateAsync(ctx);

        Assert.AreEqual("Custom", result.Message);
    }

    [TestMethod]
    public async Task Without_a_provider_the_english_defaults_apply()
    {
        var ctx = new BormValidationContext("f", null, new BormDictionaryDataAccessor());
        var result = await new BormRequiredRule().ValidateAsync(ctx);

        Assert.AreEqual("This field is required.", result.Message);
    }
}

[TestClass]
public class TypedBuilderTests
{
    [TestMethod]
    public void A_nested_member_expression_becomes_a_dotted_path()
    {
        var form = BormBuilder.For<RegistrationModel>()
            .Field(x => x.Address.City, f => f.Required())
            .Build();

        var field = form.Fields.Single();
        Assert.AreEqual("Address.City", field.Name);
        Assert.AreEqual("City", field.Label);
    }

    [TestMethod]
    public async Task A_nested_member_field_reads_and_validates_through_its_path()
    {
        var form = BormBuilder.For<RegistrationModel>()
            .Field(x => x.Address.City, f => f.Required("City is required."))
            .Build();

        var model = new RegistrationModel();
        var state = new BormState(form, new BormModelDataAccessor(model));

        Assert.IsFalse(await state.ValidateAsync());
        Assert.AreEqual("City is required.", state.MessagesFor("Address.City")[0].Message);

        state.SetValue("Address.City", "Paris");
        Assert.AreEqual("Paris", model.Address.City);
        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public void Typed_object_builder_configures_children_against_the_child_type()
    {
        var form = BormBuilder.For<RegistrationModel>()
            .Object(x => x.Address, a => a
                .Field(x => x.Street)
                .Field(x => x.City, f => f.Label("Town")))
            .Build();

        var address = form.FindField("Address")!;
        Assert.AreEqual(BormFieldType.Object, address.Type);
        Assert.AreEqual(2, address.Children.Count);
        Assert.AreEqual("Town", address.Children[1].Label);
        // DataAnnotations on the child type are picked up too.
        Assert.IsTrue(address.Children[0].Required);
    }

    [TestMethod]
    public void Typed_array_builder_builds_an_item_template_from_the_element_type()
    {
        var form = BormBuilder.For<RegistrationModel>()
            .Array(x => x.Items, i => i
                .Field(l => l.Product)
                .Field(l => l.Quantity))
            .Build();

        var items = form.FindField("Items")!;
        Assert.AreEqual(BormFieldType.Array, items.Type);
        Assert.AreEqual(2, items.ItemTemplate!.Children.Count);
        Assert.AreEqual(BormFieldType.Integer, items.ItemTemplate.Children[1].Type);
    }

    [TestMethod]
    public void Path_exposes_a_refactor_safe_field_name_for_conditions_and_steps()
        => Assert.AreEqual("Address.City", BormBuilder<RegistrationModel>.Path(x => x.Address.City));

    [TestMethod]
    public void A_non_member_selector_is_rejected_with_guidance()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
            BormBuilder.For<RegistrationModel>().Field(x => x.FirstName.Length + 1));

        Assert.Contains("property access", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void Duplicate_field_names_are_rejected()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
            BormBuilder.Create().Text("name").Text("Name"));

        Assert.Contains("already been added", ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task Form_level_rules_can_target_the_field_the_user_should_fix()
    {
        var form = BormBuilder.For<BookingModel>()
            .Field(x => x.Start)
            .Field(x => x.End)
            .MustAll(m => m.End >= m.Start, "End must be on or after start.", nameof(BookingModel.End))
            .Build();

        var model = new BookingModel { Start = new DateOnly(2024, 5, 10), End = new DateOnly(2024, 5, 1) };
        var state = new BormState(form, new BormModelDataAccessor(model));

        Assert.IsFalse(await state.ValidateAsync());
        Assert.AreEqual("End must be on or after start.", state.MessagesFor("End")[0].Message);
    }

    [TestMethod]
    public void Scalar_arrays_can_be_declared_without_an_object_wrapper()
    {
        var form = BormBuilder.Create()
            .ArrayOf("tags", BormFieldType.Text, f => f.Items(max: 5))
            .Build();

        var tags = form.FindField("tags")!;
        Assert.AreEqual(BormFieldType.Text, tags.ItemTemplate!.Type);
        Assert.IsEmpty(tags.ItemTemplate.Children);
    }
}

[TestClass]
public class EnumOptionTests
{
    [TestMethod]
    public void Enum_options_use_Display_names_and_order()
    {
        var options = BormEnumOptions.For(typeof(Priority));

        Assert.AreEqual(2, options.Count); // Internal is AutoGenerateField = false
        Assert.AreEqual("High priority", options[0].Label);
        Assert.AreEqual("High", options[0].Value);
        Assert.AreEqual("Low priority", options[1].Label);
    }

    [TestMethod]
    public void Flags_enums_become_multi_selects_without_their_zero_member()
    {
        var form = BormSchemaGenerator.Generate<TypedModel>();
        var availability = form.FindField("Availability")!;

        Assert.AreEqual(BormFieldType.MultiSelect, availability.Type);
        Assert.AreEqual(3, availability.Options.Count); // None is excluded
        Assert.DoesNotContain(o => o.Value == "None", availability.Options);
    }

    [TestMethod]
    public void Plain_enums_stay_single_selects()
        => Assert.AreEqual(BormFieldType.Select, BormSchemaGenerator.Generate<TypedModel>().FindField("Priority")!.Type);
}

[TestClass]
public class GeneratorBehaviourTests
{
    [TestMethod]
    public void Computed_properties_are_rendered_read_only_rather_than_editable()
    {
        var summary = BormSchemaGenerator.Generate<TypedModel>().FindField("Summary")!;
        Assert.IsTrue(summary.ReadOnly);
    }

    [TestMethod]
    public void Computed_properties_can_be_dropped_entirely()
    {
        var form = BormSchemaGenerator.Generate<TypedModel>(new BormSchemaGeneratorOptions
        {
            ReadOnlyProperties = BormReadOnlyPropertyHandling.Skip
        });

        Assert.IsNull(form.FindField("Summary"));
    }

    [TestMethod]
    public void Ignored_properties_are_left_out()
    {
        var options = new BormSchemaGeneratorOptions();
        options.IgnoredProperties.Add("id");

        Assert.IsNull(BormSchemaGenerator.Generate<TypedModel>(options).FindField("Id"));
    }

    [TestMethod]
    public void A_convention_hook_runs_for_every_field()
    {
        var form = BormSchemaGenerator.Generate<RegistrationModel>(new BormSchemaGeneratorOptions
        {
            ConfigureField = f => f.HelpText ??= $"Field: {f.Name}"
        });

        Assert.AreEqual("Field: FirstName", form.FindField("FirstName")!.HelpText);
    }

    [TestMethod]
    public void Well_known_reference_types_are_not_mistaken_for_object_groups()
    {
        var form = BormSchemaGenerator.Generate<TypedModel>();

        Assert.AreEqual(BormFieldType.Url, form.FindField("Website")!.Type);
        Assert.AreEqual(BormFieldType.Text, form.FindField("Id")!.Type);
        Assert.AreEqual(BormFieldType.Time, form.FindField("Duration")!.Type);
    }

    [TestMethod]
    public void Compare_annotations_become_a_cross_field_rule()
    {
        var form = BormSchemaGenerator.Generate<SignupModel>();
        Assert.Contains(r => r.Key == "compare", form.FindField("ConfirmPassword")!.Validators);
    }
}

[TestClass]
public class RendererRegistryTests
{
    [TestMethod]
    public void File_fields_resolve_to_the_file_input_not_a_text_box()
    {
        var registry = new BormFieldRendererRegistry();
        var field = new BormFieldDefinition("cv", BormFieldType.File);

        Assert.AreEqual(typeof(BormFileInput), registry.Resolve(field));
    }

    [TestMethod]
    public void An_unregistered_custom_key_fails_loudly()
    {
        var registry = new BormFieldRendererRegistry();
        var field = new BormFieldDefinition("rating", BormFieldType.Custom) { CustomRenderer = "rating" };

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => registry.Resolve(field));
        Assert.Contains("rating", ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_renderer_key_overrides_the_type_default()
    {
        var registry = new BormFieldRendererRegistry();
        registry.RegisterCustom("fancy", typeof(BormTextAreaInput));
        var field = new BormFieldDefinition("notes", BormFieldType.Text) { CustomRenderer = "fancy" };

        Assert.AreEqual(typeof(BormTextAreaInput), registry.Resolve(field));
    }

    [TestMethod]
    public void Registering_a_non_component_is_rejected()
        => Assert.ThrowsExactly<ArgumentException>(() =>
            new BormFieldRendererRegistry().Register(BormFieldType.Text, typeof(string)));
}

[TestClass]
public class SchemaLookupTests
{
    [TestMethod]
    public void FindByPath_walks_objects_and_array_templates()
    {
        var form = BormSchemaGenerator.Generate<RegistrationModel>();

        Assert.AreEqual("City", form.FindByPath("Address.City")!.Name);
        Assert.AreEqual("Product", form.FindByPath("Items[0].Product")!.Name);
        Assert.AreEqual("Items", form.FindByPath("Items")!.Name);
        Assert.IsNull(form.FindByPath("Nope.Missing"));
    }
}

[TestClass]
public class ConditionOperatorTests
{
    private static IBormDataReader Data(params (string, object?)[] values)
    {
        var d = new BormDictionaryDataAccessor();
        foreach (var (k, v) in values) d.SetValue(k, v);
        return d;
    }

    [TestMethod]
    public void StartsWith_and_EndsWith_compare_text()
    {
        var data = Data(("Code", "AB-1234"));
        Assert.IsTrue(new BormFieldCondition("Code", BormConditionOperator.StartsWith, "AB").Evaluate(data));
        Assert.IsTrue(new BormFieldCondition("Code", BormConditionOperator.EndsWith, "1234").Evaluate(data));
        Assert.IsFalse(new BormFieldCondition("Code", BormConditionOperator.StartsWith, "ZZ").Evaluate(data));
    }

    [TestMethod]
    public void Dates_compare_against_a_string_operand()
    {
        var data = Data(("Start", new DateOnly(2024, 5, 10)));
        Assert.IsTrue(new BormFieldCondition("Start", BormConditionOperator.GreaterThan, "2024-01-01").Evaluate(data));
        Assert.IsFalse(new BormFieldCondition("Start", BormConditionOperator.LessThan, "2024-01-01").Evaluate(data));
    }

    [TestMethod]
    public void A_malformed_regex_operand_simply_does_not_match()
    {
        var data = Data(("Code", "ABC"));
        Assert.IsFalse(new BormFieldCondition("Code", BormConditionOperator.Matches, "([unclosed").Evaluate(data));
    }

    [TestMethod]
    public void Numeric_equality_ignores_the_ambient_culture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var data = Data(("Price", 1.5m));
            Assert.IsTrue(new BormFieldCondition("Price", BormConditionOperator.Equals, 1.5).Evaluate(data));
            Assert.IsTrue(new BormFieldCondition("Price", BormConditionOperator.GreaterThan, 1).Evaluate(data));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}
