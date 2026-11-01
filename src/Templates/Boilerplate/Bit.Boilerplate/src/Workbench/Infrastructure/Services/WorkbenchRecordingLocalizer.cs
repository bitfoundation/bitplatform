using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Wraps every string localizer: shows texts saved in Edit text mode, which the resx files only serve after a restart,
/// and records each text it shows, so a text picked on the page can be traced to its resource entry.
/// </summary>
public sealed class WorkbenchRecordingLocalizer<T>(IStringLocalizerFactory factory, WorkbenchTextRecorder recorder, WorkbenchTextEdits edits) : IStringLocalizer<T>
{
    private readonly IStringLocalizer inner = factory.Create(typeof(T));

    public LocalizedString this[string name]
    {
        get
        {
            var value = edits.Find(typeof(T).Name, name) is { } edited ? new LocalizedString(name, edited) : inner[name];

            return Record(name, value.Value, value);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            if (edits.Find(typeof(T).Name, name) is { } edited)
                return Record(name, edited, new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, edited, arguments)));

            // One lookup gives both the raw text to record and the text to format, as the inner localizer formats it.
            var raw = inner[name];

            return Record(name, raw.Value, new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, raw.Value, arguments), raw.ResourceNotFound, raw.SearchedLocation));
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => inner.GetAllStrings(includeParentCultures);

    private LocalizedString Record(string name, string raw, LocalizedString value)
    {
        if (value.ResourceNotFound is false)
        {
            recorder.Record(typeof(T).Name, name, raw, value.Value);
        }

        return value;
    }
}
