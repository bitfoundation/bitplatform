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
public class BitCountry(string name, string code, string iso2, string iso3)
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
