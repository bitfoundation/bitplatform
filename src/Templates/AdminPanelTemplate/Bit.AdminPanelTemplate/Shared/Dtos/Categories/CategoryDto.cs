namespace AdminPanelTemplate.Shared.Dtos.Categories;
public class CategoryDto
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string? Name { get; set; }
    public string? Color { get; set; } = "#FFFFFF";

    //[NotMapped]
    //public string? Color_Rgb { get; set; } = "rgb(0,0,255)";

    //[NotMapped]
    //public string? Color_Alpha { get; set; } = "1";

   

}
