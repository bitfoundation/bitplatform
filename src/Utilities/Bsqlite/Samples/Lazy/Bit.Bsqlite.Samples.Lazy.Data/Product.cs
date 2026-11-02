namespace Bit.Bsqlite.Samples.Lazy.Data;

/// <summary>The one entity of the sample. It derives from nothing of EF Core's, so any assembly could hold it.</summary>
public sealed class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
