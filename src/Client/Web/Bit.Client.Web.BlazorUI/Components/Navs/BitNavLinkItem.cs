using System.Collections.Generic;

namespace Bit.Client.Web.BlazorUI
{
    public class BitNavLinkItem
    {
        /// <summary>
        /// Unique, stable key for this link, used when rendering the list of links and for tracking the currently selected link
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Text to render for this link
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Text for title tooltip and ARIA description
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// URL to navigate to for this link
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// ARIA label when items is collapsed and can be expanded
        /// </summary>
        public string? CollapseAriaLabel { get; set; }

        /// <summary>
        /// Name of an icon to render next to this link button
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Whether or not the link is in an expanded state
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// Whether or not the link is disabled
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Link target, specifies how to open the link
        /// </summary>
        public string? Target { get; set; }

        /// <summary>
        /// Child links to this link, the items to display in the a child of navigation bar
        /// </summary>
        public IEnumerable<BitNavLinkItem>? Links { get; set; }

        internal int Depth { get; set; }
    }
}
