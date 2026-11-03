namespace Boilerplate.LandingPage.Client.Components.Pages.HotelSoftware;

public partial class HotelSoftwarePricingPage
{
    private const int YearlySavingPercent = 10;

    private static readonly HotelPlan[] plans =
    [
        new("essentials", nameof(HotelSoftwareStrings.PlanEssentials), nameof(HotelSoftwareStrings.PlanEssentialsSummary), 4, 39,
            [nameof(HotelSoftwareStrings.PlanEssentialsFeature1), nameof(HotelSoftwareStrings.PlanEssentialsFeature2), nameof(HotelSoftwareStrings.PlanEssentialsFeature3), nameof(HotelSoftwareStrings.PlanEssentialsFeature4)]),
        new("professional", nameof(HotelSoftwareStrings.PlanProfessional), nameof(HotelSoftwareStrings.PlanProfessionalSummary), 7, 79,
            [nameof(HotelSoftwareStrings.PlanProfessionalFeature1), nameof(HotelSoftwareStrings.PlanProfessionalFeature2), nameof(HotelSoftwareStrings.PlanProfessionalFeature3), nameof(HotelSoftwareStrings.PlanProfessionalFeature4)]),
        new("groups", nameof(HotelSoftwareStrings.PlanGroups), nameof(HotelSoftwareStrings.PlanGroupsSummary), 6, 160,
            [nameof(HotelSoftwareStrings.PlanGroupsFeature1), nameof(HotelSoftwareStrings.PlanGroupsFeature2), nameof(HotelSoftwareStrings.PlanGroupsFeature3), nameof(HotelSoftwareStrings.PlanGroupsFeature4)])
    ];

    private static readonly string[] enterprisePoints =
    [
        nameof(HotelSoftwareStrings.Enterprise1),
        nameof(HotelSoftwareStrings.Enterprise2),
        nameof(HotelSoftwareStrings.Enterprise3),
        nameof(HotelSoftwareStrings.Enterprise4)
    ];

    [AutoInject] private IStringLocalizer<HotelSoftwareStrings> H = default!;

    private double rooms = 24;
    private string? planKey = "professional";
    private BitChoiceGroupItem<string>[] planItems = [];
    private LandingFaq[] faq = [];

    private HotelPlan Plan => plans.FirstOrDefault(p => p.Key == planKey) ?? plans[1];

    private int Rooms => (int)rooms;

    private decimal ByRooms => Rooms * Plan.PerRoom;

    private bool MinimumApplies => ByRooms < Plan.Minimum;

    private decimal Monthly => Math.Max(ByRooms, Plan.Minimum);

    private decimal Yearly => Monthly * 12 * (100 - YearlySavingPercent) / 100;

    protected override void OnParametersSet()
    {
        planItems = [.. plans.Select(p => new BitChoiceGroupItem<string> { Value = p.Key, Text = H[p.Name] })];

        faq =
        [
            new(H[nameof(HotelSoftwareStrings.PricingFaq1Question)], H[nameof(HotelSoftwareStrings.PricingFaq1Answer)]),
            new(H[nameof(HotelSoftwareStrings.PricingFaq2Question)], H[nameof(HotelSoftwareStrings.PricingFaq2Answer)]),
            new(H[nameof(HotelSoftwareStrings.PricingFaq3Question)], H[nameof(HotelSoftwareStrings.PricingFaq3Answer)]),
            new(H[nameof(HotelSoftwareStrings.PricingFaq4Question)], H[nameof(HotelSoftwareStrings.PricingFaq4Answer)]),
            new(H[nameof(HotelSoftwareStrings.PricingFaq5Question)], H[nameof(HotelSoftwareStrings.PricingFaq5Answer)])
        ];

        base.OnParametersSet();
    }

    private string Money(decimal value) => H[nameof(HotelSoftwareStrings.MoneyEur), value.ToString("N0", CurrentCulture)];

    private string RoomsText(double value) => H[nameof(HotelSoftwareStrings.CalcRoomsValue), value.ToString("N0", CurrentCulture)];

    private sealed record HotelPlan(string Key, string Name, string Summary, decimal PerRoom, decimal Minimum, string[] Features);
}
