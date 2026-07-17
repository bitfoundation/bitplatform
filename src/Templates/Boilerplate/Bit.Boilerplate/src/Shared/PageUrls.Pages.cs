using System.Text;
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
    /// Consumed by both the chatbot's GetAppPages tool and <see cref="GetPagesMarkdown"/>.
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
    /// Builds a markdown list of the pages returned by <see cref="GetPages"/> (built by hand, no external library).
    /// Used as context for the follow-up suggestions agent.
    /// </summary>
    public static string GetPagesMarkdown()
    {
        var builder = new StringBuilder();

        foreach (var page in GetPages())
        {
            builder.Append("- `").Append(page.Url).Append("` — ").AppendLine(page.Description);
        }

        return builder.ToString();
    }
}
