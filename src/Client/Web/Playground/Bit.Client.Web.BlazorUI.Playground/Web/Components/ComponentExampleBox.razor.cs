﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Components
{
    public partial class ComponentExampleBox
    {
        private bool showCode;

        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        [Parameter] public string Title { get; set; }
        [Parameter] public string ExampleId { get; set; }
        [Parameter] public string ExampleSourceCodeForCopy { get; set; }
        [Parameter] public RenderFragment ExamplePreview { get; set; }
        [Parameter] public RenderFragment ExampleSourceCode { get; set; }

        private async Task CopyCodeToClipboard()
        {
            await JSRuntime.CopyToClipboard(ExampleSourceCodeForCopy);
        }

        private async Task CopyLinkToClipboard()
        {
            var currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
            currentUrl = currentUrl.Contains("#") ? currentUrl.Substring(0, currentUrl.IndexOf("#")) : currentUrl;
            var exampleUrl = $"{currentUrl}#{ExampleId}";
            await JSRuntime.CopyToClipboard(exampleUrl);
        }
    }
}
