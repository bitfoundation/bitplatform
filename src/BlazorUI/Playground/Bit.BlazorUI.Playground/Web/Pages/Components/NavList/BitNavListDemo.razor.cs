using System.Collections.Generic;
using System.Linq;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.NavList;

public partial class BitNavListDemo
{
    private static readonly List<NavItemModel> BasicNavItems = new()
    {
        new NavItemModel
        {
            Text = "Bit Platform",
            ExpandAriaLabel = "Bit Platform Expanded",
            CollapseAriaLabel = "Bit Platform Collapsed",
            IconName = BitIconName.TabletMode,
            IsExpanded = true,
            Items = new List<NavItemModel>
            {
                new NavItemModel { Text = "Home", Url = "https://bitplatform.dev/", Target="_blank" },
                new NavItemModel
                {
                    Text = "Products & Services",
                    Items = new List<NavItemModel>
                    {
                        new NavItemModel
                        {
                            Text = "Project Templates",
                            Items = new List<NavItemModel>
                            {
                                new NavItemModel { Text = "TodoTemplate", Url = "https://bitplatform.dev/todo-template/overview", Target="_blank", },
                                new NavItemModel { Text = "AdminPanel", Url = "https://bitplatform.dev/admin-panel/overview", Target = "_blank", },
                            }
                        },
                        new NavItemModel { Text = "BlazorUI", Url = "https://bitplatform.dev/components", Target = "_blank", },
                        new NavItemModel { Text = "Cloud hosting solutions", Url = "https://bitplatform.dev/#", Target = "_blank", IsEnabled = false },
                        new NavItemModel { Text = "Bit academy", Url = "https://bitplatform.dev/#", Target = "_blank", IsEnabled = false },
                    }
                },
                new NavItemModel { Text = "Pricing", Url = "https://bitplatform.dev/pricing", Target="_blank" },
                new NavItemModel { Text = "About", Url = "https://bitplatform.dev/about-us", Target="_blank" },
                new NavItemModel { Text = "Contact us", Url = "https://bitplatform.dev/contact-us", Target="_blank" },
            },
        },
        new NavItemModel
        {
            Text = "Community",
            ExpandAriaLabel = "Community Expanded",
            CollapseAriaLabel = "Community Collapsed",
            IconName = BitIconName.Heart,
            Items = new List<NavItemModel>
            {
                new NavItemModel { Text = "Linkedin", Url = "https://www.linkedin.com/company/bitplatformhq/about/", Target="_blank" },
                new NavItemModel { Text = "Twitter", Url = "https://twitter.com/bitplatformhq", Target="_blank" },
                new NavItemModel { Text = "Github repo", Url = "https://github.com/bitfoundation/bitplatform", Target="_blank" },
            }
        },
        new NavItemModel { Text = "Iconography", Url = "/icons", Target="_blank" },
    };

    private static readonly List<NavItemModel> GroupedNavItems = new()
    {
        new NavItemModel
        {
            Text = "Bit Platform",
            ExpandAriaLabel = "Bit Platform Expanded",
            CollapseAriaLabel = "Bit Platform Collapsed",
            IconName = BitIconName.TabletMode,
            Items = new List<NavItemModel>
            {
                new NavItemModel { Text = "Home", Url = "https://bitplatform.dev/", Target="_blank" },
                new NavItemModel
                {
                    Text = "Products & Services",
                    Items = new List<NavItemModel>
                    {
                        new NavItemModel
                        {
                            Text = "Project Templates",
                            Items = new List<NavItemModel>
                            {
                                new NavItemModel { Text = "TodoTemplate", Url = "https://bitplatform.dev/todo-template/overview", Target="_blank", },
                                new NavItemModel { Text = "AdminPanel", Url = "https://bitplatform.dev/admin-panel/overview", Target = "_blank", },
                            }
                        },
                        new NavItemModel { Text = "BlazorUI", Url = "https://bitplatform.dev/components", Target = "_blank", },
                        new NavItemModel { Text = "Cloud hosting solutions", Url = "https://bitplatform.dev/#", Target = "_blank", IsEnabled = false },
                        new NavItemModel { Text = "Bit academy", Url = "https://bitplatform.dev/#", Target = "_blank", IsEnabled = false },
                    }
                },
                new NavItemModel { Text = "Pricing", Url = "https://bitplatform.dev/pricing", Target="_blank" },
                new NavItemModel { Text = "About", Url = "https://bitplatform.dev/about-us", Target="_blank" },
                new NavItemModel { Text = "Contact us", Url = "https://bitplatform.dev/contact-us", Target="_blank" },
            },
        },
        new NavItemModel
        {
            Text = "Community",
            ExpandAriaLabel = "Community Expanded",
            CollapseAriaLabel = "Community Collapsed",
            IconName = BitIconName.Heart,
            Items = new List<NavItemModel>
            {
                new NavItemModel { Text = "Linkedin", Url = "https://www.linkedin.com/company/bitplatformhq/about/", Target="_blank" },
                new NavItemModel { Text = "Twitter", Url = "https://twitter.com/bitplatformhq", Target="_blank" },
                new NavItemModel { Text = "Github repo", Url = "https://github.com/bitfoundation/bitplatform", Target="_blank" },
            }
        },
    };

