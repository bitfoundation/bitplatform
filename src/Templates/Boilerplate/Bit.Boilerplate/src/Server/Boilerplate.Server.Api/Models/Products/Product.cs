//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Categories;

namespace Boilerplate.Server.Api.Models.Products;

public partial class Product
{
    public Guid Id { get; set; }

    /// <summary>
    /// The product's ShortId is used to create a more human-friendly URL.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int ShortId { get; set; }
        //#if (database != "PostgreSQL" && database != "SqlServer")
        = Environment.TickCount; // Using a database sequence for this is recommended.
        //#endif

    [Required, MaxLength(64)]
    public string? Name { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [MaxLength(4096)]
    public string? DescriptionHTML { get; set; }

    [MaxLength(4096)]
    public string? DescriptionText { get; set; }

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public Guid CategoryId { get; set; }

    public byte[] Version { get; set; } = [];

    public bool HasPrimaryImage { get; set; } = false;

    public string? PrimaryImageAltText { get; set; }

    //#if (database == "PostgreSQL")
    public Pgvector.Vector? Embedding { get; set; }
    //#elif (database == "SqlServer")
    //#if (IsInsideProjectTemplate == true)
    /*
    //#endif
    public Microsoft.Data.SqlTypes.SqlVector<float>? Embedding { get; set; }
    //#if (IsInsideProjectTemplate == true)
    */
    //#endif
    //#endif
}
