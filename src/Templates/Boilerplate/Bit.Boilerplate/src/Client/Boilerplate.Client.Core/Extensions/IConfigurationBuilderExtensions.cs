using System.Reflection;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Microsoft.Extensions.Configuration;

public static partial class IConfigurationBuilderExtensions
{
    /// <summary>
    /// Configuration priority (Lowest to highest) =>
    /// Shared/appsettings.json
    /// Shared/appsettings.{environment}.json (If present)
    /// Client/Core/appsettings.json
    /// Client/Core/appsettings.{environment}.json (If present)
    ///     Server.Web (blazor server and pre-rendering) and/or Server.Api only =>
    ///         Server/appsettings.json
    ///         Server/appsettings.{environment}.json (If present)
    ///         https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration#default-application-configuration-sources
    ///     Blazor WebAssembly only =>
    ///         Client/Client.Web/appsettings.json
    ///         Client/Client.Web/appsettings.{environment}.json (If present)
    ///         Client/Client.Web/wwwroot/appsettings.json (If present)
    ///         Client/Client.Web/wwwroot/{environment}.json (If present)
    ///     Maui only =>
    ///         Client/Client.Maui/appsettings.json
    ///         Client/Client.Maui/appsettings.{environment}.json (If present)
    ///     Windows only =>
    ///         Client/Client.Windows/appsettings.json
    ///         Client/Client.Windows/appsettings.{environment}.json (If present)
    /// </summary>
    public static IConfigurationBuilder AddClientConfigurations(this IConfigurationBuilder builder, string clientEntryAssemblyName)
    {
        IConfigurationBuilder configBuilder = AppPlatform.IsBrowser ? new WebAssemblyHostConfiguration() : new ConfigurationBuilder();

        var sharedAssembly = Assembly.Load("Boilerplate.Shared");

        configBuilder.AddJsonStream(sharedAssembly.GetManifestResourceStream("Boilerplate.Shared.appsettings.json")!);

        var envSharedAppSettings = sharedAssembly.GetManifestResourceStream($"Boilerplate.Shared.appsettings.{AppEnvironment.Current}.json");
        if (envSharedAppSettings != null)
        {
            configBuilder.AddJsonStream(envSharedAppSettings);
        }

        var clientCoreAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(asm => asm.GetName()?.Name is "Boilerplate.Client.Core");

        configBuilder.AddJsonStream(clientCoreAssembly.GetManifestResourceStream("Boilerplate.Client.Core.appsettings.json")!);

        var envClientCoreAppSettings = clientCoreAssembly.GetManifestResourceStream($"Boilerplate.Client.Core.appsettings.{AppEnvironment.Current}.json");
        if (envClientCoreAppSettings != null)
        {
            configBuilder.AddJsonStream(envClientCoreAppSettings);
        }

        var clientEntryAssembly = Assembly.Load(clientEntryAssemblyName);

        if (clientEntryAssembly is not null)
        {
            configBuilder.AddJsonStream(clientEntryAssembly.GetManifestResourceStream($"{clientEntryAssemblyName}.appsettings.json")!);

            var envAppSettings = clientEntryAssembly.GetManifestResourceStream($"{clientEntryAssemblyName}.appsettings.{AppEnvironment.Current}.json");
            if (envAppSettings != null)
            {
                configBuilder.AddJsonStream(envAppSettings);
            }
        }

        if (AppPlatform.IsBrowser)
        {
            var providersField = builder.GetType().GetField("_providers", BindingFlags.NonPublic | BindingFlags.Instance)!;
            providersField.SetValue(builder, (((IConfigurationRoot)configBuilder).Providers).Union(((IConfigurationRoot)builder).Providers).ToList());
        }
        else if (AppPlatform.IsBlazorHybrid)
        {
            foreach (var source in configBuilder.Sources)
            {
                builder.Sources.Add(source);
            }
        }
        else
        {
            var originalSources = builder.Sources.ToList();
            builder.Sources.Clear();
            foreach (var source in configBuilder.Sources.Union(originalSources))
            {
                builder.Sources.Add(source);
            }
        }

        return builder;
    }
}
