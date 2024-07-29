using Boilerplate.Server.Api.Models.Categories;

namespace Boilerplate.Server.Api.Models.Products;

public class Product
{
    public Guid Id { get; set; }

    [Required, MaxLength(64)]
    public string? Name { get; set; }

    [Column(TypeName = "money")]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public Guid CategoryId { get; set; }
}
