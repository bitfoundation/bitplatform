using System.Reflection;

namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddClientConfigurations(this IConfigurationBuilder builder)
    {
        var assembly = Assembly.Load("Bit.Websites.Careers.Client");
        builder.AddJsonStream(assembly.GetManifestResourceStream("Bit.Websites.Careers.Client.appsettings.json")!);
    }
}
