namespace Bit.Bsqlite.Demo.Domain;

public sealed class OrderLine
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public SalesOrder? Order { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }
}
