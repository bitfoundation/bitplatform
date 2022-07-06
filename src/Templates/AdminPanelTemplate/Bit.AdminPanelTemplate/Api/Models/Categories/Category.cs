using System.Diagnostics.CodeAnalysis;
using AdminPanelTemplate.Api.Models.Products;

namespace AdminPanelTemplate.Api.Models.Categories;

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
