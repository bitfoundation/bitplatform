namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.CountryPicker;

public partial class BitCountryPickerDemo
{
    private readonly string example1RazorCode = @"
<BitCountryPicker Placeholder=""Select a country"" />";

    private readonly string example2RazorCode = @"
<BitCountryPicker Label=""Country"" Placeholder=""Select a country"" />

<BitCountryPicker Label=""Nationality"" Required Placeholder=""Select a country"" />

<BitCountryPicker Label=""Country of residence""
                  Description=""Where you have lived for the last six months.""
                  Placeholder=""Select a country"" />";

    private readonly string example3RazorCode = @"
<BitCountryPicker Label=""Country"" Placeholder=""Select a country"" @bind-Value=""boundCountry"" />

<div>Name: @boundCountry?.Name</div>
<div>Iso2: @boundCountry?.Iso2</div>
<div>Iso3: @boundCountry?.Iso3</div>
<div>Emoji: @boundCountry?.Emoji</div>";
    private readonly string example3CsharpCode = @"
private BitCountry? boundCountry;";

    private readonly string example4RazorCode = @"
<BitCountryPicker Label=""DefaultValue""
                  DefaultValue=""BitCountries.Canada""
                  OnChange=""c => changedCountry = c"" />
<div>Changed to: @changedCountry?.Name</div>

<BitCountryPicker Label=""DefaultCountry"" DefaultCountry=""BitCountries.Germany"" />";
    private readonly string example4CsharpCode = @"
private BitCountry? changedCountry;";

    private readonly string example5RazorCode = @"
<BitCountryPicker Label=""Countries (five markets)"" Countries=""customCountries"" Placeholder=""Select a country"" />

<BitCountryPicker Label=""ExcludeCountries (every country but these three)"" ExcludeCountries=""excludedCountries"" Placeholder=""Select a country"" />";
    private readonly string example5CsharpCode = @"
private readonly List<BitCountry> customCountries =
[
    BitCountries.UnitedStates,
    BitCountries.Canada,
    BitCountries.UnitedKingdom,
    BitCountries.Germany,
    BitCountries.France,
];

private readonly List<BitCountry> excludedCountries =
[
    BitCountries.UnitedStates,
    BitCountries.UnitedKingdom,
    BitCountries.Germany,
];";

    private readonly string example6RazorCode = @"
<BitCountryPicker Label=""Country"" PreferredCountries=""preferredCountries"" Placeholder=""Select a country"" />";
    private readonly string example6CsharpCode = @"
private readonly List<BitCountry> preferredCountries =
[
    BitCountries.Germany,
    BitCountries.France,
    BitCountries.UnitedStates,
];";

    private readonly string example7RazorCode = @"
<BitCountryPicker Label=""Search box""
                  Placeholder=""Select a country""
                  SearchBoxPlaceholder=""Country or ISO code""
                  NoResultsMessage=""No matching country"" />

<BitCountryPicker Label=""NoSearchBox"" NoSearchBox Placeholder=""Select a country"" />";

    private readonly string example8RazorCode = @"
<BitCountryPicker Label=""NoFlags"" NoFlags DefaultValue=""BitCountries.Japan"" />

<BitCountryPicker Label=""FlagUrlSelector"" FlagUrlSelector=""GetFlagUrl"" DefaultValue=""BitCountries.Switzerland"" />";
    private readonly string example8CsharpCode = @"
// Any url will do: this one asks a public flag CDN for a 24px png of the country.
private static string? GetFlagUrl(BitCountry country) => $""https://flagcdn.com/24x18/{country.Iso2.ToLowerInvariant()}.png"";";

    private readonly string example9RazorCode = @"
<BitCountryPicker Label=""Country"" DefaultValue=""BitCountries.Portugal"">
    <ValueTemplate Context=""country"">
        <span>@(country is null ? ""Pick one"" : $""{country.Emoji} {country.Name} ({country.Iso3})"")</span>
    </ValueTemplate>
    <ItemTemplate Context=""country"">
        <span>@country.Emoji</span>
        <span style=""flex:1 1 0;overflow:hidden;text-overflow:ellipsis"">@country.Name</span>
        <b>@country.Iso2</b>
    </ItemTemplate>
    <NoResultsTemplate>
        <span>🔍 nothing here</span>
    </NoResultsTemplate>
</BitCountryPicker>";

    private readonly string example10RazorCode = @"
<BitCountryPicker Invalid Label=""Invalid"" DefaultValue=""BitCountries.Spain"" />

<BitCountryPicker Label=""Shipping country""
                  ErrorMessage=""We do not ship there yet.""
                  Description=""We ship to the European Union only.""
                  DefaultValue=""BitCountries.Brazil"" />

<EditForm Model=""validationModel"" OnValidSubmit=""() => validationSubmitted = true"" novalidate>
    <DataAnnotationsValidator />
    <BitCountryPicker Label=""Nationality""
                      Required
                      Placeholder=""Select a country""
                      @bind-Value=""validationModel.Nationality"" />
    <ValidationMessage For=""() => validationModel.Nationality"" />
    <br />
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>

@if (validationSubmitted)
{
    <div>Submitted: @validationModel.Nationality?.Name</div>
}";
    private readonly string example10CsharpCode = @"
public class BitCountryPickerValidationModel
{
    [Required(ErrorMessage = ""Select your nationality."")]
    public BitCountry? Nationality { get; set; }
}

private bool validationSubmitted;
private readonly BitCountryPickerValidationModel validationModel = new();";

