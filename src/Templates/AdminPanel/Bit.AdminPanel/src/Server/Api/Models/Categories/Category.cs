using AdminPanel.Server.Api.Models.Products;

namespace AdminPanel.Server.Api.Models.Categories;

public class Category
{
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string? Name { get; set; }

    public string? Color { get; set; }

    public IList<Product>? Products { get; set; }
}
