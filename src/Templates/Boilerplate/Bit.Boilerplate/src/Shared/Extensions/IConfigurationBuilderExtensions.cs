using System.Reflection;

namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddSharedConfigurations(this IConfigurationBuilder builder)
    {
        var assembly = Assembly.Load("Boilerplate.Shared");
        builder.AddJsonStream(assembly.GetManifestResourceStream("Boilerplate.Shared.appsettings.json")!);

        if (BuildConfiguration.IsDebug())
        {
            var settings = assembly.GetManifestResourceStream("Boilerplate.Shared.appsettings.Debug.json");
            if (settings is not null)
            {
                builder.AddJsonStream(settings);
            }
        }
        else
        {
            var settings = assembly.GetManifestResourceStream("Boilerplate.Shared.appsettings.Release.json");
            if (settings is not null)
            {
                builder.AddJsonStream(settings);
            }
        }
    }
}
