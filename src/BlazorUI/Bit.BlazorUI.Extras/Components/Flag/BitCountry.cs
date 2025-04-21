namespace Bit.BlazorUI;

/// <summary>
/// Represents the basic information of a specific country.
/// </summary>
public class BitCountry(string name, string code, string iso2, string iso3)
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
}
