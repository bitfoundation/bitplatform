using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Borm;

/// <summary>
/// Validates the files chosen for a <see cref="BormFieldType.File"/> field. The browser's
/// <c>accept</c> attribute is only a filter in the file picker — a user can still drag in or select
/// anything — so size and extension are checked again here. On a text value it checks the extension
/// of the file name the text holds, which is what <c>[FileExtensions]</c> on a string property means.
/// </summary>
/// <param name="maxSizeBytes">The largest file accepted, in bytes.</param>
/// <param name="accept">The accepted extensions and media types, as the <c>accept</c> attribute lists them.</param>
/// <param name="message">The message for either failure, unless one of the more specific ones is given.</param>
/// <param name="sizeMessage">The message for a file that is too large.</param>
/// <param name="typeMessage">The message for a file of a type not accepted.</param>
public sealed class BormFileRule(
    long? maxSizeBytes = null,
    string? accept = null,
    string? message = null,
    string? sizeMessage = null,
    string? typeMessage = null)
    : IBormValidationRule
{
    private readonly string[] _accept = ParseAccept(accept);

    /// <summary>The caller's own message for a file that is too large, if there is one.</summary>
    internal string? SizeMessage { get; } = sizeMessage ?? message;

    /// <summary>The caller's own message for a file of a type not accepted, if there is one.</summary>
    internal string? TypeMessage { get; } = typeMessage ?? message;

    /// <inheritdoc />
    public string Key => "file";

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        // A file *name* — what [FileExtensions] checks on a string property — has no size and no media
        // type, so only the extensions in the accept list can be held against it.
        if (ctx.Value is string name)
        {
            var extensions = _accept.Where(t => t.StartsWith('.')).ToArray();
            if (name.Length == 0 || extensions.Length == 0
                || extensions.Any(e => name.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                return new(BormRuleResult.Success());
            return new(BormRuleResult.Fail(TypeMessage ?? ctx.Message(BormMessageKeys.FileType, string.Join(", ", extensions))));
        }

        foreach (var file in Enumerate(ctx.Value))
        {
            if (maxSizeBytes is { } max && file.Size > max)
                return new(BormRuleResult.Fail(SizeMessage ?? ctx.Message(BormMessageKeys.FileSize, FormatSize(max))));

            if (_accept.Length > 0 && !IsAccepted(file))
                return new(BormRuleResult.Fail(TypeMessage ?? ctx.Message(BormMessageKeys.FileType, string.Join(", ", _accept))));
        }
        return new(BormRuleResult.Success());
    }

    private bool IsAccepted(IBrowserFile file)
    {
        foreach (var token in _accept)
        {
            if (token.StartsWith('.'))
            {
                if (file.Name.EndsWith(token, StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (token.EndsWith("/*", StringComparison.Ordinal))
            {
                var prefix = token[..^1]; // "image/" from "image/*"
                if (file.ContentType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (string.Equals(file.ContentType, token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static IEnumerable<IBrowserFile> Enumerate(object? value) => value switch
    {
        IBrowserFile single => [single],
        IEnumerable<IBrowserFile> many => many,
        _ => Array.Empty<IBrowserFile>()
    };

    private static string[] ParseAccept(string? accept)
        => string.IsNullOrWhiteSpace(accept)
            ? []
            : accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// A byte count the way people read one — <c>1.5 MB</c>. Shared with the file input's list of chosen
    /// files, so the size a file is shown at and the limit it is told it broke are written alike.
    /// </summary>
    internal static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $"{size:0.#} {units[unit]}";
    }
}
