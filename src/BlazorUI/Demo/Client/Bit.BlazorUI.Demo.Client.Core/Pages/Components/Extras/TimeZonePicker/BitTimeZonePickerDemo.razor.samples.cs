namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.TimeZonePicker;

public partial class BitTimeZonePickerDemo
{
    private readonly string example1RazorCode = @"
<BitTimeZonePicker TValue=""string"" Placeholder=""Select a time zone"" />";

    private readonly string example2RazorCode = @"
<BitTimeZonePicker TValue=""string"" Label=""Time zone"" Placeholder=""Select a time zone"" />

<BitTimeZonePicker TValue=""string"" Label=""Meeting time zone"" Required Placeholder=""Select a time zone"" />

<BitTimeZonePicker TValue=""string""
                   Label=""Display time zone""
                   Description=""Dates and times are shown in this time zone.""
                   Placeholder=""Select a time zone"" />";

    private readonly string example3RazorCode = @"
<BitTimeZonePicker Label=""Bound to the id"" Placeholder=""Select a time zone"" @bind-Value=""boundTimeZoneId"" />
<div>Id: @boundTimeZoneId</div>

<BitTimeZonePicker Label=""Bound to a TimeZoneInfo"" Placeholder=""Select a time zone"" @bind-Value=""boundTimeZone"" />
<div>Id: @boundTimeZone?.Id</div>
<div>Display name: @boundTimeZone?.DisplayName</div>
<div>Time there: @(boundTimeZone is null ? null : TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, boundTimeZone).ToString(""t""))</div>";
    private readonly string example3CsharpCode = @"
private string? boundTimeZoneId;
private TimeZoneInfo? boundTimeZone;";

    private readonly string example4RazorCode = @"
<BitTimeZonePicker Label=""DefaultValue""
                   DefaultValue=""@(""Europe/Paris"")""
                   OnChange=""(string? id) => changedTimeZoneId = id"" />
<div>Changed to: @changedTimeZoneId</div>

<BitTimeZonePicker Label=""A Windows id"" DefaultValue=""@(""Iran Standard Time"")"" />
<BitTimeZonePicker Label=""A renamed IANA id"" DefaultValue=""@(""Asia/Calcutta"")"" />

<BitTimeZonePicker Label=""DetectTimeZone"" DetectTimeZone @bind-Value=""detectedTimeZoneId"" />
<div>Detected: @detectedTimeZoneId</div>";
    private readonly string example4CsharpCode = @"
private string? changedTimeZoneId;
private string? detectedTimeZoneId;";

    private readonly string example5RazorCode = @"
<BitTimeZonePicker TValue=""string"" Label=""Europe only"" TimeZones=""europeanTimeZones"" Placeholder=""Select a time zone"" />

<BitTimeZonePicker TValue=""string"" Label=""Five offices"" TimeZones=""officeTimeZones"" Placeholder=""Select a time zone"" />";
    private readonly string example5CsharpCode = @"
private readonly List<BitTimeZone> europeanTimeZones = [.. BitTimeZones.All.Where(z => z.Id.StartsWith(""Europe/""))];

private readonly List<BitTimeZone> officeTimeZones =
[
    BitTimeZones.Find(""America/New_York"")!,
    BitTimeZones.Find(""Europe/London"")!,
    BitTimeZones.Find(""Europe/Berlin"")!,
    BitTimeZones.Find(""Asia/Tehran"")!,
    BitTimeZones.Find(""Asia/Singapore"")!,
];";

    private readonly string example6RazorCode = @"
<BitTimeZonePicker TValue=""string"" Label=""Time zone"" PreferredTimeZones=""preferredTimeZones"" Placeholder=""Select a time zone"" />";
    private readonly string example6CsharpCode = @"
private readonly List<string> preferredTimeZones = [""Europe/Berlin"", ""Asia/Tehran"", ""America/New_York""];";

    private readonly string example7RazorCode = @"
