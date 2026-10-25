namespace Microsoft.Extensions.Configuration;

public static class IConfigurationExtensions
{
    public static string GetApiServerAddress(this IConfiguration configuration)
    {
        var apiServerAddress = configuration.GetValue("ApiServerAddress", defaultValue: "").TrimEnd('/').ToString();

        return Uri.TryCreate(apiServerAddress, UriKind.RelativeOrAbsolute, out _)
            ? apiServerAddress
            : throw new InvalidOperationException($"Api server address {apiServerAddress} is invalid");
    }

    /// <summary>
    /// Joins a relative api path onto the api server address. The address comes back without its trailing
    /// slash, so a plain concatenation only works while it is empty; this puts the separator back otherwise.
    /// </summary>
    public static string GetApiUrl(this IConfiguration configuration, string relativePath)
    {
        var apiServerAddress = configuration.GetApiServerAddress();

        return string.IsNullOrEmpty(apiServerAddress)
            ? relativePath
            : $"{apiServerAddress}/{relativePath.TrimStart('/')}";
    }
}
