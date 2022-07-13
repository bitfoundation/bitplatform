namespace AdminPanel.Shared.Dtos.Products;

public class ProductDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product name should not be empty")]
    [MaxLength(64)]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Product price should not be empty")]
    [Range(0, Double.MaxValue ,ErrorMessage ="Product price can't be take negative value")]
    public decimal Price { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Category name should not be empty")]
    public int? CategoryId { get; set; }

}
