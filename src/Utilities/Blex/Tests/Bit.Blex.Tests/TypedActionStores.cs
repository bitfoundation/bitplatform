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

public sealed record Parcel(string Name, int Quantity);

/// <summary>
/// An action taking one object, actions with optional parameters, and actions that call another
/// store's action from inside their own body.
/// </summary>
[BlexStoreAttribute(Name = "shipments")]
public partial class ShipmentStore
{
    private readonly OrderBookStore? _orders;

    [BlexStateAttribute] private string[] _shipped = [];
    [BlexStateAttribute] private int _bumps;
    [BlexStateAttribute] private string? _note = "none";

    public ShipmentStore()
    {
    }

    public ShipmentStore(OrderBookStore orders) => _orders = orders;

    [BlexActionAttribute]
    private void OnShip(Parcel parcel) => Shipped = [.. Shipped, $"{parcel.Quantity} x {parcel.Name}"];

    [BlexActionAttribute]
    private void OnBump(int amount = 1) => Bumps += amount;

    [BlexActionAttribute]
    private void OnAnnotate(string? note = null, string prefix = "#") => Note = note is null ? null : prefix + note;

    [BlexActionAttribute]
    private void OnShipAndBill(string name)
    {
        Shipped = [.. Shipped, name];
        _orders!.Place(name, 1);
    }

    [BlexActionAttribute]
    private async Task OnShipLaterAsync(string name)
    {
        await Task.Yield();
        _orders!.Place(name, 1);
        Shipped = [.. Shipped, name];
    }
}
