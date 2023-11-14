using System.Reflection;

namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddClientConfigurations(this IConfigurationBuilder builder)
    {
        var assembly = Assembly.Load("Bit.Websites.Sales.Web");
        builder.AddJsonStream(assembly.GetManifestResourceStream("Bit.Websites.Sales.Web.appsettings.json")!);
    }
}
