namespace Boilerplate.Shared.Infrastructure.Attributes;

/// <summary>
/// Marks a <see cref="PageUrls"/> url as one search engines should not index: a step of a flow that only makes sense
/// when the app sends you there (confirming an email, resetting a password, approving an OAuth client), a page for
/// troubleshooting, an error page. Such a page in the search results takes a visitor nowhere useful, and every one a
/// crawler spends its time on is time it does not spend on the pages that should rank.
/// <para>
/// <c>AppPageData</c> renders <c>&lt;meta name="robots" content="noindex"&gt;</c> on it, Server.Web also sends it as
/// an <c>X-Robots-Tag</c> header, which holds with pre-rendering off too, and <c>sitemap.xml</c> leaves it out. Pages
/// people do search for, such as sign-in and sign-up, stay indexable.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class NoIndexAttribute : Attribute
{
}
