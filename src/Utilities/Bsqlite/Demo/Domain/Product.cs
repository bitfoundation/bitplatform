namespace Bit.Bsqlite.Demo.Domain;

public sealed class Product
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public double WeightKg { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateOnly? DiscontinuedOn { get; set; }

    public TimeSpan LeadTime { get; set; } = TimeSpan.FromDays(2);

    public string Tags { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public List<OrderLine> Lines { get; } = [];
}
