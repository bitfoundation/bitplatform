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
        Assert.DoesNotContain("Task.Delay", result.SingleGenerated);
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
        Assert.Contains("Task.Delay(250", generated);

        // The gate must precede BeginAction: a superseded invocation is never offered to
        // middleware and records nothing.
        Assert.IsTrue(generated.IndexOf("Task.Delay(250") < generated.IndexOf("BeginAction(\"Search\""));
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