    private static readonly List<NavItemModel> ManualNavItems = new()
    {
        new NavItemModel
        {
            Text = "Bit Platform",
            ExpandAriaLabel = "Bit Platform Expanded",
            CollapseAriaLabel = "Bit Platform Collapsed",
            IconName = BitIconName.TabletMode,
            Items = new List<NavItemModel>
            {
                new NavItemModel { Text = "Home" },
                new NavItemModel
                {
                    Text = "Products & Services",
                    Items = new List<NavItemModel>
                    {
                        new NavItemModel
                        {
                            Text = "Project Templates",
                            Items = new List<NavItemModel>
                            {
                                new NavItemModel { Text = "TodoTemplate" },
                                new NavItemModel { Text = "AdminPanel" },
                            }
                        },
                        new NavItemModel { Text = "BlazorUI" },
                        new NavItemModel { Text = "Cloud hosting solutions" },
                        new NavItemModel { Text = "Bit academy" },
                    }
                },
                new NavItemModel { Text = "Pricing" },
                new NavItemModel { Text = "About" },
                new NavItemModel { Text = "Contact us" },
            },
        },
        new NavItemModel
        {
            Text = "Community",
            ExpandAriaLabel = "Community Expanded",
            CollapseAriaLabel = "Community Collapsed",
            IconName = BitIconName.Heart,
            Items = new List<NavItemModel>
            {
                new NavItemModel { Text = "Linkedin" },
                new NavItemModel { Text = "Twitter" },
                new NavItemModel { Text = "Github repo" },
            }
        },
        new NavItemModel { Text = "Iconography" },
    };

    private static readonly List<BitDropDownItem> DropDownItems = new()
    {
        new BitDropDownItem
        {
            Text = "Home",
            Value = "Home",
        },
        new BitDropDownItem
        {
            Text = "TodoTemplate",
            Value = "TodoTemplate",
        },
        new BitDropDownItem
        {
            Text = "AdminPanel",
            Value = "AdminPanel",
        },
        new BitDropDownItem
        {
            Text = "BlazorUI",
            Value = "BlazorUI",
        },
        new BitDropDownItem
        {
            Text = "Pricing",
            Value = "Pricing",
        },
        new BitDropDownItem
        {
            Text = "About",
            Value = "About",
        },
        new BitDropDownItem
        {
            Text = "Contact us",
            Value = "Contact us",
        },
        new BitDropDownItem
        {
            Text = "Linkedin",
            Value = "Linkedin",
        },
        new BitDropDownItem
        {
            Text = "Twitter",
            Value = "Twitter",
        },
        new BitDropDownItem
        {
            Text = "Github repo",
            Value = "Github repo",
        },
        new BitDropDownItem
        {
            Text = "Iconography",
            Value = "Iconography",
        },
    };
    private static List<NavItemModel> Flatten(IList<NavItemModel> e) => e.SelectMany(c => Flatten(c.Items)).Concat(e).ToList();
    private NavItemModel SelectedItemNav = ManualNavItems[0].Items[0];
    private string SelectedItemText = ManualNavItems[0].Items[0].Text;

