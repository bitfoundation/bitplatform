using System.Runtime.CompilerServices;

namespace Boilerplate.Workbench.Components.Pages.Showcase.Assistant.Data;

public partial class AssistantService
{
    [AutoInject] private WorkbenchScenarioState scenarioState = default!;
    [AutoInject] private IStringLocalizer<WorkbenchStrings> localizer = default!;

    public static IReadOnlyList<AssistantTopic> Topics { get; } =
    [
        new("sales", nameof(WorkbenchStrings.AssistantSalesPrompt), nameof(WorkbenchStrings.AssistantSalesAnswer), AssistantCardKind.SalesTrend, BitIconName.BarChartVerticalFill),
        new("stock", nameof(WorkbenchStrings.AssistantStockPrompt), nameof(WorkbenchStrings.AssistantStockAnswer), AssistantCardKind.LowStock, BitIconName.Package),
        new("promo", nameof(WorkbenchStrings.AssistantPromoPrompt), nameof(WorkbenchStrings.AssistantPromoAnswer), AssistantCardKind.PromoDraft, BitIconName.Megaphone),
        new("staff", nameof(WorkbenchStrings.AssistantStaffPrompt), nameof(WorkbenchStrings.AssistantStaffAnswer), AssistantCardKind.None, BitIconName.People)
    ];

    public static AssistantTopic Fallback { get; } = new("other", string.Empty, nameof(WorkbenchStrings.AssistantFallbackAnswer), AssistantCardKind.None, BitIconName.Help);

    public AssistantTopic Match(string prompt)
    {
        var text = prompt.Trim();

        return Topics.FirstOrDefault(topic => string.Equals(localizer[topic.PromptKey], text, StringComparison.CurrentCultureIgnoreCase)) ?? Fallback;
    }

    public Task<List<AssistantConversation>> GetConversations(CancellationToken cancellationToken) =>
        scenarioState.Load("assistant.conversations", CreateConversations, () => [], cancellationToken);

    public async IAsyncEnumerable<string> StreamAnswer(AssistantTopic topic, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var isSlow = scenarioState.Scenario.Data is WorkbenchDataState.Slow;
        var answer = localizer[topic.AnswerKey].Value;

        await Task.Delay(isSlow ? 1800 : 700, cancellationToken);

        for (var index = 0; index < answer.Length;)
        {
            var length = Math.Min(answer.Length - index, Random.Shared.Next(2, 7));

            yield return answer.Substring(index, length);

            index += length;

            await Task.Delay(isSlow ? Random.Shared.Next(40, 90) : Random.Shared.Next(10, 28), cancellationToken);
        }
    }

    private List<AssistantConversation> CreateConversations()
    {
        var now = DateTimeOffset.Now;

        return
        [
            CreateConversation("weekly", nameof(WorkbenchStrings.AssistantHistoryWeekly), now.AddMinutes(-12), Topics[0], isFailed: scenarioState.Scenario.Data is WorkbenchDataState.Mixed),
            CreateConversation("restock", nameof(WorkbenchStrings.AssistantHistoryRestock), now.AddDays(-1), Topics[1]),
            CreateConversation("autumn", nameof(WorkbenchStrings.AssistantHistoryAutumn), now.AddDays(-3), Topics[2]),
            CreateConversation("team", nameof(WorkbenchStrings.AssistantHistoryTeam), now.AddDays(-6), Topics[3])
        ];
    }

    private AssistantConversation CreateConversation(string id, string titleKey, DateTimeOffset at, AssistantTopic topic, bool isFailed = false) => new()
    {
        Id = id,
        Title = localizer[titleKey],
        UpdatedAt = at,
        Messages =
        [
            new() { Role = AssistantRole.User, At = at.AddMinutes(-1), Text = localizer[topic.PromptKey] },
            isFailed
                ? new() { Role = AssistantRole.Assistant, At = at, Topic = topic, IsFailed = true }
                : new() { Role = AssistantRole.Assistant, At = at, Topic = topic, Text = localizer[topic.AnswerKey], Card = topic.Card }
        ]
    };
}
