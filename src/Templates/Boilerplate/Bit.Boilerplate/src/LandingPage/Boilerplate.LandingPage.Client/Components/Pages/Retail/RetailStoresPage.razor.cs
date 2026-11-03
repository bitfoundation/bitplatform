using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Client.Components.Pages.Retail;

public partial class RetailStoresPage : IDisposable
{
    [AutoInject] private IStringLocalizer<RetailStrings> R = default!;

    private bool mapReady;
    private DateTime? now;
    private string selected = RetailShops.All[0].Key;
    private JsonObject? structuredData;
    private PeriodicTimer? timer;

    private RetailShop Selected => RetailShops.All.FirstOrDefault(s => s.Key == selected, RetailShops.All[0]);

    protected override void OnParametersSet()
    {
        structuredData = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new JsonArray([.. RetailShops.All.Select(shop => (JsonNode)new JsonObject
            {
                ["@type"] = "GroceryStore",
                ["name"] = $"Northside Market {R[shop.NameKey].Value}",
                ["telephone"] = shop.Phone,
                ["address"] = new JsonObject
                {
                    ["@type"] = "PostalAddress",
                    ["streetAddress"] = shop.Address,
                    ["addressLocality"] = "Dublin",
                    ["addressCountry"] = "IE"
                },
                ["geo"] = new JsonObject
                {
                    ["@type"] = "GeoCoordinates",
                    ["latitude"] = shop.Latitude,
                    ["longitude"] = shop.Longitude
                },
                ["openingHoursSpecification"] = new JsonArray([.. Enum.GetValues<DayOfWeek>().Select(day => (JsonNode)new JsonObject
                {
                    ["@type"] = "OpeningHoursSpecification",
                    ["dayOfWeek"] = day.ToString(),
                    ["opens"] = shop.Week[(int)day].Open.ToString("HH:mm", CultureInfo.InvariantCulture),
                    ["closes"] = shop.Week[(int)day].Close.ToString("HH:mm", CultureInfo.InvariantCulture)
                })])
            })])
        };

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RendererInfo.IsInteractive)
        {
            mapReady = true;
            now = RetailShops.DublinNow();
            StateHasChanged();

            _ = Tick();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task Tick()
    {
        timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync())
        {
            now = RetailShops.DublinNow();
            await InvokeAsync(StateHasChanged);
        }
    }

    private static IEnumerable<DayOfWeek> Week()
    {
        var first = CurrentCulture.DateTimeFormat.FirstDayOfWeek is DayOfWeek.Sunday ? DayOfWeek.Monday : CurrentCulture.DateTimeFormat.FirstDayOfWeek;

        return Enumerable.Range(0, 7).Select(offset => (DayOfWeek)(((int)first + offset) % 7));
    }

    private static List<(List<DayOfWeek> Days, RetailDayHours Hours)> Groups(RetailShop shop)
    {
        var groups = new List<(List<DayOfWeek> Days, RetailDayHours Hours)>();

        foreach (var day in Week())
        {
            var hours = shop.Week[(int)day];

            if (groups.Count > 0 && groups[^1].Hours == hours)
            {
                groups[^1].Days.Add(day);
            }
            else
            {
                groups.Add((new List<DayOfWeek> { day }, hours));
            }
        }

        return groups;
    }

    private string DaysLabel(List<DayOfWeek> days)
    {
        var names = CurrentCulture.DateTimeFormat;

        return days.Count switch
        {
            1 => names.GetDayName(days[0]),
            7 => R[nameof(RetailStrings.StoresEveryDay)],
            _ => R[nameof(RetailStrings.StoresDayRange), names.GetDayName(days[0]), names.GetDayName(days[^1])]
        };
    }

    public void Dispose() => timer?.Dispose();
}
