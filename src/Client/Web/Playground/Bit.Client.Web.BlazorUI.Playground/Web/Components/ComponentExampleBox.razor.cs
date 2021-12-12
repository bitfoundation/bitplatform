using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Components
{
    public partial class ComponentExampleBox
    {
        private bool showCode { get; set; }
        private string exampleId = Guid.NewGuid().ToString();

        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        [Parameter] public string Title { get; set; }
        [Parameter] public string Description { get; set; }
        [Parameter] public string CodeSampleContentForCopy { get; set; }
        [Parameter] public RenderFragment SampleContent { get; set; }
        [Parameter] public RenderFragment CodeSampleContent { get; set; }

        private async Task CopyCodeToClipboard()
        {
            await JSRuntime.CopyToClipboard(CodeSampleContentForCopy);
        }

        private async Task CopyLinkToClipboard()
        {
            var currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
            var exampleUrl = $"{currentUrl}#{exampleId}";
            await JSRuntime.CopyToClipboard(exampleUrl);
        }
    }
}
