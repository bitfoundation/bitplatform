﻿namespace Bit.Websites.Platform.Client.Pages.Home;

public partial class Products
{
    private ProductAccordionItem[] visibleProducts = [];

    /// <summary>
    /// Which products to list: the home page shows only the core ones, the products page shows every group.
    /// </summary>
    [Parameter] public ProductGroup Group { get; set; }

    protected override async Task OnParamsSetAsync()
    {
        visibleProducts = [.. products.Where(p => p.IsCore == (Group is ProductGroup.Core))];

        await base.OnParamsSetAsync();
    }

    private sealed class ProductAccordionItem
    {
        public required string Title { get; init; }
        public required string Subtitle { get; init; }
        public required string LinkUrl { get; init; }
        public bool External { get; init; }
        public bool IsCore { get; init; }
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
