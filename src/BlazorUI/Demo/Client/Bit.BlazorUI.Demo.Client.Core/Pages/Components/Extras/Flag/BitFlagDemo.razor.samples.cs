namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Flag;

public partial class BitFlagDemo
{
    private readonly string example1RazorCode = @"
<BitFlag Country=""BitCountries.Iran"" />
<BitFlag Country=""BitCountries.Netherlands"" />
<BitFlag Country=""BitCountries.Japan"" />
<BitFlag Country=""BitCountries.Brazil"" />
<BitFlag Height=""2rem"" Country=""BitCountries.Brazil"" />";

    private readonly string example2RazorCode = @"
<BitFlag Iso2=""nl"" />

<BitFlag Iso3=""NLD"" />

<BitFlag Code=""+31"" />

<BitFlag Name=""Netherlands"" />

<BitFlag Name=""Holland"" />

<BitFlag Name=""Czechia"" />

<BitFlag Name=""Curaçao"" />

<BitFlag Iso2=""UK"" />

<BitFlag Code=""+1"" />";

    private readonly string example3RazorCode = @"
<BitFlag Rounded Height=""2rem"" Country=""BitCountries.Japan"" />

<BitFlag Circular Height=""2rem"" Country=""BitCountries.Japan"" />

<BitFlag Bordered Height=""2rem"" Country=""BitCountries.Japan"" />

<BitFlag Shadow Height=""2rem"" Country=""BitCountries.Japan"" />

<BitFlag Circular Bordered Shadow Height=""2rem"" Country=""BitCountries.Japan"" />

<BitFlag Grayscale Rounded Height=""2rem"" Country=""BitCountries.Brazil"" />";

    private readonly string example4RazorCode = @"
<BitFlag Emoji Country=""BitCountries.Iran"" />

<BitFlag Emoji Height=""1.5rem"" Country=""BitCountries.Netherlands"" />

<BitFlag Emoji Height=""2rem"" Country=""BitCountries.Japan"" />

<BitFlag Emoji Height=""3rem"" Country=""BitCountries.Brazil"" />

<BitFlag Emoji Height=""3rem"" Iso2=""GB-SCT"" />

<BitFlag Emoji Circular Bordered Height=""3rem"" Country=""BitCountries.Brazil"" />";

    private readonly string example5RazorCode = @"
<BitFlag Country=""BitCountries.Canada"" />

<BitFlag AutoAlt Country=""BitCountries.Canada"" />

<BitFlag Alt=""Ships to Canada"" Country=""BitCountries.Canada"" />

<BitFlag AutoAlt AutoTitle Country=""BitCountries.Canada"" />

<BitFlag Emoji AutoAlt Height=""1.5rem"" Country=""BitCountries.Canada"" />";

    private readonly string example6RazorCode = @"
<BitFlag Height=""3rem"" Country=""BitCountries.Brazil"" />

<BitFlag ImageSet=""BitFlagImageSet.Flat"" Height=""3rem"" Country=""BitCountries.Brazil"" />

<BitFlag ImageSet=""BitFlagImageSet.Shiny"" Height=""3rem"" Country=""BitCountries.Brazil"" />

<BitFlag ImageSet=""BitFlagImageSet.Flat"" ImageSize=""BitFlagImageSize.Size16"" Height=""3rem"" Country=""BitCountries.Brazil"" />

<BitFlag ImageSet=""BitFlagImageSet.Flat"" Rounded Bordered Height=""3rem"" Country=""BitCountries.Brazil"" />


<CascadingValue Value=""BitFlagImageSet.Shiny"">
    <BitFlag Height=""2rem"" Country=""BitCountries.Iran"" />
    <BitFlag Height=""2rem"" Country=""BitCountries.Netherlands"" />
    <BitFlag Height=""2rem"" Country=""BitCountries.Japan"" />
    <BitFlag Height=""2rem"" ImageSet=""BitFlagImageSet.Flat"" Country=""BitCountries.Brazil"" />
</CascadingValue>";

