using BenchmarkDotNet.Attributes;

namespace Bit.Borm.Benchmarks;

/// <summary>
/// Turning a schema into a form and back: a server-owned JSON Schema document imported on every page
/// load, and the export a schema editor runs on every edit.
/// </summary>
[MemoryDiagnoser]
public class SchemaBenchmarks
{
    [Params(20, 400)]
    public int Properties { get; set; }

    private string _json = null!;
    private BormDefinition _definition = null!;

    [GlobalSetup]
    public void Setup()
    {
        _json = Workloads.JsonSchema(Properties);
        _definition = BormJsonSchemaImporter.Import(_json);
    }

    [Benchmark(Description = "JSON Schema import")]
    public int Import() => BormJsonSchemaImporter.Import(_json).Fields.Count;

    [Benchmark(Description = "JSON Schema export")]
    public int Export() => BormJsonSchemaExporter.Export(_definition).Length;

    [Benchmark(Description = "Definition.Validate()")]
    public int Diagnose() => _definition.Validate().Count;
}
