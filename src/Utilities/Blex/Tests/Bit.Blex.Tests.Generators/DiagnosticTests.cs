using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests.Generators;

[TestClass]
public class DiagnosticTests
{
    private const string Usings = "using System.Threading; using System.Threading.Tasks; using Bit.Blex;\n";

    [TestMethod]
    public void NotPartial_ReportsBLEX001()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public class S { [BlexStateAttribute] private int _x; }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX001"));
    }

    [TestMethod]
    public void ActionWithoutOnPrefix_ReportsBLEX002()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S { [BlexActionAttribute] private void Increment() { } }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX002"));
    }

    [TestMethod]
    public void ComputedWithBadName_ReportsBLEX003()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S { [BlexComputedAttribute] private int Twice() => 2; }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX003"));
    }

    [TestMethod]
    public void ComputedWithParameters_ReportsBLEX004()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S { [BlexComputedAttribute] private int ComputeX(int y) => y; }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX004"));
    }

    [TestMethod]
    public void CollidingGeneratedNames_ReportsBLEX005()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _value;
                [BlexComputedAttribute] private int ComputeValue() => 1;
            }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX005"));
    }

    [TestMethod]
    public void CollisionWithUserMember_ReportsBLEX005()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _count;
                public int Count => 42; // user already declared what the generator would emit
            }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX005"));
    }

    [TestMethod]
    public void CollisionWithReservedName_ReportsBLEX005()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S { [BlexStateAttribute] private string _name; }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX005")); // Name is a BlexStoreBase member
    }

    [TestMethod]
    [DataRow("Persist = true, Version = 2", "public override int PersistVersion => 3;", "PersistVersion")]
    [DataRow("Persist = true", "public override bool Persist => true;", "Persist")]
    [DataRow("EnforceActions = true", "protected override bool EnforcesActions => true;", "EnforcesActions")]
    [DataRow("", "public override string Name => \"s\";", "Name")]
    [DataRow("", "public override System.Text.Json.Nodes.JsonObject SerializeState() => new();", "SerializeState")]
    [DataRow("", "[BlexStateAttribute(Persist = false)] private int _y; protected override System.Collections.Generic.IReadOnlyCollection<string> TransientStateKeys => [];", "TransientStateKeys")]
    public void OwnMemberNamedLikeAGeneratedOverride_ReportsBLEX026_OnThatMember(string arguments, string member, string name)
    {
        // The generator overrides these too: without BLEX026 the build would fail inside generated code.
        var source = Usings + $$"""
            [BlexStoreAttribute({{arguments}})] public partial class S
            {
                [BlexStateAttribute] private int _x;
                {{member}}
            }
            """;
        var result = GeneratorTestHelper.Run(source);

        var diagnostic = result.GeneratorDiagnostics.Single(d => d.Id == "BLEX026");
        Assert.AreEqual(name, source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
        StringAssert.Contains(diagnostic.GetMessage(), $"'{name}'");
        Assert.IsEmpty(result.GeneratedSources);
    }

    [TestMethod]
    public void OwnMemberNamedLikeAnyGeneratedOverride_ReportsBLEX026()
    {
        // Every override the generator writes, read off a store that has them all: one Emit starts writing
        // without listing it in GeneratedOverrides fails here, not in a user's build inside generated code.
        const string Arguments = "Persist = true, Version = 2, EnforceActions = true, RollbackOnError = true";
        const string Members = """
            [BlexStateAttribute] private int _x;
            [BlexStateAttribute(Persist = false)] private int _y;
            [BlexComputedAttribute] private int ComputeTwice() => X * 2;
            [BlexEffectAttribute] private Task OnLoad(CancellationToken ct) => Task.CompletedTask;
            """;

        var all = GeneratorTestHelper.Run(Usings + $$"""[BlexStoreAttribute({{Arguments}})] public partial class S { {{Members}} }""");
        var overrides = CSharpSyntaxTree.ParseText(all.SingleGenerated).GetRoot().DescendantNodes()
            .OfType<MemberDeclarationSyntax>()
            .Where(member => member.Modifiers.Any(SyntaxKind.OverrideKeyword))
            .Select(member => member switch
            {
                MethodDeclarationSyntax method => (Name: method.Identifier.ValueText, Declaration: $"{method.Modifiers} {method.ReturnType} {method.Identifier}{method.ParameterList} => throw null!;"),
                PropertyDeclarationSyntax property => (Name: property.Identifier.ValueText, Declaration: $"{property.Modifiers} {property.Type} {property.Identifier} => throw null!;"),
                _ => throw new InvalidOperationException($"Unexpected generated override: {member}"),
            })
            .ToList();

        Assert.HasCount(13, overrides); // InvalidateComputed twice: both overloads

        foreach (var (name, declaration) in overrides)
        {
            var result = GeneratorTestHelper.Run(Usings + $$"""[BlexStoreAttribute({{Arguments}})] public partial class S { {{Members}} {{declaration}} }""");

            Assert.IsTrue(result.GeneratorDiagnostics.Any(d => d.Id == "BLEX026" && d.GetMessage().Contains($"'{name}'")), $"A store's own {declaration} is not reported.");
            Assert.IsEmpty(result.GeneratedSources, $"Code was generated next to a store's own {declaration}.");
        }
    }

    [TestMethod]
    public void OwnOverloadOfAGeneratedOverride_IsFine()
    {
        // The generated InvalidateComputed() and InvalidateComputed(string?) leave an overload of the store's own alone.
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexComputedAttribute] private int ComputeTwice() => X * 2;
                private void InvalidateComputed(int unused) { }
            }
            """);
        Assert.IsFalse(result.HasDiagnostic("BLEX026"));
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void OwnPersistVersion_WithoutAStoreVersion_IsFine()
    {
        // The generator overrides PersistVersion only for [BlexStore(Version = n)]; a store can still write its own.
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute(Persist = true)] public partial class S
            {
                [BlexStateAttribute] private int _x;
                public override int PersistVersion => 3;
            }
            """);
        Assert.IsFalse(result.HasDiagnostic("BLEX005"));
        Assert.IsFalse(result.HasDiagnostic("BLEX026"));
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void NestedStore_ReportsBLEX006()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            public class Outer { [BlexStoreAttribute] public partial class S { [BlexStateAttribute] private int _x; } }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX006"));
    }

    [TestMethod]
    public void SyncEffect_ReportsBLEX007()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S { [BlexEffectAttribute] private void OnLoad() { } }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX007"));
    }

    [TestMethod]
    public void StaticStateField_ReportsBLEX008()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S { [BlexStateAttribute] private static int _x; }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX008"));
    }

    [TestMethod]
    public void ReadonlyStateField_ReportsBLEX008()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S { [BlexStateAttribute] private readonly int _x; }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX008"));
    }

    [TestMethod]
    public void LatestEffectWithoutToken_ReportsBLEX009Warning()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Latest)]
                private async Task OnSearch(string q) => await Task.Yield();
            }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX009"));
        Assert.IsFalse(result.HasGeneratorError); // warning only; wrapper still generated
    }

    [TestMethod]
    public void AsyncVoidAction_ReportsBLEX010()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S { [BlexActionAttribute] private async void OnFire() => await Task.Yield(); }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX010"));
    }

    [TestMethod]
    public void ValueReturningAction_ReportsBLEX011Warning()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S { [BlexActionAttribute] private int OnCalc() => 42; }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX011"));
        Assert.IsFalse(result.HasGeneratorError);
    }

    [TestMethod]
    public void StateFieldWithoutPrefix_ReportsBLEX012()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S { [BlexStateAttribute] private int Count; }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX012"));
    }

    [TestMethod]
    public void RefParameter_ReportsBLEX013()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S { [BlexActionAttribute] private void OnPush(ref int x) { } }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX013"));
    }

    [TestMethod]
    public void GenericActionMethod_ReportsBLEX014()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S { [BlexActionAttribute] private void OnSet<T>(T value) { } }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX014"));
    }

    [TestMethod]
    public void GenericComputedMethod_ReportsBLEX014_InsteadOfBrokenGeneratedCode()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S { [BlexComputedAttribute] private T ComputeFirst<T>() => default!; }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX014"));
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void RecordStore_ReportsBLEX015()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial record S { [BlexStateAttribute] private int _x; }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX015"));
    }

    [TestMethod]
    public void StoreWithBaseClass_ReportsBLEX016()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            public class MyBase { }
            [BlexStoreAttribute] public partial class S : MyBase { [BlexStateAttribute] private int _x; }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX016"));
    }

    [TestMethod]
    public void GenericStore_ReportsBLEX006()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S<T> { [BlexStateAttribute] private int _x; }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX006"));
    }

    [TestMethod]
    public void StaticStore_ReportsBLEX006()
    {
        // A static class cannot derive from BlexStoreBase or hold the instance members the generator
        // emits; without this the user gets four raw CS errors inside generated code.
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public static partial class S { }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX006"));
        Assert.IsEmpty(result.GeneratedSources);
    }

    [TestMethod]
    public void GeneratedMemberCollidingWithBeginAction_ReportsBLEX005()
    {
        // BeginAction/DispatchApprovedAsync are BlexStoreBase members the effect wrappers call.
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexActionAttribute] private void OnBeginAction() { }
            }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX005"));
    }

    [TestMethod]
    public void VoidComputed_ReportsBLEX017()
    {
        // `public void X { get { ... } }` does not compile; the memoized property needs a value.
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexComputedAttribute] private void ComputeThing() { }
            }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX017"));
    }

    [TestMethod]
    public void VoidComputed_StillEmitsTheRestOfTheStoreWithoutCompileErrors()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexComputedAttribute] private void ComputeThing() { }
                [BlexComputedAttribute] private int ComputeDouble() => X * 2;
                [BlexActionAttribute] private void OnBump() => X++;
            }
            """);

        // The offending member is skipped rather than poisoning the whole emission, so the only
        // failure the user sees is the precise BLEX017 diagnostic.
        Assert.IsTrue(result.HasDiagnostic("BLEX017"));
        Assert.IsEmpty(result.CompileErrors());
        Assert.Contains("public int Double", result.SingleGenerated);
    }

    [TestMethod]
    public void StateFieldOutsideAStore_ReportsBLEX018()
    {
        // Without the [BlexStoreAttribute] the whole file compiles cleanly and generates nothing,
        // which is indistinguishable from a broken generator. Say so.
        var result = GeneratorTestHelper.Run(Usings + """
            public partial class NotAStore { [BlexStateAttribute] private int _x; }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX018"));
    }

    [TestMethod]
    public void ActionComputedAndEffectOutsideAStore_ReportBLEX018()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            public partial class NotAStore
            {
                [BlexComputedAttribute] private int ComputeX() => 1;
                [BlexActionAttribute] private void OnBump() { }
                [BlexEffectAttribute] private Task OnLoad() => Task.CompletedTask;
            }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX018"));
        Assert.AreEqual(3, result.GeneratorDiagnostics.Count(d => d.Id == "BLEX018"));
    }

    [TestMethod]
    public void MembersInsideAStore_DoNotReportBLEX018()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexComputedAttribute] private int ComputeDouble() => X * 2;
                [BlexActionAttribute] private void OnBump() => X++;
                [BlexEffectAttribute] private Task OnLoad() => Task.CompletedTask;
            }
            """);
        Assert.IsFalse(result.HasDiagnostic("BLEX018"));
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void NegativeDebounce_ReportsBLEX019()
    {
        // Task.Delay throws on a negative delay, at runtime, from inside generated code.
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexEffectAttribute(DebounceMilliseconds = -1)] private Task OnLoad() => Task.CompletedTask;
            }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX019"));
    }

    [TestMethod]
    public void TwoActionsUnderOneLabel_ReportsBLEX021()
    {
        // Different wrappers (SaveDraft, SaveFinal), so BLEX005 has nothing to say -- but both are
        // dispatched, subscribed to and replayed as "Save it".
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexActionAttribute(Name = "Save it")] private void OnSaveDraft() { }
                [BlexEffectAttribute(Name = "Save it")] private Task OnSaveFinal(int v) => Task.CompletedTask;
            }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX021"));
    }

    [TestMethod]
    public void AnActionLabelledLikeADirectAssignment_ReportsBLEX021()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _count;
                [BlexActionAttribute(Name = "Set Count")] private void OnChangeCount(int v) => Count = v;
            }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX021"));
    }

    [TestMethod]
    public void DistinctLabels_AndAStoreThatRecordsNoAssignments_ReportNothing()
    {
        // EnforceActions: no direct assignment is ever recorded, so "Set Count" is free.
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute(EnforceActions = true)] public partial class S
            {
                [BlexStateAttribute] private int _count;
                [BlexActionAttribute(Name = "Set Count")] private void OnChangeCount(int v) => Count = v;
                [BlexActionAttribute(Name = "Save it")] private void OnSaveDraft() { }
                [BlexActionAttribute(Name = "Save it for good")] private void OnSaveFinal() { }
            }
            """);
        Assert.IsFalse(result.HasDiagnostic("BLEX021"));
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    [DataRow("RetryCount = -1")]
    [DataRow("RetryCount = 1, RetryDelayMilliseconds = -5")]
    [DataRow("TimeoutMilliseconds = -1")]
    public void NegativeRetryOrTimeout_ReportsBLEX022(string arguments)
    {
        var result = GeneratorTestHelper.Run(Usings + $$"""
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexEffectAttribute({{arguments}})] private Task OnLoad(CancellationToken ct) => Task.CompletedTask;
            }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX022"));
    }

    [TestMethod]
    public void TimeoutWithoutAToken_ReportsBLEX023()
    {
        // The attempt could not be stopped: it would carry on, and change state, after its run had failed.
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexEffectAttribute(TimeoutMilliseconds = 500)] private Task OnLoad() => Task.CompletedTask;
            }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX023"));
    }

    [TestMethod]
    public void NegativeStoreVersion_ReportsBLEX024_OnTheVersion()
    {
        var source = Usings + """
            [BlexStoreAttribute(Persist = true, Version = -1)] public partial class S { [BlexStateAttribute] private int _x; }
            """;
        var result = GeneratorTestHelper.Run(source);

        var diagnostic = result.GeneratorDiagnostics.Single(d => d.Id == "BLEX024");
        Assert.AreEqual("Version = -1", source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
        Assert.DoesNotContain("PersistVersion", result.SingleGenerated);
    }

    [TestMethod]
    public void StoreVersionWithoutPersist_ReportsBLEX025_OnTheVersion()
    {
        var source = Usings + """
            [BlexStoreAttribute(Version = 2)] public partial class S { [BlexStateAttribute] private int _x; }
            """;
        var result = GeneratorTestHelper.Run(source);

        var diagnostic = result.GeneratorDiagnostics.Single(d => d.Id == "BLEX025");
        Assert.AreEqual("Version = 2", source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void StoreVersion_WithAPersistOverrideOfItsOwn_IsFine()
    {
        // Persisted without the attribute saying so: the version is written and checked, so nothing to warn about.
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute(Version = 2)] public partial class S
            {
                [BlexStateAttribute] private int _x;
                public override bool Persist => true;
            }
            """);
        Assert.IsFalse(result.HasDiagnostic("BLEX025"));
        Assert.Contains("public override int PersistVersion => 2;", result.SingleGenerated);
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    [DataRow("Persist = true, Version = 2")]
    [DataRow("Persist = true, Version = 0")]
    [DataRow("Version = 0")]
    public void StoreVersion_WithPersistOrZero_IsFine(string arguments)
    {
        var result = GeneratorTestHelper.Run(Usings + $$"""
            [BlexStoreAttribute({{arguments}})] public partial class S { [BlexStateAttribute] private int _x; }
            """);
        Assert.IsFalse(result.HasDiagnostic("BLEX024"));
        Assert.IsFalse(result.HasDiagnostic("BLEX025"));
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void RetryWithoutAToken_IsFine()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexEffectAttribute(RetryCount = 3)] private Task OnLoad(int id) => Task.CompletedTask;
                [BlexEffectAttribute(RetryCount = 2, TimeoutMilliseconds = 100)] private ValueTask OnSave(string text, CancellationToken ct) => default;
            }
            """);
        Assert.IsFalse(result.HasDiagnostic("BLEX022"));
        Assert.IsFalse(result.HasDiagnostic("BLEX023"));
        Assert.IsEmpty(result.CompileErrors());
        Assert.Contains("base.RunEffectAttemptsAsync(__blexAttemptCt => OnLoad(id), default, 3, 1000, 0)", result.SingleGenerated);
        Assert.Contains("base.RunEffectAttemptsAsync(__blexAttemptCt => OnSave(text, __blexAttemptCt).AsTask(), __blexCt, 2, 1000, 100)", result.SingleGenerated);
    }

    [TestMethod]
    public void NoRetryOrTimeout_EmitsTheDirectCall()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexEffectAttribute] private Task OnLoad(CancellationToken ct) => Task.CompletedTask;
            }
            """);
        Assert.IsEmpty(result.CompileErrors());
        Assert.DoesNotContain("RunEffectAttemptsAsync", result.SingleGenerated);
    }

    [TestMethod]
    public void ZeroDebounce_IsTheDefaultAndReportsNothing()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexEffectAttribute(DebounceMilliseconds = 0)] private Task OnLoad() => Task.CompletedTask;
                [BlexEffectAttribute] private Task OnOther() => Task.CompletedTask;
            }
            """);
        Assert.IsFalse(result.HasDiagnostic("BLEX019"));
        Assert.IsEmpty(result.CompileErrors());
        Assert.DoesNotContain("DebounceEffectAsync", result.SingleGenerated);
    }

    [TestMethod]
    public void PositiveDebounce_EmitsTheGateBeforeAnythingObservable()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexEffectAttribute(DebounceMilliseconds = 250)] private Task OnSearch(string q) => Task.CompletedTask;
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
        var generated = result.SingleGenerated;
        // Through the base class, which waits on the manager's clock (so a test can advance a fake
        // TimeProvider through the quiet period) and skips the wait while a log is replayed.
        Assert.Contains("await base.DebounceEffectAsync(250, __blexDebounceCts.Token);", generated);

        // The gate must precede BeginAction: a superseded invocation is never offered to
        // middleware and records nothing.
        Assert.IsTrue(generated.IndexOf("DebounceEffectAsync(250") < generated.IndexOf("BeginAction(\"Search\""));
    }

    [TestMethod]
    [DataRow("Dispose")]
    [DataRow("CancelEffects")]
    public void ActionCollidingWithADisposalMember_ReportsBLEX005(string name)
    {
        // These are emitted (or inherited) by BlexStoreBase, so a wrapper of the same name would
        // hide them and silently stop cancelling in-flight effects.
        var result = GeneratorTestHelper.Run(Usings + $$"""
            [BlexStoreAttribute] public partial class S
            {
                [BlexActionAttribute] private void On{{name}}() { }
            }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX005"));
    }

    [TestMethod]
    public void MemberInATypeNestedInsideAStore_ReportsBLEX018()
    {
        // The nested type is not itself a store, so nothing is generated for its members either.
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                public class Inner { [BlexStateAttribute] private int _y; }
            }
            """);
        Assert.IsTrue(result.HasDiagnostic("BLEX018"));
    }
}
