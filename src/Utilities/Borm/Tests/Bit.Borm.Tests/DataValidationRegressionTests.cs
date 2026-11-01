using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Bunit;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Borm.Tests;

// ---------------------------------------------------------------- models

public struct DvPoint
{
    public int X { get; set; }
    public int Y { get; set; }
}

public struct DvFrame
{
    public DvPoint Origin { get; set; }
}

public class DvShapes
{
    public DvPoint Point { get; set; }
    public DvPoint? Maybe { get; set; }
    public DvFrame Frame { get; set; }
    public List<DvPoint> Points { get; set; } = [];
}

public class DvAccess
{
    public string Id { get; private set; } = "orig";
    public bool IsAdmin { get; internal set; }
    public string Code { get; init; } = "";
    public string Secret { private get; set; } = "hidden";
    public string Name { get; set; } = "";
}

public class DvAddress { public string? City { get; set; } }
public class DvCustomer { public DvAddress Address { get; set; } = new(); }
public class DvLine { public string? Product { get; set; } }
public class DvOrder { public List<DvLine> Lines { get; set; } = []; }
public class DvTags { public List<string> Tags { get; set; } = ["a"]; }
public class DvOuter
{
    public DvCustomer Customer { get; set; } = new();
    public DvOrder Order { get; set; } = new();
}

public class DvRow
{
    public string? Name { get; set; }
    public int Qty { get; set; }
}

public record DvTag(string Name, List<int> Codes);

public class DvLoop
{
    public string Name { get; set; } = "";
    public DvLoop? Self { get; set; }
}

[Flags]
public enum DvPerm { None = 0, Read = 1, Write = 2 }

public class DvFlagsModel { public DvPerm Perms { get; set; } }

public class DvAnnotated
{
    [Range(0d, 1d, MinimumIsExclusive = true, MaximumIsExclusive = true)]
    public double Ratio { get; set; }

    [MinLength(1)]
    public List<DayOfWeek> Days { get; set; } = [];

    [MaxLength(2)]
    public List<IBrowserFile> Attachments { get; set; } = [];

    [Url]
    public string? Site { get; set; }

    [RegularExpression(@"\d{5}")]
    public string? Zip { get; set; }

    [RegularExpression("a|b")]
    public string? Letter { get; set; }

    [FileExtensions(Extensions = "pdf,docx")]
    public string? FileName { get; set; }

    [FileExtensions(Extensions = "png")]
    public IBrowserFile? Picture { get; set; }

    [DataType(DataType.Upload)]
    public string? UploadName { get; set; }

    [Range(typeof(DateTime), "0050-01-01", "2030-01-01")]
    public DateTime Ancient { get; set; }
}

public class DvCultureBounds
{
    [Range(typeof(decimal), "0,01", "99,99")]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "0.01", "99.99", ParseLimitsInInvariantCulture = true)]
    public decimal Invariant { get; set; }
}

public class DvBinary
{
    public string Name { get; set; } = "";
    public byte[]? Thumbnail { get; set; }
    public Stream? Archive { get; set; }
    public List<byte[]>? Pages { get; set; }
}

public class DvCollections
{
    public HashSet<DayOfWeek> Days { get; set; } = [];
    public ISet<string> Labels { get; set; } = new HashSet<string>();
    public ObservableCollection<string> Watched { get; set; } = [];
    public ReadOnlyCollection<string> Frozen { get; set; } = new([]);
}

public class DvNullable { public int? Age { get; set; } }

public class DvFieldModel
{
#pragma warning disable CA1051 // a public field is exactly what this test needs
    public string? PublicField;
#pragma warning restore CA1051
}

public class DvValidatable : IValidatableObject
{
    public int Calls { get; private set; }

    [Required]
    public string? Name { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        Calls++;
        yield return new ValidationResult("Whole-model problem.");
    }
}

public class DvDates { public DateOnly? When { get; set; } }

public class DvStamped { public DateTimeOffset When { get; set; } }

public class DvParent
{
    public int NavigationReads { get; set; }
    public List<DvChild> Children { get; } = [];
}

public class DvChild(DvParent parent)
{
    public string? Name { get; set; }
    public int N { get; set; }

    // A navigation property: on an entity, reading it would lazy-load the parent row.
    public DvParent Parent
    {
        get
        {
            parent.NavigationReads++;
            return parent;
        }
    }
}

public class DvAggregate
{
    public DvAddress Shipping { get; private set; } = new();
    public List<DvLine> Lines { get; private set; } = [];
    public List<DvLine> GetOnlyLines { get; } = [];
    public DvPoint Spot { get; private set; }
    public string[] Codes { get; private set; } = [];
    public string Reference { get; private set; } = "";
}

public class DvAggregateView
{
    public DvAddress Shipping { get; private set; } = new();
    public List<DvLine> Lines { get; private set; } = [new DvLine()];
}

[CustomValidation(typeof(DvRanged), nameof(Check))]
public class DvRanged
{
    [Required]
    public string? Start { get; set; }

    // Written, as class-level checks are, on the assumption that [Required] has already passed.
    public static ValidationResult? Check(DvRanged model, ValidationContext context)
        => model.Start!.Length > 1 ? ValidationResult.Success : new ValidationResult("Start is too short.");
}

public sealed class DvFile(string name) : IBrowserFile
{
    public string Name => name;
    public DateTimeOffset LastModified => default;
    public long Size => 10;
    public string ContentType => "application/octet-stream";
    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => Stream.Null;
}

// ---------------------------------------------------------------- tests

[TestClass]
public class DataValidationRegressionTests
{
    private static BormRuleResult Run(IBormValidationRule rule, object? value,
        BormFieldDefinition? field = null, IBormDataReader? data = null)
        => rule.ValidateAsync(new BormValidationContext("x", value, data ?? new BormDictionaryDataAccessor(), null, field))
            .AsTask().GetAwaiter().GetResult();

    private static bool Valid(BormFieldDefinition field, object? value)
        => field.Validators.All(rule => Run(rule, value, field).IsValid);

    private static T InCulture<T>(string culture, Func<T> body)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try { return body(); }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    // ---- #1: a date before the year 100 no longer takes validation down

    [TestMethod]
    public void A_date_before_the_year_100_fails_a_minimum_instead_of_throwing()
    {
        var rule = new BormRangeRule(new DateTime(2020, 1, 1).ToOADate(), new DateTime(2030, 1, 1).ToOADate());

        Assert.IsFalse(Run(rule, new DateOnly(50, 1, 1)).IsValid);
        Assert.IsFalse(Run(rule, new DateTime(2, 5, 1)).IsValid);
        Assert.IsFalse(Run(rule, "0099-05-01").IsValid);
    }

    [TestMethod]
    public void A_date_before_the_year_100_is_below_a_maximum()
    {
        var rule = new BormRangeRule(null, new DateTime(2030, 1, 1).ToOADate());

        Assert.IsTrue(Run(rule, new DateOnly(50, 1, 1)).IsValid);
    }

    [TestMethod]
    public async Task A_form_given_a_year_99_date_reports_it_rather_than_throwing()
    {
        var form = BormBuilder.For<DvDates>()
            .Field(x => x.When, f => f.Range(new DateOnly(2020, 1, 1), new DateOnly(2030, 1, 1)))
            .Build();
        var state = new BormState(form, new BormModelDataAccessor(new DvDates()));
        state.SetValue("When", "0099-05-01");

        Assert.IsFalse(await state.ValidateAsync());
        Assert.IsNotEmpty(state.MessagesFor("When"));
    }

