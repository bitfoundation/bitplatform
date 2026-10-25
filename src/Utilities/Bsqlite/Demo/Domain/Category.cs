namespace Bit.Bsqlite.Demo.Domain;

public sealed class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = "#0f766e";

    public List<Product> Products { get; } = [];
}
