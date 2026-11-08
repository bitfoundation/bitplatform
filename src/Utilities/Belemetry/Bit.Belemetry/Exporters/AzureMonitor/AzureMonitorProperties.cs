using System.Diagnostics.CodeAnalysis;

namespace Bit.Belemetry;

/// <summary>
/// The properties (custom dimensions) of an Application Insights item: the attributes of its record, less the ids the
/// envelope carries as tags and the attributes the item's own fields consume, plus the properties the envelope adds
/// itself (a failed span's status message, an event's body, a trace's category, a page visit's page) unless the item
/// has a property of that key already. The envelope writes them (<see cref="AzureMonitorEnvelopeSerializer"/>), and
/// Live Metrics filters on them and samples them (<see cref="AzureMonitorLiveItem"/>): one rule, so a sample has the
/// properties of the item it stands for.
/// </summary>
/// <remarks>It reads the record's attributes when asked, not when made.</remarks>
internal readonly struct AzureMonitorProperties
{
    private static readonly Func<string, bool> _exceptionFields = static key => key.StartsWith("exception.", StringComparison.Ordinal);

    private static readonly Func<string, bool> _pageViewFields = static key => key is BelemetrySemanticConventions.UrlFull or BelemetrySemanticConventions.PageTitle;

    private static readonly Func<string, bool> _webVitalFields = static key => key is BelemetrySemanticConventions.WebVitalName or BelemetrySemanticConventions.WebVitalValue;

    // The page visit's PageName (and its PageUrl, when it has one) take the place of attributes of their keys, as in the
    // Application Insights JS SDK's autoTrackPageVisitTime shape that Scripts/exporters/azure.ts mirrors.
    private static readonly Func<string, bool> _pageVisitFields = static key => key is BelemetrySemanticConventions.UrlFull or BelemetrySemanticConventions.UrlPath
                                                                                     or BelemetrySemanticConventions.PageTitle or BelemetrySemanticConventions.PageVisitDuration
                                                                                     or PageNameProperty;

    private static readonly Func<string, bool> _pageVisitFieldsWithUrl = static key => _pageVisitFields(key) || key == PageUrlProperty;

    private const string MessageProperty = "message";
    private const string CategoryProperty = "CategoryName";
    private const string PageNameProperty = "PageName";
    private const string PageUrlProperty = "PageUrl";

    private readonly BelemetryAttributes _attributes;
    private readonly Func<string, bool>? _consumed;
    private readonly KeyValuePair<string, string>? _added;
    private readonly KeyValuePair<string, string>? _addedToo;

    /// <param name="attributes">The record's attributes.</param>
    /// <param name="consumed">Which of them the item's fields consume, which are then no properties.</param>
    /// <param name="key">The key of a property the envelope adds.</param>
    /// <param name="value">Its value: none when null or empty.</param>
    /// <param name="keyToo">The key of a second one.</param>
    /// <param name="valueToo">Its value.</param>
    private AzureMonitorProperties(BelemetryAttributes attributes, Func<string, bool>? consumed = null, string? key = null, string? value = null,
                                   string? keyToo = null, string? valueToo = null)
    {
        _attributes = attributes;
        _consumed = consumed;
        _added = null;
        _addedToo = null;
        if (key is not null && string.IsNullOrEmpty(value) is false && HasAttribute(key) is false) _added = new(key, value);
        if (keyToo is not null && string.IsNullOrEmpty(valueToo) is false && HasAttribute(keyToo) is false) _addedToo = new(keyToo, valueToo);
    }

    /// <summary>A request's or dependency's: its attributes, and a failed span's status message as its <c>message</c>.</summary>
    public static AzureMonitorProperties OfSpan(BelemetrySpan span) =>
        new(span.Attributes, null, MessageProperty, span.Status == BelemetryStatusCode.Error ? span.StatusMessage : null);

    /// <summary>An item's whose fields consume no attribute (the Message of a span event, a metric point): its attributes.</summary>
    public static AzureMonitorProperties Of(BelemetryAttributes attributes) => new(attributes);

    /// <summary>An exception's, a log record's or one recorded on a span: its attributes but its <c>exception.*</c> ones, which are its fields.</summary>
    public static AzureMonitorProperties OfException(BelemetryAttributes attributes) => new(attributes, _exceptionFields);

    /// <summary>
    /// A log record's, as the item it is: an exception's; a page view's or web vital's, less the attributes their fields
    /// consume; a page visit time's, with its <c>PageName</c> and <c>PageUrl</c>; an event's, with its body as its
    /// <c>message</c>; a trace's, with its logger's category as its <c>CategoryName</c>.
    /// </summary>
    public static AzureMonitorProperties OfLog(BelemetryLogRecord log)
    {
        var attributes = log.Attributes;
        if (log.HasException) return OfException(attributes);

        switch (log.EventName)
        {
            case BelemetrySemanticConventions.PageViewEvent:
                return new(attributes, _pageViewFields);
            case BelemetrySemanticConventions.WebVitalEvent:
                return new(attributes, _webVitalFields);
            case BelemetrySemanticConventions.PageVisitTimeEvent:
                var url = attributes.GetString(BelemetrySemanticConventions.UrlFull);
                var name = log.Body is { Length: > 0 } body ? body
                         : attributes.GetString(BelemetrySemanticConventions.PageTitle) is { Length: > 0 } title ? title
                         : attributes.GetString(BelemetrySemanticConventions.UrlTemplate) is { Length: > 0 } template ? template
                         : attributes.GetString(BelemetrySemanticConventions.UrlPath) ?? "page";
                return new(attributes, url is null ? _pageVisitFields : _pageVisitFieldsWithUrl, PageNameProperty, name, PageUrlProperty, url);
            case { Length: > 0 }:
                return new(attributes, null, MessageProperty, log.Body);
            default:
                return new(attributes, null, CategoryProperty, log.Scope.Name);
        }
    }

    /// <summary>The value of the property <paramref name="key"/>, stringified; false without one.</summary>
    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        if (IsProperty(key) && _attributes.TryGetValue(key, out var raw))
        {
            value = BelemetryAttributes.ToInvariantString(raw);
            return true;
        }
        if (_added is { } added && added.Key == key)
        {
            value = added.Value;
            return true;
        }
        if (_addedToo is { } addedToo && addedToo.Key == key)
        {
            value = addedToo.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>The properties, stringified and whole (the writer cuts them), the attributes first.</summary>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>Whether an attribute of <paramref name="key"/> is a property.</summary>
    private bool IsProperty(string key) => AzureMonitorEnvelopeSerializer.IsContextAttribute(key) is false && (_consumed?.Invoke(key) ?? false) is false;

    /// <summary>Whether the item has a property of <paramref name="key"/> from an attribute: one the envelope adds then gives way.</summary>
    private bool HasAttribute(string key) => IsProperty(key) && _attributes.Contains(key);

    /// <summary>Walks the properties: the attributes that are properties, then the ones the envelope adds.</summary>
    public struct Enumerator : IDisposable
    {
        private readonly AzureMonitorProperties _properties;
        private readonly IEnumerator<KeyValuePair<string, object>> _attributes;
        private int _added;

        internal Enumerator(AzureMonitorProperties properties)
        {
            _properties = properties;
            _attributes = properties._attributes.GetEnumerator();
        }

        public KeyValuePair<string, string> Current { get; private set; }

        public bool MoveNext()
        {
            while (_added == 0 && _attributes.MoveNext())
            {
                var (key, value) = _attributes.Current;
                if (_properties.IsProperty(key) is false) continue;
                Current = new(key, BelemetryAttributes.ToInvariantString(value));
                return true;
            }

            while (_added < 2)
            {
                var added = _added++ == 0 ? _properties._added : _properties._addedToo;
                if (added is null) continue;
                Current = added.Value;
                return true;
            }
            return false;
        }

        public readonly void Dispose() => _attributes.Dispose();
    }
}
