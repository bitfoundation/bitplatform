namespace Microsoft.Extensions.Configuration;
public static class IConfigurationExtensions
{
    public static string GetApiServerAddress(this IConfiguration configuration)
    {
#if BlazorWebAssembly
        return "api/";
#else
        return configuration.GetValue<string?>("ApiServerAddress") ?? throw new InvalidOperationException("Could not find ApiServerAddress config");
#endif
    }
}
