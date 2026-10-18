using ModelContextProtocol.Protocol;

namespace Bit.Bmage.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client offers while someone fills in a prompt
/// argument or a resource template's placeholder.
/// <para>
/// Both of those are, on this server, keys into a closed set nothing on the wire spells out: a
/// guide name, a public type name, a recipe id, an embedded source path, a hosting model. Without
/// completions a person picking <c>bmage://api/{typeName}</c> in a client has to call a listing
/// tool first just to learn what a type is called. With them the client types ahead the way an
/// editor does, and a wrong value stops being reachable.
/// </para>
/// </summary>
public static class BmageCompletions
{
    /// <summary>The protocol caps one response at 100 values; anything past that is reported through <c>hasMore</c>.</summary>
    private const int MaxValues = 100;

    // Freeform arguments cannot be completed, only started off: these are the phrasings the matching
    // prompts were written for.
    private static readonly string[] _symptoms =
    [
        "the page throws 'Could not load the Bmage browser module' on first load",
        "WebP export throws ImageCapabilityException on Safari",
        "photos from phones appear sideways after export",
        "the editor is invisible or unstyled",
        "the tab crashes on mobile when processing large photos",
        "processing is very slow in the browser",
        "uploaded avatars still contain the GPS location",
        "ObjectDisposedException: ImageBuffer after loading into the editor",
        "There is no registered service of type 'ImageProcessor'",
        "the size budget throws 'Cannot reach N bytes for this image'",
    ];

    private static readonly string[] _requests =
    [
        "let users upload an avatar, crop it to a circle and keep it under 100 KB",
        "shrink photos in the browser before they are uploaded",
        "let users paste a screenshot, annotate it and redact secrets",
        "generate thumbnails in an ASP.NET Core endpoint",
        "remove the GPS location from uploaded photos but keep the camera details",
        "add an image editor with a German toolbar",
        "make product images in three sizes on a white background",
        "compose an Open Graph image with a caption",
    ];

    private static readonly string[] _goals =
    [
        "smallest file that still looks good",
        "under 150 KB",
        "fastest processing on a phone",
        "best quality for printing",
    ];

    /// <summary>
    /// The values matching what has been typed so far. The argument's name is enough to identify the
    /// set - no two arguments on this server mean different things by the same name - so one table
    /// serves a prompt argument and a resource template placeholder alike, and an argument it has
    /// nothing for is answered with an empty list rather than an error.
    /// </summary>
    public static Completion Complete(string? argumentName, string? typed)
    {
        var candidates = CandidatesFor(argumentName);
        if (candidates.Length == 0) return new Completion { Values = [] };

        var prefix = (typed ?? string.Empty).Trim();

        var matches = prefix.Length == 0
            ? candidates
            : [.. candidates
                .Where(value => value.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                // What was typed is a prefix far more often than it is a substring.
                .OrderByDescending(value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ThenBy(value => value, StringComparer.OrdinalIgnoreCase)];

        return new Completion
        {
            Values = [.. matches.Take(MaxValues)],
            Total = matches.Length,
            HasMore = matches.Length > MaxValues
        };
    }

    private static string[] CandidatesFor(string? argumentName) => argumentName switch
    {
        "name" => [.. BmageSourceCatalog.Documents.Select(document => document.Name)],
        "typeName" => [.. BmageApiCatalog.Types.Select(type => type.Name).Distinct(StringComparer.Ordinal)],
        "path" => [.. BmageSourceCatalog.SourceFiles.Select(file => file.Path)],
        "id" => [.. BmageRecipeCatalog.All.Select(recipe => recipe.Id)],
        "renderMode" => BmageSetupGuide.RenderModes,
        "source" => BmagePipelineLab.Sources,
        "format" => ["jpeg", "png", "bmp", "webp", "avif"],
        "symptom" => _symptoms,
        "request" => _requests,
        "goal" => _goals,
        _ => []
    };
}
