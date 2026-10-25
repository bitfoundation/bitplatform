using System.Text;

namespace Bit.Borm.Benchmarks;

/// <summary>
/// The forms the suites measure. Each is shaped like something real rather than like a micro-case:
/// a long flat form with ordinary rules on it, the same form with conditions and a computed total
/// watching it, and an invoice whose lines each compute their own total.
/// </summary>
public static class Workloads
{
    /// <summary>
    /// <paramref name="fields"/> text fields bound to a dictionary, every one required and length
    /// limited, the way a long questionnaire or a back-office record looks.
    /// </summary>
    public static BormDefinition FlatForm(int fields)
    {
        var builder = BormBuilder.Create().Title("Flat");
        for (var i = 0; i < fields; i++)
            builder.Text($"f{i}", f => f.Label($"Field {i}").Required().MaxLength(80));
        return builder.Build();
    }

    /// <summary>
    /// The flat form plus the reactive machinery a large real form carries: a toggle that every tenth
    /// field is visible behind (and cleared when hidden), and a number field per ten whose sum a
    /// computed total keeps up to date.
    /// </summary>
    public static BormDefinition ReactiveForm(int fields)
    {
        var builder = BormBuilder.Create().Title("Reactive");
        builder.Checkbox("toggle");
        var numbers = new List<string>();
        for (var i = 0; i < fields; i++)
        {
            if (i % 10 == 0)
            {
                var name = $"n{i}";
                numbers.Add(name);
                builder.Number(name, f => f.Range(0, 1000));
            }
            else if (i % 10 == 5)
            {
                builder.Text($"f{i}", f => f
                    .VisibleWhen("toggle", BormConditionOperator.IsTrue)
                    .ClearOnHide());
            }
            else
            {
                builder.Text($"f{i}", f => f.Required());
            }
        }

        var deps = numbers.ToArray();
        builder.Number("total", f => f.ReadOnly().Computed(ctx =>
        {
            double sum = 0;
            foreach (var n in deps)
                if (ctx.Value(n) is double d) sum += d;
            return sum;
        }, deps));
        return builder.Build();
    }

    /// <summary>A dictionary holding a value for every field of <see cref="FlatForm"/>.</summary>
    public static Dictionary<string, object?> FlatData(int fields)
    {
        var data = new Dictionary<string, object?>();
        for (var i = 0; i < fields; i++) data[$"f{i}"] = $"value {i}";
        return data;
    }

    public static BormDefinition InvoiceForm()
        => BormBuilder.For<Invoice>()
            .Field(x => x.Customer, f => f.Required())
            .Array(x => x.Lines, line => line
                .Field(l => l.Product, f => f.Required())
                .Field(l => l.Quantity, f => f.Range(1, 999))
                .Field(l => l.UnitPrice, f => f.Range(0, 100_000))
                .Computed(l => l.LineTotal, l => l.Quantity * l.UnitPrice,
                    dependsOn: [nameof(InvoiceLine.Quantity), nameof(InvoiceLine.UnitPrice)]))
            .Computed(x => x.Total, m => m.Lines.Sum(l => l.Quantity * l.UnitPrice),
                dependsOn: [nameof(Invoice.Lines)])
            .Build();

    public static Invoice InvoiceModel(int lines)
    {
        var invoice = new Invoice { Customer = "Contoso" };
        for (var i = 0; i < lines; i++)
            invoice.Lines.Add(new InvoiceLine { Product = $"P{i}", Quantity = 1 + i % 5, UnitPrice = 10 + i });
        return invoice;
    }

    /// <summary>A JSON Schema document with <paramref name="properties"/> properties of mixed kinds.</summary>
    public static string JsonSchema(int properties)
    {
        var sb = new StringBuilder("{\"type\":\"object\",\"required\":[\"p0\"],\"properties\":{");
        for (var i = 0; i < properties; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append($"\"p{i}\":");
            sb.Append((i % 4) switch
            {
                0 => "{\"type\":\"string\",\"minLength\":1,\"maxLength\":50,\"title\":\"Text\"}",
                1 => "{\"type\":\"integer\",\"minimum\":0,\"maximum\":10}",
                2 => "{\"type\":\"string\",\"enum\":[\"a\",\"b\",\"c\"]}",
                _ => "{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"string\",\"format\":\"email\"},\"y\":{\"type\":\"boolean\"}}}"
            });
        }
        sb.Append("}}");
        return sb.ToString();
    }
}

public sealed class Invoice
{
    public string Customer { get; set; } = "";
    public List<InvoiceLine> Lines { get; set; } = [];
    public decimal Total { get; set; }
}

public sealed class InvoiceLine
{
    public string Product { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
