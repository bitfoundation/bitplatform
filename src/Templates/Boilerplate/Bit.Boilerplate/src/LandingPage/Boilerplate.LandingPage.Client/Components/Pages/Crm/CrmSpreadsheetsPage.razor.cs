namespace Boilerplate.LandingPage.Client.Components.Pages.Crm;

public partial class CrmSpreadsheetsPage
{
    private const string Sheet = "sheet";
    private const string Us = "us";

    private static readonly (string Company, int? Amount, string Stage, string? Next, string? Flag)[] sheetRows =
    [
        ("Okonkwo", 42_000, nameof(CrmStrings.DemoStageDemo), null, null),
        ("Tanaka", 27_500, nameof(CrmStrings.DemoStageProposal), nameof(CrmStrings.SheetsArtFriday), null),
        ("Albera", 36_000, nameof(CrmStrings.DemoStageWon), null, "conflict"),
        ("Lindqvist", null, nameof(CrmStrings.DemoStageProposal), null, "ref")
    ];

    private static readonly (string Title, string Body)[] sheetWins =
    [
        (nameof(CrmStrings.Win1Title), nameof(CrmStrings.Win1Body)),
        (nameof(CrmStrings.Win2Title), nameof(CrmStrings.Win2Body)),
        (nameof(CrmStrings.Win3Title), nameof(CrmStrings.Win3Body)),
        (nameof(CrmStrings.Win4Title), nameof(CrmStrings.Win4Body))
    ];

    private static readonly (string Title, string Body)[] sheetCosts =
    [
        (nameof(CrmStrings.Cost1Title), nameof(CrmStrings.Cost1Body)),
        (nameof(CrmStrings.Cost2Title), nameof(CrmStrings.Cost2Body)),
        (nameof(CrmStrings.Cost3Title), nameof(CrmStrings.Cost3Body)),
        (nameof(CrmStrings.Cost4Title), nameof(CrmStrings.Cost4Body)),
        (nameof(CrmStrings.Cost5Title), nameof(CrmStrings.Cost5Body))
    ];

    private static readonly (string Row, string Sheet, string Us, string? Winner)[] sideBySide =
    [
        (nameof(CrmStrings.Side1), nameof(CrmStrings.Side1Sheet), nameof(CrmStrings.Side1Us), Sheet),
        (nameof(CrmStrings.Side2), nameof(CrmStrings.Side2Sheet), nameof(CrmStrings.Side2Us), Sheet),
        (nameof(CrmStrings.Side3), nameof(CrmStrings.Side3Sheet), nameof(CrmStrings.Side3Us), Sheet),
        (nameof(CrmStrings.Side4), nameof(CrmStrings.Side4Sheet), nameof(CrmStrings.Side4Us), Us),
        (nameof(CrmStrings.Side5), nameof(CrmStrings.Side5Sheet), nameof(CrmStrings.Side5Us), Us),
        (nameof(CrmStrings.Side6), nameof(CrmStrings.Side6Sheet), nameof(CrmStrings.Side6Us), Us),
        (nameof(CrmStrings.Side7), nameof(CrmStrings.Side7Sheet), nameof(CrmStrings.Side7Us), Us),
        (nameof(CrmStrings.Side8), nameof(CrmStrings.Side8Sheet), nameof(CrmStrings.Side8Us), Us),
        (nameof(CrmStrings.Side9), nameof(CrmStrings.Side9Sheet), nameof(CrmStrings.Side9Us), null),
        (nameof(CrmStrings.Side10), nameof(CrmStrings.Side10Sheet), nameof(CrmStrings.Side10Us), Sheet)
    ];

    private static readonly string[] signs =
    [
        nameof(CrmStrings.Sign1),
        nameof(CrmStrings.Sign2),
        nameof(CrmStrings.Sign3),
        nameof(CrmStrings.Sign4)
    ];

    private static readonly (string Title, string Body)[] moveSteps =
    [
        (nameof(CrmStrings.Move1Title), nameof(CrmStrings.Move1Body)),
        (nameof(CrmStrings.Move2Title), nameof(CrmStrings.Move2Body)),
        (nameof(CrmStrings.Move3Title), nameof(CrmStrings.Move3Body))
    ];

    private static readonly (string From, string To, bool IsGuess)[] mapping =
    [
        (nameof(CrmStrings.MapFrom1), nameof(CrmStrings.MapTo1), true),
        (nameof(CrmStrings.MapFrom2), nameof(CrmStrings.MapTo2), true),
        (nameof(CrmStrings.MapFrom3), nameof(CrmStrings.MapTo3), true),
        (nameof(CrmStrings.MapFrom4), nameof(CrmStrings.MapTo4), false),
        (nameof(CrmStrings.MapFrom5), nameof(CrmStrings.MapTo5), true)
    ];

    [AutoInject] private IStringLocalizer<CrmStrings> R = default!;

    private readonly bool[] checkedSigns = new bool[signs.Length];

    private int CheckedCount => checkedSigns.Count(c => c);

    private bool IsTime => CheckedCount >= 2;

    private void Toggle(int index, bool value) => checkedSigns[index] = value;
}
