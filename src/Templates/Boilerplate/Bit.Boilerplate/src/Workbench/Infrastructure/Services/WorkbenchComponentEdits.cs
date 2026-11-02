using System.Text;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Writes a parameter changed in the component panel into its tag in the .razor file, as text, so the rest of the file stays
/// as it was; dotnet watch then hot reloads the change into the views. Only requests in dev mode, from this machine, may write.
/// </summary>
public sealed partial class WorkbenchComponentEdits
{
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    // How long a removal waits for hot reload to apply the default it writes first; a change that doesn't compile never applies.
    private static readonly TimeSpan maxHotReloadWait = TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> fileLocks = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether the project folder, and with it the .razor files, is on this machine.</summary>
    public bool CanWriteFiles => WorkbenchFolders.Source is not null;

    /// <summary>Sets a parameter to a value; null takes the attribute away, so the component's default, as the panel read it, applies again.</summary>
    public Task<bool> Set(string component, WorkbenchComponentTag tag, string parameter, string? value, string? defaultValue)
    {
        if (WorkbenchComponentParameters.TypeOf(component) is not { } type || WorkbenchComponentParameters.ParameterOf(type, parameter) is not { } property)
            return Task.FromResult(false);

        string? attribute = null;

        if (value is not null && (attribute = WorkbenchRazorValues.Attribute(property, value)) is null)
            return Task.FromResult(false);

        return Write(component, tag, property, attribute, attribute is null ? WorkbenchRazorValues.DefaultAttribute(property, defaultValue) : null);
    }

    /// <summary>Sets the parts of a Styles or Classes parameter; with every part empty, takes the attribute away.</summary>
    public Task<bool> SetParts(string component, WorkbenchComponentTag tag, string parameter, IEnumerable<WorkbenchComponentPart> parts)
    {
        if (WorkbenchComponentParameters.TypeOf(component) is not { } type
            || WorkbenchComponentParameters.ParameterOf(type, parameter) is not { } property
            || WorkbenchRazorValues.IsParts(property.PropertyType) is false)
            return Task.FromResult(false);

        var attribute = WorkbenchRazorValues.PartsAttribute(property, parts);

        return Write(component, tag, property, attribute, attribute is null ? WorkbenchRazorValues.DefaultAttribute(property, null) : null);
    }

    /// <summary>
    /// Replaces, adds or takes away the parameter's attribute in the tag. Returns false when the request may not change the
    /// code, the tag is gone, or code sets the parameter, which only the code's author may change.
    /// Hot reload leaves a parameter a tag stops setting at the value it last had, so before an attribute goes, the reset
    /// attribute sets the default and the views get it; the components then keep it once the attribute is gone.
    /// </summary>
    private async Task<bool> Write(string component, WorkbenchComponentTag tag, PropertyInfo property, string? attribute, string? reset)
    {
        if (CanChange is false || WorkbenchRazorFiles.FullPath(tag.File) is not { } path)
            return false;

        var fileLock = fileLocks.GetOrAdd(path, _ => new(1, 1));

        await fileLock.WaitAsync();

        try
        {
            if (reset is not null && await ResetFirst(path, component, tag, property, reset) is false)
                return false;

            return await Change(path, component, tag, property, attribute, onlyReplace: false) is not null;
        }
        finally
        {
            fileLock.Release();
        }
    }

    /// <summary>Writes the reset attribute in place of the one there and waits until hot reload applied it; false when the tag can't be changed.</summary>
    private static async Task<bool> ResetFirst(string path, string component, WorkbenchComponentTag tag, PropertyInfo property, string reset)
    {
        var updated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnUpdated() => updated.TrySetResult();

        WorkbenchComponentSources.Updated += OnUpdated;

        try
        {
            switch (await Change(path, component, tag, property, reset, onlyReplace: true))
            {
                case null:
                    return false;
                case true:
                    await Task.WhenAny(updated.Task, Task.Delay(maxHotReloadWait));
                    // The views render the update right after it applies.
                    await Task.Delay(300);
                    break;
            }

            return true;
        }
        finally
        {
            WorkbenchComponentSources.Updated -= OnUpdated;
        }
    }

    /// <summary>
    /// Puts the attribute in the tag, in place of the one there, or takes that one away for null; with onlyReplace, a tag
    /// without one stays as it is. Null when the tag is gone or code sets the parameter; otherwise whether the file changed.
    /// </summary>
    private static async Task<bool?> Change(string path, string component, WorkbenchComponentTag tag, PropertyInfo property, string? attribute, bool onlyReplace)
    {
        var raw = await File.ReadAllBytesAsync(path);
        var hasBom = raw.AsSpan().StartsWith(Encoding.UTF8.Preamble);
        var text = Encoding.UTF8.GetString(hasBom ? raw[3..] : raw);
        var starts = WorkbenchRazorTags.Find(text, component);

        if (tag.Ordinal >= starts.Count || WorkbenchRazorTags.Parse(text, starts[tag.Ordinal], component) is not { } parsed)
            return null;

        var existing = parsed.Attributes.LastOrDefault(a => string.Equals(WorkbenchComponentParameters.ParameterNameOf(a), property.Name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null && IsPlain(property, existing) is false)
            return null;

        if (existing is null && onlyReplace)
            return false;

        var changed = (existing, attribute) switch
        {
            (null, null) => text,
            (null, { } added) => Insert(text, starts[tag.Ordinal], parsed.NameEnd, parsed.Attributes, added),
            ({ } old, null) => Remove(text, old),
            ({ } old, { } replaced) => text[..old.Start] + replaced + text[old.End..]
        };

        if (changed == text)
            return false;

        await File.WriteAllBytesAsync(path, [.. (hasBom ? Encoding.UTF8.Preamble.ToArray() : []), .. Encoding.UTF8.GetBytes(changed)]);

        return true;
    }

    private static bool IsPlain(PropertyInfo property, WorkbenchRazorAttribute attribute)
    {
        if (attribute.Name.StartsWith('@'))
            return false;

        if (WorkbenchRazorValues.IsNull(property.PropertyType, attribute.Value))
            return true;

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

    /// <summary>Whether the current request, or the circuit it started, may change the code.</summary>
    private bool CanChange => httpContextAccessor.HttpContext is { } context && WorkbenchLocalRequests.CanChange(context, configuration);
}
