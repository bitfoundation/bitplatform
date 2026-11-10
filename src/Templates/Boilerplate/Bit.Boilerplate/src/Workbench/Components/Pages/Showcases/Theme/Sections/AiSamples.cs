using System.Text.Json.Nodes;

namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>
/// What the AI section shows: conversations, messages and their parts, tool calls, agent runs, artifacts and the rest,
/// built once per section with their texts in the view's language. They are dated around <see cref="ThemeSamples.Today"/>;
/// only what is still under way counts from a moment ago, since its clock reads the time itself.
/// </summary>
public sealed class AiSamples
{
    /// <summary>The name the section's weather card is registered under for generative UI.</summary>
    public const string WeatherCardName = "workbench_weather";

    /// <summary>An illustration the app ships, for the samples that show an image.</summary>
    private const string ImageUrl = ThemeSamples.IllustrationUrl;

    private const string ModelId = "thorough";

    public const string CSharpCode = """
        public static class RestockRule
        {
            // Orders a product once it would run out before the next delivery.
            public static bool ShouldOrder(int left, int soldPerDay, int daysToDelivery) =>
                left < soldPerDay * daysToDelivery;
        }
        """;

    public const string ShellCode = "dotnet run --project src/Workbench";

    /// <summary>An order the model is still writing, cut off in the middle of a line.</summary>
    public const string StreamingCode = """
        {
          "store": "utrecht",
          "lines": [
            { "product": "oat_milk", "quantity": 24 },
            { "product": "croiss
        """;

    private const string OrderCode = """
        {
          "store": "utrecht",
          "lines": [
            { "product": "oat_milk", "quantity": 24 },
            { "product": "croissants", "quantity": 60 }
          ]
        }
        """;

    private const string StockRows = """
        [
          { "product": "Oat milk", "left": 4, "order": 24, "supplier": "Dekker" },
          { "product": "Croissants", "left": 6, "order": 60, "supplier": "Dekker" },
          { "product": "Paper cups", "left": 1, "order": 20, "supplier": "Verpakt" }
        ]
        """;

    private static readonly DateTimeOffset today = ThemeSamples.Today;

    private readonly IStringLocalizer<WorkbenchStrings> localizer;

    // When the runs and steps still under way started: a moment ago, so their running clocks read like a real run's.
    private readonly DateTimeOffset now = DateTimeOffset.UtcNow;

