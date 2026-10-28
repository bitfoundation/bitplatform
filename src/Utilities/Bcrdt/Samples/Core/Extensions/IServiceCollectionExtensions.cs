using Bit.Bcrdt.Samples.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// The services the sample pages inject. Register them in every project that renders the pages - in a Blazor Web
    /// App that is both the server and the client project, because a page is prerendered on the server before it
    /// runs in the browser.
    /// </summary>
    /// <param name="services">The container to register with.</param>
    /// <param name="configure">
    /// The host's own choice of transport and store: the one thing that differs from one hosting model to the next.
    /// </param>
    public static IServiceCollection AddCoreServices(this IServiceCollection services, Action<BcrdtCollaborationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Services registered in this class can be injected in client side (Web, Android, iOS, Windows, macOS)
        services.AddBitBcrdtServices(configure);

        services.AddScoped<SampleSession>();

        return services;
    }

    /// <summary>
    /// Connects documents through the <c>BcrdtHub</c> of the web host. See <see cref="SamplesRelay"/>.
    /// </summary>
    /// <remarks>
    /// Registered ahead of the transports <see cref="BcrdtCollaborationBuilder"/> registers itself, and per scope,
    /// because in the browser it reads the page's address from the scope's <see cref="NavigationManager"/>.
    /// </remarks>
    /// <param name="hubAddress">
    /// Where the hub is, resolved from the scope's services. Omitted, it is the page's own address, which is right in
    /// the browser and only there: on a server that address comes from the browser, so a server passes one of its own
    /// - <see cref="SamplesRelay.LocalHubAddress"/>.
    /// </param>
    public static BcrdtCollaborationBuilder UseSamplesRelay(this BcrdtCollaborationBuilder collaboration, Func<IServiceProvider, Uri>? hubAddress = null)
    {
        ArgumentNullException.ThrowIfNull(collaboration);

        hubAddress ??= services => new Uri(new Uri(services.GetRequiredService<NavigationManager>().BaseUri), "hub/bcrdt");
        collaboration.Services.TryAddScoped<IBcrdtTransportFactory>(services => new SamplesRelay(() => hubAddress(services)));

        return collaboration;
    }
}
