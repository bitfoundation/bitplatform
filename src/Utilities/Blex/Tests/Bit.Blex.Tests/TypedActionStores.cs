namespace Bit.Blex.Tests;

public enum OrderPriority
{
    Low,
    High,
}

/// <summary>Every shape an action can take, for the typed descriptors and dispatch by name.</summary>
[BlexStoreAttribute(Name = "orders")]
public partial class OrderBookStore
{
    [BlexStateAttribute] private string[] _lines = [];
    [BlexStateAttribute] private int _units;
    [BlexStateAttribute] private decimal _discount;
    [BlexStateAttribute] private OrderPriority _priority;
    [BlexStateAttribute] private string _lastSync = "";
    [BlexStateAttribute] private string[] _tags = [];

    [BlexActionAttribute]
    private void OnClear()
    {
        Lines = [];
        Units = 0;
    }

    [BlexActionAttribute]
    private void OnAddUnits(int units) => Units += units;

    [BlexActionAttribute]
    private void OnPlace(string name, int quantity)
    {
        Lines = [.. Lines, $"{quantity} x {name}"];
        Units += quantity;
    }

    [BlexActionAttribute(Name = "Apply discount")]
    private void OnApplyDiscount(decimal percent) => Discount = percent;

    [BlexActionAttribute]
    private void OnSetPriority(OrderPriority priority) => Priority = priority;

    [BlexActionAttribute]
    private void OnTag(params string[] tags) => Tags = tags;

    // "rest" cannot name a tuple element (CS8126), so this payload is an unnamed tuple.
    [BlexActionAttribute]
    private void OnMove(string rest, int value) => Lines = [.. Lines, $"{rest}:{value}"];

    [BlexActionAttribute]
    private async Task OnImportAsync(int count)
    {
        await Task.Yield();
        Units += count;
    }

    [BlexEffectAttribute]
    private async Task OnSync(string reason, CancellationToken ct)
    {
        await Task.Yield();
        ct.ThrowIfCancellationRequested();
        LastSync = reason;
    }

    [BlexActionAttribute]
    private void OnFail(string why) => throw new InvalidOperationException(why);

    [BlexActionAttribute]
    private async Task OnFailLater(string why)
    {
        await Task.Yield();
        throw new InvalidOperationException(why);
    }
}

/// <summary>A store whose name contains the '/' that separates it from the action name.</summary>
[BlexStoreAttribute(Name = "admin/users")]
public partial class AdminUsersStore
{
    [BlexStateAttribute] private int _invited;

    [BlexActionAttribute]
    private void OnInvite(int count) => Invited += count;
}