    public AiSamples(IStringLocalizer<WorkbenchStrings> localizer)
    {
        this.localizer = localizer;

        Citations = CreateCitations();
        Models = CreateModels();
        Suggestions = [localizer[nameof(WorkbenchStrings.ThemeAiHowDidTheStoresDo)], localizer[nameof(WorkbenchStrings.ThemeAiWhatRunsOutBeforeFriday)], localizer[nameof(WorkbenchStrings.ThemeAiDraftTheHolidayOpeningHours)]];

        AgentConversation = CreateAgentConversation();
        RunningRun = AgentConversation.Runs["running"];
        WaitingRun = AgentConversation.Runs["waiting"];
        CompletedRun = AgentConversation.Runs["completed"];
        FailedRun = AgentConversation.Runs["failed"];
        var steps = AgentConversation.Runs["steps"].Steps;
        Steps = [.. steps.Where(step => step.StepId is not "tools")];
        StepWithTools = steps.Single(step => step.StepId is "tools");

        ArtifactConversation = CreateArtifactConversation();
        DocumentArtifact = ArtifactConversation.Artifacts["hours"];
        VersionedArtifact = ArtifactConversation.Artifacts["rule"];
        StreamingArtifact = ArtifactConversation.Artifacts["email"];
        ArtifactPart = new("prices") { ArtifactVersion = 1 };
        MissingArtifactPart = new("price-sheet");

        ReportAttachment = new("att-report", "delivery-note-1042.pdf") { ContentType = "application/pdf", Size = 182_400, Kind = BitAiAttachmentKind.Document };
        PhotoAttachment = new("att-photo", "weekly-report.webp") { ContentType = "image/webp", Size = 42_816, Kind = BitAiAttachmentKind.Image, ThumbnailUrl = ImageUrl };
        UploadingAttachment = new("att-upload", "stock-count-utrecht.xlsx") { Size = 48_640, Status = BitAiAttachmentStatus.Uploading, Progress = 0.64 };
        FailedAttachment = new("att-failed", "security-camera.mp4") { Size = 734_003_200, Kind = BitAiAttachmentKind.Video, Status = BitAiAttachmentStatus.Failed, Error = localizer[nameof(WorkbenchStrings.ThemeAiOverThe100MBLimit)] };

        ChatConversation = CreateSalesConversation("chat");
        ListConversation = CreateSalesConversation("list");
        ReplyingConversation = CreateReplyingConversation();
        MessageConversation = CreateSalesConversation("message");
        ReplyMessage = MessageConversation.Messages[1];

        UserMessage = new(BitAiRole.User);
        UserMessage.AddPart(new BitAiTextPart(localizer[nameof(WorkbenchStrings.ThemeAiCanYouCheckThisDelivery)]));
        UserMessage.AddAttachment(ReportAttachment);

        StreamingMessage = Streaming(localizer[nameof(WorkbenchStrings.ThemeAiTheDeliveryNoteLists24)]);

        PendingMessage = new(BitAiRole.Assistant);
        PendingMessage.SetStatus(BitAiMessageStatus.Pending);
        PendingMessage.SetStatusText(localizer[nameof(WorkbenchStrings.ThemeAiCheckingTheStockInUtrecht)]);

        FailedMessage = new(BitAiRole.Assistant);
        FailedMessage.SetStatus(BitAiMessageStatus.Failed);
        FailedMessage.SetError(new BitAiError(localizer[nameof(WorkbenchStrings.ThemeAiTheAssistantCouldNotAnswer)]) { Kind = BitAiErrorKind.Network, IsRetryable = true });

        SystemMessage = new(BitAiRole.System);
        SystemMessage.AddPart(new BitAiTextPart(localizer[nameof(WorkbenchStrings.ThemeAiJoinedTheConversation), localizer[nameof(WorkbenchStrings.DemoUserYas)]]));

        Conversations =
        [
            new("weekly", localizer[nameof(WorkbenchStrings.ThemeAiWeeklySalesReview)], today.AddMinutes(-12), localizer[nameof(WorkbenchStrings.ThemeAiRotterdamGrewMostThanksTo)], 6),
            new("restock", localizer[nameof(WorkbenchStrings.ThemeAiRestockTheUtrechtStore)], today.AddDays(-1), localizer[nameof(WorkbenchStrings.ThemeAiOrderPlacedForFridayMorning)], 8),
            new("promo", localizer[nameof(WorkbenchStrings.AssistantHistoryAutumn)], today.AddDays(-3), localizer[nameof(WorkbenchStrings.ThemeAiTwoDraftsOfThePoster)], 4),
            new("hours", localizer[nameof(WorkbenchStrings.ThemeAiHolidayOpeningHours)], today.AddDays(-9), localizer[nameof(WorkbenchStrings.ThemeAiPublishedOnEveryStorePage)], 10),
            new("staff", localizer[nameof(WorkbenchStrings.ThemeAiTeamScheduleForJune)], today.AddDays(-22), localizer[nameof(WorkbenchStrings.ThemeAiThreeShiftsStillOpen)], 6),
            new("suppliers", localizer[nameof(WorkbenchStrings.ThemeAiCompareTheBakeries)], today.AddDays(-41), localizer[nameof(WorkbenchStrings.ThemeAiDekkerDeliversEarliest)], 12)
        ];

        Form = CreateForm();

        WeatherPart = Generative(WeatherCardName, WeatherProps());
        UnregisteredPart = Generative("store_map", new JsonObject { ["store"] = "utrecht" });
        MismatchedPart = Generative(WeatherCardName, new JsonObject
        {
            ["city"] = "Rotterdam",
            ["temperature"] = "warm",
            ["condition"] = localizer[nameof(WorkbenchStrings.ThemeAiSunny)].Value,
            ["sky"] = "sunny",
            ["high"] = 24,
            ["low"] = 15,
            ["rain"] = 5,
            ["wind"] = "5 Bft"
        });

        ImagePart = new(ImageUrl) { Alt = localizer[nameof(WorkbenchStrings.ThemeAiTwoPeopleReadingASales)], Caption = localizer[nameof(WorkbenchStrings.ThemeAiTheWeeklyReportAsAn)] };

        OrderJson = Json("""
            {
              "order": "PO-1042",
              "store": "Utrecht",
              "delivery": { "day": "2026-05-22", "time": "07:00" },
              "lines": [
                { "product": "oat_milk", "quantity": 24 },
                { "product": "croissants", "quantity": 60 },
                { "product": "paper_cups", "quantity": 20 }
              ],
              "approved": true,
              "notes": null
            }
            """);
        TableRows = Json(StockRows);

        MarkdownDocument = BitAiMarkdownParser.Parse(CreateMarkdown());
        MarkdownText = localizer[nameof(WorkbenchStrings.ThemeAiRotterdamGrewMostThisWeek)];
        StreamingMarkdownText = localizer[nameof(WorkbenchStrings.ThemeAiRotterdamGrewMostThisWeekThanksTo)];

        ActionsMessage = Reply(new BitAiTextPart(localizer[nameof(WorkbenchStrings.ThemeAiOrderPO1042IsBooked)]));
        ActionsMessage.MergeMetadata(Metadata(1_850, 64, 1.8));
        RatedMessage = Reply(new BitAiTextPart(localizer[nameof(WorkbenchStrings.ThemeAiOrderPO1042IsBooked)]));
        RatedMessage.MergeMetadata(Metadata(1_850, 64, 1.8));
        RatedMessage.SetFeedback(BitAiFeedbackKind.Positive);

        PartsMessage = Reply(
            new BitAiMarkdownPart(localizer[nameof(WorkbenchStrings.ThemeAiHereIsTheOrderI)]),
            new BitAiCodePart(OrderCode, "json") { FileName = "order-1042.json" },
            new BitAiFilePart(ReportAttachment),
            new BitAiStructuredDataPart(Json("""{ "store": "Utrecht", "nextDelivery": "2026-05-22", "productsLow": 3, "orderTotal": 412.5 }"""), "restock_summary"));

        ThinkingPart = new BitAiReasoningPart();
        ThinkingPart.Append(localizer[nameof(WorkbenchStrings.ThemeAiTheWeekSSalesAre)]);
        ReasonedPart = Settle(Conversation(Reply(new BitAiReasoningPart(localizer[nameof(WorkbenchStrings.ThemeAiCompareEachStoreSNet)])))).Messages[0].PartsOf<BitAiReasoningPart>().First();

        WebSource = Citations[2].Source;
        DatasetSource = Citations[0].Source;

        ObjectDataPart = new(Json("""{ "store": "Utrecht", "nextDelivery": "2026-05-22", "productsLow": 3, "orderTotal": 412.5, "approved": false, "notes": null }"""), "restock_summary");
        TableDataPart = new(Json(StockRows), "low_stock");
        StreamingDataPart = new("restock_summary");
        StreamingDataPart.Append("""{ "store": "Utrecht", "nextDelivery": "2026-05-22", "productsLo""");

        TextMessage = Reply(new BitAiTextPart(localizer[nameof(WorkbenchStrings.ThemeAiUtrechtHadItsBestSaturday)]));
        TextPart = TextMessage.PartsOf<BitAiTextPart>().First();
        StreamingTextMessage = Streaming(localizer[nameof(WorkbenchStrings.ThemeAiUtrechtHadItsBestSaturdaySinceMarch)]);
        StreamingTextPart = StreamingTextMessage.PartsOf<BitAiTextPart>().First();
        (CitedMessage, CitedPart) = CreateCitedText();

        TokenUsage = new() { InputTokens = 1_850, OutputTokens = 420 };
        CostUsage = new() { InputTokens = 1_850, OutputTokens = 420, Cost = 0.0119m, Currency = "EUR" };
        GenerationMetadata = Metadata(1_850, 420, 5.2) with { Usage = CostUsage };
        DetailedUsage = new() { InputTokens = 12_400, OutputTokens = 1_860, CachedInputTokens = 9_800, ReasoningTokens = 1_100, Cost = 0.0312m, Currency = "EUR" };

        WriteApprovalCall = Approval("call-approve-order", "place_order", localizer[nameof(WorkbenchStrings.ThemeAiPlaceTheOrder)], BitIconName.DeliveryTruck, BitAiToolRisk.Write,
            localizer[nameof(WorkbenchStrings.ThemeAiOrder24OatMilk60)]);
        DestructiveApprovalCall = Approval("call-approve-delete", "delete_price_list", localizer[nameof(WorkbenchStrings.ThemeAiDeleteAPriceList)], BitIconName.Delete, BitAiToolRisk.Destructive,
            localizer[nameof(WorkbenchStrings.ThemeAiPermanentlyDeleteTheMarchPrice)]);
        DisabledApprovalCall = Approval("call-approve-disabled", "place_order", localizer[nameof(WorkbenchStrings.ThemeAiPlaceTheOrder)], BitIconName.DeliveryTruck, BitAiToolRisk.Write, null);

        StreamingCall = new("call-hours", "get_opening_hours") { Definition = Definition("get_opening_hours", localizer[nameof(WorkbenchStrings.ThemeAiOpeningHours)], BitIconName.Globe) };
        StreamingCall.AppendArguments("""{"store":"rotter""");

        RunningCall = Call("call-sales-running", "get_sales", localizer[nameof(WorkbenchStrings.ThemeAiSales)], BitIconName.BarChartVerticalFill, """{"period":"last_30_days","store":"utrecht"}""");
        RunningCall.SetStatus(BitAiToolCallStatus.Running);
        RunningCall.SetProgress(new(0.6, localizer[nameof(WorkbenchStrings.ThemeAiReading18Of30Days)]));

        AwaitingCall = Approval("call-order", "place_order", localizer[nameof(WorkbenchStrings.ThemeAiPlaceTheOrder)], BitIconName.DeliveryTruck, BitAiToolRisk.Write,
            localizer[nameof(WorkbenchStrings.ThemeAiOrder24OatMilk60)]);

        CompletedCall = Call("call-stock", "check_stock", localizer[nameof(WorkbenchStrings.ThemeAiStock)], BitIconName.Package, """{"store":"utrecht","until":"next_delivery"}""");
        CompletedCall.SetStatus(BitAiToolCallStatus.Running);
        CompletedCall.SetResult(BitAiToolOutcome.Success(Json(StockRows)) with { Duration = TimeSpan.FromMilliseconds(640) });

        FailedCall = Call("call-deliveries", "get_deliveries", localizer[nameof(WorkbenchStrings.ThemeAiDeliveries)], BitIconName.DeliveryTruck, """{"supplier":"dekker"}""");
        FailedCall.SetStatus(BitAiToolCallStatus.Running);
        FailedCall.SetResult(BitAiToolOutcome.Failure(new BitAiError(localizer[nameof(WorkbenchStrings.ThemeAiTheSupplierSSystemDid)]) { Code = "supplier_timeout", Kind = BitAiErrorKind.Tool })
            with
        { Duration = TimeSpan.FromSeconds(3.2) });

        DeclinedCall = Approval("call-declined", "delete_price_list", localizer[nameof(WorkbenchStrings.ThemeAiDeleteAPriceList)], BitIconName.Delete, BitAiToolRisk.Destructive, null);
        DeclinedCall.SetApproval(BitAiApprovalDecision.Rejected);
        DeclinedCall.SetResult(BitAiToolOutcome.Failure(new BitAiError(localizer[nameof(WorkbenchStrings.ThemeAiYouDeclinedThisToolCall)]) { Code = "rejected", Kind = BitAiErrorKind.Tool }));
        DeclinedCall.SetStatus(BitAiToolCallStatus.Rejected);

        CancelledCall = Call("call-export", "export_report", localizer[nameof(WorkbenchStrings.ThemeAiExportTheReport)], BitIconName.BarChartVerticalFill, """{"format":"pdf","weeks":4}""");
        CancelledCall.SetStatus(BitAiToolCallStatus.Running);
        CancelledCall.SetStatus(BitAiToolCallStatus.Cancelled);

        DataResultPart = new("call-sales", BitAiToolOutcome.Success(Json("""{ "store": "Utrecht", "net": 8650, "orders": 412, "change": 0.003 }"""))) { ToolName = "get_sales" };
        TextResultPart = new("call-stock-text", BitAiToolOutcome.FromText(localizer[nameof(WorkbenchStrings.ThemeAiUtrechtHas4CartonsOf)])) { ToolName = "check_stock" };
        FailedResultPart = new("call-sales-failed", BitAiToolOutcome.Failure(new BitAiError(localizer[nameof(WorkbenchStrings.ThemeAiThereIsNoStoreCalled)]) { Code = "unknown_store", Kind = BitAiErrorKind.Tool })) { ToolName = "get_sales" };

        RetryableError = new(localizer[nameof(WorkbenchStrings.ThemeAiTheSalesServiceDidNot)])
        {
            Code = "upstream_timeout",
            Kind = BitAiErrorKind.Timeout,
            IsRetryable = true,
            Detail = "HTTP 504 Gateway Timeout\nx-request-id: 7f3c2a91-4e0b-4f6e-9b1d-2c8a5e0d3b44\nupstream: sales-api (10.2 s)"
        };
        FilterError = new(localizer[nameof(WorkbenchStrings.ThemeAiTheProviderSContentFilter)]) { Code = "content_filter", Kind = BitAiErrorKind.ContentFilter };
    }

