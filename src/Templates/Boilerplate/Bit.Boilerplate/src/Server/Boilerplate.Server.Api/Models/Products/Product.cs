using Boilerplate.Server.Api.Models.Categories;

namespace Boilerplate.Server.Api.Models.Products;

public partial class Product
{
    public Guid Id { get; set; }

    [Required, MaxLength(64)]
    public string? Name { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public Guid CategoryId { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = [];

    public string? ImageFileName { get; set; }
}
