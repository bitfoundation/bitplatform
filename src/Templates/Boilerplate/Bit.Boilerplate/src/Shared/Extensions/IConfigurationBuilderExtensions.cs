using System.Reflection;

namespace Microsoft.Extensions.Configuration;

public static partial class IConfigurationBuilderExtensions
{
    /// <summary>
    /// Configuration priority (Lowest to highest) =>
    /// Shared/appsettings.json
    /// Shared/appsettings.{environment}.json (If present)
    ///     Server.Api only =>
    ///         Server/appsettings.json
    ///         Server/appsettings.{environment}.json (If present)
    ///         https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration#default-application-configuration-sources
    /// </summary>
    public static void AddSharedConfigurations(this IConfigurationBuilder builder)
    {
        var configBuilder = new ConfigurationBuilder();

        var sharedAssembly = Assembly.Load("Boilerplate.Shared");

        configBuilder.AddJsonStream(sharedAssembly.GetManifestResourceStream("Boilerplate.Shared.appsettings.json")!);

        var envSharedAppSettings = sharedAssembly.GetManifestResourceStream($"Boilerplate.Shared.appsettings.{AppEnvironment.Current}.json");
        if (envSharedAppSettings != null)
        {
            configBuilder.AddJsonStream(envSharedAppSettings);
        }

        var originalSources = builder.Sources.ToList();
        builder.Sources.Clear();
        foreach (var source in configBuilder.Sources.Union(originalSources))
        {
            builder.Sources.Add(source);
        }
    }
}