    // Agent runs and steps.
    public BitAiConversation AgentConversation { get; }

    public BitAiAgentRun RunningRun { get; }

    public BitAiAgentRun WaitingRun { get; }

    public BitAiAgentRun CompletedRun { get; }

    public BitAiAgentRun FailedRun { get; }

    /// <summary>One step in each status, in the order of <see cref="BitAiAgentStepStatus"/>.</summary>
    public IReadOnlyList<BitAiAgentStep> Steps { get; }

    /// <summary>A finished step that called two tools, one of which failed; its calls are in <see cref="AgentConversation"/>.</summary>
    public BitAiAgentStep StepWithTools { get; }

    // Artifacts.
    public BitAiConversation ArtifactConversation { get; }

    public BitAiArtifact DocumentArtifact { get; }

    /// <summary>A code artifact revised twice, so its version picker shows.</summary>
    public BitAiArtifact VersionedArtifact { get; }

    public BitAiArtifact StreamingArtifact { get; }

    public BitAiArtifactPart ArtifactPart { get; }

    /// <summary>A part pointing at an artifact the conversation doesn't hold, which shows as a placeholder.</summary>
    public BitAiArtifactPart MissingArtifactPart { get; }

    // Attachments.
    public BitAiAttachment ReportAttachment { get; }

