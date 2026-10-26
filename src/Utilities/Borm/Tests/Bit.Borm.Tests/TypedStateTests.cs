namespace Bit.Borm.Tests;

/// <summary>The state addressed by expression, so a page's reads and writes are refactor-safe.</summary>
[TestClass]
public class TypedStateTests
{
    private static (BormState State, ExpressionInvoice Model) Invoice()
    {
        var model = new ExpressionInvoice
        {
            Customer = "Contoso",
            Lines = [new() { Product = "A", Quantity = 1, UnitPrice = 2m }, new() { Product = "B", Quantity = 4, UnitPrice = 1m }]
        };
        var form = BormBuilder.For<ExpressionInvoice>()
            .Field(x => x.Customer, f => f.Required())
            .Array(x => x.Lines, l => l
                .Field(i => i.Product)
                .Field(i => i.Quantity, f => f.Range(1, 10))
                .Field(i => i.UnitPrice)
                .Computed(i => i.LineTotal, "{Quantity} * {UnitPrice}"))
            .Computed(x => x.Total, "sum({Lines[].LineTotal})")
            .Build();
        return (new BormState(form, new BormModelDataAccessor(model)), model);
    }

    [TestMethod]
    public void Values_are_read_and_written_through_the_same_expressions_the_model_uses()
    {
        var (state, model) = Invoice();
        var form = state.For<ExpressionInvoice>();
        var row = 1;

        form.SetValue(x => x.Lines[row].Quantity, 5);

        Assert.AreEqual(5, model.Lines[1].Quantity);
        Assert.AreEqual(7m, form.GetValue(x => x.Total));
        Assert.IsTrue(form.IsDirty(x => x.Lines[row].Quantity));
        Assert.IsTrue(form.IsTouched(x => x.Lines[1].Quantity));
        Assert.AreEqual("Lines[1].Quantity", form.Path(x => x.Lines[row].Quantity));
    }

    [TestMethod]
    public async Task Validation_messages_and_server_errors_are_addressed_the_same_way()
    {
        var (state, _) = Invoice();
        var form = state.For<ExpressionInvoice>();

        form.SetValue(x => x.Lines[0].Quantity, 50);
        // True: the selector names a field of the schema, which was validated.
        Assert.IsTrue(await form.ValidateFieldAsync(x => x.Lines[0].Quantity));
        Assert.IsTrue(form.GetFieldState(x => x.Lines[0].Quantity).IsInvalid);

        form.SetServerError(x => x.Customer, "On hold.");
        Assert.AreEqual("On hold.", form.MessagesFor(x => x.Customer).Single().Message);

        form.ResetField(x => x.Lines[0].Quantity);
        Assert.AreEqual(1, form.GetValue(x => x.Lines[0].Quantity));
    }

    [TestMethod]
    public void An_index_from_an_array_or_a_computed_expression_works_too()
    {
        int[] rows = [0, 1];
        Assert.AreEqual("Lines[1].Product", BormExpressionPath.OfValue((ExpressionInvoice x) => x.Lines[rows[1]].Product));
        Assert.AreEqual("Lines[0].Lots[2]", BormExpressionPath.OfValue((ExpressionInvoice x) => x.Lines[0].Lots[1 + 1]));
        Assert.AreEqual("Issued", BormExpressionPath.OfValue((ExpressionInvoice x) => x.Issued!.Value));
    }

    [TestMethod]
    public void A_selector_that_is_not_a_path_or_a_model_of_another_type_is_refused()
    {
        var (state, _) = Invoice();
        var form = state.For<ExpressionInvoice>();
        var negative = -1;

        Assert.ThrowsExactly<ArgumentException>(() => form.Path(x => x.Customer.Length > 2));
        Assert.ThrowsExactly<ArgumentException>(() => form.Path(x => x.Lines[negative].Product));
        Assert.ThrowsExactly<ArgumentException>(() => state.For<RegistrationModel>());

        // A dictionary-backed form has no model type to disagree with.
        var loose = new BormState(BormBuilder.Create().Text("Customer").Build(), new BormDictionaryDataAccessor());
        loose.For<ExpressionInvoice>().SetValue(x => x.Customer, "Fabrikam");
        Assert.AreEqual("Fabrikam", loose.GetValue("Customer"));
    }
}
