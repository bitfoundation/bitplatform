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
        // Borm's rendering services, with a custom renderer ("rating") to show the registry is open, and
        // two made of Blazor's own InputText and InputNumber for the edit context demo.
        services.AddBorm(registry => registry
            .RegisterCustom<StarRatingInput>("rating")
            .RegisterCustom<BlazorTextInput>("blazor-text")
            .RegisterCustom<BlazorNumberInput>("blazor-number"));

        // The library's own words in a form's language - the localization demo's buttons and messages. Every other
        // page follows the UI culture, which InvariantGlobalization keeps invariant, so they read English.
        services.AddBormTranslations();

        // A function of the application's own for the expressions demo: callable from any expression - a schema the
        // server sends included - so it must be safe with any argument, and returns null for one it has no answer to.
        // Registered at startup, before an expression naming it is parsed; registering it again replaces it.
        BormExpressionFunctions.Register("vatRate", args => args[0] is string country
            ? country.ToUpperInvariant() switch { "NL" => 0.21m, "DE" => 0.19m, "FR" or "GB" => 0.20m, _ => null }
            : null, minArgs: 1, maxArgs: 1);

        // The FluentValidation validator the FluentValidation demo resolves.
        services.AddScoped<IValidator<JobApplication>, JobApplicationValidator>();

        // Stand-in repository behind the async validation and cascading-options demos.
        services.AddSingleton<DemoDirectory>();

        return services;
    }
}
