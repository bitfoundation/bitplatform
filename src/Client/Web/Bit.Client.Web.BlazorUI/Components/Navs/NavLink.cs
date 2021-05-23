using System.Collections.Generic;

namespace Bit.Client.Web.BlazorUI
{
    public class NavLink
    {
        public NavLink()
        {
        }

        public NavLink(
            string name,
            string key = null,
            string url = null,
            string title = null,
            string collapseAriaLabel = null,
            string icon = null,
            bool isExpanded = false,
            bool disabled = false,
            NavLinkTargetType targetType = NavLinkTargetType.Blank,
            IEnumerable<NavLink> links = null)
        {
            Key = key;
            Name = name;
            Title = title;
            Url = url;
            CollapseAriaLabel = collapseAriaLabel;
            Icon = icon;
            IsExpanded = isExpanded;
            Disabled = disabled;
            TargetType = targetType;
            Links = links;
        }

        public string Key { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string CollapseAriaLabel { get; set; }
        public string Icon { get; set; }
        public bool IsExpanded { get; set; }
        public bool Disabled { get; set; }
        public NavLinkTargetType TargetType { get; set; }
        public IEnumerable<NavLink> Links { get; set; }
        internal int Depth { get; set; }

        public static implicit operator NavLink(string name)
        {
            return new NavLink(name: name, key: name);
        }
    }
}
