namespace Bit.Bsqlite.Demo.Domain;

/// <summary>
/// Named <see cref="SalesOrder"/> rather than Order so it does not collide with LINQ's OrderBy.
/// </summary>
public sealed class SalesOrder
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    public DateTimeOffset OrderedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateOnly? ShipBy { get; set; }

    public string Notes { get; set; } = string.Empty;

    public List<OrderLine> Lines { get; set; } = [];
}
