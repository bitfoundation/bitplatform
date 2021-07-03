using System.Collections.Generic;

namespace Bit.Client.Web.BlazorUI
{
    public class BitNavLinkItem
    {
        public string? Key { get; set; }

        public string Name { get; set; } = "";

        public string? Title { get; set; }

        public string? Url { get; set; }

        public string? CollapseAriaLabel { get; set; }

        public string? Icon { get; set; }

        public bool IsExpanded { get; set; }

        public bool Disabled { get; set; }

        public string? Target { get; set; }

        public IEnumerable<BitNavLinkItem>? Links { get; set; }

        internal int Depth { get; set; }
    }
}