    private readonly string example7RazorCode = @"
<BitFlag Height=""2rem"" Src=""@japanSvg"" Country=""BitCountries.Japan"" />

<BitFlag Height=""2rem"" SrcPattern=""@flagCdnPattern"" Country=""BitCountries.Canada"" />
<BitFlag Height=""2rem"" SrcPattern=""@flagCdnPattern"" Country=""BitCountries.Germany"" />
<BitFlag Height=""2rem"" SrcPattern=""@flagCdnPattern"" Country=""BitCountries.Japan"" />

<BitFlag Height=""2rem"" AutoAlt SrcPattern=""@flagCdnPattern"" Country=""europeanUnion"" />

<BitFlag Height=""2rem"" Bordered Src=""/not-a-real-flag.png"" Country=""BitCountries.Japan"" />

<BitFlag Iso2=""zz"" />

<BitFlag Iso2=""zz"" Bordered Height=""1.5rem"">
    <FallbackTemplate>?</FallbackTemplate>
</BitFlag>

<BitFlag Height=""1.5rem"" Bordered Src=""/not-a-real-flag.png"">
    <FallbackTemplate><BitIcon IconName=""@BitIconName.Error"" /></FallbackTemplate>
</BitFlag>";

    private readonly string example7CsharpCode = @"
// A vector image of the flag, inline so the example carries its own file.
private const string japanSvg = ""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 900 600'%3E%3Crect width='900' height='600' fill='%23fff'/%3E%3Ccircle cx='450' cy='300' r='180' fill='%23bc002d'/%3E%3C/svg%3E"";

// One url for every country: {iso2} is written in as the lower-cased alpha-2 code.
private const string flagCdnPattern = ""https://flagcdn.com/{iso2}.svg"";

// A country the table does not carry: the European Union has an alpha-2 code but no alpha-3 one.
private static readonly BitCountry europeanUnion = new(""European Union"", """", ""EU"", """");";

    private readonly string example8RazorCode = @"
<BitFlag Bordered Height=""2rem"" Src=""@netherlandsSvg"" Country=""BitCountries.Netherlands"" />

<BitFlag Bordered Height=""2rem"" AspectRatio=""3/2"" Src=""@netherlandsSvg"" Country=""BitCountries.Netherlands"" />

<BitFlag Bordered Height=""2rem"" AspectRatio=""4/3"" Src=""@netherlandsSvg"" Country=""BitCountries.Netherlands"" />

<BitFlag Bordered Height=""2rem"" Fit=""BitImageFit.Contain"" Src=""@netherlandsSvg"" Country=""BitCountries.Netherlands"" />

<BitFlag Bordered Height=""2rem"" AspectRatio=""1"" Country=""BitCountries.Brazil"" />
<BitFlag Rounded Height=""2rem"" AspectRatio=""1"" ImageSet=""BitFlagImageSet.Shiny"" Country=""BitCountries.Brazil"" />
<BitFlag Bordered Height=""2rem"" AspectRatio=""2"" Country=""BitCountries.Brazil"" />";

    private readonly string example8CsharpCode = @"
// A 3:2 vector image, the shape the flag itself is drawn in.
private const string netherlandsSvg = ""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 900 600'%3E%3Crect width='900' height='600' fill='%231e4785'/%3E%3Crect width='900' height='400' fill='%23fff'/%3E%3Crect width='900' height='200' fill='%23ae1c28'/%3E%3C/svg%3E"";";

    private readonly string example9RazorCode = @"
<BitButton OnClick=""RenderEventFlags"">
    @(eventFlagsRenderCount == 0 ? ""Render the flags"" : ""Render the flags again"")
</BitButton>

@if (eventFlagsRenderCount > 0)
{
    <div @key=""eventFlagsRenderCount"">
        <BitFlag Bordered Height=""2rem""
                 Loading=""BitImageLoading.Eager""
                 Country=""BitCountries.Portugal""
                 ImageAttributes=""@(new() { { ""referrerpolicy"", ""no-referrer"" } })"" />

        <BitFlag Bordered Height=""2rem""
                 Country=""BitCountries.Portugal""
                 OnLoad=""@(() => loadedCount++)"" />
        <span>OnLoad - fired @loadedCount time(s)</span>

        <BitFlag Bordered Height=""2rem""
                 Src=""/not-a-real-flag.png""
                 Country=""BitCountries.Portugal""
                 OnError=""@(() => failedCount++)"" />
        <span>OnError - fired @failedCount time(s)</span>
    </div>
}";

    private readonly string example9CsharpCode = @"
private int eventFlagsRenderCount;
private int loadedCount;
private int failedCount;

private void RenderEventFlags()
{
    loadedCount = 0;
    failedCount = 0;
    eventFlagsRenderCount++;
}";

    private readonly string example10RazorCode = @"
@foreach (var country in clickableCountries)
{
    <BitFlag AutoAlt AutoTitle
             Bordered Rounded
             Height=""2rem""
             Country=""country""
             Grayscale=""@(selectedCountry != country)""
             aria-pressed=""@(selectedCountry == country ? ""true"" : ""false"")""
             OnClick=""@(() => selectedCountry = country)"" />
}

<BitFlag AutoAlt Bordered Rounded Grayscale
         Height=""2rem""
         IsEnabled=""false""
         Title=""Not shipped to Portugal yet""
         Country=""BitCountries.Portugal""
         OnClick=""@(() => selectedCountry = BitCountries.Portugal)"" />

<div>Selected: @(selectedCountry?.Name ?? ""(none)"")</div>";

    private readonly string example10CsharpCode = @"
private static readonly BitCountry[] clickableCountries =
[
    BitCountries.France,
    BitCountries.Germany,
    BitCountries.Italy,
    BitCountries.Spain
];

private BitCountry? selectedCountry;";

    private readonly string example11RazorCode = @"
<div class=""flag-grid"">
    @foreach (var country in BitCountries.All)
    {
        <BitFlag Bordered Country=""country"" Title=""@($""{country.Name} - {country.Iso2}"")"" />
    }
</div>";

    private readonly string example12RazorCode = @"
<BitParams Parameters=""flagParams"">
    <BitFlag Country=""BitCountries.Canada"" />
    <BitFlag Country=""BitCountries.Germany"" />
    <BitFlag Country=""BitCountries.Japan"" />
    <BitFlag Rounded=""false"" Country=""BitCountries.Brazil"" />
</BitParams>";

    private readonly string example12CsharpCode = @"
private readonly BitFlagParams[] flagParams =
[
    new()
    {
        Height = ""2rem"",
        Rounded = true,
        Bordered = true,
        AutoAlt = true,
        AutoTitle = true,
        ImageSet = BitFlagImageSet.Shiny,
    }
];";

    private readonly string example13RazorCode = @"
<BitFlag Size=""BitSize.Small"" Bordered Country=""BitCountries.Italy"" />

<BitFlag Size=""BitSize.Medium"" Bordered Country=""BitCountries.Italy"" />

<BitFlag Size=""BitSize.Large"" Bordered Country=""BitCountries.Italy"" />

<BitFlag Width=""4rem"" Height=""2rem"" Bordered Country=""BitCountries.Italy"" />";

    private readonly string example14RazorCode = @"
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
</BitFlag>

<BitFlag Rounded Bordered Height=""2rem"" Country=""BitCountries.Spain""
         Style=""--bit-Flag-radius: 0.75rem 0; --bit-Flag-border-width: 2px; --bit-Flag-border-color: crimson"" />

<div style=""--bit-Flag-size: 2rem; --bit-Flag-grayscale-filter: grayscale(1) opacity(0.4)"">
    <BitFlag Country=""BitCountries.Spain"" />
    <BitFlag Grayscale Country=""BitCountries.Portugal"" />
    <BitFlag Grayscale Country=""BitCountries.France"" />
</div>";

    private readonly string example15RazorCode = @"
<div dir=""rtl"">
    <BitFlag Dir=""BitDir.Rtl"" Bordered Height=""2rem"" AutoTitle Country=""BitCountries.Iran"" />

    <BitFlag Dir=""BitDir.Rtl"" Bordered Height=""2rem"" AutoTitle Country=""BitCountries.SaudiArabia"" />

    <BitFlag Dir=""BitDir.Rtl"" Bordered Height=""2rem"" AutoTitle Country=""BitCountries.UnitedArabEmirates"" />
</div>";
}