    public BitAiAttachment PhotoAttachment { get; }

    public BitAiAttachment UploadingAttachment { get; }

    public BitAiAttachment FailedAttachment { get; }

    // Conversations and messages.
    public BitAiConversation ChatConversation { get; }

    public BitAiConversation ListConversation { get; }

    /// <summary>A conversation whose reply is still being written.</summary>
    public BitAiConversation ReplyingConversation { get; }

    public BitAiConversation MessageConversation { get; }

    /// <summary>The first reply of <see cref="MessageConversation"/>: reasoning, a tool call, a table and a source.</summary>
    public BitAiMessage ReplyMessage { get; }

    public BitAiMessage UserMessage { get; }

    public BitAiMessage StreamingMessage { get; }

    public BitAiMessage PendingMessage { get; }

    public BitAiMessage FailedMessage { get; }

    public BitAiMessage SystemMessage { get; }

    public BitAiMessage ActionsMessage { get; }

    public BitAiMessage RatedMessage { get; }

    /// <summary>A reply with a part of each of several kinds, for the part view.</summary>
    public BitAiMessage PartsMessage { get; }

    public IReadOnlyList<BitAiConversationSummary> Conversations { get; }

    public IReadOnlyList<string> Suggestions { get; }

    public IReadOnlyList<BitAiModel> Models { get; }

    // Citations and sources.
    public IReadOnlyList<BitAiCitation> Citations { get; }

    public BitAiCitationSource WebSource { get; }

    public BitAiCitationSource DatasetSource { get; }

    // Content.
    public BitAiFormDefinition Form { get; }

    public BitAiGenerativeUiPart WeatherPart { get; }

    public BitAiGenerativeUiPart UnregisteredPart { get; }

    /// <summary>A weather card whose props have a value of the wrong type and one the card doesn't take.</summary>
    public BitAiGenerativeUiPart MismatchedPart { get; }

    public BitAiImagePart ImagePart { get; }

    public JsonElement OrderJson { get; }

    public JsonElement TableRows { get; }

    public BitAiMarkdownDocument MarkdownDocument { get; }

    public string MarkdownText { get; }

    public string StreamingMarkdownText { get; }

    public BitAiReasoningPart ThinkingPart { get; }

    /// <summary>A finished reasoning that took a few seconds.</summary>
    public BitAiReasoningPart ReasonedPart { get; }

    public BitAiStructuredDataPart ObjectDataPart { get; }

    public BitAiStructuredDataPart TableDataPart { get; }

    public BitAiStructuredDataPart StreamingDataPart { get; }

    public BitAiMessage TextMessage { get; }

    public BitAiTextPart TextPart { get; }

    public BitAiMessage StreamingTextMessage { get; }

    public BitAiTextPart StreamingTextPart { get; }

    public BitAiMessage CitedMessage { get; }

    public BitAiTextPart CitedPart { get; }

    // Usage.
    public BitAiTokenUsage TokenUsage { get; }

    public BitAiTokenUsage CostUsage { get; }

    public BitAiGenerationMetadata GenerationMetadata { get; }

    public BitAiTokenUsage DetailedUsage { get; }

    // Tool calls and their results.
    public BitAiToolCallPart WriteApprovalCall { get; }

    public BitAiToolCallPart DestructiveApprovalCall { get; }

    public BitAiToolCallPart DisabledApprovalCall { get; }

    public BitAiToolCallPart StreamingCall { get; }

    public BitAiToolCallPart RunningCall { get; }

    public BitAiToolCallPart AwaitingCall { get; }

    public BitAiToolCallPart CompletedCall { get; }

    public BitAiToolCallPart FailedCall { get; }

    public BitAiToolCallPart DeclinedCall { get; }

    public BitAiToolCallPart CancelledCall { get; }

    public BitAiToolResultPart DataResultPart { get; }

    public BitAiToolResultPart TextResultPart { get; }

    public BitAiToolResultPart FailedResultPart { get; }

    // Errors.
    public BitAiError RetryableError { get; }

    public BitAiError FilterError { get; }

    /// <summary>The sources the answers cite: a dataset, a document and a web page.</summary>
    private List<BitAiCitation> CreateCitations()
    {
        var report = new BitAiCitationSource("sales-week-20", localizer[nameof(WorkbenchStrings.ThemeAiSalesReportWeek20)])
        {
            Kind = BitAiSourceKind.Dataset,
            SiteName = localizer[nameof(WorkbenchStrings.ThemeAiSales)],
            Description = localizer[nameof(WorkbenchStrings.ThemeAiNetSalesOfEveryStore)]
        };

        var prices = new BitAiCitationSource("price-list", localizer[nameof(WorkbenchStrings.ThemeAiSupplierPriceList2026)])
        {
            Kind = BitAiSourceKind.Document,
            SiteName = localizer[nameof(WorkbenchStrings.ThemeAiPurchasing)],
            Url = "https://www.example.com/purchasing/price-list-2026"
        };

        var hours = new BitAiCitationSource("opening-hours", localizer[nameof(WorkbenchStrings.ThemeAiOpeningHoursOfTheStores)])
        {
            Kind = BitAiSourceKind.Web,
            SiteName = localizer[nameof(WorkbenchStrings.ThemeAiStoreWebsite)],
            Url = "https://www.example.com/stores/opening-hours"
        };

        return
        [
            new("cite-sales", report) { Number = 1, Quote = localizer[nameof(WorkbenchStrings.ThemeAiRotterdamNetSalesUp12)] },
            new("cite-prices", prices) { Number = 2, Quote = localizer[nameof(WorkbenchStrings.ThemeAiOatMilkPerCartonOf)] },
            new("cite-hours", hours) { Number = 3 }
        ];
    }

    private List<BitAiModel> CreateModels() =>
    [
        new("thorough", localizer[nameof(WorkbenchStrings.AssistantModelThorough)])
        {
            Family = localizer[nameof(WorkbenchStrings.ThemeAiStoreAssistant)],
            Description = localizer[nameof(WorkbenchStrings.ThemeAiThinksItThroughAndUses)],
            Icon = BitIconName.Lightbulb,
            ContextWindow = 32_000,
            Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Tools | BitAiModelCapabilities.Reasoning | BitAiModelCapabilities.Citations
        },
        new("quick", localizer[nameof(WorkbenchStrings.AssistantModelQuick)])
        {
            Family = localizer[nameof(WorkbenchStrings.ThemeAiStoreAssistant)],
            Description = localizer[nameof(WorkbenchStrings.ThemeAiShortAnswersRightAway)],
            Icon = BitIconName.LightningBolt,
            ContextWindow = 16_000,
            Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Tools
        },
        new("vision", localizer[nameof(WorkbenchStrings.ThemeAiVision)])
        {
            Description = localizer[nameof(WorkbenchStrings.ThemeAiReadsPhotosOfShelvesAnd)],
            Icon = BitIconName.Camera,
            ContextWindow = 16_000,
            Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Vision
        }
    ];