    private readonly string example11RazorCode = @"
<BitCountryPicker Label=""Disabled"" Disabled DefaultValue=""BitCountries.Spain"" />

<BitCountryPicker Label=""ReadOnly"" ReadOnly DefaultValue=""BitCountries.Spain"" />";

    private readonly string example12RazorCode = @"
<BitCountryPicker NoBorder Placeholder=""NoBorder"" />
<BitCountryPicker Underlined Placeholder=""Underlined"" />

<BitCountryPicker Background=""BitColorKind.Primary"" Placeholder=""Background Primary"" />
<BitCountryPicker Background=""BitColorKind.Secondary"" Placeholder=""Background Secondary"" />
<BitCountryPicker Background=""BitColorKind.Tertiary"" Placeholder=""Background Tertiary"" />
<BitCountryPicker Background=""BitColorKind.Transparent"" Placeholder=""Background Transparent"" />
<BitCountryPicker Border=""BitColorKind.Tertiary"" Placeholder=""Border Tertiary"" />
<BitCountryPicker Border=""BitColorKind.Transparent"" Background=""BitColorKind.Secondary"" Placeholder=""Border Transparent"" />";

    private readonly string example13RazorCode = @"
<BitCountryPicker Label=""DropDirection All + MaxHeight 200""
                  MaxHeight=""200""
                  DropDirection=""BitDropDirection.All""
                  Placeholder=""Select a country"" />

<BitCountryPicker Label=""NoWrapNavigation (open it and hold the up arrow)""
                  NoWrapNavigation
                  NoSearchBox
                  Placeholder=""Select a country"" />

<BitCountryPicker Label=""Responsive"" Responsive Placeholder=""Select a country"" />";

    private readonly string example14RazorCode = @"
<BitCountryPicker Label=""Country""
                  Placeholder=""Select a country""
                  OnChange=""c => eventCountry = c""
                  OnSearch=""s => searchedText = s""
                  OnOpen='() => calloutState = ""opened""'
                  OnClose='() => calloutState = ""closed""'
                  OnFocus='() => focusState = ""focused""'
                  OnBlur='() => focusState = ""blurred""' />

<div>Selected country: @eventCountry?.Name</div>
<div>Search text: @searchedText</div>
<div>Callout: @calloutState</div>
<div>Field: @focusState</div>";
    private readonly string example14CsharpCode = @"
private BitCountry? eventCountry;
private string? searchedText;
private string calloutState = ""closed"";
private string focusState = ""blurred"";";

    private readonly string example15RazorCode = @"
<BitCountryPicker @ref=""controlledPicker""
                  @bind-IsOpen=""isCalloutOpen""
                  Label=""Country""
                  Placeholder=""Select a country"" />

<BitStack Horizontal Wrap>
    <BitButton OnClick=""() => isCalloutOpen = !isCalloutOpen"">Toggle the list</BitButton>
    <BitButton OnClick=""() => controlledPicker.SelectCountryAsync(BitCountries.Japan)"">Select Japan</BitButton>
    <BitButton OnClick=""async () => await controlledPicker.FocusAsync()"">Focus</BitButton>
</BitStack>

<div>IsOpen: @isCalloutOpen</div>";
    private readonly string example15CsharpCode = @"
private bool isCalloutOpen;
private BitCountryPicker controlledPicker = default!;";

    private readonly string example16RazorCode = @"
<BitCountryPicker Name=""country""
                  Label=""Billing country""
                  Placeholder=""Select a country""
                  @bind-Value=""submissionCountry"" />

<div>Posted as country: @submissionCountry?.Iso2</div>";
    private readonly string example16CsharpCode = @"
private BitCountry? submissionCountry;";

    private readonly string example17RazorCode = @"
<BitCountryPicker Label=""Land""
                  Countries=""germanCountries""
                  Placeholder=""Land auswählen""
                  SearchBoxPlaceholder=""Suchen""
                  SearchBoxAriaLabel=""Land suchen""
                  NoResultsMessage=""Kein Land gefunden""
                  SearchResultsAnnouncement=""@(count => count == 1 ? ""1 Land gefunden"" : $""{count} Länder gefunden"")"" />";
    private readonly string example17CsharpCode = @"
private readonly List<BitCountry> germanCountries =
[
    new(""Deutschland"", ""49"", ""DE"", ""DEU""),
    new(""Frankreich"", ""33"", ""FR"", ""FRA""),
    new(""Italien"", ""39"", ""IT"", ""ITA""),
    new(""Österreich"", ""43"", ""AT"", ""AUT""),
    new(""Schweiz"", ""41"", ""CH"", ""CHE""),
];";

