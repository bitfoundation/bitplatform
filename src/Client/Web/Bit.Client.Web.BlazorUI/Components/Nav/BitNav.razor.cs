using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        
        private bool SelectedKeyHasBeenSet;
        private string? selectedKey;

        /// <summary>
        /// Determines how the navigation will be handled
        /// The default value is Automatic
        /// </summary>
        [Parameter] public BitNavMode Mode { get; set; } = BitNavMode.Automatic;

        /// <summary>
        /// The way to render nav links 
        /// </summary>
        [Parameter] public BitNavRenderType RenderType { get; set; } = BitNavRenderType.Normal;

        /// <summary>
        /// (Optional) The key of the nav item initially selected in manual mode
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
                if (Mode == BitNavMode.Automatic)
                {
                    SelectedKeyChanged.InvokeAsync(selectedKey);
                    return;
                }

                selectedKey = value;
            }
        }

        [Parameter] public EventCallback<string> SelectedKeyChanged { get; set; }

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
        /// Used to customize how content inside the group header is rendered
        /// </summary>
        [Parameter] public RenderFragment<BitNavLinkItem>? HeaderTemplate { get; set; }

        /// <summary>
        /// Used to customize how content inside the link tag is rendered
        /// </summary>
        [Parameter] public RenderFragment<BitNavLinkItem>? LinkTemplate { get; set; }

        protected override string RootElementClass => "bit-nav";

        protected override async Task OnInitializedAsync()
        {
            if (Mode == BitNavMode.Automatic)
            {
                NavigationManager.LocationChanged += OnLocationChanged;
            }

            foreach (var navLink in NavLinkItems)
            {
                SetParentKeys(navLink, null);
            };

            if (RenderType == BitNavRenderType.Grouped)
            {
                foreach (var link in NavLinkItems)
                {
                    if (link.IsCollapseByDefault is not null)
                        link.IsExpanded = !link.IsCollapseByDefault.Value;
                }
            }

            var flatNavLinkItems = Flatten(NavLinkItems).ToList();
            var currrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
            selectedKey = flatNavLinkItems.FirstOrDefault(item => item.Url == currrentUrl)?.Key
                                      ?? selectedKey
                                      ?? InitialSelectedKey;

            if (SelectedKey.HasValue() is false) return;

            //To expand all the parent links of the selected item
            var selectedItem = flatNavLinkItems.Find(i => i.Key == selectedKey)!;
            if (selectedItem is not null)
                ExpandSelectedNavLinkItemParents(selectedItem);

            await base.OnInitializedAsync();
        }

        private static IEnumerable<BitNavLinkItem> Flatten(IEnumerable<BitNavLinkItem> e) => e.SelectMany(c => Flatten(c.Links)).Concat(e);

        private void SetParentKeys(BitNavLinkItem navLink, string? parentKey)
        {
            navLink.ParentKey = parentKey;
            if (navLink.Links.Any() is false) return;

            foreach (var item in navLink.Links)
            {
                SetParentKeys(item, navLink.Key);
            }
        }

        private void ExpandSelectedNavLinkItemParents(BitNavLinkItem item)
        {
            if (item.ParentKey is null) return;

            var parentItem = Flatten(NavLinkItems).ToList().Find(i => i.Key == item.ParentKey)!;
            parentItem.IsExpanded = true;

            if (parentItem.ParentKey is null) return;

            ExpandSelectedNavLinkItemParents(parentItem);
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
        {
            if (Mode == BitNavMode.Manual) return;

            var currentPage = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);

            var currentItem = Flatten(NavLinkItems).ToList().FirstOrDefault(CreateComparer(currentPage));
            string currentPageKey = currentItem?.Key ?? string.Empty;

            if (currentPageKey.HasNoValue()) return;

            selectedKey = currentPageKey;
            ExpandSelectedNavLinkItemParents(currentItem!);
            StateHasChanged();

            Func<BitNavLinkItem, bool> CreateComparer(string currentPage)
            {
                return item => (item.Url ?? "").ToLower(Thread.CurrentThread.CurrentCulture) == currentPage.ToLower(Thread.CurrentThread.CurrentCulture);
            }
        }

        private async Task HandleLinkExpand(BitNavLinkItem navLinkItem)
        {
            if (navLinkItem.IsEnabled is false || navLinkItem.Links.Any() is false) return;

            navLinkItem.IsExpanded = !navLinkItem.IsExpanded;

            await OnLinkExpandClick.InvokeAsync(navLinkItem);
        }

        private async Task HandleLinkClick(BitNavLinkItem navLinkItem)
        {
            if (navLinkItem.IsEnabled is false) return;

            if (Mode == BitNavMode.Manual && navLinkItem.Links.Any() is false)
            {
                selectedKey = navLinkItem.Key;
                await SelectedKeyChanged.InvokeAsync(selectedKey);
            }

            await OnLinkClick.InvokeAsync(navLinkItem);
            if (navLinkItem.Url.HasNoValue() && navLinkItem.Links.Any())
            {
                await HandleLinkExpand(navLinkItem);
            }
        }

        private static void HandleClick(BitNavLinkItem navLinkItem)
        {
            if (navLinkItem.IsEnabled is false) return;

            navLinkItem.OnClick?.Invoke(navLinkItem);
        }

        private async Task HandleGroupHeaderClick(BitNavLinkItem navLinkItem)
        {
            navLinkItem.OnHeaderClick?.Invoke(navLinkItem.IsExpanded);

            if (navLinkItem.Links.Any())
            {
                await HandleLinkExpand(navLinkItem);
            }
        }

        private string GetLinkClass(BitNavLinkItem navLinkItem)
        {
            var enabledClass = navLinkItem.IsEnabled ? "enabled" : "disabled";
            var hasUrlClass = navLinkItem.Url.HasNoValue() ? "nourl" : "hasurl";

            var mainStyle = $"bit-nav-link-{enabledClass}-{hasUrlClass}-{VisualClassRegistrar()}";

            var selectedClass = selectedKey.HasValue() && navLinkItem.Key == SelectedKey
                                    ? $"bit-nav-selected-{VisualClassRegistrar()}"
                                    : string.Empty;

            return $"{mainStyle} {selectedClass}";
        }

        private static string? GetExpandButtonAriaLabel(BitNavLinkItem link)
        {
            var finalExpandBtnAriaLabel = "";
            if (link.Links.Any())
            {
                if (link.CollapseAriaLabel.HasValue() || link.ExpandAriaLabel.HasValue())
                {
                    finalExpandBtnAriaLabel = link.IsExpanded ? link.CollapseAriaLabel : link.ExpandAriaLabel;
                }
            }

            return finalExpandBtnAriaLabel;
        }

        private static bool IsRelativeUrl(string url)
        {
            var regex = new Regex(@"!/^[a-z0-9+-.]+:\/\//i");
            return regex.IsMatch(url);
        }

        private static string? GetNavLinkItemRel(BitNavLinkItem link) => link.Url.HasValue() && link.Target.HasValue() && !IsRelativeUrl(link.Url!) ? "noopener noreferrer" : null;

        private static Dictionary<BitNavLinkItemAriaCurrent, string> AriaCurrentMap = new()
        {
            [BitNavLinkItemAriaCurrent.Page] = "page",
            [BitNavLinkItemAriaCurrent.Step] = "step",
            [BitNavLinkItemAriaCurrent.Location] = "location",
            [BitNavLinkItemAriaCurrent.Time] = "time",
            [BitNavLinkItemAriaCurrent.Date] = "date",
            [BitNavLinkItemAriaCurrent.True] = "true"
        };

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && Mode == BitNavMode.Automatic)
            {
                NavigationManager.LocationChanged -= OnLocationChanged;
            }
        }
    }
}
