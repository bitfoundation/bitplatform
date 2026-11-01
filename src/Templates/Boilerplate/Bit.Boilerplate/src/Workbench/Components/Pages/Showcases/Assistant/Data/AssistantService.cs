using System.Runtime.CompilerServices;

namespace Boilerplate.Workbench.Components.Pages.Showcases.Assistant.Data;

/// <summary>
/// The assistant's stand-in model: an <c>IBitAiTransport</c> that streams scripted replies per topic, with reasoning,
/// tool calls, an approval, generative UI cards and a document, plus the sample tools those replies call.
/// </summary>
public partial class AssistantService : IBitAiTransport
{
    public const string DefaultModelId = "thorough";

    private const string QuickModelId = "quick";
    private const string SalesTool = "get_sales";
    private const string StockTool = "check_stock";
    private const string TeamTool = "get_team_sales";
    private const string OrderTool = "place_order";
    private const decimal WeekTotal = 34_820m;
    private const decimal WeekChange = 0.084m;

    private static readonly double[] dailySales = [4380, 4710, 4520, 4960, 5340, 6120, 4790];

    // Each product's name key, the units left and the quantity to order.
    private static readonly (string NameKey, int Left, int Order)[] stockLevels =
    [
        (nameof(WorkbenchStrings.AssistantStockOatMilk), 4, 24),
        (nameof(WorkbenchStrings.AssistantStockCroissants), 6, 60),
        (nameof(WorkbenchStrings.AssistantStockPaperCups), 1, 20)
    ];

    [AutoInject] private WorkbenchScenarioState scenarioState = default!;
    [AutoInject] private IStringLocalizer<WorkbenchStrings> localizer = default!;

    private BitAiModel[]? models;
    private BitAiToolRegistry? tools;

    /// <summary>The topics the script answers, each matched by the exact text of its suggestion.</summary>
    public static IReadOnlyList<AssistantTopic> Topics { get; } =
    [
        new("sales", nameof(WorkbenchStrings.AssistantSalesPrompt), nameof(WorkbenchStrings.AssistantSalesReasoning)),
        new("stock", nameof(WorkbenchStrings.AssistantStockPrompt), nameof(WorkbenchStrings.AssistantStockReasoning)),
        new("promo", nameof(WorkbenchStrings.AssistantPromoPrompt), nameof(WorkbenchStrings.AssistantPromoReasoning)),
        new("staff", nameof(WorkbenchStrings.AssistantStaffPrompt), nameof(WorkbenchStrings.AssistantStaffReasoning))
    ];

    /// <summary>The topic of a prompt that matches no suggestion.</summary>
    public static AssistantTopic Fallback { get; } = new("other", string.Empty, null);

    /// <summary>The two stand-in models; only the thorough one shows its reasoning.</summary>
    public IReadOnlyList<BitAiModel> Models => models ??=
    [
        new(DefaultModelId, localizer[nameof(WorkbenchStrings.AssistantModelThorough)])
        {
            Description = localizer[nameof(WorkbenchStrings.AssistantModelThoroughDescription)],
            Icon = BitIconName.Lightbulb,
            ContextWindow = 32_000,
            Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Tools | BitAiModelCapabilities.Vision | BitAiModelCapabilities.Documents | BitAiModelCapabilities.Citations | BitAiModelCapabilities.Reasoning
        },
        new(QuickModelId, localizer[nameof(WorkbenchStrings.AssistantModelQuick)])
        {
            Description = localizer[nameof(WorkbenchStrings.AssistantModelQuickDescription)],
            Icon = BitIconName.LightningBolt,
            ContextWindow = 16_000,
            Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Tools | BitAiModelCapabilities.Vision | BitAiModelCapabilities.Documents | BitAiModelCapabilities.Citations
        }
    ];

    private bool IsSlow => scenarioState.Scenario.Data is WorkbenchDataState.Slow;

