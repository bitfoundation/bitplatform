using Boilerplate.LandingPage.Client.Components.Pages.Ecommerce;
using Boilerplate.LandingPage.Client.Components.Pages.Hotel;
using Boilerplate.LandingPage.Client.Components.Pages.Retail;

namespace Boilerplate.LandingPage.Client.Infrastructure;

public static class LandingSites
{
    public static LandingSite[] All { get; } =
    [
        EcommerceSite.Info,
        RetailSite.Info,
        HotelSite.Info
    ];

    public static LandingSite? Find(string? key) => All.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));

    public static LandingSite? FromPath(string absolutePath)
    {
        var (_, path) = LandingCultures.Split(absolutePath);

        return All.FirstOrDefault(s => path == s.Path || path.StartsWith($"{s.Path}/", StringComparison.OrdinalIgnoreCase));
    }
}
