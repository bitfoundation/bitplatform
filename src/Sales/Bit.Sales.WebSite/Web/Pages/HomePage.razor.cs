using System.Reflection;
using Case = Bit.Sales.WebSite.App.Models.Case;

namespace Bit.Sales.WebSite.App.Pages
{
    public partial class HomePage
    {
        private Case[] Cases { get; set; } = Array.Empty<Case>();

        protected override async Task OnInitAsync()
        {
            Cases = await System.Text.Json.JsonSerializer.DeserializeAsync<Case[]>(Assembly.Load("Bit.Sales.WebSite.App").GetManifestResourceStream("Bit.Sales.WebSite.App.Data.Cases.json"));
            await base.OnInitAsync();
        }
    }
}
