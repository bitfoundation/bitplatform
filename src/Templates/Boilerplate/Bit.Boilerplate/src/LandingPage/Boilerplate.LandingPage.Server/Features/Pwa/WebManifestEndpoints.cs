using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Server.Features.Pwa;

public static class WebManifestEndpoints
{
    public static void MapLandingWebManifests(this WebApplication app)
    {
        foreach (var site in LandingSites.All)
        {
            app.MapGet($"{site.Path}/manifest.webmanifest", (HttpContext context) => Manifest(context, site, LandingCultures.Default)).ExcludeFromDescription();

            if (LandingCultures.InvariantGlobalization is false)
            {
                app.MapGet($"/{{culture}}{site.Path}/manifest.webmanifest", (HttpContext context, string culture) =>
                    LandingCultures.IsPrefix(culture) ? Manifest(context, site, LandingCultures.Find(culture)!) : Results.NotFound()).ExcludeFromDescription();
            }
        }
    }

    private static IResult Manifest(HttpContext context, LandingSite site, CultureInfo culture)
    {
        var home = LandingCultures.Localize(site.Path.Length is 0 ? LandingRoutes.Home : site.Path, culture);

        var manifest = new JsonObject
        {
            ["id"] = home,
            ["name"] = site.Name,
            ["short_name"] = site.Name,
            ["lang"] = culture.Name,
            ["dir"] = culture.TextInfo.IsRightToLeft ? "rtl" : "ltr",
            ["start_url"] = home,
            ["scope"] = home,
            ["display"] = "standalone",
            ["theme_color"] = site.AccentColor,
            ["background_color"] = "#FFFFFF",
            ["icons"] = new JsonArray(new JsonObject
            {
                ["src"] = $"{context.Request.PathBase}/images/icons/bit-icon-512.png",
                ["sizes"] = "512x512",
                ["type"] = "image/png"
            })
        };

        return Results.Text(manifest.ToJsonString(), "application/manifest+json");
    }
}
