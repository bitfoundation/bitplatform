using Bit.BlazeDb.Samples.Core.Data;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Registered by every container a sample page is created in - the server's (prerendering, and Interactive
    /// Server circuits), the browser's (Interactive WebAssembly) and the MAUI app's - each with the
    /// <see cref="SampleDatabase"/> that fits where it runs.
    /// </summary>
    public static IServiceCollection AddCoreServices<TDatabase>(this IServiceCollection services)
        where TDatabase : SampleDatabase
    {
        ArgumentNullException.ThrowIfNull(services);

        // A singleton: the database is opened once and shared - by every page of a browser tab or of the MAUI
        // app, and on a server by every circuit, which is why ServerSampleDatabase serializes them.
        services.AddSingleton<SampleDatabase, TDatabase>();

        return services;
    }
}
