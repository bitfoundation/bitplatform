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
    }
}
