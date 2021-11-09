using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components
{
    public partial class Header
    {
        private string currentUri;

        [Inject] public NavigationManager NavigationManager { get; set; }

        protected override void OnInitialized()
        {
            currentUri = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

            base.OnInitialized();
        }
    }
}