<BitTimeZonePicker TValue=""string""
                   Label=""Search box""
                   Placeholder=""Select a time zone""
                   SearchBoxPlaceholder=""City, country or offset""
                   NoResultsMessage=""No matching time zone"" />

<BitTimeZonePicker TValue=""string"" Label=""NoSearchBox"" NoSearchBox Placeholder=""Select a time zone"" />";

    private readonly string example8RazorCode = @"
<BitTimeZonePicker Label=""ReferenceDate (1 January)"" ReferenceDate=""januaryFirst"" DefaultValue=""@(""America/New_York"")"" />
<BitTimeZonePicker Label=""ReferenceDate (1 July)"" ReferenceDate=""julyFirst"" DefaultValue=""@(""America/New_York"")"" />

<BitTimeZonePicker Label=""NoOffset"" NoOffset DefaultValue=""@(""Asia/Tokyo"")"" />
<BitTimeZonePicker Label=""NoCountry"" NoCountry DefaultValue=""@(""Asia/Tokyo"")"" />";
    private readonly string example8CsharpCode = @"
private readonly DateTimeOffset januaryFirst = new(DateTimeOffset.UtcNow.Year, 1, 1, 12, 0, 0, TimeSpan.Zero);
private readonly DateTimeOffset julyFirst = new(DateTimeOffset.UtcNow.Year, 7, 1, 12, 0, 0, TimeSpan.Zero);";

    private readonly string example9RazorCode = @"
<BitTimeZonePicker Label=""Time zone"" DefaultValue=""@(""Australia/Sydney"")"">
    <ValueTemplate Context=""zone"">
        <span>@(zone is null ? ""Pick one"" : $""{zone.Name} - {LocalTime(zone)}"")</span>
    </ValueTemplate>
    <ItemTemplate Context=""zone"">
        <span style=""flex:1 1 0;overflow:hidden;text-overflow:ellipsis"">@zone.Name</span>
        <b>@LocalTime(zone)</b>
    </ItemTemplate>
    <NoResultsTemplate>
        <span>🔍 nothing here</span>
    </NoResultsTemplate>
</BitTimeZonePicker>";
    private readonly string example9CsharpCode = @"
// The time it is in a time zone right now, which is what a template can add to a row.
private static string LocalTime(BitTimeZone zone) => zone.TimeZoneInfo is { } timeZone
                                                        ? TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).ToString(""t"")
                                                        : string.Empty;";

    private readonly string example10RazorCode = @"
<BitTimeZonePicker Invalid Label=""Invalid"" DefaultValue=""@(""Europe/Madrid"")"" />

<BitTimeZonePicker Label=""Office time zone""
                   ErrorMessage=""We have no office there yet.""
                   Description=""Pick the time zone of one of our offices.""
                   DefaultValue=""@(""America/Sao_Paulo"")"" />

<EditForm Model=""validationModel"" OnValidSubmit=""() => submittedTimeZoneId = validationModel.TimeZoneId"" novalidate>
    <DataAnnotationsValidator />
    <BitTimeZonePicker Label=""Time zone""
                       Required
                       Placeholder=""Select a time zone""
                       @bind-Value=""validationModel.TimeZoneId"" />
    <ValidationMessage For=""() => validationModel.TimeZoneId"" />
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>

@if (submittedTimeZoneId is not null)
{
    <div>Submitted: @submittedTimeZoneId</div>
}";
    private readonly string example10CsharpCode = @"
public class BitTimeZonePickerValidationModel
{
    [Required(ErrorMessage = ""Select your time zone."")]
    public string? TimeZoneId { get; set; }
}

private string? submittedTimeZoneId;
private readonly BitTimeZonePickerValidationModel validationModel = new();";

    private readonly string example11RazorCode = @"
<BitTimeZonePicker Label=""Disabled"" Disabled DefaultValue=""@(""Europe/London"")"" />

