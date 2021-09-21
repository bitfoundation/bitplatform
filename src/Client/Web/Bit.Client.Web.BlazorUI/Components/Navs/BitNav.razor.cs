using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitNav : IDisposable
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [Inject] private NavigationManager NavigationManager { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

#pragma warning disable CA1823 // Avoid unused private fields
        private bool SelectedKeyHasBeenSet;
#pragma warning restore CA1823 // Avoid unused private fields

        private string? selectedKey;

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

                if (selectedKey is null) return;

                var currentNavLinkItem = NavLinkItems.SelectMany(item => item.Links)
                                                     .FirstOrDefault(item => (item.Key?.Contains(selectedKey, StringComparison.Ordinal)) ?? false);

                if (currentNavLinkItem is null) return;

                var currentUrl = currentNavLinkItem.Url;

                if (currentUrl.HasNoValue()) return;

                NavigationManager.NavigateTo(currentUrl!);
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
        [Parameter] public IEnumerable<BitNavLinkItem> NavLinkItems { get; set; } = new List<BitNavLinkItem>();

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

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsOnTop
                                            ? $"{RootElementClass}-top"
                                            : $"{RootElementClass}-no-top");
        }

        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += OnLocationChanged;
            selectedKey ??= InitialSelectedKey;

            await base.OnInitializedAsync();
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
        {
            var currentPage = NavigationManager.Uri.Replace(NavigationManager.BaseUri, string.Empty, StringComparison.Ordinal);

            var currentItem = NavLinkItems.SelectMany(item => item.Links).FirstOrDefault(CreateComparer(currentPage));
            string currentPageKey = currentItem?.Key ?? string.Empty;

            if (currentPageKey.HasNoValue()) return;

            SelectedKey = currentPageKey;

            StateHasChanged();

            Func<BitNavLinkItem, bool> CreateComparer(string currentPage)
            {
                return item => (item.Url ?? "").ToLower(Thread.CurrentThread.CurrentCulture)
                                               .Contains(currentPage.ToLower(Thread.CurrentThread.CurrentCulture), StringComparison.Ordinal);
            }
        }

        private async Task OnLinkExpand(BitNavLinkItem navLinkItem)
        {
            if (IsEnabled is false || navLinkItem.IsEnabled is false || navLinkItem.Links.Any() is false) return;

            navLinkItem.IsExpanded = !navLinkItem.IsExpanded;

            await OnLinkExpandClick.InvokeAsync(navLinkItem);
        }

        private async Task HandleLinkClick(BitNavLinkItem navLinkItem)
        {
            if (IsEnabled is false || navLinkItem.IsEnabled is false) return;

            await OnLinkClick.InvokeAsync(navLinkItem);

            if (navLinkItem.Url.HasNoValue() && navLinkItem.Links.Any())
            {
                await OnLinkExpand(navLinkItem);
            }
        }

        private void HandleClick(BitNavLinkItem navLinkItem)
        {
            if (IsEnabled is false || navLinkItem.IsEnabled is false) return;

            navLinkItem.OnClick?.Invoke(navLinkItem);
        }


        private string GetLinkClass(BitNavLinkItem navLinkItem)
        {
            var enabledClass = navLinkItem.IsEnabled ? "enabled" : "disabled";
            var hasUrlClass = navLinkItem.Url.HasNoValue() ? "nourl" : "hasurl";

            var mainStyle = $"bit-nav-link-{enabledClass}-{hasUrlClass}-{VisualClassRegistrar()}";

            var selectedClass = navLinkItem.Key == SelectedKey
                                    ? $"bit-nav-selected-{VisualClassRegistrar()}"
                                    : string.Empty;

            var hasIcon = navLinkItem.Icon.HasNoValue()
                            ? string.Empty
                            : $"bit-nav-with-icon-{VisualClassRegistrar()}";

            var isGroup = navLinkItem.IsGroup
                            ? $"bit-nav-isgroup-{VisualClassRegistrar()}"
                            : string.Empty;

            return $"{mainStyle} {selectedClass} {hasIcon} {isGroup}";
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                NavigationManager.LocationChanged -= OnLocationChanged;
            }
        }
    }
}
