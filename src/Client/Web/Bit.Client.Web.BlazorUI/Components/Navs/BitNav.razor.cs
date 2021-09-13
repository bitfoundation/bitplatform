using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitNav : IDisposable
    {
        private string? selectedKey;
        [Inject] private NavigationManager navigationManager { get; set; }
        private bool SelectedKeyHasBeenSet;

        /// <summary>
        /// (Optional) The key of the nav item initially selected.
        /// </summary>
        [Parameter] public string? InitialSelectedKey { get; set; }

        /// <summary>
        /// The key of the nav item selected by caller
        /// </summary>
        [Parameter]
        public string? SelectedKey
        {
            get => selectedKey;
            set
            {
                if (value == selectedKey) return;
                selectedKey = value;
                SelectedKeyChanged.InvokeAsync(selectedKey);

                var currentUrl = NavLinkItems.Where(nli => nli.Links != null)
                    .SelectMany(sli => sli.Links)
                    .FirstOrDefault(sli => sli.Key.Contains(selectedKey))?.Key ?? string.Empty;

                if (!string.IsNullOrEmpty(currentUrl) && navigationManager.Uri.Contains(currentUrl))
                {
                    navigationManager.NavigateTo(currentUrl);
                }
            }
        }

        [Parameter] public EventCallback<string> SelectedKeyChanged { get; set; }

        /// <summary>
        /// Indicates whether the navigation component renders on top of other content in the UI
        /// </summary>
        [Parameter] public bool IsOnTop { get; set; }

        /// <summary>
        /// A collection of link items to display in the navigation bar
        /// </summary>
        [Parameter] public ICollection<BitNavLinkItem> NavLinkItems { get; set; } = new List<BitNavLinkItem>();

        /// <summary>
        /// Function callback invoked when the chevron on a link is clicked
        /// </summary>
        [Parameter] public EventCallback<BitNavLinkItem> OnLinkExpandClick { get; set; }

        /// <summary>
        /// Function callback invoked when a link in the navigation is clicked
        /// </summary>
        [Parameter] public EventCallback<BitNavLinkItem> OnLinkClick { get; set; }

        /// <summary>
        /// The template of the header for each nav item, which is a generic RenderFramgment that accepts a BitNavLinItem as input
        /// </summary>
        [Parameter] public RenderFragment<BitNavLinkItem>? HeaderTemplate { get; set; }

        protected override string RootElementClass => "bit-nav";


        protected override async Task OnInitializedAsync()
        {
            navigationManager.LocationChanged += OnLocationChanged;
            selectedKey = selectedKey ?? InitialSelectedKey;

            await base.OnInitializedAsync();
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs args)
        {
            var currentPage = navigationManager.Uri.Replace(navigationManager.BaseUri, string.Empty);

            string currentPageKey = NavLinkItems.Where(nli => nli.Links != null)
                .SelectMany(nli => nli.Links)
                .FirstOrDefault(l => l.Url.ToLower()
                    .Contains(currentPage.ToLower())
                    )?.Key ?? string.Empty;


            if (string.IsNullOrEmpty(currentPageKey)) return;

            SelectedKey = currentPageKey;
            StateHasChanged();
        }

        protected async Task HandleOnClick(BitNavLinkItem navLink, bool toggle = true)
        {
            if (IsEnabled is false || navLink.Disabled) return;

            if (navLink.Links?.Any() ?? false && toggle)
            {
                navLink.IsExpanded = !navLink.IsExpanded;
            }

            await OnLinkExpandClick.InvokeAsync(navLink);
            await navLink.OnClick.InvokeAsync();
            await OnLinkClick.InvokeAsync(navLink);
        }

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsOnTop
                                            ? $"{RootElementClass}-top"
                                            : $"{RootElementClass}-no-top");
        }

        private string GetLinkClass(BitNavLinkItem navLink)
        {
            var enabledClass = navLink.Disabled ? "disabled" : "enabled";
            var hasUrlClass = navLink.Url.HasNoValue() ? "nourl" : "hasurl";

            var mainStyle = $"bit-nav-link-{enabledClass}-{hasUrlClass}-{VisualClassRegistrar()}";
            var selectedClass = navLink.Key == SelectedKey ? $"bit-nav-selected-{VisualClassRegistrar()}" : string.Empty;
            var hasIcon = navLink.Icon.HasNoValue()
                            ? $"bit-nav-has-not-icon-{VisualClassRegistrar()}"
                            : $"bit-nav-has-icon-{VisualClassRegistrar()}";
            var isGroup = navLink.IsGroup ? $"bit-nav-isgroup-{VisualClassRegistrar()}" : string.Empty;

            return $"{mainStyle} {selectedClass} {hasIcon} {isGroup}";
        }

        public void Dispose()
        {
            navigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
