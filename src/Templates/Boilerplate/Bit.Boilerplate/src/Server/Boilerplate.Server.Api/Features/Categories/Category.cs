using Boilerplate.Server.Api.Features.Products;

namespace Boilerplate.Server.Api.Features.Categories;

public partial class Category
{
    public Guid Id { get; set; }

    [Required, MaxLength(64)]
    public string? Name { get; set; }

    public string? Color { get; set; }

    public byte[] Version { get; set; } = [];

    public IList<Product> Products { get; set; } = [];
}
