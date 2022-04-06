using System;
using Bit.Client.Web.BlazorUI.Playground.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Components
{
    public partial class Header
    {
        private string CurrentUrl = string.Empty;
        private bool IsHeaderMenuOpen;

        [Inject] public NavigationManager NavigationManager { get; set; }
        [Inject] public NavManuService NavManuService { get; set; }

        protected override void OnInitialized()
        {
            CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
            NavigationManager.LocationChanged += OnLocationChanged;

            base.OnInitialized();
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs args)
        {
            CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
            StateHasChanged();
        }

        private void ToggleMenu()
        {
            NavManuService.ToggleMenu();
        }
        private string GetActiveRouteName()
        {
            switch (CurrentUrl)
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
            IsHeaderMenuOpen = !IsHeaderMenuOpen;
        }
    }
}
