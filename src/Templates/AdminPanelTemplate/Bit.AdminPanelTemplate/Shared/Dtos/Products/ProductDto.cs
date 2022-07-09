namespace AdminPanelTemplate.Shared.Dtos.Products;

public class ProductDto
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string? Name { get; set; }

    [Required]
    public decimal Price { get; set; }

    [MaxLength(512)]
    public string? Desc { get; set; }

    [Required(ErrorMessage = "The Category field is required.")]
    public int? CategoryId { get; set; }

}
