using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ToolApproval;

public partial class BitAiToolApprovalDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Call",
            Type = "BitAiToolCallPart",
            DefaultValue = "",
            Description = "The call awaiting a decision (required). Its ApprovalReason is shown as the question when it has one, and the Risk of its Definition decides how the prompt looks - read again on every render, since a session resolves the definition onto the call only as it starts running it. Without a Session, the decision is recorded on it (BitAiToolCallPart.SetApproval).",
            LinkType = LinkType.Link,
            Href = "/components/ai/toolcallview#tool-call-part",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiToolApprovalClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the tool approval.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "OnDecision",
            Type = "EventCallback<BitAiApprovalDecision>",
            DefaultValue = "",
            Description = "Raised after a decision - Approved, ApprovedAlways or Rejected - once it has been handed to the session, or recorded on the call when there is none. Nothing is raised while the prompt is disabled.",
            LinkType = LinkType.Link,
            Href = "#approval-decision-enum",
        },
        new()
        {
            Name = "Session",
            Type = "BitAiChatSession?",
            DefaultValue = "null",
            Description = "The session that is waiting for the decision, which it is handed to (BitAiChatSession.Approve): that is what resumes the paused turn, and what lets the approval policy remember an always-allow. Required for the decision to take effect in a session; without one, it is only recorded on the Call, which is what a layout driving its own tool loop reads.",
            LinkType = LinkType.Link,
            Href = "/components/ai/chat#chat-session",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiToolApprovalClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the tool approval.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiToolApprovalClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiToolApproval.",
                },
                new()
                {
                    Name = "Text",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the question (or the reason the approval was asked for) of the BitAiToolApproval.",
                },
                new()
                {
                    Name = "Actions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the row of the answer buttons of the BitAiToolApproval.",
                },
                new()
                {
                    Name = "RejectButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the decline button of the BitAiToolApproval.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles",
                },
                new()
                {
                    Name = "ApproveAlwaysButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the always-allow button of the BitAiToolApproval.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles",
                },
                new()
                {
                    Name = "ApproveButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the allow-once button of the BitAiToolApproval.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles",
                },
            ],
        },
        new()
        {
            Id = "tool-definition",
            Title = "BitAiToolDefinition",
            Description = "The provider-independent description of a tool: what it is called, what it does, what arguments it takes - and how the UI presents it. A tool created with BitAiTool.Create carries one (adjust it through the configure delegate: definition => definition with { Title = ..., Risk = ... }); the session copies it onto every call of the tool (BitAiToolCallPart.Definition).",
            Parameters =
            [
                new()
                {
                    Name = "BitAiToolDefinition(name)",
                    Type = "constructor",
                    DefaultValue = "",
                    Description = "Creates a definition for the tool the model calls by that name.",
                },
                new()
                {
                    Name = "Name",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The identifier the model uses to call the tool.",
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A human-friendly label, which the approval question and the tool card name the tool by.",
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "What the tool does - the text the model reads when deciding to call it.",
                },
                new()
                {
                    Name = "ParametersSchema",
                    Type = "JsonElement?",
                    DefaultValue = "null",
                    Description = "A JSON Schema object describing the arguments.",
                },
                new()
                {
                    Name = "Risk",
                    Type = "BitAiToolRisk",
                    DefaultValue = "BitAiToolRisk.ReadOnly",
                    Description = "How much damage the tool can do. The default approval policy asks before every call of a Destructive tool, and the approval prompt of one is drawn in the error role.",
                    LinkType = LinkType.Link,
                    Href = "#tool-risk-enum",
                },
                new()
                {
                    Name = "RequiresApproval",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Whether a human must approve each call before it runs, either way. Left null, the approval policy decides - by default, from the Risk.",
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "An icon name for the host's own tool UI (a custom renderer); the built-in card draws its own tool glyph.",
                },
                new()
                {
                    Name = "Metadata",
                    Type = "IReadOnlyDictionary<string, object?>?",
                    DefaultValue = "null",
                    Description = "Values of the application's own, carried through to custom tool renderers.",
                },
                new()
                {
                    Name = "DisplayName",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The label to show for the tool: Title, else Name.",
                },
            ],
        },
        new()
        {
            Id = "approval-policy",
            Title = "IBitAiToolApprovalPolicy",
            Description = "Decides which calls wait for a human decision before they run. AddBitBlazorUIAIServices registers BitAiDefaultToolApprovalPolicy - scoped, or a singleton with trySingleton, which a Blazor Server app must not pass since the policy remembers what a user allowed always. Replace it with ai.UseApprovalPolicy<T>() (always scoped), or hand a session one of its own (new BitAiChatSession(transport, approvalPolicy: ...)).",
            Parameters =
            [
                new()
                {
                    Name = "RequiresApprovalAsync(definition, invocation, cancellationToken?)",
                    Type = "ValueTask<bool>",
                    DefaultValue = "",
                    Description = "Whether this call of the tool needs a decision. Handed the definition and the invocation (with the arguments), so a policy of the app's own can decide by argument or by user. A policy that throws is treated as asking for approval.",
                },
                new()
                {
                    Name = "BitAiDefaultToolApprovalPolicy.RememberAlwaysApproved(toolName)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "The default policy asks for every Destructive tool and honours RequiresApproval either way, except for the tools a user allowed always - which the session records here when the decision is ApprovedAlways.",
                },
            ],
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "tool-risk-enum",
            Name = "BitAiToolRisk",
            Description = "How much damage a tool can do, used to pick the default approval policy and to style the approval prompt.",
            Items =
            [
                new() { Name = "ReadOnly", Description = "Reads data and has no side effects.", Value = "0" },
                new() { Name = "Write", Description = "Changes state in a way that can be undone.", Value = "1" },
                new() { Name = "Destructive", Description = "Destructive, irreversible or visible outside the app. The default policy asks before every call, and the prompt is drawn in the error role.", Value = "2" },
            ],
        },
        new()
        {
            Id = "approval-decision-enum",
            Name = "BitAiApprovalDecision",
            Description = "A human decision on an approval request.",
            Items =
            [
                new() { Name = "Undecided", Description = "No decision has been made yet.", Value = "0" },
                new() { Name = "Approved", Description = "Allowed for this call only.", Value = "1" },
                new() { Name = "Rejected", Description = "Declined: the call does not run, and is reported to the model as a failure.", Value = "2" },
                new() { Name = "ApprovedAlways", Description = "Allowed, and later calls of the same tool are not asked about again.", Value = "3" },
            ],
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiToolApproval-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Text of the question." },
        new() { Name = "--bit-AiToolApproval-background", DefaultValue = "var(--bit-clr-wrn-tint)", Description = "Surface of the prompt." },
        new() { Name = "--bit-AiToolApproval-destructive-background", DefaultValue = "var(--bit-clr-err-tint)", Description = "Surface of the prompt of a tool declared Destructive, which wins over --bit-AiToolApproval-background." },
        new() { Name = "--bit-AiToolApproval-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner radius of the prompt. Inside a BitAiToolCallView the prompt is the foot of the card and square." },
        new() { Name = "--bit-AiToolApproval-padding", DefaultValue = "spacing(1.5)", Description = "Inset of the prompt." },
        new() { Name = "--bit-AiToolApproval-gap", DefaultValue = "spacing(1)", Description = "Room between the question and the buttons, and between the buttons." },
    ];



    private static BitAiToolCallPart AwaitingApproval(string toolName, string title, BitAiToolRisk risk, string? reason = null)
    {
        var call = new BitAiToolCallPart(BitAiId.NewToolCallId(), toolName)
        {
            Definition = new BitAiToolDefinition(toolName) { Title = title, Risk = risk, RequiresApproval = true },
            ApprovalReason = reason,
        };

        call.CompleteArguments();
        call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

        return call;
    }



    private BitAiToolCallPart basicCall = AwaitingApproval("archive_thread", "Archive the thread", BitAiToolRisk.Write);
    private BitAiApprovalDecision basicDecision;

    private void ResetBasic()
    {
        basicCall = AwaitingApproval("archive_thread", "Archive the thread", BitAiToolRisk.Write);
        basicDecision = BitAiApprovalDecision.Undecided;
    }



    private readonly BitAiToolCallPart reasonCall = AwaitingApproval("send_email", "Send email", BitAiToolRisk.Write,
        "Send the weekly summary to the 3 people on the thread?");



    private readonly BitAiToolCallPart readOnlyCall = AwaitingApproval("read_logs", "Read the logs", BitAiToolRisk.ReadOnly);
    private readonly BitAiToolCallPart writeCall = AwaitingApproval("send_email", "Send email", BitAiToolRisk.Write);
    private readonly BitAiToolCallPart destructiveCall = AwaitingApproval("delete_records", "Delete records", BitAiToolRisk.Destructive,
        "Permanently delete the 128 archived records? This cannot be undone.");
    private string riskDecision = "none yet";



    [AutoInject] private IBitAiToolRegistry toolRegistry = default!;

    private BitAiRenderScheduler sessionRenders = default!;
    private BitAiChatSession session = default!;

    // The call of the latest turn, whatever it is waiting for.
    private BitAiToolCallPart? SessionCall => session.Conversation.Messages
        .LastOrDefault(m => m.Role.IsAssistant)?
        .PartsOf<BitAiToolCallPart>()
        .FirstOrDefault();

    protected override Task OnInitAsync()
    {
        sessionRenders = new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));

        session = new BitAiChatSession(
            AiDemoToolTransports.CallsTool(
                "delete_records",
                """{"filter":"status = 'archived'"}""",
                "Done."),
            tools: toolRegistry,
            approvalPolicy: new BitAiDefaultToolApprovalPolicy());
        session.StateChanged += sessionRenders.Request;

        return base.OnInitAsync();
    }

    private Task AskToDeleteAsync() => session.SubmitAsync("Delete the archived records");

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (session is not null) await session.DisposeAsync();
        if (sessionRenders is not null) await sessionRenders.DisposeAsync();

        await base.DisposeAsync(disposing);
    }



    private readonly BitAiUiOptions persianOptions = BitAiUiOptions.Default with { Strings = new PersianAiStrings() };
    private readonly BitAiToolCallPart rtlCall = AwaitingApproval("delete_records", "حذف رکوردها", BitAiToolRisk.Destructive,
        "۱۲۸ رکورد بایگانی‌شده برای همیشه حذف شوند؟ این کار برگشت‌پذیر نیست.");
    private BitAiApprovalDecision rtlDecision;

    private sealed class PersianAiStrings : BitAiStrings
    {
        public override string ApproveTool(string tool) => $"اجازه‌ی اجرای {tool} داده شود؟";
        public override string Approve => "اجازه";
        public override string ApproveAlways => "همیشه اجازه";
        public override string Reject => "رد";
    }



    private readonly BitAiToolApprovalParams[] approvalParams =
    [
        new() { Styles = new() { ApproveButton = new() { Root = "border-radius: 1rem" }, Text = "font-style: italic" } },
    ];
}
