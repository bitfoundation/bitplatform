using Bit.Bswup.Demo.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Bit.Bswup.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client can offer while someone is typing a
/// resource URI or filling in a prompt argument.
/// <para>
/// Every templated resource this server exposes is keyed by a value out of a closed list - a docs
/// slug, a guide heading, a source path - and none of those lists is guessable. Without
/// completions a person browsing the server has to call a listing tool, read it, and type a slug
/// back by hand; with them the client fills the URI in. The values come from the same catalogs the
/// tools read, so a page or file added to the site turns up here without anything to remember.
/// </para>
/// </summary>
public static class BswupCompletions
{
    // The protocol caps one completion response at 100 values, and a client shows far fewer.
    private const int MaxValues = 100;

    public static ValueTask<CompleteResult> CompleteAsync(RequestContext<CompleteRequestParams> request, CancellationToken cancellationToken)
    {
        var argument = request.Params?.Argument;

        return ValueTask.FromResult(Complete(request.Params?.Ref, argument?.Name, argument?.Value));
    }

    private static CompleteResult Complete(Reference? reference, string? argumentName, string? typed)
    {
        var candidates = reference switch
        {
            ResourceTemplateReference resource => ForResource(resource.Uri),
            PromptReference prompt => ForPrompt(prompt.Name, argumentName),
            _ => null
        };

        if (candidates is null) return new CompleteResult();

        // Prefix first, then anything containing what was typed: someone typing "worker" means the
        // service-worker page, and someone typing "ser" is most likely still spelling it out.
        var matches = candidates
            .Where(value => value.Contains(typed ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(value => value.StartsWith(typed ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .ThenBy(value => value.Length)
            .ThenBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new CompleteResult
        {
            Completion = new Completion
            {
                Values = [.. matches.Take(MaxValues)],
                Total = matches.Length,
                HasMore = matches.Length > MaxValues
            }
        };
    }

    private static IEnumerable<string>? ForResource(string? uriTemplate) => uriTemplate switch
    {
        // The introduction's slug is the empty string, which is not a value anyone can pick from a
        // list - and DocsCatalog.FindBySlug maps the word back, so offering it is the honest entry.
        "bswup://docs/{slug}" => DocsCatalog.AllPages.Select(page => page.Slug.Length == 0 ? "introduction" : page.Slug),
        "bswup://guide/{heading}" => BswupSourceCatalog.GuideSections.Select(section => section.Heading),
        "bswup://source/{path}" => BswupSourceCatalog.SourceFiles.Select(file => file.Path),
        _ => null
    };

    private static IEnumerable<string>? ForPrompt(string? name, string? argumentName)
    {
        return (name, argumentName) switch
        {
            ("add-bswup-to-app", "hostingModel") => [.. BswupSetupGuide.HostingModels, "unknown"],
            _ => null
        };
    }
}
