using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Platform.WebSite.Web.Components
{
    public partial class GettingStarted
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }

        public async Task OnScrollTo(string id)
        {
            await JSRuntime.ScrollToElement(id);
        }

        public string Code1 = "<section class=\"getting-started-list-section\">\r\n    <div class=\"item\" onclick=\"@OnScrollTo(\"step-number-1\")\">Instal</div>\r\n    <div class=\"item selected-item\" onclick=\"@OnScrollTo(\"step-number-2\")\">Development prerequisites</div>\r\n    <div class=\"item\" onclick=\"@OnScrollTo(\"step-number-3\")\">Prepare The Project</div>\r\n</section>";

        public string Code2 = "public async Task OnScrollTo(string id)\r\n{\r\n     await JSRuntime.ScrollToElement(id);\r\n}";
    }
}
