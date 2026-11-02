using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Borm.Tests;

public sealed class BridgePerson
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public BridgeAddress Address { get; set; } = new();
}

public sealed class BridgeAddress
{
    public string City { get; set; } = "";
}

/// <summary>A custom renderer built on Blazor's own <see cref="InputText"/>, which needs an edit context to exist at all.</summary>
public sealed class InputTextRenderer : BormInputBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<InputText>(0);
        builder.AddAttribute(1, "id", Context.ElementId);
        builder.AddAttribute(2, nameof(InputText.Value), Context.StringValue);
        builder.AddAttribute(3, nameof(InputText.ValueChanged),
            EventCallback.Factory.Create<string?>(this, value => Context.SetFromStringAsync(value)));
        builder.AddAttribute(4, nameof(InputText.ValueExpression), Context.ValueExpression<string>());
        builder.CloseComponent();
    }
}

/// <summary>
/// The Blazor edit context a form can provide: the rest of the ecosystem's inputs, messages and summaries
/// see Borm's validation as their own.
/// </summary>
[TestClass]
public class EditContextBridgeTests : ComponentTestBase
{
    private static BormDefinition Form() => BormBuilder.For<BridgePerson>()
        .Field(x => x.Name, f => f.Required().CustomRenderer("inputText"))
        .Field(x => x.Age, f => f.Range(18, 120))
        .Object(x => x.Address, a => a.Field(c => c.City, f => f.Required().CustomRenderer("inputText")))
        .Build();

    public EditContextBridgeTests() => Services.AddBorm(r => r.RegisterCustom<InputTextRenderer>("inputText"));

