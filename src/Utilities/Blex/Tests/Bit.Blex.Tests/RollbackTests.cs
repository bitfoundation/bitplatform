using Bit.Blex.Testing;

namespace Bit.Blex.Tests;

/// <summary>A store whose actions are transactions: a throw puts every field back.</summary>
[BlexStoreAttribute(Name = "ledger", RollbackOnError = true)]
public partial class LedgerStore
{
    [BlexStateAttribute] private int _balance = 100;
    [BlexStateAttribute] private string? _lastEntry;
    [BlexStateAttribute] private IReadOnlyList<int> _entries = [];

    [BlexComputedAttribute] private int ComputeDoubled() => Balance * 2;

    /// <summary>Applies the change, then fails validation after it: half an action, unless rolled back.</summary>
    [BlexActionAttribute]
    private void OnWithdraw(int amount)
    {
        Balance -= amount;
        LastEntry = "withdraw " + amount;
        Entries = [.. Entries, -amount];
        if (Balance < 0)
            throw new InvalidOperationException("Insufficient funds.");
    }

    [BlexActionAttribute]
    private void OnDeposit(int amount) => Balance += amount;

    /// <summary>Reads a computed after its own write, the way validation inside an action does.</summary>
    [BlexActionAttribute]
    private void OnWithdrawChecked(int amount)
    {
        _ = Doubled;          // memoized before the write
        Balance -= amount;
        if (Doubled < 0)      // must see the write, not the memo
            throw new InvalidOperationException("Doubled went negative.");
    }

    /// <summary>Tries a withdrawal it expects may fail, and records the attempt either way.</summary>
    [BlexActionAttribute]
    private void OnTryWithdraw(int amount)
    {
        try
        {
            Withdraw(amount);
        }
        catch (InvalidOperationException)
        {
            LastEntry = "declined " + amount;
        }
    }
}

/// <summary>The same shape without the opt-in: a throwing action keeps what it changed, like any C# method.</summary>
[BlexStoreAttribute(Name = "loose")]
public partial class LooseLedgerStore
{
    [BlexStateAttribute] private int _balance = 100;

    [BlexActionAttribute]
    private void OnWithdraw(int amount)
    {
        Balance -= amount;
        if (Balance < 0)
            throw new InvalidOperationException("Insufficient funds.");
    }
}

/// <summary><c>[BlexStore(RollbackOnError = true)]</c>: a synchronous action that throws leaves no trace.</summary>
[TestClass]
public class RollbackTests
{
    [TestMethod]
    public void A_throwing_action_puts_every_field_back_and_records_nothing()
    {
        using var harness = BlexTestHarness.For<LedgerStore>();
        var store = harness.Store;
        var notifications = 0;
        store.StateChanged += () => notifications++;

        Assert.ThrowsExactly<InvalidOperationException>(() => store.Withdraw(150));

        Assert.AreEqual(100, store.Balance);
        Assert.IsNull(store.LastEntry);
        Assert.IsEmpty(store.Entries);
        Assert.AreEqual(0, notifications, "nothing changed, so nothing is notified");
        Assert.IsEmpty(harness.Log.Actions, "a rolled-back action is not recorded, persisted or undoable");
        Assert.ContainsSingle(harness.Failures, "the failure still reaches the action-error hooks");
    }

    [TestMethod]
    public void A_computed_read_during_the_failed_action_is_not_left_stale()
    {
        using var harness = BlexTestHarness.For<LedgerStore>();
        var store = harness.Store;
        Assert.AreEqual(200, store.Doubled);

        Assert.ThrowsExactly<InvalidOperationException>(() => store.WithdrawChecked(150));

        Assert.AreEqual(100, store.Balance);
        Assert.AreEqual(200, store.Doubled);
    }

    [TestMethod]
    public void A_computed_read_later_in_the_same_action_sees_the_write()
    {
        using var harness = BlexTestHarness.For<LedgerStore>();
        var store = harness.Store;
        Assert.AreEqual(200, store.Doubled);   // memoized

        store.WithdrawChecked(50);             // reads Doubled after writing Balance; fine as long as it is fresh

        Assert.AreEqual(50, store.Balance);
        Assert.AreEqual(100, store.Doubled);
        Assert.ThrowsExactly<InvalidOperationException>(() => store.WithdrawChecked(60), "the check inside the action must see the new balance");
    }

    [TestMethod]
    public void A_nested_action_whose_failure_the_caller_catches_is_still_undone()
    {
        using var harness = BlexTestHarness.For<LedgerStore>();
        var store = harness.Store;

        store.TryWithdraw(150);

        Assert.AreEqual(100, store.Balance, "the nested withdrawal's own writes are rolled back");
        Assert.IsEmpty(store.Entries);
        Assert.AreEqual("declined 150", store.LastEntry, "the caller's own change stays");
        Assert.AreSequenceEqual(["TryWithdraw"], harness.Log.Names.ToList());
    }

    [TestMethod]
    public void A_successful_action_is_unaffected()
    {
        using var harness = BlexTestHarness.For<LedgerStore>();

        harness.Store.Withdraw(30);
        harness.Store.Deposit(5);

        Assert.AreEqual(75, harness.Store.Balance);
        Assert.AreSequenceEqual(["Withdraw", "Deposit"], harness.Log.Names.ToList());
    }

    [TestMethod]
    public void Without_the_opt_in_a_throwing_action_keeps_its_partial_change()
    {
        using var harness = BlexTestHarness.For<LooseLedgerStore>();

        Assert.ThrowsExactly<InvalidOperationException>(() => harness.Store.Withdraw(150));

        Assert.AreEqual(-50, harness.Store.Balance);
        Assert.AreSequenceEqual(["Withdraw"], harness.Log.Names.ToList(), "it changed state, so it is recorded");
    }

    [TestMethod]
    public void Selectors_do_not_keep_a_value_derived_from_the_undone_change()
    {
        using var harness = BlexTestHarness.For<LedgerStore>();
        var store = harness.Store;
        using var selector = harness.Manager.CreateSelector(() => store.Balance);
        Assert.AreEqual(100, selector.Value);

        Assert.ThrowsExactly<InvalidOperationException>(() => store.Withdraw(150));

        Assert.AreEqual(100, selector.Value);
    }
}
