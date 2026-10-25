using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Borm.Tests;

public sealed class StaticPerson
{
    public string Name { get; set; } = "";
    public bool Subscribe { get; set; }
    public string Plan { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public StaticAddress Address { get; set; } = new();
    public List<StaticLine> Lines { get; set; } = [];
}

public sealed class StaticAddress
{
    public string City { get; set; } = "";
}

public sealed class StaticLine
{
    public string Product { get; set; } = "";
}

/// <summary>
/// A named form, posted like any HTML form when it is rendered with no interactivity: every control under the name
/// model binding reads, and a form that needs a click to move on shown whole instead.
/// </summary>
[TestClass]
public class StaticRenderingTests : ComponentTestBase
{
    private sealed class Antiforgery : AntiforgeryStateProvider
    {
        public override AntiforgeryRequestToken? GetAntiforgeryToken() => new("token-value", "__RequestVerificationToken");
    }

    public StaticRenderingTests() => Services.AddSingleton<AntiforgeryStateProvider, Antiforgery>();

    private static BormDefinition Form(bool wizard = false)
    {
        var builder = BormBuilder.For<StaticPerson>()
            .Field(x => x.Name, f => f.Required())
            .Field(x => x.Subscribe)
            .Field(x => x.Plan, f => f.AsRadio().Options(("solo", "Solo"), ("team", "Team")))
            .Field(x => x.Tags, f => f.AsTags())
            .Object(x => x.Address, a => a.Field(c => c.City))
            .Array(x => x.Lines, l => l.Field(x => x.Product), f => f.PageSize(1));
        if (wizard)
            builder.Step("you", s => s.Title("You").Fields("Name", "Subscribe", "Plan"))
                   .Step("more", s => s.Title("More").Fields("Tags", "Address", "Lines"));
        return builder.Build();
    }

    private static StaticPerson Person() => new()
    {
        Name = "Ada", Tags = ["red", "blue"], Lines = [new() { Product = "A" }, new() { Product = "B" }]
    };

    [TestMethod]
    public void A_named_form_posts_each_control_under_the_name_model_binding_reads()
    {
#if NET9_0_OR_GREATER
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
#endif
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Form()).Add(x => x.Model, Person()).Add(x => x.FormName, "signup"));

