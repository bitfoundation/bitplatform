namespace AdminPanel.Shared.Dtos.Products;

public class ProductDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product name can't be empty")]
    [MaxLength(64)]
    public string? Name { get; set; }

    [Required(ErrorMessage ="Price can't be empty")]
    [Range(0, Double.MaxValue ,ErrorMessage ="Price can't be take negative value")]
    public decimal Price { get; set; }

    [MaxLength(512)]
    public string? Desc { get; set; }

    [Required(ErrorMessage = "Category can't be empty.")]
    public int? CategoryId { get; set; }

}
