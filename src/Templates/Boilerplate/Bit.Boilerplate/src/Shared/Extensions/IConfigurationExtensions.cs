//-:cnd:noEmit
namespace Microsoft.Extensions.Configuration;
public static class IConfigurationExtensions
{
    public static T GetRequiredValue<T>(this IConfiguration configuration, string name)
    {
        return (configuration ?? throw new ArgumentNullException(nameof(configuration)))
            .GetValue<T>(name ?? throw new ArgumentNullException(nameof(name))) ?? throw new InvalidOperationException($"{name} config could not be found");
    }

    public static string GetServerAddress(this IConfiguration configuration)
    {
        var serverAddress = configuration.GetRequiredValue<string>("ServerAddress");

        if (AppEnvironment.IsDev() &&
            serverAddress.Contains("localhost", StringComparison.InvariantCultureIgnoreCase) &&
            OperatingSystem.IsAndroid())
        {
            const string androidEmulatorDevMachineIP = "10.0.2.2"; // Special alias to your host loopback interface in Android Emulators (127.0.0.1 on your development machine)
            serverAddress = serverAddress.Replace("localhost", androidEmulatorDevMachineIP, StringComparison.InvariantCultureIgnoreCase);
        }

        return Uri.TryCreate(serverAddress, UriKind.RelativeOrAbsolute, out _)
            ? serverAddress.TrimEnd('/')
            : throw new InvalidOperationException($"Api server address {serverAddress} is invalid");
    }

    public static string GetWebClientUrl(this IConfiguration configuration)
    {
        var webClientUrl = configuration.GetValue<string?>("WebClientUrl");

        return string.IsNullOrEmpty(webClientUrl) is false ? webClientUrl : configuration.GetServerAddress();
    }
}
