using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Shared.Dtos.DataGridDemo;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.DataGrid;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace Bit.BlazorUI.Playground.Web.Components;

public partial class PopularComponents
{
    [Inject] public IJSRuntime JSRuntime { get; set; }
    [Inject] public IConfiguration Configuration { get; set; }

    private IQueryable<Country> allCountries;
    private BitDataGridPaginationState pagination = new() { ItemsPerPage = 7 };
    private string typicalSampleNameFilter = string.Empty;

    public IQueryable<Country> FilteredItems => allCountries?.Where(x => x.Name.Contains(typicalSampleNameFilter, StringComparison.CurrentCultureIgnoreCase));

    private List<PopularComponent> Components = new List<PopularComponent>
    {
        new PopularComponent()
        {
            Name = "ColorPicker",
            Description = "The ColorPicker component is used to browse through and select colors.",
            Url = "/components/color-picker"
        },
        new PopularComponent()
        {
            Name = "DatePicker",
            Description = "The DatePicker component offers a drop-down control that’s optimized for picking a single date from a calendar view.",
            Url = "/components/date-picker"
        },
        new PopularComponent()
        {
            Name = "FileUpload",
            Description = "The FileUpload component wraps the HTML file input element(s) and uploads them to a given URL.",
            Url = "/components/file-upload"
        },
        new PopularComponent()
        {
            Name = "DropDown",
            Description = "The DropDown component is a list in which the selected item is always visible while other items are visible on demand by clicking a dropdown button.",
            Url = "/components/drop-down"
        },
        new PopularComponent()
        {
            Name = "Nav (TreeList)",
            Description = "The Nav (TreeList) component provides links to the main areas of an app or site.",
            Url = "/components/nav"
        },
        new PopularComponent()
        {
            Name = "DataGrid",
            Description = "BitDataGrid is a robust way to display an information-rich collection of items, and allow people to sort, and filter the content. Use a data-grid when information density is critical.",
            Url = "/components/data-grid"
        }
    };

    private PopularComponent SelectedComponent;
    private string ColorRgb = "rgb(0,101,239)";
    private double Alpha = 1;
    string UploadUrl => $"{GetBaseUrl()}FileUpload/UploadChunkedFile";
    string RemoveUrl => $"FileUpload/RemoveFile";

    string GetBaseUrl()
    {
#if BlazorWebAssembly
        return "/api/";
#else
        return Configuration.GetValue<string>("ApiServerAddress");
#endif
    }

    protected override void OnInitialized()
    {
        SelectedComponent = Components[0];
        allCountries = _countries.AsQueryable();
        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JSRuntime.InvokeVoidAsync("highlightSnippet");
    }

    private void SelectComponent(PopularComponent com)
    {
        SelectedComponent = com;
        StateHasChanged();
    }

