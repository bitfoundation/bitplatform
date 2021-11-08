using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components
{
    public partial class Header
    {
        private readonly string demoPath = "components";
        private readonly string getStartedPath = "get-started";

        private string currentUri;

        [Inject] public NavigationManager NavigationManager { get; set; }

        protected override void OnInitialized()
        {
            currentUri = NavigationManager.Uri;

            base.OnInitialized();
        }
    }
}
