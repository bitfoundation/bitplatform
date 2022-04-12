using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Breadcrumb
{
    public partial class BitBreadcrumbDemo
    {
        public string onClickValue { get; set; } = string.Empty;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new()
            {
                Name = "Items (required)",
                Type = "BreadcrumbItem[]",
                DefaultValue = "",
                Description = "Collection of breadcrumbs to render"
            },
            new()
            {
                Name = "AriaLabel",
                Type = "string",
                DefaultValue = "",
                Description = "Aria label for the root element of the breadcrumb (which is a navigation landmark)."
            },
            new()
            {
                Name = "Class",
                Type = "string",
                DefaultValue = "",
                Description = "Custom CSS class for the root element of the component."
            },
            new()
            {
                Name = "DividerIcon",
                Type = "string",
                DefaultValue = "bit-icon--ChevronRight",
                Description = "Render a custom divider in place of the default chevron >"
            },
            new()
            {
                Name = "MaxDisplayedItems",
                Type = "byte?",
                DefaultValue = "",
                Description = "The maximum number of breadcrumbs to display before coalescing. If not specified, all breadcrumbs will be rendered."
            },
            new()
            {
                Name = "OnRenderOverflowIcon",
                Type = "string",
                DefaultValue = "bit-icon--More",
                Description = "Render a custom overflow icon in place of the default icon ..."
            },
            new()
            {
                Name = "OverflowAriaLabel",
                Type = "string?",
                DefaultValue = "bit-icon--More",
                Description = "Aria label for the overflow button."
            },
            new()
            {
                Name = "OverflowIndex",
                Type = "byte?",
                DefaultValue = "",
                Description = "Optional index where overflow items will be collapsed."
            },
            new()
            {
                Name = "IsEnabled",
                Type = "bool",
                DefaultValue = "",
                Description = "Whether or not the component is enabled."
            },
            new()
            {
                Name = "Visibility",
                Type = "BitComponentVisibility",
                LinkType = LinkType.Link,
                Href = "#component-visibility-enum",
                DefaultValue = "BitComponentVisibility.Visible",
                Description = "Whether the component is Visible,Hidden,Collapsed."
            },
            new()
            {
                Name = "HtmlAttributes",
                Type = "Dictionary<string, object>",
                DefaultValue = "",
                Description = "Capture and render additional attributes in addition to the component's parameters."
            }
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "component-visibility-enum",
                Title = "BitComponentVisibility Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new ()
                    {
                        Name= "Visible",
                        Description="Show content of the component.",
                        Value="0"
                    },
                    new ()
                    {
                        Name= "Hidden",
                        Description="Hide content of the component,though the space it takes on the page remains.",
                        Value="1"
                    },
                    new ()
                    {
                        Name= "Collapsed",
                        Description="Hide content of the component,though the space it takes on the page gone.",
                        Value="2"
                    }
                }
            }
        };

        private List<BitBreadcrumbItem> GetBreadcrumbItems()
        {
            return new List<BitBreadcrumbItem>()
            {
                new()
                {
                    Text = "Folder 1",
                    Key = "f1",
                    href = "/components/breadcrumb",
                    OnClick = (() => onClickValue = "Folder 1 clicked")
                },
                new()
                {
                    Text = "Folder 2",
                    Key = "f2",
                    href = "/components/breadcrumb",
                    OnClick = (() => onClickValue = "Folder 2 clicked")
                },
                new()
                {
                    Text = "Folder 3",
                    Key = "f3",
                    href = "/components/breadcrumb",
                    OnClick = (() => onClickValue = "Folder 3 clicked")
                },
                new()
                {
                    Text = "Folder 4",
                    Key = "f4",
                    href = "/components/breadcrumb",
                    IsCurrentItem = true,
                    OnClick = (() => onClickValue = "Folder 4 clicked")
                }
            };
        }

        private readonly string example1HTMLCode = @"<div class=""example-desc"">With items rendered as links</div>
<div>
    <Breadcrumb Items=""GetBreadcrumbItems()""></Breadcrumb>
</div>
<div class=""example-desc"">With custom rendered divider and overflow icon</div>
<div>
    <Breadcrumb Items=""GetBreadcrumbItems()""
                DividerIcon=""bit-icon--Separator""
                OverflowIndex=""1""
                MaxDisplayedItems=""2""
                OnRenderOverflowIcon=""bit-icon--ChevronRight""></Breadcrumb>
</div>";

        private readonly string example2HTMLCode = @"<div class=""example-desc"">With no maxDisplayedItems</div>
<div>
    <Breadcrumb Items=""GetBreadcrumbItems()"" AriaLabel=""Breadcrumb with no maxDisplayedItems""></Breadcrumb>
</div>

<div class=""example-desc"" >With maxDisplayedItems set to 3</div>
<div>
    <Breadcrumb Items=""GetBreadcrumbItems()"" MaxDisplayedItems=""3"" AriaLabel=""Breadcrumb with 3 maxDisplayedItems""></Breadcrumb>
</div>

<div class=""example-desc"">With maxDisplayedItems set to 2 and overflowIndex set to 1 (second element)</div>
<div>
    <Breadcrumb Items=""GetBreadcrumbItems()"" MaxDisplayedItems=""2"" OverflowIndex=""1""></Breadcrumb>
</div>";

        private readonly string example1CSharpCode = @"
private List<BitBreadcrumbItem> GetBreadcrumbItems()
{
    return new List<BitBreadcrumbItem>()
    {
        new()
        {
            Text = ""Folder 1"",
            Key = ""f1"",
            href = ""/components/breadcrumb"",
            OnClick = (() => onClickValue = ""Folder 1 clicked"")
        },
        new()
        {
            Text = ""Folder 2"",
            Key = ""f2"",
            href = ""/components/breadcrumb"",
            OnClick = (() => onClickValue = ""Folder 2 clicked"")
        },
        new()
        {
            Text = ""Folder 3"",
            Key = ""f3"",
            href = ""/components/breadcrumb"",
            OnClick = (() => onClickValue = ""Folder 3 clicked"")
        },
        new()
        {
            Text = ""Folder 4"",
            Key = ""f4"",
            href = ""/components/breadcrumb"",
            IsCurrentItem = true,
            OnClick = (() => onClickValue = ""Folder 4 clicked"")
        }
    };
}";
    }
}
