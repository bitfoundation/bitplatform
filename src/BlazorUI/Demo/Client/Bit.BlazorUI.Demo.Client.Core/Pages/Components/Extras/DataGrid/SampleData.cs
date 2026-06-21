namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.DataGrid;

public static class SampleData
{
    private static readonly string[] Adjectives =
        { "Ultra", "Premium", "Eco", "Smart", "Classic", "Pro", "Mini", "Mega", "Vintage", "Modern", "Deluxe", "Compact" };
    private static readonly string[] Nouns =
        { "Widget", "Gadget", "Speaker", "Notebook", "Jacket", "Lamp", "Blender", "Drone", "Backpack", "Sneaker", "Camera", "Mug" };
    private static readonly string[] Suppliers =
        { "Acme Corp", "Globex", "Initech", "Umbrella", "Soylent", "Stark Industries", "Wayne Enterprises", "Wonka Inc" };

    /// <summary>Deterministic generator so demos are reproducible.</summary>
    public static List<Product> Generate(int count, int seed = 42)
    {
        var rng = new Random(seed);
        var categories = Enum.GetValues<Category>();
        var list = new List<Product>(count);
        // Fixed reference date keeps the generated data deterministic regardless of when it runs.
        var referenceDate = new DateTime(2024, 1, 1);
        for (int i = 1; i <= count; i++)
        {
            list.Add(new Product
            {
                Id = i,
                Name = $"{Adjectives[rng.Next(Adjectives.Length)]} {Nouns[rng.Next(Nouns.Length)]} {rng.Next(100, 999)}",
                Category = categories[rng.Next(categories.Length)],
                Price = Math.Round((decimal)(rng.NextDouble() * 990 + 5), 2),
                Stock = rng.Next(0, 500),
                Rating = Math.Round(rng.NextDouble() * 4 + 1, 1),
                Discontinued = rng.Next(0, 5) == 0,
                ReleaseDate = referenceDate.AddDays(-rng.Next(0, 2000)),
                Supplier = Suppliers[rng.Next(Suppliers.Length)]
            });
        }
        return list;
    }
}
