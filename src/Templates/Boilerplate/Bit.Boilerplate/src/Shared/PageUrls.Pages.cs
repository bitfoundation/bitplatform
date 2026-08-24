using System.Reflection;
using System.ComponentModel;

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
}
