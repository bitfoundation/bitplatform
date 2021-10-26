using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Components
{
    public partial class CodeSample
    {
        private ElementReference CodeReference { get; set; }
        private bool ShowCode { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public string Title { get; set; }
        [Parameter] public string Description { get; set; }
        [Parameter] public RenderFragment SampleContent { get; set; }
        [Parameter] public RenderFragment CodeSampleContent { get; set; }

        private async Task HandleOnCopyClick()
        {
            await JSRuntime.InvokeVoidAsync("copyCodeSample", CodeReference);
        }
    }
}
