namespace AdminPanel.Shared.Dtos.Products;

[DtoResourceType(typeof(AppStrings))]
public partial class ProductDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [MaxLength(64, ErrorMessage = nameof(AppStrings.MaxLengthAttribute_InvalidMaxLength))]
    [Display(Name = nameof(AppStrings.Name))]
    public string? Name { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Range(0, double.MaxValue, ErrorMessage = nameof(AppStrings.RangeAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Price))]
    public decimal Price { get; set; }

    [MaxLength(512, ErrorMessage = nameof(AppStrings.MaxLengthAttribute_InvalidMaxLength))]
    [Display(Name = nameof(AppStrings.Description))]
    public string? Description { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Category))]
    public Guid? CategoryId { get; set; }

    [Display(Name = nameof(AppStrings.Category))]
    public string? CategoryName { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = [];
}
