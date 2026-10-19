using Bit.BlazeDb.Demo.Client.Components;
using Bit.BlazeDb.Demo.Client.Data;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The services of the demo's WebAssembly client.
/// <para>
/// Singletons rather than scoped services, because a WebAssembly app has exactly one scope per tab anyway
/// and the demo database is the tab's: <see cref="DemoDbService"/> opens it once - on OPFS when this tab
/// wins the Web Locks election - and every page shares it. The server host registers none of these: it
/// prerenders nothing, so no component ever runs there.
/// </para>
/// </summary>
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services)
    {
        services.AddSingleton<DemoDbService>();
        services.AddSingleton<AppInterop>();

        return services;
    }
}
