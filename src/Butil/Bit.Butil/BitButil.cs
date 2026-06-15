using Microsoft.Extensions.DependencyInjection;

namespace Bit.Butil;

public static class BitButil
{
    public static IServiceCollection AddBitButilServices(this IServiceCollection services)
    {
        // Scoped matches Blazor's "one circuit / one WASM app instance per user" model.
        // Transient would create a fresh wrapper on every @inject, fragmenting per-instance
        // listener bookkeeping and keeping captured component delegates alive longer than
        // the component itself.
        services.AddScoped<Clipboard>();
        services.AddScoped<Console>();
        services.AddScoped<Cookie>();
        services.AddScoped<CookieStore>();
        services.AddScoped<Crypto>();
        services.AddScoped<Battery>();
        services.AddScoped<BackgroundSync>();
        services.AddScoped<BroadcastChannel>();
        services.AddScoped<CacheStorage>();
        services.AddScoped<ContactPicker>();
        services.AddScoped<Document>();
        services.AddScoped<EyeDropper>();
        services.AddScoped<Fetch>();
        services.AddScoped<FileReader>();
        services.AddScoped<Geolocation>();
        services.AddScoped<History>();
        services.AddScoped<IdleDetector>();
        services.AddScoped<IndexedDb>();
        services.AddScoped<Keyboard>();
        services.AddScoped<LocalStorage>();
        services.AddScoped<SessionStorage>();
        services.AddScoped<Location>();
        services.AddScoped<MediaDevices>();
        services.AddScoped<Navigator>();
        services.AddScoped<NetworkInformation>();
        services.AddScoped<Nfc>();
        services.AddScoped<Notification>();
        services.AddScoped<ObjectUrls>();
        services.AddScoped<Performance>();
        services.AddScoped<Permissions>();
        services.AddScoped<Push>();
        services.AddScoped<Reporting>();
        services.AddScoped<Screen>();
        services.AddScoped<ScreenOrientation>();
        services.AddScoped<ServiceWorker>();
        services.AddScoped<SpeechRecognition>();
        services.AddScoped<SpeechSynthesis>();
        services.AddScoped<StorageManager>();
        services.AddScoped<UserAgent>();
        services.AddScoped<VisualViewport>();
        services.AddScoped<WakeLock>();
        services.AddScoped<WebAudio>();
        services.AddScoped<WebLocks>();
        services.AddScoped<Window>();
        services.AddScoped<WebAuthn>();

        return services;
    }

    private static volatile bool _fastInvokeEnabled;

    internal static bool FastInvokeEnabled => _fastInvokeEnabled;

    /// <summary>
    /// Enables the synchronous in-process ("fast") invoke path for the APIs that opt into it.
    /// <br/>
    /// Only APIs backed by synchronous JavaScript functions (for example <see cref="LocalStorage"/>,
    /// <see cref="SessionStorage"/>, <see cref="Cookie"/>, <see cref="Console"/> and <see cref="Location"/>)
    /// use this path; everything that wraps an asynchronous (Promise-returning) browser API always runs
    /// asynchronously regardless of this setting, so enabling it can't break those calls.
    /// Only effective on Blazor WebAssembly (where an <see cref="Microsoft.JSInterop.IJSInProcessRuntime"/> is available).
    /// <br/>
    /// NOTE: this is a process-wide static toggle, not per-app/per-circuit. It is intended to be set
    /// once at startup. On Blazor Server it is effectively a no-op (the fast path always falls back to
    /// the async path because there is no in-process runtime), so sharing it across circuits is benign.
    /// </summary>
    public static void UseFastInvoke()
    {
        _fastInvokeEnabled = true;
    }

    /// <summary>
    /// Disables the synchronous in-process ("fast") invoke path; all calls run asynchronously.
    /// Process-wide static toggle - see <see cref="UseFastInvoke"/>.
    /// </summary>
    public static void UseNormalInvoke()
    {
        _fastInvokeEnabled = false;
    }
}
