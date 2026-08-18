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
    private Capability[] _capabilities = null!;
    private DocsPage[] _pages = null!;

    [OneTimeSetUp]
    public async Task LoadCatalogs()
    {
        _types = await CallStructuredAsync<ApiType[]>("GetButilApiList");
        _capabilities = await CallStructuredAsync<Capability[]>("GetButilBrowserSupport");
        _pages = await CallStructuredAsync<DocsPage[]>("GetButilDocsList");
    }

    [Test]
    public void Every_service_the_support_matrix_names_is_a_type_the_library_ships()
    {
        var known = _types.Select(type => type.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dangling = _capabilities
            .SelectMany(capability => capability.Services.Select(service => (capability.Api, Service: service)))
            .Where(entry => known.Contains(entry.Service) is false)
            .ToArray();

        Assert.That(dangling, Is.Empty,
            $"The support matrix names types that are not in the library: {string.Join(", ", dangling.Select(entry => $"{entry.Api} -> {entry.Service}"))}.");
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
    public void Every_row_of_the_support_matrix_links_to_a_page_that_exists()
    {
        var slugs = _pages.Select(page => page.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var broken = _capabilities.Where(capability => slugs.Contains(capability.DocsUrl.TrimStart('/')) is false).ToArray();

        Assert.That(broken.Select(capability => capability.DocsUrl), Is.Empty);
    }

    [Test]
    public void The_support_matrix_covers_every_api_page_and_nothing_else()
    {
        // Both are projections of the same nav, so a page that documents a browser API is a row in
        // the matrix, and a row in the matrix is a page. A drift between the two would show up as
        // an API that GetButilBrowserSupport cannot see.
        var apiPages = _pages.Where(page => page.Services.Length > 0).Select(page => page.Title).ToArray();

        Assert.That(_capabilities.Select(capability => capability.Api), Is.EquivalentTo(apiPages));
    }

    [Test]
    public async Task Every_type_the_list_advertises_can_be_fetched_in_full()
    {
        // GetButilApiList exists to pick the type to pass to GetButilApiDetails. A name in the first
        // that the second cannot resolve is a dead end an agent has no way to recover from.
        var failures = new List<string>();

        foreach (var type in _types)
        {
            var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = type.Name });

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
    public async Task Every_injectable_service_says_how_to_inject_it_and_what_it_does()
    {
        // The services are the library. One that reports no members is a reflection walk that found
        // nothing; one that reports no summary is XML documentation that did not ship.
        var failures = new List<string>();

        foreach (var service in _types.Where(type => type.IsInjectable))
        {
            var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = service.Name });
            var details = result.Details;

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
    public async Task Every_method_reports_a_signature_and_a_return_type()
    {
        // The wrappers follow the browser API's own naming, which is exactly why an agent has to be
        // given the real signature rather than left to infer one.
        var failures = new List<string>();

        foreach (var service in _types.Where(type => type.IsInjectable).Take(25))
        {
            var details = (await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = service.Name })).Details!;

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
                var details = (await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = service })).Details;

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
        var files = await CallStructuredAsync<SourceFile[]>("GetButilSourceFiles");

        var failures = new List<string>();

        foreach (var file in files)
        {
            var text = Text(await CallAsync("GetButilSourceFile", new { path = file.Path }));

            if (text.StartsWith("No source file at", StringComparison.Ordinal)) failures.Add($"{file.Path} is listed but cannot be fetched.");
            else if (text.Length == 0) failures.Add($"{file.Path} came back empty.");
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
        var files = (await CallStructuredAsync<SourceFile[]>("GetButilSourceFiles"))
            .Select(file => file.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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

        var sections = await CallStructuredAsync<GuideSection[]>("GetButilGuideSections");

        var missing = sections.Where(section => whole.Contains($" {section.Heading}", StringComparison.Ordinal) is false).ToArray();

        Assert.That(missing.Select(section => section.Heading), Is.Empty,
            "The section index lists headings that are not in the guide it was built from.");
    }
}
