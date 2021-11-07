using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Components
{
    public partial class CodeSample
    {
        private bool showCode { get; set; }
        private string copyButtonTitle { get; set; } = "copy";

        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public string Title { get; set; }
        [Parameter] public string Description { get; set; }
        [Parameter] public string CodeSampleContentForCopy { get; set; } = "hfhgf";
        [Parameter] public RenderFragment SampleContent { get; set; }
        [Parameter] public RenderFragment CodeSampleContent { get; set; }

        private async Task CopyCodeToClipboard()
        {
            await JSRuntime.CopyToClipboardOnClickEvent(CodeSampleContentForCopy);

            copyButtonTitle = "copied";
        }
    }
}
