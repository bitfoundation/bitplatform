using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Shared
{
    public partial class MainLayout : IDisposable
    {
        [Inject]
        public NavigationManager NavigationManager { get; set; }
        public string CurrentUrl { get; set; }

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

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
