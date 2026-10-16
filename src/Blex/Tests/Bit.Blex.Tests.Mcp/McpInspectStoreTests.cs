using Bit.Blex.Demo.Client;
using Bit.Blex.Demo.Server.Dtos;

namespace Bit.Blex.Tests.Mcp;

/// <summary>
/// InspectBlexStore: the real generator's verdict on a piece of store source.
/// <para>
/// The oracle test at the bottom is the one that matters most: every rejected sample on the diagnostics
/// page is put to the generator, which has to report exactly the diagnostic the page says it does - and
/// every fixed sample has to come back clean. No expected answer is written down there; the docs are
/// the claim and the generator is the judge.
/// </para>
/// </summary>
[TestClass]
public class McpInspectStoreTests
{
    private const string Counter = """
        [BlexStore(Name = "counter")]
        public partial class CounterStore
        {
            [BlexState] private int _count;
            [BlexComputed] private int ComputeDouble() => Count * 2;
            [BlexAction] private void OnIncrement() => Count++;
            [BlexEffect] private async Task OnLoad(int id, CancellationToken ct) => await Task.Delay(id, ct);
        }
        """;

    [TestMethod]
    public async Task A_valid_store_reports_the_members_the_generator_adds()
    {
        var result = await InspectAsync(Counter);

        Assert.IsTrue(result.IsValid, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.AreEqual(0, result.Diagnostics.Length);

        var store = result.Stores.Single();
        Assert.AreEqual("CounterStore", store.Name);
        Assert.IsTrue(store.Generated);

        var signatures = store.Members.Select(member => member.Signature).ToArray();

        CollectionAssert.Contains(signatures, "int Count { get; set; }");
        CollectionAssert.Contains(signatures, "int Double { get; }");
        CollectionAssert.Contains(signatures, "void Increment()");
        // The CancellationToken is supplied by the wrapper, so it is not on the public method.
        CollectionAssert.Contains(signatures, "Task Load(int id)");
        CollectionAssert.Contains(signatures, "void CancelLoad()");
        CollectionAssert.Contains(signatures, "bool LoadIsLoading { get; }");
    }

    [TestMethod]
    public async Task A_rejected_store_reports_the_diagnostic_on_its_line()
    {
        var result = await InspectAsync("""
            using Bit.Blex;

            [BlexStore]
            public class CounterStore
            {
                [BlexState] private int _count;
            }
            """);

        Assert.IsFalse(result.IsValid);

        var diagnostic = result.Diagnostics.Single(d => d.Id == "BLEX001");
        Assert.AreEqual("generator", diagnostic.Source);
        Assert.AreEqual("Error", diagnostic.Severity);
        Assert.AreEqual(4, diagnostic.Line, "The diagnostic does not point at the class declaration.");

        Assert.IsFalse(result.Stores.Single().Generated);
        Assert.IsTrue(result.Notes.Any(note => note.Contains("was rejected", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_warning_leaves_the_store_valid()
    {
        var result = await InspectAsync("""
            [BlexStore]
            public partial class SearchStore
            {
                [BlexState] private string _query = "";
                [BlexEffect(Concurrency = BlexEffectConcurrency.Latest)]
                private async Task OnSearch(string text) => await Task.Delay(1);
            }
            """);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual("Warning", result.Diagnostics.Single(d => d.Id == "BLEX009").Severity);
    }

    [TestMethod]
    public async Task Broken_csharp_around_a_store_is_reported_by_the_compiler()
    {
        var result = await InspectAsync("""
            [BlexStore]
            public partial class CounterStore
            {
                [BlexState] private int _count;
                [BlexAction] private void OnIncrement() => Count += Missing;
            }
            """);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Diagnostics.Any(d => d.Source == "compiler" && d.Id == "CS0103"));
        Assert.IsTrue(result.Stores.Single().Generated, "A store the generator accepted is reported as rejected.");
    }

    [TestMethod]
    public async Task Source_without_a_store_says_so()
    {
        var result = await InspectAsync("public class NotAStore { }");

        Assert.AreEqual(0, result.Stores.Length);
        Assert.IsTrue(result.Notes.Any(note => note.Contains("No class marked [BlexStore]", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task The_generated_code_comes_back_only_when_asked_for()
    {
        Assert.IsNull((await InspectAsync(Counter)).GeneratedCode);

        var withCode = await McpCall.StructuredAsync<BlexStoreInspectionDto>("InspectBlexStore",
            new() { ["source"] = Counter, ["includeGeneratedCode"] = true });

        StringAssert.Contains(withCode.GeneratedCode!, "partial class CounterStore : global::Bit.Blex.BlexStoreBase");
    }

    [TestMethod]
    public async Task An_oversized_paste_is_cut_and_says_so()
    {
        var result = await InspectAsync(Counter + new string(' ', 60_000));

        StringAssert.StartsWith(result.Notes[0], "INCOMPLETE ANALYSIS");
    }

    [TestMethod]
    public async Task Every_documented_diagnostic_sample_is_judged_by_the_generator_as_the_docs_say()
    {
        var judged = 0;

        foreach (var doc in DiagnosticsCatalog.All)
        {
            if (string.IsNullOrWhiteSpace(doc.Bad)) continue;

            var bad = await InspectAsync(AsStore(doc.Bad));

            Assert.IsTrue(bad.Diagnostics.Any(d => d.Id == doc.Code),
                $"The docs' rejected sample for {doc.Code} is not reported as {doc.Code} by the generator: " +
                $"{string.Join("; ", bad.Diagnostics.Select(d => $"{d.Id} {d.Message}"))}");

            if (string.IsNullOrWhiteSpace(doc.Good) is false)
            {
                // Only the diagnostic being fixed is held against the fix: some samples show two alternative
                // fixes side by side, which together collide on a generated name (BLEX005) - each on its own
                // would not.
                var good = await InspectAsync(AsStore(doc.Good));

                Assert.IsFalse(good.Diagnostics.Any(d => d.Id == doc.Code),
                    $"The docs' fix for {doc.Code} is still reported as {doc.Code} by the generator: {string.Join("; ", good.Diagnostics.Select(d => $"{d.Id} {d.Message}"))}");
            }

            judged++;
        }

        Assert.IsTrue(judged >= 15, $"Only {judged} documented samples were judged.");
    }

    /// <summary>
    /// A docs sample is either a whole store or the members of one; the members are put in a store so
    /// the generator has something to judge them as part of.
    /// </summary>
    private static string AsStore(string sample)
    {
        if (sample.Contains("class ", StringComparison.Ordinal) || sample.Contains("record ", StringComparison.Ordinal)) return sample;

        return $$"""
            [BlexStore]
            public partial class SampleStore
            {
                [BlexState] private int _count;

            {{sample}}
            }
            """;
    }

    private static Task<BlexStoreInspectionDto> InspectAsync(string source)
        => McpCall.StructuredAsync<BlexStoreInspectionDto>("InspectBlexStore", new() { ["source"] = source });
}
