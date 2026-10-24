namespace Bit.Blex.Demo.Client.Stores;

/// <summary>
/// <c>EnforceActions = true</c>: the generated <c>Balance</c> setter is private, so nothing outside the store can
/// assign it - <c>account.Balance = 0</c> does not compile - and an assignment the store's own code makes outside
/// one of its actions throws once the store is registered. <see cref="SneakBonus"/> is that mistake, kept on
/// purpose for the actions page to run. The site also registers a typed filter over <c>Deposit</c>
/// (<c>options.UseFilter(AccountStore.Actions.Deposit, ...)</c>) for the middleware page.
/// </summary>
[BlexStoreAttribute(Name = "account", EnforceActions = true)]
public partial class AccountStore
{
    [BlexStateAttribute] private decimal _balance;

    [BlexActionAttribute] private void OnDeposit(decimal amount) => Balance += amount;

    [BlexActionAttribute] private void OnWithdraw(decimal amount) => Balance -= amount;

    /// <summary>
    /// Compiles - it is the store's own code - but assigns state outside an action, which an enforcing store
    /// refuses at runtime with an <see cref="InvalidOperationException"/>. Always a change, so it always throws:
    /// an assignment of the value already held is no assignment at all.
    /// </summary>
    public void SneakBonus() => Balance += 1000m;
}
