using System.Reflection;

namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddClientConfigurations(this IConfigurationBuilder builder)
    {
        var assembly = Assembly.Load("Boilerplate.Client.Core");
        builder.AddJsonStream(assembly.GetManifestResourceStream("Boilerplate.Client.Core.appsettings.json")!);
    }
}
