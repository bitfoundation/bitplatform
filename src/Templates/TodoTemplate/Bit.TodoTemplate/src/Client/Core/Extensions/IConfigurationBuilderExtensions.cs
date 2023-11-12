using System.Reflection;

namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddClientConfigurations(this IConfigurationBuilder builder)
    {
        var assembly = Assembly.Load("TodoTemplate.Client.Core");
        builder.AddJsonStream(assembly.GetManifestResourceStream("TodoTemplate.Client.Core.appsettings.json")!);
    }
}
