namespace Bit.Butil.Demo.Client.Docs;

/// <summary>
/// The corpus behind the site's search box, served by the host at <c>/api/docs/search-index</c> and
/// fetched once by the browser.
/// <para>
/// It is built on the server (Server/Services/DocsContentIndex.cs) from the embedded source of the
/// pages themselves, so it cannot go stale against them: a section added to a page is a section
/// that becomes searchable, with no list to remember to update.
/// </para>
/// </summary>
public record DocsSearchIndex(DocsSearchEntry[] Entries);
