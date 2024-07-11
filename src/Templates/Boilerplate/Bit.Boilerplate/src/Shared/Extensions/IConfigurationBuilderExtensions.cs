using System.Reflection;
using Microsoft.Extensions.Configuration.Json;

namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddSharedConfigurations(this IConfigurationBuilder builder)
    {
        List<Stream?> appSettingSources = [];

        var sharedAssembly = Assembly.Load("Boilerplate.Shared");

        appSettingSources.Add(sharedAssembly.GetManifestResourceStream("Boilerplate.Shared.appsettings.json"));
        appSettingSources.Add(sharedAssembly.GetManifestResourceStream($"Boilerplate.Shared.appsettings.{AppEnvironment.Current}.json"));

        var originalSources = builder.Sources.ToArray(); // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration#default-application-configuration-sources
        if (builder.Sources.IsReadOnly is false)
        {
            builder.Sources.Clear();
        }

        builder.Sources.AddRange(appSettingSources.Where(appsetting => appsetting is not null).Select(appsetting => new JsonStreamConfigurationSource { Stream = appsetting! }));
        builder.Sources.AddRange(originalSources);
    }
}
