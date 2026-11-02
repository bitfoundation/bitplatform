using BenchmarkDotNet.Attributes;

namespace Bit.Borm.Benchmarks;

/// <summary>
/// The expression language on its hot paths: parsing (once per schema), evaluating a condition (once per
/// field per render), and a computed total over every row of a list (once per change to the list).
/// </summary>
[MemoryDiagnoser]
public class ExpressionBenchmarks
{
    private BormExpression _condition = null!;
    private BormExpression _sum = null!;
    private BormFieldCondition _operatorCondition = null!;
    private BormDictionaryDataAccessor _data = null!;
    private BormModelDataAccessor _invoice = null!;

    private const string ConditionText = "{IsBusiness} and {Employees} > 10 and {Country} in ['NL', 'BE', 'DE']";

    [GlobalSetup]
    public void Setup()
    {
        _condition = BormExpression.Parse(ConditionText);
        _sum = BormExpression.Parse("sum({Lines[].LineTotal})");
        _operatorCondition = new BormFieldCondition("Employees", BormConditionOperator.GreaterThan, 10);
        _data = new BormDictionaryDataAccessor(new Dictionary<string, object?>
        {
            ["IsBusiness"] = true, ["Employees"] = 42L, ["Country"] = "NL"
        });
        var invoice = Workloads.InvoiceModel(200);
        foreach (var line in invoice.Lines) line.LineTotal = line.Quantity * line.UnitPrice;
        _invoice = new BormModelDataAccessor(invoice);
    }

    [Benchmark(Description = "Parse a three-clause condition")]
    public BormExpression Parse() => BormExpression.Parse(ConditionText);

    [Benchmark(Baseline = true, Description = "Evaluate an operator condition (for comparison)")]
    public bool EvaluateOperator() => _operatorCondition.Evaluate(_data);

    [Benchmark(Description = "Evaluate a three-clause expression condition")]
    public bool EvaluateExpression() => _condition.IsTrue(_data);

    [Benchmark(Description = "sum({Lines[].LineTotal}) over 200 rows")]
    public object? SumRows() => _sum.Evaluate(_invoice);
}
