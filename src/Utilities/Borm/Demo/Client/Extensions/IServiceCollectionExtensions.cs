using Bit.Borm;
using Bit.Borm.Demo.Client.Forms;
using Bit.Borm.Demo.Client.Renderers;
using FluentValidation;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The services shared by the WebAssembly client and the server host. Both containers have to
/// register the same set: every page is prerendered on the server before it runs in the browser,
/// so anything a page injects must resolve in both.
/// </summary>
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services)
    {
        // Borm's rendering services, with a custom renderer ("rating") to show the registry is open.
        services.AddBorm(registry => registry.RegisterCustom<StarRatingInput>("rating"));

        // The FluentValidation validator the FluentValidation demo resolves.
        services.AddScoped<IValidator<JobApplication>, JobApplicationValidator>();

        // Stand-in repository behind the async validation and cascading-options demos.
        services.AddSingleton<DemoDirectory>();

        return services;
    }
}