    [TestMethod]
    public void A_range_bound_before_the_year_100_is_no_bound_rather_than_an_exception()
    {
        var field = BormSchemaGenerator.Generate<DvAnnotated>().FindField("Ancient")!;

        Assert.IsNull(field.Min);
        Assert.IsNotNull(field.Max);
    }

    // ---- #2: writes through a struct reach the model

    [TestMethod]
    public void A_write_through_a_struct_property_reaches_the_model()
    {
        var model = new DvShapes();
        var data = new BormModelDataAccessor(model);

        data.SetValue("Point.X", 5);

        Assert.IsFalse(data.LastWriteFailed);
        Assert.AreEqual(5, model.Point.X);
    }

    [TestMethod]
    public void A_write_through_a_struct_inside_a_struct_reaches_the_model()
    {
        var model = new DvShapes();
        var data = new BormModelDataAccessor(model);

        data.SetValue("Frame.Origin.Y", 3);

        Assert.AreEqual(3, model.Frame.Origin.Y);
    }

    [TestMethod]
    public void A_write_into_a_struct_list_element_reaches_the_list()
    {
        var model = new DvShapes { Points = [new DvPoint(), new DvPoint()] };
        var data = new BormModelDataAccessor(model);

        data.SetValue("Points[1].X", 7);

        Assert.AreEqual(7, model.Points[1].X);
        Assert.AreEqual(0, model.Points[0].X);
    }

    [TestMethod]
    public void A_write_through_a_nullable_struct_creates_and_keeps_it()
    {
        var model = new DvShapes();
        var data = new BormModelDataAccessor(model);

        data.SetValue("Maybe.X", 9);
        data.SetValue("Maybe.Y", 4);

        Assert.AreEqual(9, model.Maybe?.X);
        Assert.AreEqual(4, model.Maybe?.Y);
    }

    [TestMethod]
    public void A_generated_struct_group_is_editable_through_the_form()
    {
        var model = new DvShapes();
        var state = new BormState(BormSchemaGenerator.Generate<DvShapes>(), new BormModelDataAccessor(model));

        state.SetValue("Point.X", 42);

        Assert.AreEqual(42, model.Point.X);
        Assert.AreEqual(42, state.GetValue("Point.X"));
    }

    // ---- #3: uniqueItems compares content

    [TestMethod]
    public void Distinct_dictionary_rows_are_not_duplicates()
    {
        var rows = new List<object?>
        {
            new Dictionary<string, object?> { ["name"] = "A" },
            new Dictionary<string, object?> { ["name"] = "B" }
        };

        Assert.IsTrue(Run(new BormUniqueItemsRule(), rows).IsValid);
    }

    [TestMethod]
    public void Numbers_are_duplicates_by_the_value_they_were_written_as()
    {
        Assert.IsTrue(Run(new BormUniqueItemsRule(), new List<object?> { 1000000000000001.0, 1000000000000002.0 }).IsValid);
        Assert.IsFalse(Run(new BormUniqueItemsRule(), new List<object?> { 0.1f, 0.1m }).IsValid);
    }

    [TestMethod]
    public void Equal_dictionary_rows_are_duplicates_whatever_the_key_casing_or_order()
    {
        var rows = new List<object?>
        {
            new Dictionary<string, object?> { ["name"] = "A", ["qty"] = 1L },
            new Dictionary<string, object?> { ["QTY"] = 1.0, ["Name"] = "A" }
        };

        Assert.IsFalse(Run(new BormUniqueItemsRule(), rows).IsValid);
    }

    [TestMethod]
    public void Model_rows_are_compared_by_their_properties()
    {
        var rule = new BormUniqueItemsRule();

        Assert.IsTrue(Run(rule, new List<DvRow> { new() { Name = "A" }, new() { Name = "B" } }).IsValid);
        Assert.IsFalse(Run(rule, new List<DvRow> { new() { Name = "A", Qty = 2 }, new() { Name = "A", Qty = 2 } }).IsValid);
    }

    [TestMethod]
    public void Model_rows_are_compared_by_their_scalar_members_only()
    {
        // A model row's reference-typed members (a list here, a navigation property on an entity) are
        // not read at all, so two rows that differ only there are the same row as far as the rule goes.
        var rule = new BormUniqueItemsRule();

        Assert.IsFalse(Run(rule, new List<DvTag> { new("a", [1, 2]), new("a", [2, 1]) }).IsValid);
        Assert.IsTrue(Run(rule, new List<DvTag> { new("a", [1, 2]), new("b", [1, 2]) }).IsValid);
    }

    [TestMethod]
    public async Task A_model_rows_navigation_members_are_never_read()
    {
        var parent = new DvParent();
        for (var i = 0; i < 2000; i++) parent.Children.Add(new DvChild(parent) { Name = "c" + i, N = i });

        var ctx = new BormValidationContext("Children", parent.Children, new BormModelDataAccessor(parent));

        Assert.IsTrue((await new BormUniqueItemsRule().ValidateAsync(ctx)).IsValid);
        Assert.AreEqual(0, parent.NavigationReads);
    }

    [TestMethod]
    public void A_dictionary_row_that_refers_back_to_the_form_is_cut_off_there()
    {
        var root = new Dictionary<string, object?>();
        var rows = new List<object?>
        {
            new Dictionary<string, object?> { ["name"] = "A", ["owner"] = root },
            new Dictionary<string, object?> { ["name"] = "B", ["owner"] = root }
        };
        root["rows"] = rows;
        var data = new BormDictionaryDataAccessor(root);

        Assert.IsTrue(Run(new BormUniqueItemsRule(), rows, data: data).IsValid);
        rows.Add(new Dictionary<string, object?> { ["name"] = "A", ["owner"] = root });
        Assert.IsFalse(Run(new BormUniqueItemsRule(), rows, data: data).IsValid);
    }

    [TestMethod]
    public void Numbers_are_equal_by_value_whatever_type_holds_them()
    {
        var rule = new BormUniqueItemsRule();

        Assert.IsFalse(Run(rule, new List<object?> { 1, 1.0m }).IsValid);
        Assert.IsFalse(Run(rule, new List<object?> { 1L, 1.0 }).IsValid);
        Assert.IsFalse(Run(rule, new List<object?> { 0.1, 0.1m }).IsValid);
        Assert.IsTrue(Run(rule, new List<object?> { 1, 2 }).IsValid);
    }

    [TestMethod]
    public void Text_and_numbers_are_never_equal_and_text_is_compared_exactly()
    {
        var rule = new BormUniqueItemsRule();

        Assert.IsTrue(Run(rule, new List<object?> { "1", 1 }).IsValid);
        Assert.IsTrue(Run(rule, new List<object?> { "a", "A" }).IsValid);
        Assert.IsTrue(Run(rule, new List<object?> { new List<object?> { "a,b" }, new List<object?> { "a", "b" } }).IsValid);
    }

    [TestMethod]
    public void Json_items_are_compared_as_the_values_they_stand_for()
    {
        var rule = new BormUniqueItemsRule();
        var same = JsonSerializer.Deserialize<List<object?>>("""["a", "a"]""");
        var different = JsonSerializer.Deserialize<List<object?>>("""["a", "b"]""");
        var numbers = JsonSerializer.Deserialize<List<object?>>("""[1, 1.0]""");

        Assert.IsFalse(Run(rule, same).IsValid);
        Assert.IsTrue(Run(rule, different).IsValid);
        Assert.IsFalse(Run(rule, numbers).IsValid);
    }

