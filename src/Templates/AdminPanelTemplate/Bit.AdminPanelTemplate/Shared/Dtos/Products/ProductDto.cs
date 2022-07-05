namespace AdminPanelTemplate.Shared.Dtos.Products;

public class ProductDto
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string? Name { get; set; }

    [MaxLength(128)]
    public string? QuantityPerUnit { get; set; }
    
    public decimal UnitPrice { get; set; }

    [MaxLength(512)]
    public string? Desc { get; set; }

    public int? CateggoryId { get; set; }

}