    /// <summary>The sample tools the replies call; placing an order writes, so it waits for the user's approval.</summary>
    private BitAiToolRegistry Tools => tools ??= new(
    [
        CreateTool(SalesTool, nameof(WorkbenchStrings.AssistantToolSales), BitIconName.BarChartVerticalFill),
        CreateTool(StockTool, nameof(WorkbenchStrings.AssistantToolStock), BitIconName.Package),
        CreateTool(TeamTool, nameof(WorkbenchStrings.AssistantToolTeam), BitIconName.People),
        CreateTool(OrderTool, nameof(WorkbenchStrings.AssistantToolOrder), BitIconName.DeliveryTruck, BitAiToolRisk.Write)
    ]);

    /// <summary>The model with that id, or the first one.</summary>
    public BitAiModel FindModel(string? modelId) => Models.FirstOrDefault(m => m.ModelId == modelId) ?? Models[0];

    /// <summary>The topic whose suggestion is the prompt, ignoring case and surrounding spaces.</summary>
    public AssistantTopic Match(string prompt)
    {
        var text = prompt.Trim();

        return Topics.FirstOrDefault(topic => string.Equals(localizer[topic.PromptKey], text, StringComparison.CurrentCultureIgnoreCase)) ?? Fallback;
    }

    /// <summary>The sample history, loaded through the scenario state so the data-state switches reach it.</summary>
    public Task<List<BitAiConversationSnapshot>> GetConversations(CancellationToken cancellationToken) =>
        scenarioState.Load("assistant.conversations", CreateConversations, () => [], cancellationToken);

    /// <summary>A session on this transport and the sample tools, unpaced because <c>StreamAsync</c> paces itself.</summary>
    public BitAiChatSession CreateSession(BitAiConversation conversation, string modelId, BitAiStrings strings) =>
        new(this, conversation, new() { RequestOptions = new() { ModelId = modelId }, Strings = strings, Pacing = null }, Tools);

    /// <summary>Streams the scripted reply to the latest user message in small pieces with short pauses, like a live model.</summary>
    public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // The reply answers the latest user message; the tool calls after it tell how far its script has got.
        var start = request.Messages.Count - 1;

        while (start >= 0 && request.Messages[start].Role.IsUser is false)
        {
            start--;
        }

        var calls = request.Messages.Skip(start + 1).SelectMany(m => m.Parts.OfType<BitAiToolCallPart>()).ToArray();
        // An order approved with "always" earlier in the chat isn't asked about again.
        var askApproval = request.Messages.SelectMany(m => m.Parts.OfType<BitAiToolCallPart>()).Any(c => c.ToolName == OrderTool && c.Approval is BitAiApprovalDecision.ApprovedAlways) is false;
        var topic = Match(start < 0 ? string.Empty : request.Messages[start].GetText());

        await Task.Delay(calls.Length is 0 ? (IsSlow ? 1800 : 700) : (IsSlow ? 900 : 300), cancellationToken);

