namespace Bit.BevTools.Samples.Core;

public sealed record Product(int Id, string Name, decimal Price);

public static class SampleCatalog
{
    public static readonly IReadOnlyList<Product> Products =
    [
        new(1, "Keyboard", 49.90m),
        new(2, "Headset", 89.00m),
        new(3, "Monitor", 219.00m),
        new(4, "Notebook", 4.50m),
    ];

    public static Product? Find(int id) => Products.FirstOrDefault(p => p.Id == id);
}