<BitTimeZonePicker Label=""ReadOnly"" ReadOnly DefaultValue=""@(""Europe/London"")"" />";

    private readonly string example12RazorCode = @"
<BitTimeZonePicker TValue=""string"" NoBorder Placeholder=""NoBorder"" />
<BitTimeZonePicker TValue=""string"" Underlined Placeholder=""Underlined"" />

<BitTimeZonePicker TValue=""string"" Background=""BitColorKind.Primary"" Placeholder=""Background Primary"" />
<BitTimeZonePicker TValue=""string"" Background=""BitColorKind.Secondary"" Placeholder=""Background Secondary"" />
<BitTimeZonePicker TValue=""string"" Background=""BitColorKind.Tertiary"" Placeholder=""Background Tertiary"" />
<BitTimeZonePicker TValue=""string"" Background=""BitColorKind.Transparent"" Placeholder=""Background Transparent"" />
<BitTimeZonePicker TValue=""string"" Border=""BitColorKind.Tertiary"" Placeholder=""Border Tertiary"" />
<BitTimeZonePicker TValue=""string"" Border=""BitColorKind.Transparent"" Background=""BitColorKind.Secondary"" Placeholder=""Border Transparent"" />";

    private readonly string example13RazorCode = @"
<BitTimeZonePicker TValue=""string""
                   Label=""DropDirection All + MaxHeight 200""
                   MaxHeight=""200""
                   DropDirection=""BitDropDirection.All""
                   Placeholder=""Select a time zone"" />

<BitTimeZonePicker TValue=""string""
                   Label=""NoWrapNavigation (open it and hold the up arrow)""
                   NoWrapNavigation
                   NoSearchBox
                   Placeholder=""Select a time zone"" />

<BitTimeZonePicker TValue=""string"" Label=""Responsive"" Responsive Placeholder=""Select a time zone"" />";

    private readonly string example14RazorCode = @"
<BitTimeZonePicker TValue=""TimeZoneInfo""
                   Label=""Time zone""
                   Placeholder=""Select a time zone""
                   OnChange=""tz => eventTimeZone = tz""
                   OnSearch=""s => searchedText = s""
                   OnOpen='() => calloutState = ""opened""'
                   OnClose='() => calloutState = ""closed""'
                   OnFocus='() => focusState = ""focused""'
                   OnBlur='() => focusState = ""blurred""' />

<div>Selected time zone: @eventTimeZone?.Id</div>
<div>Search text: @searchedText</div>
<div>Callout: @calloutState</div>
<div>Field: @focusState</div>";
    private readonly string example14CsharpCode = @"
private TimeZoneInfo? eventTimeZone;
private string? searchedText;
private string calloutState = ""closed"";
private string focusState = ""blurred"";";

    private readonly string example15RazorCode = @"
<BitTimeZonePicker @ref=""controlledPicker""
                   TValue=""string""
                   @bind-IsOpen=""isCalloutOpen""
                   Label=""Time zone""
                   Placeholder=""Select a time zone"" />

<BitStack Horizontal Wrap>
    <BitButton OnClick=""() => isCalloutOpen = !isCalloutOpen"">Toggle the list</BitButton>
    <BitButton OnClick='() => controlledPicker.SelectTimeZoneAsync(""Asia/Tokyo"")'>Select Tokyo</BitButton>
    <BitButton OnClick=""() => controlledPicker.SelectTimeZoneAsync(TimeZoneInfo.Utc)"">Select UTC</BitButton>
    <BitButton OnClick=""async () => await controlledPicker.FocusAsync()"">Focus</BitButton>
</BitStack>

<div>IsOpen: @isCalloutOpen</div>
<div>SelectedTimeZone: @controlledPicker?.SelectedTimeZone?.Name</div>";
    private readonly string example15CsharpCode = @"
private bool isCalloutOpen;
private BitTimeZonePicker<string> controlledPicker = default!;";

    private readonly string example16RazorCode = @"