    private NavItemModel ClickedItem;
    private NavItemModel SelectedItem;
    private NavItemModel ToggledItem;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaCurrentField",
            Type = "string",
            DefaultValue = "AriaCurrent",
            Description = "Aria-current token for active nav item. Must be a valid token value, and defaults to 'page'."
        },
        new ComponentParameter()
        {
            Name = "AriaCurrentFieldSelector",
            Type = "Expression<Func<TItem, BitNavListItemAriaCurrent>>?",
            Href = "nav-list-item-aria-current",
            LinkType = LinkType.Link,
            Description = "Aria-current token for active nav item. Must be a valid token value, and defaults to 'page'."
        },
        new ComponentParameter()
        {
            Name = "AriaLabelField",
            Type = "string",
            DefaultValue = "AriaLabel",
            Description = "Aria label for the item. Ignored if collapseAriaLabel or expandAriaLabel is provided."
        },
        new ComponentParameter()
        {
            Name = "AriaLabelFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Aria label for the item. Ignored if collapseAriaLabel or expandAriaLabel is provided."
        },
        new ComponentParameter()
        {
            Name = "DefaultSelectedItem",
            Type = "TItem?",
            Description = "The initially selected item in manual mode."
        },
        new ComponentParameter()
        {
            Name = "CollapseAriaLabelField",
            Type = "string",
            DefaultValue = "CollapseAriaLabel",
            Description = "Aria label when group is collapsed."
        },
        new ComponentParameter()
        {
            Name = "CollapseAriaLabelFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Aria label when group is collapsed."
        },
        new ComponentParameter()
        {
            Name = "ExpandAriaLabelField",
            Type = "string",
            DefaultValue = "ExpandAriaLabel",
            Description = "Aria label when group is expanded."
        },
        new ComponentParameter()
        {
            Name = "ExpandAriaLabelFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Aria label when group is expanded."
        },
        new ComponentParameter()
        {
            Name = "ForceAnchorField",
            Type = "string",
            DefaultValue = "ForceAnchor",
            Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)"
        },
        new ComponentParameter()
        {
            Name = "ForceAnchorFieldelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)"
        },
        new ComponentParameter()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "Used to customize how content inside the group header is rendered."
        },
        new ComponentParameter()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "Used to customize how content inside the item is rendered."
        },
        new ComponentParameter()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "A collection of item to display in the navigation bar."
        },
        new ComponentParameter()
        {
            Name = "ItemsField",
            Type = "string",
            DefaultValue = "Items",
            Description = "A list of items to render as children of the current item."
        },
        new ComponentParameter()
        {
            Name = "ItemsFieldSelector",
            Type = "Expression<Func<TItem, IList<TItem>>>?",
            Description = "A list of items to render as children of the current item."
        },
        new ComponentParameter()
        {
            Name = "IconNameField",
            Type = "string",
            DefaultValue = "IconName",
            Description = "Name of an icon to render next to the item button."
        },
        new ComponentParameter()
        {
            Name = "IconNameFieldSelector",
            Type = "Expression<Func<TItem, BitIconName>>?",
            Description = "Name of an icon to render next to the item button."
        },
        new ComponentParameter()
        {
            Name = "IsExpandedField",
            Type = "string",
            DefaultValue = "IsExpanded",
            Description = "Whether or not the group is in an expanded state."
        },
        new ComponentParameter()
        {
            Name = "IsExpandedFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Whether or not the group is in an expanded state."
        },
        new ComponentParameter()
        {
            Name = "IsEnabledField",
            Type = "string",
            DefaultValue = "IsEnabled",
            Description = "Whether or not the item is disabled."
        },
        new ComponentParameter()
        {
            Name = "IsEnabledFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Whether or not the item is disabled."
        },
        new ComponentParameter()
        {
            Name = "Mode",
            Type = "BitNavListMode",
            DefaultValue = "BitNavListMode.Automatic",
            Href = "nav-list-mode-enum",
            LinkType = LinkType.Link,
            Description = "Determines how the navigation will be handled."
        },
        new ComponentParameter()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when an item is clicked."
        },
        new ComponentParameter()
        {
            Name = "OnSelectItem",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when an item is selected."
        },
        new ComponentParameter()
        {
            Name = "OnItemToggle",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when a group header is clicked and Expanded or Collapse."
        },
        new ComponentParameter()
        {
            Name = "RenderType",
            Type = "BitNavListRenderType",
            DefaultValue = "BitNavListRenderType.Normal",
            Href = "nav-list-render-type-enum",
            LinkType = LinkType.Link,
            Description = "The way to render nav items."
        },
        new ComponentParameter()
        {
            Name = "SelectedItem",
            Type = "TItem?",
            Description = "Selected item to show in Nav."
        },
        new ComponentParameter()
        {
            Name = "StyleField",
            Type = "string",
            DefaultValue = "Style",
            Description = "Custom style for the each item element."
        },
        new ComponentParameter()
        {
            Name = "StyleFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Custom style for the each item element."
        },
        new ComponentParameter()
        {
            Name = "TextField",
            Type = "string",
            DefaultValue = "Name",
            Description = "Text to render for the item."
        },
        new ComponentParameter()
        {
            Name = "TextFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Text to render for the item."
        },
        new ComponentParameter()
        {
            Name = "TitleField",
            Type = "string",
            DefaultValue= "Title",
            Description = "Text for the item tooltip."
        },
        new ComponentParameter()
        {
            Name = "TitleFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Text for the item tooltip."
        },
        new ComponentParameter()
        {
            Name = "TargetField",
            Type = "string",
            DefaultValue = "Target",
            Description = "Link target, specifies how to open the item link."
        },
        new ComponentParameter()
        {
            Name = "TargetFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Link target, specifies how to open the item link."
        },
        new ComponentParameter()
        {
            Name = "UrlField",
            Type = "string",
            DefaultValue = "Url",
            Description = "URL to navigate for the item link."
        },
        new ComponentParameter()
        {
            Name = "UrlFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "URL to navigate for the item link."
        },
    };
    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "nav-list-item-aria-current",
            Title = "BitNavListItemAriaCurrent Enum",
            EnumList = new List<EnumItem>
            {
                new EnumItem()
                {
                    Name= "Page",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Step",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Location",
                    Value="2",
                },
                new EnumItem()
                {
                    Name= "Date",
                    Value="3",
                },
                new EnumItem()
                {
                    Name= "Time",
                    Value="4",
                },
                new EnumItem()
                {
                    Name= "True",
                    Value="5",
                },
            }
        },
        new EnumParameter()
        {
            Id = "nav-list-mode-enum",
            Title = "BitNavListMode Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Automatic",
                    Description="The value of selected key will change using NavigationManager and the current url inside the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Manual",
                    Description="Selected key changes will be sent back to the parent component and the component won't change its value.",
                    Value="1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "nav-list-render-type-enum",
            Title = "BitNavListRenderType Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Normal",
                    Description="",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Grouped",
                    Description="",
                    Value="1",
                }
            }
        },
    };

    #region Sample Code 1

    private static string example1HTMLCode = @"
