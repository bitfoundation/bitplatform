namespace Boilerplate.Shared.Infrastructure.Attributes;

/// <summary>
/// Leaves a <see cref="PageUrls"/> url out of <c>sitemap.xml</c> and <c>llms.txt</c> while search engines may still index
/// it: a page that only exists to sign in or to create an account, or one that needs an account to show anything of use.
/// The sitemap is the list of pages the site asks search engines to crawl and rank, and such a page there spends a
/// crawler's time on a form. A search for the app's sign-in still finds it through the links that lead to it.
/// A page that carries <see cref="NoIndexAttribute"/> is left out already.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class NotInSitemapAttribute : Attribute
{
}
