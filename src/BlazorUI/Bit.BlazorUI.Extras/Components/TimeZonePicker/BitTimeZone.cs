namespace Bit.BlazorUI;

/// <summary>
/// Represents a time zone of the IANA time zone database, the way <see cref="BitTimeZonePicker{TValue}"/> lists it.
/// </summary>
/// <remarks>
/// A time zone is identified by its canonical IANA <see cref="Id"/> ("America/Los_Angeles"), which is the one id every
/// runtime a Blazor app runs on can resolve: the browser's WebAssembly runtime, Linux, Android, iOS and macOS read IANA
/// ids natively, and Windows converts them through ICU. The <see cref="WindowsId"/> it maps to is carried beside it,
/// so a Windows id handed in from a Windows server is resolved back to the same entry, and a Windows runtime that
/// cannot convert IANA ids (an app running in NLS mode) still finds its <see cref="TimeZoneInfo"/>.
/// <br />
/// The instances of <see cref="BitTimeZones"/> are shared: they are the single copy every lookup and every component
/// answers with, so a page that edits one edits it for everything on the page. Build a new instance instead of writing
/// over a shared one - which is how the names are translated.
/// </remarks>
/// <param name="id">The canonical IANA id of the time zone.</param>
/// <param name="name">The name of the place the time zone is named after.</param>
/// <param name="countryCode">The ISO 3166-1 alpha-2 code of the country the time zone is in.</param>
/// <param name="windowsId">The Windows id the time zone maps to.</param>
/// <param name="countryName">The name of the country the time zone is in, when it is not the one of <see cref="BitCountries"/>.</param>
public class BitTimeZone(string id, string name, string? countryCode = null, string? windowsId = null, string? countryName = null)
{
    private string? _countryName = countryName;
    private bool _countryNameSet = countryName is not null;

    /// <summary>
    /// The canonical IANA id of the time zone ("America/Los_Angeles").
    /// </summary>
    /// <remarks>
    /// This is the id everything else is keyed by - the value a <see cref="BitTimeZonePicker{TValue}"/> bound to a
    /// string writes, the text a form posts and the lookups of <see cref="BitTimeZones"/>.
    /// </remarks>
    public string Id { get; set; } = id;

    /// <summary>
    /// The name of the place the time zone is named after ("Los Angeles"), which is what the picker shows and
    /// searches. It is the last part of the IANA id with its underscores read as spaces, unless a page translates it.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// The ISO 3166-1 alpha-2 code of the country the time zone is in, or null for one that belongs to no country
    /// (UTC).
    /// </summary>
    public string? CountryCode { get; set; } = countryCode;

    /// <summary>
    /// The name of the country the time zone is in, which the picker shows beside the <see cref="Name"/> and searches
    /// as well, so "Germany" finds Berlin. Unless it is given, it is the name <see cref="BitCountries"/> carries for the
    /// <see cref="CountryCode"/>.
    /// </summary>
    public string? CountryName
    {
        get
        {
            if (_countryNameSet is false)
            {
                _countryName = BitCountries.FindByIso2(CountryCode)?.Name;
                _countryNameSet = true;
            }

            return _countryName;
        }
        set
        {
            _countryName = value;
            _countryNameSet = true;
        }
    }

    /// <summary>
    /// The Windows id the time zone maps to ("Pacific Standard Time"), from the CLDR mapping between the two, or null
    /// for a time zone Windows has none for. Several time zones share one Windows id.
    /// </summary>
    public string? WindowsId { get; set; } = windowsId;

    /// <summary>
    /// The <see cref="System.TimeZoneInfo"/> of the time zone on the runtime the app is running on, or null where the
    /// runtime knows nothing of it - an app that stripped the time zone data out of its WebAssembly runtime
    /// (InvariantTimezone).
    /// </summary>
    /// <remarks>
    /// It is looked up by the IANA id first, then by the older ids that stand for it, and then by the
    /// <see cref="WindowsId"/>, so a Windows runtime that cannot convert IANA ids still finds it. The answer is cached
    /// for the process.
    /// </remarks>
    public TimeZoneInfo? TimeZoneInfo => BitTimeZones.GetTimeZoneInfo(this);

    /// <summary>
    /// The offset from UTC the time zone is at, at the given moment - daylight saving time included - or null where
    /// the runtime knows nothing of the time zone.
    /// </summary>
    public TimeSpan? GetUtcOffset(DateTimeOffset at) => TimeZoneInfo?.GetUtcOffset(at);

    /// <summary>
    /// Returns the name of the time zone.
    /// </summary>
    public override string ToString() => Name;
}