<BitNavList Items=""BasicNavItems""
            TextField=""@nameof(NavItemModel.Text)""
            UrlField=""@nameof(NavItemModel.Url)""
            TargetField=""@nameof(NavItemModel.Target)""
            TitleField=""@nameof(NavItemModel.Title)""
            IsExpandedField=""@nameof(NavItemModel.IsExpanded)""
            IconNameField=""@nameof(NavItemModel.IconName)""
            IsEnabledField=""@nameof(NavItemModel.IsEnabled)""
            CollapseAriaLabelField=""@nameof(NavItemModel.CollapseAriaLabel)""
            ExpandAriaLabelField=""@nameof(NavItemModel.ExpandAriaLabel)""
            ItemsField=""@nameof(NavItemModel.Items)"" />
";

    private static string example1CSharpCode = @"
private static readonly List<NavItemModel> BasicNavItems = new()
{
    new NavItemModel
    {
        Text = ""Bit Platform"",
        ExpandAriaLabel = ""Bit Platform Expanded"",
        CollapseAriaLabel = ""Bit Platform Collapsed"",
        IconName = BitIconName.TabletMode,
        IsExpanded = true,
        Items = new List<NavItemModel>
        {
            new NavItemModel { Text = ""Home"", Url = ""https://bitplatform.dev/"", Target=""_blank"" },
            new NavItemModel
            {
                Text = ""Products & Services"",
                Items = new List<NavItemModel>
                {
                    new NavItemModel
                    {
                        Text = ""Project Templates"",
                        Items = new List<NavItemModel>
                        {
                            new NavItemModel { Text = ""TodoTemplate"", Url = ""https://bitplatform.dev/todo-template/overview"", Target=""_blank"", },
                            new NavItemModel { Text = ""AdminPanel"", Url = ""https://bitplatform.dev/admin-panel/overview"", Target = ""_blank"", },
                        }
                    },
                    new NavItemModel { Text = ""BlazorUI"", Url = ""https://bitplatform.dev/components"", Target = ""_blank"", },
                    new NavItemModel { Text = ""Cloud hosting solutions"", Url = ""https://bitplatform.dev/#"", Target = ""_blank"", IsEnabled = false },
                    new NavItemModel { Text = ""Bit academy"", Url = ""https://bitplatform.dev/#"", Target = ""_blank"", IsEnabled = false },
                }
            },
            new NavItemModel { Text = ""Pricing"", Url = ""https://bitplatform.dev/pricing"", Target=""_blank"" },
            new NavItemModel { Text = ""About"", Url = ""https://bitplatform.dev/about-us"", Target=""_blank"" },
            new NavItemModel { Text = ""Contact us"", Url = ""https://bitplatform.dev/contact-us"", Target=""_blank"" },
        },
    },
    new NavItemModel
    {
        Text = ""Community"",
        ExpandAriaLabel = ""Community Expanded"",
        CollapseAriaLabel = ""Community Collapsed"",
        IconName = BitIconName.Heart,
        Items = new List<NavItemModel>
        {
            new NavItemModel { Text = ""Linkedin"", Url = ""https://www.linkedin.com/company/bitplatformhq/about/"", Target=""_blank"" },
            new NavItemModel { Text = ""Twitter"", Url = ""https://twitter.com/bitplatformhq"", Target=""_blank"" },
            new NavItemModel { Text = ""Github repo"", Url = ""https://github.com/bitfoundation/bitplatform"", Target=""_blank"" },
        }
    },
    new NavItemModel { Text = ""Iconography"", Url = ""/icons"", Target=""_blank"" },
};
";

    #endregion

    #region Sample Code 2

    private static string example2HTMLCode = @"
<BitNavList Items=""GroupedNavItems""
            TextField=""@nameof(NavItemModel.Text)""
            UrlField=""@nameof(NavItemModel.Url)""
            TargetField=""@nameof(NavItemModel.Target)""
            TitleField=""@nameof(NavItemModel.Title)""
            IsExpandedField=""@nameof(NavItemModel.IsExpanded)""
            IconNameField=""@nameof(NavItemModel.IconName)""
            IsEnabledField=""@nameof(NavItemModel.IsEnabled)""
            CollapseAriaLabelField=""@nameof(NavItemModel.CollapseAriaLabel)""
            ExpandAriaLabelField=""@nameof(NavItemModel.ExpandAriaLabel)""
            ItemsField=""@nameof(NavItemModel.Items)""
            RenderType=""BitNavListRenderType.Grouped"" />
";

    private static string example2CSharpCode = @"
private static readonly List<NavItemModel> GroupedNavItems = new()
{
    new NavItemModel
    {
        Text = ""Bit Platform"",
        ExpandAriaLabel = ""Bit Platform Expanded"",
        CollapseAriaLabel = ""Bit Platform Collapsed"",
        IconName = BitIconName.TabletMode,
        Items = new List<NavItemModel>
        {
            new NavItemModel { Text = ""Home"", Url = ""https://bitplatform.dev/"", Target=""_blank"" },
            new NavItemModel
            {
                Text = ""Products & Services"",
                Items = new List<NavItemModel>
                {
                    new NavItemModel
                    {
                        Text = ""Project Templates"",
                        Items = new List<NavItemModel>
                        {
                            new NavItemModel { Text = ""TodoTemplate"", Url = ""https://bitplatform.dev/todo-template/overview"", Target=""_blank"", },
                            new NavItemModel { Text = ""AdminPanel"", Url = ""https://bitplatform.dev/admin-panel/overview"", Target = ""_blank"", },
                        }
                    },
                    new NavItemModel { Text = ""BlazorUI"", Url = ""https://bitplatform.dev/components"", Target = ""_blank"", },
                    new NavItemModel { Text = ""Cloud hosting solutions"", Url = ""https://bitplatform.dev/#"", Target = ""_blank"", IsEnabled = false },
                    new NavItemModel { Text = ""Bit academy"", Url = ""https://bitplatform.dev/#"", Target = ""_blank"", IsEnabled = false },
                }
            },
            new NavItemModel { Text = ""Pricing"", Url = ""https://bitplatform.dev/pricing"", Target=""_blank"" },
            new NavItemModel { Text = ""About"", Url = ""https://bitplatform.dev/about-us"", Target=""_blank"" },
            new NavItemModel { Text = ""Contact us"", Url = ""https://bitplatform.dev/contact-us"", Target=""_blank"" },
        },
    },
    new NavItemModel
    {
        Text = ""Community"",
        ExpandAriaLabel = ""Community Expanded"",
        CollapseAriaLabel = ""Community Collapsed"",
        IconName = BitIconName.Heart,
        Items = new List<NavItemModel>
        {
            new NavItemModel { Text = ""Linkedin"", Url = ""https://www.linkedin.com/company/bitplatformhq/about/"", Target=""_blank"" },
            new NavItemModel { Text = ""Twitter"", Url = ""https://twitter.com/bitplatformhq"", Target=""_blank"" },
            new NavItemModel { Text = ""Github repo"", Url = ""https://github.com/bitfoundation/bitplatform"", Target=""_blank"" },
        }
    },
};
";

    #endregion

    #region Sample Code 3

    private static string example3HTMLCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitNavList Items=""ManualNavItems""
                TextFieldSelector=""item => item.Text""
                UrlFieldSelector=""item => item.Url""
                TargetFieldSelector=""item => item.Target""
                TitleFieldSelector=""item => item.Title""
                IsExpandedFieldSelector=""item => item.IsExpanded""
                IconNameFieldSelector=""item => item.IconName""
                IsEnabledFieldSelector=""item => item.IsEnabled""
                CollapseAriaLabelFieldSelector=""item => item.CollapseAriaLabel""
                ExpandAriaLabelFieldSelector=""item => item.ExpandAriaLabel""
                ItemsFieldSelector=""item => item.Items""
                DefaultSelectedItem=""ManualNavItems[0].Items[0]""
                Mode=""BitNavListMode.Manual"" />
</div>

<div class=""margin-top"">
    <BitLabel>Two-Way Bind</BitLabel>

    <BitNavList @bind-SelectedItem=""SelectedItemNav""
                Items=""ManualNavItems""
                TextFieldSelector=""item => item.Text""
                UrlFieldSelector=""item => item.Url""
                TargetFieldSelector=""item => item.Target""
                TitleFieldSelector=""item => item.Title""
                IsExpandedFieldSelector=""item => item.IsExpanded""
                IconNameFieldSelector=""item => item.IconName""
                IsEnabledFieldSelector=""item => item.IsEnabled""
                CollapseAriaLabelFieldSelector=""item => item.CollapseAriaLabel""
                ExpandAriaLabelFieldSelector=""item => item.ExpandAriaLabel""
                ItemsFieldSelector=""item => item.Items""
                Mode=""BitNavListMode.Manual""
                OnSelectItem=""(NavItemModel item) => SelectedItemText = DropDownItems.FirstOrDefault(i => i.Text == item.Text).Text"" />

    <BitDropDown @bind-Value=""SelectedItemText""
                    Label=""Select Item""
                    Items=""DropDownItems""
                    OnSelectItem=""(item) => SelectedItemNav = Flatten(ManualNavItems).FirstOrDefault(i => i.Text == item.Value)"" />
</div>
";

    private static string example3CSharpCode = @"
private static readonly List<NavItemModel> ManualNavItems = new()
{
    new NavItemModel
    {
        Text = ""Bit Platform"",
        ExpandAriaLabel = ""Bit Platform Expanded"",
        CollapseAriaLabel = ""Bit Platform Collapsed"",
        IconName = BitIconName.TabletMode,
        Items = new List<NavItemModel>
        {
            new NavItemModel { Text = ""Home"" },
            new NavItemModel
            {
                Text = ""Products & Services"",
                Items = new List<NavItemModel>
                {
                    new NavItemModel
                    {
                        Text = ""Project Templates"",
                        Items = new List<NavItemModel>
                        {
                            new NavItemModel { Text = ""TodoTemplate"" },
                            new NavItemModel { Text = ""AdminPanel"" },
                        }
                    },
                    new NavItemModel { Text = ""BlazorUI"" },
                    new NavItemModel { Text = ""Cloud hosting solutions"" },
                    new NavItemModel { Text = ""Bit academy"" },
                }
            },
            new NavItemModel { Text = ""Pricing"" },
            new NavItemModel { Text = ""About"" },
            new NavItemModel { Text = ""Contact us"" },
        },
    },
    new NavItemModel
    {
        Text = ""Community"",
        ExpandAriaLabel = ""Community Expanded"",
        CollapseAriaLabel = ""Community Collapsed"",
        IconName = BitIconName.Heart,
        Items = new List<NavItemModel>
        {
            new NavItemModel { Text = ""Linkedin"" },
            new NavItemModel { Text = ""Twitter"" },
            new NavItemModel { Text = ""Github repo"" },
        }
    },
    new NavItemModel { Text = ""Iconography"" },
};

private static readonly List<BitDropDownItem> DropDownItems = new()
{
    new BitDropDownItem
    {
        Text = ""Home"",
        Value = ""Home"",
    },
    new BitDropDownItem
    {
        Text = ""TodoTemplate"",
        Value = ""TodoTemplate"",
    },
    new BitDropDownItem
    {
        Text = ""AdminPanel"",
        Value = ""AdminPanel"",
    },
    new BitDropDownItem
    {
        Text = ""BlazorUI"",
        Value = ""BlazorUI"",
    },
    new BitDropDownItem
    {
        Text = ""Pricing"",
        Value = ""Pricing"",
    },
    new BitDropDownItem
    {
        Text = ""About"",
        Value = ""About"",
    },
    new BitDropDownItem
    {
        Text = ""Contact us"",
        Value = ""Contact us"",
    },
    new BitDropDownItem
    {
        Text = ""Linkedin"",
        Value = ""Linkedin"",
    },
    new BitDropDownItem
    {
        Text = ""Twitter"",
        Value = ""Twitter"",
    },
    new BitDropDownItem
    {
        Text = ""Github repo"",
        Value = ""Github repo"",
    },
    new BitDropDownItem
    {
        Text = ""Iconography"",
        Value = ""Iconography"",
    },
};
private static List<NavItemModel> Flatten(IList<NavItemModel> e) => e.SelectMany(c => Flatten(c.Items)).Concat(e).ToList();
private NavItemModel SelectedItemNav = ManualNavItems[0].Items[0];
private string SelectedItemText = ManualNavItems[0].Items[0].Text;
";

    #endregion

    #region Sample Code 4

    private static string example4HTMLCode = @"
<style>
    .nav-list-custom-header {
        color: green;
    }

    .nav-list-custom-item {
        display: flex;
        flex-flow: row nowrap;
        color: #ff7800;
        font-weight: 600;

        &.disabled-item {
            color: #ff780061;
        }
    }
</style>

<div>
    <BitLabel>Header Template (in Grouped mode)</BitLabel>
    <BitNavList Items=""GroupedNavItems""
                TextField=""@nameof(NavItemModel.Text)""
                UrlField=""@nameof(NavItemModel.Url)""
                TargetField=""@nameof(NavItemModel.Target)""
                TitleField=""@nameof(NavItemModel.Title)""
                IsExpandedField=""@nameof(NavItemModel.IsExpanded)""
                IconNameField=""@nameof(NavItemModel.IconName)""
                IsEnabledField=""@nameof(NavItemModel.IsEnabled)""
                CollapseAriaLabelField=""@nameof(NavItemModel.CollapseAriaLabel)""
                ExpandAriaLabelField=""@nameof(NavItemModel.ExpandAriaLabel)""
                ItemsField=""@nameof(NavItemModel.Items)""
                RenderType=""BitNavListRenderType.Grouped"">

        <HeaderTemplate Context=""item"">
            <div class=""nav-list-custom-header"">
                <BitIcon IconName=""BitIconName.FavoriteStarFill"" />
                <span>@item.Text</span>
            </div>
        </HeaderTemplate>
    </BitNavList>
</div>

<div class=""margin-top"">
    <BitLabel>Item Template</BitLabel>
    <BitNavList Items=""ManualNavItems""
                TextFieldSelector=""item => item.Text""
                UrlFieldSelector=""item => item.Url""
                TargetFieldSelector=""item => item.Target""
                TitleFieldSelector=""item => item.Title""
                IsExpandedFieldSelector=""item => item.IsExpanded""
                IconNameFieldSelector=""item => item.IconName""
                IsEnabledFieldSelector=""item => item.IsEnabled""
                CollapseAriaLabelFieldSelector=""item => item.CollapseAriaLabel""
                ExpandAriaLabelFieldSelector=""item => item.ExpandAriaLabel""
                ItemsFieldSelector=""item => item.Items""
                Mode=""BitNavListMode.Manual"">

        <ItemTemplate Context=""item"">
            <div class=""nav-list-custom-item @(item.IsEnabled is false ? ""disabled-item"" : """")"">
                <BitCheckbox IsEnabled=""@(item.IsEnabled)"" />
                <BitIcon IconName=""@item.IconName"" />
                <span>@item.Text</span>
            </div>
        </ItemTemplate>
    </BitNavList>
</div>
";

    private static string example4CSharpCode = @"
private static readonly List<NavItemModel> GroupedNavItems = new()
{
    new NavItemModel
    {
        Text = ""Bit Platform"",
        ExpandAriaLabel = ""Bit Platform Expanded"",
        CollapseAriaLabel = ""Bit Platform Collapsed"",
        IconName = BitIconName.TabletMode,
        Items = new List<NavItemModel>
        {
            new NavItemModel { Text = ""Home"", Url = ""https://bitplatform.dev/"", Target=""_blank"" },
            new NavItemModel
            {
                Text = ""Products & Services"",
                Items = new List<NavItemModel>
                {
                    new NavItemModel
                    {
                        Text = ""Project Templates"",
                        Items = new List<NavItemModel>
                        {
                            new NavItemModel { Text = ""TodoTemplate"", Url = ""https://bitplatform.dev/todo-template/overview"", Target=""_blank"", },
                            new NavItemModel { Text = ""AdminPanel"", Url = ""https://bitplatform.dev/admin-panel/overview"", Target = ""_blank"", },
                        }
                    },
                    new NavItemModel { Text = ""BlazorUI"", Url = ""https://bitplatform.dev/components"", Target = ""_blank"", },
                    new NavItemModel { Text = ""Cloud hosting solutions"", Url = ""https://bitplatform.dev/#"", Target = ""_blank"", IsEnabled = false },
                    new NavItemModel { Text = ""Bit academy"", Url = ""https://bitplatform.dev/#"", Target = ""_blank"", IsEnabled = false },
                }
            },
            new NavItemModel { Text = ""Pricing"", Url = ""https://bitplatform.dev/pricing"", Target=""_blank"" },
            new NavItemModel { Text = ""About"", Url = ""https://bitplatform.dev/about-us"", Target=""_blank"" },
            new NavItemModel { Text = ""Contact us"", Url = ""https://bitplatform.dev/contact-us"", Target=""_blank"" },
        },
    },
    new NavItemModel
    {
        Text = ""Community"",
        ExpandAriaLabel = ""Community Expanded"",
        CollapseAriaLabel = ""Community Collapsed"",
        IconName = BitIconName.Heart,
        Items = new List<NavItemModel>
        {
            new NavItemModel { Text = ""Linkedin"", Url = ""https://www.linkedin.com/company/bitplatformhq/about/"", Target=""_blank"" },
            new NavItemModel { Text = ""Twitter"", Url = ""https://twitter.com/bitplatformhq"", Target=""_blank"" },
            new NavItemModel { Text = ""Github repo"", Url = ""https://github.com/bitfoundation/bitplatform"", Target=""_blank"" },
        }
    },
};

private static readonly List<NavItemModel> ManualNavItems = new()
{
    new NavItemModel
    {
        Text = ""Bit Platform"",
        ExpandAriaLabel = ""Bit Platform Expanded"",
        CollapseAriaLabel = ""Bit Platform Collapsed"",
        IconName = BitIconName.TabletMode,
        Items = new List<NavItemModel>
        {
            new NavItemModel { Text = ""Home"" },
            new NavItemModel
            {
                Text = ""Products & Services"",
                Items = new List<NavItemModel>
                {
                    new NavItemModel
                    {
                        Text = ""Project Templates"",
                        Items = new List<NavItemModel>
                        {
                            new NavItemModel { Text = ""TodoTemplate"" },
                            new NavItemModel { Text = ""AdminPanel"" },
                        }
                    },
                    new NavItemModel { Text = ""BlazorUI"" },
                    new NavItemModel { Text = ""Cloud hosting solutions"" },
                    new NavItemModel { Text = ""Bit academy"" },
                }
            },
            new NavItemModel { Text = ""Pricing"" },
            new NavItemModel { Text = ""About"" },
            new NavItemModel { Text = ""Contact us"" },
        },
    },
    new NavItemModel
    {
        Text = ""Community"",
        ExpandAriaLabel = ""Community Expanded"",
        CollapseAriaLabel = ""Community Collapsed"",
        IconName = BitIconName.Heart,
        Items = new List<NavItemModel>
        {
            new NavItemModel { Text = ""Linkedin"" },
            new NavItemModel { Text = ""Twitter"" },
            new NavItemModel { Text = ""Github repo"" },
        }
    },
    new NavItemModel { Text = ""Iconography"" },
};
";

    #endregion

    #region Sample Code 5

    private static string example5HTMLCode = @"
<BitNavList Items=""ManualNavItems""
            TextFieldSelector=""item => item.Text""
            UrlFieldSelector=""item => item.Url""
            TargetFieldSelector=""item => item.Target""
            TitleFieldSelector=""item => item.Title""
            IsExpandedFieldSelector=""item => item.IsExpanded""
            IconNameFieldSelector=""item => item.IconName""
            IsEnabledFieldSelector=""item => item.IsEnabled""
            CollapseAriaLabelFieldSelector=""item => item.CollapseAriaLabel""
            ExpandAriaLabelFieldSelector=""item => item.ExpandAriaLabel""
            ItemsFieldSelector=""item => item.Items""
            DefaultSelectedItem=""ManualNavItems[0].Items[0]""
            Mode=""BitNavListMode.Manual""
            OnItemClick=""(NavItemModel item) => ClickedItem = item""
            OnSelectItem=""(NavItemModel item) => SelectedItem = item""
            OnItemToggle=""(NavItemModel item) => ToggledItem = item"" />

<div class=""flex"">
    <span>Clicked Item: @ClickedItem?.Text</span>
    <span>Selected Item: @SelectedItem?.Text</span>
    <span>Toggled Item: @(ToggledItem is null ? ""N/A"" : $""{ToggledItem.Text} ({(ToggledItem.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</span>
</div>
";

    private static string example5CSharpCode = @"
private static readonly List<NavItemModel> ManualNavItems = new()
{
    new NavItemModel
    {
        Text = ""Bit Platform"",
        ExpandAriaLabel = ""Bit Platform Expanded"",
        CollapseAriaLabel = ""Bit Platform Collapsed"",
        IconName = BitIconName.TabletMode,
        Items = new List<NavItemModel>
        {
            new NavItemModel { Text = ""Home"" },
            new NavItemModel
            {
                Text = ""Products & Services"",
                Items = new List<NavItemModel>
                {
                    new NavItemModel
                    {
                        Text = ""Project Templates"",
                        Items = new List<NavItemModel>
                        {
                            new NavItemModel { Text = ""TodoTemplate"" },
                            new NavItemModel { Text = ""AdminPanel"" },
                        }
                    },
                    new NavItemModel { Text = ""BlazorUI"" },
                    new NavItemModel { Text = ""Cloud hosting solutions"" },
                    new NavItemModel { Text = ""Bit academy"" },
                }
            },
            new NavItemModel { Text = ""Pricing"" },
            new NavItemModel { Text = ""About"" },
            new NavItemModel { Text = ""Contact us"" },
        },
    },
    new NavItemModel
    {
        Text = ""Community"",
        ExpandAriaLabel = ""Community Expanded"",
        CollapseAriaLabel = ""Community Collapsed"",
        IconName = BitIconName.Heart,
        Items = new List<NavItemModel>
        {
            new NavItemModel { Text = ""Linkedin"" },
            new NavItemModel { Text = ""Twitter"" },
            new NavItemModel { Text = ""Github repo"" },
        }
    },
    new NavItemModel { Text = ""Iconography"" },
};

private NavItemModel ClickedItem;
private NavItemModel SelectedItem;
private NavItemModel ToggledItem;
";

    #endregion
}
