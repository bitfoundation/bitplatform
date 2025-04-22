namespace Bit.BlazorUI;

/// <summary>
/// BitFlag is a component that renders the flag image of a country.
/// </summary>
public partial class BitFlag : BitComponentBase
{
    /// <summary>
    /// The dialing code of the country.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Code { get; set; }

    /// <summary>
    /// The country to render the flag.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitCountry? Country { get; set; }

    /// <summary>
    /// The ISO 3166-1 alpha-2 code of the country.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Iso2 { get; set; }

    /// <summary>
    /// The ISO 3166-1 alpha-3 code of the country.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Iso3 { get; set; }

    /// <summary>
    /// The tooltip value of the flag element.
    /// </summary>
    [Parameter] public string? Title { get; set; }



    protected override string RootElementClass => "bit-flg";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() =>
        {
            var iso2 = FindIso2();
            return iso2.HasValue()
                    ? $"background-image:url('_content/Bit.BlazorUI.Extras/flags/{iso2!.ToUpperInvariant()}-flat-16.webp')" 
                    : string.Empty;
        });
    }


    private string? FindIso2()
    {
        if (Country is not null) return Country.Iso2;

        if (Iso2.HasValue()) return Iso2;

        if (Iso3.HasValue())
        {
            return BitCountries.All.FirstOrDefault(c => c.Iso3.Equals(Iso3, StringComparison.InvariantCultureIgnoreCase))?.Iso2;
        }

        if (Code.HasValue())
        {
            return BitCountries.All.FirstOrDefault(c => c.Code.Equals(Code, StringComparison.InvariantCultureIgnoreCase))?.Iso2;
        }

        return null;
    }
}