    [TestMethod]
    public void An_object_graph_that_loops_does_not_hang_the_rule()
    {
        var a = new DvLoop { Name = "a" };
        a.Self = a;
        var b = new DvLoop { Name = "b" };
        b.Self = b;

        Assert.IsTrue(Run(new BormUniqueItemsRule(), new List<DvLoop> { a, b }).IsValid);
    }

    [TestMethod]
    public async Task A_json_schema_array_of_objects_with_uniqueItems_accepts_two_different_rows()
    {
        const string json = """{"type":"object","properties":{"rows":{"type":"array","uniqueItems":true,"items":{"type":"object","properties":{"name":{"type":"string"}}}}}}""";
        var data = new BormDictionaryDataAccessor();
        data.SetValue("rows[0].name", "A");
        data.SetValue("rows[1].name", "B");
        var state = new BormState(BormJsonSchemaImporter.Import(json), data);

        Assert.IsTrue(await state.ValidateAsync());

        state.SetValue("rows[1].name", "A");
        Assert.IsFalse(await state.ValidateAsync());
    }

    // ---- #4: [RegularExpression] keeps its whole-value meaning

    [TestMethod]
    public void RegularExpression_must_match_the_whole_value()
    {
        var field = BormSchemaGenerator.Generate<DvAnnotated>().FindField("Zip")!;

        Assert.AreEqual(@"^(?:\d{5})$", field.Pattern);
        Assert.IsTrue(Valid(field, "12345"));
        Assert.IsFalse(Valid(field, "abc12345xyz"));
        Assert.AreEqual(new RegularExpressionAttribute(@"\d{5}").IsValid("abc12345xyz"), Valid(field, "abc12345xyz"));
    }

    [TestMethod]
    public void RegularExpression_does_not_accept_a_trailing_newline()
    {
        // .NET's `$` also matches before a final newline; the rule ends at \z, as the attribute's own
        // whole-length check does. The rendered pattern keeps `$`, the browser's dialect.
        var field = BormSchemaGenerator.Generate<DvAnnotated>().FindField("Zip")!;

        Assert.IsFalse(new RegularExpressionAttribute(@"\d{5}").IsValid("12345\n"));
        Assert.IsFalse(Valid(field, "12345\n"));
        Assert.AreEqual(@"^(?:\d{5})\z", field.Validators.OfType<BormPatternRule>().Single().Pattern);
    }

    [TestMethod]
    public void RegularExpression_with_an_alternation_is_anchored_at_both_ends()
    {
        var field = BormSchemaGenerator.Generate<DvAnnotated>().FindField("Letter")!;

        Assert.IsTrue(Valid(field, "a"));
        Assert.IsFalse(Valid(field, "ab"));
        Assert.IsFalse(Valid(field, "xb"));
    }

    // ---- #5: only public accessors are the form's to use

    [TestMethod]
    public void A_property_with_a_non_public_setter_is_generated_read_only()
    {
        var form = BormSchemaGenerator.Generate<DvAccess>();

        Assert.IsTrue(form.FindField("Id")!.ReadOnly);
        Assert.IsTrue(form.FindField("IsAdmin")!.ReadOnly);
        Assert.IsFalse(form.FindField("Code")!.ReadOnly); // init is a public setter
        Assert.IsFalse(form.FindField("Name")!.ReadOnly);
    }

    [TestMethod]
    public void A_property_with_a_non_public_getter_is_not_generated_or_read()
    {
        var form = BormSchemaGenerator.Generate<DvAccess>();
        var data = new BormModelDataAccessor(new DvAccess());

        Assert.IsNull(form.FindField("Secret"));
        Assert.IsNull(data.GetValue("Secret"));
        Assert.IsFalse(data.TryGetValue("Secret", out _));
    }

    [TestMethod]
    public void A_property_with_a_non_public_setter_is_skipped_when_read_only_properties_are()
    {
        var form = BormSchemaGenerator.Generate<DvAccess>(new BormSchemaGeneratorOptions
        {
            ReadOnlyProperties = BormReadOnlyPropertyHandling.Skip
        });

        Assert.IsNull(form.FindField("Id"));
        Assert.IsNull(form.FindField("IsAdmin"));
        Assert.IsNotNull(form.FindField("Name"));
    }

    [TestMethod]
    public void The_accessor_refuses_to_write_through_a_non_public_setter()
    {
        var model = new DvAccess();
        var data = new BormModelDataAccessor(model);

        data.SetValue("Id", "hacked");
        Assert.IsTrue(data.LastWriteFailed);
        Assert.AreEqual("orig", model.Id);

        data.SetValue("IsAdmin", true);
        Assert.IsTrue(data.LastWriteFailed);
        Assert.IsFalse(model.IsAdmin);
    }

    [TestMethod]
    public void The_typed_builder_marks_a_property_without_a_public_setter_read_only()
    {
        var form = BormBuilder.For<DvAccess>().Field(x => x.Id).Field(x => x.Name).Build();

        Assert.IsTrue(form.FindField("Id")!.ReadOnly);
        Assert.IsFalse(form.FindField("Name")!.ReadOnly);
    }

    [TestMethod]
    public void A_class_group_or_list_with_a_non_public_setter_stays_editable()
    {
        // Children are written on the object the property already holds and rows are added to the list
        // in place, so the setter is never needed — and a read-only container would lock everything in it.
        var form = BormSchemaGenerator.Generate<DvAggregate>();

        Assert.IsFalse(form.FindField("Shipping")!.ReadOnly);
        Assert.IsFalse(form.FindField("Lines")!.ReadOnly);
        Assert.IsFalse(form.FindField("GetOnlyLines")!.ReadOnly);
        Assert.IsTrue(form.FindField("Spot")!.ReadOnly);   // a struct is written back through its setter
        Assert.IsTrue(form.FindField("Codes")!.ReadOnly);  // an array grows by being replaced
        Assert.IsTrue(form.FindField("Reference")!.ReadOnly);

        var built = BormBuilder.For<DvAggregate>().Field(x => x.Lines).Field(x => x.Reference).Build();
        Assert.IsFalse(built.FindField("Lines")!.ReadOnly);
        Assert.IsTrue(built.FindField("Reference")!.ReadOnly);
    }

    [TestMethod]
    public void A_class_group_or_list_with_a_non_public_setter_is_edited_in_place()
    {
        var model = new DvAggregate();
        var form = BormSchemaGenerator.Generate<DvAggregate>();
        var state = new BormState(form, new BormModelDataAccessor(model));

        state.SetValue("Shipping.City", "Delft");
        state.AddArrayItem(form.FindField("Lines")!, "Lines");
        state.SetValue("Lines[0].Product", "Widget");

        Assert.AreEqual("Delft", model.Shipping.City);
        Assert.AreEqual("Widget", Assert.ContainsSingle(model.Lines).Product);
    }

    // ---- #2 follow-up: raw content through the typed builder fails where it is written