<BitTimeZonePicker Name=""timeZone""
                   Label=""Time zone""
                   Placeholder=""Select a time zone""
                   @bind-Value=""submissionTimeZone"" />

<div>Posted as timeZone: @(BitTimeZones.Find(submissionTimeZone)?.Id)</div>";
    private readonly string example16CsharpCode = @"
private TimeZoneInfo? submissionTimeZone;";

    private readonly string example17RazorCode = @"
<BitTimeZonePicker TValue=""string""
                   Label=""Zeitzone""
                   TimeZones=""germanTimeZones""
                   Placeholder=""Zeitzone auswählen""
                   SearchBoxPlaceholder=""Suchen""
                   SearchBoxAriaLabel=""Zeitzone suchen""
                   NoResultsMessage=""Keine Zeitzone gefunden""
                   SearchResultsAnnouncement=""@(count => count == 1 ? ""1 Zeitzone gefunden"" : $""{count} Zeitzonen gefunden"")"" />";
    private readonly string example17CsharpCode = @"
private readonly List<BitTimeZone> germanTimeZones =
[
    new(""Europe/Berlin"", ""Berlin"", ""DE"", ""W. Europe Standard Time"", ""Deutschland""),
    new(""Europe/Vienna"", ""Wien"", ""AT"", ""W. Europe Standard Time"", ""Österreich""),
    new(""Europe/Zurich"", ""Zürich"", ""CH"", ""W. Europe Standard Time"", ""Schweiz""),
    new(""Europe/Rome"", ""Rom"", ""IT"", ""W. Europe Standard Time"", ""Italien""),
    new(""Europe/Lisbon"", ""Lissabon"", ""PT"", ""GMT Standard Time"", ""Portugal""),
];";

    private readonly string example18RazorCode = @"
<BitParams Parameters=""@timeZonePickerParams"">
    <BitTimeZonePicker TValue=""string"" Label=""Takes the preferred time zones"" />
    <BitTimeZonePicker TValue=""TimeZoneInfo"" Label=""So does this one, bound to a TimeZoneInfo"" />
    <BitTimeZonePicker TValue=""string"" Label=""Its own Placeholder, the cascaded rest"" Placeholder=""Where are you?"" />
</BitParams>

<BitTimeZonePicker TValue=""string"" Label=""Outside the cascade"" />";
    private readonly string example18CsharpCode = @"
private readonly BitTimeZonePickerParams[] timeZonePickerParams =
[
    new()
    {
        PreferredTimeZones = [""Europe/Berlin"", ""Europe/Vienna"", ""Europe/Zurich""],
        Placeholder = ""Select a time zone"",
        NoCountry = true,
    }
];";

    private readonly string example19RazorCode = @"
<BitTimeZonePicker TValue=""string"" Color=""BitColor.Primary"" Placeholder=""Primary"" />
<BitTimeZonePicker TValue=""string"" Color=""BitColor.Secondary"" Placeholder=""Secondary"" />
<BitTimeZonePicker TValue=""string"" Color=""BitColor.Tertiary"" Placeholder=""Tertiary"" />
<BitTimeZonePicker TValue=""string"" Color=""BitColor.Info"" Placeholder=""Info"" />
<BitTimeZonePicker TValue=""string"" Color=""BitColor.Success"" Placeholder=""Success"" />
<BitTimeZonePicker TValue=""string"" Color=""BitColor.Warning"" Placeholder=""Warning"" />
<BitTimeZonePicker TValue=""string"" Color=""BitColor.SevereWarning"" Placeholder=""SevereWarning"" />
<BitTimeZonePicker TValue=""string"" Color=""BitColor.Error"" Placeholder=""Error"" />
<BitTimeZonePicker TValue=""string"" Color=""BitColor.PrimaryForeground"" Placeholder=""PrimaryForeground"" />
<BitTimeZonePicker TValue=""string"" Color=""BitColor.TertiaryBorder"" Placeholder=""TertiaryBorder"" />";

    private readonly string example20RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitTimeZonePicker TValue=""string""
                   Responsive
                   Label=""Time zone""
                   ResponsiveCloseIcon=""@BitIconInfo.Css(""fa-solid fa-xmark"")""
                   Placeholder=""Select a time zone"" />";

    private readonly string example21RazorCode = @"
