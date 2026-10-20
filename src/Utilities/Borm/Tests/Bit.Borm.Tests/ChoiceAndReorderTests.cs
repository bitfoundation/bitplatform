using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// Two options whose values differ only in characters that are not id-safe fold to the same id. When
/// they do, one <c>&lt;label for&gt;</c> points at the other's control — so clicking a label ticks the
/// wrong box, and nothing about the page looks broken.
/// </summary>
[TestClass]
public class OptionIdCollisionTests : ComponentTestBase
{
    private static BormDefinition Choices(BormFieldType type) => BormBuilder.Create()
        .Field("locale", type, f => f.Options(("en-US", "American"), ("en_US", "Also American"), ("en US", "Third")))
        .Build();

    [TestMethod]
    [DataRow(BormFieldType.Radio)]
    [DataRow(BormFieldType.MultiSelect)]
    public void Every_option_gets_an_id_of_its_own(BormFieldType type)
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Choices(type)));

        var ids = cut.FindAll("input[id]").Select(i => i.GetAttribute("id")).ToList();

        Assert.AreEqual(3, ids.Count);
        Assert.AreEqual(3, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    [DataRow(BormFieldType.Radio)]
    [DataRow(BormFieldType.MultiSelect)]
    public void Every_label_points_at_its_own_control(BormFieldType type)
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Choices(type)));

        foreach (var label in cut.FindAll("label[for]"))
        {
            var target = cut.Find($"#{label.GetAttribute("for")}");
            Assert.AreEqual(label.TextContent.Trim(), cut.Find($"label[for='{target.GetAttribute("id")}']").TextContent.Trim());
        }
    }
}

/// <summary>
/// A button that disables itself as a result of being pressed is a focus trap in reverse: the browser
/// drops focus on the body. The repeater's reorder arrows stay focusable and guard the click instead.
/// </summary>
[TestClass]
public class ReorderButtonTests : ComponentTestBase
{
    private static Bunit.IRenderedComponent<BormView> RenderRows(BunitContext ctx, int rows)
    {
        var model = new RegistrationModel();
        for (var i = 0; i < rows; i++) model.Items.Add(new LineItem { Product = $"P{i}" });

        var form = BormBuilder.Create().Array("Items", item => item.Text("Product")).Build();
        return ctx.Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Model, model));
    }

    [TestMethod]
    public void The_arrow_at_the_end_of_the_list_stays_in_the_tab_order()
    {
        var cut = RenderRows(this, rows: 2);
        var up = cut.FindAll("button[aria-label*='up']")[0];

        Assert.AreEqual("true", up.GetAttribute("aria-disabled"));
        Assert.IsFalse(up.HasAttribute("disabled"));
    }

    [TestMethod]
    public void Pressing_it_anyway_does_nothing()
    {
        var cut = RenderRows(this, rows: 2);
        var model = (RegistrationModel)cut.Instance.Form.Data.Root!;
        var before = model.Items.Select(i => i.Product).ToList();

        cut.FindAll("button[aria-label*='up']")[0].Click();

        Assert.AreSequenceEqual(before, model.Items.Select(i => i.Product));
    }

    [TestMethod]
    public void A_real_move_still_reorders_and_rebinds()
    {
        var cut = RenderRows(this, rows: 2);
        var model = (RegistrationModel)cut.Instance.Form.Data.Root!;

        cut.FindAll("button[aria-label*='down']")[0].Click();

        Assert.AreSequenceEqual(["P1", "P0"], model.Items.Select(i => i.Product));
        Assert.AreEqual("P1", cut.Find("input#borm_Items_0_Product").GetAttribute("value"));
    }
}

[TestClass]
public class OptionRoundTripTests
{
    [TestMethod]
    public void Option_groups_and_disabled_flags_survive_a_JSON_round_trip()
    {
        var form = BormBuilder.Create()
            .Select("country", f => f.Options(
                new BormSelectOption("fr", "France", Group: "Europe"),
                new BormSelectOption("de", "Germany", Group: "Europe"),
                new BormSelectOption("jp", "Japan", Disabled: true, Group: "Asia")))
            .Build();

        var reimported = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(form));
        var options = reimported.FindField("country")!.Options;

        Assert.AreSequenceEqual(["Europe", "Europe", "Asia"], options.Select(o => o.Group));
        Assert.AreSequenceEqual(["fr", "de", "jp"], options.Select(o => o.Value));
        Assert.AreSequenceEqual([false, false, true], options.Select(o => o.Disabled));
    }

    [TestMethod]
    public void A_plain_option_list_exports_nothing_extra()
    {
        var form = BormBuilder.Create().Select("t", f => f.Options(("a", "A"))).Build();

        var json = BormJsonSchemaExporter.Export(form);

        Assert.DoesNotContain("x-enumGroups", json, StringComparison.Ordinal);
        Assert.DoesNotContain("x-enumDisabled", json, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_grouped_select_renders_optgroups()
    {
        var form = BormBuilder.Create()
            .Select("country", f => f.Options(
                new BormSelectOption("fr", "France", Group: "Europe"),
                new BormSelectOption("jp", "Japan", Group: "Asia")))
            .Build();

        using var ctx = new ComponentTestHost();
        var cut = ctx.Render<BormView>(p => p.Add(x => x.Definition, form));

        Assert.AreSequenceEqual(["Europe", "Asia"], cut.FindAll("optgroup").Select(g => g.GetAttribute("label")));
    }
}

/// <summary>A bUnit host usable from a test class that is not itself a BunitContext.</summary>
public sealed class ComponentTestHost : BunitContext
{
    public ComponentTestHost() => Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions
        .AddSingleton<IBormFieldRendererRegistry>(Services, new BormFieldRendererRegistry());
}

[TestClass]
public class DoubleSourcedOptionsDiagnosticTests
{
    [TestMethod]
    public void A_field_with_both_a_list_and_a_provider_is_reported()
    {
        // The provider wins, so the static list is never shown — but it is still in the schema, and
        // reading it is the natural way to be wrong about what the field offers.
        var form = BormBuilder.Create()
            .Select("city", f => f
                .Options(("a", "A"))
                .OptionsFrom(_ => new ValueTask<IReadOnlyList<BormSelectOption>>(Array.Empty<BormSelectOption>())))
            .Build();

        Assert.Contains(d => d.Message.Contains("the provider wins", StringComparison.Ordinal), form.Validate());
    }
}
