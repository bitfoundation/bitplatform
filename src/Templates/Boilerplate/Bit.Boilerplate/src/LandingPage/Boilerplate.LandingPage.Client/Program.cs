using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Boilerplate.LandingPage.Client;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Configuration.AddLandingPageClientConfigurations(builder.HostEnvironment.Environment);

        // Swaps the culture WebAssembly started in for the customized one in LandingCultures, Persian calendar included
        LandingCultures.Apply(LandingCultures.Find(CultureInfo.CurrentUICulture.Name) ?? LandingCultures.Default);

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        builder.Services.AddLandingPageClientServices(builder.Configuration);

        await builder.Build().RunAsync();
    }
}
