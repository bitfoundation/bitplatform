using System.Diagnostics.CodeAnalysis;
using BlazorWeb.Server.Models.Categories;

namespace BlazorWeb.Server.Models.Products;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string? Name { get; set; }

    [Column(TypeName = "money")]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    [NotNull]
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public int CategoryId { get; set; }
}
