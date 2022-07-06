namespace AdminPanelTemplate.Shared.Dtos.Products;

public class ProductDto
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string? Name { get; set; }

    public decimal Price { get; set; }

    [MaxLength(512)]
    public string? Desc { get; set; }

    public int? CategoryId { get; set; }

}