    [TestMethod]
    public void The_typed_builder_refuses_raw_content_without_a_renderer()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() => BormBuilder.For<DvBinary>().Field(x => x.Thumbnail));
        Assert.Contains("CustomRenderer", ex.Message, StringComparison.Ordinal);
        Assert.ThrowsExactly<ArgumentException>(() => BormBuilder.For<DvBinary>().Field(x => x.Archive));

        var form = BormBuilder.For<DvBinary>().Field(x => x.Thumbnail, f => f.CustomRenderer("thumbnail")).Build();
        Assert.AreEqual("thumbnail", form.Fields[0].CustomRenderer);
    }

    // ---- condition evaluation stays exception-free on text that is not the typed value

    [TestMethod]
    public void Comparing_typed_values_with_unparseable_text_throws_nothing()
    {
        var data = new BormDictionaryDataAccessor(new Dictionary<string, object?>
        {
            ["Code"] = "other",
            ["Count"] = 3L,
            ["When"] = new DateTime(2024, 1, 1),
            ["Id"] = Guid.NewGuid(),
            ["Day"] = DayOfWeek.Monday
        });
        BormFieldCondition[] conditions =
        [
            new("Code", BormConditionOperator.Equals, 1),
            new("Count", BormConditionOperator.Equals, ""),
            new("Count", BormConditionOperator.GreaterThan, "lots"),
            new("When", BormConditionOperator.Equals, "free text"),
            new("When", BormConditionOperator.Equals, 5),
            new("Id", BormConditionOperator.Equals, "not a guid"),
            new("Day", BormConditionOperator.Equals, "Someday"),
            new("Day", BormConditionOperator.Equals, "   ")
        ];

        // The handler runs on the throwing thread; counting only this one keeps other tests out of it.
        var thread = Environment.CurrentManagedThreadId;
        var thrown = 0;
        void Count(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (Environment.CurrentManagedThreadId == thread) thrown++;
        }

        AppDomain.CurrentDomain.FirstChanceException += Count;
        try
        {
            foreach (var condition in conditions)
                Assert.IsFalse(condition.Evaluate(data));
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= Count;
        }

        Assert.AreEqual(0, thrown);
    }

    // ---- #6: paths beneath a container whose name is dotted

    private static BormDefinition DottedContainers() => BormBuilder.For<DvOuter>()
        .Object(x => x.Customer.Address, a => a.Field(p => p.City, f => f.Required()))
        .Array(x => x.Order.Lines, l => l.Field(p => p.Product))
        .Build();

    [TestMethod]
    public void FindByPath_reaches_fields_beneath_a_dotted_container()
    {
        var form = DottedContainers();

        Assert.AreEqual("City", form.FindByPath("Customer.Address.City")?.Name);
        Assert.AreEqual("Product", form.FindByPath("Order.Lines[0].Product")?.Name);
        Assert.AreEqual("item", form.FindByPath("Order.Lines[3]")?.Name);
        Assert.IsNull(form.FindByPath("Customer.Address.Nope"));
        Assert.IsNull(form.FindByPath("Customer.Nope"));
    }

    [TestMethod]
    public void FindByPath_still_resolves_ordinary_paths()
    {
        var form = BormBuilder.Create()
            .Object("Address", a => a.Text("City"))
            .Array("Items", i => i.Text("Product"))
            .Build();

        Assert.AreEqual("City", form.FindByPath("address.city")?.Name);
        Assert.AreEqual("Product", form.FindByPath("Items[2].Product")?.Name);
        Assert.IsNull(form.FindByPath("[0]"));
    }

    [TestMethod]
    public void A_change_handler_beneath_a_dotted_container_runs()
    {
        var hits = 0;
        var form = BormBuilder.For<DvOuter>()
            .Object(x => x.Customer.Address, a => a.Field(p => p.City, f => f.OnChange(_ => hits++)))
            .Build();
        var state = new BormState(form, new BormModelDataAccessor(new DvOuter()));

        state.SetValue("Customer.Address.City", "Paris");

        Assert.AreEqual(1, hits);
    }

    [TestMethod]
    public async Task A_wizard_step_validates_a_field_beneath_a_dotted_container()
    {
        var form = BormBuilder.For<DvOuter>()
            .Object(x => x.Customer.Address, a => a.Field(p => p.City, f => f.Required()))
            .Step("where", s => s.Fields("Customer.Address.City"))
            .Build();
        var state = new BormState(form, new BormModelDataAccessor(new DvOuter()));

        Assert.IsFalse(await state.ValidateStepAsync());
        Assert.IsTrue(await state.ValidateFieldAsync("Customer.Address.City"));
        Assert.DoesNotContain(d => d.Message.Contains("not in the schema", StringComparison.Ordinal), form.Validate());
    }

    // ---- #7: multipleOf on realistic amounts

    [TestMethod]
    [DataRow(1234567.89, true)]
    [DataRow(99999999.99, true)]
    [DataRow(0.07, true)]
    [DataRow(4.35, true)]
    [DataRow(1234567.891, false)]
    [DataRow(0.005, false)]
    public void MultipleOf_a_cent_judges_large_amounts_correctly(double value, bool expected)
        => Assert.AreEqual(expected, Run(new BormMultipleOfRule(0.01), value).IsValid);

    [TestMethod]
    public void MultipleOf_still_handles_binary_fractions_decimals_and_non_numbers()
    {
        Assert.IsTrue(Run(new BormMultipleOfRule(0.1), 0.3).IsValid);
        Assert.IsFalse(Run(new BormMultipleOfRule(0.1), 0.35).IsValid);
        Assert.IsTrue(Run(new BormMultipleOfRule(0.01), 123456789.12m).IsValid);
        Assert.IsFalse(Run(new BormMultipleOfRule(5), double.NaN).IsValid);
        Assert.IsTrue(Run(new BormMultipleOfRule(5), 15L).IsValid);
    }

    [TestMethod]
    public void MultipleOf_reads_every_number_as_the_one_it_was_written_as()
    {
        // Integers are exact past a double's fifteen digits, and so is a whole double below 2^53.
        Assert.IsFalse(Run(new BormMultipleOfRule(2), 1000000000000001L).IsValid);
        Assert.IsTrue(Run(new BormMultipleOfRule(2), 1000000000000002L).IsValid);
        Assert.IsFalse(Run(new BormMultipleOfRule(2), 1000000000000001.0).IsValid);
        Assert.IsFalse(Run(new BormMultipleOfRule(2), ulong.MaxValue).IsValid);
        // A float is the number it prints as, not its binary noise.
        Assert.IsTrue(Run(new BormMultipleOfRule(0.1), 0.3f).IsValid);
        Assert.IsFalse(Run(new BormMultipleOfRule(0.1), 0.35f).IsValid);
        // A fraction left noisy by binary arithmetic is still a multiple.
        Assert.IsTrue(Run(new BormMultipleOfRule(0.1), 0.1 + 0.2).IsValid);
        Assert.IsTrue(Run(new BormMultipleOfRule(0.01), 19.99 * 3).IsValid);
        // Text is read through the double it parses as.
        Assert.IsTrue(Run(new BormMultipleOfRule(0.01), "1234567.89").IsValid);
        Assert.IsFalse(Run(new BormMultipleOfRule(0.01), "1234567.891").IsValid);
    }

    // ---- #9: the dictionary store

    [TestMethod]
    public void The_callers_dictionary_is_the_live_store_at_every_level()
    {
        var address = new Dictionary<string, object?> { ["city"] = "Paris" };
        var data = new Dictionary<string, object?> { ["name"] = "Ann", ["address"] = address };
        var store = new BormDictionaryDataAccessor(data);

        store.SetValue("name", "Bea");
        store.SetValue("address.city", "Rome");
        store.SetValue("added", 1);

        Assert.AreSame(data, store.Root);
        Assert.AreEqual("Bea", data["name"]);
        Assert.AreEqual("Rome", address["city"]);
        Assert.AreEqual(1, data["added"]);
    }

    [TestMethod]
    public void A_case_sensitive_nested_dictionary_is_read_and_written_without_regard_to_case()
    {
        var address = new Dictionary<string, object?> { ["City"] = "Paris" };
        var store = new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["Address"] = address });

        Assert.AreEqual("Paris", store.GetValue("address.city"));

        store.SetValue("ADDRESS.city", "Rome");

        Assert.AreSequenceEqual(["City"], address.Keys);
        Assert.AreEqual("Rome", address["City"]);
    }

    [TestMethod]
    public void Writing_past_the_end_of_an_array_grows_it_and_keeps_its_type()
    {
        var data = new Dictionary<string, object?> { ["tags"] = new[] { "a" } };
        var store = new BormDictionaryDataAccessor(data);

        store.SetValue("tags[2]", "c");

        Assert.IsFalse(store.LastWriteFailed);
        var tags = Assert.IsExactInstanceOfType<string[]>(data["tags"]);
        Assert.AreEqual(3, tags.Length);
        Assert.AreEqual("a", tags[0]);
        Assert.IsNull(tags[1]);
        Assert.AreEqual("c", tags[2]);
    }

    [TestMethod]
    public void A_typed_list_is_padded_with_its_own_default_and_given_converted_values()
    {
        var numbers = new List<int> { 1 };
        var store = new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["nums"] = numbers });

        store.SetValue("nums[3]", "7");

        Assert.IsFalse(store.LastWriteFailed);
        Assert.AreSequenceEqual([1, 0, 0, 7], numbers);
    }

    [TestMethod]
    public void Writing_far_past_the_end_of_a_list_is_refused_rather_than_padded()
    {
        var tags = new List<object?> { "a" };
        var data = new Dictionary<string, object?> { ["tags"] = tags, ["codes"] = new[] { "a" } };
        var store = new BormDictionaryDataAccessor(data);

        store.SetValue("tags[100000000]", "x");
        Assert.IsTrue(store.LastWriteFailed);
        store.SetValue("codes[100000000]", "x");
        Assert.IsTrue(store.LastWriteFailed);

        Assert.HasCount(1, tags);
        Assert.AreEqual("a", tags[0]);
        var codes = Assert.IsExactInstanceOfType<string[]>(data["codes"]);
        Assert.HasCount(1, codes);
        Assert.AreEqual("a", codes[0]);
    }

    [TestMethod]
    public void Writing_far_past_the_end_of_a_model_list_is_refused_rather_than_padded()
    {
        var model = new DvOrder();
        var tags = new DvTags();

        var store = new BormModelDataAccessor(tags);
        store.SetValue("Tags[100000000]", "x");
        Assert.IsTrue(store.LastWriteFailed);
        // A row below the end is materialised on the way, so it is refused there too.
        new BormModelDataAccessor(model).SetValue("Lines[100000000].Product", "x");

        Assert.AreSequenceEqual(["a"], tags.Tags);
        Assert.IsEmpty(model.Lines);
        // A short gap is still padded, as it always was.
        store.SetValue("Tags[3]", "x");
        Assert.IsFalse(store.LastWriteFailed);
        Assert.HasCount(4, tags.Tags);
        Assert.AreEqual("x", tags.Tags[3]);

        // The limit itself: from one item, a gap of exactly MaxPadding is padded and one more is refused.
        var atLimit = new DvTags();
        var atLimitStore = new BormModelDataAccessor(atLimit);
        atLimitStore.SetValue("Tags[1001]", "x");
        Assert.IsFalse(atLimitStore.LastWriteFailed);
        Assert.HasCount(1002, atLimit.Tags);

        var pastLimit = new DvTags();
        var pastLimitStore = new BormModelDataAccessor(pastLimit);
        pastLimitStore.SetValue("Tags[1002]", "x");
        Assert.IsTrue(pastLimitStore.LastWriteFailed);
        Assert.AreSequenceEqual(["a"], pastLimit.Tags);
    }

    [TestMethod]
    public void A_matches_rule_whose_pattern_times_out_fails_rather_than_passes()
    {
        var value = new string('a', 80) + "!";
        var rule = new BormExpressionRule("matches({x}, '^(a|aa)+$')", "Letters only.");

        var result = Run(rule, value, data: new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["x"] = value }));

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("This value could not be checked; please simplify it.", result.Message);
    }

    [TestMethod]
    public void A_value_a_typed_container_cannot_hold_is_reported_not_thrown()
    {
        var names = new List<string> { "a" };
        var scores = new Dictionary<string, int> { ["math"] = 9 };
        var store = new BormDictionaryDataAccessor(new Dictionary<string, object?>
        {
            ["names"] = names,
            ["scores"] = scores
        });

        store.SetValue("names[0].first", "x");
        Assert.IsTrue(store.LastWriteFailed);
        Assert.AreSequenceEqual(["a"], names);

        store.SetValue("scores.math", "abc");
        Assert.IsTrue(store.LastWriteFailed);
        Assert.AreEqual(9, scores["math"]);

        store.SetValue("bad[", 1);
        Assert.IsTrue(store.LastWriteFailed);
    }

    [TestMethod]
    public void A_read_only_seed_dictionary_is_copied_rather_than_locking_the_form()
    {
        var seed = new Dictionary<string, object?> { ["a"] = 1 };
        var readOnly = new ReadOnlyDictionary<string, object?>(seed);
        var store = new BormDictionaryDataAccessor(readOnly);

        store.SetValue("a", 2);
        store.SetValue("b", 3);

        Assert.IsFalse(store.LastWriteFailed);
        Assert.AreEqual(2, store.GetValue("A"));
        Assert.AreEqual(3, store.GetValue("b"));
        Assert.AreEqual(1, seed["a"]);
        Assert.AreNotSame(readOnly, store.Root);

        var immutable = new BormDictionaryDataAccessor(
            System.Collections.Immutable.ImmutableDictionary<string, object?>.Empty.Add("x", "y"));
        immutable.SetValue("x", "z");
        Assert.IsFalse(immutable.LastWriteFailed);
        Assert.AreEqual("z", immutable.GetValue("x"));
    }

    [TestMethod]
    public void A_read_only_nested_container_is_replaced_by_a_writable_copy()
    {
        var frozen = new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?> { ["keep"] = 1 });
        var data = new Dictionary<string, object?> { ["frozen"] = frozen, ["list"] = new ReadOnlyCollection<object?>(["a"]) };
        var store = new BormDictionaryDataAccessor(data);

        store.SetValue("frozen.key", 2);
        store.SetValue("list[1]", "b");

        Assert.IsFalse(store.LastWriteFailed);
        Assert.AreEqual(1, store.GetValue("frozen.keep"));
        Assert.AreEqual(2, store.GetValue("frozen.key"));
        Assert.AreEqual("b", store.GetValue("list[1]"));
        Assert.AreNotSame(frozen, data["frozen"]);
    }

    [TestMethod]
    public void A_typed_nested_dictionary_is_written_into_not_replaced()
    {
        var scores = new Dictionary<string, int> { ["math"] = 9, ["art"] = 7 };
        var data = new Dictionary<string, object?> { ["scores"] = scores };
        var store = new BormDictionaryDataAccessor(data);

        store.SetValue("scores.math", 5L);

        Assert.IsFalse(store.LastWriteFailed);
        Assert.AreSame(scores, data["scores"]);
        Assert.AreEqual(5, scores["math"]);
        Assert.AreEqual(7, store.GetValue("scores.art"));
    }

    [TestMethod]
    public void Deserialized_json_is_read_and_written_through_and_becomes_clr_values()
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, object?>>(
            """{"name":"Ann","age":30,"ratio":0.5,"tags":["x","y"],"address":{"city":"Paris","zip":"75001"}}""")!;
        var store = new BormDictionaryDataAccessor(data);

        Assert.AreEqual("Paris", store.GetValue("address.city"));
        Assert.AreEqual(30L, store.GetValue("age"));
        Assert.AreEqual(0.5, store.GetValue("ratio"));
        Assert.AreEqual("y", store.GetValue("tags[1]"));

        store.SetValue("address.city", "Rome");

        Assert.AreEqual("75001", store.GetValue("address.zip"));
        var address = Assert.IsInstanceOfType<IDictionary<string, object?>>(data["address"]);
        Assert.AreEqual("Rome", address["city"]);
        Assert.IsExactInstanceOfType<long>(data["age"]);
    }

    [TestMethod]
    public async Task Rules_see_clr_values_in_a_form_over_deserialized_json()
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, object?>>("""{"age":30}""")!;
        var form = BormBuilder.Create().Integer("age", f => f.Range(0, 10)).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(data));

        Assert.IsFalse(await state.ValidateAsync());
        Assert.IsNotEmpty(state.MessagesFor("age"));
    }

    // ---- #10: any constructible collection is a valid target

    [TestMethod]
    public void A_multi_select_writes_into_sets_and_other_collections()
    {
        var model = new DvCollections();
        var data = new BormModelDataAccessor(model);

        data.SetValue("Days", new List<string> { "Monday", "Tuesday" });
        Assert.IsFalse(data.LastWriteFailed);
        Assert.AreSequenceEqual([DayOfWeek.Monday, DayOfWeek.Tuesday], model.Days.OrderBy(d => d));

        data.SetValue("Labels", new List<string> { "a", "b" });
        Assert.IsFalse(data.LastWriteFailed);
        Assert.IsTrue(model.Labels.SetEquals(["a", "b"]));

        data.SetValue("Watched", new List<string> { "w" });
        Assert.IsFalse(data.LastWriteFailed);
        Assert.AreSequenceEqual(["w"], model.Watched);

        data.SetValue("Frozen", new List<string> { "f" });
        Assert.IsFalse(data.LastWriteFailed);
        Assert.AreSequenceEqual(["f"], model.Frozen);
    }

    // ---- #11: raw content and file names

    [TestMethod]
    public void Byte_arrays_streams_and_lists_of_them_are_not_generated()
    {
        var form = BormSchemaGenerator.Generate<DvBinary>();

        Assert.AreSequenceEqual(["Name"], form.Fields.Select(f => f.Name));
        Assert.AreEqual(BormFieldType.Custom, BormFieldTypeResolver.Resolve(typeof(byte[])));
        Assert.AreEqual(BormFieldType.Custom, BormFieldTypeResolver.Resolve(typeof(MemoryStream)));
        Assert.AreEqual(BormFieldType.File, BormFieldTypeResolver.Resolve(typeof(IBrowserFile)));
        Assert.AreEqual(BormFieldType.File, BormFieldTypeResolver.Resolve(typeof(List<IBrowserFile>)));
    }

    [TestMethod]
    public void FileExtensions_on_a_string_keeps_it_text_and_checks_the_name()
    {
        var field = BormSchemaGenerator.Generate<DvAnnotated>().FindField("FileName")!;

        Assert.AreEqual(BormFieldType.Text, field.Type);
        Assert.IsTrue(Valid(field, "report.PDF"));
        Assert.IsTrue(Valid(field, "notes.docx"));
        Assert.IsTrue(Valid(field, ""));
        Assert.IsFalse(Valid(field, "virus.exe"));
    }

    [TestMethod]
    public void FileExtensions_on_a_browser_file_is_still_a_picker()
    {
        var field = BormSchemaGenerator.Generate<DvAnnotated>().FindField("Picture")!;

        Assert.AreEqual(BormFieldType.File, field.Type);
        Assert.AreEqual(".png", field.Accept);
        Assert.IsFalse(Valid(field, new DvFile("a.gif")));
        Assert.IsTrue(Valid(field, new DvFile("a.png")));
    }

    [TestMethod]
    public void DataType_upload_on_a_string_does_not_make_it_a_picker()
        => Assert.AreEqual(BormFieldType.Text, BormSchemaGenerator.Generate<DvAnnotated>().FindField("UploadName")!.Type);

    // ---- #12: a timestamp round-trips to the same instant

    // Every offset here is stated explicitly, so none of these depends on the machine's time zone.

    [TestMethod]
    [DataRow(5, 45)]
    [DataRow(-9, 30)]
    [DataRow(14, 0)]
    [DataRow(-12, 0)]
    public void A_DateTimeOffset_is_shown_on_its_own_clock_and_keeps_its_offset(int hours, int minutes)
    {
        var offset = new TimeSpan(hours, hours < 0 ? -minutes : minutes, 0);
        var original = new DateTimeOffset(2024, 1, 15, 10, 30, 0, offset);

        var text = BormValueConverter.ToInputString(original, BormFieldType.DateTime);
        Assert.AreEqual("2024-01-15T10:30", text);
        Assert.AreEqual("2024-01-15", BormValueConverter.ToInputString(original, BormFieldType.Date));

        var unchanged = BormValueConverter.FromInputString(text, typeof(DateTimeOffset), BormFieldType.DateTime, original);
        Assert.AreEqual(original, unchanged);
        Assert.AreEqual(offset, ((DateTimeOffset)unchanged!).Offset);

        var edited = BormValueConverter.FromInputString("2024-01-15T11:00", typeof(DateTimeOffset?), BormFieldType.DateTime, original);
        Assert.AreEqual(new DateTimeOffset(2024, 1, 15, 11, 0, 0, offset), edited);
    }

    [TestMethod]
    public void A_DateTimeOffset_with_no_current_value_takes_the_local_offset_of_that_moment()
    {
        var parsed = Assert.IsExactInstanceOfType<DateTimeOffset>(
            BormValueConverter.FromInputString("2024-07-01T09:00", typeof(DateTimeOffset), BormFieldType.DateTime));

        var clock = new DateTime(2024, 7, 1, 9, 0, 0);
        Assert.AreEqual(new DateTimeOffset(clock, TimeZoneInfo.Local.GetUtcOffset(clock)), parsed);
    }

    [TestMethod]
    public void A_DateTimeOffset_stated_with_its_own_offset_is_taken_at_its_word()
    {
        var current = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(9));
        var parsed = BormValueConverter.FromInputString("2024-01-15T10:30+02:00", typeof(DateTimeOffset), BormFieldType.Text, current);

        Assert.AreEqual(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.FromHours(2)), parsed);
    }

    [TestMethod]
    public async Task Editing_a_DateTimeOffset_through_its_input_keeps_the_offset()
    {
        var model = new DvStamped { When = new DateTimeOffset(2024, 1, 15, 10, 30, 0, new TimeSpan(5, 45, 0)) };
        var form = BormBuilder.For<DvStamped>().Field(x => x.When).Build();
        var state = new BormState(form, new BormModelDataAccessor(model));
        var context = new BormFieldContext(state, form.Fields[0], "When");

        await context.SetFromStringAsync("2024-01-15T11:00");

        Assert.AreEqual(new DateTimeOffset(2024, 1, 15, 11, 0, 0, new TimeSpan(5, 45, 0)), model.When);
    }

    [TestMethod]
    public void A_DateTimeOffset_range_is_judged_on_the_values_own_clock()
    {
        // 01:00 on New Year's Day at +05:00 is still in 2024 on its own clock, whatever zone the server
        // is in — which is the date its input shows.
        var rule = new BormRangeRule(new DateTime(2024, 1, 1).ToOADate(), new DateTime(2024, 12, 31).ToOADate());

        Assert.IsTrue(Run(rule, new DateTimeOffset(2024, 1, 1, 1, 0, 0, TimeSpan.FromHours(5))).IsValid);
        Assert.IsFalse(Run(rule, new DateTimeOffset(2023, 12, 31, 23, 0, 0, TimeSpan.FromHours(-5))).IsValid);
    }

    // ---- #13: DataAnnotations mean what they mean to DataAnnotations

    [TestMethod]
    public void An_exclusive_Range_refuses_its_bounds()
    {
        var field = BormSchemaGenerator.Generate<DvAnnotated>().FindField("Ratio")!;

        Assert.IsFalse(Valid(field, 0d));
        Assert.IsTrue(Valid(field, 0.5));
        Assert.IsFalse(Valid(field, 1d));
    }

    [TestMethod]
    public void MinLength_on_a_collection_rendered_as_a_multi_select_counts_items()
    {
        var field = BormSchemaGenerator.Generate<DvAnnotated>().FindField("Days")!;

        Assert.AreEqual(BormFieldType.MultiSelect, field.Type);
        Assert.AreEqual(1, field.MinItems);
        Assert.IsFalse(Valid(field, new List<DayOfWeek>()));
        Assert.IsTrue(Valid(field, new List<DayOfWeek> { DayOfWeek.Monday }));
    }

    [TestMethod]
    public void MaxLength_on_a_file_collection_counts_files()
    {
        var field = BormSchemaGenerator.Generate<DvAnnotated>().FindField("Attachments")!;

        Assert.AreEqual(2, field.MaxItems);
        Assert.IsFalse(Valid(field, new List<IBrowserFile> { new DvFile("a"), new DvFile("b"), new DvFile("c") }));
    }

    [TestMethod]
    public void Range_text_bounds_are_read_in_the_current_culture_unless_told_otherwise()
    {
        var (price, invariant) = InCulture("de-DE", () =>
        {
            var form = BormSchemaGenerator.Generate<DvCultureBounds>();
            return (form.FindField("Price")!, form.FindField("Invariant")!);
        });

        Assert.AreEqual(0.01, price.Min);
        Assert.AreEqual(99.99, price.Max);
        Assert.AreEqual(0.01, invariant.Min);
        Assert.AreEqual(99.99, invariant.Max);
    }

    [TestMethod]
    public void Url_accepts_the_schemes_UrlAttribute_does()
    {
        var field = BormSchemaGenerator.Generate<DvAnnotated>().FindField("Site")!;

        Assert.IsTrue(Valid(field, "ftp://files.example.com/a.zip"));
        Assert.IsTrue(Valid(field, "https://example.com"));
        Assert.IsFalse(Valid(field, "mailto:someone@example.com"));
    }

    // ---- #14: no thousands separator in the invariant reading

    [TestMethod]
    public void A_decimal_comma_is_not_read_as_a_thousands_separator()
    {
        // Under a decimal-comma culture the invariant reading used to win, with the comma taken for
        // grouping. (Under en-US the culture reading still takes "1,000" as a thousand, as it should.)
        Assert.IsTrue(InCulture("de-DE", () => Run(new BormRangeRule(0, 2), "1,5").IsValid));
        Assert.IsTrue(InCulture("de-DE", () => BormConditionEvaluator.Compare("1,5", BormConditionOperator.Equals, 1.5)));
        Assert.IsFalse(InCulture("de-DE", () => BormConditionEvaluator.Compare("1,5", BormConditionOperator.Equals, 15)));
        Assert.AreEqual("1,5", BormValueConverter.FromInputString("1,5", typeof(double), BormFieldType.Number));
        Assert.AreEqual("1,5", BormValueConverter.FromInputString("1,5", null, BormFieldType.Number));
        Assert.AreEqual(1.5, BormValueConverter.FromInputString("1.5", typeof(double), BormFieldType.Number));
        Assert.AreEqual(1.5m, BormValueConverter.FromInputString("1.5", typeof(decimal), BormFieldType.Number));
    }

    // ---- #15: conditions on dates and flags

    [TestMethod]
    public void Equals_reads_condition_text_as_the_date_it_is_compared_with()
    {
        Assert.IsTrue(BormConditionEvaluator.Compare(new DateTime(2024, 1, 1), BormConditionOperator.Equals, "2024-01-01"));
        Assert.IsTrue(BormConditionEvaluator.Compare(new DateOnly(2024, 1, 1), BormConditionOperator.Equals, "2024-01-01"));
        Assert.IsFalse(BormConditionEvaluator.Compare(new DateOnly(2024, 1, 1), BormConditionOperator.NotEquals, "2024-01-01"));
        Assert.IsFalse(BormConditionEvaluator.Compare(new DateOnly(2024, 1, 2), BormConditionOperator.Equals, "2024-01-01"));
    }

    [TestMethod]
    public void Equals_keeps_its_text_and_bool_semantics()
    {
        Assert.IsTrue(BormConditionEvaluator.Compare("abc", BormConditionOperator.Equals, "ABC"));
        Assert.IsTrue(BormConditionEvaluator.Compare(true, BormConditionOperator.Equals, "true"));
        Assert.IsFalse(BormConditionEvaluator.Compare(false, BormConditionOperator.Equals, "yes"));
    }

    [TestMethod]
    public void Contains_on_a_flags_value_asks_whether_the_flag_is_set()
    {
        var both = DvPerm.Read | DvPerm.Write;

        Assert.IsTrue(BormConditionEvaluator.Compare(both, BormConditionOperator.Contains, "Write"));
        Assert.IsFalse(BormConditionEvaluator.Compare(DvPerm.Read, BormConditionOperator.Contains, "Write"));
        Assert.IsTrue(BormConditionEvaluator.Compare(DvPerm.Read, BormConditionOperator.NotContains, "Write"));
        Assert.IsTrue(BormConditionEvaluator.Compare(both, BormConditionOperator.Contains, 2L));
        Assert.IsFalse(BormConditionEvaluator.Compare(both, BormConditionOperator.Contains, "None"));
    }

    [TestMethod]
    public void A_flags_value_with_nothing_set_is_empty()
    {
        Assert.IsTrue(BormConditionEvaluator.Compare(DvPerm.None, BormConditionOperator.IsEmpty, null));
        Assert.IsFalse(BormConditionEvaluator.Compare(DvPerm.Read, BormConditionOperator.IsEmpty, null));
        Assert.IsFalse(BormConditionEvaluator.Compare(DayOfWeek.Sunday, BormConditionOperator.IsEmpty, null));
    }

    // ---- #16: Required on a flags multi-select

    [TestMethod]
    public async Task Required_on_a_flags_multi_select_needs_something_ticked()
    {
        var form = BormBuilder.For<DvFlagsModel>().Field(x => x.Perms, f => f.Required()).Build();
        var model = new DvFlagsModel();
        var state = new BormState(form, new BormModelDataAccessor(model));

        Assert.IsFalse(await state.ValidateAsync());

        model.Perms = DvPerm.Write;
        Assert.IsTrue(await state.ValidateAsync());
        Assert.IsTrue(Run(new BormRequiredRule(), DayOfWeek.Sunday).IsValid);
    }

    // ---- the pattern rule on a tag list

    [TestMethod]
    public void A_pattern_is_applied_to_every_tag()
    {
        var tags = new BormFieldDefinition("skills", BormFieldType.Tags);
        var rule = new BormPatternRule("^[a-z]+$");

        Assert.IsTrue(Run(rule, new List<string> { "csharp", "sql" }, tags).IsValid);
        Assert.IsFalse(Run(rule, new List<string> { "csharp", "C#" }, tags).IsValid);
        // A repeater's rows are not tags; the pattern has nothing to say about the list itself.
        Assert.IsTrue(Run(rule, new List<string> { "C#" }, new BormFieldDefinition("rows", BormFieldType.Array)).IsValid);
    }

    // ---- low-severity conversions and rules

    [TestMethod]
    public void An_email_with_a_trailing_newline_is_refused()
    {
        Assert.IsFalse(Run(new BormEmailRule(), "a@b.co\n").IsValid);
        Assert.IsTrue(Run(new BormEmailRule(), "a@b.co").IsValid);
    }

    [TestMethod]
    public void A_time_without_a_colon_is_not_read_as_days()
    {
        Assert.AreEqual("12", BormValueConverter.FromInputString("12", typeof(TimeSpan), BormFieldType.Time));
        Assert.AreEqual(new TimeSpan(12, 30, 0), BormValueConverter.FromInputString("12:30", typeof(TimeSpan), BormFieldType.Time));
    }

    [TestMethod]
    public void An_expression_through_Nullable_Value_binds_the_property_itself()
    {
        Assert.AreEqual("Age", BormExpressionPath.Of<DvNullable, int>(x => x.Age!.Value));

        var form = BormBuilder.For<DvNullable>().Field(x => x.Age!.Value).Build();
        Assert.AreEqual("Age", form.Fields[0].Name);
        Assert.AreEqual(typeof(int?), form.Fields[0].ValueType);
    }

    [TestMethod]
    public void An_expression_selecting_a_field_is_refused()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() => BormExpressionPath.Of<DvFieldModel, string?>(x => x.PublicField));
        Assert.Contains("PublicField", ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task IValidatableObject_is_called_once_with_property_attributes_included()
    {
        var model = new DvValidatable { Name = "ok" };
        var state = new BormState(BormSchemaGenerator.Generate<DvValidatable>(), new BormModelDataAccessor(model))
            .UseDataAnnotations(includePropertyAttributes: true);

        await state.ValidateAsync();
        Assert.AreEqual(1, model.Calls);
        Assert.Contains(m => m.Message == "Whole-model problem.", state.AllMessages);

        // An attribute failing changes nothing about how often the model's own validation runs — and
        // its complaint is still reported alongside the attribute's.
        model.Name = null;
        await state.ValidateAsync();
        Assert.AreEqual(2, model.Calls);
        Assert.Contains(m => m.Message == "Whole-model problem.", state.AllMessages);
        Assert.Contains(m => m.Message.Contains("required", StringComparison.OrdinalIgnoreCase), state.MessagesFor("Name"));
    }

    [TestMethod]
    public async Task Class_level_attributes_run_only_once_the_property_attributes_pass()
    {
        // DataAnnotations' own order. Run regardless, the class-level check below dereferenced the null
        // [Required] had just reported and threw out of the validation pass.
        var form = BormBuilder.Create().Text("Start").Build();
        var model = new DvRanged();
        var state = new BormState(form, new BormModelDataAccessor(model))
            .UseDataAnnotations(includePropertyAttributes: true);

        Assert.IsFalse(await state.ValidateAsync());
        Assert.IsNotEmpty(state.MessagesFor("Start"));
        Assert.DoesNotContain(m => m.Message == "Start is too short.", state.AllMessages);

        model.Start = "x";
        Assert.IsFalse(await state.ValidateAsync());
        Assert.Contains(m => m.Message == "Start is too short.", state.AllMessages);

        model.Start = "xy";
        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public void Compare_treats_equal_numbers_of_different_types_as_equal()
    {
        var data = new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["pin"] = 5L });

        Assert.IsTrue(Run(new BormCompareRule("pin"), 5, data: data).IsValid);
        Assert.IsFalse(Run(new BormCompareRule("pin"), 6, data: data).IsValid);
        Assert.IsFalse(Run(new BormCompareRule("pin"), "5", data: data).IsValid);
    }

    [TestMethod]
    public void Compare_reads_a_float_and_a_long_number_as_the_ones_they_were_written_as()
    {
        var data = new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["rate"] = 0.1m, ["id"] = 1000000000000001.0 });

        Assert.IsTrue(Run(new BormCompareRule("rate"), 0.1f, data: data).IsValid);
        Assert.IsTrue(Run(new BormCompareRule("id"), 1000000000000001L, data: data).IsValid);
        Assert.IsFalse(Run(new BormCompareRule("id"), 1000000000000002.0, data: data).IsValid);
    }

    [TestMethod]
    public void A_fraction_is_not_rounded_into_a_whole_number()
    {
        Assert.IsFalse(BormValueConverter.TryCoerce(2.5, typeof(int), out _));
        Assert.IsFalse(BormValueConverter.TryCoerce(3.7m, typeof(long), out _));
        Assert.IsTrue(BormValueConverter.TryCoerce(3.0, typeof(int), out var whole));
        Assert.AreEqual(3, whole);
    }

    [TestMethod]
    public void An_undeclared_number_is_not_an_enum_member()
    {
        Assert.AreEqual("999", BormValueConverter.FromInputString("999", typeof(DayOfWeek), BormFieldType.Select));
        Assert.IsFalse(BormValueConverter.TryCoerce(999L, typeof(DayOfWeek), out _));
        Assert.AreEqual(DayOfWeek.Monday, BormValueConverter.FromInputString("1", typeof(DayOfWeek), BormFieldType.Select));
        // A [Flags] value is a combination by nature, so any of its bits may be set together.
        Assert.AreEqual(DvPerm.Read | DvPerm.Write, BormValueConverter.FromInputString("3", typeof(DvPerm), BormFieldType.MultiSelect));
    }
}

/// <summary>
/// The rendered side of keeping a class-typed group or list editable when its setter is not public:
/// a read-only container now locks everything inside it, so getting the flag wrong locks the page.
/// </summary>
[TestClass]
public class DataValidationRenderingTests : ComponentTestBase
{
    [TestMethod]
    public void Fields_inside_a_group_or_list_with_a_private_setter_render_editable()
    {
        var model = new DvAggregateView();
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, BormSchemaGenerator.Generate<DvAggregateView>())
            .Add(x => x.Model, model));

        var inputs = cut.FindAll("input");
        Assert.IsNotEmpty(inputs);
        foreach (var input in inputs)
        {
            Assert.IsFalse(input.HasAttribute("readonly"), input.OuterHtml);
        }
        Assert.IsFalse(cut.Find("button.bit-borm-btn--add").HasAttribute("disabled"));
    }
}
