using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Components
{
    public partial class PopularComponents
    {
        [Inject] public IConfiguration Configuration { get; set; }

        private List<PopularComponent> AllComponents = new List<PopularComponent>
        {
            new PopularComponent()
            {
                Name = "ColorPicker",
                Description = "The ColorPicker component is used to browse through and select colors.",
                Url = "/components/color-picker",
                CssClassName = "color-picker"
            },
            new PopularComponent()
            {
                Name = "DatePicker",
                Description = "The DatePicker component offers a drop-down control that’s optimized for picking a single date from a calendar view.",
                Url = "/components/date-picker",
                CssClassName = "date-picker"
            },
            new PopularComponent()
            {
                Name = "FileUpload",
                Description = "The FileUpload component wraps the HTML file input element(s) and uploads them to a given URL.",
                Url = "/components/file-upload",
                CssClassName = "file-upload"
            },
            new PopularComponent()
            {
                Name = "DropDown",
                Description = "The DropDown component is a list in which the selected item is always visible while other items are visible on demand by clicking a dropdown button.",
                Url = "/components/drop-down",
                CssClassName = "drop-down"
            },
            new PopularComponent()
            {
                Name = "Nav (TreeList)",
                Description = "The Nav (TreeList) component provides links to the main areas of an app or site.",
                Url = "/components/nav",
                CssClassName = "nav"
            },
        };

        private List<PopularComponent> Components;
        private PopularComponent SelectedComponent;
        private string ActiveTab = "demo";
        private string ColorRgb = "rgb(255,255,255)";
        private double Alpha = 1;
        string UploadUrl => $"{GetBaseUrl()}FileUpload/UploadStreamedFile";
        string RemoveUrl => $"{GetBaseUrl()}FileUpload/RemoveFile";

        string GetBaseUrl()
        {
#if BlazorWebAssembly
            return "/";
#else
            return Configuration.GetValue<string>("ApiServerAddress");
#endif
        }

        protected override void OnInitialized()
        {
            SelectedComponent = AllComponents[0];
            FilterComponents();
            base.OnInitialized();
        }

        private void FilterComponents()
        {
            Components = AllComponents.FindAll(com => com.Name != SelectedComponent.Name);
        }

        private void SelectComponent(PopularComponent com)
        {
            SelectedComponent = com;
            FilterComponents();
            StateHasChanged();
        }

        private void SelectTab(string tab)
        {
            ActiveTab = tab;
        }

        private readonly List<BitNavLinkItem> BasicNavLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Home",
                Url = "http://example.com",
                ExpandAriaLabel = "Expand Home section",
                CollapseAriaLabel = "Collapse Home section",
                IsExpanded = true,
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "Activity", Url = "http://msn.com", Key = "key1", Target="_blank" },
                    new BitNavLinkItem { Name = "MSN", Url = "http://msn.com", Key = "key2", IsEnabled = false, Target = "_blank" }
                }
            },
            new BitNavLinkItem { Name = "Documents", Url = "http://example.com", Key = "key3", Target = "_blank", IsExpanded = true },
            new BitNavLinkItem { Name = "Pages", Url = "http://msn.com", Key = "key4", Target = "_parent" },
            new BitNavLinkItem { Name = "Notebook", Url = "http://msn.com", Key = "key5", IsEnabled = false },
            new BitNavLinkItem { Name = "Communication and Media", Url = "http://msn.com", Key = "key6", Target = "_top" },
            new BitNavLinkItem { Name = "News", Title = "News", Url = "http://msn.com", Key = "key7", Icon = "News", Target = "_self" },
        };

        private List<BitDropDownItem> GetDropdownItems()
        {
            List<BitDropDownItem> items = new();

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Fruits"
            });

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Apple",
                Value = "f-app"
            });

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Orange",
                Value = "f-ora",
                IsEnabled = false
            });

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Banana",
                Value = "f-ban",
            });

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Divider,
            });

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Vegetables"
            });

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Broccoli",
                Value = "v-bro",
            });

            return items;
        }
    }
}
