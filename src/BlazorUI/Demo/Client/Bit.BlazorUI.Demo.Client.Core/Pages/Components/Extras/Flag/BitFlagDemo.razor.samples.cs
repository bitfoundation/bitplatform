namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Flag;

public partial class BitFlagDemo
{
    private readonly string example1RazorCode = @"
<BitFlag Country=""BitCountries.Iran"" />
<BitFlag Country=""BitCountries.Netherlands"" />
<BitFlag Country=""BitCountries.Japan"" />
<BitFlag Country=""BitCountries.Brazil"" />";

    private readonly string example2RazorCode = @"
<BitFlag Iso2=""nl"" />

<BitFlag Iso3=""NLD"" />

<BitFlag Code=""+31"" />

<BitFlag Name=""Netherlands"" />";

    private readonly string example3RazorCode = @"
<BitFlag Rounded Height=""2rem"" Country=""BitCountries.Japan"" />

<BitFlag Circular Height=""2rem"" Country=""BitCountries.Japan"" />

<BitFlag Bordered Height=""2rem"" Country=""BitCountries.Japan"" />

<BitFlag Shadow Height=""2rem"" Country=""BitCountries.Japan"" />

<BitFlag Circular Bordered Shadow Height=""2rem"" Country=""BitCountries.Japan"" />";

    private readonly string example4RazorCode = @"
<BitFlag Emoji Country=""BitCountries.Iran"" />

<BitFlag Emoji Height=""1.5rem"" Country=""BitCountries.Netherlands"" />

<BitFlag Emoji Height=""2rem"" Country=""BitCountries.Japan"" />

<BitFlag Emoji Height=""3rem"" Country=""BitCountries.Brazil"" />";

    private readonly string example5RazorCode = @"
<BitFlag Country=""BitCountries.Canada"" />

<BitFlag AutoAlt Country=""BitCountries.Canada"" />

<BitFlag Alt=""Ships to Canada"" Country=""BitCountries.Canada"" />

<BitFlag AutoTitle Country=""BitCountries.Canada"" />";

    private readonly string example6RazorCode = @"
<BitFlag Iso2=""zz"" />

<BitFlag Iso2=""zz"" Bordered Height=""1.5rem"">
    <FallbackTemplate>?</FallbackTemplate>
</BitFlag>

<BitFlag Height=""2rem"" Src=""@japanSvg"" AutoTitle Country=""BitCountries.Japan"" />

<BitFlag Height=""1.5rem"" Bordered Src=""/not-a-real-flag.png"">
    <FallbackTemplate><BitIcon IconName=""@BitIconName.Error"" /></FallbackTemplate>
</BitFlag>";

    private readonly string example6CsharpCode = @"
// A vector source of the same flag, inline so the example carries its own file: the packaged
// images are 16 pixels of raster, where a vector one stays sharp at any size at all.
private const string japanSvg = ""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 900 600'%3E%3Crect width='900' height='600' fill='%23fff'/%3E%3Ccircle cx='450' cy='300' r='180' fill='%23bc002d'/%3E%3C/svg%3E"";";

    private readonly string example7RazorCode = @"
<BitFlag Height=""2rem"" Country=""BitCountries.Brazil"" />

<BitFlag Height=""2rem"" Grayscale Country=""BitCountries.Brazil"" />

<BitFlag Height=""2rem"" IsEnabled=""false"" Country=""BitCountries.Brazil"" />

<BitFlag Height=""2rem"" Grayscale IsEnabled=""false"" Country=""BitCountries.Brazil"" />";

    private readonly string example8RazorCode = @"
@foreach (var country in clickableCountries)
{
    <BitFlag AutoAlt AutoTitle
             Bordered Rounded
             Height=""2rem""
             Country=""country""
             Grayscale=""@(selectedCountry != country)""
             OnClick=""@(() => selectedCountry = country)"" />
}

<div>Selected: @(selectedCountry?.Name ?? ""(none)"")</div>";

    private readonly string example8CsharpCode = @"
private static readonly BitCountry[] clickableCountries =
[
    BitCountries.France,
    BitCountries.Germany,
    BitCountries.Italy,
    BitCountries.Spain
];

private BitCountry? selectedCountry;";

    private readonly string example9RazorCode = @"
<div class=""flag-grid"">
    @foreach (var country in BitCountries.All)
    {
        <BitFlag Bordered Country=""country"" Title=""@($""{country.Name} - {country.Iso2}"")"" />
    }
</div>";

    private readonly string example10RazorCode = @"
<BitFlag Size=""BitSize.Small"" Bordered Country=""BitCountries.Italy"" />

<BitFlag Size=""BitSize.Medium"" Bordered Country=""BitCountries.Italy"" />

<BitFlag Size=""BitSize.Large"" Bordered Country=""BitCountries.Italy"" />

<BitFlag Height=""3rem"" Bordered Country=""BitCountries.Italy"" />

<BitFlag Width=""4rem"" Height=""2rem"" Bordered Country=""BitCountries.Italy"" />";

    private readonly string example11RazorCode = @"
<style>
    .custom-class {
        width: 3rem;
        height: 3rem;
        border-radius: 0.5rem;
        outline: 2px dashed mediumorchid;
        outline-offset: 2px;
    }

    .custom-root {
        border-radius: 0.5rem;
        background: linear-gradient(90deg, mediumorchid, dodgerblue);
    }

    .custom-fallback {
        color: white;
        font-weight: 600;
    }
</style>


<BitFlag Style=""width:3rem;height:2rem;border:2px solid dodgerblue"" Country=""BitCountries.Spain"" />

<BitFlag Class=""custom-class"" Country=""BitCountries.Spain"" />

<BitFlag Country=""BitCountries.Spain""
         Styles=""@(new() { Root = ""width:3rem;height:2rem"", Image = ""object-fit:contain"" })"" />

<BitFlag Height=""2rem"" Iso2=""zz"" Classes=""@(new() { Root = ""custom-root"", Fallback = ""custom-fallback"" })"">
    <FallbackTemplate>?</FallbackTemplate>
</BitFlag>";

    private readonly string example12RazorCode = @"
<div dir=""rtl"">
    <BitFlag Dir=""BitDir.Rtl"" Bordered Height=""2rem"" AutoTitle Country=""BitCountries.Iran"" />

    <BitFlag Dir=""BitDir.Rtl"" Bordered Height=""2rem"" AutoTitle Country=""BitCountries.SaudiArabia"" />

    <BitFlag Dir=""BitDir.Rtl"" Bordered Height=""2rem"" AutoTitle Country=""BitCountries.UnitedArabEmirates"" />
</div>";
}
