namespace Bit.Websites.Platform.Client.Pages.Home;

public partial class Products
{
    private ProductGroup? builtGroup;
    private ProductAccordionItem[] visibleProducts = [];

    /// <summary>
    /// Which products to list: the home page shows only the core ones, the products page shows every group.
    /// </summary>
    [Parameter] public ProductGroup Group { get; set; }

    protected override async Task OnParamsSetAsync()
    {
        // Only the requested group's items are built, and only when the group changes: every parent re-render
        // sets the parameters again, and rebuilding then would also collapse the accordions the user expanded.
        if (builtGroup != Group)
        {
            builtGroup = Group;

            var (catalog, accordions) = Group switch
            {
                ProductGroup.Core => (ProductCatalog.Main, CreateCoreProducts()),
                ProductGroup.Utilities => (ProductCatalog.Utilities, CreateUtilityProducts()),
                _ => throw new ArgumentOutOfRangeException(nameof(Group), Group, "Unknown product group.")
            };

            // ProductCatalog decides which products are listed, in which order, and where they link, so this
            // list can never disagree with the header Products menu and the footer; the accordions only add
            // the long-form copy, matched to their catalog entry by URL.
            var accordionsByUrl = accordions.ToDictionary(a => a.LinkUrl, StringComparer.OrdinalIgnoreCase);
            visibleProducts = catalog.Select(product =>
            {
                var item = accordionsByUrl.GetValueOrDefault(product.Url) ?? CreateFallbackProduct(product);
                item.Product = product;
                return item;
            }).ToArray();
        }

        await base.OnParamsSetAsync();
    }

    private sealed class ProductAccordionItem
    {
        public required string Title { get; init; }
        public required string Subtitle { get; init; }
        /// <summary>
        /// Matches the item to its ProductCatalog entry; the rendered link comes from <see cref="Product"/>.
        /// </summary>
        public required string LinkUrl { get; init; }
        public ProductLink Product { get; set; } = default!;
        public bool IsExpanded { get; set; }
        public required RenderFragment Body { get; init; }
    }
}

public enum ProductGroup
{
    /// <summary>
    /// The core products: Boilerplate, BlazorUI, Butil, Bswup and Brouter.
    /// </summary>
    Core,

    /// <summary>
    /// The focused Blazor libraries under src/Utilities.
    /// </summary>
    Utilities
}
