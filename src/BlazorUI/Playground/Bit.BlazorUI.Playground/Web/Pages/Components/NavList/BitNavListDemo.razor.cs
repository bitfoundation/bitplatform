using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.NavList;

public partial class BitNavListDemo
{
    private readonly List<NavMenuModel> BasicNavItems= new()
    {
        new NavMenuModel
        {
            Name = "Home",
            TitleAttribute = "Home is Parent Row",
            Url = "http://example.com",
            ExpandAriaLabel = "Expand Home section",
            CollapseAriaLabel = "Collapse Home section",
            IsExpanded = true,
            Items = new List<NavMenuModel>
            {
                new NavMenuModel { Name = "Activity", Url = "http://msn.com", Key = "key1", Target="_blank" },
                new NavMenuModel { Name = "MSN", Url = "http://msn.com", Key = "key2", IsEnabled = false, Target = "_blank" }
            }
        },
        new NavMenuModel { Name = "Documents", Url = "http://msn.com", Key = "key3", Target = "_blank", IsExpanded = true },
        new NavMenuModel { Name = "Pages", Url = "http://msn.com", Key = "key4", Target = "_parent" },
        new NavMenuModel { Name = "Notebook", Url = "http://msn.com", Key = "key5", Target = "_blank", IsEnabled = false },
        new NavMenuModel { Name = "Communication and Media", Url = "http://msn.com", Key = "key6", Target = "_top" },
        new NavMenuModel { Name = "News", Url = "http://msn.com", Key = "key7", Target = "_self", IconName = BitIconName.News },
    };

    private readonly List<NavMenuModel> GroupedNavItems = new()
    {
        new NavMenuModel
        {
            Name = "Basic Inputs",
            CollapseAriaLabel = "Collapse Basic Inputs section",
            IsExpanded = true,
            Items = new List<NavMenuModel>
            {
                new NavMenuModel { Name= "Bottons", Key = "Bottons", Url = "components/button" },
                new NavMenuModel { Name= "DropDown", Key = "DropDown", Url = "components/drop-down" },
                new NavMenuModel { Name= "FileUpload", Key = "FileUpload", Url = "components/file-upload" }
            }
        },
        new NavMenuModel
        {
            Name = "Items & Lists",
            CollapseAriaLabel = "Collapse Items & Lists section",
            IsExpanded = true,
            Items = new List<NavMenuModel>
            {
                new NavMenuModel { Name = "BasicList", Key = "BasicList", Url ="components/basic-list" },
                new NavMenuModel { Name = "DataGrid", Key = "DataGrid", Url ="components/data-grid" },
                new NavMenuModel { Name = "Carousel", Key = "Carousel", Url ="components/carousel" }
            }
        },
        new NavMenuModel
        {
            Name = "Galleries & Pickers",
            CollapseAriaLabel = "Collapse Galleries & Pickers section",
            IsExpanded = true,
            Items = new List<NavMenuModel>
            {
                new NavMenuModel { Name = "ColorPicker", Key = "ColorPicker", Url = "components/color-picker" },
                new NavMenuModel { Name = "DatePicker", Key = "DatePicker", Url = "components/date-picker" },
                new NavMenuModel { Name = "Chart", Key = "Chart", Url = "components/chart" }
            }
        }
    };

    private readonly List<NavMenuModel> ManualNavItems = new()
    {
        new NavMenuModel
        {
            Name = "Home",
            TitleAttribute = "Home is Parent Row",
            ExpandAriaLabel = "Expand Home section",
            CollapseAriaLabel = "Collapse Home section",
            Items = new List<NavMenuModel>
            {
                new NavMenuModel { Name = "Activity", Key = "key1", },
                new NavMenuModel { Name = "MSN", Key = "key2", IsEnabled = false }
            }
        },
        new NavMenuModel { Name = "Documents", Key = "key3" },
        new NavMenuModel { Name = "Pages", Key = "key4" },
        new NavMenuModel { Name = "Notebook", Key = "key5", IsEnabled = false },
        new NavMenuModel { Name = "Communication and Media", Key = "key6" },
        new NavMenuModel { Name = "News", Key = "key7", IconName = BitIconName.News },
    };

    private string ManualSelectedKey = "key1";

    private NavMenuModel ClickedItem;
    private NavMenuModel ExpandedItem;
    private void HandleOnItemExpand(NavMenuModel item)
    {
        ExpandedItem = item;
        ExpandedItem.IsExpanded = !ExpandedItem.IsExpanded;
    }
}
