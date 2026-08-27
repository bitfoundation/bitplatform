using ModelContextProtocol.Protocol;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// Answers completion/complete: the values that are valid for one argument of a prompt or of a
/// resource template, filtered by what has been typed so far.
/// <para>
/// Every one of those arguments is drawn from a closed set this server already holds - four hosting
/// models, 110 component names, the public type names, the theming reference's own chapters - and
/// without this handler a client has no way to learn any of them except by calling a tool and
/// reading it. That is fine for an agent and useless for the person picking "build a screen" out of
/// a menu in their editor. The sets come from the same catalogs the tools answer from, so a
/// component added to the nav is a name that completes.
/// </para>
/// </summary>
public static class BlazorUICompletions
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

        // Prefix first, then anything containing it: someone typing "date" wants DatePicker before
        // DateRangePicker's neighbours, and both before neither.
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

    private static string[] CandidatesFor(Reference? reference, string? argument) => reference switch
    {
        PromptReference prompt => prompt.Name switch
        {
            // The only prompt arguments with closed sets. The others - what a screen should do, what
            // goes wrong - are prose, and offering a menu of prose would be worse than silence.
            "add-bit-blazorui-to-app" when argument is "hostingModel" => [.. BlazorUISetupGuide.HostingModels, "unknown"],
            "migrate-to-bit-blazorui" when argument is "current" => ["MudBlazor", "Radzen", "Syncfusion", "Telerik", "Bootstrap markup", "hand-written HTML and CSS"],
            _ => []
        },

        // A template's arguments are named by its own placeholders. Matching on the URI rather than
        // on the resource's name keeps this readable against the templates in McpResources.
        ResourceTemplateReference template => template.Uri switch
        {
            "bitblazorui://components/{name}" or "bitblazorui://components/{name}/examples" when argument is "name"
                => [.. BlazorUIComponentCatalog.Components.Select(c => c.Name)],
            "bitblazorui://types/{typeName}" when argument is "typeName"
                => [.. BlazorUITypeCatalog.LibraryWide.Select(t => t.Name)],
            "bitblazorui://setup/{hostingModel}" when argument is "hostingModel"
                => BlazorUISetupGuide.HostingModels,
            "bitblazorui://theming/{section}" when argument is "section"
                => [.. BlazorUIThemingGuide.Chapters.Select(c => c.Title)],
            _ => []
        },

        _ => []
    };
}
