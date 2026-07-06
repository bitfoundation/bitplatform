//+:cnd:noEmit
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Primitives;

namespace Boilerplate.Server.Shared.Infrastructure.Services;

/// <summary>
/// Extends <see cref="AcceptLanguageHeaderRequestCultureProvider"/> to map neutral culture names
/// (e.g. <c>fa</c>) to their supported specific counterparts (e.g. <c>fa-IR</c>).
/// <para>
/// The built-in provider returns the raw <c>Accept-Language</c> header values as-is.
/// The middleware's <c>FallBackToParentCultures</c> only walks <em>up</em> the hierarchy
/// (specific → neutral), so a browser that sends <c>fa</c> never matches the supported
/// culture <c>fa-IR</c>. This provider handles the opposite direction by substituting
/// each neutral culture name with the first supported culture whose parent chain contains it.
/// </para>
/// </summary>
public class AppAcceptLanguageRequestCultureProvider : AcceptLanguageHeaderRequestCultureProvider
{
    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var result = await base.DetermineProviderCultureResult(httpContext);

        if (result is null || Options?.SupportedCultures is not { Count: > 0 } supportedCultures)
            return result;

        var uiSupportedCultures = Options.SupportedUICultures ?? supportedCultures;

        var mappedCultures = result.Cultures
            .Select(c => MapNeutralToSpecificCulture(c, supportedCultures))
            .ToList();

        var mappedUICultures = result.UICultures
            .Select(c => MapNeutralToSpecificCulture(c, uiSupportedCultures))
            .ToList();

        return new ProviderCultureResult(mappedCultures, mappedUICultures);
    }

    /// <summary>
    /// If <paramref name="cultureName"/> is a neutral culture (contains no <c>-</c>),
    /// returns the name of the first supported culture whose parent chain includes it.
    /// Otherwise returns the original value unchanged.
    /// </summary>
    private static StringSegment MapNeutralToSpecificCulture(StringSegment cultureName, IList<CultureInfo> supportedCultures)
    {
        if (cultureName.Value?.Contains('-') is true)
            return cultureName;

        var exact = supportedCultures.FirstOrDefault(c =>
            string.Equals(c.Name, cultureName.Value, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return new StringSegment(exact.Name);

        var matched = supportedCultures.FirstOrDefault(c =>
        {
            var parent = c.Parent;
            while (parent.Equals(CultureInfo.InvariantCulture) is false)
            {
                if (string.Equals(parent.Name, cultureName.Value, StringComparison.OrdinalIgnoreCase))
                    return true;
                parent = parent.Parent;
            }
            return false;
        });

        return matched is not null ? new StringSegment(matched.Name) : cultureName;
    }
}
