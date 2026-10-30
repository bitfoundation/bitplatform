using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchComposition(WorkbenchBackdrop Backdrop, string? Caption = null)
{
    public const int MaxCaptionLength = 120;

    public bool HasLightBackdrop => Backdrop is WorkbenchBackdrop.Paper;

    public static WorkbenchComposition? Parse(string? query)
    {
        var values = QueryHelpers.ParseQuery(query);

        if (values.TryGetValue("backdrop", out var backdrop) is false || Enum.TryParse<WorkbenchBackdrop>(backdrop, ignoreCase: true, out var value) is false)
            return null;

        var caption = values.TryGetValue("caption", out var text) ? text.ToString().Trim() : null;

        return new(value, string.IsNullOrEmpty(caption) ? null : caption[..Math.Min(caption.Length, MaxCaptionLength)]);
    }

    public string AppendTo(string url)
    {
        var values = new Dictionary<string, string?> { ["backdrop"] = Backdrop.ToString().ToLowerInvariant() };

        if (string.IsNullOrWhiteSpace(Caption) is false)
        {
            values["caption"] = Caption;
        }

        return QueryHelpers.AddQueryString(url, values);
    }
}
