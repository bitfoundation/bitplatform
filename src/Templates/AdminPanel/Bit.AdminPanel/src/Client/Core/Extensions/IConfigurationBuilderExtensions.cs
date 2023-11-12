using System.Reflection;

namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddClientConfigurations(this IConfigurationBuilder builder)
    {
        var assembly = Assembly.Load("AdminPanel.Client.Core");
        builder.AddJsonStream(assembly.GetManifestResourceStream("AdminPanel.Client.Core.appsettings.json")!);
    }
}