    /// <summary>
    /// The agent runs, one per look, and a run holding a step in each status; the step with tools looks its calls up in
    /// the reply this conversation keeps. A run or step still under way started a moment ago, the others on the samples' day.
    /// </summary>
    private BitAiConversation CreateAgentConversation()
    {
        var conversation = new BitAiConversation("agents");
        var reply = conversation.AddMessage(new BitAiMessage(BitAiRole.Assistant));

        var stock = reply.AddPart(Call("call-step-stock", "check_stock", localizer[nameof(WorkbenchStrings.ThemeAiStock)], BitIconName.Package, """{"store":"utrecht"}"""));
        stock.SetStatus(BitAiToolCallStatus.Running);
        stock.SetResult(BitAiToolOutcome.FromText(localizer[nameof(WorkbenchStrings.ThemeAi3ProductsRunOutBefore)]));

        var deliveries = reply.AddPart(Call("call-step-deliveries", "get_deliveries", localizer[nameof(WorkbenchStrings.ThemeAiDeliveries)], BitIconName.DeliveryTruck, """{"supplier":"dekker"}"""));
        deliveries.SetStatus(BitAiToolCallStatus.Running);
        deliveries.SetResult(BitAiToolOutcome.Failure(localizer[nameof(WorkbenchStrings.ThemeAiTheSupplierSSystemDid)]));

        var running = now.AddSeconds(-38);
        var waiting = now.AddSeconds(-74);

        var stepsFailure = new BitAiError(localizer[nameof(WorkbenchStrings.ThemeAi2Of3StorePages)])
        {
            Detail = $"{localizer[nameof(WorkbenchStrings.ThemeAiRotterdamThePageIsOpen)]}\n{localizer[nameof(WorkbenchStrings.ThemeAiUtrechtThePageIsOpen)]}"
        };

        return Settle(conversation,
            RestockRun("running", BitAiAgentRunStatus.Running, running, null, default,
            [
                Step("sales", localizer[nameof(WorkbenchStrings.ThemeAiReadThisWeekSSales)], BitAiAgentStepStatus.Completed, running.AddSeconds(1), 6, localizer[nameof(WorkbenchStrings.ThemeAiCroissantsUp18OatMilk)]),
                Step("stock", localizer[nameof(WorkbenchStrings.ThemeAiCheckTheStockInThe)], BitAiAgentStepStatus.Completed, running.AddSeconds(8), 11, localizer[nameof(WorkbenchStrings.ThemeAi3ProductsRunOutBeforeFriday)]),
                Step("draft", localizer[nameof(WorkbenchStrings.ThemeAiDraftTheOrder)], BitAiAgentStepStatus.Running, running.AddSeconds(20)),
                Step("send", localizer[nameof(WorkbenchStrings.ThemeAiSendItToTheSupplier)], BitAiAgentStepStatus.Pending)
            ]),
            RestockRun("waiting", BitAiAgentRunStatus.WaitingForInput, waiting, null, default,
            [
                Step("sales", localizer[nameof(WorkbenchStrings.ThemeAiReadThisWeekSSales)], BitAiAgentStepStatus.Completed, waiting.AddSeconds(1), 6, localizer[nameof(WorkbenchStrings.ThemeAiCroissantsUp18OatMilk)]),
                Step("stock", localizer[nameof(WorkbenchStrings.ThemeAiCheckTheStockInThe)], BitAiAgentStepStatus.Completed, waiting.AddSeconds(8), 11, localizer[nameof(WorkbenchStrings.ThemeAi3ProductsRunOutBeforeFriday)]),
                Step("draft", localizer[nameof(WorkbenchStrings.ThemeAiDraftTheOrder)], BitAiAgentStepStatus.Completed, waiting.AddSeconds(20), 14, localizer[nameof(WorkbenchStrings.ThemeAi24OatMilk60Croissants)]),
                Step("slot", localizer[nameof(WorkbenchStrings.ThemeAiPickADeliverySlot)], BitAiAgentStepStatus.Blocked, waiting.AddSeconds(35)),
                Step("send", localizer[nameof(WorkbenchStrings.ThemeAiSendItToTheSupplier)], BitAiAgentStepStatus.Pending)
            ]) with
            {
                PendingPrompt = localizer[nameof(WorkbenchStrings.ThemeAiDekkerCanDeliverOnFriday)],
                PendingSuggestions = [localizer[nameof(WorkbenchStrings.ThemeAiFriday700)], localizer[nameof(WorkbenchStrings.ThemeAiSaturday900)]]
            },
            RestockRun("completed", BitAiAgentRunStatus.Completed, today, today.AddSeconds(58), new() { InputTokens = 6_240, OutputTokens = 1_180 },
            [
                Step("sales", localizer[nameof(WorkbenchStrings.ThemeAiReadThisWeekSSales)], BitAiAgentStepStatus.Completed, today.AddSeconds(1), 6, localizer[nameof(WorkbenchStrings.ThemeAiCroissantsUp18OatMilk)]),
                Step("stock", localizer[nameof(WorkbenchStrings.ThemeAiCheckTheStockInThe)], BitAiAgentStepStatus.Completed, today.AddSeconds(8), 11, localizer[nameof(WorkbenchStrings.ThemeAi3ProductsRunOutBeforeFriday)]),
                Step("draft", localizer[nameof(WorkbenchStrings.ThemeAiDraftTheOrder)], BitAiAgentStepStatus.Completed, today.AddSeconds(20), 14, localizer[nameof(WorkbenchStrings.ThemeAi24OatMilk60Croissants)]),
                Step("send", localizer[nameof(WorkbenchStrings.ThemeAiSendItToTheSupplier)], BitAiAgentStepStatus.Completed, today.AddSeconds(35), 23, localizer[nameof(WorkbenchStrings.ThemeAiOrderPO1042ConfirmedFor)])
            ]),
            new BitAiAgentRunSnapshot
            {
                RunId = "failed",
                AgentName = localizer[nameof(WorkbenchStrings.ThemeAiOpeningHoursAgent)],
                Goal = localizer[nameof(WorkbenchStrings.ThemeAiPublishTheHolidayOpeningHours)],
                Status = BitAiAgentRunStatus.Failed,
                StartedAt = today,
                CompletedAt = today.AddSeconds(23),
                Error = new BitAiError(localizer[nameof(WorkbenchStrings.ThemeAiTheRunStoppedBeforeAnnouncing)]),
                Steps =
                [
                    Step("collect", localizer[nameof(WorkbenchStrings.ThemeAiCollectTheHoursFromThe)], BitAiAgentStepStatus.Completed, today.AddSeconds(1), 12, localizer[nameof(WorkbenchStrings.ThemeAiHoursFromAll3Store)]),
                    Step("pages", localizer[nameof(WorkbenchStrings.ThemeAiUpdateTheStorePages)], BitAiAgentStepStatus.Failed, today.AddSeconds(14), 9, error: stepsFailure),
                    Step("announce", localizer[nameof(WorkbenchStrings.ThemeAiAnnounceTheNewHours)], BitAiAgentStepStatus.Skipped)
                ]
            },
            new BitAiAgentRunSnapshot
            {
                RunId = "steps",
                Status = BitAiAgentRunStatus.Running,
                StartedAt = waiting,
                Steps =
                [
                    Step("send", localizer[nameof(WorkbenchStrings.ThemeAiSendTheOrderToThe)], BitAiAgentStepStatus.Pending),
                    Step("draft", localizer[nameof(WorkbenchStrings.ThemeAiDraftTheOrder)], BitAiAgentStepStatus.Running, now.AddSeconds(-17)),
                    Step("slot", localizer[nameof(WorkbenchStrings.ThemeAiPickADeliverySlot)], BitAiAgentStepStatus.Blocked, now.AddSeconds(-40)),
                    Step("sales", localizer[nameof(WorkbenchStrings.ThemeAiReadThisWeekSSales)], BitAiAgentStepStatus.Completed, today, 6, localizer[nameof(WorkbenchStrings.ThemeAiCroissantsUp18OatMilk)]) with
                    {
                        Detail = localizer[nameof(WorkbenchStrings.ThemeAiComparedWithTheSameWeek)]
                    },
                    Step("pages", localizer[nameof(WorkbenchStrings.ThemeAiUpdateTheStorePages)], BitAiAgentStepStatus.Failed, today, 9, error: stepsFailure),
                    Step("announce", localizer[nameof(WorkbenchStrings.ThemeAiAnnounceTheNewHours)], BitAiAgentStepStatus.Skipped),
                    Step("tools", localizer[nameof(WorkbenchStrings.ThemeAiCheckTheStockInThe)], BitAiAgentStepStatus.Completed, today, 11, localizer[nameof(WorkbenchStrings.ThemeAi3ProductsRunOutBeforeFriday)]) with
                    {
                        ToolCallIds = [stock.ToolCallId, deliveries.ToolCallId]
                    }
                ]
            });
    }

