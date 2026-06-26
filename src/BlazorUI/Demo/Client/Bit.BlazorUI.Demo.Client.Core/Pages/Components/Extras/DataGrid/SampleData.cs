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
        => GenerateCore(count, seed, Adjectives, Nouns, Suppliers);

    private static readonly string[] PersianAdjectives =
        { "فوق‌العاده", "ممتاز", "اقتصادی", "هوشمند", "کلاسیک", "حرفه‌ای", "کوچک", "بزرگ", "قدیمی", "مدرن", "لوکس", "فشرده" };
    private static readonly string[] PersianNouns =
        { "ویجت", "گجت", "بلندگو", "دفترچه", "ژاکت", "چراغ", "مخلوط‌کن", "پهپاد", "کوله‌پشتی", "کفش", "دوربین", "لیوان" };
    private static readonly string[] PersianSuppliers =
        { "شرکت آلفا", "گلوبکس", "اینیتک", "آمبرلا", "سویلنت", "صنایع استارک", "شرکت وین", "ونکا" };

    /// <summary>Deterministic generator that produces Persian sample data for RTL demos.</summary>
    public static List<Product> GeneratePersian(int count, int seed = 42)
        => GenerateCore(count, seed, PersianAdjectives, PersianNouns, PersianSuppliers);

    /// <summary>
    /// Shared, deterministic product generator. The fixed reference date and seeded RNG keep the
    /// generated data reproducible regardless of when (or in which locale) it runs.
    /// </summary>
    private static List<Product> GenerateCore(int count, int seed, string[] adjectives, string[] nouns, string[] suppliers)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must not be negative.");

        var rng = new Random(seed);
        var categories = Enum.GetValues<Category>();
        var list = new List<Product>(count);
        var referenceDate = new DateTime(2024, 1, 1);
        for (int i = 1; i <= count; i++)
        {
            list.Add(new Product
            {
                Id = i,
                Name = $"{adjectives[rng.Next(adjectives.Length)]} {nouns[rng.Next(nouns.Length)]} {rng.Next(100, 999)}",
                Category = categories[rng.Next(categories.Length)],
                Price = Math.Round((decimal)(rng.NextDouble() * 990 + 5), 2),
                Stock = rng.Next(0, 500),
                Rating = Math.Round(rng.NextDouble() * 4 + 1, 1),
                Discontinued = rng.Next(0, 5) == 0,
                ReleaseDate = referenceDate.AddDays(-rng.Next(0, 2000)),
                Supplier = suppliers[rng.Next(suppliers.Length)]
            });
        }
        return list;
    }
}
