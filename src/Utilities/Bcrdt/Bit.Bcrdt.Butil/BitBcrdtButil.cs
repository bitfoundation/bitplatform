using Bit.Butil;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Bcrdt.Butil;

/// <summary>Configures bit Bcrdt to follow the browser's page and network lifecycle, through Bit.Butil.</summary>
public static class BitBcrdtButil
{
    /// <summary>
    /// Registers a <see cref="BcrdtBrowserLifecycle"/> for the collaboration service's documents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registered per scope, like the collaboration service it acts for, together with the four Butil services it
    /// uses (<c>Window</c>, <c>Document</c>, <c>Navigator</c>, <c>StorageManager</c>) - with <c>TryAdd</c>, so an
    /// application that also calls <c>AddBitButilServices</c> shares them. Nothing listens until it is started,
    /// once the application runs in the browser:
    /// </para>
    /// <code>
    /// WebAssemblyHost host = builder.Build();
    /// await host.Services.GetRequiredService&lt;BcrdtBrowserLifecycle&gt;().StartAsync();
    /// await host.RunAsync();
    /// </code>
    /// <para>
    /// Outside a browser starting it does nothing, so the same registration can stay in a project that is also
    /// prerendered or hosted on the server.
    /// </para>
    /// </remarks>
    /// <param name="builder">The collaboration being configured.</param>
    /// <param name="configure">Adjusts what the lifecycle does.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static BcrdtCollaborationBuilder UseBrowserLifecycle(
        this BcrdtCollaborationBuilder builder,
        Action<BcrdtBrowserLifecycleOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new BcrdtBrowserLifecycleOptions();
        configure?.Invoke(options);

        builder.Services.TryAddScoped<Window>();
        builder.Services.TryAddScoped<Document>();
        builder.Services.TryAddScoped<Navigator>();
        builder.Services.TryAddScoped<StorageManager>();
        builder.Services.TryAddScoped(services => new BcrdtBrowserLifecycle(
            services.GetRequiredService<IBcrdtCollaborationService>(),
            services.GetRequiredService<Window>(),
            services.GetRequiredService<Document>(),
            services.GetRequiredService<Navigator>(),
            services.GetRequiredService<StorageManager>(),
            options));

        return builder;
    }
}
