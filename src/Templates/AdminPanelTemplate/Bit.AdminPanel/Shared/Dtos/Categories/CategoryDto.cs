namespace AdminPanel.Shared.Dtos.Categories;
public class CategoryDto
{
    public int Id { get; set; }

    [Required(ErrorMessage ="Category name can't be empty")]
    [MaxLength(64)]
    public string? Name { get; set; }

    public string? Color { get; set; } = "#FFFFFF";
}
