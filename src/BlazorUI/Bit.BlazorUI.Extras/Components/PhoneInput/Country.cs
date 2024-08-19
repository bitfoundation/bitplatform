namespace Bit.BlazorUI;

public class Country(string name, string code)
{
    public string Name { get; set; } = name;

    public string Code { get; set; } = code;
}
