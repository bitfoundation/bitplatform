using System.Text;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Writes a parameter changed in the component panel into its tag in the .razor file, as text, so the rest of the file stays
/// as it was; dotnet watch then hot reloads the change into the views. Only requests from this machine may write.
/// </summary>
public sealed partial class WorkbenchComponentEdits
{
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> fileLocks = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether the project folder, and with it the .razor files, is on this machine.</summary>
    public bool CanWriteFiles => WorkbenchFolders.Source is not null;

    /// <summary>Whether dotnet watch runs the Workbench, so a saved change shows without a restart.</summary>
    public static bool IsHotReloading => Environment.GetEnvironmentVariable("DOTNET_WATCH") is "1";

    /// <summary>Sets a parameter to a value; null takes the attribute away, so the component's default applies again.</summary>
    public Task<bool> Set(string component, WorkbenchComponentTag tag, string parameter, string? value)
    {
        if (WorkbenchComponentParameters.TypeOf(component) is not { } type || WorkbenchComponentParameters.ParameterOf(type, parameter) is not { } property)
            return Task.FromResult(false);

        string? attribute = null;

        if (value is not null && (attribute = WorkbenchRazorValues.Attribute(property, value)) is null)
            return Task.FromResult(false);

        return Write(component, tag, property, attribute);
    }

    /// <summary>Sets the parts of a Styles or Classes parameter; with every part empty, takes the attribute away.</summary>
    public Task<bool> SetParts(string component, WorkbenchComponentTag tag, string parameter, IEnumerable<WorkbenchComponentPart> parts)
    {
        if (WorkbenchComponentParameters.TypeOf(component) is not { } type
            || WorkbenchComponentParameters.ParameterOf(type, parameter) is not { } property
            || WorkbenchRazorValues.IsParts(property.PropertyType) is false)
            return Task.FromResult(false);

        return Write(component, tag, property, WorkbenchRazorValues.PartsAttribute(property, parts));
    }

    /// <summary>
    /// Replaces, adds or takes away the parameter's attribute in the tag. Returns false when the request isn't from this
    /// machine, the tag is gone, or code sets the parameter, which only the code's author may change.
    /// </summary>
    private async Task<bool> Write(string component, WorkbenchComponentTag tag, PropertyInfo property, string? attribute)
    {
        if (IsLocalRequest is false || WorkbenchRazorFiles.FullPath(tag.File) is not { } path)
            return false;

        var fileLock = fileLocks.GetOrAdd(path, _ => new(1, 1));

        await fileLock.WaitAsync();

        try
        {
            var raw = await File.ReadAllBytesAsync(path);
            var hasBom = raw.AsSpan().StartsWith(Encoding.UTF8.Preamble);
            var text = Encoding.UTF8.GetString(hasBom ? raw[3..] : raw);
            var starts = WorkbenchRazorTags.Find(text, component);

            if (tag.Ordinal >= starts.Count || WorkbenchRazorTags.Parse(text, starts[tag.Ordinal], component) is not { } parsed)
                return false;

            var existing = parsed.Attributes.LastOrDefault(a => string.Equals(WorkbenchComponentParameters.ParameterNameOf(a), property.Name, StringComparison.OrdinalIgnoreCase));

            if (existing is not null && IsPlain(property, existing) is false)
                return false;

            var changed = (existing, attribute) switch
            {
                (null, null) => text,
                (null, { } added) => Insert(text, starts[tag.Ordinal], parsed.NameEnd, parsed.Attributes, added),
                ({ } old, null) => Remove(text, old),
                ({ } old, { } replaced) => text[..old.Start] + replaced + text[old.End..]
            };

            if (changed != text)
            {
                await File.WriteAllBytesAsync(path, [.. (hasBom ? Encoding.UTF8.Preamble.ToArray() : []), .. Encoding.UTF8.GetBytes(changed)]);
            }

            return true;
        }
        finally
        {
            fileLock.Release();
        }
    }

    private static bool IsPlain(PropertyInfo property, WorkbenchRazorAttribute attribute)
    {
        if (attribute.Name.StartsWith('@'))
            return false;

        return WorkbenchRazorValues.IsParts(property.PropertyType)
            ? WorkbenchRazorValues.TryReadParts(attribute.Value) is not null
            : WorkbenchRazorValues.TryRead(property.PropertyType, attribute.Value, out _);
    }

    /// <summary>
    /// Adds the attribute after the tag's last one: on a line of its own, indented like it, when that one starts its line;
    /// otherwise after a space.
    /// </summary>
    private static string Insert(string text, int tagStart, int nameEnd, List<WorkbenchRazorAttribute> attributes, string attribute)
    {
        if (attributes.Count is 0)
            return text.Insert(nameEnd, $" {attribute}");

        var last = attributes[^1];
        var lineStart = text.LastIndexOf('\n', last.Start - 1) + 1;
        var indent = text[lineStart..last.Start];

        return lineStart > tagStart && string.IsNullOrWhiteSpace(indent)
            ? text.Insert(last.End, $"{(text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n")}{indent}{attribute}")
            : text.Insert(last.End, $" {attribute}");
    }

    /// <summary>
    /// Takes an attribute away with the space before it; one that starts its line goes with the line break before it, so
    /// what follows it on the line, like the tag's end, joins the line above.
    /// </summary>
    private static string Remove(string text, WorkbenchRazorAttribute attribute)
    {
        var lineStart = text.LastIndexOf('\n', attribute.Start - 1) + 1;
        var start = attribute.Start;

        if (lineStart > 0 && string.IsNullOrWhiteSpace(text[lineStart..attribute.Start]))
        {
            start = lineStart - 1;

            if (start > 0 && text[start - 1] is '\r')
            {
                start--;
            }
        }
        else
        {
            while (start > 0 && text[start - 1] is ' ' or '\t')
            {
                start--;
            }
        }

        return text[..start] + text[attribute.End..];
    }

    /// <summary>Whether the current request, or the circuit it started, came from this machine.</summary>
    private bool IsLocalRequest => httpContextAccessor.HttpContext is { } context && WorkbenchLocalRequests.IsLocal(context);
}
