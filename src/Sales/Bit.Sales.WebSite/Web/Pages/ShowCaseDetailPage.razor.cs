using System.Reflection;
using Case = Bit.Sales.WebSite.App.Models.Case;

namespace Bit.Sales.WebSite.App.Pages
{
    public partial class ShowCaseDetailPage
    {
        [Parameter]
        public string CaseKey { get; set; }
        public Case ShowCase { get; set; }

        protected override async Task OnInitAsync()
        {
            var cases = new List<Case>();
            cases = await System.Text.Json.JsonSerializer.DeserializeAsync<List<Case>>(Assembly.Load("Bit.Sales.WebSite.App").GetManifestResourceStream("Bit.Sales.WebSite.App.Data.Cases.json"));
            ShowCase = cases.Find(item => item.Key == CaseKey);
            await base.OnInitAsync();
        }

        private static Dictionary<string, string> ServiceMap = new()
        {
            ["Artificial Intelligence"] = "artificial-intelligence",
            ["UI / UX"] = "ui-ux",
            ["Support"] = "support",
            ["Mobile Development"] = "mobile-development",
            ["Web Development"] = "web-development",
            ["App Development"] = "app-development",
            ["Game Development"] = "game-development",
            ["Cloud"] = "cloud"
        };
    }
}
