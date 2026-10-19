using Microsoft.Extensions.DependencyInjection;

namespace Bit.Borm.Tests;

/// <summary>
/// A collection drawn from a fixed set of answers is a set of tick boxes. A repeater is right for a
/// list of things the user writes and absurd for a list of three possible days.
/// </summary>
[TestClass]
public class ChoiceCollectionTests
{
    private sealed class Availability
    {
        public List<AccountType> Accounts { get; set; } = [];
        public Priority[] Priorities { get; set; } = [];
        public List<AccountType?> Optional { get; set; } = [];
        public List<string> Notes { get; set; } = [];
        public List<LineItem> Lines { get; set; } = [];
    }

    private static BormDefinition Schema() => BormSchemaGenerator.Generate<Availability>();

    [TestMethod]
    public void A_list_of_enum_members_is_a_multi_select()
        => Assert.AreEqual(BormFieldType.MultiSelect, Schema().FindField("Accounts")!.Type);

    [TestMethod]
    public void An_array_of_enum_members_is_too()
        => Assert.AreEqual(BormFieldType.MultiSelect, Schema().FindField("Priorities")!.Type);

    [TestMethod]
    public void A_collection_of_nullable_enum_members_is_too()
        => Assert.AreEqual(BormFieldType.MultiSelect, Schema().FindField("Optional")!.Type);

    [TestMethod]
    public void The_choices_come_from_the_element_type_not_the_collection()
    {
        var options = Schema().FindField("Accounts")!.Options.Select(o => o.Value).ToList();

        Assert.AreEqual(Enum.GetNames<AccountType>().Length, options.Count);
        Assert.Contains("Business", options);
    }

    [TestMethod]
    public void A_list_of_free_text_is_still_a_repeater_because_the_answers_are_not_a_fixed_set()
        => Assert.AreEqual(BormFieldType.Array, Schema().FindField("Notes")!.Type);

    [TestMethod]
    public void A_list_of_objects_is_still_a_repeater()
        => Assert.AreEqual(BormFieldType.Array, Schema().FindField("Lines")!.Type);

    [TestMethod]
    public void The_chosen_members_reach_the_typed_model()
    {
        var model = new Availability();
        var state = new BormState(Schema(), new BormModelDataAccessor(model));

        state.SetValue("Accounts", new List<string> { "Business" });

        Assert.AreSequenceEqual([AccountType.Business], model.Accounts);
    }

    [TestMethod]
    public void Ticking_and_unticking_leaves_the_form_clean()
    {
        var model = new Availability();
        var state = new BormState(Schema(), new BormModelDataAccessor(model));

        state.SetValue("Accounts", new List<string> { "Business" });
        state.SetValue("Accounts", new List<string>());

        Assert.IsFalse(state.IsFormDirty);
    }
}

/// <summary>An array of a closed set of choices, described in JSON rather than in C#.</summary>
[TestClass]
public class JsonChoiceCollectionTests
{
    [TestMethod]
    public void An_array_of_enum_values_that_may_not_repeat_imports_as_a_multi_select()
    {
        const string json = """
        {"type":"object","properties":{
          "days":{"type":"array","uniqueItems":true,
                  "items":{"type":"string","enum":["mon","tue"],"enumNames":["Monday","Tuesday"]}}
        }}
        """;

        var field = BormJsonSchemaImporter.Import(json).FindField("days")!;

        Assert.AreEqual(BormFieldType.MultiSelect, field.Type);
        Assert.AreSequenceEqual(["Monday", "Tuesday"], field.Options.Select(o => o.Label));
        // The rule has nothing left to say: a set of tick boxes cannot hold the same answer twice.
        Assert.DoesNotContain(v => v.Key == "uniqueItems", field.Validators);
    }

    [TestMethod]
    public void Without_uniqueItems_it_stays_a_repeater_because_the_document_allows_duplicates()
    {
        const string json = """
        {"type":"object","properties":{
          "picks":{"type":"array","items":{"type":"string","enum":["a","b"]}}
        }}
        """;

        var field = BormJsonSchemaImporter.Import(json).FindField("picks")!;

        Assert.AreEqual(BormFieldType.Array, field.Type);
        Assert.AreEqual(BormFieldType.Select, field.ItemTemplate!.Type);
    }

