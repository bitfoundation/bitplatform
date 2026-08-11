namespace Bit.Websites.Platform.Client.Pages.Home;

public partial class Products
{
    private sealed class ProductAccordionItem
    {
        public required string Title { get; init; }
        public required string Subtitle { get; init; }
        public required string LinkUrl { get; init; }
        public bool External { get; init; }
        public bool IsExpanded { get; set; }
        public required RenderFragment Body { get; init; }
    }
}