    private readonly List<BitNavItem> BasicNavLinks = new()
    {
        new BitNavItem
        {
            Text = "Home",
            Url = "http://example.com",
            ExpandAriaLabel = "Expand Home section",
            CollapseAriaLabel = "Collapse Home section",
            IsExpanded = true,
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "Activity", Url = "http://msn.com", Target="_blank" },
                new BitNavItem { Text = "MSN", Url = "http://msn.com", IsEnabled = false, Target = "_blank" }
            }
        },
        new BitNavItem { Text = "Documents", Url = "http://example.com", Target = "_blank", IsExpanded = true },
        new BitNavItem { Text = "Pages", Url = "http://msn.com", Target = "_parent" },
        new BitNavItem { Text = "Notebook", Url = "http://msn.com", IsEnabled = false },
        new BitNavItem { Text = "Communication and Media", Url = "http://msn.com", Target = "_top" },
        new BitNavItem { Text = "News", Title = "News", Url = "http://msn.com", IconName = BitIconName.News, Target = "_self" },
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

    public readonly static Country[] _countries = new[]
    {
        new Country { Code = "AR", Name="Argentina", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 2 } },
        new Country { Code = "AM", Name="Armenia", Medals = new Medals { Gold = 0, Silver = 2, Bronze = 2 } },
        new Country { Code = "AU", Name="Australia", Medals = new Medals { Gold = 17, Silver = 7, Bronze = 22 } },
        new Country { Code = "AT", Name="Austria", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 5 } },
        new Country { Code = "AZ", Name="Azerbaijan", Medals = new Medals { Gold = 0, Silver = 3, Bronze = 4 } },
        new Country { Code = "BS", Name="Bahamas", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 0 } },
        new Country { Code = "BH", Name="Bahrain", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "BY", Name="Belarus", Medals = new Medals { Gold = 1, Silver = 3, Bronze = 3 } },
        new Country { Code = "BE", Name="Belgium", Medals = new Medals { Gold = 3, Silver = 1, Bronze = 3 } },
        new Country { Code = "BM", Name="Bermuda", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 0 } },
        new Country { Code = "BW", Name="Botswana", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "BR", Name="Brazil", Medals = new Medals { Gold = 7, Silver = 6, Bronze = 8 } },
        new Country { Code = "BF", Name="Burkina Faso", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "CA", Name="Canada", Medals = new Medals { Gold = 7, Silver = 6, Bronze = 11 } },
        new Country { Code = "TW", Name="Chinese Taipei", Medals = new Medals { Gold = 2, Silver = 4, Bronze = 6 } },
        new Country { Code = "CO", Name="Colombia", Medals = new Medals { Gold = 0, Silver = 4, Bronze = 1 } },
        new Country { Code = "CI", Name="Côte d'Ivoire", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "HR", Name="Croatia", Medals = new Medals { Gold = 3, Silver = 3, Bronze = 2 } },
        new Country { Code = "CU", Name="Cuba", Medals = new Medals { Gold = 7, Silver = 3, Bronze = 5 } },
        new Country { Code = "CZ", Name="Czech Republic", Medals = new Medals { Gold = 4, Silver = 4, Bronze = 3 } },
        new Country { Code = "DK", Name="Denmark", Medals = new Medals { Gold = 3, Silver = 4, Bronze = 4 } },
        new Country { Code = "DO", Name="Dominican Republic", Medals = new Medals { Gold = 0, Silver = 3, Bronze = 2 } },
        new Country { Code = "EC", Name="Ecuador", Medals = new Medals { Gold = 2, Silver = 1, Bronze = 0 } },
        new Country { Code = "EE", Name="Estonia", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 1 } },
        new Country { Code = "ET", Name="Ethiopia", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 2 } },
        new Country { Code = "FJ", Name="Fiji", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 1 } },
        new Country { Code = "FI", Name="Finland", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 2 } },
        new Country { Code = "FR", Name="France", Medals = new Medals { Gold = 10, Silver = 12, Bronze = 11 } },
        new Country { Code = "GE", Name="Georgia", Medals = new Medals { Gold = 2, Silver = 5, Bronze = 1 } },
        new Country { Code = "DE", Name="Germany", Medals = new Medals { Gold = 10, Silver = 11, Bronze = 16 } },
        new Country { Code = "GH", Name="Ghana", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "GB", Name="Great Britain", Medals = new Medals { Gold = 22, Silver = 21, Bronze = 22 } },
        new Country { Code = "GR", Name="Greece", Medals = new Medals { Gold = 2, Silver = 1, Bronze = 1 } },
        new Country { Code = "GD", Name="Grenada", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "HK", Name="Hong Kong, China", Medals = new Medals { Gold = 1, Silver = 2, Bronze = 3 } },
        new Country { Code = "HU", Name="Hungary", Medals = new Medals { Gold = 6, Silver = 7, Bronze = 7 } },
        new Country { Code = "ID", Name="Indonesia", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 3 } },
        new Country { Code = "IE", Name="Ireland", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 2 } },
        new Country { Code = "IR", Name="Iran", Medals = new Medals { Gold = 3, Silver = 2, Bronze = 2 } },
        new Country { Code = "IL", Name="Israel", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 2 } },
        new Country { Code = "IT", Name="Italy", Medals = new Medals { Gold = 10, Silver = 10, Bronze = 20 } },
        new Country { Code = "JM", Name="Jamaica", Medals = new Medals { Gold = 4, Silver = 1, Bronze = 4 } },
        new Country { Code = "JO", Name="Jordan", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 1 } },
        new Country { Code = "KZ", Name="Kazakhstan", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 8 } },
        new Country { Code = "KE", Name="Kenya", Medals = new Medals { Gold = 4, Silver = 4, Bronze = 2 } },
        new Country { Code = "XK", Name="Kosovo", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 0 } },
        new Country { Code = "KW", Name="Kuwait", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "LV", Name="Latvia", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 1 } },
        new Country { Code = "LT", Name="Lithuania", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "MY", Name="Malaysia", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 1 } },
        new Country { Code = "MX", Name="Mexico", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 4 } },
        new Country { Code = "MA", Name="Morocco", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 0 } },
        new Country { Code = "NA", Name="Namibia", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "NL", Name="Netherlands", Medals = new Medals { Gold = 10, Silver = 12, Bronze = 14 } },
        new Country { Code = "NZ", Name="New Zealand", Medals = new Medals { Gold = 7, Silver = 6, Bronze = 7 } },
        new Country { Code = "MK", Name="North Macedonia", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "NO", Name="Norway", Medals = new Medals { Gold = 4, Silver = 2, Bronze = 2 } },
        new Country { Code = "PH", Name="Philippines", Medals = new Medals { Gold = 1, Silver = 2, Bronze = 1 } },
        new Country { Code = "PL", Name="Poland", Medals = new Medals { Gold = 4, Silver = 5, Bronze = 5 } },
        new Country { Code = "PT", Name="Portugal", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 2 } },
        new Country { Code = "PR", Name="Puerto Rico", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 0 } },
        new Country { Code = "QA", Name="Qatar", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 1 } },
        new Country { Code = "KR", Name="Republic of Korea", Medals = new Medals { Gold = 6, Silver = 4, Bronze = 10 } },
        new Country { Code = "MD", Name="Republic of Moldova", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "RO", Name="Romania", Medals = new Medals { Gold = 1, Silver = 3, Bronze = 0 } },
        new Country { Code = "SM", Name="San Marino", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 2 } },
        new Country { Code = "SA", Name="Saudi Arabia", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "RS", Name="Serbia", Medals = new Medals { Gold = 3, Silver = 1, Bronze = 5 } },
        new Country { Code = "SK", Name="Slovakia", Medals = new Medals { Gold = 1, Silver = 2, Bronze = 1 } },
        new Country { Code = "SI", Name="Slovenia", Medals = new Medals { Gold = 3, Silver = 1, Bronze = 1 } },
        new Country { Code = "ZA", Name="South Africa", Medals = new Medals { Gold = 1, Silver = 2, Bronze = 0 } },
        new Country { Code = "ES", Name="Spain", Medals = new Medals { Gold = 3, Silver = 8, Bronze = 6 } },
        new Country { Code = "SE", Name="Sweden", Medals = new Medals { Gold = 3, Silver = 6, Bronze = 0 } },
        new Country { Code = "CH", Name="Switzerland", Medals = new Medals { Gold = 3, Silver = 4, Bronze = 6 } },
        new Country { Code = "SY", Name="Syrian Arab Republic", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "TH", Name="Thailand", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 1 } },
        new Country { Code = "TR", Name="Turkey", Medals = new Medals { Gold = 2, Silver = 2, Bronze = 9 } },
        new Country { Code = "TM", Name="Turkmenistan", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "UA", Name="Ukraine", Medals = new Medals { Gold = 1, Silver = 6, Bronze = 12 } },
        new Country { Code = "US", Name="United States of America", Medals = new Medals { Gold = 39, Silver = 41, Bronze = 33 } },
        new Country { Code = "UZ", Name="Uzbekistan", Medals = new Medals { Gold = 3, Silver = 0, Bronze = 2 } },
        new Country { Code = "VE", Name="Venezuela", Medals = new Medals { Gold = 1, Silver = 3, Bronze = 0 } },
    };
}

public class Country
{
    public string Code { get; set; }
    public string Name { get; set; }
    public Medals Medals { get; set; }
}

public class Medals
{
    public int Gold { get; set; }
    public int Silver { get; set; }
    public int Bronze { get; set; }
    public int Total => Gold + Silver + Bronze;
}
