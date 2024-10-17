namespace Bit.BlazorUI;

public class Country(string name, string code, string iso2, string iso3)
{
    public string Name { get; set; } = name;

    public string Code { get; set; } = code;

    public string Iso2 { get; set; } = iso2;

    public string Iso3 { get; set; } = iso3;
}