        foreach (var evt in Reply(topic, FindModel(request.Options?.ModelId), calls, ContextTokens(request.Messages), askApproval))
        {
            switch (evt)
            {
                case BitAiReasoningDeltaEvent reasoning:
                    foreach (var piece in Slice(reasoning.Delta))
                    {
                        yield return new BitAiReasoningDeltaEvent(piece);
                        await Task.Delay(IsSlow ? Random.Shared.Next(60, 120) : Random.Shared.Next(25, 50), cancellationToken);
                    }
                    await Task.Delay(IsSlow ? 1600 : 900, cancellationToken);
                    break;
                case BitAiTextDeltaEvent text:
                    foreach (var piece in Slice(text.Delta))
                    {
                        yield return new BitAiTextDeltaEvent(piece);
                        await Task.Delay(IsSlow ? Random.Shared.Next(40, 90) : Random.Shared.Next(10, 28), cancellationToken);
                    }
                    break;
                case BitAiArtifactDeltaEvent artifact:
                    foreach (var piece in Slice(artifact.Delta))
                    {
                        yield return new BitAiArtifactDeltaEvent(artifact.ArtifactId, piece);
                        await Task.Delay(IsSlow ? Random.Shared.Next(40, 90) : Random.Shared.Next(10, 28), cancellationToken);
                    }
                    break;
                default:
                    yield return evt;
                    break;
            }
        }
    }

    /// <summary>The events of one reply, from its topic and the tool calls already answered in this turn.</summary>
    private List<BitAiEvent> Reply(AssistantTopic topic, BitAiModel model, IReadOnlyList<BitAiToolCallPart> calls, int contextTokens, bool askApproval)
    {
        List<BitAiEvent> events = [new BitAiMessageStartedEvent()];
        var finishReason = BitAiFinishReason.Stop;

        if (calls.Count is 0)
        {
            events.Add(new BitAiMetadataEvent(new() { ModelId = model.ModelId, ContextWindow = model.ContextWindow }));

            if (topic.ReasoningKey is not null && model.Supports(BitAiModelCapabilities.Reasoning))
            {
                events.Add(new BitAiReasoningDeltaEvent(localizer[topic.ReasoningKey]));
            }
        }

        switch (topic.Key)
        {
            case "sales" when calls.Count is 0:
                events.Add(CallTool(SalesTool, """{"period":"last_7_days","stores":["Amsterdam","Rotterdam","Utrecht"]}"""));
                finishReason = BitAiFinishReason.ToolCalls;
                break;
            case "sales":
                events.Add(new BitAiTextDeltaEvent(localizer[nameof(WorkbenchStrings.AssistantSalesAnswer)]));
                events.Add(new BitAiGenerativeUiEvent("sales_trend", ToJson(new { total = WeekTotal, change = WeekChange, days = dailySales })));
                break;
            case "stock" when calls.Count is 0:
                events.Add(CallTool(StockTool, """{"until":"next_delivery"}"""));
                finishReason = BitAiFinishReason.ToolCalls;
                break;
            case "stock" when calls.Count is 1:
                var order = CallTool(OrderTool, """{"items":[{"product":"oat_milk","quantity":24},{"product":"croissants","quantity":60},{"product":"paper_cups","quantity":20}],"delivery":"friday"}""");
                events.Add(new BitAiTextDeltaEvent(localizer[nameof(WorkbenchStrings.AssistantStockAnswer)]));
                events.Add(new BitAiGenerativeUiEvent("low_stock", ToJson(new { items = StockItems() })));
                events.Add(order);
                if (askApproval)
                {
                    events.Add(new BitAiToolApprovalRequestedEvent(order.ToolCallId) { Reason = localizer[nameof(WorkbenchStrings.AssistantOrderReason)] });
                }
                finishReason = BitAiFinishReason.ToolCalls;
                break;
            case "stock":
                events.Add(new BitAiTextDeltaEvent(localizer[calls[^1].Status is BitAiToolCallStatus.Completed ? nameof(WorkbenchStrings.AssistantOrderPlaced) : nameof(WorkbenchStrings.AssistantOrderSkipped)]));
                break;
            case "promo":
                events.Add(new BitAiTextDeltaEvent(localizer[nameof(WorkbenchStrings.AssistantPromoAnswer)]));
                events.Add(new BitAiArtifactCreatedEvent("promo", BitAiArtifactKind.Document, localizer[nameof(WorkbenchStrings.AssistantPromoTitle)]));
                events.Add(new BitAiArtifactDeltaEvent("promo", localizer[nameof(WorkbenchStrings.AssistantPromoDraft)]));
                events.Add(new BitAiArtifactCompletedEvent("promo"));
                break;
            case "staff" when calls.Count is 0:
                events.Add(CallTool(TeamTool, """{"date":"today"}"""));
                finishReason = BitAiFinishReason.ToolCalls;
                break;
            case "staff":
                events.Add(new BitAiTextDeltaEvent(localizer[nameof(WorkbenchStrings.AssistantStaffAnswer)]));
                break;
            default:
                events.Add(new BitAiTextDeltaEvent(localizer[nameof(WorkbenchStrings.AssistantFallbackAnswer)]));
                break;
        }

        // Usage follows what was written, at about four characters a token.
        var reasoningLength = events.OfType<BitAiReasoningDeltaEvent>().Sum(e => e.Delta.Length);
        var writtenLength = events.Sum(e => e switch
        {
            BitAiReasoningDeltaEvent reasoning => reasoning.Delta.Length,
            BitAiTextDeltaEvent text => text.Delta.Length,
            BitAiArtifactDeltaEvent artifact => artifact.Delta.Length,
            BitAiToolCallStartedEvent call => call.Arguments?.GetRawText().Length ?? 0,
            _ => 0
        });

        events.Add(new BitAiUsageUpdatedEvent(new() { InputTokens = 1_850 + contextTokens, OutputTokens = writtenLength / 4, ReasoningTokens = reasoningLength > 0 ? reasoningLength / 4 : null }));
        events.Add(new BitAiMessageCompletedEvent(finishReason));

        return events;
    }

    /// <summary>The sample result of a tool; any other tool is the order being placed.</summary>
    private BitAiToolOutcome RunTool(string toolName) => toolName switch
    {
        SalesTool => Found(new
        {
            net = WeekTotal,
            change = WeekChange,
            stores = new[]
            {
                new { store = "Amsterdam", net = 15_940m, change = 0.071m },
                new { store = "Rotterdam", net = 10_230m, change = 0.121m },
                new { store = "Utrecht", net = 8_650m, change = 0.003m }
            },
            days = dailySales
        }, "sales", nameof(WorkbenchStrings.AssistantSalesCardTitle), nameof(WorkbenchStrings.AssistantToolSales)),
        StockTool => Found(new { nextDelivery = NextDate(DayOfWeek.Monday), items = StockItems() }, "stock", nameof(WorkbenchStrings.AssistantSourceStock), nameof(WorkbenchStrings.AssistantToolStock)),
        TeamTool => Found(new
        {
            date = DateOnly.FromDateTime(DateTime.Today),
            team = new[]
            {
                new { name = "Daan", orders = 64, sales = 1_177.60m },
                new { name = "Sarah", orders = 58, sales = 1_032.40m },
                new { name = "Niloofar", orders = 51, sales = 913.00m }
            }
        }, "team", nameof(WorkbenchStrings.AssistantSourceTeam), nameof(WorkbenchStrings.AssistantToolTeam)),
        _ => BitAiToolOutcome.Success(ToJson(new { order = "PO-1042", lines = stockLevels.Length, arrives = NextDate(DayOfWeek.Friday) }))
    };

    /// <summary>A successful result that cites the dataset it came from.</summary>
    private BitAiToolOutcome Found(object value, string source, string titleKey, string siteKey) => BitAiToolOutcome.Success(ToJson(value)) with
    {
        Citations = [new($"{source}-citation", new($"{source}-source", localizer[titleKey]) { Kind = BitAiSourceKind.Dataset, SiteName = localizer[siteKey] })]
    };

    /// <summary>The low-stock rows, with the product names in the view's language.</summary>
    private AssistantStockItem[] StockItems() => [.. stockLevels.Select(s => new AssistantStockItem(localizer[s.NameKey], s.Left, s.Order))];

    /// <summary>A sample tool that answers after a short wait, longer in the slow state.</summary>
    private AssistantTool CreateTool(string name, string titleKey, string iconName, BitAiToolRisk risk = BitAiToolRisk.ReadOnly) =>
        new(new(name) { Title = localizer[titleKey], Icon = iconName, Risk = risk }, async cancellationToken =>
        {
            await Task.Delay(IsSlow ? Random.Shared.Next(1400, 2200) : Random.Shared.Next(500, 900), cancellationToken);

            return RunTool(name);
        });

    /// <summary>The sample history; in the mixed state the newest conversation ends in a failed answer.</summary>
    private List<BitAiConversationSnapshot> CreateConversations()
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

    /// <summary>Plays a topic's script into a finished conversation, approving and answering each tool call on the way.</summary>
    private BitAiConversationSnapshot CreateConversation(string id, string titleKey, DateTimeOffset at, AssistantTopic topic, bool isFailed = false)
    {
        var conversation = new BitAiConversation(id);
        conversation.SetTitle(localizer[titleKey]);
        conversation.AddMessage(BitAiRole.User, localizer[topic.PromptKey]);

        var processor = new BitAiConversationStreamProcessor(conversation);
        var reply = processor.BeginGeneration();

        if (isFailed)
        {
            reply.SetError(new(localizer[nameof(WorkbenchStrings.AssistantAnswerFailed)]) { IsRetryable = true });
            processor.EndGeneration(BitAiMessageStatus.Failed);
        }
        else
        {
            BitAiToolCallPart[] pending;

            // One round per request, as a live session makes them, until no tool call is left unanswered.
            do
            {
                processor.BeginStream();

                foreach (var evt in Reply(topic, Models[0], [.. reply.PartsOf<BitAiToolCallPart>()], ContextTokens(conversation.Messages), askApproval: true))
                {
                    processor.Apply(evt);
                }

                pending = [.. reply.PartsOf<BitAiToolCallPart>().Where(call => call.Result is null)];

                foreach (var call in pending)
                {
                    call.Definition = Tools.Find(call.ToolName)?.Definition;

                    if (call.Status is BitAiToolCallStatus.AwaitingApproval)
                    {
                        call.SetApproval(BitAiApprovalDecision.Approved);
                    }

                    processor.Apply(new BitAiToolResultEvent(call.ToolCallId, RunTool(call.ToolName) with { Duration = TimeSpan.FromMilliseconds(Random.Shared.Next(400, 900)) }));
                }
            } while (pending.Length > 0);

            processor.EndGeneration();
        }

        return Backdate(BitAiConversationSerializer.ToSnapshot(conversation), at);
    }

    /// <summary>Moves the snapshot's times to the given moment, so the history reads as minutes or days old.</summary>
    private static BitAiConversationSnapshot Backdate(BitAiConversationSnapshot snapshot, DateTimeOffset at) => snapshot with
    {
        CreatedAt = at,
        UpdatedAt = at,
        Messages = [.. snapshot.Messages.Select(message => message with
        {
            CreatedAt = at,
            Metadata = message.Metadata is null ? null : message.Metadata with { StartedAt = at.AddSeconds(-4), CompletedAt = at, TimeToFirstToken = TimeSpan.FromSeconds(0.6) },
            Parts = [.. message.Parts.Select(part => part.Kind is BitAiMessagePartKinds.Reasoning ? part with { StartedAt = at.AddSeconds(-3.4), CompletedAt = at.AddSeconds(-1.4) } : part)]
        })]
    };

    /// <summary>A rough token count of the messages, at four characters a token.</summary>
    private static int ContextTokens(IEnumerable<IBitAiMessage> messages) => messages.Sum(m => m.GetText().Length) / 4;

    /// <summary>The first date after today that falls on the given weekday.</summary>
    private static DateOnly NextDate(DayOfWeek day) => DateOnly.FromDateTime(DateTime.Today.AddDays(((int)day - (int)DateTime.Today.DayOfWeek + 6) % 7 + 1));

    private static JsonElement ToJson(object value) => JsonSerializer.SerializeToElement(value, JsonSerializerOptions.Web);

    /// <summary>A tool call with a new id and the given JSON arguments.</summary>
    private static BitAiToolCallStartedEvent CallTool(string toolName, string arguments)
    {
        using var document = JsonDocument.Parse(arguments);

        return new(BitAiId.NewToolCallId(), toolName) { Arguments = document.RootElement.Clone() };
    }

    /// <summary>Cuts text into pieces of two to six characters, the way a model streams tokens.</summary>
    private static IEnumerable<string> Slice(string text)
    {
        for (var index = 0; index < text.Length;)
        {
            var length = Math.Min(text.Length - index, Random.Shared.Next(2, 7));

            yield return text.Substring(index, length);

            index += length;
        }
    }
}
