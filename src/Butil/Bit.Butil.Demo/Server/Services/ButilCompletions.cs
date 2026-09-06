using ModelContextProtocol.Protocol;
using Bit.Butil.Demo.Client.Docs;

namespace Bit.Butil.Demo.Server.Services;

/// <summary>
/// Answers completion/complete: the values that are valid for one argument of a prompt or of a
/// resource template, filtered by what has been typed so far.
/// <para>
/// Every one of those arguments is drawn from a closed set this server already holds - four hosting
/// models, ninety-odd docs slugs, the public type names, the guide's own headings, the embedded
/// source paths - and without this handler a client has no way to learn any of them except by
/// calling a listing tool and reading it. That is fine for an agent and useless for the person
/// picking "add Butil to an app" out of a menu in their editor, who is then asked to type a hosting
/// model with no indication of what the four are. The sets come from the same catalogs the tools
/// answer from, so a page added to the nav is a slug that completes.
/// </para>
/// </summary>
public static class ButilCompletions
{
    /// <summary>
    /// The protocol's cap on one completion response. Values past it are still counted in Total and
    /// flagged with HasMore, which is what tells a client to keep typing rather than that the list
    /// simply ends here.
    /// </summary>
    private const int MaxValues = 100;

    public static CompleteResult Complete(CompleteRequestParams? request)
    {
        var candidates = CandidatesFor(request?.Ref, request?.Argument?.Name);
        var typed = request?.Argument?.Value ?? string.Empty;

        // Prefix first, then anything containing it: someone typing "storage" wants StorageManager
        // before "Local & Session Storage", and both before neither.
        var matches = typed.Length == 0
            ? candidates
            : [.. candidates.Where(c => c.StartsWith(typed, StringComparison.OrdinalIgnoreCase)),
               .. candidates.Where(c => c.StartsWith(typed, StringComparison.OrdinalIgnoreCase) is false
                                     && c.Contains(typed, StringComparison.OrdinalIgnoreCase))];

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

    /// <summary>Every valid value for one argument, before what has been typed narrows it.</summary>
    private static string[] CandidatesFor(Reference? reference, string? argument) => reference switch
    {
        PromptReference prompt => prompt.Name switch
        {
            // The only prompt argument with a closed set. The other two - what a feature should do,
            // what goes wrong - are prose, and offering a menu of prose would be worse than silence.
            "add-butil-to-app" when argument is "hostingModel" => [.. ButilSetupGuide.HostingModels, "unknown"],
            _ => []
        },

        // A template's arguments are named by its own placeholders: butil://docs/{slug} completes
        // "slug". Matching on the URI rather than on the resource's name keeps this readable against
        // the templates as they are declared in McpResources.
        ResourceTemplateReference template => template.Uri switch
        {
            "butil://docs/{slug}" when argument is "slug" => [.. DocsNav.AllLinks.Select(l => l.Url)],
            "butil://api/{typeName}" when argument is "typeName" => [.. ButilApiCatalog.Types.Select(t => t.Name)],
            "butil://guide/{heading}" when argument is "heading" => [.. ButilSourceCatalog.GuideSections.Select(s => s.Heading)],
            "butil://source/{path}" when argument is "path" => [.. ButilSourceCatalog.SourceFiles.Select(f => f.Path)],
            _ => []
        },

        _ => []
    };
}
