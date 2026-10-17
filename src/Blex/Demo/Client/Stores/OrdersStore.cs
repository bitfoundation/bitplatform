namespace Bit.Blex.Demo.Client.Stores;

/// <summary>An order placed in the cross-store coordination demo.</summary>
public sealed record Order(int Id, string Item, int Quantity);

/// <summary>
/// The <em>source</em> store in the cross-store demo: something else listens for its actions and
/// reacts. It knows nothing about its listeners -- coordination is wired through the manager.
/// </summary>
[BlexStoreAttribute(Name = "orders")]
public partial class OrdersStore
{
    private int _nextId = 1;

    [BlexStateAttribute] private IReadOnlyList<Order> _orders = [];

    [BlexComputedAttribute] private int ComputeCount() => Orders.Count;

    [BlexComputedAttribute] private int ComputeUnits() => Orders.Sum(o => o.Quantity);

    [BlexActionAttribute]
    private void OnPlace(string item, int quantity)
        => Orders = [.. Orders, new Order(_nextId++, item, quantity)];

    [BlexActionAttribute] private void OnCancelLast()
    {
        if (Orders.Count > 0)
            Orders = [.. Orders.Take(Orders.Count - 1)];
    }

    [BlexActionAttribute] private void OnClear() => Orders = [];
}

/// <summary>
/// The <em>reacting</em> store in the cross-store demo. A subscription on the manager feeds it;
/// it has no reference to <see cref="OrdersStore"/>.
/// </summary>
[BlexStoreAttribute(Name = "notifications")]
public partial class NotificationsStore
{
    [BlexStateAttribute] private IReadOnlyList<string> _messages = [];

    [BlexComputedAttribute] private int ComputeUnread() => Messages.Count;

    [BlexActionAttribute] private void OnPush(string message) => Messages = [message, .. Messages];

    [BlexActionAttribute] private void OnClear() => Messages = [];
}
