using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Breadcrumb
{
    public partial class BitBreadcrumbDemo
    {
        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "Items",
                Type = "List<BreadcrumbItem>",
                DefaultValue = "-",
                Description = "Collection of breadcrumbs to render.",
            },
            new ComponentParameter()
            {
                Name = "MaxDisplayedItems",
                Type = "byte",
                DefaultValue = "",
                Description = "The maximum number of breadcrumbs to display before coalescing.If not specified, all breadcrumbs will be rendered.",
            },
            new ComponentParameter()
            {
                Name = "OverflowAriaLabel",
                Type = "string",
                DefaultValue = "bit-icon--More",
                Description = "Aria label for the overflow button.",
            },
            new ComponentParameter()
            {
                Name = "OverflowIndex",
                Type = "byte",
                DefaultValue = "0",
                Description = "Optional index where overflow items will be collapsed.",
            },
            new ComponentParameter()
            {
                Name = "DividerAs",
                Type = "string",
                DefaultValue = "bit-icon--ChevronRight",
                Description = "Render a custom divider in place of the default chevron >.",
            },
            new ComponentParameter()
            {
                Name = "Key",
                Type = "string",
                DefaultValue = "",
                Description = "Arbitrary unique string associated with the breadcrumb item.",
            },
            new ComponentParameter()
            {
                Name = "Text",
                Type = "string",
                DefaultValue = "",
                Description = "Text to display in the breadcrumb item.",
            },
            new ComponentParameter()
            {
                Name = "href",
                Type = "string",
                DefaultValue = "",
                Description = "URL to navigate to when this breadcrumb item is clicked.",
            },
            new ComponentParameter()
            {
                Name = "IsCurrentItem",
                Type = "bool",
                DefaultValue = "",
                Description = "Whether this is the breadcrumb item the user is currently navigated to.",
            },
            new ComponentParameter()
            {
                Name = "OnClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the dropdown clicked.",
            },
        };

        private readonly string example1HTMLCode = @"
            <Breadcrumb Items = @items
                        MaxDisplayedItems=10
                        AriaLabel=""Breadcrumb with items rendered as buttons""
                        OverflowAriaLabel=""More"" />

            <div class=""example-desc"">With items rendered as links</div>
            <Breadcrumb Items = @itemsWithHref
                        MaxDisplayedItems=3
                        AriaLabel=""Breadcrumb with items rendered as links""
                        OverflowAriaLabel=""More"" />

            <div class=""example-desc"">With last item rendered as heading</div>

            <Breadcrumb Items = @items
                        MaxDisplayedItems=5
                        AriaLabel=""With items rendered as buttons and tab navigation control""
                        OverflowAriaLabel=""More"" />
        ";
        public static EventCallback<MouseEventArgs> _onBreadcrumbItemClicked = default;

        private readonly List<BreadcrumbItem> items = new()
        {
            new BreadcrumbItem() { Text = "Files", Key = "Files", OnClick = _onBreadcrumbItemClicked },
            new BreadcrumbItem() { Text = "Folder 1", Key = "f1", OnClick = _onBreadcrumbItemClicked },
            new BreadcrumbItem() { Text = "Folder 2", Key = "f2", OnClick = _onBreadcrumbItemClicked },
            new BreadcrumbItem() { Text = "Folder 3", Key = "f3", OnClick = _onBreadcrumbItemClicked },
            new BreadcrumbItem() { Text = "Folder 4", Key = "f4" },
            new BreadcrumbItem() { Text = "Folder 5", Key = "f5", OnClick = _onBreadcrumbItemClicked },
            new BreadcrumbItem() { Text = "Folder 6", Key = "f6", OnClick = _onBreadcrumbItemClicked },
            new BreadcrumbItem() { Text = "Folder 7", Key = "f", OnClick = _onBreadcrumbItemClicked },
            new BreadcrumbItem() { Text = "Folder 8", Key = "f8", OnClick = _onBreadcrumbItemClicked },
            new BreadcrumbItem() { Text = "Folder 9", Key = "f9", OnClick = _onBreadcrumbItemClicked },
            new BreadcrumbItem() { Text = "Folder 10", Key = "f10", OnClick = _onBreadcrumbItemClicked },
            new BreadcrumbItem() { Text = "Folder 11", Key = "f11", OnClick = _onBreadcrumbItemClicked, IsCurrentItem = true },
        };

        private readonly List<BreadcrumbItem> itemsWithHref = new()
        {
            new BreadcrumbItem() { Text = "Files", Key = "Files", href = "#/controls/web/Breadcrumb" },
            new BreadcrumbItem() { Text = "Folder 1", Key = "f1", href = "#/controls/web/Breadcrumb" },
            new BreadcrumbItem() { Text = "Folder 2", Key = "f2", href = "#/controls/web/Breadcrumb" },
            new BreadcrumbItem() { Text = "Folder 3", Key = "f3", href = "#/controls/web/Breadcrumb" },
            new BreadcrumbItem() { Text = "Folder 4(non-clickable)" },
            new BreadcrumbItem() { Text = "Folder 5", Key = "f5", href = "#/controls/web/Breadcrumb", IsCurrentItem = true },
        };

        private readonly string _getCustomDivider = "bit-icon--chevron-circle-right";
        private readonly string _getCustomOverflowIcon = "bit-icon--More";
    }
}