    [TestMethod]
    public void An_array_of_objects_is_untouched_by_the_promotion()
    {
        const string json = """
        {"type":"object","properties":{
          "lines":{"type":"array","uniqueItems":true,
                   "items":{"type":"object","properties":{"product":{"type":"string"}}}}
        }}
        """;

        var field = BormJsonSchemaImporter.Import(json).FindField("lines")!;

        Assert.AreEqual(BormFieldType.Array, field.Type);
        Assert.AreEqual(BormFieldType.Object, field.ItemTemplate!.Type);
    }

    [TestMethod]
    public void A_multi_select_keeps_its_size_bounds_across_a_round_trip()
    {
        var form = BormBuilder.Create()
            .MultiSelect("days", f => f
                .Options(("mon", "Monday"), ("tue", "Tuesday"), ("wed", "Wednesday"))
                .Items(min: 1, max: 2))
            .Build();

        var field = BormJsonSchemaImporter
            .Import(BormJsonSchemaExporter.Export(form))
            .FindField("days")!;

        Assert.AreEqual(BormFieldType.MultiSelect, field.Type);
        Assert.AreEqual(1, field.MinItems);
        Assert.AreEqual(2, field.MaxItems);
        Assert.Contains(v => v.Key == "items", field.Validators);
    }

    [TestMethod]
    public void A_repeater_of_choices_this_library_exported_comes_back_as_a_repeater()
    {
        // Same shape the promotion looks for — an array of a closed set that may not repeat — but the
        // author already said what they wanted, and an export says so explicitly.
        var form = BormBuilder.Create()
            .ArrayOf("picks", BormFieldType.Select,
                f => f.UniqueItems(),
                item => item.Options(("a", "A"), ("b", "B")))
            .Build();

        var field = BormJsonSchemaImporter
            .Import(BormJsonSchemaExporter.Export(form))
            .FindField("picks")!;

        Assert.AreEqual(BormFieldType.Array, field.Type);
        Assert.IsNotNull(field.ItemTemplate);
        Assert.Contains(v => v.Key == "uniqueItems", field.Validators);
    }

    [TestMethod]
    public void A_tag_list_keeps_its_size_bounds_too()
    {
        var form = BormBuilder.Create().Tags("skills", f => f.AsTags(max: 5)).Build();

        var field = BormJsonSchemaImporter
            .Import(BormJsonSchemaExporter.Export(form))
            .FindField("skills")!;

        Assert.AreEqual(BormFieldType.Tags, field.Type);
        Assert.AreEqual(5, field.MaxItems);
    }
}

/// <summary>
/// Registering the renderer registry twice must configure the one that is there, not build a second
/// one and quietly drop it.
/// </summary>
[TestClass]
public class ServiceRegistrationTests
{
    private sealed class Dummy : BormInputBase { }

    [TestMethod]
    public void A_later_call_configures_the_registry_the_first_one_registered()
    {
        var services = new ServiceCollection();

        // What a component library or a shared module does…
        services.AddBorm();
        // …and then what the application does.
        services.AddBorm(r => r.RegisterCustom<Dummy>("rating"));

        var registry = services.BuildServiceProvider().GetRequiredService<IBormFieldRendererRegistry>();
        var field = new BormFieldDefinition("score", BormFieldType.Custom) { CustomRenderer = "rating" };

        Assert.IsTrue(registry.TryResolve(field, out var component));
        Assert.AreEqual(typeof(Dummy), component);
    }

    [TestMethod]
    public void Both_calls_contribute_rather_than_the_last_one_winning()
    {
        var services = new ServiceCollection();
        services.AddBorm(r => r.RegisterCustom<Dummy>("first"));
        services.AddBorm(r => r.RegisterCustom<Dummy>("second"));

        var registry = services.BuildServiceProvider().GetRequiredService<IBormFieldRendererRegistry>();

        Assert.IsTrue(registry.TryResolve(
            new BormFieldDefinition("a", BormFieldType.Custom) { CustomRenderer = "first" }, out _));
        Assert.IsTrue(registry.TryResolve(
            new BormFieldDefinition("b", BormFieldType.Custom) { CustomRenderer = "second" }, out _));
    }

    [TestMethod]
    public void Only_one_registry_is_ever_registered()
    {
        var services = new ServiceCollection();
        services.AddBorm();
        services.AddBorm();

        Assert.ContainsSingle(d => d.ServiceType == typeof(IBormFieldRendererRegistry), services);
    }
}
