using System.Reflection;

namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddClientAppConfigurations(this IConfigurationBuilder builder)
    {
        var assembly = Assembly.Load("BlazorWeb.Web");
        builder.AddJsonStream(assembly.GetManifestResourceStream("BlazorWeb.Web.appsettings.json")!);
    }
}
