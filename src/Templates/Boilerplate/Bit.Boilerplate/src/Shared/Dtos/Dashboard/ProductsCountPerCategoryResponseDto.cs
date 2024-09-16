namespace Boilerplate.Shared.Dtos.Dashboard;

public partial class ProductsCountPerCategoryResponseDto
{
    public string? CategoryName { get; set; }

    public string? CategoryColor { get; set; }

    public int ProductCount { get; set; }
}
