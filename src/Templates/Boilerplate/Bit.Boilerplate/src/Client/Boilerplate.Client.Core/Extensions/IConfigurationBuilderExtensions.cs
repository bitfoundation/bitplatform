using System.Reflection;

namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddClientConfigurations(this IConfigurationBuilder builder)
    {
        builder.AddSharedConfigurations();

        var assembly = Assembly.Load("Boilerplate.Client.Core");
        builder.AddJsonStream(assembly.GetManifestResourceStream("Boilerplate.Client.Core.appsettings.json")!);

        if (BuildConfiguration.IsDebug())
        {
            var settings = assembly.GetManifestResourceStream("Boilerplate.Client.Core.appsettings.Debug.json");
            if (settings is not null)
            {
                builder.AddJsonStream(settings);
            }
        }
        else
        {
            var settings = assembly.GetManifestResourceStream("Boilerplate.Client.Core.appsettings.Release.json");
            if (settings is not null)
            {
                builder.AddJsonStream(settings);
            }
        }
    }
}
