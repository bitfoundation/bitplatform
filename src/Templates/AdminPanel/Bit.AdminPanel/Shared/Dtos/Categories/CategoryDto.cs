namespace AdminPanel.Shared.Dtos.Categories;
public class CategoryDto
{
    public int Id { get; set; }

    [Required(ErrorMessage ="Category name should not be empty")]
    [MaxLength(64)]
    public string? Name { get; set; }

    public string? Color { get; set; } = "#FFFFFF";
}
