using System.Reflection;
using Microsoft.Extensions.Configuration.Json;

namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddSharedConfigurations(this IConfigurationBuilder builder)
    {
        List<Stream?> appsettings = [];

        var sharedAssembly = Assembly.Load("Boilerplate.Shared");

        appsettings.Add(sharedAssembly.GetManifestResourceStream("Boilerplate.Shared.appsettings.json"));
        appsettings.Add(sharedAssembly.GetManifestResourceStream($"Boilerplate.Shared.appsettings.{AppEnvironment.Current}.json"));

        var clonedConfigurations = builder.Sources.ToArray(); // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration#default-application-configuration-sources
        if (builder.Sources.Any())
            builder.Sources.Clear();

        builder.Sources.AddRange(appsettings.Where(appsetting => appsetting is not null).Select(appsetting => new JsonStreamConfigurationSource { Stream = appsetting! }));
        builder.Sources.AddRange(clonedConfigurations);
    }
}