    private BitAiAgentRunSnapshot RestockRun(string runId, BitAiAgentRunStatus status, DateTimeOffset startedAt, DateTimeOffset? completedAt, BitAiTokenUsage usage, IReadOnlyList<BitAiAgentStepSnapshot> steps) => new()
    {
        RunId = runId,
        AgentName = localizer[nameof(WorkbenchStrings.ThemeAiRestockAgent)],
        Goal = localizer[nameof(WorkbenchStrings.ThemeAiRestockTheUtrechtStoreBefore)],
        Status = status,
        StartedAt = startedAt,
        CompletedAt = completedAt,
        Usage = usage,
        Steps = steps
    };

    /// <summary>A step as saved: a finished one took the given seconds from its start, one under way has only its start.</summary>
    private static BitAiAgentStepSnapshot Step(string stepId, string title, BitAiAgentStepStatus status, DateTimeOffset? startedAt = null, double seconds = 0, string? summary = null, BitAiError? error = null) => new()
    {
        StepId = stepId,
        Title = title,
        Status = status,
        Summary = summary,
        Error = error,
        StartedAt = startedAt,
        CompletedAt = status is BitAiAgentStepStatus.Completed or BitAiAgentStepStatus.Failed ? startedAt?.AddSeconds(seconds) : null
    };

    /// <summary>Two finished documents, a code file revised twice and an email still being written.</summary>
    private BitAiConversation CreateArtifactConversation()
    {
        var conversation = new BitAiConversation("artifacts");

        var hours = conversation.GetOrAddArtifact("hours", BitAiArtifactKind.Document, localizer[nameof(WorkbenchStrings.ThemeAiHolidayOpeningHours)]);
        hours.SetContent($$"""
            ## {{localizer[nameof(WorkbenchStrings.ThemeAiHolidayOpeningHours)]}}

            {{localizer[nameof(WorkbenchStrings.ThemeAiEveryStoreKeepsItsUsual)]}}

            | Store | May 25 | May 26 |
            | --- | :---: | :---: |
            | Amsterdam | 09:00 - 17:00 | 10:00 - 16:00 |
            | Rotterdam | 08:00 - 17:00 | 10:00 - 16:00 |
            | Utrecht | 09:00 - 16:00 | {{localizer[nameof(WorkbenchStrings.ThemeAiClosed)]}} |

            {{localizer[nameof(WorkbenchStrings.ThemeAiOrdersPlacedInTheApp)]}}
            """);
        hours.Complete(localizer[nameof(WorkbenchStrings.ThemeAiFirstDraft)]);

        var prices = conversation.GetOrAddArtifact("prices", BitAiArtifactKind.Document, localizer[nameof(WorkbenchStrings.ThemeAiPriceChangesForJune)]);
        prices.SetContent($"""
            - **{localizer[nameof(WorkbenchStrings.PosStockOatMilk)]}**: €21.60 → €22.80
            - **{localizer[nameof(WorkbenchStrings.AssistantStockCroissants)]}**: €0.62 → €0.65
            - **{localizer[nameof(WorkbenchStrings.AssistantStockPaperCups)]}**: {localizer[nameof(WorkbenchStrings.ThemeAiNoChange)]}
            """);
        prices.Complete(localizer[nameof(WorkbenchStrings.ThemeAiFromTheSupplierSNew)]);

        var rule = conversation.GetOrAddArtifact("rule", BitAiArtifactKind.Code, "restock.ts");
        rule.Language = "typescript";
        rule.SetContent("export const shouldOrder = (left: number) => left < 6;");
        rule.Complete(localizer[nameof(WorkbenchStrings.ThemeAiFirstDraft)]);
        rule.BeginRevision();
        rule.SetContent("export const shouldOrder = (left: number, soldPerDay: number) => left < soldPerDay * 2;");
        rule.Complete(localizer[nameof(WorkbenchStrings.ThemeAiByWhatSells)]);
        rule.BeginRevision();
        rule.SetContent("""
            export const shouldOrder = (left: number, soldPerDay: number, daysToDelivery: number) =>
                left < soldPerDay * daysToDelivery;
            """);
        rule.Complete(localizer[nameof(WorkbenchStrings.ThemeAiUntilTheNextDelivery)]);

        var email = conversation.GetOrAddArtifact("email", BitAiArtifactKind.Document, localizer[nameof(WorkbenchStrings.ThemeAiEmailToTheSupplier)]);
        email.Append($"{localizer[nameof(WorkbenchStrings.ThemeAiDearDekkerTeam)]}\n\n{localizer[nameof(WorkbenchStrings.ThemeAiCouldYouDeliverTheOrder)]}");

        return conversation;
    }

