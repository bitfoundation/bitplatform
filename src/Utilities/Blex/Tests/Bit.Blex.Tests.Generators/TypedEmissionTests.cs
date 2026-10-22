using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests.Generators;

/// <summary>
/// What the generator emits for typed actions, <c>EnforceActions</c>, <c>[BlexState(Persist = false)]</c>
/// and instance keys.
/// </summary>
[TestClass]
public class TypedEmissionTests
{
    private const string Usings = "using System.Threading; using System.Threading.Tasks; using Bit.Blex;\n";

    [TestMethod]
    public void Every_action_and_effect_gets_a_typed_descriptor()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;

            [BlexStoreAttribute(Name = "orders")]
            public partial class OrdersStore
            {
                [BlexStateAttribute] private int _units;

                [BlexActionAttribute] private void OnClear() => Units = 0;
                [BlexActionAttribute] private void OnAdd(int units) => Units += units;
                [BlexActionAttribute] private void OnPlace(string name, int? quantity = null) => Units += quantity ?? 1;
                [BlexActionAttribute(Name = "Apply discount")] private void OnApplyDiscount(decimal percent) { }
                [BlexActionAttribute] private void OnTag(params string[] tags) { }
                [BlexActionAttribute] private async ValueTask OnImport(int count) => await Task.Yield();
                [BlexEffectAttribute] private async Task OnSync(string reason, CancellationToken ct) => await Task.Delay(1, ct);
            }

