namespace Bit.Blex.Demo.Client.Stores;

/// <summary>One line of a <see cref="WalletStore"/> or <see cref="LedgerStore"/> statement.</summary>
/// <param name="Label">What moved the money.</param>
/// <param name="Amount">How much: positive in, negative out.</param>
public sealed record LedgerEntry(string Label, decimal Amount);

/// <summary>
/// The default behaviour of an action that throws, kept beside <see cref="LedgerStore"/> so the two can be
/// compared: a C# method's changes before its throw stay changed, so <c>Withdraw</c> leaves the balance
/// overdrawn and the entry written - notified, recorded and persisted like any other change.
/// </summary>
[BlexStoreAttribute(Name = "wallet")]
public partial class WalletStore
{
    [BlexStateAttribute] private decimal _balance = 100m;
    [BlexStateAttribute] private IReadOnlyList<LedgerEntry> _entries = [];

    [BlexActionAttribute]
    private void OnDeposit(decimal amount)
    {
        Balance += amount;
        Entries = [.. Entries, new LedgerEntry("deposit", amount)];
    }

    /// <summary>Takes the money out and writes the entry - and only then finds out it should not have.</summary>
    [BlexActionAttribute]
    private void OnWithdraw(decimal amount)
    {
        Balance -= amount;
        Entries = [.. Entries, new LedgerEntry("withdrawal", -amount)];

        if (Balance < 0)
            throw new InvalidOperationException($"Insufficient funds: the wallet cannot cover {amount:0.00}.");
    }
}

/// <summary>
/// The same two actions with <c>RollbackOnError = true</c>: every synchronous action is a transaction, so a
/// <c>Withdraw</c> that throws puts <c>Balance</c> and <c>Entries</c> back, and nothing is notified, recorded,
/// persisted or added to the undo history.
/// </summary>
[BlexStoreAttribute(Name = "ledger", RollbackOnError = true)]
public partial class LedgerStore
{
    [BlexStateAttribute] private decimal _balance = 100m;
    [BlexStateAttribute] private IReadOnlyList<LedgerEntry> _entries = [];

    [BlexActionAttribute]
    private void OnDeposit(decimal amount)
    {
        Balance += amount;
        Entries = [.. Entries, new LedgerEntry("deposit", amount)];
    }

    [BlexActionAttribute]
    private void OnWithdraw(decimal amount)
    {
        Balance -= amount;
        Entries = [.. Entries, new LedgerEntry("withdrawal", -amount)];

        if (Balance < 0)
            throw new InvalidOperationException($"Insufficient funds: the ledger cannot cover {amount:0.00}.");
    }
}
