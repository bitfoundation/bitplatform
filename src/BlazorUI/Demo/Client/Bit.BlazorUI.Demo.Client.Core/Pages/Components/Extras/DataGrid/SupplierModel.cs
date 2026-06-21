namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.DataGrid;

public sealed class SupplierModel
{
    public string Name { get; set; } = "";
    public List<Product> Products { get; set; } = new();
    public int ProductCount => Products.Count;
    public int TotalStock => Products.Sum(p => p.Stock);
    public decimal AveragePrice => Products.Count == 0 ? 0 : Math.Round(Products.Average(p => p.Price), 2);
}
