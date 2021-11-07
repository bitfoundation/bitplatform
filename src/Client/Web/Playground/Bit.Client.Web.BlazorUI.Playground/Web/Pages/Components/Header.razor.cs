using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components
{
    public partial class Header
    {
        private string homeLinkClass;
        private string demoLinkClass;
        private string getstartedLinkClass;

        [Parameter] public PageType ActivePage { get; set; }

        protected override void OnInitialized()
        {
            string activeClassName = "active";

            switch (ActivePage)
            {
                case PageType.Demo:
                {
                    demoLinkClass = activeClassName;
                    break;
                }
                case PageType.GetStarted:
                {
                    getstartedLinkClass = activeClassName;
                    break;
                }
                default:
                {
                    homeLinkClass = activeClassName;
                    break;
                }
            }

            base.OnInitialized();
        }
    }

    public enum PageType
    {
        Home,
        Demo,
        GetStarted
    }
}