    /// <summary>
    /// A finished chat: the week's sales, with reasoning, a tool call, a table and a source, then the terrace's weather as
    /// a generative UI card. Each sample that shows it gets a copy of its own, since a chat can be written to.
    /// </summary>
    private BitAiConversation CreateSalesConversation(string conversationId)
    {
        var conversation = new BitAiConversation(conversationId);
        conversation.SetTitle(localizer[nameof(WorkbenchStrings.ThemeAiWeeklySalesReview)]);
        conversation.AddMessage(BitAiRole.User, localizer[nameof(WorkbenchStrings.ThemeAiHowDidTheStoresDo)]);

        var sales = Reply(new BitAiReasoningPart(localizer[nameof(WorkbenchStrings.ThemeAiCompareEachStoreSNet)]));
        var call = sales.AddPart(Call($"{conversationId}-sales", "get_sales", localizer[nameof(WorkbenchStrings.ThemeAiSales)], BitIconName.BarChartVerticalFill, """{"period":"last_7_days"}"""));
        call.SetStatus(BitAiToolCallStatus.Running);
        call.SetResult(BitAiToolOutcome.Success(Json("""{ "net": 34820, "change": 0.084, "stores": ["Amsterdam", "Rotterdam", "Utrecht"] }""")) with { Duration = TimeSpan.FromMilliseconds(640) });
        sales.AddPart(new BitAiMarkdownPart($"""
            {localizer[nameof(WorkbenchStrings.ThemeAiNetSalesCameTo34)]}

            | Store | Net sales | Change |
            | --- | ---: | ---: |
            | Amsterdam | €15,940 | +7.1% |
            | Rotterdam | €10,230 | +12.1% |
            | Utrecht | €8,650 | +0.3% |

            {localizer[nameof(WorkbenchStrings.ThemeAiRotterdamGrewMostThanksToTheNew)]}
            """));
        sales.AddCitation(Citations[0]);
        sales.MergeMetadata(Metadata(2_140, 286, 6.2));
        conversation.AddMessage(sales);

        conversation.AddMessage(BitAiRole.User, localizer[nameof(WorkbenchStrings.ThemeAiWillItBeWarmEnough)]);

        var weather = Reply(new BitAiTextPart(localizer[nameof(WorkbenchStrings.ThemeAiYesItWillBeMild)]));
        weather.AddPart(Generative(WeatherCardName, WeatherProps()));
        weather.MergeMetadata(Metadata(2_610, 64, 2.4));
        conversation.AddMessage(weather);

        return Settle(conversation);
    }

    /// <summary>The props of tomorrow's weather in Amsterdam, for the weather card.</summary>
    private JsonObject WeatherProps() => new()
    {
        ["city"] = "Amsterdam",
        ["temperature"] = 19,
        ["condition"] = localizer[nameof(WorkbenchStrings.ThemeAiPartlyCloudy)].Value,
        ["sky"] = "partly_cloudy",
        ["high"] = 21,
        ["low"] = 12,
        ["rain"] = 20
    };

    /// <summary>A question and the start of its answer, still being written.</summary>
    private BitAiConversation CreateReplyingConversation()
    {
        var conversation = new BitAiConversation("replying");
        conversation.AddMessage(BitAiRole.User, localizer[nameof(WorkbenchStrings.ThemeAiWhatRunsOutBeforeFriday)]);
        conversation.AddMessage(Streaming($"{localizer[nameof(WorkbenchStrings.ThemeAiThreeProductsRunOutBefore)]}\n\n- **{localizer[nameof(WorkbenchStrings.PosStockOatMilk)]}**: {localizer[nameof(WorkbenchStrings.ThemeAi4CartonsLeftEnoughUntil)]}\n- **{localizer[nameof(WorkbenchStrings.AssistantStockCroissants)]}**:"));

        return conversation;
    }

    /// <summary>A sentence whose two halves each cite a source, as a model anchors its citations.</summary>
    private (BitAiMessage Message, BitAiTextPart Part) CreateCitedText()
    {
        string first = localizer[nameof(WorkbenchStrings.ThemeAiRotterdamGrewMostThisWeek2)];
        string second = $" {localizer[nameof(WorkbenchStrings.ThemeAiOatMilkCosts2160)]}";

        var message = Reply(new BitAiTextPart(first + second));
        message.AddCitation(Citations[0] with { StartIndex = 0, EndIndex = first.Length });
        message.AddCitation(Citations[1] with { StartIndex = first.Length, EndIndex = first.Length + second.Length });

        return (message, message.PartsOf<BitAiTextPart>().First());
    }

