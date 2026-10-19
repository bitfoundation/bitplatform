using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Borm.Tests;

/// <summary>
/// A renderer that records where the form sent focus. bUnit has no real caret, so the contract under
/// test is which control the repeater asks for — which is the part that was missing.
/// </summary>
public sealed class FocusSpyInput : BormInputBase
{
    public static readonly List<string> Focused = [];

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddAttribute(1, "id", Context.ElementId);
        builder.AddAttribute(2, "value", Context.StringValue);
        builder.CloseElement();
    }

    public override ValueTask<bool> FocusAsync()
    {
        Focused.Add(Context.Path);
        return ValueTask.FromResult(true);
    }
}

/// <summary>
/// Pressing a button that removes the element containing it leaves focus on <c>&lt;body&gt;</c>: a
/// keyboard user is silently returned to the top of the page and a screen-reader user loses their
/// place. Every row operation has to say where focus goes next.
/// </summary>
[TestClass]
public class RepeaterFocusTests : BunitContext
{
    public RepeaterFocusTests()
    {
        FocusSpyInput.Focused.Clear();
        Services.AddBorm(r => r.Register(BormFieldType.Text, typeof(FocusSpyInput)));
    }

    private (Bunit.IRenderedComponent<BormView> View, RegistrationModel Model) RenderRepeater(int rows)
    {
        var model = new RegistrationModel();
        for (var i = 0; i < rows; i++) model.Items.Add(new LineItem { Product = $"P{i}" });

        var form = BormBuilder.Create()
            .Array("Items", item => item.Text("Product"))
            .Build();

        var view = Render<BormView>(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.Model, model));
        FocusSpyInput.Focused.Clear();
        return (view, model);
    }

    [TestMethod]
    public void Adding_a_row_puts_the_caret_in_it()
    {
        var (view, model) = RenderRepeater(rows: 1);

        view.Find("button.bit-borm-btn--add").Click();

        Assert.AreEqual(2, model.Items.Count);
        Assert.AreEqual("Items[1].Product", Assert.ContainsSingle(FocusSpyInput.Focused));
    }

    [TestMethod]
    public void Removing_a_row_moves_the_caret_to_the_row_that_took_its_place()
    {
        var (view, model) = RenderRepeater(rows: 3);

        view.FindAll("button.bit-borm-btn--danger")[0].Click();

        Assert.AreEqual(2, model.Items.Count);
        Assert.AreEqual("Items[0].Product", Assert.ContainsSingle(FocusSpyInput.Focused));
    }

    [TestMethod]
    public void Removing_the_last_row_moves_the_caret_back_up_the_list()
    {
        var (view, model) = RenderRepeater(rows: 3);

        view.FindAll("button.bit-borm-btn--danger")[2].Click();

        Assert.AreEqual(2, model.Items.Count);
        Assert.AreEqual("Items[1].Product", Assert.ContainsSingle(FocusSpyInput.Focused));
    }

    [TestMethod]
    public void Emptying_the_list_leaves_the_caret_on_the_button_that_refills_it()
    {
        var (view, model) = RenderRepeater(rows: 1);

        view.Find("button.bit-borm-btn--danger").Click();

        // Nothing is left to focus but the add button, which the component holds a reference to.
        Assert.IsEmpty(model.Items);
        Assert.IsEmpty(FocusSpyInput.Focused);
        Assert.IsNotNull(view.Find("button.bit-borm-btn--add"));
    }

    [TestMethod]
    public void The_rows_below_a_deletion_are_rebound_to_their_new_indices()
    {
        // The render-skip optimisation compares what a field *displays*, which does not include the
        // path it displays it from. A row Blazor reuses at a new index therefore looked unchanged, the
        // render that would apply the new path was suppressed, and the surviving rows carried on
        // rendering — and writing to — the indices they used to have. The last row bound to an element
        // that no longer existed.
        var (view, model) = RenderRepeater(rows: 3);

        view.FindAll("button.bit-borm-btn--danger")[0].Click();

        Assert.AreEqual(2, model.Items.Count);
        Assert.IsNotNull(view.Find("input#borm_Items_0_Product"));
        Assert.IsNotNull(view.Find("input#borm_Items_1_Product"));
        Assert.IsEmpty(view.FindAll("input#borm_Items_2_Product"));
    }

    [TestMethod]
    public void And_so_are_the_rows_below_an_insertion()
    {
        var model = new RegistrationModel { Items = { new LineItem { Product = "Widget" } } };
        var form = BormBuilder.Create().Array("Items", item => item.Text("Product")).Build();
        var state = new BormState(form, new BormModelDataAccessor(model));
        var view = Render<BormView>(p => p.Add(x => x.State, state));

        state.InsertArrayItem(form.FindField("Items")!, "Items", 0);

        Assert.AreEqual(2, model.Items.Count);
        Assert.IsNotNull(view.Find("input#borm_Items_1_Product"));
    }

    [TestMethod]
    public void Values_follow_their_row_after_a_deletion()
    {
        var (view, model) = RenderRepeater(rows: 3);

        view.FindAll("button.bit-borm-btn--danger")[0].Click();

        // The rows that moved up must show their own data, not the data of the index they inherited.
        Assert.AreEqual("P1", view.Find("input#borm_Items_0_Product").GetAttribute("value"));
        Assert.AreEqual("P2", view.Find("input#borm_Items_1_Product").GetAttribute("value"));
        Assert.AreSequenceEqual(["P1", "P2"], model.Items.Select(i => i.Product));
    }

    [TestMethod]
    public void Duplicating_a_row_puts_the_caret_in_the_copy()
    {
        var model = new RegistrationModel { Items = { new LineItem { Product = "Widget" } } };
        var form = BormBuilder.Create()
            .Array("Items", item => item.Text("Product"), f => f.Attr("duplicable", true))
            .Build();

        var view = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Model, model));
        FocusSpyInput.Focused.Clear();

        view.Find("button.bit-borm-btn--icon[aria-label*='Duplicate']").Click();

        Assert.AreEqual(2, model.Items.Count);
        Assert.AreEqual("Items[1].Product", Assert.ContainsSingle(FocusSpyInput.Focused));
    }
}
