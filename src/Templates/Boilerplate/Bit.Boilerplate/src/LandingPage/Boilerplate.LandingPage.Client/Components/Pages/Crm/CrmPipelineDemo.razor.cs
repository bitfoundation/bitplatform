namespace Boilerplate.LandingPage.Client.Components.Pages.Crm;

public partial class CrmPipelineDemo
{
    private const decimal Target = 120_000;

    private static readonly (string Name, int Probability)[] stages =
    [
        (nameof(CrmStrings.DemoStageQualified), 10),
        (nameof(CrmStrings.DemoStageDemo), 30),
        (nameof(CrmStrings.DemoStageProposal), 50),
        (nameof(CrmStrings.DemoStageNegotiation), 75),
        (nameof(CrmStrings.DemoStageWon), 100)
    ];

    [AutoInject] private IStringLocalizer<CrmStrings> R = default!;

    private readonly List<Deal> deals =
    [
        new(1, "Halvorsen Marine", 18_000, 0, "AB", nameof(CrmStrings.DemoNext1)),
        new(2, "Okonkwo Logistics", 42_000, 1, "RK", nameof(CrmStrings.DemoNext2)),
        new(3, "Vauclair Bistros", 9_600, 1, "MS", nameof(CrmStrings.DemoNext3)),
        new(4, "Tanaka Tooling", 27_500, 2, "LN", nameof(CrmStrings.DemoNext4)),
        new(5, "Lindqvist Dental", 12_000, 2, "AB", nameof(CrmStrings.DemoNext5)),
        new(6, "Albera Hotels", 36_000, 3, "RK", nameof(CrmStrings.DemoNext6)),
        new(7, "Kestrel Analytics", 15_400, 4, "MS", nameof(CrmStrings.DemoNext7))
    ];

    private readonly Dictionary<int, ElementReference> cards = [];

    private int? focusAfterRender;
    private int? lastMoved;
    private string? status;

    private decimal Forecast => deals.Sum(d => d.Value * stages[d.Stage].Probability / 100m);

    private decimal Won => deals.Where(d => d.Stage == stages.Length - 1).Sum(d => d.Value);

    private double ForecastPercent => Math.Min(100, (double)(Forecast / Target * 100));

    private int OpenDeals => deals.Count(d => d.Stage < stages.Length - 1);

    private decimal Total(int stage) => deals.Where(d => d.Stage == stage).Sum(d => d.Value);

    private string Money(decimal value) => R[nameof(CrmStrings.DemoMoney), value.ToString("N0", CurrentCulture)];

    private string StageName(int stage) => R[stages[stage].Name];

    private string? MoveLabel(Deal deal, int step)
    {
        var target = deal.Stage + step;

        if (target < 0 || target >= stages.Length)
            return null;

        return R[step < 0 ? nameof(CrmStrings.DemoMoveBack) : nameof(CrmStrings.DemoMoveForward), deal.Company, StageName(target)];
    }

    private void Move(Deal deal, int step)
    {
        var target = deal.Stage + step;

        if (target < 0 || target >= stages.Length)
            return;

        var before = Forecast;

        deal.Stage = target;
        lastMoved = deal.Id;
        focusAfterRender = deal.Id;

        var change = Forecast - before;
        var signed = $"{(change >= 0 ? "+" : "−")}{Money(Math.Abs(change))}";

        status = target == stages.Length - 1
            ? R[nameof(CrmStrings.DemoStatusWon), deal.Company, Money(deal.Value)]
            : R[nameof(CrmStrings.DemoStatusMoved), deal.Company, StageName(target), signed];
    }

    private void OnCardKeyDown(KeyboardEventArgs args, Deal deal)
    {
        var forward = IsRtl ? "ArrowLeft" : "ArrowRight";
        var back = IsRtl ? "ArrowRight" : "ArrowLeft";

        if (args.Key == forward)
        {
            Move(deal, 1);
        }
        else if (args.Key == back)
        {
            Move(deal, -1);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (focusAfterRender is { } id && cards.TryGetValue(id, out var card))
        {
            focusAfterRender = null;
            await card.FocusAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private sealed class Deal(int id, string company, decimal value, int stage, string owner, string next)
    {
        public int Id { get; } = id;

        public string Company { get; } = company;

        public decimal Value { get; } = value;

        public int Stage { get; set; } = stage;

        public string Owner { get; } = owner;

        public string Next { get; } = next;
    }
}
