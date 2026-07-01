namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.DataGrid;

public enum Category
{
    Electronics,
    Books,
    Clothing,
    Home,
    Toys,
    Sports,
    Grocery
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Category Category { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public double Rating { get; set; }
    public bool Discontinued { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string Supplier { get; set; } = "";

    public Product Clone() => (Product)MemberwiseClone();
}
