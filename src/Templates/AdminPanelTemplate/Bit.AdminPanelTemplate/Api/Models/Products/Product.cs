using System.Diagnostics.CodeAnalysis;
using AdminPanelTemplate.Api.Models.Categories;

namespace AdminPanelTemplate.Api.Models.Products;

[Table("Products")]
public class Product
{
    public int Id { get; set; }

    [NotNull]
    [MaxLength(64)]
    public string? Name { get; set; }

    [MaxLength(128)]
    public string? QuantityPerUnit { get; set; }
    
    [Column(TypeName = "money")]
    public decimal UnitPrice { get; set; }

    [MaxLength(512)]
    public string? Desc { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public int? CategoryId { get; set; }

}
