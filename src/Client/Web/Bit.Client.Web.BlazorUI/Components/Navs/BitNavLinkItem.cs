﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public class BitNavLinkItem
    {
        /// <summary>
        /// determin if the item is group or not to seperate it by showing the underline 
        /// </summary>
        public bool IsGroup { get; set; }

        /// <summary>
        /// (Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)
        /// </summary>
        public bool ForceAnchor { get; set; }

        /// <summary>
        /// A unique value to use as a key or id of the item, used when rendering the list of links and for tracking the currently selected link
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Text to render for this link
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Text for title tooltip
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// URL to navigate to for this link
        /// </summary>
#pragma warning disable CA1056 // URI-like properties should not be strings
        public string? Url { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings

        /// <summary>
        /// Aria label when group is collapsed and can be expanded.
        /// </summary>
        public string? ExpandAriaLabel { get; set; }

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
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Link target, specifies how to open the link
        /// </summary>
        public string? Target { get; set; }


        /// <summary>
        /// A list of items to render as children of the current item
        /// </summary>
        public IEnumerable<BitNavLinkItem> Links { get; set; } = new List<BitNavLinkItem>();

        /// <summary>
        /// Callback invoked when a link in the navigation is clicked
        /// </summary>
        public Action<BitNavLinkItem>? OnClick { get; set; }

        internal int Depth { get; set; }
    }
}
