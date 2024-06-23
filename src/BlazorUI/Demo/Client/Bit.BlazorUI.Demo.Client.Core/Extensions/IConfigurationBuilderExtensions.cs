using System.Reflection;

namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddClientConfigurations(this IConfigurationBuilder builder)
    {
        var assembly = Assembly.Load("Bit.BlazorUI.Demo.Client.Core");
        builder.AddJsonStream(assembly.GetManifestResourceStream("Bit.BlazorUI.Demo.Client.Core.appsettings.json")!);
    }
}
