using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitNav
    {
        [Parameter] public string? SelectedKey { get; set; }

        [Parameter] public string? AriaLabel { get; set; }

        [Parameter] public bool IsOnTop { get; set; }

        [Parameter] public ICollection<NavLink> NavLinks { get; set; } = new List<NavLink>();

        [Parameter] public EventCallback<NavLink> OnClick { get; set; }

        [Parameter] public RenderFragment<NavLink>? HeaderTemplate { get; set; }

        protected override string RootElementClass => "bit-nav";

        private async Task Toggle(NavLink navLink)
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

        private static string MapNavLinkTargetTypeToString(NavLinkTargetType navLinkTargetType)
        {
            return navLinkTargetType switch
            {
                NavLinkTargetType.Blank => "_blank",
                NavLinkTargetType.Parent => "_parent",
                NavLinkTargetType.Self => "_self",
                NavLinkTargetType.Top => "_top",
                _ => throw new System.Exception($"NavLinkTargetType not supported: {navLinkTargetType}")
            };
        }

        private string GetLinkClass(NavLink navLink)
        {
            var enabledClass = navLink.Disabled ? "disabled" : "enabled";
            var hasUrlClass = string.IsNullOrWhiteSpace(navLink.Url) ? "nourl" : "hasurl";

            var mainStyle = $"bit-nav-link-{enabledClass}-{hasUrlClass}-{VisualClassRegistrar()}";
            var selected = navLink.Key == SelectedKey ? $"bit-nav-selected-{VisualClassRegistrar()}" : "";
            var hasIcon = string.IsNullOrWhiteSpace(navLink.Icon) ? $"bit-nav-has-not-icon-{VisualClassRegistrar()}" : $"bit-nav-has-icon-{VisualClassRegistrar()}";

            return $"{mainStyle} {selected} {hasIcon}";
        }
    }
}
