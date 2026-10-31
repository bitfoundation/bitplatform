using Bit.Blex.Demo.Client;
using Bit.Blex.Demo.Server.Dtos;
using Bit.Blex.Demo.Server.Services;

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
    public async Task A_store_whose_computeds_multiply_past_the_budget_is_refused()
    {
        var side = (int)Math.Sqrt(BlexStoreInspector.MaxStateComputedPairs) + 1;
        var states = string.Concat(Enumerable.Range(0, side).Select(i => $"    [BlexState] private int _s{i};\n"));
        var computeds = string.Concat(Enumerable.Range(0, side).Select(i => $"    [BlexComputed] private int ComputeC{i}() => S0;\n"));

        var result = await InspectAsync($"[BlexStore]\npublic partial class WideStore\n{{\n{states}{computeds}}}");

        AssertRefused(result, "the stores in this source would make the generator emit more code");
    }

    [TestMethod]
    public async Task A_store_with_many_states_and_one_computed_stays_within_the_budget()
    {
        var side = (int)Math.Sqrt(BlexStoreInspector.MaxStateComputedPairs) + 1;
        var states = string.Concat(Enumerable.Range(0, side).Select(i => $"    [BlexState] private int _s{i};\n"));

        var result = await InspectAsync($"[BlexStore]\npublic partial class TallStore\n{{\n{states}    [BlexComputed] private int ComputeTotal() => S0;\n}}");

        Assert.IsTrue(result.IsValid, string.Join("; ", result.Diagnostics.Select(d => d.Message)));

        var store = result.Stores.Single();
        Assert.AreEqual("TallStore", store.Name);
        Assert.IsTrue(store.Generated);
    }

    [TestMethod]
    public async Task A_store_whose_type_names_multiply_past_the_budget_is_refused()
    {
        // Short enough for one type, printed once per member until the sum is past the budget.
        var ns = string.Join('.', Enumerable.Repeat(new string('N', 500), 10));
        var states = string.Concat(Enumerable.Range(0, BlexStoreInspector.MaxTypeNameLength / ns.Length + 1).Select(i => $"    [BlexState] private int _s{i};\n"));

        var result = await InspectAsync($"namespace {ns};\n\n[BlexStore]\npublic partial class DeepStore\n{{\n{states}}}");

        AssertRefused(result, "the stores in this source would make the generator emit more code");
    }

    [TestMethod]
    public async Task A_type_doubled_by_nested_aliases_is_refused_before_anything_prints_it()
    {
        // Each alias is a tuple of the one before: sixteen of them print hundreds of times longer than they are
        // written, and every level more doubles that.
        const int depth = 16;
        var aliases = string.Concat(Enumerable.Range(1, depth).Select(i => $"namespace N{i} {{ using A{i} = System.Tuple<A{i - 1}, A{i - 1}>;\n"));

        var result = await InspectAsync($$"""
            using A0 = System.Tuple<int, int>;
            {{aliases}}
            [BlexStore]
            public partial class AliasStore
            {
                [BlexState] private A{{depth}}? _value;
            }
            {{new string('}', depth)}}
            """);

        AssertRefused(result, "a type in this source prints longer than");
    }

    [TestMethod]
    public async Task A_type_doubled_by_generic_inference_is_refused_before_a_compiler_error_prints_it()
    {
        const int depth = 16;
        var call = Enumerable.Range(0, depth).Aggregate("1", (inner, _) => $"Twice({inner})");

        var result = await InspectAsync($$"""
            [BlexStore]
            public partial class InferredStore
            {
                [BlexState] private int _count;

                private static (T, T) Twice<T>(T value) => (value, value);

                [BlexAction] private void OnReset() => Count = {{call}};
            }
            """);

        AssertRefused(result, "a type in this source prints longer than");
    }

    [TestMethod]
    public async Task A_source_that_keeps_the_compiler_busy_is_stopped_at_the_deadline_and_the_server_carries_on()
    {
        // Forty nested calls: Roslyn's own inference over a type that doubles each time runs for years, inside a call
        // that never looks at a cancellation token. Only killing the worker it runs in ends it.
        var call = Enumerable.Range(0, 40).Aggregate("1", (inner, _) => $"Twice({inner})");

        var result = await InspectAsync($$"""
            [BlexStore]
            public partial class EndlessStore
            {
                [BlexState] private int _count;

                private static (T, T) Twice<T>(T value) => (value, value);

                [BlexAction] private void OnReset() => Count = {{call}};
            }
            """);

        AssertRefused(result, "the inspection did not finish within");
        Assert.IsTrue((await InspectAsync(Counter)).IsValid, "the next inspection runs as usual");
    }

    [TestMethod]
    public async Task A_constant_doubled_past_the_source_length_is_refused_before_it_is_folded()
    {
        // Twenty-four doublings of two characters: 32 million once folded, from about a thousand characters.
        var constants = string.Concat(Enumerable.Range(1, 24).Select(i => $"    private const string C{i} = C{i - 1} + C{i - 1};\n"));

        var result = await InspectAsync($$"""
            [BlexStore]
            public partial class LabelStore
            {
                private const string C0 = "ab";
            {{constants}}
                [BlexState] private string _label = "";

                [BlexAction] private void OnRename(string label = C24) => Label = label;
            }
            """);

        AssertRefused(result, "a constant in this source is longer than");
    }

    [TestMethod]
    public async Task A_store_name_doubled_through_interpolation_is_refused_too()
    {
        var constants = string.Concat(Enumerable.Range(1, 24).Select(i => $"    public const string C{i} = $\"{{C{i - 1}}}{{C{i - 1}}}\";\n"));

        var result = await InspectAsync($$"""
            public static class Names
            {
                public const string C0 = "ab";
            {{constants}}
            }

            [BlexStore(Name = Names.C24)]
            public partial class NamedStore
            {
                [BlexState] private int _count;
            }
            """);

        AssertRefused(result, "a constant in this source is longer than");
    }

    [TestMethod]
    public async Task Constants_built_from_one_another_within_the_source_length_are_inspected()
    {
        var result = await InspectAsync("""
            [BlexStore]
            public partial class TodoStore
            {
                private const string Prefix = "todos";
                private const string Added = Prefix + "/add";

                [BlexState] private string _last = "";

                [BlexAction(Name = Added)] private void OnAdd(string title = $"{Prefix}: {Added}") => Last = title;
            }
            """);

        Assert.IsTrue(result.IsValid, string.Join("; ", result.Diagnostics.Select(d => d.Message).Concat(result.Notes)));
        Assert.IsTrue(result.Stores.Single().Generated);
    }

    [TestMethod]
    public async Task Past_the_cap_the_diagnostics_are_counted_rather_than_listed()
    {
        var count = BlexStoreInspector.MaxReportedDiagnostics + 50;
        var broken = string.Concat(Enumerable.Range(0, count).Select(i => $"        int x{i} = \"text\";\n"));

        var result = await InspectAsync($$"""
            [BlexStore]
            public partial class BrokenStore
            {
                [BlexState] private int _count;

                private void Broken()
                {
            {{broken}}
                }
            }
            """);

        Assert.IsFalse(result.IsValid);
        Assert.HasCount(BlexStoreInspector.MaxReportedDiagnostics, result.Diagnostics);
        Assert.Contains(note => note.StartsWith($"Only the first {BlexStoreInspector.MaxReportedDiagnostics} of {count} diagnostics", StringComparison.Ordinal), result.Notes);
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

    /// <summary>A refusal is an answer, not an error: nothing inspected, and the reason as the last note.</summary>
    private static void AssertRefused(BlexStoreInspectionDto result, string reason)
    {
        Assert.IsFalse(result.IsValid);
        Assert.IsEmpty(result.Stores);
        StringAssert.StartsWith(result.Notes[^1], "REFUSED: " + reason);
    }
}
