using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitNav
    {
        private string? selectedKey;

        [Parameter]
        public string? SelectedKey
        {
            get => selectedKey;
            set
            {
                if (selectedKey == value) return;
                selectedKey = value;
                SelectedKeyChanged.InvokeAsync(value);
            }
        }

        [Parameter] public string? AriaLabel { get; set; }

        [Parameter] public bool IsOnTop { get; set; }

        [Parameter] public ICollection<BitNavLinkItem> NavLinkItems { get; set; } = new List<BitNavLinkItem>();

        [Parameter] public EventCallback<BitNavLinkItem> OnClick { get; set; }

        [Parameter] public EventCallback<string> SelectedKeyChanged { get; set; }

        [Parameter] public RenderFragment<BitNavLinkItem>? HeaderTemplate { get; set; }

        protected override string RootElementClass => "bit-nav";

        private async Task Toggle(BitNavLinkItem navLink)
        {
            if (IsEnabled is false || navLink.Disabled) return;

            if (navLink.Links?.Any() ?? false)
            {
                navLink.IsExpanded = !navLink.IsExpanded;
            }

            SelectedKey = navLink.Key;

            await OnClick.InvokeAsync(navLink);
        }

        private void OnLinkClicked(string? key, bool navLinkIsDisable)
        {
            if (navLinkIsDisable) return;
            SelectedKey = key;
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

            return $"{mainStyle} {selected} {hasIcon}";
        }
    }
}
