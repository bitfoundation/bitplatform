namespace AdminPanelTemplate.Shared.Dtos.Categories;
public class CategoryDto
{
    public int Id { get; set; }
    
    [MaxLength(64)]
    public string? Name { get; set; }

    public string? Color { get; set; }
}