<BitTimeZonePicker Size=""BitSize.Small"" DefaultValue=""@(""America/Chicago"")"" />
<BitTimeZonePicker Size=""BitSize.Medium"" DefaultValue=""@(""America/Chicago"")"" />
<BitTimeZonePicker Size=""BitSize.Large"" DefaultValue=""@(""America/Chicago"")"" />";

    private readonly string example22RazorCode = @"
<BitTimeZonePicker DefaultValue=""@(""Europe/Amsterdam"")""
                   Style=""--bit-TimeZonePicker-radius: 1.5rem;
                          --bit-TimeZonePicker-focus-color: #7c4dff;
                          --bit-TimeZonePicker-offset-color: #7c4dff;
                          --bit-TimeZonePicker-item-selected-background: #ede7f6;
                          --bit-TimeZonePicker-item-selected-color: #311b92;
                          --bit-TimeZonePicker-callout-radius: 1rem;"" />

<div class=""tzp-brand-scope"">
    <BitTimeZonePicker DefaultValue=""@(""Europe/Amsterdam"")"" />
</div>

<BitTimeZonePicker DefaultValue=""@(""Europe/Amsterdam"")""
                   Style=""width: 250px;""
                   Class=""custom-class""
                   Styles=""@(new() { Field = ""border-color: blueviolet;"", ItemName = ""color: blueviolet;"" })""
                   Classes=""@(new() { Text = ""custom-text"" })"" />";
    private const string example22ScssCode = @"
// The public custom properties are inherited, so one scope re-skins every picker inside it, and the
// time zone list each one opens, without a rule naming a part of the component.
.tzp-brand-scope {
    --bit-TimeZonePicker-radius: 0;
    --bit-TimeZonePicker-border-width: 2px;
    --bit-TimeZonePicker-border-color: #00796b;
    --bit-TimeZonePicker-hover-border-color: #26a69a;
    --bit-TimeZonePicker-focus-color: #00796b;
    --bit-TimeZonePicker-background: #e0f2f1;
    --bit-TimeZonePicker-color: #004d40;
    --bit-TimeZonePicker-callout-radius: 0;
    --bit-TimeZonePicker-item-selected-background: #e0f2f1;
    --bit-TimeZonePicker-item-selected-color: #004d40;
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
        new("BitTimeZonePickerDemo.razor.scss", example22ScssCode),
    ];

    private readonly string example23RazorCode = @"
<div dir=""rtl"">
    <BitTimeZonePicker TValue=""string"" Dir=""BitDir.Rtl"" TimeZones=""persianTimeZones"" Label=""منطقه زمانی"" Placeholder=""یک منطقه زمانی انتخاب کنید"" />
</div>";
    private readonly string example23CsharpCode = @"
private readonly List<BitTimeZone> persianTimeZones =
[
    new(""Asia/Tehran"", ""تهران"", ""IR"", ""Iran Standard Time"", ""ایران""),
    new(""Asia/Kabul"", ""کابل"", ""AF"", ""Afghanistan Standard Time"", ""افغانستان""),
    new(""Europe/Istanbul"", ""استانبول"", ""TR"", ""Turkey Standard Time"", ""ترکیه""),
    new(""Asia/Baghdad"", ""بغداد"", ""IQ"", ""Arabic Standard Time"", ""عراق""),
    new(""Asia/Dubai"", ""دبی"", ""AE"", ""Arabian Standard Time"", ""امارات""),
];";
}
