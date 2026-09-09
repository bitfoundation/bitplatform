namespace Bit.BlazorUI;

/// <summary>
/// Represents the basic information of a specific country.
/// </summary>
/// <param name="name">The full name of the country.</param>
/// <param name="code">The dialing code of the country.</param>
/// <param name="iso2">The ISO 3166-1 alpha-2 code of the country.</param>
/// <param name="iso3">The ISO 3166-1 alpha-3 code of the country.</param>
/// <param name="priority">The tie-breaking priority of the country among the ones sharing its dialing code.</param>
public class BitCountry(string name, string code, string iso2, string iso3, int priority = 0)
{
    /// <summary>
    /// The full name of the country.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// The dialing code of the country.
    /// </summary>
    public string Code { get; set; } = code;

    /// <summary>
    /// The ISO 3166-1 alpha-2 code of the country.
    /// </summary>
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

    private string? _digitsCode;
    private string? _digitsCodeOf;

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
            if (ReferenceEquals(_digitsCodeOf, Code) is false)
            {
                _digitsCodeOf = Code;
                _digitsCode = Code.Replace("-", string.Empty);
            }

            return _digitsCode!;
        }
    }
}
