using System.Text.Json.Nodes;
using Bit.Blex.Testing;

namespace Bit.Blex.Tests;

[BlexStoreAttribute(Name = "vault", EnforceActions = true)]
public partial class VaultStore
{
    [BlexStateAttribute] private int _balance;

    public VaultStore(int opening = 10)
    {
        // Setting up initial state before anyone can observe the store is not a mutation.
        Balance = opening;
    }

    [BlexActionAttribute]
    private void OnDeposit(int amount) => Balance += amount;

    [BlexEffectAttribute]
    private async Task OnAccrue()
    {
        await Task.Yield();
        Balance += 1;
    }

    /// <summary>The store's own code, assigning state outside any action.</summary>
    public void Sneak(int value) => Balance = value;
}

/// <summary>
/// <c>[BlexStore(EnforceActions = true)]</c>: state changes only through the store's own actions. The
/// setters are private (so outside code cannot compile an assignment), and an assignment by the
/// store's own code outside an action throws once the store is registered.
/// </summary>
[TestClass]
public class EnforceActionsTests
{
    [TestMethod]
    public void The_generated_setters_are_private()
    {
        var setter = typeof(VaultStore).GetProperty(nameof(VaultStore.Balance))!.SetMethod!;

        Assert.IsTrue(setter.IsPrivate);
        Assert.IsTrue(typeof(CounterStore).GetProperty(nameof(CounterStore.Count))!.SetMethod!.IsPublic, "Only stores that opt in are affected.");
    }

    [TestMethod]
    public async Task Actions_and_effects_change_state_as_usual()
    {
        using var harness = BlexTestHarness.For(new VaultStore());

        harness.Store.Deposit(5);
        await harness.Store.Accrue();

        Assert.AreEqual(16, harness.Store.Balance);
        CollectionAssert.AreEqual(new[] { "Deposit", "Accrue" }, harness.Log.Names.ToArray());
    }

    [TestMethod]
    public void An_assignment_outside_an_action_throws_once_registered()
    {
        using var harness = BlexTestHarness.For(new VaultStore());

        var rejected = Assert.ThrowsExactly<InvalidOperationException>(() => harness.Store.Sneak(1_000));

        StringAssert.Contains(rejected.Message, "Balance");
        Assert.AreEqual(10, harness.Store.Balance);
        Assert.IsEmpty(harness.Log.Actions);
    }

    [TestMethod]
    public void Assigning_the_value_it_already_has_is_not_a_mutation()
    {
        using var harness = BlexTestHarness.For(new VaultStore());

        harness.Store.Sneak(harness.Store.Balance);

        Assert.AreEqual(10, harness.Store.Balance);
    }

    [TestMethod]
    public void A_store_that_is_not_registered_is_not_policed()
    {
        var vault = new VaultStore();
        vault.Sneak(3);
        Assert.AreEqual(3, vault.Balance);
    }

    [TestMethod]
    public void Restores_and_resets_are_not_assignments()
    {
        using var harness = BlexTestHarness.For(new VaultStore());
        harness.Store.Deposit(90);

        harness.Store.RestoreState(new JsonObject { ["Balance"] = 42 });
        Assert.AreEqual(42, harness.Store.Balance);

        harness.Store.ResetState();
        Assert.AreEqual(10, harness.Store.Balance);
    }

    [TestMethod]
    public void No_assignment_descriptors_are_offered_for_dispatch()
    {
        var names = new VaultStore().ActionDescriptors.Select(a => a.Name).ToArray();

        CollectionAssert.Contains(names, "Deposit");
        CollectionAssert.DoesNotContain(names, "Set Balance");
    }
}