    [TestMethod]
    public async Task An_InputText_in_a_custom_renderer_shows_the_fields_errors_as_its_own()
    {
        var model = new BridgePerson();
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Form()).Add(x => x.Model, model).Add(x => x.ProvideEditContext, true));

        Assert.DoesNotContain("invalid", cut.Find("#borm_Name").ClassList);

        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        Assert.Contains("invalid", cut.Find("#borm_Name").ClassList);
        Assert.Contains("invalid", cut.Find("#borm_Address_City").ClassList);

        // Typing through the InputText writes through Borm, and the field's error goes from both.
        cut.Find("#borm_Name").Change("Ada");
        Assert.AreEqual("Ada", model.Name);
        Assert.DoesNotContain("invalid", cut.Find("#borm_Name").ClassList);
    }

    [TestMethod]
    public async Task A_view_moved_to_another_state_gives_its_inputs_that_states_context()
    {
        var first = new BormState(Form(), new BormModelDataAccessor(new BridgePerson()), Services);
        var cut = Render<BormView>(p => p.Add(x => x.State, first).Add(x => x.ProvideEditContext, true));

        var second = new BormState(Form(), new BormModelDataAccessor(new BridgePerson()), Services);
        cut.Render(p => p.Add(x => x.State, second));
        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        // An input still holding the first state's context — the cascade is fixed — would never hear of it.
        Assert.Contains("invalid", cut.Find("#borm_Name").ClassList);
    }

    [TestMethod]
    public async Task A_change_made_off_the_renderers_thread_reaches_the_inputs_on_it()
    {
        var state = new BormState(Form(), new BormModelDataAccessor(new BridgePerson()), Services);
        var cut = Render<BormView>(p => p.Add(x => x.State, state).Add(x => x.ProvideEditContext, true));
        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());
        Assert.Contains("invalid", cut.Find("#borm_Name").ClassList);

        // The inputs listening to the context render when it changes, and a render off the dispatcher throws.
        await Task.Run(async () =>
        {
            state.SetValue("Name", "Ada");
            await state.ValidateFieldAsync("Name");
        });

        cut.WaitForAssertion(() => Assert.DoesNotContain("invalid", cut.Find("#borm_Name").ClassList));
    }

    [TestMethod]
    public async Task A_ValidationSummary_in_the_form_lists_what_borm_found()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Form())
            .Add(x => x.Model, new BridgePerson { Age = 5 })
            .Add(x => x.ProvideEditContext, true)
            .Add(x => x.ChildContent, (RenderFragment)(b => { b.OpenComponent<ValidationSummary>(0); b.CloseComponent(); })));

        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        var listed = cut.FindAll(".validation-message").Select(m => m.TextContent).ToList();
        Assert.HasCount(3, listed);
        CollectionAssert.Contains(listed, "Must be between 18 and 120.");
    }

    [TestMethod]
    public void Asking_the_edit_context_to_validate_runs_the_forms_rules()
    {
        var model = new BridgePerson { Name = "Ada", Address = new() { City = "London" }, Age = 30 };
        var state = new BormState(Form(), new BormModelDataAccessor(model), Services);
        using var adapter = new BormEditContextAdapter(state);

        Assert.IsTrue(adapter.EditContext.Validate());

        state.SetValue("Age", 4);
        Assert.IsFalse(adapter.EditContext.Validate());
        Assert.AreEqual("Must be between 18 and 120.", adapter.EditContext.GetValidationMessages(adapter.FieldIdentifier("Age")).Single());
        // The identifier is the one Blazor works out from an expression over the same property.
        Assert.AreEqual(FieldIdentifier.Create(() => model.Age), adapter.FieldIdentifier("Age"));
        Assert.AreEqual(FieldIdentifier.Create(() => model.Address.City), adapter.FieldIdentifier("Address.City"));
    }

    [TestMethod]
    public async Task Rows_replaced_wholesale_have_their_messages_filed_under_the_new_rows()
    {
        var form = BormBuilder.For<ExpressionInvoice>()
            .Array(x => x.Lines, line => line.Field(l => l.Product, f => f.Required()))
            .Build();
        var invoice = new ExpressionInvoice { Lines = [new()] };
        var state = new BormState(form, new BormModelDataAccessor(invoice));
        using var adapter = new BormEditContextAdapter(state);
        await state.ValidateAsync();
        state.MarkAllTouched();
        var before = invoice.Lines[0];
        Assert.HasCount(1, adapter.EditContext.GetValidationMessages(new FieldIdentifier(before, "Product")));

        // The same path and the same complaint, about another object.
        state.SetValue("Lines", new List<ExpressionLine> { new() });
        await state.ValidateAsync();

        Assert.IsEmpty(adapter.EditContext.GetValidationMessages(new FieldIdentifier(before, "Product")));
        Assert.HasCount(1, adapter.EditContext.GetValidationMessages(new FieldIdentifier(invoice.Lines[0], "Product")));
    }

    [TestMethod]
    public async Task Validate_called_inside_a_rendered_form_answers_before_it_returns()
    {
        var state = new BormState(Form(), new BormModelDataAccessor(new BridgePerson()), Services);
        var cut = Render<BormView>(p => p.Add(x => x.State, state).Add(x => x.ProvideEditContext, true));
        var context = new BormFieldContext(state, state.Definition.Fields[0], "Name").EditContext!;

        // A hand-written submit button over the context does exactly this, on the renderer's thread —
        // where the view's dispatcher runs the copy inline rather than after the answer.
        var valid = true;
        await cut.InvokeAsync(() => { valid = context.Validate(); });

        Assert.IsFalse(valid);
        Assert.HasCount(3, context.GetValidationMessages());
    }

    [TestMethod]
    public void A_message_appears_in_the_context_no_sooner_than_the_field_would_show_it()
    {
        var state = new BormState(Form(), new BormModelDataAccessor(new BridgePerson()), Services);
        using var adapter = new BormEditContextAdapter(state);

        state.ValidateAsync().AsTask().Wait();
        Assert.IsEmpty(adapter.EditContext.GetValidationMessages());

        state.MarkTouched("Name");
        Assert.HasCount(1, adapter.EditContext.GetValidationMessages());
    }

    [TestMethod]
    public void A_dictionary_backed_form_identifies_a_field_by_its_container_and_key()
    {
        var form = BormBuilder.Create().Object("address", a => a.Text("city", f => f.Required())).Build();
        var data = new Dictionary<string, object?>();
        var state = new BormState(form, new BormDictionaryDataAccessor(data));
        using var adapter = new BormEditContextAdapter(state);

        var identifier = adapter.FieldIdentifier("address.city");
        Assert.AreSame(data["address"], identifier.Model);
        Assert.AreEqual("city", identifier.FieldName);

        var expression = adapter.ValueExpression<object?>("address.city");
        Assert.AreEqual(identifier, FieldIdentifier.Create(expression));
    }

    [TestMethod]
    public void An_adapter_made_by_hand_is_the_one_the_forms_fields_report_until_it_is_disposed()
    {
        var state = new BormState(Form(), new BormModelDataAccessor(new BridgePerson()), Services);
        var field = new BormFieldContext(state, state.Definition.Fields[0], "Name");

        var adapter = new BormEditContextAdapter(state);
        Assert.AreSame(adapter.EditContext, field.EditContext);
        Assert.AreEqual(adapter.FieldIdentifier("Name"), field.FieldIdentifier);

        adapter.Dispose();
        Assert.IsNull(field.EditContext);
    }

    [TestMethod]
    public void An_expression_of_the_wrong_type_is_refused_rather_than_read_wrongly()
    {
        var state = new BormState(Form(), new BormModelDataAccessor(new BridgePerson()));
        using var adapter = new BormEditContextAdapter(state);

        Assert.ThrowsExactly<InvalidOperationException>(() => adapter.ValueExpression<string>("Age"));
        Assert.IsNotNull(adapter.ValueExpression<int>("Age"));
    }

    [TestMethod]
    public void Without_it_the_form_provides_no_edit_context_and_disposing_the_view_detaches_it()
    {
        var plain = Render<BormView>(p => p.Add(x => x.Definition, BormBuilder.Create().Text("a").Build()));
        Assert.IsNull(new BormFieldContext(plain.Instance.Form, plain.Instance.Form.Definition.Fields[0], "a").EditContext);

        var state = new BormState(Form(), new BormModelDataAccessor(new BridgePerson()), Services);
        var cut = Render<BormView>(p => p.Add(x => x.State, state).Add(x => x.ProvideEditContext, true));
        Assert.IsNotNull(new BormFieldContext(state, state.Definition.Fields[0], "Name").EditContext);

        cut.Render(p => p.Add(x => x.ProvideEditContext, false));
        Assert.IsNull(new BormFieldContext(state, state.Definition.Fields[0], "Name").EditContext);
    }
}
