namespace Bit.BlazorUI.Demo.Shared.Dtos.DataGridDemo;

public class ProductDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public decimal Price { get; set; }

    public int CategoryId { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }

    public string? Name { get; set; }
}

public class CategoryOrProductDto
{
    public int? ProductId { get; set; }

    public int? CategoryId { get; set; }

    public bool IsProduct => ProductId is not null;

    public string? Name { get; set; }

    public decimal? Price { get; set; }
}
