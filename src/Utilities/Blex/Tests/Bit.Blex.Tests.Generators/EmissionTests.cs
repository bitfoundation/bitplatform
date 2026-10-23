using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests.Generators;

[TestClass]
public class EmissionTests
{
    private const string Usings = "using System.Threading; using System.Threading.Tasks; using Bit.Blex;\n";

    [TestMethod]
    public void HappyPath_CompilesWithoutErrors()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;

            [BlexStoreAttribute(Name = "counter", Persist = true)]
            public partial class CounterStore
            {
                [BlexStateAttribute] private int _count;
                [BlexStateAttribute] private string? _label;

                [BlexComputedAttribute] private int ComputeDouble() => Count * 2;

                [BlexActionAttribute] private void OnIncrement() => Count++;
                [BlexActionAttribute] private void OnAdd(int amount, string? note = null) => Count += amount;

                [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Latest)]
                private async Task OnLoad(int id, CancellationToken ct) => await Task.Delay(1, ct);

                [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Queue)]
                private async Task OnSave() => await Task.Yield();
            }
            """);

        Assert.IsFalse(result.HasGeneratorError);
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void RollbackOnError_SnapshotsEveryStateField()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;

            public enum Mode { Off, On }

            [BlexStoreAttribute(Name = "tx", RollbackOnError = true)]
            public partial class TxStore
            {
                [BlexStateAttribute] private int _count;
                [BlexStateAttribute] private int? _maybe;
                [BlexStateAttribute] private string? _label;
                [BlexStateAttribute] private Mode _mode;
                [BlexStateAttribute] private System.Collections.Generic.List<string> @lock = new();

                [BlexActionAttribute] private void OnIncrement() => Count++;
            }

            [BlexStoreAttribute(Name = "plain")]
            public partial class PlainStore
            {
                [BlexStateAttribute] private int _count;
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
        var tx = result.GeneratedSources.Single(s => s.Key.Contains("TxStore")).Value;
        Assert.Contains("protected override object?[]? CaptureRollbackState() => new object?[] { this._count, this._maybe, this._label, this._mode, this.@lock };", tx);
        Assert.Contains("this._maybe = (int?)state[1]!;", tx);
        Assert.Contains("this.@lock = (global::System.Collections.Generic.List<string>)state[4]!;", tx);
        Assert.DoesNotContain("CaptureRollbackState", result.GeneratedSources.Single(s => s.Key.Contains("PlainStore")).Value);
    }

    [TestMethod]
    public void A_write_invalidates_only_the_computeds_that_read_it()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            using System.Linq;

            namespace App;

            public interface IRates { decimal Tax { get; } }

            public static class TrackedExtensions
            {
                public static int Weighted(this TrackedStore store) => store.A * 3;
            }

            [BlexStoreAttribute(Name = "tracked")]
            public partial class TrackedStore(IRates rates)
            {
                [BlexStateAttribute] private int _a;
                [BlexStateAttribute] private int _b;
                [BlexStateAttribute] private string _label = "";
                [BlexStateAttribute] private System.Collections.Generic.List<int> _items = new();
                private readonly int _offset = 1;

                [BlexComputedAttribute] private int ComputeSum() => A + _b;                                   // property and backing field
                [BlexComputedAttribute] private int ComputeDoubleA() => this.A * 2;
                [BlexComputedAttribute] private int ComputeChain() => DoubleA + 1;                           // through another computed
                [BlexComputedAttribute] private int ComputeTotal() => Items.Where(i => i > 0).Sum(i => i * 2); // members off a property
                [BlexComputedAttribute] private int ComputeHelped() => Helper();                             // a helper: reads everything
                [BlexComputedAttribute] private int ComputeOffset() => A + _offset;                          // a plain field: everything
                [BlexComputedAttribute] private decimal ComputeTaxed() => B * rates.Tax;                     // a primary-constructor parameter
                [BlexComputedAttribute] private string ComputeShout() => System.String.Concat(Label, "!");   // a static call
                [BlexComputedAttribute] private int ComputeExtended() => this.Weighted();                   // an extension on the store
                [BlexComputedAttribute] private int ComputeBuilt() => new Box { Value = B }.Value;          // another object's member

                private int Helper() => _b;
            }

            public sealed class Box { public int Value { get; set; } }
            """);

        Assert.IsEmpty(result.CompileErrors());
        var source = result.GeneratedSources.Single(s => s.Key.Contains("TrackedStore")).Value;

        string[] everything = ["Helped", "Offset", "Taxed", "Extended", "Built"];
        Assert.AreSequenceEqual([.. everything.Prepend("Sum").Prepend("DoubleA").Prepend("Chain").Order()], [.. Invalidated(source, "A").Order()]);
        Assert.AreSequenceEqual([.. everything.Append("Sum").Order()], [.. Invalidated(source, "B").Order()]);
        Assert.AreSequenceEqual([.. everything.Append("Shout").Order()], [.. Invalidated(source, "Label").Order()]);
        Assert.AreSequenceEqual([.. everything.Append("Total").Order()], [.. Invalidated(source, "Items").Order()]);
        Assert.Contains("default:", source);
    }

    [TestMethod]
    public void A_member_a_computed_only_writes_is_not_a_dependency()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;

            [BlexStoreAttribute(Name = "cart")]
            public partial class CartStore
            {
                [BlexStateAttribute] private int _lines;
                [BlexStateAttribute] private int _discount;

                public int SubtotalRuns { get; private set; }
                private int _totalRuns;
                private int _last;

                [BlexComputedAttribute] private int ComputeSubtotal() { SubtotalRuns++; return Lines * 10; }
                [BlexComputedAttribute] private int ComputeTotal() { ++this._totalRuns; _last = Subtotal; _totalRuns += 0; return _last - Discount; }
                [BlexComputedAttribute] private int ComputeEcho() { _last = Lines; return _last; }   // read back: that read counts
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
        var source = result.SingleGenerated;

        // A write to the discount re-runs only the total; the evaluation counters are not reads.
        Assert.AreSequenceEqual(["Echo", "Total"], [.. Invalidated(source, "Discount").Order()]);
        Assert.AreSequenceEqual(["Echo", "Subtotal", "Total"], [.. Invalidated(source, "Lines").Order()]);
    }

    // The computeds a "case" of the generated InvalidateComputed(string?) switch invalidates.
    private static IEnumerable<string> Invalidated(string source, string property)
    {
        var start = source.IndexOf($"case \"{property}\":", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"no case for {property}");
        var end = source.IndexOf("break;", start, StringComparison.Ordinal);
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(source[start..end], @"__(\w+)Valid = false;"))
            yield return match.Groups[1].Value;
    }

    [TestMethod]
    public void DeserializeState_reads_every_field_before_it_assigns_any()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private string _accent = "violet";
                [BlexStateAttribute] private int _density;
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
        var generated = result.SingleGenerated;
        var start = generated.IndexOf("public override void DeserializeState(", StringComparison.Ordinal);
        var body = generated[start..generated.IndexOf("\n    }", start, StringComparison.Ordinal)];

        // A payload whose second field fails must not leave the first one applied on its own.
        var lastRead = body.LastIndexOf("Deserialize<", StringComparison.Ordinal);
        var firstAssign = body.IndexOf("this._accent = ", StringComparison.Ordinal);
        Assert.IsGreaterThan(lastRead, firstAssign, "fields are assigned only after every one was read");
        Assert.Contains("this._density = __v1;", body);
    }

    [TestMethod]
    public void Cancelling_a_debounced_effect_also_cancels_the_invocation_still_waiting()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexEffectAttribute(DebounceMilliseconds = 300)]
                private async Task OnTypeAhead(string q, CancellationToken ct) => await Task.Delay(1, ct);

                [BlexEffectAttribute(DebounceMilliseconds = 300)]
                private async Task OnPing() => await Task.Yield();
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
        var generated = result.SingleGenerated;

        var cancel = generated[generated.IndexOf("public void CancelTypeAhead()", StringComparison.Ordinal)..];
        cancel = cancel[..cancel.IndexOf('}')];
        Assert.Contains("__TypeAheadDebounce?.Cancel();", cancel);
        Assert.Contains("__TypeAheadCts?.Cancel();", cancel);

        // No token to cancel a run with, but a quiet period still worth cancelling.
        Assert.Contains("public void CancelPing() => __PingDebounce?.Cancel();", generated);
    }

    [TestMethod]
    public void NullableAnnotations_ArePreserved()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S { [BlexStateAttribute] private string? _userName; }
            """);

        Assert.Contains("public string? UserName", result.SingleGenerated);
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void DefaultParameterValues_ArePropagatedToWrapper()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexActionAttribute] private void OnSet(int value, bool log = true, string tag = "none") => X = value;
            }
            """);

        var generated = result.SingleGenerated;
        Assert.Contains("bool log = true", generated);
        Assert.Contains("string tag = \"none\"", generated);
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void CancellationTokenParameter_IsHiddenFromPublicWrapper_AndCancelMethodEmitted()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexEffectAttribute] private async Task OnLoad(int id, CancellationToken ct) => await Task.Delay(1, ct);
            }
            """);

        var generated = result.SingleGenerated;
        Assert.Contains("public async global::System.Threading.Tasks.Task Load(int id)", generated);
        Assert.Contains("public void CancelLoad()", generated);
        Assert.DoesNotContain("Load(int id, ", generated);
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void SameClassName_InDifferentNamespaces_GetsDistinctHintNames()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace A { [BlexStoreAttribute] public partial class CartStore { [BlexStateAttribute] private int _n; } }
            namespace B { [BlexStoreAttribute] public partial class CartStore { [BlexStateAttribute] private int _n; } }
            """);

        Assert.AreEqual(2, result.GeneratedSources.Count);
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void ActionLabelWithQuotes_IsEscaped()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute(Name = "the \"weird\" store")]
            public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexActionAttribute(Name = "say \"hi\"")] private void OnGreet() => X++;
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void GlobalNamespaceStore_Compiles()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class GlobalStore { [BlexStateAttribute] private int _x; }
            """);

        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void JsonNullRestore_AssignsDefault()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S { [BlexStateAttribute] private string? _text; }
            """);

        // Present-but-null must restore to default!, only a missing property is skipped.
        Assert.Contains("is null ? default!", result.SingleGenerated);
    }

    [TestMethod]
    public void NullableEnumDefaultParameter_Compiles()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            public enum Mode { A = 0, B = 1 }
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexActionAttribute] private void OnSet(Mode? mode = Mode.B, int? count = 5, bool? flag = null) => X++;
            }
            """);

        Assert.IsFalse(result.HasGeneratorError);
        Assert.IsEmpty(result.CompileErrors());
        Assert.Contains("(global::App.Mode)(1)", result.SingleGenerated);
    }

    [TestMethod]
    public void KeywordActionName_IsTreatedAsLabelOnly()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexActionAttribute(Name = "lock")] private void OnLock() => X++;
            }
            """);

        // "lock" is a reserved keyword: it must become the display label, not the method name.
        Assert.Contains("public void Lock()", result.SingleGenerated);
        Assert.DoesNotContain("public void lock(", result.SingleGenerated);
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void ControlCharactersInName_AreEscaped()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute(Name = "line\nbreak\ttab")]
            public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexActionAttribute(Name = "do\nthing")] private void OnGo() => X++;
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void DuplicateMembers_SuppressEmission_InsteadOfEmittingBrokenCode()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _value;
                [BlexComputedAttribute] private int ComputeValue() => 1;
            }
            """);

        Assert.IsTrue(result.HasDiagnostic("BLEX005"));
        // No generated source: emitting would just bury the user in CS0102 cascades.
        Assert.IsEmpty(result.GeneratedSources);
    }

    [TestMethod]
    public void UserFieldCollidingWithGeneratedBackingField_ReportsBLEX005()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexComputedAttribute] private int ComputeFoo() => X;
                private bool __FooValid; // collides with the memoization backing field
            }
            """);

        Assert.IsTrue(result.HasDiagnostic("BLEX005"));
        Assert.IsEmpty(result.GeneratedSources);
    }

    [TestMethod]
    public void KeywordParameterNames_AreEscaped()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexActionAttribute] private void OnDoThing(int @lock, string @class) => X = @lock + @class.Length;
                [BlexEffectAttribute] private async Task OnLoad(int @event, CancellationToken ct) { await Task.Delay(1, ct); X = @event; }
            }
            """);

        Assert.Contains("int @lock", result.SingleGenerated);
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void FieldsNamedLikeGeneratedLocals_BindCorrectly()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private string? state;   // collides with DeserializeState's parameter
                [BlexStateAttribute] private int value;       // collides with the setter's implicit parameter
            }
            """);

        // this.-qualification must bind the assignments to the fields, not the parameters.
        Assert.Contains("this.state", result.SingleGenerated);
        Assert.Contains("this.value", result.SingleGenerated);
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void UserParameterNamed__ct_DoesNotCollideWithWrapperLocals()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Latest)]
                private async Task OnLoad(int __ct, CancellationToken ct) { await Task.Delay(1, ct); X = __ct; }
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void ParamsParameter_IsPreserved()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _total;
                [BlexActionAttribute] private void OnAddAll(params int[] values) { foreach (var v in values) Total += v; }
            }
            """);

        Assert.Contains("params int[] values", result.SingleGenerated);
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("Concurrency = BlexEffectConcurrency.Latest")]
    [DataRow("Concurrency = BlexEffectConcurrency.Drop")]
    [DataRow("Concurrency = BlexEffectConcurrency.Queue")]
    public void EffectWrapper_TakesTheVetoDecisionBeforeTouchingAnyLifecycleState(string concurrency)
    {
        var attribute = concurrency.Length == 0 ? "[BlexEffectAttribute]" : $"[BlexEffectAttribute({concurrency})]";
        var result = GeneratorTestHelper.Run(Usings + $$"""
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                {{attribute}}
                private async Task OnLoad(int id, CancellationToken ct) { await Task.Delay(1, ct); X = id; }
            }
            """);

        Assert.IsEmpty(result.CompileErrors());

        // Scope the search to the wrapper body: CancelLoad() mentions the token source too, and it
        // is emitted before the wrapper.
        var source = result.SingleGenerated;
        var wrapperStart = source.IndexOf("public async global::System.Threading.Tasks.Task Load(", StringComparison.Ordinal);
        Assert.IsTrue(wrapperStart > 0, "the effect wrapper should have been emitted");
        var wrapper = source.Substring(wrapperStart);

        // A veto means the action never happened, so the decision has to precede every observable
        // side effect: the loading counter, the cleared error, the cancelled predecessor and the
        // queue slot. Guard the ordering, not just the presence of the call.
        var veto = wrapper.IndexOf("if (!BeginAction(", StringComparison.Ordinal);
        Assert.IsTrue(veto > 0, "the wrapper must consult the pipeline before running");

        foreach (var sideEffect in new[] { "BeginEffect(ref", "SetEffectState(ref __LoadError, null)", "__LoadCts?.Cancel()", "__LoadQueue = " })
        {
            var index = wrapper.IndexOf(sideEffect, StringComparison.Ordinal);
            if (index >= 0)
                Assert.IsTrue(veto < index, $"'{sideEffect}' must come after the veto check");
        }

        // ...and the body itself must not re-consult the pipeline.
        Assert.Contains("DispatchApprovedAsync(", wrapper);
        Assert.DoesNotContain("await DispatchAsync(", wrapper);
    }

    [TestMethod]
    public void CancellableEffects_AreCancelledOnDisposal()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;

            [BlexStoreAttribute] public partial class S
            {
                [BlexEffectAttribute]
                private async Task OnLoad(CancellationToken ct) => await Task.Delay(1, ct);

                [BlexEffectAttribute(DebounceMilliseconds = 250)]
                private async Task OnSearch(string q) => await Task.Yield();
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
        var generated = result.SingleGenerated;

        Assert.Contains("protected override void CancelEffects()", generated);
        Assert.Contains("__LoadCts?.Cancel();", generated);
        Assert.Contains("__SearchDebounce?.Cancel();", generated);

        // The sources are owned by the frame that created them and disposed in its own finally;
        // disposing them from here would race that frame's cancellation catch.
        var start = generated.IndexOf("protected override void CancelEffects()", StringComparison.Ordinal);
        var body = generated.Substring(start, generated.IndexOf("}", generated.IndexOf("base.CancelEffects();", StringComparison.Ordinal), StringComparison.Ordinal) - start);
        Assert.DoesNotContain(".Dispose()", body);
    }

    [TestMethod]
    public void AStoreWithNothingToCancel_EmitsNoCancelOverride()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _count;
                [BlexEffectAttribute] private async Task OnLoad() => await Task.Yield();
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
        Assert.DoesNotContain("CancelEffects", result.SingleGenerated);
    }
}
