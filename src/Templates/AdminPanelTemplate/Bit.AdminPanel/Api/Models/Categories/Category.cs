using System.Diagnostics.CodeAnalysis;
using AdminPanel.Api.Models.Products;

namespace AdminPanel.Api.Models.Categories;

[Table("Categories")]
public class Category
{
    public int Id { get; set; }

    [NotNull]
    [MaxLength(64)]
    public string? Name { get; set; }

    public string? Color { get; set; }

    public IList<Product>? Products { get; set; }

}
