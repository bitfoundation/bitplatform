using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.DataGrid;

public partial class BitDataGridDemo
{
    private readonly List<ComponentParameter> componentParameters = new();
    private readonly List<EnumParameter> enumParameters = new();
    private readonly string example1HTMLCode = @"";
    private readonly string example1CSharpCode = @"";

    BitDataGridPaginationState pagination = new() { ItemsPerPage = 15 };
    IQueryable<Country> items;
    string nameFilter = string.Empty;
    BitDataGridSort<Country> rankSort = BitDataGridSort<Country>.ByDescending(x => x.Medals.Gold).ThenDescending(x => x.Medals.Silver).ThenDescending(x => x.Medals.Bronze);

    IQueryable<Country> FilteredItems => items?.Where(x => x.Name.Contains(nameFilter, StringComparison.CurrentCultureIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        items = (await GetCountriesAsync(0, null, null, true, CancellationToken.None)).Items.AsQueryable();
    }

    private async Task<BitDataGridItemsProviderResult<Country>> GetCountriesAsync(int startIndex, int? count, string sortBy, bool sortAscending, CancellationToken cancellationToken)
    {
        await Task.Delay(1000);

        var ordered = (sortBy, sortAscending) switch
        {
            (nameof(Country.Name), true) => _countries.OrderBy(c => c.Name),
            (nameof(Country.Name), false) => _countries.OrderByDescending(c => c.Name),
            (nameof(Country.Code), true) => _countries.OrderBy(c => c.Code),
            (nameof(Country.Code), false) => _countries.OrderByDescending(c => c.Code),
            ("Medals.Gold", true) => _countries.OrderBy(c => c.Medals.Gold),
            ("Medals.Gold", false) => _countries.OrderByDescending(c => c.Medals.Gold),
            _ => _countries.OrderByDescending(c => c.Medals.Gold),
        };

        var result = ordered.Skip(startIndex);

        if (count.HasValue)
        {
            result = result.Take(count.Value);
        }

        return BitDataGridItemsProviderResult.From(result.ToArray(), ordered.Count());
    }

    private readonly static Country[] _countries = new[]
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
        new Country { Code = "BG", Name="Bulgaria", Medals = new Medals { Gold = 3, Silver = 1, Bronze = 2 } },
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
        new Country { Code = "EG", Name="Egypt", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 4 } },
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
        new Country { Code = "IN", Name="India", Medals = new Medals { Gold = 1, Silver = 2, Bronze = 4 } },
        new Country { Code = "ID", Name="Indonesia", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 3 } },
        new Country { Code = "IE", Name="Ireland", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 2 } },
        new Country { Code = "IR", Name="Islamic Republic of Iran", Medals = new Medals { Gold = 3, Silver = 2, Bronze = 2 } },
        new Country { Code = "IL", Name="Israel", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 2 } },
        new Country { Code = "IT", Name="Italy", Medals = new Medals { Gold = 10, Silver = 10, Bronze = 20 } },
        new Country { Code = "JM", Name="Jamaica", Medals = new Medals { Gold = 4, Silver = 1, Bronze = 4 } },
        new Country { Code = "JP", Name="Japan", Medals = new Medals { Gold = 27, Silver = 14, Bronze = 17 } },
        new Country { Code = "JO", Name="Jordan", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 1 } },
        new Country { Code = "KZ", Name="Kazakhstan", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 8 } },
        new Country { Code = "KE", Name="Kenya", Medals = new Medals { Gold = 4, Silver = 4, Bronze = 2 } },
        new Country { Code = "XK", Name="Kosovo", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 0 } },
        new Country { Code = "KW", Name="Kuwait", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "KG", Name="Kyrgyzstan", Medals = new Medals { Gold = 0, Silver = 2, Bronze = 1 } },
        new Country { Code = "LV", Name="Latvia", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 1 } },
        new Country { Code = "LT", Name="Lithuania", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "MY", Name="Malaysia", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 1 } },
        new Country { Code = "MX", Name="Mexico", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 4 } },
        new Country { Code = "MN", Name="Mongolia", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 3 } },
        new Country { Code = "MA", Name="Morocco", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 0 } },
        new Country { Code = "NA", Name="Namibia", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "NL", Name="Netherlands", Medals = new Medals { Gold = 10, Silver = 12, Bronze = 14 } },
        new Country { Code = "NZ", Name="New Zealand", Medals = new Medals { Gold = 7, Silver = 6, Bronze = 7 } },
        new Country { Code = "NG", Name="Nigeria", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 1 } },
        new Country { Code = "MK", Name="North Macedonia", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "NO", Name="Norway", Medals = new Medals { Gold = 4, Silver = 2, Bronze = 2 } },
        new Country { Code = "CN", Name="People's Republic of China", Medals = new Medals { Gold = 38, Silver = 32, Bronze = 18 } },
        new Country { Code = "PH", Name="Philippines", Medals = new Medals { Gold = 1, Silver = 2, Bronze = 1 } },
        new Country { Code = "PL", Name="Poland", Medals = new Medals { Gold = 4, Silver = 5, Bronze = 5 } },
        new Country { Code = "PT", Name="Portugal", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 2 } },
        new Country { Code = "PR", Name="Puerto Rico", Medals = new Medals { Gold = 1, Silver = 0, Bronze = 0 } },
        new Country { Code = "QA", Name="Qatar", Medals = new Medals { Gold = 2, Silver = 0, Bronze = 1 } },
        new Country { Code = "KR", Name="Republic of Korea", Medals = new Medals { Gold = 6, Silver = 4, Bronze = 10 } },
        new Country { Code = "MD", Name="Republic of Moldova", Medals = new Medals { Gold = 0, Silver = 0, Bronze = 1 } },
        new Country { Code = "ROC", Name="ROC", Medals = new Medals { Gold = 20, Silver = 28, Bronze = 23 } },
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
        new Country { Code = "TN", Name="Tunisia", Medals = new Medals { Gold = 1, Silver = 1, Bronze = 0 } },
        new Country { Code = "TR", Name="Turkey", Medals = new Medals { Gold = 2, Silver = 2, Bronze = 9 } },
        new Country { Code = "TM", Name="Turkmenistan", Medals = new Medals { Gold = 0, Silver = 1, Bronze = 0 } },
        new Country { Code = "UG", Name="Uganda", Medals = new Medals { Gold = 2, Silver = 1, Bronze = 1 } },
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
