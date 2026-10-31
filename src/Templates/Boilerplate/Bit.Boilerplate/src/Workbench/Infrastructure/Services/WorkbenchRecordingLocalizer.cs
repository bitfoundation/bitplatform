using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Boilerplate.Workbench.Infrastructure.Services;

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
            var raw = edits.Find(typeof(T).Name, name) ?? inner[name].Value;
            var value = edits.Find(typeof(T).Name, name) is { } edited ? new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, edited, arguments)) : inner[name, arguments];

            return Record(name, raw, value);
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
