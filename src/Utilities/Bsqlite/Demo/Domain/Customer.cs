namespace Bit.Bsqlite.Demo.Domain;

public sealed class Customer
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public bool IsVip { get; set; }

    public decimal CreditLimit { get; set; }

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<SalesOrder> Orders { get; } = [];
}