    private readonly string example18RazorCode = @"
<BitParams Parameters=""@countryPickerParams"">
    <BitCountryPicker Label=""Takes the market and the list order"" />
    <BitCountryPicker Label=""So does this one"" />
    <BitCountryPicker Label=""Its own DefaultCountry, the cascaded rest"" DefaultCountry=""BitCountries.France"" />
</BitParams>

<BitCountryPicker Label=""Outside the cascade"" />";
    private readonly string example18CsharpCode = @"
private readonly BitCountryPickerParams[] countryPickerParams =
[
    new()
    {
        DefaultCountry = BitCountries.Germany,
        PreferredCountries = [BitCountries.Germany, BitCountries.Austria, BitCountries.Switzerland],
        Placeholder = ""Select a country"",
    }
];";

    private readonly string example19RazorCode = @"
<BitCountryPicker Color=""BitColor.Primary"" Placeholder=""Primary"" />
<BitCountryPicker Color=""BitColor.Secondary"" Placeholder=""Secondary"" />
<BitCountryPicker Color=""BitColor.Tertiary"" Placeholder=""Tertiary"" />
<BitCountryPicker Color=""BitColor.Info"" Placeholder=""Info"" />
<BitCountryPicker Color=""BitColor.Success"" Placeholder=""Success"" />
<BitCountryPicker Color=""BitColor.Warning"" Placeholder=""Warning"" />
<BitCountryPicker Color=""BitColor.SevereWarning"" Placeholder=""SevereWarning"" />
<BitCountryPicker Color=""BitColor.Error"" Placeholder=""Error"" />
<BitCountryPicker Color=""BitColor.PrimaryForeground"" Placeholder=""PrimaryForeground"" />
<BitCountryPicker Color=""BitColor.TertiaryBorder"" Placeholder=""TertiaryBorder"" />";

    private readonly string example20RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitCountryPicker Responsive
                  Label=""Country""
                  ResponsiveCloseIcon=""@BitIconInfo.Css(""fa-solid fa-xmark"")""
                  Placeholder=""Select a country"" />";

    private readonly string example21RazorCode = @"
<BitCountryPicker Size=""BitSize.Small"" DefaultValue=""BitCountries.UnitedStates"" />
<BitCountryPicker Size=""BitSize.Medium"" DefaultValue=""BitCountries.UnitedStates"" />
<BitCountryPicker Size=""BitSize.Large"" DefaultValue=""BitCountries.UnitedStates"" />";

    private readonly string example22RazorCode = @"
<BitCountryPicker DefaultValue=""BitCountries.Netherlands""
                  Style=""--bit-CountryPicker-radius: 1.5rem;
                         --bit-CountryPicker-focus-color: #7c4dff;
                         --bit-CountryPicker-item-selected-background: #ede7f6;
                         --bit-CountryPicker-item-selected-color: #311b92;
                         --bit-CountryPicker-callout-radius: 1rem;"" />

<div class=""cnp-brand-scope"">
    <BitCountryPicker DefaultValue=""BitCountries.Netherlands"" />
</div>

<BitCountryPicker DefaultValue=""BitCountries.Netherlands""
                  Style=""width: 250px;""
                  Class=""custom-class""
                  Styles=""@(new() { Field = ""border-color: blueviolet;"", ItemName = ""color: blueviolet;"" })""
                  Classes=""@(new() { Text = ""custom-text"" })"" />";
    private const string example22ScssCode = @"
// The public custom properties are inherited, so one scope re-skins every picker inside it, and the
// country list each one opens, without a rule naming a part of the component.
.cnp-brand-scope {
    --bit-CountryPicker-radius: 0;
    --bit-CountryPicker-border-width: 2px;
    --bit-CountryPicker-border-color: #00796b;
    --bit-CountryPicker-hover-border-color: #26a69a;
    --bit-CountryPicker-focus-color: #00796b;
    --bit-CountryPicker-background: #e0f2f1;
    --bit-CountryPicker-color: #004d40;
    --bit-CountryPicker-callout-radius: 0;
    --bit-CountryPicker-item-selected-background: #e0f2f1;
    --bit-CountryPicker-item-selected-color: #004d40;
}

::deep .custom-class {
    border-radius: 1rem;
    background: linear-gradient(90deg, rgba(138, 43, 226, 0.08), transparent);
}

::deep .custom-text {
    color: blueviolet;
    font-weight: bold;
}";
    private readonly DemoCodeFile[] example22CodeFiles =
    [
        new("BitCountryPickerDemo.razor.scss", example22ScssCode),
    ];

    private readonly string example23RazorCode = @"
<div dir=""rtl"">
    <BitCountryPicker Dir=""BitDir.Rtl"" Countries=""persianCountries"" Label=""کشور"" Placeholder=""یک کشور انتخاب کنید"" />
</div>";
    private readonly string example23CsharpCode = @"
private readonly List<BitCountry> persianCountries =
[
    new(""ایران"", ""98"", ""IR"", ""IRN""),
    new(""افغانستان"", ""93"", ""AF"", ""AFG""),
    new(""ترکیه"", ""90"", ""TR"", ""TUR""),
    new(""عراق"", ""964"", ""IQ"", ""IRQ""),
    new(""عمان"", ""968"", ""OM"", ""OMN""),
];";
}
