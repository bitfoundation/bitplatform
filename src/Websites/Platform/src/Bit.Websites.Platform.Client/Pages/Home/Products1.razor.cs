
namespace Bit.Websites.Platform.Client.Pages.Home;

public partial class Products1
{
    private class BitProduct
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Url { get; set; } = default!;
    }

    private List<BitProduct> _allProducts =
    [
        new() { Name = "Boilerplate", Description = "Feature-rich .NET project template", Url = Urls.Templates },
        new() { Name = "Butil", Description = "Blazor Utilities for JavaScript", Url = Urls.Butil },
        new() { Name = "Bswup", Description = "Blazor Service Worker Update Progress", Url = Urls.Bswup },
        new() { Name = "Besql", Description = "Blazor Entity Framework Sqlite", Url = Urls.Besql },
        new() { Name = "BlazorUI", Description = "Native Blazor UI components", Url = Urls.BlazorUI },
    ];

    private BitProduct _selectedProduct = default!;

    protected override Task OnInitAsync()
    {
        _selectedProduct = _allProducts[0];
        return base.OnInitAsync();
    }


}
