using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Pivot
{
    public partial class BitPivotDemo
    {
        private string SelectedKey = "Foo";
        private BitPivotItem SelectedPivotItem;
        private string OverridePivotSelectedKey = "1";
        private BitComponentVisibility PivotItemVisibility;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "ChildContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of pivot, It can be Any custom tag.",
            },
            new ComponentParameter()
            {
                Name = "DefaultSelectedKey",
                Type = "string",
                DefaultValue = "",
                Description = "Default selected key for the pivot.",
            },
            new ComponentParameter()
            {
                Name = "LinkFormat",
                Type = "BitLinkFormat",
                DefaultValue = "BitLinkFormat.Tabs",
                LinkType = LinkType.Link,
                Href = "#linkFormat-enum",
                Description = "Pivot link format, display mode for the pivot links.",
            },
            new ComponentParameter()
            {
                Name = "LinkSize",
                Type = "BitLinkSize",
                DefaultValue = "BitLinkSize.Normal",
                LinkType = LinkType.Link,
                Href = "#linkSize-enum",
                Description = "Pivot link size.",
            },
            new ComponentParameter()
            {
                Name = "OnLinkClick",
                Type = "EventCallback<BitPivotItem>",
                DefaultValue = "",
                LinkType = LinkType.Link,
                Href = "#pivotItem",
                Description = "Callback for when the selected pivot item is changed.",
            },
            new ComponentParameter()
            {
                Name = "OverflowBehavior",
                Type = "BitOverflowBehavior",
                DefaultValue = "BitOverflowBehavior.None",
                LinkType = LinkType.Link,
                Href = "#overflowBehavior-enum",
                Description = "Overflow behavior when there is not enough room to display all of the links/tabs.",
            },
            new ComponentParameter()
            {
                Name = "SelectedKey",
                Type = "string",
                DefaultValue = "",
                Description = "Key of the selected pivot item. Updating this will override the Pivot's selected item state.",
            },
        };

        private readonly List<ComponentSubParameter> componentSubParameters = new()
        {
            new ComponentSubParameter()
            {
                Id = "pivotItem",
                Title = "BitPivotItem",
                Parameters = new List<ComponentParameter>()
                {
                   new ComponentParameter()
                   {
                       Name = "ChildContent",
                       Type = "RenderFragment",
                       DefaultValue = "",
                       Description = "The content of the pivot item, It can be Any custom tag or a text.",
                   },
                   new ComponentParameter()
                   {
                       Name = "BodyFragment",
                       Type = "RenderFragment",
                       DefaultValue = "",
                       Description = "The content of the pivot item can be Any custom tag or a text, If HeaderContent provided value of this parameter show, otherwise use ChildContent.",
                   },
                   new ComponentParameter()
                   {
                       Name = "HeaderFragment",
                       Type = "RenderFragment",
                       DefaultValue = "",
                       Description = "The content of the pivot item header, It can be Any custom tag or a text.",
                   },
                   new ComponentParameter()
                   {
                       Name = "HeaderText",
                       Type = "string",
                       DefaultValue = "",
                       Description = "The text of the pivot item header, The text displayed of each pivot link.",
                   },
                   new ComponentParameter()
                   {
                       Name = "IconName",
                       Type = "string",
                       DefaultValue = "",
                       Description = "The icon name for the icon shown next to the pivot link.",
                   },
                   new ComponentParameter()
                   {
                       Name = "ItemCount",
                       Type = "int",
                       DefaultValue = "0",
                       Description = "Defines an optional item count displayed in parentheses just after the linkText.",
                   },
                   new ComponentParameter()
                   {
                       Name = "Key",
                       Type = "string",
                       DefaultValue = "",
                       Description = "A required key to uniquely identify a pivot item.",
                   },
                }
            }
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "linkFormat-enum",
                Title = "BitLinkFormat Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Tabs",
                        Description="Display Pivot Links as Tabs.",
                        Value="Tabs = 0",
                    },
                    new EnumItem()
                    {
                        Name= "Links",
                        Description="Display Pivot Links as links.",
                        Value="Links = 1",
                    },
                }
            },
            new EnumParameter()
            {
                Id = "linkSize-enum",
                Title = "BitLinkSize Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Normal",
                        Description="Display Link using normal font size.",
                        Value="Normal = 0",
                    },
                    new EnumItem()
                    {
                        Name= "Large",
                        Description="Display links using large font size.",
                        Value="Large = 1",
                    },
                }
            },
            new EnumParameter()
            {
                Id = "overflowBehavior-enum",
                Title = "BitOverflowBehavior Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "None",
                        Description="Pivot links will overflow the container and may not be visible.",
                        Value="None = 0",
                    },
                    new EnumItem()
                    {
                        Name= "Menu",
                        Description="Display an overflow menu that contains the tabs that don't fit.",
                        Value="Menu = 1",
                    },
                    new EnumItem()
                    {
                        Name= "Scroll",
                        Description="Display a scroll bar below of the tabs for moving between them.",
                        Value="Scroll = 2",
                    },
                }
            }
        };

        private void PivotSelectedKeyChanged(string key)
        {
            OverridePivotSelectedKey = key;
        }

        private void TogglePivotItemVisobility()
        {
            PivotItemVisibility = PivotItemVisibility == BitComponentVisibility.Visible ? BitComponentVisibility.Collapsed : BitComponentVisibility.Visible;
        }
    }
}
