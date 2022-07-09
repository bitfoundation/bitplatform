using System.Reflection;
using Case = Bit.Sales.WebSite.App.Models.Case;

namespace Bit.Sales.WebSite.App.Pages
{
    public partial class ShowCasePage
    {
        public Case[] Cases { get; set; } = Array.Empty<Case>();

        protected override async Task OnInitializedAsync()
        {
            Cases = await System.Text.Json.JsonSerializer.DeserializeAsync<Case[]>(Assembly.Load("Bit.Sales.WebSite.App").GetManifestResourceStream("Bit.Sales.WebSite.App.Data.Cases.json"));
            await base.OnInitializedAsync();
        }
    }
}
