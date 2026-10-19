using Bit.Bsqlite.Demo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bit.Bsqlite.Demo.Data;

/// <summary>
/// Deterministic workshop catalog used when a database has no categories yet.
/// </summary>
public static class DemoData
{
    public static async Task SeedIfEmptyAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Categories.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var tools = new Category { Name = "Tools", Color = "#0f766e" };
        var fasteners = new Category { Name = "Fasteners", Color = "#a16207" };
        var electrical = new Category { Name = "Electrical", Color = "#1d4ed8" };
        var safety = new Category { Name = "Safety", Color = "#b91c1c" };
        db.Categories.AddRange(tools, fasteners, electrical, safety);

        var caliper = Product(
            "TL-100",
            "Digital caliper",
            42.50m,
            0.18,
            tools,
            TimeSpan.FromDays(2),
            "metrology,stainless");
        var torque = Product(
            "TL-240",
            "Torque wrench 1/2\"",
            89.00m,
            1.35,
            tools,
            TimeSpan.FromDays(5),
            "assembly");
        var bolt = Product(
            "FS-M8",
            "M8×30 hex bolt (pack 50)",
            6.40m,
            0.62,
            fasteners,
            TimeSpan.FromDays(1),
            "metric,steel");
        var cable = Product(
            "EL-2.5",
            "2.5 mm² cable (100 m)",
            54.90m,
            8.4,
            electrical,
            TimeSpan.FromDays(7),
            "copper");
        var goggles = Product(
            "SF-01",
            "Safety goggles",
            11.25m,
            0.08,
            safety,
            TimeSpan.FromDays(1),
            "ppe");
        goggles.IsActive = false;
        goggles.DiscontinuedOn = new DateOnly(2026, 1, 15);
        db.Products.AddRange(caliper, torque, bolt, cable, goggles);

        var northwind = new Customer
        {
            PublicId = Guid.Parse("3c2a1f10-6b44-4d2a-9c11-0b7c2e4a1001"),
            DisplayName = "Northwind Workshop",
            Email = "stores@northwind.example",
            DateOfBirth = new DateOnly(1978, 4, 12),
            IsVip = true,
            CreditLimit = 25_000m,
            Notes = "Net-30. Prefers OPFS-backed copies for the shop floor tablets.",
            CreatedUtc = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc),
        };
        var contoso = new Customer
        {
            PublicId = Guid.Parse("3c2a1f10-6b44-4d2a-9c11-0b7c2e4a1002"),
            DisplayName = "Contoso Field Service",
            Email = "ops@contoso.example",
            DateOfBirth = new DateOnly(1991, 11, 3),
            IsVip = false,
            CreditLimit = 8_500.50m,
            Notes = string.Empty,
            CreatedUtc = new DateTime(2026, 3, 18, 14, 30, 0, DateTimeKind.Utc),
        };
        var alpine = new Customer
        {
            PublicId = Guid.Parse("3c2a1f10-6b44-4d2a-9c11-0b7c2e4a1003"),
            DisplayName = "Alpine Electrics",
            Email = "hello@alpine.example",
            DateOfBirth = new DateOnly(1985, 7, 22),
            IsVip = true,
            CreditLimit = 12_000m,
            CreatedUtc = new DateTime(2026, 5, 4, 8, 15, 0, DateTimeKind.Utc),
        };
        db.Customers.AddRange(northwind, contoso, alpine);

        db.Orders.AddRange(
            new SalesOrder
            {
                Number = "SO-1042",
                Customer = northwind,
                Status = OrderStatus.Shipped,
                OrderedAt = new DateTimeOffset(2026, 6, 2, 10, 15, 0, TimeSpan.Zero),
                ShipBy = new DateOnly(2026, 6, 5),
                Notes = "Left at the goods-in hatch.",
                Lines =
                [
                    new OrderLine { Product = caliper, Quantity = 2, UnitPrice = 42.50m },
                    new OrderLine { Product = goggles, Quantity = 12, UnitPrice = 11.25m },
                ],
            },
            new SalesOrder
            {
                Number = "SO-1043",
                Customer = contoso,
                Status = OrderStatus.Submitted,
                OrderedAt = new DateTimeOffset(2026, 8, 10, 16, 40, 0, TimeSpan.FromHours(2)),
                ShipBy = new DateOnly(2026, 8, 18),
                Lines =
                [
                    new OrderLine { Product = torque, Quantity = 1, UnitPrice = 89.00m },
                    new OrderLine { Product = bolt, Quantity = 4, UnitPrice = 6.40m },
                ],
            },
            new SalesOrder
            {
                Number = "SO-1044",
                Customer = alpine,
                Status = OrderStatus.Draft,
                OrderedAt = new DateTimeOffset(2026, 8, 14, 9, 5, 0, TimeSpan.FromHours(1)),
                Notes = "Waiting on cable length confirmation.",
                Lines =
                [
                    new OrderLine { Product = cable, Quantity = 3, UnitPrice = 54.90m },
                ],
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Product Product(
        string sku,
        string name,
        decimal price,
        double weightKg,
        Category category,
        TimeSpan leadTime,
        string tags)
        => new()
        {
            Sku = sku,
            Name = name,
            Price = price,
            WeightKg = weightKg,
            Category = category,
            LeadTime = leadTime,
            Tags = tags,
            CreatedUtc = new DateTime(2026, 1, 8, 12, 0, 0, DateTimeKind.Utc),
        };
}