    private BitAiFormDefinition CreateForm() => new()
    {
        Title = localizer[nameof(WorkbenchStrings.ThemeAiBookTheDelivery)],
        Description = localizer[nameof(WorkbenchStrings.ThemeAiCheckWhatIFilledIn)],
        SubmitLabel = localizer[nameof(WorkbenchStrings.ThemeAiSendTheOrder)],
        Fields =
        [
            new("store", localizer[nameof(WorkbenchStrings.PosStore)])
            {
                Type = BitAiFieldType.Select,
                Required = true,
                Value = "utrecht",
                Options = ["amsterdam", "rotterdam", "utrecht"],
                OptionLabels = ["Amsterdam", "Rotterdam", "Utrecht"]
            },
            new("date", localizer[nameof(WorkbenchStrings.ThemeAiDeliveryDate)]) { Type = BitAiFieldType.Date, Required = true, Value = "2026-05-22" },
            new("crates", localizer[nameof(WorkbenchStrings.ThemeAiCrates)]) { Type = BitAiFieldType.Number, Integer = true, Minimum = 1, Maximum = 40, Value = "12" },
            new("early", localizer[nameof(WorkbenchStrings.ThemeAiBeforeOpeningTime)]) { Type = BitAiFieldType.Boolean, Value = "true", Description = localizer[nameof(WorkbenchStrings.ThemeAiTheDriverHasAKey)] },
            new("notes", localizer[nameof(WorkbenchStrings.ThemeAiNotesForTheDriver)]) { Type = BitAiFieldType.MultilineText, Placeholder = localizer[nameof(WorkbenchStrings.ThemeAiAnythingTheDriverShouldKnow)] }
        ]
    };

    private string CreateMarkdown() => $$"""
        ## {{localizer[nameof(WorkbenchStrings.ThemeAiWeekendChecklist)]}}

        {{localizer[nameof(WorkbenchStrings.ThemeAiBeforeSaturdayEveryStoreChecks)]}}

        - [x] {{localizer[nameof(WorkbenchStrings.ThemeAiOrderTheOatMilk)]}}
        - [x] {{localizer[nameof(WorkbenchStrings.ThemeAiBookTheFridayDelivery)]}}
        - [ ] {{localizer[nameof(WorkbenchStrings.ThemeAiPutTheTerraceHeatersOut)]}}

        | Store | Opens | Closes |
        | --- | :---: | :---: |
        | Amsterdam | 07:30 | 18:00 |
        | Rotterdam | 06:30 | 18:00 |
        | Utrecht | 07:30 | 17:00 |

        > {{localizer[nameof(WorkbenchStrings.ThemeAiAFullShelfAtOpening)]}}

        ```json
        { "store": "rotterdam", "opens": "06:30" }
        ```
        """;

    /// <summary>A finished reply made of the given parts.</summary>
    private static BitAiMessage Reply(params BitAiMessagePart[] parts)
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        foreach (var part in parts)
        {
            message.AddPart(part);
        }

        return message;
    }

    /// <summary>A reply still being written, so far as the given text.</summary>
    private static BitAiMessage Streaming(string text)
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.SetStatus(BitAiMessageStatus.Streaming);
        message.AddPart(new BitAiTextPart()).Append(text);

        return message;
    }

    private static BitAiConversation Conversation(params BitAiMessage[] messages)
    {
        var conversation = new BitAiConversation();

        foreach (var message in messages)
        {
            conversation.AddMessage(message);
        }

        return conversation;
    }

    /// <summary>
    /// Saves the conversation and reads it back with the times a real one would have: on the samples' day, each finished
    /// reasoning a few seconds long, and the given agent runs in it. A reply still being written comes back stopped, so a
    /// conversation with one isn't settled.
    /// </summary>
    private static BitAiConversation Settle(BitAiConversation conversation, params BitAiAgentRunSnapshot[] runs)
    {
        var snapshot = BitAiConversationSerializer.ToSnapshot(conversation);

        return BitAiConversationSerializer.ToConversation(snapshot with
        {
            CreatedAt = today,
            UpdatedAt = today,
            Runs = runs,
            Messages = [.. snapshot.Messages.Select(message => message with
            {
                CreatedAt = today,
                Parts = [.. message.Parts.Select(part => part.Kind is BitAiMessagePartKinds.Reasoning
                    ? part with { StartedAt = today, CompletedAt = today.AddSeconds(Math.Clamp((part.Text?.Length ?? 0) / 16, 3, 12)) }
                    : part)]
            })]
        });
    }

    /// <summary>What the model reports about a finished reply: its tokens and how long it took, from a request on the samples' day.</summary>
    private static BitAiGenerationMetadata Metadata(long inputTokens, long outputTokens, double seconds) => new()
    {
        ModelId = ModelId,
        Usage = new() { InputTokens = inputTokens, OutputTokens = outputTokens },
        StartedAt = today,
        TimeToFirstToken = TimeSpan.FromMilliseconds(420),
        CompletedAt = today.AddSeconds(seconds),
        FinishReason = BitAiFinishReason.Stop
    };

    private static BitAiToolDefinition Definition(string toolName, string title, string iconName, BitAiToolRisk risk = BitAiToolRisk.ReadOnly) =>
        new(toolName) { Title = title, Icon = iconName, Risk = risk, RequiresApproval = risk is BitAiToolRisk.ReadOnly ? null : true };

    /// <summary>A tool call whose arguments the model has finished writing.</summary>
    private static BitAiToolCallPart Call(string toolCallId, string toolName, string title, string iconName, string arguments, BitAiToolRisk risk = BitAiToolRisk.ReadOnly)
    {
        var call = new BitAiToolCallPart(toolCallId, toolName) { Definition = Definition(toolName, title, iconName, risk) };
        call.CompleteArguments(Json(arguments));

        return call;
    }

    /// <summary>A call that waits for the user's approval, the way a session leaves it.</summary>
    private static BitAiToolCallPart Approval(string toolCallId, string toolName, string title, string iconName, BitAiToolRisk risk, string? reason)
    {
        var arguments = risk is BitAiToolRisk.Destructive
            ? """{"price_list":"2026-03"}"""
            : """{"supplier":"dekker","store":"utrecht","delivery":"2026-05-22T07:00"}""";
        var call = Call(toolCallId, toolName, title, iconName, arguments, risk);
        call.ApprovalReason = reason;
        call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

        return call;
    }

    /// <summary>A finished generative UI part that asks for the named component with the given props.</summary>
    private static BitAiGenerativeUiPart Generative(string componentName, JsonObject props)
    {
        var part = new BitAiGenerativeUiPart(componentName);
        part.SetProps(Json(props.ToJsonString()));
        part.Complete();

        return part;
    }

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }
}
