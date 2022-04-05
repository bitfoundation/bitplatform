using System;
using Bit.Client.Web.BlazorUI.Playground.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Components
{
    public partial class Header
    {
        private string currentUrl = string.Empty;
        private bool isHeaderMenuOpen;

        [Inject] public NavigationManager NavigationManager { get; set; }
        [Inject] public NavManuService NavManuService { get; set; }

        protected override void OnInitialized()
        {
            currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
            NavigationManager.LocationChanged += OnLocationChanged;

            base.OnInitialized();
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs args)
        {
            currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
            StateHasChanged();
        }

        private void ToggleMenu()
        {
            NavManuService.ToggleMenu();
        }
        private string GetActiveRouteName()
        {
            switch (currentUrl)
            {
                case "/":
                    return "Home";
                case "/components/overview":
                    return "Demo";
                case "/get-started":
                    return "Get Started";
                case "/icons":
                    return "Iconography";
                default:
                    return "";
            }
        }

        private void ToggleHeaderMenu()
        {
            isHeaderMenuOpen = !isHeaderMenuOpen;
        }
    }
}
