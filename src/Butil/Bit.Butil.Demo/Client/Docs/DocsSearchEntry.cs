namespace Bit.Butil.Demo.Client.Docs;

/// <summary>
/// One searchable piece of the site: a whole page, one of its sections, or one row of its API
/// reference table.
/// <para>
/// The fields are held apart rather than concatenated into a single haystack so a hit on a name can
/// outrank a hit buried in prose - see Shared/SearchBox.razor, which scores them.
/// </para>
/// </summary>
/// <param name="Title">What the hit is called: the page's title, the section's heading, the member's name.</param>
/// <param name="Page">
/// The title of the page this entry lives on, or null when the entry IS the page. It is what the
/// result list shows beside a section so the reader knows where it would take them.
/// </param>
/// <param name="Url">The href, including the fragment that scrolls to the section.</param>
/// <param name="Group">The nav group the page belongs to.</param>
/// <param name="Keywords">
/// Names that are not in the title but that someone would search by: the type the page documents,
/// the member badge on a section, the signature of an API row, the words of the slug.
/// </param>
/// <param name="Summary">The one sentence shown under the title in the result list.</param>
/// <param name="Body">
/// The rest of the entry's text - code samples, the labels and prose of the live demo, the callouts.
/// It is matched but never shown in full: a hit in it is quoted as a window around the match.
/// </param>
public record DocsSearchEntry(
    string Title,
    string? Page,
    string Url,
    string Group,
    string Keywords,
    string Summary,
    string Body);
