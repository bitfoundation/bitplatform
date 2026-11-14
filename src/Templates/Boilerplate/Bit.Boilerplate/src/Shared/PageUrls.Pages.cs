using System.Reflection;
using System.ComponentModel;
using System.Collections.Frozen;

namespace Boilerplate.Shared;

public static partial class PageUrls
{
    /// <summary>
    /// A navigable application page: its relative <see cref="Url"/> and a short <see cref="Description"/>.
    /// </summary>
    public record PageInfo(string Url, string Description);

    /// <summary>
    /// Returns the list of navigable application pages (url + short description).
    /// A page is included only when its <see cref="PageUrls"/> constant (or <see cref="SettingsSections"/> field)
    /// is decorated with a <see cref="DescriptionAttribute"/>, so this list automatically adapts to the
    /// enabled template features (Admin/Sales/multitenant/ads/...).
    /// Consumed by the public, unauthenticated GET /llms.txt endpoint
    /// (Server.Web/Infrastructure/Extensions/WebApplicationExtensions.cs) and, when signalR is enabled, by the
    /// chatbot's GetAppPages tool. The list is NOT filtered by the caller's roles or features - every description is
    /// readable by anonymous clients.
    /// </summary>
    public static IReadOnlyList<PageInfo> GetPages()
    {
        var pages = new List<PageInfo>();

        // Top-level pages: the constant value is the relative url.
        foreach (var field in typeof(PageUrls).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType != typeof(string)) continue;
            var description = field.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (string.IsNullOrWhiteSpace(description)) continue;
            if (field.GetValue(null) is not string url || string.IsNullOrWhiteSpace(url)) continue;
            pages.Add(new PageInfo(url, description));
        }

        // Settings sub-pages: the field value is only the section name, the url is "{Settings}/{section}".
        foreach (var field in typeof(SettingsSections).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType != typeof(string)) continue;
            var description = field.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (string.IsNullOrWhiteSpace(description)) continue;
            if (field.GetValue(null) is not string section || string.IsNullOrWhiteSpace(section)) continue;
            pages.Add(new PageInfo($"{Settings}/{section}", description));
        }

        return pages;
    }

    /// <summary>
    /// The urls whose <see cref="PageUrls"/> constant carries <see cref="NoIndexAttribute"/>.
    /// </summary>
    public static IReadOnlySet<string> NoIndexUrls { get; } = typeof(PageUrls).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(string) && field.IsDefined(typeof(NoIndexAttribute)))
        .Select(field => (string)field.GetValue(null)!)
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether search engines should leave the page at <paramref name="url"/> out (See <see cref="NoIndexAttribute"/>).
    /// The culture segment, the query and a trailing slash are ignored, so <c>/fa-IR/confirm/?emailToken=...</c> is
    /// <see cref="Confirm"/>. Case-insensitive, like the router.
    /// </summary>
    public static bool IsNoIndex(Uri url)
    {
        var path = url.GetPath();

        return NoIndexUrls.Contains(path.Length > 1 ? path.TrimEnd('/') : path);
    }
}
