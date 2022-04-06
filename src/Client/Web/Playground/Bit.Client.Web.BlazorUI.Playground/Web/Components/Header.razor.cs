﻿using System;
using Bit.Client.Web.BlazorUI.Playground.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Components
{
    public partial class Header
    {
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Inject] public NavManuService NavManuService { get; set; }
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

        private void ToggleMenu()
        {
            NavManuService.ToggleMenu();
        }
    }
}
