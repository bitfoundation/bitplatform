using System.Runtime.Loader;

namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class CultureService
{
    /// <summary>
    /// What the server resolves a culture-less url through (See <c>UseCultureUrlRedirection</c>) and what the WASM
    /// head boots into before <c>host.RunAsync()</c>.
    /// </summary>
    public const string CultureCookieName = ".AspNetCore.Culture";

    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private NavigationManager navigationManager = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] private ClientExceptionHandlerBase clientExceptionHandlerBase = default!;

    public async Task ChangeCulture(string? cultureName)
    {
        cultureName ??= CultureInfoManager.DefaultCulture.Name;

        if (AppPlatform.IsBlazorHybrid)
        {
            await storageService.SetItem("Culture", cultureName, persistent: true);
        }
        else
        {
            await SetCultureCookie(cultureName);
        }

        var currentUri = new Uri(navigationManager.Uri);

        if (AppPlatform.IsBlazorHybridOrBrowser && await EnsureCultureResourcesLoaded(cultureName))
        {
            CultureInfoManager.SetCurrentCulture(cultureName);

            pubSubService.Publish(ClientAppMessages.SOFT_RESTART);

            // A url carrying a culture outranks the cookie on its next load (See UseCultureUrlRedirection), so the
            // old culture must not stay in the address bar. A culture-less url is left untouched.
            if (currentUri.GetCulture() is string cultureInUrl
                && string.Equals(cultureInUrl, cultureName, StringComparison.InvariantCultureIgnoreCase) is false)
            {
                navigationManager.NavigateTo(currentUri.GetUrlWithCulture(cultureName), forceLoad: false, replace: true);
            }
            return;
        }

        // Blazor Server (no in-place switch there) and the wasm satellite-failure fall-back: a full reload of the
        // current url, re-addressed to the new culture when the url carries one.
        navigationManager.NavigateTo(currentUri.GetUrlWithCulture(cultureName), forceLoad: true, replace: true);
    }

    /// <summary>
    /// Server.Web may not write the culture cookie on a pre-rendered page - a Set-Cookie makes the whole response
    /// ineligible for the CDN edge cache (See <c>App.razor.cs</c>) - so a visitor who lands directly on a
    /// <c>/{culture}/</c> url, from a search result or a shared link, has that culture persisted here instead, once
    /// the client is running. Without it their next culture-less visit would fall back to <c>Accept-Language</c>.
    /// </summary>
    public async Task PersistCurrentCulture()
    {
        if (CultureInfoManager.InvariantGlobalization || AppPlatform.IsBlazorHybrid)
            return; // Hybrid persists the culture in storage instead, and has no culture-prefixed urls to land on.

        var currentCulture = CultureInfo.CurrentUICulture.Name;

        if (string.Equals(ExtractUiCulture(await cookie.GetValue(CultureCookieName)), currentCulture, StringComparison.OrdinalIgnoreCase))
            return;

        await SetCultureCookie(currentCulture);
    }

    private async Task SetCultureCookie(string cultureName)
    {
        await cookie.Set(new()
        {
            Name = CultureCookieName,
            Value = $"c={cultureName}|uic={cultureName}",
            MaxAge = 3600 * 24 * 30,
            Path = "/",
            // Lax, not Strict: a Strict cookie is withheld on a cross-site top level navigation, so a visitor arriving
            // from a search result at a culture-less url would be redirected into their browser's language rather than
            // the one they picked.
            SameSite = SameSite.Lax,
            Secure = AppEnvironment.IsDevelopment() is false
        });
    }

    /// <summary>
    /// The cookie carries both cultures as <c>c=&lt;culture&gt;|uic=&lt;uiCulture&gt;</c>; only the UI culture is read
    /// back, and a value that names none is treated as absent rather than as the default culture.
    /// </summary>
    public static string? ExtractUiCulture(string? cultureCookie)
    {
        if (cultureCookie is null)
            return null;

        cultureCookie = Uri.UnescapeDataString(cultureCookie);

        const string uiCultureMarker = "|uic=";
        var uiCultureIndex = cultureCookie.IndexOf(uiCultureMarker, StringComparison.InvariantCultureIgnoreCase);

        if (uiCultureIndex is -1)
            return null;

        var uiCulture = cultureCookie[(uiCultureIndex + uiCultureMarker.Length)..];

        return string.IsNullOrWhiteSpace(uiCulture) ? null : uiCulture;
    }

    private static readonly HashSet<string> loadedSatelliteLanguages = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Blazor WebAssembly downloads satellite resource assemblies only for the culture the app booted in;
    /// <see cref="System.Resources.ResourceManager"/> resolves synchronously and cannot fetch a missing assembly
    /// mid-lookup, so without this a runtime switch silently falls back to the neutral (English) strings. Fetching
    /// the target culture's satellite BEFORE the switch (and before anything asks for its strings, so no fallback
    /// result gets cached) is what makes the in-place change possible. A no-op everywhere else: hybrid ships the
    /// satellites inside the app.
    /// </summary>
    private async Task<bool> EnsureCultureResourcesLoaded(string cultureName)
    {
        if (AppPlatform.IsBrowser is false)
            return true;

        // The resx files are per language (AppStrings.fa.resx), so the satellite folder is the two-letter language
        // name: fa-IR's strings live in _framework/fa/. The default culture's family has no satellite at all - its
        // strings are the neutral resources inside the main assembly.
        var languageName = CultureInfoManager.CreateCultureInfo(cultureName).TwoLetterISOLanguageName;

        if (string.Equals(languageName, CultureInfoManager.DefaultCulture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase) ||
            loadedSatelliteLanguages.Contains(languageName))
        {
            return true;
        }

        try
        {
            // The DI HttpClient points at the api server and runs the auth handler chain; this is a static asset
            // of the web host itself, so fetch it from the page's own origin with a plain client.
            using var httpClient = new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
            using var response = await httpClient.GetAsync(GetSatelliteAssemblyUrl(languageName));

            response.EnsureSuccessStatusCode();

            AssemblyLoadContext.Default.LoadFromStream(await response.Content.ReadAsStreamAsync());

            loadedSatelliteLanguages.Add(languageName);
            return true;
        }
        catch (Exception exp)
        {
            // The caller falls back to a full reload, which localizes correctly by booting into the new culture.
            clientExceptionHandlerBase.Handle(exp.WithData("CurrentCulture", cultureName).WithData("Language", languageName));
            return false;
        }
    }

    /// <summary>
    /// Boilerplate.Shared is the app's only localized assembly (AppStrings, IdentityStrings); add any other assembly
    /// that gets its own resx files wherever this is fetched.
    /// </summary>
    private static string GetSatelliteAssemblyUrl(string languageName)
    {
        return $"_framework/{languageName}/Boilerplate.Shared.resources.{(AppEnvironment.IsDevelopment() ? "dll" : "wasm")}";
    }
}
