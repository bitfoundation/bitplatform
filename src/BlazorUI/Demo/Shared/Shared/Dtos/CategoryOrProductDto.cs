namespace Bit.BlazorUI.Demo.Shared.Dtos;
public class CategoryOrProductDto
{
    public int? ProductId { get; set; }

    public int? CategoryId { get; set; }

    public bool IsProduct => ProductId is not null;

    public string? Name { get; set; }

    public decimal? Price { get; set; }
}
