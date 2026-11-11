using System.Collections.Immutable;
using Bit.BlazeDb.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bit.BlazeDb.Tests.Mcp;

/// <summary>
/// The generator's incremental pipeline compares its output by value, so an edit that does not change a table's
/// generated source is not emitted again. This project references Roslyn through the demo server, which is why the
/// test lives here rather than with the engine's.
/// </summary>
[TestClass]
public class GeneratorCachingTests
{
    private const string Table = """
        using Bit.BlazeDb;
        namespace App;
        [BlazeDbTable("notes")]
        public partial class Note
        {
            [BlazeDbKey] public int Id { get; set; }
            [BlazeDbIndex] public string Title { get; set; } = "";
        }
        """;

    private static ImmutableArray<MetadataReference> References()
    {
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "";
        return tpa.Split(Path.PathSeparator)
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(BlazeDbDatabase).Assembly.Location))
            .ToImmutableArray();
    }

    [TestMethod]
    public void An_Edit_Elsewhere_Leaves_A_Table_s_Output_Cached()
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var table = CSharpSyntaxTree.ParseText(Table, parse, "Note.cs");
        var other = CSharpSyntaxTree.ParseText("namespace App; public class Other { }", parse, "Other.cs");
        var compilation = CSharpCompilation.Create("Caching", [table, other], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new BlazeDbTableGenerator().AsSourceGenerator()],
            parseOptions: parse,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        Assert.IsEmpty(driver.GetRunResult().Diagnostics);

        // An unrelated file changes; the table's file does not.
        var edited = compilation.ReplaceSyntaxTree(other,
            CSharpSyntaxTree.ParseText("namespace App; public class Other { public int Changed { get; set; } }", parse, "Other.cs"));
        driver = driver.RunGenerators(edited);

        var outputs = driver.GetRunResult().Results.Single().TrackedOutputSteps
            .SelectMany(step => step.Value)
            .SelectMany(run => run.Outputs)
            .ToList();
        Assert.IsNotEmpty(outputs);
        CollectionAssert.AreEquivalent(
            outputs.Select(_ => IncrementalStepRunReason.Cached).ToList(),
            outputs.Select(output => output.Reason).ToList());
    }
}
