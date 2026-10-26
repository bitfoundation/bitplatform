using Bit.Blex.Maui;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddMauiServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in Android, iOS, Windows, and macOS.
        // Scoped, exactly as in any Blazor app: the BlazorWebView creates the scope.
        services.AddCoreServices();

        // Persisted stores go to OS-native Preferences (SharedPreferences, NSUserDefaults, the Windows
        // local settings) rather than the WebView profile's localStorage, which AddBitBlexLocalStoragePersistence
        // would use just as well.
        services.AddBitBlexPreferencesPersistence(options =>
            options.DebounceInterval = TimeSpan.FromMilliseconds(300));

        return services;
    }
}
