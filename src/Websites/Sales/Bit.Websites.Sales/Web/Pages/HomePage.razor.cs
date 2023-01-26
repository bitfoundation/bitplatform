using System.Reflection;
using Case = Bit.Websites.Sales.Web.Models.Case;

namespace Bit.Websites.Sales.Web.Pages;

public partial class HomePage
{
    private Case[] Cases { get; set; } = Array.Empty<Case>();
    private BitCarousel? _carouselRef;

    protected override async Task OnInitAsync()
    {
        Cases = await System.Text.Json.JsonSerializer.DeserializeAsync<Case[]>(Assembly.Load("Bit.Websites.Sales.Web").GetManifestResourceStream("Bit.Websites.Sales.Web.Data.Cases.json"));
        await base.OnInitAsync();
    }
}