        var form = cut.Find("form");
        Assert.AreEqual("post", form.GetAttribute("method"));
        Assert.AreEqual("token-value", cut.Find("input[name=__RequestVerificationToken]").GetAttribute("value"));
        Assert.AreEqual("Model.Name", cut.Find("#borm_Name").GetAttribute("name"));
        Assert.AreEqual("Model.Address.City", cut.Find("#borm_Address_City").GetAttribute("name"));
        Assert.AreEqual("Model.Lines[0].Product", cut.Find("#borm_Lines_0_Product").GetAttribute("name"));
#if NET9_0_OR_GREATER
        // Rendered statically every row is there, past the page size of one.
        Assert.AreEqual("Model.Lines[1].Product", cut.Find("#borm_Lines_1_Product").GetAttribute("name"));
#endif
        // A ticked box posts "true"; an unticked one posts nothing, which binds as false.
        Assert.AreEqual("true", cut.Find("#borm_Subscribe").GetAttribute("value"));
        Assert.IsTrue(cut.FindAll("#borm_Plan input[type=radio]").All(r => r.GetAttribute("name") == "Model.Plan"));
        CollectionAssert.AreEqual(new[] { "red", "blue" }, cut.FindAll("input[type=hidden][name^='Model.Tags[']").Select(i => i.GetAttribute("value")).ToArray());
    }

    [TestMethod]
    public void A_read_only_choice_and_a_signature_post_their_answers_though_their_controls_cannot()
    {
#if NET9_0_OR_GREATER
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
#endif
        var signature = BormSignature.FromTypedName("Ada")!;
        var form = BormBuilder.Create()
            .Select("Plan", f => f.ReadOnly().Options(("solo", "Solo"), ("team", "Team")))
            .Radio("Size", f => f.ReadOnly().Options(("s", "S"), ("m", "M")))
            .Checkbox("Agree", f => f.ReadOnly())
            .MultiSelect("Days", f => f.ReadOnly().Options(("mon", "Mon"), ("tue", "Tue"), ("wed", "Wed")))
            .Number("Volume", f => f.ReadOnly().AsRange())
            .Text("Sign", f => f.AsSignature())
            .Select("Locked", f => f.ReadOnly().DisabledWhen(_ => true).Options(("x", "X")))
            .Build();
        var data = new Dictionary<string, object?>
        {
            ["Plan"] = "team", ["Size"] = "m", ["Agree"] = true, ["Days"] = new List<string> { "mon", "wed" },
            ["Volume"] = 7, ["Sign"] = signature, ["Locked"] = "x"
        };
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data).Add(x => x.FormName, "plan"));
        string?[] Posted(string name) => cut.FindAll($"input[type=hidden][name='Model.{name}']").Select(i => i.GetAttribute("value")).ToArray();

        // Drawn disabled - there is no read-only select - and a disabled control is never posted.
        Assert.IsTrue(cut.Find("#borm_Plan").HasAttribute("disabled"));
        CollectionAssert.AreEqual(new[] { "team" }, Posted("Plan"));
        CollectionAssert.AreEqual(new[] { "m" }, Posted("Size"));
        CollectionAssert.AreEqual(new[] { "true" }, Posted("Agree"));
        CollectionAssert.AreEqual(new[] { "mon", "wed" }, Posted("Days"));
        CollectionAssert.AreEqual(new[] { "7" }, Posted("Volume"));
        CollectionAssert.AreEqual(new[] { signature }, Posted("Sign"));
        // A disabled field is not part of what the form sends, read-only or not.
        Assert.IsEmpty(Posted("Locked"));
    }

    [TestMethod]
    public void A_form_that_is_not_named_posts_nothing_and_names_no_control()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Form()).Add(x => x.Model, Person()));

        Assert.IsNull(cut.Find("form").GetAttribute("method"));
        Assert.IsEmpty(cut.FindAll("input[name=__RequestVerificationToken]"));
        Assert.IsNull(cut.Find("#borm_Name").GetAttribute("name"));
    }

#if NET9_0_OR_GREATER
    // .NET 8 cannot tell a component how it is rendered, so there a named form keeps the shape it has interactive.
    [TestMethod]
    public void Rendered_statically_a_wizard_is_shown_whole_and_a_list_as_it_stands()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Form(wizard: true)).Add(x => x.Model, Person()).Add(x => x.FormName, "signup"));

        // Every step, as sections: there is no Next to press.
        Assert.HasCount(2, cut.FindAll(".bit-borm-section"));
        Assert.IsEmpty(cut.FindAll("button").Where(b => b.TextContent.Trim() == "Next"));
        // Every row, even past its page size, and no button that needs a click to do anything.
        Assert.HasCount(2, cut.FindAll(".bit-borm-array__item"));
        Assert.IsEmpty(cut.FindAll(".bit-borm-array__pager, .bit-borm-array__item-actions, .bit-borm-btn--add"));
    }
#endif

    [TestMethod]
    public void Interactive_the_same_named_form_is_the_wizard_it_was()
    {
#if NET9_0_OR_GREATER
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
#endif
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Form(wizard: true)).Add(x => x.Model, Person()).Add(x => x.FormName, "signup"));

        Assert.IsNotEmpty(cut.FindAll("button").Where(b => b.TextContent.Trim() == "Next"));
        Assert.IsEmpty(cut.FindAll(".bit-borm-section"));
    }

    [TestMethod]
    public async Task A_combobox_posts_the_label_it_showed_and_the_form_turns_it_into_the_value()
    {
        var form = BormBuilder.Create()
            .Combobox("country", f => f.Options(("nl", "Netherlands"), ("de", "Germany")))
            .Build();
        var data = new Dictionary<string, object?> { ["country"] = "germany" };
        var state = new BormState(form, new BormDictionaryDataAccessor(data));

        await state.ResolvePostedChoicesAsync();

        Assert.AreEqual("de", data["country"]);
    }
}
