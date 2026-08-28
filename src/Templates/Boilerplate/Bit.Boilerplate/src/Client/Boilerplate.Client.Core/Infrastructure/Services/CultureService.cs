using System.Runtime.Loader;

namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class CultureService
{
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
            await cookie.Set(new()
            {
                Name = ".AspNetCore.Culture",
                Value = $"c={cultureName}|uic={cultureName}",
                MaxAge = 3600 * 24 * 30,
                Path = "/",
                SameSite = SameSite.Strict,
                Secure = AppEnvironment.IsDevelopment() is false
            });
        }

        var currentUri = new Uri(navigationManager.Uri);

        if (AppPlatform.IsBlazorHybridOrBrowser && await EnsureCultureResourcesLoaded(cultureName))
        {
            CultureInfoManager.SetCurrentCulture(cultureName);
            pubSubService.Publish(ClientAppMessages.CULTURE_CHANGED, cultureName);

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
