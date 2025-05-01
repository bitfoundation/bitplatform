using Microsoft.Extensions.DependencyInjection;

namespace Bit.Butil;

public static class BitButil
{
    public static IServiceCollection AddBitButilServices(this IServiceCollection services)
    {
        services.AddTransient<Clipboard>();
        services.AddTransient<Console>();
        services.AddTransient<Cookie>();
        services.AddTransient<Crypto>();
        services.AddTransient<Document>();
        services.AddTransient<History>();
        services.AddTransient<Keyboard>();
        services.AddTransient<LocalStorage>();
        services.AddTransient<SessionStorage>();
        services.AddTransient<Location>();
        services.AddTransient<Navigator>();
        services.AddTransient<Notification>();
        services.AddTransient<Screen>();
        services.AddTransient<ScreenOrientation>();
        services.AddTransient<UserAgent>();
        services.AddTransient<VisualViewport>();
        services.AddTransient<Window>();
        services.AddTransient<WebAuthn>();

        return services;
    }

    internal static bool FastInvokeEnabled { get; private set; }

    /// <summary>
    /// Enables the use of the fast APIs globally when available (Invoke methods of IJSInProcessRuntime).
    /// </summary>
    public static void UseFastInvoke()
    {
        FastInvokeEnabled = true;
    }

    /// <summary>
    /// Disables the use of the fast APIs globally when available (Invoke methods of IJSInProcessRuntime).
    /// </summary>
    public static void UseNormalInvoke()
    {
        FastInvokeEnabled = false;
    }
}
