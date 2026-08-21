using NUnit.Framework;
using Bit.Butil.Tests.Mcp.Infrastructure;

namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// The catalogs held against each other.
/// <para>
/// This server answers from five separate bodies of knowledge - the reflected assembly, the docs
/// nav, the browser-support matrix, the README and the embedded sources - and the tools cross-
/// reference them constantly: a capability names services, a page names types, an inspection hands
/// back the calls that fetch both. Each catalog can be internally perfect while pointing at
/// something in another one that is no longer there, and the result is a tool that answers
/// confidently with a name nothing resolves. Nothing inside any one catalog can catch that.
/// </para>
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class CatalogConsistencyTests : McpTestBase
{
    private ApiType[] _types = null!;
    private DocsIndexRow[] _pages = null!;

    /// <summary>
    /// The full reference of every advertised type, fetched once. Several of the tests below walk
    /// the same list, and the tool is idempotent - fetching it per test is the same round trip paid
    /// over again for an answer that cannot have changed.
    /// </summary>
    private readonly Dictionary<string, ApiDetailsResult> _details = new(StringComparer.OrdinalIgnoreCase);

    [OneTimeSetUp]
    public async Task LoadCatalogs()
    {
        // Both listings come from the tool that also retrieves the single item, called with no
        // argument - which is the whole of what replaced the four listing tools this suite used to
        // call, so exercising them here is exercising that fold.
        // An empty list is the same failure as a missing one: every test below walks _types, and a
        // reflection walk that found nothing would let all of them pass by having nothing to check.
        _types = (await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails")).Types is { Length: > 0 } types
            ? types
            : throw new InvalidOperationException("GetButilApiDetails with no type name did not answer with the type list.");

        _pages = await DocsIndexAsync();

        foreach (var type in _types)
        {
            _details[type.Name] = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = type.Name });
        }
    }

    /// <summary>
    /// The cached reference of one type, fetched on the spot for a name the listing never
    /// advertised - which is itself one of the things these tests are looking for.
    /// </summary>
    private async Task<ApiDetailsResult> DetailsAsync(string typeName)
    {
        if (_details.TryGetValue(typeName, out var cached)) return cached;

        return _details[typeName] = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName });
    }

    [Test]
    public void Every_service_a_docs_page_names_is_a_type_the_library_ships()
    {
        var known = _types.Select(type => type.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dangling = _pages
            .SelectMany(page => page.Services.Select(service => (page.Slug, Service: service)))
            .Where(entry => known.Contains(entry.Service) is false)
            .ToArray();

        Assert.That(dangling, Is.Empty,
            $"Documentation pages name types that are not in the library: {string.Join(", ", dangling.Select(entry => $"/{entry.Slug} -> {entry.Service}"))}.");
    }

    [Test]
    public async Task The_support_matrix_and_the_page_index_are_one_table()
    {
        // They were two tools once, over two projections of the same nav, and the pair could drift:
        // an API the matrix knew about and the index did not, or the reverse. Folding them removed
        // the drift rather than testing for it - so what is asserted now is the fold itself, since
        // the resource and the tool answering differently would put the old bug straight back.
        var resource = await Mcp.ReadResourceAsync("butil://support", cancellationToken: Ct);

        var matrix = string.Join("\n", resource.Contents
            .OfType<ModelContextProtocol.Protocol.TextResourceContents>()
            .Select(content => content.Text));

        // Records hold their list cells as arrays, which compare by reference - so the rows are
        // flattened to their text to be compared by what they say.
        static string Row(DocsIndexRow row) => string.Join(" | ",
            row.Group, row.Slug, row.Title, row.Summary, string.Join(",", row.Services), row.Engines, string.Join(",", row.Requires));

        Assert.Multiple(() =>
        {
            // Every cell, not just the slug: the same rows carrying a different summary, a
            // different engines column or a different service list is the drift this asserts away.
            Assert.That(DocsIndexRow.ParseAll(matrix).Select(Row),
                Is.EqualTo(_pages.Select(Row)).AsCollection,
                "butil://support and GetButilDocsPage with no slug are meant to be the same table.");

            // Every row carries what an agent chooses an API on: which engines run it, and what the
            // page has to arrange first. A row with an empty engines cell is a matrix that lost the
            // column it exists for.
            Assert.That(_pages.Where(page => string.IsNullOrWhiteSpace(page.Engines)), Is.Empty);

            // And what it covers, which is the only cell that separates indexed-db from
            // cache-storage from storage-manager without fetching all three pages to find out.
            Assert.That(_pages.Where(page => string.IsNullOrWhiteSpace(page.Summary)), Is.Empty,
                "Nothing in the index would say what a page covers.");

            // The guide pages are in the table, so it has to say so: they are the rows an agent
            // would otherwise read as browser APIs that no engine implements.
            Assert.That(_pages.Where(page => page.Group == "Overview").Select(page => page.Engines).Distinct(),
                Is.EqualTo(new[] { "Guide" }).AsCollection);
        });
    }

    [Test]
    public void Every_type_the_list_advertises_can_be_fetched_in_full()
    {
        // GetButilApiDetails with no type name exists to pick the type to pass to it with one. A
        // name in the first answer that the second cannot resolve is a dead end an agent has no way
        // to recover from.
        var failures = new List<string>();

        foreach (var type in _types)
        {
            var result = _details[type.Name];

            if (result.Details is null)
            {
                failures.Add($"{type.Name}: {result.Message}");
                continue;
            }

            if (string.Equals(result.Details.Name, type.Name, StringComparison.Ordinal) is false)
            {
                failures.Add($"{type.Name} resolved to {result.Details.Name}.");
            }

            if (result.Details.Kind != type.Kind)
            {
                failures.Add($"{type.Name} is a '{type.Kind}' in the list and a '{result.Details.Kind}' in its details.");
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [Test]
    public void Every_injectable_service_says_how_to_inject_it_and_what_it_does()
    {
        // The services are the library. One that reports no members is a reflection walk that found
        // nothing; one that reports no summary is XML documentation that did not ship.
        var failures = new List<string>();

        foreach (var service in _types.Where(type => type.IsInjectable))
        {
            var details = _details[service.Name].Details;

            if (details is null)
            {
                failures.Add($"{service.Name} could not be fetched.");
                continue;
            }

            if (details.Inject != $"@inject Bit.Butil.{service.Name} {char.ToLowerInvariant(service.Name[0])}{service.Name[1..]}")
            {
                failures.Add($"{service.Name} has an unexpected inject line: '{details.Inject}'.");
            }

            if (details.Members.Length == 0) failures.Add($"{service.Name} reports no members at all.");

            if (string.IsNullOrWhiteSpace(details.Summary)) failures.Add($"{service.Name} has no summary.");

            var undocumented = details.Members.Count(member => string.IsNullOrWhiteSpace(member.Summary));

            // Not every member is documented, but a service where none of them are means the XML
            // documentation was not found rather than that someone skipped a comment.
            if (details.Members.Length > 3 && undocumented == details.Members.Length)
            {
                failures.Add($"{service.Name} has {details.Members.Length} members and not one of them carries documentation.");
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [Test]
    public void Every_method_reports_a_signature_and_a_return_type()
    {
        // The wrappers follow the browser API's own naming, which is exactly why an agent has to be
        // given the real signature rather than left to infer one.
        var failures = new List<string>();

        foreach (var service in _types.Where(type => type.IsInjectable).Take(25))
        {
            var details = _details[service.Name].Details!;

            foreach (var method in details.Members.Where(member => member.Kind == "Method"))
            {
                if (string.IsNullOrEmpty(method.Signature) || method.Signature[0] != '(')
                {
                    failures.Add($"{service.Name}.{method.Name} has no parameter list.");
                }

                if (string.IsNullOrEmpty(method.Type)) failures.Add($"{service.Name}.{method.Name} has no return type.");
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [Test]
    public async Task Every_page_that_documents_a_type_is_linked_from_that_type()
    {
        // The link is followed in both directions in practice - from a page to its services, and
        // from a type's reference to the page that covers it - so a one-way link is half broken.
        var failures = new List<string>();

        foreach (var page in _pages.Where(page => page.Services.Length > 0))
        {
            foreach (var service in page.Services)
            {
                var details = (await DetailsAsync(service)).Details;

                if (details is null)
                {
                    failures.Add($"/{page.Slug} names {service}, which has no reference.");
                    continue;
                }

                if (details.DocsUrl is null) failures.Add($"{service} is documented on /{page.Slug} but its reference links to no page.");
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [Test]
    public async Task Every_source_file_the_listing_advertises_can_be_fetched()
    {
        var files = await ListAsync("GetButilSourceFile");

        var failures = new List<string>();

        foreach (var path in files)
        {
            var text = Text(await CallAsync("GetButilSourceFile", new { path }));

            if (text.StartsWith("No source file at", StringComparison.Ordinal)) failures.Add($"{path} is listed but cannot be fetched.");
            else if (text.Length == 0) failures.Add($"{path} came back empty.");
        }

        Assert.Multiple(() =>
        {
            Assert.That(files.Length, Is.GreaterThan(50), "The listing should cover every page of this site plus the hosting samples.");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        });
    }

    [Test]
    public async Task Every_documentation_page_has_the_source_file_that_renders_it()
    {
        // The demo's pages are the working examples the tools point at - "each page IS a working
        // example" is the claim the prompts make. A page whose source was not embedded breaks that
        // for exactly one API, silently.
        var files = (await ListAsync("GetButilSourceFile")).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = _pages
            .Select(page => (page.Slug, Guess: $"Demo/Client/Pages/{page.Title.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("&", string.Empty, StringComparison.Ordinal)}Page.razor"))
            .Where(entry => files.Contains(entry.Guess) is false)
            .ToArray();

        // Not every page's component is named after its title, so this is a coverage check rather
        // than a naming rule: the great majority have to be there.
        Assert.That(missing.Length, Is.LessThan(_pages.Length / 4),
            $"Too many documentation pages have no embedded source: {string.Join(", ", missing.Select(entry => entry.Slug))}.");
    }

    [Test]
    public async Task The_guide_the_tools_serve_is_the_guide_the_resource_serves()
    {
        var readme = await Mcp.ReadResourceAsync("butil://guide", cancellationToken: Ct);

        var whole = string.Join("\n", readme.Contents
            .OfType<ModelContextProtocol.Protocol.TextResourceContents>()
            .Select(content => content.Text));

        var sections = await ListAsync("GetButilGuideSection");

        // Matched as a heading rather than as prose: the guide mentioning a section's words in a
        // sentence is not the same as the guide still having that section.
        var missing = sections.Where(heading => whole.Contains($"## {heading}", StringComparison.Ordinal) is false).ToArray();

        Assert.That(missing, Is.Empty,
            "The section index lists headings that are not in the guide it was built from.");
    }
}
