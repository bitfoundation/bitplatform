using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Borm;

/// <summary>
/// Validates the files chosen for a <see cref="BormFieldType.File"/> field. The browser's
/// <c>accept</c> attribute is only a filter in the file picker — a user can still drag in or select
/// anything — so size and extension are checked again here. On a text value it checks the extension
/// of the file name the text holds, which is what <c>[FileExtensions]</c> on a string property means.
/// </summary>
public sealed class BormFileRule(long? maxSizeBytes = null, string? accept = null, string? message = null)
    : IBormValidationRule
{
    private readonly string[] _accept = ParseAccept(accept);

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
            return new(BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.FileType, string.Join(", ", extensions))));
        }

        foreach (var file in Enumerate(ctx.Value))
        {
            if (maxSizeBytes is { } max && file.Size > max)
                return new(BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.FileSize, FormatSize(max))));

            if (_accept.Length > 0 && !IsAccepted(file))
                return new(BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.FileType, string.Join(", ", _accept))));
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

    private static string FormatSize(long bytes)
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
