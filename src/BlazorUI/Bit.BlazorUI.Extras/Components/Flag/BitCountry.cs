namespace Bit.BlazorUI;

/// <summary>
/// Represents the basic information of a specific country.
/// </summary>
/// <remarks>
/// The four values are the four ways a country is written down in practice: the English
/// <see cref="Name"/>, the ISO 3166-1 <see cref="Iso2"/> and <see cref="Iso3"/> codes, and the E.164
/// <see cref="Code"/> a telephone number begins with. <see cref="BitCountries"/> holds one instance
/// per country and resolves any of the four back to it.
/// <br />
/// The instances of <see cref="BitCountries"/> are shared: they are the single copy every lookup and
/// every component answers with, so a page that edits one edits it for everything on the page. Build
/// a new instance instead of writing over a shared one.
/// </remarks>
/// <param name="name">The full name of the country.</param>
/// <param name="code">The dialing code of the country.</param>
/// <param name="iso2">The ISO 3166-1 alpha-2 code of the country.</param>
/// <param name="iso3">The ISO 3166-1 alpha-3 code of the country.</param>
/// <param name="priority">The tie-breaking priority of the country among the ones sharing its dialing code.</param>
/// <param name="extraCodes">The other dialing codes the country answers to beyond its main one.</param>
public class BitCountry(string name, string code, string iso2, string iso3, int priority = 0, string[]? extraCodes = null)
{
    /// <summary>
    /// The full name of the country.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// The dialing code of the country.
    /// </summary>
    /// <remarks>
    /// The E.164 country calling code, written without its leading plus sign. It is not unique: the
    /// North American Numbering Plan gives Canada and the United States the same "1", and Kazakhstan
    /// and Russia share "7", so a country resolved from a dialing code alone is the first of the ones
    /// that carry it.
    /// </remarks>
    public string Code { get; set; } = code;

    /// <summary>
    /// The ISO 3166-1 alpha-2 code of the country.
    /// </summary>
    /// <remarks>
    /// This is the code everything else is keyed by - the flag image, the emoji flag, and the lookups
    /// of <see cref="BitCountries"/>.
    /// </remarks>
    public string Iso2 { get; set; } = iso2;

    /// <summary>
    /// The ISO 3166-1 alpha-3 code of the country.
    /// </summary>
    public string Iso3 { get; set; } = iso3;

    /// <summary>
    /// The tie-breaking priority of the country among the ones that share its dialing code, where a
    /// higher number wins. Several countries can share a dialing code (+1 is both Canada and the United
    /// States, +7 both Kazakhstan and Russia), so a full number typed with an international prefix would
    /// otherwise resolve to whichever of them happens to come first in the list. The country that owns
    /// the code in practice carries a priority above the default zero of the others.
    /// </summary>
    public int Priority { get; set; } = priority;

    /// <summary>
    /// The other dialing codes the country answers to beyond <see cref="Code"/>. Several countries own
    /// more than one: the Dominican Republic is reached on +1-809, +1-829 and +1-849 alike. They are
    /// matched when a full number is read back into its parts and when the country list is searched,
    /// while <see cref="Code"/> stays the one code the country is shown with.
    /// </summary>
    public string[]? ExtraCodes { get; set; } = extraCodes;

    private string? _digitsCode;
    private string? _digitsCodeOf;
    private string[]? _digitsCodes;
    private string? _digitsCodesOfCode;
    private string[]? _digitsCodesOfExtra;

    /// <summary>
    /// The dialing code of the country reduced to its digits, as it appears in an E.164 number.
    /// </summary>
    /// <remarks>
    /// Cached against the code it was derived from: a phone input matches a typed number against the
    /// code of every country it offers on every keystroke, and Code stays the same across all of them.
    /// </remarks>
    public string DigitsCode
    {
        get
        {
            // The countries of BitCountries are shared by every phone input of the process, so the
            // cache is filled value first and key last, published with a release write and read back
            // with an acquire read: a reader that sees the new key is then guaranteed to see the
            // value that goes with it rather than a half written one.
            var code = Code;

            if (ReferenceEquals(Volatile.Read(ref _digitsCodeOf), code) is false)
            {
                var digits = code.Replace("-", string.Empty);
                _digitsCode = digits;
                Volatile.Write(ref _digitsCodeOf, code);

                return digits;
            }

            return _digitsCode!;
        }
    }

    /// <summary>
    /// Every dialing code of the country - <see cref="Code"/> first, then <see cref="ExtraCodes"/> -
    /// reduced to the digits each of them carries in an E.164 number.
    /// </summary>
    /// <remarks>
    /// Cached against the codes it was derived from, for the same reason <see cref="DigitsCode"/> is:
    /// a phone input matches a typed number against every code of every country it offers on every
    /// keystroke.
    /// </remarks>
    public string[] DigitsCodes
    {
        get
        {
            var code = Code;
            var extra = ExtraCodes;

            // Filled value first and key last, for the same reason DigitsCode is.
            if (ReferenceEquals(Volatile.Read(ref _digitsCodesOfCode), code) is false ||
                ReferenceEquals(Volatile.Read(ref _digitsCodesOfExtra), extra) is false)
            {
                string[] digitsCodes = extra is null || extra.Length == 0
                                        ? [DigitsCode]
                                        : [DigitsCode, .. extra.Select(c => c.Replace("-", string.Empty))];
                _digitsCodes = digitsCodes;
                _digitsCodesOfExtra = extra;
                Volatile.Write(ref _digitsCodesOfCode, code);

                return digitsCodes;
            }

            return _digitsCodes!;
        }
    }

    /// <summary>
    /// The flag of the country as a Unicode emoji, built from the pair of regional indicator symbols
    /// its <see cref="Iso2"/> stands for.
    /// </summary>
    /// <remarks>
    /// It is text rather than a picture, so it takes the color, the size and the line box of whatever
    /// it is written in and stays crisp at any of them, and it costs no request at all. What it looks
    /// like is the platform's to decide: the emoji fonts of Apple, Google and the Noto family draw the
    /// flags, while Windows draws the two letters side by side instead - which is why the image is
    /// what <see cref="BitFlag"/> renders unless it is asked for the emoji.
    /// <br />
    /// It is null where the <see cref="Iso2"/> is not two ASCII letters.
    /// </remarks>
    public string? Emoji => BitCountries.GetEmoji(Iso2);

    /// <summary>
    /// Returns the full name of the country.
    /// </summary>
    public override string ToString() => Name;
}
