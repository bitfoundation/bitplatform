using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitNav
    {
        /// <summary>
        /// The key of the nav item selected by caller
        /// </summary>
        [Parameter] public string? SelectedKey { get; set; }

        /// <summary>
        /// The aria label of nav container for the benefit of screen readers
        /// </summary>
        [Parameter] public string? AriaLabel { get; set; }

        /// <summary>
        /// Indicates whether the navigation component renders on top of other content in the UI
        /// </summary>
        [Parameter] public bool IsOnTop { get; set; }

        /// <summary>
        /// A collection of link items to display in the navigation bar
        /// </summary>
        [Parameter] public ICollection<BitNavLinkItem> NavLinkItems { get; set; } = new List<BitNavLinkItem>();

        /// <summary>
        /// Callback invoked when a link in the navigation is clicked
        /// </summary>
        [Parameter] public EventCallback<BitNavLinkItem> OnClick { get; set; }

        /// <summary>
        /// The template of the header for each nav item, which is a generic RenderFramgment that accepts a BitNavLinItem as input
        /// </summary>
        [Parameter] public RenderFragment<BitNavLinkItem>? HeaderTemplate { get; set; }

        protected override string RootElementClass => "bit-nav";

        private async Task Toggle(BitNavLinkItem navLink)
        {
            if (IsEnabled is false || navLink.Disabled) return;

            if (navLink.Links?.Any() ?? false)
            {
                navLink.IsExpanded = !navLink.IsExpanded;
            }

            await OnClick.InvokeAsync(navLink);
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
            var selected = navLink.Key == SelectedKey ? $"bit-nav-selected-{VisualClassRegistrar()}" : "";
            var hasIcon = navLink.Icon.HasNoValue()
                            ? $"bit-nav-has-not-icon-{VisualClassRegistrar()}"
                            : $"bit-nav-has-icon-{VisualClassRegistrar()}";
            var hasChildren = navLink.Links?.Any() ?? false ? $"bit-nav-haschildren-{VisualClassRegistrar()}" : "";

            return $"{mainStyle} {selected} {hasIcon} {hasChildren}";
        }
    }
}
