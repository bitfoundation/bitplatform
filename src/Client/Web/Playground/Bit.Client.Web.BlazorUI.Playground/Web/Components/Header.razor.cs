using System;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Components
{
    public partial class Header
    {
        [Inject]
        public NavigationManager NavigationManager { get; set; }
        public string CurrentUrl { get; set; }

        protected override void OnInitialized()
        {
            CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
            base.OnInitialized();
        }
    }
}
