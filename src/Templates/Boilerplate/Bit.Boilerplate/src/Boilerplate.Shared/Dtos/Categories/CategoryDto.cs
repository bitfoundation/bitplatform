namespace Boilerplate.Shared.Dtos.Categories;

[DtoResourceType(typeof(AppStrings))]
public class CategoryDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Name))]
    [MaxLength(64, ErrorMessage = nameof(AppStrings.MaxLengthAttribute_InvalidMaxLength))]
    public string? Name { get; set; }

    [Display(Name = nameof(AppStrings.Color))]
    public string? Color { get; set; } = "#FFFFFF";

    public int ProductsCount { get; set; }
}