            public static class Usage
            {
                public static void Use(OrdersStore store, BlexManager manager)
                {
                    BlexAction<OrdersStore> clear = OrdersStore.Actions.Clear;
                    BlexAction<OrdersStore, int> add = OrdersStore.Actions.Add;
                    BlexAction<OrdersStore, (string Name, int? Quantity)> place = OrdersStore.Actions.Place;
                    BlexAction<OrdersStore, decimal> discount = OrdersStore.Actions.ApplyDiscount;
                    BlexAction<OrdersStore, string[]> tag = OrdersStore.Actions.Tag;
                    BlexAction<OrdersStore, int> import = OrdersStore.Actions.Import;
                    BlexAction<OrdersStore, string> sync = OrdersStore.Actions.Sync;

                    manager.SubscribeToAction(OrdersStore.Actions.Place, (s, order) => System.Console.WriteLine(order.Name + order.Quantity));
                }
            }
            """);

        Assert.IsFalse(result.HasGeneratorError);
        Assert.IsEmpty(result.CompileErrors());

        var generated = result.SingleGenerated;
        Assert.Contains("public static class Actions", generated);
        Assert.Contains("new(\"Apply discount\"", generated);
        Assert.Contains("new string[] { \"name\", \"quantity\" }", generated);
        Assert.Contains("protected override global::System.Collections.Generic.IReadOnlyList<global::Bit.Blex.BlexActionDescriptor> DescribeActions()", generated);
        Assert.Contains("Actions.Sync,", generated);
    }

    [TestMethod]
    public void A_parameter_that_cannot_name_a_tuple_element_leaves_the_tuple_unnamed()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexActionAttribute] private void OnRest(string rest, int value) { }
                [BlexActionAttribute] private void OnSwap(int item2, int item1) { }
                [BlexActionAttribute] private void OnCase(int name, int Name) { }
                [BlexActionAttribute] private void OnFine(int item1, int item2) { }
            }

            public static class Usage
            {
                public static void Use()
                {
                    BlexAction<S, (string, int)> rest = S.Actions.Rest;
                    BlexAction<S, (int, int)> swap = S.Actions.Swap;
                    BlexAction<S, (int, int)> casing = S.Actions.Case;
                    BlexAction<S, (int Item1, int Item2)> fine = S.Actions.Fine;
                }
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
        Assert.Contains("__s.Rest(__p.Item1, __p.Item2)", result.SingleGenerated);
    }

    [TestMethod]
    public void A_store_without_actions_gets_no_Actions_class_but_its_assignments_stay_dispatchable()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S { [BlexStateAttribute] private int _x; }
            """);

        Assert.IsEmpty(result.CompileErrors());
        var generated = result.SingleGenerated;
        Assert.DoesNotContain("class Actions", generated);
        Assert.Contains("(\"Set X\", new string[] { \"X\" }", generated);
    }

    [TestMethod]
    public void A_store_using_the_name_Actions_reports_BLEX020_and_still_compiles()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                public string[] Actions { get; } = [];
                [BlexActionAttribute] private void OnBump() => X++;
            }
            """);

        Assert.IsTrue(result.HasDiagnostic("BLEX020"));
        Assert.IsFalse(result.HasGeneratorError);
        Assert.IsEmpty(result.CompileErrors());

        // No typed surface, but the action can still be dispatched by name.
        var generated = result.SingleGenerated;
        Assert.DoesNotContain("public static class Actions", generated);
        Assert.Contains("new global::Bit.Blex.BlexAction<global::App.S>(\"Bump\"", generated);
    }

    [TestMethod]
    public void A_nested_type_named_Actions_reports_BLEX020()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                public static class Actions { }
                [BlexActionAttribute] private void OnBump() => X++;
            }
            """);

        Assert.IsTrue(result.HasDiagnostic("BLEX020"));
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void An_optional_parameter_reads_its_default_when_a_payload_leaves_it_out()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            public enum Mode { A, B }
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _x;
                [BlexActionAttribute] private void OnAdd(int amount = 1) => X += amount;
                [BlexActionAttribute] private void OnTune(string label, string? note = null, Mode mode = Mode.B) => X++;
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
        var generated = result.SingleGenerated;
        Assert.Contains("GetArgOrDefault<int>(__a, 0, \"amount\", 1)", generated);
        Assert.Contains("GetArg<string>(__a, 0, \"label\")", generated);
        Assert.Contains("GetArgOrDefault<string?>(__a, 1, \"note\", null!)", generated);
        Assert.Contains("GetArgOrDefault<global::App.Mode>(__a, 2, \"mode\", (global::App.Mode)(1))", generated);
    }

    [TestMethod]
    public void A_generated_member_named_Actions_reports_BLEX020_too()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                [BlexStateAttribute] private int _actions;
                [BlexActionAttribute] private void OnBump() => Actions++;
            }
            """);

        Assert.IsTrue(result.HasDiagnostic("BLEX020"));
        Assert.IsEmpty(result.CompileErrors());
    }

    [TestMethod]
    public void EnforceActions_makes_the_setters_private()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute(EnforceActions = true)] public partial class Vault
            {
                [BlexStateAttribute] private int _balance;
                [BlexActionAttribute] private void OnDeposit(int amount) => Balance += amount;
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
        var generated = result.SingleGenerated;
        Assert.Contains("private set => SetState(ref this._balance", generated);
        Assert.Contains("protected override bool EnforcesActions => true;", generated);
        Assert.DoesNotContain("\"Set Balance\"", generated);
    }

    [TestMethod]
    public void EnforceActions_turns_an_outside_assignment_into_a_compile_error()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute(EnforceActions = true)] public partial class Vault
            {
                [BlexStateAttribute] private int _balance;
                [BlexActionAttribute] private void OnDeposit(int amount) => Balance += amount;
            }

            public static class Thief
            {
                public static void Steal(Vault vault) => vault.Balance = 0;
            }
            """);

        Assert.IsTrue(result.CompileErrors().Any(d => d.Id == "CS0272"), "Assigning a private setter from outside must not compile.");
    }

    [TestMethod]
    public void A_field_marked_Persist_false_is_listed_as_transient()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            namespace App;
            [BlexStoreAttribute(Persist = true)] public partial class S
            {
                [BlexStateAttribute] private string[] _recent = [];
                [BlexStateAttribute(Persist = false)] private string _query = "";
                [BlexStateAttribute(Persist = true)] private int _kept;
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
        var generated = result.SingleGenerated;
        Assert.Contains("__blexTransientKeys = new string[] { \"Query\" }", generated);
        Assert.Contains("protected override global::System.Collections.Generic.IReadOnlyCollection<string> TransientStateKeys", generated);
    }

    [TestMethod]
    public void A_store_with_nothing_transient_does_not_override_TransientStateKeys()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute(Persist = true)] public partial class S { [BlexStateAttribute] private int _x; }
            """);

        Assert.DoesNotContain("TransientStateKeys", result.SingleGenerated);
    }

    [TestMethod]
    public void The_name_carries_the_instance_key()
    {
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute(Name = "counter")] public partial class S { [BlexStateAttribute] private int _x; }
            """);

        Assert.Contains("public override string Name => base.WithInstanceKey(\"counter\");", result.SingleGenerated);
    }

    [TestMethod]
    [DataRow("_timeProvider")]
    [DataRow("_instanceKey")]
    [DataRow("_actionDescriptors")]
    public void State_named_like_a_new_base_member_reports_BLEX005(string field)
    {
        var result = GeneratorTestHelper.Run(Usings + $$"""
            [BlexStoreAttribute] public partial class S { [BlexStateAttribute] private int {{field}}; }
            """);

        Assert.IsTrue(result.HasDiagnostic("BLEX005"));
    }

    [TestMethod]
    public void A_store_with_its_own_TimeProvider_member_still_debounces_on_the_managers()
    {
        // base.TimeProvider in the generated wait, so a member of the same name cannot capture it.
        var result = GeneratorTestHelper.Run(Usings + """
            [BlexStoreAttribute] public partial class S
            {
                private readonly string TimeProvider = "not a clock";
                [BlexStateAttribute] private int _x;
                [BlexEffectAttribute(DebounceMilliseconds = 100)] private async Task OnSearch() => await Task.Yield();
            }
            """);

        Assert.IsEmpty(result.CompileErrors());
    }
}
